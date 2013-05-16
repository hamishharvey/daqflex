using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MeasurementComputing.DAQFlex.Test
{
    public partial class CalibrateAiForm : Form
    {
        private DaqDevice m_daqDevice;
        private string m_calStatus;

        public CalibrateAiForm()
        {
            InitializeComponent();
        }

        public CalibrateAiForm(DaqDevice daqDevice)
        {
            m_daqDevice = daqDevice;

            InitializeComponent();
        }

        private void OnStart(object sender, EventArgs e)
        {

            startButton.Enabled = false;
            okButton.Enabled = false;

            try
            {
                m_daqDevice.SendMessage("AICAL:START");

                string percentComplete;
                int progress;

                do
                {
                    m_calStatus = m_daqDevice.SendMessage("?AICAL:STATUS").ToString();
                    percentComplete = m_calStatus.Substring(m_calStatus.IndexOf('/') + 1);

                    try
                    {
                        progress = Int32.Parse(percentComplete);
                        calProgressBar.Value = progress;
                        calProgressLabel.Text = "Running";
                    }
                    catch (Exception)
                    {
                    }

                    Application.DoEvents();

                } while (m_calStatus.Contains("RUNNING"));

                if (m_calStatus.Contains("Error"))
                    calProgressLabel.Text = m_calStatus;
                else
                    calProgressLabel.Text = "Complete";
            }
            catch (Exception ex)
            {
                calProgressLabel.Text = ex.Message;
            }

            startButton.Enabled = true;
            okButton.Enabled = true;
        }

        private void OnOk(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (m_calStatus != null && m_calStatus.Contains("RUNNING"))
                e.Cancel = true;

            base.OnClosing(e);
        }
    }
}