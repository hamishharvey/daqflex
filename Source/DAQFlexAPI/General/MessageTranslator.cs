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
            int channel = 0;

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

            if (lIndex > 0 && rIndex > 0)
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

                try
                {
                    lIndex = message.IndexOf(CurlyBraces.LEFT);

                    if (message.Contains(Constants.VALUE_RESOLVER.ToString()))
                        rIndex = message.IndexOf(Constants.VALUE_RESOLVER);
                    else
                        rIndex = message.IndexOf(CurlyBraces.RIGHT);

                    port = Convert.ToInt32(message.Substring(lIndex + 1, rIndex - (lIndex + 1)));
                }
                catch (Exception)
                {
                }
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
        internal static int GetBit(string message)
        {
            int bit = -1;

            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                int lIndex;
                int rIndex;

                try
                {
                    lIndex = message.IndexOf(Constants.VALUE_RESOLVER);
                    rIndex = message.IndexOf(CurlyBraces.RIGHT);

                    bit = Convert.ToInt32(message.Substring(lIndex + 1, rIndex - (lIndex + 1)));
                }
                catch (Exception)
                {
                }
            }

            return bit;
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
