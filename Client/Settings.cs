using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace Client
{
    public partial class Settings : Form
    {
        private int p;
        private ClientView client_view;
        private IPAddress ipAddr;

        public Settings()
        {
            InitializeComponent();
        }

        public Settings(ClientView cv, string username, string ipAddr, string password, string port)
        {
            InitializeComponent();
            this.client_view = cv;
            userText.Text = username;
            ipAddrText.Text = ipAddr;
            passwordText.Text = password;
            portText.Text = port;
        }

        public Settings(ClientView cv)
        {
            InitializeComponent();
            this.client_view = cv;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (userValid(userText.Text) &&
                passValid(passwordText.Text) &&
                ipValid(ipAddrText.Text) &&
                portValid(portText.Text))
            {
                client_view.setUser(userText.Text);
                client_view.setPw(passwordText.Text);
                client_view.setIp(ipAddr);
                client_view.setPort(p);
                client_view.setFlag();
                this.Close();
            }
        }

        private bool userValid(string user)
        {
            if (string.IsNullOrEmpty(user))
            {
                MessageBox.Show("Insert a username!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }else if(user.Equals("Server"))
            {
                MessageBox.Show("Username riservato!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
