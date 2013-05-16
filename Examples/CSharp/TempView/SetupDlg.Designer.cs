namespace TempView
{
    partial class SetupDlg
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
            this.channelComboBox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.descriptionTextBox = new System.Windows.Forms.TextBox();
            this.okButton = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.browseButton = new System.Windows.Forms.Button();
            this.tcTypeComboBox = new System.Windows.Forms.ComboBox();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.logFileTextBox = new System.Windows.Forms.TextBox();
            this.logDataCheckBox = new System.Windows.Forms.CheckBox();
            this.tcUnitsComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.samplePeriodNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.samplePeriodNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // channelComboBox
            // 
            this.channelComboBox.Location = new System.Drawing.Point(15, 61);
            this.channelComboBox.Name = "channelComboBox";
            this.channelComboBox.Size = new System.Drawing.Size(121, 21);
            this.channelComboBox.TabIndex = 81;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Location = new System.Drawing.Point(12, 42);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(102, 24);
            this.label4.TabIndex = 82;
            this.label4.Text = "Channel";
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.Location = new System.Drawing.Point(176, 15);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(251, 21);
            this.deviceComboBox.TabIndex = 80;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(120, 15);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 20);
            this.label3.TabIndex = 83;
            this.label3.Text = "Device:";
            // 
            // descriptionTextBox
            // 
            this.descriptionTextBox.Location = new System.Drawing.Point(176, 132);
            this.descriptionTextBox.Multiline = true;
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.Size = new System.Drawing.Size(251, 93);
            this.descriptionTextBox.TabIndex = 79;
            this.descriptionTextBox.TextChanged += new System.EventHandler(this.OnDescriptionChanged);
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(365, 235);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(62, 23);
            this.okButton.TabIndex = 78;
            this.okButton.Text = "OK";
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Location = new System.Drawing.Point(176, 114);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(123, 25);
            this.label6.TabIndex = 84;
            this.label6.Text = "Description";
            // 
            // browseButton
            // 
            this.browseButton.Location = new System.Drawing.Point(394, 84);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(33, 23);
            this.browseButton.TabIndex = 77;
            this.browseButton.Text = "...";
            this.browseButton.Click += new System.EventHandler(this.OnBrowseButtonClicked);
            // 
            // tcTypeComboBox
            // 
            this.tcTypeComboBox.Location = new System.Drawing.Point(15, 106);
            this.tcTypeComboBox.Name = "tcTypeComboBox";
            this.tcTypeComboBox.Size = new System.Drawing.Size(121, 21);
            this.tcTypeComboBox.TabIndex = 72;
            this.tcTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnTcTypeChanged);
            // 
            // logFileTextBox
            // 
            this.logFileTextBox.Location = new System.Drawing.Point(176, 86);
            this.logFileTextBox.Name = "logFileTextBox";
            this.logFileTextBox.Size = new System.Drawing.Size(207, 20);
            this.logFileTextBox.TabIndex = 75;
            this.logFileTextBox.TextChanged += new System.EventHandler(this.OnLogFileChanged);
            // 
            // logDataCheckBox
            // 
            this.logDataCheckBox.BackColor = System.Drawing.Color.Transparent;
            this.logDataCheckBox.Location = new System.Drawing.Point(176, 63);
            this.logDataCheckBox.Name = "logDataCheckBox";
            this.logDataCheckBox.Size = new System.Drawing.Size(105, 17);
            this.logDataCheckBox.TabIndex = 76;
            this.logDataCheckBox.Text = "Log Data";
            this.logDataCheckBox.UseVisualStyleBackColor = false;
            this.logDataCheckBox.CheckedChanged += new System.EventHandler(this.OnLogDataCheckChanged);
            // 
            // tcUnitsComboBox
            // 
            this.tcUnitsComboBox.Location = new System.Drawing.Point(15, 152);
            this.tcUnitsComboBox.Name = "tcUnitsComboBox";
            this.tcUnitsComboBox.Size = new System.Drawing.Size(121, 21);
            this.tcUnitsComboBox.TabIndex = 73;
            this.tcUnitsComboBox.SelectedIndexChanged += new System.EventHandler(this.OnTcUnitsChanged);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(12, 132);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 17);
            this.label1.TabIndex = 85;
            this.label1.Text = "Temperature Units";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(12, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 24);
            this.label2.TabIndex = 86;
            this.label2.Text = "Thermocouple Type";
            // 
            // samplePeriodNumericUpDown
            // 
            this.samplePeriodNumericUpDown.Location = new System.Drawing.Point(15, 201);
            this.samplePeriodNumericUpDown.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.samplePeriodNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.samplePeriodNumericUpDown.Name = "samplePeriodNumericUpDown";
            this.samplePeriodNumericUpDown.Size = new System.Drawing.Size(120, 20);
            this.samplePeriodNumericUpDown.TabIndex = 74;
            this.samplePeriodNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.samplePeriodNumericUpDown.Click += new System.EventHandler(this.OnSamplePeriodChanged);
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Location = new System.Drawing.Point(12, 183);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(148, 15);
            this.label5.TabIndex = 87;
            this.label5.Text = "Sample Period (sec)";
            // 
            // SetupDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(439, 273);
            this.Controls.Add(this.channelComboBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.deviceComboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.descriptionTextBox);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.tcTypeComboBox);
            this.Controls.Add(this.logFileTextBox);
            this.Controls.Add(this.logDataCheckBox);
            this.Controls.Add(this.tcUnitsComboBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.samplePeriodNumericUpDown);
            this.Controls.Add(this.label5);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SetupDlg";
            this.Text = "SetupDlg";
            ((System.ComponentModel.ISupportInitialize)(this.samplePeriodNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox channelComboBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox deviceComboBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox descriptionTextBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.ComboBox tcTypeComboBox;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.TextBox logFileTextBox;
        private System.Windows.Forms.CheckBox logDataCheckBox;
        private System.Windows.Forms.ComboBox tcUnitsComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown samplePeriodNumericUpDown;
        private System.Windows.Forms.Label label5;

    }
}