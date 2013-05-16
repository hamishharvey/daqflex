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
            m_dataWidth = 12;
            m_maxCount = (int)Math.Pow(2, m_dataWidth) - 1;
            m_maxThroughput = 10000;

            if (m_daqDevice.FirmwareVersion < 2.0)
                m_aoScanSupported = false;

            InitializeRanges();
            SetDefaultCriticalParams(deviceInfo);
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            m_supportedRanges.Add(PropertyValues.UNI4PT096V, new Range(4.096, 0.0));

            for (int i = 0; i < m_channelCount; i++)
                m_ranges[i] = String.Format("{0}{1}:{2}={3}", DaqComponents.AO, MessageTranslator.GetChannelSpecs(i), DaqProperties.RANGE, PropertyValues.UNI4PT096V);
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to set the default critical params
        /// </summary>
        //===========================================================================================
        internal override void SetDefaultCriticalParams(DeviceInfo deviceInfo)
        {
            m_daqDevice.DriverInterface.CriticalParams.AoDataWidth = m_dataWidth;
            m_daqDevice.DriverInterface.CriticalParams.OutputPacketSize = deviceInfo.MaxPacketSize;
            m_daqDevice.DriverInterface.CriticalParams.OutputFifoSize = 1024;

            m_daqDevice.DriverInterface.CriticalParams.LowAoChannel = 0;
            m_daqDevice.DriverInterface.CriticalParams.HighAoChannel = 0;
            m_daqDevice.DriverInterface.CriticalParams.OutputScanRate = 1000;
            m_daqDevice.DriverInterface.CriticalParams.OutputScanSamples = 1000;
        }

        internal override void Initialize()
        {
            base.Initialize();

            if (m_aoScanSupported)
            {
                m_daqDevice.SendMessage("AOSCAN:LOWCHAN=" + m_daqDevice.DriverInterface.CriticalParams.LowAoChannel.ToString());
                m_daqDevice.SendMessage("AOSCAN:HIGHCHAN=" + m_daqDevice.DriverInterface.CriticalParams.HighAoChannel.ToString());
                m_daqDevice.SendMessage("AOSCAN:RATE=" + m_daqDevice.DriverInterface.CriticalParams.OutputScanRate.ToString());
                m_daqDevice.SendMessage("AOSCAN:SAMPLES=" + m_daqDevice.DriverInterface.CriticalParams.OutputScanSamples.ToString());
            }
        }

        //===================================================================================================
        /// <summary>
        /// Overriden to get the supported messages specific to this Dio component
        /// </summary>
        /// <param name="daqComponent">The Daq Component name - not all implementations require this</param>
        /// <returns>A list of supported messages</returns>
        //===================================================================================================
        internal override List<string> GetMessages(string daqComponent)
        {
            List<string> messages = new List<string>();

            if (daqComponent == DaqComponents.AO)
            {
                messages.Add("AO:SCALE=*");
                messages.Add("AO{*}:VALUE=*");
                messages.Add("?AO");
                messages.Add("?AO:SCALE");
                messages.Add("?AO{*}:RANGE");
            }
            else if (daqComponent == DaqComponents.AOSCAN && m_aoScanSupported)
            {
                messages.Add("AOSCAN:LOWCHAN=*");
                messages.Add("AOSCAN:HIGHCHAN=*");
                messages.Add("AOSCAN:RATE=*");
                messages.Add("AOSCAN:SAMPLES=*");
                messages.Add("AOSCAN:SCALE=*");
                messages.Add("AOSCAN:BUFSIZE=*");
                messages.Add("AOSCAN:START");
                messages.Add("AOSCAN:STOP");

                messages.Add("?AOSCAN:LOWCHAN");
                messages.Add("?AOSCAN:HIGHCHAN");
                messages.Add("?AOSCAN:RATE");
                messages.Add("?AOSCAN:SAMPLES");
                messages.Add("?AOSCAN:SCALE");
                messages.Add("?AOSCAN:BUFSIZE");
                messages.Add("?AOSCAN:STATUS");
                messages.Add("?AOSCAN:COUNT");
                messages.Add("?AOSCAN:INDEX");
            }

            return messages;
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

        //===========================================================================================
        /// <summary>
        /// Validates the cal message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessCalMessage(ref string message)
        {
            return ErrorCodes.InvalidMessage;
        }

        internal override double GetMaxScanRate()
        {
            return m_maxThroughput / m_daqDevice.DriverInterface.CriticalParams.AoChannelCount;
        }
    }
}
