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
using System.Globalization;

namespace MeasurementComputing.DAQFlex
{
    internal abstract class PlatformInterop
    {
        static protected int deviceNumber = 0;

        protected DeviceInfo m_deviceInfo;
        protected Object m_inputTransferCompletionLock = new Object();
        protected Object m_outputTransferCompletionLock = new Object();
        protected Object m_stopInputTransferLock = new Object();
        protected Object m_stopOutputTransferLock = new Object();
        protected int m_maxTransferSize;
        protected volatile bool m_stopInputTransfers = false;
        protected volatile bool m_stopOutputTransfers = false;
        protected ErrorCodes m_errorCode;
        protected ErrorCodes m_inputScanErrorCode;
        protected ErrorCodes m_outputScanErrorCode;
        protected CriticalParams m_criticalParams;
        protected bool m_deviceInitialized;
        protected ASCIIEncoding m_ae = new ASCIIEncoding();
        protected Mutex m_controlTransferMutex = new Mutex();
        protected double m_stopWatchResolution;

		protected bool m_inputTransfersComplete;
        protected bool m_outputTransfersComplete;
        protected bool m_inputScanTriggered = false;

        internal byte[] m_driverInterfaceOutputBuffer;
        protected int m_totalBytesReceivedByDevice;

    	protected bool m_readyToStartOutputScan = false;
		protected bool m_readyToSubmitRemainingOutputTransfers = false;
        protected static string m_localListSeparator;
        protected static string m_localNumberDecimalSeparator;

        //=========================================================================================================
        /// <summary>
        /// Creates a platform specific interop object based on the platform that application is running on
        /// </summary>
        /// <returns>The platform interop object</returns>
        //=========================================================================================================
        internal static UsbPlatformInterop GetUsbPlatformInterop()
        {
            PlatformID platFormID = Environment.OSVersion.Platform;

            m_localListSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            m_localNumberDecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

#if WindowsCE
            return new WinCeUsbInterop();
#else
            if (platFormID == PlatformID.Unix)
            {
                return new LibUsbInterop();
            }
            else if (platFormID == PlatformID.Win32NT)
            {
                //return new WinUsbInterop();
                return new McUsbInterop();
            }
            return null;
#endif
        }
        
        
#if !WindowsCE
        internal static HidPlatformInterop GetHidPlatformInterop(){
           PlatformID platFormID = Environment.OSVersion.Platform;

           m_localListSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
           m_localNumberDecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

           if (platFormID == PlatformID.Unix) {
              return null;//new LibUsbInterop();
              }
           else if (platFormID == PlatformID.Win32NT){
              //return new WinUsbInterop();
              return new WindowsHidInterop(); //new McUsbInterop();
              }
           
           return null;
        }
#endif


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
                //return new WinUsbInterop(deviceInfo, criticalParams);
                return new McUsbInterop(deviceInfo, criticalParams);
            }

            return null;
#endif
        }
        
#if !WindowsCE
        internal static HidPlatformInterop GetHidPlatformInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
        {

           if (Environment.OSVersion.Platform == PlatformID.Unix)
           {
              return null;// new LibUsbInterop(deviceInfo, criticalParams);
           }
           else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
           {
              //return new WinUsbInterop(deviceInfo, criticalParams);
              return new WindowsHidInterop(deviceInfo, criticalParams); //new McUsbInterop(deviceInfo, criticalParams);
           }

           return null;

        }
