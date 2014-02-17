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
using System.IO;
using System.Globalization;

namespace MeasurementComputing.DAQFlex
{
    //===========================================================================
    /// <summary>
    /// Base class for  USB-1608G series
    /// </summary>
    /// <param name="deviceInfo">A device info object</param>
    //===========================================================================
    internal class Usb1608GBase : DaqDevice
    {
        protected const string m_fpgaFileName = "USB_1608G.rbf";

        protected byte fpgaDataRequest = 0x51;

        //===========================================================================
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="deviceInfo">deviceInfo</param>
        //===========================================================================
        internal Usb1608GBase(DeviceInfo deviceInfo)
            : base(deviceInfo, 0x7100)
        {
            m_memLockAddr = 0x8000;
            m_memUnlockCode = 0xAA55;
            m_memLockCode = 0xFFFF;
        }

        //=====================================================================================================================
        /// <summary>
        /// Handles the device reflection messages
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The message response</returns>
        //=====================================================================================================================
        internal override string GetDevCapsString(string capsKey, bool trim)
        {
            if (capsKey.Contains(DaqComponents.AI) && capsKey.Contains(DevCapNames.CHANNELS))
            {
                string config = string.Empty;
                string response;
                string capsName;

                if (capsKey.Contains(Constants.VALUE_RESOLVER.ToString()))
                {
                    capsName = capsKey;
                }
                else
                {
                    string msg = "@AI:CHMODES";
                    response = SendMessage(msg).ToString();

                    if (response.Contains("PROG"))
                    {
                        msg = Messages.AI_CHMODE_QUERY;
                        SendMessageDirect(msg);
                        response = m_driverInterface.ReadStringDirect();
                        config = MessageTranslator.GetPropertyValue(response);

                        if (config == PropertyValues.MIXED)
                        {
                            msg = Messages.AI_CH_CHMODE_QUERY;
                            msg = Messages.InsertChannel(msg, 0);
                            SendMessageDirect(msg);
                            response = m_driverInterface.ReadStringDirect();
                            config = MessageTranslator.GetPropertyValue(response);
                        }
                    }

                    capsName = capsKey + Constants.VALUE_RESOLVER + config;
                }

                string capsValue;

                bool valueFound = m_deviceCaps.TryGetValue(capsName, out capsValue);

                if (valueFound)
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

                    return MessageTranslator.ConvertToCurrentCulture(capsValue);
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return base.GetDevCapsString(capsKey, trim);
            }
        }

        //=======================================================================
        /// <summary>
        /// Overriden to get Device Component messages supported
        /// by this specific device
        /// </summary>
        /// <returns>The list of messages</returns>
        //=======================================================================
        protected override List<string> GetMessages()
        {
            List<string> list = base.GetMessages();

            list.Add("?DEV:TEMP{*}");
            list.Add("?DEV:FPGAV");

            return list;
        }

        //===================================================================
        /// <summary>
        /// Overriden to load the FPGA
        /// </summary>
        //===================================================================
        internal override void Initialize()
        {
            //SendMessageDirect("?DEV:FPGACFG");
            //string response = m_driverInterface.ReadStringDirect();

            //if (!response.Contains(PropertyValues.CONFIGURED))
            //{
            //    LoadFPGA();

            //    SendMessageDirect("?DEV:FPGACFG");
            //    response = m_driverInterface.ReadStringDirect();

            //    if (!response.Contains(PropertyValues.CONFIGURED))
            //    {
            //        DaqException dex = ResolveException(ErrorCodes.FpgaNotLoaded);
            //        throw dex;
            //    }
            //}

            base.Initialize();
        }

        public override void LoadFPGA()
        {
            SendMessageDirect("?DEV:FPGACFG");
            string response = m_driverInterface.ReadStringDirect();

            if (!response.Contains(PropertyValues.CONFIGURED))
            {
                LoadTheFPGA();

                SendMessageDirect("?DEV:FPGACFG");
                response = m_driverInterface.ReadStringDirect();

                if (!response.Contains(PropertyValues.CONFIGURED))
                {
                    DaqException dex = ResolveException(ErrorCodes.FpgaNotLoaded);
                    throw dex;
                }
            }
        }

        //================================================================================================
        /// <summary>
        /// Overridden to load the device's FPGA
        /// </summary>
        //================================================================================================
        private void LoadTheFPGA()
        {
            string file = String.Empty;

            SendMessageDirect("DEV:FPGACFG=0xAD");
            SendMessageDirect("?DEV:FPGACFG");

            try
            {
                if (Directory.Exists(LINUX_FPGA_DIR))
                    file = LINUX_FPGA_DIR + '/' + m_fpgaFileName;
                else if (Directory.Exists(MAC_FPGA_DIR))
                    file = MAC_FPGA_DIR + '/' + m_fpgaFileName;

                if (Environment.OSVersion.Platform == PlatformID.WinCE)
                    file = @"\Windows\" + m_fpgaFileName;

                FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read);
                int byteCount = (int)fs.Length;
                byte[] fpgaImage;

                using (BinaryReader br = new BinaryReader(fs))
                {
                    fpgaImage = br.ReadBytes(byteCount);
                }

                // calculate the number of complete blocks to read (e.g. 64 bytes)
                int blockCount = byteCount / Constants.MAX_COMMAND_LENGTH;

                // calculate the remaining bytes to read
                int remainingBytes = byteCount - (blockCount * Constants.MAX_COMMAND_LENGTH);

                byte[] block = new byte[Constants.MAX_COMMAND_LENGTH];

                int sourceIndex = 0;

                for (int i = 0; i < blockCount; i++)
                {
                    Array.Copy(fpgaImage, sourceIndex, block, 0, block.Length);
                    m_driverInterface.LoadFPGA(fpgaDataRequest, block);
                    sourceIndex += block.Length;
                }

                // load remaining bytes
                block = new byte[remainingBytes];
                Array.Copy(fpgaImage, sourceIndex, block, 0, block.Length);
                m_driverInterface.LoadFPGA(fpgaDataRequest, block);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }
        }
    }
}
