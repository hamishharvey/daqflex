using System;
using System.Collections.Generic;
using System.Text;
using MeasurementComputing.DAQFlex;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class MainForm
    {
        //=====================================================================================
        /// <summary>
        /// Fills the devMessageComboBox with messages supported by the DEV Component (device)
        /// </summary>
        //=====================================================================================
        private void InitializeDevMessageComboBox(List<string> commands)
        {
            // add the messages to the message combobox
            devMessageComboBox.Items.Clear();

            foreach (string command in commands)
                devMessageComboBox.Items.Add(command);

            devMessageComboBox.SelectedIndex = 0;
        }

        //==============================================================================
        /// <summary>
        /// Sends a message to the device and displays the response in a text box
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">The event args</param>
        //==============================================================================
        private void OnSendDevMessage(object sender, EventArgs e)
        {
            try
            {
                string message = devMessageComboBox.Text;

#if !WindowsCE
                // log message
                m_messageLog.LogMessage(message, m_messageLogClosed);
#endif
                // to get the device's response as text, use SendMessage.
                DaqResponse response = m_daqDevice.SendMessage(message);

                devResponseTextBox.Text = response.ToString();
                
                statusLabel.Text = "Success";
            }
            catch (DaqException ex)
            {
                // SendMessage will throw an exception if an error occurs
                // so the exception needs to be handled. Here, the exception message will
                // be displayed by the status label
                devResponseTextBox.Text = String.Empty;
                statusLabel.Text = ex.Message;
            }
        }
    }
}
