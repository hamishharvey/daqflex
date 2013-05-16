using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;
using System.Windows.Forms;
using System.Globalization;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class MainForm
    {
        //==============================================================================
        /// <summary>
        /// Fills the aiMessageComboBox with messages supported by the AI Component
        /// </summary>
        //==============================================================================
        private void InitializeAiMessageComboBox(List<string> commands)
        {
            aiMessageComboBox.Items.Clear();

            // add the messages to the message combobox
            if (commands != null)
            {
                aiMessageComboBox.Enabled = true;
                aiSendMessageButton.Enabled = true;

                foreach (string command in commands)
                    aiMessageComboBox.Items.Add(command);

                aiMessageComboBox.SelectedIndex = 0;
            }
            else
            {
                aiMessageComboBox.Enabled = false;
                aiSendMessageButton.Enabled = false;
            }
        }

        //==============================================================================
        /// <summary>
        /// Sends a message to the device and displays the response in a text box
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">The event args</param>
        //==============================================================================
        private void OnSendAiMessage(object sender, EventArgs e)
        {
            DaqResponse response = null;

            try
            {
                string message = aiMessageComboBox.Text;

#if !WindowsCE
                // log message
                m_messageLog.LogMessage(message, m_messageLogClosed);
#endif
                // send the message to the device
                response = m_daqDevice.SendMessage(message);

                // the DaqResponse has a method to get the value as a string and a method to get the value as a numeric
                if (aiTextRadioButton.Checked)
                    aiResponseTextBox.Text = response.ToString();
                else
                    aiResponseTextBox.Text = response.ToValue().ToString("F04");

                statusLabel.Text = "Success";
            }
            catch (DaqException ex)
            {
                // SendMessage will throw an exception if an error occurs
                // so the exception needs to be handled. Here, the exception message will
                // be displayed by the status label

                if (ex.LastResponse != null)
                {
                    if (aiTextRadioButton.Checked)
                        aiResponseTextBox.Text = ex.LastResponse.ToString();
                    else
                        aiResponseTextBox.Text = ex.LastResponse.ToValue().ToString("F04");
                }
                else
                {
                    aiResponseTextBox.Text = String.Empty;
                }

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
        private void OnAiMessageChanged(object sender, EventArgs e)
        {
            if (aiMessageComboBox.Text.Contains("?") && aiMessageComboBox.Text.Contains("VALUE"))
            {
                aiTextRadioButton.Enabled = true;
                aiNumericRadioButton.Enabled = true;
            }
            else
            {
                aiTextRadioButton.Checked = true;
                aiTextRadioButton.Enabled = false;
                aiNumericRadioButton.Enabled = false;
            }
        }
    }
}
