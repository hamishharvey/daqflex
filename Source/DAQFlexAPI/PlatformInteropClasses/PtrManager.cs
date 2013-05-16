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

namespace MeasurementComputing.DAQFlex
{
    //==================================================================================
    /// <summary>
    /// Helper class to extract memory pointers and data from unamanged structures.
    /// Handles 32/64 bits and BigEndian/LittleEndian storage
    /// </summary>
    //==================================================================================
    internal unsafe class PtrManager
    {
        //==================================================================================
        /// <summary>
        /// Extracts a memory pointer from an unmanaged structure.
        /// </summary>
        /// <param name="ptr">A pointer to the address within the unmanaged structure</param>
        /// <returns>The memory pointer</returns>
        //==================================================================================
        internal static IntPtr GetAddressPointer(ref byte* ptr)
        {
            int ptrSize = IntPtr.Size;

            if (BitConverter.IsLittleEndian)
            {
                // little endian 
                if (ptrSize == 8)
                {
                    // 64-bits
                    ulong p = 0;
                    for (int i = 0; i < ptrSize; i++)
                        p |= (ulong)((ulong)*ptr++ << (i * 8));

                    return (IntPtr)p;
                }
                else
                {
                    // 32-bits
                    uint p = 0;
                    for (int i = 0; i < ptrSize; i++)
                        p |= (uint)((uint)*ptr++ << (i * 8));

                    return (IntPtr)p;
                }
            }
            else
            {
                // big endian
                int subtractor = ptrSize - 1;

                if (ptrSize == 8)
                {
                    // 64-bits
                    long p = 0;
                    for (int i = 0; i < ptrSize; i++)
                        p |= (long)((long)*ptr++ << ((subtractor - i) * 8));

                    return new IntPtr(p);
                }
                else
                {
                    // 32-bits
                    int p = 0;
                    for (int i = 0; i < ptrSize; i++)
                        p |= (int)((int)*ptr++ << ((subtractor - i) * 8));

                    return new IntPtr(p);
                }
            }
        }

        //==================================================================================
        /// <summary>
        /// Extracts a 16-bit integer from an unmanaged structure
        /// </summary>
        /// <param name="ptr">A pointer to the integer within the unmanaged structure</param>
        /// <returns>The value of the integer</returns>
        //==================================================================================
        internal static short GetInt16(ref byte* ptr)
        {
            short p = 0;

            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < sizeof(short); i++)
                    p |= (short)(*ptr++ << (i * 8));
            }
            else
            {
                int subtractor = sizeof(short) - 1;
                for (int i = 0; i < sizeof(short); i++)
                    p |= (short)(*ptr++ << ((subtractor - i) * 8));
            }

            return p;
        }

        //====================================================================================================
        /// <summary>
        /// Extracts a 16-bit unsigned integer from an unmanaged structure
        /// </summary>
        /// <param name="ptr">A pointer to the unsigned integer within the unmanaged structure</param>
        /// <returns>The value of the unsigned integer</returns>
        //====================================================================================================
        internal static ushort GetUInt16(ref byte* ptr)
        {
            ushort p = 0;

            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < sizeof(ushort); i++)
                    p |= (ushort)(*ptr++ << (i * 8));
            }
            else
            {
                int subtractor = sizeof(ushort) - 1;
                for (int i = 0; i < sizeof(ushort); i++)
                    p |= (ushort)(*ptr++ << ((subtractor - i) * 8));
            }

            return p;
        }

        //====================================================================================================
        /// <summary>
        /// Extracts a 32-bit integer from an unmanaged structure
        /// </summary>
        /// <param name="ptr">A pointer to the integer within the unmanaged structure</param>
        /// <returns>The value of the integer</returns>
        //====================================================================================================
        internal static int GetInt32(ref byte* ptr)
        {
            int p = 0;

            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < sizeof(int); i++)
                    p |= (int)(*ptr++ << (i * 8));
            }
            else
            {
                int subtractor = sizeof(int) - 1;
                for (int i = 0; i < sizeof(int); i++)
                    p |= (int)(*ptr++ << ((subtractor - i) * 8));
            }

            return p;
        }

        //====================================================================================================
        /// <summary>
        /// Extracts a 32-bit unsigned integer from an unmanaged structure
        /// </summary>
        /// <param name="ptr">A pointer to the unsigned integer within the unmanaged structure</param>
        /// <returns>The value of the unsigned integer</returns>
        //====================================================================================================
        internal static uint GetUInt32(ref byte* ptr)
        {
            uint p = 0;

            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < sizeof(uint); i++)
                    p |= (uint)(*ptr++ << (i * 8));
            }
            else
            {
                int subtractor = sizeof(uint) - 1;
                for (int i = 0; i < sizeof(uint); i++)
                    p |= (uint)(*ptr++ << ((subtractor - i) * 8));
            }

            return p;
        }

        internal static IntPtr IncrementPointer(IntPtr ptr, int offset)
        {
            if (IntPtr.Size == 8)
            {
                return new IntPtr(ptr.ToInt64() + offset);
            }
            else
            {
                return new IntPtr(ptr.ToInt32() + offset);
            }
        }

        protected PtrManager()
        {
        }
    }
}
