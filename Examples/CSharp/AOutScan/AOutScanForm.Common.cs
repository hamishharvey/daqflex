using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using MeasurementComputing.DAQFlex;

namespace AOutScan
{
    public partial class AOutScanForm
    {
        private DaqDevice Device;
        private bool Stop;
        private string SelectedRange;
        private int FifoSize;
        private bool SupportsAoScan;

        protected override void OnLoad(EventArgs e)
        {
            // Get a list of devices
            string[] deviceNames = DaqDeviceManager.GetDeviceNames(DeviceNameFormat.NameAndSerno);

            if (deviceNames.Length > 0)
            {
                try
                {
                    foreach (string name in deviceNames)
                        deviceComboBox.Items.Add(name);

                    deviceComboBox.SelectedIndex = 0;

                }
                catch (Exception ex)
                {
                    statusLabel.Text = ex.Message;
                }
            }
            else
            {
                statusLabel.Text = "No devices detected";
                EnableControls(false);
            }

            base.OnLoad(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (Device != null && SupportsAoScan)
                Device.SendMessage("AOSCAN:STOP");

            base.OnClosing(e);
        }

        private void InitializeControls()
        {
            // Check if AISCAN is supported
            DaqResponse response = Device.SendMessage("@AOSCAN:MAXSCANRATE");

            if (!response.ToString().Contains("NOT_SUPPORTED"))
            {
                SupportsAoScan = true;

                statusLabel.Text = String.Empty;

                EnableControls(true);

                FifoSize = (int)Device.SendMessage("@AOSCAN:FIFOSIZE").ToValue();

                response = Device.SendMessage("@AO:CHANNELS");

                int channels = (int)response.ToValue();

                lowChannelComboBox.Items.Clear();
                highChannelComboBox.Items.Clear();

                for (int i = 0; i < channels; i++)
                {
                    lowChannelComboBox.Items.Add(i.ToString());
                    highChannelComboBox.Items.Add(i.ToString());
                }

                lowChannelComboBox.SelectedIndex = 0;
                highChannelComboBox.SelectedIndex = 0;

                // Get supported ranges
                string supportedRanges = Device.SendMessage("@AO:RANGES").ToString();
                string ranges = supportedRanges.Substring(supportedRanges.IndexOf('%') + 1);
                string[] rangeList = ranges.Split(CultureInfo.CurrentCulture.TextInfo.ListSeparator.ToCharArray());

                rangeComboBox.Items.Clear();

                foreach (string range in rangeList)
                    rangeComboBox.Items.Add(range);

                if (supportedRanges.Contains("FIXED"))
                    rangeComboBox.Enabled = false;

                rangeComboBox.SelectedIndex = 0;

                statusLabel.Text = String.Empty;
            }
            else
            {
                SupportsAoScan = false;
                EnableControls(false);
                statusLabel.Text = "The selected device does not support analog output scan!";
            }
        }

        private void OnDeviceChanged(object sender, EventArgs e)
        {
            try
            {
                // Release the device
                if (Device != null)
                    DaqDeviceManager.ReleaseDevice(Device);

                string name = deviceComboBox.SelectedItem.ToString();

                // Create a new device object
                Cursor.Current = Cursors.WaitCursor;
                Device = DaqDeviceManager.CreateDevice(name);

                InitializeControls();

                Cursor.Current = Cursors.Default;
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void OnRangeChanged(object sender, EventArgs e)
        {
            try
            {
                // Send the AO Range message
                if (rangeComboBox.Enabled)
                {
                    SelectedRange = rangeComboBox.SelectedItem.ToString();
                    string message = "AOSCAN:RANGE=" + SelectedRange;
                    Device.SendMessage(message);
                }
                else
                {
                    SelectedRange = rangeComboBox.SelectedItem.ToString();
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void OnStartButtonClicked(object sender, EventArgs e)
        {
            try
            {
                Stop = false;

                // get the D/A max count
                int maxCount = (int)Device.SendMessage("@AO:MAXCOUNT").ToValue();

                int samples;
                int channelCount = highChannelComboBox.SelectedIndex - lowChannelComboBox.SelectedIndex + 1;
                int bytesPerSample = GetBytesPerSample(maxCount);

                if (finiteRadioButton.Checked)
                    samples = Convert.ToInt32(samplesTextBox.Text);
                else
                    samples = 4 * (FifoSize / channelCount / bytesPerSample);

                // Configure the scan
                Device.SendMessage("AOSCAN:LOWCHAN=" + lowChannelComboBox.SelectedItem.ToString());
                Device.SendMessage("AOSCAN:HIGHCHAN=" + highChannelComboBox.SelectedItem.ToString());
                Device.SendMessage("AOSCAN:CAL=DISABLE");
                Device.SendMessage("AOSCAN:SCALE=DISABLE");
                Device.SendMessage("AOSCAN:RATE=" + rateTextBox.Text);

 
                // allcate the data array
                double[,] scanData = new double[channelCount, samples];

                // construct a sine wave
                double increment = (2.0 * Math.PI) / samples;
                double angle = 0.0;

                for (int i = 0; i < channelCount; i++)
                {
                    for (int j = 0; j < samples; j++)
                    {
                        scanData[i, j] = (int)((maxCount / 2) + ((maxCount / 2) * Math.Sin(angle + (i * Math.PI / 2))));
                        angle += increment;
                    }
                }

                // set the buffer size in bytes
                int bufSize = GetBufferSize(maxCount, channelCount, samples);

                Device.SendMessage("AOSCAN:BUFSIZE=" + bufSize.ToString());

                // write the scan data
                Device.WriteScanData(scanData, samples, 0);

                if (finiteRadioButton.Checked)
                    Device.SendMessage("AOSCAN:SAMPLES=" + samplesTextBox.Text);
                else
                    Device.SendMessage("AOSCAN:SAMPLES=0");

                // Start the scan
                Device.SendMessage("AOSCAN:START");

                string scanCount;
                string scanIndex;
                string status;

                do
                {
                    // Read and display status
                    status = Device.SendMessage("?AOSCAN:STATUS").ToString();
                    scanCount = Device.SendMessage("?AOSCAN:COUNT").ToString();
                    scanIndex = Device.SendMessage("?AOSCAN:INDEX").ToString();

                    statusLabel.Text = String.Format("{0}   {1}", scanCount, scanIndex);

                    System.Threading.Thread.Sleep(1);
                    Application.DoEvents();

                } while (!Stop && status.Contains("RUNNING"));

                status = Device.SendMessage("?AOSCAN:STATUS").ToString();

                if (status.Contains("IDLE"))
                    statusLabel.Text = "Scan complete";
                else
                    statusLabel.Text = status;
            }
            catch (DaqException ex)
            {
                statusLabel.Text = ex.Message;
                Device.SendMessage("AOSCAN:STOP");
            }
        }

        private void OnStopButtonClicked(object sender, EventArgs e)
        {
            Device.SendMessage("AOSCAN:STOP");
            Stop = true;
        }

        private int GetBufferSize(int maxCount, int channelCount, int samples)
        {
            int bytesPerSample = 0;
            int bufferSize;

            if (finiteRadioButton.Checked)
            {
                // for finite mode use this calculation
                bytesPerSample = GetBytesPerSample(maxCount);
                bufferSize = (bytesPerSample * channelCount * samples);
            }
            else
            {
                // for continous mode use this calculation
                bufferSize = 4 * FifoSize;
            }

            return bufferSize;
        }

        private void EnableControls(bool enableState)
        {
            lowChannelComboBox.Enabled = enableState;
            highChannelComboBox.Enabled = enableState;
            rateTextBox.Enabled = enableState;
            samplesTextBox.Enabled = enableState;
            startButton.Enabled = enableState;
            stopButton.Enabled = enableState;
            finiteRadioButton.Enabled = enableState;
            continuousRadioButton.Enabled = enableState;

            if (rangeComboBox.Items.Count > 1)
                rangeComboBox.Enabled = enableState;
            else
                rangeComboBox.Enabled = false;
        }

        private int GetBytesPerSample(int maxCount)
        {
            int bytesPerSample = 0;

            if (maxCount == 4095)
                bytesPerSample = 2;
            if (maxCount == 65535)
                bytesPerSample = 2;

            return bytesPerSample;
        }
    }
}
