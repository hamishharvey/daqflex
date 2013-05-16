using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MeasurementComputing.DAQFlex;

namespace DIn
{
    public partial class DInForm
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
                    DaqResponse response = Device.SendMessage("@DIO:CHANNELS");

                    if (!response.ToString().Contains("NOT_SUPPRORTED"))
                    {
                        int channels = (int)response.ToValue();

                        for (int i = 0; i < channels; i++)
                            portComboBox.Items.Add(i.ToString());

                        portComboBox.SelectedIndex = 0;

                        // Initialize the timer
                        timer1.Interval = 500;
                        timer1.Enabled = false;
                    }
                    else
                    {
                        DisableControls();
                        statusLabel.Text = "The selected device does not support digital input!";
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

        private void OnPortChanged(object sender, EventArgs e)
        {
            // Create the channel spec (e.g. {0} for port 0)
            int channel = portComboBox.SelectedIndex;
            ChannelSpec = "{" + channel.ToString() + "}";

            // Send the direction message
            string message = "DIO" + ChannelSpec + ":DIR=IN";

            try
            {
                Device.SendMessage(message);
                statusLabel.Text = String.Empty;
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
            if (portRadioButton.Checked)
                ReadPort();
            else
                ReadBits();
        }

        private void ReadPort()
        {
            try
            {
                // Read the DI port 
                string message = "?DIO" + ChannelSpec + ":VALUE";
                Response = Device.SendMessage(message);
                int value = (int)Response.ToValue();
                valueLabel.Text = value.ToString();
                statusLabel.Text = String.Empty;

                if ((value & 1) != 0)
                    led0.BackColor = Color.LimeGreen;
                else
                    led0.BackColor = Color.Silver;

                if ((value & 2) != 0)
                    led1.BackColor = Color.LimeGreen;
                else
                    led1.BackColor = Color.Silver;

                if ((value & 4) != 0)
                    led2.BackColor = Color.LimeGreen;
                else
                    led2.BackColor = Color.Silver;

                if ((value & 8) != 0)
                    led3.BackColor = Color.LimeGreen;
                else
                    led3.BackColor = Color.Silver;

                if ((value & 16) != 0)
                    led4.BackColor = Color.LimeGreen;
                else
                    led4.BackColor = Color.Silver;

                if ((value & 32) != 0)
                    led5.BackColor = Color.LimeGreen;
                else
                    led5.BackColor = Color.Silver;

                if ((value & 64) != 0)
                    led6.BackColor = Color.LimeGreen;
                else
                    led6.BackColor = Color.Silver;

                if ((value & 128) != 0)
                    led7.BackColor = Color.LimeGreen;
                else
                    led7.BackColor = Color.Silver;

            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void ReadBits()
        {
            try
            {
                // Read the DI bits 
                string message;
                int[] bitValues = new int[8];
                int port;
                int value = 0;

                port = portComboBox.SelectedIndex;

                // query the bit value - example "?DIO{0/0}VALUE"
                for (int i = 0; i < 8; i++)
                {
                    message = "?DIO{" + port.ToString() + "/" + i.ToString() + "}:VALUE";
                    bitValues[i] = (int)Device.SendMessage(message).ToValue();
                    statusLabel.Text = String.Empty;
                    value += (int)(Math.Pow(2, i) * bitValues[i]);
                }

                if (bitValues[0] == 1)
                    led0.BackColor = Color.LimeGreen;
                else
                    led0.BackColor = Color.Silver;

                if (bitValues[1] == 1)
                    led1.BackColor = Color.LimeGreen;
                else
                    led1.BackColor = Color.Silver;

                if (bitValues[2] == 1)
                    led2.BackColor = Color.LimeGreen;
                else
                    led2.BackColor = Color.Silver;

                if (bitValues[3] == 1)
                    led3.BackColor = Color.LimeGreen;
                else
                    led3.BackColor = Color.Silver;

                if (bitValues[4] == 1)
                    led4.BackColor = Color.LimeGreen;
                else
                    led4.BackColor = Color.Silver;

                if (bitValues[5] == 1)
                    led5.BackColor = Color.LimeGreen;
                else
                    led5.BackColor = Color.Silver;

                if (bitValues[6] == 1)
                    led6.BackColor = Color.LimeGreen;
                else
                    led6.BackColor = Color.Silver;

                if (bitValues[7] == 1)
                    led7.BackColor = Color.LimeGreen;
                else
                    led7.BackColor = Color.Silver;

                valueLabel.Text = value.ToString();
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void DisableControls()
        {
            portComboBox.Enabled = false;
            portRadioButton.Enabled = false;
            bitRadioButton.Enabled = false;
            startButton.Enabled = false;
            stopButton.Enabled = false;
        }
    }
}
