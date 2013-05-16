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
    class Usb7204Ai : AiComponent
    {
        protected string m_extClockTypeMsg = String.Empty;

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
            m_dataWidth = 12;
            m_maxCount = (int)Math.Pow(2, m_dataWidth) - 1;

            InitializeChannelModes();
            InitializeRanges();
            SetDefaultCriticalParams(deviceInfo);

            m_previousChMode = PropertyValues.DIFF;
            m_supportsBurstIO = false;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize channel modes
        /// </summary>
        //=================================================================================================================
        internal override void InitializeChannelModes()
        {
            m_channelModes = new AiChannelMode[m_maxChannels];

            // this device is programmable - default is DIFF
            for (int i = 0; i < m_channelModes.Length; i++)
                m_channelModes[i] = AiChannelMode.Differential;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to initialize range information
        /// </summary>
        //=================================================================================================================
        internal override void InitializeRanges()
        {
            m_supportedRanges.Add(PropertyValues.BIP20V + ":DIFF", new Range(20.0, -20.0));
            m_supportedRanges.Add(PropertyValues.BIP10V + ":DIFF", new Range(10.0, -10.0));
            m_supportedRanges.Add(PropertyValues.BIP5V + ":DIFF", new Range(5.0, -5.0));
            m_supportedRanges.Add(PropertyValues.BIP4V + ":DIFF", new Range(4.0, -4.0));
            m_supportedRanges.Add(PropertyValues.BIP2PT5V + ":DIFF", new Range(2.5, -2.5));
            m_supportedRanges.Add(PropertyValues.BIP2V + ":DIFF", new Range(2.0, -2.0));
            m_supportedRanges.Add(PropertyValues.BIP1PT25V + ":DIFF", new Range(1.25, -1.25));
            m_supportedRanges.Add(PropertyValues.BIP1V + ":DIFF", new Range(1.0, -1.0));
            m_supportedRanges.Add(PropertyValues.BIP10V + ":SE", new Range(10.0, -10.0));

            // get the current channel mode
            string channelMode;
            m_daqDevice.SendMessageDirect("?AI:CHMODE");
            channelMode = m_daqDevice.DriverInterface.ReadString();

            m_daqDevice.SendMessageDirect("AI:CHMODE=SE");

            for (int i = 0; i < 8; i++)
            {
                // set the range
                m_daqDevice.SendMessageDirect(String.Format("AI{0}:RANGE=BIP10V", MessageTranslator.GetChannelSpecs(i)));

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

                m_calCoeffs.Add(String.Format("Ch{0}:{1}", i, "BIP10V:SE"), new CalCoeffs(slope, offset));
            }

            // read cal coefficients for each range - 8 chs, 4 ranges (diff mode)
            m_daqDevice.SendMessageDirect("AI:CHMODE=DIFF");
            for (int i = 0; i < 4; i++)
            {
                foreach (KeyValuePair<string, Range> kvp in m_supportedRanges)
                {
                    if (kvp.Key.Contains("DIFF"))
                    {
                        string range = kvp.Key.Substring(0, kvp.Key.IndexOf(":"));
                        // set the range
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
            }

            if (channelMode != "AI:CHMODE=DIFF")
            {
                // if the channle mode was set to SE before retrieveing the cal coeffs then restore it
                m_daqDevice.SendMessageDirect("AI:CHMODE=SE");
                //m_daqDevice.DriverInterface.CriticalParams.AiChannelMode = AiChannelMode.SingleEnded;
                m_previousChMode = "DIFF";
            }
            else
            {
                //m_daqDevice.DriverInterface.CriticalParams.AiChannelMode = AiChannelMode.Differential;
            }

            // get the ai channel count again because it could of
            // changed based on the channel mode changing
            try
            {
                m_daqDevice.SendMessageDirect("?AI");
                m_channelCount = (int)m_daqDevice.DriverInterface.ReadValue();
            }
            catch (Exception)
            {
                m_channelCount = 0;
            }

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
            m_daqDevice.DriverInterface.CriticalParams.InputConversionMode = InputConversionMode.Multiplexed;
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

            m_daqDevice.SendMessage("AISCAN:QUEUE=RESET");
            m_daqDevice.SendMessage("AISCAN:QUEUE=DISABLE");
            m_daqDevice.SendMessage("AISCAN:RANGE{0}=BIP10V");
            m_daqDevice.SendMessage("AISCAN:RANGE{1}=BIP10V");
            m_daqDevice.SendMessage("AISCAN:RANGE{2}=BIP10V");
            m_daqDevice.SendMessage("AISCAN:RANGE{3}=BIP10V");
            m_daqDevice.SendMessage("AISCAN:XFRMODE=BLOCKIO");
            m_daqDevice.SendMessage("AISCAN:CAL=ENABLE");
            m_daqDevice.SendMessage("AISCAN:EXTPACER=ENABLE/MASTER");
            m_daqDevice.SendMessage("AISCAN:TRIG=DISABLE");
            m_daqDevice.SendMessage(String.Format("AISCAN:LOWCHAN={0}", lowChannel));
            m_daqDevice.SendMessage(String.Format("AISCAN:HIGHCHAN={0}", highChannel));
            m_daqDevice.SendMessage(String.Format("AISCAN:RATE={0}", rate));
            m_daqDevice.SendMessage(String.Format("AISCAN:SAMPLES={0}", samples));
            m_daqDevice.SendMessage("AISCAN:START");

#pragma warning disable 219
            double[,] multiChannelData = m_daqDevice.ReadScanData(100, 3000);
#pragma warning restore 219

            string status;

            do
            {
                status = m_daqDevice.SendMessage("?AISCAN:STATUS").ToString();
            } while (status.Contains(PropertyValues.RUNNING));

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

        //====================================================================================
        /// <summary>
        /// Virutal method to set the clock source options
        /// </summary>
        /// <param name="clockSource">The clock source</param>
        //====================================================================================
        internal override ErrorCodes SendDeferredClockMessage(ClockSource clockSource)
        {
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
        /// Overriden to validate the Ai message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The component this message pertains to</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessAiMessage(ref string message)
        {
            ErrorCodes errorCode = base.PreprocessAiMessage(ref message);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            if (message.Contains(DaqProperties.CHMODE))
                return ValidateChannelMode(message);

            return ErrorCodes.NoErrors;
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

        //=================================================================================================================
        /// <summary>
        /// Validates the channel mode and sets dependent properties
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes ValidateChannelMode(string message)
        {
            // adjust the channel count and data width for AI and AISCAN
            if (message.Contains(DaqProperties.CHMODE) && message[0] != Constants.QUERY)
            {
                string mode = MessageTranslator.GetPropertyValue(message);
                if (mode != PropertyValues.DIFF && mode != PropertyValues.SE)
                {
                    return ErrorCodes.InvalidAiChannelMode;
                }

                if (!message.Contains(m_previousChMode))
                    m_daqDevice.UpdateRanges = true;
                else
                    m_daqDevice.UpdateRanges = false;

                if (message.Contains(PropertyValues.SE))
                {
                    m_channelCount = 8;
                    m_dataWidth = 12;
                    m_previousChMode = PropertyValues.SE;

                    for (int i = 0; i < m_channelModes.Length; i++)
                        m_channelModes[i] = AiChannelMode.SingleEnded;
                }
                else if (message.Contains(PropertyValues.DIFF))
                {
                    m_channelCount = 4;
                    m_dataWidth = 12;
                    m_previousChMode = PropertyValues.DIFF;

                    for (int i = 0; i < m_channelModes.Length / 2; i++)
                        m_channelModes[i] = AiChannelMode.Differential;
                }

                m_maxCount = (int)Math.Pow(2, m_dataWidth) - 1;

                if (m_daqDevice.UpdateRanges)
                    m_ranges = new string[m_channelCount];

                return ErrorCodes.NoErrors;
            }

            return ErrorCodes.NoErrors;
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
                    m_ranges[i] = m_daqDevice.DriverInterface.ReadString();
                }

                m_daqDevice.UpdateRanges = false;
            }
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

        internal override double GetMaxScanRate()
        {
            double maxRate;

            if (m_daqDevice.DriverInterface.CriticalParams.InputTransferMode == TransferMode.BurstIO)
                maxRate = 200000.0 / m_daqDevice.DriverInterface.CriticalParams.AiChannelCount;
            else
                maxRate = 50000.0 / m_daqDevice.DriverInterface.CriticalParams.AiChannelCount;

            return maxRate;
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
        internal override void AddChannelDevCapsKey(Dictionary<string, string> devCaps,
            string component,
            string devCapsName,
            string configuration,
            string devCapsValue)
        {
            string chCaps;

            int maxChannels = m_maxChannels;

            if (configuration == DevCapConfigurations.DIFF)
                maxChannels /= 2;

            for (int channel = 0; channel < maxChannels; channel++)
            {
                chCaps = component + "{" + channel.ToString() + "}:" + devCapsName;

                if (configuration != "ALL")
                    chCaps += ("/" + configuration);

                devCaps.Add(chCaps, devCapsValue);
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
                        int workingSourceIndex = sourceCopyByteIndex;
                        int channelCount = destinationBuffer.GetLength(0);
                        int totalSamplesToCopy = channelCount * samplesToCopyPerChannel;
                        int byteRatio = (int)Math.Ceiling((double)m_daqDevice.CriticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);

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

                                    pDestinationBuffer = pDestinationBufferFixed + (channelIndex * samplesToCopyPerChannel + samplesPerChannelIndex);

                                    if (m_channelModes[0] == AiChannelMode.Differential)
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
    }
}
