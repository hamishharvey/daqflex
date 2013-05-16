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
    //=====================================================
    /// <summary>
    /// List of supported device name formats
    /// </summary>
    //=====================================================
    public enum DeviceNameFormat
    {
        NameOnly,
        NameAndSerno,
        NameAndID,
        NameSernoAndID
    }

    //=====================================================
    /// <summary>
    /// List of supported USB endpoint types
    /// </summary>
    //=====================================================
    internal enum UsbPipeType
    {
        Control = 0,
        Isochronous = 1,
        Bulk = 2,
        Interrupt = 3,
    }

    //=====================================================
    /// <summary>
    /// List of USB config info flags
    /// </summary>
    //=====================================================
    internal enum UsbDeviceConfigInfoFlags
    {
        Default = 1,
        Present = 2,
        AllClasses = 4,
        Profile = 8,
        DeviceInterface = 16,
    }

    //=====================================================
    /// <summary>
    /// List of supported USB transfer types
    /// </summary>
    //=====================================================
    internal enum UsbTransferTypes
    {
        ControlIn = 0,
        ControlOut = 1,
        BulkIn = 2,
        BulkOut = 3,
    }

    //=====================================================
    /// <summary>
    /// List of supported sampling modes for
    /// input and output scans
    /// </summary>
    //=====================================================
    internal enum SampleMode
    {
        Finite = 0,
        Continuous = 1,
    }

    //=====================================================
    /// <summary>
    /// List of supported input scan transfer modes
    /// </summary>
    //=====================================================
    internal enum TransferMode
    {
        Default,
        BlockIO,
        SingleIO,
        BurstIO,
    }

    //=====================================================
    /// <summary>
    /// list of supported scan states
    /// </summary>
    //=====================================================
    internal enum ScanState
    {
        Idle = 0,
        Running = 1,
        Overrun = 2,
        Underrun = 3,
    }

    //=====================================================
    /// <summary>
    /// list of hw-paced scan types
    /// </summary>
    //=====================================================
    internal enum ScanType
    {
        None,
        AnalogInput,
        AnalogOutput,
        DigitalInput,
        DigitalOutput,
        CounterInput,
        CompositeInput,
        CompositeOutput,
    }

    //=====================================================
    /// <summary>
    /// list of data types supported by various devices
    /// </summary>
    //=====================================================
    internal enum DeviceDataTypes
    {
        Bool = 0x00,
        Char = 0x01,
        SChar = 0x02,
        UChar = 0x03,
        SShort = 0x04,
        UShort = 0x05,
        SInt = 0x06,
        UInt = 0x07,
        SLong = 0x08,
        ULong = 0x09,
        Float = 0x0A,
        Double = 0x0B,
        LDouble = 0x0C,
        SLLong = 0x0D,
        ULLong = 0x0E,
        Invalid = 0x0F
    }

    //=====================================================
    /// <summary>
    /// List of analog input conversion modes
    /// </summary>
    //=====================================================
    internal enum InputConversionMode
    {
        Simultaneous,
        Multiplexed
    }

    //==========================================================
    /// <summary>
    /// List of queue actions to support concurrent operations
    /// on queues
    /// </summary>
    //==========================================================
    internal enum QueueAction
    {
        Enqueue,
        Dequeue
    }

    //==========================================================
    /// <summary>
    /// 
    /// </summary>
    //==========================================================
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum DeviceListUsage
    {
        ReuseList,
        RefreshList,
        UpdateList
    }

    //internal enum AiChannelMode
    //{
    //    SingleEnded,
    //    Differential,
    //    Otd,
    //    NoOtd
    //}

    //===============================================================
    /// <summary>
    /// List of supported analog input channel types
    /// </summary>
    //===============================================================
    internal enum AiChannelTypes
    {
        Voltage,
        Temperature,
    }

    //===============================================================
    /// <summary>
    /// List of supported thermocouple types
    /// </summary>
    //===============================================================
    internal enum ThermocoupleTypes
    {
        NotSet,
        TypeB,
        TypeE,
        TypeJ,
        TypeK,
        TypeN,
        TypeR,
        TypeS,
        TypeT
    }

    //===============================================================
    /// <summary>
    /// List of supported temperature units
    /// </summary>
    //===============================================================
    internal enum TemperatureUnits
    {
        None,
        Celsius,
        Fahrenheit,
        Kelvin,
        Volts
    }

    //===============================================================
    /// <summary>
    /// List of various supported counter types
    /// </summary>
    //===============================================================
    internal enum CounterTypes
    {
        Event = 1,
    }

    //===============================================================
    /// <summary>
    /// List of events that invoke callback methods
    /// </summary>
    //===============================================================
    public enum CallbackType
    {
        OnDataAvailable = 1,
        OnInputScanComplete = 2,
        OnInputScanError = 4,
        OnAcquisitionArmed = 8,
    }


    //===============================================================
    /// <summary>
    /// List of assigned device IDs
    /// </summary>
    //===============================================================
    internal enum DeviceIDs
    {
        VirtualDeviceID = 0x00,
        Usb7204ID = 0xf0,
        Usb7202ID = 0xf2,
        Usb2001TcID = 0xf9,
        Usb2408ID = 0xfd,
        Usb2408_2AoID = 0xfe,
        Usb1608GID = 0x110,
        Usb1608GXID = 0x111,
        Usb1608GX2AoID = 0x112,
        Usb1208FSPlus = 0xe8,
        Usb1408FSPlus = 0xe9,
        Usb1608FSPlus = 0xea,
        Usb7110 = 0x116,
        Usb7112 = 0x117,
        Usb711xBOOT = 0x8116,
        Usb1208FSPlusBOOT= 0x80e8,
        Usb1408FSPlusBOOT= 0x80e9,
        Usb1608FSPlusBOOT= 0x80ea,
        Usb201 = 0x113,
        Usb201BOOT = 0x8113,
        Usb204 = 0x114,
        Usb204BOOT = 0x8114,
    }
}
