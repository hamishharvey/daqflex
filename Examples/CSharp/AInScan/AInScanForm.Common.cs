using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using MeasurementComputing.DAQFlex;

namespace AInScan
{
    public partial class AInScanForm
    {
        private DaqDevice Device;
        private bool Stop;
        private string DataDisplay;

        protected override void OnLoad(EventArgs e)
        {
            // Get a list of devices
            string[] deviceNames = DaqDeviceManager.GetDeviceNames(DeviceNameFormat.NameAndSerno);

            try
            {
                if (deviceNames.Length > 0)
                {
                    foreach (string name in deviceNames)
                        deviceComboBox.Items.Add(name);

                    deviceComboBox.SelectedIndex = 0;
                }
                else
                {
                    EnableControls(false);
                    statusLabel.Text = "No devices detected!";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }

            base.OnLoad(e);
        }

        private void InitializeControls()
        {
            // Check if AISCAN is supported
            DaqResponse response = Device.SendMessage("@AISCAN:MAXSCANRATE");

            if (!response.ToString().Contains("NOT_SUPPORTED"))
            {
                response = Device.SendMessage("@AI:CHANNELS");

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
                string ranges = Device.SendMessage("@AI{0}:RANGES").ToString();

                ranges = ranges.Substring(ranges.IndexOf('%') + 1);
                string[] rangeList = ranges.Split(CultureInfo.CurrentCulture.TextInfo.ListSeparator.ToCharArray());

                rangeComboBox.Items.Clear();

                foreach (string range in rangeList)
                    rangeComboBox.Items.Add(range);

                rangeComboBox.SelectedIndex = 0;

                EnableControls(true);
                statusLabel.Text = String.Empty;
            }
            else
            {
                EnableControls(false);
                statusLabel.Text = "The selected device does not support analog input scan!";
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
                // Send the AI Range message for devices that support programmable ranges
                if (rangeComboBox.Enabled)
                {
                    string rangeValue = rangeComboBox.SelectedItem.ToString();
                    string message = "AISCAN:RANGE=" + rangeValue;
                    Device.SendMessage(message);
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
                int channelCount = highChannelComboBox.SelectedIndex - lowChannelComboBox.SelectedIndex + 1;
                int samples = Convert.ToInt32(samplesTextBox.Text);

                // Configure the scan
                Device.SendMessage("AISCAN:LOWCHAN=" + lowChannelComboBox.SelectedItem.ToString());
                Device.SendMessage("AISCAN:HIGHCHAN=" + highChannelComboBox.SelectedItem.ToString());
                Device.SendMessage("AISCAN:CAL=ENABLE");
                Device.SendMessage("AISCAN:SCALE=ENABLE");
                Device.SendMessage("AISCAN:RATE=" + rateTextBox.Text);

                if (finiteRadioButton.Checked)
                {
                    Stop = true;
                    Device.SendMessage("AISCAN:SAMPLES=" + samplesTextBox.Text);
                }
                else
                {
                    Stop = false;
                    Device.SendMessage("AISCAN:SAMPLES=0");
                }

                // Start the scan
                Device.SendMessage("AISCAN:START");

                double[,] scanData;
                string scanCount;
                string scanIndex;

                do
                {
                    // Read and display data and status
                    scanData = Device.ReadScanData(samples, 0);

                    scanCount = Device.SendMessage("?AISCAN:COUNT").ToString();
                    scanIndex = Device.SendMessage("?AISCAN:INDEX").ToString();

                    DataDisplay = String.Empty;

                    for (int i = 0; i < Math.Min(100, samples); i++)
                    {
                        for (int j = 0; j < channelCount; j++)
                        {
                            DataDisplay += scanData[j, i].ToString("F03") + "  ";
                        }

                        DataDisplay += Environment.NewLine;
                    }

                    ScanDataTextBox.Text = DataDisplay;
                    statusLabel.Text = String.Format("{0}   {1}", scanCount, scanIndex);

                    System.Threading.Thread.Sleep(1);
                    Application.DoEvents();

                } while (!Stop);
            }
            catch (DaqException ex)
            {
                statusLabel.Text = ex.Message;
                Device.SendMessage("AISCAN:STOP");
            }
        }

        private void OnStopButtonClicked(object sender, EventArgs e)
        {
            Device.SendMessage("AISCAN:STOP");
            Stop = true;
        }

        private void EnableControls(bool enableState)
        {
            lowChannelComboBox.Enabled = enableState;
            highChannelComboBox.Enabled = enableState;
            rateTextBox.Enabled = enableState;
            rangeComboBox.Enabled = enableState;
            samplesTextBox.Enabled = enableState;
            startButton.Enabled = enableState;
            stopButton.Enabled = enableState;
            finiteRadioButton.Enabled = enableState;
            continuousRadioButton.Enabled = enableState;
        }
    }
}
