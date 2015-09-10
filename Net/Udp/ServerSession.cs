using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Timers;

using CoreFramework.Net;
using CoreFramework.Net.Events;
using CoreFramework.Net.Types;
using CoreFramework.Net.Types.Basic;

namespace CoreFramework.Net.Udp
{
    public class ServerSession : CommonSession, INetServerSession
    {
        private UdpClient client;

        private UdpState[] states;
        private Dictionary<EndPoint, byte> endPoints;
        private bool requestingHeartbeat;
        private Timer heartbeatTimer;

        public event EventHandler<ConnectionRequestArgs> ConnectionRequest;

        public int Port { get; private set; }
        public string DefaultName { get; set; }
        public int HBTimeout { get; set; }

        public ServerSession() : base()
        {
            client = new UdpClient();
            states = new UdpState[256];
            endPoints = new Dictionary<EndPoint, byte>();
            DefaultName = "NoName";
            HBTimeout = 3000;
            heartbeatTimer = new Timer() { Interval = HBTimeout };
            

            msgTypes[0] = new SignalType(0, false);
            msgTypes[1] = new DictionaryMessageType(1, false);
            msgTypes[2] = new NameChangeType(2, false);
            msgTypes[3] = new SignalType(3, false);
            msgTypes[6] = new ChatType(4, false);
        }

        protected override bool OnRecv(BaseNetState netState, byte msgType, object data)
        {
            UdpState state = netState as UdpState;

            switch (msgType)
            {
                case 0: //Disconnect
                    byte signal = (byte)data;
                    if (state.Connected)
                    {
                        RemoveClient(state.EndPoint);
                        if (state.Connected) RaiseOnDisconnect(new ConnectionArgs(state.ID, state.Name));
#if DEBUG
                        Console.WriteLine("UdpServer: Client {0} Disconnected. Reason {1}", state.ToString(), signal);
#endif
                    }
                    return true;
                case 2: //Name Change
                    if (state != null)
                    {
                        string name = VerifyName((string)data);
#if DEBUG
                        Console.WriteLine("UdpServer: Client {0} changing name to {1}", state.ToString(), name);
#endif
                        state.Name = name;
                        byte[] nameArr = msgTypes[2].ServerFormatData(name);
                        client.Send(nameArr, nameArr.Length, state.EndPoint);
                    }
                    return true;
                case 3: //Connection Request
                    byte sig = (byte)data;
                    if (sig == 0)//Want to Connect
                    {
                        if (state.Connected) return true; //already made a request
#if DEBUG
                        Console.WriteLine("UdpServer: Remote {0} wants to connect", state.EndPoint);
#endif
                        byte id = FreeID();
                        if (id == 255)
                            return true;
                        state.Connected = true;
                        state.ID = id;
                        states[id] = state;
                        endPoints.Add(state.EndPoint, id);
                    }
                    else if (sig == 1)//Finished Initalization
                    {
                        if (state.Connected && state.Name != "")//Initalize requirement
                        {
#if DEBUG
                            Console.WriteLine("UdpServer: Client {0} Connected", state.ToString());
#endif
                            state.Initialized = true;
                            SendMessage(state.ID, msgTypes[3].ServerFormatData((object)state.ID));
                            RaiseOnConnect(new ConnectionArgs(state.ID, state.Name));
                        }
                    }
                    else if (sig == 2) //heartbeat response
                    {

                    }
                    return true;
                case 4: //Auth Info
                    Dictionary<string, string> authInfo = data as Dictionary<string, string>;
                    ConnectionRequestArgs arg = new ConnectionRequestArgs(state.ID, state.Name, authInfo);
                    if (ConnectionRequest != null) ConnectionRequest(this, arg);
                    if (!arg.Approved)
                        Kick(state.ID);
                    return true;
            }
            return false;
        }
        private void RemoveClient(EndPoint point)
        {
            UdpState state = states[endPoints[point]];
            states[state.ID] = null;
            endPoints.Remove(point);
        }
        private void RecvAck(IAsyncResult result)
        {
            IPEndPoint remoteIP = null;
            byte[] payload = null;

//            try
//            {
                payload = client.EndReceive(result, ref remoteIP);
//            }
//            catch (SocketException ex)
//            {
//#if DEBUG
//                Console.WriteLine("UdpServer: Error {0}: {1}, retrying", ex.ErrorCode, ex.Message);
//#endif
//                client.BeginReceive(new AsyncCallback(RecvAck), null);
//                return;
//            }

            byte msgType = payload[0];
            object data = msgTypes[msgType].ServerFormatData(payload);

            UdpState state = null;
            if (endPoints.ContainsKey(remoteIP))
                state = states[endPoints[remoteIP]];
            else
                state = new UdpState() { EndPoint = remoteIP };

            if (!OnRecv(state, msgType, data))
                if (state != null && state.Initialized)
                    RaiseMessageReceived(new NetMsgArgs(state.ID, state.Name, msgType, data));
            try
            {
                client.BeginReceive(new AsyncCallback(RecvAck), null);
            }
            catch (SocketException ex)
            {
#if DEBUG
                //if (ex.ErrorCode != 10054)
                Console.WriteLine("UdpServer: IP {0} SocketIssue. {1}", state.EndPoint, ex.Message);
#endif
                if (!requestingHeartbeat)
                    RequestHeartbeat();
                client.BeginReceive(new AsyncCallback(RecvAck), null);
            }
        }
        private void SendAck(IAsyncResult result)
        {
            int i = client.EndSend(result);
            //Console.WriteLine("UdpServer: " + i);
        }
        /// <summary>
        /// 255 is server id, meaning no free id
        /// </summary>
        /// <returns></returns>
        private byte FreeID()
        {
            for (byte i = 0; i < 255; i++)
                if (states[i] == null)
                    return i;
            return 255;
        }
        private string VerifyName(string req)
        {
            int dup = 0;
            foreach (UdpState obj in states)
            {
                if (obj == null) continue;
                if (dup == 0 && obj.Name == req)
                    dup += 1;
                else if (dup > 0 && obj.Name == req + "(" + dup + ")")
                    dup += 1;
            }
            return dup == 0 ? req : req + "(" + dup + ")";
        }
        private void RequestHeartbeat()
        {
            requestingHeartbeat = true;
            heartbeatTimer.Start();
        }

