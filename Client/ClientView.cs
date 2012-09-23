using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using pds2.Shared.Messages;
using pds2.Shared;

namespace Client
{
    public partial class ClientView : Form
    {
        private delegate void updateLogDelegate(TextMessage m);
        private delegate void receivedClipboardDelegate(ClipboardMessage msg);
        
        private int port=2626;
        private string user="", passw="password";
        private bool isConnected = false, first = true;
        private TcpClient clientSocket, clipSocket, videoSocket;
        private IPAddress ipAddr=IPAddress.Parse("127.1");
        private Form settings;
        private BlockingCollection<TextMessage> msgToSend = new BlockingCollection<TextMessage>();
        private BlockingCollection<ClipboardMessage> clipQueue = new BlockingCollection<ClipboardMessage>();
        private Thread senderThread, receiverThread, listenClipboardThread, deliverClipboardThread, videoThread;

        public ClientView()
        {
            /** On application exit, don't forget to disconnect */
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            InitializeComponent();
        }

        // The event handler for application exit
        public void OnApplicationExit(object sender, EventArgs e)
        {
            if (isConnected == true)
            {
                TextMessage t = new TextMessage();
                t.username = user;
                t.message = " si e' disconnesso";
                t.messageType = MessageType.DISCONNECT;
                msgToSend.Add(t);
                closeConnection();
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
                settings = new Settings(this, user, ipAddr.ToString(), passw, port.ToString());
                settings.ShowDialog();
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isConnected == false)
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
                isConnected = true;
                messageText.Enabled = true;
                sendButton.Enabled = true;
                connectToolStripMenuItem.Text = "Disconnetti";
            }
            else
            {
                TextMessage t = new TextMessage();
                t.username = user;
                t.message = "si e' disconnesso";
                t.messageType = MessageType.DISCONNECT;
                msgToSend.Add(t);
                closeConnection();
                messageText.Enabled = false;
                isConnected = false;
                sendButton.Enabled = false;
                connectToolStripMenuItem.Text = "Connetti";
            }

        }

