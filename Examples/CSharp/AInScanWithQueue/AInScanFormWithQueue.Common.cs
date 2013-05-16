using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using MeasurementComputing.DAQFlex;

namespace AInScan
{
    public partial class AInScanFormWithQueue
    {
        private DaqDevice Device;
        private bool Stop;
        private string DataDisplay;
        private string[] AvailableChannels;
        private string[] SupportedChannelModes;
        private char[] ValueSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator.ToCharArray();
        private string QueueConfiguration;
        private bool DataIsDisplayed = false;
        private List<string>CurrentQueueElements = new List<string>();
        private bool FormLoading = false;
        private bool UpdatingValidChannels = false;
        private bool UpdatingChannelModes = false;
        private bool UpdatingChannelRanges = false;
        private bool UpdatingTcTypes = false;

        protected override void OnLoad(EventArgs e)
        {
            FormLoading = true;

            tempUnitsComboBox.Enabled = false;

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

            FormLoading = false;
        }

        private void InitializeControls()
        {
            try
            {
                EnableControls(true);

                // check for queue support
                try
                {
                    Device.SendMessage("?AIQUEUE:COUNT");
                }
                catch (Exception)
                {
                    EnableControls(false);
                    statusLabel.Text = "The selected device does not support AIQUEUE messages!";
                    return;
                }

                // get the device properties that can be set in the queue
                QueueConfiguration = Device.SendMessage("@AISCAN:QUEUECONFIG").ToString();

                // if data rate can be programmed in the queue enable its combobox
                if (QueueConfiguration.Contains("DATARATE"))
                    dataRateComboBox.Enabled = true;
                else
                    dataRateComboBox.Enabled = false;

                // if ch mode can be programmed in the queue enable its combobox
                if (QueueConfiguration.Contains("CHMODE"))
                    channelModeComboBox.Enabled = true;
                else
                    channelModeComboBox.Enabled = false;

                InitializeChannelQueueControls();
            }
            catch (Exception)
            {
                EnableControls(false);
                statusLabel.Text = "The selected device does not support AIQUEUE messages!";
            }
        }

        private void InitializeChannelQueueControls()
        {
            string msg;
            string response;


            Device.SendMessage("AISCAN:QUEUE=ENABLE");

            // initialize the channel
            response = Device.SendMessage("?AI:VALIDCHANS").ToString();
            response = response.Substring(response.IndexOf("=") + 1);

            AvailableChannels = response.Split(ValueSeparator);
            foreach (string chan in AvailableChannels)
                channelComboBox.Items.Add(chan);

            channelComboBox.SelectedIndex = 0;


            // initialize the channel mode
            string channel = channelComboBox.SelectedItem.ToString();

            // get the supported channel modes
            msg = "@AI{*}:CHMODES";
            msg = msg.Replace("*", channel);

            response = Device.SendMessage(msg).ToString();

            // if the channel modes aren't individually programmable just get the current setting
            if (response.Contains("NOT_SUPPORTED"))
            {
                response = Device.SendMessage("?AI:CHMODE").ToString();
                int equalIndex = response.IndexOf("=");
                if (equalIndex >= 0)
                    response = response.Substring(equalIndex + 1);
            }
            else
            {
                response = response.Substring(response.IndexOf("%") + 1);
                int removeIndex = response.IndexOf("<");
                if (removeIndex >= 0)
                    response = response.Remove(removeIndex, response.Length - removeIndex);
            }

            SupportedChannelModes = response.Split(ValueSeparator);

            channelModeComboBox.Items.Clear();
            foreach (string mode in SupportedChannelModes)
                channelModeComboBox.Items.Add(mode);

            channelModeComboBox.SelectedIndex = 0;


            // Get supported ranges
            string ranges = Device.SendMessage("@AI{0}:RANGES").ToString();

            ranges = ranges.Substring(ranges.IndexOf('%') + 1);
            string[] rangeList = ranges.Split(ValueSeparator);

            rangeComboBox.Items.Clear();
            foreach (string range in rangeList)
                rangeComboBox.Items.Add(range);

            rangeComboBox.SelectedIndex = 0;


            // Get supported thermocouple types
            string tcTypes = Device.SendMessage("@AI{0}:TCTYPES").ToString();

            if (! tcTypes.Contains("NOT_SUPPORTED"))
            {
                tcTypes = tcTypes.Substring(tcTypes.IndexOf('%') + 1);
                string[] sensorList = tcTypes.Split(ValueSeparator);

                tcTypesComboBox.Items.Clear();
                foreach (string sensor in sensorList)
                    tcTypesComboBox.Items.Add(sensor);

                tcTypesComboBox.SelectedIndex = 0;
            }
            else
            {
                tcTypesComboBox.Enabled = false;
            }


            // Get supported data rates
            string dataRates = Device.SendMessage("@AI:DATARATES").ToString();

            if (!dataRates.Contains("NOT_SUPPORTED"))
            {
                dataRates = dataRates.Substring(dataRates.IndexOf('%') + 1);
                string[] dataRateList = dataRates.Split(ValueSeparator);

                dataRateComboBox.Items.Clear();
                foreach (string datarate in dataRateList)
                    dataRateComboBox.Items.Add(datarate);

                dataRateComboBox.SelectedIndex = 0;
            }
            else
            {
                dataRateComboBox.Enabled = false;
            }
        }

