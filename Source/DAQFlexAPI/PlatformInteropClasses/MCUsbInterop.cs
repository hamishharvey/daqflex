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
    internal unsafe class McUsbInterop : WindowsUsbInterop
    {
        public enum DeviceIOControlCodes : uint
        {
            //                          Device Type            Access          Function
            QueryInterfaceSettings =    ((uint)0x8001 << 16) | (0x0001 << 14) | (0x800 << 2),
            QueryPipe =                 ((uint)0x8001 << 16) | (0x0001 << 14) | (0x801 << 2),
            ControlTransfer =           ((uint)0x8001 << 16) | (0x0003 << 14) | (0x802 << 2),
            ReadPipe =                  ((uint)0x8001 << 16) | (0x0003 << 14) | (0x803 << 2),
            WritePipe =                 ((uint)0x8001 << 16) | (0x0002 << 14) | (0x804 << 2),
            AbortPipe =                 ((uint)0x8001 << 16) | (0x0003 << 14) | (0x805 << 2),
            ResetPipe =                 ((uint)0x8001 << 16) | (0x0003 << 14) | (0x806 << 2),
        }

        private const int MCWINUSB_CONTROL_TRANSFER_IN_BUFFER_SIZE = 13;
        private const int MCWINUSB_CONTROL_TRANSFER_OUT_BUFFER_SIZE = 5;
        private const int IOCTL_MCWINUSB_WRITE_PIPE_SIZE = 6;

        // Setup packet offsets
        private const int CTRL_IN_REQUEST_TYPE_OFFSET = 0;
        private const int CTRL_IN_REQUEST_OFFSET = 1;
        private const int CTRL_IN_WVALUE_OFFSET = 2;
        private const int CTRL_IN_WINDEX_OFFSET = 4;
        private const int CTRL_IN_WLENGTH_OFFSET = 6;

        // Length offset
        private const int CTRL_IN_MCLENGTH_OFFSET = 8;
        private const int CTRL_IN_BUFFER_OFFSET = 12;
        private const string DEVICE_INTERFACE_GUID = "{7AC4DE2F-80E2-4bf0-8EEE-F5D26055F956}";

        private IntPtr m_setupDeviceInfo;
        private SafeFileHandle m_deviceHandle;
        private byte[] m_statusBuffer;
        private UsbBulkInRequest m_lastInputRequestSubmitted;
                
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

        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool DeviceIoControl(SafeFileHandle hDevice,
                                                    DeviceIOControlCodes dwIoControlCode,
                                                    byte[] InBuffer,
                                                    int nInBufferSize,
                                                    byte[] OutBuffer,
                                                    int nOutBufferSize,
                                                    ref int pBytesReturned,
                                                    IntPtr overlapped);
        #endregion

        #region Setup API Interop

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetClassDevs", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(ref System.Guid classGuid, 
                                                          String enumerator, 
                                                          int hwndParent, 
                                                          int flags);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiEnumDeviceInterfaces", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfo, 
                                                                int deviceInfoData, 
                                                                ref Guid deviceGuid, 
                                                                int memberIndex, IntPtr deviceInterfaceData);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInterfaceDetail", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfo, 
                                                                    IntPtr deviceInterfaceData, 
                                                                    IntPtr deviceInterfaceDetailData, 
                                                                    int deviceInterfaceDetailDataSize, ref int requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", EntryPoint = "SetupDiDestroyDeviceInfoList", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int SetupDiDestroyDeviceInfoList(IntPtr deviceInfo);

        #endregion

        //====================================================================================================================================================================
        /// <summary>
        /// Gets the interface settings for the specified interface number
        /// </summary>
        /// <param name="deviceHandle">The device handle</param>
        /// <param name="alternateInterfaceNumber">The interface number</param>
        /// <param name="usbAltInterfaceDescriptor">An instance of UsbInterfaceDescriptor</param>
        /// <returns>True if the call succeeded otherwise false</returns>
        //====================================================================================================================================================================
        private bool McUsb_QueryInterfaceSettings(SafeFileHandle deviceHandle, 
                                                  byte alternateInterfaceNumber,  
                                                  ref UsbInterfaceDescriptor usbAltInterfaceDescriptor)
        {
            int bytesReturned = 0;

            byte[] inBuffer = new byte[]{alternateInterfaceNumber};
            byte[] outBuffer = new byte[sizeof(UsbInterfaceDescriptor)];

            bool result = DeviceIoControl(deviceHandle,
                                          DeviceIOControlCodes.QueryInterfaceSettings,
                                          inBuffer,                     
                                          inBuffer.Length,
                                          outBuffer,
                                          outBuffer.Length,
                                          ref bytesReturned,
                                          IntPtr.Zero);

            int bufferIndex = 0;
            usbAltInterfaceDescriptor.Length = outBuffer[bufferIndex++];
            usbAltInterfaceDescriptor.DescriptorType = outBuffer[bufferIndex++];
            usbAltInterfaceDescriptor.InterfaceNumber = outBuffer[bufferIndex++];
            usbAltInterfaceDescriptor.AlternateSetting = outBuffer[bufferIndex++];
            usbAltInterfaceDescriptor.NumEndpoints = outBuffer[bufferIndex++];
            usbAltInterfaceDescriptor.InterfaceClass = outBuffer[bufferIndex++];
            usbAltInterfaceDescriptor.InterfaceSubClass = outBuffer[bufferIndex++];
            usbAltInterfaceDescriptor.InterfaceProtocol = outBuffer[bufferIndex++];
            usbAltInterfaceDescriptor.Interface = outBuffer[bufferIndex];

            return result;
        }

        //====================================================================================================================================================================
        /// <summary>
        /// Gets the endpoint addresses for the specified interface number an pipe
        /// </summary>
        /// <param name="deviceHandle">The device handle</param>
        /// <param name="alternateInterfaceNumber">The interface number</param>
        /// <param name="pipeIndex">The pipe index</param>
        /// <param name="pipeInformation">An instance of UsbPipeInformation</param>
        /// <returns>True if the call succeeded otherwise false</returns>
        //====================================================================================================================================================================
        private bool McUsb_QueryPipe(SafeFileHandle deviceHandle, 
                                     byte alternateInterfaceNumber, 
                                     byte pipeIndex, 
                                     ref UsbPipeInformation pipeInformation)
        {
            int bytesReturned = 0;

            byte[] inBuffer = new byte[] { alternateInterfaceNumber, pipeIndex };
            byte[] outBuffer = new byte[sizeof(UsbPipeInformation)];

            bool result = DeviceIoControl(deviceHandle,
                                          DeviceIOControlCodes.QueryPipe,
                                          inBuffer,
                                          inBuffer.Length,
                                          outBuffer,
                                          outBuffer.Length,
                                          ref bytesReturned,
                                          IntPtr.Zero);

            pipeInformation.PipeType = (UsbPipeType)BitConverter.ToInt32(outBuffer, 0);
            pipeInformation.PipeId = (byte)BitConverter.ToChar(outBuffer, 4);
            pipeInformation.MaximumPacketSize = BitConverter.ToUInt16(outBuffer, 5);
            pipeInformation.Interval = (byte)BitConverter.ToChar(outBuffer, 6);

            return result;
        }

        //====================================================================================================================================================================
        /// <summary>
        /// Performs a control in or control out transfer
        /// </summary>
        /// <param name="deviceHandle">The device handle</param>
        /// <param name="setupPacket">A USB setup packet</param>
        /// <param name="buffer">The buffer containing data to send or to read back</param>
        /// <param name="bufferLength">The length of the buffer</param>
        /// <param name="lengthTransferred">The number of bytes transfered</param>
        /// <param name="overlapped">The overlapped struct</param>
        /// <returns>True if the call succeeded otherwise false</returns>
        //====================================================================================================================================================================
        private static bool McUsb_ControlTransfer(SafeFileHandle deviceHandle, WindowsUsbSetupPacket setupPacket, byte[] buffer, uint bufferLength, ref uint lengthTransferred, IntPtr overlapped)
        {
            int bytesReturned = 0;

            byte[] inBuffer;
            byte[] outBuffer;

            if (setupPacket.RequestType == ControlRequestType.VENDOR_CONTROL_IN)
            {
                // Device to host - Control In Transfer
                inBuffer = new byte[MCWINUSB_CONTROL_TRANSFER_IN_BUFFER_SIZE + 4];
                Array.Clear(inBuffer, 0, inBuffer.Length);

                inBuffer[0] = setupPacket.RequestType;
                inBuffer[1] = setupPacket.Request;
                inBuffer[2] = (byte)(setupPacket.Value & 0x00FF);
                inBuffer[3] = (byte)((setupPacket.Value >> 8) & 0x00FF);
                inBuffer[4] = (byte)(setupPacket.Index & 0x00FF);
                inBuffer[5] = (byte)((setupPacket.Index >> 8) & 0x00FF);

                outBuffer = new byte[MCWINUSB_CONTROL_TRANSFER_OUT_BUFFER_SIZE + bufferLength - 1];
                Array.Clear(outBuffer, 0, outBuffer.Length);
            }
            else
            {
                // Host to device - Control Out Transfer
                inBuffer = new byte[MCWINUSB_CONTROL_TRANSFER_IN_BUFFER_SIZE + bufferLength - 1];
                Array.Clear(inBuffer, 0, inBuffer.Length);

                inBuffer[0] = setupPacket.RequestType;
                inBuffer[1] = setupPacket.Request;
                inBuffer[2] = (byte)(setupPacket.Value & 0x00FF);
                inBuffer[3] = (byte)((setupPacket.Value >> 8) & 0x00FF);
                inBuffer[4] = (byte)(setupPacket.Index & 0x00FF);
                inBuffer[5] = (byte)((setupPacket.Index >> 8) & 0x00FF);

                // copy the length
                byte[] lengthParts = BitConverter.GetBytes(setupPacket.Length);
                Array.Copy(lengthParts, 0, inBuffer, CTRL_IN_MCLENGTH_OFFSET, lengthParts.Length);

                // copy the data
                Array.Copy(buffer, 0, inBuffer, CTRL_IN_BUFFER_OFFSET, bufferLength);

                outBuffer = new byte[MCWINUSB_CONTROL_TRANSFER_OUT_BUFFER_SIZE];
                Array.Clear(outBuffer, 0, outBuffer.Length);
            }

            bool result;

            if (deviceHandle != null)
            {
                result = DeviceIoControl(deviceHandle,
                                        DeviceIOControlCodes.ControlTransfer,
                                        inBuffer,
                                        inBuffer.Length,
                                        outBuffer,
                                        outBuffer.Length,
                                        ref bytesReturned,
                                        IntPtr.Zero);
            }
            else
            {
                result = false;
                System.Diagnostics.Debug.Assert(false, "Device handle is null");
            }

            // the number of bytes transferred is stored in the first four elements of outBuffer
            lengthTransferred = BitConverter.ToUInt32(outBuffer, 0);

            // get data from device to host transfer
            if (setupPacket.RequestType == ControlRequestType.VENDOR_CONTROL_IN)
                Array.Copy(outBuffer, 4, buffer, 0, lengthTransferred);

            return result;
        }

        //===========================================================================================
        /// <summary>
        /// Reads data from the bulk in pipe. This call is asynchronous.
        /// </summary>
        /// <param name="deviceHandle">The device handle</param>
        /// <param name="pipeID">The bulk in pipe id</param>
        /// <param name="buffer">The data buffer</param>
        /// <param name="bufferLength">The buffer length</param>
        /// <param name="lengthTransferred">The number of bytes transferred</param>
        /// <param name="overlapped">The overlapped struct</param>
        /// <returns>True if the call succeeded otherwise false</returns>
        //===========================================================================================
        private bool McUsb_ReadPipe(SafeFileHandle deviceHandle, 
                                     byte pipeID, 
                                     byte[] buffer, 
                                     uint bufferLength, 
                                     ref uint lengthTransferred, 
                                     IntPtr overlapped)
        {
            int bytesReturned = 0;
            byte[] inBuffer = new byte[]{pipeID};
            byte[] outBuffer = buffer;

            bool result = DeviceIoControl(deviceHandle,
                                          DeviceIOControlCodes.ReadPipe,
                                          inBuffer,
                                          inBuffer.Length,
                                          outBuffer,
                                          outBuffer.Length,
                                          ref bytesReturned,
                                          overlapped);

            return result;
        }

        //===========================================================================================
        /// <summary>
        /// Writes data to the bulk out pipe. This call is asynchronous.
        /// </summary>
        /// <param name="deviceHandle">The device handle</param>
        /// <param name="pipeID">The bulk out pipe id</param>
        /// <param name="buffer">The data buffer</param>
        /// <param name="bufferLength">The buffer length</param>
        /// <param name="lengthTransferred">The number of bytes transferred</param>
        /// <param name="overlapped">The overlapped struct</param>
        /// <returns>True if the call succeeded otherwise false</returns>
        //===========================================================================================
        private bool McUsb_WritePipe(SafeFileHandle deviceHandle, 
                                     byte pipeID, 
                                     byte[] buffer, 
                                     uint bufferLength, 
                                     ref uint lengthTransferred, 
                                     IntPtr overlapped)
        {
            int bytesReturned = 0;
            byte[] inBuffer = new byte[IOCTL_MCWINUSB_WRITE_PIPE_SIZE + buffer.Length - 1];

            unsafe
            {
                fixed (byte* pInBufferFixed = inBuffer, pBufferFixed = buffer)
                {
                    byte* pInBuffer = pInBufferFixed;
                    byte* pBuffer = pBufferFixed;

                    *pInBuffer++ = pipeID;
                    *(uint*)pInBuffer = bufferLength;
                    pInBuffer += sizeof(uint);

                    for (int i = 0; i < bufferLength; i++)
                        *pInBuffer++ = *pBuffer++;
                }
            }

            bool result = DeviceIoControl(deviceHandle,
                                          DeviceIOControlCodes.WritePipe,
                                          inBuffer,
                                          inBuffer.Length,
                                          null,
                                          0,
                                          ref bytesReturned,
                                          overlapped);

            return result;

        }

        //=========================================================================
        /// <summary>
        /// Aborts transfers on the specified pipe
        /// </summary>
        /// <param name="deviceHandle">The device handle</param>
        /// <param name="pipeID">The pipe id</param>
        /// <returns>True if the call succeeded otherwise false</returns>
        //=========================================================================
        private bool McUsb_AbortPipe(SafeFileHandle deviceHandle, 
                                     byte pipeID)
        {
            int bytesReturned = 0;
            byte[] inBuffer = new byte[] { pipeID };

            bool result = DeviceIoControl(deviceHandle,
                                          DeviceIOControlCodes.AbortPipe,
                                          inBuffer,
                                          inBuffer.Length,
                                          null,
                                          0,
                                          ref bytesReturned,
                                          IntPtr.Zero);

            return result;
        }

        //=========================================================================
        /// <summary>
        /// Resets the specified pipe
        /// </summary>
        /// <param name="deviceHandle">the device handle</param>
        /// <param name="pipeID">The pipe id</param>
        /// <returns>True if the call succeeded otherwise false</returns>
        //=========================================================================
        private bool McUsb_ResetPipe(SafeFileHandle deviceHandle, 
                                     byte pipeID)
        {
            int bytesReturned = 0;
            byte[] inBuffer = new byte[] { pipeID };

            bool result = DeviceIoControl(deviceHandle,
                                          DeviceIOControlCodes.ResetPipe,
                                          inBuffer,
                                          inBuffer.Length,
                                          null,
                                          0,
                                          ref bytesReturned,
                                          IntPtr.Zero);

            return result;
        }


        //=====================================================================================
        /// <summary>
        /// Default constructor used by the daq device manager before devices are detected
        /// </summary>
        //=====================================================================================
        internal McUsbInterop()
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
        internal McUsbInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
            : base(deviceInfo, criticalParams)
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
                if (McUsb_QueryInterfaceSettings(deviceHandle, 0, ref uid) == true)
                {
                    m_deviceInitialized = true;

                    // get the bulk pipes info
                    for (byte i = 0; i < uid.NumEndpoints; i++)
                    {
                        McUsb_QueryPipe(deviceHandle, 0, i, ref pipeInfo);

                        unsafe
                        {
                            if (pipeInfo.PipeType == UsbPipeType.Bulk)
                            {
                                if ((pipeInfo.PipeId & 0x80) == 0x80)
                                {
                                    deviceInfo.EndPointIn = pipeInfo.PipeId;

                                    deviceInfo.MaxPacketSize = pipeInfo.MaximumPacketSize;
                                }
                                else
                                {
                                    deviceInfo.EndPointOut = pipeInfo.PipeId;
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
            return CreateFile(devicePath,
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

            int byteRatio = m_criticalParams.DataInXferSize;
            int channelCount = m_criticalParams.AiChannelCount;

            for (int i = 0; i < m_numberOfWorkingInputRequests; i++)
            {
                UsbBulkInRequest request = new UsbBulkInRequest();
                request.Index = i;
                request.Overlapped = new Overlapped();

                if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
                {
                    request.Buffer = new byte[byteRatio * channelCount];

                    if (m_criticalParams.NumberOfSamplesForSingleIO > 1)
                    {
                            // use n bytes per channel...
                        request.BytesRequested = byteRatio * channelCount;
                    }
                    else
                    {
                            // only n bytes per transfer regardless of channel count...
                        request.BytesRequested = byteRatio;
                    }
                }
                else
                {
                    request.Buffer = new byte[transferSize];
                    request.BytesRequested = request.Buffer.Length;
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
            int bytesToCopyOnFirstPass;
            int bytesToCopyOnSecondPass;
            int sourceBufferLength = m_driverInterfaceOutputBuffer.Length;


            for (int i = 0; i < m_numberOfWorkingOutputRequests; i++)
            {
                UsbBulkOutRequest request = new UsbBulkOutRequest();
                request.Index = i;
                request.Overlapped = new Overlapped();
                request.Buffer = new byte[transferSize];

                if (m_criticalParams.OutputSampleMode == SampleMode.Continuous)
                {
                    // for continuous mode make each request the transfer size
                    bytesRequested = transferSize;
                }
                else
                {
                    // for finite mode make each request the transfer size except maybe not the last one
                    // the last request may be a short transfer
                    if (m_totalNumberOfOutputBytesRequested - byteCount > transferSize)
                        bytesRequested = transferSize;
                    else
                        bytesRequested = m_totalNumberOfOutputBytesRequested - byteCount;
                }

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

                if ((m_driverInterfaceOutputBufferIndex + br.BytesRequested) >= sourceBufferLength)
                {
                    // two passes are required since the current input scan write index
                    // wrapped around to the beginning of the internal read buffer
                    bytesToCopyOnFirstPass = sourceBufferLength - m_driverInterfaceOutputBufferIndex;
                    bytesToCopyOnSecondPass = br.BytesRequested - bytesToCopyOnFirstPass;
                }
                else
                {
                    // only one pass is required since the current input scan write index
                    // did not wrap around
                    bytesToCopyOnFirstPass = br.BytesRequested;
                    bytesToCopyOnSecondPass = 0;
                }

                if (bytesToCopyOnFirstPass > 0)
                    Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, br.Buffer, 0, bytesToCopyOnFirstPass);

                m_driverInterfaceOutputBufferIndex += bytesToCopyOnFirstPass;

                // reset the index to the begining of the buffer
                if (m_driverInterfaceOutputBufferIndex >= m_driverInterfaceOutputBuffer.Length)
                    m_driverInterfaceOutputBufferIndex = 0;

                if (bytesToCopyOnSecondPass > 0)
                    Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, br.Buffer, bytesToCopyOnFirstPass, bytesToCopyOnSecondPass);

                m_driverInterfaceOutputBufferIndex += bytesToCopyOnSecondPass;

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
        /// The data in the UsbBulkOutRequest objects are used to fill the device's FIFO
        /// before sending the AOSCAN:START command
        /// </summary>
        //===================================================================================
        protected override void QueueBulkOutRequests(double rate)
        {
            m_totalNumberOfOutputBytesTransferred = 0;

            for (int i = 0; i < m_numberOfQueuedOutputRequests; i++)
            {
                UsbBulkOutRequest request = m_bulkOutRequests[i];
                bool result = SubmitBulkOutRequest(request);

                if (!result)
                    break;
            }

            int numberOfOutputRequestsToWaitFor = Math.Min(m_numberOfQueuedOutputRequests, (int)Math.Floor((double)m_criticalParams.OutputFifoSize / (double)m_criticalParams.BulkOutXferSize));

            while (m_numberOfOutputRequestsCompleted < numberOfOutputRequestsToWaitFor && m_errorCode == ErrorCodes.NoErrors)
            {
                if (m_stopOutputTransfers || m_errorCode != ErrorCodes.NoErrors)
                    break;

                Thread.Sleep(0);
            }
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to clear the total number of bytes transferred
        /// </summary>
        /// <param name="scanRate">The device scan rate</param>
        /// <param name="totalNumberOfBytes">The total number of bytes to transfer</param>
        /// <param name="transferSize">The number of bytes in each transfer request</param>
        //===========================================================================================
        internal override void PrepareInputTransfers(double scanRate, int totalNumberOfBytes, int transferSize)
        {
            McUsb_ResetPipe(m_deviceHandle, m_deviceInfo.EndPointIn);

            m_totalNumberOfInputBytesTransferred = 0;

            base.PrepareInputTransfers(scanRate, totalNumberOfBytes, transferSize);

            m_dataReceivedDebugList.Clear();
            m_dataSubmittedDebugList.Clear();

            for (int i = 0; i < m_numberOfQueuedInputRequests; i++)
            {
                m_dataSubmittedDebugList.Add(String.Format(">>>>> Submitting request {0}", i));
            }
        }

        //===========================================================================================
        /// <summary>
        /// Sets up parameters for bulk in transfers
        /// </summary>
        /// <param name="scanRate">The device scan rate</param>
        /// <param name="totalNumberOfBytes">The total number of bytes to transfer</param>
        /// <param name="transferSize">The number of bytes in each transfer request</param>
        //===========================================================================================
        internal override void PrepareOutputTransfers(double scanRate, int totalNumberOfBytes, int transferSize)
        {
            McUsb_ResetPipe(m_deviceHandle, m_deviceInfo.EndPointOut);

            base.PrepareOutputTransfers(scanRate, totalNumberOfBytes, transferSize);
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
                m_inputScanErrorCode = ErrorCodes.BulkInputTransfersNotSupported;
                return false;
            }

            if (m_criticalParams.InputSampleMode == SampleMode.Finite)
            {
                // for finite mode, the number of bytes in the last transfer may need to be reduced so that the
                // number of bytes transfered equals the number of bytes requested
                if (m_totalNumberOfInputBytesTransferred + request.BytesRequested > m_totalNumberOfInputBytesRequested)
                {
                    request.BytesRequested = (m_totalNumberOfInputBytesRequested - m_totalNumberOfInputBytesTransferred);
                }
            }

            m_totalNumberOfInputBytesTransferred += request.BytesRequested;

            //DebugLogger.WriteLine("Submitting bulk in index# {0}, request# {1}  ", request.Index, request.RequestNumber);

            result = McUsb_ReadPipe(m_deviceHandle,
                                    m_deviceInfo.EndPointIn,
                                    request.Buffer,
                                    (uint)request.BytesRequested,
                                    ref lengthTransfered,
                                    request.NativeOverLappedIntPtr);

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
                int errorCode = GetLastError();

                // error 997 is Overlapped I/O operation is in progress (so this is good).
                if (errorCode == 997)
                {
                    m_inputScanErrorCode = ErrorCodes.NoErrors;
                    result = true;
                }
                else
                {
                    m_inputScanErrorCode = ErrorCodes.UnknownError;
                }
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
                return false;
            }

            //DebugLogger.WriteLine("submitting bulk out transfer # {0} - {1} bytes", request.Index, request.BytesRequested);

            result = McUsb_WritePipe(m_deviceHandle,
                                     m_deviceInfo.EndPointOut,
                                     request.Buffer,
                                     (uint)request.BytesRequested,
                                     ref lengthTransfered,
                                     request.NativeOverLappedIntPtr);

            m_numberOfOutputRequestsSubmitted++;

            if (!result)
            {
                int errorCode = GetLastError();

                // error 997 is Overlapped I/O operation is in progress (so this is good).
                if (errorCode == 997)
                {
                    m_totalNumberOfOutputBytesTransferred += request.BytesRequested;
                    result = true;
                }
            }

            return result;
        }

        private List<string> m_dataSubmittedDebugList = new List<string>();
        private List<string> m_dataReceivedDebugList = new List<string>();

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
            Monitor.Enter(m_inputTransferCompletionLock);

            m_numberOfInputRequestsCompleted++;

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

                            DebugLogger.WriteLine("Received request - index # {0}, value = {1}", bulkInRequest.Index, bulkInRequest.Buffer[0] + ( 256 * bulkInRequest.Buffer[1]));

                                // get a buffer off the Ready Queue...
                            BulkInBuffer bulkInBuffer = null;

                            while (bulkInBuffer == null)
                            {
                                bulkInBuffer = QueueBulkInReadyBuffers(null, QueueAction.Dequeue);

                                if (bulkInBuffer == null)
                                {
                                    Thread.Sleep(1);
                                }
                            }

                                // copy the index...
                            bulkInBuffer.Index = bulkInRequest.Index;

                                // copy the data...
                            Array.Copy(bulkInRequest.Buffer, bulkInBuffer.Data, numBytes);

                                // set the number of bytes...
                            bulkInBuffer.Length = (int)numBytes;

                            //DebugLogger.WriteLine("Expected transfer index = {0}", m_expectedInputTransferIndex);

                            if (bulkInRequest.Index == m_expectedInputTransferIndex)
                            {
                                //DebugLogger.WriteLine("Putting buffer {0} onto the completed queue", bulkInBuffer.Index);
                                
                                    // if this is the correct index then add the buffer to the Completed Queue...
                                //DebugLogger.WriteLine("Queueing buffer {0} into the completed buffer queue", bulkInBuffer.Index);
                                QueueBulkInCompletedBuffers(bulkInBuffer, QueueAction.Enqueue);

                                //    // log the expected index...
                                //DebugLogger.WriteLine("Expected index = {0}", m_expectedInputTransferIndex);

                                    // increment the expected index...
                                m_expectedInputTransferIndex++;

                                    // reset it when it reaches the number of working input requests...
                                if (m_expectedInputTransferIndex == m_numberOfWorkingInputRequests)
                                {
                                    m_expectedInputTransferIndex = 0;
                                }
                            }
                            else
                            {
                                //*********************************************************************************
                                // the temporary buffer is used to manage data packets that may be out of order
                                // some devices single io mode may transfer unordered packets
                                //*********************************************************************************
                                
                                    // add it to the temporary buffer...
                                m_temporaryBuffer.Add(bulkInBuffer);

                                System.Diagnostics.Debug.WriteLine(String.Format("packets out of order index = {0}, expected index ={1}", bulkInRequest.Index, m_expectedInputTransferIndex));
                                //System.Diagnostics.Debug.Assert(bulkInRequest.Index == m_expectedInputTransferIndex, "[DAQFlex.MCUsbInterop] Bulk transfer packet is out of order");
                            }

                            if (m_temporaryBuffer.Count > 0)
                            {
                                //return;

                                // keep the following code here in case we need to use it later on...

                                    // let's see if there's a buffer whose index is the expected index in the temporary buffer...
                                BulkInBuffer nextBuffer = m_temporaryBuffer.Find(NextBuffer(m_expectedInputTransferIndex));

                                if (nextBuffer != null)
                                {
                                        // now put it in the completed queue...
                                    //DebugLogger.WriteLine("Queueing buffer {0}", nextBuffer.Index);
                                    QueueBulkInCompletedBuffers(nextBuffer, QueueAction.Enqueue);

                                        // now take if out of the list...
                                    m_temporaryBuffer.Remove(nextBuffer);

                                        // increment the expected index...
                                    m_expectedInputTransferIndex++;

                                        // reset it when it reaches the number of working input requests...
                                    if (m_expectedInputTransferIndex == m_numberOfWorkingInputRequests)
                                    {
                                        m_expectedInputTransferIndex = 0;
                                    }
                                }
                            }

                            break;
                        }
                    }

                    if (bulkInRequest != null)
                    {
                        if (m_criticalParams.InputSampleMode == SampleMode.Continuous)
                        {
                            // for continuous mode 

                            // if numBytes is less than m_criticalParams.BulkInXferSize then the scan was stopped
                            // so we don't want to submit a bulk in request in that case
                            if (!m_stopInputTransfers && numBytes == m_criticalParams.BulkInXferSize)
                            {
                                SubmitBulkInRequest(m_lastInputRequestSubmitted.Next);
                            }
                        }
                        else
                        {
                            // for finite mode

                            if (!m_stopInputTransfers && (m_numberOfInputRequestsSubmitted < m_totalNumberOfInputRequests))
                            {
                                //if (m_totalNumberOfInputBytesRequested - m_totalNumberOfInputBytesTransferred < m_lastInputRequestSubmitted.BytesRequested)
                                //    m_lastInputRequestSubmitted.Next.BytesRequested = m_totalNumberOfInputBytesRequested - m_totalNumberOfInputBytesTransferred;

                                // Finite scan not complete yet
                                //DebugLogger.WriteLine("Submitting request {0}", m_lastInputRequestSubmitted.Next.Index);

                                SubmitBulkInRequest(m_lastInputRequestSubmitted.Next);
                            }
                        }
                    }
                }
            }
            else
            {
                // error 995 is operation aborted which can occur when a scan is stopped
                if (errorCode != 995)
                {
                    m_inputScanErrorCode = CheckOverrun();

                    if (m_inputScanErrorCode == ErrorCodes.DataOverrun)
                    {
                        ClearStall(m_deviceInfo.EndPointIn, m_aiScanResetPacket);
                    }
                    else
                    {
                        // if not an overrun then check other codes
                        if (errorCode == 31)
                        {
                            m_inputScanErrorCode = ErrorCodes.DeviceNotResponding;
                        }
                        else
                        {
                            m_inputScanErrorCode = ErrorCodes.UsbBulkReadError;
                        }
                    }
                }
            }

            Monitor.Exit(m_inputTransferCompletionLock);
        }


        static Predicate<BulkInBuffer> NextBuffer(int expectedIndex)
        {
            return delegate(BulkInBuffer buffer)
            {
                return buffer.Index == expectedIndex;
            };
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
            Monitor.Enter(m_outputTransferCompletionLock);

            m_numberOfOutputRequestsCompleted++;

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
                        bytesToTransfer = availableRequest.BytesRequested;

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

                //DebugLogger.WriteLine("Completed bulk out transfer # {0} - {1} bytes, errorCode {2}", availableRequest.Index, numBytes, errorCode);

                // continue submitting more bulk output requests after the initial queued requests have completed
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

                        // reset the index to the begining of the buffer
                        if (m_driverInterfaceOutputBufferIndex >= m_driverInterfaceOutputBuffer.Length)
                            m_driverInterfaceOutputBufferIndex = 0;

                        if (bytesToCopyOnSecondPass > 0)
                            Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, availableRequest.Buffer, bytesToCopyOnFirstPass, bytesToCopyOnSecondPass);

                        m_driverInterfaceOutputBufferIndex += bytesToCopyOnSecondPass;

                        // once the queued number of bulk transfers have completed start resubmitting the remaining transfers
                        SubmitBulkOutRequest(availableRequest);
                    }
                }
            }
            else
            {
                // error 995 is operation aborted which can occur when a scan is stopped
                if (errorCode != 995)
                {
                    m_outputScanErrorCode = CheckUnderrun();

                    if (m_outputScanErrorCode != ErrorCodes.DataUnderrun)
                    {
                        // if not an overrun then check other codes
                        if (errorCode == 31)
                        {
                            m_outputScanErrorCode = ErrorCodes.DeviceNotResponding;
                        }
                        else
                        {
                            m_outputScanErrorCode = ErrorCodes.UsbBulkWriteError;
                        }
                    }
                }
            }

            Monitor.Exit(m_outputTransferCompletionLock);
        }

        //===================================================================================================
        /// <summary>
        /// Indicates if all input requests that were submitted have completed
        /// </summary>
        /// <returns>true if the number of requests completed equals the number
        /// of requests submitted, otherwise false</returns>
        //===================================================================================================
        internal override bool InputScanComplete()
        {
            if (m_criticalParams.InputSampleMode == SampleMode.Finite)
            {
                if (m_totalNumberOfInputBytesTransferred == m_totalNumberOfInputBytesRequested)
                {
                    if (m_numberOfInputRequestsCompleted == (m_numberOfInputRequestsSubmitted - 1))
                        McUsb_AbortPipe(m_deviceHandle, m_deviceInfo.EndPointIn);
                }
            }
            else
            {
                McUsb_AbortPipe(m_deviceHandle, m_deviceInfo.EndPointIn);
            }

            return true;
        }

        //===================================================================================================================
        /// <summary>
        /// Fills a list with usb device information
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //====================================================================================================================
        internal override ErrorCodes GetUsbDevices(Dictionary<int, DeviceInfo> deviceInfoList, DeviceListUsage deviceListUsage)
        {
            if (deviceListUsage == DeviceListUsage.UpdateList)
                return UpdateDeviceInfoList(deviceInfoList);

            bool lastDevice = true;
            string[] pathParts = null;
            int deviceNumber = 0;
            string devicePath = string.Empty;
            int sizeOfDeviceInterfaceData;
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_setupDeviceInfo = IntPtr.Zero;

            Guid deviceGuid = new Guid(DEVICE_INTERFACE_GUID);

            if (deviceListUsage == DeviceListUsage.RefreshList)
                deviceInfoList.Clear();

            if (deviceInfoList.Count == 0)
            {
                // get a handle to the device information set using the device GUID
                m_setupDeviceInfo = SetupDiGetClassDevs(ref deviceGuid, null, 0, (int)(UsbDeviceConfigInfoFlags.Present | UsbDeviceConfigInfoFlags.DeviceInterface));

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
                        bool result = SetupDiEnumDeviceInterfaces(m_setupDeviceInfo, 0, ref deviceGuid, memberIndex, deviceInterfaceData);

                        if (result == true)
                        {
                            lastDevice = false;

                            int requiredLength = 0;

                            // first call to GetDeviceInterfaceDetail will return the required length for the detail buffer
                            result = SetupDiGetDeviceInterfaceDetail(m_setupDeviceInfo, deviceInterfaceData, IntPtr.Zero, 0, ref requiredLength, IntPtr.Zero);

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

                                    result = SetupDiGetDeviceInterfaceDetail(m_setupDeviceInfo, deviceInterfaceData, detailBuffer, requiredLength, ref requiredLength, IntPtr.Zero);

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
                                        di.DisplayName = DaqDeviceManager.GetDeviceName(di.Pid);
                                        di.SerialNumber = pathParts[2];

                                        if (Enum.IsDefined(typeof(DeviceIDs), di.Pid))
                                        {
                                            deviceInfoList.Add(deviceNumber, di);
                                            deviceNumber++;
                                        }

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
                        }

                    } while (!lastDevice);

                    // free the unmanaged memory pointed to by detailBuffer
                    Marshal.FreeHGlobal(deviceInterfaceData);

                    SetupDiDestroyDeviceInfoList(m_setupDeviceInfo);
                }
            }

            return errorCode;
        }

        //===================================================================================================================
        /// <summary>
        /// Fills a list with usb device information
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //====================================================================================================================
        internal ErrorCodes UpdateDeviceInfoList(Dictionary<int, DeviceInfo> deviceInfoList)
        {
            bool lastDevice = true;
            string[] pathParts = null;
            int deviceNumber;
            string devicePath = string.Empty;
            int sizeOfDeviceInterfaceData;
            ErrorCodes errorCode = ErrorCodes.NoErrors;
            List<int> existingDevicesDetected = new List<int>();
            SortedDictionary<int, DeviceInfo> existingDevices = new SortedDictionary<int, DeviceInfo>();
            SortedDictionary<int, DeviceInfo> newDevices = new SortedDictionary<int, DeviceInfo>();
            
            m_setupDeviceInfo = IntPtr.Zero;

            Guid deviceGuid = new Guid(DEVICE_INTERFACE_GUID);

            //*********************************************
            // temporarily store the existing devices
            //*********************************************

            foreach (KeyValuePair<int, DeviceInfo> kvp in deviceInfoList)
            {
                existingDevices.Add(kvp.Value.DeviceNumber, kvp.Value);
            }

            deviceNumber = existingDevices.Count;

            //*********************************************
            // detect devices
            //*********************************************

            // get a handle to the device information set using the device GUID
            m_setupDeviceInfo = SetupDiGetClassDevs(ref deviceGuid, null, 0, (int)(UsbDeviceConfigInfoFlags.Present | UsbDeviceConfigInfoFlags.DeviceInterface));

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
                    bool result = SetupDiEnumDeviceInterfaces(m_setupDeviceInfo, 0, ref deviceGuid, memberIndex, deviceInterfaceData);

                    if (result == true)
                    {
                        lastDevice = false;

                        int requiredLength = 0;

                        // first call to GetDeviceInterfaceDetail will return the required length for the detail buffer
                        result = SetupDiGetDeviceInterfaceDetail(m_setupDeviceInfo, deviceInterfaceData, IntPtr.Zero, 0, ref requiredLength, IntPtr.Zero);

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

                                result = SetupDiGetDeviceInterfaceDetail(m_setupDeviceInfo, deviceInterfaceData, detailBuffer, requiredLength, ref requiredLength, IntPtr.Zero);

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
                                    di.DisplayName = DaqDeviceManager.GetDeviceName(di.Pid);
                                    di.SerialNumber = pathParts[2];

                                    if (Enum.IsDefined(typeof(DeviceIDs), di.Pid))
                                    {
                                        bool addToList = true;

                                        foreach (KeyValuePair<int, DeviceInfo> kvp in existingDevices)
                                        {
                                            int devNum = kvp.Value.DeviceNumber;

                                            if (di.Pid == existingDevices[devNum].Pid && di.SerialNumber == existingDevices[devNum].SerialNumber)
                                            {
                                                existingDevicesDetected.Add(existingDevices[devNum].DeviceNumber);
                                                addToList = false;
                                                break;
                                            }
                                        }

                                        // if it's not in the existing list add it to the new list
                                        if (addToList)
                                        {
                                            newDevices.Add(deviceNumber, di);
                                            deviceNumber++;
                                        }
                                    }

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
                    }

                } while (!lastDevice);

                // free the unmanaged memory pointed to by detailBuffer
                Marshal.FreeHGlobal(deviceInterfaceData);

                SetupDiDestroyDeviceInfoList(m_setupDeviceInfo);
            }

            // check if a device in the existing list was not detected.
            if (existingDevicesDetected.Count != existingDevices.Count)
            {
                List<int> deviceNumbersToRemove = new List<int>();

                foreach (KeyValuePair<int, DeviceInfo> kvp in existingDevices)
                {
                    if (!existingDevicesDetected.Contains(kvp.Value.DeviceNumber))
                        deviceNumbersToRemove.Add(kvp.Value.DeviceNumber);
                }

                // remove any undetected devices
                foreach (int i in deviceNumbersToRemove)
                {
                    existingDevices.Remove(i);
                }
            }


            // finally rebuild the deviceInfoList
            deviceInfoList.Clear();

            deviceNumber = 0;

            // add one each from the existing devices
            foreach (KeyValuePair<int, DeviceInfo> kvp in existingDevices)
            {
                    // update the device number...
                kvp.Value.DeviceNumber = deviceNumber;

                    // add it to the device info list...
                deviceInfoList.Add(deviceNumber, kvp.Value);

                    // next device number...
                deviceNumber++;
            }

            // add one each from the new devices
            foreach (KeyValuePair<int, DeviceInfo> kvp in newDevices)
            {
                    // update the device number...
                kvp.Value.DeviceNumber = deviceNumber;

                    // add it to the device info list...
                deviceInfoList.Add(deviceNumber, kvp.Value);

                    // next device number...
                deviceNumber++;
            }

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

            //ushort count = 64;
            WindowsUsbSetupPacket winUsbPacket = new WindowsUsbSetupPacket();
            winUsbPacket.RequestType = ControlRequestType.VENDOR_CONTROL_IN;
            winUsbPacket.Request = packet.Request;
            winUsbPacket.Value = packet.Value;
            winUsbPacket.Index = packet.Index;
            winUsbPacket.Length = packet.Length;

            uint bytesTransfered = 0;

            //System.Diagnostics.Debug.WriteLine(String.Format("Starting Control In transfer on thread {0}", Thread.CurrentThread.ManagedThreadId));

            try
            {
                result = McUsb_ControlTransfer(m_deviceHandle,
                                               winUsbPacket,
                                               packet.Buffer,
                    //count,
                                               packet.Length,
                                               ref bytesTransfered,
                                               IntPtr.Zero);

                //System.Diagnostics.Debug.WriteLine(String.Format("Completed Control In transfer on thread {0}", Thread.CurrentThread.ManagedThreadId));
            }
            catch (Exception)
            {
                return ErrorCodes.UsbIOError;
            }

            packet.BytesTransfered = bytesTransfered;

            if (result == true)
            {
                return ErrorCodes.NoErrors;
            }
            else
            {
                int lastError = GetLastError();

                if (lastError == 22 || lastError == 1176)
                    return ErrorCodes.DeviceNotResponding;
                else if (lastError == 2 || lastError == 31)
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

            //System.Diagnostics.Debug.WriteLine(String.Format("Starting Control Out transfer on thread {0}", Thread.CurrentThread.ManagedThreadId));

            try
            {
                result = McUsb_ControlTransfer(m_deviceHandle,
                                               winUsbPacket,
                                               packet.Buffer,
                                               (ushort)winUsbPacket.Length,
                                               ref bytesTransfered,
                                               IntPtr.Zero);

                //System.Diagnostics.Debug.WriteLine(String.Format("Completed Control Out transfer on thread {0}", Thread.CurrentThread.ManagedThreadId));
            }
            catch (Exception)
            {
                return ErrorCodes.UsbIOError;
            }

            packet.BytesTransfered = bytesTransfered;

            if (result == true)
            {
                return ErrorCodes.NoErrors;
            }
            else
            {
                int lastError = GetLastError();

                if (lastError == 22 || lastError == 1176)
                    return ErrorCodes.DeviceNotResponding;
                else if (lastError == 2 || lastError == 31)
                    return ErrorCodes.InvalidMessage;
                else if (lastError == 6)
                    return ErrorCodes.InvalidDeviceHandle;

                System.Diagnostics.Debug.Assert(false, String.Format("Unknown Error Code: {0}", lastError));
                return ErrorCodes.UnknownError;
            }
        }

        //=============================================================================================
        /// <summary>
        /// Method for a USB Bulk IN request
        /// </summary>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <param name="bytesRequested">The number of bytes to requested</param>
        /// <param name="bytesReceived">The number of actual bytes received</param>
        /// <returns>The result</returns>
        //=============================================================================================
        internal override ErrorCodes UsbBulkInRequest(ref BulkInBuffer buffer, ref uint bytesReceived)
        {
            BulkInBuffer bulkInBuffer = null;

            do
            {
                if (m_inputScanErrorCode != ErrorCodes.NoErrors)
                    break;

                bulkInBuffer = QueueBulkInCompletedBuffers(null, QueueAction.Dequeue);

                if (bulkInBuffer == null)
                    Thread.Sleep(1);


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

            return m_inputScanErrorCode;
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

            McUsb_ResetPipe(m_deviceHandle, pipe);
        }

        //==================================================================================
        /// <summary>
        /// Stops bulk in transfers by aborting the pending transfers
        /// </summary>
        //==================================================================================
        internal override void StopInputTransfers()
        {
            Monitor.Enter(m_stopInputTransferLock);

            // if one thread already stop the tansfers then do nothing
            if (!m_stopInputTransfers)
            {
                // set this flag so running threads can terminate
                m_stopInputTransfers = true;

                Thread.Sleep(100);

                // abort all pending transferes
                if (!m_deviceHandle.IsInvalid)
                {
                    McUsb_AbortPipe(m_deviceHandle, m_deviceInfo.EndPointIn);
                    //DebugLogger.WriteLine("Aborted pending input transfers");
                }
            }

            Monitor.Exit(m_stopInputTransferLock);
        }

        //==================================================================================
        /// <summary>
        /// Stops bulk out transfers by aborting the pending transfers
        /// </summary>
        //==================================================================================
        internal override void StopOutputTransfers()
        {
            Monitor.Enter(m_stopOutputTransferLock);

            // if one thread already stop the tansfers then do nothing
            if (!m_stopOutputTransfers)
            {
                // set this flag so running threads can terminate
                m_stopOutputTransfers = true;

                Thread.Sleep(100);

                // abort all pending transferes
                if (!m_deviceHandle.IsInvalid)
                {
                    McUsb_AbortPipe(m_deviceHandle, m_deviceInfo.EndPointOut);
                    //DebugLogger.WriteLine("Output transfers aborted");
                }

            }

            Monitor.Exit(m_stopOutputTransferLock);
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
                // first abort any pending bulk transfers
                if (m_deviceInfo != null)
                {
                    McUsb_AbortPipe(m_deviceHandle, m_deviceInfo.EndPointIn);

                    while (m_numberOfInputRequestsCompleted != m_numberOfInputRequestsSubmitted)
                        Thread.Sleep(0);

                    McUsb_AbortPipe(m_deviceHandle, m_deviceInfo.EndPointOut);

                    while (m_numberOfOutputRequestsCompleted != m_numberOfOutputRequestsSubmitted)
                        Thread.Sleep(0);
                }

                m_deviceHandle.Close();
                m_deviceHandle = null;
            }
        }
    }
}
