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
   class Usb1208FSPlusAi : Usb7204Ai
   {

      //=================================================================================================================
      /// <summary>
      /// ctor 
      /// </summary>
      /// <param name="daqDevice">The DaqDevice object that creates this component</param>
      /// <param name="deviceInfo">The DeviceInfo oject passed down to the driver interface</param>
      //=================================================================================================================
      internal Usb1208FSPlusAi(DaqDevice daqDevice, DeviceInfo deviceInfo)
         : base(daqDevice, deviceInfo)
      {
     
      }

      //=================================================================================================================
      /// <summary>
      /// Overriden to set the 0 length packet flag
      /// </summary>
      //=================================================================================================================
      internal override void Initialize()
      {
         m_daqDevice.SendMessage(Messages.AISCAN_QUEUE_DISABLE);
         
         base.Initialize();

         m_daqDevice.CriticalParams.Requires0LengthPacketForSingleIO = true;
      }

      internal override List<string> GetMessages(string daqComponent)
      {
         List<string> messages = base.GetMessages(daqComponent);
         if (daqComponent == DaqComponents.AI)
            {
               messages.Add("?AI:RES");
               messages.Add("?AI:RANGE"); 
               messages.Add("AI:RANGE=*");
            }
          else if (daqComponent == DaqComponents.AISCAN)
            {
               messages.Remove("AISCAN:RANGE{*/*}=*");
            }
          else if (daqComponent == DaqComponents.AITRIG)
            {
            }

         else if (daqComponent == DaqComponents.AIQUEUE)
         {
            messages.Add("AIQUEUE:CLEAR");
            messages.Add("AIQUEUE{*}:CHAN=*");
            messages.Add("AIQUEUE{*}:RANGE=*");
            messages.Add("?AIQUEUE:COUNT");
            messages.Add("?AIQUEUE{*}:CHAN");
            messages.Add("?AIQUEUE{*}:RANGE");
         }
            
         return messages;
      }

      internal override void BeginInputScan()
      {
         base.BeginInputScan();
         m_daqDevice.CriticalParams.NumberOfSamplesForSingleIO = 1;
      }

      internal override ErrorCodes SetQueueElementRange(int element, int channel, string range)
      {
         string msg = Messages.AIQUEUE_RANGE;
         msg = msg.Replace("*", element.ToString());
         msg = msg.Replace("#", range);

         return m_daqDevice.SendMessageDirect(msg);
      }
     
   }
}
