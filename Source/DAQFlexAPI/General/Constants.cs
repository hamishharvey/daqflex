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
    internal class Constants
    {
        internal const char VALUE_RESOLVER = '/';       // The char that resolves values (e.g. EDGE/RISING)
        internal const char REFLECTOR_SYMBOL = '@';     // The char that denotes a device refelction message
        internal const char QUERY = '?';                // Identifier for querying a value
        internal const char PROPERTY_SEPARATOR = ':';   // The char that separates a property from the component part of a message
        internal const int MAX_MESSAGE_LENGTH = 64;     // Max size of the message packet
        internal const int MAX_COMMAND_LENGTH = 64;     // Max size of the message packet
        internal const char NULL_TERMINATOR = '\0';     // The null terminator for strings
        internal const string DEVICE_NAME_SEPARATOR = "::"; // The chars used to separate the device name, serial number and id
        internal const short FILE_ATTRIBUTE_NORMAL = ((short)(0x80));
        internal const int FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const short FILE_SHARE_READ = 0x1;
        internal const short FILE_SHARE_WRITE = 0x2;
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const int INVALID_HANDLE_VALUE = -1;
        internal const uint CREATE_ALWAYS = 2;
        internal const uint OPEN_EXISTING = 3;
        internal const int BITS_PER_BYTE = 8;
        internal const int SIZE_OF_DID = 28;
        internal const string EQUAL_SIGN = "=";
        internal const string PERCENT = "%";
        internal const double VALUE_OUT_OF_RANGE = -8888;
        internal const double OPEN_THERMOCOUPLE = -9999;
        internal const string NOT_SET = "NOT_SET"; // value used for when something is not set
        internal const string VALUE_SEPARATOR = ",";
        internal const string DECIMAL = ".";
        internal const char WHITE_SPACE = ' ';
        internal const char DIRECT_ROUTING_SYMBOL = '>';
        internal const char LESS_THAN_SYMBOL = '<';
        internal const char GREATER_THAN_SYMBOL = '>';
    }

    //====================================================================
    /// <summary>
    /// Device class names used for creation through reflection
    /// </summary>
    //====================================================================
    internal class DaqDeviceClassNames
    {
        internal const string USB_7202 = "Usb7202";
        internal const string USB_7204 = "Usb7204";
        internal const string USB_2001_TC = "Usb2001Tc";
        internal const string USB_2408 = "Usb2408";
        internal const string USB_2408_2AO = "Usb24082Ao";
        internal const string USB_1608G = "Usb1608G";
        internal const string USB_1608GX = "Usb1608GX";
        internal const string USB_1608GX_2AO = "Usb1608GX2Ao";
        internal const string USB_201 = "Usb201";
        internal const string USB_204 = "Usb204";
        internal const string VIRTUAL_DEVICE = "VirtualDevice";
        internal const string USB_1208FS_PLUS = "Usb1208FSPlus";
        internal const string USB_1408FS_PLUS = "Usb1408FSPlus";
        internal const string USB_1608FS_PLUS = "Usb1608FSPlus";
        internal const string USB_7110 = "Usb7110";
        internal const string USB_7112 = "Usb7112";
    }

    internal class ControlRequestType
    {
        internal const byte VENDOR_CONTROL_IN = 0xC0;
        internal const byte VENDOR_CONTROL_OUT = 0x40;
    }

    internal class ControlRequest
    {
        internal const byte MESSAGE_REQUEST = 0x80;
        internal const byte VALUE_REQUEST = 0x81;
    }

    // ideally these should go into a resource file

    internal class DaqComponents
    {
        // THE ORDER OF THESE CANNOT CHANGE - WHEN ADDING TO THIS CLASS CREATE A NEW REGION BASED ON THE VERSION
        #region version 2.0.0.0
        internal const string DEV = "DEV";
        internal const string AI = "AI";
        internal const string AISCAN = "AISCAN";
        internal const string AITRIG = "AITRIG";
        internal const string AO = "AO";
        internal const string AOSCAN = "AOSCAN";
        internal const string AOTRIG = "AOTRIG";
        internal const string DIO = "DIO";
        internal const string DIOSCAN = "DIOSCAN";
        internal const string CTR = "CTR";
        internal const string CTRSCAN = "CTRSCAN";
        internal const string TMR = "TMR";
        internal const string COMPI = "COMPI";
        internal const string COMPITRIG = "COMPITRIG";
        internal const string COMPO = "COMPO";
        internal const string COMPOTRIG = "COMPOTRIG";
        #endregion

        #region 2.2.0.0
        internal const string AIQUEUE = "AIQUEUE";
        internal const string AICAL = "AICAL";
        internal const string AOCAL = "AOCAL";
        #endregion
    }

    internal class DaqProperties : DevCapNames
    {
        internal const string MFGSER = "MFGSER";
        internal const string FWV = "FWV";
        internal const string ID = "ID";
        internal const string MFGCAL = "MFGCAL";
        internal const string OFFSET = "OFFSET";
        internal const string SLOPE = "SLOPE";
        internal const string VALUE = "VALUE";
        internal const string RANGE = "RANGE";
        internal const string CHMODE = "CHMODE";
        internal const string DIR = "DIR";
        internal const string LOWCHAN = "LOWCHAN";
        internal const string HIGHCHAN = "HIGHCHAN";
        internal const string XFERMODE = "XFRMODE";
        internal const string RATE = "RATE";
        internal const string SAMPLES = "SAMPLES";
        internal const string EXTSYNC = "EXTSYNC";
        internal const string DEBUG = "DEBUG";
        internal const string STATUS = "STATUS";
        internal const string TRIGSCR = "SRC";
        internal const string TRIGTYPE = "TYPE";
        internal const string TRIGLEVEL = "LEVEL";
        internal const string UPPERLEVEL = "UPPERLEVEL";
        internal const string LOWERLEVEL = "LOWERLEVEL";
        internal const string PERIOD = "PERIOD";
        internal const string SERNO = "SERNO";
        internal const string SCALE = "SCALE";
        internal const string CAL = "CAL";
        internal const string QUEUE = "QUEUE";
        internal const string SENSOR = "SENSOR";
        internal const string MEMHANDLE = "MEMHANDLE";
        internal const string BUFOVERWRITE = "BUFOVERWRITE";
        internal const string CHAN = "CHAN";
        internal const string COUNT = "COUNT";
        internal const string DATARATE = "DATARATE";
        internal const string TEMPUNITS = "TEMPUNITS";
        internal const string YEAR = "YEAR";
        internal const string MONTH = "MONTH";
        internal const string DAY = "DAY";
        internal const string HOUR = "HOUR";
        internal const string MIN = "MIN";
        internal const string SEC = "SEC";
        internal const string TRIGREARM = "REARM";
        internal const string BLOCKSIZE = "BLOCKSIZE";
    }

    internal class PropertyValues : DevCapValues
    {
        internal const string DEFAULT = "DEFAULT";
        internal const string IN = "IN";
        internal const string OUT = "OUT";
        internal const string MASTER = "MASTER";
        internal const string SLAVE = "SLAVE";
        internal const string OTD = "OTD";
        internal const string MAXRNG = "MAXRNG";
        internal const string MINRNG = "MINRNG";
        internal const string READY = "READY";
        internal const string IDLE = "IDLE";
        internal const string RUNNING = "RUNNING";
        internal const string OVERRUN = "OVERRUN";
        internal const string UNDERRUN = "UNDERRUN";
        internal const string NOT_SUPPORTED = "NOT_SUPPORTED";
        internal const string CONFIGMODE = "CONFIGMODE";
        internal const string CONFIGURED = "CONFIGURED";
        internal const string DEVICE_BUSY = "DEVICE_BUSY";
        internal const string INVALID = "INVALID";
        internal const string NOT_SET = "NOT_SET";
    }

    internal class ValueResolvers
    {
        internal const string RAW = "RAW";
        internal const string VOLTS = "VOLTS";
        internal const string DEGC = "DEGC";
        internal const string DEGF = "DEGF";
        internal const string KELVIN = "KELVIN";
    }

    internal class DaqCommands
    {
        internal const string START = "START";
        internal const string STOP = "STOP";
        internal const string INIT = "INIT";
        internal const string FLASHLED = "FLASHLED";
        internal const string RESET = "RESET";
        internal const string LOADCAPS = "LOADCAPS";
        internal const string CLEAR = "CLEAR";
    }

    internal class APIMessages
    {
        internal const string AISCANBUFSIZE = "AISCAN:BUFSIZE";
        internal const string AISCANSTATUS_QUERY = "?AISCAN:STATUS";
        internal const string AISCANCOUNT_QUERY = "?AISCAN:COUNT";
        internal const string AISCANINDEX_QUERY = "?AISCAN:INDEX";
        internal const string AISCANCAL_QUERY = "?AISCAN:CAL";
        internal const string AISCANSCALE_QUERY = "?AISCAN:SCALE";
        internal const string AISCANBUFSIZE_QUERY = "?AISCAN:BUFSIZE";
        internal const string AISCANBLOCKSIZE = "AISCAN:BLOCKSIZE";
        internal const string AISCANBLOCKSIZE_QUERY = "?AISCAN:BLOCKSIZE";
        internal const string AISENSOR = "AI:SENSOR";
        internal const string AISENSOR_QUERY = "?AI:SENSOR";
        internal const string DEVPID = "?DEV:PID";
        internal const string AOSCANBUFSIZE = "AOSCAN:BUFSIZE";
        internal const string AOSCANSTATUS_QUERY = "?AOSCAN:STATUS";
        internal const string AOSCANCOUNT_QUERY = "?AOSCAN:COUNT";
        internal const string AOSCANINDEX_QUERY = "?AOSCAN:INDEX";
        internal const string AOSCANBUFSIZE_QUERY = "?AOSCAN:BUFSIZE";
        internal const string AISCAN_MIN_SAMPLE_RATE_QUERY = "?AISCAN:MINSAMPLERATE";
        internal const string AISCAN_MAX_SAMPLE_RATE_QUERY = "?AISCAN:MAXSAMPLERATE";
        internal const string AISCAN_SAMPLE_DT_QUERY = "?AISCAN:SAMPLEDT";
        internal const string AISCAN_XFER_TIME = "AISCAN:XFERTIME";
        internal const string AISCAN_XFER_TIME_QUERY = "?AISCAN:XFERTIME";
    }

    internal class CurlyBraces
    {
        internal const char LEFT = '{';
        internal const char RIGHT = '}';
    }
}
