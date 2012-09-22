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
        private delegate void updatePictureBoxDelegate(ImageMessage msg);
        private delegate void toolStripMenuDelegate(string text);
        
        private int port=2626;
        private string user="", passw="password";
        private bool isConnected = false, first = true;
        private Stream streamSender;
        private Stream streamReceiver;
        private TcpClient clientSocket, clipSocket, videoSocket;
        private IPAddress ipAddr=IPAddress.Parse("127.1");
        private Form settings;
        private BlockingCollection<TextMessage> msgToSend;
        private Thread senderThread, receiverThread, clipboardThread, videoThread;

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

        /*private void shareClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
                IDataObject d = Clipboard.GetDataObject();
                NetworkStream streamClip = clipSocket.GetStream();
                ClipboardMessage cm = new ClipboardMessage(user);

                if (d.GetDataPresent(DataFormats.Text))  //invio testo
                {
                    try
                    {
                        shareClipboardToolStripMenuItem.Enabled = false;
                        cm.clipboardType = ClipBoardType.TEXT;
                        String clipText = (string)d.GetData(DataFormats.Text);
                        cm.text = clipText;
                        cm.sendMe(streamClip);
                        shareClipboardToolStripMenuItem.Enabled = true;
                    }
                    catch
                    {
                        shareClipboardToolStripMenuItem.Enabled = true;
                        return;
                    }
                }
                else if (d.GetDataPresent(DataFormats.FileDrop, true))  //invio file
                {
                    ParameterizedThreadStart thrSendFile = new ParameterizedThreadStart(sendClipFile);
                    Thread thr = new Thread(thrSendFile);
                    thr.Start(d);
                }

                else if (Clipboard.ContainsImage())
                {
                    shareClipboardToolStripMenuItem.Enabled = false;
                    Bitmap img = (Bitmap)Clipboard.GetImage();
                    cm.username = user;
                    cm.clipboardType = ClipBoardType.BITMAP;
                    cm.bitmap = img;
                    shareClipboardToolStripMenuItem.Enabled = true;
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Invio fallito: la clipboard è vuota", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    shareClipboardToolStripMenuItem.Enabled = false;
                }
            }*/

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
                streamSender = clientSocket.GetStream();
                streamReceiver = clientSocket.GetStream();
                ChallengeMessage sale = ChallengeMessage.recvMe(streamReceiver);
                ResponseChallengeMessage resp = new ResponseChallengeMessage();
                resp.username = user;
                MD5 md5 = new MD5CryptoServiceProvider();
                resp.pswMd5 = Pds2Util.createPswMD5(passw, sale.salt);
                resp.sendMe(streamSender);
                ConfigurationMessage conf = ConfigurationMessage.recvMe(streamReceiver);
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
                msgToSend = new BlockingCollection<TextMessage>();
                senderThread = new Thread(deliverMsg);
                receiverThread = new Thread(receiveMsg);
                receiverThread.Start();
                senderThread.Start();
                /*clipboardThread = new Thread(listenClipboard);
                clipboardThread.Start();*/
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
            if (clipboardThread != null)
            {
                clipboardThread.Abort();
                clipboardThread.Join();
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
                    msg.sendMe(streamSender);
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
                    TextMessage msg = TextMessage.recvMe(streamReceiver);
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

        /*private void listenClipboard()
        {
            try
            {
                NetworkStream stream = clipSocket.GetStream();
                while (true)
                {
                    ClipboardMessage msg = ClipboardMessage.recv(stream);
                    receivedClipboard(msg);
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

        /*private void receivedClipboard(ClipboardMessage msg)
        {
            int i=0;
            System.Collections.Specialized.StringCollection paths = new System.Collections.Specialized.StringCollection();
            switch(msg.clipboardType)
            {
                case ClipBoardType.TEXT:
                    this.Invoke(new toolStripMenuDelegate(this.toolStripMenu), new object[] { "false" });
                    string s = msg.text;
                    string s1 = TrimFromZero(s);
                    IDataObject ido = new DataObject();
                    ido.SetData(s1);
                    Clipboard.SetDataObject(ido, true);
                    this.Invoke(new toolStripMenuDelegate(this.toolStripMenu), new object[] { "true" });
                    break;
                case ClipBoardType.FILE:
                    this.Invoke(new toolStripMenuDelegate(this.toolStripMenu), new object[] { "false" });
                    foreach (string filename in msg.filename)
                    {
                        BinaryWriter bWrite = new BinaryWriter(File.Open(Path.GetFullPath(@".\File ricevuti\") + filename, FileMode.Create));
                        bWrite.Write(msg.filedata[i], 4 + filename.Length, msg.filedata.Length - 4 - filename.Length);
                        bWrite.Close();
                        paths.Add(Path.GetFullPath(@".\File ricevuti\") + filename);
                    }
                    Clipboard.SetFileDropList(paths);
                    this.Invoke(new toolStripMenuDelegate(this.toolStripMenu), new object[] { "true" });
                    break;
                case ClipBoardType.BITMAP:
                    this.Invoke(new toolStripMenuDelegate(this.toolStripMenu), new object[] { "false" });
                    Stream stm = clipSocket.GetStream();
                    IFormatter formatter = new BinaryFormatter();
                    Bitmap bitm = (Bitmap)formatter.Deserialize(stm);
                    Clipboard.SetImage(bitm);
                    this.Invoke(new toolStripMenuDelegate(this.toolStripMenu), new object[] { "true" });
                    break;
            }
        }*/

        /*private string TrimFromZero(string input)
        {
            int index = input.IndexOf('\0');
            if (index < 0)
                return input;

            return input.Substring(0, index);
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
