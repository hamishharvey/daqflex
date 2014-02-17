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
using System.Reflection;
using System.Threading;

namespace MeasurementComputing.DAQFlex
{
    class AoComponent : IoComponent
    {
        protected const string m_aoScanReset = "AOSCAN:RESET";

        protected int m_maxCount = 0;
        protected bool m_valueSentCalibrated = true;
        internal bool m_aoScanSupported = true;
        protected Thread m_calProcessThread;
        private int m_calThreadId = 0;
        protected string m_calStatus = PropertyValues.IDLE;
        protected double[,] m_preStartBuffer;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        public AoComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
            m_calibrateData = true;
        }

        protected object calStatusLock = new Object();

        //===========================================================================
        /// <summary>
        /// Value for the Ai cal status
        /// </summary>
        //===========================================================================
        internal string CalStatus
        {
            get
            {
                lock (calStatusLock)
                {
                    return m_calStatus;
                }
            }

            set
            {
                lock (calStatusLock)
                {
                    m_calStatus = value;
                }
            }
        }

        protected object calThreadIdLock = new Object();

        //===========================================================================
        /// <summary>
        /// This is the managed thread id that the self cal was started on
        /// </summary>
        //===========================================================================
        internal int CalThreadId
        {
            get
            {
                lock (calThreadIdLock)
                {
                    return m_calThreadId;
                }
            }

            set
            {
                lock (calThreadIdLock)
                {
                    m_calThreadId = value;
                }
            }
        }

