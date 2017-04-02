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

        public bool OpenFiles(ListView source, ListView disassembly, ListView variables, TextBox comms)
        {
            this.source = source;
            this.disassembly = disassembly;
            this.varView = variables;
            this.comms = comms;
            Breakpoints.Clear();
            if (OpenSourceFile())
            {
                if (parseSourceFile())
                {
                    if (OpenDisassembly())
                    {
                        source.Click -= Source_Click;
                        source.Click += Source_Click;
                        varView.FullRowSelect = true;
                        varView.Click -= Variable_Click;
                        varView.Click += Variable_Click;
                        //varView.ItemMouseHover -= VarView_ItemMouseHover;
                        //varView.ItemMouseHover += VarView_ItemMouseHover;
                        varView.Enabled = false;
                        return true;
                    }

                }
            }
            MessageBox.Show("Problem opening files");
            return false;
        }

        //private void VarView_ItemMouseHover(object sender, ListViewItemMouseHoverEventArgs e)
        //{
        //    if (varView.FocusedItem == null)
        //        return;
        //    if (varView.FocusedItem.Bounds.Contains(e.Item.Bounds))
        //    {
        //        MenuItem[] mItems =
        //        { new MenuItem("item1"),
        //          new MenuItem("item2") };
        //          ContextMenu cm = new ContextMenu(mItems);
        //           cm.Show(varView,Cursor.Position);
        //        }

        //}

        private bool OpenSourceFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Arduino files|*.ino;*.c;*.cpp|All files (*.*)|*.*";
            ofd.Title = "Load Arduino Sketch";
            var dialogResult = ofd.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                ShortFilename = ofd.SafeFileName;
                FullFilename = ofd.FileName;

                return true;
            }
            return false;
        }
        public bool ReOpenFile()
        {
            if (source == null)
            {
                MessageBox.Show("No existing sketch loaded");
                return false;
            }
            if (parseSourceFile())
            {
                if (OpenDisassembly())
                {
                    return true;
                }
            }
            MessageBox.Show("Problem opening files");
            return false;
        }

        private bool doObjDump(ProcessStartInfo startInfo, string fileExt)
        {

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    using (StreamWriter writer = File.CreateText(ShortFilename + fileExt))
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
                MessageBox.Show(ex.ToString());
                return false;
            }
            return true;
        }

        string FindElfPath()
        {
            // find the .elf file corresponding to this sketch
            // if more than one, use the latest

            string elfPath = null;
            string tryElfPath = null;

            if (ShortFilename.EndsWith(".ino"))
            {
                string[] arduinoPaths = Directory.GetDirectories(Path.GetTempPath(), "arduino_build_*");

                DateTime newestFile = DateTime.MinValue;
                foreach (string path in arduinoPaths)
                {
                    tryElfPath = path + "\\" + ShortFilename + ".elf";
                    if (File.Exists(tryElfPath))
                    {
                        DateTime fdate = File.GetLastWriteTime(tryElfPath);
                        if (fdate > newestFile)
                        {
                            newestFile = fdate;
                            elfPath = tryElfPath;
                        }
                    }
                }

            }
            else
            { // non-Arduino (Atmel Studio) project
                // debug files should be in ..\debug relative to source folder
                string path = FullFilename;

                int index = path.IndexOf("\\src\\");
                if (index > 0)
                {
                    //C: \Users\chris\Documents\Atmel Studio\7.0\MEGA_LED_EXAMPLE1\MEGA_LED_EXAMPLE1\src\mega_led_example.c

                    elfPath = path.Substring(0, index);
                }
                else
                {
                    index = path.IndexOf("\\" + ShortFilename);
                    elfPath = path.Substring(0, index);
                }
                int lastSlash = elfPath.LastIndexOf("\\");
                string nameRoot = elfPath.Substring(lastSlash + 1);

                elfPath = elfPath + "\\Debug\\" + nameRoot + ".elf";


            }
            if (elfPath == null)
            {
                MessageBox.Show("No compiled files found. You may need to recompile your project");
                return null;
            }
            // in case path includes spces, argument needs quotes
            elfPath = "\"" + elfPath + "\"";
            return elfPath;

        }

        private bool OpenDisassembly()
        {
            string elfPath = FindElfPath();
            if (elfPath == null)
                return false;
            // Use ProcessStartInfo class
            // objdump - d progcount2.ino.elf > progcount2.ino.lss
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "avr-objdump.exe";
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // see http://ccrma.stanford.edu/planetccrma/man/man1/avr-objdump.1.html
            startInfo.Arguments = "-S -l -C -t " + elfPath;

            // disassembly file 
            if (doObjDump(startInfo, ".lss") == false)
                return false;

            if (ParseDisassembly(ShortFilename + ".lss") == false)
                return false;
#if !__GDB__
            // debug info file 
            // see http://ccrma.stanford.edu/planetccrma/man/man1/readelf.1.html
            startInfo.Arguments = "-Wilo " + elfPath;
            if (doObjDump(startInfo, ".dbg") == false)
                return false;
            if (ParseDebugInfo(ShortFilename + ".dbg") == false)
                //return false;
                return true;
#endif
            return true;

        }
#if __GDB__

        public Process AvrGdb { get; private set; }
        private bool OpenGDB()
        {
            string elfPath = FindElfPath();
            if (elfPath == null)
                return false;

            elfPath = elfPath.Replace('\\', '/');
            AvrGdb = new Process();
            AvrGdb.StartInfo.CreateNoWindow = true;
            AvrGdb.StartInfo.UseShellExecute = false;
            AvrGdb.StartInfo.FileName = "avr-gdb.exe";
            AvrGdb.StartInfo.RedirectStandardOutput = true;
            AvrGdb.StartInfo.RedirectStandardInput = true;
            AvrGdb.StartInfo.RedirectStandardError = true;
            AvrGdb.ErrorDataReceived += Avr_gdb_ErrorDataReceived;
            
            AvrGdb.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            AvrGdb.StartInfo.Arguments =  elfPath;
            AvrGdb.OutputDataReceived += AvrGdb_OutputDataReceived;
            AvrGdb.Start();
            AvrGdb.BeginOutputReadLine();
            AvrGdb.BeginErrorReadLine();
            //AvrGdb.WaitForExit();
            return true;
        }
        static StringBuilder outData = new StringBuilder();
        static StringBuilder errData = new StringBuilder();
        private void AvrGdb_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (outData)
            {
                string reply = e.Data;
                if (e.Data.Contains("Reading symbols"))
                {
                    UpdateCommsBox(reply, false);
                    AvrGdb.StandardInput.WriteLine("set serial baud 57600");
                    AvrGdb.StandardInput.WriteLine("target remote com7");
                }
                else
                    UpdateCommsBox(reply, false);
            }

        }

        private void Avr_gdb_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (errData)
            {
                UpdateCommsBox("***" + e.Data + "***", true);

            }
        }
        // typical startup conversation:
        // (gdb) target remote com7
        // Remote debugging using com7
        // 0x00001022 in myAdd(a= 20771, b= 5, c= 1) at../src/main.c:55
        // 55              for (index = 0; index< 5; index++)
        // (gdb) step
        // 57                      a += b;
        // (gdb) step
        // 58                      a += c;
        // (gdb)
        public bool GDB_write(string line)
        {
            string reply = "";
            try
            {
                UpdateCommsBox(line, true);
                AvrGdb.StandardInput.WriteLine(line);
                return true;

            }
            catch (Exception ex )
            {
                MessageBox.Show(ex.Message);
                return false;
            }

        }

#endif
    }
}