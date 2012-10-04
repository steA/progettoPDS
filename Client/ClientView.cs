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
using System.Drawing.Imaging;

namespace Client
{
    public partial class ClientView : Form
    {
        private delegate void updateLogDelegate(TextMessage m);
        private delegate void receivedClipboardDelegate(ClipboardMessage msg);
        private delegate void closeConnectionDelegate(bool error);
        private delegate void stopVideoDelegate();
        
        private int port=5000;
        private string user="", passw="password";
        private bool isConnected = false, first = true;
        private TcpClient clientSocket, clipSocket, videoSocket;
        private IPAddress ipAddr=IPAddress.Parse("127.1");
        private Form settings;
        private BlockingCollection<TextMessage> msgToSend = new BlockingCollection<TextMessage>();
        private BlockingCollection<ClipboardMessage> clipQueue = new BlockingCollection<ClipboardMessage>();
        private Thread senderThread, receiverThread, listenClipboardThread, deliverClipboardThread, videoThread;

        public bool getIsConnected() 
        { 
            return isConnected;
        }


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
                closeConnection(false);
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
               //spostato in settings
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
                try
                {
                    InitializeConnection();
                    isConnected = true;
                    messageText.Enabled = true;
                    sendButton.Enabled = true;
                    button1.Enabled = true;
                    connectToolStripMenuItem.Text = "Disconnect";
                }
                catch(Exception ex)
                {
                    closeConnection(true);
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                closeConnection(false);
            }

        }

