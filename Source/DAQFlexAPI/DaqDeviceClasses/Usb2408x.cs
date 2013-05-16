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
using System.IO;
using System.Globalization;

namespace MeasurementComputing.DAQFlex
{
    //===========================================================================
    /// <summary>
    /// Base class for  USB-2408 series
    /// </summary>
    /// <param name="deviceInfo">A device info object</param>
    //===========================================================================
    internal class Usb2408x : DaqDevice
    {
        protected byte fpgaDataRequest = 0x51;

        //===========================================================================
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="deviceInfo">deviceInfo</param>
        //===========================================================================
        internal Usb2408x(DeviceInfo deviceInfo)
            : base(deviceInfo, 0x880)
        {
            m_eepromAssistant = new EepromAssistantII(m_driverInterface);
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
            int channel = MessageTranslator.GetChannel(capsKey);

            if (capsKey.Contains(DaqComponents.AI) && channel >= 0 &&
                    (capsKey.Contains(DevCapNames.RANGES) || capsKey.Contains(DevCapNames.INPUTS) ||
                     capsKey.Contains(DevCapNames.SENSORS) || capsKey.Contains(DevCapNames.TCTYPES)))
            {
                string config;
                string response;
                string capsName;

                if (capsKey.Contains(Constants.VALUE_RESOLVER.ToString()))
                {
                    capsName = capsKey;
                }
                else
                {
                    string msg = Messages.AI_CH_CHMODE_QUERY;
                    msg = Messages.InsertChannel(msg, channel);
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

        //=======================================================================
        /// <summary>
        /// Overriden to get Device Component messages supported
        /// by this specific device
        /// </summary>
        /// <returns>The list of messages</returns>
        //=======================================================================
        protected override List<string> GetMessages()
        {
            List<string> list = base.GetMessages();

            list.Add("?DEV:FWV/ISO");

            return list;
        }

        //===================================================================
        /// <summary>
        /// Overriden to load the FPGA
        /// </summary>
        //===================================================================
        internal override void Initialize()
        {
            string response = string.Empty;

            //IntializeTCRanges();

            while (!response.Contains(PropertyValues.READY))
            {
                SendMessageDirect("?DEV:STATUS/ISO");
                response = m_driverInterface.ReadStringDirect();
            }

            base.Initialize();
        }

        ////==========================================================================================
        ///// <summary>
        ///// Rewrites the device's capabilities to eeprom using m_defaultDevCapsImage
        ///// </summary>
        ///// <returns>The error code</returns>
        ////==========================================================================================
        //protected override ErrorCodes RestoreDeviceCaps()
        //{
        //    ErrorCodes errorCode = ErrorCodes.NoErrors;

        //    // unlock the memory for writing
        //    if (m_memLockAddr != 0x00)
        //        errorCode = m_driverInterface.UnlockDeviceMemory(m_memLockAddr, m_memUnlockCode);

        //    if (errorCode != ErrorCodes.NoErrors)
        //        return errorCode;

        //    // get the default device caps image
        //    byte[] buffer = m_defaultDevCapsImage;

        //    byte size = (byte)Constants.MAX_COMMAND_LENGTH;
        //    ushort memoryOffset = m_devCapsOffset;
        //    ushort bufferOffset = 0;

        //    // calculate whole and partial blocks
        //    int wholeBlocks = buffer.Length / size;
        //    int partialBlock = buffer.Length - size * wholeBlocks;

        //    //store the length of the device caps image
        //    byte[] count = new byte[2];
        //    count[0] = (byte)(buffer.Length & 0x00FF);
        //    count[1] = (byte)((buffer.Length & 0xFF00) >> 8);

        //    errorCode = m_driverInterface.WriteDeviceMemory2(m_memWriteCmd, memoryOffset, 2, 0, count, 2);

        //    if (errorCode != ErrorCodes.NoErrors)
        //        return errorCode;

        //    // increment the offset and write the wholse blocks
        //    memoryOffset += 2;

        //    for (int i = 0; i < wholeBlocks; i++)
        //    {
        //        errorCode = m_driverInterface.WriteDeviceMemory2(m_memWriteCmd, memoryOffset, 2, bufferOffset, buffer, size);

        //        if (errorCode != ErrorCodes.NoErrors)
        //            return errorCode;

        //        memoryOffset += size;
        //        bufferOffset += size;
        //    }

        //    // write the partial block
        //    errorCode = m_driverInterface.WriteDeviceMemory2(m_memWriteCmd, memoryOffset, 2, bufferOffset, buffer, (byte)partialBlock);

        //    if (errorCode != ErrorCodes.NoErrors)
        //        return errorCode;

        //    // re-lock the memory
        //    if (m_memLockAddr != 0x00)
        //        m_driverInterface.LockDeviceMemory(m_memLockAddr, m_memLockCode);

        //    return errorCode;
        //}

        ////===============================================================================================================
        ///// <summary>
        ///// Reads the device's capabilities from the device's eeprom
        ///// </summary>
        ////===============================================================================================================
        //protected override byte[] ReadDeviceCaps()
        //{
        //    List<byte> bList = new List<byte>();
        //    byte[] buffer;
        //    ushort offset = m_devCapsOffset;

        //    // read the first two bytes which is the number of bytes to read
        //    m_driverInterface.ReadDeviceMemory2(m_memReadCmd, offset, m_memOffsetLength, 2, out buffer);

        //    // set the byte count
        //    int byteCount = (int)buffer[0] + (int)(buffer[1] << 8);

        //    if (byteCount != 0xFFFF && byteCount == m_defaultDevCapsImage.Length)
        //    {
        //        // calculate the number of complete blocks to read (e.g. 64 bytes)
        //        int blockCount = byteCount / Constants.MAX_COMMAND_LENGTH;

        //        // calculate the remaining bytes to read
        //        int remainingBytes = byteCount - (blockCount * Constants.MAX_COMMAND_LENGTH);

        //        offset += 2;

        //        // read complete blocks
        //        for (int i = 0; i < blockCount; i++)
        //        {
        //            m_driverInterface.ReadDeviceMemory2(m_memReadCmd, offset, m_memOffsetLength, Constants.MAX_COMMAND_LENGTH, out buffer);
        //            bList.AddRange(buffer);
        //            offset += Constants.MAX_COMMAND_LENGTH;
        //        }

        //        // read the remaining bytes
        //        if (remainingBytes > 0)
        //            m_driverInterface.ReadDeviceMemory2(m_memReadCmd, offset, m_memOffsetLength, (byte)remainingBytes, out buffer);

        //        for (int i = 0; i < remainingBytes; i++)
        //            bList.Add(buffer[i]);

        //        return bList.ToArray();
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}
    }
}
