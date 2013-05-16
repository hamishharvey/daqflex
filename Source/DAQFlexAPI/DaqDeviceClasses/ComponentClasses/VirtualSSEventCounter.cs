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
    internal class VirtualSSEventCounter : EventCounter
    {
        private uint[] m_lastCount;
        private bool[] m_isCounting;

        //===================================================================================================
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="daqDevice">A reference to the DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">A referemce tp the DeviceInfo object that the Driver Interace uses</param>
        //===================================================================================================
        public VirtualSSEventCounter(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
            m_lastCount = new uint[maxChannels];
            m_isCounting = new bool[maxChannels];

            for (int i = 0; i < maxChannels; i++)
            {
                m_lastCount[i] = 0;
                m_isCounting[i] = false;
            }
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The component this message pertains to</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessCtrMessage(ref string message)
        {
            ErrorCodes errorCode = base.PreprocessCtrMessage(ref message);

            if (errorCode == ErrorCodes.NoErrors)
            {
                if (message.Contains(DaqCommands.START))
                {
                    errorCode = ProcessStartMessage(ref message);
                }
                else if (message.Contains(DaqCommands.STOP))
                {
                    errorCode = ProcessStopMessage(ref message);
                }
                else if (message.Contains(DaqProperties.VALUE) && message.Contains(Constants.EQUAL_SIGN))
                {
                    errorCode = ProcessValueSetMessage(ref message);
                }
            }

            return errorCode;
        }

        //=================================================================================================================================
        /// <summary>
        /// Overridden to process the value query message
        /// </summary>
        /// <param name="componentType">The component type</param>
        /// <param name="response">The device's response</param>
        /// <param name="value">The value in the response</param>
        /// <returns>The error code</returns>
        //=================================================================================================================================
        internal override ErrorCodes PostProcessData(string componentType, ref string response, ref double value)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (componentType == DaqComponents.CTR && 
                    response.Contains(DaqProperties.VALUE) && 
                        response.Contains(Constants.EQUAL_SIGN))
                PostProcessValueQueryMessage(ref response, ref value);

            return errorCode;
        }

        //==============================================================================================
        /// <summary>
        /// Calculates a virtual count to implement Start/Stop behavior
        /// </summary>
        /// <param name="response">The response that contains the count value</param>
        /// <returns>The error code</returns>
        //==============================================================================================
        internal virtual ErrorCodes PostProcessValueQueryMessage(ref string response, ref double value)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            try
            {
                string virtualResponse;

                int channel = MessageTranslator.GetChannel(response);
                string responseValue = MessageTranslator.GetPropertyValue(response);

                uint newCount = 0;

                if (!m_isCounting[channel])
                {
                    newCount = m_lastCount[channel];
                }
                else
                {
                    uint actualCount;

                    if (PlatformParser.TryParse(responseValue, out actualCount))
                    {
                        newCount = actualCount + m_lastCount[channel];
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Invalid count value");
                    }
                }

                value = newCount;

                int removeIndex = response.IndexOf('=') + 1;
                virtualResponse = response.Remove(removeIndex, response.Length - removeIndex);
                virtualResponse += newCount.ToString();

                response = virtualResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }

            return errorCode;
        }

        //===========================================================================
        /// <summary>
        /// Implements virtual START behavior
        /// </summary>
        /// <param name="message">The START message</param>
        /// <returns>The error code</returns>
        //===========================================================================
        internal override ErrorCodes ProcessValueSetMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            try
            {
                int channel = MessageTranslator.GetChannel(message);

                if (channel >= 0)
                    m_lastCount[channel] = 0;
            }
            catch (Exception)
            {
            }

            m_daqDevice.ApiResponse = new DaqResponse(message, Double.NaN);

            return errorCode;
        }

        //===========================================================================
        /// <summary>
        /// Implements virtual START behavior
        /// </summary>
        /// <param name="message">The START message</param>
        /// <returns>The error code</returns>
        //===========================================================================
        internal override ErrorCodes ProcessStartMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            try
            {
                m_daqDevice.SendMessageToDevice = false;

                int channel = MessageTranslator.GetChannel(message);

                if (channel >= 0)
                {
                    string msg = String.Format("CTR{0}:VALUE=0", MessageTranslator.GetChannelSpecs(channel));
                    m_daqDevice.SendMessageDirect(msg);
                    m_isCounting[channel] = true;
                }
            }
            catch (Exception)
            {
            }

            m_daqDevice.ApiResponse = new DaqResponse(message, Double.NaN);

            return errorCode;
        }

        //===========================================================================
        /// <summary>
        /// Implements virtual STOP behavior
        /// </summary>
        /// <param name="message">The STOP message</param>
        /// <returns>The error code</returns>
        //===========================================================================
        internal virtual ErrorCodes ProcessStopMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            try
            {
                m_daqDevice.SendMessageToDevice = false;

                int channel = MessageTranslator.GetChannel(message);

                if (channel >= 0)
                {
                    string msg = String.Format("?CTR{0}:VALUE", MessageTranslator.GetChannelSpecs(channel));
                    m_daqDevice.SendMessageDirect(msg);
                    uint count = (uint)m_daqDevice.DriverInterface.ReadValueDirect();

                    if (m_isCounting[channel])
                        m_lastCount[channel] = count + m_lastCount[channel];

                    m_isCounting[channel] = false;
                }


            }
            catch (Exception)
            {
            }

            m_daqDevice.ApiResponse = new DaqResponse(message, Double.NaN);

            return errorCode;
        }
    }
}
