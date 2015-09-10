using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using CoreFramework.Net.Events;
using CoreFramework.Net.Types;
using CoreFramework.Net.Types.Basic;

namespace CoreFramework.Net.Tcp
{
    public class ServerSession : CommonSession, INetServerSession
    {
        Dictionary<Socket, int> connections;
        List<Socket> connecting;
        TcpState[] states;

        Socket listener;
        int port;
        int bufferSize;
        bool idling = true;
        //bool listening;

        public event EventHandler<ConnectionRequestArgs> ConnectionRequest;

        public ServerSession()
        {
            connections = new Dictionary<Socket, int>();
            connecting = new List<Socket>();
            msgTypes = new INetMessageType[256];
            msgTypes[0] = new SignalType(0, true);
            msgTypes[2] = new NameChangeType(2, true);
            msgTypes[3] = new SignalType(3, true);
            msgTypes[6] = new ChatType(6, true);
        }

        /// <summary>
        /// Internal ID
        /// 0 - Disconnecting
        /// 2 - Name Change
        /// 3 - Initialized Connection Msg
        /// 4 - Extended Msg (stub)
        /// 5 - RC data(stub)
        /// 6 - Chat
        ///   -> 0 - Broadcast to all
        ///   -> 1 - 
        ///   -> 2 - 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override bool OnRecv(BaseNetState state, byte msgType, object data)
        {
            TcpState tcpState = state as TcpState;
            switch (msgType)
            {
                case 0:
                    //if (console != null) console.Print("Server", "Info", string.Format("Client {0} Disconnect ({1})", e.ClientID, e.FormattedData));
                    RemoveClient(state.ID);
                    return true;
                case 2:
                    //verify
                    state.Name = VerifyName((string)data);
                    //send
                    byte[] sendingData = msgTypes[msgType].ServerFormatData(state.Name);
                    tcpState.Connection.BeginSend(sendingData, 0, state.Name.Length + 8, 0, new AsyncCallback(SendAck), state);
                    return true;
                case 3:
                    if ((byte)data == 0)
                    {
                        state.Initialized = true;
                        //if (console != null) console.Print("Server", "Info", "Client (" + state.id + ")" + state.connection.LocalEndPoint + " Connected");
                        RaiseOnConnect(new ConnectionArgs(state.ID, state.Name));
                    }
                    //send names
                    //byte[] sending = PackageNames();
                    //state.connection.BeginSend(sending, 0, sending.Length, 0, new AsyncCallback(SendAck), state);
                    return true;
                case 6:
                    ChatData chat = data as ChatData;
                    byte[] payload = msgTypes[msgType].ServerFormatData(chat.Message);
                    if (chat.Recievers == null)
                    {
                        for (int i = 0; i < states.Length; i++)
                        {
                            TcpState recipiantState = states[i];
                            if (recipiantState == null) continue;
                            recipiantState.Connection.BeginSend(payload, 0, payload.Length, 0, new AsyncCallback(SendAck), recipiantState);
                        }
                        return true;
                    }
                    foreach (byte recipiant in chat.Recievers)
                    {
                        TcpState recipiantState = states[recipiant];
                        recipiantState.Connection.BeginSend(payload, 0, payload.Length, 0, new AsyncCallback(SendAck), recipiantState);
                    }
                    return true;
            }
            return false;
        }

        public int Port
        {
            get { return port; }
            private set { port = value; }
        }

        /// <summary>
        /// Resets based on cvars provided by CvarConsole
        /// </summary>
        public void Initalize()
        {
            Port = Constants.PORT;
            int max = Constants.MAX_CONNECTIONS;
            states = new TcpState[max <= Constants.MAX_CONNECTIONS ? max : Constants.MAX_CONNECTIONS];
            bufferSize = Constants.MAX_BUFFERSIZE;

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(false, 0));

