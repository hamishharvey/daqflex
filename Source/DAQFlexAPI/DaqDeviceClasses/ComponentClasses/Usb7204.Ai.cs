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
    class Usb7204Ai : DualModeAiComponent
    {
        protected const string m_oldQueueMessage = "AISCAN:RANGE{*/%}=#";

        protected string m_extClockTypeMsg = String.Empty;
        protected Dictionary<int, int> m_queueChannels = new Dictionary<int, int>();

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal Usb7204Ai(DaqDevice daqDevice, DeviceInfo deviceInfo)
            : base(daqDevice, deviceInfo, 8)
        {
            m_previousChMode = PropertyValues.DIFF;
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
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            // add the supported ranges to the list
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP20V) + ":DIFF", new Range(20.0, -20.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V) + ":DIFF", new Range(10.0, -10.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP5V) + ":DIFF", new Range(5.0, -5.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP4V) + ":DIFF", new Range(4.0, -4.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP2PT5V) + ":DIFF", new Range(2.5, -2.5));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP2V) + ":DIFF", new Range(2.0, -2.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP1PT25V) + ":DIFF", new Range(1.25, -1.25));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP1V) + ":DIFF", new Range(1.0, -1.0));
            m_supportedRanges.Add(MessageTranslator.ConvertToCurrentCulture(PropertyValues.BIP10V) + ":SE", new Range(10.0, -10.0));

            // set default ranges
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
            // get the current channel mode
            string msg;
            string response;
            string defaultMode;
            string defaultRange;

            msg = Messages.AI_CHMODE_QUERY;
            m_daqDevice.SendMessageDirect(msg);
            response = m_daqDevice.DriverInterface.ReadStringDirect();
            defaultMode = MessageTranslator.GetPropertyValue(response);

            // set the device to SE mode 
            msg = Messages.AI_CHMODE;
            msg = Messages.InsertValue(msg, PropertyValues.SE);
            m_daqDevice.SendMessage(msg);

            double slope;
            double offset;

            for (int i = 0; i < 8; i++)
            {
                // set the range
                msg = Messages.AI_CH_RANGE;
                msg = Messages.InsertChannel(msg, i);
                msg = Messages.InsertValue(msg, PropertyValues.BIP10V);
                m_daqDevice.SendMessageDirect(msg);

                // get the slope and offset for the range
                msg = Messages.AI_CH_SLOPE_QUERY;
                msg = Messages.InsertChannel(msg, i);
                m_daqDevice.SendMessageDirect(msg);
                slope = m_daqDevice.DriverInterface.ReadValueDirect();

                msg = Messages.AI_CH_OFFSET_QUERY;
                msg = Messages.InsertChannel(msg, i);
                m_daqDevice.SendMessageDirect(msg);
                offset = m_daqDevice.DriverInterface.ReadValueDirect();

                if (slope == 0.0 || Double.IsNaN(slope))
                {
                    slope = 1.0;
                    offset = 0.0;
                }

                m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, "BIP10V:SE"), new CalCoeffs(slope, offset));
            }

            // set the device to DIFF mode
            msg = Messages.AI_CHMODE;
            msg = Messages.InsertValue(msg, PropertyValues.DIFF);
            m_daqDevice.SendMessage(msg);

            

            // read cal coefficients for each range - 8 chs, 4 ranges (diff mode)
            for (int i = 0; i < 4; i++)
            {
                msg = Messages.AI_CH_RANGE_QUERY;
                msg = Messages.InsertChannel(msg, i);
                m_daqDevice.SendMessageDirect(msg);
                response = m_daqDevice.DriverInterface.ReadStringDirect();
                defaultRange = MessageTranslator.GetPropertyValue(response);

                foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
                {
                    if (kvp.Key.Contains(PropertyValues.DIFF))
                    {
                        string range = kvp.Key.Substring(0, kvp.Key.IndexOf(Constants.PROPERTY_SEPARATOR));
                        // set the range
                        msg = Messages.AI_CH_RANGE;
                        msg = Messages.InsertChannel(msg, i);
                        msg = Messages.InsertValue(msg, range);
                        m_daqDevice.SendMessageDirect(msg);

                        // get the slope and offset for the range
                        msg = Messages.AI_CH_SLOPE_QUERY;
                        msg = Messages.InsertChannel(msg, i);
                        m_daqDevice.SendMessageDirect(msg);
                        slope = m_daqDevice.DriverInterface.ReadValueDirect();

                        msg = Messages.AI_CH_OFFSET_QUERY;
                        msg = Messages.InsertChannel(msg, i);
                        m_daqDevice.SendMessageDirect(msg);
                        offset = m_daqDevice.DriverInterface.ReadValueDirect();

                        m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, kvp.Key), new CalCoeffs(slope, offset));
                    }
                }

                // restore default range
                msg = Messages.AI_CH_RANGE;
                msg = Messages.InsertChannel(msg, i);
                msg = Messages.InsertValue(msg, defaultRange);
                m_daqDevice.SendMessageDirect(msg);
            }

            msg = Messages.AI_CHMODE;
            msg = Messages.InsertValue(msg, defaultMode);
            m_daqDevice.SendMessage(msg);
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
                messages.Add("AI:CHMODE=*");
                messages.Add("AI:SCALE=*");
                messages.Add("AI:CAL=*");
                messages.Add("?AI");
                messages.Add("?AI:CHMODE");
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
                messages.Add("AISCAN:RANGE=*");
                messages.Add("AISCAN:QUEUE=*");
                messages.Add("AISCAN:RANGE{*}=*");
                messages.Add("AISCAN:RANGE{*/*}=*");
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
                messages.Add("AISCAN:START");
                messages.Add("AISCAN:STOP");

                messages.Add("?AISCAN:XFRMODE");
                messages.Add("?AISCAN:QUEUE");
                messages.Add("?AISCAN:RANGE");
                messages.Add("?AISCAN:RANGE{*}");
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
            }
            else if (daqComponent == DaqComponents.AITRIG)
            {
                messages.Add("AITRIG:TYPE=*");
                messages.Add("?AITRIG:TYPE");
                messages.Add("AITRIG:REARM=*");
                messages.Add("?AITRIG:REARM");
            }

            return messages;
        }

        //=================================================================================================================
        /// <summary>
        /// Validates the channel mode and sets dependent properties
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes ProcessChannelModeMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // update the channel count if the channel mode has changed
            
            if (message.Contains(DaqProperties.CHMODE) && message[0] != Constants.QUERY)
            {
                errorCode = base.ProcessChannelModeMessage(ref message);

                if (errorCode == ErrorCodes.NoErrors)
                {
                    if (!message.Contains(m_previousChMode))
                        m_daqDevice.UpdateRanges = true;
                    else
                        m_daqDevice.UpdateRanges = false;

                    if (message.Contains(PropertyValues.SE))
                    {
                        m_previousChMode = PropertyValues.SE;

                        for (int i = 0; i < m_channelModes.Length; i++)
                            m_channelModes[i] = m_previousChMode;
                    }
                    else if (message.Contains(PropertyValues.DIFF))
                    {
                        m_previousChMode = PropertyValues.DIFF;

                        for (int i = 0; i < m_channelModes.Length / 2; i++)
                            m_channelModes[i] = m_previousChMode;
                    }

                    if (!PlatformParser.TryParse(m_daqDevice.GetDevCapsString("AI:CHANNELS", true), out m_channelCount))
                        m_channelCount = 0;

                    if (!PlatformParser.TryParse(m_daqDevice.GetDevCapsString("AI:MAXCOUNT", true), out m_maxCount))
                        m_maxCount = 0;

                    m_dataWidth = GetResolution((ulong)m_maxCount);

                    if (m_daqDevice.UpdateRanges)
                        m_ranges = new string[m_channelCount];

                }
            }

            return errorCode;
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

            string propertyValue = MessageTranslator.GetPropertyValue(message);

            if (propertyValue != PropertyValues.ENMSTR && propertyValue != PropertyValues.ENSLV && propertyValue != PropertyValues.ENGSLV)
                errorCode = ErrorCodes.InvalidPropertyValueSpecified;

            return errorCode;
        }

        //================================================================================
        /// <summary>
        /// Overriden to send the "?AISCAN:RANGE" message in place of the "?AIQUEUE:COUNT"
        /// message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal override ErrorCodes PreprocessQueueCountQuery(ref string message)
        {
            string msg;
            string response;
            string queueState;
            int count;

            msg = Messages.AISCAN_QUEUE_QUERY;
            m_daqDevice.SendMessageDirect(msg);
            queueState = m_daqDevice.DriverInterface.ReadStringDirect();

            msg = Messages.AISCAN_QUEUE_ENABLE;
            m_daqDevice.SendMessageDirect(msg);

            msg = Messages.AISCAN_RANGE_QUERY;
            m_daqDevice.SendMessageDirect(msg);

            count = (int)m_daqDevice.DriverInterface.ReadValueDirect();

            // create response
            response = message.Substring(1);
            response += Constants.EQUAL_SIGN;
            response += count.ToString();

            m_daqDevice.ApiResponse = new DaqResponse(response, count);

            if (queueState.Contains(PropertyValues.DISABLE))
            {
                msg = Messages.AISCAN_QUEUE_DISABLE;
                m_daqDevice.SendMessageDirect(msg);
            }

            return ErrorCodes.NoErrors;
        }

        //================================================================================
        /// <summary>
        /// Virtual method for checking the queue element
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal override ErrorCodes PreprocessQueueReset(ref string message)
        {
            ErrorCodes errorCode = base.PreprocessQueueReset(ref message);

            string msg;
            string queueState;

            if (errorCode == ErrorCodes.NoErrors)
            {
                msg = Messages.AISCAN_QUEUE_QUERY;
                m_daqDevice.SendMessageDirect(msg);
                queueState = m_daqDevice.DriverInterface.ReadStringDirect();

                msg = Messages.AISCAN_QUEUE_ENABLE;
                m_daqDevice.SendMessageDirect(msg);

                for (int i = 0; i < m_queueChannels.Count; i++)
                    m_queueChannels[i] = 0;

                msg = DaqComponents.AISCAN +
                        Constants.PROPERTY_SEPARATOR +
                            DaqProperties.QUEUE +
                                Constants.EQUAL_SIGN +
                                    DaqCommands.RESET;

                m_daqDevice.SendMessageDirect(msg);

                // create response
                m_daqDevice.ApiResponse = new DaqResponse(message, Double.NaN);

                if (queueState.Contains(PropertyValues.DISABLE))
                {
                    msg = Messages.AISCAN_QUEUE_DISABLE;
                    m_daqDevice.SendMessageDirect(msg);
                }
            }

            return errorCode;
        }


         internal virtual ErrorCodes SetQueueElementRange(int element, int channel, string range)
         {
            string msg = "AISCAN:RANGE{*/#}=" + range;
            msg = msg.Replace("*", element.ToString());
            msg = msg.Replace("#", channel.ToString());
             
            return m_daqDevice.SendMessageDirect(msg);
         }
         
        //================================================================================
        /// <summary>
        /// Virtual method for checking the queue channel
        /// Validation is performed again in ValidateQueueConfiguration just before the 
        /// AISCAN:START message is sent to the device.
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal override ErrorCodes PreprocessQueueChannel(ref string message)
        {
            ErrorCodes errorCode = base.PreprocessQueueChannel(ref message);

            int element;
            int maxChannels;
            int channel;
            string queueState;
            string msg;
            string value;
            string defaultRange;
            
            if (errorCode == ErrorCodes.NoErrors)
            {
                maxChannels = (int)m_daqDevice.GetDevCapsValue("AI:CHANNELS");

                msg = Messages.AISCAN_QUEUE_QUERY;
                m_daqDevice.SendMessageDirect(msg);
                queueState = m_daqDevice.DriverInterface.ReadStringDirect();

                msg = Messages.AISCAN_QUEUE_ENABLE;
                m_daqDevice.SendMessageDirect(msg);

                if (m_queueChannels.Count == 0)
                {
                    int queueLength = (int)m_daqDevice.GetDevCapsValue("AISCAN:QUEUELEN");

                    for (int i = 0; i < queueLength; i++)
                    {
                        m_queueChannels.Add(i, 0);
                    }
                }

                element = MessageTranslator.GetQueueElement(message);
                value = MessageTranslator.GetPropertyValue(message);
                channel = Int32.Parse(value);

                if (channel < maxChannels)
                {
                    m_queueChannels[element] = channel;

                    msg = Messages.AI_CH_RANGE_QUERY;
                    msg = Messages.InsertChannel(msg, channel);
                    m_daqDevice.SendMessageDirect(msg);
                    defaultRange = m_daqDevice.DriverInterface.ReadStringDirect();
                    defaultRange = MessageTranslator.GetPropertyValue(defaultRange);


                   ErrorCodes ec=SetQueueElementRange(element, channel, defaultRange);
                    //msg = "AISCAN:RANGE{*/#}=" + defaultRange;
                    //msg = msg.Replace("*", element.ToString());
                    //msg = msg.Replace("#", channel.ToString());
                    //m_daqDevice.SendMessageDirect(msg);

                    // create response
                    int removeIndex = message.IndexOf(Constants.EQUAL_SIGN);
                    m_daqDevice.ApiResponse = new DaqResponse(message.Remove(removeIndex, message.Length - removeIndex), Double.NaN);

                    if (queueState.Contains(PropertyValues.DISABLE))
                    {
                        msg = Messages.AISCAN_QUEUE_DISABLE;
                        m_daqDevice.SendMessageDirect(msg);
                    }
                }
                else
                {
                    errorCode = ErrorCodes.InvalidAiChannelSpecified;
                }
            }

            return errorCode;
        }

        //================================================================================
        /// <summary>
        /// Virtual method for checking the queue element
        /// Message is in the form AIQUEUE{0}:RANGE=BIP10V
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal override ErrorCodes PreprocessQueueRange(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int element;
            int channel;
            string msg;
            string range;
            string supportedRanges;

            if (m_daqDevice is Usb1208FSPlus || m_daqDevice is Usb1408FSPlus)
            {
                return base.PreprocessQueueRange(ref message);
            }

            m_daqDevice.SendMessageToDevice = false;

            // get the element inside of {*}
            element = MessageTranslator.GetQueueElement(message);

            //////////////////////////////////////////////////////////
            // validate the range
            //////////////////////////////////////////////////////////

            // get the channel this queue element pertains to
            channel = m_queueChannels[element];

            // get the channel's supported ranges
            msg = ReflectionMessages.AI_CH_RANGES;
            msg = ReflectionMessages.InsertChannel(msg, channel);

            range = MessageTranslator.GetPropertyValue(message);

            supportedRanges = m_daqDevice.GetDevCapsString(msg, true);

            // check if supported ranges contains range
            if (!supportedRanges.Contains(range))
                errorCode = ErrorCodes.InvalidAiRange;

            // update the ai queue list with the new range
            if (errorCode == ErrorCodes.NoErrors)
            {
                CheckAiQueueElement(element);
                m_aiQueueList[element].Range = range;
            }

            if (errorCode == ErrorCodes.NoErrors)
            {
                element = MessageTranslator.GetQueueElement(message);
                channel = m_queueChannels[element];
                range = MessageTranslator.GetPropertyValue(message);

                msg = DaqComponents.AISCAN +
                        Constants.PROPERTY_SEPARATOR +
                            DaqProperties.RANGE +
                                CurlyBraces.LEFT +
                                    element.ToString() +
                                        Constants.VALUE_RESOLVER +
                                            channel.ToString() +
                                                CurlyBraces.RIGHT +
                                                 Constants.EQUAL_SIGN +
                                                    range;

                m_daqDevice.SendMessageDirect(msg);

                int removeIndex = message.IndexOf(Constants.EQUAL_SIGN);
                m_daqDevice.ApiResponse = new DaqResponse(message.Remove(removeIndex, message.Length - removeIndex), Double.NaN);
            }

            return errorCode;
        }

        //=================================================================================================================
        /// <summary>
        /// Overridden to update the m_ranges array after a channel mode change
        /// </summary>
        //=================================================================================================================
        internal override void UpdateRanges()
        {
            if (m_daqDevice.UpdateRanges)
            {
                m_ranges = new string[m_channelCount];

                System.Diagnostics.Debug.Assert(m_ranges.Length == m_channelCount);

                string msg;

                for (int i = 0; i < m_channelCount; i++)
                {
                    msg = Constants.QUERY +
                            DaqComponents.AI +
                                MessageTranslator.GetChannelSpecs(i) +
                                    Constants.PROPERTY_SEPARATOR +
                                        DaqProperties.RANGE;

                    m_daqDevice.SendMessageDirect(msg);
                    m_ranges[i] = m_daqDevice.DriverInterface.ReadStringDirect();
                }

                m_daqDevice.UpdateRanges = false;
            }
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
            if (m_daqDevice.FirmwareVersion >= 2.02)
            {
                unsafe
                {
                    try
                    {
                        int xfrSize;

                        double devCapsValue = m_daqDevice.GetDevCapsValue("AISCAN:XFRSIZE");

                        if (Double.IsNaN(devCapsValue))
                            xfrSize = m_daqDevice.CriticalParams.AiDataWidth;
                        else
                            xfrSize = 8 * (int)devCapsValue;

                        if (xfrSize <= 16)
                        {
                            int workingSourceIndex = sourceCopyByteIndex;
                            int channelCount = destinationBuffer.GetLength(0);
                            int totalSamplesToCopy = channelCount * samplesToCopyPerChannel;
                            int byteRatio = m_daqDevice.CriticalParams.DataInXferSize;

                            fixed (double* pSlopesFixed = m_daqDevice.CriticalParams.AiSlopes, pOffsetsFixed = m_daqDevice.CriticalParams.AiOffsets, pDestinationBufferFixed = destinationBuffer)
                            {
                                double* pSlopes = pSlopesFixed;
                                double* pOffsets = pOffsetsFixed;
                                ushort value;

                                fixed (byte* pSourceBufferFixed = sourceBuffer)
                                {
                                    ushort* pSourceBuffer = (ushort*)(pSourceBufferFixed + sourceCopyByteIndex);
                                    double* pDestinationBuffer;

                                    int channelIndex = 0;
                                    int samplesPerChannelIndex = -1;

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

                                        if (m_channelModes[0] == PropertyValues.DIFF)
                                        {
                                            value = (ushort)((*pSourceBuffer++) >> 4);
                                        }
                                        else
                                        {
                                            if ((*pSourceBuffer & 0x8000) != 0)
                                            {
                                                value = (ushort)((*pSourceBuffer) & 0x7FF0);
                                                value >>= 3;
                                            }
                                            else
                                            {
                                                value = 0;
                                            }

                                            pSourceBuffer++;
                                        }

                                        *pDestinationBuffer = ((double)value) * (*pSlopes++) + (*pOffsets++);

                                        workingSourceIndex += byteRatio;

                                        if (workingSourceIndex >= sourceBuffer.Length)
                                        {
                                            pSourceBuffer = (ushort*)pSourceBufferFixed;
                                            workingSourceIndex = 0;
                                        }

                                        channelIndex++;
                                    }
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
            else
            {
                base.CopyScanData(sourceBuffer, destinationBuffer, ref sourceCopyByteIndex, samplesToCopyPerChannel);
            }
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

        //===============================================================================================
        /// <summary>
        /// Overriden to validate the per channel rate just before AISCAN:START is sent to the device
        /// </summary>
        /// <param name="message">The device message</param>
        //===============================================================================================
        internal override ErrorCodes ValidateScanRate()
        {
            if (m_daqDevice.FirmwareVersion > 2.04)
                return base.ValidateScanRate();

            double rate = m_daqDevice.CriticalParams.InputScanRate;

            if (rate < m_minScanRate || rate > m_maxScanRate)
                return ErrorCodes.InvalidScanRateSpecified;

            return ErrorCodes.NoErrors;
        }
    }
}
