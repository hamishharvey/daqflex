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
using System.Reflection;
using System.Globalization;

namespace MeasurementComputing.DAQFlex
{
    public delegate void InputScanCallbackDelegate(ErrorCodes errorCode, CallbackType callbackType,  object callbackData);

    public partial class DaqDevice
    {
        //======================================================================
        /// <summary>
        /// Main MBD API to send a message to a device
        /// </summary>
        /// <param name="message">The message content</param>
        /// <returns>The device's response</returns>
        //======================================================================
        public DaqResponse SendMessage(string message)
        {
            m_deviceMutex.WaitOne();

            DaqException dex;

            if (message == null || message == String.Empty)
            {
                dex = ResolveException(ErrorCodes.MessageIsEmpty);
                m_deviceMutex.ReleaseMutex();
                throw dex;
            }

            m_apiMessageError = ErrorCodes.NoErrors;

            //********************************************************
            // Convert the message to upper case
            //********************************************************
            string msg;

            if (!message.Contains(Constants.QUERY.ToString()) && message.Contains(DaqComponents.DEV) && message.Contains(DaqProperties.ID))
            {
                string id = MessageTranslator.GetPropertyValue(message);
                msg = message.Remove(message.IndexOf(Constants.EQUAL_SIGN) + 1, 
                                     message.Length - message.IndexOf(Constants.EQUAL_SIGN) - 1);
                msg = msg.ToUpper();
                msg += id;
            }
            else
            {
                msg = message.ToUpper();
            }

            //********************************************************
            // Check for a Device Reflection Message
            //********************************************************
            if (msg[0] == '@')
            {
                // '@' denotes a Device Reflection message - let the reflector handle it then return
                DaqResponse featureResponse = GetDeviceCapability(msg); ;
                m_deviceMutex.ReleaseMutex();
                return featureResponse;
            }

            //********************************************************
            // Get the component type this message pertains to
            //********************************************************
            string componentType = GetComponentType(msg);

            if (componentType == String.Empty)
            {
                dex = ResolveException(ErrorCodes.InvalidComponentSpecified);
                m_deviceMutex.ReleaseMutex();
                throw dex;
            }

            //********************************************************
            // Preprocess the message
            //********************************************************
            PreprocessMessage(ref msg, componentType);

            if (m_apiMessageError != ErrorCodes.NoErrors)
            {
                if (m_apiMessageError == ErrorCodes.InvalidMessage)
                    dex = ResolveException(msg, m_apiMessageError);
                else
                    dex = ResolveException(m_apiMessageError);

                throw dex;
            }

            //********************************************************
            // Return API response for API-only messages
            //********************************************************
            if (!SendMessageToDevice)
            {
                m_deviceMutex.ReleaseMutex();

                return m_apiResponse;
            }

            //********************************************************
            // Preprocess the data
            //********************************************************

            ErrorCodes result;

            if (msg.Contains("VALUE") && msg.Contains("="))
            {
                result = PreprocessData(ref msg, componentType);

                if (result != ErrorCodes.NoErrors)
                {
                    dex = ResolveException(result);
                    throw dex;
                }
            }

            //********************************************************
            // Transfer the message to the driver interface
            //********************************************************

            ResponseType responseType = GetResponseType(msg);

            byte[] messageBytes = new byte[Constants.MAX_COMMAND_LENGTH];

            // convert message to an array of bytes for the driver interface
            for (int i = 0; i < msg.Length; i++)
                messageBytes[i] = (byte)msg[i];

            // let the driver interface transfer the message to the device
            result = m_driverInterface.TransferMessage(messageBytes, responseType);

            // retry once on device not responding
            if (result == ErrorCodes.DeviceNotResponding)
                result = m_driverInterface.TransferMessage(messageBytes, responseType);

            // if there was an error throw an exception
            // the application needs to catch this
            if (result != ErrorCodes.NoErrors)
            {
                if (result == ErrorCodes.InvalidMessage)
                {
                    ErrorCodes ec = m_driverInterface.TransferMessage(m_devIdMessage, ResponseType.Simple);
                    if (ec != ErrorCodes.NoErrors)
                        result = ErrorCodes.DeviceNotResponding;
                }

                dex = ResolveException(msg, result);
                m_deviceMutex.ReleaseMutex(); 
                throw dex;
            }

            //********************************************************
            // Read back the mesage response
            //********************************************************

            DaqResponse response = null;
            string responseText = m_driverInterface.ReadString();
            double value = m_driverInterface.ReadValue();

            // create the response object
            response = new DaqResponse(responseText, value);

            //********************************************************
            // Post process any data sent back in the response
            //********************************************************

            if (message.Contains("?") && message.Contains("VALUE"))
            {
                // PostProcessData is allowed to modify the response
                result = PostProcessData(componentType, ref responseText, ref value);

                // process the response before throwing on error or warning
                responseText = AmendResponse(responseText);

                // recreate the response in case responseText  and value were modified
                response = new DaqResponse(responseText, value);

                if (result != ErrorCodes.NoErrors)
                {
                    dex = ResolveException(result, response);
                    throw dex;
                }
            }

            if (m_updateRanges)
                UpdateIoCompRanges();
                
            m_deviceMutex.ReleaseMutex();

            System.Diagnostics.Debug.Assert(response != null);

            return response;
        }

