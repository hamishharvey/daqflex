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
    internal class EventCounter : CtrComponent
    {
        //===================================================================================================
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="daqDevice">A reference to the DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">A referemce tp the DeviceInfo object that the Driver Interace uses</param>
        //===================================================================================================
        public EventCounter(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
        }

        //===================================================================================================
        /// <summary>
        /// Overriden to initialize this IoComponent
        /// </summary>
        //===================================================================================================
        internal override void Initialize()
        {
            base.Initialize();
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

            messages.Add("CTR{*}:VALUE=*");
            messages.Add("CTR{*}:START");
            messages.Add("CTR{*}:STOP");

            messages.Add("?CTR");
            messages.Add("?CTR{*}:VALUE");

            return messages;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the count value
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ValidateCount(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int channel = MessageTranslator.GetChannel(message);
            uint count;

            if (PlatformParser.TryParse(MessageTranslator.GetPropertyValue(message), out count))
            {
                uint ldMin;
                uint ldMax;
                string devCaps;

                devCaps = String.Format("CTR{0}:LDMIN", MessageTranslator.GetChannelSpecs(channel));
                ldMin = (uint)m_daqDevice.GetDevCapsValue(devCaps);
                devCaps = String.Format("CTR{0}:LDMAX", MessageTranslator.GetChannelSpecs(channel));
                ldMax = (uint)m_daqDevice.GetDevCapsValue(devCaps);

                if (count < ldMin || count > ldMax)
                    errorCode = ErrorCodes.InvalidCountValueSpecified;
            }
            else
            {
                errorCode = ErrorCodes.InvalidCountValueSpecified;
            }

            return errorCode;
        }
    }
}
