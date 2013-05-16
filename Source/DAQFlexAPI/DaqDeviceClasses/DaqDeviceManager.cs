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
using System.ComponentModel;
using System.Text;

namespace MeasurementComputing.DAQFlex
{
    //============================================================================================
    /// <summary>
    /// Provides a set of static methods to obtain device information
    /// </summary>
    //============================================================================================
    public partial class DaqDeviceManager
    {
        private const string m_defaultCultureName = "en-US";

        private static Dictionary<int, DeviceInfo> m_deviceInfoList = new Dictionary<int, DeviceInfo>();
        private static List<string> m_deviceNames = new List<string>();
        private static List<DaqDevice> m_daqDeviceList = new List<DaqDevice>();
        private static DeviceNameFormat m_nameFormat;

        //========================================================================================================
        /// <summary>
        /// creates a list of devices that were detected and have a specific vendor id
        /// </summary>
        /// <param name="format">The format to return device names in</param>
        /// <returns>The list of device names</returns>
        //========================================================================================================
        public static string[] GetDeviceNames(DeviceNameFormat format)
        {
            return GetDeviceNames(format, DeviceListUsage.ReuseList);
        }

        //========================================================================================================
        /// <summary>
        /// creates a list of devices that were detected and have a specific vendor id
        /// </summary>
        /// <param name="format">The format to return device names in</param>
        /// <param name="format">A flag indicating if the current list should be used or recreated</param>
        /// <returns>The list of device names</returns>
        //========================================================================================================
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string[] GetDeviceNames(DeviceNameFormat format, DeviceListUsage deviceListUsage)
        {
            m_nameFormat = format;

            if (deviceListUsage == DeviceListUsage.UpdateList)
                return GetUnusedDeviceNames(format);

            if (m_daqDeviceList.Count > 0)
                throw new DaqException(ErrorMessages.DaqDeviceListNotEmpty, ErrorCodes.DaqDeviceListNotEmpty);

            PlatformInterop platformInterop = PlatformInterop.GetUsbPlatformInterop();

            if (platformInterop != null)
            {
                if (m_deviceInfoList.Count == 0 || 
                        m_deviceNames.Count == 0 || 
                            deviceListUsage == DeviceListUsage.RefreshList ||
                                deviceListUsage == DeviceListUsage.UpdateList)
                {
                    /* get a list of detected devices */
                    ErrorCodes er = platformInterop.GetDevices(m_deviceInfoList, deviceListUsage);
    				
                    /* create a DeviceInfo object for the virtual device */
                    //AddVirtualDeviceInfo();

				    if (er == ErrorCodes.LibusbCouldNotBeInitialized)
                        throw new DaqException(ErrorMessages.LibusbCouldNotBeInitialized, er);
    				
				    if (er == ErrorCodes.LibusbCouldNotBeLoaded)
                        throw new DaqException(ErrorMessages.LibusbCouldNotBeLoaded, er);

				    if (er == ErrorCodes.LibUsbGetDeviceDescriptorFailed)
                        throw new DaqException(ErrorMessages.LibUsbGetDeviceDescriptorFailed, er);
                }

                m_deviceNames.Clear();

				foreach (KeyValuePair<int, DeviceInfo> kvp in m_deviceInfoList)
                {
                    DeviceInfo di = kvp.Value;

                    di.DeviceID = platformInterop.GetDeviceID(kvp.Value);

                    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                        di.SerialNumber = platformInterop.GetSerno(kvp.Value);

                    if (m_nameFormat == DeviceNameFormat.NameOnly)
                    {
                        m_deviceNames.Add(di.DisplayName);
                    }
                    else if (m_nameFormat == DeviceNameFormat.NameAndSerno)
                    {
                        m_deviceNames.Add(String.Format("{0}{1}{2}", di.DisplayName, Constants.DEVICE_NAME_SEPARATOR, di.SerialNumber));
                    }
                    else if (m_nameFormat == DeviceNameFormat.NameAndID)
                    {
                        if (di.DeviceID != String.Empty)
                        {
                            m_deviceNames.Add(String.Format("{0}{1}{2}", di.DisplayName, Constants.DEVICE_NAME_SEPARATOR, di.DeviceID));
                        }
                    }
                    else if (m_nameFormat == DeviceNameFormat.NameSernoAndID)
                    {
                        if (di.DeviceID != String.Empty)
                        {
                            m_deviceNames.Add(String.Format("{0}{1}{2}{3}{4}", di.DisplayName, Constants.DEVICE_NAME_SEPARATOR, di.SerialNumber, Constants.DEVICE_NAME_SEPARATOR, di.DeviceID));
                        }
                    }
                }
            }
            else
            {
                throw new DaqException(ErrorMessages.PlatformNotSupported, ErrorCodes.PlatformNotSupported);
            }


            return m_deviceNames.ToArray();
        }

