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
        private double m_aiScanRate;
        private bool m_stopAiScan;
        private bool m_queueEnabled;
        private int m_timeout;
         
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

                // get a list of supported messages for the analog input trigger component
                commands.AddRange(m_daqDevice.GetSupportedMessages("AITRIG"));
                
                // get a list of supported messages for the analog input queue component
                commands.AddRange(m_daqDevice.GetSupportedMessages("AIQUEUE"));

                // all commands are in the list, now sort them
                commands.Sort();

                // add the messages to the message combobox
                foreach (string command in commands)
                    aiScanMessageComboBox.Items.Add(command);

                aiScanMessageComboBox.SelectedIndex = 0;

                // set defaults
                string message;
                string response;

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
                message = "@AISCAN:XFRMODES";
                response = m_daqDevice.SendMessage(message).ToString();
                 
                if (response.Contains("FIXED"))
                {
                    m_transferMode = m_daqDevice.SendMessage("?AISCAN:XFRMODE").ToString();
                }
                else
                {
                    message = "AISCAN:XFRMODE=BLOCKIO";
                    m_daqDevice.SendMessage(message);
                    SetAiScanCriticalParams(message);
                }

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

                    int channelCount = 0;

                    if (message.Contains("START"))
                    {
                        int lowChan = (int)m_daqDevice.SendMessage("?AISCAN:LOWCHAN").ToValue();
                        int highChan = (int)m_daqDevice.SendMessage("?AISCAN:HIGHCHAN").ToValue();

                        if (!m_queueEnabled)
                            channelCount = highChan - lowChan + 1;
                        else
                            channelCount = GetChannelCountFromQueue();

                        string extPacerSupported = m_daqDevice.SendMessage("@AISCAN:EXTPACER").ToString();
                        if (!extPacerSupported.Contains("NOT_SUPPORTED"))
                        {
                            string extPacer = m_daqDevice.SendMessage("?AISCAN:EXTPACER").ToString();

                            if (extPacer.Contains("ENABLE"))
                                m_timeout = 0;
                        }
                    }

                    // send the message to the device
                    DaqResponse response = m_daqDevice.SendMessage(message);

                    // display the response
                    aiScanResponseTextBox.Text = response.ToString();
                    Application.DoEvents();

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
            string response;

            try
            {
                // new queue method
                response = m_daqDevice.SendMessage("?AIQUEUE:COUNT").ToString();
            }
            catch (Exception)
            {
                response = m_daqDevice.SendMessage("?AISCAN:RANGE").ToString();
            }

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
                samples = (int)Math.Min(m_aiScanRate / 2, 64);
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
                samples = (int)Math.Min(m_aiScanRate / 2, 64);
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
                SetTimeout();
            }

            // save the transfer mode
            else if (message.Contains("XFRMODE"))
            {
                m_transferMode = message;
                SetTimeout();
            }

            else if (message.Contains("STOP"))
                m_stopAiScan = true;

            else if (message.Contains("RATE="))
            {
                m_aiScanRate = Convert.ToDouble(message.Substring(message.IndexOf('=') + 1));
                SetTimeout();
            }

            else if (message.Contains("QUEUE"))
            {
                if (message.Contains("ENABLE"))
                    m_queueEnabled = true;
                else
                    m_queueEnabled = false;
            }
        }

        private void SetTimeout()
        {
            if (m_transferMode.Contains("SINGLEIO"))
                m_timeout = (int)(10000.0 * (1.0 / (double)m_aiScanRate));
            else if (m_transferMode.Contains("BLOCKIO"))
                m_timeout = (int)(3000.0 * ((double)m_aiScanSamples / (double)m_aiScanRate));
            else
                m_timeout = (int)Math.Max(50, (3000.0 * ((double)m_aiScanSamples / (double)m_aiScanRate)));
        }
    }
}
