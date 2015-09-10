namespace CoreFramework.Net.Types
{
    /// <summary>
    /// Defines an interface that provides asymmetric serialization of objects that can be utilized between server and client
    /// </summary>
    public interface INetMessageType
    {
        /// <summary>
        /// The First Byte of the message
        /// </summary>
        byte MessageType { get; }
        /// <summary>
        /// If EOF Markers would be used to identify end of message
        /// </summary>
        bool UseEOFMarkers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        int ServerEOFPosition(byte[] data);
        int ClientEOFPosition(byte[] data);

        object ServerFormatData(byte[] data);
        byte[] ServerFormatData(object data);
        object ClientFormatData(byte[] data);
        byte[] ClientFormatData(object data);
    }
}
