using System;
using System.Net.Http;

namespace SimpleRESTServer
{
	public class RoutingAttribute : Attribute
	{
		public string Path;
		public HttpMethod Method;
		public string Role;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleRESTServer.RoutingAttribute"/> class.
		/// </summary>
		/// <param name="a_strPath">Path of route.</param>
		/// <param name="a_strMethod">Http method.</param>
		/// <param name="a_strRole">Role required for access.</param>
		public RoutingAttribute(string a_strPath, string a_strMethod="GET", string a_strRole="Anonymous")
		{
			Path = a_strPath;
			Role = a_strRole;

			if (a_strMethod.Equals("GET"))
				Method = HttpMethod.Get;
			else if (a_strMethod.Equals("POST"))
				Method = HttpMethod.Post;
			else if (a_strMethod.Equals("PUT"))
				Method = HttpMethod.Put;
			else if (a_strMethod.Equals("DELETE"))
				Method = HttpMethod.Delete;
			else if (a_strMethod.Equals("TRACE"))
				Method = HttpMethod.Trace;
			else if (a_strMethod.Equals("HEAD"))
				Method = HttpMethod.Head;
			else
				throw new ArgumentException();
		}
	}
}