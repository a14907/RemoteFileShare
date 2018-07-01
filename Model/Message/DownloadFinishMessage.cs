using System.IO;
using System.Text;

namespace Model
{
    public class DownloadFinishMessage : MessageBase
    {
        public DownloadFinishMessage() : base(MessageType.DownloadFinish)
        {
        }
        public string FileName { get; set; }
        public override byte[] Serialize()
        {
            using (var st = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(st, Encoding.UTF8, false))
            {
                binaryWriter.Write((int)MessageType);
                binaryWriter.Write(FileName);
                return st.ToArray();
            }
        }
    }
}
