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
using System.Threading;

namespace MeasurementComputing.DAQFlex
{
    class AiTempComponent : AiComponent
    {
        protected ThermocoupleTypes[] m_tcType;
        protected TemperatureUnits m_units;
        protected TemperatureUnits m_unitsClone;
        protected AiChannelTypes[] m_aiChannelType;
        protected Thermocouple m_thermocouple;
        protected Dictionary<ThermocoupleTypes, TcTempLimits> m_tcRanges = new Dictionary<ThermocoupleTypes, TcTempLimits>();
        protected bool m_otd;

        protected double [] m_cjcValues;
        protected System.Diagnostics.Stopwatch m_cjcStopWatch = new System.Diagnostics.Stopwatch();
        protected bool m_cjcThreadStarted = false;
        protected Thread m_cjcThread;
        protected object m_readCjcLock = new Object();

        protected bool m_UseTempUnits = false;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal AiTempComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
            m_aiChannelType = new AiChannelTypes[m_maxChannels];
            m_tcType = new ThermocoupleTypes[m_maxChannels];
            m_cjcValues = new double[m_maxChannels];
        }

        //================================================================================================
        /// <summary>
        /// Gets the type of tc that the channel is currently set for
        /// </summary>
        /// <returns>The tc type</returns>
        //================================================================================================
        internal virtual AiChannelTypes GetChannelType(int channel)
        {
            // query the sensor type which is stored on the device
            m_daqDevice.SendMessageDirect(string.Format("?AI{0}:CHMODE", MessageTranslator.GetChannelSpecs(channel)));
            string response = m_daqDevice.m_driverInterface.ReadStringDirect();

            string channelMode = response.Substring(response.IndexOf(Constants.EQUAL_SIGN) + 1);

            return GetChannelType(channelMode);
        }

        //================================================================================================
        /// <summary>
        /// Gets the type of tc that the channel is currently set for
        /// </summary>
        /// <returns>The tc type</returns>
        //================================================================================================
        internal AiChannelTypes GetChannelType(string type)
        {
            AiChannelTypes channelType;

            switch (type)
            {
                case "TC/OTD": channelType = AiChannelTypes.Temperature;
                    break;
                case "TC/NOOTD": channelType = AiChannelTypes.Temperature;
                    break;
                default: channelType = AiChannelTypes.Voltage;
                    break;
            }

            return channelType;
        }

        //================================================================================================
        /// <summary>
        /// Gets the type of tc that the channel is currently set for
        /// </summary>
        /// <returns>The tc type</returns>
        //================================================================================================
        internal ThermocoupleTypes GetTcType(int channel)
        {
            // query the sensor type which is stored on the device
            m_daqDevice.SendMessageDirect(string.Format("?AI{0}:SENSOR", MessageTranslator.GetChannelSpecs(channel)));
            string response = m_daqDevice.m_driverInterface.ReadStringDirect();

            string tcType = response.Substring(response.IndexOf(Constants.VALUE_RESOLVER) + 1);

            return GetTcType(tcType);
        }

