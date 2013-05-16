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
        /// Fills the ctrMessageComboBox with messages supported by the CTR Component
        /// </summary>
        //==============================================================================
        private void InitializeCtrMessageComboBox(List<string> commands)
        {
            ctrMessageComboBox.Items.Clear();

            // add the messages to the message combobox
            if (commands != null)
            {
                ctrMessageComboBox.Enabled = true;
                ctrSendMessageButton.Enabled = true;

                foreach (string command in commands)
                    ctrMessageComboBox.Items.Add(command);

                ctrMessageComboBox.SelectedIndex = 0;
            }
            else
            {
                ctrMessageComboBox.Enabled = false;
                ctrSendMessageButton.Enabled = false;
            }
        }

        //==============================================================================
        /// <summary>
        /// Sends a message to the device and displays the response in a text box
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">The event args</param>
        //==============================================================================
        private void OnSendCtrMessage(object sender, EventArgs e)
        {
            try
            {
                string message = ctrMessageComboBox.Text;

#if !WindowsCE
                // log message
                m_messageLog.LogMessage(message, m_messageLogClosed);
#endif
                // to get the device's response as text, use SendMessage.
                // to get the device's response as a numeric, use QueryValue.
                // all messages can respond with text but not all
                // messages can respond with a numeric.

                DaqResponse response = m_daqDevice.SendMessage(message);

                if (ctrTextRadioButton.Checked)
                    ctrResponseTextBox.Text = response.ToString();
                else
                    ctrResponseTextBox.Text = response.ToValue().ToString();

                statusLabel.Text = "Success";
            }
            catch (DaqException ex)
            {
                // SendMessage will throw an exception if an error occurs
                // so the exception needs to be handled. Here, the exception message will
                // be displayed by the status label
                ctrResponseTextBox.Text = String.Empty;
                statusLabel.Text = ex.Message;
            }
        }

        //==============================================================================
        /// <summary>
        /// This enables the radio buttons when the message is for querying the Value property
        /// This allows the value property to be returned as text or as a numeric
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">The event args</param>
        //==============================================================================
        private void OnCtrMessageChanged(object sender, EventArgs e)
        {
            if (ctrMessageComboBox.Text.Contains("?") && ctrMessageComboBox.Text.Contains("VALUE"))
            {
                ctrTextRadioButton.Enabled = true;
                ctrNumericRadioButton.Enabled = true;
            }
            else
            {
                ctrTextRadioButton.Checked = true;
                ctrTextRadioButton.Enabled = false;
                ctrNumericRadioButton.Enabled = false;
            }
        }
    }
}
