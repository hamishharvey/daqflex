using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class MessageLog : Form
    {
        private List<String> m_messages = new List<string>();

        public MessageLog()
        {
            InitializeComponent();
        }

        public void LogMessage(string message, bool formClosed)
        {
            if (!formClosed)
            {
                m_messages.Add(message);

                string displayText = String.Empty;

                foreach (string s in m_messages)
                {
                    displayText += s;
                    displayText += Environment.NewLine;
                }

                messageLogTextBox.Text = displayText;
            }
        }

        private void OnClearMessageLog(object sender, EventArgs e)
        {
            messageLogTextBox.Text = String.Empty;
            m_messages.Clear();
        }
    }
}