namespace AOut
{
    partial class AOutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AOutForm));
            this.valueLabel = new System.Windows.Forms.Label();
            this.valueTrackBar = new System.Windows.Forms.TrackBar();
            this.rangeLabel = new System.Windows.Forms.Label();
            this.rangeComboBox = new System.Windows.Forms.ComboBox();
            this.channelLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.deviceLabel = new System.Windows.Forms.Label();
            this.channelComboBox = new System.Windows.Forms.ComboBox();
            this.deviceComboBox = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.valueTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // valueLabel
            // 
            this.valueLabel.Location = new System.Drawing.Point(235, 136);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(100, 20);
            this.valueLabel.TabIndex = 24;
            this.valueLabel.Text = "0";
            // 
            // valueTrackBar
            // 
            this.valueTrackBar.Location = new System.Drawing.Point(17, 125);
            this.valueTrackBar.Name = "valueTrackBar";
            this.valueTrackBar.Size = new System.Drawing.Size(203, 45);
            this.valueTrackBar.TabIndex = 28;
            this.valueTrackBar.Scroll += new System.EventHandler(this.OnValueChanged);
            // 
            // rangeLabel
            // 
            this.rangeLabel.Location = new System.Drawing.Point(7, 74);
            this.rangeLabel.Name = "rangeLabel";
            this.rangeLabel.Size = new System.Drawing.Size(63, 21);
            this.rangeLabel.TabIndex = 29;
            this.rangeLabel.Text = "Range:";
            this.rangeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // rangeComboBox
            // 
            this.rangeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.rangeComboBox.Location = new System.Drawing.Point(76, 71);
            this.rangeComboBox.Name = "rangeComboBox";
            this.rangeComboBox.Size = new System.Drawing.Size(107, 21);
            this.rangeComboBox.TabIndex = 27;
            this.rangeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnRangeChanged);
            // 
            // channelLabel
            // 
            this.channelLabel.Location = new System.Drawing.Point(7, 45);
            this.channelLabel.Name = "channelLabel";
            this.channelLabel.Size = new System.Drawing.Size(63, 21);
            this.channelLabel.TabIndex = 30;
            this.channelLabel.Text = "Channel:";
            this.channelLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(7, 196);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(356, 39);
            this.statusLabel.TabIndex = 31;
            // 
            // deviceLabel
            // 
            this.deviceLabel.Location = new System.Drawing.Point(7, 12);
            this.deviceLabel.Name = "deviceLabel";
            this.deviceLabel.Size = new System.Drawing.Size(63, 20);
            this.deviceLabel.TabIndex = 32;
            this.deviceLabel.Text = "Device:";
            this.deviceLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // channelComboBox
            // 
            this.channelComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.channelComboBox.Location = new System.Drawing.Point(76, 42);
            this.channelComboBox.Name = "channelComboBox";
            this.channelComboBox.Size = new System.Drawing.Size(107, 21);
            this.channelComboBox.TabIndex = 26;
            this.channelComboBox.SelectedIndexChanged += new System.EventHandler(this.OnChannelChanged);
            // 
            // deviceComboBox
            // 
            this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.deviceComboBox.Location = new System.Drawing.Point(76, 12);
            this.deviceComboBox.Name = "deviceComboBox";
            this.deviceComboBox.Size = new System.Drawing.Size(242, 21);
            this.deviceComboBox.TabIndex = 25;
            this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.OnDeviceChanged);
            // 
            // AOutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(370, 247);
            this.Controls.Add(this.valueLabel);
            this.Controls.Add(this.valueTrackBar);
            this.Controls.Add(this.rangeLabel);
            this.Controls.Add(this.rangeComboBox);
            this.Controls.Add(this.channelLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.deviceLabel);
            this.Controls.Add(this.channelComboBox);
            this.Controls.Add(this.deviceComboBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AOutForm";
            this.Text = "AOut";
            ((System.ComponentModel.ISupportInitialize)(this.valueTrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label valueLabel;
        private System.Windows.Forms.TrackBar valueTrackBar;
        private System.Windows.Forms.Label rangeLabel;
        private System.Windows.Forms.ComboBox rangeComboBox;
        private System.Windows.Forms.Label channelLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label deviceLabel;
        private System.Windows.Forms.ComboBox channelComboBox;
        private System.Windows.Forms.ComboBox deviceComboBox;

    }
}

