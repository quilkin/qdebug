using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SerialPortListener.Serial;
using System.IO;
using System.Diagnostics;

namespace SerialPortListener
{
    public partial class MainForm : Form
    {
        SerialPortManager _spManager;
        public MainForm()
        {
            InitializeComponent();

            UserInitialization();
        }


        private void UserInitialization()
        {
            _spManager = new SerialPortManager();
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

            // This application is connected to a GPS sending ASCCI characters, so data is converted to text
            string str = Encoding.ASCII.GetString(e.Data);
            tbData.AppendText(str);
            tbData.ScrollToCaret();

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
            string message = this.textToSend.Text;

            if (message.Equals("quit"))
            {
                //_continue = false;
            }
            else
            {
                _spManager.Send(message);
            }
        }

        private void portNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Arduino files|*.ino";
            ofd.Title = "Load Arduino Sketch";
            var dialogResult = ofd.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                sourceView.Items.Clear();
                listDisassembly.Items.Clear();
                int count = 1;
                foreach (var line in System.IO.File.ReadLines(ofd.FileName))
                {
                    // tabs ('\t') seem ignored in listview so replace with spaces
                    string untabbed = line.Replace("\t", "    ");
                    string trimmed = line.Trim();

                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = ""; // for breakpoint markers
                    if (trimmed.StartsWith("//") || trimmed.Length < 3)
                    {
                        // don't allow breakpoints on comments or empty lines
                        lvi.Checked = true;

                    }
                    if (trimmed.StartsWith("volatile") || trimmed.StartsWith("unsigned") || trimmed.StartsWith("void") || trimmed.StartsWith("char"))
                    {
                        // don't allow breakpoints on variable declarations
                        lvi.Checked = true;

                    }
                    if (trimmed.StartsWith("int") || trimmed.StartsWith("byte") || trimmed.StartsWith("long"))
                    {
                        // don't allow breakpoints on  variable declarations
                        lvi.Checked = true;

                    }
                    if (trimmed.StartsWith("float") || trimmed.StartsWith("double") || trimmed.StartsWith("bool"))
                    {
                        // don't allow breakpoints on  variable declarations
                        lvi.Checked = true;

                    }
                    lvi.SubItems.Add((count++).ToString());
                    lvi.SubItems.Add(untabbed);
                    sourceView.Items.Add(lvi);
                }
                // find the .elf file corresponding to this sketch
                string[] arduinoPaths = Directory.GetDirectories(Path.GetTempPath(), "arduino_build_*");
                string elfPath = null;
                foreach (string path in arduinoPaths)
                {
                    elfPath = path + "\\" + ofd.SafeFileName + ".elf";
                    if (File.Exists(elfPath))
                        break; 
                }
                // Use ProcessStartInfo class
                // objdump - d progcount2.ino.elf > progcount2.ino.lss
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.FileName = "objdump.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "-d -S -l -t " + elfPath;
                startInfo.RedirectStandardOutput = true;

                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        using (StreamWriter writer = File.CreateText(ofd.SafeFileName + ".lss"))
                        using (StreamReader reader = exeProcess.StandardOutput)
                        {
                            writer.AutoFlush = true;
                            for (;;)
                            {
                                string textLine = reader.ReadLine();
                                if (textLine == null)
                                    break;
                                writer.WriteLine(textLine);
                            }

                        }
                     
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                count = 1;
                foreach (var line in System.IO.File.ReadLines(ofd.SafeFileName + ".lss"))
                {
                        ListViewItem lvi = new ListViewItem();
                    lvi.Text = (count++).ToString();
                    lvi.SubItems.Add(line);
                    listDisassembly.Items.Add(lvi);
                }
            }
        }

    }
}
