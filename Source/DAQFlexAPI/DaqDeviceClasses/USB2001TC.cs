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
using System.Reflection;

namespace MeasurementComputing.DAQFlex
{
    internal class Usb2001Tc : DaqDevice
    {
        //===========================================================================
        /// <summary>
        /// Sub-class of DaqDevice that provides methods specific to the USB-2001-TC
        /// </summary>
        /// <param name="deviceInfo">A device info object</param>
        //===========================================================================
        internal Usb2001Tc(DeviceInfo deviceInfo)
            : base(deviceInfo, 0x200)
        {
            Ai = new Usb2001TcAi(this, deviceInfo);

            m_memLockAddr = 0x400;
            m_memUnlockCode = 0xAA55;
            m_memLockCode = 0xFFFF;

            // 9/10/2010: version 1.0
            m_defaultDevCapsImage = new byte[] 
                { 0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0xED, 0xBD, 0x07, 0x60, 0x1C, 0x49, 
                  0x96, 0x25, 0x26, 0x2F, 0x6D, 0xCA, 0x7B, 0x7F, 0x4A, 0xF5, 0x4A, 0xD7, 0xE0, 0x74, 0xA1, 0x08, 
                  0x80, 0x60, 0x13, 0x24, 0xD8, 0x90, 0x40, 0x10, 0xEC, 0xC1, 0x88, 0xCD, 0xE6, 0x92, 0xEC, 0x1D, 
                  0x69, 0x47, 0x23, 0x29, 0xAB, 0x2A, 0x81, 0xCA, 0x65, 0x56, 0x65, 0x5D, 0x66, 0x16, 0x40, 0xCC, 
                  0xED, 0x9D, 0xBC, 0xF7, 0xDE, 0x7B, 0xEF, 0xBD, 0xF7, 0xDE, 0x7B, 0xEF, 0xBD, 0xF7, 0xBA, 0x3B, 
                  0x9D, 0x4E, 0x27, 0xF7, 0xDF, 0xFF, 0x3F, 0x5C, 0x66, 0x64, 0x01, 0x6C, 0xF6, 0xCE, 0x4A, 0xDA, 
                  0xC9, 0x9E, 0x21, 0x80, 0xAA, 0xC8, 0x1F, 0x3F, 0x7E, 0x7C, 0x1F, 0x3F, 0x22, 0xF6, 0xF6, 0x1F, 
                  0xFE, 0xEE, 0xBB, 0xE3, 0x9D, 0x9D, 0xDF, 0x7D, 0xF7, 0xD1, 0xCE, 0xA3, 0x6F, 0x3D, 0xFA, 0xBD, 
                  0x1F, 0xED, 0xE2, 0xBF, 0xDF, 0x1D, 0xFF, 0xCA, 0x5F, 0x3B, 0xFC, 0xD7, 0x9E, 0xFB, 0x6B, 0x67, 
                  0xFF, 0xE0, 0xFE, 0x83, 0xFB, 0xF4, 0xD9, 0x3D, 0xFA, 0x6C, 0x87, 0xBE, 0xD9, 0x7D, 0xF4, 0x60, 
                  0x7F, 0xC4, 0x9F, 0xEC, 0xDB, 0x56, 0xF7, 0x01, 0xF0, 0x3E, 0xFD, 0xF9, 0x2D, 0x06, 0xB8, 0xFF, 
                  0x80, 0xFE, 0xFC, 0xD4, 0xC2, 0xDF, 0x3F, 0x00, 0xC8, 0x87, 0xF6, 0xEB, 0x07, 0x9F, 0x02, 0xDC, 
                  0x0E, 0xFF, 0xCD, 0xF0, 0x1E, 0x8C, 0x1E, 0x1C, 0x8C, 0x1E, 0x3C, 0x1C, 0x1D, 0xEC, 0x8C, 0x0E, 
                  0x76, 0x47, 0x07, 0x7B, 0xA3, 0x83, 0x7B, 0xA3, 0x83, 0x7D, 0x34, 0x02, 0x5A, 0x3B, 0xDC, 0x05, 
                  0xF5, 0xB0, 0xB7, 0xF7, 0xE0, 0xC1, 0xDE, 0xFD, 0xFD, 0x07, 0xF7, 0xFF, 0x1F, 0x9C, 0x65, 0xD9, 
                  0x80, 0xC6, 0x00, 0x00, 0x00 };
        }

        //===============================================================================================================
        /// <summary>
        /// Loads the device's capabilities and stores them in a list
        /// </summary>
        //===============================================================================================================
        internal override void LoadDeviceCaps(bool forceUpdate)
        {
            SendMessageDirect("?DEV:FWV");
            string response = m_driverInterface.ReadString();

            if (!response.Contains("01.01.00f00"))
            {
                base.LoadDeviceCaps(forceUpdate);
                return;
            }

            m_reflector = new DeviceReflector();
            m_compressedDeviceCaps = m_defaultDevCapsImage;
            m_uncompressedDeviceCaps = m_reflector.DecompressDeviceCapsImage(m_compressedDeviceCaps);
            ConvertDeviceCaps(m_uncompressedDeviceCaps);
        }

        //================================================================================================
        /// <summary>
        /// Gets the type of tc that the channel is currently set for
        /// </summary>
        /// <returns>The tc type</returns>
        //================================================================================================
        internal ThermocoupleTypes GetTcType()
        {
            // query the sensor type which is stored on the device
            SendMessageDirect("?AI{0}:SENSOR");
            string response = m_driverInterface.ReadString();

            string tcType = response.Substring(response.IndexOf(Constants.VALUE_RESOLVER) + 1);

            return GetTcType(tcType);
        }

        //================================================================================================
        /// <summary>
        /// Gets the type of tc based on the type letter passed in
        /// </summary>
        /// <param name="type">The type letter (e.g. "T")</param>
        /// <returns></returns>
        //================================================================================================
        internal ThermocoupleTypes GetTcType(string type)
        {
            ThermocoupleTypes tcType;

            switch (type)
            {
                case "B": tcType = ThermocoupleTypes.TypeB;
                    break;
                case "E": tcType = ThermocoupleTypes.TypeE;
                    break;
                case "J": tcType = ThermocoupleTypes.TypeJ;
                    break;
                case "K": tcType = ThermocoupleTypes.TypeK;
                    break;
                case "N": tcType = ThermocoupleTypes.TypeN;
                    break;
                case "R": tcType = ThermocoupleTypes.TypeR;
                    break;
                case "S": tcType = ThermocoupleTypes.TypeS;
                    break;
                case "T": tcType = ThermocoupleTypes.TypeT;
                    break;
                default: tcType = ThermocoupleTypes.NotSet;
                    break;
            }

            return tcType;
        }

        //==============================================================================
        /// <summary>
        /// Waits for the status to go to READY
        /// </summary>
        //==============================================================================
        internal void WaitForReady()
        {
            bool ready = false;
            string response = String.Empty;

            while (!ready)
            {
                SendMessageDirect("?AI{0}:STATUS");
                response = m_driverInterface.ReadString();

                if (response.Contains("ERROR"))
                    break;

                ready = response.Contains(PropertyValues.READY);
            }
        }

        //============================================================================================
        /// <summary>
        /// Method to modify the response in the case where the message is
        /// an API message
        /// </summary>
        /// <param name="originalResponse">The response sent back from the device</param>
        /// <returns>The original response or a modified version of the original response</returns>
        //============================================================================================
        protected override string AmendResponse(string originalResponse)
        {
            return base.AmendResponse(originalResponse);
        }
    }
}
