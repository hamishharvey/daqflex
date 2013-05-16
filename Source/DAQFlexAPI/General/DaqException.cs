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

        internal DaqException(string errorMessage, ErrorCodes errorCode)
            : base(errorMessage)
        {
            m_errorCode = errorCode;
            m_level = ErrorLevel.Error;
            m_lastResponse = null;
        }

        internal DaqException(string errorMessage, ErrorCodes errorCode, ErrorLevel level)
            : base(errorMessage)
        {
            m_errorCode = errorCode;
            m_level = level;
            m_lastResponse = null;
        }

        internal DaqException(string errorMessage, ErrorCodes errorCode, ErrorLevel level, DaqResponse lastResponse)
            : base(errorMessage)
        {
            m_errorCode = errorCode;
            m_level = level;
            m_lastResponse = lastResponse;
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
    }
}
