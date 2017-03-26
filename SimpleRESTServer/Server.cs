using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SimpleRESTServer
{
	/// <summary>
	/// Enumeration of all supported authentication schemes.
	/// </summary>
	public enum EAuthenticationTypes
	{
		eNone, eCookie, eBasic, eKey
	}

	public class Server
	{
		protected List<Controller> m_lController;
		protected IUserMgr m_oUserMgr;
		protected HttpListener m_oListener;
		protected EAuthenticationTypes m_eAuthenticationType;
		protected IEnumerable<string> m_lPrefixes;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRESTServer.Server"/> class.
		/// </summary>
		/// <param name="a_eAuthType">Type of authentication scheme.</param>
		/// <param name="a_oUserMgr">Instance of a user manager.</param>
		/// <param name="a_lPrefixes">List of prefixes (specifing path, port and base url).</param>
		public Server(EAuthenticationTypes a_eAuthType, IUserMgr a_oUserMgr, IEnumerable<string> a_lPrefixes)
		{
			m_lController = new List<Controller>();

			m_eAuthenticationType = a_eAuthType;
			m_oUserMgr = a_oUserMgr;
			m_lPrefixes = a_lPrefixes;
		}

		/// <summary>
		/// Adds the controller.
		/// </summary>
		/// <param name="a_oController">A controller.</param>
		public void AddController(Controller a_oController)
		{
			if (a_oController != null)
			{
				a_oController.UserMgr = m_oUserMgr;
				m_lController.Add (a_oController);
			}
		}
			
		private IDictionary<string, Controller> collectControllerPaths()
		{
			var dictResult = new Dictionary<string, Controller>();

			foreach (Controller oCtrl in m_lController)
			{
				foreach (MethodInfo method in oCtrl.GetType().GetMethods())
				{
					// Methods needs to marked by RoutingAttribute
					object[] customAttribs = method.GetCustomAttributes(typeof(RoutingAttribute), true);
					if(customAttribs.Length < 1)
						continue;

					// ATTENTION: Assume one RoutingAttribute per method!
					var oRoutingAttrib = customAttribs[0] as RoutingAttribute;

					dictResult.Add(oRoutingAttrib.Path, oCtrl);
				}
			}

			return dictResult;
		}

		/// <summary>
		/// Run the sever.
		/// </summary>
		public void Run()
		{
			// Create server
			m_oListener = new HttpListener();
			m_oListener.IgnoreWriteExceptions = true;

			foreach(string prefix in m_lPrefixes)
				m_oListener.Prefixes.Add(prefix);

			switch (m_eAuthenticationType)
			{
				case EAuthenticationTypes.eCookie:
					m_oListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
					break;
				case EAuthenticationTypes.eBasic:
					m_oListener.AuthenticationSchemes = AuthenticationSchemes.Basic;
					break;
				case EAuthenticationTypes.eNone:
					m_oListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
					break;
				case EAuthenticationTypes.eKey:
					m_oListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
					break;
			}

			// Collect method paths
			var dictControllerPaths = collectControllerPaths();

			// Start server
			m_oListener.Start();

			// Process clients
			while (true)
			{
				// Wait for new client request
				HttpListenerContext ctx;
				try
				{
					ctx = m_oListener.GetContext();
				}
				catch(Exception)
				{
					break;
				}
				if (ctx == null)
					continue;

				// Process request in different thread
				ThreadPool.QueueUserWorkItem ((_) => {
					try
					{
						HttpListenerRequest oRequest = ctx.Request;

						// Get current user
						User oUser = null;
						if (m_eAuthenticationType == EAuthenticationTypes.eCookie)
						{
							Cookie oAuthCookie = oRequest.Cookies ["auth"];
							if (oAuthCookie != null)
							{
								if(!oAuthCookie.Expired)
									oUser = m_oUserMgr.GetUserByCookie (oAuthCookie.Value);
							}
						}
						else if (m_eAuthenticationType == EAuthenticationTypes.eBasic)
						{
							if (ctx.User != null && ctx.User.Identity.IsAuthenticated)
							{
								HttpListenerBasicIdentity oIdentity = (HttpListenerBasicIdentity)ctx.User.Identity;
								oUser = m_oUserMgr.GetUserByLogin (oIdentity.Name, oIdentity.Password);
							}
						}
						else if(m_eAuthenticationType == EAuthenticationTypes.eKey)
						{
							if(oRequest.Headers.AllKeys.Contains("key"))
								oUser = m_oUserMgr.GetUserByKey(oRequest.Headers["key"]);
						}

						if(oUser == null)
							oUser = new User("", new string[]{"Anonymous"}, false);

						// Set name to client ip addr if authentication failed (e.g. wrong login or anonymous user)
						if (oUser.IsAuthenticated == false)
							oUser.Name = oRequest.RemoteEndPoint.ToString();
						
						Thread.CurrentPrincipal = oUser;

						// Extract method name and arguments from url
						string strMethodName = oRequest.Url.LocalPath;
						string strParam = null;
						if(dictControllerPaths.ContainsKey(strMethodName) == false)	// If method path can not be found: Maybe last segment is an argument
						{
							strParam = oRequest.Url.Segments.Last();
							strMethodName = strMethodName.Replace("/" + strParam, "");
						}
						var dictParamValues = HttpUtility.ParseQueryString(oRequest.Url.Query);

						// Read body (if available)
						string strBody = "";
						if(oRequest.HasEntityBody)
						{
							using (var oReader = new StreamReader(oRequest.InputStream, oRequest.ContentEncoding))
								strBody = oReader.ReadToEnd();
						}

						// Find method
						MethodInfo oCtrlMethod = null;
						if(dictControllerPaths.ContainsKey(strMethodName))
						{
							// Check all methods of controller
							var oCtrl = dictControllerPaths[strMethodName];
							MethodInfo[] methods = oCtrl.GetType().GetMethods();
							foreach(MethodInfo method in methods)
							{
								object[] customAttribs = method.GetCustomAttributes(typeof(RoutingAttribute), true);
								if(customAttribs.Length < 1)
									continue;

								// ATTENTION: Assume one RoutingAttribute per method!
								var oRoutingAttrib = customAttribs[0] as RoutingAttribute;
								if(oRoutingAttrib.Path.Equals(strMethodName))
								{
									if(oRoutingAttrib.Method.Method.Equals(oRequest.HttpMethod))
									{
										// Method/Controller found!
										oCtrlMethod = method;

										// Check if user is authorized to use this method
										if(oUser.IsInRole(oRoutingAttrib.Role))
										{
											// Create arguments
											ParameterInfo[] paramsInfo = oCtrlMethod.GetParameters();
											object[] methodParams = new object[paramsInfo.Length];
											for(int i=0; i != paramsInfo.Length; i++)
											{
												if(i == 0 && strParam != null)
												{
													methodParams[i] = Convert.ChangeType(strParam, paramsInfo[i].ParameterType);
												}
												else if(i < dictParamValues.AllKeys.Length)
												{
													if(dictParamValues.AllKeys.Contains(paramsInfo[i].Name))
														methodParams[i] = Convert.ChangeType(dictParamValues[paramsInfo[i].Name], paramsInfo[i].ParameterType);
													else   // Parameter not given in request!
													{
														methodParams = null;
														ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
														break;
													}
													continue;
												}
												else if(string.IsNullOrEmpty(strBody) == false)
												{
													if(string.IsNullOrEmpty(strBody) == false)
													{
														// Json data?
														if(oRequest.ContentType != null && oRequest.ContentType.Equals("application/json"))
														{
															try
															{
																methodParams[i] = JsonConvert.DeserializeObject(strBody, paramsInfo[i].ParameterType);
															}
															catch(Exception)
															{
																methodParams = null;
																ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
																break;
															}
														}
													}
													else
														methodParams[i] = strBody;
												}
												else  // Method needs more arguments than given! => Send BadRequest error
												{
													ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
													methodParams = null;
												}
											}

											if(methodParams != null)  // methodParams == null indicates bad request (see above)
											{
												// Execute method with arguments and creating response
												try
												{
													Thread.SetData(Thread.GetNamedDataSlot("Response"), ctx.Response);
													Thread.SetData(Thread.GetNamedDataSlot("Request"), ctx.Request);

													method.Invoke(oCtrl, methodParams);
												}
												catch(Exception)
												{
													ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
												}
											}
										}
										else   // Access denied!
											ctx.Response.StatusCode = (int) HttpStatusCode.Unauthorized;	

										break;
									}
								}
							}
						}
								
						// Send error if no controller matched
						if(oCtrlMethod == null)
							ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
					}
					catch(Exception)
					{
						ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
					}
					finally
					{
						// Send response
						ctx.Response.Close();
					}
				});
			}
		}

		/// <summary>
		/// Stop the server.
		/// </summary>
		public void Stop()
		{
			if (m_oListener != null)
			{
				m_oListener.Stop();
				m_oListener.Close();
				m_oListener = null;
			}
		}
	}
}