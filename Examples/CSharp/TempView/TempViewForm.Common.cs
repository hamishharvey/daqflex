using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
//#if WindowsCE
using System.ComponentModel;
//#endif
using MeasurementComputing.DAQFlex;

namespace TempView
{
    public partial class TempViewForm
    {
        public const string DEG_C = "Deg C";
        public const string DEG_F = "Deg F";

        private DaqDevice DaqDevice;
        private int SamplePeriod = 1;
        private bool LogData;
        private string SelectedUnits;
        private string DeviceName;
        private string DeviceSerno;

        private SetupDlg SetupDialog = new SetupDlg();
        private Graph Graph;
        private int SampleCount;
        private double CurrentValue;
        private string LogFile;
        private string Notes;
        private string TcType;
        private StreamWriter StreamWriter;
        private bool AppendFile = false;

        protected override void OnLoad(EventArgs e)
        {
            SelectedUnits = DEG_C;

            Graph = new Graph(dataGraph, this.Text);

            valueLabel.Text = String.Empty;

            LogFile = String.Empty;
            Notes = String.Empty;
            InitializeGraph();

            startButton.Enabled = false;
            stopButton.Enabled = false;

            yLabel.Text = SelectedUnits;
            xLabel.Text = "Time (min)";

            base.OnLoad(e);
        }

        //=============================================================================
        /// <summary>
        /// Gets setup parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //=============================================================================
        private void OnSetupButtonClicked(object sender, EventArgs e)
        {
            LogData = false;
            LogFile = String.Empty;
            Notes = String.Empty;

            SetupDialog.ShowDialog();

            if (SetupDialog.DaqDevice != null)
            {
                DaqDevice = SetupDialog.DaqDevice;
                DeviceName = SetupDialog.DeviceName.Split(new char[] { ':' })[0];
                DeviceSerno = SetupDialog.DeviceName.Split(new char[] { ':' })[2];
                TcType = SetupDialog.TcType;
                SendDeviceMessage("AI{0}:SENSOR=TC/" + TcType);
                SelectedUnits = SetupDialog.Units;
                SamplePeriod = SetupDialog.SamplePeriod;
                LogData = SetupDialog.LogData;
                Notes = SetupDialog.Description;
                yLabel.Text = SelectedUnits;

                if (LogData)
                {
                    LogFile = SetupDialog.LogFile;
                    Notes = SetupDialog.Description;
                }

                startButton.Enabled = true;
                stopButton.Enabled = true;
            }

        }

        //========================================================================
        /// <summary>
        /// Intializes the graph and graph labels
        /// </summary>
        //========================================================================
        protected void InitializeGraph()
        {
            Graph.IntializeGraph(SamplePeriod, TimeFormat.Minutes);

            Graph.DrawGrid();
        }

        //========================================================================
        /// <summary>
        /// Enables/Disables the form's controls
        /// </summary>
        /// <param name="enabled">The value to enable the controls to</param>
        //========================================================================
        protected void EnableControls(bool enabled)
        {
            setupButton.Enabled = enabled;
            startButton.Enabled = enabled;
        }

        //========================================================================
        /// <summary>
        /// Starts data logging
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">Event args</param>
        //========================================================================
        private void OnStartLogging(object sender, EventArgs e)
        {
            if (LogData && LogFile == String.Empty)
            {
                MessageBox.Show("Please specify a file name for data logging", "TempView", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                return;
            }

            InitializeGraph();

            SampleCount = 0;

            ReadSample(null);

            if (LogData)
            {
                DateTime now = DateTime.Now;

                try
                {
                    int indexOfName = LogFile.LastIndexOf(Path.DirectorySeparatorChar);
                    string path = LogFile.Remove(indexOfName, LogFile.Length - indexOfName);

                    if (!Directory.Exists(path))
                    {
                        if (MessageBox.Show(String.Format("{0} does not exist. Do you wish to create it?", path),
                                        "TempView",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question,
                                        MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                            Directory.CreateDirectory(path);
                        else
                            return;
                    }

                    StreamWriter = new StreamWriter(LogFile, AppendFile);
                    StreamWriter.WriteLine("Start Time: {0}", now);
                    StreamWriter.WriteLine("File: {0}", LogFile);
                    StreamWriter.WriteLine("Device: {0}", DeviceName);
                    StreamWriter.WriteLine("Serial Number: {0}", DeviceSerno);
                    StreamWriter.WriteLine("Thermocouple Type: {0}", TcType);
                    StreamWriter.WriteLine("Units: {0}", SelectedUnits);
                    StreamWriter.WriteLine("Sample Period: {0} sec", SamplePeriod);

                    if (Notes != String.Empty)
                        StreamWriter.WriteLine("Description: {0}", Notes);

                    StreamWriter.WriteLine(Environment.NewLine);
                    StreamWriter.WriteLine("Time{0}Temperature", "\t\t");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "TempView", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    return;
                }
            }

            EnableControls(false);
            stopButton.Enabled = true;
            sampleTimer.Interval = 1000 * SamplePeriod;
            sampleTimer.Enabled = true;
        }

        //========================================================================
        /// <summary>
        /// Stops data loggin
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">Event args</param>
        //========================================================================
        private void OnStopLogging(object sender, EventArgs e)
        {
            EnableControls(true);

            if (LogData && StreamWriter != null)
                StreamWriter.Close();

            sampleTimer.Enabled = false;
        }

        //========================================================================
        /// <summary>
        /// Event handler for the sample timer
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">Event args</param>
        //========================================================================
        private void OnSampleTimerTick(object sender, EventArgs e)
        {
            ReadSample(sender);
        }

        //========================================================================
        /// <summary>
        /// Reads a sample from the device and updates the logging and charting
        /// </summary>
        /// <param name="sender">The sender (sampleTimer or null)</param>
        //========================================================================
        private void ReadSample(object sender)
        {
            try
            {
                if (SelectedUnits == DEG_C)
                    CurrentValue = DaqDevice.SendMessage("?AI{0}:VALUE/DEGC").ToValue();
                else
                    CurrentValue = DaqDevice.SendMessage("?AI{0}:VALUE/DEGF").ToValue();

                if (LogData && sender != null && StreamWriter != null)
                    StreamWriter.WriteLine(String.Format("{0}{1}{2}", SampleCount * SamplePeriod, "\t\t", CurrentValue));

                if (CurrentValue == -9999.0)
                    valueLabel.Text = "OTD";
                else
                    valueLabel.Text = String.Format("{0} {1}", CurrentValue.ToString("F02"), SelectedUnits);

                SampleCount++;

                Graph.DrawPlot(CurrentValue);
            }
            catch (DaqException e)
            {
                OnStopLogging(null, null);

                MessageBox.Show(e.Message, "TempView", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
        }

        //=============================================================================
        /// <summary>
        /// Sends a message to the device using the DAQFlex SendMessage method
        /// </summary>
        /// <param name="message">The message to send to the device</param>
        //=============================================================================
        private void SendDeviceMessage(string message)
        {
            try
            {
                DaqDevice.SendMessage(message);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                OnStopLogging(null, null);
            }
        }

        //========================================================================
        /// <summary>
        /// Event handler for the form closing event
        /// </summary>
        /// <param name="e">Event args</param>
        //========================================================================
        protected override void OnClosing(CancelEventArgs e)
        {
            OnStopLogging(null, null);
            base.OnClosing(e);
        }
    }
}
