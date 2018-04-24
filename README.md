![](http://www.imgim.com/xeeny.png)


Framework For Building And Consuming Cross Platform Services In .Net Standard. 

Cross Platform, Duplex, Scalable, Configurable, and Extendable

## Table Of Content
* [What Is Xeeny](#what-is-xeeny)
* [Nuget](#nuget)
* [Features](#features)
* [Terminology](#terminology)
* [Get Started](#get-started)
    * [Server Side](#server-side)
    * [Client Side](#client-side)
* [Duplex and Callback](#duplex-and-callback)
    * [Duplex Server](#duplex-service)
    * [Duplex Client](#duplex-client)
* [Instance Lifetime](#instance-lifetime)
    * [Service Instance Lifetime](#service-instance-lifetime)
    * [Callback Instance Lifetime](#callback-instance-lifetime)
    * [Initializing Created Instances](#initializing-created-instances)
* [Operations](#operations)
* [Managing The Connection](#managing-the-connection)
	* [Access Connection From The Service](#access-connection-from-the-service)
   	* [Access Connection From The Client](#access-connection-from-the-client)
   	* [Connection Options](#connection-options)
	* [Connection Timeout](#connection-timeout)
	* [Connection Buffers](#connection-buffers)
   	* [Keep Alive Management](#keep-alive-management)
* [Serialization](#serialization)
* [Security](#security)
* [Logging](#logging)
* [Transports](#transports)
* [Performance](#performance)
* [Extend Xeeny](#extend-xeeny)
	* [Custom Transport](#custom-transport)
	* [Custom Protocol](#custom-protocol)
* [Samples](#samples)

## What is Xeeny
Xeeny is framework for building and consuming services on devices and servers that support .net standard.

With Xeeny you can host and consume services anywhere .net standard is able to work (e.g Xamarin android, Windows Server, ...).
It is Cross Platform, Duplex, Multiple Transports, Asynchronous, Typed Proxies, Configurable, and Extendable

## Nuget
```
Install-Package Xeeny

For extensions
Install-Package Xeeny.Extentions.Loggers
Install-Package Xeeny.Serialization.JsonSerializer
Install-Package Xeeny.Serialization.ProtobufSerializer
```

## Features
Current Features:
* Building and consuming services in .Net Standard
* Multiple Transports, You can mix transports
* Duplex connections
* Typed proxies
* Asynchronous, Scalable, and Lightweight
* Extendable, you can have your custom transport (e.g Behind bluetooth), custom protocol, and custom serialization

Comming:
* Integrating AspNetCore (Logger is done, next is DI, Kestrel, and Middleware)
* Streaming
* UDP transport
* P2P Framework

## Terminology
* **Contract**: A shared interface between the Service and Client
* **Service Contract**: The interface that is referenced by the client to call remotely, and actually implemented on the server
* **Callback Contract**: The interface that is referenced by the service to call remotely, and actually implemented on the client
* **Operation**: Any method in a Contract is called Operation

## Get Started
* Define service contract (an interface) shared between your client and server
```csharp
public interface IService
{
    Task<string> Echo(string message);
}
```
### Server Side
* Create the service that implements the contract, services without interfaces won't expose methods to clients
```csharp
public class Service : IService
{
    public Task<string> Echo(string message)
    {
        return Task.FromResult(message);
    }
}
```
* Create `ServiceHost` using `ServiceHostBuilder<TService>` where <TService> is the service implementation
* Add Servers to the host using `AddXXXServer` methods
* Open the host
	
```csharp
var tcpAddress = "tcp://myhost:9999/myservice";
var httpAddress = "http://myhost/myservice";
var host = new ServiceHostBuilder<Service>(InstanceMode.PerCall)
		.AddTcpServer(tcpAddress)
		.AddWebSocketServer(httpAddress);
	
await host.Open();
```

### Client Side
* Create client connection connection using `ConnctionBuilder<T>`

```csharp
var tcpAddress = "tcp://myhost/myservice";
var client = await new ConnectionBuilder<IService>()
			.WithTcpTransport(tcpAddress)
			.CreateConnection();
```

* Once the connection is created it is connected and now you can call the service remotely

```csharp
var msg = await client.Echo("Hellow World!");
```

## Duplex and Callback
* First define a callback contract (an interface) shared between your service and client to handle the callback

```csharp
public interface ICallback
{
    Task OnCallback(string serverMessage);
}
```
* **Callback contract methods must return void or Task** as the server always invoke them as OneWay operations

### Duplex Service
* In your service, optain the typed callback channel using `OperationContext.Current.GetCallback<T>`

```csharp
public Service : IService
{
    public Task<string> Join(string name)
    {
        CallBackAfter(TimeSpan.FromSeconds(3));
        return Task.FromResult("You joined");
    }
    
    async void CallBackAfter(TimeSpan delay)
    {
        var client = OperationContext.Current.GetCallback<ICallback>();
        await Task.Delay((int)delay.TotalMilliseconds);
        await client.OnCallBack("This is a server callback");
    }
}
```

* Call `WithCallback<T>` on the builder

```csharp
var host = new ServiceHostBuilder<Service>(InstanceMode.Single)
                .WithCallback<ICallback>()
                .AddTcpServer(address)
                .CreateHost();
await host.Open();
```
* *For Duplex connections you will usually have your services PerConnection or Singleton* *

### Duplex Client
* Implement your callback contract in the client side

```csharp
public class Callback : ICallback
{
    public void OnServerUpdates(string msg)
    {
        Console.WriteLine($"Received callback msg: {msg}");
    }
}
```
* Use `DuplexConnectionBuilder` to create the duplex client, note that it is generic class, the first generic argument is the service contract, while the other one is the callback implementation not the contract interface, so the builder knows what type to instantiate when the callback request is received.

```csharp
var address = "tcp://myhost/myservice";
var client = await new DuplexConnectionBuilder<IService, Callback>(InstanceMode.Single)
                        .WithTcpTransport(address)
                        .CreateConnection();
await client.Join("My Name");
```

## Instance Lifetime
### Service Instance Lifetime
Xeeny defines three modes for creating service instances
* **PerCall**: A PerCall mode instructs the framework to create one instance of the service object for every client request, That is anytime any client invokes a method on the service a new instance is going to handle that request. Once the method is execute the instance is going to be collect by GC like any other out of scope objects.
* **PerConnection**: A PerConnection mode istructs the framework to create one instance of the service object for each client. That is every time a proxy is connected there is one instance for that client is created, This instance will last as long as the connection between the client and server is open. Once the connection is closed (Or dropped by the network) the instance is removed and becomes available for GC to collect.
* **Single**: A Singleton instance mode means that there is one instance of the service handles all requests from all clients, This is the typical chat room example. The instance will be removed when the host is closed.

You define the service instance mode using the `InstanceMode` enum when creating the ServiceHost

```csharp
var host = new ServiceHostBuilder<Service>(InstanceMode.PerCall)
		...
		.CreateHost();
await host.Open();
```
### Callback Instance Lifetime
When you create duplex connection you pass the callback type and InstanceMode to the `DuplexConnectionBuilder`. The `InstanceMode` acts the same way it does for the service when creating ServiceHost
* **PerCall**: Every callback from the service will be handled in new instance, the instance is subject to collection by GC once the callback method is done.
* **PerConnection**: All callbacks from the service to a connection created by that builder will have one instance to handle them, all callbacks to another connection created by that builder will have another callback instance, these instances are subject to collection by GC when the connection is closed or dropped by the network
* **Single**: All callbacks from the service to any connection created by that builder will be handeled by one instance.

### Initializing Created Instances
* `ServiceHostBuilder` constructor has one overload that takes instance of the service type, This allows you to create the instance and pass it to the builder, the result is `InstanceMode.Single` using the object you passed
* Similar to `ServiceHostBuilder`, the `DuplextConnectionBuilder` takes an instance of the callback type allowing you to create the singleton yourself
* Instances that are `PerCall` and `PerConnection` are created by the framework, you still can initialize them after being constructed and before executing any method by listening to the events: `ServiceHost<TService>.ServiceInstanceCreated` event and `DuplextConnectionBuilder<TContract, TCallback>.CallbackInstanceCreated`
* Once AspNetCore Dependency System (IServiceCollection and IServiceResolver) are used you will be able to have easy DI.

```csharp
host.ServiceInstanceCreated += service =>
{
    service.MyProperty  = "Something";
}
...
var builder = new DuplexConnectionBuilder<IService, Callback>(InstanceMode.PerConnection)
                    .WithTcpTransport(tcpAddress);
                    
builder.CallbackInstanceCreated += callback =>
{
    callback...
}
var client = builder.CreateConnection();
```

## Operations
* **Two Way Operations**: Methods by default are two way, even methods that return **void** or **Task** are two way methods. A two way method means that the method waits the service to finish executing the operation before it returns.
* **One Way Operations**: One way operations return when the server receives the messages (before the service invokes it), These are fire and forget methods. To define a method as one way operation you have to attribute it using `Operation` attribute passing **IsOneWay = true** in the contract (The interface)

```csharp
public interface IService
{
    [Operation(IsOneWay = true)]
    void FireAndForget(string message);
}
```

### Resolving Operation Conflicts With Names
When you have methods overload in one interface (or a similar method signature in a parent interface) you have to tell them apart using `Operation` attribute by setting `Name` property. This applies for both Service and Callback contracts.

```csharp
public interface IOtherService
{
    [Operation(Name = "AnotherEcho")]
    Task<string> Echo(string message);
}
public interface IService : IOhterService
{
    Task<string> Echo(string message);
}
class Service : IService, IOtherService
{
    public Task<string> Echo(string message)
    {
      return Task.FromResult($"Echo: {message}");
    }
        
    Task<string> IOtherService.Echo(string message)
    {
      return Task.FromResult($"This is the other Echo: {message}");
    }
}
```
## Managing The Connection
You will want to access the underlying connection to manage it, like monitoring it's status, listen to events, or manage it manually (close or open it). The connection is exposed through `IConnection` interface which provides these funtionalities:
* `State`: The connection state: `Connecting`, `Connected`, `Closing`, `Closed`
* `StateChanged`: Event fired whenever the connection state changes
* `Connect()`: Connects to the remote address
* `Close()`: Closes the connection
* `SessionEnded`: Event fired when the connection is closing (`State` changed to `Closing`)
* `Dispose()`: Disposes the connection
* `ConnectionId`: Guid identifies each connection (for now the Id on the server and client don't match)
* `ConnectionName`: Friendly connection name for easier debugging and logs analystics

### Access Connection From The Service
* If your service host doesn't define a callback you get the connection using `OperationContext.Current.GetConnection()` at the beginning of your method and before the service method spawn any new thread.
* If it is duplex, you get the connection by calling `OperationContext.Current.GetConnection()`, but most likely by calling `OperationContext.Current.GetCallback<TCallback>`. The returned instance is an instance that is emitted at runtime and implements your callback contract (defined in the generic parameter `TCallback`). This auto-generated type implements `IConnection` as well, so anytime you want to access connection functions of the challback channel just cast it to `IConnection`

```csharp
public class ChatService : IChatService
{
   ConcurrentDictionary<string, ICallback> _clients = new ConcurrentDictionary<string, ICallback>();
   
   ICallback GetCaller() => OperationContext.Current.GetCallback<ICallback>();

   public void Join(string id)
   {
      var caller = GetCaller();
      _clients.AddOrUpdate(id, caller, (k, v) => caller);
      ((IConnection)caller).SessionEnded += s =>
      {
         _clients.TryRemove(id, out ICallback _);
      };
   }
}
```
### Access Connection From The Client
Clients are instances of auto-generated types that are emitted at runtime and implement your service contract interface. Together with the contract the emitted type implements `IConnection` which means you can cast any client (Duplex or not) to `IConnection`

```csharp
var client = await new ConnectionBuilder<IService>()
                  	.WithTcpTransport(address)
                  	.CreateConnection();
var connection = (IConnection)client;
connection.StateChanged += c => Console.WriteLine(c.State);
connection.Close()
```
* The `CreateConnection` method takes one optional parameter of type boolean which is `true` by default. This flag indicates if the generated connection will connect to the server or not. by default anytime `CreateConnection` is called the generated connection will connect automatically. Sometimes you want to create connections and want to connect them later, to do that you pass `false` to the `CreateConnection` method then open your connection manually when you want

```csharp
var client = await new ConnectionBuilder<IService>()
			.WithTcpTransport(address)
			.CreateConnection(false);
var connection = (IConnection)client;
...
await connection.Connect();
```

### Connection Options
All builders expose connection options when you add Server or Transport. the options are:
* `Timeout`: Sets the connection timeout (_default 30 seconds_)
* `ReceiveTiemout`: Is the Idle remote timeout (_server default: 10 minutes, client default: Infinity_)
* `KeepAliveInterval`: Keep alive pinging interval (_default 30 seconds_)
* `KeepAliveRetries`: Number of retries before deciding the connection is off (_default 10 retries_)
* `SendBufferSize`: Sending buffer size (_default 4096 byte = 4 KB_)
* `ReceiveBufferSize` : Receiving buffer size (_default 4096 byte = 4 KB_)
* `MaxMessageSize`: Maximum size of messages (_default 1000000 byte = 1 MB_)
* `ConnectionNameFormatter`: Delegate to set or format `ConnectionName` (_default is null_). (see [Logging](#logging))
* `SecuritySettings`: SSL settings (_default is null_) (see [Security](#security))

You get these options configuration action on the server when you call AddXXXServer:

```csharp
var host = new ServiceHostBuilder<ChatService>(InstanceMode.Single)
		.WithCallback<ICallback>()
		.AddTcpServer(address, options =>
		{
			options.Timeout = TimeSpan.FromSeconds(10);
		})
		.WithConsoleLogger()
		.CreateHost();

await host.Open();
```
On the client side you get it when calling WithXXXTransport

```csharp
var client = await new DuplexConnectionBuilder<IChatService, MyCallback>(new MyCallback())
		.WithTcpTransport(address, options =>
		{
			options.KeepAliveInterval = TimeSpan.FromSeconds(5);
		})
		.WithConsoleLogger()
		.CreateConnection();
```
### Connection Timeout
When you set `Timeout` and the request doesn't complete during that time the connection will be closed and you have to create new clien. If the `Timeout` is set on the server side that will define the callback timeout and the connection will be closed when the callback isn't complete during that time. Remember that callaback is one way operation and all one way operations complete when the other side receives the message and before the remote method is executed.

The `ReceiveTimeout` is the "_Idle Remote Timeout_" If you set it on the server it will define the timeout for the server to close inactive clients who are the clients that are not sending any request or KeepAlive message during that time.

The `ReceiveTimeout` on the client is set to _Infinity_ by default, if you set it on the duplex client you are instructing the client to ignore callbacks that don't come during that time which is a weird scenario but still possible if you chose to do so.

### Connection Buffers
`ReceiveBufferSize` is the size of the receiving buffer. Setting it to small values won't affect the ability of receiving big messages, buf if that size is significantly small comparing to messages to receive then introduce more IO operations. You better leave the default value at the beginning then if needed do your load testing and analysing to find the size that performs good and occupies 

`SendBufferSize` is the size of the sending buffer. Setting it to small values won't affect the ability of sending big messages, buf if that size is significantly small comparing to messages to send then introduce more IO operations. You better leave the default value at the beginning then if needed do your load testing and analysing to find the size that performs good and occupies less memory.

A receiver's `ReceiveBufferSize` should equal the sender's  `SendBufferSize` because some transports like UDP won't work well if these two size are not equal. For now Xeeny doesn't check buffer sizes but in the future I am modifying the protocol to include this check during the Connect processing.

`MaxMessageSize` is the maximum allowed number of bytes to receive. This value has nothing to do with buffers so it doesn't affect the memory or the performance. This value is important though for validating your clients and preventing huge messages from clients, Xeeny uses size-prefix protocol so when a message arrives it will be bufferd on a buffer of size `ReceiveBufferSize` which must be ways smaller than `MaxMessageSize`, After the message arrives the size header is read, if the size is bigger than `MaxMessageSize` the message is rejected and the connection is closed.

### Keep Alive Management
Xeeny uses it's own keep-alive messages because not all kind of transports has built-in keep-alive mechanism. These messages are 5 bytes flow from the client to the server only. The interval `KeepAliveInterval` is 30 seconds by default, when you set it on the client the client will send a ping message if it didn't successfully send anything during the last `KeepAliveInterval`.

You have to set `KeepAliveInterval` to be less than the server's `ReceiveTimeout`, at least 1/2 or 1/3 of server's `ReceiveTimeout` because the server will timeout and closes the connection if it didn't receive anything during it's `ReceiveTimeout` 

`KeepAliveRetries` is the number of failing keep-alive messages, once reached the client decides that the connection is broken and closes.

Setting `KeepAliveInterval` or `KeepAliveRetries` on the server has no effect.

## Serialization
For Xeeny to be able to marshal method parameters and return types on the wire it needs to serialize them. There are three serializers already supported in the framework
* `MessagePackSerializer`: Is the MessagePack serialization implemented by [MsgPack.Cli](https://github.com/msgpack/msgpack-cli), It is the Default serializer as the serialized data is small and the implementation for .net in the given library is fast.
* `JsonSerializer`: Json serializer implemented by [Newtonsoft](https://github.com/JamesNK/Newtonsoft.Json)
* `ProtobufSerializer`: Google's ProtoBuffers serializer implemented by [Protobuf-net](https://github.com/mgravell/protobuf-net)

You can chose the serializer using the builders by calling `WithXXXSerializer`, just make sure your types are serializable using the selected serializer.

```csharp
var host = new ServiceHostBuilder<ChatService>(InstanceMode.Single)
		.WithCallback<ICallback>()
		.WithProtobufSerializer()
		.CreateHost();

await host.Open();
```

* You can also use your own serializer by implementing ISerializer and then calling `WithSerializer(ISerializer serializer)`

## Security
Xeeny uses TLS 1.2 (over TCP only for now), you need to add `X509Certificate` to the server
```csharp
 var host = new ServiceHostBuilder<Service>(...)
 		.AddTcpServer(tcpAddress, options =>
		{
			options.SecuritySettings = SecuritySettings.CreateForServer(x509Certificate2);
		})
		...
```
And on the client you need to pass the `Certificate Name`:
```csharp
await new ConnectionBuilder<IService>()
		.WithTcpTransport(tcpAddress, options =>
		{
			options.SecuritySettings = SecuritySettings.CreateForClient(certificateName);
		})
		...
```
If you want to validate remote certificate you can pass the `RemoteCertificateValidationCallback` optional delegate to `SecuritySettings.CreateForClient`

## Logging
Xeeny uses same logging system found in Asp.Net Core
* Console Logger
* Debug Logger
* Custome Logger

To use loggers add the nuget package of the logger, then call `WithXXXLogger` where you can pass the `LogLevel`

You may like to name connections so they are easy to spot when debugging or analysing logs, you can do that by setting `ConnectionNameFormatter` function delegate in the options which is passed `IConnection.ConnectionId` as parameter and the return will be assigned to `IConnection.ConnectionName`.

```csharp
var client1 = await new DuplexConnectionBuilder<IChatService, Callback>(callback1)
                        .WithTcpTransport(address, options =>
			{
				options.ConnectionNameFormatter = id => $"First-Connection ({id})";
			})
			.WithConsoleLogger(LogLevel.Trace)
			.CreateConnection();
```

## Transports 
* TCP: Sockets
* UDP: Sockets
* WebSockets (To be discontinued or implemented by [vtortola WebSocketListener](https://github.com/vtortola/WebSocketListener))


## Performance
* Xeeny is built to be high performance and async, having async contracts allows the framework to be fully async. Try always to have your operations to return `Task` or `Task<T>` instead of `void` or `T`. This will save that one extra thread that will be waiting the underlying async socket to complete in case your operations aren't async.

* The overhead in Xeeny is when it needs to emit _"New"_ types at runtime. It does that when you create `ServiceHost<TService>` (calling `ServiceHostBuilder<TService>.CreateHost()`) but that happens once per type, so once xeeny emitted the first host of the given type creating more hosts of that type has no performance issues. anyway this is usually your application start.

* Another place where emitting types happen is when you create the first client of a given contract or callback type (calling `CreateConnection`). once the first type of that proxy is emitter next clients will be created without overhead. (note that you are still creating a new socket and new connection unless you pass `false` to `CreateConnection`).

* Calling `OperationContext.Current.GetCallback<T>` also emits runtime type, like all other emissions above the emitted type is cached and the overhead happens only at the first call. you can call this method as many as you like but you better cache the return.

## Extend Xeeny
#### Custom Transport
You can get all Xeeny framwork features above to work with your custom transport (Say you want it behind device Blueetooth).
##### On the server:
* Implement `XeenyListener` abstract class
* Pass it to `ServiceHostBuilder<T>.AddCustomServer()`
##### On the client:
* Implement `IXeenyTransportFactory`
* Pass it to `ConnectionBuilder<T>.WithCustomTransport()`

#### Custom Protocol
If you want to have your own protocol from scratch, you need to implement your own connectivity, message framing, concurrency, buffering, timeout, keep-alive, ...etc.
##### On the server:
* Implement `IListener`
* Pass it to `ServiceHostBuilder<T>.AddCustomServer()`
##### On the client
* Implement `ITransportFactory`
* Pass it to `ConnectionBuilder<T>.WithCustomTransport()`

## Samples
* [Complete Chat Example](https://github.com/MhAllan/Xeeny/tree/develop/Src/Xeeny.ChatExample)
* [Multi-Transports Example](https://github.com/MhAllan/Xeeny/tree/develop/Src/Xeeny.ConsoleTest)
