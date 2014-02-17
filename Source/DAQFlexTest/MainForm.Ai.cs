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

                // get a list of supported messages for the analog output cal component
                commands.AddRange(m_daqDevice.GetSupportedMessages("AICAL"));

                // all commands are in the list, now sort them
                commands.Sort();

                foreach (string command in commands)
                    aiMessageComboBox.Items.Add(command);

                aiMessageComboBox.SelectedIndex = 0;

                // check if the device support self calibration
                string response = m_daqDevice.SendMessage("@AI:SELFCAL").ToString();

                if (response.Contains("NOT_SUPPORTED"))
                    aiCalibrateButton.Enabled = false;
                else
                    aiCalibrateButton.Enabled = true;
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

                aiResponseTextBox.Text = response.ToString();

                double numericResponse = response.ToValue();

                if (!Double.IsNaN(numericResponse))
                    aiNumericResponseTextBox.Text = numericResponse.ToString();
                else
                    aiNumericResponseTextBox.Text = String.Empty;

                statusLabel.Text = "Success";
            }
            catch (DaqException ex)
            {
                // SendMessage will throw an exception if an error occurs
                // so the exception needs to be handled. Here, the exception message will
                // be displayed by the status label

                if (ex.LastResponse != null)
                {
                    aiResponseTextBox.Text = ex.LastResponse.ToString();

                    double numericResponse = ex.LastResponse.ToValue();

                    if (!Double.IsNaN(numericResponse))
                        aiNumericResponseTextBox.Text = numericResponse.ToString();
                    else
                        aiNumericResponseTextBox.Text = String.Empty;
                }
                else
                {
                    aiResponseTextBox.Text = String.Empty;
                }

                statusLabel.Text = ex.Message;
            }
        }

        private void OnAiCalibrate(object sender, EventArgs e)
        {
            using (CalibrateAiForm caf = new CalibrateAiForm(m_daqDevice))
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
