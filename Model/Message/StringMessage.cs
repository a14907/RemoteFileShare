using System.IO;
using System.Text;

namespace Model
{
    public class StringMessage : MessageBase
    {
        public StringMessage() : base(MessageType.String)
        {
        }
        public string Content { get; set; }

        public override byte[] Serialize()
        {
            using (var st = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(st, Encoding.UTF8, false))
            {
                binaryWriter.Write((int)MessageType);
                binaryWriter.Write(Content);
                return st.ToArray();
            }
        }
    }
     
}
