using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using MeasurementComputing.DAQFlex;

namespace AInScanWithCallback
{
    public partial class AInScanForm
    {
        private DaqDevice Device;
        private bool Stop;
        private string DataDisplay;

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                // Get a list of devices
                string[] deviceNames = DaqDeviceManager.GetDeviceNames(DeviceNameFormat.NameAndSerno);

                foreach (string name in deviceNames)
                    deviceComboBox.Items.Add(name);

                if (deviceNames.Length > 0)
                {
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

                Device.EnableCallback(ScanComplete, CallbackType.OnInputScanComplete, null);
                Device.EnableCallback(ScanError, CallbackType.OnInputScanError, null);

                samplesTextBox.Text = "1000";
                rateTextBox.Text = "100";

                EnableControls(true);
                statusLabel.Text = String.Empty;
            }
            else
            {
                EnableControls(false);
                statusLabel.Text = "The selected device does not support analog input scan!";
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (Device != null)
            {
                Device.DisableCallback(CallbackType.OnInputScanComplete);
                Device.DisableCallback(CallbackType.OnInputScanError);
            }

            base.OnClosing(e);
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
                if (rangeComboBox.Items.Count > 1)
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
                Stop = false;

                int callbackCount = Int32.Parse(callbackCountTextBox.Text);

                Device.DisableCallback(CallbackType.OnDataAvailable);
                Device.EnableCallback(ReadScanData, CallbackType.OnDataAvailable, callbackCount);

                // Configure the scan
                Device.SendMessage("AISCAN:XFRMODE=BLOCKIO");
                Device.SendMessage("AISCAN:LOWCHAN=" + lowChannelComboBox.SelectedItem.ToString());
                Device.SendMessage("AISCAN:HIGHCHAN=" + highChannelComboBox.SelectedItem.ToString());
                Device.SendMessage("AISCAN:CAL=ENABLE");
                Device.SendMessage("AISCAN:SCALE=ENABLE");
                Device.SendMessage("AISCAN:RATE=" + rateTextBox.Text);

                if (finiteRadioButton.Checked)
                {
                    Device.SendMessage("AISCAN:SAMPLES=" + samplesTextBox.Text);
                }
                else
                {
                    Device.SendMessage("AISCAN:SAMPLES=0");
                }

                // Start the scan
                Device.SendMessage("AISCAN:START");

                do
                {
                    statusLabel.Text = Device.SendMessage("?AISCAN:COUNT").ToString();
                    System.Threading.Thread.Sleep(50);
                    Application.DoEvents();

                } while (!Stop);

                Device.SendMessage("AISCAN:STOP");

            }
            catch (DaqException ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        protected void ReadScanData(ErrorCodes errorCode, CallbackType callbackType, object callbackData)
        {
            int availableSamples = (int)callbackData;

            double[,] scanData;

            try
            {
                scanData = Device.ReadScanData(availableSamples, 0);

                int channels = scanData.GetLength(0);
                int samples = scanData.GetLength(1);

                DataDisplay = String.Empty;

                for (int i = 0; i < Math.Min(100, samples); i++)
                {
                    for (int j = 0; j < channels; j++)
                    {
                        DataDisplay += scanData[j, i].ToString("F03") + " ";
                    }

                    DataDisplay += Environment.NewLine;
                }

                ScanDataTextBox.Text = DataDisplay;
            }
            catch (Exception ex)
            {
                Stop = true;
                statusLabel.Text = ex.Message;
            }
        }

        protected void ScanComplete(ErrorCodes errorCode, CallbackType callbackType, object callbackData)
        {
            Stop = true;

            if (errorCode == ErrorCodes.NoErrors)
                statusLabel.Text = "Scan complete";
        }

        protected void ScanError(ErrorCodes errorCode, CallbackType callbackType, object callbackData)
        {
            Stop = true;
            statusLabel.Text = Device.GetErrorMessage(errorCode);
        }

        private void OnStopButtonClicked(object sender, EventArgs e)
        {
            Stop = true;
            Device.SendMessage("AISCAN:STOP");
        }

        private void OnRadioButtonChecked(object sender, EventArgs e)
        {
            if (continuousRadioButton.Checked)
                samplesTextBox.Enabled = false;
            else
                samplesTextBox.Enabled = true;
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
            callbackCountTextBox.Enabled = enableState;
            continuousRadioButton.Enabled = enableState;
        }
    }
}
