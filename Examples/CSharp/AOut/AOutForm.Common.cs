using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using MeasurementComputing.DAQFlex;

namespace AOut
{
    public partial class AOutForm
    {
        private DaqDevice Device;
        private int Channel;
        private string ChannelSpec;
        private double MinVolts;
        private double MaxVolts;

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
            // Get number of supported channels
            DaqResponse response = Device.SendMessage("@AO:CHANNELS");

            if (!response.ToString().Contains("NOT_SUPPORTED"))
            {
                EnableControls(true);

                int channels = (int)response.ToValue();

                channelComboBox.Items.Clear();

                for (int i = 0; i < channels; i++)
                    channelComboBox.Items.Add(i.ToString());

                channelComboBox.SelectedIndex = 0;

                // Get supported ranges
                string msg = String.Format("@AO{0}:RANGES", ChannelSpec);
                string ranges = Device.SendMessage(msg).ToString();

                if (ranges.Contains("FIXED") || ranges.Contains("HWSEL"))
                    rangeComboBox.Enabled = false;

                ranges = ranges.Substring(ranges.IndexOf('%') + 1);
                string[] rangeList = ranges.Split(CultureInfo.CurrentCulture.TextInfo.ListSeparator.ToCharArray());

                rangeComboBox.Items.Clear();

                foreach (string range in rangeList)
                    rangeComboBox.Items.Add(range);

                rangeComboBox.SelectedIndex = 0;

                statusLabel.Text = String.Empty;
            }
            else
            {
                EnableControls(false);
                statusLabel.Text = "The selected device does not support analog output";
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

        private void OnChannelChanged(object sender, EventArgs e)
        {
            Channel = channelComboBox.SelectedIndex;
            ChannelSpec = "{" + Channel.ToString() + "}";
        }

        private void OnRangeChanged(object sender, EventArgs e)
        {
            try
            {
                string rangeValue = rangeComboBox.SelectedItem.ToString();
                string message = String.Format("AO{0}:RANGE={1}", ChannelSpec, rangeValue);
                GetMinMaxVolts(message, out MinVolts, out MaxVolts);

                if (rangeComboBox.Enabled)
                {
                    // Send the AO Range message
                    Device.SendMessage(message);
                }

                // Set range of track bar
                valueTrackBar.Minimum = (int)(1000 * MinVolts);
                valueTrackBar.Maximum = (int)(1000 * MaxVolts);
                valueTrackBar.SmallChange = valueTrackBar.Maximum / 100;
                valueTrackBar.LargeChange = valueTrackBar.Maximum / 10;
                valueTrackBar.TickFrequency = valueTrackBar.LargeChange;
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void OnValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Write the AO value
                double value = (double)valueTrackBar.Value / 1000.0;
                valueLabel.Text = String.Format("{0:F5}V", value);
                string message = String.Format("AO{0}:VALUE/VOLTS={1}", ChannelSpec, value);
                Device.SendMessage(message);
                statusLabel.Text = String.Empty;
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void EnableControls(bool enableState)
        {
            channelComboBox.Enabled = enableState;
            valueTrackBar.Enabled = enableState;
            rangeComboBox.Enabled = enableState;
        }

        private void GetMinMaxVolts(string range, out double minVolts, out double maxVolts)
        {
            string value = range.Substring(range.IndexOf("=") + 1);

            int maxCount = (int)Device.SendMessage("@AO:MAXCOUNT").ToValue();

            switch (value)
            {
                case ("BIP10V"):
                    minVolts = -10.0;
                    maxVolts = 10.0 - (20.0 / (double)maxCount );
                    break;
                case ("UNI10V"):
                    minVolts = 0.0;
                    maxVolts = 10.0 - (10.0 / (double)maxCount );
                    break;
                case ("UNI4.096V"):
                    minVolts = 0.0;
                    maxVolts = 4.096 - (4.096 / (double)maxCount );;
                    break;
                default:
                    minVolts = 0.0;
                    maxVolts = 5.0 - (5.0 / (double)maxCount);
                    break;
            }
        }
    }
}