        private void esciToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isConnected == true)
            {
                TextMessage t = new TextMessage();
                t.username = user;
                t.message = " si e' disconnesso";
                t.messageType = MessageType.DISCONNECT;
                msgToSend.Add(t);
                closeConnection();
            }
            this.Close();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            if (messageText.Text != "" && messageText.Text != null)
            {
                TextMessage t = new TextMessage();
                t.message = System.Text.RegularExpressions.Regex.Replace(messageText.Text, @"^\s*$\n", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
                t.username = user;
                t.messageType = MessageType.TEXT;
                msgToSend.Add(t);
            }
            messageText.Clear();
        }

        private void messageText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                String s = System.Text.RegularExpressions.Regex.Replace(messageText.Text, @"^\s*$\n", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
                if (s != "" && s != null)
                {
                    TextMessage t = new TextMessage();
                    t.message = s;
                    t.username = user;
                    t.messageType = MessageType.TEXT;
                    msgToSend.Add(t);
                }
                messageText.Clear();
            }
        }

        private void shareClipboardToolStripMenuItem_Click(object sender, EventArgs e)
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

        public void DispatchClipboard(ClipboardMessage msg)
        {
            clipQueue.Add(msg);
        }
        /*private void sendClipFile(object data)
        {
            int i = 0, qta = 0;
            NetworkStream ns = clipSocket.GetStream();
            IDataObject d = (IDataObject)data;
            this.Invoke(new toolStripMenuDelegate(toolStripMenu), new object[]{"false"});
            object fromClipboard = d.GetData(DataFormats.FileDrop, true);
            ClipboardMessage cm = new ClipboardMessage(user);
            foreach (string sourceFileName in (Array)fromClipboard)
                qta++;
            cm.filename = new string[qta];
            cm.filedata = new byte[qta][];
            foreach (string sourceFileName in (Array)fromClipboard)
            {
                try
                {
                    FileInfo fleMembers = new FileInfo(sourceFileName);
                    float size = (float)(fleMembers.Length / 1024 / 1024);
                    if (size > 50)
                    {
                        System.Windows.Forms.MessageBox.Show("Impossibile inviare " + sourceFileName + " perché il file è troppo grosso", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                    cm.clipboardType = ClipBoardType.FILE;
                    cm.filename[i] = Path.GetFileName(sourceFileName);
                    cm.filedata[i] = File.ReadAllBytes(sourceFileName);
                    i++;
                }
                catch(Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Invoke(new toolStripMenuDelegate(toolStripMenu), new object[]{"true"});
                    return;
                }
            }
            cm.sendMe(ns);
            this.Invoke(new toolStripMenuDelegate(toolStripMenu), new object[]{"true"});
        }*/

        /*private void toolStripMenu(object f)
        {
            shareClipboardToolStripMenuItem.Enabled = (bool)f;
        }*/

        private void InitializeConnection()
        {
            try
            {
                clientSocket = new TcpClient();
                clientSocket.Connect(new IPEndPoint(ipAddr, port));
                ChallengeMessage sale = ChallengeMessage.recvMe(clientSocket.GetStream());
                ResponseChallengeMessage resp = new ResponseChallengeMessage();
                resp.username = user;
                MD5 md5 = new MD5CryptoServiceProvider();
                resp.pswMd5 = Pds2Util.createPswMD5(passw, sale.salt);
                resp.sendMe(clientSocket.GetStream());
                ConfigurationMessage conf = ConfigurationMessage.recvMe(clientSocket.GetStream());
                if (!conf.success)
                {
                    MessageBox.Show("Impossibile connettersi user e/o password errati", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    closeConnection();
                    return;
                }
                videoSocket = new TcpClient();
                videoSocket.Connect(new IPEndPoint(ipAddr, conf.video_port));
                clipSocket = new TcpClient();
                clipSocket.Connect(new IPEndPoint(ipAddr, conf.clip_port));
                senderThread = new Thread(deliverMsg);
                senderThread.IsBackground = true;
                senderThread.Start();
                receiverThread = new Thread(receiveMsg);
                receiverThread.IsBackground = true;
                receiverThread.Start();
                deliverClipboardThread = new Thread(_dispatchClipboard);
                deliverClipboardThread.IsBackground = true;
                deliverClipboardThread.Start();
                listenClipboardThread = new Thread(_listenClipboard);
                listenClipboardThread.IsBackground = true;
                listenClipboardThread.Start();
                /*videoThread = new Thread(receiveVideo);
                videoThread.Start();*/
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                closeConnection();
                return;
            }
        }

        private void closeConnection()
        {
            if(clientSocket!=null)
                clientSocket.Close();
            if(clipSocket!=null)
                clipSocket.Close();
            if(videoSocket!=null)
                videoSocket.Close();
            threadKill();
        }

        private void threadKill()
        {
            if (senderThread != null)
            {
                senderThread.Abort();
                senderThread.Join();
            }
            if (receiverThread != null)
            {
                receiverThread.Abort();
                receiverThread.Join();
            }
            if (listenClipboardThread != null)
            {
                listenClipboardThread.Abort();
                listenClipboardThread.Join();
            }
            if (deliverClipboardThread != null)
            {
                deliverClipboardThread.Abort();
                deliverClipboardThread.Join();
            }
            if (videoThread != null)
            {
                videoThread.Abort();
                videoThread.Join();
            }
        }

        private void deliverMsg()
        {
            try
            {
                while (true)
                {
                    TextMessage msg = msgToSend.Take();
                    msg.sendMe(clientSocket.GetStream());
                    /*thread safe ???*/
                    updateLog(msg);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception)
            {}
        }

        private void receiveMsg()
        {
            try
            {
                while (true)
                {
                    TextMessage msg = TextMessage.recvMe(clientSocket.GetStream());
                    /* thread safe ???? */
                    updateLog(msg);
                }
            }
            catch(ThreadAbortException)
            {
                return; 
            }
            catch(Exception)
            {}
        }

        private void _dispatchClipboard()
        {
            try
            {
                while (true)
                {
                    ClipboardMessage msg = clipQueue.Take();
                    msg.sendMe(clipSocket.GetStream());
                    /*thread safe ???*/
                    updateLog(new TextMessage(MessageType.ADMIN, "", "Hai condiviso il contenuto della clipboard"));
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception)
            {}
        }

        private void _listenClipboard()
        {
            try
            {
                while (true)
                {
                    ClipboardMessage msg = ClipboardMessage.recvMe(clipSocket.GetStream());
                    receivedClipboard(msg);
                    if(msg.clipboardType == ClipBoardType.TEXT)
                        updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con del testo"));
                    else if(msg.clipboardType == ClipBoardType.BITMAP)
                        updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con un'immagine"));
                    else if(msg.clipboardType == ClipBoardType.FILE)
                        updateLog(new TextMessage(MessageType.ADMIN, msg.username, "ha condiviso la clipboard con un file che è stato salvato in ./File ricevuti client/"+msg.filename));
                }
            }
            catch
            {
                return;
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
                            bWrite = new BinaryWriter(File.Open(Path.GetFullPath(@".\File ricevuti client\") + msg.filename, FileMode.Create));
                        }
                        catch(DirectoryNotFoundException)
                        {
                            Directory.CreateDirectory(Path.GetFullPath(@".\File ricevuti server\"));
                            bWrite = new BinaryWriter(File.Open(Path.GetFullPath(@".\File ricevuti client\") + msg.filename, FileMode.Create));
                        }                        
                        bWrite.Write(msg.filedata);
                        bWrite.Close();
                        paths.Add(Path.GetFullPath(@".\File ricevuti client\") + msg.filename);
                        Clipboard.SetFileDropList(paths);
                        break;
                    case ClipBoardType.BITMAP:
                        Bitmap bitm = msg.bitmap;
                        Clipboard.SetImage(bitm);
                        break;
                }
            }
        }

        /*private void receiveVideo()
        {
            try
            {
                NetworkStream videoStream = videoSocket.GetStream();
                while(true)
                {
                    if (videoStream.DataAvailable)
                    {
                        ImageMessage msg = ImageMessage.recvMe(videoStream);
                        updatePictureBox(msg);
                    }
                }
            }
            catch
            {
                return;
            }
        }*/

        private void updateLog(TextMessage m)
        {
            if(this.messageText.InvokeRequired)
            {
                this.Invoke(new updateLogDelegate(this.updateLog), new object[] { m }); 
            }
            else
            {
                if (m.messageType == MessageType.TEXT)
                {
                    messageLog.SelectionFont = new Font(messageLog.Font, FontStyle.Regular);
                    messageLog.SelectionColor = Color.Blue;
                    messageLog.AppendText(m.username + " says: ");
                    messageLog.SelectionColor = Color.Black;
                    messageLog.AppendText(m.message + "\r\n\r\n");
                }
                else
                {
                    messageLog.SelectionFont = new Font(messageLog.Font, FontStyle.Italic);
                    messageLog.SelectionColor = Color.Red;
                    messageLog.AppendText(m.username + " " + m.message+"\n");
                }
            }
        }

        /*private void updatePictureBox(ImageMessage m)
        {
            if (this.desktopDisplay.InvokeRequired)
            {
                this.Invoke(new updatePictureBoxDelegate(this.updatePictureBox), new object[] { m });
            }
            else
            {
                TypeConverter tc = TypeDescriptor.GetConverter(typeof(Bitmap));
                Bitmap bitmap = (Bitmap)tc.ConvertFrom(m.bitmap);
                desktopDisplay.Image = bitmap;
                desktopDisplay.Refresh();
            }
        }*/

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

        private void hideChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (hideChatToolStripMenuItem.Text == "Nascondi chat")
            {
                hideVideoPreviewToolStripMenuItem.Enabled = false;
                hideChatToolStripMenuItem.Text = "Mostra chat";
                messageLog.Hide();
                messageText.Hide();
                sendButton.Hide();
                desktopDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
                desktopDisplay.Refresh();
            }
            else
            {
                hideVideoPreviewToolStripMenuItem.Enabled = true;
                hideChatToolStripMenuItem.Text = "Nascondi chat";
                messageLog.Show();
                messageText.Show();
                sendButton.Show();
                desktopDisplay.Dock = System.Windows.Forms.DockStyle.None;
                desktopDisplay.Refresh();
            }
        }

        private void hideVideoPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (hideVideoPreviewToolStripMenuItem.Text == "Nascondi video")
            {
                hideChatToolStripMenuItem.Enabled = false;
                hideVideoPreviewToolStripMenuItem.Text = "Mostra video";
                desktopDisplay.Hide();
                messageText.Dock = System.Windows.Forms.DockStyle.Bottom;
                messageLog.Dock = System.Windows.Forms.DockStyle.Bottom;
                messageText.Refresh();
                messageLog.Refresh();
            }
            else
            {
                hideChatToolStripMenuItem.Enabled = true;
                hideVideoPreviewToolStripMenuItem.Text = "Nascondi video";
                desktopDisplay.Show();
                messageLog.Dock = System.Windows.Forms.DockStyle.None;
                messageText.Dock = System.Windows.Forms.DockStyle.None;
                desktopDisplay.Refresh();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Creato da Stefano Abraham e Enea Bagalini", "About",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Information);
        }
    }
}
