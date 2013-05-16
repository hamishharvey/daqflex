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
    internal partial class AiComponent : IoComponent
    {
        protected int m_maxCount;
        protected string m_previousChMode = String.Empty;
        protected double m_maxScanThroughput;
        protected double m_maxScanRate;
        protected double m_minScanRate;
        protected double m_maxBurstThroughput;
        protected double m_maxBurstRate;
        protected double m_minBurstRate;
        protected bool m_supportsBurstIO;
        protected int m_queueDepth = 0;
        protected AiChannelMode[] m_channelModes;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal AiComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
            try
            {
                m_daqDevice.SendMessageDirect("?AI");
                m_channelCount = (int)m_daqDevice.DriverInterface.ReadValue();
                m_ranges = new string[m_channelCount];
                m_supportsBurstIO = false;
                m_internalClockOptionMessage = DaqComponents.AISCAN +
                                                    Constants.PROPERTY_SEPARATOR +
                                                        DaqProperties.EXTPACER +
                                                            Constants.EQUAL_SIGN +
                                                                PropertyValues.ENABLE +
                                                                    Constants.VALUE_RESOLVER +
                                                                        PropertyValues.MASTER;
            }
            catch (Exception)
            {
                m_channelCount = 0;
            }
        }

        //=================================================================================================================
        /// <summary>
        /// Virtual method to initialize range information
        /// </summary>
        //=================================================================================================================
        internal virtual void InitializeChannelModes() { }

        //=================================================================================================================
        /// <summary>
        /// Initializes rate parameters
        /// </summary>
        //=================================================================================================================
        internal override void Initialize()
        {
            try
            {
                m_maxScanThroughput = Double.Parse(m_daqDevice.GetDevCapsValue("AISCAN:MAXSCANTHRUPUT", true));
                m_maxScanRate = Double.Parse(m_daqDevice.GetDevCapsValue("AISCAN:MAXSCANRATE", true));
                m_minScanRate = Double.Parse(m_daqDevice.GetDevCapsValue("AISCAN:MINSCANRATE", true));

                string xfrModes = m_daqDevice.GetDevCapsValue("AISCAN:XFRMODES", true);

                if (xfrModes.Contains(DevCapValues.BURSTIO))
                {
                    m_maxBurstThroughput = Double.Parse(m_daqDevice.GetDevCapsValue("AISCAN:MAXBURSTTHRUPUT", true));
                    m_maxBurstRate = Double.Parse(m_daqDevice.GetDevCapsValue("AISCAN:MAXBURSTRATE", true));
                    m_minBurstRate = Double.Parse(m_daqDevice.GetDevCapsValue("AISCAN:MINBURSTRATE", true));
                }

                string response = m_daqDevice.GetDevCapsValue("AISCAN:QUEUELEN", true);

                if (response != string.Empty)
                {
                    m_queueDepth = Int32.Parse(response);
                }

            }
            catch (Exception)
            {
                //System.Diagnostics.Debug.Assert(false, ex.Message);
            }
        }

        //===========================================================================
        /// <summary>
        /// The Ai channel modes
        /// </summary>
        //===========================================================================
        internal AiChannelMode[] ChannelModes
        {
            get { return m_channelModes; }
        }

        //===========================================================================
        /// <summary>
        /// A Dictionary containing the analog input calibration coefficients
        /// The key is 
        /// </summary>
        //===========================================================================
        internal Dictionary<string, CalCoeffs> AiCalCoeffs
        {
            get { return m_calCoeffs; }
        }

        //===========================================================================
        /// <summary>
        /// method to get the analog input ranges supported by this device
        /// </summary>
        /// <returns>The supported ranges</returns>
        //===========================================================================
        protected virtual string GetRanges()
        {
            Dictionary<string, Range>.KeyCollection keys = m_supportedRanges.Keys;

            string[] ranges = new string[keys.Count];
            keys.CopyTo(ranges, 0);

            return String.Join(",", ranges, 0, ranges.Length);
        }

        //====================================================================================
        /// <summary>
        /// Sets the ai channel ranges used for scaling the ai scan data
        /// TODO: make this virtual and implement in device specific classes
        /// </summary>
        //====================================================================================
        protected virtual ErrorCodes SetRanges()
        {
            bool useRangeQueue = false;
            string aiConfiguration = "SE";

            try
            {
                DaqResponse response;

                response = m_daqDevice.SendMessage("@AISCAN:QUEUELEN");
                double elements = response.ToValue();

                if (!Double.IsNaN(elements) && elements > 0)
                {
                    response = m_daqDevice.SendMessage("?AISCAN:QUEUE");

                    if (response.ToString().Contains(PropertyValues.ENABLE))
                        useRangeQueue = true;
                    else
                        useRangeQueue = false;

                    response = m_daqDevice.SendMessage("?AI:CHMODE");

                    if (response.ToString().Contains("DIFF"))
                        aiConfiguration = "DIFF";
                }
            }
            catch (Exception)
            {
                useRangeQueue = false;
            }

            if (useRangeQueue)
            {
                try
                {
                    string response;
                    string rangeValue;

                    // get the size of the queue
                    response = m_daqDevice.SendMessage("?AISCAN:RANGE").ToString();
                    int queueCount = Convert.ToInt32(response.Substring(response.IndexOf("=") + 1));
                    m_activeChannels = new ActiveChannels[queueCount];


                    int channel;
                    int indexOfChannel;
                    int indexOfBrace;

                    int rangeIndex = 0;

                    if (queueCount > 0)
                    {
                        for (int i = 0; i < queueCount; i++)
                        {
                            string rangeQuery = String.Format("?AISCAN:RANGE{0}", MessageTranslator.GetChannelSpecs(i));

                            m_daqDevice.SendMessageDirect(rangeQuery).ToString();
                            response = m_daqDevice.DriverInterface.ReadString();
                            rangeValue = response.Substring(response.IndexOf("=") + 1);
                            rangeValue += (":" + aiConfiguration);

                            indexOfChannel = response.IndexOf(Constants.VALUE_RESOLVER) + 1;
                            indexOfBrace = response.IndexOf(CurlyBraces.RIGHT);
                            channel = Convert.ToInt32(response.Substring(indexOfChannel, indexOfBrace - indexOfChannel));
                            m_activeChannels[rangeIndex].ChannelNumber = channel;
                            m_activeChannels[rangeIndex].UpperLimit = m_supportedRanges[rangeValue].UpperLimit;
                            m_activeChannels[rangeIndex].LowerLimit = m_supportedRanges[rangeValue].LowerLimit;

                            if (m_calCoeffs.Count > 0)
                            {
                                m_activeChannels[rangeIndex].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", channel, rangeValue)].Slope;
                                m_activeChannels[rangeIndex].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", channel, rangeValue)].Offset;
                            }

                            rangeIndex++;
                        }
                    }
                    else
                    {
                        return ErrorCodes.InputQueueIsEmpty;
                    }
                }
                catch (Exception)
                {
                }
            }
            else
            {
                int loChan = m_daqDevice.DriverInterface.CriticalParams.LowAiChannel;
                int hiChan = m_daqDevice.DriverInterface.CriticalParams.HighAiChannel;

                if (loChan <= hiChan)
                {
                    m_activeChannels = new ActiveChannels[hiChan - loChan + 1];

                    int rangeIndex = 0;
                    for (int i = loChan; i <= hiChan; i++)
                    {
                        string rangeQuery = String.Format("?AISCAN:RANGE{0}", MessageTranslator.GetChannelSpecs(i));

                        try
                        {
                            m_daqDevice.SendMessageDirect(rangeQuery).ToString();
                            string response = m_daqDevice.DriverInterface.ReadString();
                            string rangeValue = response.Substring(response.IndexOf("=") + 1);
                            rangeValue += (":" + aiConfiguration);

                            m_activeChannels[rangeIndex].ChannelNumber = i;
                            m_activeChannels[rangeIndex].UpperLimit = m_supportedRanges[rangeValue].UpperLimit;
                            m_activeChannels[rangeIndex].LowerLimit = m_supportedRanges[rangeValue].LowerLimit;

                            if (m_calCoeffs.Count > 0)
                            {
                                m_activeChannels[rangeIndex].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", i, rangeValue)].Slope;
                                m_activeChannels[rangeIndex].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", i, rangeValue)].Offset;
                            }
                        }
                        catch (Exception)
                        {
                        }

                        rangeIndex++;
                    }
                }
            }

            m_daqDevice.DriverInterface.CriticalParams.AiChannelCount = m_activeChannels.Length;
            m_daqDevice.DriverInterface.CriticalParams.InputXferSize =
                            m_daqDevice.DriverInterface.GetOptimalInputBufferSize(m_daqDevice.DriverInterface.CriticalParams.InputScanRate);

            return ErrorCodes.NoErrors;
        }

