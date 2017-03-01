using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace SerialPortListener
{
    class Arduino
    {

        private ListView source,disassembly;
        public String ShortFilename { private set; get; }
        public String FullFilename { private set; get; }

        private bool openSourceFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Arduino files|*.ino";
            ofd.Title = "Load Arduino Sketch";
            var dialogResult = ofd.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                ShortFilename = ofd.SafeFileName;
                FullFilename = ofd.FileName;
                source.Items.Clear();
                disassembly.Items.Clear();
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
                    source.Items.Add(lvi);
                }
                return true;
            }
            return false;
        }

        private bool OpenDisassembly()
        {
            // find the .elf file corresponding to this sketch
            string[] arduinoPaths = Directory.GetDirectories(Path.GetTempPath(), "arduino_build_*");
            string elfPath = null;
            foreach (string path in arduinoPaths)
            {
                elfPath = path + "\\" + ShortFilename + ".elf";
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
            startInfo.Arguments = "-d -S -l -t -g -C " + elfPath;
            startInfo.RedirectStandardOutput = true;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    using (StreamWriter writer = File.CreateText(ShortFilename + ".lss"))
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            int count = 1;
            foreach (var line in System.IO.File.ReadLines(ShortFilename + ".lss"))
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = (count++).ToString();
                lvi.SubItems.Add(line);
                disassembly.Items.Add(lvi);
            }
            return true;

        }


        public bool OpenFiles(ListView source, ListView disassembly)
        {
            this.source = source;
            this.disassembly = disassembly;
            if (openSourceFile())
            {
                if (OpenDisassembly())
                    return true;

            }
            MessageBox.Show("Problem opening files");
            return false;
        }
    }

}
