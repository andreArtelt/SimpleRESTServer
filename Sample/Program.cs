using System;
using System.Threading;
using SimpleRESTServer;

namespace Sample
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// Create a new server, without any authentication scheme and listening on 8080 using the http protocol (no encryption!)
			Server oMyServer = new Server(EAuthenticationTypes.eNone, new MyUserMgr(), new string[]{ "http://*:8080/" });

			// Add an controller (handling some routes)
			oMyServer.AddController(new MyController());

			// Run sever in new thread
			new Thread(delegate(){oMyServer.Run();}).Start();

			// Quit server if user pressed any key
			Console.WriteLine("Press any key to quit server...");
			Console.ReadKey();
			oMyServer.Stop();
		}
	}
}
