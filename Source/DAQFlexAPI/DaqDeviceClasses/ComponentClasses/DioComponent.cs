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
    internal partial class DioComponent : IoComponent
    {
        protected int[] m_dataWidths;
        protected int[] m_inMask;
        protected int[] m_outMask;
        protected int[] m_portValues;
        protected int m_portToReadBack;
        protected ulong m_bitCount;
        protected List<int> m_validPorts = new List<int>();
        protected Dictionary<int, string> m_portConfigs = new Dictionary<int, string>();
        protected string m_supportedConfigurations = String.Empty;

        // this is for translating UL port types
        protected Dictionary<int, int> m_portNumbers = new Dictionary<int, int>();

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        public DioComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
            m_dataWidths = new int[maxChannels];
            m_inMask = new int[maxChannels];
            m_outMask = new int[maxChannels];
            m_portValues = new int[maxChannels];
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to calculate the total bit count
        /// </summary>
        //=================================================================================================================
        internal override void Initialize()
        {
            ulong bitCount;

            m_bitCount = 0;

            int index = 0;

            foreach (int dataWidth in m_dataWidths)
            {
                bitCount = GetResolution((ulong)dataWidth);
                m_bitCount += bitCount;
                m_inMask[index] = dataWidth;
                m_outMask[index] = 0;

                // default to input
                m_portConfigs[index] = PropertyValues.IN;

                index++;
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
        internal override ErrorCodes PreprocessMessage(ref string message, string messageType)
        {
            ErrorCodes errorCode = base.PreprocessMessage(ref message, messageType);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            if (messageType == DaqComponents.DIO)
            {
                return PreprocessDioMessage(ref message);
            }

            System.Diagnostics.Debug.Assert(false, "Invalid component for DIO");

            return ErrorCodes.InvalidMessage;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The component this message pertains to</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessDioMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                errorCode = ValidateChannel(ref message);
            }

            if (errorCode == ErrorCodes.NoErrors && message.Contains(DaqProperties.DIR))
            {
                errorCode = ValidateConfig(ref message);
            }

            if (errorCode == ErrorCodes.NoErrors && message.Contains(DaqProperties.VALUE))
            {
                errorCode = ValidateOperation(ref message);
            }

            return errorCode;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the port number and bit number (if included)
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ValidateChannel(ref string message)
        {
            int port = MessageTranslator.GetPort(message);

            if (port < 0 || port > (m_portNumbers.Count - 1))
                return ErrorCodes.InvalidDioPortSpecified;

            int bit;

            if (message.Contains(Constants.VALUE_RESOLVER.ToString()))
            {
                bit = MessageTranslator.GetBit(message);

                if (bit < 0 || ((port + 1) * bit) > ((int)m_bitCount - 1))
                    return ErrorCodes.InvalidDioBitSpecified;

                int portWidth = (int)GetResolution((ulong)m_dataWidths[port]);

                if ((bit + 1) > portWidth)
                {
                    try
                    {
                        int colonIndex = message.IndexOf(Constants.PROPERTY_SEPARATOR);

                        if (colonIndex > 0)
                        {
                            string property = message.Substring(colonIndex);

                            int adjustedPort = (int)Math.Ceiling((double)portWidth / (double)bit);
                            int adjustedBit = bit - portWidth;

                            string adjustedMessage = String.Empty;

                            if (message.Contains(Constants.QUERY.ToString()))
                                adjustedMessage = Constants.QUERY.ToString();

                            adjustedMessage += DaqComponents.DIO +
                                        CurlyBraces.LEFT +
                                            adjustedPort.ToString() +
                                                Constants.VALUE_RESOLVER +
                                                    adjustedBit.ToString() +
                                                        CurlyBraces.RIGHT;

                            adjustedMessage += property;

                            message = adjustedMessage;
                        }
                    }
                    catch (Exception)
                    {
                        return ErrorCodes.InvalidMessage;
                    }
                }
            }

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the port direction
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ValidateConfig(ref string message)
        {
            if (message.Contains(Constants.QUERY.ToString()))
                return ErrorCodes.NoErrors;

            int port = MessageTranslator.GetPort(message);
            int bit = -1;

            // if the message contains "DIO{p/b}, get the bit number
            if (message.Contains(Constants.VALUE_RESOLVER.ToString()))
                bit = MessageTranslator.GetBit(message);

            // check support for bit configuration
            if (m_supportedConfigurations == String.Empty)
                m_supportedConfigurations = m_daqDevice.GetDevCapsValue("DIO{" + port + "}:CONFIG", false);

            if (bit >= 0 && !m_supportedConfigurations.Contains(DevCapValues.BITIN) && !m_supportedConfigurations.Contains(DevCapValues.BITOUT))
                return ErrorCodes.BitConfigurationNotSupported;

            // get the dio direction value
            string value = MessageTranslator.GetPropertyValue(message);

            if (value == PropertyValues.IN || value == PropertyValues.OUT)
            {
                if (!m_portConfigs.ContainsKey(port))
                    m_portConfigs.Add(port, value);

                // store the configuration
                if (bit >= 0)
                {
                    if (value == PropertyValues.IN)
                    {
                        m_inMask[port] |= (int)Math.Pow(2, bit);
                        m_outMask[port] &= ~((int)Math.Pow(2, bit));
                    }
                    else
                    {
                        m_outMask[port] |= (int)Math.Pow(2, bit);
                        m_inMask[port] &= ~((int)Math.Pow(2, bit));
                    }
                }
                else
                {
                    if (value == PropertyValues.IN)
                    {
                        m_inMask[port] = 255;
                        m_outMask[port] = 0;
                    }
                    else
                    {
                        m_outMask[port] = 255;
                        m_inMask[port] = 0;
                    }
                }

                return ErrorCodes.NoErrors;
            }

            return ErrorCodes.InvalidPortConfig;
        }

        //===========================================================================================
        /// <summary>
        /// Checks the configuration against the requested operation
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ValidateOperation(ref string message)
        {
            int port = MessageTranslator.GetPort(message);
            int bit = MessageTranslator.GetBit(message);

            m_daqDevice.SendMessageToDevice = true;

            if (m_portConfigs.ContainsKey(port))
            {
                // DIn/DBitIn
                if (message.Contains("?") && message.Contains(DaqProperties.VALUE))
                {
                    m_portToReadBack = -1;

                    if (bit >= 0)
                    {
                        // if the bit is configured for output read back the last value set
                        if ((m_outMask[port] & (int)Math.Pow(2, bit)) != 0)
                        {
                            ushort bitValue = (ushort)(m_portValues[port] & (int)Math.Pow(2, bit));
                            bitValue = (ushort)(bitValue >> bit);
                            string value = message.Substring(message.IndexOf(Constants.QUERY.ToString()) + 1);
                            value += (Constants.EQUAL_SIGN + bitValue.ToString());
                            m_daqDevice.ApiResponse = new DaqResponse(value, bitValue);
                            m_daqDevice.SendMessageToDevice = false;
                        }
                    }
                    else
                    {
                        // handle the port readback in PostProcessData
                        if (m_portConfigs[port] == PropertyValues.OUT)
                            m_portToReadBack = port;
                    }
                }
                else if (message.Contains(DaqProperties.VALUE))

                {
                    // DOut/DBitOut
                    try
                    {
                        int value = Int32.Parse(MessageTranslator.GetPropertyValue(message).ToString());

                        if (bit >= 0)
                        {
                            // the message contains a port and bit number
                            if ((m_outMask[port] & (int)Math.Pow(2, bit)) > 0)
                            {
                                if (value == 1)
                                    m_portValues[port] |= (int)Math.Pow(2, bit);
                                else
                                    m_portValues[port] &= ~(int)Math.Pow(2, bit);
                            }
                            else
                            {
                                return ErrorCodes.IncorrectPortConfig;
                            }
                        }
                        else
                        {
                            // the message just contains a port number
                            if (m_outMask[port] == m_dataWidths[port])
                                m_portValues[port] = value;
                            else
                                return ErrorCodes.IncorrectPortConfig;
                        }
                    }
                    catch (Exception)
                    {
                        return ErrorCodes.InvalidMessage;
                    }
                }
            }
            else
            {
                // no configuration has been set yet. Default is DIR=IN
                if (!message.Contains("?") && message.Contains(DaqProperties.VALUE))
                {
                    return ErrorCodes.IncorrectPortConfig;
                }
            }

            return ErrorCodes.NoErrors;
        }

        //=========================================================================================================
        /// <summary>
        /// Overriden to handle readback of a port
        /// </summary>
        /// <param name="componentType"></param>
        /// <param name="response"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        //=========================================================================================================
        internal override ErrorCodes PostProcessData(string componentType, ref string response, ref double value)
        {
            if (m_portToReadBack >= 0)
            {
                int bits = (int)GetResolution((ulong)m_dataWidths[m_portToReadBack]);
                int bitValue;
                int portValue = (int)value;

                for (int i = 0; i < bits; i++)
                {
                    if ((m_outMask[m_portToReadBack] & (int)Math.Pow(2, i)) > 0)
                    {
                        bitValue = m_portValues[m_portToReadBack] & (int)Math.Pow(2, i);
                        portValue |= bitValue;
                    }
                }

                value = portValue;

                int indexOfEqual = response.IndexOf(Constants.EQUAL_SIGN);

                if (indexOfEqual >= 0 && indexOfEqual < (response.Length - 1))
                    response = response.Remove(indexOfEqual + 1, response.Length - indexOfEqual - 1);

                response += portValue.ToString();
            }

            return ErrorCodes.NoErrors;
        }

        //==============================================================================
        /// <summary>
        /// normalizes the port and bit number - used by UL interface
        /// </summary>
        /// <param name="port">The port number</param>
        /// <param name="bit">The bit number</param>
        //==============================================================================
        internal virtual void ResolvePortBit(ref int port, ref int bit)
        {
            if (bit >= (int)m_bitCount)
                return;

            int index = 0;
            int width = m_dataWidths[index];
            int res;

            while ((Math.Pow(2, bit) - 1) >= width)
            {
                index++;
                res = (int)GetResolution((ulong)m_dataWidths[index]);
                width += (m_dataWidths[index] << res);
                port = index;
                bit -= res;
            }
        }
    }
}
