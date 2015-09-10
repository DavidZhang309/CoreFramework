using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreFramework.Net.Types.Basic
{
    public class DictionaryMessageType : SymmetricMessageType
    {
        public DictionaryMessageType(byte msgType, bool eof) : base(msgType, eof) { }

        public override int EOFPosition(byte[] data)
        {
            throw new NotImplementedException();
        }

        public override object FormatData(byte[] data)
        {
            int index = 2;
            Dictionary<string, string> dict = new Dictionary<string, string>(data[1]);
            for(int i = 0; i < data[1]; i++)
            {
                string key = NetHelper.ProcessString(data, index);
                string value = NetHelper.ProcessString(data, index + key.Length + 2);
                dict.Add(key, value);
                index += key.Length + value.Length + 4;
            }
            return dict;
        }

        public override byte[] FormatData(object data)
        {
            Dictionary<string, string> dict = data as Dictionary<string, string>;
            int count = 2;
            foreach (string key in dict.Keys)
                count += key.Length + dict[key].Length + 4;

            byte[] payload = new byte[count];
            payload[0] = MessageType;
            payload[1] = (byte)dict.Count;
            int index = 2;
            foreach (string key in dict.Keys)
            {
                NetHelper.PackageString(key, payload, index);
                index += key.Length + 2;
                string value = dict[key];
                NetHelper.PackageString(value, payload, index);
                index += value.Length + 2;
            }

            return payload;
        }
    }
}
