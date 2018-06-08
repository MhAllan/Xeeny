using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports;
using Xeeny.Transports.Channels;
using Xeeny.Transports.MessageFraming;
using Xunit;

namespace UnitTest.Xeeny.Transports.MessageFraming
{
    public class ConcurrentStreamChannelTest
    {
        ArraySegment<byte> _anySegment => It.IsAny<ArraySegment<byte>>();
        CancellationToken _anyToken => It.IsAny<CancellationToken>();

        [Theory]
        [InlineData(25, 50, 50, 50)]
        [InlineData(30, 50, 50, 50)]
        [InlineData(10, 50, 50, 50)]
        [InlineData(100, 100, 100, 100)]
        [InlineData(50, 100, 50, 100)]
        [InlineData(50, 100, 100, 50)]
        [InlineData(10, 100, 100, 100)]
        [InlineData(1000, 1000, 100, 100)]
        public async Task SendReceiveSuccess(int msgSize, int maxMessageSize, int sendBufferSize, int receiveBufferSize)
        {
            var header = ConcurrentStreamMessageChannel.HeaderSize;
            var available = sendBufferSize - header;
            var batches = (int)Math.Ceiling((double)msgSize / available);
            var allHeaders = header * batches;
            var size = msgSize + allHeaders;

            byte[] data = new byte[size];
            var writeEvt = new AutoResetEvent(true);
            int writeIndex = 0;
            var readEvt = new AutoResetEvent(false);
            int readIndex = 0;
            int read = 0;

            var next = new Mock<Channel>(ConnectionSide.Client);
            next.Setup(x => x.Send(_anySegment, _anyToken))
                .Callback<ArraySegment<byte>, CancellationToken>((segment, token) =>
                {
                    writeEvt.WaitOne();
                    writeIndex = data.WriteSegment(writeIndex, segment);
                    readEvt.Set();
                })
                .Returns(Task.CompletedTask);

            next.Setup(x => x.Receive(_anySegment, _anyToken))
                .Callback<ArraySegment<byte>, CancellationToken>((segment, token) =>
                {
                    readEvt.WaitOne();
                    read = Math.Min(writeIndex - readIndex, segment.Count);
                    segment.Array.WriteArray(segment.Offset, data, readIndex, read);
                    readIndex += read;
                    if (writeIndex == data.Length)
                        readEvt.Set();
                    else
                        writeEvt.Set();
                })
                .Returns(() => Task.FromResult(read));

            var settings = new ConcurrentStreamChannelSettings
            {
                MaxMessageSize = maxMessageSize,
                SendBufferSize = sendBufferSize,
                ReceiveBufferSize = receiveBufferSize
            };

            var channel = new ConcurrentStreamMessageChannel(next.Object, settings);

            var msg = new string('*', msgSize);
            var buffer = Encoding.ASCII.GetBytes(msg);

            Task.Run(async () => await channel.SendMessage(buffer, default));

            var resultBuffer = await channel.ReceiveMessage(default);

            var resultMsg = Encoding.ASCII.GetString(resultBuffer);

            Assert.True(resultMsg == msg);
        }
    }
}
