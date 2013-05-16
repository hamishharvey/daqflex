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
    internal class Usb1608GXAo : AoComponent
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
        public Usb1608GXAo(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 2)
        {
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V), new Range(10.0, -10.0));

            // get and store cal coefficients for each range - 8 chs, 4 ranges
            double slope;
            double offset;
            string msg;

            for (int i = 0; i < m_channelCount; i++)
            {
                foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
                {
                    // get the slope and offset for the range
                    msg = Messages.AO_CH_SLOPE_QUERY;
                    msg = Messages.InsertChannel(msg, i);
                    m_daqDevice.SendMessageDirect(msg);
                    slope = m_daqDevice.DriverInterface.ReadValueDirect();

                    msg = Messages.AO_CH_OFFSET_QUERY;
                    msg = Messages.InsertChannel(msg, i);
                    m_daqDevice.SendMessageDirect(msg);
                    offset = m_daqDevice.DriverInterface.ReadValueDirect();

#if DEBUG
                    // if there are no coeffs stored in eeprom yet, set defaults
                    if (Double.IsNaN(slope) || Double.IsInfinity(slope) || slope == 0)
                    {
                        slope = 1;
                        offset = 0;
                    }
#endif
                    m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(slope, offset));
                }
            }

            m_daqDevice.DriverInterface.CriticalParams.CalibrateAoData = true;

            for (int i = 0; i < m_channelCount; i++)
                m_ranges[i] = String.Format("{0}{1}:{2}={3}", DaqComponents.AO, MessageTranslator.GetChannelSpecs(i), DaqProperties.RANGE, MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V));
        }

        //====================================================================================
        /// <summary>
        /// Override to set the critical params
        /// </summary>
        /// <param name="message">The device message</param>
        /// <returns>An error code</returns>
        //====================================================================================
        internal override ErrorCodes PreprocessExtPacer(ref string message)
        {
            if (message.Contains(PropertyValues.DISABLE))
                m_daqDevice.CriticalParams.AoExtPacer = false;
            else
                m_daqDevice.CriticalParams.AoExtPacer = true;

            return ErrorCodes.NoErrors;
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
            if (componentType == DaqComponents.AOSCAN && response.Contains(DaqProperties.RATE))
            {
                if (m_daqDevice.CriticalParams.AoExtPacer)
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

            m_savedScanRateMsg = message;

            errorCode = base.ProcessScanRate(ref message);

            if (errorCode == ErrorCodes.NoErrors)
            {
                if (m_daqDevice.CriticalParams.AoExtPacer)
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
            int channels;
            string[] ranges;
            int progress = 0;

            CalThreadId = Thread.CurrentThread.ManagedThreadId;

            // unlock the component to perform the cal
            m_daqDevice.SendMessage(Messages.AOCAL_UNLOCK);

            ranges = m_daqDevice.GetDevCapsString("AO{0}:RANGES", true).Split(new char[] { PlatformInterop.LocalListSeparator });

            channels = (int)m_daqDevice.SendMessage(Messages.AO_CHAN_QUERY).ToValue();

            foreach (string range in ranges)
            {
                for (int i = 0; i < channels; i++)
                {
                    CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, progress.ToString());

                    errorCode = CalDAC(i, range);

                    if (errorCode != ErrorCodes.NoErrors)
                        break;

                    progress += 100 / ranges.Length;
                }
            }

            if (errorCode == ErrorCodes.NoErrors)
                CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, progress.ToString());
            else
                CalStatus = m_daqDevice.GetErrorMessage(errorCode);

            // lock the component to perform the cal
            m_daqDevice.SendMessage(Messages.AOCAL_LOCK);

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
        protected ErrorCodes CalDAC(int channel, string range)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            string msg;
            int samples = 100;
            int rate = 10000;
            double slope = 1.0;
            double offset = 0.0;
            string aiCalOption;
            string aiScaleOption;
            string aoCalOption;
            string aoScaleOption;
            double[,] data;
            int[] desiredCounts = new int[] {500, 65000};
            double[] channelData;
            double[] dataAverage = new double[desiredCounts.Length];

            /////////////////////////////////////////////////////////////
            // Set up the analog output channel
            /////////////////////////////////////////////////////////////

            // save the state of the CAL and SCALE options
            msg = Messages.AO_CAL_QUERY;
            aoCalOption = m_daqDevice.SendMessage(msg).ToString();

            msg = Messages.AO_SCALE_QUERY;
            aoScaleOption = m_daqDevice.SendMessage(msg).ToString();

            // disable AOut CAL and SCALE options
            msg = Messages.AO_CAL_DISABLE;
            m_daqDevice.SendMessage(msg);

            msg = Messages.AO_SCALE_DISABLE;
            m_daqDevice.SendMessage(msg);

            // set the cal config to the specified channel
            msg = Messages.AOCAL_CH_AIVALUE_QUERY;
            msg = Messages.InsertChannel(msg, channel);
            m_daqDevice.SendMessage(msg);

            /////////////////////////////////////////////////////////////
            // now measure the value using an analog input scan
            /////////////////////////////////////////////////////////////

            // save the state of the CAL and SCALE options
            msg = Messages.AISCAN_CAL_QUERY;
            aiCalOption = m_daqDevice.SendMessage(msg).ToString();

            msg = Messages.AISCAN_SCALE_QUERY;
            aiScaleOption = m_daqDevice.SendMessage(msg).ToString();

            // enable AISCAN CAL 
            msg = Messages.AISCAN_CAL_ENABLE;
            m_daqDevice.SendMessage(msg);

            // disable AISCAN SCALE
            msg = Messages.AISCAN_SCALE_DISABLE;
            m_daqDevice.SendMessage(msg);

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

            for (int i = 0; i < desiredCounts.Length; i++)
            {
                // set the AOut value for the specified channel
                msg = Messages.AO_CH_VALUE;
                msg = Messages.InsertChannel(msg, channel);
                msg = Messages.InsertValue(msg, desiredCounts[i]);
                m_daqDevice.SendMessage(msg);
                Thread.Sleep(500);

                msg = Messages.AISCAN_START;
                m_daqDevice.SendMessage(msg);

                data = m_daqDevice.ReadScanData(samples, 0);

                // convert to a 1D array
                channelData = GetChannelData(data, 0);

                // caluculate the average of the scan data
                dataAverage[i] = GetAverage(channelData);
            }

            // compute the slope and offset
            double desiredCountsSum = GetSum(desiredCounts);
            double avgSum = GetSum(dataAverage);
            double avgSqrSum = GetSqrSum(dataAverage);

            slope = GetInnerProduct(dataAverage, desiredCounts);
            slope = desiredCounts.Length * slope - avgSum * desiredCountsSum;
            slope /= (desiredCounts.Length * avgSqrSum - avgSum * avgSum);
            offset = (desiredCountsSum - slope * avgSum) / desiredCounts.Length;

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
                msg = Messages.AOCAL_CH_SLOPE;
                msg = Messages.InsertChannel(msg, channel);
                msg = Messages.InsertValue(msg, (float)slope);
                m_daqDevice.SendMessage(msg);

                // store the offset
                msg = Messages.AOCAL_CH_OFFSET;
                msg = Messages.InsertChannel(msg, channel);
                msg = Messages.InsertValue(msg, (float)offset);
                m_daqDevice.SendMessage(msg);
            }

            // restore cal and scale options
            m_daqDevice.SendMessage(aoCalOption).ToString();
            m_daqDevice.SendMessage(aoScaleOption).ToString();
            m_daqDevice.SendMessage(aiCalOption).ToString();
            m_daqDevice.SendMessage(aiScaleOption).ToString();

            return errorCode;
        }
    }
}
