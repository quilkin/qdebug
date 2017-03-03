using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ArdDebug.Serial;
using System.IO;

namespace ArdDebug
{
    public partial class MainForm : Form
    {
        SerialPortManager _spManager;
        Arduino _arduino;
        private int byteCount = 0;
        private ulong recvdNumber = 0;
        private int messageLength = 0;

        public MainForm()
        {
            InitializeComponent();

            UserInitialization();
        }


        private void UserInitialization()
        {
            _spManager = new SerialPortManager();
            _arduino = new Arduino();

            SerialSettings mySerialSettings = _spManager.CurrentSerialSettings;
            serialSettingsBindingSource.DataSource = mySerialSettings;
            portNameComboBox.DataSource = mySerialSettings.PortNameCollection;
            baudRateComboBox.DataSource = mySerialSettings.BaudRateCollection;


            _spManager.NewSerialDataRecieved += new EventHandler<SerialDataEventArgs>(_spManager_NewSerialDataRecieved);
            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _spManager.Dispose();
        }

        void _spManager_NewSerialDataRecieved(object sender, SerialDataEventArgs e)
        {
            if (this.InvokeRequired)
            {
                // Using this.Invoke causes deadlock when closing serial port, and BeginInvoke is good practice anyway.
                this.BeginInvoke(new EventHandler<SerialDataEventArgs>(_spManager_NewSerialDataRecieved), new object[] { sender, e });
                return;
            }

            int maxTextLength = 1000; // maximum text length in text box
            if (tbData.TextLength > maxTextLength)
                tbData.Text = tbData.Text.Remove(0, tbData.TextLength - maxTextLength);

            string str = Encoding.ASCII.GetString(e.Data);
            tbData.AppendText(str);
            //if (str.EndsWith("\n"))
            //{
            //    tbData.AppendText("\r\n");
            //}
            //if (e.Data.Length != 5 && e.Data.Length != 9)
            //{
            //    // rubbish, ignore but print details
            //    tbData.AppendText("?data length = " + e.Data.Length);
            //    tbData.AppendText(" ?");
            //    string str = Encoding.ASCII.GetString(e.Data);
            //    tbData.AppendText(str);
            //    tbData.AppendText("\r\n");
            //    return;
            //}
            //foreach (byte b in e.Data)
            //{
            //    if (e.Data.Length == 1)
            //    {
            //        if (byteCount == 0)
            //        {
            //            messageLength = e.Data[0];
            //            //recvdNumber = (ushort)((e.Data[0] << 8));
            //            tbData.AppendText(messageLength.ToString() + ": ");
            //            ++byteCount;
            //        }
            //        else if (byteCount <= messageLength)
            //        {
            //            recvdNumber *= 256;
            //            recvdNumber += (e.Data[0]);

            //            if (++byteCount > messageLength)
            //            {
            //                if (messageLength == 2)
            //                    tbData.AppendText(recvdNumber.ToString("04X"));
            //                else
            //                    tbData.AppendText(recvdNumber.ToString("08X"));
            //                byteCount = 0;
            //                recvdNumber = 0;
            //                tbData.AppendText("\r\n");
            //            }

            //        }
            //    }

            //}

            //ushort nextpc,pc;
            //if (ushort.TryParse(pcString, out pc))
            //{
            //    nextpc = _arduino.FindNextPC(pc);
            //    pcString = nextpc.ToString("X");
            //    _spManager.Send(pcString);
            //}
        }

        // Handles the "Start Listening"-buttom click event
        private void btnStart_Click(object sender, EventArgs e)
        {
            _spManager.StartListening();
        }

        // Handles the "Stop Listening"-buttom click event
        private void btnStop_Click(object sender, EventArgs e)
        {
            _spManager.StopListening();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            string message = this.textToSend.Text + '\n';
            //byte[] hexToSend = new byte[3];
            ushort progCounter = 0;
            //if (ushort.TryParse(message.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out progCounter))
            //{
            //    // sending a new address, in hex. Convert to two-byte array instead, to avoid Arduino having to deal with atoi() in hex
            //    hexToSend[0] = (byte) message[0];
            //    hexToSend[1] = (byte) (progCounter / 256);
            //    hexToSend[2] = (byte)(progCounter % 256);
            //    _spManager.Send(hexToSend);
            //}

            //else
            {
                _spManager.Send(message);
            }
        }

        private void portNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        public ListView SourceView()
        {
            return sourceView;
        }
        public ListView DisassemblyView()
        {
            return listDisassembly;
        }
        private void buttonLoad_Click(object sender, EventArgs e)
        {
            if (_arduino.OpenFiles(this.sourceView,this.listDisassembly))
            {
                this.labelSketch.Text = _arduino.FullFilename;
            }
            
        }

        private void buttonScan_Click(object sender, EventArgs e)
        {
            _spManager.ReScan();
            portNameComboBox.DataSource = _spManager.CurrentSerialSettings.PortNameCollection;
            portNameComboBox.Update();
        }
    }
}
