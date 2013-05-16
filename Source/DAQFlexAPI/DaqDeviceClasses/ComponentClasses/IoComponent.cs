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
    internal class IoComponent
    {
        protected DaqDevice m_daqDevice;

        protected Dictionary<string, CalCoeffs> m_calCoeffs = new Dictionary<string, CalCoeffs>();
        protected Dictionary<string, Range> m_supportedRanges = new Dictionary<string, Range>();
        protected ActiveChannels[] m_activeChannels;
        protected bool m_calibrateData = true;
        protected bool m_scaleData = false;

        protected string[] m_ranges;
        protected int m_channelCount;
        //protected bool m_isAiData = false;
        //protected bool m_isAoData = false;
        //protected int m_maxCount;
        protected int m_dataWidth;
        protected int m_maxChannels;
        protected bool m_voltsOnly;
        protected double m_maxThroughput;
        protected string m_valueUnits = String.Empty;
        protected string m_internalClockOptionMessage;

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
        /// Virtual method to get supported messages
        /// </summary>
        /// <returns>A list of supported messages</returns>
        //====================================================================================
        internal virtual List<string> GetMessages(string daqComponent)
        {
            return null;
        }

        //====================================================================================
        /// <summary>
        /// The list of supported ranges
        /// </summary>
        //====================================================================================
        internal Dictionary<string, Range> SupportedRanges
        {
            get { return m_supportedRanges; }
        }

        internal string DeferredClockOptionMessage { get; set; }

        //====================================================================================
        /// <summary>
        /// Virutal method to set the clock source options
        /// </summary>
        /// <param name="clockSource">The clock source</param>
        //====================================================================================
        internal virtual ErrorCodes SendDeferredClockMessage(ClockSource clockSource)
        {
            return ErrorCodes.NoErrors;
        }

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
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);
                }
                else if (message.Contains(PropertyValues.DISABLE))
                {
                    m_scaleData = false;
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);
                }
                else
                {
                    return ErrorCodes.InvalidMessage;
                }

                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
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
        internal virtual ErrorCodes ProcessValueMessage(ref string message)
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
            // The CAL setting is applied to all channels
            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                return ErrorCodes.InvalidMessage;
            }
            if (message[0] == Constants.QUERY)
            {
                m_daqDevice.ApiResponse = new DaqResponse(message.Remove(0, 1) + "=" + (m_calibrateData ? PropertyValues.ENABLE : PropertyValues.DISABLE).ToString(), double.NaN);
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
            else
            {
                if (message.Contains(PropertyValues.ENABLE))
                {
                    m_calibrateData = true;
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);
                }
                else if (message.Contains(PropertyValues.DISABLE))
                {
                    m_calibrateData = false;
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);
                }
                else
                {
                    return ErrorCodes.InvalidMessage;
                }

                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
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
        internal virtual ErrorCodes ProcessXferModeMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing the Ai trig message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessAiTrigTypeMessage(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for validating 
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual bool ValidateDaqFeature(string message, string feature)
        {
            int equalIndex = message.IndexOf(Constants.EQUAL_SIGN);

            if (equalIndex >= 0)
            {
                try
                {

                    DaqResponse response = m_daqDevice.GetDeviceCapability(feature);
                    string featureValue = MessageTranslator.GetPropertyValue(message);

                    if (!response.ToString().Contains(featureValue))
                        return false;

                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        //====================================================================================
        /// <summary>
        /// Checks the conditions against the transfer mode being set
        /// </summary>
        /// <param name="message">the device message</param>
        /// <returns>The error code</returns>
        //====================================================================================
        internal virtual ErrorCodes CheckInputTransferModeConditions(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Max rate calculation - Method1
        /// </summary>
        /// <param name="maxThroughput">The max throughput</param>
        /// <param name="channelCount">the channel count</param>
        /// <returns>The max rate</returns>
        //====================================================================================
        internal double RateCalcMethod1(double maxThroughput, int channelCount)
        {
            return maxThroughput / channelCount;
        }

        //====================================================================================
        /// <summary>
        /// Max rate calculation - Method2
        /// </summary>
        /// <param name="maxThroughput">The max throughput</param>
        /// <param name="maxRate">The max rate</param>
        /// <param name="channelCount">the channel count</param>
        /// <returns>The max rate</returns>
        //====================================================================================
        internal double RateCalcMethod2(double maxThroughput, double maxRate, int channelCount)
        {
            return Math.Min(maxRate, maxThroughput / channelCount);
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
        /// Virtual method for scaling single-point I/O data
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The error code</returns>
        //====================================================================================
        internal virtual ErrorCodes ScaleData(ref double value)
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

#region Feature related methods

        internal virtual int GetMaxChannels()
        {
            return 0;
        }

        //====================================================
        /// <summary>
        /// Gets the maximum hardware paced rate
        /// </summary>
        /// <returns>Max scan rate</returns>
        //====================================================
        internal virtual double GetMaxScanRate()
        {
            return 1000.0;
        }

        //====================================================
        /// <summary>
        /// Gets the minimum hardware paced rate
        /// </summary>
        /// <returns>Min scan rate</returns>
        //====================================================
        internal virtual double GetMinScanRate()
        {
            return 1.0;
        }

        //====================================================
        /// <summary>
        /// Gets the maximum software paced rate
        /// </summary>
        /// <returns>Max rate</returns>
        //====================================================
        internal virtual double GetMaxRate()
        {
            return 100.0;
        }

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

            for (int channel = 0; channel < m_maxChannels; channel++)
            {
                chCaps = component + "{" + channel.ToString() + "}:" + devCapsName;
                devCaps.Add(chCaps, devCapsValue);
            }
        }

        //========================================================================================
        /// <summary>
        /// Gets the resolution of the component based on its max count
        /// </summary>
        /// <param name="count">The max count</param>
        /// <returns>The resolution in bits</returns>
        //========================================================================================
        internal virtual ulong GetResolution(ulong maxCount)
        {
            ulong resolution = 0;

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
                case (0xFFFF):
                    resolution = 16;
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

#endregion
    }
}
