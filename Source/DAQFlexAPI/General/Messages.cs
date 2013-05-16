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
    internal class Messages
    {
        // The following wild cards will be used 
        //  * = channel, port
        //  $ = bit
        //  # = value
        //
        internal const string DEV_RESET_DEFAULT = "DEV:RESET/DEFAULT";
        internal const string DEV_SERNO_QUERY = "?DEV:MFGSER";
        internal const string DEV_ID = "DEV:ID=#";
        internal const string DEV_ID_QUERY = "?DEV:ID";
        internal const string DEV_FLASH_LED = "DEV:FLASHLED/#";
        internal const string DEV_FWV = "?DEV:FWV";
        internal const string DEV_MFGCAL_QUERY = "?DEV:MFGCAL";
        internal const string DEV_FPGAV_QUERY = "?DEV:FPGAV";
        internal const string DEV_DATA_TYPE_ENABLE = "DEV:DATATYPE=ENABLE";
        internal const string DEV_DATA_TYPE_DISABLE = "DEV:DATATYPE=DISABLE";

        internal const string AI_CHAN_QUERY = "?AI";
        internal const string AI_CHMODE = "AI:CHMODE=#";
        internal const string AI_CH_CHMODE = "AI{*}:CHMODE=#";
        internal const string AI_CHMODE_QUERY = "?AI:CHMODE";
        internal const string AI_CH_CHMODE_QUERY = "?AI{*}:CHMODE";
        internal const string AI_RANGE = "AI:RANGE=#";
        internal const string AI_CH_RANGE = "AI{*}:RANGE=#";
        internal const string AI_RANGE_QUERY = "?AI:RANGE";
        internal const string AI_CH_RANGE_QUERY = "?AI{*}:RANGE";
        internal const string AI_CH_SLOPE_QUERY = "?AI{*}:SLOPE";
        internal const string AI_CH_OFFSET_QUERY = "?AI{*}:OFFSET";
        internal const string AI_CH_VALUE_QUERY = "?AI{*}:VALUE";
        internal const string AI_DATARATE = "AI:DATARATE=#";
        internal const string AI_DATARATE_QUERY = "?AI:DATARATE";
        internal const string AI_CH_DATARATE = "AI{*}:DATARATE=#";
        internal const string AI_CH_DATARATE_QUERY = "?AI{*}:DATARATE";
        internal const string AI_CJC_QUERY = "?AI{*}:CJC";
        internal const string AI_RES_QUERY = "?AI:RES";
        internal const string AI_CAL_ENABLE = "AI:CAL=ENABLE";
        internal const string AI_CAL_DISABLE = "AI:CAL=DISABLE";
        internal const string AI_CAL_QUERY = "?AI:CAL";
        internal const string AI_SCALE_ENABLE = "AI:SCALE=ENABLE";
        internal const string AI_SCALE_DISABLE = "AI:SCALE=DISABLE";
        internal const string AI_SCALE_QUERY = "?AI:SCALE";
        internal const string AI_ADCAL_START = "AI:ADCAL/START";
        internal const string AI_ADCAL_STATUS_QUERY = "?AI:ADCAL/STATUS";

        internal const string AISCAN_LOWCHAN = "AISCAN:LOWCHAN=#";
        internal const string AISCAN_LOWCHAN_QUERY = "?AISCAN:LOWCHAN";
        internal const string AISCAN_HIGHCHAN = "AISCAN:HIGHCHAN=#";
        internal const string AISCAN_HIGHCHAN_QUERY = "?AISCAN:HIGHCHAN";
        internal const string AISCAN_RATE = "AISCAN:RATE=#";
        internal const string AISCAN_RATE_QUERY = "?AISCAN:RATE";
        internal const string AISCAN_SAMPLES = "AISCAN:SAMPLES=#";
        internal const string AISCAN_SAMPLES_QUERY = "?AISCAN:SAMPLES";
        internal const string AISCAN_RANGE = "AISCAN:RANGE=#";
        internal const string AISCAN_RANGE_QUERY = "?AISCAN:RANGE";
        internal const string AISCAN_XFRMODE = "AISCAN:XFRMODE=#";
        internal const string AISCAN_XFRMODE_QUERY = "?AISCAN:XFRMODE";
        internal const string AISCAN_BURSTMODE_ENABLE = "AISCAN:BURSTMODE=ENABLE";
        internal const string AISCAN_BURSTMODE_DISABLE = "AISCAN:BURSTMODE=DISABLE";
        internal const string AISCAN_BURSTMODE_QUERY = "?AISCAN:BURSTMODE";
        internal const string AISCAN_TRIG_ENABLE = "AISCAN:TRIG=ENALBE";
        internal const string AISCAN_TRIG_DISABLE = "AISCAN:TRIG=DISABLE";
        internal const string AISCAN_TRIG_QUERY = "?AISCAN:TRIG";
        internal const string AISCAN_QUEUE_ENABLE = "AISCAN:QUEUE=ENABLE";
        internal const string AISCAN_QUEUE_DISABLE = "AISCAN:QUEUE=DISABLE";
        internal const string AISCAN_QUEUE_QUERY = "?AISCAN:QUEUE";
        internal const string AISCAN_DEBUG_ENABLE = "AISCAN:DEBUG=ENALBE";
        internal const string AISCAN_DEBUG_DISABLE = "AISCAN:DEBUG=DISABLE";
        internal const string AISCAN_DEBUG_QUERY = "?AISCAN:DEBUG";
        internal const string AISCAN_CAL_ENABLE = "AISCAN:CAL=ENABLE";
        internal const string AISCAN_CAL_DISABLE = "AISCAN:CAL=DISABLE";
        internal const string AISCAN_CAL_QUERY = "?AISCAN:CAL";
        internal const string AISCAN_SCALE_ENABLE = "AISCAN:SCALE=ENABLE";
        internal const string AISCAN_SCALE_DISABLE = "AISCAN:SCALE=DISABLE";
        internal const string AISCAN_SCALE_QUERY = "?AISCAN:SCALE";
        internal const string AISCAN_STALL_ENABLE = "AISCAN:STALL=ENABLE";
        internal const string AISCAN_STALL_DISABLE = "AISCAN:STALL=DISABLE";
        internal const string AISCAN_STALL_QUERY = "?AISCAN:STALL";
        internal const string AISCAN_EXTPACER_ENABLE = "AISCAN:EXTPACER=ENABLE";
        internal const string AISCAN_EXTPACER_DISABLE = "AISCAN:EXTPACER=DISABLE";
        internal const string AISCAN_EXTPACER_ENMASTER = "AISCAN:EXTPACER=ENABLE/MASTER";
        internal const string AISCAN_EXTPACER_ENSLAVE = "AISCAN:EXTPACER=ENABLE/SLAVE";
        internal const string AISCAN_EXTPACER_DISSLAVE = "AISCAN:EXTPACER=DISABLE/SLAVE";
        internal const string AISCAN_EXTPACER_DISMASTER = "AISCAN:EXTPACER=DISABLE/MASTER";
        internal const string AISCAN_EXTPACER_QUERY = "?AISCAN:EXTPACER";
        internal const string AISCAN_STATUS_QUERY = "?AISCAN:STATUS";
        internal const string AISCAN_COUNT_QUERY = "?AISCAN:COUNT";
        internal const string AISCAN_INDEX_QUERY = "?AISCAN:INDEX";
        internal const string AISCAN_START = "AISCAN:START";
        internal const string AISCAN_STOP = "AISCAN:STOP";
        internal const string AISCAN_RESET = "AISCAN:RESET";
        internal const string AISCAN_TEMPUNITS = "AISCAN:TEMPUNITS=#";
        internal const string AISCAN_TEMPUNITS_QUERY = "?AISCAN:TEMPUNITS";
        internal const string AISCAN_BLOCK_SIZE = "AISCAN:BLOCKSIZE=#";
        internal const string AISCAN_BLOCK_SIZE_QUERY = "?AISCAN:BLOCKSIZE";

        internal const string AIQUEUE_CLEAR = "AIQUEUE:CLEAR";
        internal const string AIQUEUE_CHAN = "AIQUEUE{*}:CHAN=#";
        internal const string AIQUEUE_CHAN_QUERY = "?AIQUEUE{*}:CHAN";
        internal const string AIQUEUE_RANGE = "AIQUEUE{*}:RANGE=#";
        internal const string AIQUEUE_RANGE_QUERY = "?AIQUEUE{*}:RANGE";
        internal const string AIQUEUE_CHMODE = "AIQUEUE{*}:CHMODE=#";
        internal const string AIQUEUE_CHMODE_QUERY = "?AIQUEUE{*}:CHMODE";
        internal const string AIQUEUE_DATARATE = "AIQUEUE{*}:DATARATE=#";
        internal const string AIQUEUE_DATARATE_QUERY = "?AIQUEUE{*}:DATARATE";
        internal const string AIQUEUE_COUNT_QUERY = "?AIQUEUE:COUNT";

        internal const string AITRIG_TYPE_EDGE_RISING = "AITRIG:TYPE=EDGE/RISING";
        internal const string AITRIG_TYPE_EDGE_FALLING = "AITRIG:TYPE=EDGE/FALLING";
        internal const string AITRIG_TYPE_LEVEL_HIGH = "AITRIG:TYPE=LEVEL/HIGH";
        internal const string AITRIG_TYPE_LEVEL_LOW = "AITRIG:TYPE=LEVEL/LOW";
        internal const string AITRIG_TYPE_QUERY = "?AITRIG:TYPE";
        internal const string AITRIG_LEVEL = "AITRIG:LEVEL=#";
        internal const string AITRIG_LEVEL_QUERY = "?AITRIG:LEVEL";
        internal const string AITRIG_SRC_HW_START_DIG = "AITRIG:SRC=HWSTART/DIG";
        internal const string AITRIG_SRC_SW_START_ANLG = "AITRIG:SRC=SWSTART/AI{*}";
        internal const string AITRIG_SRC_QUERY = "?AITRIG:SRC";
        internal const string AITRIG_SRC_LEVEL = "AITRIG:LEVEL=#";
        internal const string AITRIG_REARM_ENABLE = "AITRIG:REARM=ENABLE";
        internal const string AITRIG_REARM_DISABLE = "AITRIG:REARM=DISABLE";
        internal const string AITRIG_REARM_QUERY = "?AITRIG:REARM";

        internal const string AICAL_LOCK = "AICAL:LOCK";
        internal const string AICAL_UNLOCK = "AICAL:UNLOCK";
        internal const string AICAL_REF = "AICAL:REF=#";
        internal const string AICAL_REF_QUERY = "?AICAL:REF";
        internal const string AICAL_REFVAL_QUERY = "?AICAL:REFVAL";
        internal const string AICAL_CH_SLOPE = "AICAL{*}:SLOPE=#";
        internal const string AICAL_CH_SLOPE_QUERY = "?AICAL{*}:SLOPE";
        internal const string AICAL_CH_OFFSET = "AICAL{*}:OFFSET=#";
        internal const string AICAL_CH_OFFSE_QUERY = "?AICAL{*}:OFFSET";
        internal const string AICAL_CH_SLOPE_HEX = "AICAL{*}:SLOPE/HEX=#";
        internal const string AICAL_CH_SLOPE_HEX_QUERY = "?AICAL{*}:SLOPE/HEX";
        internal const string AICAL_CH_OFFSET_HEX = "AICAL{*}:OFFSET/HEX=#";
        internal const string AICAL_CH_OFFSET_HEX_QUERY = "?AICAL{*}:OFFSET/HEX";
        internal const string AICAL_RANGE = "AICAL:RANGE=#";
        internal const string AICAL_MODE = "AICAL:MODE=#";
        internal const string AICAL_VALUE_QUERY = "?AICAL{*}:VALUE";

        internal const string AOCAL_LOCK = "AOCAL:LOCK";
        internal const string AOCAL_UNLOCK = "AOCAL:UNLOCK";
        internal const string AOCAL_CH_SLOPE = "AOCAL{*}:SLOPE=#";
        internal const string AOCAL_CH_OFFSET = "AOCAL{*}:OFFSET=#";
        internal const string AOCAL_CH_SLOPE_HEX = "AOCAL{*}:SLOPE/HEX=#";
        internal const string AOCAL_CH_OFFSET_HEX = "AOCAL{*}:OFFSET/HEX=#";
        internal const string AOCAL_RES_QUERY = "?AOCAL:RES";
        internal const string AOCAL_AIRES_QUERY = "?AOCAL:AIRES";
        internal const string AOCAL_CH_VALUE = "AOCAL{*}:VALUE=#";
        internal const string AOCAL_CH_VALUE_QUERY = "?AOCAL{*}:VALUE";
        internal const string AOCAL_CH_AIVALUE = "AOCAL{*}:AIVALUE=#";
        internal const string AOCAL_CH_AIVALUE_QUERY = "?AOCAL{*}:AIVALUE";
        internal const string AOCAL_CH_AIRANGE = "AOCAL:AIRANGE{*}=#";
        internal const string AOCAL_CH_AIRANGE_QUERY = "?AOCAL:AIRANGE{*}";
        internal const string AOCAL_CH_AISLOPE = "AOCAL{*}:AISLOPE=#";
        internal const string AOCAL_CH_AISLOPE_QUERY = "?AOCAL{*}:AISLOPE";
        internal const string AOCAL_CH_AIOFFSET = "AOCAL{*}:AIOFFSET=#";
        internal const string AOCAL_CH_AIOFFSET_QUERY = "?AOCAL{*}:AIOFFSET";
        internal const string AOCAL_CH_AISLOPE_HEX = "AOCAL{*}:AISLOPE/HEX=#";
        internal const string AOCAL_CH_AISLOPE_HEX_QUERY = "?AOCAL{*}:AISLOPE/HEX";
        internal const string AOCAL_CH_AIOFFSET_HEX = "AOCAL{*}:AIOFFSET/HEX=#";
        internal const string AOCAL_CH_AIOFFSET_HEX_QUERY = "?AOCAL{*}:AIOFFSET/HEX";

        internal const string AO_CHAN_QUERY = "?AO";
        internal const string AO_RANGE = "AO:RANGE=#";
        internal const string AO_CH_RANGE = "AO{*}:RANGE=#";
        internal const string AO_RANGE_QUERY = "?AO:RANGE";
        internal const string AO_CH_RANGE_QUERY = "?AO{*}:RANGE";
        internal const string AO_CH_SLOPE_QUERY = "?AO{*}:SLOPE";
        internal const string AO_CH_OFFSET_QUERY = "?AO{*}:OFFSET";
        internal const string AO_CH_VALUE = "AO{*}:VALUE=#";
        internal const string AO_CH_VALUE_QUERY = "?AO{*}:VALUE";
        internal const string AO_CAL_ENABLE = "AO:CAL=ENABLE";
        internal const string AO_CAL_DISABLE = "AO:CAL=DISABLE";
        internal const string AO_CAL_QUERY = "?AO:CAL";
        internal const string AO_SCALE_ENABLE = "AO:SCALE=ENABLE";
        internal const string AO_SCALE_DISABLE = "AO:SCALE=DISABLE";
        internal const string AO_SCALE_QUERY = "?AO:SCALE";

        internal const string AOSCAN_LOWCHAN = "AOSCAN:LOWCHAN=#";
        internal const string AOSCAN_LOWCHAN_QUERY = "?AOSCAN:LOWCHAN";
        internal const string AOSCAN_HIGHCHAN = "AOSCAN:HIGHCHAN=#";
        internal const string AOSCAN_HIGHCHAN_QUERY = "?AOSCAN:HIGHCHAN";
        internal const string AOSCAN_RATE = "AOSCAN:RATE=#";
        internal const string AOSCAN_RATE_QUERY = "?AOSCAN:RATE";
        internal const string AOSCAN_SAMPLES = "AOSCAN:SAMPLES=#";
        internal const string AOSCAN_SAMPLES_QUERY = "?AOSCAN:SAMPLES";
        internal const string AOSCAN_RANGE = "AOSCAN:RANGE=#";
        internal const string AOSCAN_RANGE_QUERY = "?AOSCAN:RANGE";
        internal const string AOSCAN_TRIG_ENABLE = "AOSCAN:TRIG=ENALBE";
        internal const string AOSCAN_TRIG_DISABLE = "AOSCAN:TRIG=DISABLE";
        internal const string AOSCAN_TRIG_QUERY = "?AOSCAN:TRIG";
        internal const string AOSCAN_CAL_ENABLE = "AOSCAN:SCALE=ENABLE";
        internal const string AOSCAN_CAL_DISABLE = "AOSCAN:SCALE=DISABLE";
        internal const string AOSCAN_CAL_QUERY = "?AOSCAN:CAL";
        internal const string AOSCAN_SCALE_ENABLE = "AOSCAN:SCALE=ENABLE";
        internal const string AOSCAN_SCALE_DISABLE = "AOSCAN:SCALE=DISABLE";
        internal const string AOSCAN_SCALE_QUERY = "?AOSCAN:SCALE";
        internal const string AOSCAN_STALL_ENABLE = "AOSCAN:STALL=ENABLE";
        internal const string AOSCAN_STALL_DISABLE = "AOSCAN:STALL=DISABLE";
        internal const string AOSCAN_STALL_QUERY = "?AOSCAN:STALL";
        internal const string AOSCAN_EXTPACER_ENABLE = "AOSCAN:EXTPACER=ENABLE";
        internal const string AOSCAN_EXTPACER_DISABLE = "AOSCAN:EXTPACER=DISABLE";
        internal const string AOSCAN_STATUS_QUERY = "?AOSCAN:STATUS";
        internal const string AOSCAN_COUNT_QUERY = "?AOSCAN:COUNT";
        internal const string AOSCAN_INDEX_QUERY = "?AOSCAN:INDEX";
        internal const string AOSCAN_START = "AOSCAN:START";
        internal const string AOSCAN_STOP = "AOSCAN:STOP";
        internal const string AOSCAN_RESET = "AOSCAN:RESET";

        internal const string DIO_CHAN_QUERY = "?DIO";
        internal const string DIO_PORT_DIR_IN = "DIO{*}:DIR=IN";
        internal const string DIO_PORT_DIR_OUT = "DIO{*}:DIR=OUT";
        internal const string DIO_PORT_DIR_QUERY = "?DIO{*}:DIR";
        internal const string DIO_BIT_DIR_IN = "DIO{*/$}=IN";
        internal const string DIO_BIT_DIR_OUT = "DIO{*/$}=OUT";
        internal const string DIO_BIT_DIR_QUERY = "?DIO{*/$}:DIR";

        internal const string DIO_PORT_VALUE = "DIO{*}:VALUE=#";
        internal const string DIO_PORT_VALUE_QUERY = "?DIO{*}:VALUE";
        internal const string DIO_BIT_VALUE = "DIO{*/$}:VALUE=#";
        internal const string DIO_BIT_VALUE_QUERY = "?DIO{*/$}:VALUE";
        internal const string DIO_PORT_LATCH = "DIO{*}:LATCH=#";
        internal const string DIO_PORT_LATCH_QUERY = "?DIO{*}:LATCH";
        internal const string DIO_BIT_LATCH = "DIO{*/$}:LATCH=#";
        internal const string DIO_BIT_LATCH_QUERY = "?DIO{*/$}:LATCH";

        internal const string CTR_CHAN_QUERY = "?CTR";
        internal const string CTR_CH_VALUE = "CTR{*}:VALUE=#";
        internal const string CTR_CH_VALUE_QUERY = "?CTR{*}:VALUE";
        internal const string CTR_CH_START = "CTR{*}:START";
        internal const string CTR_CH_STOP = "CTR{*}:STOP";

        internal static string InsertChannel(string message, object channel)
        {
            string chStr;

            if (channel is String)
                chStr = channel as String;
            else
                chStr = channel.ToString();

            if (message.IndexOf("*") >= 0)
                return message.Replace("*", chStr);

            else return message;
        }

        internal static string InsertElement(string message, object element)
        {
            string chStr;

            if (element is String)
                chStr = element as String;
            else
                chStr = element.ToString();

            if (message.IndexOf("*") >= 0)
                return message.Replace("*", chStr);

            else return message;
        }

        internal static string InsertPort(string message, object port)
        {
            string portStr;

            if (port is String)
                portStr = port as String;
            else
                portStr = port.ToString();

            if (message.IndexOf("*") >= 0)
                return message.Replace("*", portStr);

            else return message;
        }

        internal static string InsertBit(string message, object bit)
        {
            string bitStr;

            if (bit is String)
                bitStr = bit as String;
            else
                bitStr = bit.ToString();

            if (message.IndexOf("$") >= 0)
                return message.Replace("$", bitStr);

            else return message;
        }

        internal static string InsertValue(string message, object value)
        {
            string valueStr;

            if (value is String)
                valueStr = value as String;
            else
                valueStr = value.ToString();

            if (message.IndexOf("#") >= 0)
                return message.Replace("#", valueStr);

            else return message;
        }
    }

    internal class ReflectionMessages
    {
        // The following wild cards will be used 
        //  * = channel, port
        //  $ = bit
        internal const string AI_CHMODES = "AI:CHMODES";
        internal const string AI_CH_CHMODES = "AI{*}:CHMODES";
        internal const string AI_CHANNELS_SE = "AI:CHANNELS/SE";
        internal const string AI_CHANNELS_DIFF = "AI:CHANNELS/DIFF";
        internal const string AI_MAXCOUNT = "AI:MAXCOUNT";
        internal const string AI_RANGES = "AI:RANGES";
        internal const string AI_CH_RANGES = "AI{*}:RANGES";
        internal const string AI_DATARATES = "AI:DATARATES";

        internal static string InsertChannel(string message, object channel)
        {
            return Messages.InsertChannel(message, channel);
        }

        internal static string InsertElement(string message, object element)
        {
            return Messages.InsertElement(message, element);
        }

        internal static string InsertPort(string message, object port)
        {
            return Messages.InsertPort(message, port);
        }

        internal static string InsertBit(string message, object bit)
        {
            return Messages.InsertBit(message, bit);
        }

        internal static string InsertValue(string message, object value)
        {
            return Messages.InsertValue(message, value);
        }
    }
}
