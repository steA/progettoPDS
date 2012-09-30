using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Server
{
    public partial class VideoSettings : Form
    {

        private int x_s, y_s, w_s, h_s;
        private int captureStyle;
        private ServerView serverView;

        public void setCaptureStyle(int style) 
        {
            captureStyle = style;
        }

        public int getCaptureStyle() 
        {
            return captureStyle;
        }

        public void setScreenDimension(int x,int y,int w,int h)
        {
            x_s = x;
            y_s = y;
            w_s = w;
            h_s = h;
        }

        public VideoSettings(ServerView s)
        {
            InitializeComponent();
            this.serverView = s;

            setCaptureStyle(serverView.getCaptureStyle());
            Rectangle r = serverView.getRect();
            setScreenDimension(r.X, r.Y, r.Width, r.Height);

            switch (captureStyle)
            {
                case 1:
                    {
                        radioButton1.Checked = true;
                        groupBox2.Enabled = false;
                        break;
                    };
                case 2:
                    {
                        radioButton2.Checked = true;
                        groupBox2.Enabled = false;
                        break;
                    };
                case 3:
                    {
                        radioButton3.Checked = true;
                        groupBox2.Enabled = true;
                        break;
                    };
            }

        }


        private void areaSelection() 
        {
            Rectangle rect;
            Selection selection = new Selection();
            selection.ShowDialog();
            rect = selection.getRect();
            setScreenDimension(rect.X, rect.Y, rect.Width, rect.Height);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true) serverView.setCaptureStyle(1);
            if (radioButton2.Checked == true) serverView.setCaptureStyle(2);
            if (radioButton3.Checked == true)
            {
                setScreenDimension(int.Parse(textBox1.Text), int.Parse(textBox2.Text), int.Parse(textBox3.Text), int.Parse(textBox4.Text));
                serverView.setScreenAreaDimension(x_s, y_s, w_s, h_s);
                serverView.setCaptureStyle(3);
            }
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textDimensionUpdate()
        {
            textBox1.Text = x_s.ToString();
            textBox2.Text = y_s.ToString();
            textBox3.Text = w_s.ToString();
            textBox4.Text = h_s.ToString();
        }

        private void textDimensionUpdate(string s1, string s2, string s3, string s4)
        {
            textBox1.Text = s1;
            textBox2.Text = s2;
            textBox3.Text = s3;
            textBox4.Text = s4;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            
            if (radioButton1.Checked)
            { 
            groupBox2.Enabled = false;
            textDimensionUpdate("", "", "", "");
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            
            if (radioButton2.Checked)
            {
                textDimensionUpdate(Screen.PrimaryScreen.Bounds.X.ToString(),
                 Screen.PrimaryScreen.Bounds.Y.ToString(),
                 Screen.PrimaryScreen.Bounds.Size.Width.ToString(),
                 Screen.PrimaryScreen.Bounds.Size.Height.ToString());
                 groupBox2.Enabled = false;
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked) {
                if((x_s+y_s+w_s+h_s)==0)
                    areaSelection();
                    textDimensionUpdate();
                    groupBox2.Enabled = true;
            };

        }

        private void button3_Click(object sender, EventArgs e)
        {
            areaSelection();
            textDimensionUpdate();
        }

    }
}