        //=========================================================================================================================
        /// <summary>
        /// Overriden to initialize this IoComponent
        /// </summary>
        //=========================================================================================================================
        internal override void Initialize()
        {
            try
            {
                base.Initialize();

                // Get the D/A max count 
                m_maxCount = (int)m_daqDevice.SendMessage("@AO:MAXCOUNT").ToValue();

                // set the data width (in bits) based on the max count
                m_dataWidth = GetResolution((ulong)m_maxCount);

                double xferSize = m_daqDevice.SendMessage("@AOSCAN:XFRSIZE").ToValue();

                // set the xfer size in critical params
                if (!Double.IsNaN(xferSize))
                    m_daqDevice.CriticalParams.DataOutXferSize = (int)xferSize;
                else
                    m_daqDevice.CriticalParams.DataOutXferSize = (int)Math.Ceiling((double)m_dataWidth / (double)Constants.BITS_PER_BYTE);

                // set the data out xfer size in bytes
                //m_daqDevice.CriticalParams.DataOutXferSize = m_dataWidth / Constants.BITS_PER_BYTE;

                // get the number of channels
                m_channelCount = (int)m_daqDevice.SendMessage("@AO:CHANNELS").ToValue();
                m_ranges = new string[m_maxChannels];

                // set the calibrate data flag if factory cal is supported
                string facCal = m_daqDevice.GetDevCapsString("AO:FACCAL", false);
                if (facCal.Contains(PropertyValues.SUPPORTED)                  
                    && !facCal.Contains(PropertyValues.NOT_SUPPORTED))
                    m_calibrateData = m_calibrateDataClone = true;
                else
                    m_calibrateData = m_calibrateDataClone = false;

                // intialize the ranges
                InitializeRanges();

                // set the STALL option if output scan is supported
                if (m_aoScanSupported)
                {
                    // get the min/max input scan rates
                    m_maxScanThroughput = m_daqDevice.SendMessage("@AOSCAN:MAXSCANTHRUPUT").ToValue();
                    m_maxScanRate = m_daqDevice.SendMessage("@AOSCAN:MAXSCANRATE").ToValue();
                    m_minScanRate = m_daqDevice.SendMessage("@AOSCAN:MINSCANRATE").ToValue();

                    // enable the stall option for detection of underruns
                    m_daqDevice.SendMessage("AOSCAN:STALL=ENABLE");
                }

                // read the cal coeffs from the device's eeprom
                GetCalCoefficients();

                // set default critical params
                SetDefaultCriticalParams(m_deviceInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to set the default critical params
        /// </summary>
        //===========================================================================================
        internal override void SetDefaultCriticalParams(DeviceInfo deviceInfo)
        {
            string msg;

            m_daqDevice.CriticalParams.AoDataWidth = m_dataWidth;
            m_daqDevice.CriticalParams.OutputPacketSize = deviceInfo.MaxPacketSize;
            int fifofSize;

            PlatformParser.TryParse(m_daqDevice.GetDevCapsString("AOSCAN:FIFOSIZE", true), out fifofSize);
            m_daqDevice.CriticalParams.OutputFifoSize = fifofSize;
            m_daqDevice.CriticalParams.CalibrateAoData = true;

            if (m_aoScanSupported)
            {
                // reset critical params
                msg = Messages.AOSCAN_LOWCHAN_QUERY;
                m_daqDevice.CriticalParams.LowAoChannel = (int)m_daqDevice.SendMessage(msg).ToValue();
                msg = Messages.AOSCAN_HIGHCHAN_QUERY;
                m_daqDevice.CriticalParams.HighAoChannel = (int)m_daqDevice.SendMessage(msg).ToValue();
                msg = Messages.AOSCAN_RATE_QUERY;
                m_daqDevice.CriticalParams.OutputScanRate = (int)m_daqDevice.SendMessage(msg).ToValue();
                msg = Messages.AOSCAN_SAMPLES_QUERY;
                m_daqDevice.CriticalParams.OutputScanSamples = (int)m_daqDevice.SendMessage(msg).ToValue();

                msg = Messages.AOSCAN_LOWCHAN;
                msg = Messages.InsertValue(msg, m_daqDevice.CriticalParams.LowAoChannel);
                m_daqDevice.SendMessage(msg);
                msg = Messages.AOSCAN_HIGHCHAN;
                msg = Messages.InsertValue(msg, m_daqDevice.CriticalParams.HighAoChannel);
                m_daqDevice.SendMessage(msg);
                msg = Messages.AOSCAN_RATE;
                msg = Messages.InsertValue(msg, m_daqDevice.CriticalParams.OutputScanRate);
                m_daqDevice.SendMessage(msg);
                msg = Messages.AOSCAN_SAMPLES;
                msg = Messages.InsertValue(msg, m_daqDevice.CriticalParams.OutputScanSamples);
                m_daqDevice.SendMessage(msg);
            }

            SetRanges();
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

            string supportedRanges = m_daqDevice.GetDevCapsString("AO{0}:RANGES", false);
            string facCal = m_daqDevice.GetDevCapsString("AO:FACCAL", false);
            string reg = m_daqDevice.GetDevCapsString("AO{0}:REG", false);
            string extPacer = m_daqDevice.GetDevCapsString("AOSCAN:EXTPACER", false);

            if (daqComponent == DaqComponents.AO)
            {
                messages.Add("?AO");

                messages.Add("AO:SCALE=*");
                messages.Add("AO{*}:VALUE=*");

                if (facCal.Contains(PropertyValues.SUPPORTED)
                  && !facCal.Contains(PropertyValues.NOT_SUPPORTED))
                {
                    messages.Add("AO:CAL=*");
                    messages.Add("?AO:CAL");
                }

                if (supportedRanges.Contains(DevCapImplementations.PROG))
                    messages.Add("AO{*}:RANGE=*");

                messages.Add("?AO:SCALE");
                messages.Add("?AO{*}:RANGE");

                if (facCal.Contains(PropertyValues.SUPPORTED)
                  && !facCal.Contains(PropertyValues.NOT_SUPPORTED))
                {
                    messages.Add("?AO{*}:SLOPE");
                    messages.Add("?AO{*}:OFFSET");
                }

                messages.Add("?AO:RES");

                if (reg.Contains(DevCapImplementations.PROG))
                {
                    messages.Add("?AO{*}:REG");
                    messages.Add("AO{*}:REG=*");
                    messages.Add("AO:UPDATE");
                }
            }
            else if (daqComponent == DaqComponents.AOSCAN && m_aoScanSupported)
            {
                messages.Add("AOSCAN:LOWCHAN=*");
                messages.Add("AOSCAN:HIGHCHAN=*");
                messages.Add("AOSCAN:RATE=*");
                messages.Add("AOSCAN:SAMPLES=*");
                messages.Add("AOSCAN:SCALE=*");
                messages.Add("AOSCAN:BUFSIZE=*");
                messages.Add("AOSCAN:START");
                messages.Add("AOSCAN:STOP");
                messages.Add("AOSCAN:RESET");

                messages.Add("?AOSCAN:LOWCHAN");
                messages.Add("?AOSCAN:HIGHCHAN");
                messages.Add("?AOSCAN:RATE");
                messages.Add("?AOSCAN:SAMPLES");
                messages.Add("?AOSCAN:SCALE");
                messages.Add("?AOSCAN:BUFSIZE");
                messages.Add("?AOSCAN:STATUS");
                messages.Add("?AOSCAN:COUNT");
                messages.Add("?AOSCAN:INDEX");


                if (extPacer.Contains(DevCapImplementations.PROG))
                {
                    messages.Add("?AOSCAN:EXTPACER");
                    messages.Add("AOSCAN:EXTPACER=*");
                }

                if (facCal.Contains(PropertyValues.SUPPORTED)
                  && !facCal.Contains(PropertyValues.NOT_SUPPORTED))
                {
                    messages.Add("AOSCAN:CAL=*");
                    messages.Add("?AOSCAN:CAL");
                }
            }

            return messages;
        }

        //===========================================================================================
        /// <summary>
        /// Virutal method to validate message parameters
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <returns>An error Code</returns>
        //===========================================================================================
        internal override ErrorCodes PreprocessMessage(ref string message, string messageType)
        {
            ErrorCodes errorCode = base.PreprocessMessage(ref message, messageType);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            if (messageType == DaqComponents.AO)
            {
                return PreprocessAoMessage(ref message);
            }
            else if (messageType == DaqComponents.AOSCAN)
            {
                return PreprocessAoScanMessage(ref message);
            }
            else if (messageType == DaqComponents.AOCAL)
            {
                return PreprocessSelfCalMessage(ref message);
            }


            System.Diagnostics.Debug.Assert(false, "Invalid component for analog output");

            return ErrorCodes.InvalidMessage;
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
            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the Ao message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessAoMessage(ref string message)
        {
            // Validate the channel number
            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                ErrorCodes errorCode = ValidateChannel(ref message);

                if (errorCode != ErrorCodes.NoErrors)
                    return errorCode;
            }

            // This handles the setting of a single AO value.
            // It sets up the scaling and calibration values for the particular channel and range
            if (message.Contains(DaqProperties.VALUE) && message.Contains(Constants.QUERY.ToString()))
                return ProcessValueGetMessage(ref message);

            if (message.Contains(DaqProperties.VALUE) && !message.Contains(Constants.QUERY.ToString()))
                return ProcessValueSetMessage(ref message);

            if (message.Contains(DaqProperties.SCALE))
                return ProcessScaleMessage(ref message);

            if (message.Contains(DaqProperties.CAL))
                return ProcessCalMessage(ref message);

            if (message.Contains(DaqProperties.SLOPE) || message.Contains(DaqProperties.OFFSET))
                return ProcessSlopeOffsetMessage(ref message);

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the Ao message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessAoScanMessage(ref string message)
        {
            // if there's a pending output scan error reset it and return the error code
            if (m_daqDevice.PendingOutputScanError != ErrorCodes.NoErrors)
            {
                ErrorCodes errorCode = m_daqDevice.PendingOutputScanError;
                m_daqDevice.PendingOutputScanError = ErrorCodes.NoErrors;
                return errorCode;
            }

            // Send the AOSCAN:RESET command before sending the START command
            if (message.Contains(DaqCommands.START))
            {
                if (m_daqDevice.DriverInterface.OutputScanState != ScanState.Running)
                    m_daqDevice.SendMessageDirect(m_aoScanReset);
            }

            // Scaling is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AISCAN:SCALE=ENABLE"
            if (message.Contains(DaqProperties.SCALE))
            {
                if (message.Contains(PropertyValues.ENABLE))
                    m_daqDevice.CriticalParams.ScaleAoData = true;
                else
                    m_daqDevice.CriticalParams.ScaleAoData = false;

                return ProcessScaleMessage(ref message);
            }

            if (message.Contains(DaqProperties.CAL) && !message.Contains(Constants.QUERY.ToString()))
            {
                if (message.Contains(PropertyValues.ENABLE))
                    m_daqDevice.CriticalParams.CalibrateAoData = true;
                else
                    m_daqDevice.CriticalParams.CalibrateAoData = false;

                return ProcessCalMessage(ref message);
            }

            if (message.Contains(DaqProperties.RANGE))
            {
                // for devices that support programmable range build a list of ranges
                // for use by CopyScanData.
            }

            // The DAQFlex Library needs to set up the ao ranges for the scan
            // This message is sent to the device
            if (!message.Contains(Constants.QUERY.ToString()) && 
                    (message.Contains(DaqProperties.RANGE) ||
                        message.Contains(DaqProperties.LOWCHAN) ||
                            message.Contains(DaqProperties.HIGHCHAN)))
            {
                // set the scan type in CriticalParams for use by the driver interface
                m_daqDevice.CriticalParams.ScanType = ScanType.AnalogOutput;

                if (message.Contains(DaqProperties.LOWCHAN))
                {
                    // this needs to be set before calling SetRanges()
                    int lowChan = MessageTranslator.GetChannel(message);
                    m_daqDevice.CriticalParams.LowAoChannel = lowChan;
                }

                if (message.Contains(DaqProperties.HIGHCHAN))
                {
                    // this needs to be set before calling SetRanges()
                    int highChan = MessageTranslator.GetChannel(message);
                    m_daqDevice.CriticalParams.HighAoChannel = highChan;
                }

                // set up the ranges and active channels
                SetRanges();
            }

            if (message.Contains(DaqProperties.RATE) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessScanRate(ref message);

            // message = "?AOSCAN:STATUS
            if (message.Contains(APIMessages.AOSCANSTATUS_QUERY))
                return ProcessScanStatusQuery(ref message);

            // The scan count is calculated by the DAQFlex library, so this message is not sent to the device
            // message = "?AOSCAN:COUNT
            if (message.Contains(APIMessages.AOSCANCOUNT_QUERY))
                return ProcessScanCountQuery(ref message);

            // message = "?AOSCAN:INDEX
            if (message.Contains(APIMessages.AOSCANINDEX_QUERY))
                return ProcessScanIndexQuery(ref message);

            // The buffer size is maintained by the DAQFlex library, so this message is not sent to the device
            // message = "?AOSCAN:BUFSIZE
            if (message.Contains(APIMessages.AOSCANBUFSIZE_QUERY))
                return ProcessBufSizeQuery(ref message);

            // The buffer size is maintained by the DAQFlex library, so this message is not sent to the device
            // message = "AOSCAN:BUFSIZE=*"
            if (message.Contains(APIMessages.AOSCANBUFSIZE))
                return ProcessOutputBufferSizeMessage(ref message);

            // validate the channel numbers and set up 
            if (message.Contains(Constants.EQUAL_SIGN) &&
                (message.Contains(DaqProperties.LOWCHAN) || message.Contains(DaqProperties.HIGHCHAN)))
                return ValidateChannel(ref message);

            // process ext pacer
            if (message[0] != Constants.QUERY && message.Contains(DaqProperties.EXTPACER))
                return PreprocessExtPacer(ref message);

            // Calibration is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AISCAN:CAL=ENABLE"
            if (message.Contains(DaqProperties.CAL))
            {
                if (message.Contains(PropertyValues.ENABLE))
                    m_daqDevice.CriticalParams.CalibrateAoData = true;
                else
                    m_daqDevice.CriticalParams.CalibrateAoData = false;

                return ProcessCalMessage(ref message);
            }

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Overriden to check for ext pacer support
        /// </summary>
        /// <param name="message">The device message</param>
        /// <returns>An error code</returns>
        //====================================================================================
        internal override ErrorCodes PreprocessExtPacer(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // call base method to validate values
            errorCode = base.PreprocessExtPacer(ref message);

            // if no errors, then set critical params
            if (errorCode == ErrorCodes.NoErrors)
            {
                if (message.Contains(PropertyValues.DISABLE))
                    m_daqDevice.CriticalParams.AoExtPacer = false;
                else
                    m_daqDevice.CriticalParams.AoExtPacer = true;
            }

            return errorCode;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the Ao message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessSelfCalMessage(ref string message)
        {
            // check for the cal status message
            if (message.Contains(DaqComponents.AOCAL) && message.Contains(DaqProperties.STATUS))
            {
                double numericResponse;
                string status = String.Format("{0}:{1}={2}", DaqComponents.AOCAL, DaqProperties.STATUS, CalStatus);
                string percentComplete = status.Substring(status.IndexOf(Constants.VALUE_RESOLVER) + 1);
                PlatformParser.TryParse(percentComplete, out numericResponse);
                m_daqDevice.ApiResponse = new DaqResponse(status, numericResponse);
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }

            if (message.Contains(DaqCommands.START))
            {
                // set the status to running here because the cal runs on a separate thread
                m_daqDevice.SendMessageToDevice = false;
                CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, 0);
                m_daqDevice.ApiResponse = new DaqResponse(message, double.NaN);
                return StartCal();
            }

            if (message.Contains(DaqProperties.SLOPE) && message.Contains("HEX") && message.Contains(Constants.EQUAL_SIGN))
            {
                PreprocessCalSlopeMessage(ref message);
            }

            if (message.Contains(DaqProperties.OFFSET) && message.Contains("HEX") && message.Contains(Constants.EQUAL_SIGN))
            {
                PreprocessCalOffsetMessage(ref message);
            }

            // add cal status

            return ErrorCodes.NoErrors;
        }

        //==============================================================================================
        /// <summary>
        /// Virtual method to set up ranges and active channels for an output scan
        /// </summary>
        //==============================================================================================
        protected virtual void SetRanges()
        {
            int loChan = m_daqDevice.DriverInterface.CriticalParams.LowAoChannel;
            int hiChan = m_daqDevice.DriverInterface.CriticalParams.HighAoChannel;

            if (loChan <= hiChan)
            {
                m_activeChannels = new ActiveChannels[hiChan - loChan + 1];

                int rangeIndex = 0;
                for (int i = loChan; i <= hiChan; i++)
                {
                    string rangeQuery = String.Format("?AOSCAN:RANGE{0}", MessageTranslator.GetChannelSpecs(i));

                    try
                    {
                        string rangeValue;

                        string response = m_daqDevice.SendMessage("@AO:RANGES").ToString();
                        if (response.Contains("PROG"))
                        {
                            m_daqDevice.SendMessageDirect(rangeQuery).ToString();
                            response = m_daqDevice.DriverInterface.ReadStringDirect();
                            rangeValue = response.Substring(response.IndexOf("=") + 1);
                        }
                        else
                        {
                            rangeValue = response.Substring(response.IndexOf("%") + 1);
                        }
                        m_activeChannels[rangeIndex].ChannelNumber = i;
                        m_activeChannels[rangeIndex].UpperLimit = m_supportedRanges[rangeValue].UpperLimit;
                        m_activeChannels[rangeIndex].LowerLimit = m_supportedRanges[rangeValue].LowerLimit;

                        if (m_calCoeffs.Count > 0)
                        {
                            m_activeChannels[rangeIndex].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", i, rangeValue)].Slope;
                            m_activeChannels[rangeIndex].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", i, rangeValue)].Offset;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Assert(false, ex.Message);
                    }

                    rangeIndex++;
                }
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
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_daqDevice.SendMessageToDevice = false;

            ScanState status = m_daqDevice.DriverInterface.OutputScanState;

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AOSCANSTATUS_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + status.ToString().ToUpper(), Double.NaN);

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan count query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanCountQuery(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_daqDevice.SendMessageToDevice = false;

            ulong count = m_daqDevice.DriverInterface.OutputScanCount;

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AOSCANCOUNT_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + count.ToString(), count);

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan index query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanIndexQuery(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_daqDevice.SendMessageToDevice = false;

            long index = m_daqDevice.DriverInterface.OutputScanIndex;

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AOSCANINDEX_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + index.ToString(), index);

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a buffer size query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessBufSizeQuery(ref string message)
        {
            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AOSCANBUFSIZE_QUERY.Remove(0, 1) +
                                                        Constants.EQUAL_SIGN +
                                                            m_daqDevice.DriverInterface.OutputScanBuffer.Length.ToString(),
                                                                m_daqDevice.DriverInterface.OutputScanBuffer.Length);
            m_daqDevice.SendMessageToDevice = false;

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the cal message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessCalMessage(ref string message)
        {
            if (message.Contains(Messages.AI_ADCAL_START) || message.Contains(Messages.AI_ADCAL_STATUS_QUERY))
                return ErrorCodes.NoErrors;

            if (m_daqDevice.GetDevCapsString("AO:FACCAL", false).Contains(PropertyValues.NOT_SUPPORTED))
                return ErrorCodes.InvalidMessage;

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
                    m_calibrateData = m_calibrateDataClone = true;
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);
                }
                else if (message.Contains(PropertyValues.DISABLE))
                {
                    m_calibrateData = m_calibrateDataClone = false;
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
        /// Validates the cal message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessSlopeOffsetMessage(ref string message)
        {
            string response = m_daqDevice.GetDevCapsString("AO:FACCAL", false);

            if (response.Contains(PropertyValues.NOT_SUPPORTED))
                return ErrorCodes.InvalidMessage;

            else return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan rate
        /// This simply checks the rate against the max rate of the device without taking
        /// into consideration the number of channels. This is validated again when Start is
        /// called.
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanRate(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            try
            {
                double rate;
                PlatformParser.TryParse(MessageTranslator.GetPropertyValue(message), out rate);

                if (rate > m_maxScanRate || rate < m_minScanRate)
                    errorCode = ErrorCodes.InvalidScanRateSpecified;
            }
            catch (Exception)
            {
                errorCode = ErrorCodes.InvalidScanRateSpecified;
            }

            return errorCode;
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

            response = m_daqDevice.GetDeviceCapability("@AO:CHANNELS");

            if (response.ToString() == PropertyValues.NOT_SUPPORTED)
            {
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.InvalidMessage;
            }
            else
            {
                if (channel < 0 || channel > (int)response.ToValue() - 1)
                {
                    m_daqDevice.SendMessageToDevice = false;
                    return ErrorCodes.InvalidAoChannelSpecified;
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
        internal override ErrorCodes ProcessValueSetMessage(ref string message)
        {
            int channelNumber = MessageTranslator.GetChannel(message);

            if (channelNumber >= 0 && channelNumber < m_channelCount)
            {
                string value = String.Empty; 

                m_valueUnits = String.Empty;

                if (message.Contains(ValueResolvers.RAW))
                {
                    // set the clones for restoring original flags after SendMessage is complete
                    m_calibrateDataClone = m_calibrateData;
                    m_scaleDataClone = m_scaleData;

                    // set original flags
                    m_calibrateData = false;
                    m_scaleData = false;
                    m_valueUnits = Constants.VALUE_RESOLVER + ValueResolvers.RAW;
                    value = MessageTranslator.GetPropertyValue(message);
                    message = MessageTranslator.RemoveValueResolver(message);
                    message += (Constants.EQUAL_SIGN + value);
                }
                else if (message.Contains(ValueResolvers.VOLTS))
                {

                    // set the clones for restoring original flags after SendMessage is complete
                    m_calibrateDataClone = m_calibrateData;
                    m_scaleDataClone = m_scaleData;

                    // set original flags
                    if (m_daqDevice.GetDevCapsString("AO:FACCAL", false).Contains(DevCapValues.SUPPORTED))
                        m_calibrateData = true;

                    m_scaleData = true;
                    m_valueUnits = Constants.VALUE_RESOLVER + ValueResolvers.VOLTS;
                    value = MessageTranslator.GetPropertyValue(message);
                    message = MessageTranslator.RemoveValueResolver(message);
                    message += (Constants.EQUAL_SIGN + value);
                }
                else if (message.Contains("AO{0}:VALUE/"))
                {
                    return ErrorCodes.InvalidMessage;
                }

                m_activeChannels = new ActiveChannels[1];
                string rangeKey = m_ranges[channelNumber].Substring(m_ranges[channelNumber].IndexOf(Constants.EQUAL_SIGN) + 1);
                m_activeChannels[0].ChannelNumber = channelNumber;
                m_activeChannels[0].UpperLimit = m_supportedRanges[rangeKey].UpperLimit;
                m_activeChannels[0].LowerLimit = m_supportedRanges[rangeKey].LowerLimit;

                if (m_calCoeffs.Count > 0)
                {
                    m_activeChannels[0].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", channelNumber, rangeKey)].Offset;
                    m_activeChannels[0].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", channelNumber, rangeKey)].Slope;
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
        internal override ErrorCodes ProcessValueGetMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int channelNumber = MessageTranslator.GetChannel(message);

            if (channelNumber >= 0 && channelNumber < m_channelCount)
            {
                m_valueUnits = String.Empty;

                if (message.Contains(ValueResolvers.RAW))
                {
                    // set the clones for restoring original flags after SendMessage is complete
                    m_calibrateDataClone = m_calibrateData;
                    m_scaleDataClone = m_scaleData;

                    // set original flags
                    m_calibrateData = false;
                    m_scaleData = false;
                    m_valueUnits = Constants.VALUE_RESOLVER + ValueResolvers.RAW;
                    message = MessageTranslator.RemoveValueResolver(message);
                }
                else if (message.Contains(ValueResolvers.VOLTS))
                {
                    // set the clones for restoring original flags after SendMessage is complete
                    m_calibrateDataClone = m_calibrateData;
                    m_scaleDataClone = m_scaleData;

                    // set original flags
                    if (m_daqDevice.GetDevCapsString("AO:FACCAL", false).Contains(DevCapValues.SUPPORTED))
                        m_calibrateData = true;

                    m_scaleData = true;
                    m_valueUnits = Constants.VALUE_RESOLVER + ValueResolvers.VOLTS;
                    message = MessageTranslator.RemoveValueResolver(message);
                }
                else if (message.Contains("?AO{0}:VALUE/"))
                {
                    errorCode = ErrorCodes.InvalidMessage;
                }
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a buffer size message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessOutputBufferSizeMessage(ref string message)
        {
            int equalIndex = message.IndexOf(Constants.EQUAL_SIGN);

            m_daqDevice.SendMessageToDevice = false;

            if (equalIndex >= 0)
            {
                try
                {
                    int numberOfBytes = Convert.ToInt32(message.Substring(equalIndex + 1));

                    if (numberOfBytes > 0)
                    {
                        m_daqDevice.ApiMessageError = m_daqDevice.DriverInterface.SetOutputBufferSize(numberOfBytes);
                        m_daqDevice.DriverInterface.OverwritingOldScanData = false;
                        m_daqDevice.ApiResponse = new DaqResponse(message.Substring(0, equalIndex), double.NaN);
                        m_preStartBuffer = null;
                    }
                    else
                    {
                        m_daqDevice.ApiMessageError = ErrorCodes.InvalidOutputBufferSize;
                    }
                }
                catch (Exception)
                {
                    m_daqDevice.ApiMessageError = ErrorCodes.InvalidOutputBufferSize;
                }
            }

            return m_daqDevice.ApiMessageError;
        }

        //===========================================================================================
        /// <summary>
        /// Calculates the raw D/A value from a scaled value for use with "AO:VALUE="
        /// </summary>
        /// <param name="value">The scaled D/A value</param>
        /// <returns>The raw value</returns>
        //===========================================================================================
        internal override ErrorCodes PreprocessData(ref string message, string componentType)
        {
            if (componentType == "AO" && message.Contains("VALUE"))
            {
                double incomingValue;
                double countValue;
                int valueSentToDevice;

                if (!PlatformParser.TryParse(message.Substring(message.IndexOf("=") + 1), out incomingValue))
                    return ErrorCodes.InvalidDACValue;

                double scaleOffset = 0.0;
                double scaleSlope = 1.0;
                double calOffset = 0.0;
                double calSlope = 1.0;

                if (m_scaleData)
                {
                    double scale = m_activeChannels[0].UpperLimit - ActiveChannels[0].LowerLimit;

                    if (m_activeChannels[0].LowerLimit < 0)
                        scaleOffset = -1.0 * (scale / 2.0);

                    scaleSlope = scale / Math.Pow(2.0, m_dataWidth);
                }

                if (m_calibrateData)
                {
                    calOffset = m_activeChannels[0].CalOffset;
                    calSlope = m_activeChannels[0].CalSlope;
                    m_valueSentCalibrated = true;
                }
                else
                {
                    m_valueSentCalibrated = false;
                }

                countValue = (incomingValue - scaleOffset) / scaleSlope;
                valueSentToDevice = (int)Math.Round((countValue * calSlope) + calOffset, 0);

                // replace the value with the raw value
                int removeIndex = message.IndexOf("=") + 1;
                message = message.Remove(removeIndex, message.Length - removeIndex);
                message += valueSentToDevice.ToString();

                if (valueSentToDevice < 0 || valueSentToDevice > m_maxCount)
                    return ErrorCodes.InvalidDACValue;
            }

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to send the AO:STOP command
        /// </summary>
        //===========================================================================================
        internal override void BeginOutputScan()
        {
            if (m_preStartBuffer != null)
            {
                int destinationIndex = 0;
                CopyScanData(m_preStartBuffer, m_daqDevice.DriverInterface.OutputScanBuffer, ref destinationIndex, m_preStartBuffer.GetLength(1), 0);
                m_preStartBuffer = null;
            }

            m_daqDevice.SendMessageDirect(Messages.AOSCAN_STOP);
            m_daqDevice.SendMessageDirect(Messages.AOSCAN_RESET);
        }

        //=================================================================================================================================================================
        /// <summary>
        /// Copies scan data from the a user buffer to the driver interface's internal write buffer
        /// This override is used for DACs that have 12-bit to 16-bit resolution
        /// </summary>
        /// <param name="source">The source array</param>
        /// <param name="destination">The destination array (the driver interface's internal write buffer)</param>
        /// <param name="copyIndex">The byte index to start copying from</param>
        /// <param name="samplesToCopy">Number of samples to copy per channel</param>
        //=================================================================================================================================================================
        internal override void CopyScanData(double[,] source, byte[] destination, ref int destinationIndex, int samplesToCopyPerChannel, int timeOut)
        {
            int channelCount = source.GetLength(0);
            int destinationLength = destination.Length;

            double scaleSlope = 1.0;
            double scaleOffset = 0.0;
            double calSlope = 1.0;
            double calOffset = 0.0;

            string supportedRanges = m_daqDevice.GetDevCapsString("AO{0}:RANGES", false);

            if (supportedRanges.Contains("FIXED") && m_daqDevice.DriverInterface.CriticalParams.ScaleAoData)
            {
                try
                {
                    string range = MessageTranslator.GetReflectionValue(supportedRanges);
                    Range r = m_supportedRanges[range];
                    double scale = r.UpperLimit - r.LowerLimit;
                    double lsb = scale / Math.Pow(2.0, m_dataWidth);

                    if (r.LowerLimit < 0)
                        scaleOffset = -1.0 * (scale / 2.0);

                    scaleSlope = lsb;
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "Invalid fixed range");
                }
            }
            else if (supportedRanges.Contains("PROG") && m_daqDevice.DriverInterface.CriticalParams.ScaleAoData)
            {
                // TODO: add programmable range. Use list of ranges built in PreprocessAoScanMessage
            }

            unsafe
            {
                try
                {
                    fixed (double* pFixedSrc = source)
                    {
                        fixed (byte* pFixedDest = destination)
                        {
                            double value;
                            ushort dacValue;
                            double* pSrc;
                            byte* pDest = (pFixedDest + destinationIndex);

                            for (int i = 0; i < samplesToCopyPerChannel; i++)
                            {
                                pSrc = pFixedSrc + i;

                                for (int j = 0; j < channelCount; j++)
                                {
                                    if (m_daqDevice.DriverInterface.CriticalParams.CalibrateAoData)
                                    {
                                        calOffset = m_activeChannels[j].CalOffset;
                                        calSlope = m_activeChannels[j].CalSlope;
                                    }

                                    value = (*pSrc - scaleOffset) / scaleSlope;
                                    dacValue = (ushort)Math.Round((value * calSlope) + calOffset, 0);

                                    *pDest++ = (byte)(dacValue & 0x00FF);
                                    *pDest++ = (byte)((dacValue & 0xFF00) >> 8);

                                    if (pDest - pFixedDest > destinationLength)
                                        pDest = pFixedDest;

                                    pSrc += samplesToCopyPerChannel;
                                }
                            }

                            // update the destination byte index
                            destinationIndex += m_daqDevice.CriticalParams.DataOutXferSize * channelCount * samplesToCopyPerChannel;

                            if (destinationIndex >= destination.Length)
                            {
                                destinationIndex %= destination.Length;
                                m_daqDevice.DriverInterface.OverwritingOldScanData = true;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "Error copying data to internalWritebuffer");
                }
            }
        }

        //==============================================================================================================
        /// <summary>
        /// Directly copies the source buffer to the pre-start buffer so that cal-coeffs and scaling
        /// can be applied in the BeginOutputScan method at the point the m_activeChannels are valid.
        /// </summary>
        /// <param name="sourceBuffer">The buffer to cpyo</param>
        /// <param name="samplesToCopyPerChannel">The number of samples to copy per channel</param>
        //==============================================================================================================
        internal virtual void CopyScanDataToPreStartBuffer(double[,] sourceBuffer, int samplesToCopyPerChannel)
        {
            double[,] previousPreStartBuffer = null;

            // if the 1st dimension of the pre-start buffer is equal to the source buffer's 1st dimension then 
            // save the contents of the pre-start buffer
            if (m_preStartBuffer == null || m_preStartBuffer.GetLength(0) != sourceBuffer.GetLength(0))
            {
                m_preStartBuffer = new double[sourceBuffer.GetLength(0), sourceBuffer.GetLength(1)];

                Array.Copy(sourceBuffer, 0,
                           m_preStartBuffer, 0,
                           sourceBuffer.Length);
            }
            else
            {
                previousPreStartBuffer = m_preStartBuffer;

                // make the prestart buffer large enough to hold the contents of the previousStartbuffer and the sourceBuffer
                m_preStartBuffer = new double[previousPreStartBuffer.GetLength(0), previousPreStartBuffer.GetLength(1) + sourceBuffer.GetLength(1)];

                Array.Copy(previousPreStartBuffer, 0, 
                           m_preStartBuffer, 0, 
                           previousPreStartBuffer.Length);

                Array.Copy(sourceBuffer, 0,
                           m_preStartBuffer, previousPreStartBuffer.Length,
                           sourceBuffer.Length);
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
            int channelCount = m_daqDevice.CriticalParams.AoChannelCount;

            try
            {
                double rate = m_daqDevice.CriticalParams.OutputScanRate;

                maxRate = m_maxScanRate / channelCount;

                if (rate < m_minScanRate || rate > maxRate)
                    return ErrorCodes.InvalidScanRateSpecified;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }

            return ErrorCodes.NoErrors;
        }
    }
}
