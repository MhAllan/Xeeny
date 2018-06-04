using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;
using Xeeny.Transports.Channels.MessageFraming;
using Xeeny;
using Xunit;
using Xeeny.Transports;

namespace UnitTest.Xeeny.Transports.Channels
{
    public class SerialStreamChannelTest
    {
        ArraySegment<byte> _anySegment => It.IsAny<ArraySegment<byte>>();
        CancellationToken _anyToken => It.IsAny<CancellationToken>();

        [Theory]
        [InlineData(100, 1000, 100)]
        [InlineData(10, 10, 10)]
        [InlineData(10, 50, 30)]
        [InlineData(1000, 20, 10)]
        [InlineData(5, 5, 5)]
        public async Task SendSuccess(int msgSize, int maxMessageSize, int sendBufferSize)
        {
            var evt = new AutoResetEvent(false);

            var result = new byte[msgSize + 4];
            var index = 0;

            var next = new Mock<Channel>(ConnectionSide.Client);
            next.Setup(x => x.Send(_anySegment, _anyToken))
                .Callback<ArraySegment<byte>, CancellationToken>((segment, token) =>
                {
                    index = result.WriteSegment(index, segment);
                    if(index == result.Length)
                    {
                        evt.Set();
                    }
                })
                .Returns(Task.CompletedTask);

            var settings = new SerialStreamChannelSettings
            {
                MaxMessageSize = maxMessageSize,
                SendBufferSize = sendBufferSize,
                ReceiveBufferSize = 5
            };

            var channel = new SerialStreamMessageChannel(next.Object, settings);

            var msg = new string('*', msgSize);
            var bytes = Encoding.ASCII.GetBytes(msg);
            var buffer = new ArraySegment<byte>(bytes);

            await channel.Send(buffer, default);

            evt.WaitOne();

            var resultMsg = Encoding.ASCII.GetString(result, 4, msgSize);

            Assert.True(resultMsg == msg);
        }
    }
}
