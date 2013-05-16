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
    class EepromAssistant
    {
        protected DriverInterface m_driverInterface;

        //=============================================================================================================================================================
        /// <summary>
        /// Class to handle reading/writing to eeprom 
        /// </summary>
        /// <param name="driverInterface">A reference to the driver interface object</param>
        //=============================================================================================================================================================
        internal EepromAssistant(DriverInterface driverInterface)
        {
            m_driverInterface = driverInterface;
        }

        //=============================================================================================================================================================
        /// <summary>
        /// Read's device's memory
        /// </summary>
        /// <param name="memAddrCmd">The device's memory address command (Request)</param>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal virtual ErrorCodes ReadDeviceMemory(byte memAddrCmd, byte memReadCmd, ushort memOffset, ushort memOffsetLength, byte count, out byte[] buffer)
        {
            return m_driverInterface.ReadDeviceMemory1(memAddrCmd, memReadCmd, memOffset, memOffsetLength, count, out buffer);
        }

        //===================================================================================================
        /// <summary>
        /// Unlocks a device's memory for writing to it
        /// </summary>
        /// <param name="address">The address where the unlock code should be written to</param>
        /// <param name="unlockCode">The unlock code</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal ErrorCodes UnlockDeviceMemory(ushort address, ushort unlockCode)
        {
            return m_driverInterface.UnlockDeviceMemory(address, unlockCode);
        }

        //===================================================================================================
        /// <summary>
        /// Locks a device's memory to prevent writing to it
        /// </summary>
        /// <param name="address">The address where the lock code should be written to</param>
        /// <param name="unlockCode">The unlock code</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal ErrorCodes LockDeviceMemory(ushort address, ushort lockCode)
        {
            return m_driverInterface.LockDeviceMemory(address, lockCode);
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="memAddrCmd">The device's memory address command</param>
        /// <param name="memWriteCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal virtual ErrorCodes WriteDeviceMemory(ushort memAddrCmd, byte memWriteCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            return m_driverInterface.WriteDeviceMemory1((byte)memAddrCmd, memWriteCmd, memoryOffset, memOffsetLength, bufferOffset, buffer, count);
        }
    }
}
