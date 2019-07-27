# Interconnect
A lightweight AMQP-inspired library for reliable binary transfer between applications.


# Motivation
I was working on distributed application which replicates data between services. I initial hand-rolled a simple protocol on top of the TCP/IP layer but it was ultimately too simple and lacked the reliability needed. I then gave the AMPQ library a go but found it a little too heavy weight and messaging system focussed (surprise, suprise).

Long story short - this library is the result. Hand-rolled again, but borrowing a number of concepts from various other places.


# Features
* Handles binary content only - meaning anything can be transferred (application decodes as necessary);
* Ability to provide data on the initial connection;
* Ability to send messages to and from a server (bi-directional);
* Ability to acknowledge that a message has been received/processed;
* Ability to explicitly reject a message;
* Ability to respond to a message with data;
* Support secure SSL connections;
* Simple API with versatile message support;
* Supports IPv4 and IPv6;
* Thread-safe;
* Reliable


# Contributing
Any feedback, issues, and contributions would be very much appreciated. Specific areas include:

* Fixes
* New features
* Testing
* Real world usage
* Performance improvements
* etc...


# Future
In the near future I'm planning on the following (bear with me):

* Automated build and NuGet package publishing (likely through Azure Pipelines);
* Technical protocol documentation for contributors


# How to use?
Below is a very simple example to get you started (there's a slightly more involved example project in the repo).

### Server
```csharp
var listener = new Listener("127.0.0.1", 5000, SslSettings.NoSsl);
listener.OnConnection += (s, connection) =>
{
	connection.OnSession += (s, session) =>
	{
		session.OnMessage += (s, msg) =>
		{
			Console.WriteLine(Encoding.UTF8.GetString(msg.Data));
			msg.Accept();
		};
		session.Accept();
	};
};
listener.Start();
```

**Notes**
* Sessions and messages can be accepted or rejected. If neither are done then a timeout exception will be thrown
	

### Client
```csharp
var connection = new Connection("127.0.0.1", 5000, SslSettings.NoSsl);
var session = connection.CreateSession("myNewSession");
var response = session.Send(Encoding.UTF8.GetBytes("Test message"));
if (response.Accepted)
{
	Console.WriteLine("Message accepted by server");
}
else
{
	Console.WriteLine("Message rejected by server");
}

// do something a bit more useful
connection.Close();
```























