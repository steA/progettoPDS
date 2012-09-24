﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Server
{
    public class KeyMessageFilter : IMessageFilter
    {
        private Dictionary<Keys, bool> m_keyTable = new Dictionary<Keys, bool>();

        public Dictionary<Keys, bool> KeyTable
        {
            get { return m_keyTable; }
            private set { m_keyTable = value; }
        }

        public bool IsKeyPressed()
        {
            return m_keyPressed;
        }

        public bool IsKeyPressed(Keys k)
        {
            bool pressed = false;

            if (KeyTable.TryGetValue(k, out pressed))
            {
                return pressed;
            }

            return false;
        }

        private const int WM_KEYDOWN = 0x0100;

        private const int WM_KEYUP = 0x0101;

        private bool m_keyPressed = false;

        public event KeyPressEventHandler keyPressed;

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(uint uCode, uint uMapType);

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_KEYDOWN)
            {
                KeyTable[(Keys)m.WParam] = true;

                m_keyPressed = true;
                int nonVirtualKey = MapVirtualKey((uint)m.WParam, 2);
                char mappedChar = Convert.ToChar(nonVirtualKey);
                keyPressed(this, new KeyPressEventArgs(mappedChar));
            }

            if (m.Msg == WM_KEYUP)
            {
                KeyTable[(Keys)m.WParam] = false;

                m_keyPressed = false;
            }

            return false;
        }
    }

}
