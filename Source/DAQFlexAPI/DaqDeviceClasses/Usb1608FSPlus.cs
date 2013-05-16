﻿//***************************************************************************************
//
// DAQFlex API Library
//
// Copyright (c) 2011, Measurement Computing Corporation
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
    internal class Usb1608FSPlus : DaqDevice
    {
        //===========================================================================
        /// <summary>
        /// Sub-class of DaqDevice that provides methods specific to the USB-7202
        /// </summary>
        /// <param name="deviceInfo">A device info object</param>
        //===========================================================================
        internal Usb1608FSPlus(DeviceInfo deviceInfo)
            : base(deviceInfo, 0x000)
        {
            Ai = new Usb1608FSPlusAi(this, deviceInfo);
            Dio = new DioComponent(this, deviceInfo, 1);
            Ctr = new VirtualSSEventCounter(this, deviceInfo, 1);

            m_memReadCmd = 0x32;
            m_memWriteCmd = 0x32;

            m_memLockAddr = 0x000;
            m_memUnlockCode = 0x0000;
            m_memLockCode = 0x0000;

            m_eepromAssistant = new EepromAssistantIV(m_driverInterface);

            // 2/10/2012: version 1.2
            m_defaultDevCapsImage = new byte[] 
                {   0x1F,0x8B,0x08,0x00,0x00,0x00,0x00,0x00,0x04,0x00,0xED,0xBD,0x07,0x60,0x1C,0x49,
                    0x96,0x25,0x26,0x2F,0x6D,0xCA,0x7B,0x7F,0x4A,0xF5,0x4A,0xD7,0xE0,0x74,0xA1,0x08,
                    0x80,0x60,0x13,0x24,0xD8,0x90,0x40,0x10,0xEC,0xC1,0x88,0xCD,0xE6,0x92,0xEC,0x1D,
                    0x69,0x47,0x23,0x29,0xAB,0x2A,0x81,0xCA,0x65,0x56,0x65,0x5D,0x66,0x16,0x40,0xCC,
                    0xED,0x9D,0xBC,0xF7,0xDE,0x7B,0xEF,0xBD,0xF7,0xDE,0x7B,0xEF,0xBD,0xF7,0xBA,0x3B,
                    0x9D,0x4E,0x27,0xF7,0xDF,0xFF,0x3F,0x5C,0x66,0x64,0x01,0x6C,0xF6,0xCE,0x4A,0xDA,
                    0xC9,0x9E,0x21,0x80,0xAA,0xC8,0x1F,0x3F,0x7E,0x7C,0x1F,0x3F,0x22,0xF6,0xEE,0xED,
                    0xFF,0xEE,0xBB,0xE3,0xBD,0xDF,0x7D,0xF7,0xD1,0xCE,0xA3,0x6F,0x3D,0xFA,0xBD,0x1F,
                    0xED,0xD2,0x7F,0x3B,0xBF,0x3B,0xFE,0x95,0xBF,0x76,0x1E,0x1D,0xD0,0x5F,0x7B,0xF6,
                    0xAF,0x4F,0xEF,0xDF,0xBF,0x77,0x9F,0x3E,0xB9,0xC7,0x2D,0xF6,0xE8,0xDF,0x7B,0xA3,
                    0xFD,0xD1,0x83,0xD1,0x43,0xFA,0x6C,0xDF,0xB6,0xDA,0xDD,0x01,0x8C,0xFB,0xDC,0x06,
                    0xB0,0xF6,0xF1,0xCA,0xA7,0xB6,0x83,0x7D,0xC0,0xDC,0x7F,0x60,0xFF,0xDE,0xBD,0xF7,
                    0xF0,0x77,0xDF,0x7B,0xF4,0xD0,0xBE,0xBE,0xBF,0x83,0x87,0x3E,0xDA,0x75,0x68,0xEC,
                    0x8C,0x77,0x76,0xD0,0x6A,0xD7,0xE0,0xC9,0xBD,0x68,0x33,0x83,0x1F,0xF5,0xC9,0x7F,
                    0xDF,0x73,0xD8,0xDB,0x36,0xF7,0xFB,0xA0,0x02,0x84,0xB5,0x99,0xC3,0x52,0x40,0x19,
                    0x2C,0x77,0x1E,0xDD,0xDB,0x7B,0xF0,0xE9,0x01,0x7D,0xB4,0x27,0x18,0x60,0xEC,0xF7,
                    0xEF,0x8D,0xEE,0xEF,0x8F,0xEE,0x7F,0x4A,0x9F,0xDE,0x3F,0x08,0x80,0xA1,0xDD,0xAE,
                    0x6B,0xF7,0x70,0xF4,0xE9,0xCE,0xE8,0x00,0xED,0x76,0xDD,0x30,0x01,0x6C,0xD7,0xBC,
                    0x46,0xFF,0xED,0xDF,0xA3,0x0F,0xF6,0xF7,0xED,0x5B,0xBB,0x3B,0x0F,0x47,0xBB,0xBB,
                    0x00,0xB5,0x6F,0xB0,0xDA,0x79,0xB4,0x07,0xC8,0x7B,0xB6,0xCD,0xC1,0x7D,0x01,0x7B,
                    0xCF,0x0C,0x99,0xA9,0x4B,0xE8,0x38,0xBC,0x3F,0xDD,0xFF,0xDD,0xEF,0x3D,0xDA,0x73,
                    0xDF,0x7F,0xBA,0x87,0xBF,0x5D,0x37,0x9F,0xEE,0x8F,0x3E,0xBD,0x3F,0x7A,0xB8,0x37,
                    0x7A,0x78,0xEF,0x77,0x7F,0xF0,0xC8,0x51,0x7C,0x97,0xFE,0xDA,0x03,0x7A,0x3B,0xD2,
                    0xEC,0xD3,0xD1,0xA7,0x0F,0x46,0x9F,0x1E,0x8C,0x3E,0x7D,0x88,0x2F,0xF8,0x73,0x46,
                    0xE8,0xFE,0x7D,0xFA,0x3B,0x9C,0x7B,0xFA,0x7B,0xC7,0xBE,0xF8,0x70,0x7F,0xF4,0xF0,
                    0xFE,0xEF,0xFE,0x30,0x80,0xFC,0xF0,0xD1,0xDE,0xA7,0x0A,0x61,0xF7,0xD1,0x83,0x1D,
                    0xFC,0x6D,0x01,0xEE,0xEF,0x3D,0xDC,0x7F,0xF8,0xE9,0x83,0x3D,0x7E,0x69,0x7F,0xD7,
                    0x7E,0x8E,0x56,0xFB,0xAE,0x19,0xBF,0xF4,0xC0,0x02,0x39,0x78,0x80,0xAF,0x03,0x2C,
                    0x76,0xEF,0xED,0xEF,0x3C,0x7C,0xF8,0xF0,0xDE,0xCE,0xCE,0xFF,0x03,0xAF,0x01,0xDB,
                    0xBF,0xE6,0x02,0x00,0x00 };
        }

        //=====================================================================================================================
        /// <summary>
        /// Handles the device reflection messages
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The message response</returns>
        //=====================================================================================================================
        internal override string GetDevCapsString(string capsKey, bool trim)
        {
            if (capsKey.Contains(DaqComponents.AI) && capsKey.Contains(DevCapNames.RANGES))
            {
                if (!capsKey.Contains(Constants.VALUE_RESOLVER.ToString()) && !capsKey.Contains(PropertyValues.SE))
                {
                    capsKey += Constants.VALUE_RESOLVER;
                    capsKey += PropertyValues.SE;
                }
            }

            return base.GetDevCapsString(capsKey, trim);
        }

        //===========================================================================================
        /// <summary>
        /// Overidden to check if a BURSTIO scan running
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <returns>True if the message is to be sent to the device, otherwise false</returns>
        //===========================================================================================
        internal override bool PreprocessMessage(ref string message, string messageType)
        {
            ScanState scanState = DriverInterface.InputScanStatus;

            if (scanState == ScanState.Running && CriticalParams.InputTransferMode == TransferMode.BurstIO)
            {
                if (message.Contains(APIMessages.AISCANCOUNT_QUERY) ||
                        message.Contains(APIMessages.AISCANINDEX_QUERY) ||
                            message.Contains(APIMessages.AISCANSTATUS_QUERY) ||
                                message.Contains(DaqCommands.STOP))
                {
                    return base.PreprocessMessage(ref message, messageType);
                }
                else
                {
                    m_apiMessageError = ErrorCodes.BurstIoInProgress;
                    return false;
                }
            }
            else
            {
                return base.PreprocessMessage(ref message, messageType);
            }
        }

        ////====================================================================
        ///// <summary>
        ///// Method to shut down a device when the application exits
        ///// </summary>
        ////====================================================================
        //protected override void ShutDownDevice()
        //{
        //    if (!m_deviceReleased)
        //    {
        //        try
        //        {
        //            SendMessage("AISCAN:STOP");
        //            SendMessage("CTR{0}:STOP");
        //        }
        //        catch (Exception)
        //        {
        //            // The devic may have been unplugged
        //        }
        //    }
        //}

    }
}