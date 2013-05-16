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
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.IO;

namespace MeasurementComputing.DAQFlex
{
    internal unsafe class WinUsbInterop : WindowsUsbInterop
    {
        private IntPtr m_setupDeviceInfo;
        private SafeFileHandle m_deviceHandle;
        private IntPtr m_winUsbHandle;
        private byte[] m_statusBuffer;
        private UsbBulkInRequest m_lastInputRequestSubmitted;
        private Mutex m_outputTransferMutex = new Mutex();

        #region Kernel Interop

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", EntryPoint = "GetLastError", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetLastError();

        [DllImport("kernel32.dll", EntryPoint = "IsBadWritePtr", SetLastError = true, CharSet = CharSet.Auto)]
        internal static unsafe extern int IsBadWritePtr(void* ptr, uint count);

        #endregion

        #region Setup API Interop

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetClassDevs", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(ref System.Guid classGuid, String enumerator, int hwndParent, int flags);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiEnumDeviceInterfaces", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfo, int deviceInfoData, ref Guid deviceGuid, int memberIndex, IntPtr deviceInterfaceData);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInterfaceDetail", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfo, IntPtr deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiDestroyDeviceInfoList", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int SetupDiDestroyDeviceInfoList(IntPtr deviceInfo);

        #endregion

        #region WinUsb Interop

        private const uint PIPE_TRANSFER_TIMEOUT = 0x03;
        private const uint ALLOW_PARTIAL_READS = 0x05;
        private const uint AUTO_FLUSH = 0x06;
        private const uint RAW_IO = 0x07;
        private const string DEVICE_INTERFACE_GUID = "{E9C37F82-214C-4ede-AFFF-6B0C2C0146EF}";

        //[DllImport("user32.dll", SetLastError = true)]
        //static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_Initialize", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinUsb_Initialize(SafeFileHandle deviceHandle, out IntPtr winUsbHandle);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_Free", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinUsb_Free(IntPtr InterfaceHandle);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_QueryDeviceInformation", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinUsb_QueryDeviceInformation(IntPtr interfaceHandle, uint informationType, ref uint bufferLength, ref byte buffer);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_QueryInterfaceSettings", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinUsb_QueryInterfaceSettings(IntPtr interfaceHandle, byte alternateInterfaceNumber, ref UsbInterfaceDescriptor usbAltInterfaceDescriptor);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_QueryPipe", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinUsb_QueryPipe(IntPtr interfaceHandle, byte alternateInterfaceNumber, byte pipeIndex, ref UsbPipeInformation pipeInformation);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_ControlTransfer", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinUsb_ControlTransfer(IntPtr interfaceHandle, WindowsUsbSetupPacket setupPacket, byte[] buffer, uint bufferLength, ref uint lengthTransferred, IntPtr overlapped);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_ReadPipe", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinUsb_ReadPipe(IntPtr interfaceHandle, byte pipeID, byte[] buffer, uint bufferLength, ref uint lengthTransferred, IntPtr overlapped);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_WritePipe", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WinUsb_WritePipe(IntPtr interfaceHandle, byte pipeID, byte[] buffer, uint bufferLength, ref uint lengthTransferred, IntPtr overlapped);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_SetPipePolicy", SetLastError = true, CharSet = CharSet.Auto)]
        private unsafe static extern bool WinUsb_SetPipePolicy(IntPtr InterfaceHandle, byte pipeID, uint policyType, uint valueLength, void* value);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_AbortPipe", SetLastError = true, CharSet = CharSet.Auto)]
        private unsafe static extern bool WinUsb_AbortPipe(IntPtr InterfaceHandle, byte pipeID);

        [DllImport("winusb.dll", EntryPoint = "WinUsb_ResetPipe", SetLastError = true, CharSet = CharSet.Auto)]
        private unsafe static extern bool WinUsb_ResetPipe(IntPtr InterfaceHandle, byte pipeID);

        #endregion

        //=====================================================================================
        /// <summary>
        /// Default constructor used by the daq device manager before devices are detected
        /// </summary>
        //=====================================================================================
        internal WinUsbInterop()
        {
            m_setupDeviceInfo = IntPtr.Zero;
            m_deviceHandle = null;
        }

        //=====================================================================================
        /// <summary>
        /// Device-specific constructor used by the driver interface
        /// </summary>
        /// <param name="deviceNumber">The device number</param>
        //=====================================================================================
        internal WinUsbInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
            :base(deviceInfo, criticalParams)
        {
            m_deviceInfo = deviceInfo;

            InitializeDevice(m_deviceInfo);

            if (m_errorCode == ErrorCodes.NoErrors && !m_deviceInitialized)
                m_errorCode = ErrorCodes.DeviceNotInitialized;

            // create a setup packet for reading device status
            m_statusPacket = new WindowsUsbSetupPacket();
            m_statusPacket.RequestType = ControlRequestType.VENDOR_CONTROL_IN;
            m_statusPacket.Request = 0x44;
            m_statusPacket.Value = 0;
            m_statusPacket.Index = 0;
            m_statusPacket.Length = 2;
            m_statusBuffer = new byte[2];
            m_maxTransferSize = 256000;
        }

        //=========================================================================
        /// <summary>
        /// Initialize and configure the USB device
        /// </summary>
        /// <param name="deviceNumber">The device number</param>
        /// <returns>true if the device is successfully initiatlized and configured
        /// otherwise false</returns>
        //=========================================================================
        protected void InitializeDevice(DeviceInfo deviceInfo)
        {
            m_deviceInitialized = false;

            m_deviceHandle = GetDeviceHandle(deviceInfo);

            if (!m_deviceHandle.IsInvalid)
            {
                ThreadPool.BindHandle(m_deviceHandle);
                InitializeDevice(m_deviceHandle, deviceInfo);
            }
        }

