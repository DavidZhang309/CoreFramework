namespace CoreFramework.Net.Types
{
    public abstract class SymmetricMessageType : BaseMessageType
    {
        public SymmetricMessageType(byte msgID) : base(msgID) { }
        public SymmetricMessageType(byte msgID, bool eofMark) : base(msgID, eofMark) { }

        public abstract int EOFPosition(byte[] data);
        public override int ServerEOFPosition(byte[] data)
        {
            return EOFPosition(data);
        }
        public override int ClientEOFPosition(byte[] data)
        {
            return EOFPosition(data);
        }

        public abstract object FormatData(byte[] data);
        public abstract byte[] FormatData(object data);
        public override object ServerFormatData(byte[] data)
        {
            return FormatData(data);
        }
        public override object ClientFormatData(byte[] data)
        {
            return FormatData(data);
        }
        public override byte[] ServerFormatData(object data)
        {
            return FormatData(data);
        }
        public override byte[] ClientFormatData(object data)
        {
            return FormatData(data);
        }
    }
}
