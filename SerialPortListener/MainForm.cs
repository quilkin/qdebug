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

            Arduino.InteractionString buildStr = _arduino.comString;
            tbData.AppendText(str);
            if (buildStr != null)
            {
                buildStr.Content += str;
                if (str.Contains("\n"))
                {
                    if (buildStr.Content.StartsWith("?"))
                        buildStr.Content = buildStr.Content.Substring(1);
                    if (buildStr.Content.Length > 4) { 
                        if (buildStr.Content.StartsWith("P") )
                        {
                            string newString = _arduino.newProgramCounter(buildStr);
                            buildStr.Content = string.Empty;
                        }
                        else if ( buildStr.Content.StartsWith("S"))
                        {
                            string reply = _arduino.newProgramCounter(buildStr);
                            _spManager.Send(reply);
                            buildStr.Content = string.Empty;
                        }
                    }
                }

            }

        }

        // Handles the "Start Listening"-buttom click event
        private void btnStart_Click(object sender, EventArgs e)
        {
            _spManager.StartListening();
            _arduino.comString = new Arduino.InteractionString();
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
