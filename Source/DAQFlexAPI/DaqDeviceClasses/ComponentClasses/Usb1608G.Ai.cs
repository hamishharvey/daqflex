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
    class Usb1608GAi : MixedModeAiComponent
    {
        private const double MIN_SLOPE = 0.9;
        private const double MAX_SLOPE = 1.2;
        private const double MIN_OFFSET = -10000.0;
        private const double MAX_OFFSET = 10000.0;

        protected string m_savedScanRateMsg = String.Empty;

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal Usb1608GAi(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 16)
        {
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

        //=======================================================================
        /// <summary>
        /// Overriden to disable the queue
        /// </summary>
        //=======================================================================
        internal override void Initialize()
        {
            m_daqDevice.SendMessageDirect(Messages.AISCAN_QUEUE_DISABLE);

            base.Initialize();
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
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP2V) + ":DIFF", new Range(2.0, -2.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP1V) + ":DIFF", new Range(1.0, -1.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V) + ":SE", new Range(10.0, -10.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP5V) + ":SE", new Range(5.0, -5.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP2V) + ":SE", new Range(2.0, -2.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP1V) + ":SE", new Range(1.0, -1.0));

            // store the current ranges for each channel
            for (int i = 0; i < m_channelCount; i++)
                m_ranges[i] = String.Format("{0}{1}:{2}={3}", DaqComponents.AI, MessageTranslator.GetChannelSpecs(i), DaqProperties.RANGE, MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V));
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
            string coeffKey = String.Empty;

            m_calCoeffs.Clear();

            foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
            {
                // set the range
                string range = kvp.Key.Substring(0, kvp.Key.IndexOf(":"));

                msg = Messages.AI_CH_RANGE;
                msg = Messages.InsertChannel(msg, 0);
                msg = Messages.InsertValue(msg, range);
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
                for (int i = 0; i < m_maxChannels; i++)
                {
                    coeffKey = String.Format("Ch{0}:{1}", i, kvp.Key);
                    m_calCoeffs.Add(coeffKey, new CalCoeffs(slope, offset));
                }
            }
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
                messages.Add("AI:RANGE=*");
                messages.Add("AI{*}:RANGE=*");
                messages.Add("AI:CHMODE=*");
                messages.Add("AI{*}:CHMODE=*");
                messages.Add("AI:SCALE=*");
                messages.Add("AI:CAL=*");
                messages.Add("?AI");
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
            }
            else if (daqComponent == DaqComponents.AISCAN)
            {
                messages.Add("AISCAN:XFRMODE=*");
                messages.Add("AISCAN:RANGE=*");
                messages.Add("AISCAN:HIGHCHAN=*");
                messages.Add("AISCAN:LOWCHAN=*");
                messages.Add("AISCAN:RATE=*");
                messages.Add("AISCAN:SAMPLES=*");
                messages.Add("AISCAN:TRIG=*");
                messages.Add("AISCAN:SCALE=*");
                messages.Add("AISCAN:CAL=*");
                messages.Add("AISCAN:EXTPACER=*");
                messages.Add("AISCAN:BUFSIZE=*");
                messages.Add("AISCAN:BUFOVERWRITE=*");
                messages.Add("AISCAN:BURSTMODE=*");
                messages.Add("AISCAN:QUEUE=*");

                messages.Add("AISCAN:START");
                messages.Add("AISCAN:STOP");

                messages.Add("?AISCAN:XFRMODE");
                messages.Add("?AISCAN:RANGE");
                messages.Add("?AISCAN:HIGHCHAN");
                messages.Add("?AISCAN:LOWCHAN");
                messages.Add("?AISCAN:RATE");
                messages.Add("?AISCAN:SAMPLES");
                messages.Add("?AISCAN:TRIG");
                messages.Add("?AISCAN:SCALE");
                messages.Add("?AISCAN:CAL");
                messages.Add("?AISCAN:EXTPACER");
                messages.Add("?AISCAN:STATUS");
                messages.Add("?AISCAN:BUFSIZE");
                messages.Add("?AISCAN:BUFOVERWRITE");
                messages.Add("?AISCAN:COUNT");
                messages.Add("?AISCAN:INDEX");
                messages.Add("?AISCAN:BURSTMODE");
                messages.Add("?AISCAN:QUEUE");
            }
            else if (daqComponent == DaqComponents.AITRIG)
            {
                messages.Add("AITRIG:TYPE=*");
                messages.Add("AITRIG:REARM=*");
                messages.Add("?AITRIG:TYPE");
                messages.Add("?AITRIG:REARM");
            }
            else if (daqComponent == DaqComponents.AIQUEUE)
            {
                messages.Add("AIQUEUE:CLEAR");
                messages.Add("AIQUEUE{*}:CHAN=*");
                messages.Add("AIQUEUE{*}:CHMODE=*");
                messages.Add("AIQUEUE{*}:RANGE=*");
                messages.Add("?AIQUEUE:COUNT");
                messages.Add("?AIQUEUE{*}:CHAN");
                messages.Add("?AIQUEUE{*}:CHMODE");
                messages.Add("?AIQUEUE{*}:RANGE");
            }

            return messages;
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
            int channel = MessageTranslator.GetChannel(message);
            List<string> validChannels = new List<string>();
                
            validChannels.AddRange(GetValidChannels(false).Split(new char[] {PlatformInterop.LocalListSeparator}));

            if (message.Contains(DaqProperties.SLOPE) || message.Contains(DaqProperties.OFFSET))
            {
                if (channel < 0 || channel > m_maxChannels)
                    return ErrorCodes.InvalidAiChannelSpecified;
            }
            else if (message.Contains(Constants.QUERY.ToString()))
            {
                if (channel < 0 || channel > m_maxChannels)
                    return ErrorCodes.InvalidAiChannelSpecified;
            }
            else if (!validChannels.Contains(channel.ToString()))
            {
                return ErrorCodes.InvalidAiChannelSpecified;
            }

            if (message.Contains(DaqProperties.LOWCHAN))
                m_daqDevice.DriverInterface.CriticalParams.LowAiChannel = channel;
            else if (message.Contains(DaqProperties.HIGHCHAN))
                m_daqDevice.DriverInterface.CriticalParams.HighAiChannel = channel;

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to check for non-contiguous channels when not using the queue at start of scan
        /// in addition to validing the rate
        /// </summary>
        //===========================================================================================
        internal override ErrorCodes ValidateScanRate()
        {
            ErrorCodes errorCode = base.ValidateScanRate();

            if (errorCode == ErrorCodes.NoErrors && !m_daqDevice.CriticalParams.AiQueueEnabled)
            {
                int lowChannel = m_daqDevice.CriticalParams.LowAiChannel;
                int highChannel = m_daqDevice.CriticalParams.HighAiChannel;
                int channelCount = highChannel - lowChannel + 1;

                string[] validChannels = GetValidChannels(false).Split(new char[] { PlatformInterop.LocalListSeparator });

                for (int i = lowChannel; i < channelCount; i++)
                {
                    if (validChannels[i] != i.ToString())
                    {
                        errorCode = ErrorCodes.NoncontiguousChannelsSpecified;
                        break;
                    }
                }
            }

            return errorCode;
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
        /// Overriden to 
        /// </summary>
        /// <param name="dataType">The type of data (e.g. Ai, Ao, Dio)</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes PostProcessData(string componentType, ref string response, ref double value)
        {
            if (componentType == DaqComponents.AISCAN && response.Contains(DaqProperties.RATE))
            {
                if (m_daqDevice.CriticalParams.AiExtPacer)
                {
                    string rateValue = MessageTranslator.GetPropertyValue(m_savedScanRateMsg);
                    string devValue = MessageTranslator.GetPropertyValue(response);

                    if (PlatformParser.TryParse(rateValue, out value))
                        response = response.Replace(devValue, rateValue);
                    else
                        System.Diagnostics.Debug.Assert(false, "Invalid rate query value");
                }

                return ErrorCodes.NoErrors;
            }
            else
            {
                return base.PostProcessData(componentType, ref response, ref value);
            }
        }

        //====================================================================================
        /// <summary>
        /// Overriden to save the rate message when ext pacer is enabled.
        /// If the rate message is sent when ext pacer is enabled, it will then
        /// disable the ext pacer
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanRate(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // save the rate message
            m_savedScanRateMsg = message;

            errorCode = base.ProcessScanRate(ref message);

            if (errorCode == ErrorCodes.NoErrors)
            {
                if (m_daqDevice.CriticalParams.AiExtPacer)
                {
                    // if ext pacer is enabled don't send the message to the device but update the critical params
                    double rate;
                    bool parsed = PlatformParser.TryParse(MessageTranslator.GetPropertyValue(message), out rate);

                    m_daqDevice.SendMessageToDevice = false;

                    if (parsed)
                    {
                        m_daqDevice.CriticalParams.InputScanRate = rate;
                    }
                    else
                    {
                        errorCode = ErrorCodes.InvalidScanRateSpecified;
                    }
                }
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
        /// Calibrate for each range using channel 0
        /// </summary>
        //====================================================================================
        protected void CalProcessThread()
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            CalThreadId = Thread.CurrentThread.ManagedThreadId;

            string[] ranges = m_daqDevice.GetDevCapsString("AI{0}:RANGES", true).Split(new char[] { PlatformInterop.LocalListSeparator });

            int progress = 0;

            foreach (string range in ranges)
            {
                CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, progress.ToString());

                errorCode = CalADC(range);

                if (errorCode != ErrorCodes.NoErrors)
                    break;

                progress += 100 / ranges.Length;
            }

            if (errorCode == ErrorCodes.NoErrors)
                CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, progress.ToString());
            else
                CalStatus = m_daqDevice.GetErrorMessage(errorCode);

            // read back new cal coefficients
            GetCalCoefficients();

            Thread.Sleep(250);

            CalThreadId = 0;

            if (errorCode == ErrorCodes.NoErrors)
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
            int channel = 0;
            double rate = 250000; // make dynamic max/2
            int samples = 1000;
            double[,] data;

            try
            {
                // unlock the component to perform the cal
                m_daqDevice.SendMessage(Messages.AICAL_UNLOCK);

                // disable calibration and scaling
                m_daqDevice.SendMessage(Messages.AISCAN_CAL_DISABLE);
                m_daqDevice.SendMessage(Messages.AISCAN_SCALE_DISABLE);

                // set the range
                msg = Messages.AISCAN_RANGE;
                msg = Messages.InsertValue(msg, range);
                m_daqDevice.SendMessage(msg);

                // set the low and high channel to 0
                msg = Messages.AISCAN_LOWCHAN;
                msg = Messages.InsertValue(msg, 0);
                m_daqDevice.SendMessage(msg);

                msg = Messages.AISCAN_HIGHCHAN;
                msg = Messages.InsertValue(msg, 0);
                m_daqDevice.SendMessage(msg);

                // set the rate
                msg = Messages.AISCAN_RATE;
                msg = Messages.InsertValue(msg, (float)rate);
                m_daqDevice.SendMessage(msg);

                // set the samples
                msg = Messages.AISCAN_SAMPLES;
                msg = Messages.InsertValue(msg, samples);
                m_daqDevice.SendMessage(msg);

                double[] vRefs = GetVRefs(range);
                double[] measuredVRefs = new double[vRefs.Length];
                double[] channelData;
                double[] dataAverage = new double[vRefs.Length];
                double slope = 1.0;
                double offset = 0.0;
                string vRef;

                for (int i = 0; i < vRefs.Length; i++)
                {
                    // set the cal reference on the device
                    msg = Messages.AICAL_REF;
                    if (vRefs[i] > 0)
                        vRef = String.Format("+{0:F1}V", (float)vRefs[i]); 
                    else
                        vRef = String.Format("{0:F1}V", (float)vRefs[i]); 

                    msg = Messages.InsertValue(msg, vRef);
                    m_daqDevice.SendMessage(msg);
                    Thread.Sleep(500);

                    // get the measured value for the cal reference
                    measuredVRefs[i] = m_daqDevice.SendMessage(Messages.AICAL_REFVAL_QUERY).ToValue();

                    // start the scan
                    m_daqDevice.SendMessage(Messages.AISCAN_START);

                    // read the data
                    try
                    {
                        data = m_daqDevice.ReadScanData(samples, 0);
                    }
                    catch (DaqException ex)
                    {
                        errorCode = ex.ErrorCode;
                        return errorCode;
                    }

                    // convert to a 1D array
                    channelData = GetChannelData(data, channel);

                    // caluculate the average of the scan data
                    dataAverage[i] = GetAverage(channelData);
                }

                int[] desiredValues;
                ConvertVrefsToCounts(measuredVRefs, vRefs, out desiredValues);

                // compute the slope and offset
                double vRefSum = GetSum(desiredValues);
                double avgSum = GetSum(dataAverage);
                double avgSqrSum = GetSqrSum(dataAverage);

                slope = GetInnerProduct(dataAverage, desiredValues);
                slope = vRefs.Length * slope - avgSum * vRefSum;
                slope /= (vRefs.Length * avgSqrSum - avgSum * avgSum);
                offset = (vRefSum - slope * avgSum) / vRefs.Length;

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
                    msg = Messages.AICAL_CH_SLOPE;
                    msg = Messages.InsertChannel(msg, channel);
                    msg = Messages.InsertValue(msg, (float)slope);
                    m_daqDevice.SendMessage(msg);

                    // store the offset
                    msg = Messages.AICAL_CH_OFFSET;
                    msg = Messages.InsertChannel(msg, channel);
                    msg = Messages.InsertValue(msg, (float)offset);
                    m_daqDevice.SendMessage(msg);
                }

                m_daqDevice.SendMessage(Messages.AICAL_LOCK);
            }
            catch (Exception ex)
            {
                string errMsg = String.Format("AI self cal failed: {0}", ex.Message);
                System.Diagnostics.Debug.Assert(false, errMsg);
                errorCode = ErrorCodes.UnknownError;
            }

            return errorCode;
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
                    vRefs = new double[]{0.0, 10.0, -10.0};
                    break;
                case (PropertyValues.BIP5V):
                    vRefs = new double[] { 0.0, 5.0, -5.0 };
                    break;
                case (PropertyValues.BIP2V):
                    vRefs = new double[] { 0.0, 2.0, -2.0 };
                    break;
                case (PropertyValues.BIP1V):
                    vRefs = new double[] { 0.0, 1.0, -1.0 };
                    break;
                default:
                    vRefs = null;
                    break;
            }

            return vRefs;
        }
    }
}
