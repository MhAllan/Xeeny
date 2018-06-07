using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xeeny.Transports.Channels;
using Xeeny.Transports.MessageFraming;
using Xeeny;
using Xunit;
using Xeeny.Transports;
using Xeeny.Transports.Messages;

namespace UnitTest.Xeeny.Transports.MessageFraming
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
            var message = Encoding.ASCII.GetBytes(msg);

            await channel.SendMessage(message, default);

            evt.WaitOne();

            result.ReadInt32(0, out var size);
            result.ReadSubArray(4, msgSize, out var resultArray);

            Assert.True(size == msgSize);

            var resultMsg = Encoding.ASCII.GetString(resultArray);

            Assert.True(resultMsg == msg);
        }

        [Theory]
        [InlineData(100, 1000, 100, 50)]
        public async Task ReceiveSuccess(int msgSize, int maxMessageSize, int receiveBufferSize, int batch)
        {
            var evt = new AutoResetEvent(false);

            var msg = new string('*', msgSize);
            var bytes = Encoding.ASCII.GetBytes(msg);
            var buffer = new byte[msgSize + 4];
            buffer.WriteInt32(0, msgSize);
            buffer.WriteArray(4, bytes);
            var index = 0;
            var take = 0;

            var next = new Mock<Channel>(ConnectionSide.Client);
            next.Setup(x => x.Receive(_anySegment, _anyToken))
                .Callback<ArraySegment<byte>, CancellationToken>((segment, token) =>
                {
                    take = Math.Min(batch, segment.Count);
                    segment.Array.WriteArray(segment.Offset, buffer, index, take);
                    index += take;
                    if(index == buffer.Length)
                    {
                        evt.Set();
                    }
                })
                .Returns(() => Task.FromResult(take));

            var settings = new SerialStreamChannelSettings
            {
                MaxMessageSize = maxMessageSize,
                SendBufferSize = 5,
                ReceiveBufferSize = receiveBufferSize
            };

            var channel = new SerialStreamMessageChannel(next.Object, settings);

            var message = await channel.ReceiveMessage(default);

            evt.WaitOne();

            var resultMsg = Encoding.ASCII.GetString(message, 0, msgSize);

            Assert.True(resultMsg == msg);
        }

        [Theory]
        [InlineData(5, 5, 5, 5)]
        [InlineData(100, 100, 100, 100)]
        [InlineData(50, 100, 50, 100)]
        [InlineData(50, 100, 100, 50)]
        [InlineData(10, 100, 100, 100)]
        [InlineData(1000, 100, 100, 100)]
        public async Task SendReceiveSuccess(int msgSize, int maxMessageSize, int sendBufferSize, int receiveBufferSize)
        {
            byte[] data = new byte[msgSize + 4];
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
                    read = Math.Min(data.Length - readIndex, segment.Count);
                    readIndex = segment.Array.WriteArray(segment.Offset, data, readIndex, read);
                    writeEvt.Set();
                })
                .Returns(() => Task.FromResult(read));

            var settings = new SerialStreamChannelSettings
            {
                MaxMessageSize = maxMessageSize,
                SendBufferSize = sendBufferSize,
                ReceiveBufferSize = receiveBufferSize
            };

            var channel = new SerialStreamMessageChannel(next.Object, settings);

            var msg = new string('*', msgSize);
            var buffer = Encoding.ASCII.GetBytes(msg);

            Task.Run(async () => await channel.SendMessage(buffer, default));

            var resultBuffer = await channel.ReceiveMessage(default);

            var resultMsg = Encoding.ASCII.GetString(resultBuffer);

            Assert.True(resultMsg == msg);
        }
    }
}
