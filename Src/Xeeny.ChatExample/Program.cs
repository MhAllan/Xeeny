using Xeeny.Api.Client;
using Xeeny.Api.Server;
using Xeeny.Connections;
using Xeeny.Dispatching;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Xeeny.ChatExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var serverLogLevel = LogLevel.None;

                var address = $"tcp://localhost:9091/test";

                //note InstanceMode.Single
                var host = new ServiceHostBuilder<ChatService>(InstanceMode.Single)
                                .WithCallback<ICallback>()
                                .AddTcpServer(address, options =>
                                {
                                    options.ReceiveTimeout = TimeSpan.FromSeconds(10);
                                })
                                .WithConsoleLogger(serverLogLevel)
                                .CreateHost();

                await host.Open();

                Console.WriteLine("host is open");

                var name1 = "Client 1";
                var name2 = "Client 2";
                var name3 = "Client 3";

                var logLevel1 = LogLevel.None;
                var logLevel2 = LogLevel.None;
                var logLevel3 = LogLevel.None;

                var callback1 = new Callback { Name = name1 };
                var callback2 = new Callback { Name = name2 };
                var callback3 = new Callback { Name = name3 };

                var client1 = await new DuplexConnectionBuilder<IChatService, Callback>(callback1)
                                    .WithTcpTransport(address, options =>
                                    {
                                        options.KeepAliveInterval = TimeSpan.FromSeconds(5);
                                        options.ConnectionNameFormatter = id => $"{name1}({id})";
                                    })
                                    .WithConsoleLogger(logLevel1)
                                    .CreateConnection();

                var client2 = await new DuplexConnectionBuilder<IChatService, Callback>(callback2)
                                    .WithTcpTransport(address, options =>
                                    {
                                        options.KeepAliveInterval = TimeSpan.FromSeconds(5);
                                        options.ConnectionNameFormatter = id => $"{name2}({id})";
                                    })
                                    .WithConsoleLogger(logLevel2)
                                    .CreateConnection();

                var builder3 = new DuplexConnectionBuilder<IChatService, Callback>(callback3)
                                    .WithTcpTransport(address, options =>
                                    {
                                        //this is too slow comparing to server ReceiveTimeout
                                        //this client will timeout if it didn't send it's own application messages
                                        //within server ReceiveTimeout 
                                        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                                        options.ConnectionNameFormatter = id => $"{name3}({id})";
                                    })
                                    .WithConsoleLogger(logLevel3);

                //let's open this explicitly, pass false so connection won't open
                var client3 = await builder3.CreateConnection(false);

                //now explicitly open a client that is created but not connected
                await ((IConnection)client3).Connect();

                client1.Join(name1);
                client2.Join(name2);
                client3.Join(name3);

                await client1.Say("Hello");

                await client2.Say("All clients will see this message");

                await client2.TellOthers($"{name2} doesn't get this message");

                await client3.PrivateTo(name1, $"This is from {name3} to {name1} Only");

                await Task.Delay(1000);

                Console.WriteLine("client3 will close");
                //client3 leaves
                ((IConnection)client3).Close();

                await Task.Delay(1000);

                await client1.WhoIsOnline();

                //client3 connect again, keep-alive is slower than server ReceiverTimeout
                //so it will timeout from server side
                client3 = await builder3.CreateConnection();
                client3.Join(name3);

                Console.WriteLine("client3 joined again");
                await Task.Delay(1000);

                await client3.WhoIsOnline();

                await Task.Delay(1000);
                Console.WriteLine("Waiting client3 to timeout");
                await Task.Delay(15000); //More than Server ReceiveTimeout
                Console.WriteLine("Waiting client3 to timeout is done");

                await client1.WhoIsOnline();

                await Task.Delay(1000);

                ((IConnection)client1).Close();
                ((IConnection)client2).Close();

                Console.WriteLine("Test is done");
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
    }
}
