﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MeasurementComputing.DAQFlex {
    class Usb205 : Usb20x {
        internal Usb205(DeviceInfo deviceInfo)
            : base(deviceInfo)
        {
            Ai = new Usb20xAi(this, deviceInfo);
            Dio = new DioComponent(this, deviceInfo, 1);
            Ctr = new VirtualSSEventCounter(this, deviceInfo, 1);
            Ao = new Usb20xAo(this, deviceInfo);

            // 8/26/2013: version 1.0
            m_defaultDevCapsImage = new byte[] {
               0x1F,0x8B,0x08,0x00,0x00,0x00,0x00,0x00,0x04,0x00,0xED,0xBD,0x07,0x60,0x1C,0x49,
                0x96,0x25,0x26,0x2F,0x6D,0xCA,0x7B,0x7F,0x4A,0xF5,0x4A,0xD7,0xE0,0x74,0xA1,0x08,
                0x80,0x60,0x13,0x24,0xD8,0x90,0x40,0x10,0xEC,0xC1,0x88,0xCD,0xE6,0x92,0xEC,0x1D,
                0x69,0x47,0x23,0x29,0xAB,0x2A,0x81,0xCA,0x65,0x56,0x65,0x5D,0x66,0x16,0x40,0xCC,
                0xED,0x9D,0xBC,0xF7,0xDE,0x7B,0xEF,0xBD,0xF7,0xDE,0x7B,0xEF,0xBD,0xF7,0xBA,0x3B,
                0x9D,0x4E,0x27,0xF7,0xDF,0xFF,0x3F,0x5C,0x66,0x64,0x01,0x6C,0xF6,0xCE,0x4A,0xDA,
                0xC9,0x9E,0x21,0x80,0xAA,0xC8,0x1F,0x3F,0x7E,0x7C,0x1F,0x3F,0x22,0xF6,0x1E,0x7C,
                0xFA,0xBB,0xEF,0x8E,0x77,0x76,0x7F,0xF7,0xDD,0x47,0x3B,0x8F,0xBE,0xF5,0xE8,0xF7,
                0x7E,0xB4,0x4B,0xFF,0xED,0xFC,0xEE,0xF8,0x57,0xFE,0xDA,0x79,0x74,0x40,0x7F,0xDD,
                0xE3,0xBF,0xF1,0xE9,0x3D,0xFA,0x6B,0xCF,0x7E,0xB7,0xBF,0xF3,0xF0,0x3E,0x7D,0xB0,
                0x6F,0x3F,0xD8,0xDB,0xC1,0xCB,0xF7,0x6D,0xF3,0x7D,0x7C,0xFD,0xA9,0x85,0xBC,0x0F,
                0x60,0xFB,0x0F,0xEC,0xDF,0xBB,0xBB,0x9F,0xFE,0xEE,0x7B,0x8F,0x1E,0xDA,0xD7,0xEF,
                0xEF,0xE0,0xA1,0x8F,0x76,0x5D,0xFF,0x3B,0x84,0x1E,0x5A,0xED,0x1A,0x04,0xFD,0x66,
                0x06,0x15,0x82,0xFA,0x10,0x7F,0x1B,0xD0,0x3B,0xF4,0xD5,0xDE,0xC1,0x01,0x3E,0x72,
                0xD0,0xF9,0xCF,0x03,0xD7,0xF9,0xFE,0x3D,0xFA,0x60,0x5F,0x90,0xDF,0xC3,0x07,0x3B,
                0x80,0xB1,0xB7,0x63,0x3F,0xB8,0x7F,0x6F,0x74,0x7F,0x1F,0x1F,0xED,0xDA,0x8F,0x0E,
                0xEE,0x8F,0x0E,0x80,0xCD,0xDE,0x5E,0xEF,0xA3,0x7D,0x33,0x4E,0x22,0x03,0xFD,0x79,
                0xDF,0xF4,0x24,0x54,0xB9,0xF7,0x68,0xEF,0x9E,0xEB,0x7A,0xEF,0x21,0x3E,0x70,0x5D,
                0x7F,0xBA,0x3F,0xFA,0xF4,0xFE,0xE8,0xE1,0xDE,0xE8,0xE1,0xBD,0xDF,0x7D,0xFF,0x91,
                0x1B,0xFC,0x1E,0xFD,0xD5,0x21,0xF7,0xFE,0x23,0xC0,0x11,0xF2,0xEE,0x7D,0x4A,0x7F,
                0x7A,0xD4,0xBF,0xBF,0x83,0xF6,0xF7,0xED,0xF7,0xFB,0x68,0xDE,0xA1,0xF7,0x03,0x0F,
                0xFC,0x2E,0xFD,0xB5,0x07,0x3C,0x77,0x04,0x8B,0x4F,0x47,0x9F,0x3E,0x18,0x7D,0x7A,
                0x30,0xFA,0xF4,0x21,0x7D,0xB1,0xBF,0x63,0xBF,0x78,0xB8,0x3F,0xA2,0x9E,0x1F,0x30,
                0x2A,0x3B,0xDA,0x15,0xFE,0x0E,0x27,0xFE,0x61,0x00,0xF9,0x21,0x61,0xA7,0xCD,0x77,
                0x1F,0x3D,0xC0,0xB7,0xEE,0xED,0xFD,0xBD,0x87,0xFB,0x0F,0x3F,0x7D,0xB0,0x47,0x40,
                0x1F,0x3E,0xDA,0xDF,0xB5,0x9F,0xA3,0xD5,0xBE,0x6B,0xC6,0x2F,0x3D,0xB0,0x40,0x0E,
                0x1E,0xE0,0xEB,0x90,0xAA,0x0F,0xF6,0x3F,0xFD,0x74,0x7F,0xFF,0xE0,0xFE,0xEE,0xFF,
                0x03,0xB0,0x53,0x10,0xFE,0xCA,0x02,0x00,0x00
            };
        }
    }
}
