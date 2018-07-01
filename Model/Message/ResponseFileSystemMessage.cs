using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Model
{
    public class ResponseFileSystemMessage : MessageBase
    {
        public ResponseFileSystemMessage() : base(MessageType.ResponseFileSystem)
        {
        }
        public List<FileDescription> Files { get; set; }
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
                binaryWriter.Write(Files.ToJson());
                binaryWriter.Write(Path);
                return st.ToArray();
            }
        }
    }
    public class FileDescription
    {
        public string FileName { get; set; }
        public bool IsDirectory { get; set; }
        public long Length { get; set; }
    }
}
