using System.IO;
using System.Text;

namespace Model
{
    public class RequestSectionMessage : MessageBase
    {
        public RequestSectionMessage() : base(MessageType.RequestSection)
        {

        }
        public long Start { get; set; }
        public long End { get; set; }
        public string FileName { get; set; }

        public override byte[] Serialize()
        {
            using (var st = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(st, Encoding.UTF8, false))
            {
                binaryWriter.Write((int)MessageType);
                binaryWriter.Write(Start);
                binaryWriter.Write(End);
                binaryWriter.Write(FileName);
                return st.ToArray();
            }
        }
    }
}
