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
    //=================================================================
    /// <summary>
    /// Lightweight class to encapsulate the two types of responses
    /// that a device can return
    /// </summary>
    //=================================================================
    public class DaqResponse
    {
        private string m_response;
        private double m_value;

        //=================================================================
        /// <summary>
        /// ctor - populates the response with the two types of values
        /// </summary>
        /// <param name="response">The text response</param>
        /// <param name="value">The numeric response</param>
        //=================================================================
        internal DaqResponse(string response, double value)
        {
            m_response = response;
            m_value = value;
        }

        //===================================================
        /// <summary>
        /// Overriden to return the text response
        /// </summary>
        /// <returns>The response</returns>
        //===================================================
        public override string ToString()
        {
            return m_response;
        }

        //==================================================================
        /// <summary>
        /// Overriden to return the text response
        /// </summary>
        /// <param name="format">The format for the string</param>
        /// <returns>The response</returns>
        //==================================================================
        public string ToString(string format)
        {
            string response = m_response;

            if (!Double.IsNaN(m_value))
            {
                int equalIndex = response.IndexOf("=");
                string value = response.Substring(equalIndex + 1);

                try
                {
                    double numeric = Convert.ToDouble(value);
                    string lValue = response.Substring(0, equalIndex);
                    string rValue = numeric.ToString(format);
                    response = lValue + "=" + rValue;
                }
                catch (Exception)
                {
                }
            }

            return response;
        }

        //======================================================================
        /// <summary>
        /// Overriden to return the text response
        /// </summary>
        /// <param name="provider">The object to control formatting</param>
        /// <returns>The response</returns>
        //======================================================================
        public string ToString(IFormatProvider provider)
        {
            string response = m_response;

            if (!Double.IsNaN(m_value))
            {
                int equalIndex = response.IndexOf("=");
                string value = response.Substring(equalIndex + 1);

                try
                {
                    double numeric = Convert.ToDouble(value);
                    string lValue = response.Substring(0, equalIndex);
                    string rValue = numeric.ToString(provider);
                    response = lValue + "=" + rValue;
                }
                catch (Exception)
                {
                }
            }

            return response;
        }

        //======================================================================
        /// <summary>
        /// Overriden to return the text response
        /// </summary>
        /// <param name="format">The format for the string</param>
        /// <param name="provider">The object to control formatting</param>
        /// <returns>The response</returns>
        //======================================================================
        public string ToString(string format, IFormatProvider provider)
        {
            string response = m_response;

            if (!Double.IsNaN(m_value))
            {
                int equalIndex = response.IndexOf("=");
                string value = response.Substring(equalIndex + 1);

                try
                {
                    double numeric = Convert.ToDouble(value);
                    string lValue = response.Substring(0, equalIndex);
                    string rValue = numeric.ToString(format, provider);
                    response = lValue + "=" + rValue;
                }
                catch (Exception)
                {
                }
            }

            return response;
        }

        //===================================================
        /// <summary>
        /// Implemented to return the numeric response
        /// </summary>
        /// <returns>The response</returns>
        //===================================================
        public double ToValue()
        {
            return m_value;
        }
    }
}
