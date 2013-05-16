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
    class Usb1608FSPlusAi : Usb7202Ai
    {
        protected bool m_burstIO;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal Usb1608FSPlusAi(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo)
        {
        }

        //=========================================================================================
        /// <summary>
        /// Let the JIT compiler compile critical methods
        /// </summary>
        //=========================================================================================
        internal override void ConfigureScan()
        {
            base.ConfigureScan();

            // disable and clear the queue
            m_daqDevice.SendMessage(Messages.AISCAN_QUEUE_DISABLE);
            m_daqDevice.SendMessage(Messages.AIQUEUE_CLEAR);
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to get the supported messages specific to this Ai component
        /// </summary>
        /// <returns>A list of supported messages</returns>
        //===========================================================================================
        internal override List<string> GetMessages(string daqComponent)
        {
            List<string> messages = new List<string>();

            if (daqComponent == DaqComponents.AI)
            {
                messages.Add("AI{*}:RANGE=*");
                messages.Add("AI:RANGE=*");
                messages.Add("AI:SCALE=*");
                messages.Add("AI:CAL=*");
                messages.Add("?AI");
                messages.Add("?AI:RES");
                messages.Add("?AI{*}:VALUE");
                messages.Add("?AI{*}:VALUE/*");
                messages.Add("?AI{*}:RANGE");
                messages.Add("?AI:RANGE");
                messages.Add("?AI{*}:SLOPE");
                messages.Add("?AI{*}:OFFSET");
                messages.Add("?AI:CAL");
                messages.Add("?AI:SCALE");
            }
            else if (daqComponent == DaqComponents.AISCAN)
            {
                messages.Add("AISCAN:XFRMODE=*");
                messages.Add("AISCAN:RANGE{*}=*");
                messages.Add("AISCAN:RANGE=*");
                messages.Add("AISCAN:HIGHCHAN=*");
                messages.Add("AISCAN:LOWCHAN=*");
                messages.Add("AISCAN:DEBUG=*");
                messages.Add("AISCAN:RATE=*");
                messages.Add("AISCAN:SAMPLES=*");
                messages.Add("AISCAN:TRIG=*");
                messages.Add("AISCAN:SCALE=*");
                messages.Add("AISCAN:CAL=*");
                messages.Add("AISCAN:EXTPACER=*");
                messages.Add("AISCAN:BUFSIZE=*");
                messages.Add("AISCAN:BUFOVERWRITE=*");
                messages.Add("AISCAN:START");
                messages.Add("AISCAN:STOP");
                messages.Add("AISCAN:QUEUE=*");

                messages.Add("?AISCAN:XFRMODE");
                messages.Add("?AISCAN:RANGE{*}");
                messages.Add("?AISCAN:RANGE");
                messages.Add("?AISCAN:HIGHCHAN");
                messages.Add("?AISCAN:LOWCHAN");
                messages.Add("?AISCAN:DEBUG");
                messages.Add("?AISCAN:RATE");
                messages.Add("?AISCAN:SAMPLES");
                messages.Add("?AISCAN:TRIG");
                messages.Add("?AISCAN:SCALE");
                messages.Add("?AISCAN:CAL");
                messages.Add("?AISCAN:EXTPACER");
                messages.Add("?AISCAN:STATUS");
                messages.Add("?AISCAN:BUFSIZE");
                messages.Add("?AISCAN:BUFOVERWRITE");
                messages.Add("?AISCAN:COUNT");
                messages.Add("?AISCAN:INDEX");
                messages.Add("?AISCAN:QUEUE");
            }
            else if (daqComponent == DaqComponents.AITRIG)
            {
                messages.Add("AITRIG:TYPE=*");
                messages.Add("?AITRIG:TYPE");
            }
            else if (daqComponent == DaqComponents.AIQUEUE)
            {
                messages.Add("AIQUEUE:CLEAR");
                messages.Add("AIQUEUE{*}:CHAN=*");
                messages.Add("AIQUEUE{*}:RANGE=*");
                messages.Add("?AIQUEUE:COUNT");
                messages.Add("?AIQUEUE{*}:CHAN");
                messages.Add("?AIQUEUE{*}:RANGE");
            }

            return messages;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing the xfer mode message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes PreProcessXferModeMessage(ref string message)
        {
            string value;
            string msg;
            string response;
            
            value = MessageTranslator.GetPropertyValue(message);

            if (value == PropertyValues.BURSTIO)
            {
                m_daqDevice.SendMessageToDevice = false;
                m_burstIO = true;

                // use block IO on the device
                msg = Messages.AISCAN_XFRMODE;
                msg = msg.Replace("#", PropertyValues.BLOCKIO);
                m_daqDevice.SendMessageDirect(msg);

                // get the device response
                response = m_daqDevice.DriverInterface.ReadStringDirect();

                // generate a response
                m_daqDevice.ApiResponse = new DaqResponse(response, Double.NaN);

                return ErrorCodes.NoErrors;
            }
            else
            {
                m_burstIO = false;
                return base.PreProcessXferModeMessage(ref message);
            }
        }

        //===============================================================================================
        /// <summary>
        /// Overriden to validate the per channel rate just before AISCAN:START is sent to the device
        /// </summary>
        /// <param name="message">The device message</param>
        //===============================================================================================
        internal override ErrorCodes ValidateScanRate()
        {
            double maxRate = double.MaxValue;
            int channelCount = m_daqDevice.CriticalParams.AiChannelCount;

            try
            {
                double rate = m_daqDevice.CriticalParams.InputScanRate;

                if (m_burstIO)
                {
                    maxRate = m_maxBurstRate;

                    if (rate < m_minBurstRate || rate > maxRate)
                        return ErrorCodes.InvalidScanRateSpecified;
                }
                else
                {
                    maxRate = m_maxScanRate;

                    if (rate < m_minScanRate || rate > maxRate)
                        return ErrorCodes.InvalidScanRateSpecified;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ValidateSampleCount()
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (m_burstIO)
            {
                int fifoSize = (int)m_daqDevice.GetDevCapsValue("AISCAN:FIFOSIZE");

                if (m_daqDevice.CriticalParams.InputScanSamples == 0 || m_daqDevice.CriticalParams.AiChannelCount * m_daqDevice.CriticalParams.InputScanSamples > fifoSize)
                    errorCode = ErrorCodes.InvalidSampleCountForBurstIo;
            }

            return errorCode;
        }
    }
}
