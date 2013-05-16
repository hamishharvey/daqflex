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
        protected const int IN_DIR = 0;
        protected const int OUT_DIR = 1;

        protected int[] m_portWidths;
        protected int[] m_configMasks;
        protected int[] m_outputValues;
        protected int m_bitCount;
        protected List<int> m_validPorts = new List<int>();
        protected string m_supportedConfigurations = String.Empty;

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
            // dimension arrays here initialize them in Initialize()
            m_portWidths = new int[m_maxChannels];
            m_configMasks = new int[m_maxChannels];
            m_outputValues = new int[m_maxChannels];
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to calculate the total bit count
        /// </summary>
        //=================================================================================================================
        internal override void Initialize()
        {
            int bitCount;
            int portWidth;

            m_bitCount = 0;

            for (int i = 0; i < m_maxChannels; i++)
            {
                portWidth = (int)m_daqDevice.GetDevCapsValue(String.Format("DIO{0}:MAXCOUNT", MessageTranslator.GetChannelSpecs(i)));

                // keep track of the total bit count
                bitCount = GetResolution((ulong)portWidth);
                m_bitCount += bitCount;

                // store the port width
                m_portWidths[i] = portWidth;

                // save all ports on the device as input
                m_configMasks[i] = 0;

                // set all output values to 0
                m_outputValues[i] = 0;
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

            int lbIndex = message.IndexOf(CurlyBraces.LEFT);
            int rbIndex = message.IndexOf(CurlyBraces.RIGHT);
            int colonIndex = message.IndexOf(Constants.PROPERTY_SEPARATOR);

            if (lbIndex < rbIndex && rbIndex < colonIndex)
                errorCode = ValidateChannel(ref message);

            if (errorCode == ErrorCodes.NoErrors && message.Contains(DaqProperties.DIR + Constants.EQUAL_SIGN))
                errorCode = ProcessDirectionMessage(ref message);

            if (errorCode == ErrorCodes.NoErrors && message.Contains(DaqProperties.VALUE + Constants.EQUAL_SIGN))
                errorCode = ProcessValueSetMessage(ref message);

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
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // get the port number embedded in the message
            int port = MessageTranslator.GetPort(message);
            double bit = MessageTranslator.GetBit(message);

            // get number of supported ports
            int numberOfPorts = (int)m_daqDevice.GetDevCapsValue("DIO:CHANNELS");

            // return if port is invalid
            if (port < 0 || port >= numberOfPorts)
            {
                errorCode = ErrorCodes.InvalidDioPortSpecified;
            }

            // if the channel specs has a value resolver ('/') then test the bit
            if (errorCode == ErrorCodes.NoErrors && message.Contains(Constants.VALUE_RESOLVER.ToString()))
            {
                // get the width of the port in bits
                int maxBitNum = (int)GetResolution((ulong)m_portWidths[port]) - 1;

                // return if bit is invalid
                if (bit < 0 || bit > maxBitNum)
                {
                    errorCode = ErrorCodes.InvalidDioBitSpecified;
                }
            }

            if (errorCode != ErrorCodes.NoErrors)
                m_daqDevice.SendMessageToDevice = false;

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a direction message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessDirectionMessage(ref string message)
        {
            int port = MessageTranslator.GetPort(message);
            double bit = MessageTranslator.GetBit(message);
            string dir = MessageTranslator.GetPropertyValue(message);

            // get the supported configurations
            string supportedConfigs = m_daqDevice.GetDevCapsString(String.Format("DIO{0}:CONFIG", MessageTranslator.GetChannelSpecs(port)), false);

            // check for a valid direction value
            if (dir != PropertyValues.IN && dir != PropertyValues.OUT)
            {
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.InvalidPortConfig;
            }

            // if device is not programmable but supports the specified direction, 
            // indicate that it doesn't require configuration
            if (supportedConfigs.Contains(DevCapImplementations.FIXED) &&
                supportedConfigs.Contains(dir))
            {
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.PortRequiresNoConfiguration;
            }

            // check if the direction is supported
            if (dir == PropertyValues.IN &&
                !supportedConfigs.Contains(DevCapValues.PORTIN) && !supportedConfigs.Contains(DevCapValues.BITIN))
            {
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.PortIsOutputOnly;
            }

            // check if the direction is supported
            if (dir == PropertyValues.OUT &&
                !supportedConfigs.Contains(DevCapValues.PORTOUT) && !supportedConfigs.Contains(DevCapValues.BITOUT))
            {
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.PortIsInputOnly;
            }

            if (port >= 0 && bit == -1)
            {
                // port only specified
                if (dir == PropertyValues.IN)
                    m_configMasks[port] = 0;
                else
                    m_configMasks[port] = m_portWidths[port];
            }
            else if (bit >= 0 && port >= 0)
            {
                // port and bit specified
                if (dir == PropertyValues.IN)
                    m_configMasks[port] &= ~(int)Math.Pow(2, bit);
                else
                    m_configMasks[port] |= (int)Math.Pow(2, bit);
            }

            return ErrorCodes.NoErrors;
        }
         
        //====================================================================================
        /// <summary>
        /// Virtual method for processing a value message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessValueSetMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int port = MessageTranslator.GetPort(message);
            double bit = MessageTranslator.GetBit(message);
            string dir = MessageTranslator.GetPropertyValue(message);
            int value;

            string supportedConfigs = m_daqDevice.GetDevCapsString(String.Format("DIO{0}:CONFIG", MessageTranslator.GetChannelSpecs(port)), false);

            if (supportedConfigs.Contains(DevCapImplementations.FIXED) &&
               (!supportedConfigs.Contains(DevCapValues.PORTOUT) || !supportedConfigs.Contains(DevCapValues.BITOUT)))
            {
                // digital out not supported
                errorCode = ErrorCodes.CantWriteDioPort;
            }

            if (errorCode == ErrorCodes.NoErrors)
            {
                if (errorCode == ErrorCodes.NoErrors && PlatformParser.TryParse(dir, out value))
                {
                    int maxValue = (int)m_daqDevice.GetDevCapsValue(String.Format("DIO{0}:MAXCOUNT", MessageTranslator.GetChannelSpecs(port)));

                    if (bit == -1)
                    {
                        // port only specified
                        if (value < 0 || value > maxValue)
                            errorCode = ErrorCodes.InvalidDioPortValue;
                    }
                    else 
                    {
                        if (value < 0 || value > 1)
                            errorCode = ErrorCodes.InvalidDioBitValue;
                    }

                    // store the value in case it needs to be restored if the device is lost
                    if (errorCode == ErrorCodes.NoErrors && bit == -1)
                    {
                        m_outputValues[port] = value;
                    }
                    else
                    {
                        if (value == 0)
                            m_outputValues[port] &= ~(int)Math.Pow(2, bit);
                        else
                            m_outputValues[port] |= (int)Math.Pow(2, bit);
                    }
                }
                else
                {
                    if (bit == -1)
                        errorCode = ErrorCodes.InvalidDioPortValue;
                    else
                        errorCode = ErrorCodes.InvalidDioBitValue;
                }
            }

            if (errorCode != ErrorCodes.NoErrors)
                m_daqDevice.SendMessageToDevice = false;

            return errorCode;
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

            string config = m_daqDevice.GetDevCapsString("DIO{0}:CONFIG", false);

            if (config.Contains(DevCapImplementations.PROG))
                messages.Add("DIO{*}:DIR=*");

            messages.Add("?DIO{*}:DIR");


            if (config.Contains(DevCapValues.BITIN) || config.Contains(DevCapValues.BITOUT))
            {
                if (config.Contains(DevCapImplementations.PROG))
                    messages.Add("DIO{*/*}:DIR=*");

                messages.Add("?DIO{*/*}:DIR");
            }

            if (config.Contains(DevCapValues.PORTOUT) || config.Contains(DevCapValues.BITOUT))
            {
                messages.Add("DIO{*}:VALUE=*");
                messages.Add("DIO{*/*}:VALUE=*");
            }

            string latch = m_daqDevice.GetDevCapsString("DIO{0}:LATCH", false);

            if (latch != PropertyValues.NOT_SUPPORTED)
            {
                if (latch.Contains(DevCapValues.WRITE))
                {
                    messages.Add("DIO{*}:LATCH=*");
                    messages.Add("DIO{*/*}:LATCH=*");
                }

                messages.Add("?DIO{*}:LATCH");
                messages.Add("?DIO{*/*}:LATCH");
            }

            messages.Add("?DIO");
            messages.Add("?DIO{*}");
            messages.Add("?DIO{*}:VALUE");
            messages.Add("?DIO{*/*}:VALUE");

            return messages;
        }
    }
}
