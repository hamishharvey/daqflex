namespace DOut
{
    partial class DOutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DOutForm));
            this.bitRadioButton = new System.Windows.Forms.RadioButton();
            this.portRadioButton = new System.Windows.Forms.RadioButton();
            this.statusLabel = new System.Windows.Forms.Label();
            this.valueLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBox8 = new System.Windows.Forms.CheckBox();
            this.checkBox7 = new System.Windows.Forms.CheckBox();
            this.checkBox6 = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.channelLabel = new System.Windows.Forms.Label();
            this.portComboBox = new System.Windows.Forms.ComboBox();
            this.deviceLabel = new System.Windows.Forms.Label();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // bitRadioButton
            // 
            this.bitRadioButton.Location = new System.Drawing.Point(221, 66);
            this.bitRadioButton.Name = "bitRadioButton";
            this.bitRadioButton.Size = new System.Drawing.Size(100, 20);
            this.bitRadioButton.TabIndex = 73;
            this.bitRadioButton.Text = "Write bits";
            // 
            // portRadioButton
            // 
            this.portRadioButton.Checked = true;
            this.portRadioButton.Location = new System.Drawing.Point(221, 43);
            this.portRadioButton.Name = "portRadioButton";
            this.portRadioButton.Size = new System.Drawing.Size(100, 20);
            this.portRadioButton.TabIndex = 72;
            this.portRadioButton.TabStop = true;
            this.portRadioButton.Text = "Write port";
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(14, 128);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(329, 36);
            this.statusLabel.TabIndex = 74;
            // 
            // valueLabel
            // 
            this.valueLabel.Location = new System.Drawing.Point(221, 99);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(100, 20);
            this.valueLabel.TabIndex = 75;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(14, 99);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 21);
            this.label1.TabIndex = 76;
            this.label1.Text = "Value:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // checkBox8
            // 
            this.checkBox8.Location = new System.Drawing.Point(191, 99);
            this.checkBox8.Name = "checkBox8";
            this.checkBox8.Size = new System.Drawing.Size(24, 20);
            this.checkBox8.TabIndex = 71;
            this.checkBox8.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
            // 
            // checkBox7
            // 
            this.checkBox7.Location = new System.Drawing.Point(175, 99);
            this.checkBox7.Name = "checkBox7";
            this.checkBox7.Size = new System.Drawing.Size(24, 20);
            this.checkBox7.TabIndex = 70;
            this.checkBox7.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
            // 
            // checkBox6
            // 
            this.checkBox6.Location = new System.Drawing.Point(159, 99);
            this.checkBox6.Name = "checkBox6";
            this.checkBox6.Size = new System.Drawing.Size(24, 20);
            this.checkBox6.TabIndex = 69;
            this.checkBox6.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
            // 
            // checkBox5
            // 
            this.checkBox5.Location = new System.Drawing.Point(143, 99);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(24, 20);
            this.checkBox5.TabIndex = 68;
            this.checkBox5.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
            // 
            // checkBox4
            // 
            this.checkBox4.Location = new System.Drawing.Point(127, 99);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(24, 20);
            this.checkBox4.TabIndex = 67;
            this.checkBox4.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
            // 
            // checkBox3
            // 
            this.checkBox3.Location = new System.Drawing.Point(111, 99);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(24, 20);
            this.checkBox3.TabIndex = 66;
            this.checkBox3.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
            // 
            // checkBox2
            // 
            this.checkBox2.Location = new System.Drawing.Point(95, 99);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(24, 20);
            this.checkBox2.TabIndex = 65;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
            // 
            // checkBox1
            // 
            this.checkBox1.Location = new System.Drawing.Point(79, 99);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(24, 20);
            this.checkBox1.TabIndex = 64;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
            // 
            // channelLabel
            // 
            this.channelLabel.Location = new System.Drawing.Point(14, 50);
            this.channelLabel.Name = "channelLabel";
            this.channelLabel.Size = new System.Drawing.Size(63, 21);
            this.channelLabel.TabIndex = 77;
            this.channelLabel.Text = "Port:";
            this.channelLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // portComboBox
            // 
            this.portComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.portComboBox.Location = new System.Drawing.Point(83, 47);
            this.portComboBox.Name = "portComboBox";
            this.portComboBox.Size = new System.Drawing.Size(97, 21);
            this.portComboBox.TabIndex = 63;
            this.portComboBox.SelectedIndexChanged += new System.EventHandler(this.OnPortChanged);
            // 
            // deviceLabel
            // 
            this.deviceLabel.Location = new System.Drawing.Point(14, 9);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(63, 20);
            this.deviceLabel.TabIndex = 78;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceComboBox.Location = new System.Drawing.Point(83, 9);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(242, 21);
            this.deviceComboBox.TabIndex = 62;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // DOutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(356, 172);
            this.Controls.Add(this.bitRadioButton);
            this.Controls.Add(this.portRadioButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.valueLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBox8);
            this.Controls.Add(this.checkBox7);
            this.Controls.Add(this.checkBox6);
            this.Controls.Add(this.checkBox5);
            this.Controls.Add(this.checkBox4);
            this.Controls.Add(this.checkBox3);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.channelLabel);
            this.Controls.Add(this.portComboBox);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.deviceComboBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DOutForm";
            this.Text = "DAQFlex Example - DOut";
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.RadioButton bitRadioButton;
        internal System.Windows.Forms.RadioButton portRadioButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label valueLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBox8;
        private System.Windows.Forms.CheckBox checkBox7;
        private System.Windows.Forms.CheckBox checkBox6;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label channelLabel;
        private System.Windows.Forms.ComboBox portComboBox;
        private System.Windows.Forms.Label deviceLabel;
        private System.Windows.Forms.ComboBox deviceComboBox;
    }
}

