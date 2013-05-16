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
    class Usb7204Ao : AoComponent
    {
        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        public Usb7204Ao(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 2)
        {
            if (m_daqDevice.FirmwareVersion < 2.0)
                m_aoScanSupported = false;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.UNI4PT096V), new Range(4.096, 0.0));

            m_daqDevice.CriticalParams.CalibrateAoData = false;

            for (int i = 0; i < m_channelCount; i++)
                m_ranges[i] = String.Format("{0}{1}:{2}={3}", DaqComponents.AO, MessageTranslator.GetChannelSpecs(i), DaqProperties.RANGE, MessageTranslator.ConvertToCurrentCulture(PropertyValues.UNI4PT096V));
        }

        //========================================================================================
        /// <summary>
        /// Overriden to read set default coefficients of slope = 1.0 and offset = 0.0
        /// </summary>
        //========================================================================================
        protected override void GetCalCoefficients()
        {
            m_calCoeffs.Clear();

            foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
            {
                for (int i = 0; i < m_channelCount; i++)
                {
                    m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(1.0, 0.0));
                }
            }
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the Ao message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessAoScanMessage(ref string message)
        {
            if (m_aoScanSupported)
                return base.PreprocessAoScanMessage(ref message);

            return ErrorCodes.InvalidMessage;
        }
    }
}
