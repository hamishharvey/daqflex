﻿namespace AInScan
{
    partial class AInScanForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AInScanForm));
            this.label1 = new System.Windows.Forms.Label();
            this.highChannelComboBox = new System.Windows.Forms.ComboBox();
            this.rangeLabel = new System.Windows.Forms.Label();
            this.rangeComboBox = new System.Windows.Forms.ComboBox();
            this.channelLabel = new System.Windows.Forms.Label();
            this.deviceLabel = new System.Windows.Forms.Label();
            this.lowChannelComboBox = new System.Windows.Forms.ComboBox();
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
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(-1, 73);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 21);
            this.label1.TabIndex = 66;
            this.label1.Text = "High Channel:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // highChannelComboBox
            // 
            this.highChannelComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.highChannelComboBox.Location = new System.Drawing.Point(101, 70);
            this.highChannelComboBox.Name = "highChannelComboBox";
            this.highChannelComboBox.Size = new System.Drawing.Size(107, 21);
            this.highChannelComboBox.TabIndex = 77;
            // 
            // rangeLabel
            // 
            this.rangeLabel.Location = new System.Drawing.Point(216, 73);
            this.rangeLabel.Name = "rangeLabel";
            this.rangeLabel.Size = new System.Drawing.Size(63, 21);
            this.rangeLabel.TabIndex = 78;
            this.rangeLabel.Text = "Range:";
            this.rangeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // rangeComboBox
            // 
            this.rangeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.rangeComboBox.Location = new System.Drawing.Point(285, 70);
            this.rangeComboBox.Name = "rangeComboBox";
            this.rangeComboBox.Size = new System.Drawing.Size(107, 21);
            this.rangeComboBox.TabIndex = 76;
            this.rangeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnRangeChanged);
            // 
            // channelLabel
            // 
            this.channelLabel.Location = new System.Drawing.Point(-1, 41);
            this.channelLabel.Name = "channelLabel";
            this.channelLabel.Size = new System.Drawing.Size(96, 21);
            this.channelLabel.TabIndex = 79;
            this.channelLabel.Text = "Low Channel:";
            this.channelLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // deviceLabel
            // 
            this.deviceLabel.Location = new System.Drawing.Point(32, 8);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(63, 20);
            this.deviceLabel.TabIndex = 80;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lowChannelComboBox
            // 
            this.lowChannelComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lowChannelComboBox.Location = new System.Drawing.Point(101, 38);
            this.lowChannelComboBox.Name = "lowChannelComboBox";
            this.lowChannelComboBox.Size = new System.Drawing.Size(107, 21);
            this.lowChannelComboBox.TabIndex = 75;
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceComboBox.Location = new System.Drawing.Point(101, 8);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(242, 21);
            this.deviceComboBox.TabIndex = 74;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(13, 302);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(341, 32);
            this.statusLabel.TabIndex = 81;
            // 
            // continuousRadioButton
            // 
            this.continuousRadioButton.Location = new System.Drawing.Point(309, 143);
            this.continuousRadioButton.Name = "continuousRadioButton";
            this.continuousRadioButton.Size = new System.Drawing.Size(95, 17);
            this.continuousRadioButton.TabIndex = 73;
            this.continuousRadioButton.Text = "Continuous";
            // 
            // finiteRadioButton
            // 
            this.finiteRadioButton.Checked = true;
            this.finiteRadioButton.Location = new System.Drawing.Point(232, 143);
            this.finiteRadioButton.Name = "finiteRadioButton";
            this.finiteRadioButton.Size = new System.Drawing.Size(75, 17);
            this.finiteRadioButton.TabIndex = 72;
            this.finiteRadioButton.TabStop = true;
            this.finiteRadioButton.Text = "Finite";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(218, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 20);
            this.label4.TabIndex = 82;
            this.label4.Text = "Samples:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // samplesTextBox
            // 
            this.samplesTextBox.Location = new System.Drawing.Point(285, 99);
            this.samplesTextBox.Name = "samplesTextBox";
            this.samplesTextBox.Size = new System.Drawing.Size(107, 20);
            this.samplesTextBox.TabIndex = 71;
            this.samplesTextBox.Text = "100";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(6, 102);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 13);
            this.label3.TabIndex = 83;
            this.label3.Text = "Rate:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // rateTextBox
            // 
            this.rateTextBox.Location = new System.Drawing.Point(101, 99);
            this.rateTextBox.Name = "rateTextBox";
            this.rateTextBox.Size = new System.Drawing.Size(107, 20);
            this.rateTextBox.TabIndex = 70;
            this.rateTextBox.Text = "1000";
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(101, 137);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 23);
            this.stopButton.TabIndex = 69;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClicked);
            // 
            // ScanDataTextBox
            // 
            this.ScanDataTextBox.Location = new System.Drawing.Point(10, 168);
            this.ScanDataTextBox.Multiline = true;
            this.ScanDataTextBox.Name = "ScanDataTextBox";
            this.ScanDataTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ScanDataTextBox.Size = new System.Drawing.Size(403, 130);
            this.ScanDataTextBox.TabIndex = 68;
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(10, 137);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 67;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.OnStartButtonClicked);
            // 
            // AInScanForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(426, 343);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.highChannelComboBox);
            this.Controls.Add(this.rangeLabel);
            this.Controls.Add(this.rangeComboBox);
            this.Controls.Add(this.channelLabel);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.lowChannelComboBox);
            this.Controls.Add(this.deviceComboBox);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.continuousRadioButton);
            this.Controls.Add(this.finiteRadioButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.samplesTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.rateTextBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.ScanDataTextBox);
            this.Controls.Add(this.startButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AInScanForm";
            this.Text = "AIn Scan";
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

    }
}

