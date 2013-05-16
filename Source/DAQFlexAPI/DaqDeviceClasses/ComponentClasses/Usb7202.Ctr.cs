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
    class Usb7202Ctr : CtrComponent
    {
        //===================================================================================================
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="daqDevice">A reference to the DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">A referemce tp the DeviceInfo object that the Driver Interace uses</param>
        //===================================================================================================
        public Usb7202Ctr(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 1)
        {
            m_dataWidths = new int[m_maxChannels];
            m_counterTypes = new CounterTypes[m_maxChannels];

            m_dataWidths[0] = (int)Math.Pow(2, 16) - 1;
            m_counterTypes[0] = CounterTypes.Event;
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
            try
            {
                int count = Int32.Parse(MessageTranslator.GetPropertyValue(message));

                if (count != 0)
                    return ErrorCodes.InvalidCountValueSpecified;

                return ErrorCodes.NoErrors;
            }
            catch (Exception)
            {
                return ErrorCodes.InvalidCountValueSpecified;
            }
        }
    }
}
