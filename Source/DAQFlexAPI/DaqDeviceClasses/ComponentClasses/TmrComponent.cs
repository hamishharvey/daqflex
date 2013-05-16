//***************************************************************************************
//
// DAQFlex API Library
//
// Copyright (c) 2009, Measurement Computing Corporation
// All rights reserved
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the MEASUREMENT COMPUTING SOFTWARE LICENSE AGREEMENT (DAQFlex API)
// by Measurement Computing Coporation.
// 
// You should have received a copy of the MEASUREMENT COMPUTING SOFTWARE 
// LICENSE AGREEMENT (DAQFlex API) with this library; If not you can contact
// Measurement Computing Corporation, 10 Commerce Way, Norton MA 02766 USA.
// 
//***************************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace MeasurementComputing.DAQFlex
{
    class TmrComponent : IoComponent
    {
        protected double[] m_minPeriod;
        protected double[] m_maxPeriod;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        public TmrComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
            m_minPeriod = new double[maxChannels];
            m_maxPeriod = new double[maxChannels];
        }

        //====================================================================================
        /// <summary>
        /// Default initialization for timers
        /// </summary>
        //====================================================================================
        internal override void Initialize()
        {
            string clkSrc;
            string devCapsQuery;

            for (int i = 0; i < m_maxChannels; i++)
            {
                // get the clock source for this counter
                devCapsQuery = String.Format("TMR{0}:CLKSRC", MessageTranslator.GetChannelSpecs(i));
                clkSrc = m_daqDevice.GetDevCapsString(devCapsQuery, false);

                if (clkSrc.Contains(DevCapValues.INT))
                {
                    // gget the internal base frequency for this counter
                    devCapsQuery = String.Format("TMR{0}:BASEFREQ", MessageTranslator.GetChannelSpecs(i));
                    double baseFreq = m_daqDevice.GetDevCapsValue(devCapsQuery);

                    // get the max count of this counter
                    devCapsQuery = String.Format("TMR{0}:MAXCOUNT", MessageTranslator.GetChannelSpecs(i));
                    double maxCount = m_daqDevice.GetDevCapsValue(devCapsQuery);

                    // calculate the min and max periods
                    m_minPeriod[i] = 2.0 / baseFreq;
                    m_maxPeriod[i] = (maxCount * m_minPeriod[i]) / 2.0;

                    // specify these in milliseconds
                    m_minPeriod[i] *= 1000;
                    m_maxPeriod[i] *= 1000;
                }
                else
                {
                    m_minPeriod[i] = Double.MinValue;
                    m_maxPeriod[i] = Double.MaxValue;
                }
            }
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a direction message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessPeriodMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            double period;

            // get the counter number
            int channel = MessageTranslator.GetChannel(message);

            // get the period value
            string value = MessageTranslator.GetPropertyValue(message);

            // convert the period value to a numeric and validate
            if (PlatformParser.TryParse(value, out period))
            {
                if (period < m_minPeriod[channel] || period > m_maxPeriod[channel])
                    errorCode = ErrorCodes.InvalidTimerPeriod;
            }
            else
            {
                errorCode = ErrorCodes.InvalidTimerPeriod;
            }

            return errorCode;
        }

        //===================================================================================================
        /// <summary>
        /// Gets a list of messages supported by the CTR Component
        /// </summary>
        /// <param name="daqComponent">The Daq Component name - not all implementations require this</param>
        /// <returns>The list of messages</returns>
        //===================================================================================================
        internal override List<string> GetMessages(string daqComponent)
        {
            List<string> messages = new List<string>();

            messages.Add("?TMR");

            messages.Add("TMR{*}:PERIOD=*");
            messages.Add("?TMR{*}:PERIOD");
            messages.Add("TMR{*}:START");
            messages.Add("TMR{*}:STOP");

            return messages;
        }
    }
}
