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
    internal class CtrComponent : IoComponent
    {
        protected int[] m_dataWidths;
        protected CounterTypes[] m_counterTypes;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        public CtrComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
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

            if (messageType == DaqComponents.CTR)
            {
                return PreprocessCtrMessage(ref message);
            }

            System.Diagnostics.Debug.Assert(false, "Invalid component for CTR");

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
        internal virtual ErrorCodes PreprocessCtrMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                errorCode = ValidateChannel(ref message);
            }

            if (errorCode == ErrorCodes.NoErrors && !message.Contains(Constants.QUERY.ToString()) && message.Contains(DaqProperties.VALUE))
            {
                errorCode = ValidateCount(ref message);
            }

            return errorCode;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the channel number
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ValidateChannel(ref string message)
        {
            int channel = MessageTranslator.GetChannel(message);

            if (channel < 0 || channel > (m_counterTypes.Length - 1))
                return ErrorCodes.InvalidCtrChannelSpecified;

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the count value
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ValidateCount(ref string message)
        {
            try
            {
                int channel = MessageTranslator.GetChannel(message);
                int count = Int32.Parse(MessageTranslator.GetPropertyValue(message));

                if (count < 0 || count > m_dataWidths[channel])
                    return ErrorCodes.InvalidCountValueSpecified;

                return ErrorCodes.NoErrors;
            }
            catch (Exception)
            {
                return ErrorCodes.InvalidCountValueSpecified;
            }
        }

        //====================================================================================
        /// <summary>
        /// Default implementation for initializing counters
        /// Start them by default
        /// </summary>
        //====================================================================================
        internal override void Initialize()
        {
            try
            {
                int counters = (int)m_daqDevice.SendMessage("@CTR:CHANNELS").ToValue();

                if (counters > 0)
                {
                    string msg;

                    for (int i = 0; i < counters; i++)
                    {
                        msg = DaqComponents.CTR + "{" + i.ToString() + "}:START";
                        m_daqDevice.SendMessage(msg);
                    }
                }
            }
            catch (Exception)
            {
                // no counters
            }
        }
    }
}
