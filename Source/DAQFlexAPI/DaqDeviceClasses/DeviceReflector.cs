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
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace MeasurementComputing.DAQFlex
{
    internal class DevCapNames
    {
        // THE ORDER OF THESE CANNOT CHANGE - WHEN ADDING TO THIS CLASS CREATE A NEW REGION BASED ON THE VERSION
        #region version 2.0.0.0 

        internal const string CHMODES = "CHMODES";
        internal const string CHANNELS = "CHANNELS";
        internal const string MAXCOUNT = "MAXCOUNT";
        internal const string RANGES = "RANGES";
        internal const string MAXRATE = "MAXRATE";
        internal const string INPUTS = "INPUTS";
        internal const string FACCAL = "FACCAL";
        internal const string SELFCAL = "SELFCAL";
        internal const string FIELDCAL = "FIELDCAL";
        internal const string MAXSCANTHRUPUT = "MAXSCANTHRUPUT";
        internal const string MAXSCANRATE = "MAXSCANRATE";
        internal const string MINSCANRATE = "MINSCANRATE";
        internal const string SCANRATECALC = "SCANRATECALC";
        internal const string MAXBURSTTHRUPUT = "MAXBURSTTHRUPUT";
        internal const string MAXBURSTRATE = "MAXBURSTRATE";
        internal const string MINBURSTRATE = "MINBURSTRATE";
        internal const string BURSTRATECALC = "BURSTRATECALC";
        internal const string FIFOSIZE = "FIFOSIZE";
        internal const string QUEUESEQ = "QUEUESEQ";
        internal const string QUEUELEN = "QUEUELEN";
        internal const string XFRMODES = "XFRMODES";
        internal const string EXTPACER = "EXTPACER";
        internal const string TRIG = "TRIG";
        internal const string SRCS = "SRCS";
        internal const string TYPES = "TYPES";
        internal const string OUTPUTS = "OUTPUTS";
        internal const string TYPE = "TYPE";
        internal const string EDGE = "EDGE";
        internal const string CONFIG = "CONFIG";
        internal const string SENSORS = "SENSORS";
        internal const string TCTYPES = "TCTYPES";
        internal const string CJC = "CJC";
        internal const string REARM = "REARM";
        internal const string SIMUL = "SIMUL";

        #endregion

        // #region version 2.1.0.0 
        // #endregion
    }

    internal class DevCapConfigurations
    {
        // THE ORDER OF THESE CANNOT CHANGE - WHEN ADDING TO THIS CLASS CREATE A NEW REGION BASED ON THE VERSION
        #region version 2.0.0.0 

        internal const string ALL = "ALL";
        internal const string SE = "SE";
        internal const string DIFF = "DIFF";

        #endregion

        // #region version 2.1.0.0 
        // #endregion
    }

    internal class DevCapImplementations
    {
        // THE ORDER OF THESE CANNOT CHANGE - WHEN ADDING TO THIS CLASS CREATE A NEW REGION BASED ON THE VERSION
        #region version 2.0.0.0 

        internal const string NAP = "N/A";
        internal const string FIXED = "FIXED";
        internal const string PROG = "PROG";
        internal const string IPROG = "IPROG";
        internal const string HWSEL = "HWSEL";

        #endregion

        // #region version 2.1.0.0 
        // #endregion
    }

    internal class DevCapTypes
    {
        // THE ORDER OF THESE CANNOT CHANGE - WHEN ADDING TO THIS CLASS CREATE A NEW REGION BASED ON THE VERSION
        #region version 2.0.0.0

        internal const string NUM = "NUM";
        internal const string TXT = "TXT";

        #endregion

        // #region version 2.1.0.0 
        // #endregion
    }

    internal class DevCapValues
    {
        // THE ORDER OF THESE CANNOT CHANGE - WHEN ADDING TO THIS CLASS CREATE A NEW REGION BASED ON THE VERSION
        #region version 2.0.0.0

        internal const string SE = "SE";
        internal const string DIFF = "DIFF";
        internal const string BIP20V = "BIP20V";
        internal const string BIP10V = "BIP10V";
        internal const string BIP5V = "BIP5V";
        internal const string BIP4V = "BIP4V";
        internal const string BIP2PT5V = "BIP2.5V";
        internal const string BIP2V = "BIP2V";
        internal const string BIP1PT25V = "BIP1.25V";
        internal const string BIP1V = "BIP1V";
        internal const string BIPPT625V = "BIP0.625V";
        internal const string BIPPT5V = "BIP0.5V";
        internal const string BIPPT25V = "BIP0.25V";
        internal const string BIPPT2V = "BIP0.2V";
        internal const string BIPPT1V = "BIP0.1V";
        internal const string BIPPT05V = "BIP0.05V";
        internal const string BIPPT01V = "BIP0.01V";
        internal const string BIPPT005V = "BIP5E-3V";
        internal const string BIP1PT67V = "BIP1.67V";
        internal const string BIPPT312V = "BIP0.312V";
        internal const string BIPPT156V = "BIP0.156V";
        internal const string BIPPT078V = "BIP0.078V";
        internal const string BIP60V = "BIP60V";
        internal const string BIP15V = "BIP15V";
        internal const string BIPPT125V = "BIP0.125V";
        internal const string UNI10V = "UNI10V";
        internal const string UNI5V = "UNI5V";
        internal const string UNI4PT096V = "UNI4.096V";
        internal const string UNI2PT5V = "UNI2.5V";
        internal const string UNI2V = "UNI2V";
        internal const string UNI1PT25V = "UNI1.25V";
        internal const string UNI1V = "UNI1V";
        internal const string UNIPT5V = "UNI0.5V";
        internal const string UNIPT25V = "UNI0.25V";
        internal const string UNIPT2V = "UNI0.2V";
        internal const string UNIPT1V = "UNI0.1V";
        internal const string UNIPT05V = "UNI0.05V";
        internal const string UNIPT01V = "UNI0.01V";
        internal const string UNIPT02V = "UNI0.02V";
        internal const string UNI1PT67V = "UNI1.67V";
        internal const string MA4TO20 = "4TO20MA";
        internal const string MA2TO10 = "2TO10MA ";
        internal const string MA1TO5 = "1TO5MA";
        internal const string MAPT5TO2PT5 = "0.5TO2.5MA";
        internal const string MA0TO20 = "0TO20MA";
        internal const string VOLTS = "VOLTS";
        internal const string CURRENT = "CURRENT";
        internal const string TEMP = "TEMP";
        internal const string SUPPORTED = "SUPPORTED";
        internal const string METHOD1 = "METHOD1";
        internal const string METHOD2 = "METHOD2";
        internal const string METHOD3 = "METHOD3";
        internal const string METHOD4 = "METHOD4";
        internal const string BLOCKIO = "BLOCKIO";
        internal const string SINGLEIO = "SINGLEIO";
        internal const string BURSTAD = "BURSTAD";
        internal const string BURSTIO = "BURSTIO";
        internal const string INT = "INT";
        internal const string EXT = "EXT";
        internal const string ENMSTR = "ENABLE/MASTER";
        internal const string ENSLV = "ENABLE/SLAVE";
        internal const string ENGSLV = "ENABLE/GSLAVE";
        internal const string HWDIG = "HW/DIG";
        internal const string HWANLG = "HW/ANLG";
        internal const string EDGERISING = "EDGE/RISING";
        internal const string EDGEFALLING = "EDGE/FALLING";
        internal const string PORTIN = "PORTIN";
        internal const string PORTOUT = "PORTOUT";
        internal const string BITIN = "BITIN";
        internal const string BITOUT = "BITOUT";
        internal const string EVENT = "EVENT";
// 6/18/2010:
        internal const string DUPLICATE = "DUPLICATE";
        internal const string SEQUENTIAL = "SEQUENTIAL";
        internal const string NONSEQUENTIAL = "NONSEQUENTIAL";

        internal const string BIPPT073125 = "BIP73.125E-3V";
        internal const string BIPPT14625 = "BIP146.25E-3V";

        internal const string TC = "TC";

        // TC types
        internal const string B = "B";
        internal const string E = "E";
        internal const string J = "J";
        internal const string K = "K";
        internal const string N = "N";
        internal const string R = "R";
        internal const string S = "S";
        internal const string T = "T";

        internal const string ENABLE = "ENABLE";
        internal const string DISABLE = "DISABLE";
        internal const string RISING = "RISING";
        internal const string FALLING = "FALLING";

        #endregion
        
        // #region version 2.1.0.0 
        // #endregion
    }                         

    //======================================================================================================
    /// <summary>
    ///  Helper class to load and store device capabilities using the device's eeprom for storage
    /// </summary>
    //======================================================================================================
    internal class DeviceReflector
    {
        private const int LOOKUP_SIZE = 256;

        private Dictionary<int, string> m_componentReflectionCodes = new Dictionary<int, string>();
        private Dictionary<int, string> m_devCapsReflectionCodes = new Dictionary<int, string>();
        private Dictionary<int, string> m_implementationReflectionCodes = new Dictionary<int, string>();
        private Dictionary<int, string> m_configReflectionCodes = new Dictionary<int, string>();
        private Dictionary<int, string> m_valueTypeReflectionCodes = new Dictionary<int, string>();
        private Dictionary<int, string> m_valueReflectionCodes = new Dictionary<int, string>();
        private uint[] m_crcTable = new uint[LOOKUP_SIZE];

        internal DeviceReflector()
        {
            BuildComponentReflectionCodes();
            BuildDeviceCapsReflectionCodes();
            BuildConfigurationReflectionCodes();
            BuildImplementationReflectionCodes();
            BuildValueTypeReflectionCodes();
            BuildDevCapValueCodes();
        }

        //==========================================================================================
        /// <summary>
        /// Calculates a 32-bit CRC for an array of bytes
        /// </summary>
        /// <param name="inputBuffer">The array of bytes to calculate the CRC for</param>
        /// <returns>The 32-bit CRC</returns>
        //==========================================================================================
        internal uint GetCRC32(byte[] inputBuffer)
        {
            uint crcBuffer = 0xFFFFFFFF;
            int count = inputBuffer.Length;
            uint byteValue;
            int index;
            uint arrayValue;
            uint shiftedValue = crcBuffer;

            initializeCRCTable();

            for (int i = 0; i < count; i++)
            {
                byteValue = inputBuffer[i] ^ shiftedValue;
                index = ((int)byteValue & 0x000000FF);
                arrayValue = m_crcTable[index];

                shiftedValue = shiftedValue >> 8;
                shiftedValue &= 0x00FFFFFF;

                shiftedValue ^= arrayValue;
            }

            shiftedValue ^= 0xFFFFFFFF;

            return shiftedValue;
        }

        //==========================================================================================
        /// <summary>
        /// Initializes a lookup table for the CRC calculation
        /// </summary>
        //==========================================================================================
        internal void initializeCRCTable()
        {
            for (uint i = 0; i < LOOKUP_SIZE; i++)
            {
                uint shiftedValue = i;

                for (int j = 0; j < 8; j++)
                {
                    if ((shiftedValue & 1) == 0)
                    {
                        shiftedValue = (shiftedValue >> 1);
                    }
                    else
                    {
                        shiftedValue = (shiftedValue >> 1);
                        shiftedValue ^= 0xEDB88320;
                    }
                }

                m_crcTable[i] = shiftedValue;
            }
        }

        //==============================================================================================
        /// <summary>
        /// Gets the component name based on the component code
        /// </summary>
        /// <param name="componentCode">The component code</param>
        /// <returns>the component name</returns>
        //==============================================================================================
        internal string GetComponent(int componentCode)
        {
            string value;

            if (m_componentReflectionCodes.TryGetValue(componentCode, out value) == true)
                return value;
            else
                return String.Empty;
        }

        //==============================================================================================
        /// <summary>
        /// Gets the device capability name based on the name code
        /// </summary>
        /// <param name="componentCode">The name code</param>
        /// <returns>The device capability name</returns>
        //==============================================================================================
        internal string GetDevCapName(int nameCode)
        {
            string value;

            if (m_devCapsReflectionCodes.TryGetValue(nameCode, out value) == true)
                return value;
            else
                return String.Empty;
        }

        //==============================================================================================
        /// <summary>
        /// Gets the implentation based on the implementation code
        /// </summary>
        /// <param name="componentCode">The implementation code</param>
        /// <returns>The implementation</returns>
        //==============================================================================================
        internal string GetImplementation(int implCode)
        {
            string value;

            if (m_implementationReflectionCodes.TryGetValue(implCode, out value) == true)
                return value;
            else
                return String.Empty;
        }

        //==============================================================================================
        /// <summary>
        /// Gets the configuration based on the configuration code
        /// </summary>
        /// <param name="componentCode">The configuration code</param>
        /// <returns>The configuration</returns>
        //==============================================================================================
        internal string GetConfiguration(int configCode)
        {
            string value;

            if (m_configReflectionCodes.TryGetValue(configCode, out value) == true)
                return value;
            else
                return String.Empty;
        }

        //==============================================================================================
        /// <summary>
        /// Gets the value type based on the value type code
        /// </summary>
        /// <param name="componentCode">The value type code</param>
        /// <returns>The value type</returns>
        //==============================================================================================
        internal string GetValueType(int valueTypeCode)
        {
            string value;

            if (m_valueTypeReflectionCodes.TryGetValue(valueTypeCode, out value) == true)
                return value;
            else
                return String.Empty;
        }

        //==============================================================================================
        /// <summary>
        /// Gets the value based on the value code
        /// </summary>
        /// <param name="componentCode">The value code</param>
        /// <returns>The value</returns>
        //==============================================================================================
        internal string GetValue(int valueCode)
        {
            string value;

            if (m_valueReflectionCodes.TryGetValue(valueCode, out value) == true)
                return value;
            else
                return String.Empty;
        }

        internal void BuildComponentReflectionCodes()
        {
            Type type = typeof(MeasurementComputing.DAQFlex.DaqComponents);

            if (type != null)
            {
                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);

                if (fieldInfos != null)
                {
                    if (fieldInfos.Length > 0)
                    {
                        for (int i = 0; i < fieldInfos.Length; i++)
                        {
                            string fieldValue = (string)fieldInfos[i].GetValue(null);
                            m_componentReflectionCodes.Add(i, fieldValue);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Could not get DaqComponent fields");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Could not get DaqComponent fields");
                }
            }
        }

        internal void BuildDeviceCapsReflectionCodes()
        {
            Type type = typeof(MeasurementComputing.DAQFlex.DevCapNames);

            if (type != null)
            {
                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);

                if (fieldInfos != null)
                {
                    if (fieldInfos.Length > 0)
                    {
                        for (int i = 0; i < fieldInfos.Length; i++)
                        {
                            string fieldValue = (string)fieldInfos[i].GetValue(null);
                            m_devCapsReflectionCodes.Add(i, fieldValue);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Could not get DeviceCaps fields");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Could not get DeviceCaps fields");
                }
            }
        }

        internal void BuildImplementationReflectionCodes()
        {
            Type type = typeof(MeasurementComputing.DAQFlex.DevCapImplementations);

            if (type != null)
            {
                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);

                if (fieldInfos != null)
                {
                    if (fieldInfos.Length > 0)
                    {
                        for (int i = 0; i < fieldInfos.Length; i++)
                        {
                            string fieldValue = (string)fieldInfos[i].GetValue(null);
                            m_implementationReflectionCodes.Add(i, fieldValue);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Could not get DeviceCaps fields");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Could not get DeviceCaps fields");
                }
            }
        }

        internal void BuildConfigurationReflectionCodes()
        {
            m_configReflectionCodes.Add(0, "ALL");
            m_configReflectionCodes.Add(1, "SE");
            m_configReflectionCodes.Add(2, "DIFF");
        }

        internal void BuildValueTypeReflectionCodes()
        {
            Type type = typeof(MeasurementComputing.DAQFlex.DevCapTypes);

            if (type != null)
            {
                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);

                if (fieldInfos != null)
                {
                    if (fieldInfos.Length > 0)
                    {
                        for (int i = 0; i < fieldInfos.Length; i++)
                        {
                            string fieldValue = (string)fieldInfos[i].GetValue(null);
                            m_valueTypeReflectionCodes.Add(i, fieldValue);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Could not get DevCapTypes fields");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Could not get DevCapTypes fields");
                }
            }
        }


        internal void BuildDevCapValueCodes()
        {
            Type type = typeof(MeasurementComputing.DAQFlex.DevCapValues);

            if (type != null)
            {
                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);

                if (fieldInfos != null)
                {
                    if (fieldInfos.Length > 0)
                    {
                        for (int i = 0; i < fieldInfos.Length; i++)
                        {
                            string fieldValue = (string)fieldInfos[i].GetValue(null);
                            m_valueReflectionCodes.Add(i, fieldValue);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Could not get DeviceCapsValues fields");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Could not get DeviceCapsValues fields");
                }
            }
        }

        internal static byte[] GetCompressedImage(string fileName)
        {
            FileStream xmlFile;

            try
            {
                xmlFile = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                byte[] buffer = new byte[xmlFile.Length];

                // Read the file to ensure it is readable.
                int count = xmlFile.Read(buffer, 0, buffer.Length);

                if (count != buffer.Length)
                {
                    xmlFile.Close();
                    return null;
                }

                xmlFile.Close();

                MemoryStream ms = new MemoryStream();

                // Use the newly created memory stream for the compressed data.
                GZipStream compressedzipStream = new GZipStream(ms, CompressionMode.Compress, true);
                compressedzipStream.Write(buffer, 0, buffer.Length);

                // Close the stream.
                compressedzipStream.Close();
                Console.WriteLine("Original size: {0}, Compressed size: {1}", buffer.Length, ms.Length);

                byte[] compressedBuffer = new byte[ms.Length];

                Array.Copy(ms.GetBuffer(), compressedBuffer, (int)ms.Length);

                return compressedBuffer;
            }
            catch (Exception)
            {
                return null;
            }
        }

        //=============================================================================================
        /// <summary>
        /// Decompresses the device caps image that was read from the device's eeprom
        /// </summary>
        /// <param name="compressedImage">The compressed image</param>
        /// <returns>The uncompressed image</returns>
        //=============================================================================================
        internal byte[] DecompressDeviceCapsImage(byte[] compressedImage)
        {
            MemoryStream ms = new MemoryStream(compressedImage);
            byte[] uncompressedImage = null;

            try
            {
                using (GZipStream gzStream = new GZipStream(ms, CompressionMode.Decompress))
                {
                    byte[] sizeBuffer = new byte[4];
                    ms.Position = (int)ms.Length - 4;
                    ms.Read(sizeBuffer, 0, 4);

                    int uncompressedLength = BitConverter.ToInt32(sizeBuffer, 0);

                    ms.Position = 0;

                    uncompressedImage = new byte[uncompressedLength];

                    gzStream.Read(uncompressedImage, 0, uncompressedLength);
                }

            }
            catch (Exception)
            {
            }

            return uncompressedImage;
        }
    }
}
