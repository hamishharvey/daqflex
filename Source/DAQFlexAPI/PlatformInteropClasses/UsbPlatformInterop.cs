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
using System.Threading;
namespace MeasurementComputing.DAQFlex
{
    internal abstract class UsbPlatformInterop : PlatformInterop
    {
        protected const byte MEM_ADDR = 0x31;
        protected const byte MEM_READ = 0x30;
        protected const byte MEM_WRITE = 0x30;

        protected WindowsUsbSetupPacket m_statusPacket;
        protected Queue<BulkInBuffer> m_bulkInReadyBuffers = new Queue<BulkInBuffer>();
        protected Queue<BulkInBuffer> m_bulkInCompletedBuffers = new Queue<BulkInBuffer>();
        protected object m_bulkInReadyBuffersLock = new Object();
        protected object m_bulkInCompletedBuffersLock = new Object();
        protected List<UsbBulkInRequest> m_bulkInRequests = new List<UsbBulkInRequest>();
        protected object m_bulkInRequestLock = new object();
        protected object m_bulkOutRequestLock = new object();
        protected Queue<byte[]> m_completedBulkInRequestBuffers = new Queue<byte[]>();
        protected byte[] m_devIdMessage = new byte[Constants.MAX_MESSAGE_LENGTH];
        protected byte[] m_sernoMessage = new byte[Constants.MAX_MESSAGE_LENGTH];
        protected UsbSetupPacket m_aiScanResetPacket = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
        protected UsbSetupPacket m_aoScanResetPacket = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
        protected List<UsbBulkOutRequest> m_bulkOutRequests = new List<UsbBulkOutRequest>();

        protected int m_numberOfWorkingInputRequests = 0;
        protected int m_numberOfQueuedInputRequests = 0;
        protected int m_totalNumberOfInputRequests = 0;
        protected int m_numberOfInputRequestsSubmitted = 0;
        protected int m_numberOfInputRequestsCompleted = 0;
        protected int m_totalNumberOfInputBytesRequested = 0;
        protected int m_totalNumberOfInputBytesTransferred = 0;

        protected int m_numberOfWorkingOutputRequests = 0;
        protected int m_numberOfQueuedOutputRequests = 0;
        protected int m_totalNumberOfOutputRequests = 0;
        protected int m_numberOfOutputRequestsSubmitted = 0;
        protected int m_numberOfOutputRequestsCompleted = 0;
        protected int m_totalNumberOfOutputBytesRequested = 0;
        protected int m_totalNumberOfOutputBytesTransferred = 0;
        protected int m_driverInterfaceOutputBufferIndex;
	
        internal UsbPlatformInterop()
            : base()
        {
            // build device ID and serno messages
            m_devIdMessage[0] = (byte)'?';
            m_devIdMessage[1] = (byte)'D';
            m_devIdMessage[2] = (byte)'E';
            m_devIdMessage[3] = (byte)'V';
            m_devIdMessage[4] = (byte)':';
            m_devIdMessage[5] = (byte)'I';
            m_devIdMessage[6] = (byte)'D';

            m_sernoMessage[0] = (byte)'?';
            m_sernoMessage[1] = (byte)'D';
            m_sernoMessage[2] = (byte)'E';
            m_sernoMessage[3] = (byte)'V';
            m_sernoMessage[4] = (byte)':';
            m_sernoMessage[5] = (byte)'M';
            m_sernoMessage[6] = (byte)'F';
            m_sernoMessage[7] = (byte)'G';
            m_sernoMessage[8] = (byte)'S';
            m_sernoMessage[9] = (byte)'E';
            m_sernoMessage[10] = (byte)'R';
        }

