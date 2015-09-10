using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework.Net.Types.Basic
{
    public class ChatData
    {
        public byte SenderID { get; set; }
        public byte Flag { get; set; }
        public byte[] Recievers { get; set; }
        public string Message { get; set; }
    }

    public class ChatType : BaseMessageType
    {
        public ChatType(byte msgID, bool eofMark)
            : base(msgID, eofMark)
        { }

        public override int ServerEOFPosition(byte[] data)
        {
            int index = 0;
            if (data.Length >= 4 && data[1] == 0)
                index = data[2] * 256 + data[3] + 4;
            else if (data.Length >= 5 && data[1] == 1)
                index = data[3] * 256 + data[4] + 5;
            else if (data.Length >= 4 && data.Length >= data[3] + 4 && data[1] == 2)
            {
                int recipientLength = data[3];
                index = recipientLength + data[recipientLength + 3] * 256 + data[recipientLength + 4]; //?
            }

            return NetHelper.IsEOF(data, index) ? index : -1;
        }
        public override int ClientEOFPosition(byte[] data)
        {
            if (data.Length >= 3)
            {
                int count = data[1] * 256 + data[2];
                return (data.Length >= count + 3 && NetHelper.IsEOF(data, count + 3)) ? count + 3 : -1;
            }
            return -1;
        }


        public override object ServerFormatData(byte[] data)
        {
            byte[] sentData = (byte[])data;
            ChatData result = new ChatData() { SenderID = 255, Flag = sentData[1] };
            if (sentData[1] == 0 || sentData[1] == 3)
                result.Message = NetHelper.ProcessString(data, 2);
            else if (sentData[1] == 1)
            {
                result.Recievers = new byte[] { sentData[2] };
                result.Message = NetHelper.ProcessString(data, 3);
            }
            else if (sentData[1] == 2)
            {
                result.Recievers = new byte[sentData[2]];
                for (int i = 0; i < result.Recievers.Length; i++)
                    result.Recievers[i] = sentData[3 + i];
                NetHelper.ProcessString(data, result.Recievers.Length + 2);
            }

            return result;
        }
        public override byte[] ServerFormatData(object data)
        {
            string msg = data as string;
            byte[] result = new byte[msg.Length + 3 + (UseEOFMarkers ? NetHelper.EOF.Length : 0)];
            result[0] = MessageType;
            NetHelper.PackageString(msg, result, 1);
            if (UseEOFMarkers) NetHelper.AppendEOF(result, result.Length - 5);
            return result;
        }

        public override byte[] ClientFormatData(object data)
        {
            ChatData rawData = data as ChatData;
            int recipientLength = 1;
            if (rawData.Flag == 1) recipientLength++;
            else if (rawData.Flag == 2) recipientLength += rawData.Recievers.Length + 1;

            byte[] result = new byte[recipientLength + rawData.Message.Length + 3 + (UseEOFMarkers ? NetHelper.EOF.Length : 0)];
            result[0] = MessageType;
            result[1] = rawData.Flag;
            int index = 2;
            if (rawData.Flag == 1)
            {
                result[2] = rawData.Recievers[0];
                index++;
            }
            else if (rawData.Flag == 2)
            {
                result[2] = (byte)rawData.Recievers.Length;
                for (int i = 0; i < rawData.Recievers.Length; i++)
                    result[3 + i] = rawData.Recievers[i];
                index += rawData.Recievers.Length + 1;
            }

            NetHelper.PackageString(rawData.Message, result, index);
            if (UseEOFMarkers) NetHelper.AppendEOF(result, result.Length - 5);
            return result;
        }
        public override object ClientFormatData(byte[] data)
        {
            return NetHelper.ProcessString(data, 1);
        }

    }
}
