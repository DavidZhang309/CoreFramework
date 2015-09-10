using System.Net;

namespace CoreFramework.Net.Udp
{
    /// <summary>
    /// Udp Connection State
    /// </summary>
    public class UdpState : BaseNetState
    {
        public IPEndPoint EndPoint { get; set; }
    }
}
