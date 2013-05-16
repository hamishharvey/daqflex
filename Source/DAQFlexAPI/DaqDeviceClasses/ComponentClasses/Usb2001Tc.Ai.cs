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
    class Usb2001TcAi : AiTempComponent
    {
        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        public Usb2001TcAi(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 1)
        {
            m_scaleData = true;

            m_aiChannelType[0] = AiChannelTypes.Temperature;
            m_tcType[0] = GetTcType(0);

            if (m_tcType[0] != ThermocoupleTypes.NotSet)
                m_thermocouple = Thermocouple.CreateThermocouple(m_tcType[0]);
        }

        internal override void Initialize()
        {
            InitializeChannelModes();

            base.Initialize();

            IntializeTCRanges();
            SetDefaultCriticalParams(m_deviceInfo);

            // reset defaults
            m_daqDevice.SendMessageDirect("DEV:RESET/DEFAULT");

            ((Usb2001Tc)m_daqDevice).WaitForReady();
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to return AiChannelType as Temperature
        /// </summary>
        //=================================================================================================================
        internal override AiChannelTypes GetChannelType(int channel)
        {
            return AiChannelTypes.Temperature;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize channel modes
        /// </summary>
        //=================================================================================================================
        internal override void InitializeChannelModes()
        {
            m_channelModes = new string[m_maxChannels];

            // this device is fixed at differntial
            for (int i = 0; i < m_channelModes.Length; i++)
                m_channelModes[i] = PropertyValues.DIFF;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT073125V) + ":DIFF", new Range(0.073125, -0.073125));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT14625V) + ":DIFF", new Range(0.14625, -0.14625));

            // store default range
            m_ranges[0] = MessageTranslator.ConvertToCurrentCulture("AI{0}:RANGE=BIP73.125E-3V");

            m_activeChannels = new ActiveChannels[1];
            m_activeChannels[0].ChannelNumber = 0;
            m_activeChannels[0].UpperLimit = m_supportedRanges[MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT073125V) + ":DIFF"].UpperLimit;
            m_activeChannels[0].LowerLimit = m_supportedRanges[MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT073125V) + ":DIFF"].LowerLimit;
        }

        //========================================================================================
        /// <summary>
        /// Overriden to read in the AI calibration coefficients
        /// </summary>
        //========================================================================================
        protected override void GetCalCoefficients()
        {
            // get slope/offset
            // read cal coefficients for each range - 1 ch, 2 ranges
            for (int i = 0; i < m_channelCount; i++)
            {
                foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
                {
                    // set the range
                    string range = kvp.Key.Substring(0, kvp.Key.IndexOf(":"));

                    m_daqDevice.SendMessageDirect(String.Format("AI{0}:RANGE={1}", MessageTranslator.GetChannelSpecs(i), range));

                    // get the slope and offset for the range
                    m_daqDevice.SendMessageDirect(String.Format("?AI{0}:SLOPE", MessageTranslator.GetChannelSpecs(i)));
                    double slope = m_daqDevice.DriverInterface.ReadValueDirect();
                    m_daqDevice.SendMessageDirect(String.Format("?AI{0}:OFFSET", MessageTranslator.GetChannelSpecs(i)));
                    double offset = m_daqDevice.DriverInterface.ReadValueDirect();
                    m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(slope, offset));
                }
            }
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to set the default critical params
        /// </summary>
        //===========================================================================================
        internal override void SetDefaultCriticalParams(DeviceInfo deviceInfo)
        {
            m_daqDevice.DriverInterface.CriticalParams.AiDataWidth = m_dataWidth;
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
                // Property Set Messages
                messages.Add("AI{0}:SENSOR=TC/*");
                messages.Add("AI{0}:RANGE=*");
                messages.Add("AI:SCALE=*");
                messages.Add("AI:CAL=*");

                // Property Get Messages
                messages.Add("?AI");
                messages.Add("?AI{0}:SENSOR");
                messages.Add("?AI{0}:CJC/*");
                messages.Add("?AI{0}:VALUE");
                messages.Add("?AI{0}:VALUE/*");
                messages.Add("?AI{0}:RANGE");
                messages.Add("?AI{0}:SLOPE");
                messages.Add("?AI{0}:OFFSET");
                messages.Add("?AI{0}:STATUS");
                messages.Add("?AI:CAL");
                messages.Add("?AI:SCALE");
            }

            return messages;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The component this message pertains to</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessAiMessage(ref string message)
        {
            ErrorCodes errorCode = base.PreprocessAiMessage(ref message);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            if (message.Contains("SENSOR=TC") && !message.Contains(Constants.QUERY.ToString()))
            {
                ProcessCJCMessage(ref message);
            }

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes the CJC message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessCJCMessage(ref string message)
        {
            string tcType = message.Substring(message.IndexOf(Constants.VALUE_RESOLVER) + 1);
            m_tcType[0] = GetTcType(tcType);
            m_thermocouple = Thermocouple.CreateThermocouple(m_tcType[0]);

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Checks for MinRange, MaxRange and OTD
        /// </summary>
        /// <param name="channel">The channel to scale</param>
        /// <param name="value">The raw A/D value</param>
        /// <returns>The original value or error condition specific value</returns>
        //===========================================================================================
        internal override ErrorCodes PrescaleData(int channel, ref double value)
        {
            m_otd = false;

            if (value == m_maxCount)
            {
                if (m_tcType[0] == ThermocoupleTypes.TypeE)
                {
                    double newValue = SwitchRange(PropertyValues.BIPPT14625V, value);

                    if (newValue == m_maxCount)
                    {
                        m_otd = true;
                        value = Constants.OPEN_THERMOCOUPLE;
                        return ErrorCodes.OpenThermocouple;
                    }
                    else
                    {
                        value = newValue;
                        return ErrorCodes.NoErrors;
                    }
                }
                else
                {
                    m_otd = true;
                }

                if (m_otd == true)
                {
                    value = Constants.OPEN_THERMOCOUPLE;
                    return ErrorCodes.OpenThermocouple;
                }
            }
            else
            {
                if (m_otd)
                    value = SwitchRange(PropertyValues.BIPPT073125V, value);
            }

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// generates a response based on any errors that occur in one of the Preprocess data methods
        /// </summary>
        /// <param name="errorCode">The error code that was set in the Preprocess data method</param>
        /// <param name="originalResponse">The response before calling the Preprocess data method</param>
        /// <returns>The response</returns>
        //===========================================================================================
        internal override string GetPreprocessDataErrorResponse(ErrorCodes errorCode, string originalResponse)
        {
            int valueIndex = originalResponse.IndexOf("=") + 1;
            string response = originalResponse.Remove(valueIndex, originalResponse.Length - valueIndex);
            string errorValue = String.Empty;

            if (errorCode == ErrorCodes.OpenThermocouple)
                errorValue = PropertyValues.OTD;
            else if (errorCode == ErrorCodes.MaxTempRange)
                errorValue = PropertyValues.MAXRNG;
            else if (errorCode == ErrorCodes.MinTempRange)
                errorValue = PropertyValues.MINRNG;

            response += errorValue;

            return response;
        }

        //=======================================================================================================================
        /// <summary>
        /// Switches between the 73mV and 146mV range and gets a new value following the range switch
        /// </summary>
        /// <param name="range">The range to switch to</param>
        /// <param name="value">The value read while in the previous range setting</param>
        /// <returns>The value returned after switching ranges</returns>
        //=======================================================================================================================
        protected double SwitchRange(string range, double value)
        {
            if (!m_ranges[0].Contains(range))
            {
                range = MessageTranslator.ConvertToCurrentCulture(range);

                m_daqDevice.SendMessageDirect("AI{0}:RANGE=" + range);
                m_ranges[0] = DaqComponents.AI + "{0}" + Constants.PROPERTY_SEPARATOR + DaqProperties.RANGE + "=" + range;
                m_activeChannels[0].UpperLimit = m_supportedRanges[range + ":DIFF"].UpperLimit;
                m_activeChannels[0].LowerLimit = m_supportedRanges[range + ":DIFF"].LowerLimit;

                ((Usb2001Tc)m_daqDevice).WaitForReady();

                m_daqDevice.SendMessageDirect("?AI{0}:" + DaqProperties.VALUE);

                // return the new value
                return m_daqDevice.DriverInterface.ReadValueDirect();
            }

            // return the original value
            return value;
        }

        //========================================================================================================================
        /// <summary>
        /// Overriden to get the valid channels
        /// </summary>
        /// <param name="includeMode">A flag to indicate if the channel mode should be included with the channel number</param>
        /// <returns>The valid channels</returns>
        //========================================================================================================================
        internal override string GetValidChannels(bool includeMode)
        {
            string validChannels = String.Empty;

            for (int i = 0; i < m_maxChannels; i++)
            {
                validChannels += i.ToString();

                if (includeMode)
                    validChannels += String.Format(" ({0})", m_channelModes[i]);

                if (i < (m_maxChannels - 1))
                    validChannels += PlatformInterop.LocalListSeparator;
            }

            if (validChannels.EndsWith(","))
                validChannels = validChannels.Remove(validChannels.LastIndexOf(","), 1);

            return validChannels;
        }

        //====================================================================================================================
        /// <summary>
        /// Overriden to NOT device maxChannels by 2 if the configuration is DIFF 
        /// This is used when the channels that a feature pertains to = "ALL"
        /// </summary>
        /// <param name="devCaps">The devCaps list</param>
        /// <param name="devCapsKey">The devCaps key</param>
        /// <param name="devCapsValue">The devCaps value</param>
        //====================================================================================================================
        internal override void AddChannelDevCapsKey(Dictionary<string, string> devCaps,
            string component,
            string devCapsName,
            string configuration,
            string devCapsValue)
        {
            string chCaps;

            int maxChannels = m_maxChannels;

            for (int channel = 0; channel < maxChannels; channel++)
            {
                chCaps = component + "{" + channel.ToString() + "}:" + devCapsName;

                if (configuration != "ALL")
                    chCaps += ("/" + configuration);

                devCaps.Add(chCaps, devCapsValue);
            }
        }
    }
}