#endif

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

        //===================================================================================
        /// <summary>
        /// Gets the current culture list separator
        /// </summary>
        //===================================================================================
        internal static char LocalListSeparator
        {
            get { return m_localListSeparator.ToCharArray()[0]; }
        }

        //===================================================================================
        /// <summary>
        /// Gets the current culture list separator
        /// </summary>
        //===================================================================================
        internal static char LocalNumberDecimalSeparator
        {
            get { return m_localNumberDecimalSeparator.ToCharArray()[0]; }
        }

        //===================================================================================================================
        /// <summary>
        /// Abstract method for getting a list of DeviceInfos
        /// </summary>
        /// <param name="deviceInfoList">The list of devices</param>
        /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
        //===================================================================================================================
        internal abstract ErrorCodes GetDevices(Dictionary<int, DeviceInfo> deviceInfoList, DeviceListUsage deviceListUsage);

        //=================================================================================================
        /// <summary>
        /// A flag that indicates that all bulk input transfers have completed
        /// </summary>
        //=================================================================================================
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

        //=================================================================================================
        /// <summary>
        /// A flag that indicates that all bulk output transfers have completed
        /// </summary>
        //=================================================================================================
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

        //=============================================================================================================================================================
        /// <summary>
        /// Virtual method to read a device's memory
        /// </summary>
        /// <param name="memAddrCmd">The device's memory address command (Request)</param>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal virtual ErrorCodes ReadDeviceMemory1(byte memAddrCmd, byte memReadCmd, ushort memoryOffset, ushort memoryOffsetLength, byte count, out byte[] buffer)
        {
            buffer = null;
            System.Diagnostics.Debug.Assert(false, "ReadDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //=============================================================================================================================================================
        /// <summary>
        /// Virtual method to read a device's memory
        /// </summary>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal virtual ErrorCodes ReadDeviceMemory2(byte memReadCmd, ushort memoryOffset, ushort memoryOffsetLength, byte count, out byte[] buffer)
        {
            buffer = null;
            System.Diagnostics.Debug.Assert(false, "ReadDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //=============================================================================================================================================================
        /// <summary>
        /// Virtual method to read a device's memory
        /// </summary>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal virtual ErrorCodes ReadDeviceMemory3(byte memReadCmd, ushort memoryOffset, ushort memoryOffsetLength, byte count, out byte[] buffer)
        {
            buffer = null;
            System.Diagnostics.Debug.Assert(false, "ReadDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //=============================================================================================================================================================
        /// <summary>
        /// Virtual method to read a device's memory
        /// </summary>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal virtual ErrorCodes ReadDeviceMemory4(byte memReadCmd, ushort memoryOffset, ushort memoryOffsetLength, byte count, out byte[] buffer)
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
        internal virtual ErrorCodes WriteDeviceMemory1(byte memAddrCmd, byte memWriteCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            System.Diagnostics.Debug.Assert(false, "WriteDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="memWriteCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal virtual ErrorCodes WriteDeviceMemory2(byte memWriteCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            System.Diagnostics.Debug.Assert(false, "WriteDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="unlockKey">The unlock key</param>
        /// <param name="memCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal virtual ErrorCodes WriteDeviceMemory3(ushort unlockKey, byte memCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            System.Diagnostics.Debug.Assert(false, "WriteDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="unlockKey">The unlock key</param>
        /// <param name="memCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal virtual ErrorCodes WriteDeviceMemory4(ushort unlockKey, byte memCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            System.Diagnostics.Debug.Assert(false, "WriteDeviceMemory must be implemented in a derived class");
            return ErrorCodes.MethodRequiresImplementation;
        }

        //===================================================================================================
        /// <summary>
        /// Virtual method to load data into the device's FPGA
        /// </summary>
        /// <param name="buffer">The data to load</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal virtual ErrorCodes LoadFPGA(byte request, byte[] buffer)
        {
            System.Diagnostics.Debug.Assert(false, "LoadFPGA must be implemented in a derived class");
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
        internal virtual bool CheckDeviceResponding()
        {
            System.Diagnostics.Debug.Assert(false, "CheckDeviceResponding must be implemented in a derived class");
            return false;
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

        //===========================================================================================
        /// <summary>
        /// This property will get set within methods that do not return error codes
        /// but need to check errors
        /// </summary>
        //===========================================================================================
        internal ErrorCodes InputScanErrorCode
        {
            get { return m_inputScanErrorCode; }
        }

        //===========================================================================================
        /// <summary>
        /// This property will get set within methods that do not return error codes
        /// but need to check errors
        /// </summary>
        //===========================================================================================
        internal ErrorCodes OutputScanErrorCode
        {
            get { return m_outputScanErrorCode; }
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
        /// Clears the error code - typically used for staring scans
        /// </summary>
        //==================================================================
        internal void ClearError()
        {
            m_errorCode = ErrorCodes.NoErrors;
        }

        //==================================================================
        /// <summary>
        /// Clears the error code - typically used for staring scans
        /// </summary>
        //==================================================================
        internal void ClearInputScanError()
        {
            m_inputScanErrorCode = ErrorCodes.NoErrors;
        }

        //==================================================================
        /// <summary>
        /// Clears the error code - typically used for staring scans
        /// </summary>
        //==================================================================
        internal void ClearOutputScanError()
        {
            m_outputScanErrorCode = ErrorCodes.NoErrors;
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
