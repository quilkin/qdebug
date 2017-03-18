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
                        varView.Enabled = false;
                        return true;
                    }

                }
            }
            MessageBox.Show("Problem opening files");
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
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }

        private bool OpenDisassembly()
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
                return false;
            }
            // in case path includes spces, argument needs quotes
            elfPath = "\"" + elfPath + "\"";

            // Use ProcessStartInfo class
            // objdump - d progcount2.ino.elf > progcount2.ino.lss
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "avr-objdump.exe";
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            startInfo.Arguments = "-S -l -C -t " + elfPath;

            // disassembly file
            if (doObjDump(startInfo, ".lss") == false)
                return false;
            //if (ParseSourceInfo(ShortFilename) == false)
            //    return false;
            if (ParseDisassembly(ShortFilename + ".lss") == false)
                return false;

            // debug info file
            startInfo.Arguments = "-Wil " + elfPath;
            if (doObjDump(startInfo, ".dbg") == false)
                return false;
            if (ParseDebugInfo(ShortFilename + ".dbg") == false)
                return false;

            //// line number table
            //startInfo.Arguments = "-W " + elfPath;
            //if (doObjDump(startInfo, ".lin") == false)
            //    return false;

            //startInfo.FileName = "avr-nm.exe";
            //startInfo.Arguments = "-A -C -n -S  " + elfPath;
            //if (doObjDump(startInfo, ".sym") == false)
            //    return false;

            return true;

        }

 


    }
}