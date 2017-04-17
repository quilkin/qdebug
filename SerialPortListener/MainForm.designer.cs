namespace ArdDebug
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.serialSettingsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.portNameComboBox = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonScan = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.commsData = new System.Windows.Forms.TextBox();
            this.buttonPause = new System.Windows.Forms.Button();
            this.buttonRun = new System.Windows.Forms.Button();
            this.buttonStep = new System.Windows.Forms.Button();
            this.varView = new System.Windows.Forms.ListView();
            this.columnExpander = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.labelSketch = new System.Windows.Forms.Label();
            this.sourceView = new System.Windows.Forms.ListView();
            this.columnBP = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnLineNum = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnLine = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonLoad = new System.Windows.Forms.Button();
            this.disassemblyView = new System.Windows.Forms.ListView();
            this.columnNum = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnText = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonStepOver = new System.Windows.Forms.Button();
            this.buttonComms = new System.Windows.Forms.Button();
            this.buttonDiss = new System.Windows.Forms.Button();
            this.panelStopped = new System.Windows.Forms.Panel();
            this.panelRunning = new System.Windows.Forms.Panel();
            this.buttonReload = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.buttonFunctions = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.textBoxInput = new System.Windows.Forms.TextBox();
            this.buttonSend = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.serialSettingsBindingSource)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // portNameComboBox
            // 
            this.portNameComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.serialSettingsBindingSource, "PortName", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.portNameComboBox.FormattingEnabled = true;
            this.portNameComboBox.Location = new System.Drawing.Point(6, 19);
            this.portNameComboBox.Name = "portNameComboBox";
            this.portNameComboBox.Size = new System.Drawing.Size(61, 21);
            this.portNameComboBox.TabIndex = 8;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonScan);
            this.groupBox1.Controls.Add(this.portNameComboBox);
            this.groupBox1.Location = new System.Drawing.Point(3, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(78, 76);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection";
            // 
            // buttonScan
            // 
            this.buttonScan.Location = new System.Drawing.Point(6, 46);
            this.buttonScan.Name = "buttonScan";
            this.buttonScan.Size = new System.Drawing.Size(61, 23);
            this.buttonScan.TabIndex = 17;
            this.buttonScan.Text = "Re-scan";
            this.buttonScan.UseVisualStyleBackColor = true;
            this.buttonScan.Click += new System.EventHandler(this.buttonScan_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(672, 6);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(81, 23);
            this.btnStop.TabIndex = 12;
            this.btnStop.Text = "Stop / Close";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(283, 4);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(76, 23);
            this.btnStart.TabIndex = 12;
            this.btnStart.Text = "Start / Reset";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // commsData
            // 
            this.commsData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.commsData.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.commsData.Location = new System.Drawing.Point(514, 389);
            this.commsData.Multiline = true;
            this.commsData.Name = "commsData";
            this.commsData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.commsData.Size = new System.Drawing.Size(227, 128);
            this.commsData.TabIndex = 13;
            // 
            // buttonPause
            // 
            this.buttonPause.Location = new System.Drawing.Point(607, 5);
            this.buttonPause.Name = "buttonPause";
            this.buttonPause.Size = new System.Drawing.Size(59, 23);
            this.buttonPause.TabIndex = 15;
            this.buttonPause.Text = "Pause";
            this.buttonPause.UseVisualStyleBackColor = true;
            this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
            // 
            // buttonRun
            // 
            this.buttonRun.Location = new System.Drawing.Point(490, 5);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(55, 23);
            this.buttonRun.TabIndex = 18;
            this.buttonRun.Text = "Run";
            this.buttonRun.UseVisualStyleBackColor = true;
            this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
            // 
            // buttonStep
            // 
            this.buttonStep.Location = new System.Drawing.Point(365, 3);
            this.buttonStep.Name = "buttonStep";
            this.buttonStep.Size = new System.Drawing.Size(49, 23);
            this.buttonStep.TabIndex = 17;
            this.buttonStep.Text = "Step";
            this.buttonStep.UseVisualStyleBackColor = true;
            this.buttonStep.Click += new System.EventHandler(this.buttonStep_Click);
            // 
            // varView
            // 
            this.varView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.varView.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.varView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnExpander,
            this.columnName,
            this.columnValue});
            this.varView.GridLines = true;
            this.varView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.varView.Location = new System.Drawing.Point(519, 45);
            this.varView.MultiSelect = false;
            this.varView.Name = "varView";
            this.varView.Size = new System.Drawing.Size(227, 310);
            this.varView.TabIndex = 19;
            this.varView.UseCompatibleStateImageBehavior = false;
            this.varView.View = System.Windows.Forms.View.Details;
            // 
            // columnExpander
            // 
            this.columnExpander.Text = "";
            this.columnExpander.Width = 15;
            // 
            // columnName
            // 
            this.columnName.Text = "Variable";
            this.columnName.Width = 80;
            // 
            // columnValue
            // 
            this.columnValue.Text = "Value";
            this.columnValue.Width = 125;
            // 
            // labelSketch
            // 
            this.labelSketch.AutoSize = true;
            this.labelSketch.Location = new System.Drawing.Point(0, 28);
            this.labelSketch.Name = "labelSketch";
            this.labelSketch.Size = new System.Drawing.Size(64, 13);
            this.labelSketch.TabIndex = 20;
            this.labelSketch.Text = "Your sketch";
            // 
            // sourceView
            // 
            this.sourceView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnBP,
            this.columnLineNum,
            this.columnLine});
            this.sourceView.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sourceView.FullRowSelect = true;
            this.sourceView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.sourceView.Location = new System.Drawing.Point(3, 48);
            this.sourceView.MultiSelect = false;
            this.sourceView.Name = "sourceView";
            this.sourceView.Size = new System.Drawing.Size(502, 560);
            this.sourceView.TabIndex = 22;
            this.sourceView.UseCompatibleStateImageBehavior = false;
            this.sourceView.View = System.Windows.Forms.View.Details;
            // 
            // columnBP
            // 
            this.columnBP.Text = "BP";
            this.columnBP.Width = 19;
            // 
            // columnLineNum
            // 
            this.columnLineNum.Text = "Line";
            this.columnLineNum.Width = 5;
            // 
            // columnLine
            // 
            this.columnLine.Text = "Instructions";
            this.columnLine.Width = 1000;
            // 
            // buttonLoad
            // 
            this.buttonLoad.Location = new System.Drawing.Point(3, 2);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(117, 23);
            this.buttonLoad.TabIndex = 23;
            this.buttonLoad.Text = "Load New Sketch";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
            // 
            // disassemblyView
            // 
            this.disassemblyView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.disassemblyView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnNum,
            this.columnText});
            this.disassemblyView.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.disassemblyView.FullRowSelect = true;
            this.disassemblyView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.disassemblyView.HideSelection = false;
            this.disassemblyView.Location = new System.Drawing.Point(3, 399);
            this.disassemblyView.MultiSelect = false;
            this.disassemblyView.Name = "disassemblyView";
            this.disassemblyView.Size = new System.Drawing.Size(502, 209);
            this.disassemblyView.TabIndex = 24;
            this.disassemblyView.UseCompatibleStateImageBehavior = false;
            this.disassemblyView.View = System.Windows.Forms.View.Details;
            this.disassemblyView.Visible = false;
            // 
            // columnNum
            // 
            this.columnNum.Text = "#";
            this.columnNum.Width = 1;
            // 
            // columnText
            // 
            this.columnText.Text = "Disassembly";
            this.columnText.Width = 1000;
            // 
            // buttonStepOver
            // 
            this.buttonStepOver.Location = new System.Drawing.Point(420, 4);
            this.buttonStepOver.Name = "buttonStepOver";
            this.buttonStepOver.Size = new System.Drawing.Size(64, 23);
            this.buttonStepOver.TabIndex = 25;
            this.buttonStepOver.Text = "StepOver";
            this.buttonStepOver.UseVisualStyleBackColor = true;
            this.buttonStepOver.Click += new System.EventHandler(this.buttonStepOver_Click);
            // 
            // buttonComms
            // 
            this.buttonComms.Location = new System.Drawing.Point(87, 32);
            this.buttonComms.Name = "buttonComms";
            this.buttonComms.Size = new System.Drawing.Size(131, 23);
            this.buttonComms.TabIndex = 26;
            this.buttonComms.Text = "Show/Hide Comms";
            this.buttonComms.UseVisualStyleBackColor = true;
            this.buttonComms.Click += new System.EventHandler(this.buttonComms_Click);
            // 
            // buttonDiss
            // 
            this.buttonDiss.Location = new System.Drawing.Point(87, 61);
            this.buttonDiss.Name = "buttonDiss";
            this.buttonDiss.Size = new System.Drawing.Size(129, 23);
            this.buttonDiss.TabIndex = 27;
            this.buttonDiss.Text = "Show/Hide Disassembly";
            this.buttonDiss.UseVisualStyleBackColor = true;
            this.buttonDiss.Click += new System.EventHandler(this.buttonDiss_Click);
            // 
            // panelStopped
            // 
            this.panelStopped.AccessibleName = " disassemblyView.Visible = !disassemblyView.Visible;";
            this.panelStopped.BackColor = System.Drawing.Color.Red;
            this.panelStopped.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelStopped.Location = new System.Drawing.Point(551, 6);
            this.panelStopped.Name = "panelStopped";
            this.panelStopped.Size = new System.Drawing.Size(22, 22);
            this.panelStopped.TabIndex = 28;
            // 
            // panelRunning
            // 
            this.panelRunning.AccessibleName = " disassemblyView.Visible = !disassemblyView.Visible;";
            this.panelRunning.BackColor = System.Drawing.Color.DarkGreen;
            this.panelRunning.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelRunning.Location = new System.Drawing.Point(579, 5);
            this.panelRunning.Name = "panelRunning";
            this.panelRunning.Size = new System.Drawing.Size(22, 22);
            this.panelRunning.TabIndex = 29;
            // 
            // buttonReload
            // 
            this.buttonReload.Location = new System.Drawing.Point(126, 3);
            this.buttonReload.Name = "buttonReload";
            this.buttonReload.Size = new System.Drawing.Size(122, 23);
            this.buttonReload.TabIndex = 30;
            this.buttonReload.Text = "ReLoad Sketch";
            this.buttonReload.UseVisualStyleBackColor = true;
            this.buttonReload.Click += new System.EventHandler(this.buttonReload_Click);
            // 
            // buttonFunctions
            // 
            this.buttonFunctions.Location = new System.Drawing.Point(87, 8);
            this.buttonFunctions.Name = "buttonFunctions";
            this.buttonFunctions.Size = new System.Drawing.Size(131, 23);
            this.buttonFunctions.TabIndex = 31;
            this.buttonFunctions.Text = "Function List";
            this.buttonFunctions.UseVisualStyleBackColor = true;
            this.buttonFunctions.Click += new System.EventHandler(this.buttonFunctions_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.buttonFunctions);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.buttonComms);
            this.panel1.Controls.Add(this.buttonDiss);
            this.panel1.Location = new System.Drawing.Point(519, 523);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(222, 89);
            this.panel1.TabIndex = 32;
            // 
            // textBoxInput
            // 
            this.textBoxInput.Location = new System.Drawing.Point(520, 361);
            this.textBoxInput.Name = "textBoxInput";
            this.textBoxInput.Size = new System.Drawing.Size(133, 20);
            this.textBoxInput.TabIndex = 33;
            // 
            // buttonSend
            // 
            this.buttonSend.Location = new System.Drawing.Point(664, 361);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(75, 23);
            this.buttonSend.TabIndex = 34;
            this.buttonSend.Text = "Send";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(757, 615);
            this.Controls.Add(this.buttonSend);
            this.Controls.Add(this.textBoxInput);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonReload);
            this.Controls.Add(this.panelRunning);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.panelStopped);
            this.Controls.Add(this.buttonStepOver);
            this.Controls.Add(this.buttonRun);
            this.Controls.Add(this.commsData);
            this.Controls.Add(this.disassemblyView);
            this.Controls.Add(this.buttonStep);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.buttonPause);
            this.Controls.Add(this.sourceView);
            this.Controls.Add(this.labelSketch);
            this.Controls.Add(this.varView);
            this.Controls.Add(this.btnStart);
            this.Name = "MainForm";
            this.Text = "Arduino Debugger";
            ((System.ComponentModel.ISupportInitialize)(this.serialSettingsBindingSource)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource serialSettingsBindingSource;
        private System.Windows.Forms.ComboBox portNameComboBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.TextBox commsData;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button buttonPause;
        private System.Windows.Forms.ListView varView;
        private System.Windows.Forms.Label labelSketch;
        private System.Windows.Forms.ListView sourceView;
        private System.Windows.Forms.Button buttonLoad;
        private System.Windows.Forms.ColumnHeader columnBP;
        private System.Windows.Forms.ColumnHeader columnLine;
        private System.Windows.Forms.ColumnHeader columnLineNum;
        private System.Windows.Forms.ListView disassemblyView;
        private System.Windows.Forms.ColumnHeader columnText;
        private System.Windows.Forms.ColumnHeader columnNum;
        private System.Windows.Forms.Button buttonScan;
        private System.Windows.Forms.ColumnHeader columnName;
        private System.Windows.Forms.ColumnHeader columnValue;
        private System.Windows.Forms.Button buttonStep;
        private System.Windows.Forms.Button buttonRun;
        private System.Windows.Forms.Button buttonStepOver;
        private System.Windows.Forms.Button buttonComms;
        private System.Windows.Forms.Button buttonDiss;
        private System.Windows.Forms.Panel panelStopped;
        private System.Windows.Forms.Panel panelRunning;
        private System.Windows.Forms.Button buttonReload;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button buttonFunctions;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox textBoxInput;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.ColumnHeader columnExpander;
    }
}

