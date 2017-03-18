﻿using System;
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
        public Arduino(MainForm form)
        {
            this.stopLED = form.LedStopped;
            this.runLED = form.LedStopped;
            this.btnPause = form.Pause;
            this.btnRun = form.Run;
            this.btnStart = form.Start;
            this.btnStep= form.Step;
            this.btnStepOver = form.StepOver;
            GUI = form;
            GUI.RunButtons(false);
            

        }
        private MainForm GUI;
        private ListView source, disassembly, varView;
        private TextBox comms;
        public String ShortFilename { private set; get; }
        public String FullFilename { private set; get; }

        private List<Breakpoint> Breakpoints = new List<Breakpoint>();
        private List<Variable> Variables = new List<Variable>();
        private List<VariableType> VariableTypes = new List<VariableType>();
        /// <summary>
        /// variable names found by parsing the assembler file, included in our own source files
        /// Just used for comaprison when parsing full list of variables which contain eveything
        /// </summary>
        private List<String> MyVariables = new List<String>();

        /// <summary>
        /// where program counter is currently sitting
        /// </summary>
        private Breakpoint currentBreakpoint = null;
        /// <summary>
        /// next place to stop if we skip over a function call
        /// </summary>
        private Breakpoint nextBreakpoint = null;
        private Panel stopLED, runLED;
        private String comString = String.Empty;
        private Button btnStart, btnStep, btnStepOver, btnRun, btnPause;

        Serial.SerialPortManager spmanager;

        public bool pauseReqd { set; get; }
        /// <summary>
        /// ascii chars used for interaction strings
        /// (can't use under 128 because normal Serial.print will use them)
        /// </summary>
        //public enum Chars : byte { PROGCOUNT_CHAR = 248, TARGET_CHAR, STEPPING_CHAR, ADDRESS_CHAR, DATA_CHAR, NO_CHAR, YES_CHAR };

        // varaible types built in to gcc or typedefs defined by Arduino
        public readonly IList<string> ReservedTypeWords = new List<string> {
            "signed",
            "char",
            "unsigned",
            "int",
            "float",
            "double",
            "long",
            "volatile"
        };
        public  IList<string> TypedefWords = new List<string> {
            "word",
            "boolean",
            "bool",
            "byte",
            "uint8_t",
            "uint16_t",
            "uint32_t",
            "uint64_t"
        };

        System.Drawing.Color sourceLineColour = System.Drawing.Color.AliceBlue;
        System.Drawing.Color breakpointColour = System.Drawing.Color.Red;
        System.Drawing.Color breakpointHitColour = System.Drawing.Color.Orange;

        //private void RunButtons(bool enabled)
        //{
        //    btnStep.Enabled =  enabled;
        //    btnStepOver.Enabled = enabled;
        //    btnRun.Enabled = enabled;
        //}
        public void Startup(Serial.SerialPortManager _spmanager)
        {
            
       //     Leds(true);
            //btnPause.Enabled = false;
            GUI.RunButtons(false);
            this.spmanager = _spmanager;
            spmanager.StartListening();  // this will reset the Arduino
            comString = null;
            currentBreakpoint = null;
            nextBreakpoint = null;
            Send("startup\n");
            // might take  a while for a reset etc
            comString = ReadLine(5000);
            if (comString.Length < 2)
            {
                MessageBox.Show("Cannot find device, please check connection");
                GUI.RunButtons(true);
                return;
            }
            GetVariables();
            SingleStep();
    //        Leds(false);
            GUI.RunButtons(true);
            varView.Enabled = true;
        }

        //private void Leds(bool running)
        //{
        //    if (running)
        //    {
        //        runLED.BackColor = System.Drawing.Color.LimeGreen;
        //        stopLED.BackColor = System.Drawing.Color.DarkRed;
        //    }
        //    else
        //    {
        //        runLED.BackColor = System.Drawing.Color.DarkGreen;
        //        stopLED.BackColor = System.Drawing.Color.Red;
        //    }
        //}
        public void SingleStep()
        {
            String stepStr = "P0000\n";
            //bool continuing = true;
            Send(stepStr);
            //Leds(true);
            GUI.RunButtons(false);
            // make this a backgroud task so that we can abort, if required, with the 'pause' button
            _Running = new BackgroundWorker();
            _Running.WorkerSupportsCancellation = true;
            _Running.DoWork += new DoWorkEventHandler((state, args) =>
            {
                do
                {
                    comString = ReadLine();
                    if (comString.Length == 0)
                    {
                        MessageBox.Show("timeout in single step");
                        break;
                    }
                    if (_Running.CancellationPending)
                    {
                        // force device to think it's reached its target
                        Send("Y");
                        break;
                    }
                    char firstChar = comString[0];
                    if (firstChar == 'P')
                    {
                        // moved to where we need; this is our 'step'
                        //continuing = false;
                        newProgramCounter();
                        GetVariables();
                        break;
                    }
                    else if (firstChar == 'S')
                    {
                        string reply = newProgramCounter();
                        Send(reply);

                    }
                }
                while (true);
                //Leds(false);
                GUI.RunButtons(true);
            });
            _Running.RunWorkerAsync();

        }

        /// <summary>
        /// need to set a temporary breakpoint, avoiding any function calls
        /// </summary>
        public void StepOver()
        {
            //Leds(true);
            GUI.RunButtons(false);
            if (nextBreakpoint != null)
            {
                //// set up a 'temporary' breakpoint
                //Breakpoint bp = new Breakpoint("", 0);
                //bp.ProgramCounter = nextBreakpoint;
                GoToBreakpoint(nextBreakpoint);
            }
            else
            {
                MessageBox.Show("Error, no suitable step found");
            }
            //Leds(false);
            GUI.RunButtons(true);
        }

        public void FindBreakpoint()
        {
            bool bpFound = false;
            foreach (Breakpoint bp in Breakpoints)
            {
                if (bp.Manual)
                {
                    bpFound = true;
                    GoToBreakpoint(bp);
                    break;
                }
            }
            if (!bpFound)
            {
                MessageBox.Show("No breakpoints set!");
                return;
            }
        }
        public BackgroundWorker _Running = null;
        public void GoToBreakpoint(Breakpoint bp)
        {
            String sendStr = "P" + bp.ProgramCounter.ToString("X4") + "\n";
            Send(sendStr);
           // comString = ReadLine();     // should be 'Txxxx' - echo adrees that was sent.
                                        // now wait for the bp to be hit.....or a pause command
            _Running = new BackgroundWorker();
            _Running.WorkerSupportsCancellation = true;
            bool pauseReqd = false;
            //Leds(true);
            GUI.RunButtons(false);
            _Running.DoWork += new DoWorkEventHandler((state, args) =>
            {
                do
                {
                    comString = ReadLine(); // waiting for "?"...DoWeNeedToStop?
                    if (comString.Length > 0)
                    {
                        if (comString[0] == '?')
                        {
                            //check for pause button just pressed
                            if (_Running.CancellationPending)
                            {
                                Send("X");        // instruction to force targetPC to be equal to current PC
                                pauseReqd = true;
                                break;
                            }
                            else
                            {
                                Send("N");
                            }
                        }
                        else // reached predefined breakpoint
                            break;
                    }
                } while (true);
                Thread.Sleep(100); //allow time for target to catch up
                GetVariables();
                if (pauseReqd)
                    SingleStep();  // not sure why this is needed; variables don't update otherwise
                UpdateVariableWindow();
                UpdateCodeWindows(bp.ProgramCounter);
                if (!pauseReqd)
                    MarkBreakpointHit(bp);
                //Leds(false);
                GUI.RunButtons(true);
            });
            _Running.RunWorkerAsync();
        }
        
    

        public void GetVariables()
        {
            //Leds(true);
            GUI.RunButtons(false);
            String sendStr = "PFFFF\n";  // todo: can get rid of this command to save time & code space
            Send(sendStr);
            System.Threading.Thread.Sleep(100);
            foreach (Variable var in Variables)
            {
                //GetVariable(var);
                var.GetValue();
            }
            UpdateVariableWindow();
            //Leds(false);
            GUI.RunButtons(true);
        }

        private int expandedTags = 0;
        private List<Object> viewTags = new List<Object>();
        public void Variable_Click(object sender, EventArgs e)
        {
            if (varView.SelectedItems.Count == 0)
                return;

            ListViewItem clicked = varView.SelectedItems[0];
            string itemName = clicked.Name;
            Variable var = Variables.Find(x => x.Name == itemName);
            if (var == null)
                return;
            if (var.Type.BaseType == null)
                return;

            if (clicked.Tag==null)
            {
                // expand item to show contents of this compound variable
                int size = var.Type.Size;
                int index = clicked.Index;
                ushort addr = var.Address;
                ++expandedTags;

                if (var.Type.Name == "pointer")
                {
                    // get the value pointed to
                    Variable pointerElement = new Variable(this);
                    Variable indirect = new Variable(this);

                    pointerElement.Address = addr;
                    pointerElement.Type = var.Type.BaseType;
                    pointerElement.Name = "* " + itemName ;
                    pointerElement.GetValue();
                    // now get the indirected value
                    int indAddr = -1;
                    int.TryParse(pointerElement.currentValue, out indAddr);
                    if (indAddr >= 0)
                    {
                        indirect.Address = (ushort)indAddr;
                        indirect.Type = var.Type.BaseType;
                        indirect.Name = var.Type.Name;
                        // temporarily add the expanded item to the list of variables, so it will be updated during single-stepping etc.
                        Variables.Add(indirect);
                        indirect.GetValue();

                    }
                    string[] items = { "  " + pointerElement.Name, var.Type.BaseType.Name, "0x"+indAddr.ToString("X4"), indirect.currentValue };
                    ListViewItem arrayItem = new ListViewItem(items);
                    arrayItem.Name = pointerElement.Name;
                    arrayItem.BackColor = System.Drawing.Color.Azure;
                    arrayItem.Tag = clicked.Tag = expandedTags;
                    viewTags.Add(clicked.Tag);
                    varView.Items.Insert(++index, arrayItem);
                    //clicked.Tag = "expandedPointer";
                }
                else
                {
                    // get the individual array elements from memory
                    for (int i = 0; i < size; i++)
                    {
                        Variable arrayElement = new Variable(this);
                        
                        arrayElement.Address = addr;
                        arrayElement.Type = var.Type.BaseType;
                        arrayElement.Name = itemName + '[' + i + ']';
                        // temporarily add the expanded item to the list of variables, so it will be updated during single-stepping etc.
                        Variables.Add(arrayElement);
                        //GetVariable(arrayElement);
                        arrayElement.GetValue();

                        string[] items = { "  " + arrayElement.Name, var.Type.BaseType.Name, addr.ToString("X4"), arrayElement.currentValue };
                        ListViewItem arrayItem = new ListViewItem(items);
                        arrayItem.Name = arrayElement.Name;
                        arrayItem.BackColor = System.Drawing.Color.Azure;
                        arrayItem.Tag = clicked.Tag = expandedTags;
                        viewTags.Add(clicked.Tag);
                        varView.Items.Insert(++index, arrayItem);
                        addr += (ushort)var.Type.BaseType.Size;
                    }
                    //clicked.Tag = "expandedArray";
                }
                UpdateVariableWindow();
                //clicked.Tag = "expanded";
            }
            else
            {
                // unexpand the added items, and remove the extra temporary variables
                ListView.ListViewItemCollection items = varView.Items;
                foreach (ListViewItem item  in items)
                {
                    if (item.Tag == clicked.Tag)
                    {
                        string varName = item.Name;
                        Variable arrayVar = Variables.Find(x => x.Name == varName);
                        Variables.Remove(arrayVar);
                        varView.Items.Remove(item);
                    }
                }
                viewTags.Remove(clicked.Tag);
                clicked.Tag = null;
             }

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
        delegate void varViewDelegate();
        void UpdateVariableWindow()
        {
            if (varView.InvokeRequired)
            {
                varViewDelegate d = new varViewDelegate(UpdateVariableWindow);
                varView.Invoke(d, new object[] { });
            }
            else
            {

                foreach (Variable var in Variables)
                {
                    ListView.ListViewItemCollection vars = varView.Items;
                    ListViewItem[] lvis = vars.Find(var.Name,false);
                    if (lvis == null || lvis.Length == 0)
                        continue;
                    ListViewItem lvi = lvis[0];
                                   
                    lvi.SubItems[2].Text = "0x" + var.Address.ToString("X4");
                    if (var.Type.BaseType == null)
                    {
                        lvi.SubItems[3].Text = var.currentValue;
                    }
                    else if (var.Type.Name == "array")
                    {
                        lvi.SubItems[3].Text = "....";
                    }
                    else if (var.Type.Name == "pointer")
                    {
                        // show addreess pointed to (in hex)
                        int addr=-1;
                        int.TryParse(var.currentValue, out addr);
                        if (addr >= 0)
                        {
                            lvi.SubItems[3].Text = "0x" + addr.ToString("X4");
                        }
            
                    }


                }
            }
        }
        void UpdateCodeWindows(ushort pc)
        {
            UpdateDisassembly(pc);
            UpdateSource();
        }

        delegate void updateDissDelegate(ushort pc);
        void UpdateDisassembly(ushort pc)
        {
            if (disassembly.InvokeRequired)
            {
                updateDissDelegate d = new updateDissDelegate(UpdateDisassembly);
                disassembly.Invoke(d, new object[] { pc });
            }
            else
            {
                int linecount = 0;
                nextBreakpoint = null;
                if (disassembly != null && disassembly.Visible)
                {
                    // find a line that starts with [whitespace][pc][:]
                    ListView.ListViewItemCollection disItems = disassembly.Items;
                    string pcStr = pc.ToString("x") + ':';
                    linecount = 0;
                    bool currentLineFound = false;
                    int nextSourceLine = 0;
                    foreach (ListViewItem disItem in disItems)
                    {
                        ++linecount;
                        string line = disItem.SubItems[1].Text;
                        if (line.Contains(pcStr) && !currentLineFound)
                        {
                            int index = disItem.Index;
                            disassembly.Items[index].Selected = true;
                            disassembly.Select();
                            disassembly.EnsureVisible(index);
                            currentLineFound = true;
                        }
                        else if (currentLineFound)
                        {
                            // searching for next source line that we'll need for 'step over' instruction
                            //  i.e. avoiding any function calls here
                            //char[] delimiters = new char[] { ' ', ':' };
                            int starIndex = line.IndexOf(" *** ");
                            if (starIndex > 0)
                            {
                                // this is our added ref to the source code.
                                string sourceLine = line.Substring(starIndex + 5);
                                // Try to find corresponding source line in source window
                                nextSourceLine = FindSourceFromDisassembly(sourceLine);
                                if (nextSourceLine > 0)
                                {
                                    // this will be the next bp for 'step over'
                                    foreach (Breakpoint bp in Breakpoints)
                                    {
                                        if (bp.SourceLine == nextSourceLine)
                                        {
                                            nextBreakpoint = bp;
                                            break;
                                        }
                                    }
                                    // done searching
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        delegate void updateSourceDelegate();
        void UpdateSource()
        {
            if (source.InvokeRequired)
            {
                updateSourceDelegate d = new updateSourceDelegate(UpdateSource);
                source.Invoke(d, new object[] { });
            }
            else
            {
                if (source == null)
                    return;
                // find the line that contains the current breakpoint
                ListView.ListViewItemCollection sourceItems = source.Items;
                int linecount = 0;
                bool lineFound = false;
                foreach (ListViewItem sourceItem in sourceItems)
                {
                    if (sourceItem.BackColor == breakpointHitColour)
                    {
                        sourceItem.BackColor = breakpointColour;
                    }

                    ++linecount;
                    if (currentBreakpoint != null && currentBreakpoint.SourceLine == linecount)
                    {
                        int index = sourceItem.Index;
                        source.Items[index].Selected = true;
                        source.Select();
                        source.EnsureVisible(index);
                        lineFound = true;
                        break;
                    }
                }
                if (!lineFound)
                {
                    source.Items[0].Selected = true;
                    source.EnsureVisible(0);
                }
            }

        }



        private void Source_Click(object sender, EventArgs e)
        {

            if (source.SelectedItems.Count == 0)
                return;
            ListViewItem lvi = source.SelectedItems[0];
            ListView.ListViewItemCollection sourceItems = source.Items;

            // clear any previous breakpoints
            foreach (Breakpoint bp in Breakpoints)
            {
                if (bp.Manual)
                {
                    bp.Manual = false;
                    ListViewItem bpItem = sourceItems[bp.SourceLine - 1];
                     bpItem.BackColor = sourceLineColour;

                }
            }

            // set a new breakpoint (only 1 bp allowed for now....)

            if (lvi.BackColor == sourceLineColour) // i.e. breakpoint possible on this line
            {
                // now need to find the correct (single-step) breakpoint and make it manual 
                foreach (Breakpoint bp in Breakpoints)
                {
                    if (bp.SourceLine == lvi.Index + 1)
                    {

                            bp.Manual = true;
                            lvi.Selected = false;
                            lvi.BackColor = breakpointColour;

                    }
                }
            }
            //else if (lvi.BackColor == breakpointColour || lvi.BackColor == breakpointHitColour) // i.e. breakpoint possible on this line
            //{
            //    // now need to find the correct (single-step) breakpoint and make it not manual 
            //    foreach (Breakpoint bp in Breakpoints)
            //    {
            //        if (bp.SourceLine == lvi.Index + 1)
            //        {
  
            //                // undoing a previous breakpoint
            //                bp.Manual = false;
            //                lvi.Selected = false;
            //                lvi.BackColor = sourceLineColour;
            //         }
            //    }
            //}


        }
        delegate void bpDelegate(Breakpoint bp);
        private void MarkBreakpointHit(Breakpoint bp)
        {
            if (source.InvokeRequired)
            {
                bpDelegate d = new bpDelegate(MarkBreakpointHit);
                source.Invoke(d, new object[] { bp });
            }
            else
            {
                if (bp.SourceLine > 1)
                {
                    int index = bp.SourceLine - 1;
                    ListView.ListViewItemCollection sourceItems = source.Items;
                    ListViewItem item = sourceItems[index];
                    item.BackColor = breakpointHitColour;
                    source.EnsureVisible(index);

                    if (source.SelectedItems.Count == 1)
                    {
                        item = source.SelectedItems[0];
                        item.Selected = false;
                    }
                }
            }
        }


        public string newProgramCounter()
        {
            ushort pc;
            if (comString.Length < 5)
                return null;
            if (ushort.TryParse(comString.Substring(1,4), System.Globalization.NumberStyles.HexNumber, null, out pc))
            {
                byte firstChar = (byte)comString[0];
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

                }
                else
                {
                    UpdateCodeWindows(pc);
                }
            }
            return null;
        }

 
        
    }

}
