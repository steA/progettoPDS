using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections;
using pds2.Shared.Messages;
using pds2.Shared;

namespace Server
{
    public partial class ServerView : Form
    {
        private delegate void updateLogDelegate(TextMessage m);
        private delegate void receivedClipboardDelegate(ClipboardMessage msg);

        private volatile ArrayList clients = ArrayList.Synchronized(new ArrayList());
        private BlockingCollection<TextMessage> msgQueue = new BlockingCollection<TextMessage>();
        private BlockingCollection<ClipboardMessage> clipQueue = new BlockingCollection<ClipboardMessage>();
        private int port = 2626;
        private string user = "Server", passw = "password";
        private bool _connect = false, first = true;
        private IPAddress ipAddr = IPAddress.Parse("127.1");
        private Form settings;
        private Thread _accepterConn;
        private Thread _videoDispatcher;
        private Thread _chatDispatcher;
        private Thread _clipboardDispatcher;
        private TcpListener listen;
        private keyboard f;
        public Keys kstart, kend;

        private KeyMessageFilter m_filter;

        public ServerView()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            m_filter = new KeyMessageFilter();
            m_filter.keyPressed += new KeyPressEventHandler(keyPressedCallback);
            Application.AddMessageFilter(m_filter);
            InitializeComponent();
        }

        public void OnApplicationExit(object sender, EventArgs e)
        {
            if (_connect == true)
            {
                Disconnect();
            }
        }

