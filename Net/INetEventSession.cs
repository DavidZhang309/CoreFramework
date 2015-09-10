using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CoreFramework.Net.Events;
using CoreFramework.Net.Types;

namespace CoreFramework.Net
{
    public interface INetEventSession
    {
        event EventHandler<NetMsgArgs> MessageReceived;
        event EventHandler<ConnectionArgs> OnConnect;
        event EventHandler<ConnectionArgs> OnDisconnect;
        void AddMessageType(INetMessageType type);
    }

    public interface INetServerSession : INetEventSession
    {
        event EventHandler<ConnectionRequestArgs> ConnectionRequest;

        void Start();
        void Start(int port);
        void Stop();
        void SendMessage(byte msgType, object data);
        void SendMessage(byte[] ids, byte msgType, object data);
        void Kick(byte id);
        void Kick(byte id, byte reason);
    }

    public interface INetClientSession : INetEventSession
    {
        event EventHandler ConnectFailed;

        void Connect(string host, int ip);
        void Disconnect();
        void SendMessage(byte msgType, object data);
    }
}
