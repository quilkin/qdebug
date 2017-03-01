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
            System.Windows.Forms.Label baudRateLabel;
            System.Windows.Forms.Label portNameLabel;
            this.baudRateComboBox = new System.Windows.Forms.ComboBox();
            this.serialSettingsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.portNameComboBox = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.tbData = new System.Windows.Forms.TextBox();
            this.btnStop = new System.Windows.Forms.Button();
            this.buttonSend = new System.Windows.Forms.Button();
            this.textToSend = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.varView = new System.Windows.Forms.ListView();
            this.labelSketch = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.sourceView = new System.Windows.Forms.ListView();
            this.columnBP = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnLineNum = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnLine = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnAddr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonLoad = new System.Windows.Forms.Button();
            this.listDisassembly = new System.Windows.Forms.ListView();
            this.columnNum = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            baudRateLabel = new System.Windows.Forms.Label();
            portNameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.serialSettingsBindingSource)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // baudRateLabel
            // 
            baudRateLabel.AutoSize = true;
            baudRateLabel.Location = new System.Drawing.Point(10, 59);
            baudRateLabel.Name = "baudRateLabel";
            baudRateLabel.Size = new System.Drawing.Size(61, 13);
            baudRateLabel.TabIndex = 1;
            baudRateLabel.Text = "Baud Rate:";
            // 
            // portNameLabel
            // 
            portNameLabel.AutoSize = true;
            portNameLabel.Location = new System.Drawing.Point(10, 32);
            portNameLabel.Name = "portNameLabel";
            portNameLabel.Size = new System.Drawing.Size(69, 13);
            portNameLabel.TabIndex = 7;
            portNameLabel.Text = "Port Number:";
            // 
            // baudRateComboBox
            // 
            this.baudRateComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.serialSettingsBindingSource, "BaudRate", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.baudRateComboBox.FormattingEnabled = true;
            this.baudRateComboBox.Location = new System.Drawing.Point(77, 56);
            this.baudRateComboBox.Name = "baudRateComboBox";
            this.baudRateComboBox.Size = new System.Drawing.Size(96, 21);
            this.baudRateComboBox.TabIndex = 2;
            // 
            // portNameComboBox
            // 
            this.portNameComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.serialSettingsBindingSource, "PortName", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.portNameComboBox.FormattingEnabled = true;
            this.portNameComboBox.Location = new System.Drawing.Point(77, 29);
            this.portNameComboBox.Name = "portNameComboBox";
            this.portNameComboBox.Size = new System.Drawing.Size(96, 21);
            this.portNameComboBox.TabIndex = 8;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.baudRateComboBox);
            this.groupBox1.Controls.Add(baudRateLabel);
            this.groupBox1.Controls.Add(this.portNameComboBox);
            this.groupBox1.Controls.Add(portNameLabel);
            this.groupBox1.Location = new System.Drawing.Point(11, 14);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(191, 86);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "COM Settings (must be same as your sketch)";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(11, 106);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(85, 23);
            this.btnStart.TabIndex = 12;
            this.btnStart.Text = "Start listening";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // tbData
            // 
            this.tbData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbData.Location = new System.Drawing.Point(11, 164);
            this.tbData.Multiline = true;
            this.tbData.Name = "tbData";
            this.tbData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbData.Size = new System.Drawing.Size(185, 126);
            this.tbData.TabIndex = 13;
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(11, 135);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(85, 23);
            this.btnStop.TabIndex = 12;
            this.btnStop.Text = "Stop listening";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // buttonSend
            // 
            this.buttonSend.Location = new System.Drawing.Point(102, 135);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(90, 23);
            this.buttonSend.TabIndex = 15;
            this.buttonSend.Text = "Send";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            // 
            // textToSend
            // 
            this.textToSend.Location = new System.Drawing.Point(102, 109);
            this.textToSend.MaxLength = 32;
            this.textToSend.Name = "textToSend";
            this.textToSend.Size = new System.Drawing.Size(90, 20);
            this.textToSend.TabIndex = 16;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.tbData);
            this.panel1.Controls.Add(this.buttonSend);
            this.panel1.Controls.Add(this.textToSend);
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Controls.Add(this.btnStop);
            this.panel1.Location = new System.Drawing.Point(972, 21);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(205, 293);
            this.panel1.TabIndex = 17;
            // 
            // varView
            // 
            this.varView.Location = new System.Drawing.Point(983, 340);
            this.varView.Name = "varView";
            this.varView.Size = new System.Drawing.Size(185, 191);
            this.varView.TabIndex = 19;
            this.varView.UseCompatibleStateImageBehavior = false;
            // 
            // labelSketch
            // 
            this.labelSketch.AutoSize = true;
            this.labelSketch.Location = new System.Drawing.Point(19, 15);
            this.labelSketch.Name = "labelSketch";
            this.labelSketch.Size = new System.Drawing.Size(64, 13);
            this.labelSketch.TabIndex = 20;
            this.labelSketch.Text = "Your sketch";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(969, 324);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Your variables";
            // 
            // sourceView
            // 
            this.sourceView.CheckBoxes = true;
            this.sourceView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnBP,
            this.columnLineNum,
            this.columnLine,
            this.columnAddr});
            this.sourceView.FullRowSelect = true;
            this.sourceView.Location = new System.Drawing.Point(12, 40);
            this.sourceView.MultiSelect = false;
            this.sourceView.Name = "sourceView";
            this.sourceView.Size = new System.Drawing.Size(511, 491);
            this.sourceView.TabIndex = 22;
            this.sourceView.UseCompatibleStateImageBehavior = false;
            this.sourceView.View = System.Windows.Forms.View.Details;
            // 
            // columnBP
            // 
            this.columnBP.Text = "BP";
            this.columnBP.Width = 28;
            // 
            // columnLineNum
            // 
            this.columnLineNum.Text = "Line";
            this.columnLineNum.Width = 39;
            // 
            // columnLine
            // 
            this.columnLine.Text = "Instructions";
            this.columnLine.Width = 463;
            // 
            // columnAddr
            // 
            this.columnAddr.Text = "Addr";
            // 
            // buttonLoad
            // 
            this.buttonLoad.Location = new System.Drawing.Point(503, 9);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(135, 23);
            this.buttonLoad.TabIndex = 23;
            this.buttonLoad.Text = "Load Sketch";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
            // 
            // listDisassembly
            // 
            this.listDisassembly.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnNum,
            this.columnHeader1});
            this.listDisassembly.FullRowSelect = true;
            this.listDisassembly.Location = new System.Drawing.Point(545, 38);
            this.listDisassembly.MultiSelect = false;
            this.listDisassembly.Name = "listDisassembly";
            this.listDisassembly.Size = new System.Drawing.Size(418, 493);
            this.listDisassembly.TabIndex = 24;
            this.listDisassembly.UseCompatibleStateImageBehavior = false;
            this.listDisassembly.View = System.Windows.Forms.View.Details;
            // 
            // columnNum
            // 
            this.columnNum.Text = "#";
            this.columnNum.Width = 40;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Disassembly";
            this.columnHeader1.Width = 328;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1189, 549);
            this.Controls.Add(this.listDisassembly);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.sourceView);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelSketch);
            this.Controls.Add(this.varView);
            this.Controls.Add(this.panel1);
            this.Name = "MainForm";
            this.Text = "Arduino Debugger";
            ((System.ComponentModel.ISupportInitialize)(this.serialSettingsBindingSource)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource serialSettingsBindingSource;
        private System.Windows.Forms.ComboBox baudRateComboBox;
        private System.Windows.Forms.ComboBox portNameComboBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.TextBox tbData;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.TextBox textToSend;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListView varView;
        private System.Windows.Forms.Label labelSketch;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListView sourceView;
        private System.Windows.Forms.Button buttonLoad;
        private System.Windows.Forms.ColumnHeader columnBP;
        private System.Windows.Forms.ColumnHeader columnLine;
        private System.Windows.Forms.ColumnHeader columnLineNum;
        private System.Windows.Forms.ColumnHeader columnAddr;
        private System.Windows.Forms.ListView listDisassembly;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnNum;
    }
}

