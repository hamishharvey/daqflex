using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using MeasurementComputing.DAQFlex;

namespace CtrIn
{
    public partial class CtrInForm
    {
        private DaqDevice Device;
        private DaqResponse Response;

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
            DaqResponse response = Device.SendMessage("@CTR:CHANNELS");

            if (!response.ToString().Contains("NOT_SUPPORTED"))
            {
                EnableControls(true);

                int channels = (int)response.ToValue();

                counterComboBox.Items.Clear();

                for (int i = 0; i < channels; i++)
                    counterComboBox.Items.Add(i.ToString());

                counterComboBox.SelectedIndex = 0;

                // Initialize the timer
                timer1.Interval = 500;
                timer1.Enabled = false;

                statusLabel.Text = String.Empty;
            }
            else
            {
                EnableControls(false);
                statusLabel.Text = "The selected device does not have a counter";
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

        private void OnCounterChanged(object sender, EventArgs e)
        {
            string message;
            string counter;

            // Reset the counter 
            counter = counterComboBox.SelectedItem.ToString(); 
            
            message = "CTR{*}:VALUE=0";
            message = message.Replace("*", counter);
            Device.SendMessage(message);
        }

        private void OnStartButtonClicked(object sender, EventArgs e)
        {
            timer1.Enabled = true;

            try
            {
                string counter = counterComboBox.SelectedItem.ToString();

                string message;

                message = "CTR{*}:START";
                message = message.Replace("*", counter);
                Device.SendMessage(message);

                // get an immediate reading
                message = "?CTR{*}:VALUE";
                message = message.Replace("*", counter);
                Response = Device.SendMessage(message);
                responseTextBox.Text = Response.ToString();
                statusLabel.Text = String.Empty;
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
                string counter = counterComboBox.SelectedItem.ToString();

                string message;
                message = "CTR{*}:STOP";
                message = message.Replace("*", counter);
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
                string counter = counterComboBox.SelectedItem.ToString();

                // read the channel value
                string message;
                message = "?CTR{*}:VALUE";
                message = message.Replace("*", counter);
                Response = Device.SendMessage(message);
                responseTextBox.Text = Response.ToString();
                statusLabel.Text = String.Empty;
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void EnableControls(bool enableState)
        {
            counterComboBox.Enabled = enableState;
            startButton.Enabled = enableState;
            stopButton.Enabled = enableState;
        }
    }
}
