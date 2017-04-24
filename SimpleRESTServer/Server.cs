using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Specialized;
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
		protected IDictionary<string, Controller> m_dictControllerPaths;
		public static string AuthCookieName = "auth";
		public static string AuthKeyName = "key";
		public static string AuthAnonymousUserName = "Anonymous";

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

					if(dictResult.ContainsKey(oRoutingAttrib.Path) == false)	// ATTENTION: Assure one controller per route (identical routes have to be in the same controller!)
						dictResult.Add(oRoutingAttrib.Path, oCtrl);
				}
			}

			return dictResult;
		}

		protected virtual User handleAuthentication(ref HttpListenerRequest a_oRequest, ref HttpListenerContext ctx)
		{
			User oUser = null;
			
			if (m_eAuthenticationType == EAuthenticationTypes.eCookie)
			{
				Cookie oAuthCookie = a_oRequest.Cookies [AuthCookieName];
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
				if(a_oRequest.Headers.AllKeys.Contains(AuthKeyName))
					oUser = m_oUserMgr.GetUserByKey(a_oRequest.Headers[AuthKeyName]);
			}

			if(oUser == null)
				oUser = new User("", new string[]{AuthAnonymousUserName}, false);

			// Set name to client ip addr if authentication failed (e.g. wrong login or anonymous user)
			if (oUser.IsAuthenticated == false)
				oUser.Name = a_oRequest.RemoteEndPoint.ToString();

			return oUser;
		}

		protected virtual void parseRequest(ref HttpListenerRequest a_oRequest, ref string a_strMethodName, ref string a_strParam, ref NameValueCollection a_dictParamValues, ref string a_strBody)
		{
			a_strMethodName = a_oRequest.Url.LocalPath;
			a_strParam = null;
			if(m_dictControllerPaths.ContainsKey(a_strMethodName) == false)	// If method path can not be found: Maybe last segment is an argument
			{
				a_strParam = a_oRequest.Url.Segments.Last();
				a_strMethodName = a_strMethodName.Replace("/" + a_strParam, "");
			}
			a_dictParamValues = HttpUtility.ParseQueryString(a_oRequest.Url.Query);

			// Read body (if available)
			a_strBody = "";
			if(a_oRequest.HasEntityBody)
			{
				using (var oReader = new StreamReader(a_oRequest.InputStream, a_oRequest.ContentEncoding))
					a_strBody = oReader.ReadToEnd();
			}
		}

		/// <summary>
		/// Allows or denies CORS (Overwrite this method to customize the behaviour).
		/// </summary>
		/// <param name="a_oContext">Http context.</param>
		protected virtual void allowCORS(ref HttpListenerContext a_oContext)
		{
			a_oContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
		}

		/// <summary>
		/// Handles OPTIONS request.
		/// </summary>
		/// <returns>Returna <c>true</c> if no further handling of this request is required, <c>false</c> otherwise.</returns>
		/// <param name="a_oContext">Http context.</param>
		protected virtual bool handleOptionsRequest(ref HttpListenerContext a_oContext)
		{
			a_oContext.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, PUT, DELETE, OPTIONS");	// Allow "everything" by default
			a_oContext.Response.Headers.Add("Access-Control-Request-Headers", "X-PINGOTHER, Content-Type");

			return true;
		}

		/// <summary>
		/// Finds the method & controller for a given request.
		/// </summary>
		/// <returns>The method (might be null if no suiteable method has been found. In this case appropriate response status codes are set!).</returns>
		/// <param name="a_strMethodName">Name/Path of the requested method.</param>
		/// <param name="a_oContext">Http context.</param>
		protected virtual MethodInfo findMethod(string a_strMethodName, ref HttpListenerContext a_oContext)
		{
			if(m_dictControllerPaths.ContainsKey(a_strMethodName))
			{
				// Check all methods of controller
				var oCtrl = m_dictControllerPaths[a_strMethodName];
				MethodInfo[] methods = oCtrl.GetType().GetMethods ();
				foreach(MethodInfo method in methods)
				{
					object[] customAttribs = method.GetCustomAttributes(typeof(RoutingAttribute), true);
					if(customAttribs.Length < 1)
						continue;

					// ATTENTION: Assume one RoutingAttribute per method!
					var oRoutingAttrib = customAttribs[0] as RoutingAttribute;
					if(oRoutingAttrib.Path.Equals(a_strMethodName))	 // Check name/path
					{
						if(oRoutingAttrib.Method.Method.Equals(a_oContext.Request.HttpMethod))   // Check type of http method
						{
							// Check if user is authorized to use this method
							if(Thread.CurrentPrincipal.IsInRole(oRoutingAttrib.Role))
								return method;
							else  // Access denied!
							{
								a_oContext.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
								return null;
							}
						}
					}
				}
			}

			a_oContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
			return null;
		}

		/// <summary>
		/// Execute a given method.
		/// </summary>
		/// <param name="a_oMethod">Method to be executed.</param>
		/// <param name="a_strMethodName">Method name.</param>
		/// <param name="a_strParam">Parameter for method (parsed in <see cref="parseRequest"/>).</param>
		/// <param name="a_strBody">Body of request (parsed in <see cref="parseRequest"/>).</param>
		/// <param name="a_dictParamValues">Collection of parameter and their values (parsed in <see cref="parseRequest"/>).</param>
		/// <param name="a_oContext">Http context.</param>
		protected virtual void execMethod(MethodInfo a_oMethod, string a_strMethodName, string a_strParam, string a_strBody, NameValueCollection a_dictParamValues, ref HttpListenerContext a_oContext)
		{
			// Create arguments
			ParameterInfo[] paramsInfo = a_oMethod.GetParameters();
			object[] methodParams = new object[paramsInfo.Length];
			for(int i=0; i != paramsInfo.Length; i++)
			{
				if(i == 0 && a_strParam != null)
				{
					methodParams[i] = Convert.ChangeType(a_strParam, paramsInfo[i].ParameterType);
				}
				else if(i < a_dictParamValues.AllKeys.Length)
				{
					if(a_dictParamValues.AllKeys.Contains(paramsInfo[i].Name))
						methodParams[i] = Convert.ChangeType(a_dictParamValues[paramsInfo[i].Name], paramsInfo[i].ParameterType);
					else   // Parameter not given in request!
					{
						methodParams = null;
						a_oContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
						break;
					}
					continue;
				}
				else if(string.IsNullOrEmpty(a_strBody) == false)
				{
					// Json data?
					if(a_oContext.Request.ContentType != null && a_oContext.Request.ContentType.Contains("application/json"))
					{
						try
						{
							methodParams[i] = JsonConvert.DeserializeObject(a_strBody, paramsInfo[i].ParameterType);
						}
						catch(Exception)
						{
							methodParams = null;
							a_oContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
							break;
						}
					}
					else
						methodParams[i] = a_strBody;
				}
				else  // Method needs more arguments than given! => Send BadRequest error
				{
					a_oContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
					methodParams = null;
				}
			}

			if(methodParams != null)  // methodParams == null indicates bad request (see above)
			{
				// Execute method with arguments and creating response
				try
				{
					Thread.SetData(Thread.GetNamedDataSlot("Response"), a_oContext.Response);
					Thread.SetData(Thread.GetNamedDataSlot("Request"), a_oContext.Request);

					a_oMethod.Invoke(m_dictControllerPaths[a_strMethodName], methodParams);
				}
				catch(Exception)
				{
					a_oContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
				}
			}
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
			m_dictControllerPaths = collectControllerPaths();

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
						User oUser = handleAuthentication(ref oRequest, ref ctx);

						Thread.CurrentPrincipal = oUser;

						// Allow or deny CORS (Cross-Origin Resource Sharing)
						allowCORS(ref ctx);

						// Handle OPTIONS request
						if(oRequest.HttpMethod.Equals("OPTIONS"))
						{
							if(handleOptionsRequest(ref ctx) == true)
								return;
						}

						// Extract method name and arguments from url
						string strMethodName = "", strParam = "", strBody = "";
						NameValueCollection dictParamValues = null;
						
						parseRequest(ref oRequest, ref strMethodName, ref strParam, ref dictParamValues, ref strBody);

						// Find method
						MethodInfo oCtrlMethod = findMethod(strMethodName, ref ctx);
						if(oCtrlMethod == null)
							return;

						// Execute method
						execMethod(oCtrlMethod, strMethodName, strParam, strBody, dictParamValues, ref ctx);
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