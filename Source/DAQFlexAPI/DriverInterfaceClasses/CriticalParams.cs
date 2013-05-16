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
    //============================================================================
    /// <summary>
    /// This is a light-weight class containing parameters that are critical
    /// for setting up input and output scans
    /// </summary>
    //============================================================================
    internal class CriticalParams
    {
        internal int AiDataWidth { get; set; }
        internal double[] AiSlopes { get; set; }
        internal double[] AiOffsets { get; set; }
        internal bool AiQueueEnabled { get; set; }
        internal bool CalibrateAiData { get; set; }
        internal bool CalibrateAoData { get; set; }
        internal bool ScaleAiData { get; set; }
        internal bool ScaleAoData { get; set; }
        internal double InputScanRate { get; set; }
        internal int InputScanSamples { get; set; }
        internal bool InputScanOverwrite { get; set; }
        internal int InputPacketSize { get; set; }
        internal int BulkInXferSize { get; set; }
        internal int DataInXferSize { get; set; }
        internal InputConversionMode InputConversionMode { get; set; }
        internal bool InputTriggerEnabled { get; set; }
        internal int AiChannel { get; set; }
        internal int LowAiChannel { get; set; }
        internal int HighAiChannel { get; set; }
        internal int AiChannelCount { get; set; }
        internal int AoDataWidth { get; set; }
        internal int LowAoChannel { get; set; }
        internal int HighAoChannel { get; set; }
        internal int AoChannelCount { get; set; }
        internal double OutputScanRate { get; set; }
        internal int OutputDataWidth { get; set; }
        internal int OutputPacketSize { get; set; }
        internal int BulkOutXferSize { get; set; }
        internal int DataOutXferSize { get; set; }
        internal int OutputScanSamples { get; set; }
        internal bool OutputTriggerEnabled { get; set; }
        internal int DiDataWidth { get; set; }
        internal int DoDataWidth { get; set; }
        internal int CtrDataWidth { get; set; }
        internal int TmrDataWidth { get; set; }
        internal SampleMode InputSampleMode { get; set; }
        internal SampleMode OutputSampleMode { get; set; }
        internal TransferMode InputTransferMode { get; set; }
        internal int DeltaRearmInputSamples { get; set; }
        internal bool TriggerRearmEnabled { get; set; }
        internal ScanType ScanType { get; set; }
        internal int OutputFifoSize { get; set; }
        internal string InputTriggerSource { get; set; }
        internal string InputTriggerType { get; set; }
        internal double InputTriggerLevel { get; set; }
        internal bool AiExtPacer { get; set; }
        internal bool AoExtPacer { get; set; }
        internal double RequestedInputTriggerLevel { get; set; }
        internal bool ResendInputTriggerLevelMessage { get; set; }
        internal double[] DataRates { get; set; }
        internal string[] TempUnits { get; set; }
        internal bool AiDataIsSigned { get; set; }
        internal bool Requires0LengthPacketForSingleIO { get; set; }
        internal int AdjustedRearmSamplesPerTrigger { get; set; }
        internal int NumberOfSamplesForSingleIO { get; set; }
    }
}
