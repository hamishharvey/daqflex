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
    internal class Usb2408Ao : AoComponent
    {
        private const int SIGN_BITMASK = 1 << 23;
        private const int FULL_SCALE24_BITMASK = (1 << 24) - 1;
        private  const int SIGN_EXT_BITMASK = ~FULL_SCALE24_BITMASK;

        private const double MIN_SLOPE = 0.9;
        private const double MAX_SLOPE = 1.2;
        private const double MIN_OFFSET = -10000.0;
        private const double MAX_OFFSET = 10000.0;


        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        public Usb2408Ao(DaqDevice daqDevice, DeviceInfo deviceInfo)
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

            for (int i = 0; i < m_channelCount; i++)
            {
                foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
                {
                    // get the slope and offset for the range
                    m_daqDevice.SendMessageDirect(String.Format("?AO{0}:SLOPE", MessageTranslator.GetChannelSpecs(i)));
                    slope = m_daqDevice.DriverInterface.ReadValueDirect();

                    m_daqDevice.SendMessageDirect(String.Format("?AO{0}:OFFSET", MessageTranslator.GetChannelSpecs(i)));
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

        //==========================================================================================
        /// <summary>
        /// Runs a scan to compile critical code
        /// </summary>
        //==========================================================================================
        internal override void RunScan()
        {
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to set the default critical params
        /// </summary>
        //===========================================================================================
        internal override void SetDefaultCriticalParams(DeviceInfo deviceInfo)
        {
            base.SetDefaultCriticalParams(deviceInfo);

            //int fifofSize = (int)m_daqDevice.GetDevCapsValue("AOSCAN:FIFOSIZE");
            //m_daqDevice.CriticalParams.OutputFifoSize = fifofSize - 256;
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

            string[] ranges = m_daqDevice.GetDevCapsString("AO{0}:RANGES", true).Split(new char[] { PlatformInterop.LocalListSeparator });

            int progress = 0;
            int progressIncrement = 100 / (ranges.Length + 1);

            // calibrate the voltage ranges
            int channels = (int)m_daqDevice.SendMessage(Messages.AO_CHAN_QUERY).ToValue();
            foreach (string range in ranges)
            {
                for (int i = 0; i < channels; i++)
                {
                    CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, progress.ToString());

                    errorCode = CalDAC(i, range);

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
        /// Performs a self-calibration of the analog outputs for each range and
        /// stores the cal coefficients. Cal measurements are only made on channel 0
        /// for this device
        /// </summary>
        /// <param name="range">The range to calibrate</param>
        //======================================================================================
        internal ErrorCodes CalDAC(int channel, string range)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int loOut = 1000;
            int hiOut = 64500;
            int gndOut = 32768;
            int loIn = 0;
            int hiIn = 0;
            int gndIn = 0;

            string msg;
            string response;

            try
            {
                // disable calibration and scaling
                m_daqDevice.SendMessage(Messages.AOSCAN_CAL_DISABLE);
                m_daqDevice.SendMessage(Messages.AOSCAN_SCALE_DISABLE);

                // unlock the component to perform the cal
                m_daqDevice.SendMessage("AOCAL:UNLOCK");

                // get the AICAL slope
                msg = Messages.AOCAL_CH_AISLOPE_HEX_QUERY;
                msg = Messages.InsertChannel(msg, channel);
                response = m_daqDevice.SendMessage(msg).ToString();
                response = MessageTranslator.GetPropertyValue(response).Remove(0, 2);
                double aiCalSlope = HexStringToDouble(response);

                // get the AICAL offset
                msg = Messages.AOCAL_CH_AIOFFSET_HEX_QUERY;
                msg = Messages.InsertChannel(msg, channel);
                response = m_daqDevice.SendMessage(msg).ToString();
                response = MessageTranslator.GetPropertyValue(response).Remove(0, 2);
                double aiCalOffset = HexStringToDouble(response);

                // set the value for the output channel
                msg = Messages.AOCAL_CH_VALUE;
                msg = Messages.InsertChannel(msg, channel);
                msg = Messages.InsertValue(msg, loOut);
                m_daqDevice.SendMessage(msg);
                Thread.Sleep(100);

                // read back the input channel
                msg = Messages.AOCAL_CH_AIVALUE_QUERY;
                msg = Messages.InsertChannel(msg, channel);
                string aiValue = m_daqDevice.SendMessage(msg).ToString();
                aiValue = aiValue.Substring(aiValue.IndexOf("=") + 1);

                loIn = Int32.Parse(aiValue);

                // scale and convert to 16 bits for output
                double measLo = loIn;
                measLo *= aiCalSlope;
                measLo += aiCalOffset;
                measLo /= 256.0;
                measLo += 32768;



                // set the value for the output channel
                msg = Messages.AOCAL_CH_VALUE;
                msg = Messages.InsertChannel(msg, channel);
                msg = Messages.InsertValue(msg, hiOut);
                m_daqDevice.SendMessage(msg);
                Thread.Sleep(100);

                // read back the input channel
                msg = Messages.AOCAL_CH_AIVALUE_QUERY;
                msg = Messages.InsertChannel(msg, channel);
                aiValue = m_daqDevice.SendMessage(msg).ToString();
                aiValue = aiValue.Substring(aiValue.IndexOf("=") + 1);

                hiIn = Int32.Parse(aiValue);

                // scale and convert to 16 bits for output
                double measHi = hiIn;
                measHi *= aiCalSlope;
                measHi += aiCalOffset;
                measHi /= 256.0;
                measHi += 32768;



                // set the value for the output channel
                msg = Messages.AOCAL_CH_VALUE;
                msg = Messages.InsertChannel(msg, channel);
                msg = Messages.InsertValue(msg, gndOut);
                m_daqDevice.SendMessage(msg);
                Thread.Sleep(100);

                // read back the input channel
                msg = Messages.AOCAL_CH_AIVALUE_QUERY;
                msg = Messages.InsertChannel(msg, channel);
                aiValue = m_daqDevice.SendMessage(msg).ToString();
                aiValue = aiValue.Substring(aiValue.IndexOf("=") + 1);

                gndIn = Int32.Parse(aiValue);

                // scale and convert to 16 bits for output
                double measGnd = gndIn;
                measGnd *= aiCalSlope;
                measGnd += aiCalOffset;
                measGnd /= 256.0;
                measGnd += 32768;

                double denom = 3 * ((measLo * measLo) + (measHi * measHi) + (measGnd * measGnd));
                denom -= (measLo + measHi + measGnd) * (measLo + measHi + measGnd);

                double slope = 3 * ((loOut * measLo) + (hiOut * measHi) + (gndOut * measGnd));
                slope -= (measLo + measHi + measGnd) * (loOut + hiOut + gndOut);
                slope /= denom;

                double offset = (loOut + hiOut + gndOut);
                offset -= slope * (measLo + measHi + measGnd);
                offset /= 3;


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
                    msg = Messages.AOCAL_CH_SLOPE_HEX;
                    msg = Messages.InsertChannel(msg, channel);
                    msg = Messages.InsertValue(msg, DoubleToHexString(slope));
                    m_daqDevice.SendMessage(msg);

                    // store the offset
                    msg = Messages.AOCAL_CH_OFFSET_HEX;
                    msg = Messages.InsertChannel(msg, channel);
                    msg = Messages.InsertValue(msg, DoubleToHexString(offset));
                    m_daqDevice.SendMessage(msg);
                }
            }
            catch (Exception ex)
            {
                msg = String.Format("AO self cal failed: {0}", ex.Message);
                System.Diagnostics.Debug.Assert(false, msg);
                errorCode = ErrorCodes.UnknownError;
            }


            // lock the component to read a value
            m_daqDevice.SendMessage("AOCAL:LOCK");

            return errorCode;
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

            m_calCoeffs.Clear();

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
                    if (slope == 0 || Double.IsNaN(slope))
                    {
                        slope = 1;
                        offset = 0;
                    }
#endif
                    m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(slope, offset));
                }
            }
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
    }
}
