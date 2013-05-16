namespace AInScan
{
    partial class AInScanFormWithQueue
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AInScanFormWithQueue));
            this.rangeLabel = new System.Windows.Forms.Label();
            this.rangeComboBox = new System.Windows.Forms.ComboBox();
            this.deviceLabel = new System.Windows.Forms.Label();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.continuousRadioButton = new System.Windows.Forms.RadioButton();
            this.finiteRadioButton = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.samplesTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.rateTextBox = new System.Windows.Forms.TextBox();
            this.stopButton = new System.Windows.Forms.Button();
            this.ScanDataTextBox = new System.Windows.Forms.TextBox();
            this.startButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tcTypesComboBox = new System.Windows.Forms.ComboBox();
            this.addToQueueButton = new System.Windows.Forms.Button();
            this.clearQueueButton = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.dataRateComboBox = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.channelModeComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.channelComboBox = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tempUnitsComboBox = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // rangeLabel
            // 
            this.rangeLabel.AutoSize = true;
            this.rangeLabel.Location = new System.Drawing.Point(356, 24);
            this.rangeLabel.Name = "rangeLabel";
            this.rangeLabel.Size = new System.Drawing.Size(42, 13);
            this.rangeLabel.TabIndex = 78;
            this.rangeLabel.Text = "Range:";
            this.rangeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // rangeComboBox
            // 
            this.rangeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.rangeComboBox.Location = new System.Drawing.Point(357, 40);
            this.rangeComboBox.Name = "rangeComboBox";
            this.rangeComboBox.Size = new System.Drawing.Size(107, 21);
            this.rangeComboBox.TabIndex = 76;
            // 
            // deviceLabel
            // 
            this.deviceLabel.AutoSize = true;
            this.deviceLabel.Location = new System.Drawing.Point(19, 17);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(44, 13);
            this.deviceLabel.TabIndex = 80;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceComboBox.Location = new System.Drawing.Point(69, 14);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(286, 21);
            this.deviceComboBox.TabIndex = 74;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(22, 438);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(587, 32);
            this.statusLabel.TabIndex = 81;
            // 
            // continuousRadioButton
            // 
            this.continuousRadioButton.Location = new System.Drawing.Point(450, 201);
            this.continuousRadioButton.Name = "continuousRadioButton";
            this.continuousRadioButton.Size = new System.Drawing.Size(95, 17);
            this.continuousRadioButton.TabIndex = 73;
            this.continuousRadioButton.Text = "Continuous";
            // 
            // finiteRadioButton
            // 
            this.finiteRadioButton.Checked = true;
            this.finiteRadioButton.Location = new System.Drawing.Point(383, 201);
            this.finiteRadioButton.Name = "finiteRadioButton";
            this.finiteRadioButton.Size = new System.Drawing.Size(75, 17);
            this.finiteRadioButton.TabIndex = 72;
            this.finiteRadioButton.TabStop = true;
            this.finiteRadioButton.Text = "Finite";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(122, 185);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 13);
            this.label4.TabIndex = 82;
            this.label4.Text = "Samples";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // samplesTextBox
            // 
            this.samplesTextBox.Location = new System.Drawing.Point(125, 204);
            this.samplesTextBox.Name = "samplesTextBox";
            this.samplesTextBox.Size = new System.Drawing.Size(107, 20);
            this.samplesTextBox.TabIndex = 71;
            this.samplesTextBox.Text = "100";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 185);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 83;
            this.label3.Text = "Rate";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // rateTextBox
            // 
            this.rateTextBox.Location = new System.Drawing.Point(22, 204);
            this.rateTextBox.Name = "rateTextBox";
            this.rateTextBox.Size = new System.Drawing.Size(91, 20);
            this.rateTextBox.TabIndex = 70;
            this.rateTextBox.Text = "1000";
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(126, 245);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 23);
            this.stopButton.TabIndex = 69;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClicked);
            // 
            // ScanDataTextBox
            // 
            this.ScanDataTextBox.Location = new System.Drawing.Point(22, 274);
            this.ScanDataTextBox.Multiline = true;
            this.ScanDataTextBox.Name = "ScanDataTextBox";
            this.ScanDataTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ScanDataTextBox.Size = new System.Drawing.Size(587, 160);
            this.ScanDataTextBox.TabIndex = 68;
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(22, 245);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 67;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.OnStartButtonClicked);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.tcTypesComboBox);
            this.groupBox1.Controls.Add(this.addToQueueButton);
            this.groupBox1.Controls.Add(this.clearQueueButton);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.dataRateComboBox);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.channelModeComboBox);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.rangeLabel);
            this.groupBox1.Controls.Add(this.channelComboBox);
            this.groupBox1.Controls.Add(this.rangeComboBox);
            this.groupBox1.Location = new System.Drawing.Point(9, 47);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(600, 126);
            this.groupBox1.TabIndex = 84;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Configuration";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(236, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 89;
            this.label1.Text = "TC Type:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // tcTypesComboBox
            // 
            this.tcTypesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tcTypesComboBox.Location = new System.Drawing.Point(236, 40);
            this.tcTypesComboBox.Name = "tcTypesComboBox";
            this.tcTypesComboBox.Size = new System.Drawing.Size(107, 21);
            this.tcTypesComboBox.TabIndex = 88;
            // 
            // addToQueueButton
            // 
            this.addToQueueButton.Location = new System.Drawing.Point(119, 87);
            this.addToQueueButton.Name = "addToQueueButton";
            this.addToQueueButton.Size = new System.Drawing.Size(73, 23);
            this.addToQueueButton.TabIndex = 87;
            this.addToQueueButton.Text = "Add";
            this.addToQueueButton.UseVisualStyleBackColor = true;
            this.addToQueueButton.Click += new System.EventHandler(this.OnAddQueueElements);
            // 
            // clearQueueButton
            // 
            this.clearQueueButton.Location = new System.Drawing.Point(13, 87);
            this.clearQueueButton.Name = "clearQueueButton";
            this.clearQueueButton.Size = new System.Drawing.Size(71, 23);
            this.clearQueueButton.TabIndex = 86;
            this.clearQueueButton.Text = "Clear";
            this.clearQueueButton.UseVisualStyleBackColor = true;
            this.clearQueueButton.Click += new System.EventHandler(this.OnClearQueue);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(477, 24);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(59, 13);
            this.label6.TabIndex = 85;
            this.label6.Text = "Data Rate:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // dataRateComboBox
            // 
            this.dataRateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dataRateComboBox.Location = new System.Drawing.Point(477, 40);
            this.dataRateComboBox.Name = "dataRateComboBox";
            this.dataRateComboBox.Size = new System.Drawing.Size(107, 21);
            this.dataRateComboBox.TabIndex = 84;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(113, 24);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(79, 13);
            this.label5.TabIndex = 83;
            this.label5.Text = "Channel Mode:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // channelModeComboBox
            // 
            this.channelModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.channelModeComboBox.Location = new System.Drawing.Point(117, 40);
            this.channelModeComboBox.Name = "channelModeComboBox";
            this.channelModeComboBox.Size = new System.Drawing.Size(107, 21);
            this.channelModeComboBox.TabIndex = 82;
            this.channelModeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnChannelModeChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 81;
            this.label2.Text = "Channel:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // channelComboBox
            // 
            this.channelComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.channelComboBox.Location = new System.Drawing.Point(13, 40);
            this.channelComboBox.Name = "channelComboBox";
            this.channelComboBox.Size = new System.Drawing.Size(91, 21);
            this.channelComboBox.TabIndex = 80;
            this.channelComboBox.SelectedIndexChanged += new System.EventHandler(this.OnChannelChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(245, 188);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(61, 13);
            this.label7.TabIndex = 87;
            this.label7.Text = "Temp Units";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // tempUnitsComboBox
            // 
            this.tempUnitsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tempUnitsComboBox.Location = new System.Drawing.Point(248, 204);
            this.tempUnitsComboBox.Name = "tempUnitsComboBox";
            this.tempUnitsComboBox.Size = new System.Drawing.Size(107, 21);
            this.tempUnitsComboBox.TabIndex = 86;
            this.tempUnitsComboBox.Click += new System.EventHandler(this.OnTempUnitsChanged);
            // 
            // AInScanFormWithQueue
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(633, 462);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.tempUnitsComboBox);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.deviceComboBox);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.continuousRadioButton);
            this.Controls.Add(this.finiteRadioButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.rateTextBox);
            this.Controls.Add(this.ScanDataTextBox);
            this.Controls.Add(this.samplesTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.startButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AInScanFormWithQueue";
            this.Text = "AIn Scan With Queue";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label rangeLabel;
        private System.Windows.Forms.ComboBox rangeComboBox;
        private System.Windows.Forms.Label deviceLabel;
        private System.Windows.Forms.ComboBox deviceComboBox;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.RadioButton continuousRadioButton;
        private System.Windows.Forms.RadioButton finiteRadioButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox samplesTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox rateTextBox;
        internal System.Windows.Forms.Button stopButton;
        internal System.Windows.Forms.TextBox ScanDataTextBox;
        internal System.Windows.Forms.Button startButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox channelModeComboBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox channelComboBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox dataRateComboBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox tempUnitsComboBox;
        private System.Windows.Forms.Button addToQueueButton;
        private System.Windows.Forms.Button clearQueueButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox tcTypesComboBox;

    }
}

