using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Model
{
    public static class BufferExt
    {
        public static MessageBase MessageDeSerialize(this byte[] buf,int count)
        {
            using (var ms=new MemoryStream(buf,0, count))
            using (var br = new BinaryReader(ms,Encoding.UTF8,false))
            {
                var type = br.ReadInt32();
                switch ((MessageType)type)
                {
                    case MessageType.FileContent:
                        var filemsg = new FileMessage();
                        filemsg.FileName = br.ReadString();
                        filemsg.Buffer = br.ReadBytes(count - (int)ms.Position);
                        return filemsg;
                    case MessageType.RequestFile:
                        var rqmsg = new RequestFileMessage();
                        rqmsg.FileName = br.ReadString();
                        return rqmsg;
                    case MessageType.Close:
                        var closemsg = new CloseMessage();
                        return closemsg;
                    case MessageType.RequestFileSystem:
                        var rqfsMsg = new RequestFileSystemMessage();
                        rqfsMsg.Path = br.ReadString();
                        return rqfsMsg;
                    case MessageType.ResponseFileSystem:
                        var resfsMsg = new ResponseFileSystemMessage();
                        resfsMsg.Files = br.ReadString().FromJson<List<FileDescription>>();
                        resfsMsg.Path = br.ReadString();
                        return resfsMsg;
                    case MessageType.DownloadFinish:
                        var finishMsg = new DownloadFinishMessage();
                        finishMsg.FileName = br.ReadString();
                        return finishMsg;
                    case MessageType.RequestSection:
                        var sectionMsg = new RequestSectionMessage();
                        sectionMsg.Start = br.ReadInt64();
                        sectionMsg.End = br.ReadInt64();
                        sectionMsg.FileName = br.ReadString();
                        return sectionMsg;
                    case MessageType.ResponseSection:
                        var resnMsg = new ResponseSectionMessage();
                        resnMsg.Start = br.ReadInt64();
                        resnMsg.End = br.ReadInt64();
                        resnMsg.FileName = br.ReadString();
                        resnMsg.Buffer = br.ReadBytes(count-(int)ms.Position);
                        return resnMsg;
                    default:
                        throw new System.Exception("unknow message type!+"+type);
                }
            }
        }
    }
}
