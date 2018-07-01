using System;
using System.Net.Sockets;

namespace Model
{
    public static class SocketExt
    {
        private static int IntLen = sizeof(int);
        public static int ReceiveInt(this Socket socket)
        {
            var buf = new byte[IntLen];
            var len = 0;
            do
            {
                len += socket.Receive(buf, len, IntLen - len, SocketFlags.None);
            } while (len != IntLen);
            return BitConverter.ToInt32(buf, 0);
        }
        public static void ReceiveBuffer(this Socket socket, byte[] buf, int packagelen)
        {
            int readSum = 0;
            do
            {
                var readlen = socket.Receive(buf,readSum,packagelen-readSum, SocketFlags.None);
                readSum += readlen;
            } while (readSum != packagelen);
        }

        public static void EnsureSendBuffer(this Socket socket, byte[] buf)
        {
            int totallen = buf.Length;
            int sendSum = 0;
            do
            {
                sendSum += socket.Send(buf, sendSum, totallen - sendSum, SocketFlags.None);
            } while (sendSum != totallen);
        }

        public static void EnsureSendInt(this Socket socket, int num)
        {
            socket.EnsureSendBuffer(BitConverter.GetBytes(num));
        }

        public static void SendMsg(this Socket socket, MessageBase fileMsg)
        {
            var sendbuf = fileMsg.Serialize();
            Console.WriteLine("发送包长度： {0}",sendbuf.Length);
            Console.WriteLine("fileMsg.MessageType:" + fileMsg.MessageType);
            socket.EnsureSendInt(sendbuf.Length);
            socket.EnsureSendBuffer(sendbuf);
        }

        public static void SendMsg(this Socket socket, string msg)
        {
            var strmsg = new StringMessage() { Content = msg };
            socket.SendMsg(strmsg);
        }

        public static void SendFile(this Socket socket, byte[] buf, int readlen,string filename)
        {
            var fileMsg = new FileMessage() { Buffer = buf, Count = readlen , FileName= filename };
            socket.SendMsg(fileMsg);
        }

    }
}
