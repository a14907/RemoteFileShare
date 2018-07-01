using System.IO;
using System.Text;

namespace Model
{
    public class ResponseSectionMessage : MessageBase
    {
        public ResponseSectionMessage() : base(MessageType.ResponseSection)
        {

        }
        public long Start { get; set; }
        public long End { get; set; }
        public int RealLen { get; set; }
        public string FileName { get; set; }
        public byte[] Buffer { get; set; }

        public override byte[] Serialize()
        {
            using (var st = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(st, Encoding.UTF8, false))
            {
                binaryWriter.Write((int)MessageType);
                binaryWriter.Write(Start);
                binaryWriter.Write(End);
                binaryWriter.Write(FileName);
                binaryWriter.Write(Buffer,0,RealLen);
                return st.ToArray();
            }
        }
    }
}
