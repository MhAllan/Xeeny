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
using System.Security.Cryptography.X509Certificates;
using Xeeny.Transports;

namespace Xeeny.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await SslTest();
                //await Profile(5000, SocketType.TCP, false, 1024 * 2);
                //await EndToEndTest();
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


        static async Task Profile(int count, SocketType socketType, bool useSsl, int bufferSize)
        {
            X509Certificate2 x509Cert = null;
            string certName = null;
            if(useSsl)
            {
                x509Cert = GetXeenyTestCertificate(out certName);
            }

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
                    options.ReceiveBufferSize = bufferSize;
                    if(useSsl)
                    {
                        throw new NotImplementedException();
                    }
                });

                clientBuilder = new ConnectionBuilder<IService>()
                                    .WithWebSocketTransport(httpAddress, options =>
                                    {
                                        options.SendBufferSize = bufferSize;
                                        if(useSsl)
                                        {
                                            throw new NotImplementedException();
                                        }
                                    });
            }
            else if(socketType == SocketType.TCP)
            {
                hostBuilder.AddTcpServer(tcpAddress, options =>
                {
                    options.ReceiveBufferSize = bufferSize;
                    if(useSsl)
                    {
                        options.SecuritySettings = SecuritySettings.CreateForServer(x509Cert);
                    }
                });

                clientBuilder = new ConnectionBuilder<IService>()
                                    .WithTcpTransport(tcpAddress, options =>
                                    {
                                        options.SendBufferSize = bufferSize;
                                        if(useSsl)
                                        {
                                            options.SecuritySettings = SecuritySettings.CreateForClient(certName);
                                        }
                                    });
            }

            var host = hostBuilder.CreateHost();

            await host.Open();

            Console.WriteLine("Host is open");

            var client = await clientBuilder.CreateConnection();

            //1 KB, the actual message on the wire will be more than 1 KB (depends on method type, name, and parameters)
            var msg = new string('*', 1000);

            for (int j = 0; j < 100; j++)
            {
                await Task.Delay(1000);
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < count; i++)
                {
                    var resp = await client.Echo(msg);
                    //Task.Run(async () =>
                    //{
                    //    var resp = await client.Echo(msg);
                    //    Console.WriteLine(resp.Length);
                    //});
                }

                sw.Stop();
                Console.WriteLine($">> {sw.ElapsedMilliseconds}");
            }
        }

        static async Task SslTest()
        {
            var x509Cert = GetXeenyTestCertificate(out string certName);

            var tcpAddress = $"tcp://localhost:9999/tcpTest";

            var host = new ServiceHostBuilder<Service>(InstanceMode.PerConnection)
                            .AddTcpServer(tcpAddress, options =>
                            {
                                options.SecuritySettings = SecuritySettings.CreateForServer(x509Cert);
                            })
                            .WithConsoleLogger(LogLevel.Trace)
                            .CreateHost();
            await host.Open();

            var client = await new ConnectionBuilder<IService>()
                               .WithTcpTransport(tcpAddress, options =>
                               {
                                   options.SecuritySettings = SecuritySettings.CreateForClient(certName);
                               })
                               .WithConsoleLogger(LogLevel.Trace)
                               .CreateConnection();

            var msg = await client.Echo("test");
            Console.WriteLine(msg);
            Console.WriteLine("Test Done!");
        }

        static async Task EndToEndTest()
        {
            var httpAddress = $"http://localhost/test";
            var tcpAddress = $"tcp://localhost:9999/tcpTest";

            var host = new ServiceHostBuilder<Service>(InstanceMode.PerConnection)
                            .WithCallback<ICallback>()
                            //add websocket server
                            .AddWebSocketServer(httpAddress)
                            //add tcp server
                            .AddTcpServer(tcpAddress)
                            .WithMessagePackSerializer() //it is the default
                            .WithConsoleLogger()
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

        static X509Certificate2 GetXeenyTestCertificate(out string certificateName)
        {
            certificateName = "xeeny.test";
            var subject = $"CN={certificateName}";
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subject, false);
            if (certificates.Count == 0)
            {
                throw new Exception($"No certificates were found for subject: {subject}");
            }

            var x509Cert = certificates[0];

            return x509Cert;
        }
    }
}
