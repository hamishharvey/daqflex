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
    class FixedModeAiComponent :AiComponent
    {
        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal FixedModeAiComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
            : base(daqDevice, deviceInfo, maxChannels)
        {
        }

        //=================================================================================================================
        /// <summary>
        /// Overriden for fixed mode channels
        /// </summary>
        //=================================================================================================================
        internal override void InitializeChannelModes()
        {
            m_channelModes = new string[m_maxChannels];

            string chMode = m_daqDevice.SendMessage("?AI:CHMODE").ToString();

            for (int i = 0; i < m_maxChannels; i++)
            {
                m_channelModes[i] = MessageTranslator.GetPropertyValue(chMode);
            }
        }

        //========================================================================================================================
        /// <summary>
        /// Overriden to get the valid channels
        /// </summary>
        /// <param name="includeMode">A flag to indicate if the channel mode should be included with the channel number</param>
        /// <returns>The valid channels</returns>
        //========================================================================================================================
        internal override string GetValidChannels(bool includeMode)
        {
            string validChannels = String.Empty;

            for (int i = 0; i < m_maxChannels; i++)
            {
                validChannels += i.ToString();

                if (includeMode)
                    validChannels += String.Format(" ({0})", m_channelModes[i]);

                if (i < (m_maxChannels - 1))
                    validChannels += PlatformInterop.LocalListSeparator;
            }

            if (validChannels.EndsWith(","))
                validChannels = validChannels.Remove(validChannels.LastIndexOf(","), 1);

            return validChannels;
        }
    }
}
