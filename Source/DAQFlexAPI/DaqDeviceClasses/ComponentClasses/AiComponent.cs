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
    internal partial class AiComponent : IoComponent
    {
        protected int m_maxCount;
        protected string m_previousChMode = String.Empty;
        protected double m_maxBurstThroughput;
        protected double m_maxBurstRate;
        protected double m_minBurstRate;
        protected int m_queueDepth = 0;
        protected double m_currentScanRate;
        protected string[] m_channelModes;
        protected Thread m_calProcessThread;
        private int m_calThreadId = 0;
        protected string m_calStatus = PropertyValues.IDLE;
        protected double m_triggerLevel = 0.0;
        protected List<AiQueue> m_aiQueueList = new List<AiQueue>();
        protected Dictionary<int, int> m_channelMappings = new Dictionary<int, int>();
        protected int m_SignBitMask = 0;
        protected Dictionary<int, double> m_queueDataRates = new Dictionary<int, double>();
        protected int m_savedSampleCount = 0;

        //=================================================================================================================
        /// <summary>
        /// Stores the properties of the AIQUEUE component as the properties are set
        /// </summary>
        //=================================================================================================================
        protected class AiQueue
        {
            private int m_channelNumber;
            private string m_range;
            private string m_channelMode;
            private double m_dataRate;

            public AiQueue()
            {
                m_channelNumber = -1;
                m_range = Constants.NOT_SET;
                m_channelMode = Constants.NOT_SET;
                m_dataRate = -1;
            }

            internal int ChannelNumber
            {
                get { return m_channelNumber; }
                set { m_channelNumber = value; }
            }

            internal string Range
            {
                get { return m_range; }
                set { m_range = value; }
            }

            internal string ChannelMode
            {
                get { return m_channelMode; }
                set { m_channelMode = value; }
            }

            internal double DataRate
            {
                get { return m_dataRate; }
                set { m_dataRate = value; }
            }
        }

        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal AiComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
            m_calibrateData = true;
            m_aiQueueList.Clear();
        }

        //=================================================================================================================
        /// <summary>
        /// Virtual method to initialize range information
        /// </summary>
        //=================================================================================================================
        internal virtual void InitializeChannelModes() 
        {
            string msg;
            m_channelModes = new string[m_maxChannels];

            string supportedModes = m_daqDevice.SendMessage("@AI{0}:CHMODES").ToString();

            if (supportedModes.Contains(PropertyValues.NOT_SUPPORTED))
            {
                msg = Messages.AI_CHMODE_QUERY;
                string chMode = m_daqDevice.SendMessage(msg).ToString();

                for (int i = 0; i < m_maxChannels; i++)
                    m_channelModes[i] = MessageTranslator.GetPropertyValue(chMode);
            }
            else
            {
                for (int i = 0; i < m_maxChannels; i++)
                {
                    msg = Messages.AI_CH_CHMODE_QUERY;
                    msg = Messages.InsertChannel(msg, i);
                    string chMode = m_daqDevice.SendMessage(msg).ToString();

                    m_channelModes[i] = MessageTranslator.GetPropertyValue(chMode);
                }
            }
        }

        //=================================================================================================================
        /// <summary>
        /// Initializes rate parameters
        /// </summary>
        //=================================================================================================================
        internal override void Initialize()
        {
            try
            {
                bool scanSupported = false;

                // Initialize the channel modes
                InitializeChannelModes();

                m_daqDevice.CriticalParams.Requires0LengthPacketForSingleIO = false;

                // get the number of channels
                m_channelCount = (int)m_daqDevice.GetDevCapsValue("AI:CHANNELS");
                m_ranges = new string[m_maxChannels];

                // get the A/D max count
                m_maxCount = (int)m_daqDevice.GetDevCapsValue("AI:MAXCOUNT");

                // set the data width (in bits) based on the max count
                m_dataWidth = GetResolution((ulong)m_maxCount);

                // get the max input scan throughput (if supported)
                m_maxScanThroughput = m_daqDevice.GetDevCapsValue("AISCAN:MAXSCANTHRUPUT");
                if (!Double.IsNaN(m_maxScanThroughput))
                {
                    scanSupported = true;

                    // get the input transfer size
                    double xferSize = m_daqDevice.GetDevCapsValue("AISCAN:XFRSIZE");

                    // set the xfer size in critical params
                    if (!Double.IsNaN(xferSize))
                        m_daqDevice.CriticalParams.DataInXferSize = (int)xferSize;
                    else
                        m_daqDevice.CriticalParams.DataInXferSize = (int)Math.Ceiling((double)m_dataWidth / (double)Constants.BITS_PER_BYTE);
                    
                    // get the default scan rate
                    m_currentScanRate = m_daqDevice.SendMessage(Messages.AISCAN_RATE_QUERY).ToValue();

                    // get the min/max input scan rates
                    m_maxScanRate = m_daqDevice.GetDevCapsValue("AISCAN:MAXSCANRATE");
                    m_minScanRate = m_daqDevice.GetDevCapsValue("AISCAN:MINSCANRATE");

                    // get the supported transfer modes
                    string xfrModes = m_daqDevice.GetDevCapsString("AISCAN:XFRMODES", false);

                    if (xfrModes.Contains(DevCapValues.BURSTIO))
                    {
                        // get the min/max rate values for BURSTIO
                        m_maxBurstThroughput = m_daqDevice.GetDevCapsValue("AISCAN:MAXBURSTTHRUPUT");
                        m_maxBurstRate = m_daqDevice.GetDevCapsValue("AISCAN:MAXBURSTRATE");
                        m_minBurstRate = m_daqDevice.GetDevCapsValue("AISCAN:MINBURSTRATE");
                    }

                    // get the supported queue length
                    //double queueDepth = m_daqDevice.SendMessage("@AISCAN:QUEUELEN").ToValue();
                    double queueDepth = m_daqDevice.GetDevCapsValue("AISCAN:QUEUELEN");

                    // if supported set the queue depth
                    if (!Double.IsNaN(queueDepth))
                        m_queueDepth = (int)queueDepth;
                }

                // initialize the ranges
                InitializeRanges();

                // read the cal coeffs from the device's eeprom
                GetCalCoefficients();

                // set default input scan critical params
                SetDefaultCriticalParams(m_deviceInfo);

                m_SignBitMask = (int)Math.Pow(2.0, (double)m_daqDevice.CriticalParams.AiDataWidth - 1);

                if (scanSupported)
                {
                    // if input scan is supported configure and run a scan
                    ConfigureScan();
                    RunScan();
                }

                // set the default data calibration flag
                if (m_daqDevice.GetDevCapsString("AI:FACCAL", false).Contains(PropertyValues.SUPPORTED))
                    m_calibrateData = m_calibrateDataClone = true;
                else
                    m_calibrateData = m_calibrateDataClone = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }
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
            m_daqDevice.DriverInterface.CriticalParams.ResendInputTriggerLevelMessage = false;
            m_daqDevice.DriverInterface.CriticalParams.CalibrateAiData = true;

            m_daqDevice.DriverInterface.CriticalParams.InputTriggerEnabled = false;
            m_daqDevice.DriverInterface.CriticalParams.InputTriggerLevel = 0;

            string response = m_daqDevice.SendMessage("@AITRIG:SRCS").ToString();

            if (response.Contains(DevCapImplementations.PROG))
            {
                m_daqDevice.SendMessageDirect(Messages.AITRIG_SRC_QUERY);
                response = m_daqDevice.DriverInterface.ReadStringDirect();
                m_daqDevice.DriverInterface.CriticalParams.InputTriggerSource = response.Substring(response.IndexOf('=') + 1);
            }
            else if (response.Contains(DevCapImplementations.FIXED))
            {
                m_daqDevice.DriverInterface.CriticalParams.InputTriggerSource = response.Substring(response.IndexOf(Constants.PERCENT)+1);
            }

            response = m_daqDevice.SendMessage("@AITRIG:TYPES").ToString();

            if (response.Contains(DevCapImplementations.PROG))
            {
                m_daqDevice.SendMessageDirect(Messages.AITRIG_TYPE_QUERY);
                response = m_daqDevice.DriverInterface.ReadStringDirect();
                m_daqDevice.DriverInterface.CriticalParams.InputTriggerType = response.Substring(response.IndexOf('=') + 1);
            }
            else if (response.Contains(DevCapImplementations.FIXED))
            {
                m_daqDevice.DriverInterface.CriticalParams.InputTriggerType = response.Substring(response.IndexOf(Constants.PERCENT) + 1);
            }

            m_daqDevice.SendMessageDirect(Messages.AI_RES_QUERY);
            response = m_daqDevice.DriverInterface.ReadStringDirect();
            m_daqDevice.DriverInterface.CriticalParams.AiDataIsSigned = response.Substring(response.IndexOf('=') + 1).Contains("S");
        }

        //=========================================================================================
        /// <summary>
        /// Let the JIT compiler compile critical methods
        /// </summary>
        //=========================================================================================
        internal override void ConfigureScan()
        {
            string msg;
            string supportedXfrModes;
            string triggerSupport;
            string triggerRearm;
            string extClock;
            int channels;
            double rate;

            // set the stall for overruns
            m_daqDevice.SendMessage(Messages.AISCAN_STALL_ENABLE);

            // disable ext trigger if supported
            triggerSupport = m_daqDevice.GetDevCapsString("AISCAN:TRIG", false);

            if (triggerSupport.Contains(DevCapImplementations.PROG))
                m_daqDevice.SendMessage(Messages.AISCAN_TRIG_DISABLE);

            // disable trigger rearm if supported
            triggerRearm = m_daqDevice.GetDevCapsString("AITRIG:REARM", false);

            if (triggerRearm.Contains(DevCapImplementations.PROG))
                m_daqDevice.SendMessage(Messages.AITRIG_REARM_DISABLE);

            // disable ext clock if supported
            extClock = m_daqDevice.GetDevCapsString("AISCAN:EXTPACER", false);

            if (extClock.Contains(PropertyValues.DISABLE))
                m_daqDevice.SendMessage(Messages.AISCAN_EXTPACER_DISABLE);
            else if (extClock.Contains(PropertyValues.ENMSTR))
                m_daqDevice.SendMessage(Messages.AISCAN_EXTPACER_ENMASTER);

            // get the supported transfer modes
            supportedXfrModes = m_daqDevice.GetDevCapsString("AISCAN:XFRMODES", false);

            if (supportedXfrModes.Contains(DevCapImplementations.PROG) && supportedXfrModes.Contains(PropertyValues.BLOCKIO))
            {
                // set xfer mode to BLOCKIO if the device supports it
                msg = Messages.AISCAN_XFRMODE;
                msg = Messages.InsertValue(msg, PropertyValues.BLOCKIO);
                m_daqDevice.SendMessage(msg);
            }

            // get the number of channles
            channels = (int)m_daqDevice.GetDevCapsValue("AI:CHANNELS");
            
            System.Diagnostics.Debug.Assert(channels > 0);

            // set the low channel
            msg = Messages.AISCAN_LOWCHAN;
            msg = Messages.InsertValue(msg, 0);
            m_daqDevice.SendMessage(msg);

            // set the hight channel
            msg = Messages.AISCAN_HIGHCHAN;
            msg = Messages.InsertValue(msg, channels - 1);
            m_daqDevice.SendMessage(msg);
            m_daqDevice.SendMessage(msg);

            // get the device's max rate
            rate = m_daqDevice.GetDevCapsValue("AISCAN:MAXSCANRATE");
            rate /= 2.0;
            rate /= channels;

            // set the rate
            msg = Messages.AISCAN_RATE;
            msg = Messages.InsertValue(msg, rate);
            m_daqDevice.SendMessage(msg);

            // set the number of samples per channel
            msg = Messages.AISCAN_SAMPLES;
            msg = Messages.InsertValue(msg, 100);
            m_daqDevice.SendMessage(msg);
        }

        //==========================================================================================
        /// <summary>
        /// Runs a scan to compile critical code
        /// </summary>
        //==========================================================================================
        internal override void RunScan()
        {
            int samples = (int)m_daqDevice.SendMessage(Messages.AISCAN_SAMPLES_QUERY).ToValue();

            m_daqDevice.SendMessage(Messages.AISCAN_START);

#pragma warning disable 219
            double[,] multiChannelData = m_daqDevice.ReadScanData(samples, 0);
#pragma warning restore 219

            m_daqDevice.SendMessage(Messages.AISCAN_STOP);

            WaitForIdle();

            ResetCriticalParams();

            m_daqDevice.DriverInterface.ResetInputScanCount();   
        }

        //===========================================================================
        /// <summary>
        /// The Ai channel modes
        /// </summary>
        //===========================================================================
        internal string[] ChannelModes
        {
            get { return m_channelModes; }
        }

        //===========================================================================
        /// <summary>
        /// A Dictionary containing the analog input calibration coefficients
        /// The key is 
        /// </summary>
        //===========================================================================
        internal Dictionary<string, CalCoeffs> AiCalCoeffs
        {
            get { return m_calCoeffs; }
        }

        protected object calStatusLock = new Object();

        //===========================================================================
        /// <summary>
        /// Value for the Ai cal status
        /// </summary>
        //===========================================================================
        internal string CalStatus
        {
            get
            {
                lock (calStatusLock)
                {
                    return m_calStatus;
                }
            }

            set
            {
                lock (calStatusLock)
                {
                    m_calStatus = value;
                }
            }
        }

        protected object calThreadIdLock = new Object();

        //===========================================================================
        /// <summary>
        /// This is the managed thread id that the self cal was started on
        /// </summary>
        //===========================================================================
        internal int CalThreadId
        {
            get
            {
                lock (calThreadIdLock)
                {
                    return m_calThreadId;
                }
            }

            set
            {
                lock (calThreadIdLock)
                {
                    m_calThreadId = value;
                }
            }
        }

        //========================================================================================================================
        /// <summary>
        /// Virtual method for returning a series of comma separated valid channels
        /// </summary>
        /// <param name="includeMode">A flag to indicate if the channel mode should be included with the channel number</param>
        /// <returns>The valid channels</returns>
        //========================================================================================================================
        internal virtual string GetValidChannels(bool includeMode)
        {
            return String.Empty;
        }

        //====================================================================================
        /// <summary>
        /// Sets up the active channels array with scaling and cal coeff info
        /// This gets called from the Ai.PreprocesAiScanMessage when the AISCAN:START message
        /// is sent.
        /// </summary>
        //====================================================================================
        protected virtual ErrorCodes SetRanges()
        {
            bool useRangeQueue = false;
            bool useNewQueueMethod = false;
            string aiConfiguration = "SE";
            string queueQuery;

            try
            {
                DaqResponse response;

                /////////////////////////////////////////////////////////////
                // first check if the input queue is supported and enabled
                /////////////////////////////////////////////////////////////

                // get the supported queue length
                response = m_daqDevice.SendMessage("@AISCAN:QUEUELEN");
                double elements = response.ToValue();

                if (!Double.IsNaN(elements) && elements > 0)
                {
                    // check if the queue is enabled
                    m_daqDevice.SendMessageDirect(Messages.AISCAN_QUEUE_QUERY);
                    queueQuery = m_daqDevice.DriverInterface.ReadStringDirect();

                    if (queueQuery.Contains(PropertyValues.ENABLE))
                        useRangeQueue = true;
                    else
                        useRangeQueue = false;

                    // now check if the queue is implemented with the old or new method
                    // the old method uses the AISCAN:RANGE{e/c} message 
                    // the new method uses the AIQUEUE{e}:CHAN, AIQUEUE{e}:CHMODE, AIQUEUE{e}:RANGE messages
                    // (e = element, c = channel)
                    if (useRangeQueue && m_daqDevice.SendMessageDirect(Messages.AIQUEUE_COUNT_QUERY) == ErrorCodes.NoErrors)
                        useNewQueueMethod = true;
                }
            }
            catch (Exception)
            {
                useRangeQueue = false;
            }

            if (useRangeQueue)
            {
                ///////////////////////////////////////////////
                // Using Range Queue
                ///////////////////////////////////////////////

                try
                {
                    if (useNewQueueMethod)
                    {
                        ///////////////////////////////////////////////
                        // using new queue programming method
                        ///////////////////////////////////////////////
                        string response;
                        string rangeValue;
                        string rangeKey;
                        string supportedChannelModes;
                        string queueConfig;

                        // get the number of elements in the queue
                        m_daqDevice.SendMessageDirect(Messages.AIQUEUE_COUNT_QUERY);
                        response = m_daqDevice.DriverInterface.ReadStringDirect();

                        int queueCount = Convert.ToInt32(MessageTranslator.GetPropertyValue(response));
                        m_activeChannels = new ActiveChannels[queueCount];

                        int channel;
                        //int rangeIndex = 0;
                        string msg;
                        string chMode;

                        if (queueCount > 0)
                        {
                            for (int i = 0; i < queueCount; i++)
                            {
                                ////////////////////////////////////////////
                                // get the channel for queue element i
                                ////////////////////////////////////////////

                                channel = m_aiQueueList[i].ChannelNumber;

                                if (channel < 0)
                                {
                                    msg = Messages.AIQUEUE_CHAN_QUERY;
                                    msg = Messages.InsertElement(msg, i);
                                    m_daqDevice.SendMessageDirect(msg);
                                    channel = (int)m_daqDevice.DriverInterface.ReadValueDirect();
                                }

                                m_activeChannels[i].ChannelNumber = channel;

                                ////////////////////////////////////////////
                                // get the channel mode for queue element i
                                ////////////////////////////////////////////

                                queueConfig = m_daqDevice.GetDevCapsString("AISCAN:QUEUECONFIG", false);

                                if (queueConfig.Contains(PropertyValues.CHMODE))
                                {
                                    chMode = m_aiQueueList[i].ChannelMode;

                                    if (chMode == PropertyValues.NOT_SET)
                                    {
                                        msg = Messages.AIQUEUE_CHMODE_QUERY;
                                        msg = Messages.InsertElement(msg, i);
                                        m_daqDevice.SendMessageDirect(msg);
                                        chMode = m_daqDevice.DriverInterface.ReadStringDirect();
                                        chMode = MessageTranslator.GetPropertyValue(chMode);
                                    }
                                }
                                else
                                {
                                    supportedChannelModes = m_daqDevice.GetDevCapsString("AI:CHMODES", false);

                                    if (supportedChannelModes.Contains(DevCapImplementations.FIXED))
                                    {
                                        chMode = m_daqDevice.GetDevCapsString("AI:CHMODES", true);
                                    }
                                    else
                                    {
                                        msg = Messages.AI_CH_CHMODE_QUERY;
                                        msg = Messages.InsertChannel(msg, channel);
                                        m_daqDevice.SendMessageDirect(msg);
                                        chMode = m_daqDevice.DriverInterface.ReadStringDirect();
                                        chMode = MessageTranslator.GetPropertyValue(chMode);
                                    }
                                }


                                ////////////////////////////////////////////
                                // get the range for queue element i
                                ////////////////////////////////////////////

                                rangeValue = m_aiQueueList[i].Range;

                                if (rangeValue == PropertyValues.NOT_SET)
                                {
                                    msg = Messages.AIQUEUE_RANGE_QUERY;
                                    msg = Messages.InsertElement(msg, i);
                                    m_daqDevice.SendMessageDirect(msg);
                                    rangeValue = m_daqDevice.DriverInterface.ReadStringDirect();
                                    rangeValue = MessageTranslator.GetPropertyValue(rangeValue);
                                }

                                // build the range key
                                rangeKey = String.Format("{0}:{1}", rangeValue, chMode);

                                m_activeChannels[i].UpperLimit = m_supportedRanges[rangeKey].UpperLimit;
                                m_activeChannels[i].LowerLimit = m_supportedRanges[rangeKey].LowerLimit;

                                if (m_calCoeffs.Count > 0)
                                {
                                    m_activeChannels[i].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", channel, rangeKey)].Slope;
                                    m_activeChannels[i].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", channel, rangeKey)].Offset;
                                }
                            }
                        }
                        else
                        {
                            return ErrorCodes.InputQueueIsEmpty;
                        }

                    }
                    else
                    {
                        ///////////////////////////////////////////////
                        // using old queue programming method
                        ///////////////////////////////////////////////

                        string response;
                        string rangeValue;

                        // get the channel mode (with the old method this query pertains to all channels)
                        m_daqDevice.SendMessageDirect(Messages.AI_CHMODE_QUERY);
                        response = m_daqDevice.DriverInterface.ReadStringDirect();

                        aiConfiguration = MessageTranslator.GetPropertyValue(response);

                        // get the number of elements in the queue
                        m_daqDevice.SendMessageDirect(Messages.AISCAN_RANGE_QUERY);
                        response = m_daqDevice.DriverInterface.ReadStringDirect();

                        int queueCount = Convert.ToInt32(MessageTranslator.GetPropertyValue(response));
                        m_activeChannels = new ActiveChannels[queueCount];

                        int channel;
                        int indexOfChannel;
                        int indexOfBrace;
                        int rangeIndex = 0;

                        if (queueCount > 0)
                        {
                            for (int i = 0; i < queueCount; i++)
                            {
                                // Continue here for the USB-7204
                                string rangeQuery = String.Format("?AISCAN:RANGE{0}", MessageTranslator.GetChannelSpecs(i));

                                m_daqDevice.SendMessageDirect(rangeQuery).ToString();
                                response = m_daqDevice.DriverInterface.ReadStringDirect();
                                rangeValue = response.Substring(response.IndexOf("=") + 1);
                                rangeValue += (":" + aiConfiguration);

                                indexOfChannel = response.IndexOf(Constants.VALUE_RESOLVER) + 1;
                                indexOfBrace = response.IndexOf(CurlyBraces.RIGHT);
                                channel = Convert.ToInt32(response.Substring(indexOfChannel, indexOfBrace - indexOfChannel));
                                m_activeChannels[rangeIndex].ChannelNumber = channel;
                                m_activeChannels[rangeIndex].UpperLimit = m_supportedRanges[rangeValue].UpperLimit;
                                m_activeChannels[rangeIndex].LowerLimit = m_supportedRanges[rangeValue].LowerLimit;

                                if (m_calCoeffs.Count > 0)
                                {
                                    m_activeChannels[rangeIndex].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", channel, rangeValue)].Slope;
                                    m_activeChannels[rangeIndex].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", channel, rangeValue)].Offset;
                                }

                                rangeIndex++;
                            }
                        }
                        else
                        {
                            return ErrorCodes.InputQueueIsEmpty;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            else
            {
                ///////////////////////////////////////////////
                // Not using Range Queue
                ///////////////////////////////////////////////

                int loChan = m_daqDevice.DriverInterface.CriticalParams.LowAiChannel;
                int hiChan = m_daqDevice.DriverInterface.CriticalParams.HighAiChannel;

                if (loChan <= hiChan)
                {
                    string msg;
                    string response;

                    m_activeChannels = new ActiveChannels[hiChan - loChan + 1];

                    int rangeIndex = 0;

                    for (int i = loChan; i <= hiChan; i++)
                    {
                        string rangeQuery = GetAiScanRange(i);

                        try
                        {
                            if (this is FixedModeAiComponent)
                            {
                                msg = "@AI:CHMODES";
                                response = m_daqDevice.SendMessage(msg).ToString();
                                aiConfiguration = response.Substring(response.IndexOf('%') + 1);
                            }
                            else
                            {
                                if (this is DualModeAiComponent)
                                {
                                    msg = Messages.AI_CHMODE_QUERY;
                                }
                                else
                                {
                                    msg = Messages.AI_CH_CHMODE_QUERY;
                                    msg = Messages.InsertChannel(msg, i);
                                }
                                response = m_daqDevice.SendMessage(msg).ToString();
                                aiConfiguration = response.Substring(response.IndexOf('=') + 1);
                            }

                            m_daqDevice.SendMessageDirect(rangeQuery);
                            response = m_daqDevice.DriverInterface.ReadStringDirect();

                            if (response.Contains(PropertyValues.MIXED))
                            {
                                msg = Messages.AI_CH_RANGE_QUERY;
                                msg = Messages.InsertChannel(msg, i);
                                m_daqDevice.SendMessageDirect(msg);
                                response = m_daqDevice.DriverInterface.ReadStringDirect();
                            }
                            
                            string rangeValue = response.Substring(response.IndexOf("=") + 1);
                            rangeValue += (":" + aiConfiguration);

                            m_activeChannels[rangeIndex].ChannelNumber = i;
                            m_activeChannels[rangeIndex].UpperLimit = m_supportedRanges[rangeValue].UpperLimit;
                            m_activeChannels[rangeIndex].LowerLimit = m_supportedRanges[rangeValue].LowerLimit;

                            if (m_calCoeffs.Count > 0)
                            {
                                m_activeChannels[rangeIndex].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", i, rangeValue)].Slope;
                                m_activeChannels[rangeIndex].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", i, rangeValue)].Offset;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Assert(false, ex.Message);
                            return ErrorCodes.ActiveChannelsNotSet;
                        }

                        rangeIndex++;
                    }
                }
            }

            // update the channel count and recalculate the bulk in xfer size
            m_daqDevice.DriverInterface.CriticalParams.AiChannelCount = m_activeChannels.Length;
            m_daqDevice.DriverInterface.CriticalParams.BulkInXferSize =
                            m_daqDevice.DriverInterface.GetOptimalInputBufferSize(m_daqDevice.DriverInterface.CriticalParams.InputScanRate);

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for initializing the channel types array
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual void InitializeAiChannelTypes()
        {
        }

        //================================================================================
        /// <summary>
        /// Virtual method for querying the AiScan range when the queue is not being used
        /// </summary>
        /// <param name="channel">The Ai channel</param>
        //================================================================================
        protected virtual string GetAiScanRange(int channel)
        {
            return String.Format("?AISCAN:RANGE{0}", MessageTranslator.GetChannelSpecs(channel));
        }

        //=======================================================================
        /// <summary>
        /// Waits for the device to report IDLE
        /// </summary>
        //=======================================================================
        protected virtual void WaitForIdle()
        {
            string status;

            do
            {
                status = m_daqDevice.SendMessage(Messages.AISCAN_STATUS_QUERY).ToString();
                System.Threading.Thread.Sleep(1);
            } while (status.Contains(PropertyValues.RUNNING));
        }

        //=======================================================================
        /// <summary>
        /// Resets the device's default values 
        /// </summary>
        //=======================================================================
        protected override void ResetCriticalParams()
        {
            m_daqDevice.SendMessage(Messages.DEV_RESET_DEFAULT);
            m_daqDevice.SendMessage(Messages.AISCAN_STALL_ENABLE);

            string xfrMode = m_daqDevice.SendMessage(Messages.AISCAN_XFRMODE_QUERY).ToString();
            m_daqDevice.CriticalParams.InputTransferMode = MessageTranslator.GetTransferMode(xfrMode);
            m_daqDevice.CriticalParams.LowAiChannel = (int)m_daqDevice.SendMessage(Messages.AISCAN_LOWCHAN_QUERY).ToValue();
            m_daqDevice.CriticalParams.HighAiChannel = (int)m_daqDevice.SendMessage(Messages.AISCAN_HIGHCHAN_QUERY).ToValue();
            m_daqDevice.CriticalParams.InputScanRate = (int)m_daqDevice.SendMessage(Messages.AISCAN_RATE_QUERY).ToValue();
            m_daqDevice.CriticalParams.InputScanSamples = (int)m_daqDevice.SendMessage(Messages.AISCAN_SAMPLES_QUERY).ToValue();
            m_daqDevice.CriticalParams.ResendInputTriggerLevelMessage = false;
        }

#region Message Processing and Validation

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageType">The component this message pertains to</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal override ErrorCodes PreprocessMessage(ref string message, string messageType)
        {
            ErrorCodes errorCode = base.PreprocessMessage(ref message, messageType);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            if (messageType == DaqComponents.AI)
            {
                return PreprocessAiMessage(ref message);
            }
            else if (messageType == DaqComponents.AISCAN)
            {
                return PreprocessAiScanMessage(ref message);
            }
            else if (messageType == DaqComponents.AITRIG)
            {
                return PreprocessAiTrigMessage(ref message);
            }
            else if (messageType == DaqComponents.AIQUEUE)
            {
                return PreProcessAiQueueMessage(ref message);
            }
            else if (messageType == DaqComponents.AICAL)
            {
                return PreprocessSelfCalMessage(ref message);
            }

            System.Diagnostics.Debug.Assert(false, "Invalid component for analog input");

            return ErrorCodes.InvalidMessage;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the Ai message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessAiMessage(ref string message)
        {
            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
            {
                ErrorCodes errorCode =  ValidateChannel(ref message);

                if (errorCode != ErrorCodes.NoErrors)
                    return errorCode;
            }

            // Process the channel mode value
            if (message.Contains(DaqProperties.CHMODE) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessChannelModeMessage(ref message);

            // Scaling is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AI:SCALE=ENABLE
            if (message.Contains(DaqProperties.SCALE))
                return ProcessScaleMessage(ref message);

            // Calibration is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AISCAN:CAL=ENABLE" or "AI:CAL=ENABLE
            if (message.Contains(DaqProperties.CAL))
                return ProcessCalMessage(ref message);

            // The DAQFlex API stores the range information for scaling data but this message is sent to the device
            // message = "AI{*}:RANGE=*"
            if (message.Contains(DaqProperties.RANGE) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessRangeMessage(ref message);

            if (message.Contains(DaqProperties.DATARATE) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessDataRateMessage(message);

            // This handles the querying of a single AI value.
            // It sets up the scaling and calibration values for the particular channel, channel mode and range
            if (message.Contains(DaqProperties.VALUE) && message.Contains(Constants.QUERY.ToString()))
                return ProcessValueGetMessage(MessageTranslator.GetChannel(message), ref message);

            if (message.Contains(DaqProperties.VALIDCHANS))
                return PreprocessValidChannels(ref message);

            if (message.Contains(DaqProperties.CHMODE) && message.Contains(Constants.EQUAL_SIGN))
                return ValidateChannelMode(message);

            if (message.Contains(DaqProperties.TEMPUNITS))
                return ProcessTempUnitsMessage(message);

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden to validate the Ai message parameters also sets the daqDevice's SendMessageToDevice flag
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessSelfCalMessage(ref string message)
        {
            // check for the cal status message
            if (message.Contains(DaqComponents.AICAL) && message.Contains(DaqProperties.STATUS))
            {
                double numericResponse;
                string status = String.Format("{0}:{1}={2}", DaqComponents.AICAL, DaqProperties.STATUS, CalStatus);
                string percentComplete = status.Substring(status.IndexOf(Constants.VALUE_RESOLVER) + 1);
                PlatformParser.TryParse(percentComplete, out numericResponse);
                m_daqDevice.ApiResponse = new DaqResponse(status, numericResponse);
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }

            if (message.Contains(DaqCommands.START))
            {
                // set the status to running here because the cal runs on a separate thread
                m_daqDevice.SendMessageToDevice = false;
                CalStatus = String.Format("{0}/{1}", PropertyValues.RUNNING, 0);
                m_daqDevice.ApiResponse = new DaqResponse(message, double.NaN);
                return StartCal();
            }

            if (message.Contains(DaqProperties.SLOPE) && message.Contains("HEX") && message.Contains(Constants.EQUAL_SIGN))
            {
                PreprocessCalSlopeMessage(ref message);
            }

            if (message.Contains(DaqProperties.OFFSET) && message.Contains("HEX") && message.Contains(Constants.EQUAL_SIGN))
            {
                PreprocessCalOffsetMessage(ref message);
            }

            // add cal status

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Checks for AISCAN messages that the DAQFlex Library needs to repsond to rather than the device.
        /// These messages are not passed down to the device
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessAiScanMessage(ref string message)
        {
            // if there's a pending input scan error reset it and return the error code
            if (m_daqDevice.PendingInputScanError != ErrorCodes.NoErrors)
            {
                ErrorCodes errorCode = m_daqDevice.PendingInputScanError;
                m_daqDevice.PendingInputScanError = ErrorCodes.NoErrors;
                return errorCode;
            }

            // message = "?AISCAN:STATUS
            if (message.Contains(APIMessages.AISCANSTATUS_QUERY))
                return ProcessScanStatusQuery(ref message);

            // The scan count is calculated by the DAQFlex library, so this message is not sent to the device
            // message = "?AISCAN:COUNT
            if (message.Contains(APIMessages.AISCANCOUNT_QUERY))
                return ProcessScanCountQuery(ref message);

            // message = "?AISCAN:INDEX
            if (message.Contains(APIMessages.AISCANINDEX_QUERY))
                return ProcessScanIndexQuery(ref message);

            // The buffer size is maintained by the DAQFlex library, so this message is not sent to the device
            // message = "?AISCAN:BUFSIZE
            if (message.Contains(APIMessages.AISCANBUFSIZE_QUERY))
                return ProcessBufSizeQuery(ref message);

            // The buffer size is maintained by the DAQFlex library, so this message is not sent to the device
            // message = "AISCAN:BUFSIZE=*"
            if (message.Contains(APIMessages.AISCANBUFSIZE))
                return ProcessInputBufferSizeMessage(ref message);

            if (message.Contains(APIMessages.AISCANBLOCKSIZE_QUERY))
                return PreprocessBlockSizeQueryMessage(ref message);

            if (message.Contains(APIMessages.AISCANBLOCKSIZE))
                return PreprocessBlockSizeMessage(ref message);

            if (message.Contains(DaqProperties.RANGE) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessRangeMessage(ref message);

            if (message.Contains(DaqProperties.SAMPLES) && message.Contains(Constants.EQUAL_SIGN))
                return ProcessSamplesMessage(ref message);

            if (message.Contains(DaqProperties.RATE)&& message.Contains(Constants.EQUAL_SIGN))
                return ProcessScanRate(ref message);

            if (message.Contains(DaqProperties.TEMPUNITS))
                return ProcessTempUnitsMessage(message);

            // The DAQFlex Library needs to set up the ai ranges for the scan
            // This message is sent to the device
            // message = "AISCAN:RANGE=*"
            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqCommands.START))
            {
                ErrorCodes errorCode;

                // set the scan type in CriticalParams for use by the driver interface
                m_daqDevice.CriticalParams.ScanType = ScanType.AnalogInput;

                // set up the ranges and active channels
                errorCode = SetRanges();

                return errorCode;
            }

            // Scaling is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AISCAN:SCALE=ENABLE"
            if (message.Contains(DaqProperties.SCALE))
                return ProcessScaleMessage(ref message);

            // Calibration is handled by the DAQFlex Library
            // This message is not sent to the device
            // Message = "AISCAN:CAL=ENABLE"
            if (message.Contains(DaqProperties.CAL))
                return ProcessCalMessage(ref message);

            // validate the channel numbers
            if (!message.Contains(Constants.QUERY.ToString()) &&
                (message.Contains(DaqProperties.LOWCHAN) || message.Contains(DaqProperties.HIGHCHAN)))
                return ValidateChannel(ref message);

            // process the transfer mode
            if (message.Contains(DaqProperties.XFERMODE))
                return PreProcessXferModeMessage(ref message);

            // process buffer overwrite query
            if (message.Contains(DaqProperties.BUFOVERWRITE) && message.Contains(Constants.QUERY.ToString()))
                return ProcessBufferOverwriteQueryMessage(ref message);

            // process buffer overwrite
            if (message.Contains(DaqProperties.BUFOVERWRITE))
                return ProcessBufferOverwriteMessage(ref message);

            // process ext pacer
            if (message[0] != Constants.QUERY && message.Contains(DaqProperties.EXTPACER))
                return PreprocessExtPacer(ref message);

            // message = "?AISCAN:TRIG
            if (message.Contains(DaqProperties.TRIG) && message.Contains(Constants.EQUAL_SIGN))
            {
                if (message.Contains(PropertyValues.ENABLE))
                    m_daqDevice.CriticalParams.InputTriggerEnabled = true;
                else
                    m_daqDevice.CriticalParams.InputTriggerEnabled = false;

                return ProcessTrigMessage(ref message);
            }

            return ErrorCodes.NoErrors;
        }

        //================================================================================
        /// <summary>
        /// Overriden to pre-process AIQUEUE messages
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal override ErrorCodes PreProcessAiQueueMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (!message.Contains(DaqProperties.COUNT) && !message.Contains(DaqCommands.CLEAR))
                errorCode = PreprocessQueueElement(ref message);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            if (message.Contains(DaqCommands.CLEAR))
                return PreprocessQueueReset(ref message);

            if (message.Contains(DaqProperties.COUNT))
                return PreprocessQueueCountQuery(ref message);

            if (message.Contains(DaqProperties.CHAN) && message.Contains(Constants.EQUAL_SIGN))
                return PreprocessQueueChannel(ref message);

            if (message.Contains(DaqProperties.CHMODE) && message.Contains(Constants.EQUAL_SIGN))
                return PreprocessQueueChannelMode(ref message);

            if (message.Contains(DaqProperties.RANGE) && message.Contains(Constants.EQUAL_SIGN))
                return PreprocessQueueRange(ref message);

            if (message.Contains(DaqProperties.DATARATE) && message.Contains(Constants.EQUAL_SIGN))
                return PreprocessQueueDataRate(ref message);

            return errorCode;
        }

        //===========================================================================================
        /// <summary>
        /// Overriden to set the RESET command
        /// Other overrides should call this at the beginning or end of the override
        /// </summary>
        //===========================================================================================
        internal override void BeginInputScan()
        {
            m_daqDevice.CriticalParams.NumberOfSamplesForSingleIO = m_daqDevice.CriticalParams.AiChannelCount;


            if (m_daqDevice.DriverInterface.InputScanStatus != ScanState.Running)
                m_daqDevice.SendMessageDirect(Messages.AISCAN_RESET);
        }

        //================================================================================
        /// <summary>
        /// Virtual method for checking the queue element
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal virtual ErrorCodes PreprocessQueueReset(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_aiQueueList.Clear();

            m_queueDataRates.Clear();

            return errorCode;
        }

        //================================================================================
        /// <summary>
        /// Virtual method for preprocessing the "?AIQUEUE:COUNT" message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal virtual ErrorCodes PreprocessQueueCountQuery(ref string message)
        {
            return ErrorCodes.NoErrors;
        }

        //================================================================================
        /// <summary>
        /// Virtual method for checking the queue element
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal virtual ErrorCodes PreprocessQueueElement(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int queueLength;
            int element;

            queueLength = (int)m_daqDevice.GetDevCapsValue("AISCAN:QUEUELEN");

            element = MessageTranslator.GetQueueElement(message);

            if (element < 0 || element >= queueLength)
            {
                errorCode = ErrorCodes.InvalidQueueElement;
            }


            return errorCode;
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
        internal virtual ErrorCodes PreprocessQueueChannel(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;
            string channel;
            int channelNumber;

            // validate the channel
            channel = MessageTranslator.GetPropertyValue(message);

            bool parsed = PlatformParser.TryParse(channel, out channelNumber);

            if (parsed)
            {
                if (channelNumber < 0 || channelNumber >= m_maxChannels)
                    errorCode = ErrorCodes.InvalidAiChannelSpecified;
            }
            else
            {
                errorCode = ErrorCodes.InvalidAiChannelSpecified;
            }

            if (errorCode == ErrorCodes.NoErrors)
            {
                int element = MessageTranslator.GetQueueElement(message);

                CheckAiQueueElement(element);
                m_aiQueueList[element].ChannelNumber = channelNumber;
            }

            return errorCode;
        }


        //================================================================================
        /// <summary>
        /// Virtual method for checking the queue channel mode
        /// Message is in the form AIQUEUE{0}:CHMODE=SE
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //================================================================================
        internal virtual ErrorCodes PreprocessQueueChannelMode(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int element;
            string msg;
            int channel;
            string channelMode;
            string supportedChannelModes;

            // get the element inside of {*}
            element = MessageTranslator.GetQueueElement(message);

            //////////////////////////////////////////////////////////
            // validate the channel mode
            //////////////////////////////////////////////////////////

            // get the channel this queue element pertains to
            msg = Messages.AIQUEUE_CHAN_QUERY;
            msg = Messages.InsertElement(msg, element);
            m_daqDevice.SendMessageDirect(msg);
            channel = (int)m_daqDevice.DriverInterface.ReadValueDirect();

            // get the channel's supported modes
            msg = ReflectionMessages.AI_CH_CHMODES;
            msg = ReflectionMessages.InsertChannel(msg, channel);

            channelMode = MessageTranslator.GetPropertyValue(message);

            supportedChannelModes = m_daqDevice.GetDevCapsString(msg, true);

            // check if supported channel modes contains channel mode
            if (!supportedChannelModes.Contains(channelMode))
                errorCode = ErrorCodes.InvalidAiChannelMode;

            // update the ai queue list with the new range
            if (errorCode == ErrorCodes.NoErrors)
            {
                CheckAiQueueElement(element);
                m_aiQueueList[element].ChannelMode = channelMode;
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
        internal virtual ErrorCodes PreprocessQueueRange(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int element;
            string msg;
            int channel;
            string range;
            string supportedRanges;

            // get the element inside of {*}
            element = MessageTranslator.GetQueueElement(message);

            //////////////////////////////////////////////////////////
            // validate the range
            //////////////////////////////////////////////////////////

            // get the channel this queue element pertains to
            msg = Messages.AIQUEUE_CHAN_QUERY;
            msg = Messages.InsertElement(msg, element);
            m_daqDevice.SendMessageDirect(msg);
            channel = (int)m_daqDevice.DriverInterface.ReadValueDirect();

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
        internal virtual ErrorCodes PreprocessQueueDataRate(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int element;
            string msg;
            string dataRate;
            string supporteDataRates;

            // get the element inside of {*}
            element = MessageTranslator.GetQueueElement(message);

            //////////////////////////////////////////////////////////
            // validate the data rate
            //////////////////////////////////////////////////////////

            // get the channel's supported ranges
            msg = ReflectionMessages.AI_DATARATES;
            supporteDataRates = m_daqDevice.GetDevCapsString(msg, true);

            dataRate = MessageTranslator.GetPropertyValue(message);

            // check if supported ranges contains range
            if (!supporteDataRates.Contains(dataRate))
                errorCode = ErrorCodes.InvalidDataRate;

            // update the ai queue list with the new range
            if (errorCode == ErrorCodes.NoErrors)
            {
                CheckAiQueueElement(element);
                m_aiQueueList[element].DataRate = Convert.ToDouble(dataRate);
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Overriden to check for ext pacer support
        /// </summary>
        /// <param name="message">The device message</param>
        /// <returns>An error code</returns>
        //====================================================================================
        internal override ErrorCodes PreprocessExtPacer(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // call base method to validate values
            errorCode = base.PreprocessExtPacer(ref message);

            // if no errors, then set critical params
            if (errorCode == ErrorCodes.NoErrors)
            {
                if (message.Contains(PropertyValues.DISABLE))
                    m_daqDevice.CriticalParams.AiExtPacer = false;
                else
                    m_daqDevice.CriticalParams.AiExtPacer = true;
            }

            return errorCode;
        }

        //=================================================================================================================
        /// <summary>
        /// Checks for AITRIG messages that the DAQFlex Library needs to repsond to rather than the device.
        /// These messages are not passed down to the device
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessAiTrigMessage(ref string message)
        {
            if (message.Contains(DaqProperties.TRIGSCR))
                return PreprocessAiTrigSrcMessage(ref message);

            if (message.Contains(DaqProperties.TRIGTYPE))
                return PreprocessAiTrigTypeMessage(ref message);

            if (message.Contains(DaqProperties.TRIGLEVEL))
                return PreprocessAiTrigLevelMessage(ref message);

            if (message.Contains(DaqProperties.TRIGREARM))
                return PreprocessAiTrigRearmMessage(ref message);

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

            response = m_daqDevice.GetDeviceCapability("@AI:CHANNELS");

            if (response.ToString() == PropertyValues.NOT_SUPPORTED)
            {
                return ErrorCodes.InvalidMessage;
            }
            else
            {
                if (message.Contains(DaqProperties.SLOPE) || message.Contains(DaqProperties.OFFSET))
                {
                    if (channel < 0 || channel > m_maxChannels)
                        return ErrorCodes.InvalidAiChannelSpecified;
                }
                else if (message.Contains(Constants.QUERY.ToString()))
                {
                    if (channel < 0 || channel > m_maxChannels)
                        return ErrorCodes.InvalidAiChannelSpecified;
                }
                else if (channel < 0 || channel > (int)response.ToValue() - 1)
                {
                    return ErrorCodes.InvalidAiChannelSpecified;
                }

                if (message.Contains(DaqProperties.LOWCHAN))
                    m_daqDevice.DriverInterface.CriticalParams.LowAiChannel = channel;
                else if (message.Contains(DaqProperties.HIGHCHAN))
                    m_daqDevice.DriverInterface.CriticalParams.HighAiChannel = channel;
            }

            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Validates the channel mode and sets dependent properties
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes ValidateChannelMode(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //=================================================================================================================
        /// <summary>
        /// Virtual method to process the ?AI:VALIDCHANS message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //=================================================================================================================
        internal virtual ErrorCodes PreprocessValidChannels(ref string message)
        {
            string validChannels;
            string response;

            m_daqDevice.SendMessageToDevice = false;

            // get the valid channels
            if (message.Contains(PropertyValues.CHMODE))
                validChannels = GetValidChannels(true);
            else
                validChannels = GetValidChannels(false);

            // create the response
            response = String.Format("{0}={1}", message.Substring(1), validChannels);

            m_daqDevice.ApiResponse = new DaqResponse(response, Double.NaN);

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Validates the cal message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessCalMessage(ref string message)
        {
            if (message.Contains(Messages.AI_ADCAL_START) || message.Contains(Messages.AI_ADCAL_STATUS_QUERY))
                return ErrorCodes.NoErrors;

            // The CAL setting is applied to all channels
            if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
                return ErrorCodes.InvalidMessage;

            if (m_daqDevice.GetDevCapsString("AI:FACCAL", false).Contains(PropertyValues.NOT_SUPPORTED))
                return ErrorCodes.InvalidMessage;

            if (message[0] == Constants.QUERY)
            {
                m_daqDevice.ApiResponse = new DaqResponse(message.Remove(0, 1) + "=" + (m_calibrateData ? PropertyValues.ENABLE : PropertyValues.DISABLE).ToString(), Double.NaN);
                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
            else
            {
                if (message.Contains(PropertyValues.ENABLE))
                {
                    m_calibrateData = m_calibrateDataClone = true;
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);

                    m_daqDevice.CriticalParams.CalibrateAiData = true;
                }
                else if (message.Contains(PropertyValues.DISABLE))
                {
                    m_calibrateData = m_calibrateDataClone = false;
                    m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);

                    m_daqDevice.CriticalParams.CalibrateAiData = false;
                }
                else
                {
                    return ErrorCodes.InvalidMessage;
                }


                RecalculateTriggerLevel();

                m_daqDevice.SendMessageToDevice = false;
                return ErrorCodes.NoErrors;
            }
        }

        //===========================================================================================
        /// <summary>
        /// Validates the trig message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessTrigMessage(ref string message)
        {
            m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);
            //m_daqDevice.SendMessageToDevice = false;
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
                        ///////////////////////////////////////////////////////////////////////////////////
                        // Message is in the form AISCAN:RANGE{0}=BIP10V or AISCAN:RANGE{0/0}=BIP10V
                        ///////////////////////////////////////////////////////////////////////////////////

                        // process a gain queue
                        int channel = MessageTranslator.GetQueueChannel(message);
                        int element = MessageTranslator.GetQueueElement(message);

                        if (channel >= 0)
                        {
                            capsKey = DaqComponents.AI +
                                        Constants.PROPERTY_SEPARATOR +
                                            DevCapNames.CHANNELS;

                            channels = m_daqDevice.GetDevCapsString(capsKey, true);
                            channels = MessageTranslator.GetReflectionValue(channels);

                            int chCount = 0;

                            PlatformParser.TryParse(channels, out chCount);

                            if (channel >= chCount)
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

                        if (element >= 0)
                        {
                            if (element > (m_queueDepth - 1))
                                return ErrorCodes.GainQueueDepthExceeded;
                        }
                    }
                    else
                    {
                        //////////////////////////////////////////////////////
                        // Messsage is in the form AISCAN:RANGE=BIP10V
                        //////////////////////////////////////////////////////

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
                // Messsage is in the form AI{0}:RANGE=BIP10V or AI:RANGE=BIP10V

                int channel;
                
                if (message.Contains(CurlyBraces.LEFT.ToString()) && message.Contains(CurlyBraces.RIGHT.ToString()))
                    channel = MessageTranslator.GetChannel(message);
                else
                    channel = 0; // all channels should be set to the same ch mode

                try
                {
                    if (channel >= 0)
                    {
                        string capsKey = DaqComponents.AI +
                                            CurlyBraces.LEFT +
                                                channel.ToString() +
                                                    CurlyBraces.RIGHT +
                                                        Constants.PROPERTY_SEPARATOR +
                                                            DevCapNames.RANGES;

                        supportedRanges = m_daqDevice.GetDevCapsString(capsKey, true);

                        m_ranges[channel] = message;

                        if (!supportedRanges.Contains(rangeValue))
                            return ErrorCodes.InvalidAiRange;
                    }
                    else
                    {
                        return ErrorCodes.InvalidAiRange;
                    }
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "Invalid range");
                    return ErrorCodes.InvalidAiRange;
                }
            }

            RecalculateTriggerLevel();

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Processes the SAMPLES message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessSamplesMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            string samples = MessageTranslator.GetPropertyValue(message);
            int sampleCount;

            bool parsed = PlatformParser.TryParse(samples, out sampleCount);

            if (!parsed || sampleCount < 0)
                errorCode = ErrorCodes.InvalidPropertyValueSpecified;

            return errorCode;
        }


        //===========================================================================================
        /// <summary>
        /// Validates the Ai Value message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes ProcessValueGetMessage(int channelNumber, ref string message)
        {
            if (channelNumber >= 0 && channelNumber < m_channelCount)
            {
                m_valueUnits = String.Empty;

                if (message.Contains(ValueResolvers.RAW))
                {
                    // set the clones for restoring original flags after SendMessage is complete
                    m_calibrateDataClone = m_calibrateData;
                    m_scaleDataClone = m_scaleData;

                    // set original flags
                    m_calibrateData = false;
                    m_scaleData = false;
                    m_valueUnits = Constants.VALUE_RESOLVER + ValueResolvers.RAW;
                    message = MessageTranslator.RemoveValueResolver(message);
                }
                else if (message.Contains(ValueResolvers.VOLTS))
                {
                    // set the clones for restoring original flags after SendMessage is complete
                    m_calibrateDataClone = m_calibrateData;
                    m_scaleDataClone = m_scaleData;

                    // set original flags
                    if (m_daqDevice.GetDevCapsString("AI:FACCAL", false).Contains(DevCapValues.SUPPORTED))
                        m_calibrateData = true;

                    m_scaleData = true;
                    m_valueUnits = Constants.VALUE_RESOLVER + ValueResolvers.VOLTS;
                    message = MessageTranslator.RemoveValueResolver(message);
                }
                else if (message.Contains("?AI{0}:VALUE/"))
                {
                    return ErrorCodes.InvalidMessage;
                }
                else
                {
                    // set the clones for restoring original flags after SendMessage is complete
                    m_calibrateDataClone = m_calibrateData;
                    m_scaleDataClone = m_scaleData;
                }

                m_activeChannels = new ActiveChannels[1];
                string rangeKey = m_ranges[channelNumber].Substring(m_ranges[channelNumber].IndexOf(Constants.EQUAL_SIGN) + 1);

                rangeKey += String.Format(":{0}", m_channelModes[channelNumber]);

                if (channelNumber < m_channelCount)
                {
                    m_activeChannels[0].ChannelNumber = channelNumber;
                    m_activeChannels[0].UpperLimit = m_supportedRanges[rangeKey].UpperLimit;
                    m_activeChannels[0].LowerLimit = m_supportedRanges[rangeKey].LowerLimit;

                    if (m_calCoeffs.Count > 0)
                    {
                        m_activeChannels[0].CalOffset = m_calCoeffs[String.Format("Ch{0}:{1}", channelNumber, rangeKey)].Offset;
                        m_activeChannels[0].CalSlope = m_calCoeffs[String.Format("Ch{0}:{1}", channelNumber, rangeKey)].Slope;
                    }

                    return ErrorCodes.NoErrors;
                }
                else
                {
                    return ErrorCodes.NoErrors;
                }
            }
            else
            {
                // let the device respond with invalid command
                return ErrorCodes.NoErrors;
            }
        }

        //====================================================================================================
        /// <summary>
        /// Validates the channel mode value
        /// </summary>
        /// <param name="message">The device message</param>
        /// <returns>The error code</returns>
        //====================================================================================================
        internal virtual ErrorCodes ProcessChannelModeMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            string msg;
            string supportedModes = m_daqDevice.GetDevCapsString(ReflectionMessages.AI_CHMODES, true);
            int channel = MessageTranslator.GetChannel(message);

            if (!supportedModes.Contains(PropertyValues.NOT_SUPPORTED))
            {
                string modeValue = MessageTranslator.GetPropertyValue(message);

                if (supportedModes.Contains(DevCapValues.MIXED))
                {
                    // must get modes per channel
                    msg = ReflectionMessages.AI_CH_CHMODES;

                    if (channel >= 0)
                        msg = ReflectionMessages.InsertChannel(msg, channel);
                    else
                        msg = ReflectionMessages.InsertChannel(msg, 0);

                    supportedModes = m_daqDevice.GetDevCapsString(msg, true);
                }

                if (!supportedModes.Contains(modeValue))
                    errorCode = ErrorCodes.InvalidAiChannelMode;

                if (errorCode == ErrorCodes.NoErrors)
                {
                    // store the configuration
                    modeValue = MessageTranslator.GetPropertyValue(message);

                    if (channel >= 0)
                    {
                        m_channelModes[channel] = modeValue;
                    }
                    else
                    {
                        for (int i = 0; i < m_maxChannels; i++)
                        {
                            // set all channels to the specified mode
                            if (i < (m_maxChannels / 2))
                                m_channelModes[i] = modeValue;
                            else
                                m_channelModes[i] = PropertyValues.SE;
                        }
                    }
                }
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan status query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanStatusQuery(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            ScanState status;

            m_daqDevice.SendMessageToDevice = false;

            status = m_daqDevice.DriverInterface.InputScanStatus;

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AISCANSTATUS_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + status.ToString().ToUpper(), Double.NaN);

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan count query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanCountQuery(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_daqDevice.SendMessageToDevice = false;

            ulong count = m_daqDevice.DriverInterface.InputScanCount;

            if (m_daqDevice.DriverInterface.CriticalParams.InputSampleMode == SampleMode.Finite)
            {
                int totalFiniteSamplesPerChannel = m_daqDevice.DriverInterface.CriticalParams.InputScanSamples;
                count = (ulong)Math.Min(totalFiniteSamplesPerChannel, (double)count);
            }

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AISCANCOUNT_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + count.ToString(), count);

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan index query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanIndexQuery(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_daqDevice.SendMessageToDevice = false;

            long index = m_daqDevice.DriverInterface.InputScanIndex;

            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AISCANINDEX_QUERY.Remove(0, 1) + Constants.EQUAL_SIGN + index.ToString(), index);

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a scan rate
        /// This simply checks the rate against the max rate of the device without taking
        /// into consideration the number of channels. This is validated again when Start is
        /// called.
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessScanRate(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            try
            {
                double rate;
                PlatformParser.TryParse(MessageTranslator.GetPropertyValue(message), out rate);

                if (m_daqDevice.CriticalParams.InputTransferMode == TransferMode.BurstIO)
                {
                    if (rate > m_maxBurstThroughput || rate < m_minBurstRate)
                        errorCode = ErrorCodes.InvalidScanRateSpecified;
                }
                else
                {
                    if (rate > m_maxScanRate || rate < m_minScanRate)
                        errorCode = ErrorCodes.InvalidScanRateSpecified;
                }
            }
            catch (Exception)
            {
                errorCode = ErrorCodes.InvalidScanRateSpecified;
            }
           
            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a buffer size query message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessBufSizeQuery(ref string message)
        {
            m_daqDevice.ApiResponse = new DaqResponse(APIMessages.AISCANBUFSIZE_QUERY.Remove(0, 1) + 
                                                        Constants.EQUAL_SIGN + 
                                                            m_daqDevice.DriverInterface.InputScanBuffer.Length.ToString(),
                                                                m_daqDevice.DriverInterface.InputScanBuffer.Length);
            m_daqDevice.SendMessageToDevice = false;

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a buffer size message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessInputBufferSizeMessage(ref string message)
        {
            int equalIndex = message.IndexOf(Constants.EQUAL_SIGN);

            m_daqDevice.SendMessageToDevice = false;

            if (equalIndex >= 0)
            {
                try
                {
                    int numberOfBytes = Convert.ToInt32(message.Substring(equalIndex + 1));
                    m_daqDevice.ApiMessageError = m_daqDevice.DriverInterface.SetInputBufferSize(numberOfBytes);
                    m_daqDevice.ApiResponse = new DaqResponse(message.Substring(0, equalIndex), double.NaN);
                }
                catch (Exception)
                {
                    m_daqDevice.ApiMessageError = ErrorCodes.InvalidMessage;
                }
            }

            return m_daqDevice.ApiMessageError;
        }

        //====================================================================================
        /// <summary>
        /// Overridden method for processing a data rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ProcessDataRateMessage(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a temp units message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessTempUnitsMessage(string message)
        {
            return ErrorCodes.NoErrors;
        }

        //===============================================================================================
        /// <summary>
        /// Overriden to validate the data rate just before AISCAN:START is sent to the device
        /// </summary>
        /// <param name="message">The device message</param>
        //===============================================================================================
        internal override ErrorCodes ValidateDataRate()
        {
            return ErrorCodes.NoErrors;
        }

        //===============================================================================================
        /// <summary>
        /// Virtual method to validate the queue configuration just before AISCAN:START is sent to the device
        /// </summary>
        /// <returns></returns>
        //===============================================================================================
        internal virtual ErrorCodes ValidateQueueConfiguration()
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;
            string validChannels;
            string queueSequence;

            if (m_daqDevice.CriticalParams.AiQueueEnabled)
            {
                //////////////////////////////////////////////////////////
                // check valid channels
                //////////////////////////////////////////////////////////
                validChannels = GetValidChannels(false);

                foreach (AiQueue aiq in m_aiQueueList)
                {
                    if (!validChannels.Contains(aiq.ChannelNumber.ToString()))
                    {
                        return ErrorCodes.InvalidAiChannelSpecified;
                    }
                }

                if (errorCode == ErrorCodes.NoErrors)
                {
                    queueSequence = m_daqDevice.GetDevCapsString("AISCAN:QUEUESEQ", true);

                    //**********************************************************
                    // check if duplicate channels are supported
                    //**********************************************************

                    if (!queueSequence.Contains(PropertyValues.DUPLICATE))
                    {
                        for (int i = 0; i < m_aiQueueList.Count; i++)
                        {
                            AiQueue aiq = m_aiQueueList[i];

                            for (int j = 0; j < m_aiQueueList.Count; j++)
                            {
                                if (j != i)
                                {
                                    if (m_aiQueueList[j].ChannelNumber == aiq.ChannelNumber)
                                    {
                                        return ErrorCodes.DuplicateChannelsNotSupportedInQueue;
                                    }
                                }
                            }
                        }
                    }

                    //**********************************************************
                    // check if channels must be in ascending, adjacent order
                    //**********************************************************

                    if (queueSequence.Contains(PropertyValues.ADJACENT) && queueSequence.Contains(PropertyValues.ASCENDING))
                    {
                        for (int i = 0; i < m_aiQueueList.Count; i++)
                        {
                            if (i > 0)
                            {
                                if (m_aiQueueList[i].ChannelNumber - m_aiQueueList[i - 1].ChannelNumber != 1)
                                {
                                    return ErrorCodes.NonAdjacentNonAscendingChannelsNotSupportedInQueue;
                                }
                            }
                        }
                    }

                    //**********************************************************
                    // check if channels must be in ascending order
                    //**********************************************************

                    if (queueSequence.Contains(PropertyValues.ASCENDING) && !queueSequence.Contains(PropertyValues.ADJACENT))
                    {
                        for (int i = 0; i < m_aiQueueList.Count; i++)
                        {
                            if (i > 0)
                            {
                                if (m_aiQueueList[i].ChannelNumber < m_aiQueueList[i - 1].ChannelNumber)
                                {
                                    return ErrorCodes.NonAscendingChannelsNotSupportedInQueue;
                                }
                            }
                        }
                    }

                }
            }

            return errorCode;
        }

        //===============================================================================================
        /// <summary>
        /// Overriden to validate the per channel rate just before AISCAN:START is sent to the device
        /// </summary>
        /// <param name="message">The device message</param>
        //===============================================================================================
        internal override ErrorCodes ValidateScanRate()
        {
            double maxRate = double.MaxValue;
            int channelCount = m_daqDevice.CriticalParams.AiChannelCount;

            try
            {
                double rate = m_daqDevice.CriticalParams.InputScanRate;

                if (m_daqDevice.CriticalParams.InputTransferMode == TransferMode.BurstIO)
                {
                    maxRate = m_maxBurstRate / channelCount;

                    if (rate < m_minBurstRate || rate > maxRate)
                        return ErrorCodes.InvalidScanRateSpecified;
                }
                else
                {
                    maxRate = m_maxScanThroughput / channelCount;

                    if (rate < m_minScanRate || rate > maxRate)
                        return ErrorCodes.InvalidScanRateSpecified;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing a rate message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes ValidateSampleCount()
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (m_daqDevice.CriticalParams.InputTransferMode == TransferMode.BurstIO)
            {
                int fifoSize = (int)m_daqDevice.GetDevCapsValue("AISCAN:FIFOSIZE");

                if (m_daqDevice.CriticalParams.InputScanSamples == 0 || m_daqDevice.CriticalParams.AiChannelCount * m_daqDevice.CriticalParams.InputScanSamples > fifoSize)
                    errorCode = ErrorCodes.InvalidSampleCountForBurstIo;
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing the xfer mode message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal override ErrorCodes PreProcessXferModeMessage(ref string message)
        {
            if (!ValidateDaqFeature(message, "AISCAN:XFRMODES"))
                return ErrorCodes.InvalidInputScanXferMode;
            else
                return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing the buffer overwrite message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessBufferOverwriteMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_daqDevice.SendMessageToDevice = false;

            string value = MessageTranslator.GetPropertyValue(message);

            if (value != PropertyValues.ENABLE && value != PropertyValues.DISABLE)
            {
                errorCode = ErrorCodes.InvalidBufferOverwrite;
            }
            else if (value == PropertyValues.ENABLE)
            {
                m_daqDevice.CriticalParams.InputScanOverwrite = true;
                m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);

            }
            else
            {
                m_daqDevice.CriticalParams.InputScanOverwrite = false;
                m_daqDevice.ApiResponse = new DaqResponse(MessageTranslator.ExtractResponse(message), double.NaN);
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing the buffer overwrite message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes ProcessBufferOverwriteQueryMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_daqDevice.SendMessageToDevice = false;

            if (message != String.Format("{0}{1}{2}{3}", Constants.QUERY, DaqComponents.AISCAN, Constants.PROPERTY_SEPARATOR, DaqProperties.BUFOVERWRITE))
                errorCode = ErrorCodes.InvalidMessage;

            if (errorCode == ErrorCodes.NoErrors)
            {
                string value;
                string response;

                if (m_daqDevice.CriticalParams.InputScanOverwrite)
                    value = "ENABLE";
                else
                    value = "DISABLE";

                // remove the query
                response = message.Substring(1);

                // append the value
                response += (Constants.EQUAL_SIGN + value);

                m_daqDevice.ApiResponse = new DaqResponse(response, double.NaN);
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// This allows the user to override the automatic setting of the bulk transfer size
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The error code</returns>
        //====================================================================================
        internal virtual ErrorCodes PreprocessBlockSizeMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            string blockSize = MessageTranslator.GetPropertyValue(message);
            double value;

            bool valid = PlatformParser.TryParse(blockSize, out value);

            if (valid)
            {
                m_daqDevice.DriverInterface.InputBlockSize = blockSize;
            }
            else
            {
                if (blockSize == PropertyValues.DEFAULT)
                    m_daqDevice.DriverInterface.InputBlockSize = blockSize;
                else
                    errorCode = ErrorCodes.InvalidInputBlockSize;
            }

            m_daqDevice.SendMessageToDevice = false;

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// This allows the user to override the automatic setting of the bulk transfer size
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The error code</returns>
        //====================================================================================
        internal virtual ErrorCodes PreprocessBlockSizeQueryMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            m_daqDevice.SendMessageToDevice = false;

            if (message != String.Format("{0}{1}{2}{3}", Constants.QUERY, DaqComponents.AISCAN, Constants.PROPERTY_SEPARATOR, DaqProperties.BLOCKSIZE))
                errorCode = ErrorCodes.InvalidMessage;

            if (errorCode == ErrorCodes.NoErrors)
            {
                string response;

                // remove the query
                response = message.Substring(1);

                // append the value
                response += (Constants.EQUAL_SIGN + m_daqDevice.DriverInterface.InputBlockSize);

                if (m_daqDevice.DriverInterface.InputBlockSize == PropertyValues.DEFAULT)
                {
                    m_daqDevice.ApiResponse = new DaqResponse(response, double.NaN);
                }
                else
                {
                    double size = double.NaN;;

                    PlatformParser.TryParse(m_daqDevice.DriverInterface.InputBlockSize, out size);
                    m_daqDevice.ApiResponse = new DaqResponse(response, size);
                }
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Processes the Ai trig message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes PreprocessAiTrigSrcMessage(ref string message)
        {
            if (message.Contains(DaqProperties.TRIGSCR))
            {
                if (!ValidateDaqFeature(message, "AITRIG:SRCS"))
                    return ErrorCodes.InvalidAiTriggerSource;

                if (message.Contains(Constants.EQUAL_SIGN))
                {
                    m_daqDevice.CriticalParams.InputTriggerSource = message.Substring(message.IndexOf(Constants.EQUAL_SIGN) + 1);

                    RecalculateTriggerLevel();
                }

                return ErrorCodes.NoErrors;
            }
            else
            {
                return ErrorCodes.NoErrors;
            }
        }

        //====================================================================================
        /// <summary>
        /// Overriden to process the Ai trig message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes PreprocessAiTrigTypeMessage(ref string message)
        {
            if (message.Contains(DaqProperties.TRIGTYPE))
            {
                if (!ValidateDaqFeature(message, "AITRIG:TYPES"))
                    return ErrorCodes.InvalidAiTriggerType;

                if (message.Contains(Constants.EQUAL_SIGN))
                    m_daqDevice.CriticalParams.InputTriggerType = message.Substring(message.IndexOf(Constants.EQUAL_SIGN) + 1);

                return ErrorCodes.NoErrors;
            }
            else
            {
                return ErrorCodes.NoErrors;
            }
        }

        //====================================================================================
        /// <summary>
        /// Validates the rearm value and sets the critical params
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes PreprocessAiTrigRearmMessage(ref string message)
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (message.Contains(DaqProperties.TRIGREARM) && message.Contains(Constants.EQUAL_SIGN))
            {
                string rearmValue = MessageTranslator.GetPropertyValue(message);

                if (rearmValue != PropertyValues.ENABLE && rearmValue != PropertyValues.DISABLE)
                {
                    errorCode = ErrorCodes.InvalidPropertyValueSpecified;
                }
                else
                {
                    if (rearmValue == PropertyValues.ENABLE)
                        m_daqDevice.CriticalParams.TriggerRearmEnabled = true;
                    else
                        m_daqDevice.CriticalParams.TriggerRearmEnabled = false;
                }
            }

            return errorCode;
        }

        //====================================================================================
        /// <summary>
        /// Virtual method for processing the Ai trig message
        /// </summary>
        /// <param name="message">The device message</param>
        //====================================================================================
        internal virtual ErrorCodes PreprocessAiTrigLevelMessage(ref string message)
        {
            double level = 0.0;

            int equalIndex = message.IndexOf(Constants.EQUAL_SIGN);

            if (equalIndex >= 0)
            {
                // get the requested trigger level from the message
                if (message.Contains(DaqProperties.TRIGLEVEL))
                {
                    if (!PlatformParser.TryParse(message.Substring(message.IndexOf(Constants.EQUAL_SIGN) + 1), out level))
                        return ErrorCodes.InvalidAiTriggerLevel;

                    m_daqDevice.CriticalParams.RequestedInputTriggerLevel = level;
                    
                    // recalculate the range to insure it is in counts, and modify 
                    // the message to replace voltage level with count level
                    RecalculateTriggerLevel();
                    message = message.Substring(0, equalIndex + 1) + m_daqDevice.CriticalParams.InputTriggerLevel.ToString();
                }
            }
            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Private method for validatinging the Ai trigger
        /// </summary>
        /// <param name="channel">The AI trigger channel</param>
        //====================================================================================
        internal ErrorCodes ValidateAiTrigger()
        {
            // if trigger not enabled, just return
            if (!m_daqDevice.CriticalParams.InputTriggerEnabled)
                return ErrorCodes.NoErrors;

            string sources = m_daqDevice.GetDevCapsString("AITRIG:SRCS", false);
            if (sources.Contains("PROG"))
            {
                if (m_daqDevice.CriticalParams.InputTriggerSource == null)
                    return ErrorCodes.InvalidAiTriggerChannel;
            }

            if (m_daqDevice.CriticalParams.InputTriggerType == null)
                return ErrorCodes.InvalidAiTriggerType;

            // is HW or SW triggering selected
            string trigSrc = m_daqDevice.CriticalParams.InputTriggerSource;

            if (trigSrc.Contains("SWSTART"))
            {
                // validate SW triggering
                int channelNumber = MessageTranslator.GetChannel(trigSrc.Substring(trigSrc.IndexOf(Constants.VALUE_RESOLVER) + 1));
                return ValidateAiSwTrigger(channelNumber);
            }

            if (trigSrc.Contains("HWSTART") || trigSrc.Contains("HW/DIG"))
                // validate HW triggering
                return ValidateAiHwTrigger();

            return ErrorCodes.NoErrors;
        }
        
        //====================================================================================
        /// <summary>
        /// Private method for validatinging the Ai software trigger
        /// </summary>
        /// <param name="channel">The AI trigger channel</param>
        //====================================================================================
        private ErrorCodes ValidateAiSwTrigger(int channelNumber)
        {
            if (channelNumber > m_daqDevice.CriticalParams.AiChannelCount)
                return ErrorCodes.InvalidAiTriggerChannel;

            if (channelNumber < m_daqDevice.CriticalParams.LowAiChannel || channelNumber > m_daqDevice.CriticalParams.HighAiChannel)
                return ErrorCodes.InvalidAiTriggerChannel;

            double level = m_daqDevice.CriticalParams.RequestedInputTriggerLevel;
            if (level < m_activeChannels[channelNumber].LowerLimit || level > m_activeChannels[channelNumber].UpperLimit)
                return ErrorCodes.InvalidAiTriggerLevel;


            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Private method for validating the Ai hardware trigger
        /// </summary>
        //====================================================================================
        private ErrorCodes ValidateAiHwTrigger()
        {

            return ErrorCodes.NoErrors;
        }

        //====================================================================================
        /// <summary>
        /// Internal method for recalculating the trigger level
        /// </summary>
        //====================================================================================
        internal override void RecalculateTriggerLevel()
        {
            string msg;
            string response = m_daqDevice.GetDevCapsString("AISCAN:TRIG", true);

            if (response == PropertyValues.NOT_SUPPORTED)
            {
                m_daqDevice.CriticalParams.ResendInputTriggerLevelMessage = false;
                return;
            }
            else
                m_daqDevice.CriticalParams.ResendInputTriggerLevelMessage = true;


            int channelNumber = 0;
            string defaultRange = string.Empty;
            string defaultChmode = string.Empty;

            string trigSrc = m_daqDevice.CriticalParams.InputTriggerSource;
            if (trigSrc == null)
                channelNumber = m_daqDevice.CriticalParams.LowAiChannel;
            else
            {
                // only validate level for SW trigger
                if (trigSrc.Contains("HWSTART") || trigSrc.Contains("HW/DIG"))
                    return;

                channelNumber = MessageTranslator.GetChannel(trigSrc.Substring(trigSrc.IndexOf(Constants.VALUE_RESOLVER) + 1));
            }

            msg = Messages.AI_CH_RANGE_QUERY;
            msg = Messages.InsertChannel(msg, channelNumber);
            m_daqDevice.SendMessageDirect(msg);
            response = m_daqDevice.DriverInterface.ReadStringDirect();
            defaultRange = MessageTranslator.GetPropertyValue(response);

            response = m_daqDevice.GetDevCapsString("AI:CHMODES", false);
            if (response.Contains("FIXED"))
            {
                defaultChmode = response.Substring(response.IndexOf('%') + 1);
            }
            else
            {
                msg = Messages.AI_CHMODE_QUERY;
                m_daqDevice.SendMessageDirect(msg);
                response = m_daqDevice.DriverInterface.ReadStringDirect();
                defaultChmode = MessageTranslator.GetPropertyValue(response);
            }

            string rangeIndex = defaultRange + ":" + defaultChmode;

            double level = m_daqDevice.CriticalParams.RequestedInputTriggerLevel;

            // if scaling is enabled, unscale the level
            if (m_daqDevice.CriticalParams.ScaleAiData)
            {
                // get the upper and lower limits
                double upperLimit = m_supportedRanges[rangeIndex].UpperLimit;
                double lowerLimit = m_supportedRanges[rangeIndex].LowerLimit;

                // unscale the level
                level = (level - lowerLimit) / ((upperLimit - lowerLimit) / (m_maxCount + 1));
            }

            // if calibration is enabled, uncalibrate the level
            if (m_daqDevice.CriticalParams.CalibrateAiData)
            {
                double calSlope = m_calCoeffs[String.Format("Ch{0}:{1}", channelNumber, rangeIndex)].Slope;
                double calOffset = m_calCoeffs[String.Format("Ch{0}:{1}", channelNumber, rangeIndex)].Offset;

                // uncalibrate the level
                level = (level - calOffset) / calSlope;
            }

            m_daqDevice.CriticalParams.InputTriggerLevel = level;
        }

#endregion

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
            unsafe
            {
                try
                {
                    double response;
                    int xfrSize;

                    // transfer size is the number of BYTES transferred per sample per channel over the bulk in endpoint
                    response = m_daqDevice.GetDevCapsValue("AISCAN:XFRSIZE");

                    if (Double.IsNaN(response))
                        xfrSize = m_daqDevice.CriticalParams.AiDataWidth / Constants.BITS_PER_BYTE;
                    else
                        xfrSize = (int)response;

                    if (xfrSize <= 2)
                    {
                        int workingSourceIndex = sourceCopyByteIndex;
                        int channelCount = destinationBuffer.GetLength(0);
                        int totalSamplesToCopy = channelCount * samplesToCopyPerChannel;

                        fixed (double* pSlopesFixed = m_daqDevice.CriticalParams.AiSlopes, pOffsetsFixed = m_daqDevice.CriticalParams.AiOffsets, pDestinationBufferFixed = destinationBuffer)
                        {
                            double* pSlopes = pSlopesFixed;
                            double* pOffsets = pOffsetsFixed;

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

                                    *pDestinationBuffer = ((double)*pSourceBuffer++) * (*pSlopes++) + (*pOffsets++);

                                    workingSourceIndex += xfrSize;

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
    

        //===============================================================================================================================
        /// <summary>
        /// Copies the most recent input buffer to the unmanaged external read buffer
        /// </summary>
        /// <param name="bulkReadBuffer">The buffer that just received a bulk in transfer</param>
        /// <param name="externalReadBuffer">the external read buffer to copy data to</param>
        /// <param name="readBufferLength">The length of the external read buffer</param>
        /// <param name="bytesToTransfer">The number of bytes to transfer</param>
        //===============================================================================================================================
        internal unsafe virtual ErrorCodes CopyToExternalReadBuffer(byte[] sourceBuffer,
                                                              void* externalReadBuffer,
                                                              int readBufferLength,
                                                              uint bytesToTransfer,
                                                              ref int lastExternalBufferIndex)
        {
            unsafe
            {
                try
                {
                    int dataWidth = m_daqDevice.CriticalParams.AiDataWidth;

                    if (dataWidth <= 16)
                    {
                        int samplesToTransfer = (int)bytesToTransfer / sizeof(ushort);

                        fixed (double* pSlopesFixed = m_daqDevice.CriticalParams.AiSlopes, pOffsetsFixed = m_daqDevice.CriticalParams.AiOffsets)
                        {
                            double* pSlopes = pSlopesFixed;
                            double* pOffsets = pOffsetsFixed;

                            fixed (byte* pBulkReadBufferFixed = sourceBuffer)
                            {
                                ushort* pBulkReadBuffer = (ushort*)pBulkReadBufferFixed;
                                ushort* pExtReadBuffer = (ushort*)((byte*)externalReadBuffer + lastExternalBufferIndex + 1);
                                ushort* extReadBufferEnd = (ushort*)((byte*)externalReadBuffer + (readBufferLength - 1));

                                for (int i = 0; i < samplesToTransfer; i++)
                                {
                                    if (i % m_daqDevice.CriticalParams.AiChannelCount == 0)
                                    {
                                       pSlopes = pSlopesFixed;
                                       pOffsets = pOffsetsFixed;
                                    }

                                    *pExtReadBuffer++ = (ushort)((*pBulkReadBuffer++) * (*pSlopes++) + (*pOffsets++));

                                    lastExternalBufferIndex += sizeof(ushort);

                                    if (pExtReadBuffer > extReadBufferEnd)
                                    {
                                        pExtReadBuffer = (ushort*)externalReadBuffer;
                                        lastExternalBufferIndex = -1;
                                    }
                                }
                            }
                        }
                    }

                    return ErrorCodes.NoErrors;
                }
                catch (Exception)
                {
                    return ErrorCodes.ErrorWritingDataToExternalInputBuffer;
                }
            }
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to process any data after a message is sent to a device
        /// </summary>
        /// <param name="dataType">The type of data (e.g. Ai, Ao, Dio)</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal override ErrorCodes PostProcessData(string componentType, ref string response, ref double value)
        {
            ErrorCodes result = ErrorCodes.NoErrors;

            if (componentType == DaqComponents.AI && response.Contains(DaqProperties.VALUE))
            {
                value = CalibrateData(m_daqDevice.DriverInterface.CriticalParams.AiChannel, value);
                response = response.Substring(0, response.IndexOf("=") + 1) + value.ToString();

                result = PrescaleData(m_daqDevice.DriverInterface.CriticalParams.AiChannel, ref value);

                if (result == ErrorCodes.NoErrors)
                {
                    result = ScaleData(0, ref value);

                    if (result == ErrorCodes.NoErrors)
                        response = response.Substring(0, response.IndexOf("=") + 1) + value.ToString();
                    else
                        response = GetPreprocessDataErrorResponse(result, response);
                }
                else
                {
                    if (result == ErrorCodes.OpenThermocouple && m_scaleData)
                        response = MessageTranslator.ReplaceValue(response, Constants.OPEN_THERMOCOUPLE.ToString());

                    response = GetPreprocessDataErrorResponse(result, response);
                }
            }

            return result;
        }

        //========================================================================================
        /// <summary>
        /// Adds one or more elements to the queue list if not already in the list
        /// </summary>
        /// <param name="element"></param>
        //========================================================================================
        internal virtual void CheckAiQueueElement(int element)
        {
            if (element >= m_aiQueueList.Count)
            {
                while (m_aiQueueList.Count <= element)
                {
                    m_aiQueueList.Add(new AiQueue());
                }
            }
        }

        //========================================================================================
        /// <summary>
        /// Virtual method to preprocess Ai data such as checkinf for temp overrange values
        /// before scaling the raw data
        /// </summary>
        /// <param name="channel">The channel number</param>
        /// <param name="value">The raw data value</param>
        /// <returns>Teh Error code</returns>
        //========================================================================================
        internal virtual ErrorCodes PrescaleData(int channel, ref double value)
        {
            return ErrorCodes.NoErrors;
        }

        //======================================================================================================================
        /// <summary>
        /// Converts the measured Voltage references to counts. This implementation assumes
        /// measuredVRefs[0] = 0 Volts
        /// measuredVRefs[1] = +N Volts
        /// measuredVRefs[2] = -N Volts
        /// </summary>
        /// <param name="measuredVRefs">The voltage references in volts</param>
        /// <param name="vRefRanges">The voltage reference ranges in volts</param>
        /// <param name="vRefsOut">The voltage references in counts</param>
        //======================================================================================================================
        internal virtual void ConvertVrefsToCounts(double[] measuredVRefs, double[] vRefRanges, out int[] vRefsOut)
        {
            vRefsOut = new int[3];

            if (measuredVRefs.Rank != 1)
            {
                string msg = String.Format("vRefsIn does not have the right dimensions: is {0}, expected {1}", measuredVRefs.Rank, 1);
                System.Diagnostics.Debug.Assert(false, msg);
                return;
            }

            if (measuredVRefs.Length != 3)
            {
                string msg = String.Format("vRefsIn does not have the right number of elements: is {0}, expected {1}", measuredVRefs.Length, 3);
                System.Diagnostics.Debug.Assert(false, msg);
                return;
            }

            double Scale = vRefRanges[1] - vRefRanges[2];

            double lsb = Scale / (m_maxCount + 1);
            double midPoint = (m_maxCount + 1) / 2;

            for (int i = 0; i < measuredVRefs.Length; i++)
                vRefsOut[i] = Convert.ToInt32(measuredVRefs[i] / lsb + midPoint);
        }

        //===========================================================================================
        /// <summary>
        /// Applies calibration coefficients to the raw A/D value if the CAL=ENABLE message
        /// was previously sent
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

                    if (! m_daqDevice.CriticalParams.AiDataIsSigned)
                        calibratedValue = Math.Max(0, calibratedValue);
                    calibratedValue = Math.Min(m_maxCount, calibratedValue);
                }
            }

            return calibratedValue;
        }

        //===========================================================================================
        /// <summary>
        /// Scales an analog input value
        /// </summary>
        /// <param name="value">The raw A/D value</param>
        /// <returns>The scaled value</returns>
        //===========================================================================================
        internal override ErrorCodes ScaleData(int channelIndex, ref double value)
        {
            double scaledValue = value;

            if (m_scaleData)
            {
                scaledValue = scaledValue * ((m_activeChannels[channelIndex].UpperLimit - m_activeChannels[channelIndex].LowerLimit) / (m_maxCount + 1));
                if (! m_daqDevice.CriticalParams.AiDataIsSigned)
                    scaledValue += m_activeChannels[channelIndex].LowerLimit;
            }

            value = scaledValue;

            return ErrorCodes.NoErrors;
        }

        //================================================================================================
        /// <summary>
        /// Gets the channel mode that the channel is currently set for
        /// </summary>
        /// <returns>The mode type</returns>
        //================================================================================================
        internal string GetChannelMode(int channel)
        {
            string msg;
            string response;
            string mode;

            // query the sensor type which is stored on the device
            msg = Messages.AI_CH_CHMODE_QUERY;
            msg = Messages.InsertChannel(msg, channel);
            m_daqDevice.SendMessageDirect(msg);
            response = m_daqDevice.m_driverInterface.ReadStringDirect();

            mode = response.Substring(response.IndexOf(Constants.EQUAL_SIGN) + 1);

            return mode;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to post process a message
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <returns>True if the message is to be sent to the device, otherwise false</returns>
        //===========================================================================================
        internal override void PostProcessMessage(ref string message, string messageType)
        {
            if (messageType == DaqComponents.AI && message.Contains(DaqProperties.CHMODE) && message.Contains(Constants.EQUAL_SIGN))
            {
                m_channelCount = (int)m_daqDevice.GetDevCapsValue("AI:CHANNELS");

                if (m_channelCount == m_maxChannels)
                {
                    string msg;
                    string response;

                    for (int i = 0; i < m_ranges.Length; i++)
                    {
                        if (m_ranges[i] == null)
                        {
                            msg = Messages.AI_CH_RANGE_QUERY;
                            msg = msg.Replace("*", i.ToString());
                            m_daqDevice.SendMessageDirect(msg);
                            response = m_daqDevice.DriverInterface.ReadStringDirect();
                            response = MessageTranslator.GetPropertyValue(response);
                            m_ranges[i] = String.Format("{0}{1}:{2}={3}", DaqComponents.AI, MessageTranslator.GetChannelSpecs(i), DaqProperties.RANGE, MessageTranslator.ConvertToCurrentCulture(response));

                        }
                    }
                }
            }
        }
    }
}
