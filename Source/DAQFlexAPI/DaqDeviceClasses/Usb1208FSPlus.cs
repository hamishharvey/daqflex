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

namespace MeasurementComputing.DAQFlex
{
   internal class Usb1208FSPlus : DaqDevice
   {
      //===========================================================================
      /// <summary>
      /// Sub-class of DaqDevice that provides methods specific to the USB-7202
      /// </summary>
      /// <param name="deviceInfo">A device info object</param>
      //===========================================================================
      internal Usb1208FSPlus(DeviceInfo deviceInfo)
         : base(deviceInfo, 0x040)
      {
         Ai = new Usb1208FSPlusAi(this, deviceInfo);
         Ao = new Usb1208FSPlusAo(this, deviceInfo);
         Dio = new DioComponent(this, deviceInfo, 2);
         Ctr = new VirtualSSEventCounter(this, deviceInfo, 1);

         m_memReadCmd = 0x32;
         m_memWriteCmd = 0x32;

         m_memLockAddr = 0x000;
         m_memUnlockCode = 0x0000;
         m_memLockCode = 0x0000;

         m_eepromAssistant = new EepromAssistantIV(m_driverInterface);

         // 1/16/2012: version 1.0
         m_defaultDevCapsImage = new byte[] 
                {   0x1F,0x8B,0x08,0x00,0x00,0x00,0x00,0x00,0x04,0x00,0xED,0xBD,0x07,0x60,0x1C,0x49,
                    0x96,0x25,0x26,0x2F,0x6D,0xCA,0x7B,0x7F,0x4A,0xF5,0x4A,0xD7,0xE0,0x74,0xA1,0x08,
                    0x80,0x60,0x13,0x24,0xD8,0x90,0x40,0x10,0xEC,0xC1,0x88,0xCD,0xE6,0x92,0xEC,0x1D,
                    0x69,0x47,0x23,0x29,0xAB,0x2A,0x81,0xCA,0x65,0x56,0x65,0x5D,0x66,0x16,0x40,0xCC,
                    0xED,0x9D,0xBC,0xF7,0xDE,0x7B,0xEF,0xBD,0xF7,0xDE,0x7B,0xEF,0xBD,0xF7,0xBA,0x3B,
                    0x9D,0x4E,0x27,0xF7,0xDF,0xFF,0x3F,0x5C,0x66,0x64,0x01,0x6C,0xF6,0xCE,0x4A,0xDA,
                    0xC9,0x9E,0x21,0x80,0xAA,0xC8,0x1F,0x3F,0x7E,0x7C,0x1F,0x3F,0x22,0xF6,0xEE,0xED,
                    0xFD,0xEE,0xBB,0xE3,0x9D,0xDF,0x7D,0xF7,0xD1,0xCE,0xA3,0x6F,0x3D,0xFA,0xBD,0x1F,
                    0xED,0x3D,0xA2,0xDF,0x46,0xBB,0x8F,0x76,0x47,0xF7,0xE8,0x33,0xFC,0xF7,0x7B,0xE3,
                    0x93,0x47,0x07,0xFC,0xD7,0x9E,0xFE,0xB5,0x4F,0x7F,0xDD,0xA3,0xDF,0xBE,0xC5,0x2D,
                    0xEE,0xF1,0x5F,0x7B,0xF4,0x17,0xDE,0xDE,0x1B,0xDD,0x1B,0xED,0x8F,0xEE,0x8F,0x3E,
                    0x1D,0x3D,0x18,0x1D,0x8C,0x1E,0xFE,0xEE,0x78,0xCB,0x40,0xD9,0xDF,0x79,0x78,0x9F,
                    0x3F,0xB0,0x80,0xE4,0x83,0x7D,0xEE,0x1B,0x1F,0xEC,0xEE,0x00,0x97,0xFB,0x16,0xF6,
                    0xFE,0x7D,0xFE,0x73,0xCF,0xFF,0xF3,0x53,0x6D,0x4D,0x7F,0x02,0xAD,0xFD,0x07,0xF6,
                    0xEF,0xDD,0xDD,0x4F,0x7F,0xF7,0xBD,0x47,0x0F,0x2D,0xB4,0xFB,0x3B,0xF4,0xD0,0x27,
                    0xBB,0xBB,0xF6,0xA3,0x9D,0xF1,0xCE,0xEE,0x3E,0x3E,0xDA,0xE9,0xB7,0xDA,0x73,0x80,
                    0x1F,0xE2,0x6F,0x03,0x98,0xF1,0x04,0xE4,0x7D,0x41,0x14,0x03,0xDA,0xDD,0x79,0x38,
                    0xDA,0xDD,0xE5,0xD7,0x1E,0x7A,0x54,0xE2,0x3F,0x1D,0x99,0xE8,0xCF,0x03,0x87,0xDD,
                    0xFE,0x3D,0xFA,0x60,0xCF,0x11,0xFA,0xFE,0xBD,0xD1,0x7D,0xB4,0xD9,0x37,0x23,0xDA,
                    0x79,0xB4,0x47,0x7F,0xDE,0x37,0xAF,0x08,0x39,0xE8,0x95,0x5D,0xF7,0xCA,0xC3,0xD1,
                    0xA7,0xFC,0xD1,0x9E,0xFD,0xE8,0xE0,0xFE,0xE8,0x00,0xC8,0xDD,0x77,0xE8,0x7E,0xBA,
                    0xFF,0xBB,0xD3,0x84,0xDC,0xB3,0x3D,0x7F,0xBA,0x87,0xBF,0x1D,0xF2,0x9F,0xEE,0x8F,
                    0x3E,0xBD,0x3F,0x7A,0xB8,0x37,0x7A,0x78,0x8F,0xBE,0xB8,0xD7,0x85,0xB5,0xFF,0xC8,
                    0xD1,0x6B,0x8F,0xFE,0x32,0x74,0xD1,0xF9,0xDA,0x7F,0x04,0xC8,0x32,0x21,0x7B,0x68,
                    0x1D,0x4E,0x1F,0xB5,0xBF,0x6F,0xBF,0xDF,0x47,0xF3,0xCE,0x0C,0xDD,0xEF,0xCE,0x07,
                    0x3E,0xE9,0x4E,0xC7,0x7D,0x6F,0x1A,0x77,0xF1,0x09,0x3E,0x0A,0x67,0xE8,0x7E,0x48,
                    0xB8,0xFB,0xBD,0x09,0xBB,0xDF,0xA1,0xC9,0xFD,0x47,0xF7,0x1C,0x4D,0x0E,0xEE,0xFF,
                    0xEE,0x0F,0x82,0x71,0x3E,0x78,0xB4,0x07,0xC2,0xEF,0x6C,0x0B,0xC7,0x7E,0xFA,0xE9,
                    0xE8,0xD3,0x07,0xF4,0xE1,0xFE,0x8E,0xF7,0xE1,0xC3,0xFD,0xD1,0x43,0xBC,0xB7,0xA7,
                    0x9F,0xF1,0x9B,0xF7,0xF1,0x49,0x48,0x83,0x87,0x1E,0xE4,0x5D,0xFA,0x6B,0x0F,0x88,
                    0xEE,0x70,0xBF,0x0F,0xF0,0xED,0x9E,0xFE,0x49,0x88,0xEE,0x3D,0xDC,0x7F,0xF8,0xE9,
                    0x83,0x3D,0x02,0xFB,0xF0,0xD1,0xFE,0xAE,0xFD,0x1C,0xAD,0xF6,0x5D,0x33,0x7E,0xE9,
                    0x81,0x05,0x72,0xF0,0x00,0x5F,0x07,0x5D,0xEE,0xDE,0xDF,0xBD,0xB7,0x7B,0xFF,0xFE,
                    0xFD,0x7B,0xFB,0xFF,0x0F,0xF2,0x71,0xE1,0x9A,0xD4,0x03,0x00,0x00 };
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
         //System.Diagnostics.Debug.Assert(!capsKey.Contains(DevCapNames.QUEUELEN));
         
         if (capsKey.Contains(DaqComponents.AI) && (capsKey.Contains(DevCapNames.CHANNELS) ||
                                                      capsKey.Contains(DevCapNames.MAXCOUNT) ||
                                                      capsKey.Contains(DevCapNames.RANGES)||
                                                      capsKey.Contains(DevCapNames.QUEUELEN)))
         {
            //string config = MessageTranslator.GetPropertyValue(SendMessage("?AI:CHMODE").ToString());
            string config;
            string response;
            string capsName;


            if (capsKey.Contains(Constants.VALUE_RESOLVER.ToString()))
            {
               capsName = capsKey;
            }
            else
            {
               string msg = Messages.AI_CHMODE_QUERY;
               SendMessageDirect(msg);
               response = m_driverInterface.ReadStringDirect();
               config = MessageTranslator.GetPropertyValue(response);
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



      ////====================================================================
      ///// <summary>
      ///// Method to shut down a device when the application exits
      ///// </summary>
      ////====================================================================
      //protected override void ShutDownDevice()
      //{
      //    if (!m_deviceReleased)
      //    {
      //        try
      //        {
      //            SendMessage("AISCAN:STOP");
      //            SendMessage("CTR{0}:STOP");
      //        }
      //        catch (Exception)
      //        {
      //            // The devic may have been unplugged
      //        }
      //    }
      //}

   }
}