using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using MeasurementComputing.DAQFlex;

namespace AIn
{
    public partial class AInForm
    {
        private DaqDevice Device;
        private DaqResponse Response;
        private string ChannelSpec = "{0}";

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
                    DaqResponse response = Device.SendMessage("@AI:CHANNELS");

                    if (!response.ToString().Contains("NOT_SUPPRORTED"))
                    {
                        int channels = (int)response.ToValue();

                        for (int i = 0; i < channels; i++)
                            channelComboBox.Items.Add(i.ToString());

                        channelComboBox.SelectedIndex = 0;

                        // Get supported ranges for the selected channel
                        string msg = String.Format("@AI{{0}}:RANGES", channelComboBox.SelectedIndex);
                        string ranges = Device.SendMessage(msg).ToString();
                        ranges = ranges.Substring(ranges.IndexOf('%') + 1);
                        string[] rangeList = ranges.Split(new char[] { ',' });

                        foreach (string range in rangeList)
                            rangeComboBox.Items.Add(range);

                        rangeComboBox.SelectedIndex = 0;

                        // Initialize the timer
                        timer1.Interval = 500;
                        timer1.Enabled = false;
                    }
                    else
                    {
                        DisabelControls();
                        statusLabel.Text = "The selected device does not support analog input!";
                    }
                }
                else
                {
                    DisabelControls();
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
            int channel = channelComboBox.SelectedIndex;
            ChannelSpec = "{" + channel.ToString() + "}";
        }

        private void OnRangeChanged(object sender, EventArgs e)
        {
            try
            {
                // Send the AI Range message
                string rangeValue = rangeComboBox.SelectedItem.ToString();
                string message = "AI" + ChannelSpec + ":RANGE=" + rangeValue;
                Device.SendMessage(message);
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void OnStartButtonClicked(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void OnStopButtonClicked(object sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            try
            {
                // Read the AI channel 
                string message = "?AI" + ChannelSpec + ":VALUE/VOLTS";
                Response = Device.SendMessage(message);
                responseTextBox.Text = Response.ToString();
                statusLabel.Text = String.Empty;
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void DisabelControls()
        {
            channelComboBox.Enabled = false;
            rangeComboBox.Enabled = false;
            startButton.Enabled = false;
            stopButton.Enabled = false;
        }
    }
}
