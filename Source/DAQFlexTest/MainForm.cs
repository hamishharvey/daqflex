using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MeasurementComputing.DAQFlex;
using System.Reflection;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class MainForm : Form
    {
        private string m_deviceName = String.Empty;
        private DaqDevice m_daqDevice = null;
        private DaqDevice m_previouslySelectedDevice = null;
        private MessageLog m_messageLog;
        private bool m_messageLogClosed;
        private Dictionary<string, TabPage> m_tabPages = new Dictionary<string,TabPage>();

        //==============================================================================
        /// <summary>
        /// ctor - initialize components and create a message log window
        /// </summary>
        //==============================================================================
        public MainForm()
        {
            InitializeComponent();

            AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            Version v = assemblyName.Version;

            Text = String.Format("{0} - {1}.{2}", assemblyName.Name, v.Major, v.Minor);

            m_messageLog = new MessageLog();
        }

        //==============================================================================
        /// <summary>
        /// Get a list of detected device names from the daq device manager
        /// </summary>
        /// <param name="e">EventArgs</param>
        //==============================================================================
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            statusLabel.Text = String.Empty;

            m_tabPages.Clear();

            foreach (TabPage tp in testTabControl.TabPages)
            {
                m_tabPages.Add(tp.Text, tp);
            }

            testTabControl.TabPages.Clear();

            // Get a list of device names from the daq device manager
            try
            {
                string[] deviceNames = DaqDeviceManager.GetDeviceNames(DeviceNameFormat.NameAndSerno);

                // fill the device list combo box
                if (deviceNames.Length > 0)
                {
                    foreach (string name in deviceNames)
                        deviceListComboBox.Items.Add(name);
                }
                else
                {
                    deviceListComboBox.Enabled = false;
                    testTabControl.Enabled = false;
                    statusLabel.Text = "No devices detected!";
                    return;
                }

                SetInitialControlValues();

                m_messageLog.Location = new Point(this.Location.X + this.Width, this.Location.Y);
                m_messageLog.BringToFront();

                m_messageLog.FormClosing += new FormClosingEventHandler(OnMessageLogFormClosing);
            }
            catch (DaqException ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        //==============================================================================
        /// <summary>
        /// Gets a daq device object based on the selected daq item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //==============================================================================
        private void OnDeviceSelected(object sender, EventArgs e)
        {
            m_deviceName = deviceListComboBox.Items[deviceListComboBox.SelectedIndex].ToString();

            // create a daq device object using the daq device manager and the device name
            try
            {
                if (m_previouslySelectedDevice != null)
                    DaqDeviceManager.ReleaseDevice(m_previouslySelectedDevice);

                this.Cursor = Cursors.WaitCursor;

                m_daqDevice = DaqDeviceManager.CreateDevice(m_deviceName);

                m_previouslySelectedDevice = m_daqDevice;

                // initialize the message combo boxes
                AddPages();
            }
            catch (DaqException ex)
            {
                statusLabel.Text = ex.Message;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        //=======================================================================
        /// <summary>
        /// Add a page for each supported DAQ Component
        /// </summary>
        //=======================================================================
        protected void AddPages()
        {
            List<string> commands;

            testTabControl.TabPages.Clear();
            commands = m_daqDevice.GetSupportedMessages("DEV");
            testTabControl.TabPages.Add(m_tabPages["DEV"]);
            InitializeDevMessageComboBox(commands);

            commands = m_daqDevice.GetSupportedMessages("AI");
            if (commands != null && commands.Count > 0)
            {
                testTabControl.TabPages.Add(m_tabPages["AI"]);
                InitializeAiMessageComboBox(commands);
            }

            commands = m_daqDevice.GetSupportedMessages("AISCAN");
            if (commands != null && commands.Count > 0)
            {
                testTabControl.TabPages.Add(m_tabPages["AISCAN"]);
                InitializeAiScanMessageComboBox(commands);
            }

            commands = m_daqDevice.GetSupportedMessages("AO");
            if (commands != null && commands.Count > 0)
            {
                testTabControl.TabPages.Add(m_tabPages["AO"]);
                InitializeAoMessageComboBox(commands);
            }

            commands = m_daqDevice.GetSupportedMessages("AOSCAN");
            if (commands != null && commands.Count > 0)
            {
                testTabControl.TabPages.Add(m_tabPages["AOSCAN"]);
                InitializeAoScanMessageComboBox(commands);
            }

            commands = m_daqDevice.GetSupportedMessages("DIO");
            if (commands != null && commands.Count > 0)
            {
                testTabControl.TabPages.Add(m_tabPages["DIO"]);
                InitializeDioMessageComboBox(commands);
            }

            commands = m_daqDevice.GetSupportedMessages("CTR");
            if (commands != null && commands.Count > 0)
            {
                testTabControl.TabPages.Add(m_tabPages["CTR"]);
                InitializeCtrMessageComboBox(commands);
            }

            commands = m_daqDevice.GetSupportedMessages("TMR");
            if (commands != null && commands.Count > 0)
            {
                testTabControl.TabPages.Add(m_tabPages["TMR"]);
                InitializeTmrMessageComboBox(commands);
            }
        }

        //==============================================================================
        /// <summary>
        /// Highlight the asterisks for quicker editting
        /// </summary>
        /// <param name="sender">One of the message combobox controls</param>
        /// <param name="e">Mouse event args</param>
        //==============================================================================
        private void OnComboBoxClick(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;

            int asteriksIndex = cb.Text.IndexOf("*");

            if (asteriksIndex >= 0)
            {
                cb.SelectionStart = asteriksIndex;
                cb.SelectionLength = 1;
            }
        }

        //==============================================================================
        /// <summary>
        /// Log device messages to the message log window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //==============================================================================
        private void OnLogMessage(object sender, EventArgs e)
        {
            if (messageLogCheckBox.Checked)
            {
                m_messageLogClosed = false;
                m_messageLog = new MessageLog();
                m_messageLog.Show();
            }
            else
            {
                m_messageLogClosed = true;
                m_messageLog.Close();
                m_messageLog.Dispose();
            }
        }

        private void OnMessageLogFormClosing(object sender, CancelEventArgs e)
        {
            m_messageLogClosed = true;
        }

        //==============================================================================
        /// <summary>
        /// Sets the inital values for the controls on the form
        /// </summary>
        //==============================================================================
        private void SetInitialControlValues()
        {
            // set initial values of controls
            deviceListComboBox.SelectedIndex = 0;

            messageLogCheckBox.Checked = true;
        }

        protected int GetChannel(string message)
        {
            int channel = 0;

            if (message.Contains("HIGHCHAN") || message.Contains("LOWCHAN"))
            {
                int chIndex = message.IndexOf("=") + 1;

                try
                {
                    channel = Convert.ToInt32(message.Substring(chIndex));
                }
                catch (Exception)
                {
                    channel = 0;
                }
            }
            else if (message.Contains("{") && message.Contains("}"))
            {
                int lIndex = message.IndexOf("{");
                int rIndex = message.IndexOf("}");

                try
                {
                    channel = Convert.ToInt32(message.Substring(lIndex + 1, rIndex - (lIndex + 1)));
                }
                catch (Exception)
                {
                    channel = 0;
                }
            }

            return channel;
        }
    }
}
