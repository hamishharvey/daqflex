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
    class Usb7204Dio : DioComponent
    {
        //===================================================================================================
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="daqDevice">A reference to the DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">A referemce tp the DeviceInfo object that the Driver Interace uses</param>
        //===================================================================================================
        internal Usb7204Dio(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 2)
        {
            for (int i = 0; i < m_maxChannels; i++)
            {
                m_dataWidths[i] = 255;
                m_inMask[i] = 255;
                m_outMask[i] = 0;
            }

            m_portNumbers.Add(10, 0); // port 0 = FIRSTPORTA
            m_portNumbers.Add(11, 1); // port 1 = FIRSTPORTB
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

            messages.Add("DIO{*}:DIR=*");
            messages.Add("DIO{*}:VALUE=*");
            messages.Add("DIO{*/*}:VALUE=*");

            messages.Add("?DIO");
            messages.Add("?DIO{*}:DIR");
            messages.Add("?DIO{*}:VALUE");
            messages.Add("?DIO{*/*}:VALUE");

            return messages;
        }
    }
}
