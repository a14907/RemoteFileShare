using System;
using System.IO;
using System.Text;

namespace Model
{
    public class FileMessage : MessageBase
    {
        public FileMessage() : base(MessageType.FileContent)
        {

        }
        public byte[] Buffer { get; set; }
        /// <summary>
        /// 这个指的是Buffer前几位是有效的,服务端使用
        /// </summary>
        public int Count { get; set; }
        public string FileName { get; set; }

        public override byte[] Serialize()
        {
            using (var st = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(st, Encoding.UTF8, false))
            {
                binaryWriter.Write((int)MessageType);
                binaryWriter.Write(FileName);
                binaryWriter.Write(Buffer, 0, Count);
                return st.ToArray();
            }
        }
    }
}
