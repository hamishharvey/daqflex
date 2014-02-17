﻿//***************************************************************************************
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
using System.Threading;

namespace MeasurementComputing.DAQFlex
{
    internal class Usb7204 : DaqDevice
    {
        //===========================================================================
        /// <summary>
        /// Sub-class of DaqDevice that provides methods specific to the USB-7204
        /// </summary>
        /// <param name="deviceInfo">A device info object</param>
        //===========================================================================
        internal Usb7204(DeviceInfo deviceInfo)
            : base(deviceInfo, 0x200)
        {
            Ai = new Usb7204Ai(this, deviceInfo);
            Ao = new Usb7204Ao(this, deviceInfo);
            Dio = new DioComponent(this, deviceInfo, 2); 
            Ctr = new EventCounter(this, deviceInfo, 1);

            m_memLockAddr = 0x400;
            m_memUnlockCode = 0xAA55;
            m_memLockCode = 0xFFFF;

            // 1/10/2012: version 1.2
            m_defaultDevCapsImage = new byte[] 
                {   
                    0x1F,0x8B,0x08,0x00,0x00,0x00,0x00,0x00,0x04,0x00,0xED,0xBD,0x07,0x60,0x1C,0x49,
                    0x96,0x25,0x26,0x2F,0x6D,0xCA,0x7B,0x7F,0x4A,0xF5,0x4A,0xD7,0xE0,0x74,0xA1,0x08,
                    0x80,0x60,0x13,0x24,0xD8,0x90,0x40,0x10,0xEC,0xC1,0x88,0xCD,0xE6,0x92,0xEC,0x1D,
                    0x69,0x47,0x23,0x29,0xAB,0x2A,0x81,0xCA,0x65,0x56,0x65,0x5D,0x66,0x16,0x40,0xCC,
                    0xED,0x9D,0xBC,0xF7,0xDE,0x7B,0xEF,0xBD,0xF7,0xDE,0x7B,0xEF,0xBD,0xF7,0xBA,0x3B,
                    0x9D,0x4E,0x27,0xF7,0xDF,0xFF,0x3F,0x5C,0x66,0x64,0x01,0x6C,0xF6,0xCE,0x4A,0xDA,
                    0xC9,0x9E,0x21,0x80,0xAA,0xC8,0x1F,0x3F,0x7E,0x7C,0x1F,0x3F,0x22,0xF6,0xF6,0xF7,
                    0x7E,0xF7,0xDD,0xF1,0xEE,0xCE,0xEF,0xBE,0xFB,0x68,0xE7,0xD1,0xB7,0x1E,0xFD,0xDE,
                    0x8F,0xF6,0x1E,0xD1,0x6F,0xA3,0xDD,0x47,0xBB,0xA3,0x7B,0xF4,0x19,0xFE,0xFB,0xBD,
                    0xF1,0xC9,0xA3,0x03,0xFA,0x6B,0xCF,0xFE,0xB5,0xBF,0xF3,0xF0,0x3E,0x7D,0x70,0x8F,
                    0xFE,0xF8,0x16,0x37,0x92,0xC6,0x7B,0xE6,0x6B,0x6E,0x6C,0xFF,0x32,0x8D,0xF7,0xA8,
                    0x31,0x60,0xEC,0x8D,0xEE,0x8D,0xF6,0x47,0xF7,0x47,0x9F,0x8E,0x1E,0x8C,0x0E,0x46,
                    0x0F,0xE9,0xBB,0x7D,0xEE,0x1B,0x8D,0x77,0x77,0x80,0xCB,0x7D,0x0B,0x78,0xFF,0x3E,
                    0xFF,0xB9,0xE7,0xFF,0xF9,0xA9,0xB6,0xA6,0x3F,0x0F,0x7E,0xF7,0xBD,0x47,0x0F,0xED,
                    0xCB,0xF7,0x77,0xE8,0xA1,0x4F,0x76,0x77,0xED,0x47,0x3B,0xE3,0xFB,0x9F,0x3E,0xC4,
                    0x47,0x3B,0xFD,0x56,0x7B,0x0E,0x0E,0x37,0x79,0x60,0x9B,0xDC,0xDB,0x7B,0xF0,0x29,
                    0x40,0xEF,0x3A,0xD8,0xBB,0x9F,0xE2,0xEF,0x03,0xFB,0xCA,0x83,0xDD,0xD1,0x2E,0x61,
                    0x43,0xC3,0x74,0x84,0xBB,0x7F,0x6F,0x74,0x7F,0x9F,0x3E,0xBA,0x6F,0x9A,0xA1,0x33,
                    0x34,0xD9,0x75,0x4D,0x1E,0x8E,0x3E,0xDD,0x19,0x7D,0xBA,0x8B,0x4F,0xF7,0xEC,0xA7,
                    0x07,0xF7,0x47,0x07,0x80,0x7F,0xDF,0xA1,0xF0,0xE9,0xFE,0xEF,0x4E,0x14,0xBB,0x67,
                    0xFB,0xFB,0x74,0x0F,0x7F,0xEF,0xDB,0x57,0x3E,0xDD,0x1F,0x7D,0x7A,0x9F,0x3E,0xBA,
                    0xD7,0x85,0xB2,0xFF,0xC8,0x8D,0x7E,0x8F,0xFE,0x32,0xA3,0xD4,0x89,0xD8,0x7F,0x04,
                    0x98,0x42,0xCD,0xBD,0x07,0xF4,0x67,0x48,0x7B,0x6A,0x7F,0xDF,0x7E,0x4F,0xE3,0xBB,
                    0xEF,0x13,0x73,0x17,0x7F,0x3A,0x42,0xEE,0x32,0x21,0xEF,0xFB,0x84,0xBC,0xFF,0x00,
                    0x7F,0xBB,0x51,0xEC,0xEE,0xEC,0xED,0xD3,0x27,0xE1,0xB8,0x1E,0x04,0x18,0x3E,0x78,
                    0xB4,0x07,0x72,0xED,0x6C,0x0B,0x83,0x7D,0xFA,0xE9,0xE8,0xD3,0x07,0xF8,0x50,0x3F,
                    0xE3,0x46,0xF7,0xEF,0xD3,0x27,0x21,0xA2,0x0F,0x3D,0x20,0xBB,0xF4,0xD7,0x1E,0xB8,
                    0x62,0x87,0x91,0x78,0x80,0x6F,0xF7,0xF4,0x4F,0x1A,0xF6,0xDE,0xC3,0xFD,0x87,0x9F,
                    0x3E,0xD8,0xA3,0xC1,0xD3,0xC7,0x0F,0x6C,0xB3,0x83,0x07,0xF4,0x77,0x08,0xF4,0xDE,
                    0x83,0xFB,0xFB,0xF7,0xEF,0xDF,0xDF,0xDD,0xFD,0x7F,0x00,0x38,0xE6,0xC9,0xB0,0x18,
                    0x03,0x00,0x00 };

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
            if (capsKey.Contains(DaqComponents.AI) && (capsKey.Contains(DevCapNames.CHANNELS) ||
                                                       capsKey.Contains(DevCapNames.MAXCOUNT) ||
                                                       capsKey.Contains(DevCapNames.RANGES)))
            {
                //string config = MessageTranslator.GetPropertyValue(SendMessage("?AI:CHMODE").ToString());
                string config;
                string response;
                string capsName;


                if (capsKey.Contains(Constants.VALUE_RESOLVER.ToString()))
                {
                    capsName = capsKey;
                }
                else
                {
                    string msg = Messages.AI_CHMODE_QUERY;
                    SendMessageDirect(msg);
                    response = m_driverInterface.ReadStringDirect();
                    config = MessageTranslator.GetPropertyValue(response);
                    capsName = capsKey + Constants.VALUE_RESOLVER + config;
                }

                string capsValue;

                bool valueFound = m_deviceCaps.TryGetValue(capsName, out capsValue);

                if (valueFound)
                {
                    try
                    {
                        if (trim)
                        {
                            capsValue = capsValue.Substring(capsValue.IndexOf(Constants.PERCENT) + 1);
                        }
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.Assert(false, "Exception in GetDevCapsValue");
                    }

                    return MessageTranslator.ConvertToCurrentCulture(capsValue);
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return base.GetDevCapsString(capsKey, trim);
            }
        }

        //=====================================================================================================================
        /// <summary>
        /// Handles the device reflection messages
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The message response</returns>
        //=====================================================================================================================
        internal override DaqResponse GetDeviceCapability(string message)
        {
            if (message.Contains(DaqComponents.AOSCAN) && !Ao.m_aoScanSupported)
                return new DaqResponse("NOT_SUPPORTED", Double.NaN);

            return base.GetDeviceCapability(message);
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
        //            // The device may have been unplugged
        //        }
        //    }
        //}
    }
}
