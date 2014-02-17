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
using System.Globalization;

namespace MeasurementComputing.DAQFlex
{
    public class PlatformParser
    {
        public static bool TryParse(string valueToParse, out int result)
        {
#if WindowsCE
            try
            {
                result = Int32.Parse(valueToParse);
                return true;
            }
            catch (Exception)
            {
                result = 0;
                return false;
            }
#else
            return Int32.TryParse(valueToParse, out result);
#endif
        }

        public static bool TryParse(string valueToParse, out double result)
        {
#if WindowsCE
            try
            {
                result = Double.Parse(valueToParse);
                return true;
            }
            catch (Exception)
            {
                result = 0.0;
                return false;
            }
#else
            //return Double.TryParse(valueToParse, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out result);
            return Double.TryParse(valueToParse, out result);
#endif
        }

        public static bool TryParse(string valueToParse, out uint result)
        {
#if WindowsCE
            try
            {
                result = UInt32.Parse(valueToParse);
                return true;
            }
            catch (Exception)
            {
                result = 0;
                return false;
            }
#else
            return UInt32.TryParse(valueToParse, out result);
#endif
        }

    }
}
