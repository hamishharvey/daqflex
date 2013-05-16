namespace DIn
{
    partial class DInForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DInForm));
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.bitRadioButton = new System.Windows.Forms.RadioButton();
            this.portRadioButton = new System.Windows.Forms.RadioButton();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.valueLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.led0 = new System.Windows.Forms.TextBox();
            this.led1 = new System.Windows.Forms.TextBox();
            this.led2 = new System.Windows.Forms.TextBox();
            this.led3 = new System.Windows.Forms.TextBox();
            this.led4 = new System.Windows.Forms.TextBox();
            this.led5 = new System.Windows.Forms.TextBox();
            this.led6 = new System.Windows.Forms.TextBox();
            this.led7 = new System.Windows.Forms.TextBox();
            this.channelLabel = new System.Windows.Forms.Label();
            this.portComboBox = new System.Windows.Forms.ComboBox();
            this.deviceLabel = new System.Windows.Forms.Label();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.OnTimerTick);
            // 
            // bitRadioButton
            // 
            this.bitRadioButton.Location = new System.Drawing.Point(213, 62);
            this.bitRadioButton.Name = "bitRadioButton";
            this.bitRadioButton.Size = new System.Drawing.Size(100, 20);
            this.bitRadioButton.TabIndex = 75;
            this.bitRadioButton.Text = "Read bits";
            // 
            // portRadioButton
            // 
            this.portRadioButton.Checked = true;
            this.portRadioButton.Location = new System.Drawing.Point(213, 39);
            this.portRadioButton.Name = "portRadioButton";
            this.portRadioButton.Size = new System.Drawing.Size(100, 20);
            this.portRadioButton.TabIndex = 74;
            this.portRadioButton.TabStop = true;
            this.portRadioButton.Text = "Read port";
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(213, 90);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(82, 27);
            this.stopButton.TabIndex = 73;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClicked);
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(80, 90);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(82, 27);
            this.startButton.TabIndex = 72;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.OnStartButtonClicked);
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(24, 164);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(322, 40);
            this.statusLabel.TabIndex = 76;
            // 
            // valueLabel
            // 
            this.valueLabel.Location = new System.Drawing.Point(240, 130);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(100, 20);
            this.valueLabel.TabIndex = 77;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(15, 130);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 20);
            this.label1.TabIndex = 78;
            this.label1.Text = "Value:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // led0
            // 
            this.led0.BackColor = System.Drawing.SystemColors.Control;
            this.led0.Location = new System.Drawing.Point(213, 130);
            this.led0.Name = "led0";
            this.led0.Size = new System.Drawing.Size(21, 20);
            this.led0.TabIndex = 71;
            // 
            // led1
            // 
            this.led1.BackColor = System.Drawing.SystemColors.Control;
            this.led1.Location = new System.Drawing.Point(194, 130);
            this.led1.Name = "led1";
            this.led1.Size = new System.Drawing.Size(21, 20);
            this.led1.TabIndex = 70;
            // 
            // led2
            // 
            this.led2.BackColor = System.Drawing.SystemColors.Control;
            this.led2.Location = new System.Drawing.Point(175, 130);
            this.led2.Name = "led2";
            this.led2.Size = new System.Drawing.Size(21, 20);
            this.led2.TabIndex = 69;
            // 
            // led3
            // 
            this.led3.BackColor = System.Drawing.SystemColors.Control;
            this.led3.Location = new System.Drawing.Point(156, 130);
            this.led3.Name = "led3";
            this.led3.Size = new System.Drawing.Size(21, 20);
            this.led3.TabIndex = 68;
            // 
            // led4
            // 
            this.led4.BackColor = System.Drawing.SystemColors.Control;
            this.led4.Location = new System.Drawing.Point(137, 130);
            this.led4.Name = "led4";
            this.led4.Size = new System.Drawing.Size(21, 20);
            this.led4.TabIndex = 67;
            // 
            // led5
            // 
            this.led5.BackColor = System.Drawing.SystemColors.Control;
            this.led5.Location = new System.Drawing.Point(118, 130);
            this.led5.Name = "led5";
            this.led5.Size = new System.Drawing.Size(21, 20);
            this.led5.TabIndex = 66;
            // 
            // led6
            // 
            this.led6.BackColor = System.Drawing.SystemColors.Control;
            this.led6.Location = new System.Drawing.Point(99, 130);
            this.led6.Name = "led6";
            this.led6.Size = new System.Drawing.Size(21, 20);
            this.led6.TabIndex = 65;
            // 
            // led7
            // 
            this.led7.BackColor = System.Drawing.SystemColors.Control;
            this.led7.Location = new System.Drawing.Point(80, 130);
            this.led7.Name = "led7";
            this.led7.Size = new System.Drawing.Size(21, 20);
            this.led7.TabIndex = 64;
            // 
            // channelLabel
            // 
            this.channelLabel.Location = new System.Drawing.Point(11, 47);
            this.channelLabel.Name = "channelLabel";
            this.channelLabel.Size = new System.Drawing.Size(63, 21);
            this.channelLabel.TabIndex = 79;
            this.channelLabel.Text = "Port:";
            this.channelLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // portComboBox
            // 
            this.portComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.portComboBox.Location = new System.Drawing.Point(80, 44);
            this.portComboBox.Name = "portComboBox";
            this.portComboBox.Size = new System.Drawing.Size(97, 21);
            this.portComboBox.TabIndex = 63;
            this.portComboBox.SelectedIndexChanged += new System.EventHandler(this.OnPortChanged);
            // 
            // deviceLabel
            // 
            this.deviceLabel.Location = new System.Drawing.Point(11, 10);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(63, 20);
            this.deviceLabel.TabIndex = 80;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceComboBox.Location = new System.Drawing.Point(80, 10);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(242, 21);
            this.deviceComboBox.TabIndex = 62;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // DInForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(356, 214);
            this.Controls.Add(this.bitRadioButton);
            this.Controls.Add(this.portRadioButton);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.valueLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.led0);
            this.Controls.Add(this.led1);
            this.Controls.Add(this.led2);
            this.Controls.Add(this.led3);
            this.Controls.Add(this.led4);
            this.Controls.Add(this.led5);
            this.Controls.Add(this.led6);
            this.Controls.Add(this.led7);
            this.Controls.Add(this.channelLabel);
            this.Controls.Add(this.portComboBox);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.deviceComboBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DInForm";
            this.Text = "DIn";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        internal System.Windows.Forms.RadioButton bitRadioButton;
        internal System.Windows.Forms.RadioButton portRadioButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label valueLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox led0;
        private System.Windows.Forms.TextBox led1;
        private System.Windows.Forms.TextBox led2;
        private System.Windows.Forms.TextBox led3;
        private System.Windows.Forms.TextBox led4;
        private System.Windows.Forms.TextBox led5;
        private System.Windows.Forms.TextBox led6;
        private System.Windows.Forms.TextBox led7;
        private System.Windows.Forms.Label channelLabel;
        private System.Windows.Forms.ComboBox portComboBox;
        private System.Windows.Forms.Label deviceLabel;
        private System.Windows.Forms.ComboBox deviceComboBox;

    }
}

