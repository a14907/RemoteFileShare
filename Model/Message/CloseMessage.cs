using System.IO;
using System.Text;

namespace Model
{
    public class CloseMessage : MessageBase
    {
        public CloseMessage() : base(MessageType.Close)
        {
        }

        public override byte[] Serialize()
        {
            using (var st = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(st, Encoding.UTF8, false))
            {
                binaryWriter.Write((int)MessageType);
                return st.ToArray();
            }
        }
    }
}
