
using System;
using System.Collections.Generic;
using System.Text;

namespace MeasurementComputing.DAQFlex
{
    internal class EepromAssistantIV : EepromAssistant
    {
        internal EepromAssistantIV(DriverInterface driverInterface)
            : base(driverInterface)
        {
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
        internal override ErrorCodes ReadDeviceMemory(byte memAddrCmd, byte memReadCmd, ushort memOffset, ushort memOffsetLength, byte count, out byte[] buffer)
        {
            return m_driverInterface.ReadDeviceMemory4(memReadCmd, memOffset, memOffsetLength, count, out buffer);
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="unlockKey">The device's unlock key</param>
        /// <param name="memWriteCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal override ErrorCodes WriteDeviceMemory(ushort unlockKey, byte memCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            return m_driverInterface.WriteDeviceMemory4(unlockKey, memCmd, memoryOffset, memOffsetLength, bufferOffset, buffer, count);
        }
    }
}
