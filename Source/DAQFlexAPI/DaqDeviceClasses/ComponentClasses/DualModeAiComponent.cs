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
    class DualModeAiComponent : AiComponent
    {
        //=================================================================================================================
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="daqDevice">The DaqDevice object that creates this component</param>
        /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
        //=================================================================================================================
        internal DualModeAiComponent(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxChannels)
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
                if (i < (m_maxChannels / 2))
                {
                    m_channelModes[i] = MessageTranslator.GetPropertyValue(chMode);
                }
                else
                {
                    m_channelModes[i] = PropertyValues.SE;
                }
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
            int channelCount;
            string validChannels = String.Empty;

            if (m_channelModes[0] == PropertyValues.SE)
                channelCount = m_maxChannels;
            else
                channelCount = m_maxChannels / 2;

            for (int i = 0; i < channelCount; i++)
            {
                validChannels += i.ToString();

                if (includeMode)
                    validChannels += String.Format(" ({0})", m_channelModes[i]);

                if (i < (channelCount - 1))
                    validChannels += PlatformInterop.LocalListSeparator;
            }

            if (validChannels.EndsWith(","))
                validChannels = validChannels.Remove(validChannels.LastIndexOf(","), 1);

            return validChannels;
        }
    }
}
