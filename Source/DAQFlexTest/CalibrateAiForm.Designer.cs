namespace MeasurementComputing.DAQFlex.Test
{
    partial class CalibrateAiForm
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
            this.startButton = new System.Windows.Forms.Button();
            this.calProgressBar = new System.Windows.Forms.ProgressBar();
            this.okButton = new System.Windows.Forms.Button();
            this.calProgressLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(29, 26);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(76, 26);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.OnStart);
            // 
            // calProgressBar
            // 
            this.calProgressBar.Location = new System.Drawing.Point(29, 75);
            this.calProgressBar.Name = "calProgressBar";
            this.calProgressBar.Size = new System.Drawing.Size(407, 20);
            this.calProgressBar.TabIndex = 3;
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(360, 133);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(76, 26);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "OK";
            this.okButton.Click += new System.EventHandler(this.OnOk);
            // 
            // calProgressLabel
            // 
            this.calProgressLabel.AutoSize = true;
            this.calProgressLabel.Location = new System.Drawing.Point(29, 109);
            this.calProgressLabel.Name = "calProgressLabel";
            this.calProgressLabel.Size = new System.Drawing.Size(0, 13);
            this.calProgressLabel.TabIndex = 0;
            // 
            // CalibrateAiForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(463, 171);
            this.Controls.Add(this.calProgressLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.calProgressBar);
            this.Controls.Add(this.startButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "CalibrateAiForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Ai Self Calibration";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.ProgressBar calProgressBar;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label calProgressLabel;
    }
}