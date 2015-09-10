using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using CoreFramework.Net.Events;
using CoreFramework.Net.Types;
using CoreFramework.Net.Types.Basic;

namespace CoreFramework.Net.Tcp
{
    public class ClientSession : CommonSession, INetClientSession
    {
        TcpState serverState;
        int bufferSize = 255;

        public event EventHandler ConnectFailed;

        public event EventHandler<NetChatArgs> OnChat;
        public event EventHandler<NetNameArgs> ObtainedNameList;

        public ClientSession()
        {
            msgTypes[0] = new SignalType(0, true);
            msgTypes[2] = new NameChangeType(2, true);
            msgTypes[3] = new SignalType(3, true);
            msgTypes[6] = new ChatType(6, true);
        }

        public string Name { get; set; }

        public void Initalize()
        {
            //bufferSize = Convert.ToInt32(console["net_buffersize"]);
        }
        public void Connect(string host, int port)
        {
            serverState = new TcpState();
            serverState.ByteData = new List<byte>();
            serverState.Buffer = new byte[bufferSize];
            serverState.Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverState.Data = "";
            serverState.Name = Name;

            serverState.Connection.BeginConnect(IPAddress.Parse(host), port, new AsyncCallback(RecvConnection), serverState);
        }
        public void Disconnect()
        {
            byte[] msg = new byte[6];
            NetHelper.AppendEOF(msg);
            serverState.Connection.Send(msg);
            serverState.Connection.Close();
            serverState.Closed = true;
        }
        public void SendMessage(byte id, object data)
        {
            byte[] msg = msgTypes[id].ClientFormatData(data);
            byte[] sending = new byte[msg.Length + 1];
            sending[0] = id;
            for (int i = 0; i < msg.Length; i++)
                sending[i + 1] = msg[i];
            serverState.Connection.BeginSend(sending, 0, sending.Length, 0, new AsyncCallback(SendAck), serverState);
        }
        public void SendChat(string msg)
        {
            byte[] data = new byte[msg.Length + 9];
            data[0] = 6;
            data[1] = 0;
            data[2] = (byte)(msg.Length / 256);
            data[3] = (byte)(msg.Length % 256);
            for (int i = 0; i < msg.Length; i++)
                data[4 + i] = (byte)msg[i];
            NetHelper.AppendEOF(data, data.Length - 5);

            serverState.Connection.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendAck), serverState);
        }

        protected override bool OnRecv(BaseNetState state, byte msgType, object data)
        {
            switch (msgType)
            {
                case 0:
                    //console.Print("Client", "Info", string.Format("Server shutting down({0})", e.FormattedData));
                    RaiseOnDisconnect(new ConnectionArgs(serverState.ID, serverState.Name));
                    return true;
                case 2:
                    serverState.Name = (string)data;
                    //Temporary - Initialize connection
                    byte[] sending = msgTypes[3].ClientFormatData((byte)0);
                    serverState.Connection.BeginSend(sending, 0, sending.Length, 0, new AsyncCallback(SendAck), serverState);
                    return true;
                case 3:
                    serverState.ID = (byte)data;
                    //console.Print("Client", "Info", "Connected to server. Received id of " + serverState.id);
                    byte[] sendingData = msgTypes[msgType].ClientFormatData((byte)0);
                    RaiseOnConnect(new ConnectionArgs(serverState.ID, serverState.Name));
                    return true;
                case 6:
                    //console.Print("Client", "Info", "Server sent chat");
                    if (OnChat != null)
                        OnChat(this, new NetChatArgs(255, (string)data));
                    return true;
            }
            return false;
        }
        private void RecvConnection(IAsyncResult args)
        {
            serverState.Connection.EndConnect(args);
            serverState.Connected = true;
            //console.Print("Client", "Info", "Connecting to server");
            if (serverState.Name == null) serverState.Name = Constants.DEFAULT_NAME;
            byte[] sendingData = msgTypes[2].ClientFormatData(serverState.Name);
            serverState.Connection.Send(sendingData);
            serverState.Connection.BeginReceive(serverState.Buffer, 0, bufferSize, 0, new AsyncCallback(RecvMsg), serverState);
        }
        private void RecvMsg(IAsyncResult args)
        {
            int bytesA = 0;
            try
            {
                bytesA = serverState.Connection.EndReceive(args);
            }
            catch (SocketException ex)
            {
                //console.Print("Client", "Info", "Client has unexpected disconnection");
                if (!serverState.Closed) Disconnect();
                return;
            }

            if (bytesA == 0) return;
            //transfer buffer data
            for (int i = 0; i < bytesA; i++)
            {
                serverState.Data += (char)serverState.Buffer[i];
                serverState.ByteData.Add(serverState.Buffer[i]);
                serverState.Buffer[i] = 0;
            }

            byte[] sentData = serverState.ByteData.ToArray();
            INetMessageType msgHandler = msgTypes[sentData[0]];
            int pos = -1;
            if (msgHandler != null)
            {
                pos = msgHandler.ClientEOFPosition(sentData);
                if (pos != -1)
                {
                    object data = msgHandler.ClientFormatData(sentData);
                    if (!OnRecv(serverState, msgHandler.MessageType, data) & serverState.Initialized)
                        RaiseMessageReceived(new NetMsgArgs(serverState.ID, serverState.Name, msgHandler.MessageType, data));
                    serverState.Data = serverState.Data.Substring(pos + 5);
                    serverState.ByteData.RemoveRange(0, pos + 5);
                }
            }
            try
            {
                serverState.Connection.BeginReceive(serverState.Buffer, 0, bufferSize, 0, new AsyncCallback(RecvMsg), serverState);
            }
            catch (SocketException ex)
            {
                //console.Print("Client", "Info", "Client has unexpected disconnection");
                if (!serverState.Closed)
                    Disconnect();
                return;
            }
            catch (ObjectDisposedException ex)
            {
                //console.Print("Client", "Info", "Caught a lingering async op(RecvMsg): " + ex.Message);
            }
        }
        private void SendAck(IAsyncResult args)
        {
            serverState.Connection.EndSend(args);
        }
    }
}