        //================================================================================================================
        /// <summary>
        /// creates a list of device names for devices that weren't detected from a previous call to GetUnusedDeviceNames
        /// </summary>
        /// <param name="format">The format to return device names in</param>
        /// <param name="format">A flag indicating if the current list should be used or recreated</param>
        /// <returns>The list of device names</returns>
        //================================================================================================================
        protected static string[] GetUnusedDeviceNames(DeviceNameFormat format)
        {
            List<string> unusedDeviceNames = new List<string>();

            m_nameFormat = format;

            PlatformInterop platformInterop = PlatformInterop.GetUsbPlatformInterop();

            if (platformInterop != null)
            {
                /* get a list of detected devices */
                ErrorCodes er = platformInterop.GetDevices(m_deviceInfoList, DeviceListUsage.UpdateList);

                /* create a DeviceInfo object for the virtual device */
                //AddVirtualDeviceInfo();

                if (er == ErrorCodes.LibusbCouldNotBeInitialized)
                    throw new DaqException(ErrorMessages.LibusbCouldNotBeInitialized, er);

                if (er == ErrorCodes.LibusbCouldNotBeLoaded)
                    throw new DaqException(ErrorMessages.LibusbCouldNotBeLoaded, er);

                if (er == ErrorCodes.LibUsbGetDeviceDescriptorFailed)
                    throw new DaqException(ErrorMessages.LibUsbGetDeviceDescriptorFailed, er);

                m_deviceNames.Clear();

                foreach (KeyValuePair<int, DeviceInfo> kvp in m_deviceInfoList)
                {
                    string name = String.Empty;
                    DeviceInfo di = kvp.Value;

                    /* get the device ID */
                    di.DeviceID = platformInterop.GetDeviceID(kvp.Value);

                    /* get the serial number */
                    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                        di.SerialNumber = platformInterop.GetSerno(kvp.Value);

                    if (m_nameFormat == DeviceNameFormat.NameOnly)
                    {
                        name = di.DisplayName;
                    }
                    else if (m_nameFormat == DeviceNameFormat.NameAndSerno)
                    {
                        name = String.Format("{0}{1}{2}", di.DisplayName, Constants.DEVICE_NAME_SEPARATOR, di.SerialNumber);
                    }
                    else if (m_nameFormat == DeviceNameFormat.NameAndID)
                    {
                        if (di.DeviceID != String.Empty)
                        {
                            name = String.Format("{0}{1}{2}", di.DisplayName, Constants.DEVICE_NAME_SEPARATOR, di.DeviceID);
                        }
                    }
                    else if (m_nameFormat == DeviceNameFormat.NameSernoAndID)
                    {
                        if (di.DeviceID != String.Empty)
                        {
                            name = String.Format("{0}{1}{2}{3}{4}", di.DisplayName, Constants.DEVICE_NAME_SEPARATOR, di.SerialNumber, Constants.DEVICE_NAME_SEPARATOR, di.DeviceID);
                        }
                    }

                    /* add all names to the internal list */
                    if (name != String.Empty)
                        m_deviceNames.Add(name);

                    /* add only unused device names to the return list */
                    bool addToUnusedList = true;

                    foreach (DaqDevice device in m_daqDeviceList)
                    {
                        if (device.DeviceInfo.DisplayName == di.DisplayName && device.DeviceInfo.SerialNumber == di.SerialNumber)
                        {
                            addToUnusedList = false;
                        }
                    }

                    if (addToUnusedList)
                        unusedDeviceNames.Add(name);
                }
            }
            else
            {
                throw new DaqException(ErrorMessages.PlatformNotSupported, ErrorCodes.PlatformNotSupported);
            }

            return unusedDeviceNames.ToArray();
        }

