using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
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

        protected override void OnLoad(EventArgs e)
        {
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

                // Get number of supported channels
                int channels = (int)DaqDevice.SendMessage("@AI:CHANNELS").ToValue();

                channelComboBox.Items.Clear();

                for (int i = 0; i < channels; i++)
                    channelComboBox.Items.Add(i.ToString());

                channelComboBox.SelectedIndex = 0;

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

                logDataCheckBox.Checked = false;
                logFileTextBox.Enabled = false;
                browseButton.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            base.OnLoad(e);
        }

        private void OnDeviceChanged(object sender, EventArgs e)
        {
            try
            {
                // Release the device
                if (DaqDevice != null)
                    DaqDeviceManager.ReleaseDevice(DaqDevice);

                DeviceName = deviceComboBox.SelectedItem.ToString();
                
                // Create a new device object
                DaqDevice = DaqDeviceManager.CreateDevice(DeviceName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
    }
}
