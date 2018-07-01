using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Model;
using System.Linq;

namespace Server
{
    public class Server : IDisposable
    {
        private IConfigurationRoot config;
        private int _port;
        private int _socketListenCount;
        private Socket _server;
        private bool _isStop = false;
        private string _baseDir;
        private IFileProvider _fileProvider;

        public Server(IConfigurationRoot config)
        {
            this.config = config;
            _port = int.Parse(config.GetSection("BindPort").Value);
            _socketListenCount = int.Parse(config.GetSection("SocketListenCount").Value);
            _baseDir = config.GetSection("BaseDir").Value;
            _fileProvider = new PhysicalFileProvider(_baseDir);
        }

        public void Start()
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.Bind(new IPEndPoint(IPAddress.Any, _port));
            _server.Listen(_socketListenCount);

            Thread thread = new Thread(StartAccept);
            thread.IsBackground = true;
            thread.Start();
        }

        private void StartAccept()
        {
            while (!_isStop)
            {
                var clientSocket = _server.Accept();
                Console.WriteLine("接收到客户端连接");
                Console.WriteLine("客户端IPEndPoint:" + clientSocket.RemoteEndPoint);
                Thread thread = new Thread(HandlerClientSocket);
                thread.IsBackground = true;
                thread.Start(clientSocket);
            }
        }

        private void HandlerClientSocket(object obj)
        {
            var clientSocket = obj as Socket;
            while (!_isStop)
            {
                int packagelen = 0;
                try
                {
                    packagelen = clientSocket.ReceiveInt();
                }
                catch (Exception)
                {
                    Console.WriteLine("客户端关闭");
                    clientSocket.Close();
                    clientSocket.Dispose();
                    return;
                }
                var buf = new byte[packagelen];
                try
                {
                    clientSocket.ReceiveBuffer(buf, packagelen);
                }
                catch (Exception)
                {
                    Console.WriteLine("客户端关闭");
                    clientSocket.Close();
                    clientSocket.Dispose();
                    return;
                }
                var msgbase = buf.MessageDeSerialize(packagelen);
                switch (msgbase.MessageType)
                {
                    case MessageType.RequestFileSystem:
                        Console.WriteLine("客户端请求文件系统信息");
                        HandlerRequestFileSystemMessage(clientSocket, msgbase as RequestFileSystemMessage);
                        break;
                    case MessageType.RequestFile:
                        Console.WriteLine("客户端请求文件");
                        HandlerRequestFileMessage(clientSocket, msgbase as RequestFileMessage);
                        break;
                    case MessageType.Close:
                        Console.WriteLine("客户的关闭");
                        clientSocket.Close();
                        clientSocket.Dispose();
                        return;
                    case MessageType.RequestSection:
                        Console.WriteLine("下载文件片段");
                        HandlerDownloadSectionMessage(clientSocket, msgbase as RequestSectionMessage);
                        break;
                }
            }
            clientSocket.Close();
            clientSocket.Dispose();
        }

        private void HandlerDownloadSectionMessage(Socket clientSocket, RequestSectionMessage downloadSectionMessage)
        {
            var filename = downloadSectionMessage.FileName;
            var fileinfo = _fileProvider.GetFileInfo(filename);
            if (fileinfo.IsDirectory)
            {
                clientSocket.SendMsg("请求的不是文件");
                return;
            }
            var length = downloadSectionMessage.End - downloadSectionMessage.Start;
            var buf = new byte[length];
            using (var stream = fileinfo.CreateReadStream())
            {
                stream.Position = downloadSectionMessage.Start;
                int readsum = 0;
                do
                {
                    readsum += stream.Read(buf, 0, buf.Length);
                } while (readsum != length && stream.Length!=stream.Position);
                Console.WriteLine("发送文件片段section ");
                clientSocket.SendMsg(new ResponseSectionMessage()
                {
                    Buffer = buf,
                    RealLen = readsum,
                    End = downloadSectionMessage.End,
                    Start = downloadSectionMessage.Start,
                    FileName = downloadSectionMessage.FileName
                });
                if (stream.Length == stream.Position)
                {
                    Console.WriteLine("文件发送完成");
                    clientSocket.SendMsg(new DownloadFinishMessage() { FileName = filename });
                }
            }
           
        }

        private void HandlerRequestFileSystemMessage(Socket clientSocket, RequestFileSystemMessage requestFileSystemMessage)
        {
            var dir = _fileProvider.GetDirectoryContents(requestFileSystemMessage.Path);
            var resMsg = new ResponseFileSystemMessage();
            resMsg.Files = dir.Select(m => new FileDescription
            {
                FileName = m.Name,
                IsDirectory = m.IsDirectory,
                Length = (int)m.Length
            }).ToList();
            resMsg.Path = requestFileSystemMessage.Path;
            Console.WriteLine("文件系统信息发送");
            clientSocket.SendMsg(resMsg);
        }
        private void HandlerRequestFileMessage(Socket clientSocket, RequestFileMessage requestFileMessage)
        {
            var filename = requestFileMessage.FileName;
            var fileinfo = _fileProvider.GetFileInfo(filename);
            if (fileinfo.IsDirectory)
            {
                clientSocket.SendMsg("请求的不是文件");
                return;
            }

            var buf = new byte[1024 * 1024];
            using (var stream = fileinfo.CreateReadStream())
            {
                int readlen = 0;
                while ((readlen = stream.Read(buf, 0, buf.Length)) != 0)
                {
                    Console.WriteLine("发送文件片段 ");
                    clientSocket.SendFile(buf, readlen, filename);
                }
            }
            Console.WriteLine("文件发送完成");
            clientSocket.SendMsg(new DownloadFinishMessage() { FileName = filename });
        }















        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。
                if (_server != null)
                {
                    _server.Dispose();
                    _server = null;
                    _isStop = true;
                }
                if (_fileProvider != null)
                {
                    (_fileProvider as PhysicalFileProvider)?.Dispose();
                    _fileProvider = null;
                }
                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~Server() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }


}