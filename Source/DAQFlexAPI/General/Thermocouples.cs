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
    internal struct VoltageRange
    {
        internal double LowerValue;
        internal double UpperValue;

        internal VoltageRange(double lowerValue, double upperValue)
        {
            LowerValue = lowerValue;
            UpperValue = upperValue;
        }
    }

    //=========================================================================
    /// <summary>
    /// Base class used for linearizing thermocouple measurements
    /// </summary>
    //=========================================================================
    internal class Thermocouple
    {
        internal const double TypeBMinTemp = 0.0;
        internal const double TypeBMaxTemp = 1820.0;
        internal const double TypeEMinTemp = -270.0;
        internal const double TypeEMaxTemp = 1000.0;
        internal const double TypeJMinTemp = -210.0;
        internal const double TypeJMaxTemp = 1200.0;
        internal const double TypeKMinTemp = -270.0;
        internal const double TypeKMaxTemp = 1372.0;
        internal const double TypeNMinTemp = -270.0;
        internal const double TypeNMaxTemp = 1300.0;
        internal const double TypeRMinTemp = -50.0;
        internal const double TypeRMaxTemp = 1768.1;
        internal const double TypeSMinTemp = -50.0;
        internal const double TypeSMaxTemp = 1768.1;
        internal const double TypeTMinTemp = -270.0;
        internal const double TypeTMaxTemp = 400.0;

        protected List<double[]> m_coeffRanges = new List<double[]>();
        protected List<double[]> m_reverseCoeffRanges = new List<double[]>();

        protected double[] m_activeCoeffs;
        protected VoltageRange[] m_tempRanges;

        protected Thermocouple()
        {
        }

        //==========================================================================
        /// <summary>
        /// Creates an instance of a thermocouple object based on the TC type
        /// </summary>
        /// <param name="type">The TC type</param>
        /// <returns>A Thermocouple object</returns>
        //==========================================================================
        internal static Thermocouple CreateThermocouple(ThermocoupleTypes tcType)
        {
            if (tcType == ThermocoupleTypes.TypeB)
                return new TypeBThermocouple();

            if (tcType == ThermocoupleTypes.TypeE)
                return new TypeEThermocouple();

            if (tcType == ThermocoupleTypes.TypeJ)
                return new TypeJThermocouple();

            if (tcType == ThermocoupleTypes.TypeK)
                return new TypeKThermocouple();

            if (tcType == ThermocoupleTypes.TypeN)
                return new TypeNThermocouple();

            if (tcType == ThermocoupleTypes.TypeR)
                return new TypeRThermocouple();

            if (tcType == ThermocoupleTypes.TypeS)
                return new TypeSThermocouple();

            if (tcType == ThermocoupleTypes.TypeT)
                return new TypeTThermocouple();

            return null;
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal virtual double VoltageToTemperature(double mVolts)
        {
            double value = 0.0;

            for (int i = 0; i < m_activeCoeffs.Length; i++)
                value += m_activeCoeffs[i] * Math.Pow(mVolts, (double)i);

            return value;
        }

        //====================================================================
        /// <summary>
        /// Converts temperature in deg C to milli-volts
        /// </summary>
        /// <param name="tempValue">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal virtual double TemperatureToVoltage(double tempValue)
        {
            double volts = 0.0;

            m_activeCoeffs = m_reverseCoeffRanges[0];

            for (int i = 0; i < m_activeCoeffs.Length; i++)
                volts += m_activeCoeffs[i] * Math.Pow(tempValue, (double)i);

            return volts;
        }

        //====================================================================
        /// <summary>
        /// Deg C to deg F conversion
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal double CtoF(double value)
        {
            return (value * 9 / 5) + 32;
        }

        //====================================================================
        /// <summary>
        /// Deg C to kelvin conversion
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal double CtoK(double value)
        {
            return value + 273.1;
        }

        //====================================================================
        /// <summary>
        /// Deg F to deg C conversion
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal double FtoC(double value)
        {
            return (value - 32) * 5 / 9;
        }

        //====================================================================
        /// <summary>
        /// Deg F to kelvin conversion
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal double FtoK(double value)
        {
            return FtoC(value) + 273.15;
        }

        //====================================================================
        /// <summary>
        /// Kelvin to deg C conversion
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal double KtoC(double value)
        {
            return value - 273.15;
        }

        //====================================================================
        /// <summary>
        /// Kelvin to deg F conversion
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal double KtoF(double value)
        {
            return (value - 273.15) * 9 / 5 + 32;
        }

        //====================================================================
        /// <summary>
        /// A list of the voltage ranges for the derived thermocouple type
        /// </summary>
        //====================================================================
        internal VoltageRange[] VoltageRanges
        {
            get { return m_tempRanges; }
        }
    }

    //========================================================================
    /// <summary>
    /// Class for type B thermocouple
    /// </summary>
    //========================================================================
    internal class TypeBThermocouple : Thermocouple
    {
        //========================================================================
        /// <summary>
        /// ctro - sets up the voltage ranges and the coefficients for each range
        /// </summary>
        //========================================================================
        internal TypeBThermocouple()
        {
            m_tempRanges = new VoltageRange[2]{ 
                                new VoltageRange(0.291, 2.431), 
                                new VoltageRange(2.431, 13.820)
                                };

            double[] coeffs;
            coeffs = new double[] { 
                        9.8423321E+01,
                        6.9971500E+02,
                       -8.4765304E+02,
                        1.0052644E+03,
                       -8.3345952E+02,
                        4.5508542E+02,
                       -1.5523037E+02,
                        2.9886750E+01,
                       -2.4742860E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] { 
                        2.1315071E+02,
                        2.8510504E+02,
                       -5.2742887E+01,
                        9.9160804E+00,
                       -1.2965303E+00,
                        1.1195870E-01,
                       -6.0625199E-03,
                        1.8661696E-04,
                       -2.4878585E-06
                        };
            m_coeffRanges.Add(coeffs);

            // Reverse coefficients
            coeffs = new double[] { 
	                     0.000000000000E+00,
	                    -0.246508183460E-03,
	                     0.590404211710E-05,
	                    -0.132579316360E-08,
	                     0.156682919010E-11,
	                    -0.169445292400E-14,
	                     0.629903470940E-18
                        };
            m_reverseCoeffRanges.Add(coeffs);
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double VoltageToTemperature(double mVolts)
        {
            if (mVolts >= m_tempRanges[0].LowerValue && mVolts < m_tempRanges[0].UpperValue)
                m_activeCoeffs = m_coeffRanges[0];
            else
                m_activeCoeffs = m_coeffRanges[1];

            return base.VoltageToTemperature(mVolts);
        }
    }

    //========================================================================
    /// <summary>
    /// Class for type E thermocouple
    /// </summary>
    //========================================================================
    internal class TypeEThermocouple : Thermocouple
    {
        //========================================================================
        /// <summary>
        /// ctro - sets up the voltage ranges and the coefficients for each range
        /// </summary>
        //========================================================================
        internal TypeEThermocouple()
        {
            m_tempRanges = new VoltageRange[]{ 
                                new VoltageRange(-8.825, 0.0), 
                                new VoltageRange(0.0, 76.373)
                                };

            double[] coeffs;
            coeffs = new double[] { 
                        0.0000000E+00,
                        1.6977288E+01,
                       -4.3514970E-01,
                       -1.5859697E-01,
                       -9.2502871E-02,
                       -2.6084314E-02,
                       -4.1360199E-03,
                       -3.4034030E-04,
                       -1.1564890E-05,
                        0.0000000E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] { 
                        0.0000000E+00,
                        1.7057035E+01,
                       -2.3301759E-01,
                        6.5435585E-03,
                       -7.3562749E-05,
                       -1.7896001E-06,
                        8.4036165E-08,
                       -1.3735879E-09,
                        1.0629823E-11,
                       -3.2447087E-14,
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] { 
	                     0.000000000000E+00,
	                     0.586655087100E-01,
	                     0.450322755820E-04,
	                     0.289084072120E-07,
	                    -0.330568966520E-09,
	                     0.650244032700E-12,
	                    -0.191974955040E-15,
	                    -0.125366004970E-17,
	                     0.214892175690E-20,
	                    -0.143880417820E-23,
	                     0.359608994810E-27
                        };
            m_reverseCoeffRanges.Add(coeffs);
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double VoltageToTemperature(double mVolts)
        {
            if (mVolts >= m_tempRanges[0].LowerValue && mVolts < m_tempRanges[0].UpperValue)
                m_activeCoeffs = m_coeffRanges[0];
            else
                m_activeCoeffs = m_coeffRanges[1];

            return base.VoltageToTemperature(mVolts);
        }
    }

    //========================================================================
    /// <summary>
    /// Class for type E thermocouple
    /// </summary>
    //========================================================================
    internal class TypeJThermocouple : Thermocouple
    {
        //========================================================================
        /// <summary>
        /// ctro - sets up the voltage ranges and the coefficients for each range
        /// </summary>
        //========================================================================
        internal TypeJThermocouple()
        {
            m_tempRanges = new VoltageRange[]{ 
                                new VoltageRange(-8.095, 0.0), 
                                new VoltageRange(0.0, 42.919),
                                new VoltageRange(42.919, 69.553)
                                };

            double[] coeffs;
            coeffs = new double[] { 
                        0.0000000E+00,
                        1.9528268E+01,
                       -1.2286185E+00,
                       -1.0752178E+00,
                       -5.9086933E-01,
                       -1.7256713E-01,
                       -2.8131513E-02,
                       -2.3963370E-03,
                       -8.3823321E-05
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                        0.000000E+00,
                        1.978425E+01,
                       -2.001204E-01,
                        1.036969E-02,
                       -2.549687E-04,
                        3.585153E-06,
                       -5.344285E-08,
                        5.099890E-10,
                        0.000000E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                       -3.11358187E+03,
                        3.00543684E+02,
                       -9.94773230E+00,
                        1.70276630E-01,
                       -1.43033468E-03,
                        4.73886084E-06,
                        0.00000000E+00,
                        0.00000000E+00,
                        0.00000000E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
	                     0.000000000000E+00,
	                     0.503811878150E-01,
	                     0.304758369300E-04,
	                    -0.856810657200E-07,
	                     0.132281952950E-09,
	                    -0.170529583370E-12,
	                     0.209480906970E-15,
	                    -0.125383953360E-18,
	                     0.156317256970E-22
                        };
            m_reverseCoeffRanges.Add(coeffs);
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double VoltageToTemperature(double mVolts)
        {
            if (mVolts >= m_tempRanges[0].LowerValue && mVolts < m_tempRanges[0].UpperValue)
                m_activeCoeffs = m_coeffRanges[0];
            else if (mVolts >= m_tempRanges[1].LowerValue && mVolts < m_tempRanges[1].UpperValue)
                m_activeCoeffs = m_coeffRanges[1];
            else
                m_activeCoeffs = m_coeffRanges[2];

            return base.VoltageToTemperature(mVolts);
        }
    }

    //========================================================================
    /// <summary>
    /// Class for type K thermocouple
    /// </summary>
    //========================================================================
    internal class TypeKThermocouple : Thermocouple
    {
        private double[] a = new double[] {
                                         	 0.118597600000E+00,
	                                        -0.118343200000E-03,
	                                         0.126968600000E+03
                                          };

        //========================================================================
        /// <summary>
        /// ctro - sets up the voltage ranges and the coefficients for each range
        /// </summary>
        //========================================================================
        internal TypeKThermocouple()
        {
            m_tempRanges = new VoltageRange[]{ 
                                new VoltageRange(-6.458, 0.0), 
                                //new VoltageRange(-5.891, 0.0), 
                                new VoltageRange(0.0, 20.644),
                                new VoltageRange(20.644, 54.886)
                                };

            double[] coeffs;
            coeffs = new double[] { 
                         0.0000000E+00,
                         2.5173462E+01,
                        -1.1662878E+00,
                        -1.0833638E+00,
                        -8.9773540E-01,
                        -3.7342377E-01,
                        -8.6632643E-02,
                        -1.0450598E-02,
                        -5.1920577E-04,
                         0.0000000E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                         0.000000E+00,
                         2.508355E+01,
                         7.860106E-02,
                        -2.503131E-01,
                         8.315270E-02,
                        -1.228034E-02,
                         9.804036E-04,
                        -4.413030E-05,
                         1.057734E-06,
                        -1.052755E-08
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                        -1.318058E+02,
                         4.830222E+01,
                        -1.646031E+00,
                         5.464731E-02,
                        -9.650715E-04,
                         8.802193E-06,
                        -3.110810E-08,
                         0.000000E+00,
                         0.000000E+00,
                         0.000000E+00
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
	                    -0.176004136860E-01,
	                     0.389212049750E-01,
	                     0.185587700320E-04,
	                    -0.994575928740E-07,
	                     0.318409457190E-09,
	                    -0.560728448890E-12,
	                     0.560750590590E-15,
	                    -0.320207200030E-18,
	                     0.971511471520E-22,
	                    -0.121047212750E-25
                         };
            m_reverseCoeffRanges.Add(coeffs);
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double VoltageToTemperature(double mVolts)
        {
            if (mVolts >= m_tempRanges[0].LowerValue && mVolts < m_tempRanges[0].UpperValue)
                m_activeCoeffs = m_coeffRanges[0];
            else if (mVolts >= m_tempRanges[1].LowerValue && mVolts < m_tempRanges[1].UpperValue)
                m_activeCoeffs = m_coeffRanges[1];
            else
                m_activeCoeffs = m_coeffRanges[2];

            return base.VoltageToTemperature(mVolts);
        }

        //====================================================================
        /// <summary>
        /// Converts temperature in deg C to milli-volts
        /// </summary>
        /// <param name="tempValue">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double TemperatureToVoltage(double tempValue)
        {
            double volts = 0.0;

            m_activeCoeffs = m_reverseCoeffRanges[0];

            double exp = a[0] * Math.Exp(a[1] * Math.Pow(tempValue - a[2], 2) );

            for (int i = 0; i < m_activeCoeffs.Length; i++)
                volts += m_activeCoeffs[i] * Math.Pow(tempValue, (double)i);

            return (volts + exp);
        }
    }

    //========================================================================
    /// <summary>
    /// Class for type N thermocouple
    /// </summary>
    //========================================================================
    internal class TypeNThermocouple : Thermocouple
    {
        //========================================================================
        /// <summary>
        /// ctro - sets up the voltage ranges and the coefficients for each range
        /// </summary>
        //========================================================================
        internal TypeNThermocouple()
        {
            m_tempRanges = new VoltageRange[]{ 
                                new VoltageRange(-3.990, 0.0), 
                                new VoltageRange(0.0, 20.613),
                                new VoltageRange(20.613, 47.513)
                                };

            double[] coeffs;
            coeffs = new double[] { 
                        0.0000000E+00,
                        3.8436847E+01,
                        1.1010485E+00,
                        5.2229312E+00,
                        7.2060525E+00,
                        5.8488586E+00,
                        2.7754916E+00,
                        7.7075166E-01,
                        1.1582665E-01,
                        7.3138868E-03
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                        0.00000E+00,
                        3.86896E+01,
                       -1.08267E+00,
                        4.70205E-02,
                       -2.12169E-06,
                       -1.17272E-04,
                        5.39280E-06,
                       -7.98156E-08,
                        0.00000E+00,
                        0.00000E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                        1.972485E+01,
                        3.300943E+01,
                       -3.915159E-01,
                        9.855391E-03,
                       -1.274371E-04,
                        7.767022E-07,
                        0.000000E+00,
                        0.000000E+00,
                        0.000000E+00,
                        0.000000E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
	                     0.000000000000E+00,
	                     0.259293946010E-01,
	                     0.157101418800E-04,
	                     0.438256272370E-07,
	                    -0.252611697940E-09,
	                     0.643118193390E-12,
	                    -0.100634715190E-14,
	                     0.997453389920E-18,
	                    -0.608632456070E-21,
	                     0.208492293390E-24,
	                    -0.306821961510E-28
                        };
            m_reverseCoeffRanges.Add(coeffs);
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double VoltageToTemperature(double mVolts)
        {
            if (mVolts >= m_tempRanges[0].LowerValue && mVolts < m_tempRanges[0].UpperValue)
                m_activeCoeffs = m_coeffRanges[0];
            else if (mVolts >= m_tempRanges[1].LowerValue && mVolts < m_tempRanges[1].UpperValue)
                m_activeCoeffs = m_coeffRanges[1];
            else
                m_activeCoeffs = m_coeffRanges[2];

            return base.VoltageToTemperature(mVolts);
        }

        internal override double TemperatureToVoltage(double tempValue)
        {
            m_activeCoeffs = m_reverseCoeffRanges[0];

            return base.TemperatureToVoltage(tempValue);
        }
    }

    //========================================================================
    /// <summary>
    /// Class for type R thermocouple
    /// </summary>
    //========================================================================
    internal class TypeRThermocouple : Thermocouple
    {
        //========================================================================
        /// <summary>
        /// ctro - sets up the voltage ranges and the coefficients for each range
        /// </summary>
        //========================================================================
        internal TypeRThermocouple()
        {
            m_tempRanges = new VoltageRange[]{ 
                                new VoltageRange(-0.226, 1.923), 
                                new VoltageRange(1.923, 13.228),
                                new VoltageRange(13.228, 19.739),
                                new VoltageRange(19.739, 21.103),
                                };

            double[] coeffs;
            coeffs = new double[] { 
                         0.0000000E+00,
                         1.8891380E+02,
                        -9.3835290E+01,
                         1.3068619E+02,
                        -2.2703580E+02,
                         3.5145659E+02,
                        -3.8953900E+02,
                         2.8239471E+02,
                        -1.2607281E+02,
                         3.1353611E+01,
                        -3.3187769E+00
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                         1.334584505E+01,
                         1.472644573E+02,
                        -1.844024844E+01,
                         4.031129726E+00,
                        -6.249428360E-01,
                         6.468412046E-02,
                        -4.458750426E-03,
                         1.994710149E-04,
                        -5.313401790E-06,
                         6.481976217E-08,
                         0.000000000E+00,
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                        -8.199599416E+01,
                         1.553962042E+02,
                        -8.342197663E+00,
                         4.279433549E-01,
                        -1.191577910E-02,
                         1.492290091E-04,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                         3.406177836E+04,
                        -7.023729171E+03,
                         5.582903813E+02,
                        -1.952394635E+01,
                         2.560740231E-01,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
	                     0.000000000000E+00,
	                     0.528961729765E-02,
	                     0.139166589782E-04,
	                    -0.238855693017E-07,
	                     0.356916001063E-10,
	                    -0.462347666298E-13,
	                     0.500777441034E-16,
	                    -0.373105886191E-19,
	                     0.157716482367E-22,
	                    -0.281038625251E-26
                         };
            m_reverseCoeffRanges.Add(coeffs);
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double VoltageToTemperature(double mVolts)
        {
            if (mVolts >= m_tempRanges[0].LowerValue && mVolts < m_tempRanges[0].UpperValue)
                m_activeCoeffs = m_coeffRanges[0];
            else if (mVolts >= m_tempRanges[1].LowerValue && mVolts < m_tempRanges[1].UpperValue)
                m_activeCoeffs = m_coeffRanges[1];
            else if (mVolts >= m_tempRanges[2].LowerValue && mVolts < m_tempRanges[2].UpperValue)
                m_activeCoeffs = m_coeffRanges[2];
            else
                m_activeCoeffs = m_coeffRanges[3];

            return base.VoltageToTemperature(mVolts);
        }
    }

    //========================================================================
    /// <summary>
    /// Class for type S thermocouple
    /// </summary>
    //========================================================================
    internal class TypeSThermocouple : Thermocouple
    {
        //========================================================================
        /// <summary>
        /// ctro - sets up the voltage ranges and the coefficients for each range
        /// </summary>
        //========================================================================
        internal TypeSThermocouple()
        {
            m_tempRanges = new VoltageRange[]{ 
                                new VoltageRange(-.235, 1.874), 
                                new VoltageRange(1.874, 11.950),
                                new VoltageRange(11.950, 17.536),
                                new VoltageRange(17.536, 18.693),
                                };

            double[] coeffs;
            coeffs = new double[] { 
                         0.00000000E+00,
                         1.84949460E+02,
                        -8.00504062E+01,
                         1.02237430E+02,
                        -1.52248592E+02,
                         1.88821343E+02,
                        -1.59085941E+02,
                         8.23027880E+01,
                        -2.34181944E+01,
                         2.79786260E+00
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                         1.291507177E+01,
                         1.466298863E+02,
                        -1.534713402E+01,
                         3.145945973E+00,
                        -4.163257839E-01,
                         3.187963771E-02,
                        -1.291637500E-03,
                         2.183475087E-05,
                        -1.447379511E-07,
                         8.211272125E-09
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                        -8.087801117E+01,
                         1.621573104E+02,
                        -8.536869453E+00,
                         4.719686976E-01,
                        -1.441693666E-02,
                         2.081618890E-04,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
                         5.333875126E+04,
                        -1.235892298E+04,
                         1.092657613E+03,
                        -4.265693686E+01,
                         6.247205420E-01,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00,
                         0.000000000E+00
                         };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] {
	                     0.000000000000E+00,
	                     0.540313308631E-02,
	                     0.125934289740E-04,
	                    -0.232477968689E-07,
	                     0.322028823036E-10,
	                    -0.331465196389E-13,
	                     0.255744251786E-16,
	                    -0.125068871393E-19,
	                     0.271443176145E-23
                         };
            m_reverseCoeffRanges.Add(coeffs);
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double VoltageToTemperature(double mVolts)
        {
            if (mVolts >= m_tempRanges[0].LowerValue && mVolts < m_tempRanges[0].UpperValue)
                m_activeCoeffs = m_coeffRanges[0];
            else if (mVolts >= m_tempRanges[1].LowerValue && mVolts < m_tempRanges[1].UpperValue)
                m_activeCoeffs = m_coeffRanges[1];
            else if (mVolts >= m_tempRanges[2].LowerValue && mVolts < m_tempRanges[2].UpperValue)
                m_activeCoeffs = m_coeffRanges[2];
            else
                m_activeCoeffs = m_coeffRanges[3];

            return base.VoltageToTemperature(mVolts);
        }
    }

    //========================================================================
    /// <summary>
    /// Class for type T thermocouple
    /// </summary>
    //========================================================================
    internal class TypeTThermocouple : Thermocouple
    {
        //========================================================================
        /// <summary>
        /// ctro - sets up the voltage ranges and the coefficients for each range
        /// </summary>
        //========================================================================
        internal TypeTThermocouple()
        {
            m_tempRanges = new VoltageRange[]{ 
                                new VoltageRange(-5.603, 0.0), 
                                new VoltageRange(0.0, 20.872)
                                };

            double[] coeffs;
            coeffs = new double[] { 
                         0.0000000E+00,
                         2.5949192E+01,
                        -2.1316967E-01,
                         7.9018692E-01,
                         4.2527777E-01,
                         1.3304473E-01,
                         2.0241446E-02,
                         1.2668171E-03,
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] { 
                         0.000000E+00,
                         2.592800E+01,
                        -7.602961E-01,
                         4.637791E-02,
                        -2.165394E-03,
                         6.048144E-05,
                        -7.293422E-07,
                         0.000000E+00
                        };
            m_coeffRanges.Add(coeffs);

            coeffs = new double[] { 
	                     0.000000000000E+00,
	                     0.387481063640E-01,
	                     0.332922278800E-04,
	                     0.206182434040E-06,
	                    -0.218822568460E-08,
	                     0.109968809280E-10,
	                    -0.308157587720E-13,
	                     0.454791352900E-16,
	                    -0.275129016730E-19
                        };
            m_reverseCoeffRanges.Add(coeffs);
        }

        //====================================================================
        /// <summary>
        /// Converts milli-volts to temperature in deg C
        /// </summary>
        /// <param name="mVolts">The value to convert</param>
        /// <returns>The converted value</returns>
        //====================================================================
        internal override double VoltageToTemperature(double mVolts)
        {
            if (mVolts >= m_tempRanges[0].LowerValue && mVolts < m_tempRanges[0].UpperValue)
                m_activeCoeffs = m_coeffRanges[0];
            else
                m_activeCoeffs = m_coeffRanges[1];

            return base.VoltageToTemperature(mVolts);
        }
    }
}