        internal UsbPlatformInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
            : base(deviceInfo, criticalParams)
        {
            // build device ID and serno messages
            m_devIdMessage[0] = (byte)'?';
            m_devIdMessage[1] = (byte)'D';
            m_devIdMessage[2] = (byte)'E';
            m_devIdMessage[3] = (byte)'V';
            m_devIdMessage[4] = (byte)':';
            m_devIdMessage[5] = (byte)'I';
            m_devIdMessage[6] = (byte)'D';

            m_sernoMessage[0] = (byte)'?';
            m_sernoMessage[1] = (byte)'D';
            m_sernoMessage[2] = (byte)'E';
            m_sernoMessage[3] = (byte)'V';
            m_sernoMessage[4] = (byte)':';
            m_sernoMessage[5] = (byte)'M';
            m_sernoMessage[6] = (byte)'F';
            m_sernoMessage[7] = (byte)'G';
            m_sernoMessage[8] = (byte)'S';
            m_sernoMessage[9] = (byte)'E';
            m_sernoMessage[10] = (byte)'R';

            // build Ai scan reset message
            byte[] aiScanResetCmd = new byte[Constants.MAX_MESSAGE_LENGTH];

            aiScanResetCmd[0] = 65;
            aiScanResetCmd[1] = 73;
            aiScanResetCmd[2] = 83;
            aiScanResetCmd[3] = 67;
            aiScanResetCmd[4] = 65;
            aiScanResetCmd[5] = 78;
            aiScanResetCmd[6] = 58;
            aiScanResetCmd[7] = 82;
            aiScanResetCmd[8] = 69;
            aiScanResetCmd[9] = 83;
            aiScanResetCmd[10] = 69;
            aiScanResetCmd[11] = 84;

            m_aiScanResetPacket.TransferType = UsbTransferTypes.ControlOut;
            m_aiScanResetPacket.Request = 0x80;
            m_aiScanResetPacket.DeferTransfer = false;
            m_aiScanResetPacket.BytesTransfered = 0;

            for (int i = 0; i < aiScanResetCmd.Length; i++)
                m_aiScanResetPacket.Buffer[i] = aiScanResetCmd[i];

            // build Ai scan reset message
            byte[] aoScanResetCmd = new byte[Constants.MAX_MESSAGE_LENGTH];

            aoScanResetCmd[0] = (byte)'A';
            aoScanResetCmd[1] = (byte)'O';
            aoScanResetCmd[2] = (byte)'S';
            aoScanResetCmd[3] = (byte)'C';
            aoScanResetCmd[4] = (byte)'A';
            aoScanResetCmd[5] = (byte)'N';
            aoScanResetCmd[6] = (byte)':';
            aoScanResetCmd[7] = (byte)'R';
            aoScanResetCmd[8] = (byte)'E';
            aoScanResetCmd[9] = (byte)'S';
            aoScanResetCmd[10] = (byte)'E';
            aoScanResetCmd[11] = (byte)'T';

            m_aoScanResetPacket.TransferType = UsbTransferTypes.ControlOut;
            m_aoScanResetPacket.Request = 0x80;
            m_aoScanResetPacket.DeferTransfer = false;
            m_aoScanResetPacket.BytesTransfered = 0;

            for (int i = 0; i < aoScanResetCmd.Length; i++)
                m_aoScanResetPacket.Buffer[i] = aoScanResetCmd[i];

        }

        //===================================================================================================
		/// <value>
		/// A value indicating the device's output FIFO is primed
		/// for doing an output scan
		/// </value>
        //===================================================================================================
		internal bool ReadyToStartOutputScan
		{
			get {return m_readyToStartOutputScan;}
		}
		
        //===================================================================================================
		/// <value>
		/// A value indicating that the device has started an output
		/// scan and is ready to receive more data 
		/// </value>
        //===================================================================================================
		internal bool ReadyToSubmitRemainingOutputTransfers
		{
			set {m_readyToSubmitRemainingOutputTransfers = value;}
		}
		
        //===================================================================================================
        /// <summary>
        /// Virtual method for getting a list of DeviceInfos
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //===================================================================================================
        internal override ErrorCodes GetDevices(Dictionary<int, DeviceInfo> deviceInfoList, bool refresh)
        {
            return GetUsbDevices(deviceInfoList, refresh);
        }

