using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;
using System.Windows.Forms;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class MainForm
    {
        private int m_aiScanSamples = 0;
        private string m_transferMode;
        private int m_lowAiChannel;
        private int m_highAiChannel;
        private int m_aiScanRate;
        private bool m_stopAiScan;
        private int m_timeout = 0;
        private bool m_queueEnabled;
         
        //==============================================================================
        /// <summary>
        /// Fills the aiScanMessageComboBox with messages supported by the AISCAN Component
        /// </summary>
        //==============================================================================
        private void InitializeAiScanMessageComboBox(List<string> commands)
        {
            aiScanMessageComboBox.Items.Clear();

            if (commands != null)
            {
                aiScanMessageComboBox.Enabled = true;
                aiScanSendMessageButton.Enabled = true;

                // add the messages to the message combobox
                foreach (string command in commands)
                    aiScanMessageComboBox.Items.Add(command);

                // get a list of supported messages for the analog input component
                List<string> trigCommands = m_daqDevice.GetSupportedMessages("AITRIG");

                // add the messages to the message combobox
                foreach (string command in trigCommands)
                    aiScanMessageComboBox.Items.Add(command);

                aiScanMessageComboBox.SelectedIndex = 0;

                // set defaults


                string message;

                try
                {
                    message = "AISCAN:QUEUE=DISABLE";
                    m_daqDevice.SendMessage(message);
                    SetAiScanCriticalParams(message);
                }
                catch (Exception)
                {
                    // not all devices support a gain queue
                }

                // might want to query these from the device and resend them
                // so the values match the device's defaults and also matches
                // the critical params
                message = "AISCAN:XFRMODE=BLOCKIO";
                m_daqDevice.SendMessage(message);
                SetAiScanCriticalParams(message);
                message = "AISCAN:LOWCHAN=0";
                m_daqDevice.SendMessage(message);
                SetAiScanCriticalParams(message);
                message = "AISCAN:HIGHCHAN=0";
                m_daqDevice.SendMessage(message);
                SetAiScanCriticalParams(message);
                message = "AISCAN:RATE=1000";
                m_daqDevice.SendMessage(message);
                SetAiScanCriticalParams(message);
                message = "AISCAN:SAMPLES=1000";
                m_daqDevice.SendMessage(message);
                SetAiScanCriticalParams(message);
            }
            else
            {
                aiScanMessageComboBox.Enabled = false;
                aiScanSendMessageButton.Enabled = false;
            }
        }

        //==============================================================================
        /// <summary>
        /// Sends a message to the device, displays the device response and Ai data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //==============================================================================
        private void OnSendAiScanMessage(object sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = String.Empty;
                Application.DoEvents();

                // get the message from the text box
                string message = aiScanMessageComboBox.Text;

                // reset the stop flag
                m_stopAiScan = false;

                try
                {
                    // save any critical parameters
                    SetAiScanCriticalParams(message);

#if !WindowsCE
                    // log message
                    m_messageLog.LogMessage(message, m_messageLogClosed);
#endif
                    // send the message to the device
                    DaqResponse response = m_daqDevice.SendMessage(message);

                    // display the response
                    aiScanResponseTextBox.Text = response.ToString();
                    Application.DoEvents();

                    // calculate the channel count
                    int channelCount;

                    if (!m_queueEnabled)
                        channelCount = m_highAiChannel - m_lowAiChannel + 1;
                    else
                        channelCount = GetChannelCountFromQueue();

                    // if the start message was sent then read the data
                    if (message.Contains("START"))
                    {
                        // display the data

                        if (m_transferMode.Contains("SINGLEIO"))
                        {
                            // single I/O mode
                            ProcessSingleIO(channelCount);
                        }
                        else
                        {
                            // block or burst I/O mode 
                            ProcessBlockIO(channelCount);
                        }
                    }

                    statusLabel.Text = "Success";
                }
                catch (Exception ex)
                {
                    //statusLabel.Text = ex.Message;
                    statusLabel.Text = ex.Message;
                }
            }
            catch (DaqException ex)
            {
                // SendMessage will throw an exception if an error occurs
                // so the exception needs to be handled. Here, the exception message will
                // be displayed by the status label
                aiResponseTextBox.Text = String.Empty;
                statusLabel.Text = ex.Message;
            }
        }

        private int GetChannelCountFromQueue()
        {
            string response = m_daqDevice.SendMessage("?AISCAN:RANGE").ToString();
            return Convert.ToInt32(response.Substring(response.IndexOf("=") + 1));
        }

        //=============================================================================
        /// <summary>
        /// Reads and displays some of the data for multiple channels in SINGLEIO mode
        /// </summary>
        /// <param name="channelCount">The channel count</param>
        //=============================================================================
        private void ProcessSingleIO(int channelCount)
        {
            double[,] aiScanData;
            string s;
            int samples;
            DaqResponse response = null;

            if (m_aiScanSamples == 0)
            {
                // continuous mode
                m_stopAiScan = false;
                samples = Math.Min(m_aiScanRate / 2, 64);
            }
            else
            {
                // finite mode
                m_stopAiScan = true;
                samples = m_aiScanSamples;
            }

            do
            {
                s = String.Empty;

                for (int i = 0; i < Math.Min(100, samples); i++)
                {
                    aiScanData = m_daqDevice.ReadScanData(1, m_timeout);

                    for (int j = 0; j < channelCount; j++)
                    {
                        s += String.Format("{0}\t", aiScanData[j, 0]);
                    }

                    s += Environment.NewLine;

                    scanDataTextBox.Text = s;

                    response = m_daqDevice.SendMessage("?AISCAN:COUNT");
                    statusLabel.Text = response.ToString();

                    Application.DoEvents();
                }

            } while (!m_stopAiScan);
        }

        //=============================================================================
        /// <summary>
        /// Reads and displays some of the data for mulitple channels in BLOCKIO mode
        /// </summary>
        /// <param name="channelCount"></param>
        //=============================================================================
        private void ProcessBlockIO(int channelCount)
        {
            double[,] aiScanData; 
            DaqResponse response = null;
            int samples;
            string s;

            if (m_aiScanSamples == 0)
            {
                // continuous mode
                m_stopAiScan = false;
                samples = Math.Min(m_aiScanRate / 2, 64);
            }
            else
            {
                // finite mode
                m_stopAiScan = true;
                samples = Math.Min(100, m_aiScanSamples);
            }

            do
            {
                aiScanData = m_daqDevice.ReadScanData(samples, m_timeout);

                s = String.Empty;

                response = m_daqDevice.SendMessage("?AISCAN:COUNT");
                statusLabel.Text = response.ToString();

                for (int i = 0; i < samples; i++)
                {
                    for (int j = 0; j < channelCount; j++)
                    {
                        s += String.Format("{0}\t", aiScanData[j, i]);
                    }

                    s += Environment.NewLine;
                }

                scanDataTextBox.Text = s;

                Application.DoEvents();

            } while (!m_stopAiScan);
        }

        //======================================================================================
        /// <summary>
        /// Stores any critical params that determine how the data should be processed
        /// </summary>
        /// <param name="message">The message begin sent to the device</param>
        //======================================================================================
        private void SetAiScanCriticalParams(string message)
        {
            message = message.ToUpper();

            // save the sample count
            if (message.Contains("SAMPLES="))
            {
                m_aiScanSamples = Convert.ToInt32(message.Substring(message.IndexOf('=') + 1));
            }

            // save the transfer mode
            else if (message.Contains("XFRMODE"))
            {
                m_transferMode = message;
            }

            // save the low channel
            else if (message.Contains("LOWCHAN"))
                m_lowAiChannel = GetChannel(message);

            // save the low channel
            else if (message.Contains("HIGHCHAN"))
                m_highAiChannel = GetChannel(message);

            else if (message.Contains("STOP"))
                m_stopAiScan = true;

            else if (message.Contains("RATE="))
            {
                m_aiScanRate = Convert.ToInt32(message.Substring(message.IndexOf('=') + 1));
            }

            else if (message.Contains("QUEUE"))
            {
                if (message.Contains("ENABLE"))
                    m_queueEnabled = true;
                else
                    m_queueEnabled = false;
            }
        }
    }
}
