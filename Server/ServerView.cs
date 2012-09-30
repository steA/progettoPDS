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
using System.Net.NetworkInformation;


namespace Server
{
    public partial class ServerView : Form
    {
        private delegate void updateLogDelegate(TextMessage m);
        private delegate void receivedClipboardDelegate(ClipboardMessage msg);
        private delegate void enableButtonsDelegate();
        private delegate void disableButtonsDelegate();

        private volatile ArrayList clients = ArrayList.Synchronized(new ArrayList());
        private BlockingCollection<TextMessage> msgQueue = new BlockingCollection<TextMessage>();
        private BlockingCollection<ClipboardMessage> clipQueue = new BlockingCollection<ClipboardMessage>();
        private BlockingCollection<ImageMessage> videoQueue = new BlockingCollection<ImageMessage>();
        private int port = 5000;
        private string user = "Server", passw = "password";
        private bool _connect = false, first = true;
        private IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
        private Form settings;
        private Thread _accepterConn;
        private Thread _videoDispatcher;
        
        public VideoManager videoManager;
        public Thread videoThread;
        private  int w_s=0, h_s=0, x_s=0, y_s=0;
        private int captureStyle=1;
       
        private Thread _chatDispatcher;
        private Thread _clipboardDispatcher;
        private TcpListener listen;
        private KeyboardSettings f;
        public Keys kstart = Keys.A, kend = Keys.S;
        private Boolean videoRunning;

        private KeyMessageFilter m_filter;

        public ServerView()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            m_filter = new KeyMessageFilter();
            m_filter.keyPressed += new KeyPressEventHandler(keyPressedCallback);
            Application.AddMessageFilter(m_filter);
            videoManager = new VideoManager(captureStyle,videoQueue);
            InitializeComponent();
            NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler(cableError);
        }

        private void cableError(object sender, NetworkAvailabilityEventArgs e) 
        {
            MessageBox.Show("Cavo di rete disconnesso","Errore",MessageBoxButtons.OK,MessageBoxIcon.Error);
            Disconnect();
        }
        public bool getIsConnected()
        {
            return _connect;
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
                    settings = new ConnectionSettings(this, user, ipAddr.ToString(), passw, port.ToString());
                    settings.ShowDialog();
                    //utente preme annulla
                    if (first == true)
                        return;
                }

                InitializeConnection();
                TextMessage tm = new TextMessage();
                tm.username = user;
                tm.message = "Connesso";
                tm.messageType = MessageType.ADMIN;
                updateLog(tm);
                _connect = true;
                
