namespace AIn
{
    partial class AInForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AInForm));
            this.label1 = new System.Windows.Forms.Label();
            this.responseTextBox = new System.Windows.Forms.TextBox();
            this.rangeLabel = new System.Windows.Forms.Label();
            this.rangeComboBox = new System.Windows.Forms.ComboBox();
            this.channelLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.deviceLabel = new System.Windows.Forms.Label();
            this.channelComboBox = new System.Windows.Forms.ComboBox();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(6, 161);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 21);
            this.label1.TabIndex = 16;
            this.label1.Text = "Response:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // responseTextBox
            // 
            this.responseTextBox.Location = new System.Drawing.Point(86, 161);
            this.responseTextBox.Name = "responseTextBox";
            this.responseTextBox.ReadOnly = true;
            this.responseTextBox.Size = new System.Drawing.Size(242, 20);
            this.responseTextBox.TabIndex = 21;
            // 
            // rangeLabel
            // 
            this.rangeLabel.Location = new System.Drawing.Point(17, 74);
            this.rangeLabel.Name = "rangeLabel";
            this.rangeLabel.Size = new System.Drawing.Size(63, 21);
            this.rangeLabel.TabIndex = 22;
            this.rangeLabel.Text = "Range:";
            this.rangeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // rangeComboBox
            // 
            this.rangeComboBox.Location = new System.Drawing.Point(86, 71);
            this.rangeComboBox.Name = "rangeComboBox";
            this.rangeComboBox.Size = new System.Drawing.Size(107, 21);
            this.rangeComboBox.TabIndex = 20;
            this.rangeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnRangeChanged);
            // 
            // channelLabel
            // 
            this.channelLabel.Location = new System.Drawing.Point(17, 45);
            this.channelLabel.Name = "channelLabel";
            this.channelLabel.Size = new System.Drawing.Size(63, 21);
            this.channelLabel.TabIndex = 23;
            this.channelLabel.Text = "Channel:";
            this.channelLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(17, 196);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(356, 39);
            this.statusLabel.TabIndex = 24;
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.OnTimerTick);
            // 
            // deviceLabel
            // 
            this.deviceLabel.Location = new System.Drawing.Point(17, 12);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(63, 20);
            this.deviceLabel.TabIndex = 25;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // channelComboBox
            // 
            this.channelComboBox.Location = new System.Drawing.Point(86, 42);
            this.channelComboBox.Name = "channelComboBox";
            this.channelComboBox.Size = new System.Drawing.Size(107, 21);
            this.channelComboBox.TabIndex = 19;
            this.channelComboBox.SelectedIndexChanged += new System.EventHandler(this.OnChannelChanged);
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.Location = new System.Drawing.Point(86, 12);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(242, 21);
            this.deviceComboBox.TabIndex = 18;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(223, 111);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(82, 27);
            this.stopButton.TabIndex = 17;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClicked);
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(86, 111);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(82, 27);
            this.startButton.TabIndex = 15;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.OnStartButtonClicked);
            // 
            // AInForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(379, 247);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.responseTextBox);
            this.Controls.Add(this.rangeLabel);
            this.Controls.Add(this.rangeComboBox);
            this.Controls.Add(this.channelLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.channelComboBox);
            this.Controls.Add(this.deviceComboBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AInForm";
            this.Text = "DAQFlex Example - AIn";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox responseTextBox;
        private System.Windows.Forms.Label rangeLabel;
        private System.Windows.Forms.ComboBox rangeComboBox;
        private System.Windows.Forms.Label channelLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label deviceLabel;
        private System.Windows.Forms.ComboBox channelComboBox;
        private System.Windows.Forms.ComboBox deviceComboBox;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button startButton;

    }
}

