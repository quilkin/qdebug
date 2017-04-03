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
        delegate void commsDelegate(string str, bool sending);
        public void UpdateCommsBox(string str, bool sending)
        {
            if (comms.InvokeRequired)
            {
                commsDelegate d = new commsDelegate(UpdateCommsBox);
                comms.Invoke(d, new object[] {  str, sending });
            }
            else
            {
                if (comms.Visible == false)
                    return;
                if (str == null)
                    return;
                //if (_Running != null && _Running.IsBusy)
                //    return;
                int maxTextLength = 1000; // maximum text length in text box
                if (comms.TextLength > maxTextLength)
                    comms.Text = comms.Text.Remove(0, maxTextLength / 2);

                comms.ForeColor = (sending ? System.Drawing.Color.Red : System.Drawing.Color.Black);
#if __GDB__
                comms.AppendText(str + Environment.NewLine);
               
                
#else
                comms.AppendText(str + " ");
#endif
            }
        }

        public string ReadLine(int timeout)
        {
            //while (_Running.IsBusy)
            //{ }
            //while (comString != string.Empty)
            //{
            //}
            if (spmanager == null)
                return null;
            string str = spmanager.ReadLine(timeout);
            if (str.Length > 3)
                UpdateCommsBox(str, false);
            return str;
        }
        public string ReadLine()
        {
            //while (comString != string.Empty)
            //{
            //}
            if (spmanager == null)
                return null;
            string str = spmanager.ReadLine();

            //if (str.Length > 3)
                UpdateCommsBox(str, false);
            return str;
        }
        public void Send(string str)
        {
            if (spmanager == null)
                return;
            if (str == null)
                return;
            //if (str.Length > 3)
                UpdateCommsBox(str, true);
            spmanager.Send(str);
            comString = string.Empty;
        }

    }
}