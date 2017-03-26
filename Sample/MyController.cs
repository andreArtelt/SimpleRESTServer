using System;
using SimpleRESTServer;

namespace Sample
{
	public class MyTestClass
	{
		public int id;
		public string name;

		public MyTestClass()
		{
			id = 0;
			name = "_";
		}
	}

	public class MyController : Controller
	{
		public MyController()
		{
		}

		[RoutingAttribute("/MyMethod", "GET", "Anonymous")]
		public void MyMethod()
		{
			Console.WriteLine(Request.LocalEndPoint.ToString());
			Ok();
		}

		[RoutingAttribute("/AnotherMethod", "GET", "Anonymous")]
		public void AnotherMethod()
		{
			SetCookie (new System.Net.Cookie("mycookie", "1234567890"));
			Ok(new MyTestClass());
		}

		[RoutingAttribute("/JsonMethod", "POST", "Anonymous")]
		public void JsonMethod(MyTestClass a_oData)
		{
			Console.WriteLine (a_oData.id + " " + a_oData.name);
			Ok ();
		}

		[RoutingAttribute("/objec/sub/subsub", "GET", "Anonymous")]
		public void ComplexMethod()
		{
			Ok ();
		}

		[RoutingAttribute("/AdminMethod", "GET", "Admin")]
		public void AdminMethod()
		{
			// TODO: Do smth.
			Ok ();
		}

		[RoutingAttribute("/RemoveItem", "DELETE", "Anonymous")]
		public void RemoveMethod(int a_iID)
		{
			Console.WriteLine(a_iID);
			Ok();
		}

		[RoutingAttribute("/index.htm", "GET", "Anonymous")]
		public void IndexFile()
		{
			Ok("<html><body></body></html>");
		}
			
		[RoutingAttribute("/Login", "POST", "Anonymous")]
		public void LoginMethod(string a_strUserPw)
		{
			// TODO: Call user manager for login!
			//UserMgr.GetCookieByLogin("", "");
			Console.WriteLine (a_strUserPw);
			Ok();
		}

		[RoutingAttribute("/GetFile", "GET", "Anonymous")]
		public void GetHttpMethod(string name, int id)
		{
			Console.WriteLine(name + " " + id);
			Ok ();
		}
	}
}