        //=========================================================================
        /// <summary>
        /// Initialize and configures the USB device
        /// </summary>
        /// <param name="deviceHandle">The handle to the device</param>
        /// <param name="deviceInfo">The device number</param>
        //=========================================================================
        protected void InitializeDevice(SafeFileHandle deviceHandle, DeviceInfo deviceInfo)
        {
            UsbInterfaceDescriptor uid = new UsbInterfaceDescriptor();
            UsbPipeInformation pipeInfo = new UsbPipeInformation();

            uid.Length = 0;
            uid.DescriptorType = 0;
            uid.InterfaceNumber = 0;
            uid.AlternateSetting = 0;
            uid.NumEndpoints = 0;
            uid.InterfaceClass = 0;
            uid.InterfaceSubClass = 0;
            uid.InterfaceProtocol = 0;
            uid.Interface = 0;

            pipeInfo.PipeType = 0;
            pipeInfo.PipeId = 0;
            pipeInfo.MaximumPacketSize = 0;
            pipeInfo.Interval = 0;

            if (!deviceHandle.IsInvalid)
            {
                m_winUsbHandle = IntPtr.Zero;

                if (WinUsbInterop.WinUsb_Initialize(deviceHandle, out m_winUsbHandle) == true)
                {
                    if (WinUsbInterop.WinUsb_QueryInterfaceSettings(m_winUsbHandle, 0, ref uid) == true)
                    {
                        // get the device speed
                        byte[] buffer = new byte[1];
                        uint length = 1;
                        WinUsbInterop.WinUsb_QueryDeviceInformation(m_winUsbHandle, 0x01, ref length, ref buffer[0]);

                        m_deviceInitialized = true;

                        // get the bulk pipes info
                        for (byte i = 0; i < uid.NumEndpoints; i++)
                        {
                            WinUsbInterop.WinUsb_QueryPipe(m_winUsbHandle, 0, i, ref pipeInfo);

                            uint timeOut = 0;
                            bool result;

                            unsafe
                            {
                                if (pipeInfo.PipeType == UsbPipeType.Bulk)
                                {
                                    if ((pipeInfo.PipeId & 0x80) == 0x80)
                                    {
                                        deviceInfo.EndPointIn = pipeInfo.PipeId;
                                       //    deviceInfo.EndPointOut = pipeInfo.PipeId;

                                        deviceInfo.MaxPacketSize = pipeInfo.MaximumPacketSize;

                                        // set timeout to 0
                                        result = WinUsbInterop.WinUsb_SetPipePolicy(m_winUsbHandle,
                                                                                        pipeInfo.PipeId,
                                                                                        PIPE_TRANSFER_TIMEOUT,
                                                                                        sizeof(uint),
                                                                                        &timeOut);

                                        // allow partial reads
                                        if (result == true)
                                        {
                                            bool allow = true;
                                            result = WinUsbInterop.WinUsb_SetPipePolicy(m_winUsbHandle,
                                                                                pipeInfo.PipeId,
                                                                                ALLOW_PARTIAL_READS,
                                                                                sizeof(uint),
                                                                                &allow);
                                        }

                                        // auto flush (discard extra data)
                                        if (result == true)
                                        {
                                            bool flush = true;
                                            result = WinUsbInterop.WinUsb_SetPipePolicy(m_winUsbHandle,
                                                                                pipeInfo.PipeId,
                                                                                AUTO_FLUSH,
                                                                                sizeof(uint),
                                                                                &flush);
                                        }

                                        if (result == true)
                                        {
                                            // set Raw I/O mode
                                            byte rawIO = 1;
                                            result = WinUsbInterop.WinUsb_SetPipePolicy(m_winUsbHandle,
                                                                                            pipeInfo.PipeId,
                                                                                            RAW_IO,
                                                                                            sizeof(uint),
                                                                                            &rawIO);
                                        }

                                        if (!result)
                                            m_errorCode = ErrorCodes.DeviceNotInitialized;
                                    }
                                    else
                                    {
                                        deviceInfo.EndPointOut = pipeInfo.PipeId;

                                        //// set Raw I/O mode
                                        //byte rawIO = 1;
                                        //result = WinUsbInterop.WinUsb_SetPipePolicy(m_winUsbHandle,
                                        //                                                pipeInfo.PipeId,
                                        //                                                RAW_IO,
                                        //                                                sizeof(uint),
                                        //                                                &rawIO);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                m_errorCode = ErrorCodes.DeviceHandleAlreadyCreated;
            }
        }

        //=======================================================================
        /// <summary>
        /// Gets the device handle used for communicating with the device
        /// </summary>
        /// <param name="deviceInfo"></param>
        /// <returns>The device handle</returns>
        //=======================================================================
        protected SafeFileHandle GetDeviceHandle(DeviceInfo deviceInfo)
        {
            string devicePath = deviceInfo.DevicePath;

            // create a new handle for the device
            return WinUsbInterop.CreateFile(devicePath,
                                        Constants.GENERIC_WRITE | Constants.GENERIC_READ,
                                        Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE,
                                        IntPtr.Zero,
                                        Constants.OPEN_EXISTING,
                                        Constants.FILE_FLAG_OVERLAPPED,
                                        IntPtr.Zero);


        }

        //===================================================================================================
        /// <summary>
        /// Used to 
        /// </summary>
        //===================================================================================================
        internal override bool AcquireDevice()
        {
            InitializeDevice(m_deviceInfo);

            if (m_deviceInitialized)
                return true;
            else
                return false;
        }

        //===================================================================================================
        /// <summary>
        /// Get the device ID that a user set to store in the device info object
        /// </summary>
        /// <returns>The device ID</returns>
        //===================================================================================================
        internal override string GetDeviceID(DeviceInfo deviceInfo)
        {
            m_controlTransferMutex.WaitOne();

            string deviceID = String.Empty;

            InitializeDevice(deviceInfo);

            if (m_deviceInitialized)
            {
                UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);

                packet.TransferType = UsbTransferTypes.ControlOut;
                packet.Request = 0x80;
                packet.DeferTransfer = false;
                packet.BytesTransfered = 0;

                for (int i = 0; i < m_devIdMessage.Length; i++)
                    packet.Buffer[i] = m_devIdMessage[i];

                UsbControlOutRequest(packet);

                packet.TransferType = UsbTransferTypes.ControlIn;
                UsbControlInRequest(packet);

                m_deviceHandle.Close();

                string response = m_ae.GetString(packet.Buffer).Trim(new char[] { Constants.NULL_TERMINATOR });

                int index = response.IndexOf('=');

                if (index >= 0)
                    deviceID = response.Substring(index + 1);

                m_controlTransferMutex.ReleaseMutex();
            }

            ReleaseDevice();

            return deviceID;
        }

        //===================================================================================================
        /// <summary>
        /// Creates one or more BulkInRequest objects that contain the overlapped struct and data buffer
        /// These are used by the SubmitBulkInRequest and CompleteBulkInRequest methods
        /// </summary>
        //===================================================================================================
        protected override void CreateBulkInputRequestObjects(int transferSize)
        {
            m_bulkInRequests.Clear();

            int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);
            int channelCount = m_criticalParams.AiChannelCount;

            for (int i = 0; i < m_numberOfWorkingInputRequests; i++)
            {
                UsbBulkInRequest request = new UsbBulkInRequest();
                request.Index = i;
                request.Overlapped = new Overlapped();

                if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
                {
                    request.Buffer = new byte[byteRatio * channelCount];
                    request.BytesRequested = byteRatio * channelCount;

                    byte rawIO = 0;
                    WinUsbInterop.WinUsb_SetPipePolicy(m_winUsbHandle,
                                                       m_deviceInfo.EndPointIn,
                                                       RAW_IO,
                                                       sizeof(uint),
                                                       &rawIO);
                }
                else if (m_criticalParams.InputTransferMode == TransferMode.BurstIO)
                {
                    request.Buffer = new byte[transferSize];
                    request.BytesRequested = request.Buffer.Length;

                    byte rawIO = 0;
                    WinUsbInterop.WinUsb_SetPipePolicy(m_winUsbHandle,
                                                       m_deviceInfo.EndPointIn,
                                                       RAW_IO,
                                                       sizeof(uint),
                                                       &rawIO);
                }
                else
                {
                    request.Buffer = new byte[transferSize];
                    request.BytesRequested = request.Buffer.Length;

                    // enable Raw I/O mode for better throughput
                    byte rawIO = 1;
                    WinUsbInterop.WinUsb_SetPipePolicy(m_winUsbHandle,
                                                       m_deviceInfo.EndPointIn,
                                                       RAW_IO,
                                                       sizeof(uint),
                                                       &rawIO);
                }

                request.NativeOverlapped = request.Overlapped.Pack(CompleteBulkInRequest, request.Buffer);
                
                if (sizeof(IntPtr) == 8)
                    request.NativeOverLappedIntPtr = new IntPtr((long)request.NativeOverlapped);
                else
                    request.NativeOverLappedIntPtr = new IntPtr((int)request.NativeOverlapped);

                if (i > 0)
                    m_bulkInRequests[i - 1].Next = request;

                m_bulkInRequests.Add(request);
            }

            m_bulkInRequests[m_bulkInRequests.Count - 1].Next = m_bulkInRequests[0];
        }

        //===================================================================================================
        /// <summary>
        /// Creates one or more BulkOutRequest objects that contain the overlapped struct and data buffer
        /// These are used by the SubmitBulkOutRequest and CompleteBulkOutRequest methods
        /// </summary>
        //===================================================================================================
        protected override void CreateBulkOutputRequestObjects(int transferSize)
        {
            m_bulkOutRequests.Clear();

            int byteCount = 0;
            int bytesRequested;

            for (int i = 0; i < m_numberOfWorkingOutputRequests; i++)
            {
                UsbBulkOutRequest request = new UsbBulkOutRequest();
                request.Index = i;
                request.Overlapped = new Overlapped();
                request.Buffer = new byte[transferSize];

                if (m_totalNumberOfOutputBytesRequested - byteCount > transferSize)
                    bytesRequested = request.Buffer.Length;
                else
                    bytesRequested = m_totalNumberOfOutputBytesRequested - byteCount;

                byteCount += bytesRequested;

                request.BytesRequested = bytesRequested;
                
                request.NativeOverlapped = request.Overlapped.Pack(CompleteBulkOutRequest, request.Buffer);

                if (sizeof(IntPtr) == 8)
                    request.NativeOverLappedIntPtr = new IntPtr((long)request.NativeOverlapped);
                else
                    request.NativeOverLappedIntPtr = new IntPtr((int)request.NativeOverlapped);

                if (i > 0)
                    m_bulkOutRequests[i - 1].Next = request;

                m_bulkOutRequests.Add(request);
            }

            UsbBulkOutRequest br;

            for (int i = 0; i < m_numberOfQueuedOutputRequests; i++)
            {
                br = m_bulkOutRequests[i];

                // transfer data from the driver interface's internal write buffer to the request buffer
                Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, br.Buffer, 0, br.BytesRequested);

                m_driverInterfaceOutputBufferIndex += br.BytesRequested;

                if (m_driverInterfaceOutputBufferIndex >= m_driverInterfaceOutputBuffer.Length)
                    m_driverInterfaceOutputBufferIndex = 0;
            }

            m_bulkOutRequests[m_bulkOutRequests.Count - 1].Next = m_bulkOutRequests[0];
        }

        //===================================================================================
        /// <summary>
        /// Queues one or more bulk in requests just prior to starting an input scan
        /// </summary>
        //===================================================================================
        protected override void QueueBulkInRequests(double rate)
        {
            for (int i = 0; i < m_numberOfQueuedInputRequests; i++)
            {
                UsbBulkInRequest request = m_bulkInRequests[i];
                bool result = SubmitBulkInRequest(request);

                if (!result)
                    break;
            }
        }

        //===================================================================================
        /// <summary>
        /// Queues one or more bulk out requests just prior to starting an output scan
        /// </summary>
        //===================================================================================
        protected override void QueueBulkOutRequests(double rate)
        {
            m_outputTransferMutex.WaitOne();

            m_totalNumberOfOutputBytesTransferred = 0;

            for (int i = 0; i < m_numberOfQueuedOutputRequests; i++)
            {
                UsbBulkOutRequest request = m_bulkOutRequests[i];
                bool result = SubmitBulkOutRequest(request);

                if (!result)
                    break;
            }

            m_outputTransferMutex.ReleaseMutex();
        }

        //===================================================================================================
        /// <summary>
        /// Sumbimits one or more read pipe requests using overlapped I/O
        /// The I/O completion callback is handled by CompleteBulkInRequest
        /// </summary>
        /// <param name="request">A UsbBulkInRequest object</param>
        /// <returns>The result of the read pipe method</returns>
        //===================================================================================================
        protected bool SubmitBulkInRequest(UsbBulkInRequest request)
        {
            bool result;
            uint lengthTransfered = 0;

            if (m_deviceInfo.EndPointIn == 0)
            {
                m_errorCode = ErrorCodes.BulkInputTransfersNotSupported;
                return false;
            }

            result = WinUsbInterop.WinUsb_ReadPipe(m_winUsbHandle,
                                                   m_deviceInfo.EndPointIn,
                                                   request.Buffer,
                                                   (uint)request.BytesRequested,
                                                   ref lengthTransfered,
                                                   request.NativeOverLappedIntPtr);

            m_totalNumberOfInputBytesTransferred += (int)lengthTransfered;

            // store this request object so we know which one to submit next
            m_lastInputRequestSubmitted = request;

            // increment number of requests submitted
            m_numberOfInputRequestsSubmitted++;

            // for continuous mode keep incrementing the total number of requests
            if (m_criticalParams.InputSampleMode == SampleMode.Continuous)
            {
                m_totalNumberOfInputRequests++;

                if (m_totalNumberOfInputRequests < 0)
                {
                    m_numberOfInputRequestsSubmitted = 0;
                    m_totalNumberOfInputRequests = m_numberOfInputRequestsSubmitted + 1;
                }
            }

            if (!result)
            {
                int errorCode = WinUsbInterop.GetLastError();

                // error 997 is Overlapped I/O operation is in progress (so this is good).
                if (errorCode == 997)
                    result = true;
            }

            return result;
        }

        //===================================================================================================
        /// <summary>
        /// Overriden to submit bulk out requests
        /// </summary>
        /// <param name="request">The request object</param>
        /// <returns>True on success otherwise false</returns>
        //===================================================================================================
        internal override bool SubmitBulkOutRequest(UsbBulkOutRequest request)
        {
            bool result;
            uint lengthTransfered = 0;

            if (m_deviceInfo.EndPointOut == 0)
            {
                m_errorCode = ErrorCodes.BulkOutputTransfersNotSupported;
                m_outputTransferMutex.ReleaseMutex();
                return false;
            }

            result = WinUsbInterop.WinUsb_WritePipe(m_winUsbHandle,
                                                    m_deviceInfo.EndPointOut,
                                                    request.Buffer,
                                                    (uint)request.BytesRequested,
                                                    ref lengthTransfered,
                                                    request.NativeOverLappedIntPtr);

            m_numberOfOutputRequestsSubmitted++;

            if (!result)
            {
                int errorCode = WinUsbInterop.GetLastError();

                // error 997 is Overlapped I/O operation is in progress (so this is good).
                if (errorCode == 997)
                {
                    m_totalNumberOfOutputBytesTransferred += request.BytesRequested;
                    result = true;
                }
            }

            return result;
        }


        //===================================================================================================
        /// <summary>
        /// This is the I/O completion callback for WinUsb_ReadPipe
        /// The data is contained in the BulkInRequest objects
        /// </summary>
        /// <param name="errorCode">The error code of the I/O operation</param>
        /// <param name="numBytes">The number of bytes transfered</param>
        /// <param name="nativeOverlapped">A pointer to the native overlapped structure</param>
        //===================================================================================================
        protected void CompleteBulkInRequest(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            m_completionMutex.WaitOne();

            if (errorCode == 0)
            {
                UsbBulkInRequest bulkInRequest = null;

                // add the bulk in request that was completed to the queue
                if (numBytes > 0)
                {
                    foreach (UsbBulkInRequest request in m_bulkInRequests)
                    {
                        if (request.NativeOverlapped == nativeOverlapped)
                        {
                            bulkInRequest = request;

                            // get a buffer off the Ready Queue
                            BulkInBuffer bulkInBuffer = null;

                            while (bulkInBuffer == null)
                            {
                                bulkInBuffer = QueueBulkInReadyBuffers(null, QueueAction.Dequeue);

                                if (bulkInBuffer == null)
                                {
                                    Thread.Sleep(1);
                                }
                            }

                            // copy the data into the buffer that was just dequeued
                            Array.Copy(bulkInRequest.Buffer, bulkInBuffer.Data, numBytes);
                            bulkInBuffer.Length = (int)numBytes;

                            // now add the buffer to the Completed Queue
                            QueueBulkInCompletedBuffers(bulkInBuffer, QueueAction.Enqueue);

                            break;
                        }
                    }

                    if (bulkInRequest != null)
                    {
                        m_numberOfInputRequestsCompleted++;

                        if ((m_inputSampleMode == SampleMode.Continuous && !m_stopInputTransfers) ||
                            (m_inputSampleMode == SampleMode.Finite && m_numberOfInputRequestsSubmitted < m_totalNumberOfInputRequests))
                        {
                            SubmitBulkInRequest(m_lastInputRequestSubmitted.Next);
                        }
                    }
                }
            }
            else
            {
                // error 995 is operation aborted which can occur when a scan is stopped
                if (errorCode != 995)
                {
                    m_errorCode = CheckOverrun();

                    if (m_errorCode == ErrorCodes.DataOverrun)
                    {
                        ClearStall(m_deviceInfo.EndPointIn, m_aiScanResetPacket);
                    }
                    else
                    {
                        // if not an overrun then check other codes
                        if (errorCode == 31)
                        {
                            m_errorCode = ErrorCodes.DeviceNotResponding;
                        }
                        else
                        {
                            m_errorCode = ErrorCodes.UnknownError;
                        }
                    }
                }
            }

            m_completionMutex.ReleaseMutex();
        }

        //===================================================================================================
        /// <summary>
        /// This is the I/O completion callback for WinUsb_WritePipe
        /// </summary>
        /// <param name="errorCode">The error code of the I/O operation</param>
        /// <param name="numBytes">The number of bytes transfered</param>
        /// <param name="nativeOverlapped">A pointer to the native overlapped structure</param>
        //===================================================================================================
        protected void CompleteBulkOutRequest(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            m_outputTransferMutex.WaitOne();

            if (errorCode == 0 && numBytes != 0 && nativeOverlapped != null)
            {
                int bytesToCopyOnFirstPass;
                int bytesToCopyOnSecondPass;
                int sourceBufferLength = m_driverInterfaceOutputBuffer.Length;
                int bytesToTransfer = 0;

                m_totalBytesReceivedByDevice += (int)numBytes;

                UsbBulkOutRequest availableRequest = null;

                foreach (UsbBulkOutRequest br in m_bulkOutRequests)
                {
                    if (br.NativeOverlapped == nativeOverlapped)
                    {
                        availableRequest = br;
                        bytesToTransfer = availableRequest.Buffer.Length;

                        if (m_criticalParams.OutputSampleMode == SampleMode.Finite)
                        {
                            if (m_totalNumberOfOutputBytesRequested - m_totalNumberOfOutputBytesTransferred < bytesToTransfer)
                            {
                                bytesToTransfer = m_totalNumberOfOutputBytesRequested - m_totalNumberOfOutputBytesTransferred;
                                availableRequest.BytesRequested = bytesToTransfer;
                            }
                        }

                        break;
                    }
                }

                m_numberOfOutputRequestsCompleted++;

                if (availableRequest != null && bytesToTransfer > 0)
                {
                    if ((m_criticalParams.OutputSampleMode == SampleMode.Continuous && !m_stopOutputTransfers) ||
                        (m_criticalParams.OutputSampleMode == SampleMode.Finite && m_numberOfOutputRequestsSubmitted < m_totalNumberOfOutputRequests))
                    {

                        if ((m_driverInterfaceOutputBufferIndex + bytesToTransfer) >= sourceBufferLength)
                        {
                            // two passes are required since the current input scan write index
                            // wrapped around to the beginning of the internal read buffer
                            bytesToCopyOnFirstPass = sourceBufferLength - m_driverInterfaceOutputBufferIndex;
                            bytesToCopyOnSecondPass = (int)bytesToTransfer - bytesToCopyOnFirstPass;
                        }
                        else
                        {
                            // only one pass is required since the current input scan write index
                            // did not wrap around
                            bytesToCopyOnFirstPass = (int)bytesToTransfer;
                            bytesToCopyOnSecondPass = 0;
                        }

                        if (bytesToCopyOnFirstPass > 0)
                            Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, availableRequest.Buffer, 0, bytesToCopyOnFirstPass);

                        m_driverInterfaceOutputBufferIndex += bytesToCopyOnFirstPass;

                        if (m_driverInterfaceOutputBufferIndex >= m_driverInterfaceOutputBuffer.Length)
                            m_driverInterfaceOutputBufferIndex = 0;

                        if (bytesToCopyOnSecondPass > 0)
                            Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, availableRequest.Buffer, bytesToCopyOnFirstPass, bytesToCopyOnSecondPass);

                        m_driverInterfaceOutputBufferIndex += bytesToCopyOnSecondPass;

                        SubmitBulkOutRequest(availableRequest);
                    }
                }
            }
            else
            {
                if (errorCode == 995) // stop or abort
                    m_errorCode = ErrorCodes.NoErrors;
                else if (errorCode == 31)
                    m_errorCode = ErrorCodes.DataUnderrun;
                else
                    m_errorCode = ErrorCodes.UnknownError;
            }

            m_outputTransferMutex.ReleaseMutex();
        }

        //=====================================================================================================
        /// <summary>
        /// Fills a list with usb device information
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //=====================================================================================================
        internal override ErrorCodes GetUsbDevices(Dictionary<int, DeviceInfo> deviceInfoList, bool refresh)
        {
            bool lastDevice = true;
            string[] pathParts = null;
            int deviceNumber = 0;
            string devicePath = string.Empty;
            int sizeOfDeviceInterfaceData;
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_setupDeviceInfo = IntPtr.Zero;

            Guid deviceGuid = new Guid(DEVICE_INTERFACE_GUID);

            if (refresh == true)
                deviceInfoList.Clear();

            if (deviceInfoList.Count == 0)
            {
                // get a handle to the device information set using the device GUID
                m_setupDeviceInfo = WinUsbInterop.SetupDiGetClassDevs(ref deviceGuid, null, 0, (int)(UsbDeviceConfigInfoFlags.Present | UsbDeviceConfigInfoFlags.DeviceInterface));

                if (m_setupDeviceInfo != IntPtr.Zero)
                {
                    // initialize the device interface data
                    if (sizeof(IntPtr) == 4)
                        sizeOfDeviceInterfaceData = 28;
                    else
                        sizeOfDeviceInterfaceData = 32;

                    IntPtr deviceInterfaceData = Marshal.AllocHGlobal(sizeOfDeviceInterfaceData);
                    Marshal.WriteInt32(deviceInterfaceData, sizeOfDeviceInterfaceData);

                    int memberIndex = 0;

                    do
                    {
                        DeviceInfo di = new DeviceInfo();

                        // get a handle to the device interface data structure
                        bool result = WinUsbInterop.SetupDiEnumDeviceInterfaces(m_setupDeviceInfo, 0, ref deviceGuid, memberIndex, deviceInterfaceData);

                        if (result == true)
                        {
                            lastDevice = false;

                            int requiredLength = 0;

                            // first call to GetDeviceInterfaceDetail will return the required length for the detail buffer
                            result = WinUsbInterop.SetupDiGetDeviceInterfaceDetail(m_setupDeviceInfo, deviceInterfaceData, IntPtr.Zero, 0, ref requiredLength, IntPtr.Zero);

                            if (requiredLength > 0)
                            {
                                // allocate the detail buffer using the required length
                                IntPtr detailBuffer = Marshal.AllocHGlobal(requiredLength);

                                if (detailBuffer != IntPtr.Zero)
                                {
                                    // set the size
                                    if (sizeof(IntPtr) == 4)
                                        Marshal.WriteInt32(detailBuffer, 4 + Marshal.SystemDefaultCharSize);
                                    else
                                        Marshal.WriteInt32(detailBuffer, 8);

                                    result = WinUsbInterop.SetupDiGetDeviceInterfaceDetail(m_setupDeviceInfo, deviceInterfaceData, detailBuffer, requiredLength, ref requiredLength, IntPtr.Zero);

                                    if (result == true)
                                    {
                                        // set the device path name ptr (detailBuffer[4])
                                        char* pPathName = (char*)((byte*)detailBuffer.ToPointer() + 4);

                                        // copy unmanaged string contents to managed string
                                        devicePath = new String(pPathName);
                                        pathParts = devicePath.Split(new char[] { '#' });

                                        // set the device info properties
                                        di.DevicePath = devicePath;
                                        di.DeviceNumber = deviceNumber;
                                        di.Vid = GetVid(pathParts[1]);
                                        di.Pid = GetPid(pathParts[1]);
                                        di.DisplayName = GetDeviceName(di.Pid);
                                        di.SerialNumber = pathParts[2];

                                        deviceInfoList.Add(deviceNumber, di);

                                        deviceNumber++;

                                        memberIndex++;
                                    }
                                }
                                else
                                {
                                    lastDevice = true;
                                    errorCode = ErrorCodes.DetailBufferIsNull;
                                }

                                // free the unmanaged memory pointed to by detailBuffer
                                Marshal.FreeHGlobal(detailBuffer);
                            }
                            else
                            {
                                lastDevice = true;
                                errorCode = ErrorCodes.RequiredLengthIsZero;
                            }
                        }
                        else
                        {
                            lastDevice = true;
                            //???return ErrorCodes.SetupDiEnumDeviceInterfacesFailed;
                        }

                    } while (!lastDevice);

                    // free the unmanaged memory pointed to by detailBuffer
                    Marshal.FreeHGlobal(deviceInterfaceData);

                    WinUsbInterop.SetupDiDestroyDeviceInfoList(m_setupDeviceInfo);
                }
            }

            return errorCode;
        }

        ////===================================================================================================
        ///// <summary>
        ///// Reads a device's memory
        ///// </summary>
        ///// <param name="offset">The starting addresss</param>
        ///// <param name="count">The number of bytes to read</param>
        ///// <param name="buffer">The buffer containing the memory contents</param>
        ///// <returns>The error code</returns>
        ////===================================================================================================
        //internal override ErrorCodes ReadDeviceMemory(ushort memoryOffset, byte count, out byte[] buffer)
        //{
        //    ErrorCodes errorCode = ErrorCodes.NoErrors;

        //    UsbSetupPacket packet = new UsbSetupPacket(count);
        //    packet.TransferType = UsbTransferTypes.ControlOut;
        //    packet.Request = MEM_ADDR;
        //    packet.Value = 0;
        //    packet.Index = 0;
        //    packet.Length = 2;
        //    packet.Buffer[0] = (byte)(0x00FF & memoryOffset);
        //    packet.Buffer[1] = (byte)((0xFF00 & memoryOffset) >> 8);

        //    buffer = null;

        //    if (count > Constants.MAX_COMMAND_LENGTH)
        //        return ErrorCodes.CountGreaterThanMaxLength;

        //    errorCode = UsbControlOutRequest(packet);

        //    if (errorCode == ErrorCodes.NoErrors)
        //    {
        //        packet.TransferType = UsbTransferTypes.ControlIn;
        //        packet.Request = MEM_READ;
        //        packet.Length = count;

        //        errorCode = UsbControlInRequest(packet);

        //        buffer = packet.Buffer;
        //    }

        //    if (errorCode != ErrorCodes.NoErrors)
        //        errorCode = ErrorCodes.ErrorReadingDeviceMemory;

        //    return errorCode;
        //}

        //===================================================================================================
        /// <summary>
        /// Writes data to a device's memory
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

        //==================================================================
        /// <summary>
        /// Method for a USB control IN request
        /// </summary>
        /// <returns>The result</returns>
        //==================================================================
        internal override ErrorCodes UsbControlInRequest(UsbSetupPacket packet)
        {
            bool result;

            WindowsUsbSetupPacket winUsbPacket = new WindowsUsbSetupPacket();
            winUsbPacket.RequestType = ControlRequestType.VENDOR_CONTROL_IN;
            winUsbPacket.Request = packet.Request;
            winUsbPacket.Value = packet.Value;
            winUsbPacket.Index = packet.Index;
            winUsbPacket.Length = packet.Length;

            uint bytesTransfered = 0;

            result = WinUsbInterop.WinUsb_ControlTransfer(m_winUsbHandle,
                                                          winUsbPacket,
                                                          packet.Buffer,
                                                          packet.Length,
                                                          ref bytesTransfered,
                                                          IntPtr.Zero);

            packet.BytesTransfered = bytesTransfered;

            if (result == true)
            {
                return ErrorCodes.NoErrors;
            }
            else
            {
                int lastError = WinUsbInterop.GetLastError();

                if (lastError == 22 || lastError == 1176)
                    return ErrorCodes.DeviceNotResponding;
                else if (lastError == 31)
                    return ErrorCodes.InvalidMessage;
                else if (lastError == 6)
                    return ErrorCodes.InvalidDeviceHandle;

                System.Diagnostics.Debug.Assert(false, String.Format("Unknown Error Code: {0}", lastError));
                return ErrorCodes.UnknownError;
            }
        }

        //==================================================================
        /// <summary>
        /// Method for a USB control OUT request
        /// </summary>
        /// <returns>The result</returns>
        //==================================================================
        internal override ErrorCodes UsbControlOutRequest(UsbSetupPacket packet)
        {
            bool result;

            WindowsUsbSetupPacket winUsbPacket = new WindowsUsbSetupPacket();
            winUsbPacket.RequestType = ControlRequestType.VENDOR_CONTROL_OUT;
            winUsbPacket.Request = packet.Request;
            winUsbPacket.Value = packet.Value;
            winUsbPacket.Index = packet.Index;
            winUsbPacket.Length = packet.Length;

            uint bytesTransfered = 0;

            result = WinUsbInterop.WinUsb_ControlTransfer(m_winUsbHandle,
                                                          winUsbPacket,
                                                          packet.Buffer,
                                                          (ushort)winUsbPacket.Length,
                                                          ref bytesTransfered,
                                                          IntPtr.Zero);

            packet.BytesTransfered = bytesTransfered;

            if (result == true)
            {
                return ErrorCodes.NoErrors;
            }
            else
            {
                int lastError = WinUsbInterop.GetLastError();

                if (lastError == 22 || lastError == 1176)
                    return ErrorCodes.DeviceNotResponding;
                else if (lastError == 31)
                    return ErrorCodes.InvalidMessage;
                else if (lastError == 6)
                    return ErrorCodes.InvalidDeviceHandle;

                System.Diagnostics.Debug.Assert(false, String.Format("Unknown Error Code: {0}", lastError));
                return ErrorCodes.UnknownError;
            }
        }

        //==================================================================
        /// <summary>
        /// Method for a USB Bulk IN request
        /// </summary>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <param name="bytesRequested">The number of bytes to requested</param>
        /// <param name="bytesReceived">The number of actual bytes received</param>
        /// <returns>The result</returns>
        //==================================================================
        internal override ErrorCodes UsbBulkInRequest(ref BulkInBuffer buffer, ref uint bytesReceived)
        {
            BulkInBuffer bulkInBuffer = null;

            do
            {
                if (m_errorCode != ErrorCodes.NoErrors)
                    break;

                if (m_bulkInCompletedBuffers.Count > 0)
                {
                    bulkInBuffer = QueueBulkInCompletedBuffers(null, QueueAction.Dequeue);
                }
                else
                {
                    Thread.Sleep(1);
                }

            } while (bulkInBuffer == null && !m_stopInputTransfers);

            if (bulkInBuffer != null)
            {
                buffer = bulkInBuffer;
                bytesReceived = (uint)bulkInBuffer.Length;
            }
            else
            {
                buffer = null;
                bytesReceived = 0;
            }

            return m_errorCode;
        }

        //==================================================================
        /// <summary>
        /// Clears the overrun condition 
        /// </summary>
        //==================================================================
        internal override void ClearDataOverrun()
        {
            ClearStall(m_deviceInfo.EndPointIn, m_aiScanResetPacket);
        }

        //==================================================================
        /// <summary>
        /// Clears the overrun condition 
        /// </summary>
        //==================================================================
        internal override void ClearDataUnderrun()
        {
            ClearStall(m_deviceInfo.EndPointOut, m_aoScanResetPacket);
        }

        //==================================================================
        /// <summary>
        /// Clears the pipe's stall state 
        /// </summary>
        //==================================================================
        protected void ClearStall(byte pipe, UsbSetupPacket resetCommandPacket)
        {
            m_controlTransferMutex.WaitOne();

            UsbControlOutRequest(resetCommandPacket);

            m_controlTransferMutex.ReleaseMutex();

            WinUsb_ResetPipe(m_winUsbHandle, pipe);
        }

        //==================================================================================
        /// <summary>
        /// Stops bulk in transfers by aborting the pending transfers
        /// </summary>
        //==================================================================================
        internal override void StopInputTransfers()
        {
            m_stopInputTransferMutex.WaitOne();

            // if one thread already stop the tansfers then do nothing
            if (!m_stopInputTransfers)
            {
                // abort all pending transferes
                if (m_winUsbHandle != IntPtr.Zero)
                    WinUsbInterop.WinUsb_AbortPipe(m_winUsbHandle, m_deviceInfo.EndPointIn);

                // set this flag so running threads can terminate
                m_stopInputTransfers = true;
            }

            m_stopInputTransferMutex.ReleaseMutex();
        }

        //==================================================================================
        /// <summary>
        /// Stops bulk out transfers by aborting the pending transfers
        /// </summary>
        //==================================================================================
        internal override void StopOutputTransfers()
        {
            m_stopOutputTransferMutex.WaitOne();

            // if one thread already stop the tansfers then do nothing
            if (!m_stopOutputTransfers)
            {
                // abort all pending transferes
                if (m_winUsbHandle != IntPtr.Zero)
                    WinUsbInterop.WinUsb_AbortPipe(m_winUsbHandle, m_deviceInfo.EndPointOut);

                // set this flag so running threads can terminate
                m_stopOutputTransfers = true;
            }

            m_stopOutputTransferMutex.ReleaseMutex();
        }

        protected object queueInputTransferLock = new object();

        //==============================================================================================
        /// <summary>
        /// Synchronizes access to the bulk in buffer queue
        /// </summary>
        /// <param name="bulkInBuffer">The buffer to queue</param>
        /// <param name="queueAction">The actions to take (enqueue or dequeue)</param>
        /// <returns>The buffer that was dequeued</returns>
        //==============================================================================================
        protected byte[] QueueInputTransfer(byte[] bulkInBuffer, QueueAction queueAction)
        {
            lock(queueInputTransferLock)
            {
                if (queueAction == QueueAction.Enqueue)
                {
                    m_completedBulkInRequestBuffers.Enqueue(bulkInBuffer);
                    return null;
                }
                else
                {
                    if (m_completedBulkInRequestBuffers.Count > 0)
                        return m_completedBulkInRequestBuffers.Dequeue();
                    else
                        return null;
                }
            }
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
            
            uint bytesTransferred = 0;

            WindowsUsbSetupPacket packet = new WindowsUsbSetupPacket();
            packet.RequestType = m_statusPacket.RequestType;
            packet.Request = m_statusPacket.Request;
            packet.Value = m_statusPacket.Value;
            packet.Index = m_statusPacket.Index;
            packet.Length = m_statusPacket.Length;

            m_controlTransferMutex.WaitOne();

            WinUsbInterop.WinUsb_ControlTransfer(m_winUsbHandle,
                                                 packet,
                                                 m_statusBuffer,
                                                 (ushort)m_statusBuffer.Length,
                                                 ref bytesTransferred,
                                                 IntPtr.Zero);

            m_controlTransferMutex.ReleaseMutex();

            if ((m_statusBuffer[0] & 0x04) != 0)
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

            uint bytesTransferred = 0;

            WindowsUsbSetupPacket packet = new WindowsUsbSetupPacket();
            packet.RequestType = m_statusPacket.RequestType;
            packet.Request = m_statusPacket.Request;
            packet.Value = m_statusPacket.Value;
            packet.Index = m_statusPacket.Index;
            packet.Length = m_statusPacket.Length;

            m_controlTransferMutex.WaitOne();

            WinUsbInterop.WinUsb_ControlTransfer(m_winUsbHandle,
                                                 packet,
                                                 m_statusBuffer,
                                                 (ushort)m_statusBuffer.Length,
                                                 ref bytesTransferred,
                                                 IntPtr.Zero);

            m_controlTransferMutex.ReleaseMutex();

            if ((m_statusBuffer[0] & 0x10) != 0)
                errorCode = ErrorCodes.DataUnderrun;

            return errorCode;
        }

        //==================================================================
        /// <summary>
        /// Release the device and WinUsb handles
        /// </summary>
        //==================================================================
        internal override void ReleaseDevice()
        {
            if (m_deviceHandle != null)
            {
                m_deviceHandle.Close();
                m_deviceHandle = null;
            }

            if (m_winUsbHandle != IntPtr.Zero)
            {
                WinUsbInterop.WinUsb_Free(m_winUsbHandle);
                m_winUsbHandle = IntPtr.Zero;
            }
        }
    }
}
