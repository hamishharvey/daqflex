using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;

namespace CtrIn
{
    public partial class CtrInForm
    {
        private DaqDevice Device;
        private DaqResponse Response;
        //private string ChannelSpec = "{0}";

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
                    DaqResponse response = Device.SendMessage("@CTR:CHANNELS");

                    if (!response.ToString().Contains("NOT_SUPPORTED"))
                    {
                        int channels = (int)response.ToValue();

                        for (int i = 0; i < channels; i++)
                            counterComboBox.Items.Add(i.ToString());

                        counterComboBox.SelectedIndex = 0;

                        // Initialize the timer
                        timer1.Interval = 500;
                        timer1.Enabled = false;
                    }
                    else
                    {
                        DisableControls();
                        statusLabel.Text = "The selected device does not have a counter";
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

        private void OnStartButtonClicked(object sender, EventArgs e)
        {
            timer1.Enabled = true;

            try
            {
                string message;
                message = String.Format("CTR{{0}}:VALUE=0", counterComboBox.SelectedIndex);
                Device.SendMessage(message);
                message = String.Format("CTR{{0}}:START", counterComboBox.SelectedIndex);
                Device.SendMessage(message);
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void OnStopButtonClicked(object sender, EventArgs e)

        {
            timer1.Enabled = false;

            try
            {
                string message = String.Format("CTR{{0}}:STOP", counterComboBox.SelectedIndex);
                Device.SendMessage(message);
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            try
            {
                // Read the AI channel 
                string message = String.Format("?CTR{{0}}:VALUE", counterComboBox.SelectedIndex);
                Response = Device.SendMessage(message);
                responseTextBox.Text = Response.ToString();
                statusLabel.Text = String.Empty;
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void DisableControls()
        {
            counterComboBox.Enabled = false;
            startButton.Enabled = false;
            stopButton.Enabled = false;
        }
    }
}
