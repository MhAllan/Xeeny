using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeeny.Transports.Channels
{
    class MessageAssemblerManager : IDisposable
    {
        readonly Timer _timer;
        readonly int _timeoutMS;

        ConcurrentDictionary<Guid, MessageAssembler> _assemblers = new ConcurrentDictionary<Guid, MessageAssembler>();

        public MessageAssemblerManager(int timeoutMS)
        {
            if(timeoutMS <= 0)
            {
                throw new ArgumentException(nameof(timeoutMS));
            }

            _timer = new Timer(Clean, null, 0, timeoutMS);
            _timeoutMS = timeoutMS;
        }

        void Clean(object sender)
        {
            var now = DateTime.Now;
            foreach(var assembler in _assemblers.Values)
            {
                if((now - assembler.CreationTime).TotalMilliseconds > _timeoutMS)
                {
                    DisposeAssembler(assembler);
                }
            }
        }

        void DisposeAssembler(MessageAssembler assembler)
        {
            assembler.Dispose();
            _assemblers.TryRemove(assembler.MessageId, out var _);
        }

        public bool AddPartialAndTryGetMessage(int totalSize, Guid messageId, int index, ArraySegment<byte> partialMessage,
            out ArraySegment<byte> result)
        {
            result = default;

            if(!_assemblers.TryGetValue(messageId, out var assembler))
            {
                assembler = new MessageAssembler(messageId, totalSize);
                _assemblers.TryAdd(messageId, assembler);
            }
            
            if(assembler.AddPartialMessage(partialMessage, index))
            {
                result = assembler.GetMessage();
                DisposeAssembler(assembler);
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            _timer.Dispose();
            foreach(var assembler in _assemblers.Values)
            {
                DisposeAssembler(assembler);
            }
            _assemblers = null;
        }

    }
}
