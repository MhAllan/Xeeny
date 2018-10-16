using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Messaging;
using Xeeny.Transports;
using Xeeny.Transports.Messages;

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

        public override async Task Connect(CancellationToken ct = default)
        {
            await Transport.Connect(ct);
            Transport.Listen();
            Transport.StartPing();
        }

        public void SendOneWay(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateOneWayRequest(operation, parameters);
            var task = Transport.SendMessage(msg);
            task.ConfigureAwait(false);
            task.Wait();
        }

        public void SendAndWait(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            var task = Transport.SendMessage(msg);
            task.ConfigureAwait(false);
            task.Wait();
        }

        public TResponse Invoke<TResponse>(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);

            var task = Transport.Invoke(msg);
            task.ConfigureAwait(false);
            var response = task.Result ;

            var result = _msgBuilder.UnpackResponse<TResponse>(response.Payload);
            return result;
        }


        public async Task SendOneWayAsync(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateOneWayRequest(operation, parameters);
            await Transport.SendMessage(msg);
        }

        public async Task SendAndWaitAsync(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            await Transport.Invoke(msg);
        }

        public async Task<TResponse> InvokeAsync<TResponse>(string operation, params object[] parameters)
        {
            var msg = _msgBuilder.CreateRequest(operation, parameters);
            var response = await Transport.Invoke(msg);
            var result = _msgBuilder.UnpackResponse<TResponse>(response.Payload);

            return result;
        }

        protected override void OnRequestReceived(ITransport socket, Message message)
        {
            throw new Exception("Something went wrong, Client should not receive request message!");
        }
    }
}
