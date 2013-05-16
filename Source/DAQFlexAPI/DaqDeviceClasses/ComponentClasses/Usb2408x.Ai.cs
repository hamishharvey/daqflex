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
    class Usb2408xAi : AiTempComponent
    {
        private const int SIGN_BITMASK = 1 << 23;
        private const int FULL_SCALE24_BITMASK = (1 << 24) - 1;
        private const int SIGN_EXT_BITMASK = ~FULL_SCALE24_BITMASK;

        private const double MIN_SLOPE = 0.9;
        private const double MAX_SLOPE = 1.2;
        private const double MIN_OFFSET = -10000.0;
        private const double MAX_OFFSET = 10000.0;

        private const double m_devMinCount = -8388608.0;
        private const double m_devMaxCount = 8388607.0;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal Usb2408xAi(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 16)
        {
            for (int i = 0; i < (m_maxChannels / 2); i++)
            {
                m_aiChannelType[i] = GetChannelType(i);
                m_tcType[i] = GetTcType(i);

                if (m_tcType[i] != ThermocoupleTypes.NotSet)
                    m_thermocouple = Thermocouple.CreateThermocouple(m_tcType[i]);
            }

            // create channel mappings
            m_channelMappings.Add(0, 8);
            m_channelMappings.Add(1, 9);
            m_channelMappings.Add(2, 10);
            m_channelMappings.Add(3, 11);
            m_channelMappings.Add(4, 12);
            m_channelMappings.Add(5, 13);
            m_channelMappings.Add(6, 14);
            m_channelMappings.Add(7, 15);
        }

        internal override void Initialize()
        {
            InitializeChannelModes();

            InitializeDataRates();

            InitializeAiChannelTypes();
            
            m_daqDevice.SendMessageDirect(Messages.AISCAN_QUEUE_DISABLE);

            base.Initialize();

            IntializeTCRanges();
            SetDefaultCriticalParams(m_deviceInfo);
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize channel modes
        /// </summary>
        //=================================================================================================================
        internal override void InitializeChannelModes()
        {
            m_channelModes = new string[m_maxChannels];

            // this device is programmable - default is SE
            for (int i = 0; i < m_channelModes.Length; i++)
                m_channelModes[i] = GetChannelMode(i);
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize channel modes
        /// </summary>
        //=================================================================================================================
        internal void InitializeDataRates()
        {
            string msg = string.Empty;
            string response = string.Empty;
            double datarate = 0.0;

            m_daqDevice.CriticalParams.DataRates = new double[m_maxChannels];

            for (int i = 0; i < m_maxChannels; i++)
            {
                msg = Messages.AI_DATARATE_QUERY;
                msg = Messages.InsertChannel(msg, i);

                response = m_daqDevice.SendMessage(msg).ToString();

                PlatformParser.TryParse(response.Substring(response.IndexOf("=")+1), out datarate);
                m_daqDevice.CriticalParams.DataRates[i] = datarate;
            }
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize channel modes
        /// </summary>
        //=================================================================================================================
        internal override void InitializeAiChannelTypes()
        {
            for (int i = 0; i < m_aiChannelType.Length / 2; i++)
                m_aiChannelType[i] = GetChannelType(i);
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            // create supported ranges list
            m_supportedRanges.Clear();
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V) + ":DIFF", new Range(10.0, -10.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP5V) + ":DIFF", new Range(5.0, -5.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP2PT5V) + ":DIFF", new Range(2.5, -2.5));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP1PT25V) + ":DIFF", new Range(1.25, -1.25));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT625V) + ":DIFF", new Range(.625, -.625));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT3125V) + ":DIFF", new Range(.3125, -.3125));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT15625V) + ":DIFF", new Range(.15625, -.15625));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT078125V) + ":DIFF", new Range(.078125, -.078125));

            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V) + ":SE", new Range(10.0, -10.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP5V) + ":SE", new Range(5.0, -5.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP2PT5V) + ":SE", new Range(2.5, -2.5));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP1PT25V) + ":SE", new Range(1.25, -1.25));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT625V) + ":SE", new Range(.625, -.625));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT3125V) + ":SE", new Range(.3125, -.3125));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT15625V) + ":SE", new Range(.15625, -.15625));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT078125V) + ":SE", new Range(.078125, -.078125));

            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT078125V) + ":TC/OTD", new Range(.078125, -.078125));

            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIPPT078125V) + ":TC/NOOTD", new Range(.078125, -.078125));

            // store the current ranges for each channel
            for (int i = 0; i < m_channelCount; i++)
            {
                if (m_channelModes[i] == PropertyValues.TCNOOTD || m_channelModes[i] == PropertyValues.TCOTD)
                    m_ranges[i] = MessageTranslator.ConvertToCurrentCulture("AI{0}:RANGE=BIP78.125E-3V");
                else
                    m_ranges[i] = String.Format("{0}{1}:{2}={3}", DaqComponents.AI, MessageTranslator.GetChannelSpecs(i), DaqProperties.RANGE, MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V));
            }
        }

        //========================================================================================
        /// <summary>
        /// Overriden to read in the AI calibration coefficients
        /// </summary>
        //========================================================================================
        protected override void GetCalCoefficients()
        {
            // get and store cal coefficients for each range - 8 chs, 4 ranges
            double slope = 0;
            double offset = 0;

            string msg;
            string response;
            string defaultRange;
            string defaultMode;

            m_calCoeffs.Clear();

            msg = Messages.AI_CH_RANGE_QUERY;
            msg = Messages.InsertChannel(msg, 0);
            m_daqDevice.SendMessageDirect(msg);
            response = m_daqDevice.DriverInterface.ReadStringDirect();
            defaultRange = MessageTranslator.GetPropertyValue(response);

            msg = Messages.AI_CH_CHMODE_QUERY;
            msg = Messages.InsertChannel(msg, 0);
            m_daqDevice.SendMessageDirect(msg);
            response = m_daqDevice.DriverInterface.ReadStringDirect();
            defaultMode = MessageTranslator.GetPropertyValue(response);

            foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
            {
                // set the mode
                int msgIndex = kvp.Key.IndexOf(":");
                string mode = kvp.Key.Substring(msgIndex + 1, kvp.Key.Length - msgIndex - 1);

                msg = Messages.AI_CH_CHMODE;
                msg = Messages.InsertValue(msg, mode);
                msg = Messages.InsertChannel(msg, 0);
                m_daqDevice.SendMessageDirect(msg);

                // set the range
                string range = kvp.Key.Substring(0, msgIndex);

                msg = Messages.AI_CH_RANGE;
                msg = Messages.InsertValue(msg, range);
                msg = Messages.InsertChannel(msg, 0);
                m_daqDevice.SendMessageDirect(msg);

                // get the slope and offset for the range
                msg = Messages.AI_CH_SLOPE_QUERY;
                msg = Messages.InsertChannel(msg, 0);
                m_daqDevice.SendMessageDirect(msg);
                slope = m_daqDevice.DriverInterface.ReadValueDirect();

                msg = Messages.AI_CH_OFFSET_QUERY;
                msg = Messages.InsertChannel(msg, 0);
                m_daqDevice.SendMessageDirect(msg);
                offset = m_daqDevice.DriverInterface.ReadValueDirect();

#if DEBUG
                // if there are no coeffs stored in eeprom yet, set defaults
                if (slope == 0 || Double.IsNaN(slope))
                {
                    slope = 1;
                    offset = 0;
                }
#endif
                for (int i = 0; i < m_channelCount; i++)
                {
                    m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(slope, offset));
                }
            }

            // restore default mode
            msg = Messages.AI_CH_CHMODE;
            msg = Messages.InsertChannel(msg, 0);
            msg = Messages.InsertValue(msg, defaultMode);
            m_daqDevice.SendMessageDirect(msg);

            // restore default range
            msg = Messages.AI_CH_RANGE;
            msg = Messages.InsertChannel(msg, 0);
            msg = Messages.InsertValue(msg, defaultRange);
            m_daqDevice.SendMessageDirect(msg);
        }

        //===========================================================================================
        /// <summary>
        /// Overridden to get the supported messages specific to this Ai component
        /// </summary>
        /// <returns>A list of supported messages</returns>
        //===========================================================================================
        internal override List<string> GetMessages(string daqComponent)
        {
            List<string> messages = new List<string>();

            if (daqComponent == DaqComponents.AI)
            {
                messages.Add("AI{*}:SENSOR=*");
                messages.Add("AI:RANGE=*");
                messages.Add("AI{*}:RANGE=*");
                messages.Add("AI:CHMODE=*");
                messages.Add("AI{*}:CHMODE=*");
                messages.Add("AI:SCALE=*");
                messages.Add("AI:CAL=*");
                messages.Add("AI:DATARATE=*");
                messages.Add("AI{*}:DATARATE=*");
                messages.Add("AI:ADCAL/START");

                messages.Add("?AI");
                messages.Add("?AI{*}:SENSOR");
                messages.Add("?AI:RANGE");
                messages.Add("?AI{*}:RANGE");
                messages.Add("?AI:CHMODE");
                messages.Add("?AI{*}:CHMODE");
                messages.Add("?AI{*}:VALUE");
                messages.Add("?AI{*}:VALUE/*");
                messages.Add("?AI:SCALE");
                messages.Add("?AI:CAL");
                messages.Add("?AI{*}:SLOPE");
                messages.Add("?AI{*}:OFFSET");
                messages.Add("?AI:VALIDCHANS");
                messages.Add("?AI:VALIDCHANS/CHMODE");
                messages.Add("?AI:RES");
                messages.Add("?AI:DATARATE");
                messages.Add("?AI{*}:DATARATE");
                messages.Add("?AI{*}:CJC");
                messages.Add("?AI:ADCAL/STATUS");
            }
            else if (daqComponent == DaqComponents.AISCAN)
            {
                messages.Add("AISCAN:XFRMODE=*");
                messages.Add("AISCAN:RANGE=*");
                messages.Add("AISCAN:HIGHCHAN=*");
                messages.Add("AISCAN:LOWCHAN=*");
                messages.Add("AISCAN:RATE=*");
                messages.Add("AISCAN:SAMPLES=*");
                messages.Add("AISCAN:SCALE=*");
                messages.Add("AISCAN:CAL=*");
                messages.Add("AISCAN:BUFSIZE=*");
                messages.Add("AISCAN:BUFOVERWRITE=*");
                messages.Add("AISCAN:TEMPUNITS=*");
                messages.Add("AISCAN:QUEUE=*");

                messages.Add("AISCAN:START");
                messages.Add("AISCAN:STOP");
                messages.Add("AISCAN:RESET");

                messages.Add("?AISCAN:XFRMODE");
                messages.Add("?AISCAN:RANGE");
                messages.Add("?AISCAN:HIGHCHAN");
                messages.Add("?AISCAN:LOWCHAN");
                messages.Add("?AISCAN:DEBUG");
                messages.Add("?AISCAN:RATE");
                messages.Add("?AISCAN:SAMPLES");
                messages.Add("?AISCAN:SCALE");
                messages.Add("?AISCAN:CAL");
                messages.Add("?AISCAN:STATUS");
                messages.Add("?AISCAN:BUFSIZE");
                messages.Add("?AISCAN:BUFOVERWRITE");
                messages.Add("?AISCAN:COUNT");
                messages.Add("?AISCAN:INDEX");
                messages.Add("?AISCAN:TEMPUNITS");
                messages.Add("?AISCAN:QUEUE");
            }
            else if (daqComponent == DaqComponents.AIQUEUE)
            {
                messages.Add("AIQUEUE:CLEAR");
                messages.Add("AIQUEUE{*}:CHAN=*");
                messages.Add("AIQUEUE{*}:CHMODE=*");
                messages.Add("AIQUEUE{*}:RANGE=*");
                messages.Add("AIQUEUE{*}:DATARATE=*");
                messages.Add("?AIQUEUE:COUNT");
                messages.Add("?AIQUEUE{*}:CHAN");
                messages.Add("?AIQUEUE{*}:CHMODE");
                messages.Add("?AIQUEUE{*}:RANGE");
                messages.Add("?AIQUEUE{*}:DATARATE");
            }

            return messages;
        }

        //================================================================================
        /// <summary>
        /// Overriden to use the AI range when the queue is not being used
        /// </summary>
        /// <param name="channel"></param>
        //================================================================================
        protected override string GetAiScanRange(int channel)
        {
            return "?AISCAN:RANGE";
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to set the RESET command
        /// Other overrides should call this at the beginning or end of the override
        /// </summary>
        //===========================================================================================
        internal override void BeginInputScan()
        {
            m_daqDevice.CriticalParams.NumberOfSamplesForSingleIO = 1;
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
                if (value > m_devMinCount && value < m_devMaxCount)
                {
                    if (m_activeChannels[0].CalSlope != 0 && !Double.IsNaN(m_activeChannels[0].CalSlope))
                    {
                        calibratedValue = value * m_activeChannels[0].CalSlope;
                        calibratedValue += m_activeChannels[0].CalOffset;

                        if (!m_daqDevice.CriticalParams.AiDataIsSigned)
                            calibratedValue = Math.Max(0, calibratedValue);
                        calibratedValue = Math.Min(m_maxCount, calibratedValue);
                    }
                }
            }

            return calibratedValue;
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

            double maxCount;
            double minCount;

            if (m_scaleData)
            {
                maxCount = m_devMaxCount;
                minCount = m_devMinCount;
            }
            else
            {
                maxCount = m_maxCount;
                minCount = 0;
            }

            if (value >= maxCount || value <= minCount)
            {
                m_otd = true;

                if (m_channelModes[0] == PropertyValues.TCOTD)
                    errorCode = ErrorCodes.OpenThermocouple;
            }
            else
            {
                m_otd = false;
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Overriden to start the self calibration
        /// </summary>
        //====================================================================================
        internal override ErrorCodes StartCal()
        {
            m_calProcessThread = new Thread(new ThreadStart(CalProcessThread));
            m_calProcessThread.Start();

            // for now 
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Overridden method for processing a data rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessDataRateMessage(string message)
        {
            int channel;
            int lIndex = message.IndexOf(CurlyBraces.LEFT);
            int rIndex = message.IndexOf(CurlyBraces.RIGHT);
            PlatformParser.TryParse(message.Substring(lIndex + 1, rIndex - lIndex), out channel);

            m_daqDevice.SendMessageDirect(message).ToString();

            double datarate;
            PlatformParser.TryParse(message.Substring(message.IndexOf(Constants.EQUAL_SIGN) + 1), out datarate);
            m_daqDevice.CriticalParams.DataRates[channel] = datarate;

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Overridden method for processing a channel mode message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessChannelModeMessage(ref string message)
        {
            if (message.Contains("?"))
                return ErrorCodes.NoErrors;

            ErrorCodes errorcode = base.ProcessChannelModeMessage(ref message);

            if (errorcode == ErrorCodes.NoErrors)
            {
                string channelMode = message.Substring(message.IndexOf(Constants.EQUAL_SIGN) + 1);

                if (message.Contains("{"))
                {
                    // set channel mode for specified channel
                    int channel = MessageTranslator.GetChannel(message);
                    m_aiChannelType[channel] = GetChannelType(channelMode);
                    m_channelModes[channel] = MessageTranslator.GetPropertyValue(message);

                    try
                    {
                        // if temp mode, set range for specified channel to BIP78.125E-3V
                        if (message.Contains("TC/OTD") || message.Contains("TC/NOOTD"))
                        {
                            string msg = Messages.AI_CH_RANGE;
                            msg = msg.Replace("*", channel.ToString());
                            msg = msg.Replace("#", DevCapValues.BIPPT078125V);
                            m_daqDevice.SendMessageDirect(msg);

                            // update range info
                            ProcessRangeMessage(ref msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Assert(false, ex.Message);
                    }
                }
                else
                {
                    // BOBG - 08/17/11
                    // Remove this to fix issue Jason is seeing with the ValidChannels query
                    //
                    //// set channel mode for all channels
                    ////for (int i = 0; i < m_aiChannelType.Length; i++)
                    ////{
                    ////    m_aiChannelType[i] = GetChannelType(channelMode);
                    ////    m_channelModes[i] = GetChannelMode(i);
                    ////}

                    try
                    {
                        // if temp mode, set range for all channels to BIP78.125E-3V
                        if (message.Contains("TC/OTD") || message.Contains("TC/NOOTD"))
                        {
                            string msg = Messages.AI_RANGE;
                            msg = msg.Replace("#", DevCapValues.BIPPT078125V);
                            m_daqDevice.SendMessageDirect(msg);

                            // update range info
                            ProcessRangeMessage(ref msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Assert(false, ex.Message);
                    }
                }
            }

            return errorcode;
        }

        //====================================================================================
        /// <summary>
        /// Overridden method for validating a data rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ValidateDataRate()
        {
            string supportedDataRates = m_daqDevice.GetDevCapsString("AI:DATARATES", true);

            double[] datarates = m_daqDevice.CriticalParams.DataRates;
            foreach (double dr in datarates)
            {
                string drStr = dr.ToString();
                if (supportedDataRates.Contains(drStr))
                    continue;
                else
                    return ErrorCodes.InvalidDataRate;
            }

            return ErrorCodes.NoErrors;
        }

        //===============================================================================================
        /// <summary>
        /// Overriden to validate the per channel rate just before AISCAN:START is sent to the device
        /// </summary>
        /// <param name="message">The device message</param>
        //===============================================================================================
        internal override ErrorCodes ValidateScanRate()
        {
            ErrorCodes errorcode = ValidateDataRate();
            if (errorcode != ErrorCodes.NoErrors)
                return errorcode;

            double maxRate = double.MaxValue;
            int channelCount = m_daqDevice.CriticalParams.AiChannelCount;

            try
            {
                double rate = m_daqDevice.CriticalParams.InputScanRate;

                if (m_daqDevice.CriticalParams.InputTransferMode == TransferMode.BurstIO)
                {
                    maxRate = m_maxBurstRate / channelCount;

                    if (rate < m_minBurstRate || rate > maxRate)
                        return ErrorCodes.InvalidScanRateSpecified;
                }
                else
                {
                    m_maxScanThroughput = CalculateMaxScanThroughput();
                    //maxRate = m_maxScanThroughput / channelCount;

                    if (rate < m_minScanRate || rate > m_maxScanThroughput)
                        return ErrorCodes.InvalidScanRateSpecified;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }

            return ErrorCodes.NoErrors;
        }

        protected double CalculateMaxScanThroughput()
        {
            double sum = 0.0;

            m_daqDevice.SendMessageDirect(Messages.AISCAN_QUEUE_QUERY);
            string response = m_daqDevice.DriverInterface.ReadStringDirect();

            if (response.Contains(DevCapValues.ENABLE))
            {
                m_daqDevice.SendMessageDirect(Messages.AIQUEUE_COUNT_QUERY);
                response = m_daqDevice.DriverInterface.ReadStringDirect();
                int queueCount = Convert.ToInt32(MessageTranslator.GetPropertyValue(response));

                // make sure we have a data rate for each entry in the queue
                for (int i=0; i<queueCount; i++)
                {
                    // if the queue list data rate is -1, then we need to get 
                    // the default queue data rate for the entry
                    if (m_aiQueueList[i].DataRate == -1)
                    {
                        // get the datarate for the queue entry
                        string msg = Messages.AIQUEUE_DATARATE_QUERY;
                        msg = msg.Replace("*", i.ToString());
                        m_daqDevice.SendMessageDirect(msg);
                        response = m_daqDevice.DriverInterface.ReadStringDirect();
                        double dataRate = Convert.ToDouble(MessageTranslator.GetPropertyValue(response));
                        m_aiQueueList[i].DataRate = dataRate;
                    }
                }

                for (int i = 0; i < queueCount; i++)
                {
                    sum += (1 / m_aiQueueList[i].DataRate) + .000640;
                }
            }
            else
            {
                int lowChannel = m_daqDevice.CriticalParams.LowAiChannel;
                int highChannel = m_daqDevice.CriticalParams.HighAiChannel;

                for (int i = lowChannel; i <= highChannel; i++)
                {
                    sum += (1 / m_daqDevice.CriticalParams.DataRates[i]) + .000640;
                }
            }
            double maxScanThroughput = 1 / sum; ;

            return maxScanThroughput;
        }

        //====================================================================================
        /// <summary>
        /// Calibrate for each range using channel 0
        /// </summary>
        //====================================================================================
        protected void CalProcessThread()
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            CalThreadId = Thread.CurrentThread.ManagedThreadId;

            string[] ranges = m_daqDevice.GetDevCapsString("AI{0}:RANGES", true).Split(new char[] { PlatformInterop.LocalListSeparator });

            int progress = 0;
            int progressIncrement = 100 / (ranges.Length + 1);

            // calibrate the voltage ranges
            foreach (string range in ranges)
            {
                CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, progress.ToString());

                errorCode = CalADC(range);

                if (errorCode != ErrorCodes.NoErrors)
                    break;

                progress += progressIncrement;
            }

            // calibrate the thermocouple range
            if (errorCode == ErrorCodes.NoErrors)
            {
                // set the TC mode to get a TC range
                m_daqDevice.SendMessage("AI:CHMODE=TC/OTD");
                ranges = m_daqDevice.GetDevCapsString("AI{0}:RANGES", true).Split(new char[] { PlatformInterop.LocalListSeparator });
                foreach (string range in ranges)
                {
                    string tc_range = "TC_" + range;

                    // add "TC_" to the range so that CalADC can tell this is 
                    // a thermocuple range
                    errorCode = CalADC(tc_range);

                    if (errorCode != ErrorCodes.NoErrors)
                        break;

                    progress += progressIncrement;
                }
            }

            if (errorCode == ErrorCodes.NoErrors)
                CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, progress.ToString());
            else
                CalStatus = m_daqDevice.GetErrorMessage(errorCode);


            // read back new cal coefficients
            GetCalCoefficients();

            Thread.Sleep(250);

            CalThreadId = 0;

            CalStatus = PropertyValues.IDLE;
        }

        //======================================================================================
        /// <summary>
        /// Performs a self-calibration of the analog inputs for each range and
        /// stores the cal coefficients. Cal measurements are only made on channel 0
        /// for this device
        /// </summary>
        /// <param name="range">The range to calibrate</param>
        //======================================================================================
        protected ErrorCodes CalADC(string range)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            string msg;

            try
            {
                // disable calibration and scaling
                m_daqDevice.SendMessage(Messages.AISCAN_CAL_DISABLE);
                m_daqDevice.SendMessage(Messages.AISCAN_SCALE_DISABLE);

                bool TCRange = false;
                if (range.Contains("TC_"))
                {
                    TCRange = true;
                    range = range.Substring(range.IndexOf('_') + 1);
                }

                double[] vRefs = GetVRefs(range);
                double[] measuredVRefs = new double[vRefs.Length];
                double[] dataReading = new double[vRefs.Length];
                double slope = 1.0;
                double offset = 0.0;
                string vRef;

                // unlock the component to perform the cal
                m_daqDevice.SendMessage("AICAL:UNLOCK");

                // set the range to calibrate
                msg = Messages.AICAL_RANGE;
                msg = Messages.InsertValue(msg, range);
                m_daqDevice.SendMessage(msg);


                // set the offset calibration mode
                msg = Messages.AICAL_MODE;
                if (TCRange)
                    msg = Messages.InsertValue(msg, "TCOFFSET");
                else
                    msg = Messages.InsertValue(msg, "AIOFFSET");
                m_daqDevice.SendMessage(msg);

                // get the measured GND value
                msg = Messages.AICAL_VALUE_QUERY;
                msg = Messages.InsertChannel(msg, 0);
                string aiValue = m_daqDevice.SendMessage(msg).ToString();
                aiValue = aiValue.Substring(aiValue.IndexOf("=") + 1);

                dataReading[0] = Int32.Parse(aiValue);


                // set the gain calibration mode
                msg = Messages.AICAL_MODE;
                if (TCRange)
                    msg = Messages.InsertValue(msg, "TCGAIN/POS");
                else
                    msg = Messages.InsertValue(msg, "AIGAIN");
                m_daqDevice.SendMessage(msg);

                // set the HIGH cal reference on the device
                msg = Messages.AICAL_REF;
                vRef = VrefDoubleToString(vRefs[1]);
                msg = Messages.InsertValue(msg, vRef);
                m_daqDevice.SendMessage(msg);
                Thread.Sleep(500);

                // get the measured HIGH ref value 
                string response = m_daqDevice.SendMessage(Messages.AICAL_REFVAL_QUERY + "/HEX").ToString();
                response = MessageTranslator.GetPropertyValue(response).Remove(0, 2);
                measuredVRefs[1] = HexStringToDouble(response);

                // get the measured HIGH AI value
                msg = Messages.AICAL_VALUE_QUERY;
                msg = Messages.InsertChannel(msg, 0);
                aiValue = m_daqDevice.SendMessage(msg).ToString();
                aiValue = aiValue.Substring(aiValue.IndexOf("=") + 1);

                dataReading[1] = Int32.Parse(aiValue);


                // if NOT temperature range calibrate the LOW values
                if (!TCRange)
                {
                    // set the LOW cal reference on the device
                    msg = Messages.AICAL_REF;
                    vRef = VrefDoubleToString(vRefs[2]);
                    msg = Messages.InsertValue(msg, vRef);
                    m_daqDevice.SendMessage(msg);
                    Thread.Sleep(500);

                    // get the measured LOW ref value 
                    response = m_daqDevice.SendMessage(Messages.AICAL_REFVAL_QUERY + "/HEX").ToString();
                    response = MessageTranslator.GetPropertyValue(response).Remove(0, 2);
                    measuredVRefs[2] = HexStringToDouble(response);

                    // get the measured LOW AI value 
                    msg = Messages.AICAL_VALUE_QUERY;
                    msg = Messages.InsertChannel(msg, 0);
                    aiValue = m_daqDevice.SendMessage(msg).ToString();
                    aiValue = aiValue.Substring(aiValue.IndexOf("=") + 1);

                    dataReading[2] = double.Parse(aiValue);
                }


                int[] vRefValues;
                ConvertVrefsToCounts(TCRange, measuredVRefs, vRefs, out vRefValues);

                // compute the slope and offset
                slope = (vRefValues[1] - vRefValues[2]) / (dataReading[1] - dataReading[2]);
                offset = -slope * dataReading[0];

                // compare to MIN_SLOPE, MIN_OFFSET, MAX_SLOPE, MAX_OFFSET
                if (slope < MIN_SLOPE)
                    errorCode = ErrorCodes.MinAiCalSlopeValueReached;
                else if (slope > MAX_SLOPE)
                    errorCode = ErrorCodes.MaxAiCalSlopeValueReached;

                if (offset < MIN_OFFSET)
                    errorCode = ErrorCodes.MinAiCalOffsetValueReached;
                else if (offset > MAX_OFFSET)
                    errorCode = ErrorCodes.MaxAiCalOffsetValueReached;


                if (errorCode == ErrorCodes.NoErrors)
                {
                    // store the slope
                    msg = Messages.AICAL_CH_SLOPE_HEX;
                    msg = Messages.InsertChannel(msg, 0);
                    msg = Messages.InsertValue(msg, DoubleToHexString(slope));
                    m_daqDevice.SendMessage(msg);

                    // store the offset
                    msg = Messages.AICAL_CH_OFFSET_HEX;
                    msg = Messages.InsertChannel(msg, 0);
                    msg = Messages.InsertValue(msg, DoubleToHexString(offset));
                    m_daqDevice.SendMessage(msg);
                }

            }
            catch (Exception ex)
            {
                msg = String.Format("AI self cal failed: {0}", ex.Message);
                System.Diagnostics.Debug.Assert(false, msg);
                errorCode = ErrorCodes.UnknownError;
            }


            // lock the component to read a value
            m_daqDevice.SendMessage("AICAL:LOCK");

            return errorCode;
        }


        //==========================================================================================================
        /// <summary>
        /// Converts Voltage references to counts. This implementation assumes
        /// measuredVRefs[0] = 0 Volts
        /// measuredVRefs[1] = +N Volts
        /// measuredVRefs[2] = -N Volts
        /// </summary>
        /// <param name="measuredVRefs">The voltage references in volts</param>
        /// <param name="vRefRanges">The voltage reference ranges in volts</param>
        /// <param name="vRefsOut">The voltage references in counts</param>
        //==========================================================================================================
        internal void ConvertVrefsToCounts(bool tcRange, double[] measuredVRefs, double[] vRefRanges, out int[] vRefsOut)
        {
            vRefsOut = new int[3];

            if (measuredVRefs.Rank != 1)
            {
                string msg = String.Format("vRefsIn does not have the right dimensions: is {0}, expected {1}", measuredVRefs.Rank, 1);
                System.Diagnostics.Debug.Assert(false, msg);
                return;
            }

            if (measuredVRefs.Length != 3)
            {
                string msg = String.Format("vRefsIn does not have the right number of elements: is {0}, expected {1}", measuredVRefs.Length, 3);
                System.Diagnostics.Debug.Assert(false, msg);
                return;
            }

            double Scale;
            if (tcRange)
                Scale = 0.078125 * 2;
            else
                Scale = vRefRanges[1] - vRefRanges[2];

            double lsb = Scale / Math.Pow(2.0, 24);

            for (int i=0; i<vRefsOut.Length; i++)
                vRefsOut[i] = Convert.ToInt32((measuredVRefs[i]) / lsb);
        }

        //=======================================================================================
        /// <summary>
        /// Gets the vrefs for the specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        //=======================================================================================
        protected string VrefDoubleToString(double vref)
        {
            string s = string.Empty;

            if (vref == 10.0)
                s = "+10.0V";
            else if (vref == -10.0)
                s = "-10.0V";
            else if (vref == 5.0)
                s = "+5.0V";
            else if (vref == -5.0)
                s = "-5.0V";
            else if (vref == 2.5)
                s = "+2.5V";
            else if (vref == -2.5)
                s = "-2.5V";
            else if (vref == 1.25)
                s = "+1.25V";
            else if (vref == -1.25)
                s = "-1.25V";
            else if (vref == .625)
                s = "+625.0E-3V";
            else if (vref == -.625)
                s = "-625.0E-3V";
            else if (vref == .3125)
                s = "+312.5E-3V";
            else if (vref == -.3125)
                s = "-312.5E-3V";
            else if (vref == .15625)
                s = "+156.25E-3V";
            else if (vref == -.15625)
                s = "-156.25E-3V";
            else if (vref == .078125)
                s = "+78.125E-3V";
            else if (vref == -.078125)
                s = "-78.125E-3V";

            return s;
        }

        //=======================================================================================
        /// <summary>
        /// Gets the vrefs for the specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        //=======================================================================================
        protected double[] GetVRefs(string range)
        {
            double[] vRefs = null;

            string convertedRange = MessageTranslator.ConvertToEnglish(range);

            switch (convertedRange)
            {
                case (PropertyValues.BIP10V):
                    vRefs = new double[] { 0.0, 10.0, -10.0 };
                    break;
                case (PropertyValues.BIP5V):
                    vRefs = new double[] { 0.0, 5.0, -5.0 };
                    break;
                case (PropertyValues.BIP2PT5V):
                    vRefs = new double[] { 0.0, 2.5, -2.5 };
                    break;
                case (PropertyValues.BIP1PT25V):
                    vRefs = new double[] { 0.0, 1.25, -1.25 };
                    break;
                case (PropertyValues.BIPPT625V):
                    vRefs = new double[] { 0.0, 0.625, -0.625 };
                    break;
                case (PropertyValues.BIPPT3125V):
                    vRefs = new double[] { 0.0, 0.3125, -0.3125 };
                    break;
                case (PropertyValues.BIPPT15625V):
                    vRefs = new double[] { 0.0, 0.15625, -0.15625 };
                    break;
                case (PropertyValues.BIPPT078125V):
                    vRefs = new double[] { 0.0, 0.078125, -0.078125 };
                    break;
                default:
                    vRefs = null;
                    break;
            }

            return vRefs;
        }

        //=========================================================================================
        /// <summary>
        /// Let the JIT compiler compile critical methods
        /// </summary>
        //=========================================================================================
        internal override void ConfigureScan()
        {
            string msg;

            base.ConfigureScan();

            // set max datarate to avoid overrun
            msg = Messages.AI_DATARATE;
            msg = Messages.InsertValue(msg, 3750);
            m_daqDevice.SendMessage(msg).ToValue();

            // use the default rate to avoid an overrun
            msg = Messages.AISCAN_RATE_QUERY;

            // use 1/2 the channels for a quicker scan
            msg = Messages.AISCAN_HIGHCHAN_QUERY;
            int highChannel = (int)m_daqDevice.SendMessage(msg).ToValue();
            msg = Messages.AISCAN_HIGHCHAN;
            msg = Messages.InsertValue(msg, (highChannel / 2));
            m_daqDevice.SendMessage(msg);

            msg = Messages.AISCAN_RATE;
            msg = Messages.InsertValue(msg, 100);
            m_daqDevice.SendMessage(msg);

            msg = Messages.AISCAN_SAMPLES;
            msg = Messages.InsertValue(msg, 20);
            m_daqDevice.SendMessage(msg);
        }

        ////====================================================================================
        ///// <summary>
        ///// Virtual method for processing the xfer mode message
        ///// </summary>
        ///// <param name="message">The device message</param>
        ////====================================================================================
        //internal override ErrorCodes PreProcessXferModeMessage(ref string message)
        //{
        //    ErrorCodes errorCode;

        //    errorCode = base.PreProcessXferModeMessage(ref message);

        //    /* override the setting of this critical param. This device only supports one sample per channel in SINGLEIO mode because of the different data rates */
        //    if (errorCode == ErrorCodes.NoErrors)
        //        m_daqDevice.CriticalParams.NumberOfSamplesForSingleIO = 1;

        //    return errorCode;
        //}

        //======================================================================================================================
        /// <summary>
        /// Overridden because the channels can have different channel mode settings
        /// </summary>
        /// <param name="channelNumber">The channel number</param>
        /// <param name="message">The value get message</param>
        /// <returns>An error code</returns>
        //======================================================================================================================
        internal override ErrorCodes ProcessValueGetMessage(int channelNumber, ref string message)
        {
            m_aiChannelType[channelNumber] = GetChannelType(channelNumber);

            m_activeChannels = new ActiveChannels[1];
            string rangeKey = m_ranges[channelNumber].Substring(m_ranges[channelNumber].IndexOf(Constants.EQUAL_SIGN) + 1);

            m_channelModes[channelNumber] = GetChannelMode(channelNumber);

            rangeKey += String.Format(":{0}", m_channelModes[channelNumber]);

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
            }

            return base.ProcessValueGetMessage(channelNumber, ref message);
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
            List<int> upperChannels = new List<int>();
            int midChannel = m_maxChannels / 2;

            for (int i = 0; i < midChannel; i++)
            {
                validChannels += i.ToString();

                if (includeMode)
                    validChannels += String.Format(" ({0})", m_channelModes[i]);

                if (m_channelModes[i] == PropertyValues.SE)
                    upperChannels.Add(m_channelMappings[i]);

                if (i < (m_maxChannels - 1))
                    validChannels += PlatformInterop.LocalListSeparator;
            }

            int upperChannel;
            int count = upperChannels.Count;
            for (int i = 0; i < count; i++)
            {
                upperChannel = upperChannels[i];

                validChannels += upperChannel.ToString();

                if (includeMode)
                {
                    if (m_channelModes[i + midChannel] == PropertyValues.DIFF)
                    {
                        validChannels += String.Format(" ({0})", PropertyValues.DIFF);
                    }
                    else
                    {
                        validChannels += String.Format(" ({0})", PropertyValues.SE);
                        upperChannels.Add(m_channelMappings[i]);
                    }
                }

                if (i < (upperChannels.Count - 1))
                    validChannels += PlatformInterop.LocalListSeparator;
            }

            if (validChannels.EndsWith(","))
                validChannels = validChannels.Remove(validChannels.LastIndexOf(","), 1);

            return validChannels;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to handle the /HEX=0x... format
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        private ErrorCodes FormatHexDouble(ref string message)
        {
            if (message.Contains("/HEX=0X"))
            {
                message = message.Replace("HEX=0X", "HEX=0x");
            }

            return ErrorCodes.NoErrors;
        }
        //=================================================================================================================
        /// <summary>
        /// Overriden to handle the /HEX=0x... format
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessCalSlopeMessage(ref string message)
        {
            FormatHexDouble(ref message);

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to handle the /HEX=0x... format
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessCalOffsetMessage(ref string message)
        {
            FormatHexDouble(ref message);

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to handle the /HEX=0x... format
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PostProcessData(string componentType, ref string response, ref double value)
        {
            ErrorCodes errorcode = ErrorCodes.NoErrors;

            if (componentType == DaqComponents.AI && response.Contains(DaqProperties.VALUE))
            {
                if (m_scaleData == false)
                {
                    // no scaling required, but if the data is signed, we
                    // need to out it in a range of 0 to (MaxCount - 1)
                    if (m_daqDevice.CriticalParams.AiDataIsSigned)
                    {
                        int intValue = Convert.ToInt32(value);

                        if ((intValue & m_SignBitMask) == 0)
                        {
                            //Positive data
                            uint mask = 0xFFFFFF;
                            intValue &= (int)mask;
                            intValue += (m_maxCount + 1) / 2;
                        }
                        else
                        {
                            //Negative data
                            uint mask = 0xFF000000;
                            intValue |= (int)mask;
                            intValue += (m_maxCount + 1) / 2;
                        }

                        value = intValue;
                        response = response.Substring(0, response.IndexOf("=") + 1) + value.ToString();

                    }
                    else
                    {
                        errorcode = base.PostProcessData(componentType, ref response, ref value);
                    }
                }
                else
                {
                    errorcode = base.PostProcessData(componentType, ref response, ref value);
                }
            }

            return errorcode;
        }


        //===========================================================================================================================================
        /// <summary>
        /// Copies scan data from the driver interface's internal read buffer to the destination array
        /// This override is used for 12-bit to 16-bit products
        /// </summary>
        /// <param name="source">The source array (driver interface's internal read buffer)</param>
        /// <param name="destination">The destination array (array return to the application)</param>
        /// <param name="copyIndex">The byte index to start copying from</param>
        /// <param name="samplesToCopy">Number of samples to copy</param>
        //===========================================================================================================================================
        internal override void CopyScanData(byte[] sourceBuffer, double[,] destinationBuffer, ref int sourceCopyByteIndex, int samplesToCopyPerChannel)
        {
            unsafe
            {
                try
                {
                    int xfrSize;
                    int bitSize;

                    // transfer size is the number of BYTES transferred per sample per channel over the bulk in endpoint
                    xfrSize = (int)m_daqDevice.GetDevCapsValue("AISCAN:XFRSIZE");

                    if (Double.IsNaN(xfrSize))
                        bitSize = m_daqDevice.CriticalParams.AiDataWidth;
                    else
                        bitSize = Constants.BITS_PER_BYTE * (int)xfrSize;

                    if (bitSize <= 32)
                    {
                        int workingSourceIndex = sourceCopyByteIndex;
                        int channelCount = destinationBuffer.GetLength(0);
                        int totalSamplesToCopy = channelCount * samplesToCopyPerChannel;

                        fixed (double* pSlopesFixed = m_daqDevice.CriticalParams.AiSlopes, pOffsetsFixed = m_daqDevice.CriticalParams.AiOffsets, pDestinationBufferFixed = destinationBuffer)
                        {
                            double* pSlopes = pSlopesFixed;
                            double* pOffsets = pOffsetsFixed;

                            fixed (byte* pSourceBufferFixed = sourceBuffer)
                            {
                                uint* pSourceBuffer = (uint*)(pSourceBufferFixed + sourceCopyByteIndex);
                                double* pDestinationBuffer;

                                int channelIndex = 0;
                                int samplesPerChannelIndex = -1;

                                Monitor.Enter(m_readCjcLock);

                                for (int i = 0; i < totalSamplesToCopy; i++)
                                {
                                    if (i % m_daqDevice.CriticalParams.AiChannelCount == 0)
                                    {
                                        pSlopes = pSlopesFixed;
                                        pOffsets = pOffsetsFixed;
                                        channelIndex = 0;
                                        samplesPerChannelIndex++;
                                    }

                                    pDestinationBuffer = pDestinationBufferFixed + (channelIndex * destinationBuffer.GetLength(1) + samplesPerChannelIndex);

                                    int value = (int)*pSourceBuffer++;

                                    double scaledValue = 0.0;
                                    if (m_scaleData)
                                    {
                                        // scale the data
                                        if ((value & m_SignBitMask) == 0)
                                        {
                                            // positive value so clear upper byte
                                            value &= (int)0xFFFFFF;
                                        }
                                        else
                                        {
                                            // negative value so set upper byte
                                            uint mask = 0xFF000000;
                                            value |= (int)mask;
                                        }

                                        scaledValue = value;

                                        scaledValue = scaledValue * (*pSlopes++) + (*pOffsets++);
                                    }
                                    else
                                    {
                                        // no scaling required, but if the data is signed, we
                                        // need to put it in a range of 0 to (MaxCount - 1)
                                        if ((value & m_SignBitMask) == 0)
                                        {
                                            //Positive data
                                            uint mask = 0xFFFFFF;
                                            value &= (int)mask;
                                            value += (m_maxCount + 1) / 2;
                                        }
                                        else
                                        {
                                            //Negative data
                                            uint mask = 0xFF000000;
                                            value |= (int)mask;
                                            value += (m_maxCount + 1) / 2;
                                        }

                                        scaledValue = value;

                                        scaledValue = scaledValue * (*pSlopes++) + (*pOffsets);
                                    }

                                    int channel = m_activeChannels[channelIndex].ChannelNumber;
                                    if (m_daqDevice.CriticalParams.AiQueueEnabled)
                                    {
                                        AiQueue aiQueue = m_aiQueueList[channelIndex];
                                        if ((aiQueue.ChannelMode == DevCapConfigurations.TCOTD) || (aiQueue.ChannelMode == DevCapConfigurations.TCNOOTD))
                                            ScaledDataToTemperature(channelIndex, ref scaledValue);
                                    }
                                    else
                                    {
                                        if (m_aiChannelType[channel] == AiChannelTypes.Temperature)
                                        {
                                            ScaledDataToTemperature(channelIndex, ref scaledValue);
                                        }
                                    }
                                    *pDestinationBuffer = scaledValue;

                                    workingSourceIndex += xfrSize;

                                    if (workingSourceIndex >= sourceBuffer.Length)
                                    {
                                        pSourceBuffer = (uint*)pSourceBufferFixed;
                                        workingSourceIndex = 0;
                                    }

                                    channelIndex++;
                                }

                                Monitor.Exit(m_readCjcLock);
                            }
                        }

                        sourceCopyByteIndex = workingSourceIndex;
                    }
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "Error copying ai scan data");
                }
            }
        }
    }
}
