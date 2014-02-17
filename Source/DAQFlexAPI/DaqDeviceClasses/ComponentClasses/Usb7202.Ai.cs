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
    class Usb7202Ai : FixedModeAiComponent
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
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to set the 0 length packet flag
        /// </summary>
        //=================================================================================================================
        internal override void Initialize()
        {
            base.Initialize();

            m_daqDevice.CriticalParams.Requires0LengthPacketForSingleIO = true;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize channel modes
        /// </summary>
        //=================================================================================================================
        internal override void InitializeChannelModes()
        {
            m_channelModes = new string[m_maxChannels];

            // this device is fixed at single-ended
            for (int i = 0; i < m_channelModes.Length; i++)
                m_channelModes[i] = PropertyValues.SE;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            // create supported ranges list
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
            string msg;
            string response;
            string defaultRange;

            // get and store cal coefficients for each range - 8 chs, 4 ranges
            for (int i = 0; i < m_channelCount; i++)
            {
                msg = String.Format("?AI{0}:RANGE", MessageTranslator.GetChannelSpecs(i));
                m_daqDevice.SendMessageDirect(msg);
                response = m_daqDevice.DriverInterface.ReadStringDirect();
                defaultRange = MessageTranslator.GetPropertyValue(response);

                foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
                {
                    // set the range
                    string range = kvp.Key.Substring(0, kvp.Key.IndexOf(":"));

                    m_daqDevice.SendMessageDirect(String.Format("AI{0}:RANGE={1}", MessageTranslator.GetChannelSpecs(i), range));

                    // get the slope and offset for the range
                    m_daqDevice.SendMessageDirect(String.Format("?AI{0}:SLOPE", MessageTranslator.GetChannelSpecs(i)));
                    double slope = m_daqDevice.DriverInterface.ReadValueDirect();
                    m_daqDevice.SendMessageDirect(String.Format("?AI{0}:OFFSET", MessageTranslator.GetChannelSpecs(i)));
                    double offset = m_daqDevice.DriverInterface.ReadValueDirect();

                    m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(slope, offset));
                }
                // restore default range
                msg = String.Format("AI{0}:RANGE={1}", MessageTranslator.GetChannelSpecs(i), defaultRange);
                m_daqDevice.SendMessageDirect(msg);
            }
        }

        //=========================================================================================
        /// <summary>
        /// Let the JIT compiler compile critical methods
        /// </summary>
        //=========================================================================================
        internal override void ConfigureScan()
        {
            m_daqDevice.SendMessage("AISCAN:STALL=ENABLE");
            m_daqDevice.SendMessage("AISCAN:LOWCHAN=0");
            m_daqDevice.SendMessage("AISCAN:HIGHCHAN=3");
            m_daqDevice.SendMessage("AISCAN:RATE=5000");
            m_daqDevice.SendMessage("AISCAN:SAMPLES=100");
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
                messages.Add("AISCAN:BUFOVERWRITE=*");
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
                messages.Add("?AISCAN:BUFOVERWRITE");
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

        //====================================================================================================================================
        /// <summary>
        /// Virtual method for processing an external pacer message
        /// </summary>
        /// <param name="message">The device message</param>
        /// <returns>An error code</returns>
        //====================================================================================================================================
        internal override ErrorCodes PreprocessExtPacer(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (message != Messages.AISCAN_EXTPACER_ENMASTER &&
                    message != Messages.AISCAN_EXTPACER_ENSLAVE &&
                        message != Messages.AISCAN_EXTPACER_DISMASTER &&
                            message != Messages.AISCAN_EXTPACER_DISSLAVE &&
                                message != Messages.AISCAN_EXTPACER_ENABLE &&
                                    message != Messages.AISCAN_EXTPACER_DISABLE)
                errorCode = ErrorCodes.InvalidPropertyValueSpecified;

            return errorCode;
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

                            channels = m_daqDevice.GetDevCapsString(capsKey, true);
                            channels = MessageTranslator.GetReflectionValue(channels);

                            int chCount = 0;

                            if (!PlatformParser.TryParse(channels, out chCount))
                                chCount = 0;

                            if (channel >= chCount || channel < 0)
                                return ErrorCodes.InvalidAiChannelSpecified;

                            capsKey = DaqComponents.AI +
                                        CurlyBraces.LEFT +
                                            channel.ToString() +
                                                CurlyBraces.RIGHT +
                                                    Constants.PROPERTY_SEPARATOR +
                                                        DevCapNames.RANGES;

                            supportedRanges = m_daqDevice.GetDevCapsString(capsKey, true);

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

                            supportedRanges = m_daqDevice.GetDevCapsString(capsKey, true);

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

        ////===============================================================================================
        ///// <summary>
        ///// Overriden to validate the per channel rate just before AISCAN:START is sent to the device
        ///// </summary>
        ///// <param name="message">The device message</param>
        ////===============================================================================================
        //internal override ErrorCodes ValidateScanRate()
        //{
        //    try
        //    {
        //        int channelCount = m_daqDevice.CriticalParams.HighAiChannel - m_daqDevice.CriticalParams.LowAiChannel + 1;
        //        double rate = m_daqDevice.CriticalParams.InputScanRate;
        //        double maxRate;

        //        if (m_daqDevice.CriticalParams.InputTransferMode == TransferMode.BurstIO)
        //        {
        //            maxRate = Math.Min(m_maxBurstThroughput / channelCount, m_maxScanRate);

        //            if (rate < m_minBurstRate || rate > maxRate)
        //                return ErrorCodes.InvalidScanRateSpecified;
        //        }
        //        else
        //        {
        //            maxRate = Math.Min(m_maxScanThroughput / channelCount, m_maxScanRate);

        //            if (rate < m_minScanRate || rate > maxRate)
        //                return ErrorCodes.InvalidScanRateSpecified;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.Assert(false, ex.Message);
        //    }

        //    return ErrorCodes.NoErrors;
        //}

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
        /// Overriden to set the default critical params resolution
        /// </summary>
        //===========================================================================================
        internal override void SetDefaultCriticalParams(DeviceInfo deviceInfo)
        {
            base.SetDefaultCriticalParams(deviceInfo);

            m_daqDevice.DriverInterface.CriticalParams.AiDataIsSigned = false;
        }
    }
}