            //console.Print("Server", "TEST", DataEOF(NetHelper.AppendEOF(new byte[] { 3 }), 3).ToString());
        }
        public void Start()
        {
            Start(port);
        }
        public void Start(int CustomPort)
        {
            if (!idling)
                throw new InvalidOperationException("Server Already Listening");
            listener.Bind(new IPEndPoint(IPAddress.Any, CustomPort));
            listener.Listen(64);
            listener.BeginAccept(new AsyncCallback(RecvConnection), listener);
            idling = false;
            System.Threading.Thread.Sleep(500);
            //if (console != null)
            //{
            //    console["net_port"] = CustomPort.ToString();
            //    console.Print("Server", "Info", "Server Started");
            //}
        }
        //Shuts down the listener 
        //due to the async nature of network, this will take at least 2 seconds
        public void Stop()
        {
            idling = true;
            byte[] endMsg = new byte[7];
            NetHelper.AppendEOF(endMsg);
            foreach (int connectionID in connections.Values)
            {
                states[connectionID].Connection.Send(endMsg);
                states[connectionID].Connection.Close();
            }
        }
        public void SendMessage(byte id, object data)
        {
            byte[] sending = msgTypes[id].ServerFormatData(data);
            for (byte i = 0; id < states.Length; i++)
                if (states[i] != null) states[i].Connection.BeginSend(sending, 0, sending.Length, 0, new AsyncCallback(SendAck), states[i]);
        }
        public void SendMessage(byte client, byte id, object data)
        {
            SendMessage(new byte[] { client }, id, data);
        }
        public void SendMessage(byte[] clients, byte msgType, object data)
        {
            byte[] msg = msgTypes[msgType].ClientFormatData(data);
            byte[] sending = new byte[msg.Length + 1];
            sending[0] = msgType;
            for (int i = 0; i < msg.Length; i++)
                sending[i + 1] = msg[i];
            foreach(byte client in clients)
                states[client].Connection.BeginSend(sending, 0, sending.Length, 0, new AsyncCallback(SendAck), states[client]);
        }