        private void connettiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_connect == false)
            {
                if (first == true)
                {
                    settings = new Settings(this, user, ipAddr.ToString(), passw, port.ToString());
                    settings.ShowDialog();
                    //utente preme annulla
                    if (first == true)
                        return;
                }

                InitializeConnection();
                _connect = true;
                txtMessage.Enabled = true;
                btnSend.Enabled = true;
                bntClipboard.Enabled = true;
                connettiToolStripMenuItem.Text = "Disconnetti";
                label1.Text = "Connesso";
            }
            else
            {
                Disconnect();
                TextMessage t = new TextMessage();
                t.username = user;
                t.message = "si e' disconnesso";
                t.messageType = MessageType.DISCONNECT;
                updateLog(t);
                txtMessage.Enabled = false;
                _connect = false;
                btnSend.Enabled = false;
                bntClipboard.Enabled = false;
                connettiToolStripMenuItem.Text = "Connetti";
                label1.Text = "Disconnesso";
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_connect == true)
            {
                Disconnect();
                TextMessage t = new TextMessage();
                t.username = user;
                t.message = "si e' disconnesso";
                t.messageType = MessageType.DISCONNECT;
                updateLog(t);
            }
            this.Close();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Creato da Stefano Abraham e Enea Bagalini", "About",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Information);
        }

        private void opzioniConnessioneToolStripMenuItem_Click(object sender, EventArgs e)
        {
                settings = new Settings(this, user, ipAddr.ToString(), passw, port.ToString());
                settings.ShowDialog();
        }

        private void impostazioniToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void configurazioneTastiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            f = new keyboard(this);
            f.ShowDialog();
        }

        private void InitializeConnection()
        {
            if (_connect)
                throw new ArgumentException("The pool is already connected");
            lock (this)
            {
                _connect = true;
                _accepterConn = new Thread(receiveConnection);
                _accepterConn.IsBackground = true;
                _accepterConn.Start();
                _chatDispatcher = new Thread(_dispatchChat);
                _chatDispatcher.IsBackground = true;
                _chatDispatcher.Start();
                _clipboardDispatcher = new Thread(_dispatchClipboard);
                _clipboardDispatcher.IsBackground = true;
                _clipboardDispatcher.Start();
                /*_videoDispatcher = new Thread(_dispatchVideo);
                _videoDispatcher.IsBackground = true;
                _videoDispatcher.Start();*/
            }
        }

        private void _dispatchChat()
        {
            try
            {
                while (_connect)
                {
                    TextMessage msg = msgQueue.Take();
                    if (msg.messageType.Equals(MessageType.DISCONNECT))
                    {
                        foreach (ClientConnection cli in (ArrayList)clients.Clone())
                            if (cli.Username.Equals(msg.username))
                            {
                                clients.Remove(cli);
                            }
                    }
                    IEnumerator en = clients.GetEnumerator();
                    while (en.MoveNext())
                    {
                        try
                        {
                                ((ClientConnection)en.Current).sendChat(msg);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                TextMessage l = new TextMessage();
                                l.messageType = MessageType.USER_LEAVE;
                                l.username = ((ClientConnection)en.Current).Username;
                                DispatchMsg(l);
                                clients.Remove(((ClientConnection)en.Current));
                                ((ClientConnection)en.Current).Disconnect();
                            }
                            catch (Exception) { }
                        }
                    }
                    updateLog(msg);
                }
            }
            catch (ObjectDisposedException)
            {
                return; //the queue has been closed
            }
        }

        private void _dispatchClipboard()
        {
            while (_connect)
            {
                try
                {
                    ClipboardMessage msg = clipQueue.Take();
                    IEnumerator en = clients.GetEnumerator();
                        while (en.MoveNext())
                        {
                                ((ClientConnection)en.Current).sendClipboard(msg);
                        }
                        if (msg.username.Equals(user))
                            updateLog(new TextMessage(MessageType.ADMIN, "", "Hai condiviso la clipboard"));
                        else
                        {
                            receivedClipboard(msg);
                            if (msg.clipboardType == ClipBoardType.TEXT)
                                updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con del testo"));
                            else if (msg.clipboardType == ClipBoardType.BITMAP)
                                updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con un'immagine"));
                            else if (msg.clipboardType == ClipBoardType.FILE)
                                updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con un file che e' stato salvato in ./File ricevuti server/"+msg.filename));
                        }
                }
                catch (ObjectDisposedException)
                {
                    return; //the queue has been closed
                }
                
            }
        }

        private void receivedClipboard(ClipboardMessage msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new receivedClipboardDelegate(this.receivedClipboard), new object[] { msg });
            }
            else
            {
                System.Collections.Specialized.StringCollection paths = new System.Collections.Specialized.StringCollection();
                switch (msg.clipboardType)
                {
                    case ClipBoardType.TEXT:
                        string s = msg.text;
                        IDataObject ido = new DataObject();
                        ido.SetData(s);
                        Clipboard.SetDataObject(ido, true);
                        break;
                    case ClipBoardType.FILE:
                        BinaryWriter bWrite;
                        try
                        {
                            bWrite = new BinaryWriter(File.Open(Path.GetFullPath(@".\File ricevuti server\") + msg.filename, FileMode.Create));
                        }
                        catch(DirectoryNotFoundException)
                        {
                            Directory.CreateDirectory(Path.GetFullPath(@".\File ricevuti server\"));
                            bWrite = new BinaryWriter(File.Open(Path.GetFullPath(@".\File ricevuti server\") + msg.filename, FileMode.Create));
                        }
                        bWrite.Write(msg.filedata);
                        bWrite.Close();
                        paths.Add(Path.GetFullPath(@".\File ricevuti server\") + msg.filename);
                        Clipboard.SetFileDropList(paths);
                        break;
                    case ClipBoardType.BITMAP:
                        Bitmap bitm = msg.bitmap;
                        Clipboard.SetImage(bitm);
                        break;
                }
            }
        }

        private void receiveConnection()
        {
            try
            {
                listen = new TcpListener(ipAddr, port);
                listen.Start();
                while (_connect)
                {
                    if (listen != null)
                    {
                        TcpClient ncli = listen.AcceptTcpClient();
                        ArrayList name = new ArrayList();
                        foreach (ClientConnection cli in clients)
                            name.Add(cli.Username);
                        ClientConnection c = new ClientConnection(this, ncli, passw, name);
                        clients.Add(c);
                        TextMessage ms = new TextMessage();
                        ms.messageType = MessageType.USER_JOIN;
                        ms.username = c.Username;
                        ms.message = "nuovo utente connesso";
                        DispatchMsg(ms);
                    }
                }
            }
            catch (ClientConnectionFail ae)
            {
                TextMessage ms = new TextMessage();
                ms.messageType = MessageType.ADMIN;
                ms.message = ae.Message;
                DispatchMsg(ms);
            }
            catch (IOException)
            {
                if (listen != null)
                {
                    listen.Stop();
                    listen = null;

                }
                return;
            }
            catch (Exception)
            {

                if (listen != null)
                {
                    listen.Stop();
                    listen = null;

                }
                return;
            }  
        }

        public void Disconnect()
        {
            if (!_connect)
                throw new ArgumentException("The pool is not connected");
            lock (this)
            {
                //shutdown all threads
                _connect = false;
                if (listen != null)
                    listen.Stop();
                killThread(_accepterConn);
                killThread(_clipboardDispatcher);
                killThread(_chatDispatcher);
                /*killThread(_videoDispatcher);*/
                //reset state
                foreach (ClientConnection client in (ArrayList)clients.Clone())
                {
                    client.Disconnect();
                    clients.Remove(client);
                }
            }
        }

        private void killThread(Thread t)
        {
            try
            {
                t.Abort();
            }
            catch (Exception) { }
        }
        
        private void updateLog(TextMessage m)
        {
            if (this.txtLog.InvokeRequired)
            {
                this.Invoke(new updateLogDelegate(this.updateLog), new object[] { m });
            }
            else
            {
                if (m.messageType == MessageType.TEXT)
                {
                    txtLog.SelectionFont = new Font(txtLog.Font, FontStyle.Regular);
                    txtLog.SelectionColor = Color.Blue;
                    txtLog.AppendText(m.username + " says: ");
                    txtLog.SelectionColor = Color.Black;
                    txtLog.AppendText(m.message + "\r\n\r\n");
                }
                else
                {
                    txtLog.SelectionFont = new Font(txtLog.Font, FontStyle.Italic);
                    txtLog.SelectionColor = Color.Red;
                    txtLog.AppendText(m.username + " " + m.message+"\n");
                }
            }
        }

        public void DispatchMsg(TextMessage msg)
        {
            msgQueue.Add(msg);
        }

        public void DispatchClipboard(ClipboardMessage msg)
        {
            clipQueue.Add(msg);
        }

        public void setFlag()
        {
            first = false;
        }

        public void setUser(string usr)
        {
            user = usr;
        }

        public void setPw(string pw)
        {
            passw = pw;
        }

        public void setIp(IPAddress ip)
        {
            ipAddr = ip;
        }

        public void setPort(int p)
        {
            port = p;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text != "" && txtMessage.Text != null)
            {
                TextMessage t = new TextMessage();
                t.message = System.Text.RegularExpressions.Regex.Replace(txtMessage.Text, @"^\s*$\n", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
                t.username = user;
                t.messageType = MessageType.TEXT;
                msgQueue.Add(t);
            }
            txtMessage.Clear();
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                String s = System.Text.RegularExpressions.Regex.Replace(txtMessage.Text, @"^\s*$\n", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
                if (s != "" && s != null)
                {
                    TextMessage t = new TextMessage();
                    t.message = s;
                    t.username = user;
                    t.messageType = MessageType.TEXT;
                    msgQueue.Add(t);
                }
                txtMessage.Clear();
            }
        }

        private void bntClipboard_Click(object sender, EventArgs e)
        {
            string strclip;
            IDataObject d = Clipboard.GetDataObject();
            if (d.GetDataPresent(DataFormats.Text))  //invio testo
            {
                strclip = (string)d.GetData(DataFormats.Text);
                ClipboardMessage cm = new ClipboardMessage(user);
                cm.text = strclip;
                cm.clipboardType = ClipBoardType.TEXT;
                DispatchClipboard(cm);
            }
            else if (d.GetDataPresent(DataFormats.FileDrop, true))  //invio file
            {
                object fromClipboard = d.GetData(DataFormats.FileDrop);
                foreach (string sourceFileName in (Array)fromClipboard)
                {
                    ClipboardMessage cm = new ClipboardMessage(user);
                    cm.clipboardType = ClipBoardType.FILE;
                    cm.filename = Path.GetFileName(sourceFileName);
                    cm.filedata = File.ReadAllBytes(sourceFileName);
                    DispatchClipboard(cm);
                }
            }
            else if (Clipboard.ContainsImage()) //invio immagine
            {
                Bitmap img = (Bitmap)Clipboard.GetImage();
                ClipboardMessage cm = new ClipboardMessage(user);
                cm.bitmap = img;
                cm.clipboardType = ClipBoardType.BITMAP;
                DispatchClipboard(cm);
            }
        }

        public void keyPressedCallback(object sender, EventArgs e)
        {
        }
    }
}