        private void esciToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isConnected == true)
            {
                closeConnection(false);
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
            //sostituita con pulsante
        }

        public void DispatchClipboard(ClipboardMessage msg)
        {
            clipQueue.Add(msg);
        }

        private void InitializeConnection()
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
                    throw new Exception("Impossibile connettersi user e/o password errati");
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
                videoThread = new Thread(receiveVideo);
                videoThread.IsBackground = true;
                videoThread.Start();
        }

        private void closeConnection(bool error)
        {
            try
            {
                threadKill();
                if (!error)
                {
                    TextMessage t = new TextMessage();
                    t.username = user;
                    t.message = "si è disconnesso";
                    t.messageType = MessageType.DISCONNECT;
                    t.sendMe(clientSocket.GetStream());
                    updateLog(t);
                }
                else
                {
                    TextMessage t = new TextMessage();
                    t.username = "Errore";
                    t.message = "disconnessione";
                    t.messageType = MessageType.DISCONNECT;
                    updateLog(t);
                }
                
            }
            catch { }

            finally
            {

                if (clientSocket != null)
                    clientSocket.Close();
                if (clipSocket != null)
                    clipSocket.Close();
                if (videoSocket != null)
                    videoSocket.Close();
                isConnected = false;
                messageText.Enabled = false;
                sendButton.Enabled = false;
                button1.Enabled = false;
                connectToolStripMenuItem.Text = "Connect";
                desktopDisplay.Image = null;
            }
        }

        private void threadKill()
        {
            if (senderThread != null)
            {
                senderThread.Abort();
            }
            if (receiverThread != null)
            {
                receiverThread.Abort();
            }
            if (listenClipboardThread != null)
            {
                listenClipboardThread.Abort();
            }
            if (deliverClipboardThread != null)
            {
                deliverClipboardThread.Abort();
            }
            if (videoThread != null)
            {
                videoThread.Abort();
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
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void receiveMsg()
        {
            try
            {
                while (true)
                {
                    //if (clientSocket.GetStream().DataAvailable)
                    //{
                        TextMessage msg = TextMessage.recvMe(clientSocket.GetStream());

                        if (msg.messageType == MessageType.VIDEO_STOP)
                        {
                            nullPictureBox();
                        }

                        else
                        {
                            updateLog(msg);
                            if (msg.username.Equals("Server") && msg.messageType == MessageType.DISCONNECT)
                            {
                                this.Invoke(new closeConnectionDelegate(this.closeConnection), new object[] { false });
                                return;
                            }

                        }
                   // }
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception)
            {
                this.Invoke(new closeConnectionDelegate(this.closeConnection), new object[] { true });
                return;
            }
        }

        private void _dispatchClipboard()
        {
            try
            {
                while (true)
                {
                    ClipboardMessage msg = clipQueue.Take();
                    msg.sendMe(clipSocket.GetStream());
                    updateLog(new TextMessage(MessageType.ADMIN, "", "Hai condiviso il contenuto della clipboard"));
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void _listenClipboard()
        {
            try
            {
                while (true)
                {
                    //if (clipSocket.GetStream().DataAvailable)
                    //{
                        ClipboardMessage msg = ClipboardMessage.recvMe(clipSocket.GetStream());
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
                    //}
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch
            {
                this.Invoke(new closeConnectionDelegate(this.closeConnection), new object[] { true });
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
                        Clipboard.SetText(msg.text);
                        break;
                    case ClipBoardType.FILE:
                        BinaryWriter bWrite;
                        try
                        {
                            bWrite = new BinaryWriter(File.Open(Path.GetFullPath(@".\File ricevuti client\") + msg.filename, FileMode.Create));
                        }
                        catch (DirectoryNotFoundException)
                        {
                            Directory.CreateDirectory(Path.GetFullPath(@".\File ricevuti client\"));
                            bWrite = new BinaryWriter(File.Open(Path.GetFullPath(@".\File ricevuti client\") + msg.filename, FileMode.Create));
                        }
                        bWrite.Write(msg.filedata);
                        bWrite.Close();
                        paths.Add(Path.GetFullPath(@".\File ricevuti client\") + msg.filename);
                        Clipboard.SetFileDropList(paths);
                        break;
                    case ClipBoardType.BITMAP:
                        Clipboard.SetImage(msg.bitmap);
                        break;
                }
            }
        }

        private void receiveVideo()
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
            catch(ThreadAbortException)
            {
                return;
            }
            catch
            {
                this.Invoke(new closeConnectionDelegate(this.closeConnection), new object[] { true });
                return;
            }
        }

        private void updatePictureBox(ImageMessage m) 
        {

            MemoryStream ms = new MemoryStream(m.bitmap);
            desktopDisplay.Image = byteToImage(ms, m);
        
        }

        public Image byteToImage(MemoryStream ms, ImageMessage m)
        {
            //MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = null;
            Bitmap b = null;
            try
            {
                returnImage = Bitmap.FromStream(ms);
                //Graphics gr = Graphics.FromImage(returnImage);
                if (m.style == 3)
                {
                    Size s = GenerateImageDimensions(m.img_size.Width, m.img_size.Height, desktopDisplay.Width, desktopDisplay.Height);
                    b = new Bitmap(returnImage, s.Width, s.Height);
                }
                else
                    b = (Bitmap)returnImage;
                //center the new image
                desktopDisplay.SizeMode = PictureBoxSizeMode.CenterImage;
            }
            catch (System.Exception ec)
            {
                MessageBox.Show(ec.ToString());
            }
            return (Image)b;
        }

        //Generate new image dimensions
        public Size GenerateImageDimensions(int currW, int currH, int destW, int destH)
        {
            //double to hold the final multiplier to use when scaling the image
            double multiplier = 0;

            //string for holding layout
            string layout;

            //determine if it's Portrait or Landscape
            if (currH > currW) layout = "portrait";
            else layout = "landscape";

            switch (layout.ToLower())
            {
                case "portrait":
                    //calculate multiplier on heights
                    if (destH > destW)
                    {
                        multiplier = (double)destW / (double)currW;
                    }

                    else
                    {
                        multiplier = (double)destH / (double)currH;
                    }
                    break;
                case "landscape":
                    //calculate multiplier on widths
                    if (destH > destW)
                    {
                        multiplier = (double)destW / (double)currW;
                    }

                    else
                    {
                        multiplier = (double)destH / (double)currH;
                    }
                    break;
            }

            //return the new image dimensions
            return new Size((int)(currW * multiplier), (int)(currH * multiplier));
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

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
                    messageLog.AppendText(m.username + ": ");
                    messageLog.SelectionColor = Color.Black;
                    messageLog.AppendText(m.message + "\n");
                }
                else
                {
                    messageLog.SelectionFont = new Font(messageLog.Font, FontStyle.Italic);
                    messageLog.SelectionColor = Color.Red;
                    messageLog.AppendText(m.username + " " + m.message+"\n");
                }

                messageLog.SelectionStart = messageLog.Text.Length;
                messageLog.ScrollToCaret();
                messageLog.Refresh();
            }
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

        private void hideChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (hideChatToolStripMenuItem.Text == "Hide Chat")
            {
                int dd_width;
                hideVideoPreviewToolStripMenuItem.Enabled = false;
                hideChatToolStripMenuItem.Text = "Show Chat";
                messageLog.Hide();
                messageText.Hide();
                sendButton.Hide();
                button1.Hide();
                dd_width = desktopDisplay.Width;
                desktopDisplay.Width = dd_width + messageLog.Width + 24;
                desktopDisplay.Refresh();
            }
            else
            {
                int dd_width;
                hideVideoPreviewToolStripMenuItem.Enabled = true;
                hideChatToolStripMenuItem.Text = "Hide Chat";
                messageLog.Show();
                messageText.Show();
                sendButton.Show();
                button1.Show();
                dd_width = desktopDisplay.Width;
                desktopDisplay.Width = dd_width - messageLog.Width - 24;
                desktopDisplay.Refresh();
            }
        }

        private void hideVideoPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (hideVideoPreviewToolStripMenuItem.Text == "Hide Video")
            {
                hideChatToolStripMenuItem.Enabled = false;
                hideVideoPreviewToolStripMenuItem.Text = "Show Video";
                desktopDisplay.Hide();
                int ml_width = messageLog.Width;
                int mt_width = messageText.Width;
                messageLog.Left = desktopDisplay.Left;
                messageLog.Width = ml_width + desktopDisplay.Width + 24;
                messageLog.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                messageText.Left = messageLog.Left;
                messageText.Width = mt_width + desktopDisplay.Width + 24;
                messageText.Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
                button1.Left = messageLog.Left;
                button1.Width = messageLog.Width;
                sendButton.Left = messageLog.Left;
                sendButton.Width = messageLog.Width;
            }
            else
            {
                hideChatToolStripMenuItem.Enabled = true;
                hideVideoPreviewToolStripMenuItem.Text = "Hide Video";
                desktopDisplay.Show();
                int ml_width = messageLog.Width;
                int mt_width = messageText.Width;
                messageLog.Left = desktopDisplay.Location.X + desktopDisplay.Width + 24;
                messageLog.Width = ml_width - desktopDisplay.Width - 24;
                messageLog.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                messageText.Left = desktopDisplay.Location.X + desktopDisplay.Width + 24;
                messageText.Width = mt_width - desktopDisplay.Width - 24;
                messageText.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                button1.Left = messageLog.Left;
                button1.Width = messageLog.Width;
                sendButton.Left = messageLog.Left;
                sendButton.Width = messageLog.Width;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Creato da Stefano Abraham e Enea Bagalini", "About",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Information);
        }


        //Connection settings
        private void connectionToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            settings = new Settings(this, user, ipAddr.ToString(), passw, port.ToString());
            settings.ShowDialog();
        }


        //share clipboard
        private void button1_Click_1(object sender, EventArgs e)
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
                    string sourceFileName = ((string[])d.GetData(DataFormats.FileDrop))[0];
                    FileInfo fleMembers = new FileInfo(sourceFileName);
                    float size = (float)(fleMembers.Length / 1024 / 1024); //MB
                    if (size > 50)
                    {
                        MessageBox.Show("Impossibile inviare " + sourceFileName + " perché troppo grande", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
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

        private void nullPictureBox()
        {
            if (this.InvokeRequired)
                this.Invoke(new stopVideoDelegate(this.nullPictureBox));
            else
            desktopDisplay.Image = null;
        }

        private void desktopDisplay_Click(object sender, EventArgs e)
        {

        }
    }
}
