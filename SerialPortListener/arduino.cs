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

        private ListView source, disassembly;
        public String ShortFilename { private set; get; }
        public String FullFilename { private set; get; }

        private List<Breakpoint> Breakpoints = new List<Breakpoint>();

        private Breakpoint currentBreakpoint = null;

        /// <summary>
        /// ascii chars used for interaction strings
        /// (can't use under 128 because normal Serial.print will use them)
        /// </summary>
        //public enum Chars : byte { PROGCOUNT_CHAR = 248, TARGET_CHAR, STEPPING_CHAR, ADDRESS_CHAR, DATA_CHAR, NO_CHAR, YES_CHAR };

        public class InteractionString
        {
            public int byteCount { get; set; }
            public int messageLength { get; set; }
            public ushort RecvdNumber { get; set; }

            public string Content { get;  set; }
            public InteractionString()
            {
                byteCount = 0;
                messageLength = 0;
                Content = string.Empty;
                RecvdNumber = 0;
            }
        }
        public InteractionString comString;


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
            int bpCount = 0;

            foreach (string line in File.ReadLines(ShortFilename + ".lss"))
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = (count++).ToString();
                lvi.SubItems.Add(line.Replace("\t", "    "));
                disassembly.Items.Add(lvi);

                if (bp != null)
                {
                    // in the middle of breakpoint parse. Find the line containing the debug info (see below)
                    int colon = line.LastIndexOf(':');
                    if (colon >= 0 && colon < 12) // to avoid colons in comments etc
                    {
                        string strPC = line.Substring(0, colon);
                        ushort progCounter;
                        
                        if (ushort.TryParse(strPC, System.Globalization.NumberStyles.HexNumber, null, out progCounter))
                        {
                            if (++bpCount > 1)// miss out lines before call to qdebug????
                            {
                                bp.SetDetails(progCounter, line);
                                Breakpoints.Add(bp);

                                // find the line in the source view and mark as appropriate
                                ListView.ListViewItemCollection sourceItems = source.Items;
                                ListViewItem sourceItem = sourceItems[bp.SourceLine - 1];
                                if (sourceItem != null)
                                {
                                    sourceItem.Checked = true;
                                }
                            }
                            bp = null;
                        }
                        else
                        {
                            // might happen .....
                            MessageBox.Show("confusing debug line? " + line);
                        };
                    }
                }
                if (line.Contains(ShortFilename) ) 
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
                MessageBox.Show("error with disassembly listing");
                return false;
            }
            return true;

        }

        /// <summary>
        /// use the list of breakpoint-able lines to determine the next place to 'single-step' to
        /// </summary>
        /// <param name="pc">pc at current breakpoint</param>
        /// <returns>next pc to pause execution</returns>
        public ushort FindNextPC(ushort pc)
        {
            // placeholder only for now
            return pc;

        }

        /// <summary>
        /// use the list of breakpoint-able lines to determine if we are at the next place to 'single-step' to
        /// </summary>
        /// <param name="pc">pc at current breakpoint</param>
        public bool AreWeThereYet(ushort pc)
        {
            foreach (Breakpoint bp in Breakpoints)
            {
                if (bp.ProgramCounter == pc)
                {
                    currentBreakpoint = bp;
                    return true;
                }
            }
            return false;
        }

        void UpdateCodeWindows(ushort pc) {
            int linecount = 0;
            if (disassembly != null && disassembly.Visible)
            {
                // find a line that starts with [whitespace][pc][:]
                ListView.ListViewItemCollection disItems = disassembly.Items;
                string pcStr = pc.ToString("x") + ':';
                linecount = 0;
                foreach (ListViewItem disItem in disItems)
                {
                    ++linecount;
                    string line = disItem.SubItems[1].Text;
                    if (line.Contains(pcStr))
                    {
                        int index = disItem.Index;
                        disassembly.Items[index].Selected = true;
                        disassembly.Select();
                        disassembly.EnsureVisible(index);
                        break;
                    }
                }
            }
            // find the line that contians the current breakpoint
            ListView.ListViewItemCollection sourceItems = source.Items;
            linecount = 0;
            foreach (ListViewItem sourceItem in sourceItems)
            {
                ++linecount;
                //string line = sourceItem.SubItems[2].Text;
                if (currentBreakpoint != null &&  currentBreakpoint.SourceLine == linecount)
                {
                    int index = sourceItem.Index;
                    source.Items[index].Selected = true;
                    source.Select();
                    source.EnsureVisible(index);
                    break;
                }
            }

        }

        public string newProgramCounter(InteractionString pcString)
        {
            ushort pc;
            if (pcString.Content.Length < 5)
                return null;
            if (ushort.TryParse(pcString.Content.Substring(1,4), System.Globalization.NumberStyles.HexNumber, null, out pc))
            {
                byte firstChar = (byte)pcString.Content[0];
                if (firstChar == 'S')
                //if (firstChar == (char)Chars.STEPPING_CHAR)
                    {
                    // single-stepping
                    if (AreWeThereYet(pc))
                    {
                        UpdateCodeWindows(pc);
                        //return Chars.YES_CHAR.ToString();
                        return "Y";
                    }
                    else
                        //return Chars.NO_CHAR.ToString();
                        return "N";

                    //nextpc = FindNextPC(pc);
                    //string newString = nextpc.ToString("X");
                    //return newString;
                }
                else
                {
                    UpdateCodeWindows(pc);
                }
            }
            return null;
        }


        public bool OpenFiles(ListView source, ListView disassembly)
        {
            this.source = source;
            this.disassembly = disassembly;
            if (openSourceFile())
            {
                if (OpenDisassembly())
                {

                    return true;
                }

            }
            MessageBox.Show("Problem opening files");
            return false;
        }
    }

}
