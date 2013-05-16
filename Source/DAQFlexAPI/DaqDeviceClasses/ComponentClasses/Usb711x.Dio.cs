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
   class Usb711xDio : DioComponent
   {
   
      //=================================================================================================================
      /// <summary>
      /// ctor 
      /// </summary>
      /// <param name="daqDevice">The DaqDevice object that creates this component</param>
      /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
      //=================================================================================================================
      internal Usb711xDio(DaqDevice daqDevice, DeviceInfo deviceInfo, int maxPorts)
         : base(daqDevice, deviceInfo, maxPorts)
      {
         
      }

      

      //===========================================================================================
      /// <summary>
      /// Overridden to get the supported messages specific to this Ai component
      /// </summary>
      /// <returns>A list of supported messages</returns>
      //===========================================================================================
      internal override List<string> GetMessages(string daqComponent)
      {
         List<string> messages = base.GetMessages(daqComponent);//new List<string>();

         messages.Remove("?DIO{*}:LATCH");
         messages.Remove("?DIO{*/*}:LATCH");

         messages.Add("?DIO{0}:LATCH");
         messages.Add("?DIO{0/*}:LATCH");

         //port 0 is for the relays, port 1 is for the inputs...
         string filter = m_daqDevice.GetDevCapsString("DIO{1}:FILTER", false);

         if (filter != PropertyValues.NOT_SUPPORTED)
         {
            if (filter.Contains(DevCapValues.WRITE))
            {
               messages.Add("DIO{1}:FILTER=*");
               messages.Add("DIO{1/*}:FILTER=*");
            }

            messages.Add("?DIO{1}:FILTER");
            messages.Add("?DIO{1/*}:FILTER");
         }

         string filttime = m_daqDevice.GetDevCapsString("DIO{1}:FILTTIME", false);

         if (filttime != PropertyValues.NOT_SUPPORTED)
         {
            if (filttime.Contains(DevCapValues.WRITE))
            {
               messages.Add("DIO{1}:FILTTIME=*");
              // messages.Add("DIO{1/*}:FILTTIME=*");
            }

            messages.Add("?DIO{1}:FILTTIME");
            //messages.Add("?DIO{1/*}:FILTTIME");
         }

         return messages;
      }

}
}