        private void CheckForTempInputs(string channel, string channelMode)
        {
            string msg;
            string response;

            // get the supported input types
            msg = "@AI{*}:INPUTS/";
            msg = msg.Replace("*", channel);
            msg += channelMode;

            response = Device.SendMessage(msg).ToString();

            // if the unit supports temperature inputs, fill the combobox with temp units
            if (response.Contains("TEMP"))
            {
                tempUnitsComboBox.Enabled = true;

                string tempUnits = "DEGF, DEGC, KELVIN";

                string[] tempUnitsList = tempUnits.Split(ValueSeparator);

                tempUnitsComboBox.Items.Clear();
                foreach (string tempunit in tempUnitsList)
                    tempUnitsComboBox.Items.Add(tempunit);

                tempUnitsComboBox.SelectedIndex = 0;
            }
        }

        private void OnDeviceChanged(object sender, EventArgs e)
        {
            statusLabel.Text = string.Empty;
            ScanDataTextBox.Text = string.Empty;
            CurrentQueueElements.Clear();

            tempUnitsComboBox.Items.Clear();
            tempUnitsComboBox.Enabled = false;

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

        private void OnTempUnitsChanged(object sender, EventArgs e)
        {
            
        }

        private void OnClearQueue(object sender, EventArgs e)
        {
            Device.SendMessage("AIQUEUE:CLEAR");

            ScanDataTextBox.Text = string.Empty;
            CurrentQueueElements.Clear();

            tempUnitsComboBox.Items.Clear();
            tempUnitsComboBox.Enabled = false;

            UpdateValidChannels();
        }

        private void OnAddQueueElements(object sender, EventArgs e)
        {
            string channel;
            string channelMode;
            string range;
            string dataRate = "0";
            string tcType = "";
            string msg;
            int element;

            if (DataIsDisplayed == true)
            {
                ScanDataTextBox.Text = string.Empty;
                foreach (string s in CurrentQueueElements)
                    ScanDataTextBox.Text += s + Environment.NewLine;
            }
            DataIsDisplayed = false;

            channel = channelComboBox.Text;
            channelMode = channelModeComboBox.Text;
            range = rangeComboBox.Text;

            try
            {
                element = (int)Device.SendMessage("?AIQUEUE:COUNT").ToValue();

                msg = "AIQUEUE{*}:CHAN=#";
                msg = msg.Replace("*", element.ToString());
                msg = msg.Replace("#", channel);
                Device.SendMessage(msg);

                if (channelModeComboBox.Enabled)
                {
                    msg = "AIQUEUE{*}:CHMODE=#";
                    msg = msg.Replace("*", element.ToString());
                    msg = msg.Replace("#", channelMode);
                    Device.SendMessage(msg);
                }

                msg = "AIQUEUE{*}:RANGE=#";
                msg = msg.Replace("*", element.ToString());
                msg = msg.Replace("#", range);
                Device.SendMessage(msg);

                if (dataRateComboBox.Enabled)
                {
                    dataRate = dataRateComboBox.Text;
                    
                    msg = "AIQUEUE{*}:DATARATE=#";
                    msg = msg.Replace("*", element.ToString());
                    msg = msg.Replace("#", dataRate);
                    Device.SendMessage(msg);
                }

                if (tcTypesComboBox.Enabled)
                {
                    tcType = tcTypesComboBox.Text;
                   
                    msg = "AI{*}:SENSOR=/TC/#";
                    msg = msg.Replace("*", channel);
                    msg = msg.Replace("#", tcType);
                    Device.SendMessage(msg);
                }

                msg = string.Format("Element {0}:  Channel = {1}, Mode = {2}, Range = {3}", element, channel, channelMode, range);
                if (dataRateComboBox.Enabled)
                    msg += string.Format(", Datarate = {0}", dataRate);
                if (tcTypesComboBox.Enabled)
                    msg += string.Format(", TC Type = {0}", tcType);

                ScanDataTextBox.Text += msg + Environment.NewLine;
                CurrentQueueElements.Add(msg);

                CheckForTempInputs(channel, channelMode);

                UpdateValidChannels();

                statusLabel.Text = String.Empty;
            }
            catch (DaqException ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void OnStartButtonClicked(object sender, EventArgs e)
        {
            try
            {
                ScanDataTextBox.Text = string.Empty;

                int samples = Int32.Parse(samplesTextBox.Text);

                Device.SendMessage("AISCAN:RATE=" + rateTextBox.Text);

                if (tempUnitsComboBox.Enabled)
                    Device.SendMessage("AISCAN:TEMPUNITS=" + tempUnitsComboBox.Text);

                string response = Device.SendMessage("?AIQUEUE:COUNT").ToString();
                response = response.Substring(response.IndexOf("=") + 1);
                int channelCount = Int32.Parse(response);

                if (finiteRadioButton.Checked)
                {
                    Stop = true;
                    Device.SendMessage("AISCAN:SAMPLES=" + samplesTextBox.Text);
                }
                else
                {
                    Stop = false;
                    Device.SendMessage("AISCAN:SAMPLES=0");
                }

                // Start the scan
                Device.SendMessage("AISCAN:SCALE=ENABLE");
                Device.SendMessage("AISCAN:START");

                double[,] scanData;
                string scanCount;
                string scanIndex;

                DataIsDisplayed = true;

                do
                {
                    // Read and display data and status
                    scanData = Device.ReadScanData(samples, 0);

                    scanCount = Device.SendMessage("?AISCAN:COUNT").ToString();
                    scanIndex = Device.SendMessage("?AISCAN:INDEX").ToString();

                    DataDisplay = String.Empty;

                    for (int i = 0; i < Math.Min(100, samples); i++)
                    {
                        for (int j = 0; j < channelCount; j++)
                        {
                            DataDisplay += scanData[j, i].ToString("F03") + "  ";
                        }

                        DataDisplay += Environment.NewLine;
                    }

                    ScanDataTextBox.Text = DataDisplay;
                    statusLabel.Text = String.Format("{0}   {1}", scanCount, scanIndex);

                    System.Threading.Thread.Sleep(1);
                    Application.DoEvents();

                } while (!Stop);
            }
            catch (DaqException ex)
            {
                statusLabel.Text = ex.Message;
                //Device.SendMessage("AISCAN:STOP");
            }
        }

        private void OnStopButtonClicked(object sender, EventArgs e)
        {
            Device.SendMessage("AISCAN:STOP");
            Stop = true;
        }

        private void EnableControls(bool enableState)
        {
            rateTextBox.Enabled = enableState;
            rangeComboBox.Enabled = enableState;
            samplesTextBox.Enabled = enableState;
            startButton.Enabled = enableState;
            stopButton.Enabled = enableState;
            finiteRadioButton.Enabled = enableState;
            continuousRadioButton.Enabled = enableState;
            channelComboBox.Enabled = enableState;
            channelModeComboBox.Enabled = enableState;
            rangeComboBox.Enabled = enableState;
            dataRateComboBox.Enabled = enableState;
            clearQueueButton.Enabled = enableState;
            addToQueueButton.Enabled = enableState;
            tcTypesComboBox.Enabled = enableState;
        }

        private void OnChannelChanged(object sender, EventArgs e)
        {
            if (FormLoading == true)
                return;

            UpdateValidChannelModes(channelComboBox.Text);
            UpdateValidRanges(channelComboBox.Text);
        }

        private void OnChannelModeChanged(object sender, EventArgs e)
        {
            UpdateValidChannels();
            UpdateValidRanges(channelComboBox.Text);
            UpdateValidTcTypes(channelComboBox.Text);
        }

        private void UpdateValidChannelModes(string channel)
        {
            if (UpdatingChannelModes)
                return;
            else
                UpdatingChannelModes = true;

            if (QueueConfiguration.Contains("CHMODE"))
            {
                string modes = Device.SendMessage("@AI:CHMODES").ToString();

                if (modes.Contains("MIXED"))
                {
                    string msg = "@AI{*}:CHMODES";
                    msg = msg.Replace("*", channel);
                    modes = Device.SendMessage(msg).ToString();
                }

                modes = modes.Substring(modes.IndexOf("%") + 1);
                int removeIndex = modes.IndexOf("<");
                modes = modes.Remove(removeIndex, modes.Length - removeIndex);

                string[] modeList = modes.Split(ValueSeparator);

                string currentMode = channelModeComboBox.Text;

                channelModeComboBox.Items.Clear();
                foreach (string mode in modeList)
                    channelModeComboBox.Items.Add(mode);

                // if the currentMode is still an item in the ComboBox, then
                // select it; otherwise, select the firrst item
                if (channelModeComboBox.Items.Contains(currentMode))
                    channelModeComboBox.SelectedItem = currentMode;
                else
                    channelModeComboBox.SelectedIndex = 0;

                UpdatingChannelModes = false;
            }
        }

        private void UpdateValidTcTypes(string channel)
        {
            string msg = string.Empty;

            if (UpdatingTcTypes)
                return;
            else
                UpdatingTcTypes = true;

            msg = "@AI{*}:TCTYPES/";
            msg += channelModeComboBox.SelectedItem;
            msg = msg.Replace("*", channel);
            string tcTypes = Device.SendMessage(msg).ToString();

            string currentTcType = tcTypesComboBox.Text;

            tcTypesComboBox.Items.Clear();
            if (!tcTypes.Contains("NOT_SUPPORTED"))
            {
                tcTypes = tcTypes.Substring(tcTypes.IndexOf("%") + 1);
                string[] tcTypeList = tcTypes.Split(ValueSeparator);

                foreach (string tcType in tcTypeList)
                    tcTypesComboBox.Items.Add(tcType);

                // if the currentMode is still an item in the ComboBox, then
                // select it; otherwise, select the firrst item
                if (tcTypesComboBox.Items.Contains(currentTcType))
                    tcTypesComboBox.SelectedItem = currentTcType;
                else
                    tcTypesComboBox.SelectedIndex = 0;

                tcTypesComboBox.Enabled = true;
            }
            else
                tcTypesComboBox.Enabled = false;

            UpdatingTcTypes = false;
        }

        private void UpdateValidRanges(string channel)
        {
            if (UpdatingChannelRanges)
                return;
            else
                UpdatingChannelRanges = true;

            string msg = "@AI{*}:RANGES/#";
            msg = msg.Replace("*", channel);
            msg = msg.Replace("#", channelModeComboBox.Text);
            string ranges = Device.SendMessage(msg).ToString();

            ranges = ranges.Substring(ranges.IndexOf("%") + 1);

            string[] rangeList = ranges.Split(ValueSeparator);

            string currentRange = rangeComboBox.Text;

            rangeComboBox.Items.Clear();
            foreach (string range in rangeList)
                rangeComboBox.Items.Add(range);

            if (rangeList.Length == 1)
                rangeComboBox.Enabled = false;
            else
                rangeComboBox.Enabled = true;

            // if the currentRange is still an item in the ComboBox, then
            // select it; otherwise, select the firrst item
            if (rangeComboBox.Items.Contains(currentRange))
                rangeComboBox.SelectedItem = currentRange;
            else
                rangeComboBox.SelectedIndex = 0;

            UpdatingChannelRanges = false;
        }

        private void UpdateValidChannels()
        {
            if (UpdatingValidChannels)
                return;
            else
                UpdatingValidChannels = true;

            string validChannels = Device.SendMessage("?AI:VALIDCHANS").ToString();
            validChannels = validChannels.Substring(validChannels.IndexOf("=") + 1);
            
            string[] channelList = validChannels.Split(ValueSeparator);

            string currentChannel = channelComboBox.Text;

            channelComboBox.Items.Clear();
            foreach (string chan in channelList)
                channelComboBox.Items.Add(chan);

            channelComboBox.SelectedItem = currentChannel;

            UpdatingValidChannels = false;
        }
    }
}
