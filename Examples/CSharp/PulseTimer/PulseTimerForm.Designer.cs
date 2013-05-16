namespace PulseTimer
{
    partial class PulseTimerForm
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
            this.timerLabel = new System.Windows.Forms.Label();
            this.deviceLabel = new System.Windows.Forms.Label();
            this.timerComboBox = new System.Windows.Forms.ComboBox();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.periodLabel = new System.Windows.Forms.Label();
            this.PeriodTextBox = new System.Windows.Forms.TextBox();
            this.dutyCycleTextBox = new System.Windows.Forms.TextBox();
            this.dutyCycleLabel = new System.Windows.Forms.Label();
            this.delayTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.startButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.pulseCountTextBox = new System.Windows.Forms.TextBox();
            this.pulseCountLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // timerLabel
            // 
            this.timerLabel.Location = new System.Drawing.Point(25, 49);
            this.timerLabel.Name = "timerLabel";
            this.timerLabel.Size = new System.Drawing.Size(63, 21);
            this.timerLabel.TabIndex = 31;
            this.timerLabel.Text = "Timer:";
            this.timerLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // deviceLabel
            // 
            this.deviceLabel.Location = new System.Drawing.Point(25, 18);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(63, 20);
            this.deviceLabel.TabIndex = 32;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // timerComboBox
            // 
            this.timerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.timerComboBox.Location = new System.Drawing.Point(94, 48);
            this.timerComboBox.Name = "timerComboBox";
            this.timerComboBox.Size = new System.Drawing.Size(107, 21);
            this.timerComboBox.TabIndex = 29;
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceComboBox.Location = new System.Drawing.Point(94, 18);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(242, 21);
            this.deviceComboBox.TabIndex = 28;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(10, 193);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(356, 39);
            this.statusLabel.TabIndex = 33;
            // 
            // periodLabel
            // 
            this.periodLabel.Location = new System.Drawing.Point(25, 81);
            this.periodLabel.Name = "periodLabel";
            this.periodLabel.Size = new System.Drawing.Size(63, 21);
            this.periodLabel.TabIndex = 34;
            this.periodLabel.Text = "Period:";
            this.periodLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // PeriodTextBox
            // 
            this.PeriodTextBox.Location = new System.Drawing.Point(94, 78);
            this.PeriodTextBox.Name = "PeriodTextBox";
            this.PeriodTextBox.Size = new System.Drawing.Size(107, 20);
            this.PeriodTextBox.TabIndex = 35;
            this.PeriodTextBox.Text = "100";
            // 
            // dutyCycleTextBox
            // 
            this.dutyCycleTextBox.Location = new System.Drawing.Point(94, 105);
            this.dutyCycleTextBox.Name = "dutyCycleTextBox";
            this.dutyCycleTextBox.Size = new System.Drawing.Size(107, 20);
            this.dutyCycleTextBox.TabIndex = 37;
            this.dutyCycleTextBox.Text = "50";
            // 
            // dutyCycleLabel
            // 
            this.dutyCycleLabel.Location = new System.Drawing.Point(25, 108);
            this.dutyCycleLabel.Name = "dutyCycleLabel";
            this.dutyCycleLabel.Size = new System.Drawing.Size(63, 21);
            this.dutyCycleLabel.TabIndex = 36;
            this.dutyCycleLabel.Text = "Duty Cycle:";
            this.dutyCycleLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // delayTextBox
            // 
            this.delayTextBox.Location = new System.Drawing.Point(94, 131);
            this.delayTextBox.Name = "delayTextBox";
            this.delayTextBox.Size = new System.Drawing.Size(107, 20);
            this.delayTextBox.TabIndex = 39;
            this.delayTextBox.Text = "0";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(25, 134);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 21);
            this.label3.TabIndex = 38;
            this.label3.Text = "Delay:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(243, 76);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 40;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.OnStartButtonClicked);
            // 
            // stopButton
            // 
            this.stopButton.Location = new System.Drawing.Point(243, 128);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 23);
            this.stopButton.TabIndex = 41;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClicked);
            // 
            // pulseCountTextBox
            // 
            this.pulseCountTextBox.Location = new System.Drawing.Point(94, 157);
            this.pulseCountTextBox.Name = "pulseCountTextBox";
            this.pulseCountTextBox.Size = new System.Drawing.Size(107, 20);
            this.pulseCountTextBox.TabIndex = 43;
            this.pulseCountTextBox.Text = "0";
            // 
            // pulseCountLabel
            // 
            this.pulseCountLabel.Location = new System.Drawing.Point(13, 160);
            this.pulseCountLabel.Name = "pulseCountLabel";
            this.pulseCountLabel.Size = new System.Drawing.Size(75, 21);
            this.pulseCountLabel.TabIndex = 42;
            this.pulseCountLabel.Text = "Pulse Count:";
            this.pulseCountLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // PulseTimerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(378, 250);
            this.Controls.Add(this.pulseCountTextBox);
            this.Controls.Add(this.pulseCountLabel);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.delayTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dutyCycleTextBox);
            this.Controls.Add(this.dutyCycleLabel);
            this.Controls.Add(this.PeriodTextBox);
            this.Controls.Add(this.periodLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.timerLabel);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.timerComboBox);
            this.Controls.Add(this.deviceComboBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PulseTimerForm";
            this.Text = "DAQFlex Example - PulseTimer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label timerLabel;
        private System.Windows.Forms.Label deviceLabel;
        private System.Windows.Forms.ComboBox timerComboBox;
        private System.Windows.Forms.ComboBox deviceComboBox;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label periodLabel;
        private System.Windows.Forms.TextBox PeriodTextBox;
        private System.Windows.Forms.TextBox dutyCycleTextBox;
        private System.Windows.Forms.Label dutyCycleLabel;
        private System.Windows.Forms.TextBox delayTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.TextBox pulseCountTextBox;
        private System.Windows.Forms.Label pulseCountLabel;
    }
}

