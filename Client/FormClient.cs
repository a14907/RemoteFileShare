using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client.Properties;
using Model;

namespace Client
{
    public partial class FormClient : Form, IFrontFunc
    {
        private Client _client;
        private bool _downloadFinish = true;
        private string _fileSaveName = "";
        private FolderBrowserDialog _folderBrowserDialog;
        private SynchronizationContext _synchronizationContext;
        private Dictionary<string, int> _progressDic = new Dictionary<string, int>();
        private List<ListViewItem> _listViewItems = new List<ListViewItem>();

        public FormClient()
        {
            InitializeComponent();
            _folderBrowserDialog = new FolderBrowserDialog();
            _synchronizationContext = SynchronizationContext.Current;
            BindSource();
            timer1.Start();
        }

        private void BindSource()
        {
            var bs = new BindingSource()
            {
                DataSource = _listViewItems
            };
            lbProgress.DataSource = bs;
            lbProgress.DisplayMember = "Text";
            lbProgress.ValueMember = "Tag";
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            _client = new Client(tbIP.Text.Trim(), int.Parse(tbPort.Text.Trim()), this);
            _client.Start();
            btnConnect.Enabled = false;
            btnClose.Enabled = true;

            //请求获取文件系统 /
            _client.RequestFileSystem("/");
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }

            btnConnect.Enabled = true;
            btnClose.Enabled = false;
        }

        public void ShowMsg(string msg)
        {
            MessageBox.Show(msg);
        }

        public void ShowFileSystem(List<FileDescription> ls, string path)
        {
            _synchronizationContext.Post(data =>
            {
                var d = data as Tuple<List<FileDescription>, string>;
                if (d.Item2 == "/")
                {
                    tvFileSystem.Nodes.Clear();
                    AppendNode(tvFileSystem.Nodes, d.Item1);
                }
                else
                {
                    TreeNode parentnode = FindParentNode(tvFileSystem.Nodes, d.Item2);
                    parentnode.Nodes.Clear();
                    AppendNode(parentnode.Nodes, d.Item1);
                }
            }, new Tuple<List<FileDescription>, string>(ls, path));
        }

        private void AppendNode(TreeNodeCollection nodes, List<FileDescription> ls)
        {
            if (nodes.Count != 0)
            {
                return;
            }
            foreach (var item in ls)
            {
                var node = new TreeNode(item.FileName);
                node.Tag = item;
                if (item.IsDirectory)
                {

                }
                nodes.Add(node);
            }
        }

        private TreeNode FindParentNode(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode item in nodes)
            {
                if (item.FullPath == path)
                {
                    return item;
                }
                else if (item.Nodes.Count != 0)
                {
                    var pnode = FindParentNode(item.Nodes, path);
                    if (pnode != null)
                    {
                        return pnode;
                    }
                }
            }
            return null;
        }

        public void AddDownloadProgress(int len, string filename)
        { 
            _synchronizationContext.Post(d =>
            {
                var data = d as Tuple<int, string>;
                foreach (ListViewItem item in _listViewItems)
                {
                    var tag = item.Tag as Tuple<string, long>;
                     
                    if (tag.Item1 == Path.GetFileName(data.Item2))
                    {
                        var rawstr = item.Text;
                        var index = rawstr.IndexOf("/");
                        item.Tag = new Tuple<string, long>(tag.Item1, tag.Item2 + data.Item1) ; 
                        item.Text = rawstr.Substring(0, index + 1) + (tag.Item2 + data.Item1);
                        BindSource();
                        return;
                    }
                }
            }, new Tuple<int, string>(len, filename));
        }

        public void SetTotalProgress(long total, string filename)
        {
            _synchronizationContext.Post(d =>
            {
                var data = d as Tuple<long, string>;
                var item = new ListViewItem(data.Item2 + ":" + data.Item1 + "/0")
                {
                    Tag = new Tuple<string, long>(data.Item2, 0)
                };
                _listViewItems.Add(item);
                BindSource();
            }, new Tuple<long, string>(total, filename));
        }



        private void tvFileSystem_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var fileInfo = (e.Node.Tag as FileDescription);
            if (fileInfo.IsDirectory)
            {
                _client.RequestFileSystem(e.Node.FullPath);
            }
            else
            {               
                //确定文件下载位置
                var res = _folderBrowserDialog.ShowDialog();
                if (res != DialogResult.OK)
                {
                    return;
                }
                _fileSaveName = Path.Combine(_folderBrowserDialog.SelectedPath, e.Node.Text);
                //需要减产文件是否存在，如果存在并且文件长度就是下载完成的长度则提示不需要再下载
                if (File.Exists(_fileSaveName))
                {
                    using (var fs=File.Open(_fileSaveName, FileMode.Open))
                    {
                        if (fileInfo.Length==fs.Length)
                        {
                            MessageBox.Show("该文件已经下载完成，无需下载");
                            return;
                        }
                    }
                }
                //请求文件
                _client.RequestSystem(e.Node.FullPath, _fileSaveName);
                _downloadFinish = false;
                //设置进度
                SetTotalProgress(fileInfo.Length, fileInfo.FileName);
            }          
        }

        public void DownloadFinish(string filename)
        {
            _synchronizationContext.Post(d =>
            {
                _downloadFinish = true;
                MessageBox.Show(d + "下载结束");
            }, filename);

        }

        private void FormClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        private Queue<string> _queueLog = new Queue<string>();

        public void ShowLog(string msg)
        {
            _synchronizationContext.Post(d => _queueLog.Enqueue(d + "\r\n"), msg);
        }

        public void Shutdown()
        {
            _synchronizationContext.Post(d =>
            {
                if (_client != null)
                {
                    _client.Dispose();
                    _client = null;
                }

                btnConnect.Enabled = true;
                btnClose.Enabled = false;
            }, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var ss=_client._fileDataDic;
            var dd = _client._isfinishDic;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            while (_queueLog.Count != 0)
            {
                tbLog.AppendText(_queueLog.Dequeue());
            }
        }
    }
}
