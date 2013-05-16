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
using System.Windows.Forms;
using System.Threading;

namespace MeasurementComputing.DAQFlex
{
    //======================================================================
    /// <summary>
    /// Base class for all daq device classes
    /// </summary>
    //======================================================================
    public partial class DaqDevice
    {
        internal DriverInterface m_driverInterface;
        internal ASCIIEncoding m_ae;
        internal DaqResponse m_apiResponse;
        internal ErrorCodes m_apiMessageError = ErrorCodes.NoErrors;

        internal Dictionary<string, string> m_deviceCaps = new Dictionary<string, string>();

        internal DeviceReflector m_reflector;
        protected Mutex m_deviceMutex = new Mutex();
        protected bool m_deviceReleased = true;
        private byte[] m_devIdMessage = new byte[Constants.MAX_COMMAND_LENGTH];
        protected List<string> m_compositeResponseMessages = new List<string>();
        protected bool m_updateRanges = false;
        protected ushort m_devCapsOffset = 0;
        protected byte[] m_compressedDeviceCaps;
        protected byte[] m_uncompressedDeviceCaps;
        protected byte[] m_defaultDevCapsImage;
        protected string m_devCapsVerion;
        protected string m_devCapsID;
        protected int m_deviceID;
        protected string m_mfgSerno;
        protected ushort m_memLockAddr;
        protected ushort m_memUnlockCode;
        protected ushort m_memLockCode;
        protected double m_firmwareVersion;

        //======================================================================
        /// <summary>
        /// ctor - creates the driver interface object
        /// </summary>
        /// <param name="deviceInfo">A device info object</param>
        //======================================================================
        internal DaqDevice(DeviceInfo deviceInfo, ushort devCapsOffset)
        {
            m_deviceID = deviceInfo.Pid;

            m_devCapsOffset = devCapsOffset;

#if !WindowsCE
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
#endif
            m_driverInterface = new DriverInterface(this, deviceInfo);

            //PopulateCompositeResponseList();

            m_deviceReleased = false;

            // create device ID message "?DEV:ID"
            m_devIdMessage[0] = 0x3F;
            m_devIdMessage[1] = 0x44;
            m_devIdMessage[2] = 0x45;
            m_devIdMessage[3] = 0x56;
            m_devIdMessage[4] = 0x3A;
            m_devIdMessage[5] = 0x49;
            m_devIdMessage[6] = 0x44;

            m_ae = new ASCIIEncoding();

            m_firmwareVersion = SendMessage("?DEV:FWV").ToValue();

            if (m_driverInterface.ErrorCode == ErrorCodes.NoErrors)
            {
                // Include the data type in the first byte of data returned for 
                SendMessageDirect("DEV:DATATYPE=ENABLE");
            }
            else
            {
                DaqException ex = ResolveException(m_driverInterface.ErrorCode);
                throw ex;
            }
        }

        #region Component classes
        //====================================================================
        /// <summary>
        /// The device's Ai component
        /// </summary>
        //====================================================================
        internal AiComponent Ai { get; set; }

        //====================================================================
        /// <summary>
        /// The device's Ao component
        /// </summary>
        //====================================================================
        internal AoComponent Ao { get; set; }

        //====================================================================
        /// <summary>
        /// The device's Dio component
        /// </summary>
        //====================================================================
        internal DioComponent Dio { get; set; }

        //====================================================================
        /// <summary>
        /// The device's Ctr component
        /// </summary>
        //====================================================================
        internal CtrComponent Ctr { get; set; }

        //====================================================================
        /// <summary>
        /// The device's Tmr component
        /// </summary>
        //====================================================================
        internal TmrComponent Tmr { get; set; }

        #endregion

        //====================================================================
        /// <summary>
        /// Critical parameters for use by the driver interface
        /// </summary>
        //====================================================================
        internal CriticalParams CriticalParams
        {
            get { return m_driverInterface.CriticalParams; }
        }

        //====================================================================
        /// <summary>
        /// Gets a reference to the device's driver interface
        /// </summary>
        //====================================================================
        internal DriverInterface DriverInterface
        {
            get { return m_driverInterface; }
        }

        //====================================================================
        /// <summary>
        /// A reference to the device's DaqReflector object
        /// </summary>
        //====================================================================
        internal DeviceReflector Reflector
        {
            get { return m_reflector; }
        }

        //====================================================================
        /// <summary>
        /// The devices product ID
        /// </summary>
        //====================================================================
        internal int Pid
        {
            get { return m_deviceID; }
        }

