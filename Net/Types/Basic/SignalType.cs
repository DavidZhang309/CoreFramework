using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework.Net.Types.Basic
{
    public class SignalType : SymmetricMessageType
    {
        public SignalType(byte messageType, bool eofMark)
            : base(messageType, eofMark)
        { }

        public override int EOFPosition(byte[] data)
        {
            return NetHelper.IsEOF(data, 2) ? 2 : -1;
        }

        /// <summary>
        /// Takes in a byte value as signal and formats and package into network message
        /// </summary>
        /// <param name="data">The byte signal</param>
        /// <returns>Formatted network message</returns>
        public override byte[] FormatData(object data)
        {
            byte signal = (byte)data;
            byte[] formatted = new byte[2 + (UseEOFMarkers ? NetHelper.EOF.Length : 0)];
            formatted[0] = MessageType;
            formatted[1] = signal;
            if (UseEOFMarkers) NetHelper.AppendEOF(formatted, 2);
            return formatted;
        }
        /// <summary>
        /// Returns the byte signal in the network message
        /// </summary>
        /// <param name="data">Network message</param>
        /// <returns>The byte signal</returns>
        public override object FormatData(byte[] data)
        {
            return data[1];
        }
    }
}
