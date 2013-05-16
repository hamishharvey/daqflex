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
    internal abstract class PlatformInterop
    {
        static protected int deviceNumber = 0;

        protected DeviceInfo m_deviceInfo;
        protected SampleMode m_inputSampleMode = SampleMode.Finite;
        protected Mutex m_completionMutex = new Mutex();
        protected int m_maxTransferSize;
        protected volatile bool m_stopInputTransfers = false;
        protected volatile bool m_stopOutputTransfers = false;
        protected ErrorCodes m_errorCode;
        protected CriticalParams m_criticalParams;
        protected bool m_deviceInitialized;
        protected ASCIIEncoding m_ae = new ASCIIEncoding();
        protected Mutex m_controlTransferMutex = new Mutex();
        //protected System.Diagnostics.Stopwatch m_statusStopWatch = new System.Diagnostics.Stopwatch();
        protected double m_stopWatchResolution;
        protected Mutex m_stopInputTransferMutex = new Mutex();
        protected Mutex m_stopOutputTransferMutex = new Mutex();
		protected bool m_inputTransfersComplete;
        protected bool m_outputTransfersComplete;
        protected bool m_inputScanTriggered = false;

        internal byte[] m_driverInterfaceOutputBuffer;
        protected int m_totalBytesReceivedByDevice;

    	protected bool m_readyToStartOutputScan = false;
		protected bool m_readyToSubmitRemainingOutputTransfers = false;

        internal int NumberOfInputRequestsSubmitted;
        internal int NumberOfInputRequestsCompleted;
        internal int TotalNumberOfInputRequests;

        //=========================================================================================================
        /// <summary>
        /// Creates a platform specific interop object based on the platform that application is running on
        /// </summary>
        /// <returns>The platform interop object</returns>
        //=========================================================================================================
        internal static UsbPlatformInterop GetUsbPlatformInterop()
        {
            PlatformID platFormID = Environment.OSVersion.Platform;

#if WindowsCE
            return new WinCeUsbInterop();
#else
            if (platFormID == PlatformID.Unix)
            {
                return new LibUsbInterop();
            }
            else if (platFormID == PlatformID.Win32NT)
            {
                return new WinUsbInterop();
            }
            return null;
#endif
        }

        //=========================================================================================================
        /// <summary>
        /// Creates a platform specific interop object based on the platform that application is running on
        /// </summary>
        /// <returns>The platform interop object</returns>
        //=========================================================================================================
        internal static UsbPlatformInterop GetUsbPlatformInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
        {
#if WindowsCE
            return new WinCeUsbInterop(deviceInfo, criticalParams);
#else
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return new LibUsbInterop(deviceInfo, criticalParams);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return new WinUsbInterop(deviceInfo, criticalParams);
            }

            return null;
#endif
        }

        //===================================================================================
        /// <summary>
        /// default ctor
        /// </summary>
        //===================================================================================
        internal PlatformInterop()
        {
        }

        //===================================================================================
        /// <summary>
        /// ctor - stores the deviceInfo and criticalParams to members
        /// </summary>
        /// <param name="deviceInfo">A DeviceInfo object</param>
        /// <param name="criticalParams">A criticalParams object</param>
        //===================================================================================
        internal PlatformInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
        {
            m_deviceInfo = deviceInfo;
            m_criticalParams = criticalParams;
			m_readyToStartOutputScan = true;
            m_stopWatchResolution = 1.0 / (double)System.Diagnostics.Stopwatch.Frequency;
		}

        //=================================================================================================
        /// <summary>
        /// Abstract method for getting a list of DeviceInfos
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //=================================================================================================
        internal abstract ErrorCodes GetDevices(Dictionary<int, DeviceInfo> deviceInfoList, bool refresh);

        protected static object inputTransferCompleteMutex = new object();

        internal bool InputTransfersComplete
		{
			get 
			{
				lock(inputTransferCompleteMutex)
				{
					return m_inputTransfersComplete;
				}
			}
			
			set
			{
				lock(inputTransferCompleteMutex)
				{
					m_inputTransfersComplete = value;
				}
			}
		}

        protected static object outputTransferCompleteMutex = new object();

        internal bool OutputTransfersComplete
        {
            get
            {
                lock (outputTransferCompleteMutex)
                {
                    return m_outputTransfersComplete;
                }
            }

            set
            {
                lock (outputTransferCompleteMutex)
                {
                    m_outputTransfersComplete = value;
                }
            }
        }

        //===================================================================================
        /// <summary>
        /// The accumulated number of bytes reported by the CompleteBulkOutRequest callback
        /// </summary>
        //===================================================================================
        internal int TotalBytesReceivedByDevice
        {
            get { return m_totalBytesReceivedByDevice; }
        }

        //===================================================================================
        /// <summary>
        /// Indicates if the input scan was triggered (either internally or externally)
        /// </summary>
        //===================================================================================
        internal bool InputScanTriggered
        {
            get { return m_inputScanTriggered; }
        }

        ////==================================================================
        /// <summary>
        /// Virtual method for getting the user-defined device ID
        /// </summary>
        /// <param name="deviceInfo">A deviceInfo object</param>
        /// <returns>The ID as a string</returns>
        ////==================================================================
        internal abstract string GetDeviceID(DeviceInfo deviceInfo);

        ////==================================================================
        /// <summary>
        /// Virtual method for getting the mfg serial number
        /// </summary>
        /// <param name="deviceInfo">A deviceInfo object</param>
        /// <returns>The ID as a string</returns>
        ////==================================================================
        internal abstract string GetSerno(DeviceInfo deviceInfo);

        //===================================================================================================
        /// <summary>
        /// Used to find a device after its been disconnected.
        /// The platform interop subclass should make sure a running app can continue to communicate
        /// with the device after its been disconnected and reconnected
        /// </summary>
        //===================================================================================================
        internal virtual bool AcquireDevice()
        {
            System.Diagnostics.Debug.Assert(false, "AcquireDevice must be implemented in a derived class");
            return false;
        }

        //===================================================================================================
        /// <summary>
        /// Virtual method to read a device's memory
        /// </summary>
        /// <param name="offset">The starting addresss</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer containing the memory contents</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal virtual ErrorCodes ReadDeviceMemory(ushort offset, byte count, out byte[] buffer)
        {
            buffer = null;
            System.Diagnostics.Debug.Assert(false, "ReadDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //===================================================================================================
        /// <summary>
        /// Virutal method to unlock a device's memory for writing to it
        /// </summary>
        /// <param name="address">The address of the unlock code</param>
        /// <param name="unlockCode">The unlock code</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal virtual ErrorCodes UnlockDeviceMemory(ushort address, ushort unlockCode)
        {
            System.Diagnostics.Debug.Assert(false, "UnlockDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //===================================================================================================
        /// <summary>
        /// Virutal method to lock a device's to prevent writing to it
        /// </summary>
        /// <param name="address">The address of the lock code</param>
        /// <param name="unlockCode">The lock code</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal virtual ErrorCodes LockDeviceMemory(ushort address, ushort unlockCode)
        {
            System.Diagnostics.Debug.Assert(false, "LockDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //===================================================================================================
        /// <summary>
        /// Virtual method for writing a device's capabilites to eeprom
        /// </summary>
        /// <param name="offset">The offset to start writing to</param>
        /// <param name="count">The number of bytes to write</param>
        /// <param name="deviceCaps">Buffer containing the compressed device caps image</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal virtual ErrorCodes WriteDeviceCaps(ushort offset, byte count, byte[] deviceCaps)
        {
            System.Diagnostics.Debug.Assert(false, "WriteDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //===================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="memoryOffset">The starting addresss of the device's memory</param>
        /// <param name="bufferOffset">The starting addresss of the data buffer</param>
        /// <param name="buffer">The data buffer</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal virtual ErrorCodes WriteDeviceMemory(ushort memoryOffset, ushort bufferOffset, byte[] buffer, byte count)
        {
            System.Diagnostics.Debug.Assert(false, "WriteDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //===================================================================================================
        /// <summary>
        /// A reference to the buffer used for output scans
        /// </summary>
        //===================================================================================================
        internal byte[] DriverInterfaceOutputBuffer
        {
            set { m_driverInterfaceOutputBuffer = value; }
        }

        //==============================================================================================
        /// <summary>
        /// Virtual method to check if a device has reported a data overrun
        /// </summary>
        /// <returns>The result</returns>
        //==============================================================================================
        internal virtual ErrorCodes CheckOverrun()
        {
            System.Diagnostics.Debug.Assert(false, "CheckOverrun must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //==============================================================================================
        /// <summary>
        /// Virtual method to check if a device has reported a data underrun
        /// </summary>
        /// <returns>The result</returns>
        //==============================================================================================
        internal virtual ErrorCodes CheckUnderrun()
        {
            System.Diagnostics.Debug.Assert(false, "CheckUnderrun must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method that allows a derived platform interop object to
        /// set up parameters for data transfer
        /// </summary>
        /// <param name="scanRate">The device scan rate</param>
        /// <param name="totalNumberOfBytes">The total number of bytes to transfer</param>
        /// <param name="transferSize">The number of bytes in each transfer request</param>
        //===========================================================================================
        internal virtual void PrepareInputTransfers(double scanRate, int totalNumberOfBytes, int transferSize)
        {
            System.Diagnostics.Debug.Assert(false, "PrepareInputTransfers must be implemented in a derived class");
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method that allows a derived platform interop object to
        /// set up parameters for data transfer
        /// </summary>
        /// <param name="scanRate">The device scan rate</param>
        /// <param name="totalNumberOfBytes">The total number of bytes to transfer</param>
        /// <param name="transferSize">The number of bytes in each transfer request</param>
        //===========================================================================================
        internal virtual void PrepareOutputTransfers(double scanRate, int totalNumberOfBytes, int transferSize)
        {
            System.Diagnostics.Debug.Assert(false, "PrepareInputTransfers must be implemented in a derived class");
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to stop input transferes
        /// </summary>
        //===========================================================================================
        internal virtual void StopInputTransfers()
        {
            System.Diagnostics.Debug.Assert(false, "StopInputTransfers must be implemented in a derived class");
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to stop output transferes
        /// </summary>
        //===========================================================================================
        internal virtual void StopOutputTransfers()
        {
            System.Diagnostics.Debug.Assert(false, "StopOutputTransfers must be implemented in a derived class");
        }

		internal virtual void FlushInputDataFromDevice()
		{
		}

        internal virtual void OnOutputErrorCleanup()
        {
        }

        //===========================================================================================
        /// <summary>
        /// This property will get set within methods that do not return error codes
        /// but need to check errors
        /// </summary>
        //===========================================================================================
        internal ErrorCodes ErrorCode
        {
            get { return m_errorCode; }
        }

        //======================================================================================
        /// <summary>
        /// Gets the device name based on the product ID
        /// </summary>
        /// <param name="pid">The product ID</param>
        /// <returns>Name of the device</returns>
        //======================================================================================
        protected string GetDeviceName(int pid)
        {
            string deviceName = String.Empty;

            switch (pid)
            {
                case (0xF2):
                    deviceName = "USB-7202";
                    break;
                case (0xF0):
                    deviceName = "USB-7204";
                    break;
                case (0xF9):
                    deviceName = "USB-2001-TC";
                    break;
                default:
                    deviceName = "Unknown Device";
                    break;
            }

            return deviceName;
        }

        internal int MaxTransferSize
        {
            get { return m_maxTransferSize; }
        }

        //===========================================================================================
        /// <summary>
        /// Idicates if the device was successfully initialized
        /// </summary>
        //===========================================================================================
        internal bool DeviceInitialized
        {
            get { return m_deviceInitialized; }
        }


		//==================================================================
		/// <summary>
		/// Virtual method to release the device's driver resources
		/// </summary>
		//==================================================================
        internal virtual void ReleaseDevice()
        {
        }
		
		//==================================================================
		/// <summary>
		/// Virtual method to clear the pipe's stall state 
		/// </summary>
		//==================================================================
		internal virtual void ClearDataOverrun()
		{
		}

        //==================================================================
        /// <summary>
        /// Virtual method to clear the pipe's stall state 
        /// </summary>
        //==================================================================
        internal virtual void ClearDataUnderrun()
        {
        }
    }
}
