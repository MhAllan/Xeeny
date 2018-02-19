using Xeeny.Api.Client;
using Xeeny.Api.Server;
using Xeeny.Dispatching;
using Xeeny.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Xeeny.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                //await Profile(SocketType.TCP);
                await EndToEndTest();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }


        //monitor response time, cpu, and memory //WebSockets: 1kb msg => 5000 request/second on Corei5 4GB RAM
        //monitor response time, cpu, and memory //TCP: 1kb msg => 8000 request/second on Corei5 4GB RAM
        static async Task Profile(SocketType socketType)
        {
            var httpAddress = $"http://localhost/test";
            var tcpAddress = "tcp://localhost:9988";

            var hostBuilder = new ServiceHostBuilder<Service>(InstanceMode.PerCall)
                            .WithCallback<ICallback>()
                            .WithMessagePackSerializer() //it is the default
                            .WithConsoleLogger(); //default is empty

            ConnectionBuilder<IService> clientBuilder = null;

            if (socketType == SocketType.WebSocket)
            {
                hostBuilder.AddWebSocketServer(httpAddress, options =>
                {
                    //if you set this on WebSocket,
                    // it must be less or equal to the client's SendBufferSize, default 4KB
                    options.ReceiveBufferSize = 1024 * 2;
                });

                clientBuilder = new ConnectionBuilder<IService>()
                                    .WithWebSocketTransport(httpAddress, options =>
                                    {
                                        //if you set this on WebSocket, 
                                        // it must be less or equal to the server's ReceiveBufferSize, default 4KB
                                        options.SendBufferSize = 1024 * 2;
                                    });
            }
            else if(socketType == SocketType.TCP)
            {
                hostBuilder.AddTcpServer(tcpAddress, options =>
                {
                    //if you set this on WebSocket,
                    // it must be less or equal to the client's SendBufferSize, default 4KB
                    options.ReceiveBufferSize = 1024 * 2;
                });

                clientBuilder = new ConnectionBuilder<IService>()
                                    .WithTcpTransport(tcpAddress, options =>
                                    {
                                        //if you set this on WebSocket, 
                                        // it must be less or equal to the server's ReceiveBufferSize, default 4KB
                                        options.SendBufferSize = 1024 * 2;
                                    });
            }

            var host = hostBuilder.CreateHost();

            await host.Open();

            Console.WriteLine("Host is open");

            var client = await clientBuilder.CreateConnection();

            //1 KB, the actual message on the wire will be more than 1 KB (depends on method type, name, and payload)
            var msg = new string('*', 1000);

            for (int j = 0; j < 100; j++)
            {
                await Task.Delay(1000);
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < 50000; i++)
                {
                    var resp = await client.Echo(msg);
                }

                sw.Stop();
                Console.WriteLine($">> {sw.ElapsedMilliseconds}");
            }
        }

        static async Task EndToEndTest()
        {
            var httpAddress = $"http://localhost/test";
            var tcpAddress = $"tcp://localhost:9999/tcpTest";

            var host = new ServiceHostBuilder<Service>(InstanceMode.PerConnection)
                            .WithCallback<ICallback>()
                            //add websocket server
                            .AddWebSocketServer(httpAddress, options => options.ReceiveBufferSize = 1024)
                            //add tcp server
                            .AddTcpServer(tcpAddress, options =>
                            {
                                options.ReceiveBufferSize = 125;
                            })
                            .WithMessagePackSerializer() //it is the default
                            .WithConsoleLogger(LogLevel.Trace) //default is empty
                            .CreateHost();

            host.ServiceInstanceCreated += service =>
            {
                //configure service instances
                Console.WriteLine("Instance created");
            };

            await host.Open();

            Console.WriteLine("Host is open");

            var clientBuilder1 = new DuplexConnectionBuilder<IService, MyCallback>(InstanceMode.PerConnection)
                            .WithWebSocketTransport(httpAddress, options =>
                            {
                                //set connection options
                                options.SendBufferSize = 125;
                                options.KeepAliveInterval = TimeSpan.FromMinutes(10);
                            })
                            .WithMessagePackSerializer()
                            .WithConsoleLogger();

            clientBuilder1.CallbackInstanceCreated += obj =>
            {
                Console.WriteLine($"Created callback of type {obj.GetType()}");
                var callback = obj;
                //config the callback instance
            };

            var client1 = await clientBuilder1.CreateConnection();

            var cc = new string('*', 1000);
            var resp = await client1.Echo(cc);
            Console.WriteLine(resp.Length);
            Console.WriteLine(resp);

            return;

            var client2 = await clientBuilder1.CreateConnection();

            var msg = await client1.Echo("From Client 1");
            Console.WriteLine(msg);

            msg = await client2.Echo("From Client 2");
            Console.WriteLine(msg);

            msg = await client1.CallMeBack();
            Console.WriteLine(msg);

            var client3 = await clientBuilder1.CreateConnection();

            client3.FireAndForget("server is here now");
            Console.WriteLine("client3 is here now"); //this should appear first

            var clientBuilder2 = new ConnectionBuilder<IOtherService>()
                            .WithWebSocketTransport(httpAddress)
                            .WithConsoleLogger();

            var client4 = await clientBuilder2.CreateConnection();
            msg = await client4.Echo("It is another Echo"); //another interface same Echo signature
            Console.WriteLine(msg);

            await Task.Delay(6000);
            Console.WriteLine("now with tcp");

            var tcpCient = await new DuplexConnectionBuilder<IService, MyCallback>(InstanceMode.Single)
                                .WithTcpTransport(tcpAddress)
                                .WithConsoleLogger()
                                .CreateConnection();

            msg = await tcpCient.Echo("From TCP Client");
            Console.WriteLine(msg);

            await tcpCient.CallMeBack();

            //how to close connection, the listening will show exception that has no effect
            //await ((IConnection)client4).Close();
        }
    }
}
