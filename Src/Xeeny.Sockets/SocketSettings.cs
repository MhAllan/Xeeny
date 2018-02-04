using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets
{
    public class SocketSettings
    {
        //this is also the socket's SendTimeout
        /// <summary>
        /// The timeout of the connection
        /// <para>Default is 30 seconds</para>
        /// </summary>
        public ConnectionTimeout Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// <para></para>
        /// Socket Receive Timeout, When no receiving beyond this time the remote is Idle and connection closes
        /// <remarks>For callback you should put this to TimeSpan.MaxValue or the server won't be able to callback anytime
        /// You can chose to set it on a callback to ignore server callbacks beyond a given time</remarks>
        /// <para>Default is 600 seconds on server and TimeSpan.MaxValue on callbacks</para>
        /// </summary>
        public ConnectionTimeout ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(600);

        /// <summary>
        /// <para>KeelAlive messages interval</para>
        /// <remarks>If you set this to small value, you may increase KeepAliveRetries as well, and vice versa
        /// </remarks>
        /// <para>Default is 30 seconds</para>
        /// </summary>
        public ConnectionTimeout KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30);
        /// <summary>
        /// KeepAlive retries, connection closes after this number of failed keep alive messages
        /// <para>Default is 10 retries</para>
        /// </summary>
        public byte KeepAliveRetries { get; set; } = 10;


        /// <summary>
        /// <para></para>
        /// <para>The size of the receiving buffer (Default 4096 = 4 KB)</para>
        /// <remark>Sender's <c>SendBufferSize</c> and receiver's <c>ReceiveBufferSize</c>should be equal</remark>
        /// <remark>Smaller sizes won't affect the ability of receiving big messages,
        /// If you set it big and your messages are small it will occupy more memory than you need,
        /// If you set it small and your messages are big it will introduce more IO operations</remark>
        /// <para>Default 4096 = 4 KB</para>
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 4096; // 4 KB

        /// <summary>
        /// <para></para>
        /// <para>The size of the sending buffer (Default 4096 = 4 KB)</para>
        /// <remark>Sender's <c>SendBufferSize</c> and receiver's <c>ReceiveBufferSize</c>should be equal</remark>
        /// <remark>Smaller sizes won't affect the ability of sending big messages,
        /// If you set it big and your messages are small it will occupy more memory than you need,
        /// <para>Default 4096 = 4 KB</para>
        /// </summary>
        public int SendBufferSize { get; set; } = 4096; // 4 KB

        /// <summary>
        /// Maximum message size (Default 1000000 = 1 MB) 
        /// usually you will set this big and set ReceiveBufferSize small, 
        /// the message will be received on ReceiveBuffer first, then Xeeny reads the size of 
        /// the whole message from that buffer. if the size is valid against MaxMessageSize then the receive continues
        /// <para>Default 1000000 = 1 MB</para>
        /// </summary>
        public int MaxMessageSize { get; set; } = 1000000; //1 MB
    }
}
