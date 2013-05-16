using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using MeasurementComputing.DAQFlex;

namespace AInScan
{
    public partial class AInScanWithTriggerForm
    {
        private DaqDevice Device;
        private bool Stop;
        private string DataDisplay;
        private char[] ValueSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator.ToCharArray();

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
            statusLabel.Text = String.Empty;

            if (Device.SendMessage("@AISCAN:TRIG").ToString().Contains("NOT_SUPPORTED"))
            {
                EnableControls(false);
                statusLabel.Text = "The selected device does not support AITRIG messages!";
                return;
            }

            EnableControls(true);

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
                string[] rangeList = ranges.Split(ValueSeparator);

                rangeComboBox.Items.Clear();

                foreach (string range in rangeList)
                    rangeComboBox.Items.Add(range);

                rangeComboBox.SelectedIndex = 0;

                InitializeTriggerControls(channels);
            }
            else
            {
                EnableControls(false);
                statusLabel.Text = "The selected device does not support AITRIG messages!";
            }
        }

        private void InitializeTriggerControls(int channels)
        {
            // Get supported trigger types
            string types = Device.SendMessage("@AITRIG:TYPES").ToString();
            types = types.Substring(types.IndexOf('%') + 1);
            string[] typeList = types.Split(ValueSeparator);

            triggerTypeComboBox.Items.Clear();

            foreach (string type in typeList)
                triggerTypeComboBox.Items.Add(type);

            string selectedType = Device.SendMessage("?AITRIG:TYPE").ToString();
            selectedType = selectedType.Substring(selectedType.IndexOf('=') + 1);
            triggerTypeComboBox.SelectedItem = selectedType;
            triggerTypeComboBox.Enabled = triggerEnableCheckBox.Checked;

            // Get supported trigger sources
            string sources = Device.SendMessage("@AITRIG:SRCS").ToString();
            sources = sources.Substring(sources.IndexOf('%') + 1);
            string[] sourceList = sources.Split(ValueSeparator);

            triggerSourceComboBox.Items.Clear();

            foreach (string source in sourceList)
                triggerSourceComboBox.Items.Add(source);

            // is the trigger source fixed
            string selectedSource;
            sources = Device.SendMessage("@AITRIG:SRCS").ToString();
            if (sources.Contains("FIXED"))
            {
                // select the fixed source
                selectedSource = sources.Substring(sources.IndexOf('%') + 1);
                triggerSourceComboBox.SelectedItem = selectedSource;
                triggerSourceComboBox.Enabled = false;
            }
            else
            {
                // select the hardware trigger source
                selectedSource = Device.SendMessage("?AITRIG:SRC").ToString();
                selectedSource = selectedSource.Substring(selectedSource.IndexOf('=') + 1);
                triggerSourceComboBox.SelectedItem = selectedSource;
                triggerSourceComboBox.Enabled = true;
            }

            // check for analog trigger support
            if (types.Contains("SWSTART"))
            {
                // add the trigger channels
                for (int i = 0; i < channels; i++)
                {
                    triggerChannelComboBox.Items.Add(i.ToString());
                }

                triggerChannelComboBox.SelectedIndex = 0;
                triggerChannelComboBox.Enabled = triggerEnableCheckBox.Checked;
                triggerLevelNumericUpDown.Enabled = triggerEnableCheckBox.Checked;
            }
            else
            {
                triggerChannelComboBox.Enabled = triggerEnableCheckBox.Checked;
                triggerLevelNumericUpDown.Enabled = triggerEnableCheckBox.Checked;
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
                // Send the AI Range message
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
                ScanDataTextBox.Text = String.Empty;
                ScanDataTextBox.Refresh();

                statusLabel.Text = String.Empty;
                statusLabel.Refresh();

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

                // configure the trigger
                if (triggerEnableCheckBox.Checked)
                {
                    Device.SendMessage("AISCAN:TRIG=ENABLE");

                    Device.SendMessage("AITRIG:TYPE=" + triggerTypeComboBox.Text);

                    string sources = Device.SendMessage("@AITRIG:SRCS").ToString();
                    if (sources.Contains("PROG"))
                    {
                        Device.SendMessage("AITRIG:LEVEL=" + triggerLevelNumericUpDown.Text);

                        string trigSourceMsg = "AITRIG:SRC=" + triggerSourceComboBox.Text;
                        if (triggerSourceComboBox.Text.Contains("SWSTART"))
                            trigSourceMsg += "{" + triggerChannelComboBox.Text + "}";
                        Device.SendMessage(trigSourceMsg);
                    }
                }
                else
                    Device.SendMessage("AISCAN:TRIG=DISABLE");


                // Start the scan
                Device.SendMessage("AISCAN:START");

                double[,] scanData;
                string scanCount;
                string scanIndex;
                int timeOut = 0;

                try
                {
                    timeOut = Int32.Parse(timeOutTextBox.Text);
                }
                catch (Exception)
                {
                }

                do
                {
                    // Read and display data and status
                    scanData = Device.ReadScanData(samples, timeOut);

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
            triggerEnableCheckBox.Enabled = enableState;
            triggerSourceComboBox.Enabled = enableState;
            triggerTypeComboBox.Enabled = enableState;
            triggerChannelComboBox.Enabled = enableState;
            triggerLevelNumericUpDown.Enabled = enableState;
        }

        private void OnTriggerEnableChanged(object sender, EventArgs e)
        {
            // enable/disable the trigger controls 
            triggerTypeComboBox.Enabled = triggerEnableCheckBox.Checked;

            // disable the source control if the source is fixed
            string sources = Device.SendMessage("@AITRIG:SRCS").ToString();
            if (sources.Contains("FIXED"))
                triggerSourceComboBox.Enabled = false;
            else
                triggerSourceComboBox.Enabled = triggerEnableCheckBox.Checked;

            // the channel and level controls will be enabled/disabled 
            // based on the trigger source setting
            if (triggerSourceComboBox.Text.Contains("HWSTART/DIG") || triggerSourceComboBox.Text.Contains("HW/DIG"))
            {
                triggerChannelComboBox.Enabled = false;
                triggerLevelNumericUpDown.Enabled = false;
            }
            else
            {
                triggerChannelComboBox.Enabled = triggerEnableCheckBox.Checked;
                triggerLevelNumericUpDown.Enabled = triggerEnableCheckBox.Checked;
            }
        }

        private void OnTriggerSourceChanged(object sender, EventArgs e)
        {
            if (triggerSourceComboBox.Text.Contains("HWSTART/DIG") || triggerSourceComboBox.Text.Contains("HW/DIG"))
            {
                triggerChannelComboBox.Enabled = false;
                triggerLevelNumericUpDown.Enabled = false;
            }
            else
            {
                triggerChannelComboBox.Enabled = true;
                triggerLevelNumericUpDown.Enabled = true;
            }
        }
    }
}
