namespace Server
{
    partial class Settings
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
            this.password = new System.Windows.Forms.Label();
            this.ipAddress = new System.Windows.Forms.Label();
            this.port = new System.Windows.Forms.Label();
            this.passwordText = new System.Windows.Forms.TextBox();
            this.ipAddrText = new System.Windows.Forms.TextBox();
            this.portText = new System.Windows.Forms.TextBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // password
            // 
            this.password.AutoSize = true;
            this.password.Location = new System.Drawing.Point(12, 41);
            this.password.Name = "password";
            this.password.Size = new System.Drawing.Size(53, 13);
            this.password.TabIndex = 1;
            this.password.Text = "Password";
            // 
            // ipAddress
            // 
            this.ipAddress.AutoSize = true;
            this.ipAddress.Location = new System.Drawing.Point(12, 120);
            this.ipAddress.Name = "ipAddress";
            this.ipAddress.Size = new System.Drawing.Size(57, 13);
            this.ipAddress.TabIndex = 2;
            this.ipAddress.Text = "IP address";
            // 
            // port
            // 
            this.port.AutoSize = true;
            this.port.Location = new System.Drawing.Point(12, 204);
            this.port.Name = "port";
            this.port.Size = new System.Drawing.Size(26, 13);
            this.port.TabIndex = 3;
            this.port.Text = "Port";
            // 
            // passwordText
            // 
            this.passwordText.Location = new System.Drawing.Point(100, 34);
            this.passwordText.Name = "passwordText";
            this.passwordText.PasswordChar = '*';
            this.passwordText.Size = new System.Drawing.Size(100, 20);
            this.passwordText.TabIndex = 5;
            // 
            // ipAddrText
            // 
            this.ipAddrText.Location = new System.Drawing.Point(100, 113);
            this.ipAddrText.Name = "ipAddrText";
            this.ipAddrText.Size = new System.Drawing.Size(100, 20);
            this.ipAddrText.TabIndex = 7;
            // 
            // portText
            // 
            this.portText.Location = new System.Drawing.Point(100, 197);
            this.portText.Name = "portText";
            this.portText.Size = new System.Drawing.Size(100, 20);
            this.portText.TabIndex = 8;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(140, 260);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 9;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(38, 260);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 10;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(274, 318);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.portText);
            this.Controls.Add(this.ipAddrText);
            this.Controls.Add(this.passwordText);
            this.Controls.Add(this.port);
            this.Controls.Add(this.ipAddress);
            this.Controls.Add(this.password);
            this.Name = "Settings";
            this.Text = "Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label password;
        private System.Windows.Forms.Label ipAddress;
        private System.Windows.Forms.Label port;
        private System.Windows.Forms.TextBox passwordText;
        private System.Windows.Forms.TextBox ipAddrText;
        private System.Windows.Forms.TextBox portText;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
    }
}