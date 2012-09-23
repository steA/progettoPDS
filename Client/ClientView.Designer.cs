namespace Client
{
    partial class ClientView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.sendButton = new System.Windows.Forms.Button();
            this.messageText = new System.Windows.Forms.TextBox();
            this.messageLog = new System.Windows.Forms.RichTextBox();
            this.desktopDisplay = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.shareClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hideChatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hideVideoPreviewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.desktopDisplay)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // sendButton
            // 
            this.sendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendButton.Enabled = false;
            this.sendButton.Location = new System.Drawing.Point(596, 351);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(75, 23);
            this.sendButton.TabIndex = 1;
            this.sendButton.Text = "Send";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // messageText
            // 
            this.messageText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.messageText.Enabled = false;
            this.messageText.Location = new System.Drawing.Point(350, 337);
            this.messageText.Multiline = true;
            this.messageText.Name = "messageText";
            this.messageText.Size = new System.Drawing.Size(233, 48);
            this.messageText.TabIndex = 2;
            this.messageText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.messageText_KeyPress);
            // 
            // messageLog
            // 
            this.messageLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.messageLog.Location = new System.Drawing.Point(350, 30);
            this.messageLog.Name = "messageLog";
            this.messageLog.ReadOnly = true;
            this.messageLog.Size = new System.Drawing.Size(315, 275);
            this.messageLog.TabIndex = 10;
            this.messageLog.Text = "";
            // 
            // desktopDisplay
            // 
            this.desktopDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.desktopDisplay.BackColor = System.Drawing.Color.Black;
            this.desktopDisplay.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.desktopDisplay.Location = new System.Drawing.Point(15, 30);
            this.desktopDisplay.Name = "desktopDisplay";
            this.desktopDisplay.Size = new System.Drawing.Size(311, 355);
            this.desktopDisplay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.desktopDisplay.TabIndex = 4;
            this.desktopDisplay.TabStop = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(3, 3);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(680, 24);
            this.menuStrip1.TabIndex = 11;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.shareClipboardToolStripMenuItem,
            this.hideChatToolStripMenuItem,
            this.hideVideoPreviewToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // connectToolStripMenuItem
            // 
            this.connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            this.connectToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.connectToolStripMenuItem.Text = "Connetti";
            this.connectToolStripMenuItem.Click += new System.EventHandler(this.connectToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.settingsToolStripMenuItem.Text = "Impostazioni";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // shareClipboardToolStripMenuItem
            // 
            this.shareClipboardToolStripMenuItem.Name = "shareClipboardToolStripMenuItem";
            this.shareClipboardToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.shareClipboardToolStripMenuItem.Text = "Condividi clipboard";
            this.shareClipboardToolStripMenuItem.Click += new System.EventHandler(this.shareClipboardToolStripMenuItem_Click);
            // 
            // hideChatToolStripMenuItem
            // 
            this.hideChatToolStripMenuItem.Name = "hideChatToolStripMenuItem";
            this.hideChatToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.hideChatToolStripMenuItem.Text = "Nascondi chat";
            this.hideChatToolStripMenuItem.Click += new System.EventHandler(this.hideChatToolStripMenuItem_Click);
            // 
            // hideVideoPreviewToolStripMenuItem
            // 
            this.hideVideoPreviewToolStripMenuItem.Name = "hideVideoPreviewToolStripMenuItem";
            this.hideVideoPreviewToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.hideVideoPreviewToolStripMenuItem.Text = "Nascondi video";
            this.hideVideoPreviewToolStripMenuItem.Click += new System.EventHandler(this.hideVideoPreviewToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.exitToolStripMenuItem.Text = "Esci";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.esciToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.helpToolStripMenuItem.Text = "Aiuto";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // ClientView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.ClientSize = new System.Drawing.Size(686, 404);
            this.Controls.Add(this.desktopDisplay);
            this.Controls.Add(this.messageLog);
            this.Controls.Add(this.messageText);
            this.Controls.Add(this.sendButton);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ClientView";
            this.Padding = new System.Windows.Forms.Padding(3);
            this.Text = "Chat Client";
            ((System.ComponentModel.ISupportInitialize)(this.desktopDisplay)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button sendButton;
        private System.Windows.Forms.TextBox messageText;
        private System.Windows.Forms.RichTextBox messageLog;
        private System.Windows.Forms.PictureBox desktopDisplay;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shareClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hideVideoPreviewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hideChatToolStripMenuItem;
    }
}