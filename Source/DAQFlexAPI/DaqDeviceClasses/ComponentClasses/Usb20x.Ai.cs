using System;
using System.Collections.Generic;
using System.Text;

namespace MeasurementComputing.DAQFlex
{
    class Usb20xAi : FixedModeAiComponent
    {
        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal Usb20xAi(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 8)
        {
            m_rateCalculatorDecrementCount = 20;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            // create supported ranges list
            m_supportedRanges.Clear();
            m_supportedRanges.Add(PropertyValues.BIP10V + ":SE", new Range(10.0, -10.0));

            // store the current ranges for each channel
            for (int i = 0; i < m_channelCount; i++)
                m_ranges[i] = String.Format("{0}{1}:{2}={3}", DaqComponents.AI, MessageTranslator.GetChannelSpecs(i), DaqProperties.RANGE, PropertyValues.BIP10V);

        }

        //========================================================================================
        /// <summary>
        /// Overriden to read in the AI calibration coefficients
        /// </summary>
        //========================================================================================
        protected override void GetCalCoefficients()
        {
            // get and store cal coefficients for each range - 8 chs (always SE), 1 range (BIP10V)
            double slope = 0;
            double offset = 0;

            m_calCoeffs.Clear();

            for (int i = 0; i < m_channelCount; i++)
            {
                // set the range
                string key = String.Format("Ch{0}:BIP10V:SE", i);

                // get the slope and offset for the range
                m_daqDevice.SendMessageDirect(String.Format("?AI{0}:SLOPE", MessageTranslator.GetChannelSpecs(i)));
                slope = m_daqDevice.DriverInterface.ReadValueDirect();

                m_daqDevice.SendMessageDirect(String.Format("?AI{0}:OFFSET", MessageTranslator.GetChannelSpecs(i)));
                offset = m_daqDevice.DriverInterface.ReadValueDirect();

#if DEBUG
                // if there are no coeffs stored in eeprom yet, set defaults
                if (slope == 0 || Double.IsNaN(slope))
                {
                    slope = 1;
                    offset = 0;
                }
#endif

                m_calCoeffs.Add(key, new CalCoeffs(slope, offset));
            }
        }

        //===========================================================================================
        /// <summary>
        /// Overridden to get the supported messages specific to this Ai component
        /// </summary>
        /// <returns>A list of supported messages</returns>
        //===========================================================================================
        internal override List<string> GetMessages(string daqComponent)
        {
            List<string> messages = new List<string>();

            if (daqComponent == DaqComponents.AI)
            {
                messages.Add("AI:SCALE=*");
                messages.Add("AI:CAL=*");
                messages.Add("?AI");
                messages.Add("?AI:RANGE");
                messages.Add("?AI{*}:RANGE");
                messages.Add("?AI:CHMODE");
                messages.Add("?AI{*}:VALUE");
                messages.Add("?AI{*}:VALUE/*");
                messages.Add("?AI:SCALE");
                messages.Add("?AI:CAL");
                messages.Add("?AI:RES");
                messages.Add("?AI{*}:SLOPE");
                messages.Add("?AI{*}:OFFSET");
            }
            else if (daqComponent == DaqComponents.AISCAN)
            {
                messages.Add("AISCAN:XFRMODE=*");
                messages.Add("AISCAN:HIGHCHAN=*");
                messages.Add("AISCAN:LOWCHAN=*");
                messages.Add("AISCAN:RATE=*");
                messages.Add("AISCAN:SAMPLES=*");
                messages.Add("AISCAN:TRIG=*");
                messages.Add("AISCAN:SCALE=*");
                messages.Add("AISCAN:CAL=*");
                messages.Add("AISCAN:EXTPACER=*");
                messages.Add("AISCAN:BUFSIZE=*");
                messages.Add("AISCAN:BUFOVERWRITE=*");
                messages.Add("AISCAN:START");
                messages.Add("AISCAN:STOP");
                messages.Add("AISCAN:QUEUE=*");

                messages.Add("?AISCAN:XFRMODE");
                messages.Add("?AISCAN:RANGE");
                messages.Add("?AISCAN:RANGE{*}");
                messages.Add("?AISCAN:HIGHCHAN");
                messages.Add("?AISCAN:LOWCHAN");
                messages.Add("?AISCAN:RATE");
                messages.Add("?AISCAN:SAMPLES");
                messages.Add("?AISCAN:TRIG");
                messages.Add("?AISCAN:SCALE");
                messages.Add("?AISCAN:CAL");
                messages.Add("?AISCAN:EXTPACER");
                messages.Add("?AISCAN:STATUS");
                messages.Add("?AISCAN:BUFSIZE");
                messages.Add("?AISCAN:BUFOVERWRITE");
                messages.Add("?AISCAN:COUNT");
                messages.Add("?AISCAN:INDEX");
                messages.Add("?AISCAN:QUEUE");
            }
            else if (daqComponent == DaqComponents.AITRIG)
            {
                messages.Add("AITRIG:SRC=*");
                messages.Add("?AITRIG:SRC");
                messages.Add("AITRIG:TYPE=*");
                messages.Add("?AITRIG:TYPE");
            }
            else if (daqComponent == DaqComponents.AIQUEUE)
            {
                messages.Add("AIQUEUE:CLEAR");
                messages.Add("AIQUEUE{*}:CHAN=*");

                messages.Add("?AIQUEUE:COUNT");
                messages.Add("?AIQUEUE{*}:CHMODE");
                messages.Add("?AIQUEUE{*}:CHAN");
                messages.Add("?AIQUEUE{*}:RANGE");
            }

            return messages;
        }
    }
}
