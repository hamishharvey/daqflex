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
    //=========================================================================
    /// <summary>
    /// Contains static methods to get property values from a message 
    /// </summary>
    //=========================================================================
    internal class MessageTranslator
    {
        //========================================================================
        /// <summary>
        /// Extracts the number of samples from the message content
        /// </summary>
        /// <param name="messageStream">The message</param>
        /// <returns>The number of samples</returns>
        //========================================================================
        internal static int GetSamples(string message)
        {
            int samples = 0;

            string msg = message.ToUpper();

            if (msg.Contains(DaqProperties.SAMPLES))
            {
                int equalsIndex = msg.IndexOf('=');

                try
                {
                    samples = Convert.ToInt32(msg.Substring(equalsIndex + 1));
                }
                catch (Exception)
                {
                    samples = 0;
                }
            }

            return samples;
        }

        //========================================================================
        /// <summary>
        /// Extracts the transfer mode from the message content
        /// </summary>
        /// <param name="messageStream">The message</param>
        /// <returns>the transfer mode</returns>
        //========================================================================
        internal static TransferMode GetTransferMode(string message)
        {
            if (message.Contains(DaqProperties.XFERMODE))
            {
                string mode = message.Substring(message.IndexOf("=") + 1).Trim('\0');

                if (mode == PropertyValues.BLOCKIO)
                    return TransferMode.BlockIO;
                if (mode == PropertyValues.BURSTIO)
                    return TransferMode.BurstIO;
                if (mode == PropertyValues.SINGLEIO)
                    return TransferMode.SingleIO;
            }

            return TransferMode.Default;
        }

        //========================================================================
        /// <summary>
        /// Extracts the scan rate from the message content
        /// </summary>
        /// <param name="messageStream">The message</param>
        /// <returns>The scan rate</returns>
        //========================================================================
        internal static double GetRate(string message)
        {
            double rate = 0;

            if (message.Contains(DaqProperties.RATE))
            {
                int rateIndex = message.IndexOf("=") + 1;

                try
                {
                    rate = Convert.ToDouble(message.Substring(rateIndex, message.Length - rateIndex));
                }
                catch (Exception)
                {
                    rate = 0;
                }
            }

            return rate;
        }

        //========================================================================
        /// <summary>
        /// Extracts the channel number from the message content
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The channel number</returns>
        //========================================================================
        internal static int GetChannel(string message)
        {
            int channel = -1;

            if (message.Contains(DaqProperties.HIGHCHAN) || message.Contains(DaqProperties.LOWCHAN))
            {
                int chIndex = message.IndexOf("=") + 1;

                try
                {
                    channel = Convert.ToInt32(message.Substring(chIndex));
                }
                catch (Exception)
                {
                    channel = -1;
                }
            }
            else if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                int lIndex = message.IndexOf(CurlyBraces.LEFT);
                int rIndex = message.IndexOf(CurlyBraces.RIGHT);

                try
                {
                    channel = Convert.ToInt32(message.Substring(lIndex + 1, rIndex - (lIndex + 1)));
                }
                catch (Exception)
                {
                    channel = -1;
                }
            }

            return channel;
        }

        //========================================================================
        /// <summary>
        /// Gets the element number if the message is in the form of 
        /// AISCAN:RANGE{0/0}=BIP5V or AISCAN:RANGE{0}=BIP5V
        /// </summary>
        /// <param name="message">The messsage</param>
        /// <returns>The channel number</returns>
        //========================================================================
        internal static int GetQueueElement(string message)
        {
            int lIndex = message.IndexOf(CurlyBraces.LEFT);
            int rIndex = message.IndexOf(CurlyBraces.RIGHT);
            int fsIndex = message.IndexOf(Constants.VALUE_RESOLVER);
            string elementText;
            int elementNumber;

            if (message.Contains("AIQUEUE"))
            {
                elementText = message.Substring(lIndex + 1, rIndex - lIndex - 1);

                try
                {
                    elementNumber = Convert.ToInt32(elementText);
                }
                catch (Exception)
                {
                    elementNumber = -1;
                }
            }
            else if (lIndex > 0 && rIndex > 0)
            {
                if (fsIndex > 0)
                {
                    elementText = message.Substring(lIndex + 1, fsIndex - lIndex - 1);
                }
                else
                {
                    elementText = message.Substring(lIndex + 1, rIndex - lIndex - 1);
                }

                try
                {
                    elementNumber = Convert.ToInt32(elementText);
                }
                catch (Exception)
                {
                    elementNumber = -1;
                }
            }
            else
            {
                elementNumber = -1;
            }

            return elementNumber;
        }

        //========================================================================
        /// <summary>
        /// Gets the channel number if the message is in the form of 
        /// AISCAN:RANGE{0/0}=BIP5V or AISCAN:RANGE{0}=BIP5V
        /// </summary>
        /// <param name="message">The messsage</param>
        /// <returns>The channel number</returns>
        //========================================================================
        internal static int GetQueueChannel(string message)
        {
            int lIndex = message.IndexOf(CurlyBraces.LEFT);
            int rIndex = message.IndexOf(CurlyBraces.RIGHT);
            int fsIndex = message.IndexOf(Constants.VALUE_RESOLVER);
            string channelText = String.Empty;
            int channelNumber = -1;

            if (lIndex > 0 && rIndex > 0)
            {
                if (fsIndex > 0)
                {
                    channelText = message.Substring(fsIndex + 1, rIndex - fsIndex - 1);

                    try
                    {
                        channelNumber = Convert.ToInt32(channelText);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return channelNumber;
        }

        //========================================================================
        /// <summary>
        /// Extracts the port number form the message content
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The port number</returns>
        //========================================================================
        internal static int GetPort(string message)
        {
            int port = -1;

            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                int lIndex;
                int rIndex;

                lIndex = message.IndexOf(CurlyBraces.LEFT);

                if (message.Contains(Constants.VALUE_RESOLVER.ToString()))
                    rIndex = message.IndexOf(Constants.VALUE_RESOLVER);
                else
                    rIndex = message.IndexOf(CurlyBraces.RIGHT);

                string portPart = message.Substring(lIndex + 1, rIndex - (lIndex + 1));

                if (!PlatformParser.TryParse(portPart, out port))
                    port = -1;
            }

            return port;
        }

        //========================================================================
        /// <summary>
        /// Extracts the bit number form the message content
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The port number</returns>
        //========================================================================
        internal static double GetBit(string message)
        {
            double bit = -1;

            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                int lIndex = message.IndexOf(Constants.VALUE_RESOLVER);
                int rIndex = message.IndexOf(CurlyBraces.RIGHT);

                if (lIndex >= 0 && lIndex < rIndex)
                {
                    string bitPart = message.Substring(lIndex + 1, rIndex - (lIndex + 1));

                    if (!PlatformParser.TryParse(bitPart, out bit))
                        bit = -1;
                }
            }

            return bit;
        }

        //============================================================================
        /// <summary>
        /// Extracts the property name from property set messages
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The property name</returns>
        //============================================================================
        internal static string GetPropertyName(string message)
        {
            string property = String.Empty;

            if (message[0] != Constants.QUERY && message.Contains(Constants.EQUAL_SIGN))
            {
                int equalIndex = message.IndexOf(Constants.EQUAL_SIGN);

                property = message.Substring(0, equalIndex);
            }

            return property;
        }

        //============================================================================
        /// <summary>
        /// Extracts the property value from a query message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The property value</returns>
        //============================================================================
        internal static string GetPropertyValue(string message)
        {
            string propertyValue = String.Empty;

            if (message.Contains("="))
            {
                propertyValue = message.Substring(message.IndexOf("=") + 1);
            }

            return propertyValue;
        }

        //=======================================================================
        /// <summary>
        /// Gets the value to the right of the equal sign
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The response</returns>
        //=======================================================================
        internal static string ExtractResponse(string message)
        {
            return message.Substring(0, message.IndexOf(Constants.EQUAL_SIGN));
        }

        //=======================================================================
        /// <summary>
        /// Gets the value to the right of the percent sign
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The response</returns>
        //=======================================================================
        internal static string GetReflectionValue(string message)
        {
            int percentIndex = message.IndexOf(Constants.PERCENT);
            return message.Substring(percentIndex + 1, message.Length - percentIndex - 1);
        }

        //=====================================================================================
        /// <summary>
        /// formats a channel number to include in a message
        /// </summary>
        /// <param name="channelNumber">The channel number</param>
        /// <returns>A channel spec in the format "{N}" wher N is the channel number</returns>
        //=====================================================================================
        internal static string GetChannelSpecs(int channelNumber)
        {
            return String.Format("{0}{1}{2}", "{", channelNumber, "}");
        }

        //=====================================================================================
        /// <summary>
        /// formats a channel number to include in a message
        /// </summary>
        /// <param name="channelNumber">The channel number</param>
        /// <returns>A channel spec in the format "{N}" wher N is the channel number</returns>
        //=====================================================================================
        internal static string GetChannelSpecs(int channelNumber, int bitNumber)
        {
            return String.Format("{0}{1}{2}{3}{4}", "{", channelNumber, "/", bitNumber, "}");
        }

        //=====================================================================================
        /// <summary>
        /// Removes the property resolver part of a message
        /// e.g. message = "?AI{0}:VALUE/VOLTS" return = "?AI{0}:VALUE"
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The message without the value resolver part</returns>
        //=====================================================================================
        internal static string RemoveValueResolver(string message)
        {
            int length = message.Length;
            int startIndex = message.IndexOf(Constants.VALUE_RESOLVER);

            if (startIndex >= 0)
                return message.Remove(startIndex, length - startIndex);
            else
                return message;
        }

        //=====================================================================================
        /// <summary>
        /// Removes the property resolver part of a message
        /// e.g. message = "?AI{0}:VALUE/VOLTS" return = "?AI{0}:VALUE"
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The message without the value resolver part</returns>
        //=====================================================================================
        internal static string ConvertToCurrentCulture(string value)
        {
            string dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            value = value.Replace(Constants.DECIMAL, dec);

            return value;
        }

        //=========================================================================
        /// <summary>
        /// Converts values for internal use to be used with constants such as
        /// PropertyValues.BIP2PT5V
        /// </summary>
        /// <param name="value">The value to be converted</param>
        /// <returns>The converted value</returns>
        //=========================================================================
        internal static string ConvertToEnglish(string value)
        {
            string dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            value = value.Replace(dec, Constants.DECIMAL);

            return value;
        }

        //=========================================================================
        /// <summary>
        /// Replaces the value part of a response to a query message
        /// </summary>
        /// <param name="value">The original response</param>
        /// <returns>The new response</returns>
        //=========================================================================
        internal static string ReplaceValue(string response, string newValue)
        {
            int indexOfEquals = response.IndexOf(Constants.EQUAL_SIGN);

            if (indexOfEquals >= 0)
            {
                string newResponse;
                newResponse = response.Remove(indexOfEquals + 1, response.Length - indexOfEquals - 1);
                newResponse += newValue;
                return newResponse;
            }
            else
            {
                return response;
            }
        }

        //=========================================================================
        /// <summary>
        /// protected - static use only
        /// </summary>
        //=========================================================================
        protected MessageTranslator()
        {
        }
    }
}