#region Message Processing and Validation

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

            if (messageType == DaqComponents.AI)
            {
                return PreprocessAiMessage(ref message);
            }
            else if (messageType == DaqComponents.AISCAN)
            {
                return PreprocessAiScanMessage(ref message);
            }
            else if (messageType == DaqComponents.AITRIG)
            {
                return PreprocessAiTrigMessage(ref message);
            }

            System.Diagnostics.Debug.Assert(false, "Invalid component for analog input");

            return ErrorCodes.InvalidMessage;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the Ai message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessAiMessage(ref string message)
        {
            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                ErrorCodes errorCode =  ValidateChannel(ref message);

                if (errorCode != ErrorCodes.NoErrors)
                    return errorCode;
            }

            //// Scaling is handled by the DAQFlex Library
            //// This message is not sent to the device
            //// Message = "AI:SCALE=ENABLE
            if (message.Contains(DaqProperties.SCALE))
                return ProcessScaleMessage(ref message);

            // Calibration is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AISCAN:CAL=ENABLE" or "AI:CAL=ENABLE
            if (message.Contains(DaqProperties.CAL))
                return ProcessCalMessage(ref message);

            // The DAQFlex API stores the range information for scaling data but this message is sent to the device
            // message = "AI{*}:RANGE=*"
            if (message.Contains(DaqProperties.RANGE) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessRangeMessage(ref message);

            // Process "?AI:RANGE/ALL" - THIS IS NOW A DRM
            if (message.Contains(DaqProperties.RANGE) && message.Contains(Constants.QUERY.ToString()) && message.Contains(DaqProperties.RANGE + "/ALL"))
            {
                string ranges = GetRanges();
                m_daqDevice.ApiResponse = new DaqResponse(message + "=" + ranges, double.NaN);
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }

            // This handles the querying of a single AI value.
            // It sets up the scaling and calibration values for the particular channel, channel mode and range
            if (message.Contains(DaqProperties.VALUE) && message.Contains(Constants.QUERY.ToString()))
                return ProcessValueMessage(ref message);

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Checks for AISCAN messages that the DAQFlex Library needs to repsond to rather than the device.
        /// These messages are not passed down to the device
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessAiScanMessage(ref string message)
        {
            // message = "?AISCAN:STATUS
            if (message.Contains(APIMessages.AISCANSTATUS_QUERY))
                return ProcessScanStatusQuery(ref message);

            // The scan count is calculated by the DAQFlex library, so this message is not sent to the device
            // message = "?AISCAN:COUNT
            if (message.Contains(APIMessages.AISCANCOUNT_QUERY))
                return ProcessScanCountQuery(ref message);

            // message = "?AISCAN:INDEX
            if (message.Contains(APIMessages.AISCANINDEX_QUERY))
                return ProcessScanIndexQuery(ref message);

            // The buffer size is maintained by the DAQFlex library, so this message is not sent to the device
            // message = "?AISCAN:BUFSIZE
            if (message.Contains(APIMessages.AISCANBUFSIZE_QUERY))
                return ProcessBufSizeQuery(ref message);

            // The buffer size is maintained by the DAQFlex library, so this message is not sent to the device
            // message = "AISCAN:BUFSIZE=*"
            if (message.Contains(APIMessages.AISCANBUFSIZE))
                return ProcessInputBufferSizeMessage(ref message);

            if (message.Contains(DaqProperties.RANGE) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessRangeMessage(ref message);

            if (message.Contains(DaqProperties.SAMPLES) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessSamplesMessage(ref message);

            // The DAQFlex Library needs to set up the ai ranges for the scan
            // This message is sent to the device
            // message = "AISCAN:RANGE=*"
            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqCommands.START))
            {
                // set the scan type in CriticalParams for use by the driver interface
                m_daqDevice.CriticalParams.ScanType = ScanType.AnalogInput;

                // set up the ranges and active channels
                ErrorCodes errorCode = SetRanges();

                return errorCode;
            }

            // Scaling is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AISCAN:SCALE=ENABLE"
            if (message.Contains(DaqProperties.SCALE))
            {
                if (message.Contains(PropertyValues.ENABLE))
                    m_daqDevice.CriticalParams.ScaleAiData = true;
                else
                    m_daqDevice.CriticalParams.ScaleAiData = false;

                return ProcessScaleMessage(ref message);
            }

            // Calibration is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AISCAN:CAL=ENABLE"
            if (message.Contains(DaqProperties.CAL))
            {
                if (message.Contains(PropertyValues.ENABLE))
                    m_daqDevice.CriticalParams.CalibrateAiData = true;
                else
                    m_daqDevice.CriticalParams.CalibrateAiData = false;

                return ProcessCalMessage(ref message);
            }

            // validate the channel numbers
            if (!message.Contains(Constants.QUERY.ToString()) &&
                (message.Contains(DaqProperties.LOWCHAN) || message.Contains(DaqProperties.HIGHCHAN)))
                return ValidateChannel(ref message);

            // process the transfer mode
            if (message.Contains(DaqProperties.XFERMODE))
                return ProcessXferModeMessage(ref message);

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Checks for AITRIG messages that the DAQFlex Library needs to repsond to rather than the device.
        /// These messages are not passed down to the device
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessAiTrigMessage(ref string message)
        {
            if (message.Contains(DaqProperties.TRIGTYPE))
                return ProcessAiTrigTypeMessage(ref message);

            return ErrorCodes.NoErrors;
        }


        //===========================================================================================
        /// <summary>
        /// Validates the channel number
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ValidateChannel(ref string message)
        {
            DaqResponse response;

            int channel = MessageTranslator.GetChannel(message);

            response = m_daqDevice.GetDeviceCapability("@AI:CHANNELS");

            if (response.ToString() == PropertyValues.NOT_SUPPORTED)
            {
                return ErrorCodes.InvalidMessage;
            }
            else
            {
                if (channel < 0 || channel > (int)response.ToValue() - 1)
                    return ErrorCodes.InvalidAiChannelSpecified;

                if (message.Contains(DaqProperties.LOWCHAN))
                    m_daqDevice.DriverInterface.CriticalParams.LowAiChannel = channel;
                else if (message.Contains(DaqProperties.HIGHCHAN))
                    m_daqDevice.DriverInterface.CriticalParams.HighAiChannel = channel;
            }

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Validates the channel mode and sets dependent properties
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes ValidateChannelMode(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the range message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessRangeMessage(ref string message)
        {
            string rangeValue = MessageTranslator.GetPropertyValue(message);
            string supportedRanges = String.Empty;
            string channels = String.Empty;

            if (message.Contains(DaqComponents.AISCAN))
            {
                try
                {
                    string capsKey;

                    if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
                    {
                        // Message is in the form AISCAN:RANGE{0}=BIP10V or AISCAN:RANGE{0/0}=BIP10V

                        // process a gain queue
                        int channel = MessageTranslator.GetQueueChannel(message);
                        int element = MessageTranslator.GetQueueElement(message);

                        if (channel >= 0)
                        {
                            capsKey = DaqComponents.AI +
                                        Constants.PROPERTY_SEPARATOR +
                                            DevCapNames.CHANNELS;

                            channels = m_daqDevice.GetDevCapsValue(capsKey, true);
                            channels = MessageTranslator.GetReflectionValue(channels);

                            int chCount = 0;
#if WindowsCE
                            try
                            {
                                chCount = Int32.Parse(channels);
                            }
                            catch (Exception)
                            {
                                chCount = 0;
                            }
#else
                            Int32.TryParse(channels, out chCount);
#endif
                            if (channel >= chCount)
                                return ErrorCodes.InvalidAiChannelSpecified;

                            capsKey = DaqComponents.AI +
                                        CurlyBraces.LEFT +
                                            channel.ToString() +
                                                CurlyBraces.RIGHT +
                                                    Constants.PROPERTY_SEPARATOR +
                                                        DevCapNames.RANGES;

                            supportedRanges = m_daqDevice.GetDevCapsValue(capsKey, true);

                            if (!supportedRanges.Contains(rangeValue))
                                return ErrorCodes.InvalidAiRange;
                        }

                        if (element >= 0)
                        {
                            if (element > (m_queueDepth - 1))
                                return ErrorCodes.GainQueueDepthExceeded;
                        }
                    }
                    else
                    {
                        // Messsage is in the form AISCAN:RANGE=BIP10V

                        for (int i = m_daqDevice.CriticalParams.LowAiChannel; i <= m_daqDevice.CriticalParams.HighAiChannel; i++)
                        {
                            capsKey = DaqComponents.AI +
                                            CurlyBraces.LEFT +
                                                i.ToString() +
                                                    CurlyBraces.RIGHT +
                                                        Constants.PROPERTY_SEPARATOR +
                                                            DevCapNames.RANGES;

                            supportedRanges = m_daqDevice.GetDevCapsValue(capsKey, true);

                            if (!supportedRanges.Contains(rangeValue))
                                return ErrorCodes.InvalidAiRange;
                        }
                    }
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "Invalid range");
                    return ErrorCodes.InvalidAiRange;
                }
            }
            else
            {
                // Messsage is in the form AI{0}:RANGE=BIP10V

                int channel = MessageTranslator.GetChannel(message);

                try
                {
                    if (channel >= 0)
                    {
                        string capsKey = DaqComponents.AI +
                                            CurlyBraces.LEFT +
                                                channel.ToString() +
                                                    CurlyBraces.RIGHT +
                                                        Constants.PROPERTY_SEPARATOR +
                                                            DevCapNames.RANGES;

                        supportedRanges = m_daqDevice.GetDevCapsValue(capsKey, true);

                        if (!supportedRanges.Contains(rangeValue))
                            return ErrorCodes.InvalidAiRange;

                        // store the current range message for use in PostProcessData
                        m_ranges[channel] = message;
                    }
                    else
                    {
                        return ErrorCodes.InvalidAiRange;
                    }
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "Invalid range");
                    return ErrorCodes.InvalidAiRange;
                }
            }

            //// if only a single range is supported, the device doesn't accept a range command
            //if (supportedRanges.Contains(DevCapImplementations.FIXED) && !message.Contains(DaqComponents.AISCAN))
            //    m_daqDevice.SendMessageToDevice = false;

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes the SAMPLES message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessSamplesMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            string samples = MessageTranslator.GetPropertyValue(message);
            int sampleCount;

#if WindowsCE
            try
            {
                sampleCount = Int32.Parse(samples);
            }
            catch (Exception)
            {
                sampleCount = 0;
            }
#else
            bool parsed = Int32.TryParse(samples, out sampleCount);

            if (!parsed || sampleCount <0)
                errorCode = ErrorCodes.InvalidPropertyValueSpecified;
#endif

            return errorCode;
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
            int channelNumber = MessageTranslator.GetChannel(message);

            if (channelNumber >= 0 && channelNumber < m_channelCount)
            {
                m_valueUnits = String.Empty;

                if (message.Contains(ValueResolvers.RAW))
                {
                    m_calibrateData = false;
                    m_scaleData = false;
                    m_valueUnits = "/RAW";
                    message = MessageTranslator.RemoveValueResolver(message);
                }
                else if (message.Contains(ValueResolvers.VOLTS))
                {
                    m_calibrateData = true;
                    //m_voltsOnly = true;
                    m_scaleData = true;
                    m_valueUnits = "/VOLTS";
                    message = MessageTranslator.RemoveValueResolver(message);
                }
                else if (message.Contains("?AI{0}:VALUE/"))
                {
                    return ErrorCodes.InvalidMessage;
                }

                m_activeChannels = new ActiveChannels[1];
                string rangeKey = m_ranges[channelNumber].Substring(m_ranges[channelNumber].IndexOf(Constants.EQUAL_SIGN) + 1);

                if (m_channelModes[channelNumber] == AiChannelMode.SingleEnded)
                    rangeKey += ":SE";
                else
                    rangeKey += ":DIFF";

                if (channelNumber < m_channelCount)
                {
                    m_activeChannels[0].ChannelNumber = channelNumber;
                    m_activeChannels[0].UpperLimit = m_supportedRanges[rangeKey].UpperLimit;
                    m_activeChannels[0].LowerLimit = m_supportedRanges[rangeKey].LowerLimit;

                    if (m_calCoeffs.Count > 0)
                    {
                        m_activeChannels[0].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", channelNumber, rangeKey)].Offset;
                        m_activeChannels[0].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", channelNumber, rangeKey)].Slope;
                    }

                    return ErrorCodes.NoErrors;
                }
                else
                {
                    return ErrorCodes.NoErrors;
                }
            }
            else
            {
                // let the device respond with invalid command
                return ErrorCodes.NoErrors;
            }
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan status query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanStatusQuery(ref string message)
        {
            ScanState status = m_daqDevice.DriverInterface.InputScanStatus;

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AISCANSTATUS_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + status.ToString().ToUpper(), Double.NaN);

            m_daqDevice.SendMessageToDevice = false;

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan count query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanCountQuery(ref string message)
        {
            long count = m_daqDevice.DriverInterface.InputScanCount;

            if (m_daqDevice.DriverInterface.CriticalParams.InputSampleMode == SampleMode.Finite)
            {
                int totalFiniteSamplesPerChannel = m_daqDevice.DriverInterface.CriticalParams.InputScanSamples;
                count = Math.Min(totalFiniteSamplesPerChannel, count);
            }

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AISCANCOUNT_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + count.ToString(), count);

            m_daqDevice.SendMessageToDevice = false;

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan index query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanIndexQuery(ref string message)
        {
            long index = m_daqDevice.DriverInterface.InputScanIndex;

            if (m_daqDevice.DriverInterface.CriticalParams.InputSampleMode == SampleMode.Finite)
            {
                int totalFiniteSamples = m_daqDevice.CriticalParams.AiChannelCount * m_daqDevice.DriverInterface.CriticalParams.InputScanSamples;
                index = Math.Min(totalFiniteSamples - m_daqDevice.CriticalParams.AiChannelCount, index);
            }

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AISCANINDEX_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + index.ToString(), index);

            m_daqDevice.SendMessageToDevice = false;

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a buffer size query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessBufSizeQuery(ref string message)
        {
            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AISCANBUFSIZE_QUERY.Remove(0, 1) + 
                                                        Constants.EQUAL_SIGN + 
                                                            m_daqDevice.DriverInterface.InputScanBuffer.Length.ToString(),
                                                                m_daqDevice.DriverInterface.InputScanBuffer.Length);
            m_daqDevice.SendMessageToDevice = false;

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a buffer size message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessInputBufferSizeMessage(ref string message)
        {
            int equalIndex = message.IndexOf(Constants.EQUAL_SIGN);

            if (equalIndex >= 0)
            {
                try
                {
                    int numberOfBytes = Convert.ToInt32(message.Substring(equalIndex + 1));
                    m_daqDevice.DriverInterface.SetInputBufferSize(numberOfBytes);
                    m_daqDevice.ApiResponse = new DaqResponse(message.Substring(0, equalIndex), double.NaN);
                }
                catch (Exception)
                {
                    m_daqDevice.ApiMessageError = ErrorCodes.InvalidMessage;
                }
            }

            m_daqDevice.SendMessageToDevice = false;

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ValidateScanRate()
        {
            double maxRate = double.MaxValue;
            string methodValue;
            int channelCount = m_daqDevice.CriticalParams.AiChannelCount;

            try
            {
                double rate = m_daqDevice.CriticalParams.InputScanRate;

                if (m_daqDevice.CriticalParams.InputTransferMode == TransferMode.BurstIO)
                {
                    methodValue = m_daqDevice.GetDevCapsValue("AISCAN:BURSTRATECALC", true);

                    switch (methodValue)
                    {
                        case ("METHOD1"):
                            maxRate = RateCalcMethod1(m_maxBurstThroughput, channelCount);
                            break;
                        case ("METHOD2"):
                            maxRate = RateCalcMethod2(m_maxBurstThroughput, m_maxBurstRate, channelCount);
                            break;
                    }

                    if (rate < m_minBurstRate || rate > maxRate)
                        return ErrorCodes.InvalidScanRateSpecified;
                }
                else
                {
                    methodValue = m_daqDevice.GetDevCapsValue("AISCAN:SCANRATECALC", true);

                    switch (methodValue)
                    {
                        case ("METHOD1"):
                            maxRate = RateCalcMethod1(m_maxScanThroughput, channelCount);
                            break;
                        case ("METHOD2"):
                            maxRate = RateCalcMethod2(m_maxScanThroughput, m_maxScanRate, channelCount);
                            break;
                    }

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

            if (m_daqDevice.CriticalParams.InputTransferMode == TransferMode.BurstIO)
            {
                int fifoSize = 0;
                bool parsed;

                string devCapsValue = m_daqDevice.GetDevCapsValue("AISCAN:FIFOSIZE", true);

#if WindowsCE
                try
                {
                    fifoSize = Int32.Parse(devCapsValue);
                    parsed = true;
                }
                catch (Exception)
                {
                    parsed = false;
                    errorCode = ErrorCodes.InvalidSampleCountForBurstIo;
                }
#else
                parsed = Int32.TryParse(devCapsValue, out fifoSize);
#endif

                if (!parsed || m_daqDevice.CriticalParams.InputScanSamples == 0 || m_daqDevice.CriticalParams.InputScanSamples > fifoSize)
                    errorCode = ErrorCodes.InvalidSampleCountForBurstIo;
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing the xfer mode message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessXferModeMessage(ref string message)
        {
            if (!ValidateDaqFeature(message, "@AISCAN:XFRMODES"))
                return ErrorCodes.InvalidInputScanXferMode;
            else
                return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Overriden to process the Ai trig message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessAiTrigTypeMessage(ref string message)
        {
            if (!ValidateDaqFeature(message, "@AITRIG:TYPES"))
                return ErrorCodes.InvalidAiTriggerType;
            else
                return ErrorCodes.NoErrors;
        }

#endregion

        //===========================================================================================
        /// <summary>
        /// Scales an analog input value
        /// </summary>
        /// <param name="value">The raw A/D value</param>
        /// <returns>The scaled value</returns>
        //===========================================================================================
        internal override ErrorCodes ScaleData(ref double value)
        {
            double scaledValue = value;

            if (m_scaleData)
                scaledValue = value * ((m_activeChannels[0].UpperLimit - m_activeChannels[0].LowerLimit) / (m_maxCount + 1)) + m_activeChannels[0].LowerLimit;

            value = scaledValue;

            return ErrorCodes.NoErrors;
        }

        //==================================================================================================================
        /// <summary>
        /// Copies scan data from the driver interface's internal read buffer to the destination array
        /// This override is used for 12-bit to 16-bit products
        /// </summary>
        /// <param name="source">The source array (driver interface's internal read buffer)</param>
        /// <param name="destination">The destination array (array return to the application)</param>
        /// <param name="copyIndex">The byte index to start copying from</param>
        /// <param name="samplesToCopy">Number of samples to copy</param>
        //==================================================================================================================
        internal override void CopyScanData(byte[] source, double[,] destination, ref int copyIndex, int samplesToCopy)
        {
            unsafe
            {
                int channelCount = m_activeChannels.Length;
                int destinationLength = destination.GetLength(1);

                ushort value;
                double calibratedValue;

                double scaleOffset = 0.0;
                double scaleMultiplier = 1.0;
                double calOffset = 0.0;
                double calSlope = 1.0;

                try
                {
                    // instruct GC to pin destination's location
                    fixed (double* pFixedDestinationBuffer = destination)
                    {
                        // instruct GC to pin the source's location
                        fixed (byte* pFixedSourceBuffer = source)
                        {
                            // the pointers defined by the fixed key words cannot be incremented
                            // so create local pointers that can be

                            // these are per channel
                            int samplesToCopyOnFirstPass;
                            int samplesToCopyOnSencondPass;

                            int byteRatio = sizeof(ushort) / sizeof(byte);
                            int bytesToCopy = byteRatio * channelCount * samplesToCopy;

                            if (copyIndex + bytesToCopy >= source.Length)
                            {
                                samplesToCopyOnFirstPass = ((source.Length - copyIndex) / byteRatio) / channelCount;
                                samplesToCopyOnSencondPass = samplesToCopy - samplesToCopyOnFirstPass;
                            }
                            else
                            {
                                samplesToCopyOnFirstPass = samplesToCopy;
                                samplesToCopyOnSencondPass = 0;
                            }

                            // note: the source array is interleaved, the destination array is not

                            // pointers to source and destination arrays
                            ushort* pSourceBuffer;
                            double* pDestinationBuffer = pFixedDestinationBuffer;
                            int startIndex = copyIndex;

                            for (int i = 0; i < channelCount; i++)
                            {
                                pSourceBuffer = (ushort*)(pFixedSourceBuffer + startIndex) + i;

                                if (m_calibrateData)
                                {
                                    calOffset = m_activeChannels[i].CalOffset;
                                    calSlope = m_activeChannels[i].CalSlope;
                                }

                                if (m_scaleData)
                                {
                                    scaleOffset = m_activeChannels[i].LowerLimit;
                                    scaleMultiplier = (m_activeChannels[i].UpperLimit - m_activeChannels[i].LowerLimit) / (m_maxCount + 1);
                                }

                                for (int j = 0; j < samplesToCopyOnFirstPass; j++)
                                {
                                    value = *pSourceBuffer;
                                    pSourceBuffer += channelCount;
                                    calibratedValue = calSlope * (double)value + calOffset;
                                    *pDestinationBuffer = scaleMultiplier * calibratedValue + scaleOffset;
                                    pDestinationBuffer++;
                                }

                                // if the scan was stopped short then samples to copy may be less than samples requested
                                // so move the pointer to where the next channel should be located in the destination array
                                if (samplesToCopy < destinationLength)
                                    pDestinationBuffer += destinationLength - samplesToCopy;

                                // point to the next channel
                                if (samplesToCopyOnSencondPass > 0)
                                    pDestinationBuffer += samplesToCopyOnSencondPass;
                            }

                            copyIndex += byteRatio * channelCount * samplesToCopyOnFirstPass;

                            if (copyIndex == source.Length)
                                copyIndex = 0;

                            if (samplesToCopyOnSencondPass > 0)
                            {
                                pDestinationBuffer = pFixedDestinationBuffer + samplesToCopyOnFirstPass;

                                for (int i = 0; i < channelCount; i++)
                                {
                                    pSourceBuffer = (ushort*)(pFixedSourceBuffer) + i;

                                    if (m_calibrateData)
                                    {
                                        calOffset = m_activeChannels[i].CalOffset;
                                        calSlope = m_activeChannels[0].CalSlope;
                                    }

                                    if (m_scaleData)
                                    {
                                        scaleOffset = m_activeChannels[i].LowerLimit;
                                        scaleMultiplier = (m_activeChannels[i].UpperLimit - m_activeChannels[i].LowerLimit) / (m_maxCount + 1);
                                    }

                                    for (int j = 0; j < samplesToCopyOnSencondPass; j++)
                                    {
                                        value = *pSourceBuffer;
                                        pSourceBuffer += channelCount;
                                        calibratedValue = calSlope * (double)value + calOffset;
                                        *pDestinationBuffer = scaleMultiplier * calibratedValue + scaleOffset;
                                        pDestinationBuffer++;
                                    }

                                    // if the scan was stopped short then samples to copy may be less than samples requested
                                    // so move the pointer to where the next channel should be located in the destination array
                                    if (samplesToCopy < destinationLength)
                                        pDestinationBuffer += destinationLength - samplesToCopy;

                                    // point to the next channel
                                    pDestinationBuffer += samplesToCopyOnFirstPass;
                                }

                                // update copy index to where the next copy will start from
                                copyIndex += byteRatio * channelCount * samplesToCopyOnSencondPass;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //string s = e.Message;
                }
            }
        }

        //===============================================================================================================================
        /// <summary>
        /// Copies the most recent input buffer to the unmanaged external read buffer
        /// </summary>
        /// <param name="bulkReadBuffer">The buffer that just received a bulk in transfer</param>
        /// <param name="externalReadBuffer">the external read buffer to copy data to</param>
        /// <param name="readBufferLength">The length of the external read buffer</param>
        /// <param name="bytesToTransfer">The number of bytes to transfer</param>
        //===============================================================================================================================
        internal unsafe virtual ErrorCodes CopyToExternalReadBuffer(byte[] sourceBuffer,
                                                              void* externalReadBuffer,
                                                              int readBufferLength,
                                                              uint bytesToTransfer,
                                                              ref int lastExternalBufferIndex)
        {
            unsafe
            {
                try
                {
                    int dataWidth = m_daqDevice.CriticalParams.AiDataWidth;

                    if (dataWidth <= 16)
                    {
                        int samplesToTransfer = (int)bytesToTransfer / sizeof(ushort);

                        fixed (double* pSlopesFixed = m_daqDevice.CriticalParams.AiSlopes, pOffsetsFixed = m_daqDevice.CriticalParams.AiOffsets)
                        {
                            double* pSlopes = pSlopesFixed;
                            double* pOffsets = pOffsetsFixed;

                            fixed (byte* pBulkReadBufferFixed = sourceBuffer)
                            {
                                ushort* pBulkReadBuffer = (ushort*)pBulkReadBufferFixed;
                                ushort* pExtReadBuffer = (ushort*)((byte*)externalReadBuffer + lastExternalBufferIndex + 1);
                                ushort* extReadBufferEnd = (ushort*)((byte*)externalReadBuffer + (readBufferLength - 1));

                                for (int i = 0; i < samplesToTransfer; i++)
                                {
                                    if (i % m_daqDevice.CriticalParams.AiChannelCount == 0)
                                    {
                                       pSlopes = pSlopesFixed;
                                       pOffsets = pOffsetsFixed;
                                    }

                                    *pExtReadBuffer++ = (ushort)((*pBulkReadBuffer++) * (*pSlopes++) + (*pOffsets++));

                                    lastExternalBufferIndex += sizeof(ushort);

                                    if (pExtReadBuffer > extReadBufferEnd)
                                    {
                                        pExtReadBuffer = (ushort*)externalReadBuffer;
                                        lastExternalBufferIndex = -1;
                                    }
                                }
                            }
                        }
                    }

                    return ErrorCodes.NoErrors;
                }
                catch (Exception)
                {
                    return ErrorCodes.ErrorWritingDataToExternalInputBuffer;
                }
            }
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to process any data after a message is sent to a device
        /// </summary>
        /// <param name="dataType">The type of data (e.g. Ai, Ao, Dio)</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes PostProcessData(string componentType, ref string response, ref double value)
        {
            ErrorCodes result = ErrorCodes.NoErrors;

            if (componentType == DaqComponents.AI)
            {
                value = CalibrateData(m_daqDevice.DriverInterface.CriticalParams.AiChannel, value);
                response = response.Substring(0, response.IndexOf("=") + 1) + value.ToString();

                result = PrescaleData(m_daqDevice.DriverInterface.CriticalParams.AiChannel, ref value);

                if (result == ErrorCodes.NoErrors)
                {
                    result = ScaleData(ref value);

                    if (result == ErrorCodes.NoErrors)
                        response = response.Substring(0, response.IndexOf("=") + 1) + value.ToString();
                    else
                        response = GetPreprocessDataErrorResponse(result, response);
                }
                else
                {
                    response = GetPreprocessDataErrorResponse(result, response);
                }
            }

            return result;
        }

        //========================================================================================
        /// <summary>
        /// Virtual method to preprocess Ai data such as checkinf for temp overrange values
        /// before scaling the raw data
        /// </summary>
        /// <param name="channel">The channel number</param>
        /// <param name="value">The raw data value</param>
        /// <returns>Teh Error code</returns>
        //========================================================================================
        internal virtual ErrorCodes PrescaleData(int channel, ref double value)
        {
            return ErrorCodes.NoErrors;
        }
    }
}
