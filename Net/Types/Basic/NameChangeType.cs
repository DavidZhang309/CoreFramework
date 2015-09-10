using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework.Net.Types.Basic
{
    public class NameChangeType : SymmetricMessageType
    {
        public NameChangeType(byte msgType, bool eofMark)
            : base(msgType, eofMark)
        {
            MessageType = msgType;
        }

        public override int EOFPosition(byte[] data)
        {
            int count = data[1] * 256 + data[2];
            return NetHelper.IsEOF(data, 3 + count) ? 3 + count : -1;
        }
        public override object FormatData(byte[] data)
        {
            return NetHelper.ProcessString(data, 1);
        }
        public override byte[] FormatData(object data)
        {
            string argument = (string)data;
            byte[] result = new byte[argument.Length + 3 + (UseEOFMarkers ? NetHelper.EOF.Length : 0)];
            result[0] = MessageType;
            NetHelper.PackageString(argument, result, 1);
            if (UseEOFMarkers) NetHelper.AppendEOF(result, result.Length - 5);
            return result;
        }
    }
}
