using System;
using SimpleRESTServer;

namespace Sample
{
	// NOTE: Instead of implementing an "empty" user manager you sould use SimpleRESTServer.DummyUserMgr
	public class MyUserMgr : IUserMgr
	{
		public MyUserMgr()
		{
		}

		public bool UpdateUser(User a_oUser)
		{
			return true;
		}

		public string GetCookieByLogin(string a_strUser, string a_strPw)
		{
			return null;
		}

		public void LogoutWithCookie(string a_strCookie)
		{
		}

		public User GetUserByCookie(string a_strCookie)
		{
			return null;
		}

		public User GetUserByLogin(string a_strUser, string a_strPw)
		{
			return null;
		}

		public User GetUserByKey(string a_strKey)
		{
			return null;
		}
	}

}

