using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Model;
using System.Linq;
using System.IO;

namespace Client
{
    public class Client : IDisposable
    {
        private IPEndPoint _ipEndPoint;
        private IFrontFunc _frontFunc;
        private Socket _socket;
        private bool _isStop = false;
        public Dictionary<Tuple<string, string>, Queue<byte[]>> _fileDataDic = new Dictionary<Tuple<string, string>, Queue<byte[]>>();
        public Dictionary<Tuple<string, string>, Tuple<bool, bool>> _isfinishDic = new Dictionary<Tuple<string, string>, Tuple<bool, bool>>();

        public Client(string ip, int port, IFrontFunc frontFunc)
        {
            _ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _frontFunc = frontFunc;
        }

        internal void Start()
        {
            _isStop = false;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(_ipEndPoint);

            _frontFunc.ShowLog("连接成功");
            _frontFunc.ShowLog("本地IPEndPoint:" + _socket.LocalEndPoint);
            Thread thread = new Thread(HandlerSocket);
            thread.IsBackground = true;
            thread.Start();

        }


        private void HandlerFileSave(object obj)
        {
            var key = obj as Tuple<string, string>;
            var filesave = key.Item2;
            var stream = new FileStream(filesave, FileMode.Append);
            while (!_isfinishDic[key].Item1)
            {
                byte[] buf = null;
                try
                {
                    buf = _fileDataDic[key].Dequeue();
                    stream.Write(buf, 0, buf.Length);

                    stream.Flush();
                    if (_isfinishDic[key].Item2 == true && _fileDataDic[key].Count == 0)
                    {
                        _isfinishDic[key] = new Tuple<bool, bool>(true, true);
                        _frontFunc.DownloadFinish(key.Item1);
                    }
                }
                catch (Exception)
                { }
            }
            stream.Dispose();

            _fileDataDic.Remove(key);
            _isfinishDic.Remove(key);
        }

        private void HandlerSocket()
        {
            try
            {
                while (!_isStop)
                {
                    int packageLen = 0;
                    try
                    {
                        packageLen = _socket.ReceiveInt();
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    var buf = new byte[packageLen];
                    _frontFunc.ShowLog("接受包长度：" + packageLen);
                    _socket.ReceiveBuffer(buf, packageLen);
                    var msgbase = buf.MessageDeSerialize(packageLen);
                    switch (msgbase.MessageType)
                    {
                        case MessageType.ResponseFileSystem:
                            var resFsMsg = msgbase as ResponseFileSystemMessage;
                            _frontFunc.ShowLog("文件系统信息接受成功：" + resFsMsg.Files.Count);
                            _frontFunc.ShowFileSystem(resFsMsg.Files, resFsMsg.Path);
                            break;
                        case MessageType.FileContent:
                            var fileMsg = msgbase as FileMessage;
                            _frontFunc.ShowLog("文件接受" + fileMsg.Buffer.Length + ":" + fileMsg.FileName);
                            var key = _fileDataDic.Keys.First(m => m.Item1 == fileMsg.FileName);
                            _fileDataDic[key].Enqueue(fileMsg.Buffer);
                            if (!_isfinishDic.ContainsKey(key))
                            {
                                //创建消费线程
                                _isfinishDic.Add(key, new Tuple<bool, bool>(false, false));
                                Thread thread = new Thread(HandlerFileSave);
                                thread.IsBackground = true;
                                thread.Start(key);
                            }
                            //向前端报告进度
                            _frontFunc.AddDownloadProgress(fileMsg.Buffer.Length, fileMsg.FileName);
                            break;
                        case MessageType.ResponseSection:

                            var sectionMsg = msgbase as ResponseSectionMessage;
                            _frontFunc.ShowLog(DateTime.Now+"文件接受" + sectionMsg.Buffer.Length + ":" + sectionMsg.FileName);
                            var sectionkey = _fileDataDic.Keys.First(m => m.Item1 == sectionMsg.FileName);
                            _fileDataDic[sectionkey].Enqueue(sectionMsg.Buffer);
                            if (!_isfinishDic.ContainsKey(sectionkey))
                            {
                                //创建消费线程
                                _isfinishDic.Add(sectionkey, new Tuple<bool, bool>(false, false));
                                Thread thread = new Thread(HandlerFileSave);
                                thread.IsBackground = true;
                                thread.Start(sectionkey);
                            }
                            //向前端报告进度
                            _frontFunc.AddDownloadProgress(sectionMsg.Buffer.Length, sectionMsg.FileName);
                            if (sectionMsg.Buffer.Length<(sectionMsg.End- sectionMsg.Start))
                            {
                                break;
                            }
                            var req = new RequestSectionMessage()
                            {
                                FileName = sectionMsg.FileName,
                                Start = sectionMsg.End,
                                End = sectionMsg.End+ 1024 * 1024
                            };
                            _socket.SendMsg(req);

                            break;
                        case MessageType.String:
                            _frontFunc.ShowLog("接受字符串信息");
                            var strMsg = msgbase as StringMessage;
                            _frontFunc.ShowMsg(strMsg.Content);
                            break;
                        case MessageType.DownloadFinish:
                            _frontFunc.ShowLog("服务端传输完毕");
                            var finishMsg = msgbase as DownloadFinishMessage;
                            var keyFinish = _fileDataDic.Keys.First(m => m.Item1 == finishMsg.FileName);
                            _isfinishDic[keyFinish] = new Tuple<bool, bool>(false, true);
                            break;
                        case MessageType.Close:
                            _frontFunc.ShowLog("连接关闭");
                            _frontFunc.Shutdown();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _frontFunc.ShowLog(ex.ToString());
                _frontFunc.Shutdown();
            }
        }

        internal void RequestFileSystem(string path)
        {
            _frontFunc.ShowLog("请求文件系统");
            RequestFileSystemMessage requestFileSystemMessage = new RequestFileSystemMessage()
            {
                Path = path
            };
            _socket.SendMsg(requestFileSystemMessage);
        }

        internal void RequestSystem(string fileName, string saveFileName)
        {
            var key = new Tuple<string, string>(fileName, saveFileName);
            if (_fileDataDic.ContainsKey(key))
            {
                _frontFunc.ShowMsg("已经在下载");
                return;
            }
            long startIndex = 0;
            if (File.Exists(saveFileName))
            {
                using (var s = File.Open(saveFileName, FileMode.Open))
                {
                    startIndex = s.Length;
                }
            }
            var req = new RequestSectionMessage()
            {
                FileName = fileName,
                Start = startIndex,
                End = startIndex+1024 * 1024
            };
            _socket.SendMsg(req);
            _fileDataDic.Add(key, new Queue<byte[]>());
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

                if (_socket != null)
                {
                    try
                    {
                        _socket.SendMsg(new CloseMessage());
                        Thread.Sleep(1000);
                        _socket.Dispose();
                    }
                    catch (Exception)
                    { 
                    }
                    _socket = null;
                    _isStop = true;
                }

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~Client() {
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

    public interface IFrontFunc
    {
        void ShowMsg(string msg);
        void ShowFileSystem(List<FileDescription> ls, string path);
        void AddDownloadProgress(int len, string filename);
        void DownloadFinish(string fileName);
        void ShowLog(string msg);
        void Shutdown();
    }
}
