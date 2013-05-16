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
using System.Threading;

namespace MeasurementComputing.DAQFlex
{
    //=================================================================================
    /// <summary>
    /// This is a light-weight class that contains data used for USB Bulk In requests
    /// </summary>
    //=================================================================================
#if WindowsCE
    internal unsafe class UsbBulkInRequest
    {
        internal int Index = 0;
        internal WinCeUsbOverlapped Overlapped;
        internal IntPtr UnmanagedOverlapped;
        internal byte[] Buffer = null;
        internal UsbBulkInRequest Next = null;
        internal int BytesReceived;
    }

    internal unsafe class UsbBulkOutRequest
    {
        internal int Index = 0;
        internal WinCeUsbOverlapped Overlapped;
        internal IntPtr UnmanagedOverlapped;
        internal byte[] Buffer = null;
        internal UsbBulkOutRequest Next = null;
        internal int BytesRequested;
    }

#else
    internal unsafe class UsbBulkInRequest
    {
        internal int Index = 0;
        internal Overlapped Overlapped = null;
        internal NativeOverlapped* NativeOverlapped = null;
        internal IntPtr NativeOverLappedIntPtr;
        internal byte[] Buffer = null;
        internal UsbBulkInRequest Next = null;
        internal int BytesRequested;
        internal int BytesReceived;
    }

    internal unsafe class UsbBulkOutRequest
    {
        internal int Index = 0;
        internal Overlapped Overlapped = null;
        internal NativeOverlapped* NativeOverlapped = null;
        internal IntPtr NativeOverLappedIntPtr;
        internal byte[] Buffer = null;
        internal UsbBulkOutRequest Next = null;
        internal int BytesRequested;
    }

#endif
}
