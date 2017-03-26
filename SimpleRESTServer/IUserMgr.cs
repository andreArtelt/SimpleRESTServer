using System;

namespace SimpleRESTServer
{
	public interface IUserMgr
	{
		/// <summary>
		/// Updates a given user.
		/// NOTE: You should implement this method if a user can be updated (e.g. change password or other settings)
		/// </summary>
		/// <returns><c>true</c>, if user was updated successfully, <c>false</c> otherwise.</returns>
		/// <param name="a_oUser">User object to update.</param>
		bool UpdateUser(User a_oUser);

		/// <summary>
		/// Gets the cookie for a given login (given a user name and pasword).
		/// NOTE: You have to implement this method if you use the cookie authentication scheme.
		/// </summary>
		/// <returns>The cookie (might be null or empty if invalid/wrong credentials are given).</returns>
		/// <param name="a_strUser">User name.</param>
		/// <param name="a_strPw">User password.</param>
		string GetCookieByLogin(string a_strUser, string a_strPw);

		/// <summary>
		/// Logouts the user specified by a given cookie.
		/// NOTE: You have to implement this method if you use the cookie authentication scheme.
		/// </summary>
		/// <param name="a_strCookie">Cookie.</param>
		void LogoutWithCookie(string a_strCookie);

		/// <summary>
		/// Gets the specific user object associated with a given cookie (if cookie is null/empty or invalid this method should return null).
		/// NOTE: You have to implement this method if you use the cookie authentication scheme.
		/// </summary>
		/// <returns>The user object (might be null).</returns>
		/// <param name="a_strCookie">Cookie.</param>
		User GetUserByCookie(string a_strCookie);

		/// <summary>
		/// Gets the user object for a given login (user name and password) (if login/credentials are invalid this method should return null).
		/// NOTE: You have to implement this method if you use the basic http authentication scheme.
		/// </summary>
		/// <returns>The user object (might be null).</returns>
		/// <param name="a_strUser">User name.</param>
		/// <param name="a_strPw">User password.</param>
		User GetUserByLogin(string a_strUser, string a_strPw);

		/// <summary>
		/// Gets the user object associcated with a given key (if key is invalid this method should return null).
		/// NOTE: You have to implement this method if you use the key authentication scheme.
		/// </summary>
		/// <returns>The user object (might be null).</returns>
		/// <param name="a_strKey">The key.</param>
		User GetUserByKey(string a_strKey);
	}
}