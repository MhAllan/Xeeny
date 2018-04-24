using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xeeny.Messaging;
using Xeeny.Transports;

namespace Xeeny.Connections
{
    public class ClientConnection : ConnectionBase, IClientConnection
    {
        readonly IMessageBuilder _msgBuilder;

        internal ClientConnection(ITransport transport, IMessageBuilder msgBuilder)
            : base(transport)
        {
            _msgBuilder = msgBuilder;
        }

        public override async Task Connect()
        {
            await this.Transport.Connect();
            this.Transport.Listen();
            this.Transport.StartPing();
        }

        public void SendOneWay(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateOneWayRequest(operation, parameters);
            var task = this.Transport.SendOneWay(msg);
            task.ConfigureAwait(false);
            task.Wait();
        }

        public void SendAndWait(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            var task = this.Transport.SendRequest(msg);
            task.ConfigureAwait(false);
            task.Wait();
        }

        public TResponse Invoke<TResponse>(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);

            var task = this.Transport.SendRequest(msg);
            task.ConfigureAwait(false);
            var response = task.Result ;

            var result = _msgBuilder.UnpackResponse<TResponse>(response.Payload);
            return result;
        }


        public async Task SendOneWayAsync(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateOneWayRequest(operation, parameters);
            await this.Transport.SendOneWay(msg);
        }

        public async Task SendAndWaitAsync(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            await this.Transport.SendRequest(msg);
        }

        public async Task<TResponse> InvokeAsync<TResponse>(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            var response = await this.Transport.SendRequest(msg);
            var result = _msgBuilder.UnpackResponse<TResponse>(response.Payload);

            return result;
        }
    }
}
