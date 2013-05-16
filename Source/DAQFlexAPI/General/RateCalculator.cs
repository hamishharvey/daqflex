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
        public static double GetMaxAiScanRate(DaqDevice device)
        {
            double deviceMaxRate;
            double deviceMaxThruput;
            double deviceMaxBurstRate;
            double deviceMaxBurstThruput;
            double maxRate = 0;
            int channelCount = 0;
            string xferMode;
            string sampleRateCalculationMethod;

            try
            {
                    // test if the queue component is supported by the device...
                channelCount = (int)device.SendMessage(Messages.AIQUEUE_COUNT_QUERY).ToValue();

                if (channelCount == 0)
                {
                        // if queue channel count is 0 use low/high channel...
                    int lowChannel = (int)device.SendMessage(Messages.AISCAN_LOWCHAN_QUERY).ToValue();
                    int highChannel = (int)device.SendMessage(Messages.AISCAN_HIGHCHAN_QUERY).ToValue();
                    channelCount = highChannel - lowChannel + 1;
                }
            }
            catch (Exception)
            {
                    // no queue component support...
                int lowChannel = (int)device.SendMessage(Messages.AISCAN_LOWCHAN_QUERY).ToValue();
                int highChannel = (int)device.SendMessage(Messages.AISCAN_HIGHCHAN_QUERY).ToValue();
                channelCount = highChannel - lowChannel + 1;
            }

                // get the transfer mode...
            xferMode = device.SendMessage(Messages.AISCAN_XFRMODE_QUERY).ToString();

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
#if !WindowsCE
                                maxRate = Math.Truncate(deviceMaxBurstRate / channelCount);
#else
                                maxRate = (int)(deviceMaxBurstRate / channelCount);
#endif
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
#if !WindowsCE
                                maxRate = Math.Truncate(deviceMaxRate / channelCount);
#else
                                maxRate = (int)(deviceMaxRate / channelCount);
#endif
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

#if !WindowsCE
                                maxRate = Math.Truncate(maxRate);
#else
                                maxRate = (int)maxRate;
#endif
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
#if !WindowsCE
                                maxRate = Math.Truncate(maxRate);
#else
                                maxRate = (int)maxRate;
#endif
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
                            string msg = Messages.AIQUEUE_DATARATE_QUERY;
                            msg = msg.Replace("*", i.ToString());
                            dataRate = device.SendMessage(msg).ToValue();
                            sum += (1.0 / dataRate) + 0.000640;
                        }

#if !WindowsCE
                        maxRate = Math.Truncate(1.0 / sum);
#else
                        maxRate = (int)(1.0 / sum);
#endif

                        break;
                    }

                default:
                    {
                        System.Diagnostics.Debug.Assert(false, String.Format("{0} is not supported by GetMaxAiScanRate()", sampleRateCalculationMethod));
                        break;
                    }
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
