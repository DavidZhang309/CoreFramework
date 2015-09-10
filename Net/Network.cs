using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using CoreFramework.Net.Types;

namespace CoreFramework.Net
{
    public class NetHelper
    {
        public static readonly byte[] EOF = new byte[] { 60, 69, 79, 70, 62 };

        public static void PackageString(string data, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(data.Length / 256);
            buffer[offset + 1] = (byte)(data.Length % 256);
            for (int i = 0; i < data.Length; i++)
                buffer[offset + 2 + i] = (byte)data[i];
        }
        public static string ProcessString(byte[] data, int startIndex)
        {
            string ret = "";
            int length = data[startIndex] * 256 + data[startIndex + 1];
            for (int i = 0; i < length; i++)
                ret += (char)(data[startIndex + 2 + i]);
            return ret;
        }

        public static int PkgStringsCount(byte[] strId, string[] strs)
        {
            int count = 1;
            for (int i = 0; i < strs.Length; i++)
            {
                if (strs[i] != null)
                    count += strs[i].Length + 3;
            }
            return count;
        }
        public static void PackageStrings(byte[] strId, string[] strs, byte[] data, int start)
        {
            byte nameCount = 0;
            int index = start + 1;
            for (int i = 0; i < strs.Length; i++)
            {
                if (strs[i] != null)
                {
                    nameCount++;
                    data[index] = strId[i];
                    PackageString(strs[i], data, index + 1);
                    index += strs[i].Length + 3;
                }
            }
            data[start] = nameCount;
        }
        public static void ProcessStrings(byte[] data, int start, byte[] strId, string[] strs)
        {
            int index = start + 1;
            for (int i = 0; i < data[start]; i++)
            {
                strId[i] = data[index];
                strs[i] = ProcessString(data, index + 1);
                index += strs[i].Length + 3;
            }
        }

        public static void AppendEOF(byte[] data)
        {
            for (int i = 0; i < EOF.Length; i++)
                data[data.Length - EOF.Length + i] = EOF[i];
        }
        public static void AppendEOF(byte[] data, int index)
        {
            for (int i = 0; i < EOF.Length; i++)
                data[index + i] = EOF[i];
        }
        public static bool IsEOF(byte[] data, int index)
        {
            if (data.Length - index < EOF.Length)
                return false;
            for (int i = 0; i < EOF.Length; i++)
            {
                if (i >= data.Length || data[index + i] != NetHelper.EOF[i])
                    return false;
            }
            return true;
        }
    }

    public class BaseNetState
    {
        public string Name { get; set; }
        public byte ID { get; set; }
        public bool Connected { get; set; }
        public bool Initialized { get; set; }

        public override string ToString()
        {
            return "State" + string.Format("[ID: {0}, Name: {1}]", ID, Name);
        }
    }
    
    public struct NetworkID
    {
        public byte ID { get; private set; }
        public string Name { get; private set; }

        public NetworkID(byte id, string name) : this()
        {
            ID = id;
            Name = name;
        }
    }
}