        //================================================================================================================
        /// <summary>
        /// Enables a callback method to be invoked when a certain condition is met
        /// </summary>
        /// <param name="callback">The callback delegate</param>
        /// <param name="type">The callback type</param>
        /// <param name="numberOfSamples">The number of samples that will be passed to the callback method</param>
        //================================================================================================================
        public void EnableCallback(InputScanCallbackDelegate callback, CallbackType callbackType, object callbackData)
        {
            m_deviceMutex.WaitOne();

            if (callbackType == CallbackType.OnDataAvailable)
            {
                if (m_driverInterface.OnDataAvailableCallbackControl != null)
                {
                    DaqException dex = new DaqException(ErrorMessages.CallbackOperationAlreadyEnabled, ErrorCodes.CallbackOperationAlreadyEnabled);
                    throw dex;
                }

                m_driverInterface.OnDataAvailableCallbackControl = new CallbackControl(this, callback, callbackType, callbackData);
            }
            else if (callbackType == CallbackType.OnInputScanComplete)
            {
                if (m_driverInterface.OnInputScanCompleteCallbackControl != null)
                {
                    DaqException dex = new DaqException(ErrorMessages.CallbackOperationAlreadyEnabled, ErrorCodes.CallbackOperationAlreadyEnabled);
                    throw dex;
                }

                m_driverInterface.OnInputScanCompleteCallbackControl = new CallbackControl(this, callback, callbackType, callbackData);
            }
            else if (callbackType == CallbackType.OnInputScanError)
            {
                if (m_driverInterface.OnInputScanErrorCallbackControl != null)
                {
                    DaqException dex = new DaqException(ErrorMessages.CallbackOperationAlreadyEnabled, ErrorCodes.CallbackOperationAlreadyEnabled);
                    throw dex;
                }

                m_driverInterface.OnInputScanErrorCallbackControl = new CallbackControl(this, callback, callbackType, callbackData);
            }

            m_deviceMutex.ReleaseMutex();
        }

        //===================================================================================================
        /// <summary>
        /// Disables a callback
        /// </summary>
        /// <param name="type">The callback type</param>
        //===================================================================================================
        public void DisableCallback(CallbackType callbackType)
        {
            m_deviceMutex.WaitOne();

            if (callbackType == CallbackType.OnDataAvailable)
                m_driverInterface.OnDataAvailableCallbackControl = null;
            else if (callbackType == CallbackType.OnInputScanComplete)
                m_driverInterface.OnInputScanCompleteCallbackControl = null;
            else if (callbackType == CallbackType.OnInputScanError)
                m_driverInterface.OnInputScanErrorCallbackControl = null;

            m_deviceMutex.ReleaseMutex();
            
        }

        //===================================================================================================
        /// <summary>
        /// Main MBD API to read scan data 
        /// </summary>
        /// <param name="samplesRequested">The number of samples to read</param>
        /// <param name="timeOut">The timeout in milli-seconds</param>
        /// <returns>An array containing the data</returns>
        //===================================================================================================
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use ReadScanData(int samplesRequested, int timeOut) instead")]
        public double[,] ReadScanData(int samplesRequested)
        {
            return ReadScanData(samplesRequested, 0);
        }

