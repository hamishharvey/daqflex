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
using System.Windows.Forms;

namespace MeasurementComputing.DAQFlex
{
    internal class DriverInterface
    {
        internal delegate void StopOutputScanDelegate();

#if !WindowsCE
        protected const int INTERNAL_READ_BUFFER_SIZE = 1024000;
        protected const int INTERNAL_WRITE_BUFFER_SIZE = 65536;
#else
        protected const int INTERNAL_READ_BUFFER_SIZE = 65536;
        protected const int INTERNAL_WRITE_BUFFER_SIZE = 65536;
#endif
        private const double BULK_IN_XFER_TIME = 0.05;  // 50 mS
        private const double BULK_OUT_XFER_TIME = 0.05; // 50 mS

        private DeviceInfo m_deviceInfo;
        private DaqDevice m_daqDevice;
        private ErrorCodes m_errorCode;
        private ErrorCodes m_inputScanErrorCode;
        private ErrorCodes m_outputScanErrorCode;
        private UsbPlatformInterop m_platformInterop;
#if !WindowsCE
        private HidPlatformInterop m_hidPlatformInterop;
#endif
        private List<UsbSetupPacket> m_usbPackets;
        private List<UsbSetupPacket> m_usbPacketsDirect;
        protected string m_internalReadString;
        protected double m_internalReadValue;
        protected byte[] m_internalReadBuffer;
        protected string m_internalReadStringDirect;
        protected double m_internalReadValueDirect;
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
        private ulong m_inputSamplesReadPerChannel;
        private int m_totalBytesToRead;
        private ulong m_totalBytesReceived;
        protected ASCIIEncoding m_ae;
        protected Queue<UsbSetupPacket> m_deferredInputMessages = new Queue<UsbSetupPacket>();
        protected Queue<UsbSetupPacket> m_deferredOutputMessages = new Queue<UsbSetupPacket>();
        protected List<string> m_deferredInputResponses = new List<string>();
        protected List<string> m_deferredOutputResponses = new List<string>();
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
        private ulong m_inputScanCount;
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
        private byte[] m_aoStatusMessage = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aiScanStopMessage = new byte[Constants.MAX_COMMAND_LENGTH];
        private byte[] m_aoScanStopMessage = new byte[Constants.MAX_COMMAND_LENGTH];

        private int m_invokeCallbackCount;
        private UsbSetupPacket m_controlInPacket;
        private UsbSetupPacket m_controlOutPacket;
        private UsbSetupPacket m_controlInPacketDirect;
        private UsbSetupPacket m_controlOutPacketDirect;
        private bool m_inputBufferFilled;
        private bool m_inputScanStarted;
        private Object m_callbackLock = new Object();
        protected bool m_deviceLost = false;
        protected ScanState m_inputScanStatus;
        protected System.Diagnostics.Stopwatch m_readStopWatch = new System.Diagnostics.Stopwatch();
        protected System.Diagnostics.Stopwatch m_writeStopWatch = new System.Diagnostics.Stopwatch();

        protected int m_outputTransferStartIndex;
        protected int m_currentOutputScanWriteIndex;
        protected int m_currentOutputScanOutputIndex;
        protected ulong m_outputScanCount;
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
        protected Object m_deviceLock;
        protected string m_inputBlockSize;

