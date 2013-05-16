using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;
using System.Windows.Forms;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class MainForm
    {
        private int m_aoScanSamples = 0;
        private int m_lowAoChannel;
        private int m_highAoChannel;
        private bool m_stopAoScan;

        //=================================================================================
        /// <summary>
        /// Fills the aoScanMessageComboBox with messages supported by the AOSCAN Component
        /// </summary>
        //=================================================================================
        private void InitializeAoScanMessageComboBox(List<string> commands)
        {
            aoScanMessageComboBox.Items.Clear();

            // add the messages to the message combobox
            if (commands != null)
            {
                aoScanMessageComboBox.Enabled = true;
                aoScanSendMessageButton.Enabled = true;

                foreach (string command in commands)
                    aoScanMessageComboBox.Items.Add(command);

                aoScanMessageComboBox.SelectedIndex = 0;
            }
            else
            {
                aoScanMessageComboBox.Enabled = false;
                aoScanSendMessageButton.Enabled = false;
            }

            string message;

            message = "AOSCAN:LOWCHAN=0";
            m_daqDevice.SendMessage(message);
            SetAoScanCriticalParams(message);
            message = "AOSCAN:HIGHCHAN=0";
            m_daqDevice.SendMessage(message);
            SetAoScanCriticalParams(message);
            message = "AOSCAN:RATE=1000";
            m_daqDevice.SendMessage(message);
            SetAoScanCriticalParams(message);
            message = "AOSCAN:SAMPLES=1000";
            m_daqDevice.SendMessage(message);
            SetAoScanCriticalParams(message);

        }

        //==============================================================================
        /// <summary>
        /// Sends a message to the device, displays the device response 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //==============================================================================
        private void OnSendAoScanMessage(object sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = String.Empty;
                Application.DoEvents();

                // get the message from the text box
                string message = aoScanMessageComboBox.Text;

                try
                {
                    // save any critical parameters
                    SetAoScanCriticalParams(message);

                    // if the start message was sent then read the data
                    if (message.Contains("START"))
                    {
                        m_stopAoScan = false;
                        WriteScanData();
                    }
#if !WindowsCE
                    // log message
                    m_messageLog.LogMessage(message, m_messageLogClosed);
#endif
                    // send the message to the device
                    DaqResponse response = m_daqDevice.SendMessage(message);

                    // display the response
                    aoScanResponseTextBox.Text = response.ToString();
                    Application.DoEvents();

                    if (message.Contains("START"))
                        ProcessAOutScan();

                    if (message.Contains("STOP"))
                        m_stopAoScan = true;

                    statusLabel.Text = "Success";
                }
                catch (Exception ex)
                {
                    statusLabel.Text = ex.Message;
                }
            }
            catch (DaqException ex)
            {
                // SendMessage will throw an exception if an error occurs
                // so the exception needs to be handled. Here, the exception message will
                // be displayed by the status label
                aoResponseTextBox.Text = String.Empty;
                statusLabel.Text = ex.Message;
            }
        }

        //===================================================================================
        /// <summary>
        /// Writes scan data using the DaqDevice.WriteScanData method
        /// </summary>
        //===================================================================================
        private void WriteScanData()
        {
            // get the D/A max count
            int maxCount = (int)m_daqDevice.SendMessage("@AO:MAXCOUNT").ToValue();

            int samples = m_aoScanSamples;

            // set the number of samples to twice the fifo size for continuous mode
            if (m_daqDevice.SendMessage("?AOSCAN:SAMPLES").ToValue() == 0)
                samples = 2 * (int)m_daqDevice.SendMessage("@AOSCAN:FIFOSIZE").ToValue();

            int channelCount = m_highAoChannel - m_lowAoChannel + 1;

            // allcate the data array
            double[,] scanData = new double[channelCount, samples];

            // construct a sine wave
            double increment = (2.0 * Math.PI) / samples;
            double angle = 0.0;

            // determine bipolar or unipolar range
            string range = m_daqDevice.SendMessage("?AO{0}:RANGE").ToString();

            if (range.Contains("BIP"))
            {
                // bipolar range
                for (int i = 0; i < channelCount; i++)
                {
                    for (int j = 0; j < samples; j++)
                    {
                        scanData[i, j] = (int)(maxCount * Math.Sin(angle + (i * Math.PI / 2)));
                        angle += increment;
                    }
                }
            }
            else
            {
                // unipolar range
                for (int i = 0; i < channelCount; i++)
                {
                    for (int j = 0; j < samples; j++)
                    {
                        scanData[i, j] = (int)((maxCount / 2) + ((maxCount / 2) * Math.Sin(angle + (i * Math.PI / 2))));
                        angle += increment;
                    }
                }
            }

            // set the buffer size in bytes
            int bufSize = GetBufferSize(maxCount, channelCount, samples);

            m_daqDevice.SendMessage("AOSCAN:BUFSIZE=" + bufSize.ToString());

            // write the scan data
            m_daqDevice.WriteScanData(scanData, samples, 0);
        }

        //===============================================================================
        /// <summary>
        /// Gets the status and count for the scan
        /// </summary>
        //===============================================================================
        private void ProcessAOutScan()
        {
            string status = m_daqDevice.SendMessage("?AOSCAN:STATUS").ToString();
            string count;

            while (!m_stopAoScan && status.Contains("RUNNING"))
            {
                status = m_daqDevice.SendMessage("?AOSCAN:STATUS").ToString();
                count = m_daqDevice.SendMessage("?AOSCAN:COUNT").ToString();

                statusLabel.Text = String.Format("{0} : {1}", status, count);

                Application.DoEvents();
            }
        }

        //===============================================================================
        /// <summary>
        /// Calculates the buffer size based on the parameters passed ing
        /// </summary>
        /// <param name="maxCount">The D/A's max count</param>
        /// <param name="channelCount">The number of channels in the scan</param>
        /// <param name="samples">The number of samples</param>
        /// <returns></returns>
        //===============================================================================
        private int GetBufferSize(int maxCount, int channelCount, int samples)
        {
            int bytesPerSample = 0;
            int count = maxCount;

            do
            {
                count = count >> 8;
                bytesPerSample++;
            } while (count > 0);

            return bytesPerSample * channelCount * samples;
        }

        //======================================================================================
        /// <summary>
        /// Stores any critical params that determine how the data should be processed
        /// </summary>
        /// <param name="message">The message begin sent to the device</param>
        //======================================================================================
        private void SetAoScanCriticalParams(string message)
        {
            message = message.ToUpper();

            // save the sample count
            if (message.Contains("SAMPLES="))
                m_aoScanSamples = Convert.ToInt32(message.Substring(message.IndexOf('=') + 1));

            // save the low channel
            else if (message.Contains("LOWCHAN"))
                m_lowAoChannel = GetChannel(message);

            // save the low channel
            else if (message.Contains("HIGHCHAN"))
                m_highAoChannel = GetChannel(message);

            else if (message.Contains("STOP"))
                m_stopAoScan = true;
        }
    }
}