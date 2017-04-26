using System;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace SimpleRESTServer
{
	public class Controller
	{
		private IUserMgr m_oUserMgr;
		public IUserMgr UserMgr
		{
			get
			{
				return m_oUserMgr;
			}
			set
			{
				if(value != null && value is IUserMgr)
					m_oUserMgr = value;
			}
		}

		public Controller()
		{
		}

		public HttpListenerResponse Response
		{
			get
			{
				return (HttpListenerResponse) Thread.GetData(Thread.GetNamedDataSlot("Response"));
			}
		}

		public HttpListenerRequest Request
		{
			get
			{
				return (HttpListenerRequest) Thread.GetData(Thread.GetNamedDataSlot("Request"));
			}
		}

		public User CurrentUser
		{
			get
			{
				return (User) Thread.CurrentPrincipal;
			}
		}

		/// <summary>
		/// Gets the cookies from the request.
		/// </summary>
		/// <value>The cookies.</value>
		public CookieCollection Cookies
		{
			get
			{
				return Request.Cookies;
			}
		}

		/// <summary>
		/// Adds a cookie to the response.
		/// </summary>
		/// <param name="a_oCookie">Cookie.</param>
		public void SetCookie(Cookie a_oCookie)
		{
			Response.Cookies.Add(a_oCookie);
		}

		/// <summary>
		/// Sets response status code to bad request.
		/// </summary>
		public void BadRequest()
		{
			Response.StatusCode = (int) HttpStatusCode.BadRequest;
		}

		/// <summary>
		/// Sets response status code to internal server error.
		/// </summary>
		public void InternalError()
		{
			Response.StatusCode = (int) HttpStatusCode.InternalServerError;
		}

		/// <summary>
		/// Sets response status code to unauthorized.
		/// </summary>
		public void NotAuthenticated()
		{
			Response.StatusCode = (int) HttpStatusCode.Unauthorized;
		}

		/// <summary>
		/// Sets response status code to not found.
		/// </summary>
		public void NotFound()
		{
			Response.StatusCode = (int) HttpStatusCode.NotFound;
		}

		/// <summary>
		/// Sets response status code to forbidden.
		/// </summary>
		public void Forbidden()
		{
			Response.StatusCode = (int) HttpStatusCode.Forbidden;
		}

		/// <summary>
		/// Sets response status code to method not allowed.
		/// </summary>
		public void MethodNotAllowed()
		{
			Response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
		}

		/// <summary>
		/// Sets response status code to not implemented.
		/// </summary>
		public void NotImplemented()
		{
			Response.StatusCode = (int) HttpStatusCode.NotImplemented;
		}

		/// <summary>
		/// Sets response status code to service unavailable.
		/// </summary>
		public void ServiceUnavailable()
		{
			Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
		}

		/// <summary>
		/// Sets response status code to accepted.
		/// </summary>
		public void Accepted()
		{
			Response.StatusCode = (int) HttpStatusCode.Accepted;
		}

		/// <summary>
		/// Sets response status code to not modified.
		/// </summary>
		public void NotModified()
		{
			Response.StatusCode = (int) HttpStatusCode.NotModified;
		}

		/// <summary>
		/// Sets response status code to request timeout.
		/// </summary>
		public void RequestTimeout()
		{
			Response.StatusCode = (int) HttpStatusCode.RequestTimeout;
		}

		/// <summary>
		/// Sets response status code to not acceptable.
		/// </summary>
		public void NotAcceptable()
		{
			Response.StatusCode = (int) HttpStatusCode.NotAcceptable;
		}

		/// <summary>
		/// Sets response status code to created.
		/// </summary>
		public void Created()
		{
			Response.StatusCode = (int) HttpStatusCode.Created;
		}

		/// <summary>
		/// Sets response status code to ok.
		/// </summary>
		public void Ok()
		{
			Response.StatusCode = (int) HttpStatusCode.OK;
		}

		/// <summary>
		/// Sets response status code to ok and puts arbitrary data into the the response body.
		/// </summary>
		/// <param name="a_Data">The data as an byte array.</param>
		/// <param name="a_strContentType">Type of data (Content-Type in http response).</param>
		/// <param name="a_strContentEncoding">Encoding of data (Content-Encoding in http response).</param>
		public void Ok(byte[] a_Data, string a_strContentType, string a_strContentEncoding)
		{
			Response.ContentLength64 = a_Data.LongLength;
			Response.ContentType = a_strContentType;
			Response.AddHeader("Content-Encoding", a_strContentEncoding);
			Response.OutputStream.Write(a_Data, 0, a_Data.Length);
		}

		/// <summary>
		/// Sets response status code to ok and puts arbitrary text data into the the response body.
		/// </summary>
		/// <param name="a_strData">Data as a string.</param>
		/// <param name="a_strContentType">Optinal: Type of data (Content-Type in http response).</param>
		/// <param name="a_strContentEncoding">Optional: Encoding of data (Content-Encoding in http response).</param>
		public void Ok(string a_strData, string a_strContentType="text/plain", string a_strContentEncoding="identity")
		{
			Response.StatusCode = (int) HttpStatusCode.OK;

			Response.ContentLength64 = a_strData.Length;
			Response.ContentType = a_strContentType;
			Response.AddHeader("Content-Encoding", a_strContentEncoding);
			using (TextWriter oWriter = new StreamWriter(Response.OutputStream))
				oWriter.Write(a_strData);
		}

		/// <summary>
		/// Sets response status code to ok and puts an object (serialized to json) into the response body.
		/// </summary>
		/// <param name="a_oData">Data as an object.</param>
		public void Ok(object a_oData)
		{
			Response.StatusCode = (int) HttpStatusCode.OK;

			// Serialize object
			string strData = JsonConvert.SerializeObject(a_oData);

			byte[] data = System.Text.Encoding.UTF8.GetBytes(strData);

			Ok(data, "application/json; charset=utf-8", "identity");
		}
	}
}
