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
using System.Runtime.InteropServices;

namespace MeasurementComputing.DAQFlex
{
	internal unsafe class UsbDeviceDescriptor
	{
		internal IntPtr ptr;
		internal byte length;
		internal byte type;
		internal ushort bcdUsb;
		internal byte deviceClass;
		internal byte deviceSubClass;
		internal byte deviceProtocol;
		internal byte maxPacketSize;
		internal ushort vendorID;
		internal ushort deviceID;
		internal ushort bcdDevice;
		internal byte manufacturer;
		internal byte iProduct;
		internal byte serialNumber;
		internal byte numConfigs;
		
		internal UsbDeviceDescriptor(IntPtr pDeviceDescriptor)
		{
			ptr = pDeviceDescriptor;
			
			byte* pb = (byte*)ptr.ToPointer();
			
			length = *pb++;
			type = *pb++;
			bcdUsb = PtrManager.GetUInt16(ref pb);
			deviceClass = *pb++;
			deviceSubClass = *pb++;
			deviceProtocol = *pb++;
			maxPacketSize = *pb++;
			vendorID = PtrManager.GetUInt16(ref pb);
			deviceID = PtrManager.GetUInt16(ref pb);
			bcdDevice = PtrManager.GetUInt16(ref pb);
			manufacturer = *pb++;
			iProduct = *pb++;
			serialNumber = *pb++;
			numConfigs = *pb;
		}
	}

    internal unsafe class LibUsbInterop : SynchronousUsbInterop
    {
		protected const ushort VID = 0x9DB;

        protected IntPtr m_devHandle;
        protected static Mutex m_deviceChangeMutex = new Mutex();

		private byte[] m_statusBuffer = new byte[2];

        #region entry points for libusb

        private const string LIB_USB = "libusb-1.0.0.dll";

		[DllImport(LIB_USB, EntryPoint = "libusb_init", CharSet = CharSet.Auto)]
		internal static extern int LibUsbInit(IntPtr ctx);
		
		[DllImport(LIB_USB, EntryPoint = "libusb_exit", CharSet = CharSet.Auto)]
		internal static extern int LibUsbExit(IntPtr ctx);
		
		[DllImport(LIB_USB, EntryPoint = "libusb_set_debug", CharSet = CharSet.Auto)]
		internal static extern void LibUsbSetDebug(IntPtr ctx, int level);
		
		[DllImport(LIB_USB, EntryPoint = "libusb_claim_interface", CharSet = CharSet.Auto)]
		internal static extern int LibUsbClaimInterface(IntPtr devHandle, int iface);

		[DllImport(LIB_USB, EntryPoint = "libusb_release_interface", CharSet = CharSet.Auto)]
		internal static extern int LibUsbReleaseInterface(IntPtr devHandle, int iface);

		[DllImport(LIB_USB, EntryPoint = "libusb_control_transfer", CharSet = CharSet.Auto)]
        internal static extern int LibUsbControlTransfer(IntPtr devHandle, byte requestType, byte request, ushort value, ushort index, byte[] buffer, ushort length, uint timeout);

		[DllImport(LIB_USB, EntryPoint = "libusb_get_device_list", CharSet = CharSet.Auto)]
		internal static extern int LibUsbGetDeviceList(IntPtr ctx, int*** device);

		[DllImport(LIB_USB, EntryPoint = "libusb_get_device_descriptor", CharSet = CharSet.Auto)]
		internal static extern int LibUsbGetDeviceDescriptor(IntPtr dev, IntPtr desc);

		[DllImport(LIB_USB, EntryPoint = "libusb_open", CharSet = CharSet.Auto)]
		internal static extern int LibUsbOpen(IntPtr dev, int** devHandle);

        [DllImport(LIB_USB, EntryPoint = "libusb_close", CharSet = CharSet.Auto)]
        internal static extern int LibUsbClose(IntPtr devHandle);

        [DllImport(LIB_USB, EntryPoint = "libusb_bulk_transfer", CharSet = CharSet.Auto)]
        internal static extern int LibUsbBulkTransfer(IntPtr devHandle, byte ep, byte[] buffer, int length, ref int lengthXfered, uint timeOut);

		[DllImport(LIB_USB, EntryPoint = "libusb_clear_halt", CharSet = CharSet.Auto)]
        internal static extern int LibUsbClearHalt(IntPtr devHandle, byte ep);

