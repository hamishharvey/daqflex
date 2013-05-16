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

namespace MeasurementComputing.DAQFlex
{
    class AoComponent : IoComponent
    {
        protected const string m_aoScanReset = "AOSCAN:RESET";

        protected int m_maxCount;
        internal bool m_aoScanSupported = true;

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
            try
            {
                m_daqDevice.SendMessageDirect("?AO");
                m_channelCount = (int)m_daqDevice.DriverInterface.ReadValue();
                m_ranges = new string[m_channelCount];

                System.Diagnostics.Debug.Assert(m_channelCount > 0);
            }
            catch (Exception)
            {
                m_channelCount = 0; // need to throw
            }
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


            System.Diagnostics.Debug.Assert(false, "Invalid component for analog output");

            return ErrorCodes.InvalidMessage;
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
            if (message.Contains(DaqProperties.VALUE))
                return ProcessValueMessage(ref message);

            if (message.Contains(DaqProperties.SCALE))
                return ProcessScaleMessage(ref message);

            if (message.Contains(DaqProperties.CAL))
                return ProcessCalMessage(ref message);

            return ErrorCodes.NoErrors;
        }

        internal override void Initialize()
        {
            try
            {
                m_daqDevice.SendMessage("AOSCAN:STALL=ENABLE");
            }
            catch (Exception)
            {
                // not supported in firmware
            }
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

            if (message.Contains(DaqProperties.RANGE))
            {
                // for devices that support programmable range build a list of ranges
                // for use by CopyScanData.
            }

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

            // validate the channel numbers
            if (!message.Contains(Constants.QUERY.ToString()) &&
                (message.Contains(DaqProperties.LOWCHAN) || message.Contains(DaqProperties.HIGHCHAN)))
                return ValidateChannel(ref message);

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan status query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanStatusQuery(ref string message)
        {
            ScanState status = m_daqDevice.DriverInterface.OutputScanState;

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AOSCANSTATUS_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + status.ToString().ToUpper(), Double.NaN);

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
            long count = m_daqDevice.DriverInterface.OutputScanCount;

            //if (m_daqDevice.DriverInterface.CriticalParams.InputSampleMode == SampleMode.Finite)
            //{
            //    int totalFiniteSamplesPerChannel = m_daqDevice.DriverInterface.CriticalParams.InputScanSamples;
            //    count = Math.Min(totalFiniteSamplesPerChannel, count);
            //}

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AOSCANCOUNT_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + count.ToString(), count);

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
            long index = m_daqDevice.DriverInterface.OutputScanIndex;

