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
    internal class PulseTimer : TmrComponent
    {
        protected const double MIN_DUTY_CYCLE = 1.0;
        protected const double MAX_DUTY_CYCLE = 99.0;

        protected double[] m_minDelay;
        protected double[] m_maxDelay;

        //============================================================================================================
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        /// <param name="maxChannels">The max number of counters for this device</param>
        //============================================================================================================
        public PulseTimer(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
            m_minDelay = new double[maxChannels];
            m_maxDelay = new double[maxChannels];
        }

        //============================================================================================================
        /// <summary>
        /// Overriden to initialize limits
        /// </summary>
        //============================================================================================================
        internal override void Initialize()
        {
            base.Initialize();

            string clkSrc;
            string devCapsQuery;

            for (int i = 0; i < m_maxChannels; i++)
            {
                // get the clock source for this counter
                devCapsQuery = String.Format("TMR{0}:CLKSRC", MessageTranslator.GetChannelSpecs(i));
                clkSrc = m_daqDevice.GetDevCapsString(devCapsQuery, false);

                if (clkSrc.Contains(DevCapValues.INT))
                {
                    // calculate the min and max delays
                    m_minDelay[i] = 0.0;
                    m_maxDelay[i] = UInt32.MaxValue;
                }
                else
                {
                    m_minDelay[i] = 0.0;
                    m_maxDelay[i] = UInt32.MaxValue;
                }
            }
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The component this message pertains to</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessMessage(ref string message, string messageType)
        {

            ErrorCodes errorCode = base.PreprocessMessage(ref message, messageType);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            if (messageType == DaqComponents.TMR)
            {
                return PreprocessTmrMessage(ref message);
            }

            System.Diagnostics.Debug.Assert(false, "Invalid component for TMR");

            return ErrorCodes.InvalidMessage;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The component this message pertains to</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessTmrMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // check for "{n}" in the message to validate n as the channel
            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
                errorCode = ValidateChannel(ref message);

            // check for "TMR{n}:PERIOD=" in the message
            if (errorCode == ErrorCodes.NoErrors && !message.Contains(Constants.QUERY.ToString()) && message.Contains(DaqProperties.PERIOD))
                errorCode = ProcessPeriodMessage(ref message);

            // check for "TMR{n}:DUTYCYCLE=" in the message
            if (errorCode == ErrorCodes.NoErrors && !message.Contains(Constants.QUERY.ToString()) && message.Contains(DaqProperties.DUTYCYCLE))
                errorCode = ProcessDutyCycleMessage(ref message);

            // check for "TMR{n}:DELAY=" in the message
            if (errorCode == ErrorCodes.NoErrors && !message.Contains(Constants.QUERY.ToString()) && message.Contains(DaqProperties.DELAY))
                errorCode = ProcessDelayMessage(ref message);

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a direction message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessDelayMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            double delay;

            // get the counter number
            int channel = MessageTranslator.GetChannel(message);

            // get the delay value
            string value = MessageTranslator.GetPropertyValue(message);

            // convert delay value to a numeric and validate
            if (PlatformParser.TryParse(value, out delay))
            {
                if (delay < m_minDelay[channel] || delay > m_maxDelay[channel])
                    errorCode = ErrorCodes.InvalidTimerDelay;
            }
            else
            {
                errorCode = ErrorCodes.InvalidTimerDelay;
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a direction message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessDutyCycleMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            double dutyCycle;

            // get the duty cycle value
            string value = MessageTranslator.GetPropertyValue(message);

            // conver the duty cycle to a numeric and validate
            if (PlatformParser.TryParse(value, out dutyCycle))
            {
                if (dutyCycle < MIN_DUTY_CYCLE || dutyCycle > MAX_DUTY_CYCLE)
                    errorCode = ErrorCodes.InvalidTimerDutyCycle;
            }
            else
            {
                errorCode = ErrorCodes.InvalidTimerDutyCycle;
            }

            return errorCode;
        }

        //===================================================================================================
        /// <summary>
        /// Overriden to get the supported messages specific to this Dio component
        /// </summary>
        /// <param name="daqComponent">The Daq Component name - not all implementations require this</param>
        /// <returns>A list of supported messages</returns>
        //===================================================================================================
        internal override List<string> GetMessages(string daqComponent)
        {
            List<string> messages = new List<string>();

            messages.Add("?TMR");

            messages.Add("TMR{*}:PERIOD=*");
            messages.Add("TMR{*}:DUTYCYCLE=*");
            messages.Add("TMR{*}:DELAY=*");
            messages.Add("TMR{*}:PULSECOUNT=*");
            messages.Add("TMR{*}:IDLESTATE=*");

            messages.Add("?TMR{*}:PERIOD");
            messages.Add("?TMR{*}:DUTYCYCLE");
            messages.Add("?TMR{*}:DELAY");
            messages.Add("?TMR{*}:PULSECOUNT");
            messages.Add("?TMR{*}:IDLESTATE");

            messages.Add("TMR{*}:START");
            messages.Add("TMR{*}:STOP");

            return messages;
        }
    }
}
