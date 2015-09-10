using System;
using System.Collections.Generic;
using System.Threading;
using CoreFramework.Net;
using CoreFramework.Net.Events;
using CoreFramework.Net.Types;

namespace CoreFramework.Net
{
    /// <summary>
    /// Abstract event-based network sessions for server and clients
    /// </summary>
    public abstract class CommonSession : INetEventSession
    {
        protected INetMessageType[] msgTypes;
        private AutoResetEvent connectWaiter;
        private AutoResetEvent msgWaiter;

        public event EventHandler<NetMsgArgs> MessageReceived;
        public event EventHandler<ConnectionArgs> OnConnect;
        public event EventHandler<ConnectionArgs> OnDisconnect;

        public CommonSession()
        {
            msgTypes = new INetMessageType[256];
            msgWaiter = new AutoResetEvent(false);
            connectWaiter = new AutoResetEvent(false);
        }

        /// <summary>
        /// Handles internal messages, and returns whether the message got handled
        /// </summary>
        /// <param name="state">Remote state that sent it</param>
        /// <param name="msgType">Message Type ID</param>
        /// <param name="payload">Formatted data from payload</param>
        /// <returns>True if message was handled, false if not</returns>
        protected abstract bool OnRecv(BaseNetState state, byte msgType, object data);
        /// <summary>
        /// Raises the OnConnect event and sets WaitForConnection blocker
        /// </summary>
        /// <param name="args">OnConnect event arguments</param>
        protected void RaiseOnConnect(ConnectionArgs args)
        {
            if (OnConnect != null) OnConnect(this, args);
            connectWaiter.Set();
        }
        /// <summary>
        /// Raises the OnDisconnect event
        /// </summary>
        /// <param name="args">OnDisconnect event arguments</param>
        protected void RaiseOnDisconnect(ConnectionArgs args)
        {
            if (OnDisconnect != null) OnDisconnect(this, args);
        }
        protected void RaiseMessageReceived(NetMsgArgs args)
        {
            if (MessageReceived != null) MessageReceived(this, args);
            msgWaiter.Set();
        }

        /// <summary>
        /// Blocks the calling thread until after the message is processed
        /// </summary>
        public void WaitForNextMessage()
        {
            msgWaiter.WaitOne();
        }
        /// <summary>
        /// Blocks calling thread until a connection is established
        /// </summary>
        public void WaitForConnection()
        {
            connectWaiter.WaitOne();
        }
        /// <summary>
        /// Blocks the calling thread until after the message is processed or the timeout is reached
        /// </summary>
        /// <param name="msTimeout">Timeout period in milliseconds</param>
        public void WaitForNextMessage(int msTimeout)
        {
            msgWaiter.WaitOne(msTimeout);
        }

        /// <summary>
        /// Note:
        ///     ID 0-10 will mostly be populated internally
        /// Exceptions:
        ///     InvalidOperationException
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="type"></param>
        public void AddMessageType(INetMessageType type)
        {
            if (msgTypes[type.MessageType] != null) throw new InvalidOperationException("ID " + type.MessageType + " is being used by " + msgTypes[type.MessageType].ToString());
            msgTypes[type.MessageType] = type;
        }
    }
}
