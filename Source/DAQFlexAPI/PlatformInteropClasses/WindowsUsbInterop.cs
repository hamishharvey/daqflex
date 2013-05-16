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
using System.Runtime.InteropServices;
using System.Threading;

namespace MeasurementComputing.DAQFlex
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UsbInterfaceDescriptor
    {
        internal byte Length;
        internal byte DescriptorType;
        internal byte InterfaceNumber;
        internal byte AlternateSetting;
        internal byte NumEndpoints;
        internal byte InterfaceClass;
        internal byte InterfaceSubClass;
        internal byte InterfaceProtocol;
        internal byte Interface;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UsbPipeInformation
    {
        internal UsbPipeType PipeType;
        internal byte PipeId;
        internal ushort MaximumPacketSize;
        internal byte Interval;
    }

#if !WindowsCE
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
#endif
    internal struct WindowsUsbSetupPacket
    {
        internal byte RequestType;
        internal byte Request;
        internal ushort Value;
        internal ushort Index;
        internal ushort Length;
    }


    internal class WindowsUsbInterop : UsbPlatformInterop
    {
        //protected WindowsUsbSetupPacket m_aiScanStatusMessagePacket = new WindowsUsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);

        internal WindowsUsbInterop()
        {
        }

        internal WindowsUsbInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
            : base(deviceInfo, criticalParams)
        {
            //m_aiScanResetPacket.TransferType = UsbTransferTypes.ControlOut;
            //m_aiScanResetPacket.Request = 0x80;
            //m_aiScanResetPacket.DeferTransfer = false;
            //m_aiScanResetPacket.BytesTransfered = 0;

            //for (int i = 0; i < m_aiScanResetPacket.Length; i++)
            //    m_aiScanResetPacket.Buffer[i] = m_aiScanStatusMessage[i];
        }

        //=========================================================================================================================
        /// <summary>
        /// Virtual method for getting a list of DeviceInfos
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //=========================================================================================================================
        internal override ErrorCodes GetUsbDevices(Dictionary<int, DeviceInfo> deviceInfoList, DeviceListUsage deviceListUsage)
        {
            System.Diagnostics.Debug.Assert(false, "GetUsbDevices must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        internal override string GetDeviceID(DeviceInfo deviceInfo)
        {
            throw new NotImplementedException();
        }

        //=============================================================================================================
        /// <summary>
        /// Virtual method for a USB control IN request
        /// </summary>
        /// <returns>The result</returns>
        //=============================================================================================================
        internal override ErrorCodes UsbControlInRequest(UsbSetupPacket packet)
        {
            System.Diagnostics.Debug.Assert(false, "UsbControlInRequest must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //=============================================================================================================
        /// <summary>
        /// Virtual method for a USB control OUT request
        /// </summary>
        /// <returns>The result</returns>
        //=============================================================================================================
        internal override ErrorCodes UsbControlOutRequest(UsbSetupPacket packet)
        {
            System.Diagnostics.Debug.Assert(false, "UsbControlOutRequest must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //=============================================================================================================
        /// <summary>
        /// Virtual method for a USB Bulk IN request
        /// </summary>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <param name="bytesRequested">The number of bytes to requested</param>
        /// <param name="bytesReceived">The number of actual bytes received</param>
        /// <returns>The result</returns>
        //=============================================================================================================
        internal override ErrorCodes UsbBulkInRequest(ref BulkInBuffer buffer, ref uint bytesReceived)
        {
            System.Diagnostics.Debug.Assert(false, "UsbBulkInRequest must be implemented in a derived class");
            buffer = null;
            bytesReceived = 0;
            return ErrorCodes.MethodRequiresImplementation;
        }

        //=============================================================================================================
        /// <summary>
        /// Overriden for bulk out request
        /// </summary>
        /// <param name="buffer">The buffer containing the data to send</param>
        /// <param name="count">The number of samples to send</param>
        /// <returns>The result</returns>
        //=============================================================================================================
        internal override int UsbBulkOutRequest(UsbBulkOutRequest br, ref int bytesTransferred)
        {
            System.Diagnostics.Debug.Assert(false, "UsbControlOutRequest must be implemented in a derived class");
            return 0;
        }
    
        //=============================================================================================================
        /// <summary>
        /// Sets up parameters for bulk in transfers
        /// </summary>
        /// <param name="scanRate">The device scan rate</param>
        /// <param name="totalNumberOfBytes">The total number of bytes to transfer</param>
        /// <param name="transferSize">The number of bytes in each transfer request</param>
        //=============================================================================================================
        internal override void PrepareInputTransfers(double scanRate, int totalNumberOfBytes, int transferSize)
        {
            m_errorCode = ErrorCodes.NoErrors;

            m_totalNumberOfInputBytesRequested = totalNumberOfBytes;

            m_completedBulkInRequestBuffers.Clear();

            if (m_criticalParams.InputSampleMode == SampleMode.Finite)
            {
                m_numberOfWorkingInputRequests = Math.Min(8, Math.Max(1, totalNumberOfBytes / transferSize));

                //if (m_criticalParams.InputScanRate >= 10000 && m_numberOfWorkingInputRequests < 4)
                //    m_numberOfWorkingInputRequests = 4;

                m_numberOfQueuedInputRequests = Math.Max(1, m_numberOfWorkingInputRequests / 2);
            }
            else
            {
                m_numberOfWorkingInputRequests = 8;
                m_numberOfQueuedInputRequests = 4;
            }

            if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
            {
                int aiChannelCount = m_criticalParams.AiChannelCount;
                int byteRatio = m_criticalParams.DataInXferSize;

                m_totalNumberOfInputRequests = totalNumberOfBytes / (byteRatio * m_criticalParams.NumberOfSamplesForSingleIO);
            }
            else
            {
                if (totalNumberOfBytes <= transferSize)
                    m_totalNumberOfInputRequests = 1;
                else
                    m_totalNumberOfInputRequests = (int)Math.Ceiling((double)totalNumberOfBytes / (double)transferSize);
            }

            // the device will send a zero-length packet after the last data packet if
            // the number of bytes is a multiple of the packet size so add an extra request (or is it the transfer size)
            if ((totalNumberOfBytes % m_criticalParams.InputPacketSize == 0) || 
                (m_criticalParams.InputTransferMode == TransferMode.SingleIO && m_criticalParams.Requires0LengthPacketForSingleIO))
            {
                m_totalNumberOfInputRequests++;

                if (m_numberOfWorkingInputRequests == 1)
                    m_numberOfWorkingInputRequests++;
            }

            m_stopInputTransfers = false;

            int numberOfBulkInCopyBuffers = 50;

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

            // create the bulk in request objects that will be used in the transfers
            CreateBulkInputRequestObjects(transferSize);

            m_numberOfInputRequestsSubmitted = 0;
            m_numberOfInputRequestsCompleted = 0;

            // queue bulk in requests - at this point the device has not yet started
            QueueBulkInRequests(scanRate);
        }

        //============================================================================================================
        /// <summary>
        /// Sets up parameters for bulk in transfers
        /// </summary>
        /// <param name="scanRate">The device scan rate</param>
        /// <param name="totalNumberOfBytes">The total number of bytes to transfer</param>
        /// <param name="transferSize">The number of bytes in each transfer request</param>
        //============================================================================================================
        internal override void PrepareOutputTransfers(double scanRate, int totalNumberOfBytes, int transferSize)
        {
            m_errorCode = ErrorCodes.NoErrors;

            m_totalBytesReceivedByDevice = 0;
            m_driverInterfaceOutputBufferIndex = 0;
            m_totalNumberOfOutputBytesRequested = totalNumberOfBytes;

            if (m_criticalParams.OutputSampleMode == SampleMode.Continuous)
            {
                m_numberOfQueuedOutputRequests = 4;
                m_numberOfWorkingOutputRequests = 8;

                // for continuous mode this just has to be greater than m_numberOfQueuedOutputRequests
                m_totalNumberOfOutputRequests = 8; 
            }
            else if (totalNumberOfBytes <= transferSize)
            {
                m_totalNumberOfOutputRequests = 1;
                m_numberOfWorkingOutputRequests = 1;
                m_numberOfQueuedOutputRequests = 1;
            }
            else
            {
                m_totalNumberOfOutputRequests = (int)Math.Ceiling((double)totalNumberOfBytes / (double)transferSize);
                m_numberOfQueuedOutputRequests = Math.Min(4, m_totalNumberOfOutputRequests / 4);
                m_numberOfQueuedOutputRequests = Math.Max(1, m_numberOfQueuedOutputRequests);
                m_numberOfWorkingOutputRequests = Math.Min(8, m_totalNumberOfOutputRequests / 2); 
            }

            m_stopOutputTransfers = false;

            // create the bulk in request objects that will be used in the transfers
            CreateBulkOutputRequestObjects(transferSize);

            m_numberOfOutputRequestsSubmitted = 0;
            m_numberOfOutputRequestsCompleted = 0;

            // queue bulk in requests - at this point the device has not yet started
            QueueBulkOutRequests(scanRate);
        }

        //===================================================================================================
        /// <summary>
        /// Virtual method for creating bulk in request objects
        /// </summary>
        /// <param name="transferSize">The transfer size that will be used for the bulk transfers</param>
        //===================================================================================================
        protected virtual void CreateBulkInputRequestObjects(int transferSize)
        {
        }

        //===================================================================================================
        /// <summary>
        /// Virtual method for creating bulk out request objects
        /// </summary>
        /// <param name="transferSize">The transfer size that will be used for the bulk transfers</param>
        //===================================================================================================
        protected virtual void CreateBulkOutputRequestObjects(int transferSize)
        {
        }

        //===================================================================================
        /// <summary>
        /// Queues one or more bulk in requests just prior to starting an input scan
        /// </summary>
        //===================================================================================
        protected virtual void QueueBulkInRequests(double rate)
        {
        }

        //===================================================================================
        /// <summary>
        /// Queues one or more bulk out requests just prior to starting an output scan
        /// </summary>
        //===================================================================================
        protected virtual void QueueBulkOutRequests(double rate)
        {
        }

        //======================================================================
        /// <summary>
        /// Extracts the VID from the string
        /// </summary>
        /// <param name="vidPid">The string containing the VID and PID</param>
        /// <returns>The VID</returns>
        //======================================================================
        protected int GetVid(string vidPid)
        {
            int vid = 0;

            try
            {
                vid = Convert.ToInt32(vidPid.Substring(4, 4), 16);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.Assert(false, "Error reading device Vendor ID - string = {0}", vidPid);
            }

            return vid;
        }

        //======================================================================
        /// <summary>
        /// Extracts the PID from the string
        /// </summary>
        /// <param name="vidPid">The string containing the VID and PID</param>
        /// <returns>The PID</returns>
        //======================================================================
        protected int GetPid(string vidPid)
        {
            int pid = 0;

            try
            {
                pid = Convert.ToInt32(vidPid.Substring(13, 4), 16);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.Assert(false, "Error reading device product ID - string = {0}", vidPid);
            }

            return pid;
        }
    }
}
