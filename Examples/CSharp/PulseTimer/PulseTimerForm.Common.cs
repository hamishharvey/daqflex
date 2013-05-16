using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using MeasurementComputing.DAQFlex;

namespace PulseTimer
{
    public partial class PulseTimerForm
    {
        private DaqDevice Device;

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
            DaqResponse response = Device.SendMessage("@TMR:CHANNELS");

            if (!response.ToString().Contains("NOT_SUPPORTED"))
            {
                int channels = (int)response.ToValue();

                timerComboBox.Items.Clear();

                for (int i = 0; i < channels; i++)
                {
                    if (Device.SendMessage(String.Format("@TMR{{0}}:TYPE", i)).ToString().Contains("PULSE"))
                        timerComboBox.Items.Add(i.ToString());
                }

                if (timerComboBox.Items.Count > 0)
                {
                    timerComboBox.SelectedIndex = 0;

                    EnableControls(true);
                    statusLabel.Text = String.Empty;
                }
                else
                {
                    EnableControls(false);
                    statusLabel.Text = "The selected device does not have a pulse timer";
                }
            }
            else
            {
                EnableControls(false);
                statusLabel.Text = "The selected device does not have a pulse timer";
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
                Cursor = Cursors.WaitCursor;
                Device = DaqDeviceManager.CreateDevice(name);

                InitializeControls();

                Cursor = Cursors.Default;
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
                string msg;
                string channel = timerComboBox.SelectedIndex.ToString();

                // set the period
                msg = "TMR{*}:PERIOD=#";
                msg = msg.Replace("*", channel);
                msg = msg.Replace("#", PeriodTextBox.Text);
                Device.SendMessage(msg);

                // set the delay
                msg = "TMR{*}:DELAY=#";
                msg = msg.Replace("*", channel);
                msg = msg.Replace("#", delayTextBox.Text);
                Device.SendMessage(msg);

                // set the duty cycle
                msg = "TMR{*}:DUTYCYCLE=#";
                msg = msg.Replace("*", channel);
                msg = msg.Replace("#", dutyCycleTextBox.Text);
                Device.SendMessage(msg);

                // set the duty cycle
                msg = "TMR{*}:PULSECOUNT=#";
                msg = msg.Replace("*", channel);
                msg = msg.Replace("#", pulseCountTextBox.Text);
                Device.SendMessage(msg);

                // start the timer
                msg = "TMR{*}:START";
                msg = msg.Replace("*", channel);
                Device.SendMessage(msg);
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void OnStopButtonClicked(object sender, EventArgs e)
        {
            try
            {
                string msg;
                string channel = timerComboBox.SelectedIndex.ToString();

                // stop the timer
                msg = "TMR{*}:STOP";
                msg = msg.Replace("*", channel);
                Device.SendMessage(msg);
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void EnableControls(bool enableState)
        {
            timerComboBox.Enabled = enableState;
            PeriodTextBox.Enabled = enableState;
            dutyCycleTextBox.Enabled = enableState;
            delayTextBox.Enabled = enableState;
            pulseCountTextBox.Enabled = enableState;
            startButton.Enabled = enableState;
            stopButton.Enabled = enableState;
        }
    }
}