        //===================================================================================================
        /// <summary>
        /// Virtual method for getting a list of DeviceInfos
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //===================================================================================================
        internal abstract ErrorCodes GetUsbDevices(Dictionary<int, DeviceInfo> deviceInfoList, bool refresh);

        //===================================================================================================
        /// <summary>
        /// Overrides abstact method in base class
        /// </summary>
        /// <param name="deviceInfo">A deviceInfo object</param>
        /// <returns>An empty string</returns>
        //===================================================================================================
        internal override string GetDeviceID(DeviceInfo deviceInfo)
        {
            System.Diagnostics.Debug.Assert(false, "GetDeviceID not implemented in UsbPlatformInterop");
            return String.Empty;
        }

        //===================================================================================================
        /// <summary>
        /// Overrides abstact method in base class
        /// </summary>
        /// <param name="deviceInfo">A deviceInfo object</param>
        /// <returns>An empty string</returns>
        //===================================================================================================
        internal override string GetSerno(DeviceInfo deviceInfo)
        {
            System.Diagnostics.Debug.Assert(false, "GetSerno not implemented in UsbPlatformInterop");
            return String.Empty;
        }

        //==================================================================
        /// <summary>
        /// Virtual method for a USB control IN request
        /// </summary>
        /// <returns>The result</returns>
        //==================================================================
        internal abstract ErrorCodes UsbControlInRequest(UsbSetupPacket packet);

        //==================================================================
        /// <summary>
        /// Virtual method for a USB control OUT request
        /// </summary>
        /// <returns>The result</returns>
        //==================================================================
        internal abstract ErrorCodes UsbControlOutRequest(UsbSetupPacket packet);

        //==============================================================================================
        /// <summary>
        /// Virtual method for a USB Bulk IN request
        /// </summary>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <param name="bytesReceived">The number of actual bytes received</param>
        /// <returns>The result</returns>
        //==============================================================================================
        internal abstract ErrorCodes UsbBulkInRequest(ref BulkInBuffer buffer, ref uint bytesReceived);

        //===================================================================================
        /// <summary>
        /// Virtual method for a USB Bulk OUT request
        /// </summary>
        /// <param name="buffer">The buffer containing the data to send</param>
        /// <param name="count">The number of samples to send</param>
        /// <returns>The result</returns>
        //===================================================================================
        internal abstract int UsbBulkOutRequest(UsbBulkOutRequest br, ref int bytesTransferred);

        //===================================================================================
        /// <summary>
        /// A mutex to synchronize access to the control transfer methods
        /// </summary>
        //===================================================================================
        internal Mutex ControlTransferMutex
        {
            get { return m_controlTransferMutex; }
        }

        //===================================================================================
        /// <summary>
        /// Indicates if there is scan data available.
        /// This is used for finite scans to continue reading
        /// </summary>
        /// <returns>true if scan data is available otherwise false</returns>
        //===================================================================================
        internal virtual bool IsInputScanDataAvailable()
        {
            if (m_completedBulkInRequestBuffers.Count > 0)
                return true;

            return false;
        }

        //==============================================================================================
        /// <summary>
        /// Synchronizes access to the bulk in ready buffers queue
        /// </summary>
        /// <param name="bulkInBuffer">The buffer to queue</param>
        /// <param name="queueAction">The actions to take (enqueue or dequeue)</param>
        /// <returns>The buffer that was dequeued</returns>
        //==============================================================================================
        internal BulkInBuffer QueueBulkInReadyBuffers(BulkInBuffer bulkInBuffer, QueueAction queueAction)
        {
            lock (m_bulkInReadyBuffersLock)
            {
                if (queueAction == QueueAction.Enqueue)
                {
                    m_bulkInReadyBuffers.Enqueue(bulkInBuffer);
                    return null;
                }
                else
                {
                    if (m_bulkInReadyBuffers.Count > 0)
                        return m_bulkInReadyBuffers.Dequeue();
                    else
                        return null;
                }
            }
        }

