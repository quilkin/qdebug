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

            Arduino = new Arduino(this);

            SerialSettings mySerialSettings = _spManager.CurrentSerialSettings;
            serialSettingsBindingSource.DataSource = mySerialSettings;
            portNameComboBox.DataSource = mySerialSettings.PortNameCollection;
            baudRateComboBox.DataSource = mySerialSettings.BaudRateCollection;

            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
            this.timer1.Enabled = false;
            this.timer1.Interval = 500;
        }

        delegate void buttonDelegate(bool enabled);
        public void RunButtons(bool enabled)
        {
            if (buttonStep.InvokeRequired)
            {
                buttonDelegate d = new buttonDelegate(RunButtons);
                buttonStep.Invoke(d, new object[] { enabled });
            }
            else
            {
                if (enabled)
                {
                    panelRunning.BackColor = Color.DarkGreen;
                    panelStopped.BackColor = Color.Red;
                    timer1.Stop();

                }
                else
                {
                    panelRunning.BackColor = Color.LimeGreen;
                    panelStopped.BackColor = Color.DarkRed;
                    timer1.Enabled = true;
                    timer1.Start();
                }
                buttonStep.Enabled = enabled;
                buttonStepOver.Enabled = enabled;
                buttonRun.Enabled = enabled;
                buttonPause.Enabled = !enabled;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _spManager.Dispose();
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            if (disassemblyView.Items.Count < 10)
            {
                MessageBox.Show("Must open your Arduino sketch first!");
                this.buttonLoad.Select();
                return;
            }
            
            RunButtons(false);
            buttonStep.Enabled = false;
            buttonStepOver.Enabled = false;
            buttonRun.Enabled = false;
            buttonPause.Enabled = false;
            Arduino.Startup(_spManager);
        }

        // Handles the "Stop Listening"-buttom click event
        private void btnStop_Click(object sender, EventArgs e)
        {
            _spManager.StopListening();
            RunButtons(true);
            buttonStep.Enabled = false;
            buttonStepOver.Enabled = false;
            buttonRun.Enabled = false;
            buttonPause.Enabled = false;
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


        private void buttonLoad_Click(object sender, EventArgs e)
        {
            if (Arduino.OpenFiles(this.sourceView,this.disassemblyView,this.varView,this.commsData))
            {
                this.labelSketch.Text = Arduino.FullFilename;
            }        
        }

        private void buttonReload_Click(object sender, EventArgs e)
        {
            Arduino.ReOpenFile();
            RunButtons(false);
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
            commsData.Visible = !commsData.Visible;
            if (commsData.Visible)
            {
                varView.Height = 350;
            }
            else
            {
                varView.Height = 500;
            }
            varView.Refresh();
        }

        private void buttonDiss_Click(object sender, EventArgs e)
        {
            disassemblyView.Visible = !disassemblyView.Visible;
            if (disassemblyView.Visible)
            {
                sourceView.Height = 420;
            }
            else
            {
                sourceView.Height = 620;
            }
            sourceView.Refresh();
           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (panelRunning.BackColor == Color.LimeGreen)
            {
                panelRunning.BackColor = Color.DarkGreen;
            }
            else
            {
                panelRunning.BackColor = Color.LimeGreen;
            }
        }

        private void buttonFunctions_Click(object sender, EventArgs e)
        {
            Arduino.FunctionList();
        }
        
        //public string InputText { get; private set; }

        //public bool InputReady { get; set; }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            string text = this.textBoxInput.Text;
            if (text.Length > 2)
            {
                //InputText = text;
                //InputReady = true;

                Arduino.NewCommand(text);
            }

        }
    }
}
