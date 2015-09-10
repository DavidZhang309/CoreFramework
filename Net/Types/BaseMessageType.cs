namespace CoreFramework.Net.Types
{
    public abstract class BaseMessageType : INetMessageType
    {
        public BaseMessageType(byte msgID)
        {
            MessageType = msgID;
        }
        public BaseMessageType(byte msgID, bool eofMark)
            : this(msgID)
        {
            UseEOFMarkers = eofMark;
        }

        public byte MessageType { get; protected set; }
        public bool UseEOFMarkers { get; set; }

        public abstract int ServerEOFPosition(byte[] data);
        public abstract int ClientEOFPosition(byte[] data);
        public abstract byte[] ServerFormatData(object data);
        public abstract byte[] ClientFormatData(object data);
        public abstract object ServerFormatData(byte[] data);
        public abstract object ClientFormatData(byte[] data);
    }
}
