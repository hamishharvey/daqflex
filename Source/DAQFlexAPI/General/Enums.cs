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
    public enum DeviceNameFormat
    {
        NameOnly,
        NameAndSerno,
        NameAndID,
        NameSernoAndID
    }

    //internal enum MessageDataType
    //{
    //    None,
    //    Ai,
    //    Ao,
    //    Dio,
    //    Ctr,
    //    Tmr
    //}

    internal enum UsbPipeType
    {
        Control = 0,
        Isochronous = 1,
        Bulk = 2,
        Interrupt = 3,
    }
    
    internal enum UsbDeviceConfigInfoFlags
    {
        Default = 1,
        Present = 2,
        AllClasses = 4,
        Profile = 8,
        DeviceInterface = 16,
    }

    internal enum UsbTransferTypes
    {
        ControlIn = 0,
        ControlOut = 1,
        BulkIn = 2,
        BulkOut = 3,
    }

    internal enum SampleMode
    {
        Finite = 0,
        Continuous = 1,
    }

    internal enum TransferMode
    {
        Default,
        BlockIO,
        SingleIO,
        BurstIO,
    }

    internal enum FeatureImplementation
    {
        Fixed,
        Programmable,
        HWSelectable,
        NotApplicable,
    }

    internal enum ScanState
    {
        Idle = 0,
        Running = 1,
        Overrun = 2,
        Underrun = 3,
    }

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

    internal enum ResponseType
    {
        // Single is Text response only
        Simple,

        // Composite is Text and Numeric response
        Composite
    }

    //internal enum SyncMode
    //{
    //    Master,
    //    Slave,
    //    GatedSlave,
    //}

    internal enum ClockSource
    {
        Internal,
        External
    }

    internal enum InputConversionMode
    {
        Simultaneous,
        Multiplexed
    }

    internal enum QueueAction
    {
        Enqueue,
        Dequeue
    }

    internal enum AiChannelMode
    {
        SingleEnded,
        Differential
    }

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

    internal enum TemperatureUnits
    {
        None,
        Celsius,
        Fahrenheit,
        Kelvin
    }

    internal enum CounterTypes
    {
        Type1,
        Type2,
        Type3,
        Type4,
        Event = 5,
    }

    public enum CallbackOperation
    {
        InputScan,
        OutputScan,
    }

    public enum CallbackType
    {
        OnDataAvailable = 1,
        OnInputScanComplete = 2,
        OnInputScanError = 4,
    }


    //===============================================================
    /// <summary>
    /// List of assigned device IDs
    /// </summary>
    //===============================================================
    internal enum DeviceIDs
    {
        Usb7204ID = 240,
        Usb7202ID = 242,
        Usb2001TcID = 249,
    }
}
