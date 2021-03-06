﻿using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class MainForm
    {
        //==============================================================================
        /// <summary>
        /// Fills the aoMessageComboBox with messages supported by the AI Component
        /// </summary>
        //==============================================================================
        private void InitializeAoMessageComboBox(List<string> commands)
        {
            aoMessageComboBox.Items.Clear();

            // add the messages to the message combobox
            if (commands != null)
            {
                aoMessageComboBox.Enabled = true;
                aoSendMessageButton.Enabled = true;

                // get a list of supported messages for the analog input cal component
                commands.AddRange(m_daqDevice.GetSupportedMessages("AOCAL"));

                // all commands are in the list, now sort them
                commands.Sort();

                foreach (string command in commands)
                    aoMessageComboBox.Items.Add(command);

                aoMessageComboBox.SelectedIndex = 0;

                // check if the device support self calibration
                string response = m_daqDevice.SendMessage("@AO:SELFCAL").ToString();

                if (response.Contains("NOT_SUPPORTED"))
                    aoCalibrateButton.Enabled = false;
                else
                    aoCalibrateButton.Enabled = true;
            }
            else
            {
                aoMessageComboBox.Enabled = false;
                aoSendMessageButton.Enabled = false;
            }
        }

        //==============================================================================
        /// <summary>
        /// Sends a message to the device and displays the response in a text box
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">The event args</param>
        //==============================================================================
        private void OnSendAoMessage(object sender, EventArgs e)
        {
            try
            {
                string message = aoMessageComboBox.Text;

#if !WindowsCE
                // log message
                m_messageLog.LogMessage(message, m_messageLogClosed);
#endif
                // send the message to the device
                DaqResponse response = m_daqDevice.SendMessage(message);

                aoResponseTextBox.Text = response.ToString();

                double numericRepsonse = response.ToValue();

                if (!Double.IsNaN(numericRepsonse))
                    aoNumericResponseTextBox.Text = numericRepsonse.ToString();
                else
                    aoNumericResponseTextBox.Text = String.Empty;

                statusLabel.Text = "Success";
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

        private void OnAoCalibrate(object sender, EventArgs e)
        {
            using (CalibrateAoForm caf = new CalibrateAoForm(m_daqDevice))
            {
#if WindowsCE
                caf.ShowDialog();
#else
                caf.ShowDialog(this);
#endif
            }
        }
    }
}
