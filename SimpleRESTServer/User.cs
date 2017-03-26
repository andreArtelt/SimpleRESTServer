using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace SimpleRESTServer
{
	public class User : IPrincipal, IIdentity
	{
		protected string m_strName;
		protected string[] m_lRoles;
		protected bool m_bIsAuthenticated;

		public User(string a_strName, string[] a_lRoles, bool a_bIsAuthenticated)
		{
			m_strName = a_strName;
			m_lRoles = a_lRoles;
			m_bIsAuthenticated = a_bIsAuthenticated;
		}

		public IIdentity Identity
		{
			get
			{
				return this;
			}
		} 

		public string Name
		{
			get
			{
				return m_strName;
			}
			set
			{
				m_strName = value;
			}
		}

		public string[] Roles
		{
			get
			{
				return m_lRoles;
			}
		}

		public bool IsAuthenticated
		{
			get
			{
				return m_bIsAuthenticated;
			}
		}

		public string AuthenticationType
		{
			get
			{
				return "Custom";
			}
		}

		public bool IsInRole(string a_strRole)
		{
			foreach (var role in m_lRoles)
			{
				if (role.Equals (a_strRole))
					return true;
			}

			return false;
		}
	}
}