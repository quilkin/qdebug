using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;

namespace ArdDebug
{
    partial class Arduino
    {
        private void UpdateCommsBox(string str, bool sending)
        {
            if (str == null)
                return;
            if (_Running != null && _Running.IsBusy)
                return;
            int maxTextLength = 1000; // maximum text length in text box
            if (comms.TextLength > maxTextLength)
                comms.Text = comms.Text.Remove(0, maxTextLength / 2);

            comms.ForeColor = (sending ? System.Drawing.Color.Red : System.Drawing.Color.Black);

            comms.AppendText(str + " ");
        }

        public string ReadLine(int timeout)
        {
            string str = spmanager.ReadLine(timeout);
            if (str.Length > 3)
                UpdateCommsBox(str, false);
            return str;
        }
        public string ReadLine()
        {
            string str = spmanager.ReadLine();

            if (str.Length > 3)
                UpdateCommsBox(str, false);
            return str;
        }
        public void Send(string str)
        {
            if (spmanager == null)
                return;
            if (str.Length > 3)
                UpdateCommsBox(str, true);
            spmanager.Send(str);
        }

    }
}