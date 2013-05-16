//***************************************************************************************
//
// DAQFlex API Library
//
// Copyright (c) 2011, Measurement Computing Corporation
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
   internal abstract class HidPlatformInterop : PlatformInterop
   {

      internal HidPlatformInterop()
         : base()
      {

      }

      internal HidPlatformInterop(DeviceInfo deviceInfo, CriticalParams criticalParams)
         : base(deviceInfo, criticalParams)
      {
   

      }


      //==================================================================================================================
      /// <summary>
      /// Virtual method for getting a list of DeviceInfos
      /// </summary>
      /// <param name="deviceInfoList">The list of devices</param>
      /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
      //==================================================================================================================
      internal override ErrorCodes GetDevices(Dictionary<int, DeviceInfo> deviceInfoList, DeviceListUsage deviceListUsage)
      {
         return GetHIDDevices(deviceInfoList, deviceListUsage);
      }

      //==================================================================================================================
      /// <summary>
      /// Virtual method for getting a list of DeviceInfos
      /// </summary>
      /// <param name="deviceInfoList">The list of devices</param>
      /// <param name="deviceInfoList">A flag indicating if the device list should be refreshed</param>
      //==================================================================================================================
      internal abstract ErrorCodes GetHIDDevices(Dictionary<int, DeviceInfo> deviceInfoList, DeviceListUsage deviceListUsage);

      //===================================================================================================
      /// <summary>
      /// Overrides abstact method in base class
      /// </summary>
      /// <param name="deviceInfo">A deviceInfo object</param>
      /// <returns>An empty string</returns>
      //===================================================================================================
      internal override string GetDeviceID(DeviceInfo deviceInfo)
      {
         System.Diagnostics.Debug.Assert(false, "GetDeviceID not implemented in HidPlatformInterop");
         return String.Empty;
      }

      //===================================================================================================
      /// <summary>
      /// Overrides abstact method in base class
      /// </summary>
      /// <param name="deviceInfo">A deviceInfo object</param>
      /// <returns>An empty string</returns>
      //===================================================================================================
      internal override string GetSerno(DeviceInfo deviceInfo)
      {
         System.Diagnostics.Debug.Assert(false, "GetSerno not implemented in HidPlatformInterop");
         return String.Empty;
      }

      public abstract int OutReportTimeOut
      {
         get;
         set;
      }

      public abstract int OutReportLength
      {
         get;
      }
      
      public abstract int InReportTimeOut
      {
         get;
         set;
      }

      public abstract int InReportLength
      {
         get ;
      }
      //==============================================================================================
      /// <summary>
      /// Virtual method for a USB Bulk IN request
      /// </summary>
      /// <param name="buffer">The buffer to receive the data</param>
      /// <param name="bytesReceived">The number of actual bytes received</param>
      /// <returns>The result</returns>
      //==============================================================================================
      internal abstract unsafe ErrorCodes HIDInReport(byte [] buffer, ref int bytesReceived);

      //===================================================================================
      /// <summary>
      /// Virtual method for a USB Bulk OUT request
      /// </summary>
      /// <param name="buffer">The buffer containing the data to send</param>
      /// <param name="count">The number of samples to send</param>
      /// <returns>The result</returns>
      //===================================================================================
      internal abstract unsafe ErrorCodes HIDOutReport(byte [] buffer, ref int bytesTransferred);

}
}
