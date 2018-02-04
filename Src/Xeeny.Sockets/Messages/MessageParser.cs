using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Messages
{
    public class MessageParser
    {
        //ping only: [size](int) [messageType](byte)
        //[size](int) [messageType](byte) [id](guid) [isEncrypted](byte) [addressSize](int) [address](variable) [payload](variable)

        const int _sizeLength = sizeof(int);

        const int _messageTypeIndex = _sizeLength;
        const int _messageTypeLength = 1; //flag

        const int _idIndex = _messageTypeIndex + 1;
        const int _idLength = 16; //16 fixed, Guid for example

        const int _isEncryptedIndex = _idIndex + _idLength;
        const int _isEncryptedLength = 1; //flag

        const int _addressSizeIndex = _isEncryptedIndex + 1;
        const int _addressSizeLength = sizeof(int);

        //does not apply on ping message
        const int _minMessageSize = _sizeLength + _messageTypeLength + _idLength + _isEncryptedLength + _addressSizeLength;

       
        const int _addressIndex = _addressSizeIndex + _addressSizeLength; //4 bytes for _addressSizeIndex

        //[size](int) [messageType](byte) => 1(in four bytes) then the byte of Ping
        //0x5 = 4 for the size, 1 for the message type
        static readonly byte[] Ping_Message_Bytes = new byte[] { 0x5, 0x0, 0x0, 0x0, (byte)MessageType.Ping };
        static readonly byte[] Close_Message_Bytes = new byte[] { 0x5, 0x0, 0x0, 0x0, (byte)MessageType.Close };

        static readonly byte[] Empty_Array = new byte[0];

        public int MaxMessageSize => _maxMessageSize;

        readonly int _maxMessageSize;
        public MessageParser(int maxMessageSize)
        {
            if(maxMessageSize <= _minMessageSize)
            {
                throw new Exception($"{nameof(maxMessageSize)} can not be less than {_minMessageSize}");
            }
            _maxMessageSize = maxMessageSize;
        }

        public void ValidateSize(byte[] message, out int size)
        {
            var span = new Span<byte>(message);
            ValidateSize(span, out size);
        }

        public void ValidateSize(Span<byte> message, out int size)
        {
            size = BitConverter.ToInt32(message.Slice(0, _sizeLength).ToArray(), 0);
            if (size > _maxMessageSize)
                throw new Exception($"Received message is too big, receive {size}, while max is {_maxMessageSize}");
        }

        public Message GetMessage(byte[] message)
        {
            var span = new Span<byte>(message);

            if (span.IsEmpty) return new Message();

            ValidateSize(message, out int size);

            var messageType = (MessageType)span[_messageTypeIndex];

            if (messageType == MessageType.Ping)
            {
                return new Message { MessageType = MessageType.Ping };
            }

            if(messageType == MessageType.Close)
            {
                return new Message { MessageType = MessageType.Close };
            }

            var id = new Guid(span.Slice(_idIndex, _idLength).ToArray());
            var isEncrypted = span[_isEncryptedIndex] == 1;

            var addressSize = BitConverter.ToInt32(span.Slice(_addressSizeIndex).ToArray(), 0);
            var address = span.Slice(_addressIndex, addressSize).ToArray();

            var payloadIndex = _addressIndex + addressSize;
            var payload = span.Slice(payloadIndex).ToArray();

            return new Message
            {
                Id = id,
                MessageType = messageType,
                Address = address,
                Payload = payload
            };
        }

        public byte[] GetPingBytes() => Ping_Message_Bytes;

        public byte[] GetCloseBytes() => Close_Message_Bytes;

        public byte[] GetBytes(Message message)
        {
            var messageType = message.MessageType;

            if(messageType == MessageType.Ping)
            {
                return GetPingBytes();
            }

            if(messageType == MessageType.Close)
            {
                return GetCloseBytes();
            }

            var id = message.Id;
            var isEncrypted = message.IsEncrypted;
            var address = message.Address;
            var payload = message.Payload;

            var addressLength = address != null ? address.Length : 0;
            var payloadLength = payload != null ? payload.Length : 0;

            var msgSize = addressLength + payloadLength + _minMessageSize;

            if (msgSize > _maxMessageSize)
                throw new Exception("Message is too big");

            var result = new byte[msgSize];

            //put size 
            var size = BitConverter.GetBytes(msgSize);
            CopyArray(size, result);

            //put message type
            result[_messageTypeIndex] = (byte)messageType;

            //put id (size fixed to 16)
            CopyToIndex(id.ToByteArray(), result, _idIndex);

            //put isEncrypted
            result[_isEncryptedIndex] = Convert.ToByte(_isEncryptedIndex);

            //put address size
            var addressSize = BitConverter.GetBytes(addressLength);
            CopyToIndex(addressSize, result, _addressSizeIndex);

            if (addressLength > 0)
            {
                //put address
                CopyToIndex(address, result, _addressIndex);
            }
            if (payloadLength > 0)
            {
                var payloadIndex = _addressIndex + addressLength;
                //put payload
                CopyToIndex(payload, result, payloadIndex);
            }

            return result;
        }

        void CopyArray(byte[] src, byte[] dest)
        {
            Array.Copy(src, 0, dest, 0, src.Length);
        }

        void CopyToIndex(byte[] src, byte[] dest, int destIndex)
        {
            Array.Copy(src, 0, dest, destIndex, src.Length);
        }
    }
}
