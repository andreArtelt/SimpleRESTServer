# SimpleRESTSever
This repository contains a minimalistic REST server.

## About
SimpleRestServer is a **simple** and **minimalistic REST server** written in
**C#**. Currently **1-layer-api** and following **authentication
schemes** are supported:
 - None
 - HttpBasic
 - Custom cookies
 - Key

## Requirements
 - .NET framework >= 4.5
 - [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) >= 6

The code has been tested on ubuntu using mono (but it should work on other systems too).

## How to use?
**Binaries** can be obtained from [here](https://github.com/andreArtelt/SimpleRESTServer/releases).

You can find a **sample program** in the
[Sample](https://github.com/andreArtelt/SimpleRESTServer/tree/master/Sample) folder.

### Quick start guide
For a new server you have to create an instance of the ``Server`` class.
```cs
var oSever = new Server(EAuthenticationTypes.eBasic, new MyUserMgr (), new string[]{ "http://*:8080/" });
```
In this case your new sever uses *basic http for authentication* (first argument) and
is listening on *port 8080 using unencrypted http* (third argument).

**Note:** If you want to use *https* you have to use smth. like ``"https://:8443/"``
and provide/install the certificate (e.g. on windows: ``netsh http add sslcert ipport=0.0.0.0:8443 certhash=INSERT_CERTHASH appid={INSERT_APPID}``).

In the second argument you have to give an implementation (of ``IUserMgr``) of a user manager.
Here *MyUserMgr* implements the user management which has to be an implementation of ``IUserMgr`` and is used by the server internally.

Depending on the used authentication scheme you have to provide implementations of different
methods. In this case of basic http authentication you have to implement the
``User GetUserByLogin(string, string)`` method. It creates an ``User`` object which represents the user given by the name and password.

**Note:** Take a look at *IUserMgr.cs* to see which methods for which authentication scheme have to be implemented.

**Note:** You can use your own user class but it has to be derived from ``User``
which has some methods used for checking authorization by the server.
```cs
public class MyUserMgr : IUserMgr
{
  ....

  // BasicHttp authentication
  public User GetUserByLogin(string a_strUser, string a_strPw)
  {
    return null;
  }
}
```

**Note:** You always have to provide an implementation of ``IUserMgr`` even if you do not use any authentication!
(in the case of not authentication you might want to use the predefined ``DummyUserMgr``)

Next you have to add some routes/methods to your server. You can do so by adding one or more controllers.
Each controller is a class which is dervied from ``Controller``. Each route is assigned
a method which is marked by a ``RoutingAttribute``. In this attribute you have to specifiy
the *path* of the route as well as the *http type* (GET, POST, ...) and
the *role* a user has to be in if he wants to access this method (**Note:** Assigning roles to users is done in/by the user manager).

A method implementing a route does not return anything. Instead a method can manipulate the response
by using one of the predefined methods in the base class ``Controller`` or by accessing the response
via ``Response`` (*Note:* You can access the request by ``Request``, the user manager by ``UserMgr`` and
the current user by ``CurrentUser``).

**Attention:** You have are responsible that your methods are thread safe!
```cs
public class MyController : Controller
{
  ....

  [RoutingAttribute("/AnotherMethod", "GET", "Anonymous")]
  public void AnotherMethod()
  {
    SetCookie (new System.Net.Cookie("mycookie", "1234567890"));
    Ok (new MyTestClass());
  }

  [RoutingAttribute("/JsonMethod", "POST", "Anonymous")]
  public void JsonMethod(MyTestClass a_oData)
  {
    Console.WriteLine (a_oData.id + " " + a_oData.name);
    Ok ();
  }

  [RoutingAttribute("/AdminMethod", "GET", "Admin")]
  public void AdminMethod()
  {
    // TODO: Do smth.
    Ok ();
  }

  ....
}
```

Adding a controller to the sever can be done by using the ``AddController`` method.
```cs
oServer.AddController(new MyController());
```

Finally you can start/stop the server by calling ``Start()``/``Stop()``.

## Contribution
Collaboration/Contributions/Pull-requests are highly welcome.

## License
Licensed under MIT license (see LICENSE).
