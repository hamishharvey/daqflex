namespace AOutScan
{
    partial class AOutScanForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AOutScanForm));
            this.label1 = new System.Windows.Forms.Label();
            this.highChannelComboBox = new System.Windows.Forms.ComboBox();
            this.rangeLabel = new System.Windows.Forms.Label();
            this.rangeComboBox = new System.Windows.Forms.ComboBox();
            this.channelLabel = new System.Windows.Forms.Label();
            this.deviceLabel = new System.Windows.Forms.Label();
            this.lowChannelComboBox = new System.Windows.Forms.ComboBox();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            this.continuousRadioButton = new System.Windows.Forms.RadioButton();
            this.finiteRadioButton = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.samplesTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.rateTextBox = new System.Windows.Forms.TextBox();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(14, 88);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 21);
            this.label1.TabIndex = 84;
            this.label1.Text = "High Channel:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // highChannelComboBox
            // 
            this.highChannelComboBox.Location = new System.Drawing.Point(116, 85);
            this.highChannelComboBox.Name = "highChannelComboBox";
            this.highChannelComboBox.Size = new System.Drawing.Size(107, 21);
            this.highChannelComboBox.TabIndex = 94;
            // 
            // rangeLabel
            // 
            this.rangeLabel.Location = new System.Drawing.Point(231, 56);
            this.rangeLabel.Name = "rangeLabel";
            this.rangeLabel.Size = new System.Drawing.Size(63, 21);
            this.rangeLabel.TabIndex = 95;
            this.rangeLabel.Text = "Range:";
            this.rangeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // rangeComboBox
            // 
            this.rangeComboBox.Location = new System.Drawing.Point(300, 53);
            this.rangeComboBox.Name = "rangeComboBox";
            this.rangeComboBox.Size = new System.Drawing.Size(107, 21);
            this.rangeComboBox.TabIndex = 93;
            this.rangeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnRangeChanged);
            // 
            // channelLabel
            // 
            this.channelLabel.Location = new System.Drawing.Point(14, 56);
            this.channelLabel.Name = "channelLabel";
            this.channelLabel.Size = new System.Drawing.Size(96, 21);
            this.channelLabel.TabIndex = 96;
            this.channelLabel.Text = "Low Channel:";
            this.channelLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // deviceLabel
            // 
            this.deviceLabel.Location = new System.Drawing.Point(47, 14);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(63, 20);
            this.deviceLabel.TabIndex = 97;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lowChannelComboBox
            // 
            this.lowChannelComboBox.Location = new System.Drawing.Point(116, 53);
            this.lowChannelComboBox.Name = "lowChannelComboBox";
            this.lowChannelComboBox.Size = new System.Drawing.Size(107, 21);
            this.lowChannelComboBox.TabIndex = 92;
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.Location = new System.Drawing.Point(116, 14);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(242, 21);
            this.deviceComboBox.TabIndex = 91;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // continuousRadioButton
            // 
            this.continuousRadioButton.Location = new System.Drawing.Point(316, 117);
            this.continuousRadioButton.Name = "continuousRadioButton";
            this.continuousRadioButton.Size = new System.Drawing.Size(95, 17);
            this.continuousRadioButton.TabIndex = 90;
            this.continuousRadioButton.Text = "Continuous";
            this.continuousRadioButton.CheckedChanged += new System.EventHandler(this.OnSampleModeChanged);
            // 
            // finiteRadioButton
            // 
            this.finiteRadioButton.Checked = true;
            this.finiteRadioButton.Location = new System.Drawing.Point(251, 117);
            this.finiteRadioButton.Name = "finiteRadioButton";
            this.finiteRadioButton.Size = new System.Drawing.Size(57, 17);
            this.finiteRadioButton.TabIndex = 89;
            this.finiteRadioButton.TabStop = true;
            this.finiteRadioButton.Text = "Finite";
            this.finiteRadioButton.CheckedChanged += new System.EventHandler(this.OnSampleModeChanged);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(235, 89);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 20);
            this.label4.TabIndex = 98;
            this.label4.Text = "Samples:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // samplesTextBox
            // 
            this.samplesTextBox.Location = new System.Drawing.Point(300, 86);
            this.samplesTextBox.Name = "samplesTextBox";
            this.samplesTextBox.Size = new System.Drawing.Size(107, 20);
            this.samplesTextBox.TabIndex = 88;
            this.samplesTextBox.Text = "1000";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(21, 117);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 13);
            this.label3.TabIndex = 99;
            this.label3.Text = "Rate:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // rateTextBox
            // 
            this.rateTextBox.Location = new System.Drawing.Point(116, 114);
            this.rateTextBox.Name = "rateTextBox";
            this.rateTextBox.Size = new System.Drawing.Size(107, 20);
            this.rateTextBox.TabIndex = 87;
            this.rateTextBox.Text = "1000";
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(251, 171);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 23);
            this.stopButton.TabIndex = 86;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClicked);
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(116, 171);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 85;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.OnStartButtonClicked);
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(38, 206);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(341, 32);
            this.statusLabel.TabIndex = 100;
            // 
            // AOutScanForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(424, 251);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.highChannelComboBox);
            this.Controls.Add(this.rangeLabel);
            this.Controls.Add(this.rangeComboBox);
            this.Controls.Add(this.channelLabel);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.lowChannelComboBox);
            this.Controls.Add(this.deviceComboBox);
            this.Controls.Add(this.continuousRadioButton);
            this.Controls.Add(this.finiteRadioButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.samplesTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.rateTextBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AOutScanForm";
            this.Text = "DAQFlex Example - AOutScan";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox highChannelComboBox;
        private System.Windows.Forms.Label rangeLabel;
        private System.Windows.Forms.ComboBox rangeComboBox;
        private System.Windows.Forms.Label channelLabel;
        private System.Windows.Forms.Label deviceLabel;
        private System.Windows.Forms.ComboBox lowChannelComboBox;
        private System.Windows.Forms.ComboBox deviceComboBox;
        private System.Windows.Forms.RadioButton continuousRadioButton;
        private System.Windows.Forms.RadioButton finiteRadioButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox samplesTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox rateTextBox;
        internal System.Windows.Forms.Button stopButton;
        internal System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label statusLabel;
    }
}

