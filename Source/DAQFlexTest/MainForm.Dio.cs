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
        /// Fills the aiMessageComboBox with messages supported by the AI Component
        /// </summary>
        //==============================================================================
        private void InitializeDioMessageComboBox(List<string> commands)
        {
            dioMessageComboBox.Items.Clear();

            // add the messages to the message combobox
            if (commands != null)
            {
                dioMessageComboBox.Enabled = true;
                dioSendMessageButton.Enabled = true;

                foreach (string command in commands)
                    dioMessageComboBox.Items.Add(command);

                dioMessageComboBox.SelectedIndex = 0;
            }
            else
            {
                dioMessageComboBox.Enabled = false;
                dioSendMessageButton.Enabled = false;
            }
        }

        //==============================================================================
        /// <summary>
        /// Sends a message to the device and displays the response in a text box
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">The event args</param>
        //==============================================================================
        private void OnSendDioMessage(object sender, EventArgs e)
        {
            try
            {
                string message = dioMessageComboBox.Text;

#if !WindowsCE
                // log message
                m_messageLog.LogMessage(message, m_messageLogClosed);
#endif
                DaqResponse response = m_daqDevice.SendMessage(message);

                if (dioTextRadioButton.Checked)
                    dioResponseTextBox.Text = response.ToString();
                else
                    dioResponseTextBox.Text = response.ToValue().ToString();

                statusLabel.Text = "Success";
            }
            catch (DaqException ex)
            {
                // SendMessage and QueryValue will throw an exception if an error occurs
                // so the exception needs to be handled. Here, the exception message will
                // be displayed by the status label
                dioResponseTextBox.Text = String.Empty;
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
        private void OnDioMessageChanged(object sender, EventArgs e)
        {
            if (dioMessageComboBox.Text.Contains("?") && dioMessageComboBox.Text.Contains("VALUE"))
            {
                dioTextRadioButton.Enabled = true;
                dioNumericRadioButton.Enabled = true;
            }
            else
            {
                dioTextRadioButton.Checked = true;
                dioTextRadioButton.Enabled = false;
                dioNumericRadioButton.Enabled = false;
            }
        }
    }
}
