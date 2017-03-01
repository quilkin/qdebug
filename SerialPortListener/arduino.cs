using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace ArdDebug
{
    class Arduino
    {

        private ListView source,disassembly;
        public String ShortFilename { private set; get; }
        public String FullFilename { private set; get; }

        private List<Breakpoint> Breakpoints = new List<Breakpoint>();

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
            bool found = false;
            foreach (string path in arduinoPaths)
            {
                elfPath = path + "\\" + ShortFilename + ".elf";
                if (File.Exists(elfPath))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                MessageBox.Show("No compiled files found. You may need to recomplie your project");
                return false;
            }
            // Use ProcessStartInfo class
            // objdump - d progcount2.ino.elf > progcount2.ino.lss
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "objdump.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-d -S -l -t -C " + elfPath;
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
            Breakpoint bp = null;
            
            foreach (string line in File.ReadLines(ShortFilename + ".lss"))
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = (count++).ToString();
                lvi.SubItems.Add(line.Replace("\t", "    "));
                disassembly.Items.Add(lvi);
                
                if (bp != null)
                {
                    // in the middle of breakpoint parse. Find the line containing te debug info (see below)
                    int colon = line.LastIndexOf(':');
                    if (colon >= 0)
                    {
                        string strPC = line.Substring(0, colon);
                        ushort progCounter;
                        if (ushort.TryParse(strPC, System.Globalization.NumberStyles.HexNumber, null, out progCounter))
                        {
                            bp.SetDetails(progCounter, line);
                            Breakpoints.Add(bp);
                            bp = null;
                        }
                        else
                        {
                            // might happen if there is a comment with a colon in it...
                            MessageBox.Show("confusing debug line? " + line);
                        };
                    }
                }
                if (line.Contains(ShortFilename))
                {
                    // there should be a debuggable line shortly after, e.g.
                    ////    C: \Users\chris\Documents\Arduino\sketch_feb26a / sketch_feb26a.ino:31
                    ////    float f;
                    ////    for (int index = 0; index < 5; index++)
                    ////         790:	1e 82           std Y+6, r1; 0x06  
                    // In this case, line 31 would be a breakpoint, pointing to address 0x0790
                    // We need to parse these lines (from filename line, as far as the line with the address ':')
                    int colon = line.LastIndexOf(':');
                    string strSourceLine = line.Substring(colon + 1);
                    int sourceLine = 0;
                    if (int.TryParse(strSourceLine, out sourceLine))
                    {
                        bp = new Breakpoint(ShortFilename, sourceLine);
                    }
                }
                
            }
            if (disassembly.Items.Count < 3)
            {
                MessageBox.Show("error with disaassembly listing");
                return false;
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