        internal DriverInterface(DaqDevice daqDevice, DeviceInfo deviceInfo, Object deviceLock)
        {
            m_deviceInfo = deviceInfo;
            m_daqDevice = daqDevice;
            m_deviceLock = deviceLock;

            m_platformInterop = PlatformInterop.GetUsbPlatformInterop(deviceInfo, m_criticalParams);

            // the error code may be set if the device did not initialzie
            m_errorCode = m_platformInterop.ErrorCode;

            m_usbPackets = new List<UsbSetupPacket>();
            m_usbPacketsDirect = new List<UsbSetupPacket>();
            m_ae = new ASCIIEncoding();

            string message;

            // convert following messages to an array of bytes for direct use by this driver interface

            message = "?AISCAN:STATUS";
            Array.Clear(m_aiStatusMessage, 0, m_aiStatusMessage.Length);
            m_ae.GetBytes(message.ToCharArray(), 0, message.Length, m_aiStatusMessage, 0);

            message = "?AISCAN:TRIG";
            Array.Clear(m_aiTrigStatus, 0, m_aiTrigStatus.Length);
            m_ae.GetBytes(message.ToCharArray(), 0, message.Length, m_aiTrigStatus, 0);

            message = "?AITRIG:REARM";
            Array.Clear(m_aiRearmStatus, 0, m_aiRearmStatus.Length);
            m_ae.GetBytes(message.ToCharArray(), 0, message.Length, m_aiRearmStatus, 0);

            message = "?AISCAN:SAMPLES";
            Array.Clear(m_aiQuerySamples, 0, m_aiQuerySamples.Length);
            m_ae.GetBytes(message.ToCharArray(), 0, message.Length, m_aiQuerySamples, 0);

            message = "AISCAN:SAMPLES=";
            Array.Clear(m_aiScanSamples, 0, m_aiScanSamples.Length);
            m_ae.GetBytes(message.ToCharArray(), 0, message.Length, m_aiScanSamples, 0);

            message = "?AOSCAN:STATUS";
            Array.Clear(m_aoStatusMessage, 0, m_aoStatusMessage.Length);
            m_ae.GetBytes(message.ToCharArray(), 0, message.Length, m_aoStatusMessage, 0);

            message = "AISCAN:STOP";
            Array.Clear(m_aiScanStopMessage, 0, m_aiScanStopMessage.Length);
            m_ae.GetBytes(message.ToCharArray(), 0, message.Length, m_aiScanStopMessage, 0);

            message = "AOSCAN:STOP";
            Array.Clear(m_aoScanStopMessage, 0, m_aoScanStopMessage.Length);
            m_ae.GetBytes(message.ToCharArray(), 0, message.Length, m_aoScanStopMessage, 0);

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

            m_controlInPacketDirect = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            m_controlInPacketDirect.TransferType = UsbTransferTypes.ControlIn;
            m_controlInPacketDirect.Request = ControlRequest.MESSAGE_REQUEST;

            m_controlOutPacketDirect = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
            m_controlOutPacketDirect.TransferType = UsbTransferTypes.ControlOut;
            m_controlOutPacketDirect.Request = ControlRequest.MESSAGE_REQUEST;

            m_criticalParams.ScanType = ScanType.AnalogInput;

            m_currentOutputScanWriteIndex = 0;
            m_outputTransferStartIndex = 0;

            unsafe
            {
                m_externalReadBuffer = null;
            }

            m_inputBlockSize = PropertyValues.DEFAULT;

            // by default, don't let old data that hasn't been read yet get overwritten
            m_criticalParams.InputScanOverwrite = false;

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

                if (value > m_currentInputScanReadIndex)
                    deltaByteIndex = (value - m_currentInputScanReadIndex);
                else
                    deltaByteIndex = (m_internalReadBuffer.Length - m_currentInputScanReadIndex + value);

                channelCount = m_criticalParams.AiChannelCount;
                m_inputSamplesReadPerChannel += (ulong)(deltaByteIndex / channelCount / m_criticalParams.DataInXferSize);

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
        /// The user specified block size for input scans
        /// </summary>
        //===========================================================================================
        internal string InputBlockSize
        {
            get { return m_inputBlockSize; }

            set 
            {
                // initially set it to the device's max packet size
                int size = m_deviceInfo.MaxPacketSize;

                PlatformParser.TryParse(value, out size);

                int multiplier = (int)Math.Ceiling(size / (double)m_deviceInfo.MaxPacketSize);

                size = multiplier * m_deviceInfo.MaxPacketSize;

                m_inputBlockSize = size.ToString(); 
            }
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
        
#if !WindowsCE
        internal HidPlatformInterop HidPlatformInterop
        {
            get {    
               if (m_hidPlatformInterop==null)
                  m_hidPlatformInterop = PlatformInterop.GetHidPlatformInterop(m_deviceInfo, m_criticalParams);
                  
               return m_hidPlatformInterop;
               }
        }
#endif
        //===========================================================================================
        /// <summary>
        /// Calculates the size of the buffer that will be used for each
        /// Bulk In request based on the rate. The higher the rate, the larger the buffer size.
        /// </summary>
        /// <returns>the size of the buffer in bytes</returns>
        //===========================================================================================
        internal int GetOptimalInputBufferSize(double scanRate)
        {
            int packetSize = m_criticalParams.InputPacketSize;
            int bufferSize = 0;

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
                bufferSize = Math.Max(m_criticalParams.InputPacketSize, (int)(m_criticalParams.DataInXferSize * 0.05 * (double)scanRate));

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

            if (scanRate == 0.0 || packetSize == 0)
                return 0;

            int channelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;

            if (channelCount <= 0)
                return 0;

            // Set buffer size for 50 mS transfers
            bufferSize = Math.Max(m_criticalParams.OutputPacketSize, (int)(channelCount * m_criticalParams.DataInXferSize * BULK_OUT_XFER_TIME * (double)scanRate));

            if (bufferSize % packetSize != 0)
            {
                int multiplier = (int)Math.Ceiling((double)bufferSize / (double)packetSize);
                bufferSize = multiplier * packetSize;
            }

            return bufferSize;

            //if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            //    return bufferSize;
            //else
            //    // the transfer size can't be larger than the device's output FIFO size
            //    return (int)Math.Min(bufferSize, m_criticalParams.OutputFifoSize);
        }

        //======================================================================================
        /// <summary>
        /// Transfers the incoming message to the device using one of the platform interop objects
        /// Each message results in a Send/Receive transfer. The Send is to send the message to
        /// the device through a Control Out transfer and the Receive is to receive a response
        /// through a Control In transfer
        /// </summary>
        /// <param name="incomingMessage">The incoming message string</param>
        /// <returns>The error code</returns>
        //======================================================================================
        internal ErrorCodes TransferMessage(byte[] incomingMessage)
        {
            if (m_deviceLost)
                m_deviceLost = !m_platformInterop.AcquireDevice();

            ErrorCodes errorCode = ErrorCodes.NoErrors;

            UsbSetupPacket packet;

            if (!m_platformInterop.DeviceInitialized)
            {
                errorCode = ErrorCodes.DeviceNotInitialized;
            }
            else
            {
                // Acquire the  mutex so that a Control Out
                m_platformInterop.ControlTransferMutex.WaitOne();

                // create packets for the incoming message
                // each message will get a packet for a Control Out transfer and a Control In transfer
                CreateUsbPackets(incomingMessage);

                if (errorCode == ErrorCodes.NoErrors)
                {
                    ////////////////////////////////////////////
                    // Control Out request
                    ////////////////////////////////////////////
                    packet = m_usbPackets[0];

                    if (m_usbPackets.Count == 1 && packet.TransferType == UsbTransferTypes.ControlOut)
                    {
                        m_internalReadString = String.Empty;
                        m_internalReadValue = double.NaN;
                    }


                    // Convert any decimal separators back to en-US
                    for (int i = 0; i < packet.Buffer.Length; i++)
                    {
                        if (packet.Buffer[i] == (byte)PlatformInterop.LocalNumberDecimalSeparator)
                            packet.Buffer[i] = (byte)Constants.DECIMAL.ToCharArray()[0];
                    }

                    // define a packet for deferred messages
                    UsbSetupPacket deferredPacket = null;

                    // send the Control Out request to the device
                    if (!packet.DeferTransfer)
                    {
                        errorCode = m_platformInterop.UsbControlOutRequest(packet);
                    }
                    else
                    {
                        // queue the message packet. It will be sent at the beginning of a scan thread

                        // first make a copy so it doesn't get overwritten since its an instance member of this class
                        deferredPacket = new UsbSetupPacket(packet.Buffer.Length);

                        deferredPacket.DeferTransfer = packet.DeferTransfer;
                        deferredPacket.TransferType = packet.TransferType;
                        deferredPacket.Request = packet.Request;
                        deferredPacket.Value = packet.Value;
                        deferredPacket.Index = packet.Index;
                        deferredPacket.Length = packet.Length;
                        Array.Copy(packet.Buffer, deferredPacket.Buffer, packet.Buffer.Length);
                    }


                    ////////////////////////////////////////////
                    // Control In request
                    ////////////////////////////////////////////
                    if (m_usbPackets.Count > 1)
                    {
                        packet = m_usbPackets[1];

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

                                if (m_internalReadString == PropertyValues.INVALID)
                                {
                                    errorCode = ErrorCodes.InvalidMessage;
                                }
                                else
                                {
                                    m_internalReadValue = TryConvertData(ref m_internalReadString);
                                }
                            }
                            else
                            {
                                m_internalReadString = String.Empty;
                                m_internalReadValue = double.NaN;
                            }
                        }
                    }

                    if (m_startInputScan)
                    {
                        if (errorCode == ErrorCodes.NoErrors)
                        {
                            if (deferredPacket != null)
                                m_deferredInputMessages.Enqueue(deferredPacket);

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
                                m_deferredInputResponses.Clear();

                                CheckTriggerRearm();

                                if (m_criticalParams.InputSampleMode == SampleMode.Continuous)
                                {
                                    if (m_onDataAvailableCallbackControl != null)
                                    {
                                        if (m_criticalParams.AiChannelCount * m_criticalParams.DataInXferSize * m_onDataAvailableCallbackControl.NumberOfSamples > m_internalReadBuffer.Length / 2)
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
                                    if (m_inputScanErrorCode != ErrorCodes.NoErrors)
                                    {
                                        errorCode = m_inputScanErrorCode;
                                    }
                                    else if (packet.DeferTransfer)
                                    {
                                        m_internalReadString = m_deferredInputResponses[0].Trim(new char[] { Constants.NULL_TERMINATOR });
                                    }
                                }
                                else
                                {
                                    // if an error occurred dont't send the deferred message
                                    m_deferredInputMessages.Clear();
                                }
                            }
                        }
                    }
                    else if (m_startOutputScan)
                    {
                        if (errorCode == ErrorCodes.NoErrors)
                        {
                            if (deferredPacket != null)
                                m_deferredOutputMessages.Enqueue(deferredPacket);

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
                                m_deferredOutputResponses.Clear();

                                // release the control transfer mutex so that the deferred messages can be sent
                                m_platformInterop.ControlTransferMutex.ReleaseMutex();

                                if (errorCode == ErrorCodes.NoErrors)
                                    errorCode = StartOutputScan();

                                // reaquire the control transfer mutex
                                m_platformInterop.ControlTransferMutex.WaitOne();

                                if (errorCode == ErrorCodes.NoErrors)
                                {
                                    if (m_outputScanErrorCode != ErrorCodes.NoErrors)
                                    {
                                        errorCode = m_outputScanErrorCode;
                                    }
                                    else if (packet.DeferTransfer)
                                    {
                                        if (m_deferredOutputResponses.Count > 0)
                                            m_internalReadString = m_deferredOutputResponses[0].Trim(new char[] { Constants.NULL_TERMINATOR });
                                        else
                                            System.Diagnostics.Debug.Assert(false, "Deferred response list is empty");
                                    }
                                }
                                else
                                {
                                    // if an error occurred don't send the deferred message
                                    m_deferredOutputMessages.Clear();
                                }
                            }
                        }
                    }
                    else if (m_initiateStopForInput && m_deviceInfo.EndPointIn != 0)
                    {
                        // release the control transfer mutex so that the Input scan thread can exit
                        m_platformInterop.ControlTransferMutex.ReleaseMutex();

                        // m_initiateStopForInput is set to true in CheckForCriticalParams

                        if (deferredPacket != null)
                        {
                            // executes on Linux - this must be called first before sending
                            // the deferred STOP command
                            StopInputScan(false);

                            errorCode = m_platformInterop.UsbControlOutRequest(deferredPacket);

                            if (m_deferredOutputResponses.Count > 0)
                                m_internalReadString = m_deferredOutputResponses[0].Trim(new char[] { Constants.NULL_TERMINATOR });
                            else
                                System.Diagnostics.Debug.Assert(false, "Deferred response list is empty");

                            m_platformInterop.FlushInputDataFromDevice();
                            m_initiateStopForInput = false;
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

                        if (deferredPacket != null)
                        {
                            // executes on Linux - this must be called first before sending
                            // the deferred STOP command
                            StopOutputScan(false);

                            errorCode = m_platformInterop.UsbControlOutRequest(deferredPacket);

                            if (m_deferredOutputResponses.Count > 0)
                                m_internalReadString = m_deferredOutputResponses[0].Trim(new char[] { Constants.NULL_TERMINATOR });
                            else
                                System.Diagnostics.Debug.Assert(false, "Deferred response list is empty");

                            m_initiateStopForOutput = false;
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

                m_platformInterop.ControlTransferMutex.ReleaseMutex();
            }

            if (errorCode == ErrorCodes.DeviceNotResponding)
            {
                ReleaseDevice();
                m_deviceLost = true;
            }

            return errorCode;
        }

        //======================================================================================
        /// <summary>
        /// Transfers the incoming message to the device using one of the platform interop objects
        /// Each message results in a Send/Receive transfer. The Send is to send the message to
        /// the device through a Control Out transfer and the Receive is to receive a response
        /// through a Control In transfer
        /// </summary>
        /// <param name="incomingMessage">The incoming message string</param>
        /// <returns>The error code</returns>
        //======================================================================================
        internal ErrorCodes TransferMessageDirect(byte[] incomingMessage)
        {
            if (m_deviceLost)
                m_deviceLost = !m_platformInterop.AcquireDevice();

            ErrorCodes errorCode = ErrorCodes.NoErrors;

            if (!m_platformInterop.DeviceInitialized)
            {
                errorCode = ErrorCodes.DeviceNotInitialized;
            }
            else
            {
                // Acquire the  mutex so that a Control Out
                m_platformInterop.ControlTransferMutex.WaitOne();

                // create packets for the incoming message
                // each messasge will get a packet for a Control Out transfer and a Control In transfer
                CreateUsbPacketsDirect(incomingMessage);

                foreach (UsbSetupPacket packet in m_usbPacketsDirect)
                {
                    if (errorCode == ErrorCodes.NoErrors)
                    {
                        if (m_usbPacketsDirect.Count == 1 && packet.TransferType == UsbTransferTypes.ControlOut)
                        {
                            m_internalReadStringDirect = String.Empty;
                            m_internalReadValueDirect = double.NaN;
                        }

                        // Control In request
                        if (packet.TransferType == UsbTransferTypes.ControlIn)
                        {
                            errorCode = m_platformInterop.UsbControlInRequest(packet);

                            if (packet.Request == ControlRequest.MESSAGE_REQUEST)
                            {
                                if (errorCode == ErrorCodes.NoErrors)
                                {
                                    m_internalReadStringDirect = m_ae.GetString(packet.Buffer, 0, packet.Buffer.Length);

                                    if (m_internalReadStringDirect.IndexOf(Constants.NULL_TERMINATOR) >= 0)
                                    {
                                        int indexOfNt = m_internalReadStringDirect.IndexOf(Constants.NULL_TERMINATOR);
                                        m_internalReadStringDirect = m_internalReadStringDirect.Remove(indexOfNt, m_internalReadStringDirect.Length - indexOfNt);
                                    }

                                    if (m_internalReadStringDirect == PropertyValues.INVALID)
                                    {
                                        errorCode = ErrorCodes.InvalidMessage;
                                    }
                                    else
                                    {
                                        m_internalReadValueDirect = TryConvertData(ref m_internalReadStringDirect);
                                    }
                                }
                                else
                                {
                                    m_internalReadStringDirect = String.Empty;
                                    m_internalReadValueDirect = double.NaN;
                                }
                            }
                        }
                        // Control Out request
                        else if (packet.TransferType == UsbTransferTypes.ControlOut)
                        {
                            // Convert any decimal separators back to en-US
                            for (int i = 0; i < packet.Buffer.Length; i++)
                            {
                                if (packet.Buffer[i] == (byte)PlatformInterop.LocalNumberDecimalSeparator)
                                    packet.Buffer[i] = (byte)Constants.DECIMAL.ToCharArray()[0];
                            }

                            // send the Control Out request to the device
                            errorCode = m_platformInterop.UsbControlOutRequest(packet);
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
        internal ErrorCodes SetInputBufferSize(int numberOfBytes)
        {
            ScanState scanState = GetInputScanState();

            if (scanState != ScanState.Running)
            {
                int mulitplier = (int)Math.Ceiling((double)numberOfBytes / (double)m_deviceInfo.MaxPacketSize);
                m_totalBytesToRead = Math.Max(m_deviceInfo.MaxPacketSize, mulitplier * m_deviceInfo.MaxPacketSize);
                m_internalReadBuffer = new byte[m_totalBytesToRead];
                m_inputBufferSizeOverride = true;
                return ErrorCodes.NoErrors;
            }
            else
            {
                m_errorCode = ErrorCodes.InputBufferCannotBeSet;
                return m_errorCode;
            }
        }

        //======================================================================================
        /// <summary>
        /// Sets the size of the internal output scan write buffer
        /// </summary>
        /// <param name="numberOfBytes">The number of bytes</param>
        //======================================================================================
        internal ErrorCodes SetOutputBufferSize(int numberOfBytes)
        {
            if (m_outputScanState != ScanState.Running)
            {
                m_totalBytesToWrite = numberOfBytes;
                m_internalWriteBuffer = new byte[m_totalBytesToWrite];
                m_outputBufferSizeOverride = true;
                return ErrorCodes.NoErrors;
            }
            else
            {
                m_errorCode = ErrorCodes.OutputBufferCannotBeSet;
                return m_errorCode;
            }
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
        internal ulong InputScanCount
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
        internal ulong OutputScanCount
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
        internal ulong InputSamplesReadPerChannel
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
            string originalResponse = response;
            double value = double.NaN;

            string dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            response = response.Replace(".", dec);

            int equalIndex = response.IndexOf(Constants.EQUAL_SIGN);

            if (equalIndex >= 0)
            {
                double parsedValue = Double.NaN;
                bool parsed = false;

                string responseValue = response.Substring(equalIndex + 1);

                parsed = PlatformParser.TryParse(responseValue, out parsedValue);

                if (parsed)
                {
                    value = parsedValue;
                }
                else
                {
                    if (originalResponse.Contains(Messages.DEV_ID.TrimEnd(new char[]{'#'})))
                        response = originalResponse;
                }
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
        /// Resets the input scan count and index properties
        /// </summary>
        //====================================================================
        internal void ResetInputScanCount()
        {
            m_inputScanCount = 0;
            m_inputScanIndex = -1;
        }

        //====================================================================
        /// <summary>
        /// Resets the input scan count and index properties
        /// </summary>
        //====================================================================
        internal void ResetOutputScanCount()
        {
            m_outputScanCount = 0;
            m_outputScanIndex = -1;
        }

        //====================================================================
        /// <summary>
        /// Starts the input scan thread
        /// </summary>
        //====================================================================
        protected ErrorCodes StartInputScan()
        {
            if (ValidateInputCriticalParams())
            {
                CheckForTriggerLevelResend();

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

                m_errorCode = ErrorCodes.NoErrors;
                m_inputScanErrorCode = ErrorCodes.NoErrors;

                m_platformInterop.ClearError();
                m_platformInterop.ClearInputScanError();

                DebugLogger.ClearDebugList();
                DebugLogger.StopWatch.Reset();
                DebugLogger.StopWatch.Start();

                // initialize scan variables
                m_inputScanCount = 0;
                m_startInputScan = false;
                m_inputScanIndex = -1; // equivalent to UL GetStatus curIndex
                m_currentInputScanReadIndex = 0;
                m_lastInputScanWriteIndex = -1;
                m_inputSamplesReadPerChannel = 0;
                m_inputScanComplete = false;
                m_stopInputScan = false;
                m_invokeCallbackCount = 0;
                m_inputBufferFilled = false;
                m_callbackInfoQueue.Clear();

                // set scaling/cal coefficients
                SetADScalingCoefficients();

                // start the Process Input Scan thread
                m_inputScanStatus = ScanState.Idle;
                m_inputScanThread = new Thread(new ThreadStart(ProcessInputScanThread));
                m_inputScanThread.Name = "InputScanThread";
                m_inputScanThread.Start();
                m_inputScanStarted = true;

                // wait for the ProcessInputScanThread to send the actual START message 
                // and set the scan state to RUNNING (or OVERRUN) to the device
                while (m_inputScanStatus == ScanState.Idle && m_inputScanErrorCode == ErrorCodes.NoErrors && m_platformInterop.InputScanErrorCode == ErrorCodes.NoErrors)
                    Thread.Sleep(0);

                if (m_inputScanErrorCode == ErrorCodes.NoErrors && m_platformInterop.InputScanErrorCode != ErrorCodes.NoErrors)
                    m_inputScanErrorCode = m_platformInterop.InputScanErrorCode;
            }
            else
            {
                return m_errorCode;
            }

            return m_errorCode;
        }

        //=====================================================================
        /// <summary>
        /// Checks to see if the trigger level needs to be resent 
        /// in case other messages sent after the AITRIG:LEVEL message
        /// effects its scaling
        /// </summary>
        //=====================================================================
        protected void CheckForTriggerLevelResend()
        {
            if (m_daqDevice.CriticalParams.InputTriggerEnabled &&
                    m_daqDevice.CriticalParams.ResendInputTriggerLevelMessage &&
                        m_daqDevice.CriticalParams.InputTriggerSource.Contains("SWSTART"))
            {
                // release the control transfer mutex so the trigger message can be sent
                m_platformInterop.ControlTransferMutex.WaitOne();

                UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
                packet.Request = ControlRequest.MESSAGE_REQUEST;
                packet.TransferType = UsbTransferTypes.ControlOut;
                packet.Index = 0;
                packet.Length = (ushort)packet.Buffer.Length;
                packet.Value = 0;

                string msg = Messages.AITRIG_LEVEL;
                msg = Messages.InsertValue(msg, m_criticalParams.InputTriggerLevel);

                for (int i = 0; i < msg.Length; i++)
                    packet.Buffer[i] = (byte)msg[i];

                m_errorCode = m_platformInterop.UsbControlOutRequest(packet);

                // reaquire the control transfer mutex
                m_platformInterop.ControlTransferMutex.ReleaseMutex();
            }
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
            m_outputScanErrorCode = ErrorCodes.NoErrors;

            if (ValidateOutputCriticalParams())
            {
                m_platformInterop.ClearError();
                m_platformInterop.ClearOutputScanError();

                m_numberOfSamplesPerChannelWrittenToDevice = 0;
                m_outputScanComplete = false;
                m_outputScanCount = 0;
                m_outputScanIndex = 0;
                m_outputScanState = ScanState.Idle;
                m_totalBytesReceivedByDevice = 0;
                m_stopOutputScan = false;

                m_criticalParams.AoChannelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;

                m_daqDevice.BeginOutputScan();

                m_outputScanThread = new Thread(new ThreadStart(ProcessOutputScanThread));
                m_outputScanThread.Name = "OutputScanThread";
                m_outputScanThread.Start();
                m_outputScanStarted = true;

                // wait for the ProcessInputScanThread to send the actual START message to the device
                while (m_outputScanState != ScanState.Running && m_outputScanErrorCode == ErrorCodes.NoErrors && m_platformInterop.ErrorCode == ErrorCodes.NoErrors)
                    Thread.Sleep(0);

                if (m_outputScanErrorCode == ErrorCodes.NoErrors && m_platformInterop.ErrorCode != ErrorCodes.NoErrors)
                    m_outputScanErrorCode = m_platformInterop.ErrorCode;
            }

            return m_errorCode;
        }

        protected object queueOutputTransferLock = new object();

        //==========================================================================================
        /// <summary>
        /// Checks critical input params before starting the scan
        /// </summary>
        /// <returns>True if the params are valid otherwise false</returns>
        //==========================================================================================
        protected bool ValidateInputCriticalParams()
        {
            if (m_criticalParams.LowAiChannel > m_criticalParams.HighAiChannel)
            {
                m_errorCode = ErrorCodes.LowChannelIsGreaterThanHighChannel;
                return false;
            }

            if (m_criticalParams.InputSampleMode == SampleMode.Finite)
            {
                if ((m_criticalParams.DataInXferSize * m_criticalParams.AiChannelCount * m_criticalParams.InputScanSamples) > m_internalReadBuffer.Length)
                {
                    m_errorCode = ErrorCodes.InputSamplesGreaterThanBufferSize;
                    return false;
                }
            }

            m_errorCode = m_daqDevice.Ai.ValidateScanRate();

            if (m_errorCode == ErrorCodes.NoErrors)
                m_errorCode = m_daqDevice.Ai.ValidateSampleCount();

            if (m_errorCode == ErrorCodes.NoErrors)
                m_errorCode = m_daqDevice.Ai.ValidateQueueConfiguration();

            if (m_errorCode == ErrorCodes.NoErrors)
                m_errorCode = m_daqDevice.Ai.ValidateAiTrigger();

            if (m_errorCode != ErrorCodes.NoErrors)
                return false;

            return true;
        }

        //==========================================================================================
        /// <summary>
        /// Checks critical output params before starting the scan
        /// </summary>
        /// <returns>True if the params are valid otherwise false</returns>
        //==========================================================================================
        protected bool ValidateOutputCriticalParams()
        {
            if (m_criticalParams.LowAoChannel > m_criticalParams.HighAoChannel)
            {
                m_errorCode = ErrorCodes.LowChannelIsGreaterThanHighChannel;
                return false;
            }

            if (m_criticalParams.OutputSampleMode == SampleMode.Finite)
            {
                if ((m_criticalParams.DataOutXferSize * m_criticalParams.AoChannelCount * m_criticalParams.OutputScanSamples) > m_internalWriteBuffer.Length)
                {
                    m_errorCode = ErrorCodes.TotalNumberOfSamplesGreaterThanOutputBufferSize;
                    return false;
                }
            }

            m_errorCode = m_daqDevice.Ao.ValidateScanRate();

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
                        {
                            if (m_daqDevice.CriticalParams.AiDataIsSigned)
                                offset = 0;
                            else
                                offset = -1.0 * (scale / 2.0);
                        }
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

            // Call EndInvoke 
            caller.EndInvoke(ar);
        }

        //========================================================================
        /// <summary>
        /// Transmits any messages that have been deferred such as "AISCAN:START"
        /// </summary>
        //========================================================================
        protected void TransmitDeferredInputMessages()
        {
            UsbSetupPacket messagePacket = null;

            m_platformInterop.ControlTransferMutex.WaitOne();

            do
            {
                if (m_deferredInputMessages.Count > 0)
                    messagePacket = m_deferredInputMessages.Dequeue();

                if (messagePacket != null)
                {
                    m_platformInterop.UsbControlOutRequest(messagePacket);
                    m_deferredInputResponses.Add(m_ae.GetString(messagePacket.Buffer, 0, messagePacket.Buffer.Length).Trim(new char[] {Constants.NULL_TERMINATOR}));
                }

            } while (messagePacket != null && m_deferredInputMessages.Count > 0);

            m_platformInterop.ControlTransferMutex.ReleaseMutex();
        }

        //========================================================================
        /// <summary>
        /// Transmits any messages that have been deferred such as "AOSCAN:START"
        /// </summary>
        //========================================================================
        protected void TransmitDeferredOutputMessages()
        {
            UsbSetupPacket messagePacket = null;

            m_platformInterop.ControlTransferMutex.WaitOne();

            do
            {
                if (m_deferredOutputMessages.Count > 0)
                    messagePacket = m_deferredOutputMessages.Dequeue();

                if (messagePacket != null)
                {
                    m_platformInterop.UsbControlOutRequest(messagePacket);
                    m_deferredOutputResponses.Add(m_ae.GetString(messagePacket.Buffer, 0, messagePacket.Buffer.Length).Trim(new char[] { Constants.NULL_TERMINATOR }));
                }

            } while (messagePacket != null && m_deferredOutputMessages.Count > 0);

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
            System.Diagnostics.Debug.Assert(m_criticalParams.BulkInXferSize != 0);

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
            int optimalBufferSize = m_criticalParams.BulkInXferSize;

            int channelCount = m_criticalParams.AiChannelCount;

            m_totalBytesToRead = channelCount * (m_criticalParams.DataInXferSize * m_criticalParams.InputScanSamples);

            if (m_criticalParams.InputSampleMode == SampleMode.Continuous && !m_inputBufferSizeOverride)
                m_internalReadBuffer = new byte[2 * m_totalBytesToRead];

            if (usingInternalBuffer)
                readBufferLength = m_internalReadBuffer.Length;
            else
                readBufferLength = m_externalReadBufferSize;

            if (m_totalBytesToRead < optimalBufferSize)
            {
                optimalBufferSize = m_totalBytesToRead;
                m_criticalParams.BulkInXferSize = optimalBufferSize;
            }

            // give the device an opportunity to do device-specific stuff before starting
            m_daqDevice.BeginInputScan();

            if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
            {
                // single sample only
                optimalBufferSize = m_criticalParams.DataInXferSize * m_criticalParams.NumberOfSamplesForSingleIO;
                m_criticalParams.BulkInXferSize = optimalBufferSize;
            }

            // the platform interop object will allocate and return the bulk read buffer
            m_bulkReadBuffer = null;

            //// give the device an opportunity to do device-specific stuff before starting
            //m_daqDevice.BeginInputScan();

            // this will queue one or more bulk in requests if the interop object supports asynchronous I/O
            m_platformInterop.PrepareInputTransfers(m_criticalParams.InputScanRate,
                                                    m_totalBytesToRead,
                                                    optimalBufferSize);
            
            // this will start the device scan
            TransmitDeferredInputMessages();

            int numberOfBulkTransfersToExecute;
            uint bytesReceivedInCurrentTransfer = 0;
            uint totalBytesTransfered = 0;
            uint bytesToTransfer = 0;
            int deltaBytes = 0;

            while (ContinueProcessingInputScan(m_inputScanErrorCode))
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
                        bytesToTransfer = (uint)(m_criticalParams.DataInXferSize * m_criticalParams.NumberOfSamplesForSingleIO);
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
                    m_inputScanErrorCode = m_platformInterop.UsbBulkInRequest(ref m_bulkReadBuffer, ref bytesReceivedInCurrentTransfer);

                    // update the total number of bytes received
                    m_totalBytesReceived += (ulong)bytesReceivedInCurrentTransfer;

                    // m_bulkInReadBuffer could be null if the input scan was stopped with the Stop command

                    if (m_inputScanErrorCode == (int)ErrorCodes.NoErrors && m_bulkReadBuffer != null)
                    {
                        // update the total number of bytes transfered so far
                        totalBytesTransfered += bytesReceivedInCurrentTransfer;

                        try
                        {
                            if (m_criticalParams.DeltaRearmInputSamples > 0)
                            {
                                triggerRearmByteCount += (int)bytesToTransfer;

                                if (triggerRearmByteCount >= (m_criticalParams.DataInXferSize * channelCount * m_criticalParams.AdjustedRearmSamplesPerTrigger))
                                {
                                    rearmTriggerCount++;
                                    bytesToTransfer -= (uint)(m_criticalParams.DataInXferSize * channelCount * m_criticalParams.DeltaRearmInputSamples);
                                    m_totalBytesReceived -= (ulong)(m_criticalParams.DataInXferSize * channelCount * m_criticalParams.DeltaRearmInputSamples);
                                    triggerRearmByteCount = 0;
                                }
                            }

                            // upate the number of samples acquired so far per channel
                            lock (m_inputScanCountLock)
                            {
                                m_inputScanCount = (m_totalBytesReceived / (ulong)m_criticalParams.DataInXferSize) / (ulong)channelCount;

                                if (m_criticalParams.InputScanOverwrite)
                                {
                                    deltaBytes = m_criticalParams.DataInXferSize * (int)(m_inputScanCount - m_inputSamplesReadPerChannel);

                                    if (deltaBytes > readBufferLength)
                                    {
                                        m_inputScanErrorCode = ErrorCodes.InputBufferOverrun;
                                        continue;
                                    }
                                }
                            }

                            if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
                                callbackCount += ((int)bytesReceivedInCurrentTransfer / m_criticalParams.DataInXferSize) / m_criticalParams.NumberOfSamplesForSingleIO;
                            else
                                callbackCount += ((int)bytesReceivedInCurrentTransfer / m_criticalParams.DataInXferSize) / channelCount;
                            
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

                            // calculate current index (some devices in SINGLEIO mode only transfer one channel per packet and the second calculation will always return 0)
                            if (m_criticalParams.InputTransferMode == TransferMode.SingleIO)
                                m_inputScanIndex = (long)Math.Max(0, (m_inputScanCount % (ulong)(readBufferLength / m_criticalParams.DataInXferSize)) - 1);
                            else
                                m_inputScanIndex = (long)Math.Max(0, (m_inputScanCount % (ulong)(readBufferLength / m_criticalParams.DataInXferSize)) - (ulong)channelCount);

                            // add the m_bulkReadBuffer to the ready buffers queue so it may be reused
                            m_platformInterop.QueueBulkInReadyBuffers(m_bulkReadBuffer, QueueAction.Enqueue);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Assert(false, ex.Message);
                            m_inputScanErrorCode = ErrorCodes.UnknownError;
                        }

                        //*****************************************************************************************
                        // OnDataAvailable callback
                        //*****************************************************************************************
                        if (m_inputScanErrorCode == ErrorCodes.NoErrors)
                        {
                            if (m_onDataAvailableCallbackControl != null && m_onDataAvailableCallbackControl.Created)
                            {
                                if (m_criticalParams.InputSampleMode == SampleMode.Finite)
                                {
                                    if (m_totalBytesReceived >= (ulong)m_totalBytesToRead && m_totalBytesReceived > 0)
                                        TerminateCallbacks = true;
                                }
                            }
                        }
                    }
                }

                //*****************************************************************************************
                // OnInputScanError callback
                //*****************************************************************************************
                if (m_inputScanErrorCode != ErrorCodes.NoErrors)
                {
                    m_callbackInfoQueue.Clear();
                    TerminateCallbacks = true;

                    if (m_onInputScanErrorCallbackControl != null && m_onInputScanErrorCallbackControl.Created)
                    {
                        if (m_inputScanErrorCode == ErrorCodes.DataOverrun)
                            m_inputScanStatus = ScanState.Overrun;
                        else
                            m_inputScanStatus = ScanState.Idle;

                        m_errorCode = m_inputScanErrorCode;
                        m_invokeCallbackCount++;

                        if (m_onInputScanErrorCallbackControl.ExecuteOnUIThread)
                        {
                            m_scanErrorCallbackParam[0] = (int)(InputScanCount - InputSamplesReadPerChannel);
                            m_onInputScanErrorCallbackControl.BeginInvoke(m_onInputScanErrorCallback, m_scanErrorCallbackParam);
                        }
                        else
                        {
                            Thread errorCallbackThread = new Thread(new ThreadStart(ProcessErrorCallbackThread));
                            errorCallbackThread.Start();
                        }
                    }
                }

                Thread.Sleep(1);
            }

            while (!m_platformInterop.InputScanComplete())
                Thread.Sleep(0);

            DebugLogger.WriteLine("Input scan complete");
            DebugLogger.StopWatch.Stop();

            if (m_inputScanErrorCode == ErrorCodes.DataOverrun)
                m_inputScanStatus = ScanState.Overrun;
            else
                m_inputScanStatus = ScanState.Idle;

            m_inputScanComplete = true;
            m_inputScanStarted = false;

            // give the device an opportunity to do device-specific stuff before stopping
            // THIS MUST BE DONE AFTER m_inputScanStatus HAS BEEN SET 
            m_daqDevice.EndInputScan();

            if (m_onDataAvailableCallbackControl != null)
            {
                // if this is finite mode and a normal end of scan, then let the callback thread complete
                if (m_criticalParams.InputSampleMode == SampleMode.Finite && !m_stopInputScan && m_inputScanErrorCode == ErrorCodes.NoErrors)
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
                m_errorCode = m_inputScanErrorCode;
                m_invokeCallbackCount++;

                //DebugLogger.WriteLine("Invoking ScanComplete callback - {0} samples", availableSamplesForCallbackPerChannel);
                //DebugLogger.WriteLine("   Input samples read per channel = {0}", InputSamplesReadPerChannel);
                //DebugLogger.WriteLine("   Input scan count = {0}", InputScanCount);

                if (m_onInputScanCompleteCallbackControl.ExecuteOnUIThread)
                {
                    int samplesPerChannelRead = m_currentInputScanReadIndex / (m_criticalParams.DataInXferSize * channelCount);

                    if (m_criticalParams.InputSampleMode == SampleMode.Finite && !m_stopInputScan)
                        m_scanCompleteCallbackParam[0] = m_totalSamplesToReadPerChannel - samplesPerChannelRead;
                    else
                        m_scanCompleteCallbackParam[0] = (int)(InputScanCount - InputSamplesReadPerChannel);

                    m_onInputScanCompleteCallbackControl.BeginInvoke(m_onInputScanCompleteCallback, m_scanCompleteCallbackParam);
                }
                else
                {
#if !WindowsCE
                    Thread scanCompleteCallbackThread = new Thread(new ParameterizedThreadStart(ProcessScanCompleteCallbackThread));
                    scanCompleteCallbackThread.Start(channelCount);
#endif
                }
            }

            //DebugLogger.DumpDebugInfo();

            m_errorCode = m_inputScanErrorCode;

            // set the DaqDevice's pending error so that it can handle it on the next message sent
            if (m_errorCode != ErrorCodes.NoErrors && m_errorCode != ErrorCodes.DataOverrun)
                m_daqDevice.SetPendingInputScanError(m_errorCode);
        }

        //===============================================================================================================================
        /// <summary>
        /// This invokes the scan error callback method when it's not called on the UI thread
        /// </summary>
        //===============================================================================================================================
        protected void ProcessErrorCallbackThread()
        {
            Monitor.Enter(m_callbackLock);
            m_scanErrorCallbackParam[0] = (int)(InputScanCount - InputSamplesReadPerChannel);
            DebugLogger.WriteLine("Raising error callback - {0} samples", m_scanErrorCallbackParam[0]);
            m_onInputScanErrorCallbackControl.NotifyApplication((int)m_scanErrorCallbackParam[0]);
            Monitor.Exit(m_callbackLock);
        }

        //===============================================================================================================================
        /// <summary>
        /// This invokes the scan complete callback method when it's not called on the UI thread
        /// </summary>
        //===============================================================================================================================
        protected void ProcessScanCompleteCallbackThread(object channelCount)
        {
            Monitor.Enter(m_callbackLock);

            int samplesPerChannelRead = m_currentInputScanReadIndex / (m_criticalParams.DataInXferSize * (int)channelCount);

            if (m_criticalParams.InputSampleMode == SampleMode.Finite && !m_stopInputScan)
                m_scanCompleteCallbackParam[0] = m_totalSamplesToReadPerChannel - samplesPerChannelRead;
            else
                m_scanCompleteCallbackParam[0] = (int)(InputScanCount - InputSamplesReadPerChannel);

            DebugLogger.WriteLine("Raising scan complete callback - {0} samples", m_scanCompleteCallbackParam[0]);
            m_onInputScanCompleteCallbackControl.NotifyApplication((int)m_scanCompleteCallbackParam[0]);
            Monitor.Exit(m_callbackLock);
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
            int channelCount = m_criticalParams.AiChannelCount;
            int availableSamplesPerChannel;
            int samplesSentToCallbackPerChannel = 0;

            while (!TerminateCallbacks &&
                   ((m_criticalParams.InputSampleMode == SampleMode.Continuous) ||
                    (m_criticalParams.InputSampleMode == SampleMode.Finite && samplesSentToCallbackPerChannel < m_criticalParams.InputScanSamples)))
            {
                    availableSamplesPerChannel = (int)(InputScanCount - InputSamplesReadPerChannel);

                    if (availableSamplesPerChannel >= m_onDataAvailableCallbackControl.NumberOfSamples)
                    {
                        callbackParam[0] = availableSamplesPerChannel;

                        if (m_onDataAvailableCallbackControl != null)
                        {
                            if (m_onDataAvailableCallbackControl.ExecuteOnUIThread)
                            {
                                DebugLogger.WriteLine("Raising data available callback - {0} samples available ", callbackParam[0]);
                                asyncCallbackResult = m_onDataAvailableCallbackControl.BeginInvoke(m_onDataAvailableCallback, callbackParam);

                                while (!asyncCallbackResult.IsCompleted)
                                {
                                    // wait for the callback to complete or abort
                                    if (TerminateCallbacks)
                                    {
                                        m_onDataAvailableCallbackControl.Abort = true;
                                        break;
                                    }

                                    Thread.Sleep(1);
                                }

                                DebugLogger.WriteLine("Data available callback complete");

                                if (!TerminateCallbacks)
                                {
                                    // complete callback invocation
                                    if (m_onDataAvailableCallbackControl != null)
                                        m_onDataAvailableCallbackControl.EndInvoke(asyncCallbackResult);
                                }
                            }
                            else
                            {
                                Monitor.Enter(m_callbackLock);
                                DebugLogger.WriteLine("Raising data available callback - {0} samples available ", callbackParam[0]);
                                m_onDataAvailableCallbackControl.NotifyApplication((int)callbackParam[0]);
                                DebugLogger.WriteLine("Data available callback complete");
                                Monitor.Exit(m_callbackLock);
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
                m_inputScanErrorCode = ErrorCodes.InternalReadBufferError;
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
                    m_inputScanErrorCode = ErrorCodes.ErrorWritingDataToExternalInputBuffer;
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
                    UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);
                    packet.TransferType = UsbTransferTypes.ControlOut;
                    packet.Request = ControlRequest.MESSAGE_REQUEST;
                    packet.Index = 0;
                    packet.Value = 0;
                    packet.Length = (ushort)m_aiScanStopMessage.Length;
                    Array.Copy(m_aiScanStopMessage, packet.Buffer, m_aiScanStopMessage.Length);

                    // send the status message
                    m_platformInterop.ControlTransferMutex.WaitOne();

                    m_platformInterop.UsbControlOutRequest(packet);

                    m_platformInterop.ControlTransferMutex.ReleaseMutex();

                    m_platformInterop.StopInputTransfers();

                    m_stopInputScan = true;
                }

                return false;
            }

            if (m_criticalParams.InputSampleMode == SampleMode.Finite && 
                m_totalBytesReceived >= 0 &&
                m_totalBytesReceived >= (ulong)m_totalBytesToRead)
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

            bool outputTriggerred = false;
            string triggerResponse;
            int statusSleepTime;
            int transferSize = m_criticalParams.BulkOutXferSize;
            int channelCount = m_criticalParams.AoChannelCount;
            int outputBufferSampleSize = m_internalWriteBuffer.Length / m_criticalParams.DataOutXferSize;
            int whileSleepTime = (int)((1000.0 * BULK_OUT_XFER_TIME) / 2.0);

			m_platformInterop.ReadyToSubmitRemainingOutputTransfers = false;

            // check the transfer size against the write buffer and adjust it if necessary
            if (transferSize > m_internalWriteBuffer.Length)
            {
                m_criticalParams.BulkOutXferSize = m_internalWriteBuffer.Length;
                transferSize = m_criticalParams.BulkInXferSize;
            }

            // m_criticalParams.BulkOutXferSize is the number of bytes submitted to each transfer
            // m_criticalParams.DataOutXferSize is the number of bytes per sample per channel that the device sends for each transfer

            if (m_criticalParams.OutputSampleMode == SampleMode.Continuous)
            {
                statusSleepTime = (int)Math.Max(1, m_criticalParams.OutputScanRate / 10);
                m_criticalParams.OutputScanSamples = transferSize / (m_criticalParams.DataOutXferSize * channelCount);
            }
            else
            {
                statusSleepTime = (int)(((1.0 / m_criticalParams.OutputScanRate) * m_criticalParams.OutputScanSamples) * 100.0);
                statusSleepTime = Math.Max(1, statusSleepTime);
            }

            statusSleepTime = (int)Math.Min(statusSleepTime, 100);

            m_totalBytesToWrite = (m_criticalParams.DataOutXferSize * channelCount * m_criticalParams.OutputScanSamples);

            m_platformInterop.DriverInterfaceOutputBuffer = m_internalWriteBuffer;

            if (m_outputScanErrorCode == ErrorCodes.NoErrors)
            {
                m_platformInterop.PrepareOutputTransfers(m_criticalParams.OutputScanRate, m_totalBytesToWrite, transferSize);

                // wait for data to be written to the FIFO if transfer size is less than the FIFO size
                while (!m_platformInterop.ReadyToStartOutputScan)
                {
                    Thread.Sleep(1);
                }

                // this will start the output scan
                TransmitDeferredOutputMessages();

                m_platformInterop.ReadyToSubmitRemainingOutputTransfers = true;
            }

            do
            {
                if (m_outputScanErrorCode == ErrorCodes.NoErrors)
                {
                    if (!outputTriggerred)
                    {
                        TransferMessageDirect(m_aoStatusMessage);
                        triggerResponse = ReadStringDirect();

                        if (triggerResponse.Contains(PropertyValues.RUNNING))
                            outputTriggerred = true;
                    }

                    if (m_outputScanState != ScanState.Running)
                        m_outputScanState = ScanState.Running;

                    if (m_platformInterop.OutputScanErrorCode == ErrorCodes.NoErrors)
                    {
                        m_totalBytesReceivedByDevice = m_platformInterop.TotalBytesReceivedByDevice;

                        m_currentOutputScanOutputIndex = (m_totalBytesReceivedByDevice - 1) % m_internalWriteBuffer.Length;

                        // update count and index
                        if (outputTriggerred)
                        {
                            m_outputScanCount = (ulong)(m_totalBytesReceivedByDevice / (m_criticalParams.DataOutXferSize * channelCount));

                            if (m_outputScanCount > (ulong)outputBufferSampleSize)
                                m_outputScanIndex = (long)Math.Max(-1, ((long)m_outputScanCount % (long)(m_internalWriteBuffer.Length / (long)m_criticalParams.DataOutXferSize)) - (long)channelCount);
                            else
                                m_outputScanIndex = (long)Math.Max(-1, (long)m_outputScanCount - (long)channelCount);
                        }
                    }
                    else
                    {
                        m_outputScanErrorCode = m_platformInterop.OutputScanErrorCode;
#if !WindowsCE
                        m_stopOutputScanDelegate.BeginInvoke(m_stopOutputScanCallback, m_stopOutputScanDelegate);
#endif
                    }

                    Thread.Sleep(whileSleepTime);
                }

            } while (ContinueProcessingOutputScan(m_outputScanErrorCode));

            while (!m_platformInterop.OutputScanComplete() && m_outputScanErrorCode == ErrorCodes.NoErrors)
                Thread.Sleep(0);

            // at this point all data has been accepted by the device for a finite scan
            // or the scan has been stopped so now wait for the actual device to go idle
            while (m_outputScanState == ScanState.Running)
            {
                m_outputScanState = GetOutputScanState();
                Thread.Sleep(statusSleepTime);
            }

            if (m_outputScanErrorCode == ErrorCodes.DataUnderrun)
                m_outputScanState = ScanState.Underrun;
            else
                m_outputScanState = ScanState.Idle;

            if (m_outputScanState == ScanState.Underrun)
                m_platformInterop.ClearDataUnderrun();

            m_currentOutputScanWriteIndex = 0;
            m_outputScanComplete=  true;

            m_errorCode = m_outputScanErrorCode;

            // set the DaqDevice's pending error so that it can handle it on the next message sent
            if (m_errorCode != ErrorCodes.NoErrors && m_errorCode != ErrorCodes.DataUnderrun)
                m_daqDevice.SetPendingOutputScanError(m_errorCode);
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

            if (errorCode != ErrorCodes.NoErrors)
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

        //===============================================================================
        /// <summary>
        /// Returns the last device response as a string
        /// </summary>
        /// <returns>The device response</returns>
        //===============================================================================
        internal string ReadStringDirect()
        {
            return m_internalReadStringDirect;
        }

        //===============================================================================
        /// <summary>
        /// Returns the last device component Value property as a numeric
        /// </summary>
        /// <returns>The component Value property</returns>
        //===============================================================================
        internal double ReadValueDirect()
        {
            return m_internalReadValueDirect;
        }

        //=============================================================================================================================================================
        /// <summary>
        /// Read's device's memory
        /// </summary>
        /// <param name="memAddrCmd">The device's memory address command (Request)</param>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal ErrorCodes ReadDeviceMemory1(byte memAddrCmd, byte memReadCmd, ushort memOffset, ushort memOffsetLength, byte count, out byte[] buffer)
        {
            return m_platformInterop.ReadDeviceMemory1(memAddrCmd, memReadCmd, memOffset, memOffsetLength, count, out buffer);
        }

        //=============================================================================================================================================================
        /// <summary>
        /// Read's device's memory
        /// </summary>
        /// <param name="memAddrCmd">The device's memory address command (Request)</param>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal ErrorCodes ReadDeviceMemory2(byte memReadCmd, ushort memOffset, ushort memOffsetLength, byte count, out byte[] buffer)
        {
            return m_platformInterop.ReadDeviceMemory2(memReadCmd, memOffset, memOffsetLength, count, out buffer);
        }

        //=============================================================================================================================================================
        /// <summary>
        /// Read's device's memory
        /// </summary>
        /// <param name="memAddrCmd">The device's memory address command (Request)</param>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal ErrorCodes ReadDeviceMemory3(byte memReadCmd, ushort memOffset, ushort memOffsetLength, byte count, out byte[] buffer)
        {
            return m_platformInterop.ReadDeviceMemory3(memReadCmd, memOffset, memOffsetLength, count, out buffer);
        }

        //=============================================================================================================================================================
        /// <summary>
        /// Read's device's memory
        /// </summary>
        /// <param name="memAddrCmd">The device's memory address command (Request)</param>
        /// <param name="memReadCmd">The device's memory read command (Request)</param>
        /// <param name="memoryOffset">The memory offset to read from</param>
        /// <param name="memoryOffsetLength">The size of the memory offset value (typically 2 bytes)</param>
        /// <param name="count">The number of bytes to read</param>
        /// <param name="buffer">The buffer to receive the data</param>
        /// <returns></returns>
        //=============================================================================================================================================================
        internal ErrorCodes ReadDeviceMemory4(byte memReadCmd, ushort memOffset, ushort memOffsetLength, byte count, out byte[] buffer)
        {
            return m_platformInterop.ReadDeviceMemory4(memReadCmd, memOffset, memOffsetLength, count, out buffer);
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

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="memAddrCmd">The device's memory address command</param>
        /// <param name="memWriteCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal ErrorCodes WriteDeviceMemory1(byte memAddrCmd, byte memWriteCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            return m_platformInterop.WriteDeviceMemory1(memAddrCmd, memWriteCmd, memoryOffset, memOffsetLength, bufferOffset, buffer, count);
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="memWriteCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal ErrorCodes WriteDeviceMemory2(byte memWriteCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            return m_platformInterop.WriteDeviceMemory2(memWriteCmd, memoryOffset, memOffsetLength, bufferOffset, buffer, count);
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="unlockKey">The device's unlock key</param>
        /// <param name="memWriteCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal ErrorCodes WriteDeviceMemory3(ushort unlockKey, byte memCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            return m_platformInterop.WriteDeviceMemory3(unlockKey, memCmd, memoryOffset, memOffsetLength, bufferOffset, buffer, count);
        }

        //==============================================================================================================================================================================
        /// <summary>
        /// Virtual method to Write data to a device's memory
        /// </summary>
        /// <param name="unlockKey">The device's unlock key</param>
        /// <param name="memWriteCmd">The device's memory write command</param>
        /// <param name="memoryOffset">The memory offset to start writing to</param>
        /// <param name="memOffsetLength">The size of the memoryOffset value (typically 2 bytes)</param>
        /// <param name="bufferOffset">The buffer offset</param>
        /// <param name="buffer">The buffer containg the data to write to memory</param>
        /// <param name="count">The number of bytes to write</param>
        /// <returns></returns>
        //==============================================================================================================================================================================
        internal ErrorCodes WriteDeviceMemory4(ushort unlockKey, byte memCmd, ushort memoryOffset, ushort memOffsetLength, ushort bufferOffset, byte[] buffer, byte count)
        {
            return m_platformInterop.WriteDeviceMemory4(unlockKey, memCmd, memoryOffset, memOffsetLength, bufferOffset, buffer, count);
        }

        //===================================================================================================
        /// <summary>
        /// Loads data into the device's FPGA
        /// </summary>
        /// <param name="buffer">The data to load</param>
        /// <returns>The error code</returns>
        //===================================================================================================
        internal ErrorCodes LoadFPGA(byte request, byte[] buffer)
        {
            return m_platformInterop.LoadFPGA(request, buffer);
        }

        //===============================================================================
        /// <summary>
        /// Creates a set of USB packets to send to the device. One for sending the
        /// message to the device and one for receiving the device's response
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

            bool deferMessage = false;

            deferMessage = CheckForCriticalParams(msg);

            // Add a Control Out Packet

            // store the message in the Control Out Packet buffer
            for (int i = 0; i < message.Length; i++)
            {
                m_controlOutPacket.Buffer[i] = message[i];
            }

            m_controlOutPacket.DeferTransfer = deferMessage;
            m_controlOutPacket.BytesTransfered = 0;

            m_usbPackets.Add(m_controlOutPacket);

            // Add a Control In Packet for the device's response
            if (!m_controlOutPacket.DeferTransfer)
            {
                Array.Clear(m_controlInPacket.Buffer, 0, m_controlInPacket.Length);
                m_usbPackets.Add(m_controlInPacket);
            }
        }

        //===============================================================================
        /// <summary>
        /// Creates a set of USB packets to send to the device. One for sending the
        /// message to the device and one for receiving the device's response
        /// </summary>
        /// <param name="message">The message to send to the device</param>
        //===============================================================================
        protected void CreateUsbPacketsDirect(byte[] message)
        {
            // clear the list of packets
            m_usbPacketsDirect.Clear();
            
            // Add a Control Out Packet

            // store the message in the Control Out Packet buffer
            for (int i = 0; i < message.Length; i++)
            {
                m_controlOutPacketDirect.Buffer[i] = message[i];
            }

            m_controlOutPacketDirect.DeferTransfer = false;
            m_controlOutPacketDirect.BytesTransfered = 0;

            m_usbPacketsDirect.Add(m_controlOutPacketDirect);

            // Add a Control In Packet for the device's response
            if (!m_controlOutPacketDirect.DeferTransfer)
            {
                m_usbPacketsDirect.Add(m_controlInPacket);
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
                        m_totalSamplesToReadPerChannel = m_internalReadBuffer.Length / (m_criticalParams.DataInXferSize * m_criticalParams.AiChannelCount);
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
                m_criticalParams.BulkInXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.SAMPLES))
            {
                // Finite mode
                m_totalSamplesToReadPerChannel = MessageTranslator.GetSamples(message);
                m_criticalParams.InputScanSamples = m_totalSamplesToReadPerChannel;
                m_criticalParams.InputSampleMode = SampleMode.Finite;
                m_criticalParams.BulkInXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.XFERMODE))
            {
                TransferMode tm = MessageTranslator.GetTransferMode(message);
                m_criticalParams.InputTransferMode = tm;
                m_criticalParams.BulkInXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.HIGHCHAN))
            {
                int ch = MessageTranslator.GetChannel(message);
                m_criticalParams.HighAiChannel = ch;
                m_criticalParams.BulkInXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
                return false;
            }

            if (message.Contains(DaqComponents.AISCAN) && message.Contains(DaqProperties.LOWCHAN))
            {
                int ch = MessageTranslator.GetChannel(message);
                m_criticalParams.LowAiChannel = ch;
                m_criticalParams.BulkInXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
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
                    m_criticalParams.BulkInXferSize = GetOptimalInputBufferSize(m_criticalParams.InputScanRate);
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
                m_criticalParams.BulkOutXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
                return false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqProperties.SAMPLES))
            {
                // Finite mode
                m_totalSamplesToWritePerChannel = MessageTranslator.GetSamples(message);
                m_criticalParams.OutputScanSamples = m_totalSamplesToWritePerChannel;
                m_criticalParams.OutputSampleMode = SampleMode.Finite;
                m_criticalParams.BulkOutXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
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
                    m_criticalParams.BulkOutXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
                }

                return false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqProperties.HIGHCHAN))
            {
                int ch = MessageTranslator.GetChannel(message);
                m_criticalParams.HighAoChannel = ch;
                m_criticalParams.AoChannelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;
                m_criticalParams.BulkOutXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
                return false;
            }

            else if (message.Contains(DaqComponents.AOSCAN) && message.Contains(DaqProperties.LOWCHAN))
            {
                int ch = MessageTranslator.GetChannel(message);
                m_criticalParams.LowAoChannel = ch;
                m_criticalParams.AoChannelCount = m_criticalParams.HighAoChannel - m_criticalParams.LowAoChannel + 1;
                m_criticalParams.BulkOutXferSize = GetOptimalOutputBufferSize(m_criticalParams.OutputScanRate);
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
            int bytesPerSample = CriticalParams.DataInXferSize;
            int channelCount = m_criticalParams.AiChannelCount;
            int numberOfBytesRequested;
            int numberOfNewBytes = 0;
            long elapsedTime;

            if (timeOut < 0)
                timeOut = 0;

            if (m_inputScanStatus == ScanState.Idle && m_inputScanComplete && m_onInputScanCompleteCallbackControl != null && m_onInputScanCompleteCallbackControl.Created)
            {
                numberOfBytesRequested = channelCount * bytesPerSample * numberOfSamplesRequested;
                numberOfNewBytes = channelCount * bytesPerSample * (int)m_scanCompleteCallbackParam[0];
            }
            else if (m_inputScanStatus == ScanState.Idle && m_inputScanComplete && m_inputSamplesReadPerChannel < (ulong)m_criticalParams.InputScanSamples)
            {
                numberOfBytesRequested = channelCount * bytesPerSample * numberOfSamplesRequested;

                if (m_inputBufferFilled)
                    numberOfNewBytes = (int)((ulong)m_internalReadBuffer.Length - m_inputSamplesReadPerChannel);
                else
                    numberOfNewBytes = GetFreshDataCount();
            }
            else
            {
                numberOfNewBytes = 0;

                if (m_criticalParams.InputSampleMode == SampleMode.Finite && 
                        numberOfSamplesRequested > m_criticalParams.InputScanSamples)
                {
                    numberOfSamplesRequested = m_criticalParams.InputScanSamples;
                }
                else if (m_criticalParams.TriggerRearmEnabled)
                {
                    if (m_onDataAvailableCallbackControl != null)
                        numberOfSamplesRequested = m_onDataAvailableCallbackControl.NumberOfSamples;
                    else
                        numberOfSamplesRequested = m_criticalParams.AdjustedRearmSamplesPerTrigger;
                }
                else if (numberOfSamplesRequested > m_internalReadBuffer.Length)
                {
                    m_errorCode = ErrorCodes.TooManySamplesRequested;
                    return 0;
                }

                numberOfBytesRequested = channelCount * (bytesPerSample * numberOfSamplesRequested);

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

                    if (timeOut == 0 || timeOut > 20)
                        Thread.Sleep(1);

                    elapsedTime = m_readStopWatch.ElapsedMilliseconds;

                    if (timeOut > 0 && elapsedTime >= timeOut)
                    {
                        m_errorCode = ErrorCodes.InputScanTimeOut;
                        break;
                    }

                    // this is called from the same thread as ReadScanData so 
                    // we need to check for system events
                    //Application.DoEvents();
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

            if (timeOut < 0)
                timeOut = 0;

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

                    if (timeOut == 0 || timeOut > 20)
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
            string triggerSupport = m_daqDevice.GetDevCapsString("AISCAN:TRIG", false);

            if (triggerSupport.Contains(DevCapImplementations.PROG))
            {
                string response;

                m_daqDevice.SendMessageDirect(Messages.AISCAN_TRIG_QUERY);
                response = ReadStringDirect();

                UsbSetupPacket packet = new UsbSetupPacket(Constants.MAX_MESSAGE_LENGTH);

                // if the trigger is enabled, check the AISCAN:REARM setting
                if (response.Contains(PropertyValues.ENABLE))
                {
                    string rearmSupport = m_daqDevice.GetDevCapsString("AITRIG:REARM", false);

                    if (rearmSupport.Contains(DevCapImplementations.PROG))
                    {
                        // check if rearm is enabled
                        m_daqDevice.SendMessageDirect(Messages.AITRIG_REARM_QUERY);
                        response = ReadStringDirect();

                        if (response.Contains(PropertyValues.ENABLE))
                        {
                            // transfer size must be integer multiple of the packet size because
                            // we're going to switch to continuous mode 
                            while (m_criticalParams.BulkInXferSize % m_criticalParams.AiChannelCount != 0)
                                m_criticalParams.BulkInXferSize += m_deviceInfo.MaxPacketSize;

                            // switch to continuous mode so the device can continually re-trigger
                            m_criticalParams.InputSampleMode = SampleMode.Continuous;

                            // get the current sample count
                            m_daqDevice.SendMessageDirect(Messages.AISCAN_SAMPLES_QUERY);
                            response = ReadStringDirect();
                            response = response.Trim(new char[] { Constants.NULL_TERMINATOR });

                            int samples = MessageTranslator.GetSamples(response);
                            m_criticalParams.AdjustedRearmSamplesPerTrigger = samples;

                            // calculate the number of bytes in each transfer
                            int inputBytes = m_criticalParams.AiChannelCount * m_criticalParams.DataInXferSize * samples;

                            int rearmBytes = 0;
                            int rearmSamples = 0;

                            if (inputBytes % m_criticalParams.BulkInXferSize != 0)
                            {
                                // when xfer mode is not SINGLEIO, the rearm bytes need to be an integer multiple of the xfer size
                                // because we're using continuous mode
                                if (m_criticalParams.InputTransferMode != TransferMode.SingleIO)
                                    rearmBytes = (int)Math.Floor((double)inputBytes / (double)m_criticalParams.BulkInXferSize) * m_criticalParams.BulkInXferSize;
                                else
                                    rearmBytes = inputBytes;

                                // adjust the number of samples 
                                rearmSamples = rearmBytes / (m_criticalParams.AiChannelCount * m_criticalParams.DataInXferSize);

                                // now update the device's SAMPLE property 
                                string msg = Messages.AISCAN_SAMPLES;
                                msg = Messages.InsertValue(msg, rearmSamples);

                                m_criticalParams.AdjustedRearmSamplesPerTrigger = rearmSamples;
                            }

                            // Delta rearm samples will non-zero when xfer mode is not SINGLEIO
                            if (m_criticalParams.InputTransferMode != TransferMode.SingleIO)
                                m_criticalParams.DeltaRearmInputSamples = rearmSamples - samples;
                            else
                                m_criticalParams.DeltaRearmInputSamples = 0;

                            m_totalSamplesToReadPerChannel = INTERNAL_READ_BUFFER_SIZE / 2;

                            m_totalSamplesToReadPerChannel = (int)Math.Ceiling((double)m_totalSamplesToReadPerChannel / (double)m_criticalParams.AiChannelCount) * m_criticalParams.AiChannelCount;
                            m_criticalParams.InputScanSamples = m_totalSamplesToReadPerChannel;

                            if (m_onDataAvailableCallbackControl != null && m_onDataAvailableCallbackControl.NumberOfSamples > m_criticalParams.AdjustedRearmSamplesPerTrigger)
                                m_onDataAvailableCallbackControl.NumberOfSamples = m_criticalParams.AdjustedRearmSamplesPerTrigger;
                        }
                        else
                        {
                            m_criticalParams.DeltaRearmInputSamples = 0;
                        }
                    }
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
                    // Let ProcessOutputScanThread clear the data underrun when it exits.
                    //m_platformInterop.ClearDataUnderrun();
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

            numberOfNewBytes = (int)Math.Min(m_internalReadBuffer.Length, (double)((ulong)(m_criticalParams.DataInXferSize * m_criticalParams.AiChannelCount) * (m_inputScanCount - m_inputSamplesReadPerChannel)));

            return numberOfNewBytes;
        }

        //======================================================================

        /// <summary>
        /// Lets the driver free any resources associated with the device
        /// </summary>
        //======================================================================
        internal void ReleaseDevice()
        {
            if (m_inputScanStatus == ScanState.Running)
                StopInputScan(true);

            if (m_outputScanState == ScanState.Running)
                StopOutputScan(true);

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
        protected CallbackInfo QueueCallbackInfo(int avaiableSamplesPerChannel, ulong samplesReadPerChannel, QueueAction queueAction)
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

#if DEBUG

        List<Dictionary<string, string>> m_objectDumps = new List<Dictionary<string, string>>();

        protected void DumpObject(object obj)
        {
            Dictionary<string, string> objectDump = new Dictionary<string, string>();

            Type type = obj.GetType();

            if (type != null)
            {
                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                if (fieldInfos != null)
                {
                    if (fieldInfos.Length > 0)
                    {
                        string fieldName;
                        string fieldValue;
                        object val;

                        for (int i = 0; i < fieldInfos.Length; i++)
                        {
                            fieldName = fieldInfos[i].Name;
                            val = fieldInfos[i].GetValue(this);

                            if (val != null)
                            {
                                fieldValue = val.ToString();
                                objectDump.Add(fieldName, fieldValue);
                            }
                            else
                            {
                                objectDump.Add(fieldName, "null");
                            }
                        }

                        m_objectDumps.Add(objectDump);

                        CompareObjects();
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Could not get DaqComponent fields");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Could not get DaqComponent fields");
                }
            }
        }

        protected void CompareObjects()
        {
            if (m_objectDumps.Count > 1)
            {
                // compare the last two object dumps
                Dictionary<string, string> obj1 = m_objectDumps[m_objectDumps.Count - 1];
                Dictionary<string, string> obj2 = m_objectDumps[m_objectDumps.Count - 2];

                int fieldCount = obj1.Count;

                string[] obj1Values = new string[fieldCount];
                string[] obj2Values = new string[fieldCount];
                string[] keys = new string[fieldCount];

                obj1.Values.CopyTo(obj1Values, 0);
                obj2.Values.CopyTo(obj2Values, 0);
                obj1.Keys.CopyTo(keys, 0);

                bool dumpValues = false;

                for (int i = 0; i < obj1Values.Length; i++)
                {
                    if (obj1Values[i] != obj2Values[i])
                    {
                        dumpValues = true;
                        DebugLogger.WriteLine("{0}.{1} = {2},{3}", this, keys[i], obj1Values[i], obj2Values[i]);
                    }
                }

                if (dumpValues)
                    DebugLogger.DumpDebugInfo();
            }
        }
#endif

    }

    //=====================================================================================
    /// <summary>
    /// Encapsulates data used for invoking callback methods
    /// </summary>
    //=====================================================================================
    internal class CallbackInfo
    {
        internal CallbackInfo(int availableSamplesPerChannel, ulong samplesReadPerChannel)
        {
            AvailableSamplesPerChannel = availableSamplesPerChannel;
            SamplesReadPerChannel = samplesReadPerChannel;
        }

        internal int AvailableSamplesPerChannel { get; set; }

        internal ulong SamplesReadPerChannel { get; set; }
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

        internal UsbSetupPacket(int bufferSize)
        {
            m_buffer = new byte[bufferSize];
            m_wLength = (ushort)bufferSize;
            m_wValue = 0;
            m_wIndex = 0;
            m_deferTransfer = false;
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

        //internal bool IsQuery
        //{
        //    get { return m_isQuery; }
        //    set { m_isQuery = value; }
        //}
    }
}
