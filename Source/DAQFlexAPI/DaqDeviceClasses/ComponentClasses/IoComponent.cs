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
using System.Globalization;


namespace MeasurementComputing.DAQFlex
{
    internal class IoComponent
    {
        protected const int DEFAULT_READ_TIMEOUT = 3000;
        protected const int DEFAULT_WRITE_TIMEOUT = 3000;

        protected DaqDevice m_daqDevice;
        protected DeviceInfo m_deviceInfo;

        protected Dictionary<string, CalCoeffs> m_calCoeffs = new Dictionary<string, CalCoeffs>();
        protected Dictionary<string, Range> m_supportedRanges = new Dictionary<string, Range>();
        protected ActiveChannels[] m_activeChannels;

        protected bool m_calibrateData;
        protected bool m_calibrateDataClone;
        protected bool m_scaleData = false;
        protected bool m_scaleDataClone = false;

        protected string[] m_ranges;
        protected int m_channelCount;
        protected int m_dataWidth;
        protected int m_maxChannels;
        protected bool m_voltsOnly;
        protected double m_maxScanThroughput;
        protected double m_maxScanRate;
        protected double m_minScanRate;
        protected string m_valueUnits = String.Empty;
        protected string m_valueUnitsClone = String.Empty;
        protected List<string> m_defaultParamMessages = new List<string>();
        protected bool m_adjustScanRateForChannelCount = false;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal IoComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
        {
            m_daqDevice = daqDevice;
            m_deviceInfo = deviceInfo;
            m_maxChannels = maxChannels;
        }

        //====================================================================================
        /// <summary>
        /// Optional units used to set and query values
        /// e.g. VALUE/VOLTS or VALUE/RAW
        /// </summary>
        //====================================================================================
        internal string ValueUnits
        {
            get { return m_valueUnits; }
        }

        //====================================================================================
        /// <summary>
        /// Gets the max number of channels for the IOComponent
        /// </summary>
        //====================================================================================
        internal virtual int MaxChannels
        {
            get { return m_maxChannels; }
        }