        //==============================================================================================
        /// <summary>
        /// Synchronizes access to the bulk in completed buffers queue
        /// </summary>
        /// <param name="bulkInBuffer">The buffer to queue</param>
        /// <param name="queueAction">The actions to take (enqueue or dequeue)</param>
        /// <returns>The buffer that was dequeued</returns>
        //==============================================================================================
        internal BulkInBuffer QueueBulkInCompletedBuffers(BulkInBuffer bulkInBuffer, QueueAction queueAction)
        {
            lock (m_bulkInCompletedBuffersLock)
            {
                if (queueAction == QueueAction.Enqueue)
                {
                    m_bulkInCompletedBuffers.Enqueue(bulkInBuffer);
                    return null;
                }
                else
                {
                    if (m_bulkInCompletedBuffers.Count > 0)
                        return m_bulkInCompletedBuffers.Dequeue();
                    else
                        return null;
                }
            }
        }

        //==============================================================================================
        /// <summary>
        /// Sets up the BulkInReady and BulkInCompleted queues
        /// </summary>
        /// <param name="transferSize">The size of the BulkIn transfers in bytes</param>
        //==============================================================================================
        internal void PrepareBulkInQueues(int transferSize)
        {
            int numberOfBulkInCopyBuffers = 20;

            // empty the bulkInReadyBuffers queue
            while (m_bulkInReadyBuffers.Count > 0)
            {
                BulkInBuffer buffer = m_bulkInReadyBuffers.Dequeue();
                new WeakReference(buffer, false);
                buffer = null;
            }

            // empty the bulkInCompletedBuffers queue
            while (m_bulkInCompletedBuffers.Count > 0)
            {
                BulkInBuffer buffer = m_bulkInCompletedBuffers.Dequeue();
                new WeakReference(buffer, false);
                buffer = null;
            }

            GC.Collect();

            for (int i = 0; i < numberOfBulkInCopyBuffers; i++)
            {
                QueueBulkInReadyBuffers(new BulkInBuffer(transferSize), QueueAction.Enqueue);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Virtual method for submitting bulk out requests
        /// </summary>
        /// <param name="request">The request object</param>
        /// <returns>True on success otherwise false</returns>
        //===================================================================================================
        internal virtual bool SubmitBulkOutRequest(UsbBulkOutRequest request)
        {
            return false;
        }

        //===================================================================================================
        /// <summary>
        /// Reads a device's memory
        /// </summary>
        /// <param name="offset">The starting addresss</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer containing the memory contents</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal override ErrorCodes ReadDeviceMemory(ushort memoryOffset, byte count, out byte[] buffer)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            UsbSetupPacket packet = new UsbSetupPacket(count);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = MEM_ADDR;
            packet.Value = 0;
            packet.Index = 0;
            packet.Length = 2;

            // store the memory offset in the first two bytes of the buffer
            packet.Buffer[0] = (byte)(0x00FF & memoryOffset);
            packet.Buffer[1] = (byte)((0xFF00 & memoryOffset) >> 8);

            buffer = null;

            if (count > Constants.MAX_COMMAND_LENGTH)
                return ErrorCodes.CountGreaterThanMaxLength;

            // send the mem address command
            errorCode = UsbControlOutRequest(packet);

            if (errorCode == ErrorCodes.NoErrors)
            {
                packet.TransferType = UsbTransferTypes.ControlIn;
                packet.Request = MEM_READ;
                packet.Length = count;

                // read a block of memory (up to max packet size)
                errorCode = UsbControlInRequest(packet);

                buffer = packet.Buffer;
            }

            if (errorCode != ErrorCodes.NoErrors)
                errorCode = ErrorCodes.ErrorReadingDeviceMemory;

            return errorCode;
        }

        //===================================================================================================
        /// <summary>
        /// Virutal method to unlock a device's memory for writing to it
        /// </summary>
        /// <param name="address">The address of the unlock code</param>
        /// <param name="unlockCode">The unlock code</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal override ErrorCodes UnlockDeviceMemory(ushort address, ushort unlockCode)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // send memory unlock code
            UsbSetupPacket packet = new UsbSetupPacket(64);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = MEM_ADDR;
            packet.Value = 0;
            packet.Index = 0;
            packet.Length = 2;
            packet.Buffer[0] = (byte)(0x00FF & address);
            packet.Buffer[1] = (byte)((0xFF00 & address) >> 8);

            // MemAddress command
            errorCode = UsbControlOutRequest(packet);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            packet.Request = MEM_WRITE;
            packet.Value = 0;
            packet.Index = 0;
            packet.Length = 2;
            packet.Buffer[0] = (byte)(0x00FF & unlockCode);
            packet.Buffer[1] = (byte)((0xFF00 & unlockCode) >> 8);

            // MemWrite command
            errorCode = UsbControlOutRequest(packet);

            return errorCode;
        }

