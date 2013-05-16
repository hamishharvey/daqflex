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

namespace MeasurementComputing.DAQFlex
{
    internal class DeviceInfo
    {
        private int m_vid;
        private int m_pid;
        private int m_deviceNumber;
        private string m_serialNumber;
        private string m_devicePath;
        private string m_displayName;
        private string m_deviceName;
        private string m_deviceID;
        private IntPtr m_usbDevicePtr;
        private IntPtr m_deviceHandle;
        private IntPtr m_deviceDescriptor;
        private byte m_endpointIn;
        private byte m_endpointOut;
        private int m_maxPacketSize;
        private uint m_handle;

        internal int Vid
        {
            get { return m_vid; }
            set { m_vid = value; }
        }

        internal int Pid
        {
            get { return m_pid; }
            set { m_pid = value; }
        }

        internal int DeviceNumber
        {
            get { return m_deviceNumber; }
            set { m_deviceNumber = value; }
        }

        internal string SerialNumber
        {
            get { return m_serialNumber; }
            set { m_serialNumber = value; }
        }

        internal string DevicePath
        {
            get { return m_devicePath; }
            set { m_devicePath = value; }
        }

        internal string DisplayName
        {
            get { return m_displayName; }
            set { m_displayName = value; }
        }

        internal string DeviceName
        {
            get { return m_deviceName; }
            set { m_deviceName = value; }
        }

        internal string DeviceID
        {
            get { return m_deviceID; }
            set { m_deviceID = value; }
        }

        internal IntPtr UsbDevicePtr
        {
            get { return m_usbDevicePtr; }
            set { m_usbDevicePtr = value; }
        }

        internal IntPtr DeviceHandle
        {
            get { return m_deviceHandle; }
            set { m_deviceHandle = value; }
        }

        internal uint Handle
        {
            get { return m_handle; }
            set { m_handle = value; }
        }

        internal IntPtr DeviceDescriptor
        {
            get { return m_deviceDescriptor; }
            set { m_deviceDescriptor = value; }
        }

        internal byte EndPointIn
        {
            get { return m_endpointIn; }
            set { m_endpointIn = value; }
        }

        internal byte EndPointOut
        {
            get { return m_endpointOut; }
            set { m_endpointOut = value; }
        }

        internal int MaxPacketSize
        {
            get { return m_maxPacketSize; }
            set { m_maxPacketSize = value; }
        }
    }
}