        public void Start()
        {
            Start(Port);
        }
        public void Start(int port)
        {
            Port = port;
            Start(new IPEndPoint(IPAddress.Any, port));
        }
        public void Start(EndPoint endPoint)
        {
            client.Client.Bind(endPoint);
            client.BeginReceive(new AsyncCallback(RecvAck), null);
        }
        public void Stop()
        {

        }

        private void SendMessage(byte id, byte[] payload)
        {
            if (states[id] == null) return;
            if (states[id].Initialized)
                client.BeginSend(payload, payload.Length, states[id].EndPoint, new AsyncCallback(SendAck), null);
            else
                Console.WriteLine("UdpServer: Error: id {0} ({1}) not initialized, cannot send message.", id, states[id].EndPoint.Address);
        }
        /// <summary>
        /// Sends the message to all connected clients
        /// </summary>
        /// <param name="msgType">Message Serializer ID</param>
        /// <param name="arg">Data being serialized</param>
        public void SendMessage(byte msgType, object arg)
        {
            byte[] data = msgTypes[msgType].ServerFormatData(arg);
            foreach (byte id in endPoints.Values)
                SendMessage(id, data);
        }
        /// <summary>
        /// Sends the message to specified clients
        /// </summary>
        /// <param name="ids">List of IDs to send to</param>
        /// <param name="msgType">Message Serializer ID</param>
        /// <param name="arg">Data being serialized</param>
        public void SendMessage(byte[] ids, byte msgType, object arg)
        {
            byte[] data = msgTypes[msgType].ServerFormatData(arg);
            foreach (byte id in ids)
                SendMessage(id, data);
        }
        public void Kick(byte id)
        {
            Kick(id, 1);
        }
        public void Kick(byte id, byte reason)
        {
            SendMessage(id, msgTypes[0].ServerFormatData((object)reason));
            RemoveClient(states[id].EndPoint);
        }
    }
}
