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
using System.Reflection;

namespace MeasurementComputing.DAQFlex
{
    public partial class DaqDeviceManager
    {
        //==================================================================================================
        /// <summary>
        /// Factory class to create instances of daq device objects supported by the DAQFlex API.
        /// DaqDeviceFactory is a protected member of DaqDeviceManager so only DaqDeviceManager can use it
        /// </summary>
        //==================================================================================================
        protected class DaqDeviceFactory
        {
            private static Dictionary<int, string> m_supportedDevices = new Dictionary<int, string>();

            //============================================================================================
            /// <summary>
            /// Uses reflection to create the specific DaqDevice object
            /// </summary>
            /// <param name="deviceInfo">A deviceInfo object</param>
            /// <returns>An instance of the DaqDevice object associated with the device PID</returns>
            //============================================================================================
            internal static DaqDevice CreateDeviceObject(DeviceInfo deviceInfo)
            {
                if (m_supportedDevices.Count == 0)
                    GenerateSupportedDeviceList();

                // create a specific daq device sub-class
                string targetDeviceClass;

                if (m_supportedDevices.TryGetValue(deviceInfo.Pid, out targetDeviceClass))
                {
                    // get the assembly
                    Assembly DAQFlexApi = Assembly.GetExecutingAssembly();

                    // get the namespace of the daq device base class
                    string nameSpace = typeof(DaqDevice).Namespace;

                    // get the class type associated with the target device class name
                    Type deviceClassType = DAQFlexApi.GetType(String.Format("{0}.{1}", nameSpace, targetDeviceClass));

                    // get the constructor info from the class type
                    ConstructorInfo ci = deviceClassType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, 
                                                                        null,
                                                                        new Type[] { typeof(DeviceInfo) },
                                                                        null);

                    // invoke the constructor to create the DaqDevice object
                    try
                    {
                        DaqDevice targetDeviceObject = (DaqDevice)ci.Invoke(new Object[] { deviceInfo });
                        return targetDeviceObject;
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException is DaqException)
                        {
                            DaqException dex = ex.InnerException as DaqException;
                            throw dex;
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
                else
                {
                    throw new DaqException(ErrorMessages.InvalidDeviceIdentifier, ErrorCodes.InvalidDeviceIdentifier);
                }
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
                string deviceClass;

                if (m_supportedDevices.Count == 0)
                    GenerateSupportedDeviceList();

                return m_supportedDevices.TryGetValue(pid, out deviceClass);
            }

            //===========================================================================
            /// <summary>
            /// ctor
            /// </summary>
            //===========================================================================
            protected DaqDeviceFactory()
            {
            }

            //======================================================================
            /// <summary>
            /// Populates a lists of devices supported by this API
            /// </summary>
            //======================================================================
            protected static void GenerateSupportedDeviceList()
            {
                // key = PID, value = device class name
                m_supportedDevices.Add((int)DeviceIDs.Usb7204ID, DaqDeviceClassNames.USB_7204);
                m_supportedDevices.Add((int)DeviceIDs.Usb7202ID, DaqDeviceClassNames.USB_7202);
                m_supportedDevices.Add((int)DeviceIDs.Usb2001TcID, DaqDeviceClassNames.USB_2001_TC);
            }
        }
    }
}
