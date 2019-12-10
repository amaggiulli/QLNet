﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_OptionletStripper : IDisposable
   {
      #region Initialize&Cleanup
      private SavedSettings backup;
#if NET452
      [TestInitialize]
      public void testInitialize()
      {
#else
      public T_OptionletStripper()
      {
#endif

         backup = new SavedSettings();
      }
#if NET452
      [TestCleanup]
#endif
      public void testCleanup()
      {
         Dispose();
      }
      public void Dispose()
      {
         backup.Dispose();
      }
      #endregion

      class CommonVars
      {
         // global data
         public Calendar calendar;
         public DayCounter dayCounter;

         public RelinkableHandle<YieldTermStructure> yieldTermStructure = new RelinkableHandle<YieldTermStructure>();

         public List<double> strikes;
         public List<Period> optionTenors;
         public Matrix termV;
         public List<double> atmTermV;
         public List<Handle<Quote>> atmTermVolHandle;

         public Handle<CapFloorTermVolCurve> capFloorVolCurve;
         public Handle<CapFloorTermVolCurve> flatTermVolCurve;

         public CapFloorTermVolSurface capFloorVolSurface;
         public CapFloorTermVolSurface flatTermVolSurface;

         public double accuracy;
         public double tolerance;

         public CommonVars()
         {
            accuracy = 1.0e-6;
            tolerance = 2.5e-8;
         }

         public void setTermStructure()
         {

            calendar = new TARGET();
            dayCounter = new Actual365Fixed();

            double flatFwdRate = 0.04;
            yieldTermStructure.linkTo(new FlatForward(0, calendar, flatFwdRate, dayCounter));
         }

         public void setFlatTermVolCurve()
         {
            setTermStructure();

            optionTenors = new InitializedList<Period>(10);
            for (int i = 0; i < optionTenors.Count; ++i)
               optionTenors[i] = new Period(i + 1, TimeUnit.Years);

            double flatVol = .18;

            List<Handle<Quote> >  curveVHandle = new InitializedList<Handle<Quote>>(optionTenors.Count);
            for (int i = 0; i < optionTenors.Count; ++i)
               curveVHandle[i] = new Handle<Quote>(new SimpleQuote(flatVol));

            flatTermVolCurve = new Handle<CapFloorTermVolCurve>(new CapFloorTermVolCurve(0, calendar, BusinessDayConvention.Following, optionTenors,
                                                                                         curveVHandle, dayCounter));

         }

         public void setFlatTermVolSurface()
         {

            setTermStructure();

            optionTenors = new InitializedList<Period>(10);
            for (int i = 0; i < optionTenors.Count; ++i)
               optionTenors[i] = new Period(i + 1, TimeUnit.Years);

            strikes = new InitializedList<double>(10);
            for (int j = 0; j < strikes.Count; ++j)
               strikes[j] = (double)(j + 1) / 100.0;

            double flatVol = .18;
            termV = new Matrix(optionTenors.Count, strikes.Count, flatVol);
            flatTermVolSurface = new CapFloorTermVolSurface(0, calendar, BusinessDayConvention.Following,
                                                            optionTenors, strikes, termV, dayCounter);
         }


         public void setCapFloorTermVolCurve()
         {

            setTermStructure();

            //atm cap volatility curve
            optionTenors = new List<Period>();
            optionTenors.Add(new Period(1, TimeUnit.Years));
            optionTenors.Add(new Period(18, TimeUnit.Months));
            optionTenors.Add(new Period(2, TimeUnit.Years));
            optionTenors.Add(new Period(3, TimeUnit.Years));
            optionTenors.Add(new Period(4, TimeUnit.Years));
            optionTenors.Add(new Period(5, TimeUnit.Years));
            optionTenors.Add(new Period(6, TimeUnit.Years));
            optionTenors.Add(new Period(7, TimeUnit.Years));
            optionTenors.Add(new Period(8, TimeUnit.Years));
            optionTenors.Add(new Period(9, TimeUnit.Years));
            optionTenors.Add(new Period(10, TimeUnit.Years));
            optionTenors.Add(new Period(12, TimeUnit.Years));
            optionTenors.Add(new Period(15, TimeUnit.Years));
            optionTenors.Add(new Period(20, TimeUnit.Years));
            optionTenors.Add(new Period(25, TimeUnit.Years));
            optionTenors.Add(new Period(30, TimeUnit.Years));

            //atm capfloor vols from mkt vol matrix using flat yield curve
            atmTermV = new List<double>();
            atmTermV.Add(0.090304);
            atmTermV.Add(0.12180);
            atmTermV.Add(0.13077);
            atmTermV.Add(0.14832);
            atmTermV.Add(0.15570);
            atmTermV.Add(0.15816);
            atmTermV.Add(0.15932);
            atmTermV.Add(0.16035);
            atmTermV.Add(0.15951);
            atmTermV.Add(0.15855);
            atmTermV.Add(0.15754);
            atmTermV.Add(0.15459);
            atmTermV.Add(0.15163);
            atmTermV.Add(0.14575);
            atmTermV.Add(0.14175);
            atmTermV.Add(0.13889);
            atmTermVolHandle = new InitializedList<Handle<Quote>>(optionTenors.Count);
            for (int i = 0; i < optionTenors.Count; ++i)
            {
               atmTermVolHandle[i] = new Handle<Quote>(new SimpleQuote(atmTermV[i]));
            }

            capFloorVolCurve = new Handle<CapFloorTermVolCurve>(new CapFloorTermVolCurve(0, calendar, BusinessDayConvention.Following,
                                                                                         optionTenors, atmTermVolHandle, dayCounter));

         }

         public void setCapFloorTermVolSurface()
         {
            setTermStructure();

            //cap volatility smile matrix
            optionTenors = new List<Period>();
            optionTenors.Add(new Period(1, TimeUnit.Years));
            optionTenors.Add(new Period(18, TimeUnit.Months));
            optionTenors.Add(new Period(2, TimeUnit.Years));
            optionTenors.Add(new Period(3, TimeUnit.Years));
            optionTenors.Add(new Period(4, TimeUnit.Years));
            optionTenors.Add(new Period(5, TimeUnit.Years));
            optionTenors.Add(new Period(6, TimeUnit.Years));
            optionTenors.Add(new Period(7, TimeUnit.Years));
            optionTenors.Add(new Period(8, TimeUnit.Years));
            optionTenors.Add(new Period(9, TimeUnit.Years));
            optionTenors.Add(new Period(10, TimeUnit.Years));
            optionTenors.Add(new Period(12, TimeUnit.Years));
            optionTenors.Add(new Period(15, TimeUnit.Years));
            optionTenors.Add(new Period(20, TimeUnit.Years));
            optionTenors.Add(new Period(25, TimeUnit.Years));
            optionTenors.Add(new Period(30, TimeUnit.Years));

            strikes = new List<double>();
            strikes.Add(0.015);
            strikes.Add(0.0175);
            strikes.Add(0.02);
            strikes.Add(0.0225);
            strikes.Add(0.025);
            strikes.Add(0.03);
            strikes.Add(0.035);
            strikes.Add(0.04);
            strikes.Add(0.05);
            strikes.Add(0.06);
            strikes.Add(0.07);
            strikes.Add(0.08);
            strikes.Add(0.1);

            termV = new Matrix(optionTenors.Count, strikes.Count);
            termV[0, 0] = 0.287;  termV[0, 1] = 0.274;  termV[0, 2] = 0.256;  termV[0, 3] = 0.245;  termV[0, 4] = 0.227;  termV[0, 5] = 0.148;  termV[0, 6] = 0.096;  termV[0, 7] = 0.09;   termV[0, 8] = 0.11;   termV[0, 9] = 0.139;  termV[0, 10] = 0.166;  termV[0, 11] = 0.19;   termV[0, 12] = 0.214;
            termV[1, 0] = 0.303;  termV[1, 1] = 0.258;  termV[1, 2] = 0.22;   termV[1, 3] = 0.203;  termV[1, 4] = 0.19;   termV[1, 5] = 0.153;  termV[1, 6] = 0.126;  termV[1, 7] = 0.118;  termV[1, 8] = 0.147;  termV[1, 9] = 0.165;  termV[1, 10] = 0.18;   termV[1, 11] = 0.192;  termV[1, 12] = 0.212;
            termV[2, 0] = 0.303;  termV[2, 1] = 0.257;  termV[2, 2] = 0.216;  termV[2, 3] = 0.196;  termV[2, 4] = 0.182;  termV[2, 5] = 0.154;  termV[2, 6] = 0.134;  termV[2, 7] = 0.127;  termV[2, 8] = 0.149;  termV[2, 9] = 0.166;  termV[2, 10] = 0.18;   termV[2, 11] = 0.192;  termV[2, 12] = 0.212;
            termV[3, 0] = 0.305;  termV[3, 1] = 0.266;  termV[3, 2] = 0.226;  termV[3, 3] = 0.203;  termV[3, 4] = 0.19;   termV[3, 5] = 0.167;  termV[3, 6] = 0.151;  termV[3, 7] = 0.144;  termV[3, 8] = 0.16;   termV[3, 9] = 0.172;  termV[3, 10] = 0.183;  termV[3, 11] = 0.193;  termV[3, 12] = 0.209;
            termV[4, 0] = 0.294;  termV[4, 1] = 0.261;  termV[4, 2] = 0.216;  termV[4, 3] = 0.201;  termV[4, 4] = 0.19;   termV[4, 5] = 0.171;  termV[4, 6] = 0.158;  termV[4, 7] = 0.151;  termV[4, 8] = 0.163;  termV[4, 9] = 0.172;  termV[4, 10] = 0.181;  termV[4, 11] = 0.188;  termV[4, 12] = 0.201;
            termV[5, 0] = 0.276;  termV[5, 1] = 0.248;  termV[5, 2] = 0.212;  termV[5, 3] = 0.199;  termV[5, 4] = 0.189;  termV[5, 5] = 0.172;  termV[5, 6] = 0.16;   termV[5, 7] = 0.155;  termV[5, 8] = 0.162;  termV[5, 9] = 0.17;   termV[5, 10] = 0.177;  termV[5, 11] = 0.183;  termV[5, 12] = 0.195;
            termV[6, 0] = 0.26;   termV[6, 1] = 0.237;  termV[6, 2] = 0.21;   termV[6, 3] = 0.198;  termV[6, 4] = 0.188;  termV[6, 5] = 0.172;  termV[6, 6] = 0.161;  termV[6, 7] = 0.156;  termV[6, 8] = 0.161;  termV[6, 9] = 0.167;  termV[6, 10] = 0.173;  termV[6, 11] = 0.179;  termV[6, 12] = 0.19;
            termV[7, 0] = 0.25;   termV[7, 1] = 0.231;  termV[7, 2] = 0.208;  termV[7, 3] = 0.196;  termV[7, 4] = 0.187;  termV[7, 5] = 0.172;  termV[7, 6] = 0.162;  termV[7, 7] = 0.156;  termV[7, 8] = 0.16;   termV[7, 9] = 0.165;  termV[7, 10] = 0.17;   termV[7, 11] = 0.175;  termV[7, 12] = 0.185;
            termV[8, 0] = 0.244;  termV[8, 1] = 0.226;  termV[8, 2] = 0.206;  termV[8, 3] = 0.195;  termV[8, 4] = 0.186;  termV[8, 5] = 0.171;  termV[8, 6] = 0.161;  termV[8, 7] = 0.156;  termV[8, 8] = 0.158;  termV[8, 9] = 0.162;  termV[8, 10] = 0.166;  termV[8, 11] = 0.171;  termV[8, 12] = 0.18;
            termV[9, 0] = 0.239;  termV[9, 1] = 0.222;  termV[9, 2] = 0.204;  termV[9, 3] = 0.193;  termV[9, 4] = 0.185;  termV[9, 5] = 0.17;   termV[9, 6] = 0.16;   termV[9, 7] = 0.155;  termV[9, 8] = 0.156;  termV[9, 9] = 0.159;  termV[9, 10] = 0.163;  termV[9, 11] = 0.168;  termV[9, 12] = 0.177;
            termV[10, 0] = 0.235; termV[10, 1] = 0.219; termV[10, 2] = 0.202; termV[10, 3] = 0.192; termV[10, 4] = 0.183; termV[10, 5] = 0.169; termV[10, 6] = 0.159; termV[10, 7] = 0.154; termV[10, 8] = 0.154; termV[10, 9] = 0.156; termV[10, 10] = 0.16;  termV[10, 11] = 0.164; termV[10, 12] = 0.173;
            termV[11, 0] = 0.227; termV[11, 1] = 0.212; termV[11, 2] = 0.197; termV[11, 3] = 0.187; termV[11, 4] = 0.179; termV[11, 5] = 0.166; termV[11, 6] = 0.156; termV[11, 7] = 0.151; termV[11, 8] = 0.149; termV[11, 9] = 0.15;  termV[11, 10] = 0.153; termV[11, 11] = 0.157; termV[11, 12] = 0.165;
            termV[12, 0] = 0.22;  termV[12, 1] = 0.206; termV[12, 2] = 0.192; termV[12, 3] = 0.183; termV[12, 4] = 0.175; termV[12, 5] = 0.162; termV[12, 6] = 0.153; termV[12, 7] = 0.147; termV[12, 8] = 0.144; termV[12, 9] = 0.144; termV[12, 10] = 0.147; termV[12, 11] = 0.151; termV[12, 12] = 0.158;
            termV[13, 0] = 0.211; termV[13, 1] = 0.197; termV[13, 2] = 0.185; termV[13, 3] = 0.176; termV[13, 4] = 0.168; termV[13, 5] = 0.156; termV[13, 6] = 0.147; termV[13, 7] = 0.142; termV[13, 8] = 0.138; termV[13, 9] = 0.138; termV[13, 10] = 0.14;  termV[13, 11] = 0.144; termV[13, 12] = 0.151;
            termV[14, 0] = 0.204; termV[14, 1] = 0.192; termV[14, 2] = 0.18;  termV[14, 3] = 0.171; termV[14, 4] = 0.164; termV[14, 5] = 0.152; termV[14, 6] = 0.143; termV[14, 7] = 0.138; termV[14, 8] = 0.134; termV[14, 9] = 0.134; termV[14, 10] = 0.137; termV[14, 11] = 0.14;  termV[14, 12] = 0.148;
            termV[15, 0] = 0.2;   termV[15, 1] = 0.187; termV[15, 2] = 0.176; termV[15, 3] = 0.167; termV[15, 4] = 0.16;  termV[15, 5] = 0.148; termV[15, 6] = 0.14;  termV[15, 7] = 0.135; termV[15, 8] = 0.131; termV[15, 9] = 0.132; termV[15, 10] = 0.135; termV[15, 11] = 0.139; termV[15, 12] = 0.146;

            capFloorVolSurface = new CapFloorTermVolSurface(0, calendar, BusinessDayConvention.Following, optionTenors, strikes,
                                                            termV, dayCounter);
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFlatTermVolatilityStripping1()
      {
         // Testing forward/forward vol stripping from flat term vol
         // surface using OptionletStripper1 class...

         CommonVars vars = new CommonVars();
         Settings.setEvaluationDate(new Date(28, Month.October, 2013));

         vars.setFlatTermVolSurface();

         IborIndex iborIndex = new Euribor6M(vars.yieldTermStructure);

         OptionletStripper optionletStripper1 = new OptionletStripper1(vars.flatTermVolSurface,
                                                                       iborIndex, null, vars.accuracy);

         StrippedOptionletAdapter strippedOptionletAdapter = new StrippedOptionletAdapter(optionletStripper1);

         Handle<OptionletVolatilityStructure> vol = new Handle<OptionletVolatilityStructure>(strippedOptionletAdapter);

         vol.link.enableExtrapolation();

         BlackCapFloorEngine strippedVolEngine = new BlackCapFloorEngine(vars.yieldTermStructure, vol);

         CapFloor cap;
         for (int tenorIndex = 0; tenorIndex < vars.optionTenors.Count; ++tenorIndex)
         {
            for (int strikeIndex = 0; strikeIndex < vars.strikes.Count; ++strikeIndex)
            {
               cap = new MakeCapFloor(CapFloorType.Cap, vars.optionTenors[tenorIndex], iborIndex,
                                      vars.strikes[strikeIndex], new Period(0, TimeUnit.Days))
               .withPricingEngine(strippedVolEngine);

               double priceFromStrippedVolatility = cap.NPV();

               IPricingEngine blackCapFloorEngineConstantVolatility = new BlackCapFloorEngine(vars.yieldTermStructure,
                                                                                              vars.termV[tenorIndex, strikeIndex]);

               cap.setPricingEngine(blackCapFloorEngineConstantVolatility);
               double priceFromConstantVolatility = cap.NPV();

               double error = Math.Abs(priceFromStrippedVolatility - priceFromConstantVolatility);
               if (error > vars.tolerance)
                  QAssert.Fail("\noption tenor:       " + vars.optionTenors[tenorIndex] +
                               "\nstrike:             " + vars.strikes[strikeIndex] +
                               "\nstripped vol price: " + priceFromStrippedVolatility +
                               "\nconstant vol price: " + priceFromConstantVolatility +
                               "\nerror:              " + error +
                               "\ntolerance:          " + vars.tolerance);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testTermVolatilityStripping1()
      {
         // Testing forward/forward vol stripping from non-flat term
         // vol surface using OptionletStripper1 class

         CommonVars vars = new CommonVars();
         Settings.setEvaluationDate(new Date(28, Month.October, 2013));

         vars.setCapFloorTermVolSurface();

         IborIndex iborIndex = new Euribor6M(vars.yieldTermStructure);

         OptionletStripper optionletStripper1 = new OptionletStripper1(vars.capFloorVolSurface, iborIndex, null, vars.accuracy);

         StrippedOptionletAdapter strippedOptionletAdapter = new StrippedOptionletAdapter(optionletStripper1);

         Handle<OptionletVolatilityStructure> vol = new Handle<OptionletVolatilityStructure>(strippedOptionletAdapter);

         vol.link.enableExtrapolation();

         BlackCapFloorEngine strippedVolEngine = new BlackCapFloorEngine(vars.yieldTermStructure, vol);

         CapFloor cap;

         for (int tenorIndex = 0; tenorIndex < vars.optionTenors.Count; ++tenorIndex)
         {
            for (int strikeIndex = 0; strikeIndex < vars.strikes.Count; ++strikeIndex)
            {
               cap = new MakeCapFloor(CapFloorType.Cap, vars.optionTenors[tenorIndex], iborIndex, vars.strikes[strikeIndex],
                                      new Period(0, TimeUnit.Days))
               .withPricingEngine(strippedVolEngine);

               double priceFromStrippedVolatility = cap.NPV();

               IPricingEngine blackCapFloorEngineConstantVolatility = new BlackCapFloorEngine(vars.yieldTermStructure,
                                                                                              vars.termV[tenorIndex, strikeIndex]);

               cap.setPricingEngine(blackCapFloorEngineConstantVolatility);
               double priceFromConstantVolatility = cap.NPV();

               double error = Math.Abs(priceFromStrippedVolatility - priceFromConstantVolatility);
               if (error > vars.tolerance)
                  QAssert.Fail("\noption tenor:       " + vars.optionTenors[tenorIndex] +
                               "\nstrike:             " + vars.strikes[strikeIndex] +
                               "\nstripped vol price: " + priceFromStrippedVolatility +
                               "\nconstant vol price: " + priceFromConstantVolatility +
                               "\nerror:              " + error +
                               "\ntolerance:          " + vars.tolerance);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFlatTermVolatilityStripping2()
      {
         // Testing forward/forward vol stripping from flat term vol
         // surface using OptionletStripper2 class...");

         CommonVars vars = new CommonVars();
         Settings.setEvaluationDate(Date.Today);

         vars.setFlatTermVolCurve();
         vars.setFlatTermVolSurface();

         IborIndex iborIndex = new Euribor6M(vars.yieldTermStructure);

         // optionletstripper1
         OptionletStripper1 optionletStripper1 = new OptionletStripper1(vars.flatTermVolSurface,
                                                                        iborIndex, null, vars.accuracy);

         StrippedOptionletAdapter strippedOptionletAdapter1 = new StrippedOptionletAdapter(optionletStripper1);

         Handle<OptionletVolatilityStructure> vol1 = new Handle<OptionletVolatilityStructure>(strippedOptionletAdapter1);

         vol1.link.enableExtrapolation();

         // optionletstripper2
         OptionletStripper optionletStripper2 = new OptionletStripper2(optionletStripper1, vars.flatTermVolCurve);

         StrippedOptionletAdapter strippedOptionletAdapter2 = new StrippedOptionletAdapter(optionletStripper2);

         Handle<OptionletVolatilityStructure> vol2 = new Handle<OptionletVolatilityStructure>(strippedOptionletAdapter2);

         vol2.link.enableExtrapolation();

         // consistency check: diff(stripped vol1-stripped vol2)
         for (int strikeIndex = 0; strikeIndex < vars.strikes.Count; ++strikeIndex)
         {
            for (int tenorIndex = 0; tenorIndex < vars.optionTenors.Count; ++tenorIndex)
            {

               double strippedVol1 = vol1.link.volatility(vars.optionTenors[tenorIndex], vars.strikes[strikeIndex], true);

               double strippedVol2 = vol2.link.volatility(vars.optionTenors[tenorIndex], vars.strikes[strikeIndex], true);

               // vol from flat vol surface (for comparison only)
               double flatVol = vars.flatTermVolSurface.volatility(vars.optionTenors[tenorIndex], vars.strikes[strikeIndex], true);

               double error = Math.Abs(strippedVol1 - strippedVol2);
               if (error > vars.tolerance)
                  QAssert.Fail("\noption tenor:  " + vars.optionTenors[tenorIndex] +
                               "\nstrike:        " + vars.strikes[strikeIndex] +
                               "\nstripped vol1: " + strippedVol1 +
                               "\nstripped vol2: " + strippedVol2 +
                               "\nflat vol:      " + flatVol +
                               "\nerror:         " + error +
                               "\ntolerance:     " + vars.tolerance);
            }
         }

      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testTermVolatilityStripping2()
      {
         // Testing forward/forward vol stripping from non-flat term vol "
         // surface using OptionletStripper2 class...");

         CommonVars vars = new CommonVars();
         Settings.setEvaluationDate(Date.Today);

         vars.setCapFloorTermVolCurve();
         vars.setCapFloorTermVolSurface();

         IborIndex iborIndex = new Euribor6M(vars.yieldTermStructure);

         // optionletstripper1
         OptionletStripper1 optionletStripper1 = new OptionletStripper1(vars.capFloorVolSurface, iborIndex, null, vars.accuracy);
         StrippedOptionletAdapter strippedOptionletAdapter1 = new StrippedOptionletAdapter(optionletStripper1);
         Handle<OptionletVolatilityStructure> vol1 = new Handle<OptionletVolatilityStructure>(strippedOptionletAdapter1);
         vol1.link.enableExtrapolation();

         // optionletstripper2
         OptionletStripper optionletStripper2 = new OptionletStripper2(optionletStripper1, vars.capFloorVolCurve);
         StrippedOptionletAdapter strippedOptionletAdapter2 = new StrippedOptionletAdapter(optionletStripper2);
         Handle<OptionletVolatilityStructure> vol2 = new Handle<OptionletVolatilityStructure>(strippedOptionletAdapter2);
         vol2.link.enableExtrapolation();

         // consistency check: diff(stripped vol1-stripped vol2)
         for (int strikeIndex = 0; strikeIndex < vars.strikes.Count; ++strikeIndex)
         {
            for (int tenorIndex = 0; tenorIndex < vars.optionTenors.Count; ++tenorIndex)
            {
               double strippedVol1 = vol1.link.volatility(vars.optionTenors[tenorIndex], vars.strikes[strikeIndex], true);
               double strippedVol2 = vol2.link.volatility(vars.optionTenors[tenorIndex], vars.strikes[strikeIndex], true);

               // vol from flat vol surface (for comparison only)
               double flatVol = vars.capFloorVolSurface.volatility(vars.optionTenors[tenorIndex], vars.strikes[strikeIndex], true);

               double error = Math.Abs(strippedVol1 - strippedVol2);
               if (error > vars.tolerance)
                  QAssert.Fail("\noption tenor:  " + vars.optionTenors[tenorIndex] +
                               "\nstrike:        " + (vars.strikes[strikeIndex]) +
                               "\nstripped vol1: " + (strippedVol1) +
                               "\nstripped vol2: " + (strippedVol2) +
                               "\nflat vol:      " + (flatVol) +
                               "\nerror:         " + (error) +
                               "\ntolerance:     " + (vars.tolerance));
            }
         }
      }
   }
}