                videoRunning = false;
                connettiToolStripMenuItem.Text = "Disconnect";
            }
            else
            {
                Disconnect();
                TextMessage t = new TextMessage();
                t.username = user;
                t.message = "si è disconnesso";
                t.messageType = MessageType.DISCONNECT;
                updateLog(t);
                txtMessage.Enabled = false;
                _connect = false;
                btnSend.Enabled = false;
                bntClipboard.Enabled = false;
                buttonStartVideo.Enabled = false;
                connettiToolStripMenuItem.Text = "Connect";
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_connect == true)
            {
                Disconnect();
                TextMessage t = new TextMessage();
                t.username = user;
                t.message = "si è disconnesso";
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
                settings = new ConnectionSettings(this, user, ipAddr.ToString(), passw, port.ToString());
                settings.ShowDialog();
        }

        private void impostazioniToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VideoSettings videoSettings = new VideoSettings(this);
            videoSettings.ShowDialog();
        }

        private void configurazioneTastiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            f = new KeyboardSettings(this);
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
                _videoDispatcher = new Thread(_dispatchVideo);
                _videoDispatcher.IsBackground = true;
                _videoDispatcher.Start();
            }
        }

        private void _dispatchVideo()
        {
            while (_connect)
            {
                try
                {
                    ImageMessage msg = videoQueue.Take();
                    foreach (ClientConnection cli in clients)
                    {
                        cli.sendVideo(msg);
                    }
                }
                catch (ObjectDisposedException)
                {
                    return; 
                }
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
                        if (clients.Count == 0)
                            disableButtons();
                    }
                    //IEnumerator en = clients.GetEnumerator();

                    //while (en.MoveNext())
                    foreach (ClientConnection c in (ArrayList)clients.Clone())
                    {
                        try
                        {
                                c.sendChat(msg);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                TextMessage l = new TextMessage();
                                l.messageType = MessageType.USER_LEAVE;
                                l.username = c.Username;
                                DispatchMsg(l);
                                clients.Remove(c);
                                (c).Disconnect();
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
                    //IEnumerator en = clients.GetEnumerator();
                       // while (en.MoveNext())
                    foreach(ClientConnection c in (ArrayList)clients.Clone())
                    {
                                (c).sendClipboard(msg);
                        }
                        if (msg.username.Equals(user))
                            updateLog(new TextMessage(MessageType.ADMIN, "", "Hai condiviso la clipboard"));
                        else
                        {
                            if (MessageBox.Show(msg.username + " ha condiviso la sua clipboard.\nAccetti di importarla?", "Clipboard", MessageBoxButtons.OKCancel,
                                 MessageBoxIcon.Question) == DialogResult.OK)
                            {
                                receivedClipboard(msg);
                                if (msg.clipboardType == ClipBoardType.TEXT)
                                    updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con del testo"));
                                else if (msg.clipboardType == ClipBoardType.BITMAP)
                                    updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con un'immagine"));
                                else if (msg.clipboardType == ClipBoardType.FILE)
                                    updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con un file"));
                            }
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
                        Clipboard.SetText(msg.text);
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
                        Clipboard.SetImage(msg.bitmap);
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
                        if(clients.Count==1)
                                enableButtons();
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
                killThread(_videoDispatcher);
                killThread(videoThread);
                //reset state
                foreach (ClientConnection client in (ArrayList)clients.Clone())
                {
                    client.Disconnect();
                    clients.Remove(client);
                }
                disableButtons();
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
                    txtLog.AppendText(m.username + ": ");
                    txtLog.SelectionColor = Color.Black;
                    txtLog.AppendText(m.message + "\n");
                }
                else
                {
                    txtLog.SelectionFont = new Font(txtLog.Font, FontStyle.Italic);
                    txtLog.SelectionColor = Color.Red;
                    txtLog.AppendText(m.username + " " + m.message+"\n");
                }

                txtLog.SelectionStart = this.txtLog.Text.Length;
                txtLog.ScrollToCaret();
                txtLog.Refresh();
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
            
            else if (Clipboard.ContainsImage()) //invio immagine
            {
                Image img = Clipboard.GetImage();
                ClipboardMessage cm = new ClipboardMessage(user);
                cm.bitmap = img;
                cm.clipboardType = ClipBoardType.BITMAP;
                DispatchClipboard(cm);
            }
            else if (d.GetDataPresent(DataFormats.FileDrop, true))  //invio file
            {
                try
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
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



        public void keyPressedCallback(object sender, KeyPressEventArgs e)
        {
            if (((Keys)e.KeyChar == kstart))
            {
                startVideoSharing(); 
            }
            else if (((Keys)e.KeyChar == kend))
            {
                stopVideoSharing();
            }
        }

        private void buttonStartVideo_Click(object sender, EventArgs e)
        {
            if (videoRunning)
            {
               
                stopVideoSharing();
               
            }
            else
            {
             
                startVideoSharing();
           
            }
        }


        public void stopVideoSharing() 
        {
            try
            {
                videoThread.Abort();
                buttonStartVideo.Text = "Start Video";
                videoRunning = false;
                TextMessage t = new TextMessage();
                t.messageType = MessageType.VIDEO_STOP;
                DispatchMsg(t);
            }
            catch
            { 
            }
        }

        public void startVideoSharing()
        {
            try
            {
                videoManager.UpdateValuesOfScreen(x_s, y_s, w_s, h_s);
                videoManager.setStyle(captureStyle);
                videoManager.setStart();
                videoThread = new Thread(videoManager.DoWork);
                videoThread.IsBackground = true;
                videoThread.Start();
                //while (!videoThread.IsAlive) ;
                buttonStartVideo.Text = "Stop Video";
                videoRunning = true;
            }
            catch
            {
            }

        }





        private void txtMessage_TextChanged(object sender, EventArgs e)
        {

        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }


        public int getCaptureStyle()
        {
            return captureStyle;
        }

        public void setCaptureStyle(int cs)
        {
            if (cs != captureStyle || cs==3) 
            { 
            captureStyle=cs;
            if (videoRunning) 
            {
                videoThread.Abort();
                videoThread.Join();
                startVideoSharing();
            }

            }
            
        }

        public Rectangle getRect()
        {
            return new Rectangle(x_s, y_s, w_s, h_s);
        }

        public void setScreenAreaDimension(int x, int y, int w, int h)
        {
            x_s = x;
            y_s = y;
            w_s = w;
            h_s = h;
        }

        private void enableButtons() 
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new enableButtonsDelegate(this.enableButtons));
            }
            else
            {
                txtMessage.Enabled = true;
                btnSend.Enabled = true;
                bntClipboard.Enabled = true;
                buttonStartVideo.Enabled = true;
            }
        }
        private void disableButtons()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new disableButtonsDelegate(this.disableButtons));
            }
            else
            {
                txtMessage.Enabled = false;
                btnSend.Enabled = false;
                bntClipboard.Enabled = false;
                buttonStartVideo.Enabled = false;
            }
        }

    }
}
