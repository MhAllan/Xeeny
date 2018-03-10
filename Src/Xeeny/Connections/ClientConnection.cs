using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Connections;
using Xeeny.Messaging;
using Xeeny.Sockets;
using Xeeny.Transports;

namespace Xeeny.Connections
{
    public class ClientConnection : ConnectionBase, IClientConnection
    {
        readonly IMessageBuilder _msgBuilder;

        internal ClientConnection(ITransport socket, IMessageBuilder msgBuilder)
            : base(socket)
        {
            _msgBuilder = msgBuilder;
        }

        public override async Task Connect()
        {
            await this.Socket.Connect();
            this.Socket.Listen();
            this.Socket.StartPing();
        }

        public void SendOneWay(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateOneWayRequest(operation, parameters);
            var task = this.Socket.SendOneWay(msg);
            task.ConfigureAwait(false);
            task.Wait();
        }

        public void SendAndWait(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            var task = this.Socket.SendRequest(msg);
            task.ConfigureAwait(false);
            task.Wait();
        }

        public TResponse Invoke<TResponse>(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);

            var task = this.Socket.SendRequest(msg);
            task.ConfigureAwait(false);
            var response = task.Result ;

            var result = _msgBuilder.UnpackResponse<TResponse>(response.Payload);
            return result;
        }


        public async Task SendOneWayAsync(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateOneWayRequest(operation, parameters);
            await this.Socket.SendOneWay(msg);
        }

        public async Task SendAndWaitAsync(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            await this.Socket.SendRequest(msg);
        }

        public async Task<TResponse> InvokeAsync<TResponse>(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            var response = await this.Socket.SendRequest(msg);
            var result = _msgBuilder.UnpackResponse<TResponse>(response.Payload);

            return result;
        }
    }
}