            //if (m_daqDevice.DriverInterface.CriticalParams.InputSampleMode == SampleMode.Finite)
            //{
            //    int totalFiniteSamples = m_daqDevice.CriticalParams.AiChannelCount * m_daqDevice.DriverInterface.CriticalParams.InputScanSamples;
            //    index = Math.Min(totalFiniteSamples - m_daqDevice.CriticalParams.AiChannelCount, index);
            //}

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AOSCANINDEX_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + index.ToString(), index);

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
            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AOSCANBUFSIZE_QUERY.Remove(0, 1) +
                                                        Constants.EQUAL_SIGN +
                                                            m_daqDevice.DriverInterface.OutputScanBuffer.Length.ToString(),
                                                                m_daqDevice.DriverInterface.OutputScanBuffer.Length);
            m_daqDevice.SendMessageToDevice = false;

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
                    message = message.Remove(message.IndexOf(Constants.VALUE_RESOLVER), m_valueUnits.Length);
                }
                else if (message.Contains(ValueResolvers.VOLTS))
                {
                    m_calibrateData = true;
                    //m_voltsOnly = true;
                    m_scaleData = true;
                    m_valueUnits = "/VOLTS";
                    message = message.Remove(message.IndexOf(Constants.VALUE_RESOLVER), m_valueUnits.Length);
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
                        m_daqDevice.DriverInterface.SetOutputBufferSize(numberOfBytes);
                        m_daqDevice.DriverInterface.OverwritingOldScanData = false;
                        m_daqDevice.ApiResponse = new DaqResponse(message.Substring(0, equalIndex), double.NaN);
                    }
                    else
                    {
                        return ErrorCodes.InvalidOutputBufferSize;
                    }
                }
                catch (Exception)
                {
                    return ErrorCodes.InvalidOutputBufferSize;
                }
            }

            return ErrorCodes.NoErrors;
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
                int rawValue;

                if (m_scaleData == true)
                {
                    // if the data is scaled, replace the value in the message with the count value
                    string dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    message = message.Replace(".", dec);

                    double scaledValue = 0.0;

                    try
                    {
                        scaledValue = Double.Parse(message.Substring(message.IndexOf("=") + 1));
                    }
                    catch (Exception)
                    {
                        return ErrorCodes.InvalidDACValue;
                    }

                    rawValue = (int)Math.Round((((scaledValue - m_activeChannels[0].LowerLimit) / (m_activeChannels[0].UpperLimit - m_activeChannels[0].LowerLimit)) * (m_maxCount + 1)), 0);

                    int removeIndex = message.IndexOf("=") + 1;
                    message = message.Remove(removeIndex, message.Length - removeIndex);
                    message += rawValue.ToString();
                }
                else
                {
                    try
                    {
                        rawValue = Int32.Parse(message.Substring(message.IndexOf("=") + 1));
                    }
                    catch (Exception)
                    {
                        return ErrorCodes.InvalidDACValue;
                    }
                }

                if (rawValue < 0 || rawValue > m_maxCount)
                    return ErrorCodes.InvalidDACValue;
            }

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================================================================
        /// <summary>
        /// Copies scan data from the a user buffer to the driver interface's internal write buffer
        /// This override is used for DACs that have 12-bit to 16-bit resolution
        /// </summary>
        /// <param name="source">The source array (driver interface's internal write buffer)</param>
        /// <param name="destination">The destination array</param>
        /// <param name="copyIndex">The byte index to start copying from</param>
        /// <param name="samplesToCopy">Number of samples to copy per channel</param>
        //=================================================================================================================================================================
        internal override void CopyScanData(double[,] source, byte[] destination, ref int destinationIndex, int samplesToCopyPerChannel, int timeOut)
        {
            int channelCount = source.GetLength(0);
            int byteRatio = 2;
            int destinationLength = destination.Length;

            double offset = 0.0;
            double slope = 1.0;

            string supportedRanges = m_daqDevice.GetDevCapsValue("AO{0}:RANGES", false);

            if (supportedRanges.Contains("FIXED") && m_daqDevice.DriverInterface.CriticalParams.ScaleAoData)
            {
                try
                {
                    string range = MessageTranslator.GetReflectionValue(supportedRanges);
                    Range r = m_supportedRanges[range];
                    double scale = r.UpperLimit - r.LowerLimit;
                    double lsb = scale / Math.Pow(2.0, m_dataWidth);

                    if (r.LowerLimit < 0)
                        offset = -1.0 * (scale / 2.0);

                    slope = lsb;
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "Invalid fixed range");
                }
            }
            else 
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
                            ushort sourceValue;
                            double* pSrc;
                            byte* pDest = (pFixedDest + destinationIndex);

                            for (int i = 0; i < samplesToCopyPerChannel; i++)
                            {
                                pSrc = pFixedSrc + i;

                                for (int j = 0; j < channelCount; j++)
                                {
                                    sourceValue = (ushort)((*pSrc - offset) / slope);

                                    *pDest++ = (byte)(sourceValue & 0x00FF);
                                    *pDest++ = (byte)((sourceValue & 0xFF00) >> 8);

                                    if (pDest - pFixedDest > destinationLength)
                                        pDest = pFixedDest;

                                    pSrc += samplesToCopyPerChannel;
                                }
                            }

                            // update the destination byte index
                            destinationIndex += byteRatio * channelCount * samplesToCopyPerChannel;

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
    }
}
