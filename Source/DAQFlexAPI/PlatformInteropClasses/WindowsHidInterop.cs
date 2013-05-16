
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
   using Microsoft.Win32.SafeHandles;

   namespace MeasurementComputing.DAQFlex
   {
      
      [StructLayout(LayoutKind.Sequential)]
      internal struct HidD_Attributes {
         internal uint Size; // = sizeof (struct _HIDD_ATTRIBUTES)
         internal ushort  VendorID;
         internal ushort ProductID;
         internal ushort VersionNumber;
      } 
      
      [StructLayout(LayoutKind.Sequential)] 
      internal struct HIDP_CAPS { 
         public ushort Usage; 
         public ushort UsagePage; 
         public ushort InputReportByteLength; 
         public ushort OutputReportByteLength; 
         public ushort FeatureReportByteLength; 
         [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] 
         public ushort[] Reserved; 
         public ushort NumberLinkCollectionNodes; 
         public ushort NumberInputButtonCaps; 
         public ushort NumberInputValueCaps; 
         public ushort NumberInputDataIndices; 
         public ushort NumberOutputButtonCaps; 
         public ushort NumberOutputValueCaps; 
         public ushort NumberOutputDataIndices; 
         public ushort NumberFeatureButtonCaps; 
         public ushort NumberFeatureValueCaps; 
         public ushort NumberFeatureDataIndices; 
      }

      internal unsafe class WindowsHidInterop : HidPlatformInterop
      {
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
             
        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern bool CloseHandle(SafeFileHandle h);

         //[DllImport("kernel32.dll", EntryPoint = "GetLastError", SetLastError = true, CharSet = CharSet.Auto)]
         //internal static extern int GetLastError();

         [DllImport("kernel32.dll", EntryPoint = "IsBadWritePtr", SetLastError = true, CharSet = CharSet.Auto)]
         internal static unsafe extern int IsBadWritePtr(void* ptr, uint count);

                                                     
          [DllImport("kernel32.dll", EntryPoint = "WriteFile", SetLastError = true, CharSet = CharSet.Auto)]
         internal static extern unsafe bool WriteFile( SafeFileHandle hDevice,
                                                IntPtr OutBuffer,
                                                int nOutBufferSize,
                                                ref int pBytesWritten,
                                                IntPtr overlapped);
                                                     
          [DllImport("kernel32.dll", EntryPoint = "ReadFile", SetLastError = true, CharSet = CharSet.Auto)]
         internal static extern unsafe bool ReadFile(  SafeFileHandle hDevice,
                                                IntPtr InBuffer,
                                                int nInBufferSize,
                                                ref int pBytesReturned,
                                                IntPtr overlapped);

          [DllImport("kernel32.dll", EntryPoint = "CancelIo", SetLastError = true, CharSet = CharSet.Auto)]                                       
         internal static extern unsafe bool CancelIo( SafeFileHandle hDevice);

          [DllImport("kernel32.dll", EntryPoint = "GetOverlappedResult", SetLastError = true, CharSet = CharSet.Auto)]
          internal static extern unsafe bool GetOverlappedResult(SafeFileHandle hDevice, 
                                                NativeOverlapped* overlapped,
                                                ref int bytesReturned,
                                                bool wait);
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
         /// <summary>
         /// //////////////////////////
         /// </summary>
         #region HID API Interop
         
         [DllImport("hid.dll", EntryPoint = "HidD_GetHidGuid", SetLastError = true, CharSet = CharSet.Auto)]
         internal static extern void HidD_GetHidGuid(ref Guid GUID);
         
         [DllImport("hid.dll", EntryPoint = "HidD_GetAttributes", SetLastError = true, CharSet = CharSet.Auto)]
         internal static extern bool HidD_GetAttributes(SafeFileHandle hidHandle, ref HidD_Attributes Attributes );
         
         [DllImport("hid.dll", EntryPoint = "HidD_FreePreparsedData", SetLastError = true, CharSet = CharSet.Auto)]
         internal static extern bool HidD_FreePreparsedData(ref IntPtr PreparsedData);
         
         [DllImport("hid.dll", EntryPoint = "HidD_GetPreparsedData", SetLastError = true, CharSet = CharSet.Auto)]
         internal static extern bool HidD_GetPreparsedData(SafeFileHandle hidHandle, out IntPtr PreparsedData);
         
         [DllImport("hid.dll", EntryPoint = "HidP_GetCaps", SetLastError = true, CharSet = CharSet.Auto)]
         internal static extern bool HidP_GetCaps(IntPtr PreparsedData, out HIDP_CAPS capabilities);
         
         #endregion

          internal unsafe class HIDRequest {
               internal IntPtr Buffer=IntPtr.Zero;
               internal int ReportLength = 64;
               internal int BytesTransferred = 0;
               internal uint ErrCode;
               
               internal int TimeOut = 1000; //msec
               internal AutoResetEvent OverLappedEvent = null;
               internal Overlapped Overlapped = null;
               internal NativeOverlapped* NativeOverlapped = null;
               internal IntPtr NativeOverlappedIntPtr;
               }
               
           HIDRequest m_inReport=new HIDRequest();
           HIDRequest m_outReport = new HIDRequest();
               

         // internal unsafe class THidInfo {
         //    internal uint uGetReportTimeout = 600;
         //    internal uint uSetReportTimeout = 500;
             
         //    internal Overlapped oRead = null;
         //    internal NativeOverlapped* oReadNative = null;
         //    internal IntPtr oReadNativeIntPtr;

         //    internal Overlapped oWrite=null;
         //    internal NativeOverlapped* oWriteNative = null;
         //    internal IntPtr oWriteNativeIntPtr;
             
         //    internal ushort wInReportBufferLength = 64;
         //    internal ushort wOutReportBufferLength = 64;
         //    internal byte[] inBuffer = new byte[256];
         //    internal ushort inBufferUsed = 0;
         //    }
            
         //private THidInfo m_hidInfo = new THidInfo();
         private HIDP_CAPS m_capabilities;
         private IntPtr m_setupDeviceInfo;
         private SafeFileHandle m_deviceHandle;
         
         internal WindowsHidInterop()
         {
            m_setupDeviceInfo = IntPtr.Zero;
            m_deviceHandle = null;
         }

         internal WindowsHidInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
            : base(deviceInfo, criticalParams)
         {
            InitializeDevice(m_deviceInfo);
            if (m_errorCode == ErrorCodes.NoErrors && !m_deviceInitialized)
               m_errorCode = ErrorCodes.DeviceNotInitialized;
         }

         ~WindowsHidInterop() 
         {
            ReleaseDevice();
            ReleaseReports();
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

            Guid deviceGuid = new Guid();
            HidD_GetHidGuid(ref deviceGuid);

            //*********************************************
            // temporarily store the existing devices
            //*********************************************

            foreach (KeyValuePair<int, DeviceInfo> kvp in deviceInfoList){
               existingDevices.Add(kvp.Key, kvp.Value);
               }

            deviceNumber = existingDevices.Count;

            //*********************************************
            // detect devices
            //*********************************************

            // get a handle to the device information set using the device GUID
            m_setupDeviceInfo = SetupDiGetClassDevs(ref deviceGuid, null, 0, (int)(UsbDeviceConfigInfoFlags.Present | UsbDeviceConfigInfoFlags.DeviceInterface));

            if (m_setupDeviceInfo != IntPtr.Zero){
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
                              di.DeviceName = di.DisplayName;
                              di.SerialNumber = "00000000";//pathParts[2];

                              if (Enum.IsDefined(typeof(DeviceIDs), di.Pid))
                              {
                                 bool addToList = true;

                                 // see if the deviceInfo object is already in the list
                                 for (int i = 0; i < existingDevices.Count; i++)
                                 {
                                    if (di.Pid == existingDevices[i].Pid || di.SerialNumber == existingDevices[i].SerialNumber)
                                    {
                                       existingDevicesDetected.Add(existingDevices[i].DeviceNumber);
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
               deviceInfoList.Add(deviceNumber++, kvp.Value);
            }

            // add one each from the new devices
            foreach (KeyValuePair<int, DeviceInfo> kvp in newDevices)
            {
               deviceInfoList.Add(deviceNumber++, kvp.Value);
            }

            return errorCode;
         }

         //=========================================================================================================================
         /// <summary>
         /// Virtual method for getting a list of DeviceInfos
         /// </summary>
         /// <param name="deviceInfoList">The list of devices</param>
         /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
         //=========================================================================================================================
         internal override ErrorCodes GetHIDDevices(Dictionary<int, DeviceInfo> deviceInfoList, DeviceListUsage deviceListUsage)
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

            Guid deviceGuid = new Guid();
            HidD_GetHidGuid(ref deviceGuid);
            
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
                                 di.DeviceName = di.DisplayName;
                                 di.SerialNumber = "00000000";//pathParts[2];
                                 di.DeviceID = "";
                              
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

      
      
        //=========================================================================
        /// <summary>
        /// Initialize and configure the USB device
        /// </summary>
        /// <param name="deviceNumber">The device number</param>
        /// <returns>true if the device is successfully initiatlized and configured
        /// otherwise false</returns>
        //=========================================================================
        public void InitializeDevice(DeviceInfo deviceInfo)
        {
            m_deviceInitialized = false;
            if (deviceInfo==null) {
               Dictionary<int,DeviceInfo> dil=new Dictionary<int,DeviceInfo>();
               GetHIDDevices(dil, DeviceListUsage.ReuseList);
               if (dil.Count >0)
                  deviceInfo = dil[0];
               else 
                  return;
               }

            m_deviceHandle = GetDeviceHandle(deviceInfo);

            if (!m_deviceHandle.IsInvalid)
            {
                ThreadPool.BindHandle(m_deviceHandle);
                InitializeDevice(m_deviceHandle, deviceInfo);
            }
        }

      

      internal override bool AcquireDevice() {
         if (null!=m_deviceHandle)
            ReleaseDevice();

         InitializeDevice(m_deviceInfo);

         if (m_deviceInitialized)
            return true;
         else
            return false;
         }
        
      internal override void ReleaseDevice(){
         ReleaseReports();
         if (m_deviceHandle != null) {
            
            if (!m_deviceHandle.IsInvalid){
               CloseHandle(m_deviceHandle);
               m_deviceHandle.Close();
               System.Diagnostics.Debug.WriteLine(string.Format("ReleaseDevice - CloseHandle(0x{0})", m_deviceHandle.DangerousGetHandle().ToString("x")));
               }
            //else if (!m_deviceHandle.IsClosed)
            //   m_deviceHandle.Close();
               
            m_deviceHandle = null;
            m_deviceInitialized = false;
            }
         }
         
      internal void InitializeReports(HIDP_CAPS caps) 
      {
         m_inReport.ReportLength = m_capabilities.InputReportByteLength;
         if (m_inReport.Buffer != IntPtr.Zero) {
            Marshal.FreeHGlobal(m_inReport.Buffer);
            m_inReport.Buffer = IntPtr.Zero;
            }
            
         m_inReport.Buffer = Marshal.AllocHGlobal(m_inReport.ReportLength);
         m_inReport.OverLappedEvent = new AutoResetEvent(false);
         m_inReport.Overlapped = new Overlapped();
         if (null!=m_inReport.NativeOverlapped)
            Overlapped.Unpack(m_inReport.NativeOverlapped);
            
         m_inReport.NativeOverlapped = m_inReport.Overlapped.Pack(CompleteHIDInReport, m_inReport.Buffer);
         if (sizeof(IntPtr) == 8)
            m_inReport.NativeOverlappedIntPtr = new IntPtr((long)m_inReport.NativeOverlapped);
         else
            m_inReport.NativeOverlappedIntPtr = new IntPtr((int)m_inReport.NativeOverlapped);

         m_outReport.ReportLength = m_capabilities.OutputReportByteLength;
         if (m_outReport.Buffer != IntPtr.Zero)
         {
            Marshal.FreeHGlobal(m_outReport.Buffer);
            m_outReport.Buffer = IntPtr.Zero;
         }
         m_outReport.Buffer = Marshal.AllocHGlobal(m_outReport.ReportLength);
         m_outReport.OverLappedEvent = new AutoResetEvent(false);
         m_outReport.Overlapped = new Overlapped();
         if (null!=m_outReport.NativeOverlapped)
            Overlapped.Unpack(m_outReport.NativeOverlapped);
            
         m_outReport.NativeOverlapped = m_outReport.Overlapped.Pack(CompleteHIDOutReport, m_outReport.Buffer);
         if (sizeof(IntPtr) == 8)
            m_outReport.NativeOverlappedIntPtr = new IntPtr((long)m_outReport.NativeOverlapped);
         else
            m_outReport.NativeOverlappedIntPtr = new IntPtr((int)m_outReport.NativeOverlapped);
      
      }
      
      internal void ReleaseReports()
      {
         if (m_inReport.Buffer !=IntPtr.Zero)
            Marshal.FreeHGlobal(m_inReport.Buffer);
         m_inReport.Buffer = IntPtr.Zero;
         if (m_inReport.OverLappedEvent != null)
            m_inReport.OverLappedEvent.Close();
         m_inReport.OverLappedEvent = null;
         
         if (m_inReport.NativeOverlapped!=null) {
            Overlapped.Unpack(m_inReport.NativeOverlapped);
            Overlapped.Free(m_inReport.NativeOverlapped);
            }
         m_inReport.NativeOverlapped=null;
         m_inReport.NativeOverlappedIntPtr = IntPtr.Zero;
         m_inReport.Overlapped = null; 
         
         if (m_outReport.Buffer!=IntPtr.Zero)
            Marshal.FreeHGlobal(m_outReport.Buffer);
         m_outReport.Buffer = IntPtr.Zero;
         if (m_outReport.OverLappedEvent!=null)
            m_outReport.OverLappedEvent.Close();
         m_outReport.OverLappedEvent = null;

         if (null!=m_outReport.NativeOverlapped){
            Overlapped.Unpack(m_outReport.NativeOverlapped);
            Overlapped.Free(m_outReport.NativeOverlapped);
            }
         m_outReport.NativeOverlapped = null;
         m_outReport.NativeOverlappedIntPtr = IntPtr.Zero;
         m_outReport.Overlapped = null;
      }
      
      
      //=========================================================================
      /// <summary>
      /// Initialize and configures the USB device
      /// </summary>
      /// <param name="deviceHandle">The handle to the device</param>
      /// <param name="deviceInfo">The device number</param>
      //=========================================================================
      protected void InitializeDevice(SafeFileHandle deviceHandle, DeviceInfo deviceInfo) {
         if (!deviceHandle.IsInvalid) {  
            
            IntPtr hPreparsedData; 
            if (HidD_GetPreparsedData(deviceHandle, out hPreparsedData)){
               m_deviceInitialized = true;
               if (HidP_GetCaps(hPreparsedData, out m_capabilities)) {
                  this.InitializeReports(m_capabilities);
                  } 
                  
               HidD_FreePreparsedData(ref hPreparsedData);
               } 
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
            SafeFileHandle h = CreateFile(devicePath,
                              Constants.GENERIC_WRITE | Constants.GENERIC_READ,
                              Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE,
                              IntPtr.Zero,
                              Constants.OPEN_EXISTING,
                              Constants.FILE_FLAG_OVERLAPPED,
                              IntPtr.Zero);
                              
            System.Diagnostics.Debug.Assert(!h.IsInvalid);
            if (!h.IsInvalid) {
               System.Diagnostics.Debug.WriteLine(string.Format("GetDeviceHandle=0x{0} at {1}", h.DangerousGetHandle().ToString("x"), devicePath));
               }
            
            return h;
        }
        
        
         internal override string GetDeviceID(DeviceInfo deviceInfo)
         {
            return String.Empty;
         }

         public override int OutReportTimeOut
         {
            get
            {
               if (m_outReport != null)
                  return (int)(this.m_outReport.TimeOut);
               else return 0;
            }

            set { if (m_outReport != null) m_outReport.TimeOut = value; }
         }

         public override int InReportTimeOut
         {
            get {  
               if (m_inReport!=null) 
                  return (int)(this.m_inReport.TimeOut); 
               else return 0; 
               }
               
            set { if (m_inReport!=null) m_inReport.TimeOut = value;}
         }
         
      public override int OutReportLength {
         get { return (int)(this.m_outReport.ReportLength);}//.wOutReportBufferLength;}
         }
         
      public override int InReportLength {
         get { return (int)(this.m_inReport.ReportLength);}//wInReportBufferLength; }
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
      internal void CompleteHIDInReport(uint errcode, uint bytesRead, NativeOverlapped* pNativeOL)
      {
         m_inReport.BytesTransferred = (int)bytesRead;
         System.Diagnostics.Debug.WriteLine(String.Format("CompletHIDInReport: error={0}  bytesTransferred={1}", errcode, bytesRead));
         m_inReport.ErrCode = errcode;
         m_inReport.OverLappedEvent.Set();
         // int bytes =0;
         
         //if (!GetOverlappedResult(m_deviceHandle, m_inReport.NativeOverlapped, ref bytes, false)){
         //   int ec = Marshal.GetLastWin32Error();
         //   System.Diagnostics.Debug.WriteLine(String.Format("CompletHIDOutReport: GetOverlappedResult returned error={0}", ec.ToString()));
         //   //System.Diagnostics.Debug.WriteLine("CompletHIDInReport: GetOverlappedResult returned error={0}", ec.ToString());
         //   }
         //System.Diagnostics.Debug.Assert(bytes==bytesRead);
      }
      
      
      internal override unsafe ErrorCodes HIDInReport(byte [] buffer, ref int bytesCount)
         {
            ErrorCodes ec = ErrorCodes.NoErrors;
            int bytesRead = 0;//InReportLength;
            int packetSize =InReportLength;
              
           System.Diagnostics.Debug.Assert(bytesCount > 0);
           // m_inReport.NativeOverlapped.OffsetHigh=0;
           // m_inReport.NativeOverlapped.OffsetLow=0;
            m_inReport.OverLappedEvent.Reset();
            if (WindowsHidInterop.ReadFile(this.m_deviceHandle, m_inReport.Buffer, m_inReport.ReportLength, ref bytesRead, m_inReport.NativeOverlappedIntPtr)) {
               System.Diagnostics.Debug.WriteLine(String.Format("HIDInReport: bytesCount={0}  bytesRead={1}", bytesCount, bytesRead));
               bytesCount = (bytesCount < bytesRead ? bytesCount : bytesRead);
               Marshal.Copy(m_inReport.Buffer, buffer,0,bytesCount);
               //Array.Copy(m_inReport.Buffer, 0, buffer, 0, bytesCount);
               }  
            else {
                int lastError = Marshal.GetLastWin32Error();//GetLastError();
                if (lastError==997) {
                  if (m_inReport.OverLappedEvent.WaitOne(m_inReport.TimeOut, false)) {
                     bytesRead =  m_inReport.BytesTransferred;
                     bytesCount = (bytesCount < bytesRead ? bytesCount : bytesRead);
                     
                     Marshal.Copy(m_inReport.Buffer, buffer, 0, bytesCount);
                     //if (!got)    lastError = Marshal.GetLastWin32Error();
                     
                     //Array.Copy(m_inReport.Buffer, 0, buffer, 0, bytesCount);
                     }
                  else {
                     WindowsHidInterop.CancelIo(m_deviceHandle);
                     ec = ErrorCodes.UsbTimeoutError;
                     bytesCount = 0;
                     }
                  }
                else switch(lastError) {
                  case 22: 
                  case 1176:
                  case 31:
                     ec= ErrorCodes.DeviceNotResponding;
                     bytesCount = 0; 
                     break;
                     
                  case 2:
                     ec=ErrorCodes.InvalidMessage;
                     bytesCount = 0; 
                     break;
                     
                  case 6:
                     ec =ErrorCodes.InvalidDeviceHandle;
                     bytesCount = 0; 
                     break;
                     
                  default:
                     ec = ErrorCodes.UnknownError;
                     bytesCount = 0; 
                     break;
                  }
                  
                   
               }
               

            return ec;
         }


      internal void CompleteHIDOutReport(uint errcode, uint bytesWrote, NativeOverlapped* pNativeOL)
      {
         m_outReport.BytesTransferred = (int)bytesWrote;
         System.Diagnostics.Debug.WriteLine(String.Format("CompletHIDOutReport: error={0}  bytesTransferred={1}", errcode, bytesWrote));
         m_outReport.ErrCode = errcode;
         m_outReport.OverLappedEvent.Set();
         //int bytes=0;
         
         //if (!GetOverlappedResult(m_deviceHandle, m_outReport.NativeOverlapped, ref bytes, false)) {
         //   int ec = Marshal.GetLastWin32Error();
         //   System.Diagnostics.Debug.WriteLine(String.Format("CompletHIDOutReport: GetOverlappedResult returned error={0}", ec.ToString()));
         //   }
         //System.Diagnostics.Debug.Assert(bytes==bytesWrote);
      }
         //=============================================================================================================
         /// <summary>
         /// Overriden for bulk out request
         /// </summary>
         /// <param name="buffer">The buffer containing the data to send</param>
         /// <param name="count">The number of samples to send</param>
         /// <returns>The result</returns>
         //=============================================================================================================
         internal override unsafe ErrorCodes HIDOutReport(byte [] buffer, ref int bytesCount)
         {
            ErrorCodes ec = ErrorCodes.NoErrors;
            int bytesWrote= 0;//OutReportLength;
            int packetSize = OutReportLength;
               
            //Array.Copy( buffer,0, m_outReport.Buffer,0, bytesCount);
            Marshal.Copy(buffer, 0, m_outReport.Buffer, bytesCount);
            m_outReport.OverLappedEvent.Reset();

            if (WindowsHidInterop.WriteFile(this.m_deviceHandle, m_outReport.Buffer, m_outReport.ReportLength, ref bytesWrote, m_outReport.NativeOverlappedIntPtr)) {
               System.Diagnostics.Debug.WriteLine(String.Format("HIDOutReport: bytesCount={0}  bytesWrote={1}", bytesCount, bytesWrote));
               bytesCount = (bytesCount < bytesWrote ? bytesCount : bytesWrote);
               }  
            else {
                int lastError = Marshal.GetLastWin32Error();//GetLastError();
                if (lastError==997) {
                   if (m_outReport.OverLappedEvent.WaitOne(m_outReport.TimeOut, false))//m_outReport.TimeOut))
                   {
                     bytesWrote = m_outReport.BytesTransferred;
                   
                      lastError = Marshal.GetLastWin32Error();
                        
                      bytesCount = (bytesCount < bytesWrote ? bytesCount : bytesWrote);
                      
                   }
                   else
                   {
                      WindowsHidInterop.CancelIo(m_deviceHandle);
                      ec = ErrorCodes.UsbTimeoutError;
                      bytesCount = 0;
                   }
                  }
                else switch(lastError) {
                  case 22: 
                  case 1176:
                  case 31:
                     ec= ErrorCodes.DeviceNotResponding;
                     bytesCount = 0;
                     break;
                     
                  case 2:
                     ec=ErrorCodes.InvalidMessage;
                     bytesCount = 0;
                     break;
                     
                  case 6:
                     ec =ErrorCodes.InvalidDeviceHandle;
                     bytesCount = 0;
                     break;
                     
                  default:
                     ec = ErrorCodes.UnknownError;
                     bytesCount = 0;
                     break;
                  }
                  
                    
               }
            
            return ec;
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

//}
