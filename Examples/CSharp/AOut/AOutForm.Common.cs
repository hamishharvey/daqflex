using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;

namespace AOut
{
    public partial class AOutForm
    {
        private DaqDevice Device;
        private int Channel;

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

                    // Get number of supported channels
                    DaqResponse response = Device.SendMessage("@AO:CHANNELS");

                    if (!response.ToString().Contains("NOT_SUPPORTED"))
                    {
                        int channels = (int)response.ToValue();

                        for (int i = 0; i < channels; i++)
                            channelComboBox.Items.Add(i.ToString());

                        channelComboBox.SelectedIndex = 0;

                        // Get supported ranges
                        string msg = String.Format("@AO{{0}}:RANGES", channelComboBox.SelectedIndex);
                        string ranges = Device.SendMessage(msg).ToString();

                        if (ranges.Contains("FIXED") || ranges.Contains("HWSEL"))
                            rangeComboBox.Enabled = false;

                        ranges = ranges.Substring(ranges.IndexOf('%') + 1);
                        string[] rangeList = ranges.Split(new char[] { ',' });

                        foreach (string range in rangeList)
                            rangeComboBox.Items.Add(range);

                        rangeComboBox.SelectedIndex = 0;

                        // Set range of track bar
                        int maxCount = (int)Device.SendMessage("@AO:MAXCOUNT").ToValue();
                        valueTrackBar.Minimum = 0;
                        valueTrackBar.Maximum = maxCount;
                        valueTrackBar.SmallChange = valueTrackBar.Maximum / 100;
                        valueTrackBar.LargeChange = valueTrackBar.Maximum / 10;
                        valueTrackBar.TickFrequency = valueTrackBar.LargeChange;
                    }
                    else
                    {
                        DisableControls();
                        statusLabel.Text = "The selected device does not support analog output";
                    }
                }
                else
                {
                    DisableControls();
                    statusLabel.Text = "No devices detected!";
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

        private void OnChannelChanged(object sender, EventArgs e)
        {
            // Create the channel spec (e.g. {0} for channel 0)
            Channel = channelComboBox.SelectedIndex;
        }

        private void OnRangeChanged(object sender, EventArgs e)
        {
            try
            {
                if (rangeComboBox.Enabled)
                {
                    // Send the AO Range message
                    string rangeValue = rangeComboBox.SelectedItem.ToString();
                    string message = String.Format("AO{{0}}:RANGE={1}", Channel, rangeValue);
                    Device.SendMessage(message);
                }
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
                string message = String.Format("AO{{0}}:VALUE/VOLTS={1}", Channel, value);
                Device.SendMessage(message);
                statusLabel.Text = String.Empty;
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void DisableControls()
        {
            channelComboBox.Enabled = false;
            rangeComboBox.Enabled = false;
            valueTrackBar.Enabled = false;
        }
    }
}
