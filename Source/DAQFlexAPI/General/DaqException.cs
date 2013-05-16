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
    public class DaqException : Exception
    {
        private ErrorCodes m_errorCode;
        private ErrorLevel m_level;
        private DaqResponse m_lastResponse;
        private string m_inputScanStatus = "IDLE";
        private ulong m_inputScanCount = 0;
        private long m_inputScanIndex = -1;
        private string m_outputScanStatus = "IDLE";
        private ulong m_outputScanCount = 0;
        private long m_outputScanIndex = -1;

        internal DaqException(string errorMessage, ErrorCodes errorCode)
            : base(errorMessage)
        {
            m_errorCode = errorCode;
            m_level = ErrorLevel.Error;
            m_lastResponse = null;
        }

        internal DaqException(DaqDevice device, string errorMessage, ErrorCodes errorCode, ErrorLevel level)
            : base(errorMessage)
        {
            m_errorCode = errorCode;
            m_level = level;
            m_lastResponse = null;
            m_inputScanStatus = device.DriverInterface.InputScanStatus.ToString();
            m_inputScanCount = device.DriverInterface.InputScanCount;
            m_inputScanIndex = device.DriverInterface.InputScanIndex;
            m_outputScanStatus = device.DriverInterface.OutputScanState.ToString();
            m_outputScanCount = device.DriverInterface.OutputScanCount;
            m_outputScanIndex = device.DriverInterface.OutputScanIndex;
        }

        internal DaqException(DaqDevice device, string errorMessage, ErrorCodes errorCode, ErrorLevel level, DaqResponse lastResponse)
            : base(errorMessage)
        {
            m_errorCode = errorCode;
            m_level = level;
            m_lastResponse = lastResponse;
            m_inputScanStatus = device.DriverInterface.InputScanStatus.ToString();
            m_inputScanCount = device.DriverInterface.InputScanCount;
            m_inputScanIndex = device.DriverInterface.InputScanIndex;
            m_outputScanStatus = device.DriverInterface.OutputScanState.ToString();
            m_outputScanCount = device.DriverInterface.OutputScanCount;
            m_outputScanIndex = device.DriverInterface.OutputScanIndex;
        }

        public ErrorCodes ErrorCode
        {
            get { return m_errorCode; }
        }

        public ErrorLevel Level
        {
            get { return m_level; }
        }

        public DaqResponse LastResponse
        {
            get { return m_lastResponse; }
        }

        public string InputScanStatus
        {
            get { return m_inputScanStatus; }
        }

        public ulong InputScanCount
        {
            get { return m_inputScanCount; }
        }

        public long InputScanIndex
        {
            get { return m_inputScanIndex; }
        }

        public string OutputScanStatus
        {
            get { return m_outputScanStatus; }
        }

        public ulong OutputScanCount
        {
            get { return m_outputScanCount; }
        }

        public long OutputScanIndex
        {
            get { return m_outputScanIndex; }
        }
    }
}