        public void SendChat(string msg)
        {
            byte[] data = msgTypes[6].ServerFormatData(msg);
            foreach (TcpState client in states)
                if (client != null && client.Initialized)
                {
                    //System.Console.WriteLine("Sending to " + client.id);
                    client.Connection.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendAck), client);
                }
        }
        public void SendChat(byte[] clients, string msg)
        {
            byte[] data = new byte[msg.Length + 9];
            data[0] = 6;
            data[1] = 255;
            NetHelper.PackageString(msg, data, 2);
            NetHelper.AppendEOF(data, data.Length - 5);
            foreach (byte client in clients)
                if (states[client] != null && states[client].Initialized)
                    states[client].Connection.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendAck), client);
        }
        public void Kick(byte id)
        {

        }
        public void Kick(byte id, byte reason)
        { }

        private byte NextID()
        {
            for (byte i = 0; i < states.Length; i++)
                if (states[i] == null)
                    return i;
            return 255;
        }
        private void GatherNames(byte[] ids, string[] names)
        {
            int count = 0;
            foreach (TcpState state in states)
                if (state != null && state.Initialized)
                {
                    ids[count] = state.ID;
                    names[count] = state.Name;
                    count++;
                }
        }
        private byte[] PackageNames()
        {
            int countOfNames = 0;
            foreach (TcpState stateObj in states)
                if (stateObj != null && stateObj.Initialized)
                    countOfNames++;
            byte[] ids = new byte[countOfNames];
            string[] names = new string[countOfNames];
            GatherNames(ids, names);
            byte[] sending = new byte[NetHelper.PkgStringsCount(ids, names) + 7];
            sending[0] = 102;
            sending[1] = 0;
            NetHelper.PackageStrings(ids, names, sending, 2);
            NetHelper.AppendEOF(sending, sending.Length - 5);
            return sending;
        }
        private void RemoveClient(byte id)
        {
            connections.Remove(states[id].Connection);
            states[id] = null;
        }
        private void RemoveClient(TcpState state)
        {
            //console.Print("Server", "Info", state.id + " Disconnected(Manual)");
            if (state.Initialized)
                RaiseOnDisconnect(new ConnectionArgs(state.ID, state.Name));
            RemoveClient(state.ID);
        }
        private string VerifyName(string req)
        {
            int dup = 0;
            foreach (TcpState obj in states)
            {
                if (obj == null) continue;
                if (dup == 0 && obj.Name == req)
                    dup += 1;
                else if (dup > 0 && obj.Name == req + "(" + dup + ")")
                    dup += 1;
            }
            return dup == 0 ? req : req + "(" + dup + ")";
        }

        private void RecvConnection(IAsyncResult args)
        {
            Socket client = listener.EndAccept(args);
            if (idling)
            {
                //console.Print("Server", "Info", "Server hasn't started");
                //client.BeginSend()
                client.Disconnect(false);
                listener.BeginAccept(new AsyncCallback(RecvConnection), listener);
                return;
            }
            byte id = NextID();
            if (id == 255)//if no more room, send 0
            {
                try
                {
                    //console.Print("Server", "Info", "No room for client");
                    byte[] sendingData = msgTypes[0].ServerFormatData((byte)0);
                    client.BeginSend(sendingData, 0, sendingData.Length, 0, new AsyncCallback(SendAck), null);
                    client.Disconnect(false);
                }
                catch (SocketException ex)
                {
                    //console.Print("Server", "Error", ex.Message);
                }
            }
            else
            {
                TcpState state = new TcpState();
                state.Connection = client;
                state.Buffer = new byte[bufferSize];
                state.ByteData = new List<byte>();
                state.ID = id;
                state.Data = "";
                //admin
                connections.Add(client, id);
                states[id] = state;
                connecting.Add(client);
                //console.Print("Server", "Info", "[Server]Connecting: (" + id + ") " + client.LocalEndPoint);
                try
                {
                    //sends 3, id to client
                    byte[] sentData = msgTypes[3].ServerFormatData((byte)id);
                    client.BeginSend(sentData, 0, sentData.Length, 0, new AsyncCallback(SendAck), state);
                    //client.BeginSend(new byte[]{ 3, 0, 1, (byte)id, 60, 69, 79, 70, 62 }, 0, 9, 0, new AsyncCallback(SendAck), state);
                    client.BeginReceive(state.Buffer, 0, bufferSize, 0, new AsyncCallback(RecvMsg), state);
                }
                catch (SocketException ex)
                {
                    //console.Print("Server", "Error", "[Server]Connection " + id + " Disconnected: " + ex.Message);
                    RemoveClient(id);
                }
            }
            //start listening
            listener.BeginAccept(new AsyncCallback(RecvConnection), null);
        }
        private void RecvMsg(IAsyncResult args)
        {
            TcpState state = args.AsyncState as TcpState;
            int bytesA = 0;
            try
            {
                bytesA = state.Connection.EndReceive(args);
            }
            catch (SocketException ex)
            {
                //console.Print("Server", "Error", "Client " + state.id + " had error: " + ex.Message);
                if (state.Initialized)
                    RaiseOnDisconnect(new ConnectionArgs(state.ID, state.Name));
                state.Connection.Close();
                state.Closed = true;
                RemoveClient(state.ID);
            }

            if (bytesA > 0)
            {
                for (int i = 0; i < bytesA; i++)
                {
                    state.Data += (char)state.Buffer[i];
                    state.ByteData.Add(state.Buffer[i]);
                    state.Buffer[i] = 0;
                }
                byte[] sentData = state.ByteData.ToArray();
                INetMessageType msgHandler = msgTypes[sentData[0]];
                int pos = -1;//position of EOF
                if (msgHandler != null)
                {
                    pos = msgHandler.ServerEOFPosition(sentData);
                    if (pos != -1)
                    {
                        object objData = msgHandler.ServerFormatData(sentData);
                        if (OnRecv(state, msgHandler.MessageType, objData) & state.Initialized)
                            RaiseMessageReceived(new NetMsgArgs(state.ID, state.Name, msgHandler.MessageType, objData));
                        state.Data = state.Data.Substring(pos + 5);
                        state.ByteData.RemoveRange(0, pos + 5);
                    }
                }
            }
            try
            {
                if (state.Closed) return;
                state.Connection.BeginReceive(state.Buffer, 0, 1, 0, new AsyncCallback(RecvMsg), state);
            }
            catch (SocketException ex)
            {
                //if (console != null) console.Print("Client", "Info", "Client " + state.id + " has error" + ex.Message);
                if (!state.Closed)
                {
                    if (state.Initialized) RaiseOnDisconnect(new ConnectionArgs(state.ID, state.Name));
                    state.Connection.Close();
                    state.Closed = true;
                    RemoveClient(state.ID);
                }
            }
        }
        private void SendAck(IAsyncResult args)
        {
            Socket socket = args.AsyncState as Socket;
            if (socket == null) return;

            try
            {
                socket.EndSend(args);
            }
            catch (SocketException ex)
            {
                //if (console != null)
                //    System.Diagnostics.Debug.Assert(console["developer"] == "1", ex.Message);
                //else
                    System.Diagnostics.Debug.Assert(false, ex.Message);
            }
        }
    }
}