        //====================================================================================
        /// <summary>
        /// Virtual method to get supported messages
        /// </summary>
        /// <returns>A list of supported messages</returns>
        //====================================================================================
        internal virtual List<string> GetMessages(string daqComponent)
        {
            return null;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method for resetting a IoComponent's critical params
        /// </summary>
        //===========================================================================================
        protected virtual void ResetCriticalParams() { }

#region Message Processing and Validation

        //===========================================================================================
        /// <summary>
        /// Virtual method to process any data before a message is sent to a device
        /// </summary>
        /// <param name="dataType">The type of data (e.g. Ai, Ao, Dio)</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes PreprocessData(ref string message, string componentType)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to process any data after a message is sent to a device
        /// </summary>
        /// <param name="dataType">The type of data (e.g. Ai, Ao, Dio)</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes PostProcessData(string componentType, ref string response, ref double value)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Virutal method to validate message parameters
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <returns>An error Code</returns>
        //===========================================================================================
        internal virtual ErrorCodes PreprocessMessage(ref string message, string messageType)
        {
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
        internal virtual string GetPreprocessDataErrorResponse(ErrorCodes errorCode, string originalResponse)
        {
            return originalResponse;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the channel number
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ValidateChannel(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes and validates the scale message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessScaleMessage(ref string message)
        {
            // The SCALE setting is applied to all channels
            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                return ErrorCodes.InvalidMessage;
            }
            if (message[0] == Constants.QUERY)
            {
                m_daqDevice.ApiResponse = new DaqResponse(message.Remove(0, 1) + "=" + (m_scaleData ? PropertyValues.ENABLE : PropertyValues.DISABLE).ToString(), double.NaN);
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
            else
            {
                if (message.Contains(PropertyValues.ENABLE))
                {
                    m_scaleData = true;
                    m_scaleDataClone = m_scaleData;
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);

                    m_daqDevice.CriticalParams.ScaleAiData = true;
                }
                else if (message.Contains(PropertyValues.DISABLE))
                {
                    m_scaleData = false;
                    m_scaleDataClone = m_scaleData;
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);

                    m_daqDevice.CriticalParams.ScaleAiData = false;
                }
                else
                {
                    return ErrorCodes.InvalidMessage;
                }

                RecalculateTriggerLevel();

                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
        }

        //===========================================================================================
        /// <summary>
        /// Validates the cal message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessSlopeOffsetMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes an IOComponent's Start mesage if supported
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessStartMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes and validates the range message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessRangeMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes the Value message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessValueGetMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes the Value message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessValueGetMessage(int channel, ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes the Value message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessValueSetMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes the CJC message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessCJCMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method for processing AIQUEUE messages
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes PreProcessAiQueueMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes the SAMPLES message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessSamplesMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the cal message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessCalMessage(ref string message)
        {
            return ErrorCodes.NoErrors;

        }

        //===========================================================================================
        /// <summary>
        /// Validates the trig message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes ProcessTrigMessage(ref string message)
        {
            return ErrorCodes.NoErrors;

        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan status query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessScanStatusQuery(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan count query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessScanCountQuery(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan index query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessScanIndexQuery(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a buffer size query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessBufSizeQuery(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing an input buffer size message
        /// </summary>
        /// <param name="message">The message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessInputBufferSizeMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing an output buffer size message
        /// </summary>
        /// <param name="message">The message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessOutputBufferSizeMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessScanRate(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a min scan rate query
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //====================================================================================
        internal virtual ErrorCodes ProcessMinSampleRateQuery(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a max scan rate query
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //====================================================================================
        internal virtual ErrorCodes ProcessMaxSampleRateQuery(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a sample dt query
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        //====================================================================================
        internal virtual ErrorCodes ProcessSampleDtQuery(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing an external pacer message
        /// </summary>
        /// <param name="message">The device message</param>
        /// <returns>An error code</returns>
        //====================================================================================
        internal virtual ErrorCodes PreprocessExtPacer(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (message != Messages.AISCAN_EXTPACER_DISMASTER &&
                            message != Messages.AISCAN_EXTPACER_DISSLAVE &&
                                message != Messages.AISCAN_EXTPACER_ENABLE &&
                                    message != Messages.AISCAN_EXTPACER_DISABLE)
                errorCode = ErrorCodes.InvalidPropertyValueSpecified;

            return errorCode;
        }

        //=================================================================================================================
        /// <summary>
        /// Virtual function to handle the /HEX=0x... format
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessCalSlopeMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// virtual function to handle the /HEX=0x... format
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessCalOffsetMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a data rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ValidateDataRate()
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ValidateScanRate()
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ValidateSampleCount()
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing the xfer mode message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes PreProcessXferModeMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method to start a component's self calibration
        /// </summary>
        //====================================================================================
        internal virtual ErrorCodes StartCal()
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a Cal status message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessCalStatusMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a data rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessDataRateMessage(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for checking the message value against the supported values
        /// using the device's reflection values.
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual bool ValidateDaqFeature(string message, string feature)
        {
            int equalIndex = message.IndexOf(Constants.EQUAL_SIGN);

            if (equalIndex >= 0)
            {
                string messageValue;
                string supportedValues;
                string[] valueParts;

                try
                {
                    messageValue = MessageTranslator.GetPropertyValue(message);
                    if (messageValue.Contains("{"))
                        messageValue = messageValue.Substring(0, messageValue.IndexOf('{'));
                    supportedValues = m_daqDevice.GetDevCapsString(feature, false);
                    supportedValues = MessageTranslator.GetReflectionValue(supportedValues);
                    valueParts = supportedValues.Split(new char[] { PlatformInterop.LocalListSeparator });

                    if (Array.IndexOf(valueParts, messageValue) < 0)
                        return false;

                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

#endregion

        //====================================================================================
        /// <summary>
        /// Virtual method for setting critical parameters used the the driver interface
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual void SetCriticalParams(string message, string messageType)
        {
        }

        //====================================================================================
        /// <summary>
        /// Virtual method to initialize IO components
        /// </summary>
        //====================================================================================
        internal virtual void Initialize() { }

        //====================================================================================
        /// <summary>
        /// Virtual method to initialize IO components
        /// </summary>
        //====================================================================================
        internal virtual void ConfigureScan() { }

        //====================================================================================
        /// <summary>
        /// Virtual method to initialize a scan operation
        /// </summary>
        //====================================================================================
        internal virtual void RunScan() { }

        //===========================================================================================
        /// <summary>
        /// Virtual method for invoking device-specific methods for starting an input scan
        /// </summary>
        //===========================================================================================
        internal virtual void BeginInputScan()
        {
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method for invoking device-specific methods for stopping an input scan
        /// </summary>
        //===========================================================================================
        internal virtual void EndInputScan()
        {
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method for invoking device-specific methods for starting an output scan
        /// </summary>
        //===========================================================================================
        internal virtual void BeginOutputScan()
        {
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method for invoking device-specific methods for stopping an input scan
        /// </summary>
        //===========================================================================================
        internal virtual void EndOutScan()
        {
        }
        
        //====================================================================================
        /// <summary>
        /// Virtual method for scaling signed single-point I/O data
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The error code</returns>
        //====================================================================================
        internal virtual ErrorCodes ScaleData(int channelIndex, ref double value)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method to convert scaled data to counts
        /// </summary>
        /// <param name="scaledValue">The scaled value</param>
        /// <param name="rawValue">The raw value</param>
        /// <returns>The error code</returns>
        //====================================================================================
        internal virtual ErrorCodes ConvertData(double scaledValue, ref int rawValue)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method to calibrate a single data point
        /// </summary>
        /// <param name="channel">The channel number</param>
        /// <param name="value">The uncalibrated value</param>
        /// <returns>The calibrated value</returns>
        //====================================================================================
        internal virtual double CalibrateData(int channel, double value)
        {
            return 0.0;
        }

        //=================================================================================================================
        /// <summary>
        /// Virtual method copy data from the internal read buffer to the user buffer
        /// </summary>
        /// <param name="source">The source array</param>
        /// <param name="destination">The destination array</param>
        /// <param name="copyIndex">The starting source index</param>
        /// <param name="samplesToCopy">The number of samples to copy</param>
        //=================================================================================================================
        internal virtual void CopyScanData(byte[] source, double[,] destination, ref int copyIndex, int samplesToCopy)
        {
        }

        //==========================================================================================================================================
        /// <summary>
        /// Virtual method copy data from a user buffer to the internal write buffer
        /// </summary>
        /// <param name="source">The source array</param>
        /// <param name="destination">The destination array</param>
        /// <param name="copyIndex">The starting source index</param>
        /// <param name="samplesToCopy">The number of samples to copy</param>
        //==========================================================================================================================================
        internal virtual void CopyScanData(double[,] source, byte[] destination, ref int copyIndex, int samplesToCopy, int timeOut)
        {
        }

        //=================================================================================================================
        /// <summary>
        /// Virtual method to initialize range information
        /// </summary>
        //=================================================================================================================
        internal virtual void InitializeRanges() { }

        //=================================================================================================================
        /// <summary>
        /// Virtual method for setting the default values for the critical params
        /// </summary>
        //=================================================================================================================
        internal virtual void SetDefaultCriticalParams(DeviceInfo deviceInfo) { }

        //=================================================================================================================
        /// <summary>
        /// Virtual method to update the m_ranges array after a channel mode change
        /// </summary>
        //=================================================================================================================
        internal virtual void UpdateRanges() { }

        //=================================================================================================================
        /// <summary>
        /// Virtual method to determine the transfer mode when its set to default
        /// </summary>
        /// <returns>The default transfer mode</returns>
        //=================================================================================================================
        internal virtual TransferMode GetDefaultTransferMode()
        {
            return TransferMode.BlockIO;
        }

        //=================================================================================================================
        /// <summary>
        /// Virtual method to recalculate the trigger level
        /// </summary>
        //=================================================================================================================
        internal virtual void RecalculateTriggerLevel() { }

        //====================================================
        /// <summary>
        /// Restores the API flags
        /// </summary>
        //====================================================
        internal virtual void RestoreApiFlags()
        {
            m_calibrateData = m_calibrateDataClone;
            m_scaleData = m_scaleDataClone;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to post process a message
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <returns>True if the message is to be sent to the device, otherwise false</returns>
        //===========================================================================================
        internal virtual void PostProcessMessage(ref string message, string messageType)
        {
        }

#region Feature related methods

        //====================================================
        /// <summary>
        /// Gets the list of active channels
        /// </summary>
        //====================================================
        internal ActiveChannels[] ActiveChannels
        {
            get {return m_activeChannels;}
        }

        //====================================================================================================================
        /// <summary>
        /// Virtual method to create devCaps keys for each channel 
        /// This is used when the channels that a feature pertains to = "ALL"
        /// </summary>
        /// <param name="devCaps">The devCaps list</param>
        /// <param name="devCapsKey">The devCaps key</param>
        /// <param name="devCapsValue">The devCaps value</param>
        //====================================================================================================================
        internal virtual void AddChannelDevCapsKey(Dictionary<string, string> devCaps,
            string component,
            string devCapsName,
            string configuration,
            string devCapsValue)
        {
            string chCaps;

            int maxChannels = m_maxChannels;

            if (configuration == DevCapConfigurations.DIFF)
                maxChannels /= 2;

            for (int channel = 0; channel < maxChannels; channel++)
            {
                chCaps = component + "{" + channel.ToString() + "}:" + devCapsName;

                if (configuration != "ALL")
                    chCaps += ("/" + configuration);

                devCaps.Add(chCaps, devCapsValue);
            }
        }

        //========================================================================================
        /// <summary>
        /// Virtual method to read in a component's calibration coefficients
        /// </summary>
        //========================================================================================
        protected virtual void GetCalCoefficients()
        {
        }

        //========================================================================================
        /// <summary>
        /// Gets the resolution of the component based on its max count
        /// </summary>
        /// <param name="count">The max count</param>
        /// <returns>The resolution in bits</returns>
        //========================================================================================
        internal virtual int GetResolution(ulong maxCount)
        {
            int resolution = 0;

            switch (maxCount)
            {
                case (0xFF):
                    resolution = 8;
                    break;
                case (0x7FF): 
                    resolution = 11;
                    break;
                case (0xFFF):
                    resolution = 12;
                    break;
                case (0x1FFF):
                    resolution = 13;
                    break;
                    
                case (0x03FFF):
                     resolution = 14;
                     break;
                     
                case (0xFFFF):
                    resolution = 16;
                    break;
                case (0xFFFFF):
                    resolution = 20;
                    break;
                case (0xFFFFFF):
                    resolution = 24;
                    break;
                case (0xFFFFFFFF):
                    resolution = 32;
                    break;
                case (0xFFFFFFFFFFFF):
                    resolution = 48;
                    break;
                //case (0xFFFFFFFFFFFFFFFF):
                //    resolution = 64;
                //    break;
            }

            return resolution;
        }

        //=====================================================================================
        /// <summary>
        /// Calculates the sum of an array
        /// </summary>
        /// <param name="data">The data array</param>
        /// <returns>The sum</returns>
        //=====================================================================================
        protected double GetSum(int[] data)
        {
            double sum = 0;

            foreach (int i in data)
            {
                sum += (double)i;
            }

            return sum;
        }

        //=====================================================================================
        /// <summary>
        /// Calculates the sum of an array
        /// </summary>
        /// <param name="data">The data array</param>
        /// <returns>The sum</returns>
        //=====================================================================================
        protected double GetSum(double[] data)
        {
            double sum = 0;

            foreach (double d in data)
            {
                sum += d;
            }

            return sum;
        }

        //=====================================================================================
        /// <summary>
        /// Calculates the sum of an array
        /// </summary>
        /// <param name="data">The data array</param>
        /// <returns>The sum</returns>
        //=====================================================================================
        protected double GetSqrSum(double[] data)
        {
            double sum = 0;

            foreach (double d in data)
            {
                sum += (d * d);
            }

            return sum;
        }

        //=====================================================================================
        /// <summary>
        /// Calculates the inner product of two arrays
        /// </summary>
        /// <param name="array1">The first array</param>
        /// <param name="array2">The second array</param>
        /// <returns>The inner product</returns>
        //=====================================================================================
        protected double GetInnerProduct(double[] array1, int[] array2)
        {
            if (array1.GetLength(0) != array2.GetLength(0))
            {
                System.Diagnostics.Debug.Assert(array1.GetLength(0) == array2.GetLength(0));
                return 0.0;
            }

            double init = 0.0;

            for (int i = 0; i < array1.Length; i++)
            {
                init += array1[i] * (double)array2[i];
            }

            return init;
        }

        //=====================================================================================
        /// <summary>
        /// Calculates the average value of an array
        /// </summary>
        /// <param name="data">The data array</param>
        /// <returns>The average</returns>
        //=====================================================================================
        protected double GetAverage(double[] data)
        {
            double sum = 0;

            foreach (double d in data)
            {
                sum += d;
            }

            return sum / (double)data.Length;
        }

        //=====================================================================================
        /// <summary>
        /// Converts a 2D array to a 1D array
        /// </summary>
        /// <param name="data">The 2D array</param>
        /// <param name="channel">The channel</param>
        /// <returns>The 1D array</returns>
        //=====================================================================================
        protected double[] GetChannelData(double[,] data, int channel)
        {
            if (channel >= data.GetLength(0))
                System.Diagnostics.Debug.Assert(false, "invalid channel specified");

            int numberOfSamples = data.GetLength(1);

            double[] channelData = new double[numberOfSamples];

            for (int i = 0; i < numberOfSamples; i++)
            {
                channelData[i] = data[channel, i];
            }

            return channelData;
        }

        //=======================================================================================
        /// <summary>
        /// Gets the vrefs for the specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        //=======================================================================================
        protected double HexStringToDouble(string hexString)
        {
            byte[] b = new byte[hexString.Length / 2];

            for (int ii = (hexString.Length - 2), j = 0; ii >= 0; ii -= 2, j++)
            //for (int ii=0, j = 0; ii<hexString.Length; ii+=2, j++)
            {
                b[j] = byte.Parse(hexString.Substring(ii, 2), NumberStyles.HexNumber);
            }
            double d = BitConverter.ToDouble(b, 0);

            return d;
        }

        //=======================================================================================
        /// <summary>
        /// Gets the vrefs for the specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        //=======================================================================================
        protected string DoubleToHexString(double d)
        {
            string s = "0x";

            unsafe
            {
                double* pDouble = &d;
                byte* pByte = (byte*)pDouble;

                for (int i = 7; i >= 0; i--)
                {
                    s += string.Format("{0:X2}", *(pByte + i));
                }
            }

            return s;
        }

#endregion
    }
}
