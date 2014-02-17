using System;
using System.Collections.Generic;
using System.Text;

namespace MeasurementComputing.DAQFlex
{
    internal class RateCalculator
    {
        private static double m_sampleDt;

        //==============================================================================================
        /// <summary>
        /// Returns the min scan rate
        /// </summary>
        /// <param name="device">The daq device</param>
        /// <returns>min rate</returns>
        //==============================================================================================
        public static double GetMinAiScanRate(DaqDevice device)
        {
            double minRate = device.GetDevCapsValue("AISCAN:MINSCANRATE");

            return minRate;
        }

        //==============================================================================================
        /// <summary>
        ///  calculates the max scan rate based on the method type calculation and channel count
        /// </summary>
        /// <param name="device">the daq device</param>
        /// <returns>max rate base on current configuration</returns>
        //==============================================================================================
        public static double CalculateMaxAiScanRate(DaqDevice device)
        {
            double deviceMaxRate;
            double deviceMaxThruput;
            double deviceMaxBurstRate;
            double deviceMaxBurstThruput;
            double maxRate = 0;
            int channelCount = 0;
            string xferMode;
            string sampleRateCalculationMethod;
            string msg;
            string textResponse;
            ErrorCodes msgError;

                // test if the queue component is supported by the device...
            msgError = device.SendMessageDirect(Messages.AIQUEUE_COUNT_QUERY);

            if (msgError == ErrorCodes.NoErrors)
            {
                    // get the response...
                textResponse = MessageTranslator.GetPropertyValue(device.DriverInterface.ReadStringDirect());

                    // convert to int...
                if (!PlatformParser.TryParse(textResponse, out channelCount))
                {
                        // alert the developer...
                    System.Diagnostics.Debug.Assert(false, "RateCalulator.CalculateMaxAiScanRate: The channel count could not be parsed");

                        // use a default value...
                    channelCount = 1;
                }
            }
            
            if (channelCount == 0)
            {
                int lowChannel;
                int highChannel;

                    // get the low channel number...
                device.SendMessageDirect(Messages.AISCAN_LOWCHAN_QUERY);

                    // get the reponse...
                textResponse = MessageTranslator.GetPropertyValue(device.DriverInterface.ReadStringDirect());

                if (!PlatformParser.TryParse(textResponse, out lowChannel))
                {
                        // alert the developer...
                    System.Diagnostics.Debug.Assert(false, "RateCalulator.CalculateMaxAiScanRate: The low channel could not be parsed");

                        // use a default value...
                    lowChannel = 0;
                }

                    // get the high channel number...
                device.SendMessageDirect(Messages.AISCAN_HIGHCHAN_QUERY);

                    // get the reponse...
                textResponse = MessageTranslator.GetPropertyValue(device.DriverInterface.ReadStringDirect());

                if (!PlatformParser.TryParse(textResponse, out highChannel))
                {
                        // alert the developer...
                    System.Diagnostics.Debug.Assert(false, "RateCalulator.CalculateMaxAiScanRate: The high channel could not be parsed");

                        // use a default value...
                    highChannel = 0;
                }

                channelCount = highChannel - lowChannel + 1;
            }

                // get the transfer mode...
            device.SendMessageDirect(Messages.AISCAN_XFRMODE_QUERY);

                // get the response...
            xferMode = MessageTranslator.GetPropertyValue(device.DriverInterface.ReadStringDirect());

                // get the scan rate calculation method...
            sampleRateCalculationMethod = device.GetDevCapsString("AISCAN:SCANRATECALC", true);

            switch (sampleRateCalculationMethod)
            {
                    // multiplexed...
                case (PropertyValues.METHOD1):
                    {
                        if (xferMode == PropertyValues.BURSTIO)
                        {
                                // get the max burst rate...
                            deviceMaxBurstRate = device.GetDevCapsValue("AISCAN:MAXBURSTRATE");

                                // calculate the max rate...
                            if (channelCount > 0)
                            {
                                maxRate = deviceMaxBurstRate / channelCount;
                            }
                            else
                            {
                                maxRate = deviceMaxBurstRate;
                            }
                        }
                        else
                        {
                                // get the max scan rate...
                            deviceMaxRate = device.GetDevCapsValue("AISCAN:MAXSCANRATE");

                                // calculate the max rate...
                            if (channelCount > 0)
                            {
                                maxRate = deviceMaxRate / channelCount;
                            }
                            else
                            {
                                maxRate = deviceMaxRate;
                            }
                        }

                        break;
                    }

                    // simultaneous...
                case (PropertyValues.METHOD2):
                    {
                        if (xferMode == PropertyValues.BURSTIO)
                        {
                                // get the max burst thruput...
                            deviceMaxBurstThruput = device.GetDevCapsValue("AISCAN:MAXBURSTTHRUPUT");

                                // get the max burst rate...
                            deviceMaxBurstRate = device.GetDevCapsValue("AISCAN:MAXBURSTRATE");

                                // calculate the max rate...
                            if (channelCount > 0)
                            {
                                maxRate = Math.Min(deviceMaxBurstThruput / channelCount, deviceMaxBurstRate);
                            }
                            else
                            {
                                maxRate = deviceMaxBurstThruput;
                            }
                        }
                        else
                        {
                                // get the max scan thruput...
                            deviceMaxThruput = device.GetDevCapsValue("AISCAN:MAXSCANTHRUPUT");

                                // get the max scan rate...
                            deviceMaxRate = device.GetDevCapsValue("AISCAN:MAXSCANRATE");

                                // calculate the max rate...
                            if (channelCount > 0)
                            {
                                maxRate = Math.Min(deviceMaxThruput / channelCount, deviceMaxRate);
                            }
                            else
                            {
                                maxRate = deviceMaxThruput;
                            }
                        }

                        break;
                    }

                    // multiplexed - 2408 series 
                case (PropertyValues.METHOD3):
                    {
                        double sum = 0.0;
                        double dataRate;

                        for (int i = 0; i < channelCount; i++)
                        {
                            msg = Messages.AIQUEUE_DATARATE_QUERY;
                            msg = msg.Replace("*", i.ToString());

                            device.SendMessageDirect(msg);

                            textResponse = MessageTranslator.GetPropertyValue(device.DriverInterface.ReadStringDirect());

                            if (PlatformParser.TryParse(textResponse, out dataRate))
                            {
                                    // get the data rate value...
                                dataRate = device.SendMessage(msg).ToValue();
                            }
                            else
                            {
                                    // alert the developer...
                                System.Diagnostics.Debug.Assert(false, "RateCalulator.CalculateMaxAiScanRate: The data rate could not be parsed");

                                    // use a default value...
                                dataRate = 100;
                            }

                            sum += (1.0 / dataRate) + 0.000640;
                        }

                        maxRate = 1.0 / sum;

                        break;
                    }

                default:
                    {
                        System.Diagnostics.Debug.Assert(false, String.Format("{0} is not supported by GetMaxAiScanRate()", sampleRateCalculationMethod));

                        break;
                    }
            }

                // truncate to two decimal digits...
#if WindowsCE
            maxRate = (double)((int)maxRate);
#else
            maxRate = Math.Truncate(maxRate);
#endif
                // save the caculated rate...
            device.Ai.CalculatedMaxSampleRate = maxRate;

                // query the rate...
            device.SendMessageDirect(Messages.AISCAN_RATE_QUERY);

                // read the current value...
            string response = MessageTranslator.GetPropertyValue(device.DriverInterface.ReadStringDirect());

                // try and convert the value...
            double currentRate = Double.NaN;

                // parse the value...
            if (PlatformParser.TryParse(response, out currentRate))
            {
                    // construct the set rate message...
                msg = Messages.AISCAN_RATE;

                    // inject the max rate value...
                msg = msg.Replace("#", maxRate.ToString());

                    // set the rate...
                device.SendMessageDirect(msg);

                    // query the rate...
                device.SendMessageDirect(Messages.AISCAN_RATE_QUERY);

                    // read the value...
                response = MessageTranslator.GetPropertyValue(device.DriverInterface.ReadStringDirect());

                double actualMaxRate = Double.NaN;

                if (PlatformParser.TryParse(response, out actualMaxRate))
                {
                    double returnedRate = actualMaxRate;

                        // was the value returned rounded up?...
                    while (returnedRate > maxRate)
                    {
                            // decrease it...
                        actualMaxRate = Math.Round(actualMaxRate - device.Ai.RateCalculatorDecrementCount, 2);

                        msg = Messages.AISCAN_RATE;
                        msg = msg.Replace("#", actualMaxRate.ToString());

                            // update the rate...
                        device.SendMessageDirect(msg);

                            // query the rate...
                        device.SendMessageDirect(Messages.AISCAN_RATE_QUERY);

                            // read the value...
                        response = MessageTranslator.GetPropertyValue(device.DriverInterface.ReadStringDirect());

                        if (!PlatformParser.TryParse(response, out returnedRate))
                        {
                                // alert developers...
                            System.Diagnostics.Debug.Assert(!Double.IsNaN(currentRate), "RateCalculator.GetMaxAiScanRate: The max rate could not be converted to a double");
                        }
                    }

                    
                        // set the actual max rate...
                    maxRate = actualMaxRate;

                        // save the actual max rate...
                    device.Ai.ActualDeviceMaxSampleRate = maxRate;

                        // restore the rate...
                    msg = Messages.AISCAN_RATE;

                        // is it the device max rate?...
                    if (currentRate == device.Ai.ActualDeviceMaxSampleRate)
                    {
                            // use the calculated max rate...
                        msg = msg.Replace("#", device.Ai.CalculatedMaxSampleRate.ToString());
                    }
                    else
                    {
                            // use the current rate...
                        msg = msg.Replace("#", currentRate.ToString());
                    }

                        // set the rate...
                    device.SendMessageDirect(msg);
                }
                else
                {
                        // alert developers...
                    System.Diagnostics.Debug.Assert(!Double.IsNaN(currentRate), "RateCalculator.GetMaxAiScanRate: The max rate could not be converted to a double");
                }
            }
            else
            {
                    // alert developers...
                System.Diagnostics.Debug.Assert(!Double.IsNaN(currentRate), "RateCalculator.GetMaxAiScanRate: The rate could not be converted to a double");
            }

            return maxRate;
        }

        //===================================================================================================
        /// <summary>
        /// Calculates the delta t between samples per channel
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        //===================================================================================================
        public static double GetSampleDt(DaqDevice device)
        {
            double sampleRate;
            int channelCount = 0;

            if (device.SendMessage(Messages.AISCAN_STATUS_QUERY).ToString().Contains(PropertyValues.IDLE))
            {
                try
                {
                    // test if the queue component is supported by the device...
                    channelCount = (int)device.SendMessage(Messages.AIQUEUE_COUNT_QUERY).ToValue();

                    ErrorCodes e = device.SendMessageDirect("XYZ");
                }
                catch (Exception)
                {
                    // no queue component support...
                    int lowChannel = (int)device.SendMessage(Messages.AISCAN_LOWCHAN_QUERY).ToValue();
                    int highChannel = (int)device.SendMessage(Messages.AISCAN_HIGHCHAN_QUERY).ToValue();
                    channelCount = highChannel - lowChannel + 1;
                }

                // get the current sample rate setting...
                sampleRate = device.SendMessage(Messages.AISCAN_RATE_QUERY).ToValue();

                // calculate the sample dt...
                m_sampleDt = 1.0 / sampleRate;
            }

            return m_sampleDt;
        }
    }
}
