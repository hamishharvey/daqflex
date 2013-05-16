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
using System.Threading;

namespace MeasurementComputing.DAQFlex
{
    class DebugLogger
    {
        private static object m_writeLock = new Object();

        internal static System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
        private static List<string> DebugList = new List<string>();

        internal static void WriteLine(String stringFormat, params object[] args)
        {
#if DEBUG
            Monitor.Enter(m_writeLock);
            
            DebugList.Add(StopWatch.ElapsedMilliseconds.ToString() + ": " + String.Format(stringFormat, args));

            Monitor.Exit(m_writeLock);
#endif
        }

        internal static void DumpDebugInfo()
        {
#if DEBUG
            Monitor.Enter(m_writeLock);

            foreach (string debugInfo in DebugList)
            {
                System.Diagnostics.Debug.WriteLine(debugInfo);
            }

            Monitor.Exit(m_writeLock);
#endif
        }

        internal static void ClearDebugList()
        {
            Monitor.Enter(m_writeLock);

            DebugList.Clear();

            Monitor.Exit(m_writeLock);
        }
    }
}