        //================================================================================================
        /// <summary>
        /// Gets the type of tc based on the type letter passed in
        /// </summary>
        /// <param name="type">The type letter (e.g. "T")</param>
        /// <returns></returns>
        //================================================================================================
        internal ThermocoupleTypes GetTcType(string type)
        {
            ThermocoupleTypes tcType;

            switch (type)
            {
                case "B": tcType = ThermocoupleTypes.TypeB;
                    break;
                case "E": tcType = ThermocoupleTypes.TypeE;
                    break;
                case "J": tcType = ThermocoupleTypes.TypeJ;
                    break;
                case "K": tcType = ThermocoupleTypes.TypeK;
                    break;
                case "N": tcType = ThermocoupleTypes.TypeN;
                    break;
                case "R": tcType = ThermocoupleTypes.TypeR;
                    break;
                case "S": tcType = ThermocoupleTypes.TypeS;
                    break;
                case "T": tcType = ThermocoupleTypes.TypeT;
                    break;
                default: tcType = ThermocoupleTypes.NotSet;
                    break;
            }

            return tcType;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the Units associtated with the Ai Value message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal ErrorCodes ValidateValueMsgUnits(int channel, string message)
        {
            if (message.Contains("VALUE/"))
            {
                string units = message.Substring(message.IndexOf("/") + 1);

                // verify the units are a valid unit
                if (!units.Equals(ValueResolvers.DEGF) &&
                        !units.Equals(ValueResolvers.DEGC) &&
                            !units.Equals(ValueResolvers.KELVIN) &&
                                !units.Equals(ValueResolvers.VOLTS) &&
                                    !units.Equals(ValueResolvers.RAW))
                    return ErrorCodes.InvalidMessage;


                // for voltage, the allowable units are VOLTS and RAW
                if (m_aiChannelType[channel] == AiChannelTypes.Voltage)
                {
                    if (!units.Equals(ValueResolvers.VOLTS) && 
                            !units.Equals(ValueResolvers.RAW))
                        return ErrorCodes.InvalidValueResolver;
                }


                // for temperature, the allowable units are DEGC, DEGF, and KELVIN
                if (m_aiChannelType[channel] == AiChannelTypes.Temperature)
                {
                    if (!units.Equals(ValueResolvers.DEGF) && 
                            !units.Equals(ValueResolvers.DEGC) && 
                                !units.Equals(ValueResolvers.KELVIN))
                        return ErrorCodes.InvalidValueResolver;
                }
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
        internal override ErrorCodes ProcessValueGetMessage(int channel, ref string message)
        {
            m_voltsOnly = false;

            if (message.Contains(string.Format("?AI{0}:VALUE", MessageTranslator.GetChannelSpecs(channel))) && m_tcType[channel] == ThermocoupleTypes.NotSet)
            {
                return ErrorCodes.ThermocoupleTypeNotSet;
            }

            ErrorCodes errorCode = ValidateValueMsgUnits(channel, message);
            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            m_aiChannelType[channel] = GetChannelType(channel); 
            if (m_aiChannelType[channel] == AiChannelTypes.Temperature)
            {
                // set the clones for restoring original flags after SendMessage is complete
                m_calibrateDataClone = m_calibrateData;
                m_scaleDataClone = m_scaleData;
                m_unitsClone = m_units;
                m_valueUnitsClone = m_valueUnits;

                //m_units = TemperatureUnits.None;
                //m_valueUnits = String.Empty;
                //m_calibrateData = false;
                //m_scaleData = false;

                if (message.Contains(string.Format("VALUE/RAW", MessageTranslator.GetChannelSpecs(channel))))
                {
                    m_calibrateData = false;
                    m_scaleData = false;

                    m_valueUnits = "/RAW";
                    message = MessageTranslator.RemoveValueResolver(message);

                    return ErrorCodes.NoErrors;
                }

                if (message.Contains(string.Format("VALUE/VOLTS", MessageTranslator.GetChannelSpecs(channel))))
                {
                    m_calibrateData = true;
                    m_scaleData = true;

                    m_valueUnits = "/VOLTS";
                    message = MessageTranslator.RemoveValueResolver(message);

                    m_daqDevice.SendMessageDirect(string.Format("?AI{0}:CJC/VOLTS", MessageTranslator.GetChannelSpecs(channel)));
                    m_cjcValues[channel] = m_daqDevice.DriverInterface.ReadValueDirect();

                    return ErrorCodes.NoErrors;
                }

                if (message.Contains(string.Format("VALUE/DEGC", MessageTranslator.GetChannelSpecs(channel))))
                {
                    m_calibrateData = true;
                    m_scaleData = true;

                    m_valueUnits = "/DEGC";
                    message = MessageTranslator.RemoveValueResolver(message);

                    // get the CJC value in deg C
                    m_daqDevice.SendMessageDirect(string.Format("?AI{0}:CJC/DEGC", MessageTranslator.GetChannelSpecs(channel)));
                    m_cjcValues[channel] = m_daqDevice.DriverInterface.ReadValueDirect();
                    m_units = TemperatureUnits.Celsius;

                    return ErrorCodes.NoErrors;
                }

                if (message.Contains(string.Format("VALUE/DEGF", MessageTranslator.GetChannelSpecs(channel))))
                {
                    //m_isAiData = true;
                    m_calibrateData = true;
                    m_scaleData = true;

                    m_valueUnits = "/DEGF";
                    message = MessageTranslator.RemoveValueResolver(message);

                    // get the CJC value in deg F
                    m_daqDevice.SendMessageDirect(string.Format("?AI{0}:CJC/DEGF", MessageTranslator.GetChannelSpecs(channel)));
                    m_cjcValues[channel] = m_daqDevice.DriverInterface.ReadValueDirect();
                    m_units = TemperatureUnits.Fahrenheit;

                    return ErrorCodes.NoErrors;
                }

                if (message.Contains(string.Format("VALUE/KELVIN", MessageTranslator.GetChannelSpecs(channel))))
                {
                    //m_isAiData = true;
                    m_calibrateData = true;
                    m_scaleData = true;

                    m_valueUnits = "/KELVIN";
                    message = MessageTranslator.RemoveValueResolver(message);

                    // get the CJC value in kelvin
                    m_daqDevice.SendMessageDirect(string.Format("?AI{0}:CJC/KELVIN", MessageTranslator.GetChannelSpecs(channel)));
                    m_cjcValues[channel] = m_daqDevice.DriverInterface.ReadValueDirect();
                    m_units = TemperatureUnits.Kelvin;

                    return ErrorCodes.NoErrors;
                }
                else if (m_UseTempUnits)
                {
                    // This is an undocumented feature for Kona ... DAQFlex customers should never get here.
                    //
                    // This allows the use of TEMPUNITS with a message that is of the form 
                    // AI{0}:VALUE (no /DEGC, /DEGF, or /KELVIN).
                    m_calibrateData = true;
                    m_scaleData = true;

                    // get the CJC value in kelvin
                    m_daqDevice.SendMessageDirect(string.Format("?AI{0}:CJC/{1}", MessageTranslator.GetChannelSpecs(channel), m_valueUnits));
                    m_cjcValues[channel] = m_daqDevice.DriverInterface.ReadValueDirect();

                    return ErrorCodes.NoErrors;
                }
            }
            else 
                base.ProcessValueGetMessage(channel, ref message);

            return ErrorCodes.NoErrors;
        }
        
        //====================================================================================
        /// <summary>
        /// Overridden method for processing a temp units message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessTempUnitsMessage(string message)
        {
            if (message[0] == Constants.QUERY)
            {
                if (m_valueUnits == string.Empty)
                    m_daqDevice.ApiResponse = new DaqResponse(message.Remove(0, 1) + "=" + m_units.ToString(), double.NaN);
                else
                    m_daqDevice.ApiResponse = new DaqResponse(message.Remove(0, 1) + "=" + m_valueUnits.Remove(0, 1), double.NaN);
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
            else
            {
                if (message.Contains("DEGC"))
                {
                    m_units = TemperatureUnits.Celsius;
                    m_valueUnits = "/DEGC";
                    m_UseTempUnits = true;
                }
                else if (message.Contains("DEGF"))
                {
                    m_units = TemperatureUnits.Fahrenheit;
                    m_valueUnits = "/DEGF";
                    m_UseTempUnits = true;
                }
                else if (message.Contains("KELVIN"))
                {
                    m_units = TemperatureUnits.Kelvin;
                    m_valueUnits = "/KELVIN";
                    m_UseTempUnits = true;
                }
                else
                {
                    m_units = TemperatureUnits.Volts;
                    m_valueUnits = "/VOLTS";
                    m_UseTempUnits = true;
                }

                m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);

                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
        }
        
        //================================================================================
        /// <summary>
        /// Override method for setting the channel type based on the channel mode.
        /// Message is in the form AIQUEUE{0}:CHMODE=SE
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal override ErrorCodes PreprocessQueueChannelMode(ref string message)
        {
            ErrorCodes errorCode = base.PreprocessQueueChannelMode(ref message);

            if (errorCode == ErrorCodes.NoErrors)
            {
                int element = MessageTranslator.GetQueueElement(message);

                string msg = Messages.AIQUEUE_CHAN_QUERY;
                msg = Messages.InsertElement(msg, element);
                m_daqDevice.SendMessageDirect(msg);
                int channel = (int)m_daqDevice.DriverInterface.ReadValueDirect();

                string channelMode = MessageTranslator.GetPropertyValue(message);
                m_aiChannelType[channel] = GetChannelType(channelMode);
            }

            return errorCode;
        }

        //================================================================================
        /// <summary>
        /// Override method for setting low and high channel in CritcalParams
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal override ErrorCodes PreprocessQueueChannel(ref string message)
        {
            ErrorCodes errorCode = base.PreprocessQueueChannel(ref message);

            if (errorCode == ErrorCodes.NoErrors)
            {
                int channel = Int32.Parse(MessageTranslator.GetPropertyValue(message));

                m_daqDevice.SendMessageDirect(Messages.AIQUEUE_COUNT_QUERY);
                string response = m_daqDevice.DriverInterface.ReadStringDirect();
                int queueCount = Convert.ToInt32(MessageTranslator.GetPropertyValue(response));

                if (queueCount == 0)
                {
                    m_daqDevice.CriticalParams.LowAiChannel = channel;
                    m_daqDevice.CriticalParams.HighAiChannel = channel;
                }
                else
                {
                    if (channel < m_daqDevice.CriticalParams.LowAiChannel)
                        m_daqDevice.CriticalParams.LowAiChannel = channel;

                    if (channel > m_daqDevice.CriticalParams.HighAiChannel)
                        m_daqDevice.CriticalParams.HighAiChannel = channel;
                }
            }

            return errorCode;
        }

        //===========================================================================================
        /// <summary>
        /// Scales an analog input value
        /// </summary>
        /// <param name="value">The raw A/D value</param>
        /// <returns>The scaled value</returns>
        //===========================================================================================
        internal override ErrorCodes ScaleData(int channelIndex, ref double value)
        {
            double scaledValue = value;
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (m_scaleData)
            {
                base.ScaleData(channelIndex, ref scaledValue);

                int channel = m_activeChannels[channelIndex].ChannelNumber;
                if (m_aiChannelType[channel] == AiChannelTypes.Temperature)
                    errorCode = ScaledDataToTemperature(channelIndex, ref scaledValue);
            }

            value = scaledValue;

            return errorCode;
        }

        //================================================================================================
        /// <summary>
        /// Sets up the min and max A/D counts for each tc type to use for OTD and range checking
        /// </summary>
        //================================================================================================
        internal ErrorCodes ScaledDataToTemperature(int channelIndex, ref double scaledValue)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // this value is calculated by the TemperatureToVoltage method and is in mV
            double cjcVolts;

            int channel = m_activeChannels[channelIndex].ChannelNumber;
            if (m_thermocouple != null && !m_voltsOnly && m_aiChannelType[channel] == AiChannelTypes.Temperature)
            {
                double cjcValue = m_cjcValues[channel];
                double chVolts = 1000 * scaledValue; // in mV
                double valueDegC = 0.0;

                if (m_units == TemperatureUnits.Fahrenheit)
                {
                    cjcVolts = m_thermocouple.TemperatureToVoltage(m_thermocouple.FtoC(cjcValue));
                    valueDegC = m_thermocouple.VoltageToTemperature(chVolts + cjcVolts);
                    scaledValue = m_thermocouple.CtoF(valueDegC);
                }
                else if (m_units == TemperatureUnits.Kelvin)
                {
                    cjcVolts = m_thermocouple.TemperatureToVoltage(m_thermocouple.KtoC(cjcValue));
                    valueDegC = m_thermocouple.VoltageToTemperature(chVolts + cjcVolts);
                    scaledValue = m_thermocouple.CtoK(valueDegC);
                }
                else if (m_units == TemperatureUnits.Celsius)
                {
                    cjcVolts = m_thermocouple.TemperatureToVoltage(cjcValue);
                    scaledValue = valueDegC = m_thermocouple.VoltageToTemperature(chVolts + cjcVolts);
                }
                else
                {
                    cjcVolts = m_thermocouple.TemperatureToVoltage(cjcValue);
                    valueDegC = m_thermocouple.VoltageToTemperature(chVolts + cjcVolts);
                    scaledValue = valueDegC;
                }

                if (m_otd)
                {
                    // we'll only make it to here if ch mode is TC/NOOTD
                    scaledValue = Constants.OPEN_THERMOCOUPLE;
                }
                else if (valueDegC < m_tcRanges[m_tcType[channel]].LowerLimit)
                {
                    scaledValue = Constants.VALUE_OUT_OF_RANGE;

                    if (m_channelModes[channelIndex] != PropertyValues.TCNOOTD)
                        errorCode = ErrorCodes.MinTempRange;
                }
                else if (valueDegC > m_tcRanges[m_tcType[channel]].UpperLimit)
                {
                    scaledValue = Constants.VALUE_OUT_OF_RANGE;

                    if (m_channelModes[channelIndex] != PropertyValues.TCNOOTD)
                        errorCode = ErrorCodes.MaxTempRange;
                }
            }

            return errorCode;
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
        /// Checks for MinRange, MaxRange and OTD
        /// </summary>
        /// <param name="channel">The channel to scale</param>
        /// <param name="value">The raw A/D value</param>
        /// <returns>The original value or error condition specific value</returns>
        //===========================================================================================
        internal override ErrorCodes PrescaleData(int channel, ref double value)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (value == m_maxCount)
            {
                m_otd = true;
                errorCode = ErrorCodes.OpenThermocouple;
            }
            else
            {
                m_otd = false;
            }

            return errorCode;
        }

        //====================================================================
        /// <summary>
        /// Thread to periodically read the CJC values
        /// </summary>
        //====================================================================
        protected void ReadCjcValuesThread()
        {
            long elapsedTime = 0;
            long lastElapsedTime = 0;
            string msg = string.Empty;

            m_cjcStopWatch.Reset();
            m_cjcStopWatch.Start();

            int lowChannel = m_daqDevice.CriticalParams.LowAiChannel;
            int highChannel = m_daqDevice.CriticalParams.HighAiChannel;

            m_cjcThreadStarted = true;

            bool continueReading = true;
            while (continueReading)
            {
                double cjcValue = 0.0;

                Monitor.Enter(m_readCjcLock);

                // read the CJCs
                for (int i=lowChannel; i<= highChannel; i++)
                {
                    if (m_aiChannelType[i] == AiChannelTypes.Temperature)
                    {
                        msg = Messages.AI_CJC_QUERY + m_valueUnits;
                        msg = Messages.InsertChannel(msg, i);

                        m_daqDevice.SendMessageDirect(msg);
                        cjcValue = m_daqDevice.DriverInterface.ReadValueDirect();

                        m_cjcValues[i] = cjcValue;
                    }
                }

                Monitor.Exit(m_readCjcLock);


                while (elapsedTime - lastElapsedTime < 5000 && continueReading)
                {
                    Thread.Sleep(1000);
                    elapsedTime = m_cjcStopWatch.ElapsedMilliseconds;

                    if (m_daqDevice.DriverInterface.InputScanStatus != ScanState.Running)
                        continueReading = false;
                }
                lastElapsedTime = elapsedTime;
            }

        }

        //===========================================================================================
        /// <summary>
        /// Override method for invoking device-specific methods for starting an input scan
        /// </summary>
        //===========================================================================================
        internal override void BeginInputScan()
        {
            if (m_scaleData)
            {
                foreach (ActiveChannels activeChan in m_activeChannels)
                {
                    if (m_aiChannelType[activeChan.ChannelNumber] == AiChannelTypes.Temperature)
                    {
                        // start the CJC thread
                        m_cjcThread = new Thread(new ThreadStart(ReadCjcValuesThread));
                        m_cjcThread.Name = "ReadCjcThread";
                        m_cjcThread.Start();

                        // wait for the ReadCjcValuesThread to set the flag indicating the thread has started
                        while (!m_cjcThreadStarted)
                            Thread.Sleep(0);

                        break;
                    }
                }
            }

            base.BeginInputScan();
        }

        //===========================================================================================
        /// <summary>
        /// Override method for invoking device-specific methods for stopping an input scan
        /// </summary>
        //===========================================================================================
        internal override void EndInputScan()
        {
            if (m_cjcThread != null)
                m_cjcThread.Join();
        }

        //====================================================
        /// <summary>
        /// Restores the API flags
        /// </summary>
        //====================================================
        internal override void RestoreApiFlags()
        {
            m_valueUnits = m_valueUnitsClone;
            m_units = m_unitsClone;

            base.RestoreApiFlags();
        }
    }
}
