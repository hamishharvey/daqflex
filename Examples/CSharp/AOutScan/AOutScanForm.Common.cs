using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using MeasurementComputing.DAQFlex;

namespace AOutScan
{
    public partial class AOutScanForm
    {
        private DaqDevice Device;
        private bool Stop;
        private string SelectedRange;

        protected override void OnLoad(EventArgs e)
        {
            // Get a list of devices
            string[] deviceNames = DaqDeviceManager.GetDeviceNames(DeviceNameFormat.NameAndSerno);

            try
            {
                foreach (string name in deviceNames)
                    deviceComboBox.Items.Add(name);

                deviceComboBox.SelectedIndex = 0;

                // Check if AISCAN is supported
                DaqResponse response = Device.SendMessage("@AOSCAN:MAXSCANRATE");

                if (!response.ToString().Contains("NOT_SUPPORTED"))
                {
                    response = Device.SendMessage("@AO:CHANNELS");

                    int channels = (int)response.ToValue();

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
                    string[] rangeList = ranges.Split(new char[] { ',' });

                    foreach (string range in rangeList)
                        rangeComboBox.Items.Add(range);

                    if (supportedRanges.Contains("FIXED"))
                        rangeComboBox.Enabled = false;

                    rangeComboBox.SelectedIndex = 0;
                }
                else
                {
                    lowChannelComboBox.Enabled = false;
                    highChannelComboBox.Enabled = false;
                    rateTextBox.Enabled = false;
                    rangeComboBox.Enabled = false;
                    samplesTextBox.Enabled = false;
                    startButton.Enabled = false;
                    stopButton.Enabled = false;
                    finiteRadioButton.Enabled = false;
                    continuousRadioButton.Enabled = false;
                    statusLabel.Text = "The selected device does not support analog output scan!";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }

            base.OnLoad(e);
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
                Device = DaqDeviceManager.CreateDevice(name);
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

                int channelCount = highChannelComboBox.SelectedIndex - lowChannelComboBox.SelectedIndex + 1;
                int samples = Convert.ToInt32(samplesTextBox.Text);

                // Configure the scan
                Device.SendMessage("AOSCAN:LOWCHAN=" + lowChannelComboBox.SelectedItem.ToString());
                Device.SendMessage("AOSCAN:HIGHCHAN=" + highChannelComboBox.SelectedItem.ToString());
                Device.SendMessage("AOSCAN:SCALE=DISABLE");
                Device.SendMessage("AOSCAN:RATE=" + rateTextBox.Text);

                // get the D/A max count
                int maxCount = (int)Device.SendMessage("@AO:MAXCOUNT").ToValue();
 
                // set the number of samples to twice the fifo size for continuous mode
                if (continuousRadioButton.Checked)
                    samples = 2 * (int)Device.SendMessage("@AOSCAN:FIFOSIZE").ToValue();

                // allcate the data array
                double[,] scanData = new double[channelCount, samples];

                // construct a sine wave
                double increment = (2.0 * Math.PI) / samples;
                double angle = 0.0;

                if (SelectedRange.Contains("BIP"))
                {
                    // bipolar range
                    for (int i = 0; i < channelCount; i++)
                    {
                        for (int j = 0; j < samples; j++)
                        {
                            scanData[i, j] = (int)(maxCount * Math.Sin(angle + (i * Math.PI/2)));
                            angle += increment;
                        }
                    }
                }
                else
                {
                    // unipolar range
                    for (int i = 0; i < channelCount; i++)
                    {
                        for (int j = 0; j < samples; j++)
                        {
                            scanData[i, j] = (int)((maxCount / 2) + ((maxCount / 2) * Math.Sin(angle + (i * Math.PI/2))));
                            angle += increment;
                        }
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

        private void OnSampleModeChanged(object sender, EventArgs e)
        {
            if (finiteRadioButton.Checked)
                samplesTextBox.Enabled = true;
            else
                samplesTextBox.Enabled = false;
        }

        private int GetBufferSize(int maxCount, int channelCount, int samples)
        {
            int bytesPerSample = 0;
            int count = maxCount;

            do
            {
                count = count >> 8;
                bytesPerSample++;
            } while (count > 0);

            return bytesPerSample * channelCount * samples;
        }
    }
}
