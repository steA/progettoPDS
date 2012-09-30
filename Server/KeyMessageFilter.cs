using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Server
{
    public class KeyMessageFilter : IMessageFilter
    {
        private const int WM_KEYDOWN = 0x0100;

        private const int WM_KEYUP = 0x0101;

        public event KeyPressEventHandler keyPressed;

        bool flag = false;

        Keys pressed;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_KEYDOWN)
            {
                if ((Keys)m.WParam == (Keys.LButton | Keys.ShiftKey))
                    flag = true;
                else if (flag)
                {
                    pressed = (Keys)m.WParam;
                    keyPressed(this, new KeyPressEventArgs((char)pressed));
                    flag = false;
                }
            }

            if (m.Msg == WM_KEYUP)
            {
                flag = false;
            }
            return false;
        }
    }

}