        //=========================================================================================================
        /// <summary>
        /// Creates a DaqDevice object specific to the device identified by deviceName, serial number or device ID
        /// </summary>
        /// <param name="deviceName">The name of the device</param>
        /// <returns>A DaqDevice object</returns>
        //=========================================================================================================
        public static DaqDevice CreateDevice(string deviceName)
        {
            DeviceInfo deviceInfo = null;
            bool deviceNameFound = false;
            bool deviceIDFound = false;
            bool deviceSernoFound = false;

            // The format of the names can be any of the following
            // 
            //  DeviceName
            //  DeviceName::SerialNumber
            //  DeviceName::ID
            //  DeviceName::SerialNumber::ID

            List<String> interimNames = new List<string>();
            string[] nameParts;
            string[] splitString = deviceName.Split(Constants.DEVICE_NAME_SEPARATOR.ToCharArray());

            foreach (string s in splitString)
            {
                if (s != String.Empty)
                    interimNames.Add(s);
            }

            nameParts = interimNames.ToArray();

            foreach (KeyValuePair<int, DeviceInfo> kvp in m_deviceInfoList)
            {
                deviceNameFound = false;
                deviceIDFound = false;
                deviceSernoFound = false;

                if (m_nameFormat == DeviceNameFormat.NameOnly)
                {
                    // DeviceName
                    if (kvp.Value.DisplayName == nameParts[0])
                    {
                        deviceNameFound = true;
                        deviceInfo = kvp.Value;
                        break;
                    }
                }
                if (m_nameFormat == DeviceNameFormat.NameAndSerno)
                {
                    // DeviceName::SerialNumber
                    if (nameParts.Length >= 1 && kvp.Value.DisplayName == nameParts[0])
                    {
                        deviceNameFound = true;

                        if (nameParts.Length >= 2 && kvp.Value.SerialNumber == nameParts[1])
                        {
                            deviceSernoFound = true;
                            deviceInfo = kvp.Value;
                            break;
                        }
                    }
                }
                if (m_nameFormat == DeviceNameFormat.NameAndID)
                {
                    // DeviceName::ID
                    if (nameParts.Length >= 1 && kvp.Value.DisplayName == nameParts[0])
                    {
                        deviceNameFound = true;

                        if (nameParts.Length >= 2 && kvp.Value.DeviceID == nameParts[1])
                        {
                            deviceIDFound = true;
                            deviceInfo = kvp.Value;
                            break;
                        }
                    }
                }
                if (m_nameFormat == DeviceNameFormat.NameSernoAndID)
                {
                    // DeviceName::SerialNumber::ID
                    if (nameParts.Length >=1 && kvp.Value.DisplayName == nameParts[0])
                    {
                        deviceNameFound = true;

                        if (nameParts.Length >= 2 && kvp.Value.SerialNumber == nameParts[1])
                        {
                            deviceSernoFound = true;

                            if (nameParts.Length >= 3 && kvp.Value.DeviceID == nameParts[2])
                            {
                                deviceSernoFound = true;
                                deviceInfo = kvp.Value;
                                break;
                            }
                        }
                    }
                }
            }

            if (deviceInfo == null)
            {
                if (m_nameFormat == DeviceNameFormat.NameOnly)
                {
                    throw new DaqException(ErrorMessages.DeviceNotFoundInList, ErrorCodes.DeviceNotFoundInList);
                }
                else if (m_nameFormat == DeviceNameFormat.NameAndSerno)
                {
                    if (!deviceNameFound)
                        throw new DaqException(ErrorMessages.DeviceNotFoundInList, ErrorCodes.DeviceNotFoundInList);

                    if (!deviceSernoFound)
                        throw new DaqException(ErrorMessages.InvalidDeviceSerno, ErrorCodes.InvalidDeviceSerno);
                }
                else if (m_nameFormat == DeviceNameFormat.NameAndID)
                {
                    if (!deviceNameFound)
                        throw new DaqException(ErrorMessages.DeviceNotFoundInList, ErrorCodes.DeviceNotFoundInList);

                    if (!deviceIDFound)
                        throw new DaqException(ErrorMessages.InvalidDeviceId, ErrorCodes.InvalidDeviceId);
                }
                else if (m_nameFormat == DeviceNameFormat.NameSernoAndID)
                {
                    if (!deviceNameFound)
                        throw new DaqException(ErrorMessages.DeviceNotFoundInList, ErrorCodes.DeviceNotFoundInList);

                    if (!deviceSernoFound)
                        throw new DaqException(ErrorMessages.InvalidDeviceSerno, ErrorCodes.InvalidDeviceSerno);

                    if (!deviceIDFound)
                        throw new DaqException(ErrorMessages.InvalidDeviceId, ErrorCodes.InvalidDeviceId);
                }
            }

            DaqDevice device = null;

            try
            {
                // if the device was found, create a DaqDevice object for it
                device = DaqDeviceFactory.CreateDeviceObject(deviceInfo);

                // load the device caps
                device.LoadDeviceCaps(false);

                // check if the device is potentially in use 
                // (this needs to have the device caps loaded)
                ErrorCodes errorCode = device.CheckUsage();

                if (errorCode != ErrorCodes.NoErrors)
                {
                    DaqException dex = device.ResolveException(errorCode);
                    throw (dex);
                }

                // Initialize device
                device.Initialize();

                // add the device to the device list
                m_daqDeviceList.Add(device);

                return device;
            }
            catch (DaqException dex)
            {
                    // set the device to null
                device = null;

                // A DaqDevice ctor can throw an exeption if a file handle was already
                // created for the device
                throw (dex);
            }
        }

