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
                tbData.Text = tbData.Text.Remove(0, maxTextLength/2);

            string str = Encoding.Default.GetString(e.Data);
            while (str.Length > 0 && str[0] == '\0')
            {
                    str = str.Substring(1);

            }
            if (str.Length == 0)
                return;

            Arduino.InteractionString buildStr = _arduino.comString;
            tbData.AppendText(str);
            if (buildStr != null)
            {

                buildStr.Content += str;

                //if (buildStr.Content.Contains("num"))
                //{
                //    buildStr.Content = "num";
                //}
                if (buildStr.Content.Contains("\n"))
                {
                    // two messages may be concatenated
                    string[] split = null;
                    if (buildStr.Content.IndexOf('\n') < buildStr.Content.Length - 1)
                    {
                        split = buildStr.Content.Split('\n');
                        if (split.Length > 1)
                        {
                            buildStr.Content = split[0];
                        }
                    }
                    char firstChar = buildStr.Content[0];
                    if (buildStr.Content.Length > 4)
                    {
                        //if (firstChar == (byte)Arduino.Chars.PROGCOUNT_CHAR) 
                        if (firstChar == 'P')
                        {
                            string newString = _arduino.newProgramCounter(buildStr);
                            //buildStr.Content = string.Empty;
                        }
                        else if (firstChar == 'S')
                        {
                            string reply = _arduino.newProgramCounter(buildStr);
                            _spManager.Send(reply);
                            //buildStr.Content = string.Empty;
                        }
                        else if (firstChar == 'T')
                        {
                            // no reply to send, just info

                        }
                    }
                    if (split != null && split.Length > 1)
                        buildStr.Content = split[1];
                    else
                        buildStr.Content = string.Empty;
                }

            }

        }

        // Handles the "Start Listening"-buttom click event
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (listDisassembly.Items.Count < 10)
            {
                MessageBox.Show("Must open your Arduino sketch first!");
                this.buttonLoad.Select();
                return;
            }
            _spManager.StartListening();
            _arduino.comString = new Arduino.InteractionString();
            _arduino.currentBreakpoint = null;
        }

        // Handles the "Stop Listening"-buttom click event
        private void btnStop_Click(object sender, EventArgs e)
        {
            _spManager.StopListening();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            string message = this.textToSend.Text + '\n';
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
            _spManager.ScanPorts();
            portNameComboBox.DataSource = _spManager.CurrentSerialSettings.PortNameCollection;
            portNameComboBox.Update();
        }
    }
}
