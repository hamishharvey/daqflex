using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using MeasurementComputing.DAQFlex;

namespace TempView
{
    public partial class SetupDlg
    {
        public DaqDevice DaqDevice;
        public string DeviceName;
        public string TcType;
        public string Units;
        public int SamplePeriod = 1;
        public bool LogData;
        public string LogFile;
        public string Description;
        public bool AppendFile = false;
        public string Channel;
        public string ChannelMode;
        public bool SupportsTemperature = false;

        protected override void OnLoad(EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            // Release the device
            if (DaqDevice != null)
                DaqDeviceManager.ReleaseDevice(DaqDevice);

            // Get a list of devices
            string[] deviceNames = DaqDeviceManager.GetDeviceNames(DeviceNameFormat.NameAndSerno);

            try
            {
                if (deviceNames.Length == 0)
                {
                    MessageBox.Show("No devices detected");
                    Close();
                    return;
                }

                deviceComboBox.Items.Clear();

                foreach (string name in deviceNames)
                    deviceComboBox.Items.Add(name);

                deviceComboBox.SelectedIndex = 0;

            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
                EnableControls(false);
            }

            base.OnLoad(e);

            Cursor.Current = Cursors.Default;
        }

        private void InitializeControls()
        {
            // Get number of supported channels
            int channels = (int)DaqDevice.SendMessage("@AI:CHANNELS").ToValue();

            // fill the channel combobox
            channelComboBox.Items.Clear();

            for (int i = 0; i < channels; i++)
                channelComboBox.Items.Add(i.ToString());

            channelComboBox.SelectedIndex = 0;

            // get the selected channel
            int channel = Int32.Parse(channelComboBox.SelectedItem.ToString());

            string msg;

            // get supported channel modes
            string[] channelModes = GetChannelModes(channel.ToString());

            if (channelModes.Length > 0)
            {
                // fill the 
                SupportsTemperature = true;

                // tc type
                tcTypeComboBox.Items.Clear();
                tcTypeComboBox.Items.Add("B");
                tcTypeComboBox.Items.Add("E");
                tcTypeComboBox.Items.Add("J");
                tcTypeComboBox.Items.Add("K");
                tcTypeComboBox.Items.Add("N");
                tcTypeComboBox.Items.Add("R");
                tcTypeComboBox.Items.Add("S");
                tcTypeComboBox.Items.Add("T");
                tcTypeComboBox.SelectedIndex = 0;

                // tc units
                tcUnitsComboBox.Items.Clear();
                tcUnitsComboBox.Items.Add(TempViewForm.DEG_C);
                tcUnitsComboBox.Items.Add(TempViewForm.DEG_F);
                tcUnitsComboBox.SelectedIndex = 0;

                EnableControls(true);

                logDataCheckBox.Checked = false;
                logFileTextBox.Enabled = false;
                browseButton.Enabled = false;
            }
            else
            {
                statusLabel.Text = "The selected device does not support temperature";
                EnableControls(false);
            }
        }

