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
    class Usb7202Ai : AiComponent
    {
        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal Usb7202Ai(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 8)
        {
            m_dataWidth = 16;
            m_maxCount = (int)Math.Pow(2, m_dataWidth) - 1;

            InitializeRanges();
            InitializeChannelModes();
            SetDefaultCriticalParams(deviceInfo);
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
            // create supported ranges list
            m_supportedRanges.Add(PropertyValues.BIP10V + ":SE", new Range(10.0, -10.0));
            m_supportedRanges.Add(PropertyValues.BIP5V + ":SE", new Range(5.0, -5.0));
            m_supportedRanges.Add(PropertyValues.BIP2V + ":SE", new Range(2.0, -2.0));
            m_supportedRanges.Add(PropertyValues.BIP1V + ":SE", new Range(1.0, -1.0));


            // get and store cal coefficients for each range - 8 chs, 4 ranges
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

                    // if there are no coeffs stored in eeprom yet, set defaults
                    if (slope == 0 || Double.IsNaN(slope))
                    {
                        slope = 1;
                        offset = 0;
                    }

                    m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(slope, offset));
                }
            }

            // store the current ranges for each channel
            for (int i = 0; i < m_channelCount; i++)
                m_ranges[i] = String.Format("{0}{1}:{2}={3}", DaqComponents.AI, MessageTranslator.GetChannelSpecs(i), DaqProperties.RANGE, PropertyValues.BIP10V);
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to set the default critical params
        /// </summary>
        //===========================================================================================
        internal override void SetDefaultCriticalParams(DeviceInfo deviceInfo)
        {
            m_daqDevice.DriverInterface.CriticalParams.InputConversionMode = InputConversionMode.Simultaneous;
            m_daqDevice.DriverInterface.CriticalParams.InputPacketSize = deviceInfo.MaxPacketSize;
            m_daqDevice.DriverInterface.CriticalParams.AiDataWidth = m_dataWidth;
            m_daqDevice.DriverInterface.CriticalParams.InputScanRate = 1000;
            m_daqDevice.DriverInterface.CriticalParams.InputScanSamples = 100;
            m_daqDevice.DriverInterface.CriticalParams.LowAiChannel = 0;
            m_daqDevice.DriverInterface.CriticalParams.HighAiChannel = 3;
            m_daqDevice.DriverInterface.CriticalParams.AiChannelCount = 4;
            m_daqDevice.DriverInterface.CriticalParams.InputXferSize =
                            m_daqDevice.DriverInterface.GetOptimalInputBufferSize(m_daqDevice.DriverInterface.CriticalParams.InputScanRate);
        }

        //=========================================================================================
        /// <summary>
        /// Let the JIT compiler compile critical methods
        /// </summary>
        //=========================================================================================
        internal override void Initialize()
        {
            base.Initialize();

            int lowChannel = m_daqDevice.DriverInterface.CriticalParams.LowAiChannel;
            int highChannel = m_daqDevice.DriverInterface.CriticalParams.HighAiChannel;
            double rate = m_daqDevice.DriverInterface.CriticalParams.InputScanRate;
            int samples = m_daqDevice.DriverInterface.CriticalParams.InputScanSamples;

            m_daqDevice.SendMessage("AISCAN:RANGE=BIP10V");
            m_daqDevice.SendMessage("AISCAN:XFRMODE=BLOCKIO");
            m_daqDevice.SendMessage("AISCAN:DEBUG=DISABLE");
            m_daqDevice.SendMessage("AISCAN:CAL=ENABLE");
            m_daqDevice.SendMessage("AISCAN:EXTPACER=DISABLE");
            m_daqDevice.SendMessage("AISCAN:TRIG=DISABLE");
            m_daqDevice.SendMessage(String.Format("AISCAN:LOWCHAN={0}", lowChannel));
            m_daqDevice.SendMessage(String.Format("AISCAN:HIGHCHAN={0}", highChannel));
            m_daqDevice.SendMessage(String.Format("AISCAN:RATE={0}", rate));
            m_daqDevice.SendMessage(String.Format("AISCAN:SAMPLES={0}", samples));
            m_daqDevice.SendMessage("AISCAN:START");

#pragma warning disable 219
            double[,] multiChannelData = m_daqDevice.ReadScanData(64, 3000);
#pragma warning restore 219

            string status;

            do
            {
                status = m_daqDevice.SendMessage("?AISCAN:STATUS").ToString();
                System.Threading.Thread.Sleep(1);
            } while (status.Contains(PropertyValues.RUNNING));

            System.Diagnostics.Debug.WriteLine(String.Format("status = {0}", status));
            m_daqDevice.SendMessage("DEV:RESET/DEFAULT");
            m_daqDevice.SendMessage("AISCAN:STALL=ENABLE");
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
                messages.Add("AI{*}:RANGE=*");
                messages.Add("AI:SCALE=*");
                messages.Add("AI:CAL=*");
                messages.Add("?AI");
                messages.Add("?AI{*}:VALUE");
                messages.Add("?AI{*}:VALUE/*");
                messages.Add("?AI{*}:RANGE");
                messages.Add("?AI{*}:SLOPE");
                messages.Add("?AI{*}:OFFSET");
                messages.Add("?AI:CAL");
                messages.Add("?AI:SCALE");
            }
            else if (daqComponent == DaqComponents.AISCAN)
            {
                messages.Add("AISCAN:XFRMODE=*");
                messages.Add("AISCAN:RANGE{*}=*");
                messages.Add("AISCAN:HIGHCHAN=*");
                messages.Add("AISCAN:LOWCHAN=*");
                messages.Add("AISCAN:DEBUG=*");
                messages.Add("AISCAN:RATE=*");
                messages.Add("AISCAN:SAMPLES=*");
                messages.Add("AISCAN:TRIG=*");
                messages.Add("AISCAN:SCALE=*");
                messages.Add("AISCAN:CAL=*");
                messages.Add("AISCAN:EXTPACER=*");
                messages.Add("AISCAN:BUFSIZE=*");

                messages.Add("AISCAN:START");
                messages.Add("AISCAN:STOP");

                messages.Add("?AISCAN:XFRMODE");
                messages.Add("?AISCAN:RANGE{*}");
                messages.Add("?AISCAN:HIGHCHAN");
                messages.Add("?AISCAN:LOWCHAN");
                messages.Add("?AISCAN:DEBUG");
                messages.Add("?AISCAN:RATE");
                messages.Add("?AISCAN:SAMPLES");
                messages.Add("?AISCAN:TRIG");
                messages.Add("?AISCAN:SCALE");
                messages.Add("?AISCAN:CAL");
                messages.Add("?AISCAN:EXTPACER");
                messages.Add("?AISCAN:STATUS");
                messages.Add("?AISCAN:BUFSIZE");
                messages.Add("?AISCAN:COUNT");
                messages.Add("?AISCAN:INDEX");
            }
            else if (daqComponent == DaqComponents.AITRIG)
            {
                messages.Add("AITRIG:TYPE=*");
                messages.Add("?AITRIG:TYPE");
            }

            return messages;
        }

        //====================================================================================
        /// <summary>
        /// Virutal method to set the clock source options
        /// </summary>
        /// <param name="clockSource">The clock source</param>
        //====================================================================================
        internal override ErrorCodes SendDeferredClockMessage(ClockSource clockSource)
        {
            if (clockSource == ClockSource.External && DeferredClockOptionMessage.Contains(PropertyValues.MASTER))
                return ErrorCodes.IncompatibleClockOption;

            try
            {
                if (clockSource == ClockSource.External)
                    m_daqDevice.SendMessage(DeferredClockOptionMessage);
                else
                    m_daqDevice.SendMessage(m_internalClockOptionMessage);
                return ErrorCodes.NoErrors;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
                return ErrorCodes.UnknownError;
            }
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the AiScan message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The component this message pertains to</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessAiScanMessage(ref string message)
        {
            ErrorCodes errorCode = base.PreprocessAiScanMessage(ref message);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

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
                        int channel = MessageTranslator.GetChannel(message);

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

                return base.ProcessRangeMessage(ref message);
            }

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to determine the transfer mode when its set to default
        /// </summary>
        /// <returns>The default transfer mode</returns>
        //=================================================================================================================
        internal override TransferMode GetDefaultTransferMode()
        {
            if (m_daqDevice.CriticalParams.InputScanRate <= 500)
                return TransferMode.SingleIO;
            else
                return TransferMode.BlockIO;
        }

        //===========================================================================================
        /// <summary>
        /// Calibrates an analog input value
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

        internal override double GetMinScanRate()
        {
            return 0.596;
        }

        internal override double GetMaxScanRate()
        {
            double maxRate;

            if (m_daqDevice.DriverInterface.CriticalParams.InputTransferMode == TransferMode.BurstIO)
                maxRate = 200000.0 / m_daqDevice.DriverInterface.CriticalParams.AiChannelCount;
            else
                maxRate = 50000.0 / m_daqDevice.DriverInterface.CriticalParams.AiChannelCount;

            return maxRate;
        }
    }
}
