using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;

namespace DOut
{
    public partial class DOutForm
    {
        private DaqDevice Device;
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

                        portRadioButton.Checked = true;
                    }
                    else
                    {
                        DisableControls();
                        statusLabel.Text = "The selected device does not support digital output!";
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
            string message = "DIO" + ChannelSpec + ":DIR=OUT";

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

        private void OnCheckChanged(object sender, EventArgs e)
        {
            if (portRadioButton.Checked)
                WritePort();
            else
                WriteBits();
        }

        private void WritePort()
        {
            int portValue = 0;

            if (checkBox1.Checked)
                portValue += 128;
            if (checkBox2.Checked)
                portValue += 64;
            if (checkBox3.Checked)
                portValue += 32;
            if (checkBox4.Checked)
                portValue += 16;
            if (checkBox5.Checked)
                portValue += 8;
            if (checkBox6.Checked)
                portValue += 4;
            if (checkBox7.Checked)
                portValue += 2;
            if (checkBox8.Checked)
                portValue += 1;

            valueLabel.Text = portValue.ToString();

            try
            {
                // Set the port value - example: "DIO{0}:VALUE=255"
                string message = "DIO" + ChannelSpec + ":VALUE=" + portValue.ToString();
                Device.SendMessage(message);
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void WriteBits()
        {
            int[] bitValues = new int[8];

            bitValues[0] = (checkBox8.Checked ? 1 : 0);
            bitValues[1] = (checkBox7.Checked ? 1 : 0);
            bitValues[2] = (checkBox6.Checked ? 1 : 0);
            bitValues[3] = (checkBox5.Checked ? 1 : 0);
            bitValues[4] = (checkBox4.Checked ? 1 : 0);
            bitValues[5] = (checkBox3.Checked ? 1 : 0);
            bitValues[6] = (checkBox2.Checked ? 1 : 0);
            bitValues[7] = (checkBox1.Checked ? 1 : 0);

            int portNumber = portComboBox.SelectedIndex;
            int value = 0;

            for (int i = 0; i < 8; i++)
            {
                value += (int)(Math.Pow(2, i) * bitValues[i]);
            }

            valueLabel.Text = value.ToString();

            try
            {
                // Set the bit values - example: "DIO{0/0}:VALUE=1"
                string message;
                for (int i = 0; i < 8; i++)
                {
                    message = "DIO{" + portNumber + "/" + i.ToString() + "}:VALUE=" + bitValues[i];
                    Device.SendMessage(message);
                }
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
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;
            checkBox3.Enabled = false;
            checkBox4.Enabled = false;
            checkBox5.Enabled = false;
            checkBox6.Enabled = false;
            checkBox7.Enabled = false;
            checkBox8.Enabled = false;
        }
    }
}
