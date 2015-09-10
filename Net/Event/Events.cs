using System;
using System.Collections.Generic;
using CoreFramework.Net.Types;

namespace CoreFramework.Net.Events
{
    public class NetTypeEventArgs : EventArgs
    {
        public NetTypeEventArgs(byte id, INetMessageType handler, object data)
        {
            Handler = handler;
            FormattedData = data;
            ClientID = id;
        }

        public INetMessageType Handler { get; private set; }
        public object FormattedData { get; private set; }
        public byte ClientID { get; private set; }
    }
    public class ConnectionArgs : EventArgs
    {
        public ConnectionArgs(byte id, string name)
        {
            ID = id;
            Name = name;
        }

        public byte ID { get; private set; }
        public string Name { get; private set; }
    }
    public class ConnectionRequestArgs : ConnectionArgs
    {
        private Dictionary<string, string> properties;
        public bool Approved { get; set; }

        public ConnectionRequestArgs(byte id, string name, Dictionary<string, string> properties)
            : base(id, name)
        {
            this.properties = properties;
        }

        public string this[string property]
        {
            get
            {
                return properties.ContainsKey(property) ? properties[property] : null;
            }
        }
    }
    public class NetMsgArgs : ConnectionArgs
    {
        public NetMsgArgs(byte id, string name, byte msgType, object data)
            : base(id, name)
        {
            DataObject = data;
            MessageType = msgType;
        }

        //new way
        public byte MessageType { get; private set; }
        public object DataObject { get; private set; }
    }
    public class NetChatArgs : EventArgs
    {
        public NetChatArgs(byte id, string msg)
        {
            ID = id;
            Message = msg;
        }

        public byte ID { get; private set; }
        public string Message { get; private set; }
    }
    public class NetNameArgs : EventArgs
    {
        public NetNameArgs(byte[] ids, string[] names)
        {
            IDs = ids;
            Names = names;
        }

        public byte[] IDs { get; private set; }
        public string[] Names { get; private set; }
    }
}
