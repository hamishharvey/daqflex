namespace TempView
{
    partial class TempViewForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TempViewForm));
            this.xLabel = new System.Windows.Forms.Label();
            this.yLabel = new System.Windows.Forms.Label();
            this.setupButton = new System.Windows.Forms.Button();
            this.valueLabel = new System.Windows.Forms.Label();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.dataGraph = new System.Windows.Forms.PictureBox();
            this.sampleTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dataGraph)).BeginInit();
            this.SuspendLayout();
            // 
            // xLabel
            // 
            this.xLabel.Location = new System.Drawing.Point(203, 267);
            this.xLabel.Name = "xLabel";
            this.xLabel.Size = new System.Drawing.Size(121, 20);
            this.xLabel.TabIndex = 49;
            this.xLabel.Text = "xLabel";
            this.xLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // yLabel
            // 
            this.yLabel.Location = new System.Drawing.Point(18, 128);
            this.yLabel.Name = "yLabel";
            this.yLabel.Size = new System.Drawing.Size(35, 20);
            this.yLabel.TabIndex = 50;
            this.yLabel.Text = "yLabel";
            this.yLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // setupButton
            // 
            this.setupButton.BackColor = System.Drawing.Color.Transparent;
            this.setupButton.Location = new System.Drawing.Point(59, 300);
            this.setupButton.Name = "setupButton";
            this.setupButton.Size = new System.Drawing.Size(75, 23);
            this.setupButton.TabIndex = 53;
            this.setupButton.Text = "Setup...";
            this.setupButton.UseVisualStyleBackColor = false;
            this.setupButton.Click += new System.EventHandler(this.OnSetupButtonClicked);
            // 
            // valueLabel
            // 
            this.valueLabel.BackColor = System.Drawing.Color.Transparent;
            this.valueLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.valueLabel.Location = new System.Drawing.Point(216, 9);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(79, 16);
            this.valueLabel.TabIndex = 54;
            this.valueLabel.Text = "value";
            this.valueLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // stopButton
            // 
            this.stopButton.BackColor = System.Drawing.Color.Transparent;
            this.stopButton.Location = new System.Drawing.Point(398, 300);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 23);
            this.stopButton.TabIndex = 52;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = false;
            this.stopButton.Click += new System.EventHandler(this.OnStopLogging);
            // 
            // startButton
            // 
            this.startButton.BackColor = System.Drawing.Color.Transparent;
            this.startButton.Location = new System.Drawing.Point(317, 300);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 51;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = false;
            this.startButton.Click += new System.EventHandler(this.OnStartLogging);
            // 
            // dataGraph
            // 
            this.dataGraph.Location = new System.Drawing.Point(59, 28);
            this.dataGraph.Name = "dataGraph";
            this.dataGraph.Size = new System.Drawing.Size(414, 236);
            this.dataGraph.TabIndex = 55;
            this.dataGraph.TabStop = false;
            // 
            // sampleTimer
            // 
            this.sampleTimer.Tick += new System.EventHandler(this.OnSampleTimerTick);
            // 
            // TempViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(490, 333);
            this.Controls.Add(this.xLabel);
            this.Controls.Add(this.yLabel);
            this.Controls.Add(this.setupButton);
            this.Controls.Add(this.valueLabel);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.dataGraph);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TempViewForm";
            this.Text = "TempView";
            ((System.ComponentModel.ISupportInitialize)(this.dataGraph)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label xLabel;
        private System.Windows.Forms.Label yLabel;
        private System.Windows.Forms.Button setupButton;
        private System.Windows.Forms.Label valueLabel;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.PictureBox dataGraph;
        private System.Windows.Forms.Timer sampleTimer;
    }
}

