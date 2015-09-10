using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace CoreFramework.Net.Tcp
{
    /// <summary>
    /// Tcp Connection State
    /// </summary>
    public class TcpState : BaseNetState
    {
        public Socket Connection { get; set; }
        public bool Closed { get; set; }
        public byte[] Buffer { get; set; }
        public string Data { get; set; }
        public List<byte> ByteData { get; set; }
    }
}
