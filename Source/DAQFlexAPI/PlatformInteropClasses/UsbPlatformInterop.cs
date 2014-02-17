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
        protected const byte MEM_OFFSET_LENGTH = 2;

        protected WindowsUsbSetupPacket m_statusPacket;
        protected Queue<BulkInBuffer> m_bulkInReadyBuffers = new Queue<BulkInBuffer>();
        protected Queue<BulkInBuffer> m_bulkInCompletedBuffers = new Queue<BulkInBuffer>();
        protected BulkInBuffer[] m_holdingBuffer;
        protected List<BulkInBuffer> m_temporaryBuffer;
        protected object m_bulkInReadyBuffersLock = new Object();
        protected object m_bulkInCompletedBuffersLock = new Object();
        protected List<UsbBulkInRequest> m_bulkInRequests = new List<UsbBulkInRequest>();
        protected List<UsbBulkOutRequest> m_bulkOutRequests = new List<UsbBulkOutRequest>();
        protected object m_bulkInRequestLock = new object();
        protected object m_bulkOutRequestLock = new object();
        protected Queue<byte[]> m_completedBulkInRequestBuffers = new Queue<byte[]>();

        // MBD messages
        protected byte[] m_devIdMessage = new byte[Constants.MAX_MESSAGE_LENGTH];
        protected byte[] m_sernoMessage = new byte[Constants.MAX_MESSAGE_LENGTH];
        protected byte[] m_aiScanStatusMessage = new byte[Constants.MAX_MESSAGE_LENGTH];
        protected byte[] m_aoScanStatusMessage = new byte[Constants.MAX_MESSAGE_LENGTH];

        protected UsbSetupPacket m_aiScanResetPacket = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
        protected UsbSetupPacket m_aoScanResetPacket = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
        
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
        protected int m_expectedInputTransferIndex = 0;
	
        internal UsbPlatformInterop()
            : base()
        {
            string msg;

            // create a device ID message
            msg = Messages.DEV_ID_QUERY;

            for (int i = 0; i < msg.Length; i++)
                m_devIdMessage[i] = (byte)msg[i];

            // create a mfg serno message
            msg = Messages.DEV_SERNO_QUERY;

            for (int i = 0; i < msg.Length; i++)
                m_sernoMessage[i] = (byte)msg[i];
        }

        internal UsbPlatformInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
            : base(deviceInfo, criticalParams)
        {
            string msg;

            // create a device ID message
            msg = Messages.DEV_ID_QUERY;

            for (int i = 0; i < msg.Length; i++)
                m_devIdMessage[i] = (byte)msg[i];

            // create a mfg serno message
            msg = Messages.DEV_SERNO_QUERY;

            for (int i = 0; i < msg.Length; i++)
                m_sernoMessage[i] = (byte)msg[i];

            // create an aiscan status message
            msg = Messages.AISCAN_STATUS_QUERY;

            for (int i = 0; i < msg.Length; i++)
                m_aiScanStatusMessage[i] = (byte)msg[i];

            // create an aoscan status message
            msg = Messages.AOSCAN_STATUS_QUERY;

            for (int i = 0; i < msg.Length; i++)
                m_aoScanStatusMessage[i] = (byte)msg[i];

            // build an ai scan reset packet
            byte[] aiScanResetCmd = new byte[Constants.MAX_MESSAGE_LENGTH];

            msg = Messages.AISCAN_RESET;

            for (int i = 0; i < msg.Length; i++)
                aiScanResetCmd[i] = (byte)msg[i];

            m_aiScanResetPacket.TransferType = UsbTransferTypes.ControlOut;
            m_aiScanResetPacket.Request = 0x80;
            m_aiScanResetPacket.DeferTransfer = false;
            m_aiScanResetPacket.BytesTransfered = 0;

            for (int i = 0; i < aiScanResetCmd.Length; i++)
                m_aiScanResetPacket.Buffer[i] = aiScanResetCmd[i];

            // build an ao scan reset packet
            byte[] aoScanResetCmd = new byte[Constants.MAX_MESSAGE_LENGTH];

            msg = Messages.AOSCAN_RESET;

            for (int i = 0; i < msg.Length; i++)
                aoScanResetCmd[i] = (byte)msg[i];

            m_aoScanResetPacket.TransferType = UsbTransferTypes.ControlOut;
            m_aoScanResetPacket.Request = 0x80;
            m_aoScanResetPacket.DeferTransfer = false;
            m_aoScanResetPacket.BytesTransfered = 0;

            for (int i = 0; i < aoScanResetCmd.Length; i++)
                m_aoScanResetPacket.Buffer[i] = aoScanResetCmd[i];

        }

        //===================================================================================================
        /// <summary>
        /// Indicates if all input requests that were submitted have completed
        /// </summary>
        /// <returns>true if the number of requests completed equals the number
        /// of requests submitted, otherwise false</returns>
        //===================================================================================================
        internal virtual bool InputScanComplete()
        {
            if (m_numberOfInputRequestsCompleted == m_numberOfInputRequestsSubmitted)
                return true;
            else
                return false;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates if all output requests that were submitted have completed
        /// </summary>
        /// <returns>true if the number of requests completed equals the number
        /// of requests submitted, otherwise false</returns>
        //===================================================================================================
        internal bool OutputScanComplete()
        {
            if (m_numberOfOutputRequestsCompleted == m_numberOfOutputRequestsSubmitted)
                return true;
            else
                return false;
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
		
        //==================================================================================================================
        /// <summary>
        /// Virtual method for getting a list of DeviceInfos
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //==================================================================================================================
        internal override ErrorCodes GetDevices(Dictionary<int, DeviceInfo> deviceInfoList, DeviceListUsage deviceListUsage)
        {
            return GetUsbDevices(deviceInfoList, deviceListUsage);
        }

        //==================================================================================================================
        /// <summary>
        /// Virtual method for getting a list of DeviceInfos
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //==================================================================================================================
        internal abstract ErrorCodes GetUsbDevices(Dictionary<int, DeviceInfo> deviceInfoList, DeviceListUsage deviceListUsage);

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

        //==================================================================
        /// <summary>
        /// Check's the device status for a data overrun
        /// </summary>
        /// <returns>The error code</returns>
        //==================================================================
        internal override ErrorCodes CheckOverrun()
        {
            ErrorCodes errorCode = m_errorCode;

            ControlTransferMutex.WaitOne();

            UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = ControlRequest.MESSAGE_REQUEST;
            packet.Index = 0;
            packet.Value = 0;
            packet.Length = (ushort)m_aiScanStatusMessage.Length;
            Array.Copy(m_aiScanStatusMessage, packet.Buffer, m_aiScanStatusMessage.Length);

            // send the status message
            UsbControlOutRequest(packet);

            // get the status response
            packet.TransferType = UsbTransferTypes.ControlIn;

            UsbControlInRequest(packet);

            ControlTransferMutex.ReleaseMutex();

            string response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);

            if (response.Contains(PropertyValues.OVERRUN))
                errorCode = ErrorCodes.DataOverrun;

            return errorCode;
        }

        //==================================================================
        /// <summary>
        /// Check's the device status for a data overrun
        /// </summary>
        /// <returns>The error code</returns>
        //==================================================================
        internal override ErrorCodes CheckUnderrun()
        {
            ErrorCodes errorCode = m_errorCode;

            ControlTransferMutex.WaitOne();

            UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = ControlRequest.MESSAGE_REQUEST;
            packet.Index = 0;
            packet.Value = 0;
            packet.Length = (ushort)m_aoScanStatusMessage.Length;
            Array.Copy(m_aoScanStatusMessage, packet.Buffer, m_aoScanStatusMessage.Length);

            // send the status message
            UsbControlOutRequest(packet);

            // get the status response
            packet.TransferType = UsbTransferTypes.ControlIn;

            UsbControlInRequest(packet);

            ControlTransferMutex.ReleaseMutex();

            string response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);

            if (response.Contains(PropertyValues.UNDERRUN))
                errorCode = ErrorCodes.DataUnderrun;

            return errorCode;
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
                    {
                        return m_bulkInReadyBuffers.Dequeue();
                    }
                    else
                    {
                        return null;
                    }
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
                try
                {
                    if (queueAction == QueueAction.Enqueue)
                    {
                        m_bulkInCompletedBuffers.Enqueue(bulkInBuffer);
                        return null;
                    }
                    else
                    {
                        if (m_bulkInCompletedBuffers.Count > 0)
                        {
                            return m_bulkInCompletedBuffers.Dequeue();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Assert(false, ex.Message);
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

            //GC.Collect();

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

        //============================================================================================================================================================
        /// <summary>
        /// Reads a device's memory
        /// </summary>
        /// <param name="offset">The starting addresss</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer containing the memory contents</param>
        /// <returns>The error code</returns>
        //============================================================================================================================================================
        internal override ErrorCodes ReadDeviceMemory1(byte memAddrCmd, byte memReadCmd, ushort memoryOffset, ushort memoryOffsetLength, byte count, out byte[] buffer)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (count > Constants.MAX_COMMAND_LENGTH)
                errorCode = ErrorCodes.CountGreaterThanMaxLength;

            UsbSetupPacket packet;

            buffer = null;

            if (errorCode == ErrorCodes.NoErrors)
            {
                // create a packet to send the memory address command
                packet = new UsbSetupPacket(memoryOffsetLength);
                packet.TransferType = UsbTransferTypes.ControlOut;
                packet.Request = memAddrCmd;
                packet.Value = 0;
                packet.Index = 0;
                packet.Length = 2;

                // store the memory offset in the first two bytes of the buffer
                packet.Buffer[0] = (byte)(0x00FF & memoryOffset);
                packet.Buffer[1] = (byte)((0xFF00 & memoryOffset) >> 8);

                buffer = null;

                // send the mem address command
                errorCode = UsbControlOutRequest(packet);

                if (errorCode == ErrorCodes.NoErrors)
                {
                    // create a new packet for reading a block of device memory
                    packet = new UsbSetupPacket(count);
                    packet.TransferType = UsbTransferTypes.ControlIn;
                    packet.Request = memReadCmd;
                    packet.Length = count;

                    // read a block of memory (up to max packet size)
                    errorCode = UsbControlInRequest(packet);

                    buffer = packet.Buffer;
                }

                if (errorCode != ErrorCodes.NoErrors)
                    errorCode = ErrorCodes.ErrorReadingDeviceMemory;
            }

            return errorCode;
        }

        //============================================================================================================================================================
        /// <summary>
        /// Reads a device's memory
        /// </summary>
        /// <param name="offset">The starting addresss</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer containing the memory contents</param>
        /// <returns>The error code</returns>
        //============================================================================================================================================================
        internal override ErrorCodes ReadDeviceMemory2(byte memReadCmd, ushort memoryOffset, ushort memoryOffsetLength, byte count, out byte[] buffer)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (count > Constants.MAX_COMMAND_LENGTH)
                errorCode = ErrorCodes.CountGreaterThanMaxLength;

            UsbSetupPacket packet;

            buffer = null;

            if (errorCode == ErrorCodes.NoErrors)
            {
                if (errorCode == ErrorCodes.NoErrors)
                {
                    // create a new packet for reading a block of device memory
                    packet = new UsbSetupPacket(count);
                    packet.TransferType = UsbTransferTypes.ControlIn;
                    packet.Request = memReadCmd;
                    packet.Value = memoryOffset;
                    packet.Length = count;

                    // read a block of memory (up to max packet size)
                    errorCode = UsbControlInRequest(packet);

                    buffer = packet.Buffer;
                }

                if (errorCode != ErrorCodes.NoErrors)
                    errorCode = ErrorCodes.ErrorReadingDeviceMemory;
            }

            return errorCode;
        }

        //============================================================================================================================================================
        /// <summary>
        /// Reads a device's memory
        /// </summary>
        /// <param name="offset">The starting addresss</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer containing the memory contents</param>
        /// <returns>The error code</returns>
        //============================================================================================================================================================
        internal override ErrorCodes ReadDeviceMemory3(byte memReadCmd, ushort memoryOffset, ushort memoryOffsetLength, byte count, out byte[] buffer)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (count > Constants.MAX_COMMAND_LENGTH)
                errorCode = ErrorCodes.CountGreaterThanMaxLength;

            UsbSetupPacket packet;

            buffer = null;

            if (errorCode == ErrorCodes.NoErrors)
            {
                if (errorCode == ErrorCodes.NoErrors)
                {
                    // create a new packet for reading a block of device memory
                    packet = new UsbSetupPacket(count);
                    packet.TransferType = UsbTransferTypes.ControlIn;
                    packet.Request = memReadCmd;
                    packet.Index = memoryOffset;
                    packet.Length = count;

                    // read a block of memory (up to max packet size)
                    errorCode = UsbControlInRequest(packet);

                    buffer = packet.Buffer;
                }

                if (errorCode != ErrorCodes.NoErrors)
                    errorCode = ErrorCodes.ErrorReadingDeviceMemory;
            }

            return errorCode;
        }

        //============================================================================================================================================================
        /// <summary>
        /// Reads a device's memory
        /// </summary>
        /// <param name="offset">The starting addresss</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer containing the memory contents</param>
        /// <returns>The error code</returns>
        //============================================================================================================================================================
        internal override ErrorCodes ReadDeviceMemory4(byte memReadCmd, ushort memoryOffset, ushort memoryOffsetLength, byte count, out byte[] buffer)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (count > Constants.MAX_COMMAND_LENGTH)
                errorCode = ErrorCodes.CountGreaterThanMaxLength;

            UsbSetupPacket packet;

            buffer = null;

            if (errorCode == ErrorCodes.NoErrors)
            {
                if (errorCode == ErrorCodes.NoErrors)
                {
                    // create a new packet for reading a block of device memory
                    packet = new UsbSetupPacket(count);
                    packet.TransferType = UsbTransferTypes.ControlIn;
                    packet.Request = memReadCmd;
                    packet.Value = memoryOffset;
                    packet.Index = 0;
                    packet.Length = count;

                    // read a block of memory (up to max packet size)
                    errorCode = UsbControlInRequest(packet);

                    buffer = packet.Buffer;
                }

                if (errorCode != ErrorCodes.NoErrors)
                    errorCode = ErrorCodes.ErrorReadingDeviceMemory;
            }

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

        //==============================================================================================================================================================================
        /// <summary>
        /// Overriden to Write data to a device's memory
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
        internal override ErrorCodes WriteDeviceMemory1(byte memAddrCmd, byte memWriteCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            m_controlTransferMutex.WaitOne();

            ErrorCodes errorCode = ErrorCodes.NoErrors;

            UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_COMMAND_LENGTH);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = memAddrCmd;
            packet.Value = 0;
            packet.Index = 0;
            packet.Length = 2;
            packet.Buffer[0] = (byte)(0x00FF & memoryOffset);
            packet.Buffer[1] = (byte)((0xFF00 & memoryOffset) >> 8);

            if (count > Constants.MAX_COMMAND_LENGTH)
            {
                m_controlTransferMutex.ReleaseMutex();
                return ErrorCodes.CountGreaterThanMaxLength;
            }

            // MemAddress command
            errorCode = UsbControlOutRequest(packet);

            // Check current MemAddress
            packet.TransferType = UsbTransferTypes.ControlIn;
            errorCode = UsbControlInRequest(packet);

            if (errorCode == ErrorCodes.NoErrors)
            {
                packet.TransferType = UsbTransferTypes.ControlOut;
                packet.Request = memWriteCmd;
                packet.Value = 0x0;

                for (int i = 0; i < count; i++)
                    packet.Buffer[i] = buffer[i + bufferOffset];

                packet.Length = count;

                // MemWrite command
                errorCode = UsbControlOutRequest(packet);
            }

            if (errorCode != ErrorCodes.NoErrors)
                errorCode = ErrorCodes.ErrorWritingDeviceMemory;

            m_controlTransferMutex.ReleaseMutex();

            return errorCode;
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Overriden to Write data to a device's memory
        /// </summary>
        /// <param name="memWriteCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal override ErrorCodes WriteDeviceMemory2(byte memWriteCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            m_controlTransferMutex.WaitOne();

            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (count > Constants.MAX_COMMAND_LENGTH)
                errorCode = ErrorCodes.CountGreaterThanMaxLength;

            if (errorCode == ErrorCodes.NoErrors)
            {
                UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_COMMAND_LENGTH);
                packet.TransferType = UsbTransferTypes.ControlOut;
                packet.Request = memWriteCmd;
                packet.Value = memoryOffset;

                for (int i = 0; i < count; i++)
                    packet.Buffer[i] = buffer[i + bufferOffset];

                packet.Length = count;

                // MemWrite command
                errorCode = UsbControlOutRequest(packet);
            }

            if (errorCode != ErrorCodes.NoErrors)
                errorCode = ErrorCodes.ErrorWritingDeviceMemory;

            m_controlTransferMutex.ReleaseMutex();

            return errorCode;
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="unlockKey">The unlock key</param>
        /// <param name="memCmd">The device's memory read/write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal override ErrorCodes WriteDeviceMemory3(ushort unlockKey, byte memCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            m_controlTransferMutex.WaitOne();

            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (count > Constants.MAX_COMMAND_LENGTH)
                errorCode = ErrorCodes.CountGreaterThanMaxLength;

            if (errorCode == ErrorCodes.NoErrors)
            {
                UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_COMMAND_LENGTH);
                packet.TransferType = UsbTransferTypes.ControlOut;
                packet.Request = memCmd;
                packet.Value = unlockKey;
                packet.Index = memoryOffset;
                packet.Length = count;

                for (int i = 0; i < count; i++)
                    packet.Buffer[i] = buffer[i + bufferOffset];

                // MemWrite command
                errorCode = UsbControlOutRequest(packet);
            }

            if (errorCode != ErrorCodes.NoErrors)
                errorCode = ErrorCodes.ErrorWritingDeviceMemory;

            m_controlTransferMutex.ReleaseMutex();

            return errorCode;
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="unlockKey">The unlock key</param>
        /// <param name="memCmd">The device's memory read/write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal override ErrorCodes WriteDeviceMemory4(ushort unlockKey, byte memCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            m_controlTransferMutex.WaitOne();

            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (count > Constants.MAX_COMMAND_LENGTH)
                errorCode = ErrorCodes.CountGreaterThanMaxLength;

            if (errorCode == ErrorCodes.NoErrors)
            {
                UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_COMMAND_LENGTH);
                packet.TransferType = UsbTransferTypes.ControlOut;
                packet.Request = memCmd;
                packet.Value = memoryOffset;
                packet.Index = 0;
                packet.Length = count;

                for (int i = 0; i < count; i++)
                    packet.Buffer[i] = buffer[i + bufferOffset];

                // MemWrite command
                errorCode = UsbControlOutRequest(packet);
            }

            if (errorCode != ErrorCodes.NoErrors)
                errorCode = ErrorCodes.ErrorWritingDeviceMemory;

            m_controlTransferMutex.ReleaseMutex();

            return errorCode;
        }

        //===================================================================================================
        /// <summary>
        /// Overriden to load data into the device's FPGA
        /// </summary>
        /// <param name="buffer">The data to load</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal override ErrorCodes LoadFPGA(byte request, byte[] buffer)
        {
            m_controlTransferMutex.WaitOne();

            ErrorCodes errorCode;

            UsbSetupPacket packet = new UsbSetupPacket(buffer.Length);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = request;
            packet.Value = 0;
            packet.Index = 0;
            packet.Length = (ushort)buffer.Length;
            Array.Copy(buffer, packet.Buffer, buffer.Length);
            errorCode = UsbControlOutRequest(packet);
            
            m_controlTransferMutex.ReleaseMutex();

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
        internal int Index;
        internal int Length;
        internal byte[] Data;

        internal BulkInBuffer(int numBytes)
        {
            Length = 0;
            Data = new byte[numBytes];
        }
    }
}
