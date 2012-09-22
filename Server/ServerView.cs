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
        private volatile ArrayList clients = ArrayList.Synchronized(
            new ArrayList());
        private BlockingCollection<TextMessage> msgQueue = new BlockingCollection<TextMessage>();
        private int port;
        private string user, passw;
        private bool _connect = false, first = true;
        private Stream streamSender;
        private Stream streamReceiver;
        private TcpClient clientSocket, clipSocket, videoSocket;
        private IPAddress ipAddr;
        private Form settings;
        private Thread _accepterConn;
        private Thread _videoDispatcher;
        private Thread _chatDispatcher;
        private Thread _clipboardDispatcher;
        private TcpListener listen;
        private IOException _disconnectReason;
        public event pds2.Shared.ConnectionState connectionStateEvent;

        public ServerView()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
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
                    settings = new Settings(this);
                    settings.ShowDialog();
                    //utente preme annulla
                    if (first == true)
                        return;
                }

                InitializeConnection();
                _connect = true;
                txtMessage.Enabled = true;
                btnSend.Enabled = true;
                connettiToolStripMenuItem.Text = "Disconnetti";
                label1.Text = "Connesso";
            }
            else
            {
                TextMessage t = new TextMessage();
                t.username = user;
                t.message = "si e' disconnesso";
                t.messageType = MessageType.DISCONNECT;
                t.sendMe(streamSender);
                t.username = "";
                t.message = "disconnesso";
                updateLog(t);
                Disconnect();
                txtMessage.Enabled = false;
                _connect = false;
                btnSend.Enabled = false;
                connettiToolStripMenuItem.Text = "Connetti";
                label1.Text = "Disconnesso";
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Creato da Stefano Abraham e Enea Bagalini", "About",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Information);
        }

        private void opzioniConnessioneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (first)
            {
                settings = new Settings(this);
                settings.ShowDialog();
            }
            else
            {
                settings = new Settings(this, user, ipAddr.ToString(), passw, port.ToString());
                settings.ShowDialog();
            }
        }

        private void impostazioniToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void configurazioneTastiToolStripMenuItem_Click(object sender, EventArgs e)
        {

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
                /*_clipboardDispatcher = new Thread(_dispatchClipboard);
                _clipboardDispatcher.IsBackground = true;
                _clipboardDispatcher.Start();
                _videoDispatcher = new Thread(_dispatchVideo);
                _videoDispatcher.IsBackground = true;
                _videoDispatcher.Start();*/
                if (connectionStateEvent != null)
                    connectionStateEvent(true);
            }
        }

        private void _dispatchChat()
        {
            while (_connect)
            {
                try
                {
                    TextMessage msg = msgQueue.Take();
                    if (msg.messageType.Equals(MessageType.USER_LEAVE))
                    {
                        foreach (ClientConnection cli in (ArrayList)clients.Clone())
                            if (cli.Username.Equals(msg.username))
                            {
                                clients.Remove(cli);
                            }
                    }
                    foreach (ClientConnection cli in (ArrayList)clients.Clone())
                    {

                        try
                        {
                            cli.sendChat(msg);
                        }
                        catch (Exception)
                        {

                            try
                            {
                                TextMessage l = new TextMessage();
                                l.messageType = MessageType.USER_LEAVE;
                                l.username = cli.Username;
                                DispatchMsg(l);
                                clients.Remove(cli);
                                cli.Disconnect();
                            }
                            catch (Exception) { }
                        }
                    }
                    updateLog(msg);
                }
                catch (ObjectDisposedException)
                {
                    return; //the queue has been closed
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
            catch (ClientConnectionFail ae)
            {
                TextMessage ms = new TextMessage();
                ms.messageType = MessageType.ADMIN;
                ms.message = ae.Message;
                DispatchMsg(ms);
            }
            catch (IOException e)
            {
                _disconnectReason = e;
                return;
            }
            catch (Exception es)
            {
                if (_connect)
                    throw es;
                return;
            }
            finally
            {
                if (listen != null)
                {
                    listen.Stop();
                    listen = null;

                }
                if (_connect)
                    Disconnect();
            }   
        }

        public void Disconnect()
        {
            if (!_connect)
                throw new ArgumentException("The pool is not connected");
            lock (this)
            {
                if (connectionStateEvent != null)
                    connectionStateEvent(false);
                //shutdown all threads
                _connect = false;
                killThread(_accepterConn);
                killThread(_chatDispatcher);
                killThread(_clipboardDispatcher);
                killThread(_chatDispatcher);
                killThread(_videoDispatcher);
                if (listen != null)
                    listen.Stop();
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
                    txtLog.Font = new Font(txtLog.SelectionFont, FontStyle.Regular);
                    txtLog.SelectionColor = Color.Blue;
                    txtLog.AppendText(m.username + " says: ");
                    txtLog.SelectionColor = Color.Black;
                    txtLog.AppendText(m.message + "\r\n\r\n");
                }
                else
                {
                    txtLog.Font = new Font(txtLog.SelectionFont, FontStyle.Italic);
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
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (txtMessage.Text != "" && txtMessage.Text != null)
                {
                    TextMessage t = new TextMessage();
                    t.message = System.Text.RegularExpressions.Regex.Replace(txtMessage.Text, @"^\s*$\n", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
                    t.username = user;
                    t.messageType = MessageType.TEXT;
                    msgQueue.Add(t);
                }
            }

        }
    }
}
