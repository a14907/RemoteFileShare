using System.IO;
using System.Text;

namespace Model
{
    public class RequestFileSystemMessage : MessageBase
    {
        public RequestFileSystemMessage() : base(MessageType.RequestFileSystem)
        {
        }
        /// <summary>
        /// 请求的目录
        /// </summary>
        public string Path { get; set; }

        public override byte[] Serialize()
        {
            using (var st = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(st, Encoding.UTF8, false))
            {
                binaryWriter.Write((int)MessageType);
                binaryWriter.Write(Path);
                return st.ToArray();
            }
        }
    }
}
