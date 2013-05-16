namespace CtrIn
{
    partial class CtrInForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CtrInForm));
            this.label1 = new System.Windows.Forms.Label();
            this.responseTextBox = new System.Windows.Forms.TextBox();
            this.stopButton = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.startButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.channelLabel = new System.Windows.Forms.Label();
            this.deviceLabel = new System.Windows.Forms.Label();
            this.counterComboBox = new System.Windows.Forms.ComboBox();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(6, 129);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 21);
            this.label1.TabIndex = 18;
            this.label1.Text = "Response:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // responseTextBox
            // 
            this.responseTextBox.Location = new System.Drawing.Point(86, 129);
            this.responseTextBox.Name = "responseTextBox";
            this.responseTextBox.ReadOnly = true;
            this.responseTextBox.Size = new System.Drawing.Size(242, 20);
            this.responseTextBox.TabIndex = 23;
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(223, 79);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(82, 27);
            this.stopButton.TabIndex = 22;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClicked);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.OnTimerTick);
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(86, 79);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(82, 27);
            this.startButton.TabIndex = 21;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.OnStartButtonClicked);
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(17, 168);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(356, 39);
            this.statusLabel.TabIndex = 24;
            // 
            // channelLabel
            // 
            this.channelLabel.Location = new System.Drawing.Point(17, 44);
            this.channelLabel.Name = "channelLabel";
            this.channelLabel.Size = new System.Drawing.Size(63, 21);
            this.channelLabel.TabIndex = 25;
            this.channelLabel.Text = "Counter:";
            this.channelLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // deviceLabel
            // 
            this.deviceLabel.Location = new System.Drawing.Point(17, 11);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(63, 20);
            this.deviceLabel.TabIndex = 26;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // counterComboBox
            // 
            this.counterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.counterComboBox.Location = new System.Drawing.Point(86, 41);
            this.counterComboBox.Name = "counterComboBox";
            this.counterComboBox.Size = new System.Drawing.Size(107, 21);
            this.counterComboBox.TabIndex = 20;
            this.counterComboBox.SelectedIndexChanged += new System.EventHandler(this.OnCounterChanged);
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceComboBox.Location = new System.Drawing.Point(86, 11);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(242, 21);
            this.deviceComboBox.TabIndex = 19;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // CtrInForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(379, 218);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.responseTextBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.channelLabel);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.counterComboBox);
            this.Controls.Add(this.deviceComboBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CtrInForm";
            this.Text = "CtrIn";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox responseTextBox;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label channelLabel;
        private System.Windows.Forms.Label deviceLabel;
        private System.Windows.Forms.ComboBox counterComboBox;
        private System.Windows.Forms.ComboBox deviceComboBox;

    }
}