        //======================================================================
        /// <summary>
        /// Lets the driver free any resources associated with the device
        /// </summary>
        //======================================================================
        public static void ReleaseDevice(DaqDevice daqDevice)
        {
            daqDevice.ReleaseDevice();

            m_daqDeviceList.Remove(daqDevice);
        }

        //======================================================================
        /// <summary>
        /// Creates a DeviceInfo object for a virtual device
        /// </summary>
        //======================================================================
        internal static void AddVirtualDeviceInfo()
        {
            DeviceInfo di = new DeviceInfo();

            di.DeviceNumber = m_deviceInfoList.Count;
            di.DeviceID = String.Empty;
            di.DeviceName = di.DisplayName = "VIRTUAL-DEVICE";
            di.SerialNumber = "00000001"; // make dynamic for support of more than 1
            di.Pid = 0x0000;
            di.Vid = 0x09DB;

            m_deviceInfoList.Add(di.DeviceNumber, di);
        }

        //===========================================================================
        /// <summary>
        /// Determines if the device with the specified pid is supported by this API
        /// </summary>
        /// <param name="pid">The device's PID</param>
        /// <returns>True if the device is supported otherwise false</returns>
        //===========================================================================
        internal static bool IsSupportedDevice(int pid)
        {
            return DaqDeviceFactory.IsSupportedDevice(pid);
        }

#warning make sure unit tests and devsCapsManager compile without these
        ////===========================================================================
        ///// <summary>
        ///// Gets the device's PID from the device list
        ///// </summary>
        ///// <param name="index">Index of device</param>
        ///// <returns>The PID</returns>
        ////===========================================================================
        //internal static int GetDeviceID(int index)
        //{
        //    int devID = 0;

        //    if (index < m_deviceInfoList.Count)
        //        devID = m_deviceInfoList[index].Pid;

        //    return devID;
        //}

        ////===========================================================================
        ///// <summary>
        ///// Gets a device's index into the device info list based on device ID and serno
        ///// </summary>
        ///// <param name="devID">The device ID</param>
        ///// <param name="serno">The serno</param>
        ///// <returns>The index</returns>
        ////===========================================================================
        //internal static int GetDeviceIndex(long devID, ulong serno)
        //{
        //    int index = -1;

        //    foreach (KeyValuePair<int, DeviceInfo> kvp in m_deviceInfoList)
        //    {
        //        ulong sn = UInt64.Parse(kvp.Value.SerialNumber, System.Globalization.NumberStyles.AllowHexSpecifier);