        #endregion

        //=====================================================================================
        /// <summary>
        /// Default constructor used by the daq device manager before devices are detected
        /// </summary>
        //=====================================================================================
        internal LibUsbInterop()
            : base()
        {
            m_devHandle = IntPtr.Zero;
			
			m_sernoMessage[0] = 63;
			m_sernoMessage[1] = 68;
			m_sernoMessage[2] = 69;
			m_sernoMessage[3] = 86;
			m_sernoMessage[4] = 58;
			m_sernoMessage[5] = 77;
			m_sernoMessage[6] = 70;
			m_sernoMessage[7] = 71;
			m_sernoMessage[8] = 83;
			m_sernoMessage[9] = 69;
			m_sernoMessage[10] = 82;
        }

        //=====================================================================================
        /// <summary>
        /// Device-specific constructor used by the driver interface
        /// </summary>
        /// <param name="deviceNumber">The device number</param>
        //=====================================================================================
        internal LibUsbInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
            : base(deviceInfo, criticalParams)
        {
            InitializeDevice(m_deviceInfo);

            if (m_errorCode == ErrorCodes.NoErrors && !m_deviceInitialized)
                m_errorCode = ErrorCodes.DeviceNotInitialized;

			m_maxTransferSize = 8192;
			
#if DEBUG
			//LibUsbInterop.LibUsbSetDebug(IntPtr.Zero, 3);
#endif
        }

        //======================================================================================
        /// <summary>
        /// Releases the libusb interface and frees the device descriptor
        /// </summary>
        //======================================================================================
        ~LibUsbInterop()
        {
            ReleaseDevice();
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
            m_deviceChangeMutex.WaitOne();

            // initialize lib_usb
			int result;
			
			try
			{
				result = LibUsbInterop.LibUsbInit(IntPtr.Zero);

                if (result != 0)
                    m_errorCode = ErrorCodes.LibusbCouldNotBeInitialized;
			}
			catch (Exception ex)
			{
				if (ex is DllNotFoundException)
				{
					m_errorCode = ErrorCodes.LibusbCouldNotBeLoaded;
				}
				else
				{
                    m_errorCode = ErrorCodes.LibusbCouldNotBeInitialized;
                }
			
				return m_errorCode;
			}

			int** pDevices = null;
			int devCount = LibUsbInterop.LibUsbGetDeviceList(IntPtr.Zero, &pDevices);

			deviceNumber = 0;
			
			if (devCount > 0)
			{
				for (int i = 0; i < devCount; i++)
				{
					IntPtr pDevice = new IntPtr((int)(*(pDevices + i)));
							
					IntPtr pDesc = Marshal.AllocHGlobal(18);
					result = LibUsbInterop.LibUsbGetDeviceDescriptor(pDevice, pDesc);

                    if (result != 0)
                    {
                        m_deviceChangeMutex.ReleaseMutex();
                        return ErrorCodes.LibUsbGetDeviceDescriptorFailed;
                    }

					UsbDeviceDescriptor udd = new UsbDeviceDescriptor(pDesc);
							
					if (udd.vendorID == VID)
					{
						DeviceInfo di = new DeviceInfo();
						
						di.UsbDevicePtr = pDevice;
						di.Vid = udd.vendorID;
						di.Pid = udd.deviceID;
                        di.SerialNumber = udd.serialNumber.ToString();
						di.MaxPacketSize = udd.maxPacketSize;
						di.DisplayName = GetDeviceName(di.Pid);
						
                        //if (Enum.IsDefined(typeof(SupportedDevices), di.Pid))
                        if (DaqDeviceManager.IsSupportedDevice(di.Pid))
                        {
						    deviceInfoList.Add(deviceNumber, di);
						    deviceNumber++;
                        }
					}

					Marshal.FreeHGlobal(pDesc);
				}
			}

            m_deviceChangeMutex.ReleaseMutex();

            return ErrorCodes.NoErrors;
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

			// get the device ID
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

			string response;
			int index;
			
            response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length).Trim(new char[] { Constants.NULL_TERMINATOR });

            index = response.IndexOf('=');

            if (index >= 0)
                deviceID = response.Substring(index + 1);
			
			// replace the serial number that came from the device descriptor with
			// the mfg serial number