        private void OnDeviceChanged(object sender, EventArgs e)
        {
            try
            {
                // Release the device
                if (DaqDevice != null)
                    DaqDeviceManager.ReleaseDevice(DaqDevice);

                Cursor.Current = Cursors.WaitCursor;

                DeviceName = deviceComboBox.SelectedItem.ToString();
                
                // Create a new device object
                DaqDevice = DaqDeviceManager.CreateDevice(DeviceName);

                Cursor.Current = Cursors.Default;

                InitializeControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnChannelChanged(object sender, EventArgs e)
        {
            Channel = channelComboBox.SelectedItem.ToString();

            string[] channelModes = GetChannelModes(Channel);

            // fill channel mode combobox
            if (channelModes.Length > 0)
            {
                channelModeComboBox.Items.Clear();

                foreach (string chMode in channelModes)
                    channelModeComboBox.Items.Add(chMode);
                 
                channelModeComboBox.SelectedIndex = 0;
                ChannelMode = channelModeComboBox.SelectedItem.ToString();
            }
        }

        private void OnChannelModeChanged(object sender, EventArgs e)
        {
            ChannelMode = channelModeComboBox.SelectedItem.ToString();
        }

        private void OnTcTypeChanged(object sender, EventArgs e)
        {
            TcType = tcTypeComboBox.SelectedItem.ToString();
        }

        private void OnTcUnitsChanged(object sender, EventArgs e)
        {
            Units = tcUnitsComboBox.SelectedItem.ToString();
        }

        private void OnSamplePeriodChanged(object sender, EventArgs e)
        {
            SamplePeriod = (int)samplePeriodNumericUpDown.Value;
        }

        private void OnLogDataCheckChanged(object sender, EventArgs e)
        {
            LogData = logDataCheckBox.Checked;

            if (LogData)
            {
                logFileTextBox.Enabled = true;
                browseButton.Enabled = true;
            }
            else
            {
                logFileTextBox.Enabled = false;
                browseButton.Enabled = false;
            }
        }

        private void OnBrowseButtonClicked(object sender, EventArgs e)
        {
            saveFileDialog.Filter = "txt files(*.txt)|*.txt";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                LogFile = saveFileDialog.FileName;
                if (!LogFile.Contains(".txt"))
                    LogFile += ".txt";
                logFileTextBox.Text = LogFile;
            }
        }

        private void OnLogFileChanged(object sender, EventArgs e)
        {
            LogFile = logFileTextBox.Text;
        }

        private void OnDescriptionChanged(object sender, EventArgs e)
        {
            Description = descriptionTextBox.Text;
        }

        private string[] GetChannelModes(string channel)
        {
            string presentMode = string.Empty;
            string msg = string.Empty;
            string supportedModes = string.Empty;
            string inputs = string.Empty;
            bool isProgrammable = false;
            List<string> modes = new List<string>();
            string[] modesArray;

            try
            {
                // get the supported modes
                msg = "@AI{*}:CHMODES";
                msg = msg.Replace("*", channel);
                supportedModes = DaqDevice.SendMessage(msg).ToString();

                // if the device doesn't return the modes using the channel in the message
                // resend the message without the channel
                if (supportedModes.Contains("NOT_SUPPORTED"))
                {
                    msg = "@AI:CHMODES";
                    supportedModes = DaqDevice.SendMessage(msg).ToString();
                }

                // check if the modes are programmable
                if (supportedModes.Contains("PROG"))
                    isProgrammable = true;

                // extract just the channel modes - response is in the form AI:CHMODES=PROG%SE,DIFF<CHANNELS,RANGES> 
                supportedModes = supportedModes.Substring(supportedModes.IndexOf('%') + 1, supportedModes.Length - (supportedModes.IndexOf('%') + 1));
                int dependentsIndex = supportedModes.IndexOf("<");

                // remove the dependents <>
                if (dependentsIndex > 0)
                    supportedModes = supportedModes.Remove(dependentsIndex, supportedModes.Length - dependentsIndex);

                // split into an array
                modesArray = supportedModes.Split(CultureInfo.CurrentCulture.TextInfo.ListSeparator.ToCharArray());

                foreach (string mode in modesArray)
                {
                    msg = "@AI{*}:INPUTS/" + mode;
                    msg = msg.Replace("*", channel);
                    inputs = DaqDevice.SendMessage(msg).ToString();

                    // if the device doesn't return the inputs using the channel in the message
                    // resend the message without the channel
                    if (inputs.Contains("NOT_SUPPORTED"))
                    {
                        msg = "@AI:INPUTS/" + mode;
                        inputs = DaqDevice.SendMessage(msg).ToString();
                    }

                    if (inputs.Contains("TEMP"))
                        modes.Add(mode);
                }
            }
            catch (Exception)
            {
            }

            return modes.ToArray();
        }

        private void EnableControls(bool enableState)
        {
            channelComboBox.Enabled = enableState;
            channelModeComboBox.Enabled = enableState;
            tcTypeComboBox.Enabled = enableState;
            tcUnitsComboBox.Enabled = enableState;
            samplePeriodNumericUpDown.Enabled = enableState;
            logDataCheckBox.Enabled = enableState;
            logFileTextBox.Enabled = enableState;
            browseButton.Enabled = enableState;
            descriptionTextBox.Enabled = enableState;
        }
    }
}
