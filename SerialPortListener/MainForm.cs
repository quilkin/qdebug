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
        Arduino Arduino;


        public MainForm()
        {
            InitializeComponent();

            UserInitialization();
        }


        private void UserInitialization()
        {
            _spManager = new SerialPortManager();
            Arduino = new Arduino(this.panel2Running,this.panelStopped);

            SerialSettings mySerialSettings = _spManager.CurrentSerialSettings;
            serialSettingsBindingSource.DataSource = mySerialSettings;
            portNameComboBox.DataSource = mySerialSettings.PortNameCollection;
            baudRateComboBox.DataSource = mySerialSettings.BaudRateCollection;

            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _spManager.Dispose();
        }


        // Handles the "Start Listening"-buttom click event
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (disassemblyView.Items.Count < 10)
            {
                MessageBox.Show("Must open your Arduino sketch first!");
                this.buttonLoad.Select();
                return;
            }
            

            Arduino.Startup(_spManager);

        }

        // Handles the "Stop Listening"-buttom click event
        private void btnStop_Click(object sender, EventArgs e)
        {
            _spManager.StopListening();
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            Arduino._Running.CancelAsync();
            //Arduino.pauseReqd = true;
        }
        private void buttonStep_Click(object sender, EventArgs e)
        {
            Arduino.SingleStep();
        }


        private void portNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void buttonLoad_Click(object sender, EventArgs e)
        {
            if (Arduino.OpenFiles(this.sourceView,this.disassemblyView,this.varView,this.tbData))
            {
                this.labelSketch.Text = Arduino.FullFilename;
            }
            
        }

        private void buttonScan_Click(object sender, EventArgs e)
        {
            _spManager.ScanPorts();
            portNameComboBox.DataSource = _spManager.CurrentSerialSettings.PortNameCollection;
            portNameComboBox.Update();
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            Arduino.FindBreakpoint();
        }

        private void buttonStepOver_Click(object sender, EventArgs e)
        {
            Arduino.StepOver();
        }

        private void buttonComms_Click(object sender, EventArgs e)
        {
            tbData.Visible = !tbData.Visible;
        }

        private void buttonDiss_Click(object sender, EventArgs e)
        {
            disassemblyView.Visible = !disassemblyView.Visible;
            if (disassemblyView.Visible)
            {
                varView.Height = 270;
            }
            else
            {
                varView.Height = 470;
            }
            varView.Refresh();
           
        }

        //private void varView_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    Arduino.ExpandVariable();
        //}
    }
}
