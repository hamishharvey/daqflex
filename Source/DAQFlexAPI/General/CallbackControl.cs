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
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace MeasurementComputing.DAQFlex
{
    internal delegate void CallbackDelegate(int availableSamples);

    internal partial class CallbackControl : UserControl
    {
        private DaqDevice m_daqDevice;
        private InputScanCallbackDelegate m_callback;
        private int m_numberOfSamples;
        private CallbackType m_type;
        private bool m_abort;

        //========================================================================================================
        /// <summary>
        /// ctor - for use with setting up a callback for an input scan 
        /// </summary>
        /// <param name="daqDevice">A DaqDevice object</param>
        /// <param name="numberOfSamples">The number of samples to pass to each callback </param>
        /// <param name="callback">A InputScanCallbackDelegate</param>
        //========================================================================================================
        internal CallbackControl(DaqDevice daqDevice, InputScanCallbackDelegate callback, CallbackType type, object callbackData)
        {
            InitializeComponent();
            m_daqDevice = daqDevice;

            if (type == CallbackType.OnDataAvailable)
            {
                try
                {
                    m_numberOfSamples = (int)callbackData;
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.Assert(false, "OnDataAvailable callback data is not the correct data type");
                }
            }

            m_callback = callback;
            m_type = type;
        }

        //================================================================================================================
        /// <summary>
        /// Method invoked by the driver interface when new input scan data is available
        /// This method reads data and passes it to a delegate created in the application
        /// </summary>
        /// <param name="channelCount">The number of channels in the input scan</param>
        //================================================================================================================
        internal void NotifyApplication(int availableSamples)
        {
            ErrorCodes errorCode = m_daqDevice.DriverInterface.ErrorCode;

            if (!m_abort)
            {
                m_callback(errorCode, m_type, availableSamples);
            }
        }

        //================================================================================================================
        /// <summary>
        /// The number of samples per channel that was set when the callback was registered
        /// Used by the driver interface to determine when to invoke NotifyApplication
        /// </summary>
        //================================================================================================================
        internal int NumberOfSamples
        {
            get { return m_numberOfSamples; }
        }

        //================================================================================================================
        /// <summary>
        /// The type of callback event this control is handling
        /// </summary>
        //================================================================================================================
        internal CallbackType CallbackType
        {
            get { return m_type; }
        }

        //================================================================================================================
        /// <summary>
        /// A flag to indicate whether to abort invoking the user callback
        /// </summary>
        //================================================================================================================
        internal bool Abort
        {
            get { return m_abort; }
            set { m_abort = value; }
        }


#if WindowsCE
        internal bool Created
        {
            get {return true;}
        }
#endif
    }
}
