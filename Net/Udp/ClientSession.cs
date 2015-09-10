using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using CoreFramework.Net;
using CoreFramework.Net.Events;
using CoreFramework.Net.Types;
using CoreFramework.Net.Types.Basic;

namespace CoreFramework.Net.Udp
{
    public class ClientSession : CommonSession, INetClientSession
    {
        private UdpClient client;
        private UdpState state;

        public event EventHandler ConnectFailed;

        public ClientSession() : base()
        {
            client = new UdpClient();
            state = new UdpState() { Name = "" };

            msgTypes[0] = new SignalType(0, false);
            msgTypes[2] = new NameChangeType(2, false);
            msgTypes[3] = new SignalType(3, false);
            msgTypes[6] = new ChatType(6, false);
        }

        public byte ID
        {
            get
            {
                return state.ID;
            }
        }
        public string Name
        {
            get
            {
                return state.Name;
            }
            set
            {
                state.Name = value;
            }
        }
        public bool Connected { get { return state.Initialized; } }

        public void Connect(string host, int port)
        {
            state.EndPoint = new IPEndPoint(Dns.GetHostAddresses(host)[0], port);
            byte[] nameArr = msgTypes[2].ClientFormatData(Name);
            client.Send(new byte[] { 3, 0 }, 2, state.EndPoint);
            client.Send(nameArr, nameArr.Length, state.EndPoint);
            client.Send(new byte[] { 3, 1 }, 2, state.EndPoint);
            state.Connected = true;
            state.EndPoint = state.EndPoint;
            try
            {
                client.BeginReceive(new AsyncCallback(RecvAck), client);
            }
            catch (SocketException ex)
            {
#if DEBUG
                Console.WriteLine("UdpClient: " + ex.Message);
#endif
                if (ConnectFailed != null) ConnectFailed(this, new EventArgs());
            }
        }
        public void Connect(string host, int port, Dictionary<string, string> info)
        {

        }
        public void Disconnect()
        {
            RaiseOnDisconnect(new ConnectionArgs(state.ID, state.Name));
            client.Send(new byte[] { 0, 0 }, 2, state.EndPoint);
            Reset();
        }
        private void Reset()
        {
            client.Close();
            client = new UdpClient();
            state.Connected = false;
            state.Initialized = false;
            state.ID = 0;
        }
        private void TryReceive()
        {
            try
            {
                client.BeginReceive(new AsyncCallback(RecvAck), client);
            }
            catch (SocketException ex)
            {
#if DEBUG
                Console.WriteLine("UdpClient: " + ex.Message);
#endif
                if (state.Initialized)
                    RaiseOnDisconnect(new ConnectionArgs(state.ID, state.Name));
                Reset();
            }
        }
        protected override bool OnRecv(BaseNetState state, byte msgType, object data)
        {
            switch (msgType)
            {
                case 0:
                    byte sig = (byte)data;
#if DEBUG
                    Console.WriteLine("UdpClient {0}: Disconnected with Reason {1}", state.ToString(), sig);
#endif
                    if (state.Initialized)
                        RaiseOnDisconnect(new ConnectionArgs(state.ID, state.Name));
                    return true;
                case 2:
                    string name = (string)data;
#if DEBUG
                    Console.WriteLine("UdpClient {0}: Name changed to {1}", state.ToString(), name);
#endif
                    state.Name = name;
                    return true;
                case 3:
                    state.Initialized = true;
                    state.ID = (byte)data;
#if DEBUG
                    Console.WriteLine("UdpClient {0}: Connected with ID {1}", state.ToString(), state.ID);
#endif
                    RaiseOnConnect(new ConnectionArgs(state.ID, state.Name));
                    return true;
            }

            return false;
        }
        private void RecvAck(IAsyncResult result)
        { 
            IPEndPoint remote = null;
            byte[] payload = null;
            try
            {
                payload = client.EndReceive(result, ref remote);
            }
            catch (ObjectDisposedException ex)
            {
                return;
            }
            byte msgType = payload[0];
            object data = msgTypes[msgType].ClientFormatData(payload);

            if (!OnRecv(state, msgType, data))
                if (state.Initialized)
                    RaiseMessageReceived(new NetMsgArgs(state.ID, state.Name, msgType, data));
            
            client.BeginReceive(new AsyncCallback(RecvAck), client);
        }
        private void SendAck(IAsyncResult result)
        {
            client.EndSend(result);
        }

        public void SendMessage(byte msgType, object arg)
        {
            byte[] data = msgTypes[msgType].ServerFormatData(arg);
            client.BeginSend(data, data.Length, state.EndPoint, new AsyncCallback(SendAck), client);
        }
    }
}
