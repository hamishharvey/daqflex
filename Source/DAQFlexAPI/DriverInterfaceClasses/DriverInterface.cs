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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.IO;

namespace MeasurementComputing.DAQFlex
{
    internal class DriverInterface
    {
        internal delegate void StopOutputScanDelegate();

#if !WindowsCE
        protected const int INTERNAL_READ_BUFFER_SIZE = 1024000;
#else
        protected const int INTERNAL_READ_BUFFER_SIZE = 256000;
#endif
        protected const int INTERNAL_WRITE_BUFFER_SIZE = 65536;

        private DeviceInfo m_deviceInfo;
        private DaqDevice m_daqDevice;
        private ErrorCodes m_errorCode;
        private UsbPlatformInterop m_platformInterop;
        private List<UsbSetupPacket> m_usbPackets;
        protected string m_internalReadString;
        protected double m_internalReadValue;
        protected byte[] m_internalReadBuffer;
        protected unsafe void* m_externalReadBuffer;
        protected int m_externalReadBufferSize;
        protected bool m_inputBufferSizeOverride;
        protected bool m_startInputScan = false;
        protected bool m_startOutputScan = false;
        protected volatile bool m_initiateStopForInput = false;
        protected volatile bool m_stopInputScan = false;
        protected bool m_stopOutputScan = false;
        protected int m_currentInputScanReadIndex;
        protected volatile int m_lastInputScanWriteIndex;
        private int m_totalSamplesToReadPerChannel;
        private int m_inputSamplesReadPerChannel;
        private int m_totalBytesToRead;
        private int m_totalBytesReceived;
        protected ASCIIEncoding m_ae;
        protected Queue<UsbSetupPacket> m_deferredMessages = new Queue<UsbSetupPacket>();
        protected List<string> m_deferredResponses = new List<string>();
        protected UsbSetupPacket m_deferredResponsePacket;
        private BulkInBuffer m_bulkReadBuffer;
        private Thread m_inputScanThread;
        private Thread m_callbackThread;
        private Thread m_outputScanThread;
        private bool m_terminateCallbacks;
        private Queue<CallbackInfo> m_callbackInfoQueue = new Queue<CallbackInfo>();
        private object callbackInfoQueueLock = new Object();
        private bool m_inputScanComplete;
        private bool m_outputScanComplete;
        private volatile ScanState m_inputScanState;
        private CriticalParams m_criticalParams = new CriticalParams();
        private long m_inputScanCount;
        private long m_inputScanIndex;
        private CallbackControl m_onDataAvailableCallbackControl;
        private CallbackControl m_onInputScanCompleteCallbackControl;
        private CallbackControl m_onInputScanErrorCallbackControl;
        private CallbackDelegate m_onDataAvailableCallback;
        private CallbackDelegate m_onInputScanCompleteCallback;
        private CallbackDelegate m_onInputScanErrorCallback;
        private int m_availableSamplesForCallbackSinceStartOfScan;
        private object[] m_scanCompleteCallbackParam = new object[1];
        private object[] m_scanErrorCallbackParam = new object[1];
        private byte[] m_aiStatusMessage = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aiTrigStatus = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aiRearmStatus = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aiQuerySamples = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aiScanSamples = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aiScanRate = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aiScanStart = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aoStatusMessage = new byte[Constants.MAX_COMMAND_LENGTH];
        private bool m_deferredMessagesSent;
        private int m_invokeCallbackCount;
        private UsbSetupPacket m_controlInPacket;
        private UsbSetupPacket m_controlOutPacket;
        private bool m_inputBufferFilled;
        private bool m_inputScanStarted;
        protected bool m_deviceLost = false;
        protected ScanState m_inputScanStatus;
        protected System.Diagnostics.Stopwatch m_readStopWatch = new System.Diagnostics.Stopwatch();
        protected System.Diagnostics.Stopwatch m_writeStopWatch = new System.Diagnostics.Stopwatch();

        protected int m_outputTransferStartIndex;
        protected int m_currentOutputScanWriteIndex;
        protected int m_currentOutputScanOutputIndex;
        protected long m_outputScanCount;
        protected long m_outputScanIndex;
        protected bool m_outputScanStarted;
        protected int m_numberOfSamplesPerChannelWrittenToDevice;
        protected byte[] m_internalWriteBuffer;
        protected int m_totalSamplesToWritePerChannel;
        protected bool m_outputBufferSizeOverride;
        protected ScanState m_outputScanState;
        protected int m_totalBytesToWrite;
        protected bool m_initiateStopForOutput;
        protected bool m_overwritingOldScanData;
        protected int m_totalBytesReceivedByDevice;
        protected StopOutputScanDelegate m_stopOutputScanDelegate;
        protected AsyncCallback m_stopOutputScanCallback;

        internal DriverInterface(DaqDevice daqDevice, DeviceInfo deviceInfo)
        {
            m_deviceInfo = deviceInfo;
            m_daqDevice = daqDevice;

            m_platformInterop = PlatformInterop.GetUsbPlatformInterop(deviceInfo, m_criticalParams);

            // the error code may be set if the device did not initialzie
            m_errorCode = m_platformInterop.ErrorCode;

            m_internalReadBuffer = new byte[INTERNAL_READ_BUFFER_SIZE];

            m_usbPackets = new List<UsbSetupPacket>();
            m_ae = new ASCIIEncoding();

            string message;

            // convert message to an array of bytes for the driver interface
            message = "?AISCAN:STATUS";
            for (int i = 0; i < message.Length; i++)
                m_aiStatusMessage[i] = (byte)(Char.ToUpper(message[i]));

            message = "?AISCAN:TRIG";
            for (int i = 0; i < message.Length; i++)
                m_aiTrigStatus[i] = (byte)(Char.ToUpper(message[i]));

            message = "?AITRIG:REARM";
            for (int i = 0; i < message.Length; i++)
                m_aiRearmStatus[i] = (byte)(Char.ToUpper(message[i]));

            message = "?AISCAN:SAMPLES";
            for (int i = 0; i < message.Length; i++)
                m_aiQuerySamples[i] = (byte)(Char.ToUpper(message[i]));

            message = "AISCAN:SAMPLES=";
            for (int i = 0; i < message.Length; i++)
                m_aiScanSamples[i] = (byte)(Char.ToUpper(message[i]));

            message = "AISCAN:RATE=";
            for (int i = 0; i < message.Length; i++)
                m_aiScanRate[i] = (byte)(Char.ToUpper(message[i]));

            message = "AISCAN:START";
            for (int i = 0; i < message.Length; i++)
                m_aiScanStart[i] = (byte)(Char.ToUpper(message[i]));

            message = "?AOSCAN:STATUS";
            for (int i = 0; i < message.Length; i++)
                m_aoStatusMessage[i] = (byte)(Char.ToUpper(message[i]));

            m_deferredResponsePacket = new UsbSetupPacket(Constants.MAX_COMMAND_LENGTH);
            m_deferredResponsePacket.TransferType = UsbTransferTypes.ControlIn;
            m_deferredResponsePacket.Request = ControlRequest.MESSAGE_REQUEST;
            m_deferredResponsePacket.DeferTransfer = false;
            m_deferredResponsePacket.Index = 0;
            m_deferredResponsePacket.Value = 0;
            m_deferredResponsePacket.Length = Constants.MAX_COMMAND_LENGTH;
            m_inputBufferSizeOverride = false;

            m_controlInPacket = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            m_controlInPacket.TransferType = UsbTransferTypes.ControlIn;
            m_controlInPacket.Request = ControlRequest.MESSAGE_REQUEST;

            m_controlOutPacket = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            m_controlOutPacket.TransferType = UsbTransferTypes.ControlOut;
            m_controlOutPacket.Request = ControlRequest.MESSAGE_REQUEST;

            m_criticalParams.ScanType = ScanType.AnalogInput;

            m_currentOutputScanWriteIndex = 0;
            m_outputTransferStartIndex = 0;

            unsafe
            {
                m_externalReadBuffer = null;
            }

            m_internalReadBuffer = new byte[INTERNAL_READ_BUFFER_SIZE];
            m_internalWriteBuffer = new byte[INTERNAL_WRITE_BUFFER_SIZE];

            m_stopOutputScanDelegate = new StopOutputScanDelegate(StopOutputScan);
            m_stopOutputScanCallback = new AsyncCallback(StopOutputScanCallback);
        }

        //=================================================================================
        /// <summary>
        /// The current input scan read byte index.
        /// This is used by the DaqDevice class and is accessed by only one thread
        /// </summary>
        //=================================================================================
        internal int CurrentInputScanReadIndex
        {
            get { return m_currentInputScanReadIndex;}

            set
            {
                int deltaByteIndex;
                int channelCount;
                int byteRatio;

                if (value > m_currentInputScanReadIndex)
                    deltaByteIndex = (value - m_currentInputScanReadIndex);
                else
                    deltaByteIndex = (m_internalReadBuffer.Length - m_currentInputScanReadIndex + value);

                channelCount = m_criticalParams.AiChannelCount;
                byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);
                m_inputSamplesReadPerChannel += deltaByteIndex / channelCount / byteRatio;

                m_currentInputScanReadIndex = value;

                if (m_currentInputScanReadIndex >= m_internalReadBuffer.Length)
                    m_currentInputScanReadIndex = 0;
            }
        }

        //=====================================================================================
        /// <summary>
        /// The current output scan write byte index
        /// </summary>
        //=====================================================================================
        internal int CurrentOutputScanWriteIndex
        {
            get { return m_currentOutputScanWriteIndex; }

            set
            {
                m_currentOutputScanWriteIndex = value;
            }
        }

        //===========================================================================================
        /// <summary>
        /// A flag that indicates the internal output scan write buffer has wrapped around
        /// this is set by the DaqDevice.Ao component since it write data to the buffer
        /// </summary>
        //===========================================================================================
        internal bool OverwritingOldScanData
        {
            set { m_overwritingOldScanData = value; }
        }

        //===========================================================================================
        /// <summary>
        /// A reference to the abstract platform interop object
        /// </summary>
        //===========================================================================================
        internal PlatformInterop PlatformInterop
        {
            get { return m_platformInterop; }
        }

        //===========================================================================================
        /// <summary>
        /// Calculates the size of the buffer that will be used for each
        /// Bulk In request based on the rate. The higher the rate, the larger the buffer size.
        /// </summary>
        /// <returns>the size of the buffer in bytes</returns>
        //===========================================================================================
        internal int GetOptimalInputBufferSize(double scanRate)
        {
            int bufferSize = 0;
            int packetSize = m_criticalParams.InputPacketSize;
            int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);

            if (scanRate == 0.0 || packetSize == 0)
                return 0;

            int channelCount = m_criticalParams.AiChannelCount;

            if (channelCount <= 0)
                return 0;

