using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets
{
    public enum FramingProtocol
    {
        /// <summary>
        /// If messages are fragmented (SendBufferSize is smaller than message) this option will send message parts in serial way, any concurrent method calls will be pending until current call completes 
        /// <para>Server and Client should have same FramingProtocol</para>
        /// </summary>
        SerialFragments,
        /// <summary>
        /// If messages are fragmented (SendBufferSize is smaller than message) this option will send message parts in concurrent way, concurrent method calls can interleave
        /// <para>Server and Client should have same FramingProtocol</para>
        /// </summary>
        ConcurrentFragments
    }
}
