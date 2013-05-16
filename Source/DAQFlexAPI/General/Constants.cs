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
        //===================================================================================================== 
        /// <summary>
        /// The char that resolves values (e.g. EDGE/RISING)
        /// </summary>
        //===================================================================================================== 
        internal const char VALUE_RESOLVER = '/';

        internal const char REFLECTOR_SYMBOL = '@';

        //===================================================================================================== 
        /// <summary>
        /// Identifier for a Query Daq Item
        /// </summary>
        //===================================================================================================== 
        internal const char QUERY = '?';

        //===================================================================================================== 
        /// <summary>
        /// Identifier for a Query Daq Item
        /// </summary>
        //===================================================================================================== 
        internal const char PROPERTY_SEPARATOR = ':';
        
        //===================================================================================================== 
        /// <summary>
        /// Number of bits to shift bytes by
        /// </summary>
        //===================================================================================================== 
        internal const int BYTE_SHIFT = 8;

        //===================================================================================================== 
        /// <summary>
        /// The size of the buffer for read servers
        /// </summary>
        //===================================================================================================== 
        internal const int READ_BLOCK_SIZE = 32768;

        //===================================================================================================== 
        /// <summary>
        /// The size of the buffer for the write servers
        /// </summary>
        //===================================================================================================== 
        internal const int WRITE_BLOCK_SIZE = 32768;

        //===================================================================================================== 
        /// <summary>
        /// Size of a message packet
        /// </summary>
        //===================================================================================================== 
        internal const int MAX_MESSAGE_LENGTH = 64;

        //===================================================================================================== 
        /// <summary>
        /// Size of a command packet
        /// </summary>
        //===================================================================================================== 
        internal const int MAX_COMMAND_LENGTH = 64;

        //===================================================================================================== 
        /// <summary>
        /// The null terminator for strings
        /// </summary>
        //===================================================================================================== 
        internal const char NULL_TERMINATOR = '\0';

        //===================================================================================================== 
        /// <summary>
        /// The max count for a 12-bit device
        /// </summary>
        //===================================================================================================== 
        internal const int TWELVE_BITS = 4095;

        //===================================================================================================== 
        /// <summary>
        /// The max count for a 13-bit device
        /// </summary>
        //===================================================================================================== 
        internal const int THIRTEEN_BITS = 8191;

        //===================================================================================================== 
        /// <summary>
        /// The max count for a 16-bit device
        /// </summary>
        //===================================================================================================== 
        internal const int SIXTEEN_BITS = 65535;

        //===================================================================================================== 
        /// <summary>
        /// This is the number of elements in a device caps string separated by ":"
        /// (e.g. "AI:MAXCOUNT:ALL:ALL:FIXED:8" = 6 (no dependent features)
        /// </summary>
        //===================================================================================================== 
        internal const int DEFAULT_DEVCAPS_COUNT = 6;

        //===================================================================================================== 
        /// <summary>
        /// The max count for a 24-bit device
        /// </summary>
        //===================================================================================================== 
        internal const int TWENTYFOUR_BITS = 16777215;

        internal const int MAX_AI_CHANNEL_COUNT = 128;
        internal const int MAX_AO_CHANNEL_COUNT = 16;
        internal const int MAX_DIO_CHANNEL_COUNT = 12;
        internal const int MAX_CTR_CHANNEL_COUNT = 20;
        internal const int MAX_DIO_PORT_WIDTH = 16;

        //===================================================================================================== 
        /// <summary>
        /// The chars used to separate the device name, serial number and id
        /// </summary>
        //===================================================================================================== 
        internal const string DEVICE_NAME_SEPARATOR = "::";

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
        internal const string ERROR_KEY = "Error Code";
        internal const int SIZE_OF_DID = 28;
        internal const string EQUAL_SIGN = "=";
        internal const string VALUE_NOT_SET = "#";
        internal const string PERCENT = "%";
        internal const double VALUE_OUT_OF_RANGE = -8888;
        internal const double OPEN_THERMOCOUPLE = -9999;
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
    }

    internal class DaqProperties
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
        internal const string EXTPACER = "EXTPACER";
        internal const string EXTSYNC = "EXTSYNC";
        internal const string DEBUG = "DEBUG";
        internal const string TRIG = "TRIG";
        internal const string STATUS = "STATUS";
        internal const string TRIGSCR = "SRC";
        internal const string TRIGTYPE = "TYPE";
        internal const string TRIGLEVEL = "LEVEL";
        internal const string UPPERLEVEL = "UPPERLEVEL";
        internal const string LOWERLEVEL = "LOWERLEVEL";
        internal const string PULSE = "PULSE";
        internal const string DUTYCYCLE = "DUTYCYCLE";
        internal const string SERNO = "SERNO";
        internal const string SCALE = "SCALE";
        internal const string CAL = "CAL";
        internal const string QUEUE = "QUEUE";
        internal const string REARM = "REARM";
        internal const string CJC = "CJC";
        internal const string SENSOR = "SENSOR";
        internal const string MEMHANDLE = "MEMHANDLE";
    }

    internal class PropertyValues
    {
        // channel configuration
        internal const string SE = "SE";
        internal const string DIFF = "DIFF";

        // scan options
        internal const string DEFAULT = "DEFAULT";
        internal const string BLOCKIO = "BLOCKIO";
        internal const string SINGLEIO = "SINGLEIO";
        internal const string BURSTIO = "BURSTIO";

        // analog ranges
        internal const string BIP20V = "BIP20V";
        internal const string BIP10V = "BIP10V";
        internal const string BIP5V = "BIP5V";
        internal const string BIP4V = "BIP4V";
        internal const string BIP2PT5V = "BIP2.5V";
        internal const string BIP2V = "BIP2V";
        internal const string BIP1PT25V = "BIP1.25V";
        internal const string BIP1V = "BIP1V";
        internal const string BIPPT073125V = "BIP73.125E-3V";
        internal const string BIPPT14625V = "BIP146.25E-3V";

        internal const string UNI4PT096V = "UNI4.096V";

        // DIO direction
        internal const string IN = "IN";
        internal const string OUT = "OUT";

        // clock source
        internal const string INT = "INT";
        internal const string EXT = "EXT";
        internal const string MASTER = "MASTER";
        internal const string SLAVE = "SLAVE";

        internal const string OTD = "OTD";
        internal const string MAXRNG = "MAXRNG";
        internal const string MINRNG = "MINRNG";

        // general
        internal const string ENABLE = "ENABLE";
        internal const string DISABLE = "DISABLE";

        internal const string READY = "READY";
        internal const string IDLE = "IDLE";
        internal const string RUNNING = "RUNNING";
        internal const string NOT_SUPPORTED = "NOT_SUPPORTED";
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
    }

    internal class DaqFeatureNames
    {
        internal const string NONE = "NONE";
        internal const string CHANNELS = "CHANNELS";
        internal const string MAXCOUNT = "MAXCOUNT";
        internal const string CHMODES = "CHMODES";
        internal const string MAXRATE = "MAXRATE";
        internal const string CONFIG = "CONFIG";
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
        internal const string AISENSOR = "AI:SENSOR";
        internal const string AISENSOR_QUERY = "?AI:SENSOR";
        internal const string DEVPID = "?DEV:PID";
        internal const string AOSCANBUFSIZE = "AOSCAN:BUFSIZE";
        internal const string AOSCANSTATUS_QUERY = "?AOSCAN:STATUS";
        internal const string AOSCANCOUNT_QUERY = "?AOSCAN:COUNT";
        internal const string AOSCANINDEX_QUERY = "?AOSCAN:INDEX";
        internal const string AOSCANBUFSIZE_QUERY = "?AOSCAN:BUFSIZE";
    }

    internal class CurlyBraces
    {
        internal const char LEFT = '{';
        internal const char RIGHT = '}';
    }
}