            if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
            {
                bufferSize = packetSize;
            }
            else
            {
                // Set buffer size for 50 mS transfers
                bufferSize = Math.Max(m_criticalParams.InputPacketSize, (int)(byteRatio * 0.05 * (double)scanRate));

                if (m_criticalParams.InputConversionMode == InputConversionMode.Simultaneous)
                    bufferSize *= channelCount;

                if (bufferSize % packetSize != 0)
                {
                    int multiplier = (int)Math.Ceiling((double)bufferSize / (double)packetSize);
                    bufferSize = multiplier * packetSize;
                }
            }

            return bufferSize;
        }

        //===========================================================================================
        /// <summary>
        /// Calculates the size of the buffer that will be used for each
        /// Bulk In request based on the rate. The higher the rate, the larger the buffer size.
        /// </summary>
        /// <returns>the size of the buffer in bytes</returns>
        //===========================================================================================
        internal int GetOptimalOutputBufferSize(double scanRate)
        {
            int bufferSize = 0;
            int packetSize = m_criticalParams.OutputPacketSize;
            int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AoDataWidth / (double)Constants.BITS_PER_BYTE);

            if (scanRate == 0.0 || packetSize == 0)
                return 0;

            int channelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;

            if (channelCount <= 0)
                return 0;

            // Set buffer size for 50 mS transfers
            bufferSize = Math.Max(m_criticalParams.OutputPacketSize, (int)(byteRatio * 0.05 * (double)scanRate));

            if (bufferSize % packetSize != 0)
            {
                int multiplier = (int)Math.Ceiling((double)bufferSize / (double)packetSize);
                bufferSize = multiplier * packetSize;
            }

            return bufferSize;
        }

        //======================================================================================
        /// <summary>
        /// Transfers the incoming command to the device using one of the platform interop objects
        /// </summary>
        /// <param name="incomingMessage">The incoming message string</param>
        /// <returns>The error code</returns>
        //======================================================================================
        internal ErrorCodes TransferMessage(byte[] incomingMessage, ResponseType responseType)
        {
            if (m_deviceLost)
                m_deviceLost = !m_platformInterop.AcquireDevice();

            ErrorCodes errorCode = ErrorCodes.NoErrors;

            UsbSetupPacket stopInputScanPacket;
            UsbSetupPacket stopOutputScanPacket;

            if (!m_platformInterop.DeviceInitialized)
            {
                errorCode = ErrorCodes.DeviceNotInitialized;
            }
            else
            {
                m_platformInterop.ControlTransferMutex.WaitOne();

                // now create the message packets for the incoming message
                CreateUsbPackets(incomingMessage);

                foreach (UsbSetupPacket packet in m_usbPackets)
                {
                    if (errorCode == ErrorCodes.NoErrors)
                    {
                        if (m_usbPackets.Count == 1 && packet.TransferType == UsbTransferTypes.ControlOut)
                        {
                            m_internalReadString = String.Empty;
                            m_internalReadValue = double.NaN;
                        }

                        // Control In request
                        if (packet.TransferType == UsbTransferTypes.ControlIn)
                        {
                            errorCode = m_platformInterop.UsbControlInRequest(packet);

                            if (packet.Request == ControlRequest.MESSAGE_REQUEST)
                            {
                                if (errorCode == ErrorCodes.NoErrors)
                                {
                                    m_internalReadString = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);

                                    if (m_internalReadString.IndexOf(Constants.NULL_TERMINATOR) >= 0)
                                    {
                                        int indexOfNt = m_internalReadString.IndexOf(Constants.NULL_TERMINATOR);
                                        m_internalReadString = m_internalReadString.Remove(indexOfNt, m_internalReadString.Length - indexOfNt);
                                    }

                                    m_internalReadValue = TryConvertData(ref m_internalReadString);
                                }
                                else
                                {
                                    m_internalReadString = String.Empty;
                                    m_internalReadValue = double.NaN;
                                }
                            }
                        }
                        // Control Out request
                        else if (packet.TransferType == UsbTransferTypes.ControlOut)
                        {
                            // send the Control Out request to the device
                            if (!packet.DeferTransfer)
                            {
                                errorCode = m_platformInterop.UsbControlOutRequest(packet);
                            }
                            else
                            {
                                // queue the message packet. It will be sent at the beginning of a scan thread
                                m_deferredMessages.Enqueue(packet);
                            }

                            if (m_startInputScan)
                            {
                                if (errorCode == ErrorCodes.NoErrors)
                                {
                                    ScanState scanState = GetInputScanState();

                                    if (scanState == ScanState.Running)
                                    {
                                        errorCode = ErrorCodes.InputScanAlreadyInProgress;
                                    }
                                    else if (m_deviceInfo.EndPointIn == 0)
                                    {
                                        errorCode = ErrorCodes.InvalidMessage;
                                    }
                                    else
                                    {
                                        // m_startInputScan is set to true in CheckForCriticalParams
                                        m_deferredResponses.Clear();

                                        CheckTriggerRearm();

                                        if (m_criticalParams.InputSampleMode == SampleMode.Continuous)
                                        {
                                            if (m_onDataAvailableCallbackControl != null)
                                            {
                                                int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);

                                                if (m_criticalParams.AiChannelCount * byteRatio * m_onDataAvailableCallbackControl.NumberOfSamples > m_internalReadBuffer.Length / 2)
                                                {
                                                    errorCode = ErrorCodes.CallbackCountTooLarge;
                                                }
                                            }
                                        }

                                        // release the control transfer mutex so that the deferred messages can be sent
                                        m_platformInterop.ControlTransferMutex.ReleaseMutex();

                                        if (errorCode == ErrorCodes.NoErrors)
                                            errorCode = StartInputScan();

                                        // reaquire the control transfer mutex
                                        m_platformInterop.ControlTransferMutex.WaitOne();

                                        if (errorCode == ErrorCodes.NoErrors)
                                        {
                                            if (m_errorCode != ErrorCodes.NoErrors)
                                                errorCode = m_errorCode;
                                            else if (packet.DeferTransfer)
                                                m_internalReadString = m_deferredResponses[0].Trim(new char[] { Constants.NULL_TERMINATOR });
                                        }
                                    }
                                }
                            }
                            else if (m_startOutputScan)
                            {
                                if (errorCode == ErrorCodes.NoErrors)
                                {
                                    ScanState scanState = GetOutputScanState();

                                    if (scanState == ScanState.Running)
                                    {
                                        errorCode = ErrorCodes.OutputScanAlreadyInProgress;
                                    }
                                    else if (m_deviceInfo.EndPointIn == 0)
                                    {
                                        errorCode = ErrorCodes.InvalidMessage;
                                    }
                                    else
                                    {
                                        // m_startInputScan is set to true in CheckForCriticalParams
                                        m_deferredResponses.Clear();

                                        //CheckTriggerRearm();

                                        if (m_criticalParams.OutputSampleMode == SampleMode.Continuous)
                                        {
                                            //if (m_onDataAvailableCallbackControl != null)
                                            //{
                                            //    int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);

                                            //    if (m_criticalParams.AiChannelCount * byteRatio * m_onDataAvailableCallbackControl.NumberOfSamples > m_internalReadBuffer.Length / 2)
                                            //    {
                                            //        errorCode = ErrorCodes.CallbackCountTooLarge;
                                            //    }
                                            //}
                                        }

                                        // release the control transfer mutex so that the deferred messages can be sent
                                        m_platformInterop.ControlTransferMutex.ReleaseMutex();

                                        if (errorCode == ErrorCodes.NoErrors)
                                            errorCode = StartOutputScan();

                                        // reaquire the control transfer mutex
                                        m_platformInterop.ControlTransferMutex.WaitOne();

                                        if (errorCode == ErrorCodes.NoErrors)
                                        {
                                            if (m_errorCode != ErrorCodes.NoErrors)
                                            {
                                                errorCode = m_errorCode;
                                            }
                                            else if (packet.DeferTransfer)
                                            {
                                                if (m_deferredResponses.Count > 0)
                                                    m_internalReadString = m_deferredResponses[0].Trim(new char[] { Constants.NULL_TERMINATOR });
                                                else
                                                    System.Diagnostics.Debug.Assert(false, "Deferred response list is empty");
                                            }
                                        }
                                    }
                                }
                            }
                            else if (m_initiateStopForInput && m_deviceInfo.EndPointIn != 0)
                            {
                                // release the control transfer mutex so that the Input scan thread can exit
                                m_platformInterop.ControlTransferMutex.ReleaseMutex();

                                // m_initiateStopForInput is set to true in CheckForCriticalParams

                                if (m_deferredMessages.Count > 0)
                                {
                                    // executes on Linux
                                    StopInputScan(false);
                                    stopInputScanPacket = m_deferredMessages.Dequeue();
                                    errorCode = m_platformInterop.UsbControlOutRequest(stopInputScanPacket);
                                    m_platformInterop.FlushInputDataFromDevice();
                                }
                                else
                                {
                                    // executes on Windows
                                    StopInputScan(true);
                                }

                                // reaquire the control transfer mutex
                                m_platformInterop.ControlTransferMutex.WaitOne();
                            }
                            else if (m_initiateStopForOutput && m_deviceInfo.EndPointOut != 0)
                            {
                                // release the control transfer mutex so that the Input scan thread can exit
                                m_platformInterop.ControlTransferMutex.ReleaseMutex();

                                // m_initiateStopForInput is set to true in CheckForCriticalParams

                                if (m_deferredMessages.Count > 0)
                                {
                                    // executes on Linux
                                    StopOutputScan(false);
                                    stopOutputScanPacket = m_deferredMessages.Dequeue();
                                    errorCode = m_platformInterop.UsbControlOutRequest(stopOutputScanPacket);
                                    //m_platformInterop.FlushInputDataFromDevice();
                                }
                                else
                                {
                                    // executes on Windows
                                    StopOutputScan(true);
                                }

                                // reaquire the control transfer mutex
                                m_platformInterop.ControlTransferMutex.WaitOne();
                            }
                        }
                    }
                }

                m_platformInterop.ControlTransferMutex.ReleaseMutex();
            }

            if (errorCode == ErrorCodes.DeviceNotResponding)
            {
                ReleaseDevice();
                m_deviceLost = true;
            }

            return errorCode;
        }

        //===================================================================================
        /// <summary>
        /// The error code set by the set by the driver interface object or returned from
        /// the platform interop object
        /// </summary>
        //===================================================================================
        internal ErrorCodes ErrorCode
        {
            get { return m_errorCode; }
        }

        //======================================================================================
        /// <summary>
        /// The buffer containing input scan data 
        /// </summary>
        /// <returns>The input scan buffer</returns>
        //======================================================================================
        internal byte[] InputScanBuffer
        {
            get
            {
                return m_internalReadBuffer;
            }
        }

        //======================================================================================
        /// <summary>
        /// The buffer containing output scan data
        /// </summary>
        //======================================================================================
        internal byte[] OutputScanBuffer
        {
            get
            {
                if (m_internalWriteBuffer == null)
                    m_internalWriteBuffer = new byte[INTERNAL_WRITE_BUFFER_SIZE];

                return m_internalWriteBuffer;
            }
        }

        //======================================================================================
        /// <summary>
        /// Sets the size of the internal input scan read buffer
        /// </summary>
        /// <param name="samples">The number of bytes</param>
        //======================================================================================
        internal void SetInputBufferSize(int numberOfBytes)
        {
            int mulitplier = (int)Math.Ceiling((double)numberOfBytes / (double)m_deviceInfo.MaxPacketSize);
            m_totalBytesToRead = Math.Max(m_deviceInfo.MaxPacketSize, mulitplier * m_deviceInfo.MaxPacketSize);
            m_internalReadBuffer = new byte[m_totalBytesToRead];
            m_inputBufferSizeOverride = true;
        }

        //======================================================================================
        /// <summary>
        /// Sets the size of the internal output scan write buffer
        /// </summary>
        /// <param name="numberOfBytes">The number of bytes</param>
        //======================================================================================
        internal void SetOutputBufferSize(int numberOfBytes)
        {
            int mulitplier = (int)Math.Ceiling((double)numberOfBytes / (double)m_deviceInfo.MaxPacketSize);
            m_totalBytesToWrite = Math.Max(m_deviceInfo.MaxPacketSize, mulitplier * m_deviceInfo.MaxPacketSize);
            m_internalWriteBuffer = new byte[m_totalBytesToWrite];
            m_outputBufferSizeOverride = true;
        }

        //======================================================================================
        /// <summary>
        /// Sets the external read buffer for applications that allocate the buffer
        /// </summary>
        /// <param name="handle">The memory handle</param>
        /// <param name="memSize">The number of bytes allocated for the memory handle</param>
        //======================================================================================
        internal unsafe void SetInputBufferHandle(void* handle, int memSize)
        {
            m_externalReadBuffer = handle;
            m_externalReadBufferSize = memSize;
        }

        //====================================================================
        /// <summary>
        /// The critical parameters required for data processing
        /// </summary>
        //====================================================================
        internal CriticalParams CriticalParams
        {
            get { return m_criticalParams; }
        }

        //====================================================================
        /// <summary>
        /// The input scan state (e.g. Running, Idle or Overrun)
        /// </summary>
        //====================================================================
        internal ScanState InputScanStatus
        {
            get { return m_inputScanStatus; }
        }

        //====================================================================
        /// <summary>
        /// The output scan state (e.g. Running, Idle or Underrun)
        /// </summary>
        //====================================================================
        internal ScanState OutputScanState
        {
            get { return m_outputScanState; }
        }

        private object m_inputScanCountLock = new Object();

        //====================================================================
        /// <summary>
        /// The number of samples per channel acquired since the scan started
        /// </summary>
        //====================================================================
        internal long InputScanCount
        {
            get 
            {
                lock (m_inputScanCountLock)
                {
                    return m_inputScanCount;
                }
            }
        }

        //================================================================================
        /// <summary>
        /// The number of samples per channel the device received since the scan started
        /// </summary>
        //================================================================================
        internal long OutputScanCount
        {
            get
            {
                return m_outputScanCount;
            }
        }

        //====================================================================
        /// <summary>
        /// The current sample index of the internal or external buffer
        /// starting with the first channel in the scan
        /// </summary>
        //====================================================================
        internal long InputScanIndex
        {
            get { return m_inputScanIndex; }
        }


        //====================================================================
        /// <summary>
        /// The current sample index of the internal or external buffer
        /// starting with the first channel in the scan
        /// </summary>
        //====================================================================
        internal long OutputScanIndex
        {
            get { return m_outputScanIndex; }
        }

        //====================================================================
        /// <summary>
        /// The state of the input scan
        /// </summary>
        //====================================================================
        internal bool InputScanStarted
        {
            get { return m_inputScanStarted; }
        }

        //====================================================================
        /// <summary>
        /// The number of samples per channel read since the start of the scan
        /// </summary>
        //====================================================================
        internal int InputSamplesReadPerChannel
        {
            get { return m_inputSamplesReadPerChannel; }
        }

        private object terminateCallbackLock = new Object();

        //=========================================================================================
        /// <summary>
        /// A flag to indicate if OnDataAvailable callbacks should be terminated
        /// </summary>
        //=========================================================================================
        internal bool TerminateCallbacks
        {
            get
            {
                lock (terminateCallbackLock)
                {
                    return m_terminateCallbacks;
                }
            }

            set
            {
                lock (terminateCallbackLock)
                {
                    m_terminateCallbacks = value;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// This will get set to a CallbackControl instance when a callback is registered
        /// This will get set to null when a callback is unregistered
        /// </summary>
        //=========================================================================================
        internal CallbackControl OnDataAvailableCallbackControl
        {
            get { return m_onDataAvailableCallbackControl; }

            set 
            {
                m_onDataAvailableCallbackControl = value;

#if !WindowsCE
                if (m_onDataAvailableCallbackControl != null)
                    m_onDataAvailableCallbackControl.CreateControl();
#endif
            }
        }

        //=========================================================================================
        /// <summary>
        /// This will get set to a CallbackControl instance when a callback is registered
        /// This will get set to null when a callback is unregistered
        /// </summary>
        //=========================================================================================
        internal CallbackControl OnInputScanCompleteCallbackControl
        {
            get { return m_onInputScanCompleteCallbackControl; }

            set
            {
                m_onInputScanCompleteCallbackControl = value;

#if !WindowsCE
                if (m_onInputScanCompleteCallbackControl != null)
                    m_onInputScanCompleteCallbackControl.CreateControl();
#endif
            }
        }

        //=========================================================================================
        /// <summary>
        /// This will get set to a CallbackControl instance when a callback is registered
        /// This will get set to null when a callback is unregistered
        /// </summary>
        //=========================================================================================
        internal CallbackControl OnInputScanErrorCallbackControl
        {
            get { return m_onInputScanErrorCallbackControl; }

            set
            {
                m_onInputScanErrorCallbackControl = value;
#if !WindowsCE
                if (m_onInputScanErrorCallbackControl != null)
                    m_onInputScanErrorCallbackControl.CreateControl();
#endif
            }
        }

        //=========================================================================================
        /// <summary>
        /// Gets the device's response as a numeric
        /// </summary>
        /// <param name="response">The response string</param>
        /// <returns>The converted value</returns>
        //=========================================================================================
        protected double TryConvertData(ref string response)
        {
            double value = double.NaN;

            string dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            response = response.Replace(".", dec);

            int equalIndex = response.IndexOf(Constants.EQUAL_SIGN);

            if (equalIndex >= 0)
            {
                double parsedValue = Double.NaN;
                bool parsed = false;

                string responseValue = response.Substring(equalIndex + 1);

#if WindowsCE
                try
                {
                    parsedValue = Double.Parse(responseValue);
                    parsed = true;
                }
                catch (Exception)
                {
                }
#else
                parsed = Double.TryParse(responseValue, out parsedValue);
#endif

                if (parsed)
                    value = parsedValue;
            }

            return value;
        }

        //=========================================================================================
        /// <summary>
        /// Converts data read from a device based on the data type
        /// </summary>
        /// <param name="buffer">the raw data buffer</param>
        /// <returns>The converted value</returns>
        //=========================================================================================
        protected double ConvertData(byte[] buffer)
        {
            double convertedValue;

            DeviceDataTypes dataType = (DeviceDataTypes)buffer[0];

            switch (dataType)
            {
                case DeviceDataTypes.Char:
                case DeviceDataTypes.UChar:
                case DeviceDataTypes.SChar:
                    {
                        byte value = (byte)BitConverter.ToChar(buffer, 1);
                        convertedValue = (double)value;
                        break;
                    }
                case DeviceDataTypes.SShort:
                case DeviceDataTypes.SInt:
                    {
                        short value = (short)BitConverter.ToInt16(buffer, 1);
                        convertedValue = (double)value;
                        break;
                    }
                case DeviceDataTypes.UShort:
                case DeviceDataTypes.UInt:
                    {
                        ushort value = (ushort)BitConverter.ToUInt16(buffer, 1);
                        convertedValue = (double)value;
                        break;
                    }
                case DeviceDataTypes.SLong:
                    {
                        int value = (int)BitConverter.ToInt32(buffer, 1);
                        convertedValue = (double)value;
                        break;
                    }
                case DeviceDataTypes.ULong:
                    {
                        uint value = (uint)BitConverter.ToUInt32(buffer, 1);
                        convertedValue = (double)value;
                        break;
                    }
                case DeviceDataTypes.SLLong:
                    {
                        long value = (long)BitConverter.ToInt64(buffer, 1);
                        convertedValue = (double)value;
                        break;
                    }
                case DeviceDataTypes.ULLong:
                    {
                        ulong value = (ulong)BitConverter.ToUInt64(buffer, 1);
                        convertedValue = (double)value;
                        break;
                    }
                case DeviceDataTypes.Float:
                    {
                        float value = (float)BitConverter.ToSingle(buffer, 1);
                        convertedValue = (double)value;
                        break;
                    }
                case DeviceDataTypes.Double:
                    {
                        convertedValue = (double)BitConverter.ToDouble(buffer, 1);
                        break;
                    }
                default:
                    convertedValue = double.NaN;
                    break;
            }

            return convertedValue;
        }

        //====================================================================
        /// <summary>
        /// Starts the input scan thread
        /// </summary>
        //====================================================================
        protected ErrorCodes StartInputScan()
        {
            if (ValidateCriticalParams())
            {
                if (m_onDataAvailableCallbackControl != null)
                {
                    if (m_criticalParams.InputSampleMode == SampleMode.Finite)
                    {
                        if (m_onDataAvailableCallbackControl.NumberOfSamples > m_totalSamplesToReadPerChannel)
                        {
                            return ErrorCodes.CallbackCountGreaterThanRequestedSamples;
                        }
                    }
                }

                // initialize scan variables
                m_inputScanCount = 0;
                m_startInputScan = false;
                m_inputScanIndex = -1; // equivalent to UL GetStatus curIndex
                m_currentInputScanReadIndex = 0;
                m_lastInputScanWriteIndex = -1;
                m_deferredMessagesSent = false;
                m_inputSamplesReadPerChannel = 0;
                m_inputScanComplete = false;
                m_errorCode = ErrorCodes.NoErrors;
                m_stopInputScan = false;
                m_invokeCallbackCount = 0;
                m_inputBufferFilled = false;
                m_callbackInfoQueue.Clear();

                // set scaling/cal coefficients
                SetADScalingCoefficients();

                // start the Process Input Scan thread
                m_inputScanThread = new Thread(new ThreadStart(ProcessInputScanThread));
                m_inputScanThread.Name = "InputScanTread";
                m_inputScanThread.Start();
                m_inputScanStarted = true;

                // wait for the ProcessInputScanThread to send the actual START message to the device
                while (!m_deferredMessagesSent)
                    Thread.Sleep(0);
            }

            return ErrorCodes.NoErrors;
        }

        //======================================================================================
        /// <summary>
        /// Start an output scan
        /// </summary>
        /// <returns></returns>
        //======================================================================================
        protected ErrorCodes StartOutputScan()
        {
            m_errorCode = ErrorCodes.NoErrors;
            m_deferredMessagesSent = false;
            m_numberOfSamplesPerChannelWrittenToDevice = 0;
            m_outputScanComplete = false;
            m_outputScanCount = 0;
            m_outputScanIndex = 0;
            m_outputScanState = ScanState.Idle;
            m_totalBytesReceivedByDevice = 0;
            m_stopOutputScan = false;

            m_criticalParams.AoChannelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;

            m_outputScanThread = new Thread(new ThreadStart(ProcessOutputScanThread));
            m_outputScanThread.Name = "OutputScanThread";
            m_outputScanThread.Start();
            m_outputScanStarted = true;

            // wait for the ProcessInputScanThread to send the actual START message to the device
            while (!m_deferredMessagesSent)
                Thread.Sleep(0);

            return ErrorCodes.NoErrors;
        }

        protected object queueOutputTransferLock = new object();

        //==========================================================================================
        /// <summary>
        /// Checks critical params before starting the scan
        /// </summary>
        /// <returns>True if the params are valid otherwise false</returns>
        //==========================================================================================
        protected bool ValidateCriticalParams()
        {
            if (m_criticalParams.LowAiChannel > m_criticalParams.HighAiChannel)
            {
                m_errorCode = ErrorCodes.LowChannelIsGreaterThanHighChannel;
                return false;
            }

            int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);

            if (m_criticalParams.InputSampleMode == SampleMode.Finite)
            {
                if ((byteRatio * m_criticalParams.AiChannelCount * m_criticalParams.InputScanSamples) > m_internalReadBuffer.Length)
                {
                    m_errorCode = ErrorCodes.InputSamplesGreaterThanBufferSize;
                    return false;
                }
            }

            m_errorCode = m_daqDevice.Ai.ValidateSampleCount();

            if (m_errorCode == ErrorCodes.NoErrors)
                m_errorCode = m_daqDevice.Ai.ValidateScanRate();

            if (m_errorCode != ErrorCodes.NoErrors)
                return false;

            return true;
        }

        //============================================================================================
        /// <summary>
        /// Sets the slopes and offsets based on calibration and scale settings 
        /// These values will be applied while copying data to the internal or external read buffer
        /// </summary>
        //============================================================================================
        protected void SetADScalingCoefficients()
        {
            List<Double> slopes = new List<Double>();
            List<Double> offsets = new List<Double>();

            if (CriticalParams.ScanType == ScanType.AnalogInput)
            {
                // default offset and slope
                double slope = 1.0;
                double offset = 0.0;

                foreach (ActiveChannels ac in m_daqDevice.Ai.ActiveChannels)
                {
                    if (m_criticalParams.ScaleAiData)
                    {
                        double scale = ac.UpperLimit - ac.LowerLimit;

                        if (ac.LowerLimit < 0)
                            offset = -1.0 * (scale / 2.0);

                        double lsb = scale / Math.Pow(2.0, m_criticalParams.AiDataWidth);

                        if (m_criticalParams.CalibrateAiData)
                        {
                            // scale and calibrate
                            slope = ac.CalSlope * lsb;
                            offset = ac.CalOffset * lsb + offset;
                        }
                        else
                        {
                            // scale only
                            slope = lsb;
                        }
                    }
                    else if (m_criticalParams.CalibrateAiData)
                    {
                        // calibrate only
                        slope = ac.CalSlope;
                        offset = ac.CalOffset;
                    }

                    slopes.Add(slope);
                    offsets.Add(offset);
                }


                m_criticalParams.AiSlopes = slopes.ToArray();
                m_criticalParams.AiOffsets = offsets.ToArray();
            }
        }

        //=================================================================================================
        /// <summary>
        /// Waits for the input scan thread to complete
        /// </summary>
        /// <param name="checkDeviceStatus">Indicates if the device's status should be checked</param>
        //=================================================================================================
        protected void StopInputScan(bool checkDeviceStatus)
        {
            m_initiateStopForInput = false;

            m_stopInputScan = true;

            ErrorCodes ec = m_errorCode;

            // pass stop request down to platform interop object
            m_platformInterop.StopInputTransfers();

            // stop the input scan thread
            if (m_inputScanThread != null && !m_inputScanComplete)
                m_inputScanThread.Join();

            if (checkDeviceStatus)
            {
                do
                {
                    Thread.Sleep(10);
                }
                while (GetInputScanState() == ScanState.Running);
            }

            // preserve the error code that was set prior to getting the input scan state.
            m_errorCode = ec;

            CriticalParams.ScanType = ScanType.None;

            m_inputScanThread = null;
        }

        protected void StopOutputScan()
        {
            StopOutputScan(false);
        }

        //====================================================================================================
        /// <summary>
        /// Stops an output scan
        /// </summary>
        /// <param name="checkDeviceStatus">A flag indicating if the device status should be checked</param>
        //====================================================================================================
        protected void StopOutputScan(bool checkDeviceStatus)
        {
            m_stopOutputScan = true;
            m_initiateStopForOutput = false;
            m_currentOutputScanWriteIndex = 0;

            ErrorCodes ec = m_errorCode;

            if (ec != ErrorCodes.DataUnderrun)
                m_outputScanState = ScanState.Idle;

            // pass stop request down to platform interop object
            m_platformInterop.StopOutputTransfers();

            // stop the input scan thread
            if (m_outputScanThread != null && !m_outputScanComplete)
                m_outputScanThread.Join();

            if (checkDeviceStatus)
            {
                do
                {
                    Thread.Sleep(10);
                }
                while (GetOutputScanState() == ScanState.Running);
            }

            // preserve the error code that was set prior to getting the input scan state.
            m_errorCode = ec;

            CriticalParams.ScanType = ScanType.None;

            m_outputScanThread = null;
        }

        //========================================================================
        /// <summary>
        /// This gets called when StopOutputScan gets called asyncnronously
        /// </summary>
        /// <param name="ar">The async result</param>
        //========================================================================
        static void StopOutputScanCallback(IAsyncResult ar)
        {
            // retrieve the delegate
            StopOutputScanDelegate caller = (StopOutputScanDelegate)ar.AsyncState;

            // Call EndIinvoke 
            caller.EndInvoke(ar);
        }

        //========================================================================
        /// <summary>
        /// Transmits any messages that have been deferred such as "AISCAN:START"
        /// or "AOSCAN:START"
        /// </summary>
        //========================================================================
        protected void TransmitDeferredMessages()
        {
            UsbSetupPacket messagePacket = null;

            m_platformInterop.ControlTransferMutex.WaitOne();

            do
            {
                if (m_deferredMessages.Count > 0)
                    messagePacket = m_deferredMessages.Dequeue();

                if (messagePacket != null)
                {
                    m_platformInterop.UsbControlOutRequest(messagePacket);
                    m_deferredResponses.Add(m_ae.GetString(messagePacket.Buffer, 0, messagePacket.Buffer.Length).Trim(new char[] {Constants.NULL_TERMINATOR}));
                }

            } while (messagePacket != null && m_deferredMessages.Count > 0);

            m_platformInterop.ControlTransferMutex.ReleaseMutex();
        }

        //====================================================================
        /// <summary>
        /// Processes bulk read requests for input scan on a separate thread
        /// This method will copy bulk in request buffers to an internal
        /// managed buffer or an extenral unmanaged buffer
        /// </summary>
        //====================================================================
        protected void ProcessInputScanThread()
        {
            System.Diagnostics.Debug.Assert(m_criticalParams.InputXferSize != 0);

            bool usingInternalBuffer = true;

            if (m_onDataAvailableCallbackControl != null)
                m_onDataAvailableCallback = new CallbackDelegate(m_onDataAvailableCallbackControl.NotifyApplication);

            if (m_onInputScanCompleteCallbackControl != null)
                m_onInputScanCompleteCallback = new CallbackDelegate(m_onInputScanCompleteCallbackControl.NotifyApplication);

            if (m_onInputScanErrorCallbackControl != null)
                m_onInputScanErrorCallback = new CallbackDelegate(m_onInputScanErrorCallbackControl.NotifyApplication);

            int triggerRearmByteCount = 0;
            int readBufferLength;
            int callbackCount = 0;
            int availableSamplesForCallbackPerChannel = 0;
            int callbackSamples = 0;
            int rearmTriggerCount = 0;

            m_availableSamplesForCallbackSinceStartOfScan = 0;
            m_terminateCallbacks = false;

            m_totalBytesReceived = 0;

            if (m_onDataAvailableCallbackControl != null)
            {
                TerminateCallbacks = false;
                m_onDataAvailableCallbackControl.Abort = false;
                m_callbackThread = new Thread(new ThreadStart(ProcessCallbackThread));
                m_callbackThread.Start();
            }

            unsafe
            {
                if (m_externalReadBuffer != null)
                    usingInternalBuffer = false;
            }

            // get the buffer size that the platform interop object calculates from the scan rate
            int optimalBufferSize = m_criticalParams.InputXferSize;

            int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);
            int channelCount = m_criticalParams.AiChannelCount;

            m_totalBytesToRead = channelCount * (byteRatio * m_criticalParams.InputScanSamples);

            if (m_criticalParams.InputSampleMode == SampleMode.Continuous)
                m_internalReadBuffer = new byte[2 * m_totalBytesToRead];

            if (usingInternalBuffer)
                readBufferLength = m_internalReadBuffer.Length;
            else
                readBufferLength = m_externalReadBufferSize;

            if (m_totalBytesToRead < optimalBufferSize)
                optimalBufferSize = m_totalBytesToRead;

            // for WinUSB buffer size needs to be a mulitple of the max packet size in order to use RAW_IO
            if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
            {
                // single sample only
                optimalBufferSize = byteRatio * channelCount;
            }
            else if (Environment.OSVersion.Platform != PlatformID.Unix &&
                     Environment.OSVersion.Platform != PlatformID.WinCE)
            {
                optimalBufferSize = (int)Math.Ceiling((double)optimalBufferSize / (double)m_criticalParams.InputPacketSize) * m_criticalParams.InputPacketSize;
            }

            // the platform interop object will allocate and return the bulk read buffer
            m_bulkReadBuffer = null;

            // this will queue one or more bulk in requests if the interop object supports asynchronous I/O
            m_platformInterop.PrepareInputTransfers(m_criticalParams.InputScanRate,
                                                    m_totalBytesToRead,
                                                    optimalBufferSize);

            // this will start the device scan
            TransmitDeferredMessages();

            m_deferredMessagesSent = true;

            int numberOfBulkTransfersToExecute;
            uint bytesReceivedInCurrentTransfer = 0;
            uint totalBytesTransfered = 0;
            uint bytesToTransfer = 0;

            while (ContinueProcessingInputScan(m_errorCode))
            {
                bytesReceivedInCurrentTransfer = 0;
                totalBytesTransfered = 0;
                bytesToTransfer = 0;

                m_inputScanStatus = ScanState.Running;

                // if the bulk read buffer length is greater than the max transfer size, then we'll need multiple transfers
                numberOfBulkTransfersToExecute = (int)Math.Ceiling((double)optimalBufferSize / (double)m_platformInterop.MaxTransferSize);

                for (int i = 0; i < numberOfBulkTransfersToExecute; i++)
                {
                    // calculate the number of bytes to process in this transfer
                    if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
                        bytesToTransfer = (uint)(byteRatio * channelCount);
                    else
                        bytesToTransfer = (uint)Math.Min(optimalBufferSize, (m_totalBytesToRead - (int)totalBytesTransfered));

                    // if the input scan is finite then check if this is the last transfer
                    if (m_criticalParams.InputSampleMode == SampleMode.Finite)
                    {
                        if (m_lastInputScanWriteIndex + bytesToTransfer > readBufferLength)
                            bytesToTransfer = (uint)(readBufferLength - m_lastInputScanWriteIndex) - 1;
                    }

                    //*********************************************************************************************************
                    // Read the data on the bulk in pipe
                    //*********************************************************************************************************
                    m_errorCode = m_platformInterop.UsbBulkInRequest(ref m_bulkReadBuffer, ref bytesReceivedInCurrentTransfer);

                    // update the total number of bytes received
                    m_totalBytesReceived += (int)bytesReceivedInCurrentTransfer;

                    // m_bulkInReadBuffer could be null if the input scan was stopped with the Stop command

                    if (m_errorCode == (int)ErrorCodes.NoErrors && m_bulkReadBuffer != null)
                    {
                        // update the total number of bytes transfered so far
                        totalBytesTransfered += bytesReceivedInCurrentTransfer;

                        try
                        {
                            if (m_criticalParams.DeltaRearmInputSamples > 0)
                            {
                                triggerRearmByteCount += (int)bytesToTransfer;

                                if (triggerRearmByteCount >= (byteRatio * channelCount * m_criticalParams.DeviceInputSampleCount))
                                {
                                    rearmTriggerCount++;
                                    bytesToTransfer -= (uint)(byteRatio * channelCount * m_criticalParams.DeltaRearmInputSamples);
                                    m_totalBytesReceived -= byteRatio * channelCount * m_criticalParams.DeltaRearmInputSamples;
                                    triggerRearmByteCount = 0;
                                }
                            }

                            // upate the number of samples acquired so far per channel
                            lock (m_inputScanCountLock)
                            {
                                m_inputScanCount = (m_totalBytesReceived / byteRatio) / channelCount;
                            }

                            // update values used for the callback method
                            availableSamplesForCallbackPerChannel = (int)m_inputScanCount - m_inputSamplesReadPerChannel;
                            availableSamplesForCallbackPerChannel = Math.Min(m_internalReadBuffer.Length / (channelCount * byteRatio), availableSamplesForCallbackPerChannel);
                            callbackCount += ((int)bytesReceivedInCurrentTransfer / byteRatio) / channelCount;

                            if (usingInternalBuffer)
                            {
                                int bytesToCopyOnFirstPass;
                                int bytesToCopyOnSecondPass;
                                
                                if (m_lastInputScanWriteIndex + bytesToTransfer >= readBufferLength)
                                {
                                    // two passes are required since the current input scan write index
                                    // wrapped around to the beginning of the internal read buffer
                                    bytesToCopyOnFirstPass = readBufferLength - (m_lastInputScanWriteIndex + 1);
                                    bytesToCopyOnSecondPass = (int)bytesToTransfer - bytesToCopyOnFirstPass;
                                    m_inputBufferFilled = true;
                                }
                                else
                                {
                                    // only one pass is required since the current input scan write index
                                    // did not wrap around
                                    bytesToCopyOnFirstPass = (int)bytesToTransfer;
                                    bytesToCopyOnSecondPass = 0;
                                }

                                CopyToInternalReadBuffer(m_bulkReadBuffer.Data,
                                                         m_internalReadBuffer,
                                                         bytesToCopyOnFirstPass,
                                                         bytesToCopyOnSecondPass);
                            }
                            else
                            {
                                unsafe
                                {
                                    CopyToExternalReadBuffer(m_bulkReadBuffer.Data,
                                                             m_externalReadBuffer,
                                                             readBufferLength,
                                                             bytesToTransfer);
                                }
                            }

                            // calculate current index
                            m_inputScanIndex = m_inputScanCount;

                            if (m_inputScanIndex >= (readBufferLength / byteRatio))
                                m_inputScanIndex = m_inputScanCount % (readBufferLength / byteRatio);

                            // add the m_bulkReadBuffer to the ready buffers queue so it may be reused
                            m_platformInterop.QueueBulkInReadyBuffers(m_bulkReadBuffer, QueueAction.Enqueue);
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Forms.MessageBox.Show(ex.Message);
                            m_errorCode = ErrorCodes.UnknownError;
                        }

                        //*****************************************************************************************
                        // OnDataAvailable callback
                        //*****************************************************************************************
                        if (m_errorCode == ErrorCodes.NoErrors)
                        {
                            if (m_onDataAvailableCallbackControl != null && m_onDataAvailableCallbackControl.Created)
                            {
                                if (callbackCount >= m_onDataAvailableCallbackControl.NumberOfSamples)
                                {
                                    callbackSamples = availableSamplesForCallbackPerChannel;

                                    m_availableSamplesForCallbackSinceStartOfScan += callbackCount;
                                    callbackCount = 0;

                                    if (callbackSamples > 0)
                                    {
                                        QueueCallbackInfo(callbackSamples, m_inputSamplesReadPerChannel, QueueAction.Enqueue);
                                    }
                                }
                                else
                                {
                                    if (m_criticalParams.InputSampleMode == SampleMode.Finite)
                                    {
                                        if (m_totalBytesReceived >= m_totalBytesToRead && callbackCount > 0)
                                            TerminateCallbacks = true;
                                    }
                                }
                            }
                        }
                    }
                }

                //*****************************************************************************************
                // OnInputScanError callback
                //*****************************************************************************************
                if (m_errorCode != ErrorCodes.NoErrors)
                {
                    m_callbackInfoQueue.Clear();
                    TerminateCallbacks = true;

                    if (m_onInputScanErrorCallbackControl != null && m_onInputScanErrorCallbackControl.Created)
                    {
                        m_invokeCallbackCount++;
                        m_onInputScanErrorCallbackControl.BeginInvoke(m_onInputScanErrorCallback, m_scanErrorCallbackParam);
                    }
                }

                Thread.Sleep(1);
            }

            m_inputScanStatus = ScanState.Idle;
            m_inputScanComplete = true;
            m_inputScanStarted = false;

            if (m_onDataAvailableCallbackControl != null)
            {
                // if this is finite mode and a normal end of scan, then let the callback thread complete
                if (m_criticalParams.InputSampleMode == SampleMode.Finite && !m_stopInputScan && m_errorCode == ErrorCodes.NoErrors)
                {
                    if (m_callbackThread != null)
                        m_callbackThread.Join();
                }
                else
                {
                    // terminate the callbacks otherwise we'll be stuck in this thread
                    m_callbackInfoQueue.Clear();
                    TerminateCallbacks = true;

                    if (m_callbackThread != null)
                        m_callbackThread.Join();
                }
            }

            //*****************************************************************************************
            // OnInputScanComplete callback
            //*****************************************************************************************
            if (m_onInputScanCompleteCallbackControl != null && m_onInputScanCompleteCallbackControl.Created)
            {
                int samplesPerChannelRead = m_currentInputScanReadIndex / (byteRatio * channelCount);

                if (m_criticalParams.InputSampleMode == SampleMode.Finite && !m_stopInputScan)
                    m_scanCompleteCallbackParam[0] = m_totalSamplesToReadPerChannel - samplesPerChannelRead;
                else
                    m_scanCompleteCallbackParam[0] = availableSamplesForCallbackPerChannel;

                m_invokeCallbackCount++;
                m_onInputScanCompleteCallbackControl.BeginInvoke(m_onInputScanCompleteCallback, m_scanCompleteCallbackParam);
            }
        }

        //===============================================================================================================================
        /// <summary>
        /// Handles invocation of OnDataAvailable callback
        /// </summary>
        //===============================================================================================================================
        protected void ProcessCallbackThread()
        {
            IAsyncResult asyncCallbackResult;
            object[] callbackParam = new object[1];
            int samplesReadPerChannel;
            int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);
            int channelCount = m_criticalParams.AiChannelCount;
            int availableSamplesPerChannel;
            int samplesSentToCallbackPerChannel = 0;

            while (!TerminateCallbacks &&
                   ((m_criticalParams.InputSampleMode == SampleMode.Continuous) ||
                    (m_criticalParams.InputSampleMode == SampleMode.Finite && samplesSentToCallbackPerChannel < m_criticalParams.InputScanSamples)))
            {
                CallbackInfo ci = QueueCallbackInfo(0, 0, QueueAction.Dequeue);

                if (ci != null)
                {
                    availableSamplesPerChannel = ci.AvailableSamplesPerChannel;

                    if (availableSamplesPerChannel > 0)
                    {
                        // use the current scan index, byte ratio and channel count to calculate
                        // the number of samples read per channel
                        samplesReadPerChannel = m_currentInputScanReadIndex / (byteRatio * channelCount);

                        // if more samples were read after ci was queued then we'll need to adjust the
                        // available samples.
                        if (samplesReadPerChannel > ci.SamplesReadPerChannel)
                            availableSamplesPerChannel = samplesReadPerChannel - ci.SamplesReadPerChannel;

                        samplesSentToCallbackPerChannel = availableSamplesPerChannel + samplesReadPerChannel;

                        // set the callback param to the number of available samples
                        callbackParam[0] = availableSamplesPerChannel;

                        // Asyncronously invoke the callback
                        if (m_onDataAvailableCallbackControl != null)
                        {
                            asyncCallbackResult = m_onDataAvailableCallbackControl.BeginInvoke(m_onDataAvailableCallback, callbackParam);

                            while (!asyncCallbackResult.IsCompleted)
                            {
                                // wait for the callback to complete or abort
                                if (TerminateCallbacks)
                                {
                                    m_onDataAvailableCallbackControl.Abort = true;
                                    break;
                                }

                                Thread.Sleep(0);
                            }

                            if (!TerminateCallbacks)
                            {
                                // complete callback invocation
                                if (m_onDataAvailableCallbackControl != null)
                                    m_onDataAvailableCallbackControl.EndInvoke(asyncCallbackResult);
                            }
                        }
                    }
                }

                if (m_onDataAvailableCallbackControl == null)
                    break;

                Thread.Sleep(1);
            }
        }

        //===============================================================================================================================
        /// <summary>
        /// Copies the most recent bulk in buffer to the managed internal read buffer
        /// </summary>
        /// <param name="bulkReadBuffer">The buffer that just received a bulk in transfer</param>
        /// <param name="internalReadBuffer">the internal read buffer to copy data to</param>
        /// <param name="bytesToCopyOnFirstPass">The bytes to copy on the first pass</param>
        /// <param name="bytesToCopyOnSecondPass">The bytes to copy on the second pass</param>
        //===============================================================================================================================
        protected void CopyToInternalReadBuffer(byte[] bulkReadBuffer, 
                                                byte[] internalReadBuffer,
                                                int bytesToCopyOnFirstPass, 
                                                int bytesToCopyOnSecondPass)
        {
            try
            {
                // data can be copied in either 1 or 2 passes. The second pass is used when the current input
                // scan write index wraps around to the beginning of the internal read buffer
                Array.Copy(bulkReadBuffer, 0, internalReadBuffer, (m_lastInputScanWriteIndex + 1), bytesToCopyOnFirstPass);

                // update the current input scan write index with the number of bytes copied on the first pass
                m_lastInputScanWriteIndex += bytesToCopyOnFirstPass;

                if (bytesToCopyOnSecondPass > 0)
                {
                    m_lastInputScanWriteIndex = -1;
                    Array.Copy(bulkReadBuffer, bytesToCopyOnFirstPass, internalReadBuffer, 0, bytesToCopyOnSecondPass);
                    m_lastInputScanWriteIndex += bytesToCopyOnSecondPass;
                }
            }
            catch (Exception)
            {
                m_errorCode = ErrorCodes.InternalReadBufferError;
            }
        }

        //===============================================================================================================================
        /// <summary>
        /// Copies the most recent bulk in buffer to the unmanaged external read buffer
        /// </summary>
        /// <param name="bulkReadBuffer">The buffer that just received a bulk in transfer</param>
        /// <param name="externalReadBuffer">the external read buffer to copy data to</param>
        /// <param name="readBufferLength">The length of the external read buffer</param>
        /// <param name="bytesToTransfer">The number of bytes to transfer</param>
        //===============================================================================================================================
        protected unsafe void CopyToExternalReadBuffer(byte[] bulkReadBuffer,
                                                       void* externalReadBuffer,
                                                       int readBufferLength,
                                                       uint bytesToTransfer)
        {
            unsafe
            {
                try
                {
                    int lastInputScanWriteIndex = m_lastInputScanWriteIndex;

                    if (m_criticalParams.ScanType == ScanType.AnalogInput)
                    {
                        m_daqDevice.Ai.CopyToExternalReadBuffer(bulkReadBuffer, 
                                                                externalReadBuffer, 
                                                                readBufferLength, 
                                                                bytesToTransfer,
                                                                ref lastInputScanWriteIndex);

                        m_lastInputScanWriteIndex = lastInputScanWriteIndex;
                    }
                }
                catch (Exception)
                {
                    m_errorCode = ErrorCodes.ErrorWritingDataToExternalInputBuffer;
                }
            }
        }

        //=================================================================
        /// <summary>
        /// Checks if an input scan in progress is ok to continue processing
        /// </summary>
        /// <returns>true to continue processing otherwise false</returns>
        //=================================================================
        protected bool ContinueProcessingInputScan(ErrorCodes errorCode)
        {
            if (m_stopInputScan && errorCode == ErrorCodes.NoErrors)
            {
                if (m_criticalParams.InputSampleMode == SampleMode.Finite && m_platformInterop.IsInputScanDataAvailable())
                    return true;
                else
                    return false;
            }

            if (errorCode != ErrorCodes.NoErrors)
            {
                if (!m_stopInputScan)
                {
                    m_platformInterop.StopInputTransfers();
                    m_stopInputScan = true;
                }

                return false;
            }

            if (m_criticalParams.InputSampleMode == SampleMode.Finite && 
                m_totalBytesReceived >= 0 &&
                m_totalBytesReceived >= m_totalBytesToRead)
            {
                // we need to stop all threads that were set up to
                // submit bulk in transfers                
                if (Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.WinCE)
                {
                    // even though we've got all our data we may still have to wait
                    // for a possible zero-length packet to complete
                    while (!m_platformInterop.InputTransfersComplete)
                    {
                        Thread.Sleep(1);
                    }

                    m_platformInterop.StopInputTransfers();
                }

                return false;
            }

            return true;
        }

        //====================================================================================================================
        /// <summary>
        /// Processes bulk write requests for an output scan on a separate thread
        /// </summary>
        //====================================================================================================================
        protected void ProcessOutputScanThread()
        {
            if (m_internalWriteBuffer == null || m_internalWriteBuffer.Length == 0)
                m_errorCode = ErrorCodes.OutputBufferNullOrEmtpy;

            int transferSize = m_criticalParams.OutputXferSize;
            int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AoDataWidth / (double)Constants.BITS_PER_BYTE);
            int channelCount = m_criticalParams.AoChannelCount;
            m_totalBytesToWrite = (byteRatio * channelCount * m_criticalParams.OutputScanSamples);

            m_platformInterop.DriverInterfaceOutputBuffer = m_internalWriteBuffer;

			m_platformInterop.ReadyToSubmitRemainingOutputTransfers = false;
			
            m_platformInterop.PrepareOutputTransfers(m_criticalParams.OutputScanRate, m_totalBytesToWrite, transferSize);

			while (!m_platformInterop.ReadyToStartOutputScan)
			{
				Thread.Sleep(1);
			}
			
            TransmitDeferredMessages();
			
			m_platformInterop.ReadyToSubmitRemainingOutputTransfers = true;

            m_deferredMessagesSent = true;

            do
            {
                if (m_outputScanState != ScanState.Running)
                    m_outputScanState = ScanState.Running;

                if (m_platformInterop.ErrorCode == ErrorCodes.NoErrors)
                {
                    m_totalBytesReceivedByDevice = m_platformInterop.TotalBytesReceivedByDevice;

                    m_currentOutputScanOutputIndex = (m_totalBytesReceivedByDevice - 1) % m_internalWriteBuffer.Length;

                    // update count and index
                    m_outputScanCount = m_totalBytesReceivedByDevice / (byteRatio * channelCount);
                    m_outputScanIndex = m_currentOutputScanOutputIndex / (byteRatio * channelCount);
                }
                else
                {
                    m_errorCode = m_platformInterop.CheckUnderrun();

                    if (m_errorCode == ErrorCodes.DataUnderrun)
                    {
                        m_platformInterop.ClearDataUnderrun();
                        m_outputScanState = ScanState.Underrun;
                    }
#if !WindowsCE
                    m_stopOutputScanDelegate.BeginInvoke(m_stopOutputScanCallback, m_stopOutputScanDelegate);
#endif
                }

                Thread.Sleep(0);

            } while (ContinueProcessingOutputScan(m_errorCode));

            // at this point all data has been accepted by the device for a finite scan
            // or the scan has been stopped so now wait for the actual device to go idle
            while (m_outputScanState == ScanState.Running)
            {
                m_outputScanState = GetOutputScanState();
                Thread.Sleep(1);
            }

            //if (m_outputScanState == ScanState.Running)
            //    m_outputScanState = ScanState.Idle;

            m_currentOutputScanWriteIndex = 0;
            m_outputScanComplete = true;
        }

        //=================================================================
        /// <summary>
        /// Checks if an output scan in progress is ok to continue processing
        /// </summary>
        /// <returns>true to continue processing otherwise false</returns>
        //=================================================================
        protected bool ContinueProcessingOutputScan(ErrorCodes errorCode)
        {
            if (m_stopOutputScan)
                return false;

            if (m_errorCode != ErrorCodes.NoErrors)
                return false;

            if (m_criticalParams.OutputSampleMode == SampleMode.Finite &&
                m_totalBytesReceivedByDevice >= m_totalBytesToWrite)
                return false;

            return true;
        }

        //===============================================================================
        /// <summary>
        /// Returns the last device response as a string
        /// </summary>
        /// <returns>The device response</returns>
        //===============================================================================
        internal string ReadString()
        {
            return m_internalReadString;
        }

        //===============================================================================
        /// <summary>
        /// Returns the last device component Value property as a numeric
        /// </summary>
        /// <returns>The component Value property</returns>
        //===============================================================================
        internal double ReadValue()
        {
            return m_internalReadValue;
        }

         //===================================================================================================
        /// <summary>
        /// Reads a device's memory
        /// </summary>
        /// <param name="offset">The starting addresss</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer containing the memory contents</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal ErrorCodes ReadDeviceMemory(ushort offset, byte count, out byte[] buffer)
        {
            return m_platformInterop.ReadDeviceMemory(offset, count, out buffer);
        }

        //===================================================================================================
        /// <summary>
        /// Unlocks a device's memory for writing to it
        /// </summary>
        /// <param name="address">The address where the unlock code should be written to</param>
        /// <param name="unlockCode">The unlock code</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal ErrorCodes UnlockDeviceMemory(ushort address, ushort unlockCode)
        {
            return m_platformInterop.UnlockDeviceMemory(address, unlockCode);
        }

        //===================================================================================================
        /// <summary>
        /// Locks a device's memory to prevent writing to it
        /// </summary>
        /// <param name="address">The address where the lock code should be written to</param>
        /// <param name="unlockCode">The unlock code</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal ErrorCodes LockDeviceMemory(ushort address, ushort lockCode)
        {
            return m_platformInterop.LockDeviceMemory(address, lockCode);
        }

        //===================================================================================================
        /// <summary>
        /// Writes to a device's memory
        /// </summary>
        /// <param name="offset">The starting addresss</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer containing the contents to write</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal ErrorCodes WriteDeviceMemory(ushort offset, ushort bufferOffset, byte[] buffer, byte count)
        {
            return m_platformInterop.WriteDeviceMemory(offset, bufferOffset, buffer, count);
        }

        //===============================================================================
        /// <summary>
        /// Creates a set of USB packets to send to the device and is based
        /// on the message contents
        /// </summary>
        /// <param name="message">The message to send to the device</param>
        //===============================================================================
        protected void CreateUsbPackets(byte[] message)
        {
            string msg = m_ae.GetString(message, 0, message.Length).ToUpper();

            // clear the list of packets
            m_usbPackets.Clear();

            m_startInputScan = false;
            m_startOutputScan = false;

            bool deferMessage = CheckForCriticalParams(msg);

            for (int i = 0; i < message.Length; i++)
                m_controlOutPacket.Buffer[i] = message[i];

            m_controlOutPacket.DeferTransfer = deferMessage;
            m_controlOutPacket.BytesTransfered = 0;

            m_usbPackets.Add(m_controlOutPacket);

            if (!m_controlOutPacket.DeferTransfer)
            {
                // add a control in packet to receive the text response
                if (msg.Contains("?"))
                    m_controlInPacket.IsQuery = true;

                //if (!msg.Contains(DaqComponents.DEV) && !msg.Contains(DaqCommands.RESET))
                    m_usbPackets.Add(m_controlInPacket);
            }
        }

        //====================================================================================
        /// <summary>
        /// Checks the content of the message to see if any of the information
        /// needs to be stored. Also determines if the message should be deferred
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>True if the message should be deferred otherwise false</returns>
        //====================================================================================
        protected bool CheckForCriticalParams(string message)
        {
            bool deferMessage = false;

            // if the message is a query, simply return
            if (message.Contains("?"))
            {
                if (message.Contains(DaqComponents.AI) && message.Contains(DaqProperties.VALUE))
                {
                    int ch = MessageTranslator.GetChannel(message);
                    m_criticalParams.AiChannel = ch;
                }

                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqCommands.START))
            {
                m_startInputScan = true;

                // update these in case the buffer size was changed
                if (m_inputBufferSizeOverride)
                {
                    if (m_internalReadBuffer.Length % m_criticalParams.AiChannelCount != 0)
                    {
                        int length = m_criticalParams.AiChannelCount * (int)Math.Ceiling((double)m_internalReadBuffer.Length / (double)m_criticalParams.AiChannelCount);

                        if (m_internalReadBuffer.Length != length)
                            m_internalReadBuffer = new byte[length];
                    }

                    if (m_criticalParams.InputSampleMode == SampleMode.Continuous)
                    {
                        int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);
                        m_totalSamplesToReadPerChannel = m_internalReadBuffer.Length / (byteRatio * m_criticalParams.AiChannelCount);
                        m_criticalParams.InputScanSamples = m_totalSamplesToReadPerChannel;
                    }
                }

                return true;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqCommands.STOP))
            {
                m_startInputScan = false;
                m_initiateStopForInput = true;

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    return  true;

                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.SAMPLES) && message.Contains("=0"))
            {
                // Continuous mode
                m_totalSamplesToReadPerChannel = 1000 * m_criticalParams.InputPacketSize;
                m_criticalParams.InputScanSamples = m_totalSamplesToReadPerChannel;
                m_criticalParams.InputSampleMode = SampleMode.Continuous;
                m_criticalParams.InputXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.SAMPLES))
            {
                // Finite mode
                m_totalSamplesToReadPerChannel = MessageTranslator.GetSamples(message);
                m_criticalParams.InputScanSamples = m_totalSamplesToReadPerChannel;
                m_criticalParams.InputSampleMode = SampleMode.Finite;
                m_criticalParams.InputXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.XFERMODE))
            {
                TransferMode tm = MessageTranslator.GetTransferMode(message);
                m_criticalParams.InputTransferMode = tm;
                m_criticalParams.InputXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.HIGHCHAN))
            {
                int ch = MessageTranslator.GetChannel(message);
                m_criticalParams.HighAiChannel = ch;
                m_criticalParams.InputXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.LOWCHAN))
            {
                int ch = MessageTranslator.GetChannel(message);
                m_criticalParams.LowAiChannel = ch;
                m_criticalParams.InputXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.RATE))
            {
                double rate = MessageTranslator.GetRate(message);

                if (rate == 0.0)
                {
                    m_errorCode = ErrorCodes.InputScanRateCannotBeZero;
                }
                else
                {
                    m_criticalParams.InputScanRate = rate;
                    m_criticalParams.InputXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                }

                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.QUEUE) && message.Contains(PropertyValues.ENABLE))
            {
                m_criticalParams.AiQueueEnabled = true;
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.QUEUE) && message.Contains(PropertyValues.DISABLE))
            {
                m_criticalParams.AiQueueEnabled = false;
                return false;
            }

            else if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.TRIG))
            {
                if (message.Contains(PropertyValues.ENABLE))
                    m_criticalParams.InputTriggerEnabled = true;
                else if (message.Contains(PropertyValues.DISABLE))
                    m_criticalParams.InputTriggerEnabled = false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqProperties.SAMPLES) && message.Contains("=0"))
            {
                // Continuous mode
                m_totalSamplesToWritePerChannel = INTERNAL_WRITE_BUFFER_SIZE / 2;
                m_criticalParams.OutputScanSamples = m_totalSamplesToWritePerChannel;
                m_criticalParams.OutputSampleMode = SampleMode.Continuous;
                m_criticalParams.OutputXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
                return false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqProperties.SAMPLES))
            {
                // Finite mode
                m_totalSamplesToWritePerChannel = MessageTranslator.GetSamples(message);
                m_criticalParams.OutputScanSamples = m_totalSamplesToWritePerChannel;
                m_criticalParams.OutputSampleMode = SampleMode.Finite;
                m_criticalParams.OutputXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
                return false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqProperties.RATE))
            {
                double rate = MessageTranslator.GetRate(message);

                if (rate == 0.0)
                {
                    m_errorCode = ErrorCodes.InputScanRateCannotBeZero;
                }
                else
                {
                    m_criticalParams.OutputScanRate = rate;
                    m_criticalParams.OutputXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
                }

                return false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqProperties.HIGHCHAN))
            {
                int ch = MessageTranslator.GetChannel(message);
                m_criticalParams.HighAoChannel = ch;
                m_criticalParams.AoChannelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;
                m_criticalParams.OutputXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
                return false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqProperties.LOWCHAN))
            {
                int ch = MessageTranslator.GetChannel(message);
                m_criticalParams.LowAoChannel = ch;
                m_criticalParams.AoChannelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;
                m_criticalParams.OutputXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
                return false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqCommands.START))
            {
                m_startOutputScan = true;

                // update these in case the buffer size was changed
                if (m_outputBufferSizeOverride)
                {
                    int channelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;

                    if (m_internalWriteBuffer.Length % channelCount != 0)
                    {
                        int length = channelCount * (int)Math.Ceiling((double)m_internalWriteBuffer.Length / (double)channelCount);

                        if (m_internalWriteBuffer.Length != length)
                            m_internalWriteBuffer = new byte[length];
                    }

                    if (m_criticalParams.OutputSampleMode == SampleMode.Continuous)
                    {
                        m_totalSamplesToWritePerChannel = m_internalWriteBuffer.Length / channelCount;
                        m_criticalParams.OutputScanSamples = m_totalSamplesToWritePerChannel;
                    }
                }

                if (m_criticalParams.InputTransferMode == TransferMode.BurstIO)
                    return false;
                else
                    return true;
            }

            if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqCommands.STOP))
            {
                m_startOutputScan = false;
                m_initiateStopForOutput = true;

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    return true;

                return false;
            }

            return deferMessage;
        }

        //=================================================================================
        /// <summary>
        /// Waits until there is enough fresh data to satisfy the read request
        /// </summary>
        /// <param name="numberOfSamplesRequested">The number of fresh data samples to read</param>
        /// <returns>The number of fresh data samples that can be read</returns>
        //=================================================================================
        internal int WaitForData(int numberOfSamplesRequested, int timeOut)
        {
            int bytesPerSample = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);
            int channelCount = m_criticalParams.AiChannelCount;
            int numberOfBytesRequested = channelCount * (bytesPerSample * numberOfSamplesRequested);
            int numberOfNewBytes = 0;
            long elapsedTime;

            if (m_inputScanState == ScanState.Idle && m_inputSamplesReadPerChannel < m_criticalParams.InputScanSamples && m_inputScanComplete)
            {
                if (m_inputBufferFilled)
                    numberOfNewBytes = m_internalReadBuffer.Length - m_inputSamplesReadPerChannel;
                else
                    numberOfNewBytes = GetFreshDataCount();
            }
            else
            {
                numberOfNewBytes = 0;

                if (m_criticalParams.InputSampleMode == SampleMode.Finite && numberOfSamplesRequested > m_criticalParams.InputScanSamples)
                {
                    numberOfSamplesRequested = m_criticalParams.InputScanSamples;
                }
                else if (numberOfSamplesRequested > m_internalReadBuffer.Length)
                {
                    m_errorCode = ErrorCodes.TooManySamplesRequested;
                    return 0;
                }

                m_readStopWatch.Reset();

                if (timeOut > 0)
                    m_readStopWatch.Start();

                while (numberOfNewBytes < numberOfBytesRequested && !m_stopInputScan)
                {
                    m_errorCode = m_platformInterop.ErrorCode;

                    if (m_errorCode != ErrorCodes.NoErrors)
                        break;

                    numberOfNewBytes = GetFreshDataCount();

                    if (m_inputScanStatus == ScanState.Idle)
                        break;

                    Thread.Sleep(1);

                    System.Windows.Forms.Application.DoEvents();

                    elapsedTime = m_readStopWatch.ElapsedMilliseconds;

                    if (timeOut > 0 && elapsedTime >= timeOut)
                    {
                        m_errorCode = ErrorCodes.InputScanTimeOut;
                        break;
                    }
                }

                m_readStopWatch.Stop();
            }

            numberOfNewBytes = Math.Min(numberOfNewBytes, numberOfBytesRequested);

            // return the number of samples (not the number of bytes)
            return (numberOfNewBytes / bytesPerSample) / channelCount;
        }

        //==============================================================================
        /// <summary>
        /// Waits until there's enough space in the internal write buffer for the
        /// number of bytes needed
        /// </summary>
        /// <param name="numberOfBytes">The number of bytes needed</param>
        /// <param name="timeOut">The time out value in milli-seconds</param>
        /// <returns>The space available in bytes</returns>
        //==============================================================================
        internal int WaitForSpace(int numberOfBytes, int timeOut)
        {
            long elapsedTime;

            m_writeStopWatch.Reset();
            m_writeStopWatch.Start();

            int availableBytes = 0;
            int spaceAvailableFromStartOfBuffer;

            if (m_outputScanState == ScanState.Running)
            {
                while (availableBytes < numberOfBytes && !m_stopOutputScan)
                {
                    elapsedTime = m_writeStopWatch.ElapsedMilliseconds;

                    if (timeOut > 0 && elapsedTime >= timeOut)
                    {
                        m_errorCode = ErrorCodes.OutputScanTimeout;
                        break;
                    }

                    if (m_outputScanState == ScanState.Idle)
                        break;

                    if (!m_overwritingOldScanData && (m_internalWriteBuffer.Length - m_currentOutputScanWriteIndex) >= numberOfBytes)
                    {
                        availableBytes = m_internalWriteBuffer.Length - m_currentOutputScanWriteIndex;
                    }
                    else
                    {
                        spaceAvailableFromStartOfBuffer = (m_totalBytesReceivedByDevice % m_internalWriteBuffer.Length);

                        if (CurrentOutputScanWriteIndex > spaceAvailableFromStartOfBuffer)
                            availableBytes = spaceAvailableFromStartOfBuffer + (m_internalWriteBuffer.Length - CurrentOutputScanWriteIndex);
                        else
                            availableBytes = (m_totalBytesReceivedByDevice % m_internalWriteBuffer.Length) - CurrentOutputScanWriteIndex;
                    }

                    Thread.Sleep(1);
                }
            }
            else
            {
                if (m_outputScanThread == null && m_errorCode != ErrorCodes.NoErrors)
                    m_errorCode = ErrorCodes.NoErrors;

                if (m_errorCode == ErrorCodes.DataUnderrun)
                    m_errorCode = ErrorCodes.NoErrors;

                availableBytes = m_internalWriteBuffer.Length;
            }

            return availableBytes;
        }

        //==============================================================================
        /// <summary>
        /// Waits for input scan state to go Idle
        /// </summary>
        //==============================================================================
        internal void WaitForIdle()
        {
            do
            {
                Thread.Sleep(1);
            }while (GetInputScanState() == ScanState.Running);
            m_inputScanState = ScanState.Idle;
        }

        protected void CheckTriggerRearm()
        {
            UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = ControlRequest.MESSAGE_REQUEST;
            packet.Index = 0;
            packet.Value = 0;
            packet.Length = (ushort)m_aiTrigStatus.Length;
            Array.Copy(m_aiTrigStatus, packet.Buffer, m_aiTrigStatus.Length);

            // send the status message
            m_platformInterop.UsbControlOutRequest(packet);

            // get the status response
            packet.TransferType = UsbTransferTypes.ControlIn;

            m_platformInterop.UsbControlInRequest(packet);

            string response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);

            if (response.Contains(PropertyValues.ENABLE))
            {
                packet.Length = (ushort)m_aiRearmStatus.Length;
                Array.Copy(m_aiRearmStatus, packet.Buffer, m_aiRearmStatus.Length);

                // send the status message
                m_platformInterop.UsbControlOutRequest(packet);

                // get the status response
                packet.TransferType = UsbTransferTypes.ControlIn;

                m_platformInterop.UsbControlInRequest(packet);

                response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);

                if (response.Contains(PropertyValues.ENABLE))
                {
                    Array.Copy(m_aiQuerySamples, packet.Buffer, m_aiQuerySamples.Length);

                    // send request for sample count
                    m_platformInterop.UsbControlOutRequest(packet);

                    // get the sample count
                    packet.TransferType = UsbTransferTypes.ControlIn;

                    m_platformInterop.UsbControlInRequest(packet);

                    response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);
                    response = response.Trim(new char[] { Constants.NULL_TERMINATOR });

                    int samples = MessageTranslator.GetSamples(response);
                    m_criticalParams.DeviceInputSampleCount = samples;

                    int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);
                    int inputBytes = m_criticalParams.AiChannelCount * byteRatio * samples;

                    int rearmBytes = 0;
                    int rearmSamples = 0;

                    if (samples % m_criticalParams.InputXferSize != 0)
                    {
                        // when xfer mode is not SINGLEIO, the rearm bytes need to be an integer multiple of the xfer size
                        if (m_criticalParams.InputTransferMode != TransferMode.SingleIO)
                            rearmBytes = (int)Math.Ceiling((double)inputBytes / (double)m_criticalParams.InputXferSize) * m_criticalParams.InputXferSize;
                        else
                            rearmBytes = inputBytes;

                        rearmSamples = rearmBytes / (m_criticalParams.AiChannelCount * byteRatio);
                        string s = rearmSamples.ToString();
                        int i = DaqComponents.AISCAN.Length + DaqProperties.SAMPLES.Length + 2;
                        foreach (byte b in s)
                        {
                            m_aiScanSamples[i++] = b;
                        }

                        for (int j = i; j < m_aiScanSamples.Length; j++)
                            m_aiScanSamples[j] = 0;

                        Array.Copy(m_aiScanSamples, packet.Buffer, m_aiScanSamples.Length);

                        // send new sample count to the device
                        m_platformInterop.UsbControlOutRequest(packet);
                    }

                    // Delta rearm samples will non-zero when xfer mode is not SINGLEIO
                    if (m_criticalParams.InputTransferMode != TransferMode.SingleIO)
                        m_criticalParams.DeltaRearmInputSamples = rearmSamples - samples;
                    else
                        m_criticalParams.DeltaRearmInputSamples = 0;

                    m_totalSamplesToReadPerChannel = INTERNAL_READ_BUFFER_SIZE / 2;
                    m_criticalParams.InputScanSamples = m_totalSamplesToReadPerChannel;
                    m_criticalParams.InputSampleMode = SampleMode.Continuous;
                }
                else
                {
                    m_criticalParams.DeltaRearmInputSamples = 0;
                }
            }
        }

        //=================================================================
        /// <summary>
        /// Gets the state of an input scan operation
        /// </summary>
        /// <returns>The scan state</returns>
        //=================================================================
        protected ScanState GetInputScanState()
        {
            m_platformInterop.ControlTransferMutex.WaitOne();
        
            UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = ControlRequest.MESSAGE_REQUEST;
            packet.Index = 0;
            packet.Value = 0;
            packet.Length = (ushort)m_aiStatusMessage.Length;
            Array.Copy(m_aiStatusMessage, packet.Buffer, m_aiStatusMessage.Length);

            // send the status message
            m_platformInterop.UsbControlOutRequest(packet);

            // get the status response
            packet.TransferType = UsbTransferTypes.ControlIn;

            m_platformInterop.UsbControlInRequest(packet);

            string response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);

            m_platformInterop.ControlTransferMutex.ReleaseMutex();

            if (response.Contains("RUNNING"))
            {
                m_inputScanState = ScanState.Running;
            }
            else if (response.Contains("OVERRUN"))
            {
                if (!m_stopInputScan)
                {
                    m_errorCode = ErrorCodes.DataOverrun;
                    m_inputScanState = ScanState.Overrun;
                }
                else
                {
                    m_platformInterop.ClearDataOverrun();
                    m_inputScanState = ScanState.Idle;
                }
            }
            else
            {
                m_inputScanState = ScanState.Idle;
            }

            return m_inputScanState;
        }

        //=================================================================
        /// <summary>
        /// Gets the state of an Output scan operation
        /// </summary>
        /// <returns>The scan state</returns>
        //=================================================================
        protected ScanState GetOutputScanState()
        {
            m_platformInterop.ControlTransferMutex.WaitOne();

            UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            packet.TransferType = UsbTransferTypes.ControlOut;
            packet.Request = ControlRequest.MESSAGE_REQUEST;
            packet.Index = 0;
            packet.Value = 0;
            packet.Length = (ushort)m_aoStatusMessage.Length;
            Array.Copy(m_aoStatusMessage, packet.Buffer, m_aoStatusMessage.Length);

            // send the status message
            m_platformInterop.UsbControlOutRequest(packet);

            // get the status response
            packet.TransferType = UsbTransferTypes.ControlIn;

            m_platformInterop.UsbControlInRequest(packet);

            string response = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);

            m_platformInterop.ControlTransferMutex.ReleaseMutex();

            if (response.Contains("RUNNING"))
            {
                m_outputScanState = ScanState.Running;
            }
            else if (response.Contains("UNDERRUN"))
            {
                if (!m_stopInputScan)
                {
                    m_errorCode = ErrorCodes.DataUnderrun;
                    m_outputScanState = ScanState.Underrun;
                }
                else
                {
                    m_platformInterop.ClearDataUnderrun();
                    m_outputScanState = ScanState.Idle;
                }
            }
            else
            {
                m_outputScanState = ScanState.Idle;
            }

            return m_outputScanState;
        }

        //===============================================================================================================
        /// <summary>
        /// Gets the number of new bytes available in the internal read buffer
        /// </summary>
        /// <returns>The number of new bytes</returns>
        //===============================================================================================================
        protected int GetFreshDataCount()
        {
            int numberOfNewBytes;
            int byteRatio = (int)Math.Ceiling((double)m_criticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);

            numberOfNewBytes = (int)Math.Min(m_internalReadBuffer.Length, (byteRatio * m_criticalParams.AiChannelCount) * (m_inputScanCount - m_inputSamplesReadPerChannel));

            return numberOfNewBytes;
        }

        //======================================================================

        /// <summary>
        /// Lets the driver free any resources associated with the device
        /// </summary>
        //======================================================================
        internal void ReleaseDevice()
        {
            m_platformInterop.ReleaseDevice();
        }

        //=============================================================================================================================================
        /// <summary>
        /// Enqueues and dequeues the callback info for the OnDataAvailable callback
        /// </summary>
        /// <param name="avaiableSamplesPerChannel">The number of samples per channel available for the callback</param>
        /// <param name="samplesReadPerChannel">The number of samples per channel that has been read at the time the callback info was queued</param>
        /// <param name="queueAction">The queue action - Enqueue or Dequeue</param>
        /// <returns>The number of available samples that was dequeued</returns>
        //=============================================================================================================================================
        protected CallbackInfo QueueCallbackInfo(int avaiableSamplesPerChannel, int samplesReadPerChannel, QueueAction queueAction)
        {
            lock (callbackInfoQueueLock)
            {
                if (queueAction == QueueAction.Enqueue)
                {
                    m_callbackInfoQueue.Enqueue(new CallbackInfo(avaiableSamplesPerChannel, samplesReadPerChannel));
                    return null;
                }
                else
                {
                    if (m_callbackInfoQueue.Count > 0)
                        return m_callbackInfoQueue.Dequeue();
                    else
                        return null;
                }
            }
        }
    }

    //=====================================================================================
    /// <summary>
    /// Encapsulates data used for invoking callback methods
    /// </summary>
    //=====================================================================================
    internal class CallbackInfo
    {
        internal CallbackInfo(int availableSamplesPerChannel, int samplesReadPerChannel)
        {
            AvailableSamplesPerChannel = availableSamplesPerChannel;
            SamplesReadPerChannel = samplesReadPerChannel;
        }

        internal int AvailableSamplesPerChannel { get; set; }

        internal int SamplesReadPerChannel { get; set; }
    }

    //======================================================================
    /// <summary>
    /// Encapsulates a USB set up packet
    /// </summary>
    //======================================================================
    internal class UsbSetupPacket
    {
        protected byte m_bRequest;
        protected ushort m_wValue;
        protected ushort m_wIndex;
        protected ushort m_wLength;
        protected byte[] m_buffer;
        protected UsbTransferTypes m_transferType;
        protected uint m_bytesTransfered;
        protected bool m_deferTransfer;
        protected bool m_isQuery;

        internal UsbSetupPacket(int bufferSize)
        {
            m_buffer = new byte[bufferSize];
            m_wLength = (ushort)bufferSize;
            m_wValue = 0;
            m_wIndex = 0;
            m_deferTransfer = false;
            m_isQuery = false;
        }

        internal UsbTransferTypes TransferType
        {
            get { return m_transferType; }
            set { m_transferType = value; }
        }

        internal byte Request
        {
            get { return m_bRequest; }
            set { m_bRequest = value; }
        }

        internal ushort Value
        {
            get { return m_wValue; }
            set { m_wValue = value; }
        }

        internal ushort Index
        {
            get { return m_wIndex; }
            set { m_wIndex = value; }
        }

        internal ushort Length
        {
            get { return m_wLength; }
            set { m_wLength = value; }
        }

        internal byte[] Buffer
        {
            get { return m_buffer; }
        }

        internal bool DeferTransfer
        {
            get { return m_deferTransfer; }
            set { m_deferTransfer = value; }
        }

        internal uint BytesTransfered
        {
            get { return m_bytesTransfered; }
            set { m_bytesTransfered = value; }
        }

        internal bool IsQuery
        {
            get { return m_isQuery; }
            set { m_isQuery = value; }
        }
    }
}
