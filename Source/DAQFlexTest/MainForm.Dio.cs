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

                commands.Sort();

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

                dioResponseTextBox.Text = response.ToString();

                double numericResponse = response.ToValue();

                if (!Double.IsNaN(numericResponse))
                    dioNumericResponseTextBox.Text = numericResponse.ToString();
                else
                    dioNumericResponseTextBox.Text = String.Empty;

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
    }
}
