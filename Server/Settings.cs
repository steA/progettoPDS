using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace Server
{
    public partial class Settings : Form
    {
        private int p;
        private ServerView server_view;
        private IPAddress ipAddr;

        public Settings()
        {
            InitializeComponent();
        }

        public Settings(ServerView sv, string username, string ipAddr, string password, string port)
        {
            InitializeComponent();
            this.server_view = sv;
            ipAddrText.Text = ipAddr;
            passwordText.Text = password;
            portText.Text = port;
        }

        public Settings(ServerView sv)
        {
            InitializeComponent();
            this.server_view = sv;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (passValid(passwordText.Text) &&
                ipValid(ipAddrText.Text) &&
                portValid(portText.Text))
            {
                server_view.setPw(passwordText.Text);
                server_view.setIp(ipAddr);
                server_view.setPort(p);
                server_view.setFlag();
                this.Close();
            }
        }

        private bool userValid(string user)
        {
            if (string.IsNullOrEmpty(user))
            {
                MessageBox.Show("Insert a username!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private bool passValid(string pass)
        {
            if (string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Insert a password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private bool ipValid(string ip)
        {
            try
            {
                ipAddr=IPAddress.Parse(ip);
                return true;
            }
            catch
            {
                MessageBox.Show("Insert a valid Ip address!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool portValid(string port)
        {
            try
            {
                p = int.Parse(port);
                return true;
            }
            catch
            {
                MessageBox.Show("Insert a valid port!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
