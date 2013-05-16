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
    public enum ErrorCodes
    {
        NoErrors = 0,

        // Sytem Errors
        UnknownError = -1,
        PlatformNotSupported = 1001,

        // Warnings
        DataOverrun = 2001,
        DataUnderrun = 2002,
        OpenThermocouple = 2003,
        MaxTempRange = 2004,
        MinTempRange = 2005,
        DeviceIDNotSet = 2006,

        // Device Errors
        DeviceNotResponding = 4000,
        DeviceNotInitialized = 4001,
        DeviceNotFoundInList = 4002,
        DeviceHandleAlreadyCreated = 4003,
        InvalidDeviceIdentifier = 4004,
        InvalidDeviceSerno = 4005,
        InvalidDeviceId = 4006,
        NoDevicesDetected = 4007,
        ErrorReadingDeviceMemory = 4008,
        ErrorWritingDeviceMemory = 4009,
        CountGreaterThanMaxLength = 4010,
        AInNotResponding = 4011,
        AOutNotResponding = 4012,
        DioNotResponding = 4013,
        CtrNotResponding = 4014,
        GainQueueNotSupported = 4015,
        GainQueueDepthExceeded = 4016,
        DaqDeviceListNotEmpty = 4017,
        FpgaNotLoaded = 4018,
        AiCalibrationInProgress = 4019,
        AoCalibrationInProgress = 4020,
        BurstIoInProgress = 4021,
        DeviceHasBeenReleased = 4022,

        // General message errors
        InvalidMessage = 4100,
        InvalidComponentSpecified = 4101,
        InvalidPropertySpecified = 4102,
        InvalidPropertyValueSpecified = 4103,
        InvalidDeviceHandle = 4104,
        BadPointer = 4105,
        CallbackOperationAlreadyEnabled = 4106,
        MessageIsEmpty = 4107,
        MinAiCalSlopeValueReached = 4108,
        MaxAiCalSlopeValueReached = 4109,
        MinAiCalOffsetValueReached = 4110,
        MaxAiCalOffsetValueReached = 4111,
        DeviceHandleIsNull = 4112,

        // Invalid parameter errors
        InvalidScanRateSpecified = 4200,
        InvalidAiChannelSpecified = 4201,
        InvalidAoChannelSpecified = 4202,
        InvalidDioPortSpecified = 4203,
        InvalidCtrChannelSpecified = 4204,
        InvalidAiRange = 4205,
        InvalidDACValue = 4206,
        InvalidAiChannelMode = 4207,
        InvalidInputScanXferMode = 4208,
        InvalidAiTriggerType = 4209,
        ThermocoupleTypeNotSet = 4210,
        InvalidPortConfig = 4211,
        IncorrectPortConfig = 4212,
        InvalidCountValueSpecified = 4213,
        InvalidConfigType = 4214,
        InvalidConfigItem = 4215,
        InvalidDioBitSpecified = 4216,
        CallbackCountGreaterThanRequestedSamples = 4217,
        CallbackCountTooLarge = 4218,
        InputScanReadCountIsZero = 4219,
        InvalidDioBitValue = 4220,
        InvalidDioPortValue = 4221,
        DioPortNotSetForOutput = 4222,
        DioBitNotSetForOutput = 4223,
        CantWriteDioPort = 4224,
        InvalidTimerPeriod = 4225,
        InvalidTimerDutyCycle = 4226,
        InvalidTimerDelay = 4227,
        InvalidCalValue = 4228,
        InvalidAdAvgValue = 4229,
        InvalidAdSettlingTime = 4230,
        InvalidBufferOverwrite = 4231,
        InvalidAiTriggerLevel = 4232,
        InvalidQueueElement = 4233,
        InvalidAiTriggerChannel = 4234,
        InvalidAiTriggerSource = 4235,
        InvalidDataRate = 4236,
        InvalidSampleCountForBurstIo = 4237,
        DuplicateChannelsNotSupportedInQueue = 4238,
        NoncontiguousChannelsSpecified = 4239,
        InvalidDateTime = 4240,
        InvalidValueResolver = 4241,
        NonsequentialChannelsNotSupportedInQueue = 4242,
        InvalidInputBlockSize = 4243,
        NonAdjacentNonAscendingChannelsNotSupportedInQueue = 4244,
        NonAscendingChannelsNotSupportedInQueue = 4245,

        // unsupported functions errors
        BitConfigurationNotSupported = 4300,
        PortRequiresNoConfiguration = 4301,
        PortIsInputOnly = 4302,
        PortIsOutputOnly = 4303,

        // Input scan errors (4500)
        InputScanStartedWithZeroSamples = 4500,
        SingleChannelReadInvalidForMultiChannelScan = 4501,
        InputScanRateCannotBeZero = 4502,
        LowChannelIsGreaterThanHighChannel = 4503,
        RequiredLengthIsZero = 4504,
        TooManySamplesRequested = 4505,
        NoMoreInputSamplesAvailable = 4506,
        InputSamplesGreaterThanBufferSize = 4507,
        EndOfReadBufferReached = 4508,
        RequestedReadSamplesGreaterThanBufferSize = 4509,
        InputScanAlreadyInProgress = 4510,
        InputScanTimeOut = 4511,
        InternalReadBufferError = 4512,
        InputQueueIsEmpty = 4513,
        InputBufferCannotBeSet = 4514,
        InputBufferOverrun = 4515,
        ErrorWritingDataToExternalInputBuffer = 4516,
        InvalidInputTransferTime = 4517,
        InputBufferAllocationError = 4518,

        // Output scan errors (4600)
        OutputScanAlreadyInProgress = 4600,
        NumberOfSamplesPerChannelGreaterThanUserBufferSize = 4601,
        CopyingDataToInternalWriteBufferFailed = 4602,
        OutputBufferNullOrEmtpy = 4603,
        OutputScanTimeout = 4604,
        BulkOutTransferError = 4605,
        InvalidOutputBufferSize = 4606,
        NumberOfSamplesGreaterThanHalfBuffer = 4607,
        TotalNumberOfSamplesGreaterThanOutputBufferSize = 4608,
        OutputBufferCannotBeSet = 4609,
        OutputBufferTooSmallForContinuousScan = 4610,
        
        // USB errors (4800)
        BulkInputTransfersNotSupported = 4800,
        BulkOutputTransfersNotSupported = 4801,
        UsbIOError = 4802,
        UsbPipeError = 4803,
        UsbTimeoutError = 4804,
        UsbInsufficientPermissions = 4805,
        SetupDiEnumDeviceInterfacesFailed = 4806,
        LibUsbGetDeviceDescriptorFailed = 4807,
        DetailBufferIsNull = 4808,
        LibusbCouldNotBeLoaded = 4809,
        LibusbCouldNotBeInitialized = 4810,
        LibusbBulkTransferInterrupted = 4811,
        UsbBulkReadError = 4812,
        UsbBulkWriteError = 4813,
        
        // internal errors
        MethodRequiresImplementation = 5000,
        ActiveChannelsNotSet = 50001,
    }

    public enum ErrorLevel
    {
        Warning,
        Error,
    }

    public enum ErrorHandling
    {
    }
}
