using System;

namespace SimpleRESTServer
{
	/// <summary>
	/// Dummy user manager (no authentication!). You may want to use this one if you do not need any authentication scheme.
	/// </summary>
	public class DummyUserMgr : IUserMgr
	{
		public DummyUserMgr()
		{
		}

		public bool UpdateUser(User a_oUser)
		{
			throw new NotImplementedException();
		}

		public string GetCookieByLogin(string a_strUser, string a_strPw)
		{
			throw new NotImplementedException();
		}

		public void LogoutWithCookie(string a_strCookie)
		{
			throw new NotImplementedException();
		}

		public User GetUserByCookie(string a_strCookie)
		{
			throw new NotImplementedException();
		}

		public User GetUserByLogin(string a_strUser, string a_strPw)
		{
			throw new NotImplementedException();
		}

		public User GetUserByKey(string a_strKey)
		{
			throw new NotImplementedException();
		}
	}
}