        //====================================================================
        /// <summary>
        /// A boolean that indicates if the message should be sent to 
        /// the device or simply processed by the DAQFlex Library
        /// </summary>
        //====================================================================
        internal bool SendMessageToDevice { get; set; }

        //====================================================================
        /// <summary>
        /// The device's firmware version
        /// </summary>
        //====================================================================
        internal double FirmwareVersion
        {
            get { return m_firmwareVersion; }
            set { m_firmwareVersion = value; }
        }

        //====================================================================
        /// <summary>
        /// This is flag indicating if the IoComponent's m_ranges array 
        /// needs to be updated
        /// </summary>
        //====================================================================
        internal bool UpdateRanges
        {
            get { return m_updateRanges; }
            set { m_updateRanges = value; }
        }

        //====================================================================
        /// <summary>
        /// Gets or sets the device's API response
        /// This is a response generated by the device object and not
        /// the hardware device
        /// </summary>
        //====================================================================
        internal DaqResponse ApiResponse
        {
            get { return m_apiResponse; }
            set { m_apiResponse = value; }
        }

        //====================================================================
        /// <summary>
        /// Gets or set any errors associated with and API message
        /// </summary>
        //====================================================================
        internal ErrorCodes ApiMessageError
        {
            get { return m_apiMessageError; }
            set { m_apiMessageError = value; }
        }

        //====================================================================
        /// <summary>
        /// Handles the exit event of the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //====================================================================
#if ! WindowsCE
        private void OnApplicationExit(object sender, EventArgs e)
        {
            ShutDownDevice();
            DaqDeviceManager.ReleaseDevice(this);
        }
#endif

        //====================================================================
        /// <summary>
        /// Virutal method to shut down a device when the application exits
        /// </summary>
        //====================================================================
        protected virtual void ShutDownDevice()
        {
        }

        //=================================================================================================================
        /// <summary>
        /// updates ranges associated with the IoComponents
        /// </summary>
        //=================================================================================================================
        internal void UpdateIoCompRanges()
        {
            if (m_updateRanges)
            {
                Ai.UpdateRanges();
            }
        }

        //=================================================================================================================
        /// <summary>
        /// Get the component portion of a message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The component type</returns>
        //=================================================================================================================
        internal string GetComponentType(string message)
        {
            if (message.Contains("DEV:"))
                return DaqComponents.DEV;
            else if (message.Contains("AISCAN:") || message.Contains("AISCAN{"))
                return DaqComponents.AISCAN;
            else if (message.Contains("AITRIG:"))
                return DaqComponents.AITRIG;
            else if (message == "?AI" || message.Contains("AI{") || message.Contains("AI:"))
                return DaqComponents.AI;
            else if (message.Contains("AOSCAN"))
                return DaqComponents.AOSCAN;
            else if (message == "?AO" || message.Contains("AO{") || message.Contains("AO:"))
                return DaqComponents.AO;
            else if (message == "?DIO" || message.Contains("DIO{") || message.Contains("DIO:"))
                return DaqComponents.DIO;
            else if (message == "?CTR" || message.Contains("CTR{") || message.Contains("CTR:"))
                return DaqComponents.CTR;
            else if (message == "?TMR" || message.Contains("TMR{") || message.Contains("TMR:"))
                return DaqComponents.TMR;

            return String.Empty;
        }

        //===========================================================================
        /// <summary>
        /// Determines if the message supports a composite or single response type.
        /// A composite response type is Text + Numeric a single reponse type is
        /// Text only
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The response type</returns>
        //===========================================================================
        internal ResponseType GetResponseType(string message)
        {
            if (m_compositeResponseMessages.Contains(message))
                return ResponseType.Composite;
            else
                return ResponseType.Simple;
        }

        //============================================================================================
        /// <summary>
        /// Virtual method to modify the response in the case where the message is
        /// an API message
        /// </summary>
        /// <param name="originalResponse">The response sent back from the device</param>
        /// <returns>The original response or a modified version of the original response</returns>
        //============================================================================================
        protected virtual string AmendResponse(string originalResponse)
        {
            if (originalResponse.Contains(DaqComponents.AI) && originalResponse.Contains(DaqProperties.VALUE))
            {
                string newResponse = originalResponse.Insert(originalResponse.IndexOf("="), Ai.ValueUnits);
                return newResponse;
            }

            if (originalResponse.Contains(DaqComponents.AO) && originalResponse.Contains(DaqProperties.VALUE))
            {
                string newResponse = originalResponse + Ao.ValueUnits;
                return newResponse;
            }

            return originalResponse;
        }