        //===================================================================================================
        /// <summary>
        /// Main MBD API to read scan data 
        /// </summary>
        /// <param name="channel">The channel to read data form</param>
        /// <param name="numberOfSamples">The number of samples to read</param>
        /// <returns>An array containing the data</returns>
        //===================================================================================================
        public double[,] ReadScanData(int samplesRequested, int timeOut)
        {
            m_deviceMutex.WaitOne();

            if (samplesRequested == 0)
            {
                Exception e = ResolveException(ErrorCodes.InputScanReadCountIsZero);
                m_deviceMutex.ReleaseMutex();
                throw e;
            }

            ErrorCodes errorCode;

            double[,] userScanData = null;

            int channelCount = m_driverInterface.CriticalParams.AiChannelCount;
            int byteRatio = (int)Math.Ceiling((double)m_driverInterface.CriticalParams.AiDataWidth / (double)Constants.BITS_PER_BYTE);
            int bytesRequested = samplesRequested * byteRatio * channelCount;

            if (bytesRequested > m_driverInterface.InputScanBuffer.Length)
            {
                errorCode = ErrorCodes.RequestedReadSamplesGreaterThanBufferSize;
            }
            else
            {
                userScanData = new double[channelCount, samplesRequested];

                errorCode = m_driverInterface.ErrorCode;

                // first check the driver interface error code 
                if (errorCode == ErrorCodes.NoErrors)
                {
                    if (m_driverInterface.CriticalParams.InputSampleMode == SampleMode.Finite)
                    {
                        if (samplesRequested > m_driverInterface.CriticalParams.InputScanSamples)
                            errorCode = ErrorCodes.TooManySamplesRequested;
                        if (m_driverInterface.CriticalParams.InputScanSamples == m_driverInterface.InputSamplesReadPerChannel)
                            errorCode = ErrorCodes.NoMoreInputSamplesAvailable;
                    }

                    if (errorCode == ErrorCodes.NoErrors)
                    {
                        // wait until there is enough fresh data to read
                        // if the device went idle then samples to read may be less than samples requested
                        int samplesToRead = m_driverInterface.WaitForData(samplesRequested, timeOut);

                        // check the error code again in case a data overrun occurred
                        if (m_driverInterface.ErrorCode == ErrorCodes.NoErrors)
                        {
                            if (samplesToRead > 0)
                            {
                                // get the current read index
                                int readIndex = m_driverInterface.CurrentInputScanReadIndex;

                                // get a reference to the driver interface's internal read buffer
                                byte[] internalReadBuffer = m_driverInterface.InputScanBuffer;

                                // copy the data to the inpuScanData array
                                Ai.CopyScanData(internalReadBuffer, userScanData, ref readIndex, samplesToRead);

                                // update the current read index
                                m_driverInterface.CurrentInputScanReadIndex = readIndex;
                            }
                            else
                            {
                                errorCode = ErrorCodes.NoMoreInputSamplesAvailable;
                            }
                        }
                        else
                        {
                            errorCode = m_driverInterface.ErrorCode;
                        }

                        if (m_driverInterface.CriticalParams.InputSampleMode == SampleMode.Finite)
                        {
                            if (m_driverInterface.CriticalParams.InputScanSamples == m_driverInterface.InputScanCount)
                            {
                                m_driverInterface.WaitForIdle();
                            }
                        }
                    }
                }
            }

            // if there's an error throw an exception.
            // the application needs to catch this
            if (errorCode != ErrorCodes.NoErrors)
            {
                Exception e = ResolveException(errorCode);
                m_deviceMutex.ReleaseMutex();
                throw e;
            }

            m_deviceMutex.ReleaseMutex();

            return userScanData;
        }