        //        index++;

        //        if (kvp.Value.Pid == devID && sn == serno)
        //        {
        //            return index;
        //        }
        //    }

        //    return index;
        //}

        ////===========================================================================
        ///// <summary>
        ///// Gets the device's serial number from the device list
        ///// </summary>
        ///// <param name="index">Index of device</param>
        ///// <returns>The serial number</returns>
        ////===========================================================================
        //internal static long GetDeviceSerno(int index)
        //{
        //    long serno = 0;

        //    try
        //    {
        //        if (index < m_deviceInfoList.Count)
        //            serno = Int64.Parse(m_deviceInfoList[index].SerialNumber, System.Globalization.NumberStyles.AllowHexSpecifier);
        //    }
        //    catch (Exception)
        //    {
        //    }

        //    return serno;
        //}

        //======================================================================================
        /// <summary>
        /// Gets the device name based on the product ID
        /// </summary>
        /// <param name="pid">The product ID</param>
        /// <returns>Name of the device</returns>
        //======================================================================================
        internal static string GetDeviceName(int pid)
        {
            string deviceName = String.Empty;

            DeviceIDs deviceId = (DeviceIDs)pid;

            switch (deviceId)
            {
                case (DeviceIDs.Usb7202ID):
                    deviceName = "USB-7202";
                    break;
                case (DeviceIDs.Usb7204ID):
                    deviceName = "USB-7204";
                    break;
                case (DeviceIDs.Usb2001TcID):
                    deviceName = "USB-2001-TC";
                    break;
                case (DeviceIDs.Usb2408ID):
                    deviceName = "USB-2408";
                    break;
                case (DeviceIDs.Usb2408_2AoID):
                    deviceName = "USB-2408-2AO";
                    break;
                case (DeviceIDs.Usb1608GID):
                    deviceName = "USB-1608G";
                    break;
                case (DeviceIDs.Usb1608GXID):
                    deviceName = "USB-1608GX";
                    break;
                case (DeviceIDs.Usb1608GX2AoID):
                    deviceName = "USB-1608GX-2AO";
                    break;
                case (DeviceIDs.Usb201):
                    deviceName = "USB-201";
                    break;
                case (DeviceIDs.Usb204):
                    deviceName = "USB-204";
                    break;
                    
                 case (DeviceIDs.Usb1208FSPlus):
                     deviceName = "USB-1208FS-Plus";
                     break;
                     
                case DeviceIDs.Usb1408FSPlus:
                     deviceName = "USB-1408FS-Plus";
                     break;
                     
                case (DeviceIDs.Usb1608FSPlus):
                    deviceName = "USB-1608FS-Plus";
                    break;

                case (DeviceIDs.Usb1208FSPlusBOOT):
                    deviceName = "USB-1208FS-Plus-BootLoader";
                    break;

                case (DeviceIDs.Usb201BOOT):
                    deviceName = "USB-201-BootLoader";
                    break;
                case (DeviceIDs.Usb204BOOT):
                    deviceName = "USB-204-BootLoader";
                    break;

                case (DeviceIDs.Usb1408FSPlusBOOT):
                    deviceName = "USB-1408FS-Plus-BootLoader";
                    break;

                case (DeviceIDs.Usb1608FSPlusBOOT):
                    deviceName = "USB-1608FS-Plus-BootLoader";
                    break;

                case DeviceIDs.Usb7110:
                    deviceName = "USB-7110";
                    break;

                case DeviceIDs.Usb7112:
                    deviceName = "USB-7112";
                    break;

                case DeviceIDs.Usb711xBOOT:
                    deviceName = "USB-711x-BootLoader";
                    break;
                    
                default:
                    deviceName = "Unknown Device";
                    break;
            }

            return deviceName;
        }

        //=====================================================================
        /// <summary>
        /// Returns the default culture name
        /// </summary>
        //=====================================================================
        internal static string DefaultCultureName
        {
            get { return m_defaultCultureName; }
        }

        //=====================================================================
        /// <summary>
        /// protected ctor - this class has only static methods
        /// </summary>
        //=====================================================================
        protected DaqDeviceManager()
        {
        }
    }
}