        //===================================================================================================
        /// <summary>
        /// Overriden to lock a device's to prevent writing to it
        /// </summary>
        /// <param name="address">The address of the lock code</param>
        /// <param name="lockCode">The lock code</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal override ErrorCodes LockDeviceMemory(ushort address, ushort lockCode)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // send memory lock code
            UsbSetupPacket packet = new UsbSetupPacket(64);
            packet.Request = MEM_ADDR;
            packet.Value = 0;
            packet.Index = 0;
            packet.Length = 2;
            packet.Buffer[0] = (byte)(0x00FF & address);
            packet.Buffer[1] = (byte)((0xFF00 & address) >> 8);

            // MemAddress command
            errorCode = UsbControlOutRequest(packet);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            packet.Request = MEM_WRITE;
            packet.Value = 0;
            packet.Index = 0;
            packet.Length = 2;
            packet.Buffer[0] = (byte)(0x00FF & lockCode);
            packet.Buffer[1] = (byte)((0xFF00 & lockCode) >> 8);

            // MemWrite command
            errorCode = UsbControlOutRequest(packet);

            return errorCode;
        }

        //===================================================================================================
        /// <summary>
        /// Overriden to Write data to a device's memory
        /// </summary>
        /// <param name="memoryOffset">The starting addresss of the device's memory</param>
        /// <param name="bufferOffset">The starting addresss of the data buffer</param>
        /// <param name="buffer">The data buffer</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal override ErrorCodes WriteDeviceMemory(ushort memoryOffset, ushort bufferOffset, byte[] buffer, byte count)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            UsbSetupPacket packet = new UsbSetupPacket(64);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = MEM_ADDR;
            packet.Value = 0;
            packet.Index = 0;
            packet.Length = 2;
            packet.Buffer[0] = (byte)(0x00FF & memoryOffset);
            packet.Buffer[1] = (byte)((0xFF00 & memoryOffset) >> 8);

            if (count > Constants.MAX_COMMAND_LENGTH)
                return ErrorCodes.CountGreaterThanMaxLength;

            // MemAddress command
            errorCode = UsbControlOutRequest(packet);

            // Check current MemAddress
            packet.TransferType = UsbTransferTypes.ControlIn;
            errorCode = UsbControlInRequest(packet);

            if (errorCode == ErrorCodes.NoErrors)
            {
                packet.TransferType = UsbTransferTypes.ControlOut;
                packet.Request = MEM_WRITE;
                packet.Value = 0x0;

                for (int i = 0; i < count; i++)
                    packet.Buffer[i] = buffer[i + bufferOffset];

                packet.Length = count;

                // MemWrite command
                errorCode = UsbControlOutRequest(packet);
            }

            if (errorCode != ErrorCodes.NoErrors)
                errorCode = ErrorCodes.ErrorWritingDeviceMemory;

            // MemWrite command
            return errorCode;
        }
    }

    //===========================================================================
    /// <summary>
    /// Class used to store data bulk in transfer data
    /// Length is the actual number of bytes received, not the length of Data
    /// </summary>
    //===========================================================================
    internal class BulkInBuffer
    {
        internal int Length;
        internal byte[] Data;

        internal BulkInBuffer(int numBytes)
        {
            Length = 0;
            Data = new byte[numBytes];
        }
    }
}