            for (int i = 0; i < m_sernoMessage.Length; i++)
                packet.Buffer[i] = m_sernoMessage[i];

            UsbControlOutRequest(packet);

	        packet.TransferType = UsbTransferTypes.ControlIn;
            UsbControlInRequest(packet);

            response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length).Trim(new char[] { Constants.NULL_TERMINATOR });

            index = response.IndexOf('=');

            if (index >= 0)
                deviceInfo.SerialNumber = response.Substring(index + 1);

			LibUsbInterop.LibUsbReleaseInterface(deviceInfo.DeviceHandle, 0);
            LibUsbInterop.LibUsbClose(deviceInfo.DeviceHandle);
			deviceInfo.DeviceHandle = IntPtr.Zero;

            m_controlTransferMutex.ReleaseMutex();

            return deviceID;
        }

		//==============================================================================
        /// <summary>
        /// Sets up the device to use with LibUsb
        /// </summary>
        /// <param name="deviceNumber">The device number</param>
        //==============================================================================
        protected void InitializeDevice(DeviceInfo deviceInfo)
        {
            int result;

			if (deviceInfo.DeviceHandle != IntPtr.Zero)
			{
				m_errorCode = ErrorCodes.DeviceHandleAlreadyCreated;
				return;
			}
			
            m_deviceInitialized = false;

			int* dh = null;
			result = LibUsbInterop.LibUsbOpen(deviceInfo.UsbDevicePtr, &dh);
			IntPtr devHandle = new IntPtr((int)(dh));
								
			if (devHandle != IntPtr.Zero)
			{
				m_devHandle = devHandle;
				deviceInfo.DeviceHandle = devHandle;
									
				result = LibUsbInterop.LibUsbClaimInterface(m_devHandle, 0);

				// get the config descriptor
				byte[] epDescriptor = new byte[64];
				LibUsbInterop.LibUsbControlTransfer(m_devHandle, 
					                                0x80, 
					                                0x06, 
					                                (0x02 << 8) | 0, 
					                                0, 
					                                epDescriptor, 
					                                (ushort)epDescriptor.Length, 
					                                1000);

                if (result == 0)
                {
                    m_deviceInitialized = true;

                    // store the endpoints
                    deviceInfo.EndPointIn = GetEndpointInAddress(epDescriptor);
                    deviceInfo.EndPointOut = GetEndpointOutAddress(epDescriptor);
                }
                else
                {
                    m_errorCode = ErrorCodes.DeviceNotInitialized;
                }
			}
        }

		//==================================================================
		/// <summary>
		/// Clean up resources 
		/// </summary>
		//==================================================================
		internal override void ReleaseDevice()
		{
			if (m_deviceInfo.DeviceHandle != IntPtr.Zero)
			{
            	LibUsbInterop.LibUsbReleaseInterface(m_deviceInfo.DeviceHandle, 0);
                LibUsbInterop.LibUsbClose(m_deviceInfo.DeviceHandle);
				m_deviceInfo.DeviceHandle = IntPtr.Zero;
			}
		}

        //===================================================================================================
        /// <summary>
        /// Get the device ID that a user set to store in the device info object
        /// </summary>
        /// <returns>The device ID</returns>
        //===================================================================================================
        internal override string GetSerno(DeviceInfo deviceInfo)
        {
            m_controlTransferMutex.WaitOne();

            string deviceID = String.Empty;

            InitializeDevice(deviceInfo);

            UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);

            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = 0x80;
            packet.DeferTransfer = false;
            packet.BytesTransfered = 0;

            for (int i = 0; i < m_sernoMessage.Length; i++)
                packet.Buffer[i] = m_sernoMessage[i];

            UsbControlOutRequest(packet);

            packet.TransferType = UsbTransferTypes.ControlIn;
            UsbControlInRequest(packet);

            string response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length).Trim(new char[] { Constants.NULL_TERMINATOR });

            int index = response.IndexOf('=');

            if (index >= 0)
                deviceID = response.Substring(index + 1);

			LibUsbInterop.LibUsbReleaseInterface(deviceInfo.DeviceHandle, 0);
            LibUsbInterop.LibUsbClose(deviceInfo.DeviceHandle);
			deviceInfo.DeviceHandle = IntPtr.Zero;

			m_controlTransferMutex.ReleaseMutex();

            return deviceID;
        }

        //===================================================================================================
        /// <summary>
        /// Creates one or more BulkInRequest objects that contain the overlapped struct and data buffer
        /// These are used by the SubmitBulkInRequest and CompleteBulkInRequest methods
        /// </summary>
        //===================================================================================================
        protected override void CreateBulkInRequestObjects()
        {
            m_bulkInRequests.Clear();

            for (int i = 0; i < m_numberOfWorkingInputRequests; i++)
            {
                // create bulk in request object
                UsbBulkInRequest request = new UsbBulkInRequest();

                // assign index
                request.Index = i;

                // allocate the bulk in request buffer
                request.Buffer = new byte[m_bulkXferSize];

                m_bulkInRequests.Add(request);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Creates one or more BulkInRequest objects that contain the overlapped struct and data buffer
        /// These are used by the SubmitBulkInRequest and CompleteBulkInRequest methods
        /// </summary>
        //===================================================================================================
        protected override void CreateBulkOutRequestObjects()
        {
            m_bulkOutRequests.Clear();

            int byteCount = 0;
            int bytesRequested;

            for (int i = 0; i < m_numberOfWorkingOutputRequests; i++)
            {
                // create bulk in request object
                UsbBulkOutRequest request = new UsbBulkOutRequest();

                // assign index
                request.Index = i;

                // allocate the bulk in request buffer
                request.Buffer = new byte[m_bulkXferSize];

                if (m_totalNumberOfOutputBytesRequested - byteCount > m_bulkXferSize)
                    bytesRequested = request.Buffer.Length;
                else
                    bytesRequested = m_totalNumberOfOutputBytesRequested - byteCount;

                byteCount += bytesRequested;

                request.BytesRequested = bytesRequested;

                m_bulkOutRequests.Add(request);
            }

            UsbBulkOutRequest br;

            for (int i = 0; i < m_numberOfWorkingOutputRequests; i++)
            {
                br = m_bulkOutRequests[i];

                // transfer data from the driver interface's internal write buffer to the request buffer
                Array.Copy(m_driverInterfaceOutputBuffer, m_driverInterfaceOutputBufferIndex, br.Buffer, 0, br.BytesRequested);

                m_driverInterfaceOutputBufferIndex += br.BytesRequested;

                if (m_driverInterfaceOutputBufferIndex >= m_driverInterfaceOutputBuffer.Length)
                    m_driverInterfaceOutputBufferIndex = 0;
            }
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
            ClearStall(m_deviceInfo.EndPointIn, m_aoScanResetPacket);
        }
        
        //==================================================================
		/// <summary>
		/// Clears the pipe's stall state 
		/// </summary>
		//==================================================================
		protected void ClearStall(byte pipe, UsbSetupPacket resetPacket)
		{
            m_controlTransferMutex.WaitOne();

            UsbControlOutRequest(resetPacket);

            m_controlTransferMutex.ReleaseMutex();
            
            LibUsbClearHalt(m_devHandle, pipe);
		}
		
		//==================================================================
        /// <summary>
        /// Method for a USB control In request
        /// </summary>
        /// <returns>The result</returns>
        //==================================================================
        internal override ErrorCodes UsbControlInRequest(UsbSetupPacket packet)
        {
			int result;
			
			result = LibUsbInterop.LibUsbControlTransfer(m_devHandle, 
				                               ControlRequestType.VENDOR_CONTROL_IN, 
				                               packet.Request,
				                               packet.Value, 
				                               packet.Index,
				                               packet.Buffer,
				                               (ushort)packet.Buffer.Length,
				                               m_inputXferTimeOut);

			if (result >= 0)
			{
				packet.BytesTransfered = (uint)result;
				return ErrorCodes.NoErrors;
			}
			else if (result == -4)
			{
				return ErrorCodes.DeviceNotResponding;
			}
			else if (result == -9)
			{
				return ErrorCodes.InvalidMessage;
			}

			System.Diagnostics.Debug.Assert(false, String.Format("Unknown error in UsbControlOutRequest: {0}", result));
			return ErrorCodes.UnknownError;
		}
		
		//==================================================================
        /// <summary>
        /// Method for a USB control Out request
        /// </summary>
        /// <returns>The result</returns>
        //==================================================================
        internal override ErrorCodes UsbControlOutRequest(UsbSetupPacket packet)
        {
			int result;
			
			result = LibUsbInterop.LibUsbControlTransfer(m_devHandle, 
				                               ControlRequestType.VENDOR_CONTROL_OUT, 
				                               packet.Request,
				                               packet.Value, 
				                               packet.Index,
				                               packet.Buffer,
				                               (ushort)packet.Buffer.Length,
				                               m_inputXferTimeOut);

			if (result >=  0)
			{
				return ErrorCodes.NoErrors;
			}
			else if (result == -4)
			{
				return ErrorCodes.DeviceNotResponding;
			}
			else if (result == -9)
			{
				return ErrorCodes.InvalidMessage;
			}

			System.Diagnostics.Debug.Assert(false, String.Format("Unknown error in UsbControlOutRequest: {0}", result));
			return ErrorCodes.UnknownError;
		}
		
        //===================================================================================================
        /// <summary>
        /// Processes Bulk In requests
        /// </summary>
        //===================================================================================================
        internal override void ProcessBulkInRequests()
        {
            Monitor.Enter(m_bulkInRequestLock);

            int bytesTransfered;
            int status = 0;
            uint timeOut;
			bool localBulkInRequestsStarted = false;

            if (m_deviceInfo.EndPointIn == 0)
            {
                m_errorCode = ErrorCodes.BulkInputTransfersNotSupported;
                return;
            }

            try
            {
                while (m_errorCode == 0 && !m_stopInputTransfers && !InputTransfersComplete)
                {
                    for (int i = 0; i < m_numberOfWorkingInputRequests; i++)
                    {
                        if (m_errorCode != 0 || m_stopInputTransfers || InputTransfersComplete)
                            break;

                        bytesTransfered = 0;
                        status = 0;
                        timeOut = 0;

                        // number of requests needs to be synchronized between threads.
                        // So we need to check if we're done and if so simply return
                        // so an extra bulk transfer isn't attempted
                        if (m_criticalParams.InputSampleMode == SampleMode.Finite)
                        {
                            if (NumberOfInputRequestsSubmitted >= TotalNumberOfInputRequests)
                                break;
                        }

                        // if we didn't return, then increment the number of requests submitted
                        NumberOfInputRequestsSubmitted++;

                        if (m_criticalParams.InputSampleMode == SampleMode.Continuous)
                        {
                            TotalNumberOfInputRequests++;

                            if (TotalNumberOfInputRequests < 0)
                            {
                                NumberOfInputRequestsSubmitted = 0;
                                TotalNumberOfInputRequests = NumberOfInputRequestsSubmitted + 1;
                            }
                        }
                        else
                        {
                            // for finite mode we expect a zero-length packet after all data has been expected
                            // in this case we'll set the time out value in case a zero-length packet isn't returned
                            if (NumberOfInputRequestsSubmitted == TotalNumberOfInputRequests)
                                timeOut = 200;
                        }

                        UsbBulkInRequest bir = m_bulkInRequests[i];
                        
						if (localBulkInRequestsStarted == false)
						{
							BulkInRequestsStarted = true;
							localBulkInRequestsStarted = true;
						}
						
                        if (m_criticalParams.InputTriggerEnabled && !m_stopInputTransfers)
                        {
                            timeOut = 100;

                            while (bytesTransfered == 0 && !m_stopInputTransfers)
                            {
                                // allow a timeout to occur but ignore it and resubmit the transfer
                                status = LibUsbInterop.LibUsbBulkTransfer(m_devHandle,
                                                                          m_deviceInfo.EndPointIn,
                                                                          bir.Buffer,
                                                                          bir.Buffer.Length,
                                                                          ref bytesTransfered,
                                                                          timeOut);

                                if (status != 0 && status != -7)
                                {
                                    break;
                                }
                            }
                        }
                        else if (!m_criticalParams.InputTriggerEnabled)
                        {
                            timeOut = 0;
                            status = LibUsbInterop.LibUsbBulkTransfer(m_devHandle,
                                                                      m_deviceInfo.EndPointIn,
                                                                      bir.Buffer,
                                                                      bir.Buffer.Length,
                                                                      ref bytesTransfered,
                                                                      timeOut);
                        }


                        if (status == -9)
                        {
                            m_errorCode = ErrorCodes.DataOverrun;
                            m_dataOverrunOccurred = true;
                            ClearStall(m_deviceInfo.EndPointIn, m_aiScanResetPacket);
                        }
                        else if (status != 0)
                        {
                            m_errorCode = TranslateLibUsbErrorCode(status);
                        }
                        else
                        {
                            m_inputScanTriggered = true;

                            if (bytesTransfered > 0 && bytesTransfered < bir.Buffer.Length)
                            {
                                bytesTransfered = bir.Buffer.Length;
                            }

                            bir.BytesReceived = bytesTransfered;

                            BulkInBuffer bulkInBuffer = QueueBulkInReadyBuffers(null, QueueAction.Dequeue);

                            Array.Copy(bir.Buffer, bulkInBuffer.Data, bytesTransfered);
                            bulkInBuffer.Length = bytesTransfered;

                            QueueBulkInCompletedBuffers(bulkInBuffer, QueueAction.Enqueue);

                            NumberOfInputRequestsCompleted++;

                            if (m_criticalParams.InputSampleMode == SampleMode.Finite)
                            {
                                if (NumberOfInputRequestsCompleted == TotalNumberOfInputRequests)
                                    InputTransfersComplete = true;
                            }
                        }
                    }

                    if (m_criticalParams.InputSampleMode == SampleMode.Finite)
                    {
                        if (NumberOfInputRequestsSubmitted >= TotalNumberOfInputRequests)
                            break;
                    }
                }
            }
            catch (Exception)
            {
                m_errorCode = ErrorCodes.UnknownError;
            }
            finally
            {
                Monitor.Exit(m_bulkInRequestLock);
            }
        }

        //================================================================================================
        /// <summary>
        /// Overriden to perform a bulk out transfer using libusb
        /// </summary>
        /// <param name="br">The bulk out request object</param>
        /// <param name="bytesTransferred">The number of bytes sent to the device</param>
        /// <returns>The libusb error code</returns>
        //================================================================================================
        internal override int UsbBulkOutRequest(UsbBulkOutRequest br, ref int bytesTransferred)
        {
			int status;
			
			status = LibUsbInterop.LibUsbBulkTransfer(m_devHandle,
                           			                  m_deviceInfo.EndPointOut,
                                       			      br.Buffer,
                                                   	  br.BytesRequested,
                                                      ref bytesTransferred,
                                                      0);
			
			return status;
        }
		
        //================================================================================================
        /// <summary>
        /// Flushes out any left over data after an input scan is stopped
        /// </summary>
        //================================================================================================
        internal override void FlushInputDataFromDevice()
		{
				int bytesTransferred = 0;
				int status;
				
				do
				{
					byte[] buf = new byte[m_criticalParams.InputPacketSize];
					status = LibUsbInterop.LibUsbBulkTransfer(m_devHandle,
                                                              m_deviceInfo.EndPointIn,
			    	   	                                      buf,
			            	                                  buf.Length,
			           	    	                              ref bytesTransferred,
			               	    	                          200);
				
				} while (bytesTransferred > 0 && status == 0);
		}


        //==================================================================================
        /// <summary>
        /// Stops an input scan
        /// </summary>
        //==================================================================================
        internal override void StopInputTransfers()
        {
            m_stopInputTransferMutex.WaitOne();

            // set this flag so running threads can terminate
            m_stopInputTransfers = true;

            if (m_processBulkInRequests != null)
			{
				m_processBulkInRequests.Join();
				m_processBulkInRequests = null;
			}

            m_stopInputTransferMutex.ReleaseMutex();
        }

        //==================================================================================
        /// <summary>
        /// Stops an output scan
        /// </summary>
        //==================================================================================
        internal override void StopOutputTransfers()
        {
            m_stopOutputTransferMutex.WaitOne();

            // set this flag so running threads can terminate
            m_stopOutputTransfers = true;

            if (m_processBulkOutRequests != null)
			{
				m_processBulkOutRequests.Join();
				m_processBulkOutRequests = null;
			}

            m_stopOutputTransferMutex.ReleaseMutex();
        }

		//==================================================================
        /// <summary>
        /// Check's the device status for a data overrun
        /// </summary>
        /// <returns>The error code</returns>
        //==================================================================
        internal override ErrorCodes CheckOverrun()
        {
            m_errorCode = ErrorCodes.NoErrors;

            m_controlTransferMutex.WaitOne();

			LibUsbInterop.LibUsbControlTransfer(m_devHandle, 
				                               	0xC0, 
				                               	0x44,
				                               	0, 
				                               	0,
				                               	m_statusBuffer,
				                               	(ushort)m_statusBuffer.Length,
				                               	0);
			
            if ((m_statusBuffer[0] & 0x04) != 0)
                m_errorCode = ErrorCodes.DataOverrun;

			m_controlTransferMutex.ReleaseMutex();

            return m_errorCode;
		}
		
		//==================================================================
        /// <summary>
        /// Check's the device status for a data overrun
        /// </summary>
        /// <returns>The error code</returns>
        //==================================================================
        internal override ErrorCodes CheckUnderrun()
        {
            m_errorCode = ErrorCodes.NoErrors;

            m_controlTransferMutex.WaitOne();

			LibUsbInterop.LibUsbControlTransfer(m_devHandle, 
				                               	0xC0, 
				                               	0x44,
				                               	0, 
				                               	0,
				                               	m_statusBuffer,
				                               	(ushort)m_statusBuffer.Length,
				                               	0);
			
            if ((m_statusBuffer[0] & 0x10) != 0)
                m_errorCode = ErrorCodes.DataUnderrun;

			m_controlTransferMutex.ReleaseMutex();

            return m_errorCode;
		}
		
        //=======================================================================================
        /// <summary>
        /// Gets the address of the bulk in endpoint
        /// </summary>
        /// <param name="data">A data array which that receives the endpoint descriptor</param>
        /// <returns>The endpoint address</returns>
        //=======================================================================================
        protected byte GetEndpointInAddress(byte[] data)
		{
			int descriptorType;
			int length;
			int index = 0;
			
			while (true)
			{
				length = data[index];
				descriptorType = data[index + 1];
				
				if (length == 0)
					break;
				
				if (descriptorType != 0x05)
				{
					index += length;
				}
				else
				{
					if ((data[index + 2] & 0x80) != 0)
						return data[index + 2];
					else
						index += length;
				}
				
				if (index >= data.Length)
					break;
			}
			
			return 0;
		}

        //=======================================================================================
        /// <summary>
        /// Gets the address of the bulk out endpoint
        /// </summary>
        /// <param name="data">A data array which that receives the endpoint descriptor</param>
        /// <returns>The endpoint address</returns>
        //=======================================================================================
        protected byte GetEndpointOutAddress(byte[] data)
		{
			int descriptorType;
			int length;
			int index = 0;
			
			while (true)
			{
				length = data[index];
				descriptorType = data[index + 1];
				
				if (length == 0)
					break;
				
				if (descriptorType != 0x05)
				{
					index += length;
				}
				else
				{
					if ((data[index + 2] & 0x80) == 0)
						return data[index + 2];
					else
						index += length;
				}
				
				if (index >= data.Length)
					break;
			}
			
			return 0;
		}

		//=================================================================================
        /// <summary>
        /// Translates the error code to a MBD error code
        /// </summary>
        /// <param name="libusbErrorCode">The libusb error code</param>
        /// <returns>The MBD error code</returns>
        //=================================================================================
        protected override ErrorCodes TranslateErrorCode(int errorCode)
        {
			return TranslateLibUsbErrorCode(errorCode);
		}
		
        //=================================================================================
        /// <summary>
        /// Translates a libusb error code to a MBD error code
        /// </summary>
        /// <param name="libusbErrorCode">The libusb error code</param>
        /// <returns>The MBD error code</returns>
        //=================================================================================
        protected ErrorCodes TranslateLibUsbErrorCode(int libusbErrorCode)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            switch (libusbErrorCode)
            {
                case (-1): errorCode = ErrorCodes.UsbIOError;
                    break;
                case (-3): errorCode = ErrorCodes.UsbInsufficientPermissions;
                    break;
                case (-4): errorCode = ErrorCodes.DeviceNotResponding;
                    break;
                case (-7): errorCode = ErrorCodes.UsbTimeoutError;
                    break;
                case (-9): errorCode = ErrorCodes.UsbPipeError;
                    break;
                case (-99): errorCode = ErrorCodes.UnknownError;
                    break;
                default: errorCode = ErrorCodes.NoErrors;
                    break;
            }

            return errorCode;
        }
    }
}