        //=========================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="numberOfSamplesPerChannel"></param>
        //=========================================================================================================
        public void WriteScanData(double[,] scanData, int numberOfSamplesPerChannel, int timeOut)
        {
            m_deviceMutex.WaitOne();

            if (numberOfSamplesPerChannel > scanData.GetLength(1))
            {
                Exception e = ResolveException(ErrorCodes.NumberOfSamplesPerChannelGreaterThanUserBufferSize);
                m_deviceMutex.ReleaseMutex();
                throw e;
            }

            ErrorCodes errorCode = ErrorCodes.NoErrors;

            int channelCount = scanData.GetLength(0);
            int byteRatio = (int)Math.Ceiling((double)m_driverInterface.CriticalParams.AoDataWidth / (double)Constants.BITS_PER_BYTE);
            int bytesRequested = numberOfSamplesPerChannel * byteRatio * channelCount;

            if (bytesRequested > m_driverInterface.OutputScanBuffer.Length)
                errorCode = ErrorCodes.TotalNumberOfSamplesGreaterThanOutputBufferSize;

            if (errorCode == ErrorCodes.NoErrors || m_driverInterface.OutputScanState == ScanState.Idle)
            {
                if (m_driverInterface.OutputScanState == ScanState.Running)
                {
                    if (channelCount * byteRatio * numberOfSamplesPerChannel > (m_driverInterface.OutputScanBuffer.Length / 2))
                        errorCode = ErrorCodes.NumberOfSamplesGreaterThanHalfBuffer;
                }

                if (errorCode == ErrorCodes.NoErrors)
                {
                    // wait for enough space to become available
                    m_driverInterface.WaitForSpace(bytesRequested, timeOut);

                    if (m_driverInterface.ErrorCode == ErrorCodes.NoErrors)
                    {
                        // get the index to where the writing will start from
                        int writeIndex = m_driverInterface.CurrentOutputScanWriteIndex;

                        // get the driver interface's output scan buffer
                        byte[] internalWriteBuffer = m_driverInterface.OutputScanBuffer;

                        // copy the data to the driver interface's output scan buffer and update the write index
                        Ao.CopyScanData(scanData, internalWriteBuffer, ref writeIndex, numberOfSamplesPerChannel, timeOut);

                        // set the driver interface's current write index
                        m_driverInterface.CurrentOutputScanWriteIndex = writeIndex;
                    }
                    else
                    {
                        errorCode = m_driverInterface.ErrorCode;
                    }
                }
            }

            if (errorCode != ErrorCodes.NoErrors)
            {
                Exception e = ResolveException(errorCode);
                m_deviceMutex.ReleaseMutex();
                throw e;
            }

            m_deviceMutex.ReleaseMutex();
        }

        //===========================================================================
        /// <summary>
        /// Gets a list of messages supported by the specified Daq Component
        /// </summary>
        /// <param name="DaqComponent">The Daq Component </param>
        /// <returns>The supported messages</returns>
        //===========================================================================
        public List<string> GetSupportedMessages(string daqComponent)
        {
            List<string> messageList = null;

            switch (daqComponent)
            {
                case (DaqComponents.DEV):
                    messageList = GetMessages();
                    break;
                case (DaqComponents.AI):
                    if (Ai != null)
                        messageList = Ai.GetMessages(daqComponent);
                    break;
                case (DaqComponents.AISCAN):
                    if (Ai != null)
                        messageList = Ai.GetMessages(daqComponent);
                    break;
                case (DaqComponents.AITRIG):
                    if (Ai != null)
                        messageList = Ai.GetMessages(daqComponent);
                    break;
                case (DaqComponents.AO):
                    if (Ao != null)
                        messageList = Ao.GetMessages(daqComponent);
                    break;
                case (DaqComponents.AOSCAN):
                    if (Ao != null)
                        messageList = Ao.GetMessages(daqComponent);
                    break;
                case (DaqComponents.DIO):
                    if (Dio != null)
                        messageList = Dio.GetMessages(daqComponent);
                    break;
                case (DaqComponents.CTR):
                    if (Ctr != null)
                        messageList = Ctr.GetMessages(daqComponent);
                    break;
                default:
                    messageList = new List<string>();
                    break;
            }

            return messageList;
        }

        //===================================================================================================
        /// <summary>
        /// Gets the error message associated with the error code
        /// </summary>
        /// <param name="errorCode">the error code</param>
        /// <returns>The error message</returns>
        //===================================================================================================
        public string GetErrorMessage(ErrorCodes errorCode)
        {
            m_deviceMutex.WaitOne();

            Type type = typeof(ErrorMessages);
            PropertyInfo pi = type.GetProperty(errorCode.ToString(), BindingFlags.Static | BindingFlags.Public);

            string message;

            if (null != pi)
            {
                message = pi.GetValue(null, null).ToString();
            }
            else
            {
                //System.Diagnostics.Debug.Assert(false, "Unidentified error");
                message = String.Format("No text associated with error code {0}", errorCode);
            }

            m_deviceMutex.ReleaseMutex();

            return message;
        }
    }
}
