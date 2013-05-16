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
    class Usb2001TcAi : AiComponent
    {
        protected ThermocoupleTypes m_tcType;
        protected double m_cjcValue;
        protected TemperatureUnits m_units;
        protected Thermocouple m_thermocouple;
        protected Dictionary<ThermocoupleTypes, TcTempLimits> m_tcRanges = new Dictionary<ThermocoupleTypes, TcTempLimits>();
        protected bool m_otd;

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
            m_dataWidth = 20;
            m_maxCount = (int)Math.Pow(2, m_dataWidth) - 1;
            m_scaleData = true;

            m_tcType = ((Usb2001Tc)m_daqDevice).GetTcType();

            if (m_tcType != ThermocoupleTypes.NotSet)
                m_thermocouple = Thermocouple.CreateThermocouple(m_tcType);

            InitializeChannelModes();
            InitializeRanges();
            IntializeTCRanges();
            SetDefaultCriticalParams(deviceInfo);

            // reset defaults
            m_daqDevice.SendMessageDirect("DEV:RESET/DEFAULT");

            ((Usb2001Tc)m_daqDevice).WaitForReady();
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize channel modes
        /// </summary>
        //=================================================================================================================
        internal override void InitializeChannelModes()
        {
            m_channelModes = new AiChannelMode[m_maxChannels];

            // this device is fixed at single-ended
            for (int i = 0; i < m_channelModes.Length; i++)
                m_channelModes[i] = AiChannelMode.SingleEnded;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            m_supportedRanges.Add(PropertyValues.BIPPT073125V + ":DIFF", new Range(0.073125, -0.073125));
            m_supportedRanges.Add(PropertyValues.BIPPT14625V + ":DIFF", new Range(0.14625, -0.14625));

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
                    double slope = m_daqDevice.DriverInterface.ReadValue();
                    m_daqDevice.SendMessageDirect(String.Format("?AI{0}:OFFSET", MessageTranslator.GetChannelSpecs(i)));
                    double offset = m_daqDevice.DriverInterface.ReadValue();
                    m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(slope, offset));
                }
            }

            // store default range
            m_ranges[0] = "AI{0}:RANGE=BIP73.125E-3V";

            m_activeChannels = new ActiveChannels[1];
            m_activeChannels[0].ChannelNumber = 0;
            m_activeChannels[0].UpperLimit = m_supportedRanges[PropertyValues.BIPPT073125V + ":DIFF"].UpperLimit;
            m_activeChannels[0].LowerLimit = m_supportedRanges[PropertyValues.BIPPT073125V + ":DIFF"].LowerLimit;
        }

        //================================================================================================
        /// <summary>
        /// Sets up the min and max A/D counts for each tc type to use for OTD and range checking
        /// </summary>
        //================================================================================================
        internal void IntializeTCRanges()
        {
            m_tcRanges.Clear();

            m_tcRanges.Add(ThermocoupleTypes.TypeB, new TcTempLimits(Thermocouple.TypeBMinTemp, Thermocouple.TypeBMaxTemp));
            m_tcRanges.Add(ThermocoupleTypes.TypeE, new TcTempLimits(Thermocouple.TypeEMinTemp, Thermocouple.TypeEMaxTemp));
            m_tcRanges.Add(ThermocoupleTypes.TypeJ, new TcTempLimits(Thermocouple.TypeJMinTemp, Thermocouple.TypeJMaxTemp));
            m_tcRanges.Add(ThermocoupleTypes.TypeK, new TcTempLimits(Thermocouple.TypeKMinTemp, Thermocouple.TypeKMaxTemp));
            m_tcRanges.Add(ThermocoupleTypes.TypeN, new TcTempLimits(Thermocouple.TypeNMinTemp, Thermocouple.TypeNMaxTemp));
            m_tcRanges.Add(ThermocoupleTypes.TypeR, new TcTempLimits(Thermocouple.TypeRMinTemp, Thermocouple.TypeRMaxTemp));
            m_tcRanges.Add(ThermocoupleTypes.TypeS, new TcTempLimits(Thermocouple.TypeSMinTemp, Thermocouple.TypeSMaxTemp));
            m_tcRanges.Add(ThermocoupleTypes.TypeT, new TcTempLimits(Thermocouple.TypeTMinTemp, Thermocouple.TypeTMaxTemp));
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

        //===========================================================================================
        /// <summary>
        /// Applies calibration coefficients to the raw A/D value if the CAL=ENABLE message
        /// was previously sent
        /// </summary>
        /// <param name="channel">The channel to scale</param>
        /// <param name="value">The raw A/D value</param>
        /// <returns>The calibrated value</returns>
        //===========================================================================================
        internal override double CalibrateData(int channel, double value)
        {
            double calibratedValue = value;

            if (m_calibrateData)
            {
                if (m_activeChannels[0].CalSlope != 0 && !Double.IsNaN(m_activeChannels[0].CalSlope))
                {
                    calibratedValue = value * m_activeChannels[0].CalSlope;
                    calibratedValue += m_activeChannels[0].CalOffset;
                }
            }

            return calibratedValue;
        }

        //===========================================================================================
        /// <summary>
        /// Scales an analog input value
        /// </summary>
        /// <param name="channel">The channel to scale</param>
        /// <param name="value">The raw A/D value</param>
        /// <returns>The scaled value</returns>
        //===========================================================================================
        internal override ErrorCodes ScaleData(ref double value)
        {
            double scaledValue = value;
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (m_scaleData)
            {
                // this value is calculated by the TemperatureToVoltage method and is in mV
                double cjcVolts;

                base.ScaleData(ref value);

                scaledValue = value;

                if (m_thermocouple != null && !m_voltsOnly)
                {
                    double chVolts = 1000 * scaledValue; // in mV
                    double valueDegC = 0.0;

                    if (m_units == TemperatureUnits.Fahrenheit)
                    {
                        cjcVolts = m_thermocouple.TemperatureToVoltage(m_thermocouple.FtoC(m_cjcValue));
                        valueDegC = m_thermocouple.VoltageToTemperature(chVolts + cjcVolts);
                        scaledValue = m_thermocouple.CtoF(valueDegC);
                    }
                    else if (m_units == TemperatureUnits.Kelvin)
                    {
                        cjcVolts = m_thermocouple.TemperatureToVoltage(m_thermocouple.KtoC(m_cjcValue));
                        valueDegC = m_thermocouple.VoltageToTemperature(chVolts + cjcVolts);
                        scaledValue = m_thermocouple.CtoK(valueDegC);
                    }
                    else if (m_units == TemperatureUnits.Celsius)
                    {
                        cjcVolts = m_thermocouple.TemperatureToVoltage(m_cjcValue);
                        scaledValue = valueDegC = m_thermocouple.VoltageToTemperature(chVolts + cjcVolts);
                    }
                    else
                    {
                        cjcVolts = m_thermocouple.TemperatureToVoltage(m_cjcValue);
                        valueDegC = m_thermocouple.VoltageToTemperature(chVolts + cjcVolts);
                    }

                    if (valueDegC < m_tcRanges[m_tcType].LowerLimit)
                    {
                        scaledValue = Constants.VALUE_OUT_OF_RANGE;
                        errorCode = ErrorCodes.MinTempRange;
                    }
                    else if (valueDegC > m_tcRanges[m_tcType].UpperLimit)
                    {
                        scaledValue = Constants.VALUE_OUT_OF_RANGE;
                        errorCode = ErrorCodes.MaxTempRange;
                    }
                }
            }

            value = scaledValue;

            return errorCode;
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
        /// Validates the Ai Value message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessValueMessage(ref string message)
        {
            m_voltsOnly = false;

            if (message.Contains("?AI{0}:VALUE") && m_tcType == ThermocoupleTypes.NotSet)
            {
                return ErrorCodes.ThermocoupleTypeNotSet;
            }

            m_units = TemperatureUnits.None;
            m_valueUnits = String.Empty;
            m_calibrateData = false;
            m_scaleData = false;

            if (message.Contains("?AI{0}:VALUE/RAW"))
            {
                m_calibrateData = false;
                m_scaleData = false;

                m_valueUnits = "/RAW";
                message = MessageTranslator.RemoveValueResolver(message);

                return ErrorCodes.NoErrors;
            }

            if (message.Contains("?AI{0}:VALUE/VOLTS"))
            {
                m_calibrateData = true;
                m_scaleData = true;

                m_valueUnits = "/VOLTS";
                message = MessageTranslator.RemoveValueResolver(message);

                m_daqDevice.SendMessageDirect("?AI{0}:CJC/DEGC");
                m_cjcValue = m_daqDevice.DriverInterface.ReadValue();

                return ErrorCodes.NoErrors;
            }

            if (message.Contains("?AI{0}:VALUE/DEGC"))
            {
                m_calibrateData = true;
                m_scaleData = true;

                m_valueUnits = "/DEGC";
                message = MessageTranslator.RemoveValueResolver(message);

                // get the CJC value in deg C
                m_daqDevice.SendMessageDirect("?AI{0}:CJC/DEGC");
                m_cjcValue = m_daqDevice.DriverInterface.ReadValue();
                m_units = TemperatureUnits.Celsius;

                return ErrorCodes.NoErrors;
            }

            if (message.Contains("?AI{0}:VALUE/DEGF"))
            {
                //m_isAiData = true;
                m_calibrateData = true;
                m_scaleData = true;

                m_valueUnits = "/DEGF";
                message = MessageTranslator.RemoveValueResolver(message);

                // get the CJC value in deg F
                m_daqDevice.SendMessageDirect("?AI{0}:CJC/DEGF");
                m_cjcValue = m_daqDevice.DriverInterface.ReadValue();
                m_units = TemperatureUnits.Fahrenheit;

                return ErrorCodes.NoErrors;
            }

            if (message.Contains("?AI{0}:VALUE/KELVIN"))
            {
                //m_isAiData = true;
                m_calibrateData = true;
                m_scaleData = true;

                m_valueUnits = "/KELVIN";
                message = MessageTranslator.RemoveValueResolver(message);

                // get the CJC value in kelvin
                m_daqDevice.SendMessageDirect("?AI{0}:CJC/KELVIN");
                m_cjcValue = m_daqDevice.DriverInterface.ReadValue();
                m_units = TemperatureUnits.Kelvin;

                return ErrorCodes.NoErrors;
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
            m_tcType = ((Usb2001Tc)m_daqDevice).GetTcType(tcType);
            m_thermocouple = Thermocouple.CreateThermocouple(m_tcType);

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
            //if (value == m_maxCount && m_units != TemperatureUnits.None)
            if (value == m_maxCount)
            {
                if (m_tcType == ThermocoupleTypes.TypeE)
                {
                    double newValue = SwitchRange(PropertyValues.BIPPT14625V, value);

                    if (newValue == m_maxCount)
                    {
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
                m_daqDevice.SendMessageDirect("AI{0}:RANGE=" + range);
                m_ranges[0] = DaqComponents.AI + "{0}" + Constants.PROPERTY_SEPARATOR + DaqProperties.RANGE + "=" + range;
                m_activeChannels[0].UpperLimit = m_supportedRanges[range + ":DIFF"].UpperLimit;
                m_activeChannels[0].LowerLimit = m_supportedRanges[range + ":DIFF"].LowerLimit;

                ((Usb2001Tc)m_daqDevice).WaitForReady();

                m_daqDevice.SendMessageDirect("?AI{0}:" + DaqProperties.VALUE);

                // return the new value
                return m_daqDevice.DriverInterface.ReadValue();
            }

            // return the original value
            return value;
        }

    }
}
