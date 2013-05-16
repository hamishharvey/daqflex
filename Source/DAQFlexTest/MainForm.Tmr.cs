using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class MainForm
    {
        //==============================================================================
        /// <summary>
        /// Fills the tmrMessageComboBox with messages supported by the CTR Component
        /// </summary>
        //==============================================================================
        private void InitializeTmrMessageComboBox(List<string> commands)
        {
            tmrMessageComboBox.Items.Clear();

            // add the messages to the message combobox
            if (commands != null)
            {
                tmrMessageComboBox.Enabled = true;
                tmrSendMessageButton.Enabled = true;

                foreach (string command in commands)
                    tmrMessageComboBox.Items.Add(command);

                tmrMessageComboBox.Sorted = true;
                tmrMessageComboBox.SelectedIndex = 0;
            }
            else
            {
                tmrMessageComboBox.Enabled = false;
                tmrSendMessageButton.Enabled = false;
            }
        }

        //==============================================================================
        /// <summary>
        /// Sends a message to the device and displays the response in a text box
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">The event args</param>
        //==============================================================================
        private void OnSendTmrMessage(object sender, EventArgs e)
        {
            try
            {
                string message = tmrMessageComboBox.Text;

#if !WindowsCE
                // log message
                m_messageLog.LogMessage(message, m_messageLogClosed);
#endif
                // to get the device's response as text, use SendMessage.
                // to get the device's response as a numeric, use QueryValue.
                // all messages can respond with text but not all
                // messages can respond with a numeric.

                DaqResponse response = m_daqDevice.SendMessage(message);

                tmrResponseTextBox.Text = response.ToString();

                double numericResponse = response.ToValue();

                if (!Double.IsNaN(numericResponse))
                    tmrNumericResponseTextBox.Text = numericResponse.ToString();
                else
                    tmrNumericResponseTextBox.Text = String.Empty;

                statusLabel.Text = "Success";
            }
            catch (DaqException ex)
            {
                // SendMessage will throw an exception if an error occurs
                // so the exception needs to be handled. Here, the exception message will
                // be displayed by the status label
                tmrResponseTextBox.Text = String.Empty;
                statusLabel.Text = ex.Message;
            }
        }
    }
}
