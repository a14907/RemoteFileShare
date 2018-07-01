using System.Net.Sockets;

namespace Model
{
    public abstract class MessageBase
    {
        protected static int IntLen=sizeof(int);
        public MessageBase(MessageType type)
        {
            MessageType = type;
        }
        public MessageType MessageType { get; protected set; }

        public abstract byte[] Serialize();
    }
}