        //====================================================================================
        /// <summary>
        /// Used to send messages directly to the device bypassing CheckForAPIMessage 
        /// </summary>
        /// <param name="message">The Message</param>
        /// <returns>The error code</returns>
        //====================================================================================
        internal ErrorCodes SendMessageDirect(string message)
        {
            byte[] messageBytes = new byte[Constants.MAX_COMMAND_LENGTH];

            ResponseType responseType = GetResponseType(message);

            // convert message to an array of bytes for the driver interface
            for (int i = 0; i < message.Length; i++)
                messageBytes[i] = (byte)(Char.ToUpper(message[i]));

            // let the driver interface transfer the message to the device
            return m_driverInterface.TransferMessage(messageBytes, responseType);
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to preprocess a message
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <returns>True if the message is to be sent to the device, otherwise false</returns>
        //===========================================================================================
        internal virtual bool PreprocessMessage(ref string message, string messageType)
        {
            SendMessageToDevice = true;

            if (messageType == DaqComponents.DEV)
            {
                if (message.Contains(APIMessages.DEVPID))
                {
                    ApiResponse = new DaqResponse(APIMessages.DEVPID.Remove(0, 1) + Constants.EQUAL_SIGN + m_deviceID.ToString(), (double)m_deviceID);
                    SendMessageToDevice = false;
                    return SendMessageToDevice;
                }

                if (message.Contains(DaqCommands.LOADCAPS))
                {
                    LoadDeviceCaps(true);

                    if (Ai != null)
                        Ai.Initialize();

                    if (Ao != null)
                        Ao.Initialize();

                    if (Dio != null)
                        Dio.Initialize();

                    if (Ctr != null)
                        Ctr.Initialize();

                    if (Tmr != null)
                        Tmr.Initialize();

                    ApiResponse = new DaqResponse(DaqComponents.DEV + ":" + DaqCommands.LOADCAPS, Double.NaN);

                    SendMessageToDevice = false;

                    return SendMessageToDevice;
                }
            }

            if (messageType == DaqComponents.AI || messageType == DaqComponents.AISCAN || messageType == DaqComponents.AITRIG)
            {
                if (Ai != null)
                {
                    m_apiMessageError = Ai.PreprocessMessage(ref message, messageType);

                    if (m_apiMessageError == ErrorCodes.NoErrors)
                        Ai.SetCriticalParams(message, messageType);

                    return SendMessageToDevice;
                }
            }

            if (messageType == DaqComponents.AO || messageType == DaqComponents.AOSCAN)
            {
                if (Ao != null)
                {
                    m_apiMessageError = Ao.PreprocessMessage(ref message, messageType);

                    if (m_apiMessageError == ErrorCodes.NoErrors)
                        Ao.SetCriticalParams(message, messageType);

                    return SendMessageToDevice;
                }
            }

            if (messageType == DaqComponents.DIO)
            {
                if (Dio != null)
                {
                    m_apiMessageError = Dio.PreprocessMessage(ref message, messageType);

                    return SendMessageToDevice;
                }
            }

            if (messageType == DaqComponents.CTR)
            {
                if (Ctr != null)
                {
                    m_apiMessageError = Ctr.PreprocessMessage(ref message, messageType);

                    return SendMessageToDevice;
                }
            }

            return true;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to process any data before a message is sent to a device
        /// </summary>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes PreprocessData(ref string message, string componentType)
        {
            if (componentType == DaqComponents.AO || componentType == DaqComponents.AOSCAN)
            {
                if (Ao != null)
                {
                    return Ao.PreprocessData(ref message, componentType);
                }
            }

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to process any data after a message is sent to a device
        /// </summary>
        /// <param name="dataType">The type of data (e.g. Ai, Ao, Dio)</param>
        /// <returns>An error code</returns>
        //===========================================================================================
        internal virtual ErrorCodes PostProcessData(string componentType, ref string response, ref double value)
        {
            if (componentType == DaqComponents.AI)
            {
                if (Ai != null)
                {
                    return Ai.PostProcessData(componentType, ref response, ref value);
                }
            }
            else if (componentType == DaqComponents.AO)
            {
                if (Ao != null)
                {
                    return Ao.PostProcessData(componentType, ref response, ref value);
                }
            }
            else if (componentType == DaqComponents.DIO)
            {
                if (Dio != null)
                {
                    return Dio.PostProcessData(componentType, ref response, ref value);
                }
            }
            else if (componentType == DaqComponents.CTR)
            {
                if (Ctr != null)
                {
                    return Ctr.PostProcessData(componentType, ref response, ref value);
                }
            }
            else if (componentType == DaqComponents.TMR)
            {
                if (Tmr != null)
                {
                    return Tmr.PostProcessData(componentType, ref response, ref value);
                }
            }

            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// Virtual method to make checks for things like OTD
        /// </summary>
        /// <param name="channel">The channel to scale</param>
        /// <param name="value">The raw A/D value</param>
        /// <returns>The original value or error condition specific value</returns>
        //===========================================================================================
        internal virtual ErrorCodes PrescaleData(int channel, ref double value)
        {
            return ErrorCodes.NoErrors;
        }

        //===========================================================================================
        /// <summary>
        /// generates a response based on any errors that occur in one of the Preprocess data methods
        /// </summary>
        /// <param name="errorCode">The error code that was set in the Preprocess data method</param>
        /// <param name="originalResponse">The response before calling the Preprocess data method</param>
        /// <returns>The response</returns>
        //===========================================================================================
        internal virtual string GetPreprocessDataErrorResponse(ErrorCodes errorCode, string originalResponse)
        {
            return originalResponse;
        }

        //=======================================================================
        /// <summary>
        /// Overriden to get Device Component messages supported
        /// by this specific device
        /// </summary>
        /// <returns>The list of messages</returns>
        //=======================================================================
        protected virtual List<string> GetMessages()
        {
            List<string> messages = new List<string>();

            messages.Add("DEV:FLASHLED/*");
            messages.Add("DEV:ID=*");

            messages.Add("?DEV:ID");
            messages.Add("?DEV:MFGSER");
            messages.Add("?DEV:FWV");
            messages.Add("?DEV:MFGCAL");
            messages.Add("?DEV:MFGCAL{*}");

            return messages;
        }

        //==============================================================================
        /// <summary>
        /// Creates an Exception object based on the error code
        /// </summary>
        /// <param name="errorCode">The error that occurred</param>
        /// <returns>The Exception</returns>
        //==============================================================================
        internal DaqException ResolveException(ErrorCodes errorCode)
        {
            DaqException daqException;

            string errorMessage = GetErrorMessage(errorCode);

            ErrorLevel level;

            if (errorCode < ErrorCodes.DeviceNotResponding)
                level = ErrorLevel.Warning;
            else
                level = ErrorLevel.Error;

            daqException = new DaqException(errorMessage, errorCode, level);

            return daqException;
        }

        //==============================================================================
        /// <summary>
        /// Creates an Exception object based on the error code
        /// </summary>
        /// <param name="errorCode">The error that occurred</param>
        /// <param name="response">The last response</param>
        /// <returns>The Exception</returns>
        //==============================================================================
        internal DaqException ResolveException(ErrorCodes errorCode, DaqResponse response)
        {
            DaqException daqException;

            string errorMessage = GetErrorMessage(errorCode);

            ErrorLevel level;

            if (errorCode < ErrorCodes.DeviceNotResponding)
                level = ErrorLevel.Warning;
            else
                level = ErrorLevel.Error;

            daqException = new DaqException(errorMessage, errorCode, level, response);

            return daqException;
        }

        //==============================================================================
        /// <summary>
        /// Creates an Exception object based on the error code
        /// </summary>
        /// <param name="deviceMessage">The message that was sent to the device</param>
        /// <param name="errorCode">The error that occurred</param>
        /// <returns>The Exception</returns>
        //==============================================================================
        internal DaqException ResolveException(string deviceMessage, ErrorCodes errorCode)
        {
            DaqException daqException;

            string errorMessage = GetErrorMessage(errorCode);

            if (errorCode == ErrorCodes.InvalidMessage)
                errorMessage += String.Format(" [{0}]", deviceMessage);

            daqException = new DaqException(errorMessage, errorCode);

            return daqException;
        }

        //======================================================================
        /// <summary>
        /// Lets the driver free any resources associated with the device
        /// </summary>
        //======================================================================
        internal void ReleaseDevice()
        {
            m_driverInterface.ReleaseDevice();
            m_deviceReleased = true;
        }

        //===================================================================================================================
        /// <summary>
        /// Converts from engineering units to an A/D count value
        /// </summary>
        /// <param name="valueToConvert">The value to convert</param>
        /// <param name="minValue">The min A/D count value</param>
        /// <param name="maxValue">The max A/D count value</param>
        /// <param name="countRange">the count range (e.g. 12- bit = 4096, 16-bit = 65536)</param>
        /// <returns>The convert value as an A/D count</returns>
        //===================================================================================================================
        protected virtual int FromEngineeringUnits(double valueToConvert, double minValue, double maxValue, int countRange)
        {
            double deltaValue = maxValue - minValue;

            int convertedValue = (int)(((valueToConvert - minValue) / deltaValue) * countRange);

            return (int)Math.Max(0, Math.Min(countRange - 1, convertedValue));
        }

        //===============================================================================================================
        /// <summary>
        /// Loads the device's capabilities and stores them in a list
        /// </summary>
        //===============================================================================================================
        internal virtual void LoadDeviceCaps(bool forceUpdate)
        {
            m_reflector = new DeviceReflector();

            // read the device caps from the device's memory
            m_compressedDeviceCaps = ReadDeviceCaps();

            if (m_compressedDeviceCaps != null)
            {
                for (int i = 0; i < m_compressedDeviceCaps.Length; i++)
                {
                    if (m_compressedDeviceCaps[i] != m_defaultDevCapsImage[i])
                    {
                        m_compressedDeviceCaps = null;
                        break;
                    }
                }
            }

            if (m_compressedDeviceCaps == null || forceUpdate)
            {
                // try restoring the device caps
                if (m_memLockAddr != 0x0 && m_memUnlockCode != 0x0)
                {
                    ErrorCodes errorCode = RestoreDeviceCaps();

                    if (errorCode == ErrorCodes.NoErrors)
                        m_compressedDeviceCaps = ReadDeviceCaps();

                }

                if (m_compressedDeviceCaps == null)
                    m_compressedDeviceCaps = m_defaultDevCapsImage;

                if (m_compressedDeviceCaps == null)
                    System.Diagnostics.Debug.Assert(false, String.Format("device caps for {0} is null", this.ToString()));
            }

            if (m_compressedDeviceCaps != null)
            {
                m_uncompressedDeviceCaps = m_reflector.DecompressDeviceCapsImage(m_compressedDeviceCaps);
                ConvertDeviceCaps(m_uncompressedDeviceCaps);
            }
        }

        //===============================================================================================================
        /// <summary>
        /// Rewrites the device's capabilities to eeprom using m_defaultDevCapsImage
        /// </summary>
        /// <returns>The error code</returns>
        //===============================================================================================================
        internal ErrorCodes RestoreDeviceCaps()
        {
            ErrorCodes errorCode = ErrorCodes.NoErrors;

            // unlock the memory for writing
            errorCode = m_driverInterface.UnlockDeviceMemory(m_memLockAddr, m_memUnlockCode);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            byte[] buffer = m_defaultDevCapsImage;
            byte size = (byte)Constants.MAX_COMMAND_LENGTH;
            ushort memoryOffset = m_devCapsOffset;
            ushort bufferOffset = 0;

            int wholeBlocks = buffer.Length / size;
            int partialBlock = buffer.Length - size * wholeBlocks;

            byte[] count = new byte[2];
            count[0] = (byte)(buffer.Length & 0x00FF);
            count[1] = (byte)((buffer.Length & 0xFF00) >> 8);

            errorCode = m_driverInterface.WriteDeviceMemory(memoryOffset, 0, count, 2);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            memoryOffset += 2;

            for (int i = 0; i < wholeBlocks; i++)
            {
                errorCode = m_driverInterface.WriteDeviceMemory(memoryOffset, bufferOffset, buffer, size);

                if (errorCode != ErrorCodes.NoErrors)
                    return errorCode;

                memoryOffset += size;
                bufferOffset += size;
            }

            errorCode = m_driverInterface.WriteDeviceMemory(memoryOffset, bufferOffset, buffer, (byte)partialBlock);

            if (errorCode != ErrorCodes.NoErrors)
                return errorCode;

            // re-lock the memory
            m_driverInterface.LockDeviceMemory(m_memLockAddr, m_memLockCode);

            return errorCode;
        }

        //===============================================================================================================
        /// <summary>
        /// Virtual method to initialize IO components
        /// </summary>
        //===============================================================================================================
        internal virtual void Initialize()
        {
            if (Ai != null)
                Ai.Initialize();

            if (Ao != null)
                Ao.Initialize();

            if (Dio != null)
                Dio.Initialize();

            if (Ctr != null)
                Ctr.Initialize();

            if (Tmr != null)
                Tmr.Initialize();

            string msg = Constants.QUERY +
                            DaqComponents.DEV +
                                Constants.PROPERTY_SEPARATOR +
                                    DaqProperties.MFGSER;

            try
            {
                m_mfgSerno = MessageTranslator.GetPropertyValue(SendMessage(msg).ToString());
            }
            catch (Exception)
            {
            }
        }

        //===============================================================================================================
        /// <summary>
        /// Reads the device's capabilities from the device's eeprom
        /// </summary>
        //===============================================================================================================
        protected byte[] ReadDeviceCaps()
        {
            List<byte> bList = new List<byte>();
            byte[] buffer;
            ushort offset = m_devCapsOffset;

            // read the first two bytes which is the number of bytes to read
            m_driverInterface.ReadDeviceMemory(offset, 2, out buffer);

            // set the byte count
            int byteCount = (int)buffer[0] + (int)(buffer[1] << 8);

            if (byteCount != 0xFFFF && byteCount == m_defaultDevCapsImage.Length)
            {
                // calculate the number of complete blocks to read (e.g. 64 bytes)
                int blockCount = byteCount / Constants.MAX_COMMAND_LENGTH;

                // calculate the remaining bytes to read
                int remainingBytes = byteCount - (blockCount * Constants.MAX_COMMAND_LENGTH);

                offset += 2;

                // read complete blocks
                for (int i = 0; i < blockCount; i++)
                {
                    m_driverInterface.ReadDeviceMemory(offset, Constants.MAX_COMMAND_LENGTH, out buffer);
                    bList.AddRange(buffer);
                    offset += Constants.MAX_COMMAND_LENGTH;
                }

                // read the remaining bytes
                if (remainingBytes > 0)
                    m_driverInterface.ReadDeviceMemory(offset, (byte)remainingBytes, out buffer);

                for (int i = 0; i < remainingBytes; i++)
                    bList.Add(buffer[i]);

                return bList.ToArray();
            }
            else
            {
                return null;
            }
        }

        //================================================================================================
        /// <summary>
        /// Uncompresses the device caps image that was stored on the device
        /// and creates a list of text-based device caps
        /// </summary>
        /// <param name="compressedImage">The compressed device caps image</param>
        //================================================================================================
        protected void ConvertDeviceCaps(byte[] uncompressedImage)
        {
            string image = m_ae.GetString(uncompressedImage, 0, uncompressedImage.Length);

            if (CheckCRC(image))
            {
                string[] deviceCapsList = image.Split(new char[] { '%' });

                int index = 0;

                // store the device ID
                m_devCapsID = deviceCapsList[index++];

                // store the device caps version
                m_devCapsVerion = deviceCapsList[index++];

                m_deviceCaps.Clear();

                string[] deviceCapsParts;

                try
                {
                    string deviceCaps;
                    
                    // convert codes to text
                    for (int i = index; i < (deviceCapsList.Length - 1); i++)
                    {
                        deviceCaps = deviceCapsList[i];

                        // the deviceCapsParts are...
                        // Component:DevCapsName:Config:Channels:Implementation:Type:DevCapsValue:DependentFeatures
                        deviceCapsParts = deviceCaps.Split(new char[] { ':' });

                        if (deviceCaps != String.Empty)
                        {
                            string devCapsKey;
                            string devCapsValue = String.Empty;

                            // get the component
                            string component = m_reflector.GetComponent(Int32.Parse(deviceCapsParts[0]));

                            if (component == String.Empty)
                                continue;

                            devCapsKey = component;

                            // get the feature name
                            string devCapsName = m_reflector.GetDevCapName(Int32.Parse(deviceCapsParts[1]));

                            if (devCapsName == String.Empty)
                                continue;

                            // store the name
                            devCapsKey += (":" + devCapsName);

                            // get the configuration this feature pertains to
                            string configuration;

                            if (deviceCapsParts[2] == "*")
                            {
                                configuration = "ALL";
                            }
                            else
                            {
                                configuration = m_reflector.GetConfiguration(Int32.Parse(deviceCapsParts[2]));

                                if (configuration == String.Empty)
                                    continue;
                                
                                devCapsKey += ("/" + configuration);
                            }

                            // get the channels this feature pertains to
                            string channels;

                            channels = deviceCapsParts[3];

                            // get the feature implementation
                            string implementation = m_reflector.GetImplementation(Int32.Parse(deviceCapsParts[4]));

                            if (implementation == String.Empty)
                                continue;

                            devCapsValue += implementation;
                            devCapsValue += '%';

                            // get value type
                            string valueType = m_reflector.GetValueType(Int32.Parse(deviceCapsParts[5]));

                            if (valueType == String.Empty)
                                continue;

                            // get the feature value
                            string[] valueParts;

                            if (valueType == "TXT")
                            {
                                valueParts = deviceCapsParts[6].Split(new char[] { ',' });

                                string value = String.Empty;

                                for (int j = 0; j < valueParts.Length; j++)
                                {
                                    value = m_reflector.GetValue(Int32.Parse(valueParts[j]));

                                    if (value == String.Empty)
                                        break;

                                    devCapsValue += value;

                                    if (j < (valueParts.Length - 1))
                                        devCapsValue += ",";
                                }

                                if (value == String.Empty)
                                    continue;
                            }
                            else
                            {
                                devCapsValue += deviceCapsParts[6];
                            }

                            // get the dependent features
                            if (deviceCapsParts.Length == 8)
                            {
                                devCapsValue += "<";

                                string[] dependentParts = deviceCapsParts[7].Split(new char[] { ',' });

                                for (int j = 0; j < dependentParts.Length; j++)
                                {
                                    devCapsValue += m_reflector.GetDevCapName(Int32.Parse(dependentParts[j]));

                                    if (j < (dependentParts.Length - 1))
                                        devCapsValue += ",";
                                }

                                devCapsValue += ">";
                            }

                            if (channels != "*" && channels != "X")
                            {
                                int lowChannel;
                                int highChannel;

                                string[] chs = channels.Split(new char[] { '-' });

                                lowChannel = Int32.Parse(chs[0]);

                                if (chs.Length > 1)
                                    highChannel = Int32.Parse(chs[1]);
                                else
                                    highChannel = lowChannel;

                                string chCaps;

                                for (int j = lowChannel; j <= highChannel; j++)
                                {
                                    chCaps = component + "{" + j.ToString() + "}:" + devCapsName;

                                    if (configuration != "ALL")
                                        chCaps += ("/" + configuration);

                                    m_deviceCaps.Add(chCaps, devCapsValue);
                                }
                            }
                            else
                            {
                                m_deviceCaps.Add(devCapsKey, devCapsValue);

                                if (channels == "*")
                                {
                                    switch (component)
                                    {
                                        case (DaqComponents.AI):
                                        case (DaqComponents.AISCAN):
                                            if (Ai != null)
                                                Ai.AddChannelDevCapsKey(m_deviceCaps, component, devCapsName, configuration, devCapsValue);
                                            else
                                                System.Diagnostics.Debug.Assert(false, String.Format("{0}: Ai component is null but device caps contains Ai caps", this.ToString()));
                                            break;
                                        case (DaqComponents.AO):
                                        case (DaqComponents.AOSCAN):
                                            if (Ao != null)
                                                Ao.AddChannelDevCapsKey(m_deviceCaps, component, devCapsName, configuration, devCapsValue);
                                            else
                                                System.Diagnostics.Debug.Assert(false, String.Format("{0}: Ao component is null but device caps contains Ao caps", this.ToString()));
                                            break;
                                        case (DaqComponents.DIO):
                                            if (Dio != null)
                                                Dio.AddChannelDevCapsKey(m_deviceCaps, component, devCapsName, configuration, devCapsValue);
                                            else
                                                System.Diagnostics.Debug.Assert(false, String.Format("{0}: Dio component is null but device caps contains Dio caps", this.ToString()));
                                            break;
                                        case (DaqComponents.CTR):
                                            if (Ctr != null)
                                                Ctr.AddChannelDevCapsKey(m_deviceCaps, component, devCapsName, configuration, devCapsValue);
                                            else
                                                System.Diagnostics.Debug.Assert(false, String.Format("{0}: Ctr component is null but device caps contains Ctr caps", this.ToString()));
                                            break;
                                        case (DaqComponents.TMR):
                                            if (Tmr != null)
                                                Tmr.AddChannelDevCapsKey(m_deviceCaps, component, devCapsName, configuration, devCapsValue);
                                            else
                                                System.Diagnostics.Debug.Assert(false, String.Format("{0}: Tmr component is null but device caps contains Tmr caps", this.ToString()));
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Assert(false, ex.Message);
                }
            }
        }

        //=====================================================================================================================
        /// <summary>
        /// Handles the device reflection messages
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The message response</returns>
        //=====================================================================================================================
        internal virtual DaqResponse GetDeviceCapability(string message)
        {
            string capsKey = message.Substring(1);
            string capsValue = GetDevCapsValue(capsKey, false);

            string textResponse; 
            
            try
            {
                if (capsValue != String.Empty)
                {
                    int implementationStartIndex = 0;
                    int implementationEndIndex = capsValue.IndexOf(Constants.PERCENT);

                    // get the implementation (e.g. "FIXED" or "PROG")
                    string implementation = capsValue.Substring(implementationStartIndex, implementationEndIndex - implementationStartIndex);

                    // if the implementation is not applicable then remove it from the value
                    if (implementation == DevCapImplementations.NAP)
                        capsValue = capsValue.Remove(0, implementationEndIndex + 1);

                    // build the text repsonse
                    textResponse = capsKey + Constants.EQUAL_SIGN + capsValue;

                    double responseValue = Double.NaN;

                    // if the response has a single numeric value, then use that as the responses numeric value
                    implementationEndIndex = textResponse.IndexOf(Constants.PERCENT);

                    string value = textResponse.Substring(textResponse.IndexOf(Constants.PERCENT) + 1, textResponse.Length - implementationEndIndex - 1);

                    double parsedValue = Double.NaN;

                    bool parsed = false;
#if WindowsCE
                    try
                    {
                        parsedValue = Double.Parse(value);
                        parsed = true;
                    }
                    catch (Exception)
                    {
                    }
#else
                    parsed = Double.TryParse(value, out parsedValue);
#endif

                    if (parsed)
                        responseValue = parsedValue;
                    return new DaqResponse(textResponse, responseValue);
                }
                else
                {
                    textResponse = capsKey + Constants.EQUAL_SIGN + PropertyValues.NOT_SUPPORTED;
                    return new DaqResponse(textResponse, Double.NaN);
                }
            }
            catch (Exception)
            {
                textResponse = capsKey + Constants.EQUAL_SIGN + PropertyValues.NOT_SUPPORTED;
                return new DaqResponse(textResponse, Double.NaN);
            }
        }

        //==================================================================================================================
        /// <summary>
        /// Gets the device caps value from the dictionary
        /// (used internally by the DaqDevice and IoComponent objects)
        /// </summary>
        /// <param name="message">A device reflection message</param>
        /// <param name="trim">A flag indicating if the response should be trimmed after the percent symbol</param>
        /// <returns>The device caps value</returns>
        //==================================================================================================================
        internal virtual string GetDevCapsValue(string capsKey, bool trim)
        {
            string capsValue;
            bool result = m_deviceCaps.TryGetValue(capsKey, out capsValue);

            if (result == true)
            {
                try
                {
                    if (trim)
                    {
                        capsValue = capsValue.Substring(capsValue.IndexOf(Constants.PERCENT) + 1);
                    }
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "Exception in GetDevCapsValue");
                }

                return capsValue;
            }
            else
            {
                return string.Empty;
            }
        }

        //==================================================================================================================
        /// <summary>
        /// Checks the CRC that's stored on the device against the 
        /// </summary>
        /// <param name="uncompressedImage">The uncompressed device reflection image that's was read from the device</param>
        /// <returns>True if the CRC check passes otherwise false</returns>
        //==================================================================================================================
        protected bool CheckCRC(string uncompressedImage)
        {
            try
            {
                // get the index of the last percent symbol
                int percentIndex = uncompressedImage.LastIndexOf('%');

                // get the crc occurs after the last percent symbol
                uint crc32 = UInt32.Parse(uncompressedImage.Substring(percentIndex + 1));

                // remove the CRC to get just the reflection info
                string reflectionInfo = uncompressedImage.Remove(percentIndex + 1, (uncompressedImage.Length - 1) - percentIndex);

                // convert the reflection info string to an array of bytes
                byte[] bytes = m_ae.GetBytes(reflectionInfo);

                // check the CRC agaisnt the stored CRC
                if (m_reflector.GetCRC32(bytes) != crc32)
                    return false;
                else
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    //============================================================
    /// <summary>
    /// Encapsulates calibration coefficients
    /// </summary>
    //============================================================
    internal class CalCoeffs
    {
        private double m_slope;
        private double m_offset;

        internal CalCoeffs(double slope, double offset)
        {
            m_slope = slope;
            m_offset = offset;
        }

        internal double Slope
        {
            get { return m_slope; }
        }

        internal double Offset
        {
            get { return m_offset; }
        }
    }

    internal class Range
    {
        private double m_upperLimit = 0.0;
        private double m_lowerLimit = 0.0;

        internal Range(double upperLimit, double lowerLimit)
        {
            m_upperLimit = upperLimit;
            m_lowerLimit = lowerLimit;
        }

        //========================================================================
        /// <summary>
        ///  The range's upper limit
        /// </summary>
        //========================================================================
        internal double UpperLimit
        {
            get { return m_upperLimit; }
        }

        //========================================================================
        /// <summary>
        /// The range's lower limit
        /// </summary>
        //========================================================================
        internal double LowerLimit
        {
            get { return m_lowerLimit; }
        }
    }

    internal struct ActiveChannels
    {
        internal int ChannelNumber;
        internal double UpperLimit;
        internal double LowerLimit;
        internal double CalSlope;
        internal double CalOffset;
    }

    internal class TcTempLimits
    {
        internal double LowerLimit;
        internal double UpperLimit;

        internal TcTempLimits(double lowerLimit, double upperLimit)
        {
            LowerLimit = lowerLimit;
            UpperLimit = upperLimit;
        }
    }
}
