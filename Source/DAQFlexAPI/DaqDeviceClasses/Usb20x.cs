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
    class Usb20x : DaqDevice
    {
        //===========================================================================
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="deviceInfo">deviceInfo</param>
        //===========================================================================
        internal Usb20x(DeviceInfo deviceInfo)
            : base(deviceInfo, 0x0040)
        {
            m_memLockAddr = 0x00;
            m_memAddrCmd = 0x00;

            m_memReadCmd = 0x32;
            m_memWriteCmd = 0x32;
            m_memOffsetLength = 2;

            m_eepromAssistant = new EepromAssistantIV(m_driverInterface);
        }

    }
}