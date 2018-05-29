using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports
{
    public delegate string ConnectionNameFormatter(string connectionId);

    public class TransportSettings
    {
        public ConnectionTimeout Timeout { get; set; } = ConnectionTimeout.FromSeconds(30);
        public ConnectionTimeout ReceiveTimeout { get; set; } = ConnectionTimeout.FromSeconds(600);
        public ConnectionTimeout KeepAliveInterval { get; set; } = ConnectionTimeout.FromSeconds(30);

        /// <summary>
        /// Connection name formatter, this is for logging. First argument is connection id.
        /// </summary>
        public ConnectionNameFormatter ConnectionNameFormatter { get; set; }
    }
}
