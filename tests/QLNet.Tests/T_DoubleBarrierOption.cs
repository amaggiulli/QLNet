//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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
#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;
using System;
using System.Collections.Generic;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_DoubleBarrierOption
   {
      public void REPORT_FAILURE(string greekName, DoubleBarrier.Type barrierType, double barrierlo, double barrierhi,
                                 StrikedTypePayoff payoff, Exercise exercise, double s, double q,
                                 double r, Date today, double v, double expected, double calculated, double error,
                                 double tolerance)
      {
         QAssert.Fail(barrierType + " " + exercise + " "
                      + payoff.optionType() + " option with "
                      + payoff + " payoff:\n"
                      + "    underlying value: " + s + "\n"
                      + "    strike:           " + payoff.strike() + "\n"
                      + "    barrier low:      " + barrierlo + "\n"
                      + "    barrier high:     " + barrierhi + "\n"
                      + "    dividend yield:   " + q + "\n"
                      + "    risk-free rate:   " + r + "\n"
                      + "    reference date:   " + today + "\n"
                      + "    maturity:         " + exercise.lastDate() + "\n"
                      + "    volatility:       " + v + "\n\n"
                      + "    expected " + greekName + ":   " + expected + "\n"
                      + "    calculated " + greekName + ": " + calculated + "\n"
                      + "    error:            " + error + "\n"
                      + "    tolerance:        " + tolerance);
      }
      public void REPORT_FAILURE_VANNAVOLGA(string greekName, DoubleBarrier.Type barrierType,
                                            double barrier1, double barrier2, double rebate,
                                            StrikedTypePayoff payoff, Exercise exercise, double s, double q,
                                            double r, Date today, double vol25Put, double atmVol, double vol25Call, double v,
                                            double expected, double calculated, double error, double tolerance)
      {
         QAssert.Fail("Double Barrier Option " + barrierType + " " + exercise + " "
                      + payoff.optionType() + " option with "
                      + payoff + " payoff:\n"
                      + "    underlying value: " + s + "\n"
                      + "    strike:           " + payoff.strike() + "\n"
                      + "    barrier 1:        " + barrier1 + "\n"
                      + "    barrier 2:        " + barrier2 + "\n"
                      + "    rebate :          " + rebate + "\n"
                      + "    dividend yield:   " + q + "\n"
                      + "    risk-free rate:   " + r + "\n"
                      + "    reference date:   " + today + "\n"
                      + "    maturity:         " + exercise.lastDate() + "\n"
                      + "    25PutVol:         " + +(vol25Put) + "\n"
                      + "    atmVol:           " + (atmVol) + "\n"
                      + "    25CallVol:        " + (vol25Call) + "\n"
                      + "    volatility:       " + v + "\n\n"
                      + "    expected " + greekName + ":   " + expected + "\n"
                      + "    calculated " + greekName + ": " + calculated + "\n"
                      + "    error:            " + error + "\n"
                      + "    tolerance:        " + tolerance);
      }

      private class NewBarrierOptionData
      {
         public DoubleBarrier.Type barrierType;
         public double barrierlo;
         public double barrierhi;
         public Option.Type type;
         public Exercise.Type exType;
         public double strike;
         public double s;        // spot
         public double q;        // dividend
         public double r;        // risk-free rate
         public double t;        // time to maturity
         public double v;  // volatility
         public double result;   // result
         public double tol;      // tolerance

         public NewBarrierOptionData(DoubleBarrier.Type barrierType, double barrierlo, double barrierhi, Option.Type type, Exercise.Type exType, double strike, double s, double q, double r, double t, double v, double result, double tol)
         {
            this.barrierType = barrierType;
            this.barrierlo = barrierlo;
            this.barrierhi = barrierhi;
            this.type = type;
            this.exType = exType;
            this.strike = strike;
            this.s = s;
            this.q = q;
            this.r = r;
            this.t = t;
            this.v = v;
            this.result = result;
            this.tol = tol;
         }
      }

      private class DoubleBarrierFxOptionData
      {
         public DoubleBarrier.Type barrierType;
         public double barrier1;
         public double barrier2;
         public double rebate;
         public Option.Type type;
         public double strike;
         public double s;                 // spot
         public double q;                 // dividend
         public double r;                 // risk-free rate
         public double t;                 // time to maturity
         public double vol25Put;          // 25 delta put vol
         public double volAtm;            // atm vol
         public double vol25Call;         // 25 delta call vol
         public double v;                 // volatility at strike
         public double result;            // result
         public double tol;               // tolerance

         public DoubleBarrierFxOptionData(DoubleBarrier.Type barrierType, double barrier1, double barrier2, double rebate, Option.Type type, double strike, double s, double q, double r, double t, double vol25Put, double volAtm, double vol25Call, double v, double result, double tol)
         {
            this.barrierType = barrierType;
            this.barrier1 = barrier1;
            this.barrier2 = barrier2;
            this.rebate = rebate;
            this.type = type;
            this.strike = strike;
            this.s = s;
            this.q = q;
            this.r = r;
            this.t = t;
            this.vol25Put = vol25Put;
            this.volAtm = volAtm;
            this.vol25Call = vol25Call;
            this.v = v;
            this.result = result;
            this.tol = tol;
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testEuropeanHaugValues()
      {
         // Testing double barrier european options against Haug's values

         Exercise.Type european = Exercise.Type.European;
         NewBarrierOptionData[] values =
         {
            /* The data below are from
               "The complete guide to option pricing formulas 2nd Ed",E.G. Haug, McGraw-Hill, p.156 and following.

               Note:
               The book uses b instead of q (q=r-b)
            */
            //           BarrierType, barr.lo,  barr.hi,         type, exercise,strk,     s,   q,   r,    t,    v,  result, tol
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  4.3515, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  6.1644, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  7.0373, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  6.9853, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  7.9336, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  6.5088, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  4.3505, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  5.8500, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  5.7726, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  6.8082, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  6.3383, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  4.3841, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  4.3139, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  4.8293, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  3.7765, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  5.9697, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  4.0004, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  2.2563, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  3.7516, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  2.6387, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  1.4903, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  3.5805, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  1.5098, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  0.5635, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  1.2055, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  0.3098, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  0.0477, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  0.5537, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  0.0441, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  0.0011, 1.0e-4),

            //           BarrierType, barr.lo,  barr.hi,         type, exercise,strk,     s,   q,   r,    t,    v,  result, tol
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  1.8825, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  3.7855, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  5.7191, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  2.1374, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  4.7033, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   50.0,    150.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  7.1683, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  1.8825, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  3.7845, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  5.6060, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  2.1374, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  4.6236, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   60.0,    140.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  6.1062, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  1.8825, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  3.7014, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  4.6472, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  2.1325, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  3.8944, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   70.0,    130.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  3.5868, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  1.8600, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  2.6866, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  2.0719, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  1.8883, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  1.7851, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   80.0,    120.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  0.8244, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  0.9473, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  0.3449, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  0.0578, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  0.4555, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  0.0491, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockOut,   90.0,    110.0,  Option.Type.Put, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  0.0013, 1.0e-4),

            //           BarrierType, barr.lo,  barr.hi,         type,  strk,     s,   q,   r,    t,    v,  result, tol
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  0.0000, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  0.0900, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  1.1537, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  0.0292, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  1.6487, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    50.0,    150.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  5.7321, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  0.0010, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  0.4045, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  2.4184, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  0.2062, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  3.2439, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    60.0,    140.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  7.8569, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  0.0376, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  1.4252, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  4.4145, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  1.0447, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  5.5818, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    70.0,    130.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35,  9.9846, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  0.5999, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  3.6158, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  6.7007, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  3.4340, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  8.0724, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    80.0,    120.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35, 11.6774, 1.0e-4),

            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.15,  3.1460, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.25,  5.9447, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.25, 0.35,  8.1432, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.15,  6.4608, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.25,  9.5382, 1.0e-4),
            new NewBarrierOptionData(DoubleBarrier.Type.KnockIn,    90.0,    110.0, Option.Type.Call, european, 100, 100.0, 0.0, 0.1, 0.50, 0.35, 12.2398, 1.0e-4),

         };

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(0.0);
         SimpleQuote qRate = new SimpleQuote(0.0);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.0);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.0);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         for (int i = 0; i < values.Length; i++)
         {
            Date exDate = today + (int)(values[i].t * 360 + 0.5);
            Exercise exercise = new EuropeanExercise(exDate);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, values[i].strike);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
               new Handle<Quote>(spot),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));

            DoubleBarrierOption opt = new DoubleBarrierOption(values[i].barrierType, values[i].barrierlo,
                                                              values[i].barrierhi, 0,  // no rebate
                                                              payoff, exercise);

            // Ikeda/Kunitomo engine
            IPricingEngine engine = new AnalyticDoubleBarrierEngine(stochProcess);
            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double expected = values[i].result;
            double error = Math.Abs(calculated - expected);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("Ikeda/Kunitomo value", values[i].barrierType, values[i].barrierlo,
                              values[i].barrierhi, payoff, exercise, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              expected, calculated, error, values[i].tol);
            }

            // Wulin Suo/Yong Wang engine
            engine = new WulinYongDoubleBarrierEngine(stochProcess);
            opt.setPricingEngine(engine);

            calculated = opt.NPV();
            expected = values[i].result;
            error = Math.Abs(calculated - expected);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("Wulin/Yong value", values[i].barrierType, values[i].barrierlo,
                              values[i].barrierhi, payoff, exercise, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              expected, calculated, error, values[i].tol);
            }

         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testVannaVolgaDoubleBarrierValues()
      {
         // Testing double-barrier FX options against Vanna/Volga values
         SavedSettings backup = new SavedSettings();

         DoubleBarrierFxOptionData[] values =
         {
            //                             BarrierType,                    barr.1, barr.2, rebate,         type,    strike,          s,         q,         r,  t, vol25Put,    volAtm,vol25Call,      vol,    result,   tol
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Call,   1.13321,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.11638,   0.14413, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Call,   1.22687,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.10088,   0.07456, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Call,   1.31179,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.08925,   0.02710, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Call,   1.38843,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.08463,   0.00569, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Call,   1.46047,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.08412,   0.00013, 1.0e-4),

            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Put,   1.13321,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.11638,    0.00017, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Put,   1.22687,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.10088,    0.00353, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Put,   1.31179,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.08925,    0.02221, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Put,   1.38843,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.08463,    0.06049, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.1,    1.5,    0.0, Option.Type.Put,   1.46047,    1.30265, 0.0003541, 0.0033871, 1.0, 0.10087,   0.08925, 0.08463,   0.08412,    0.11103, 1.0e-4),

            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Call,   1.06145,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.12511,   0.19981, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Call,   1.19545,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.10890,   0.10389, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Call,   1.32238,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.09444,   0.03555, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Call,   1.44298,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.09197,   0.00634, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Call,   1.56345,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.09261,   0.00000, 1.0e-4),

            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Put,   1.06145,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.12511,    0.00000, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Put,   1.19545,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.10890,    0.00436, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Put,   1.32238,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.09444,    0.03173, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Put,   1.44298,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.09197,    0.09346, 1.0e-4),
            new DoubleBarrierFxOptionData(DoubleBarrier.Type.KnockOut,    1.0,    1.6,    0.0, Option.Type.Put,   1.56345,    1.30265, 0.0009418, 0.0039788, 2.0, 0.10891,   0.09525, 0.09197,   0.09261,    0.17704, 1.0e-4),

         };

         DayCounter dc = new Actual360();
         Date today = new Date(05, Month.Mar, 2013);
         Settings.setEvaluationDate(today);

         SimpleQuote spot = new SimpleQuote(0.0);
         SimpleQuote qRate = new SimpleQuote(0.0);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.0);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol25Put = new SimpleQuote(0.0);
         SimpleQuote volAtm = new SimpleQuote(0.0);
         SimpleQuote vol25Call = new SimpleQuote(0.0);

         for (int i = 0; i < values.Length; i++)
         {
            for (int j = 0; j <= 1; j++)
            {
               DoubleBarrier.Type barrierType = (DoubleBarrier.Type) j ;
               spot.setValue(values[i].s);
               qRate.setValue(values[i].q);
               rRate.setValue(values[i].r);
               vol25Put.setValue(values[i].vol25Put);
               volAtm.setValue(values[i].volAtm);
               vol25Call.setValue(values[i].vol25Call);

               StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, values[i].strike);

               Date exDate = today + (int)(values[i].t * 365 + 0.5);
               Exercise exercise = new EuropeanExercise(exDate);

               Handle<DeltaVolQuote> volAtmQuote = new Handle<DeltaVolQuote>(
                  new DeltaVolQuote(new Handle<Quote>(volAtm), DeltaVolQuote.DeltaType.Fwd, values[i].t,
                                    DeltaVolQuote.AtmType.AtmDeltaNeutral));

               //always delta neutral atm
               Handle<DeltaVolQuote> vol25PutQuote = new Handle<DeltaVolQuote>(new DeltaVolQuote(-0.25,
                                                                               new Handle<Quote>(vol25Put), values[i].t, DeltaVolQuote.DeltaType.Fwd));

               Handle<DeltaVolQuote> vol25CallQuote = new Handle<DeltaVolQuote>(new DeltaVolQuote(0.25,
                                                                                new Handle<Quote>(vol25Call), values[i].t, DeltaVolQuote.DeltaType.Fwd));

               DoubleBarrierOption doubleBarrierOption = new DoubleBarrierOption(barrierType,
                                                                                 values[i].barrier1, values[i].barrier2, values[i].rebate, payoff, exercise);

               double bsVanillaPrice = Utils.blackFormula(values[i].type, values[i].strike,
                                                          spot.value() * qTS.discount(values[i].t) / rTS.discount(values[i].t),
                                                          values[i].v * Math.Sqrt(values[i].t), rTS.discount(values[i].t));

               IPricingEngine vannaVolgaEngine;

               vannaVolgaEngine = new VannaVolgaDoubleBarrierEngine(volAtmQuote, vol25PutQuote, vol25CallQuote,
                                                                    new Handle<Quote>(spot),
                                                                    new Handle<YieldTermStructure>(rTS),
                                                                    new Handle<YieldTermStructure>(qTS),
                                                                    (process, series) => new WulinYongDoubleBarrierEngine(process, series),
               true,
               bsVanillaPrice);
               doubleBarrierOption.setPricingEngine(vannaVolgaEngine);

               double expected = 0;
               if (barrierType == DoubleBarrier.Type.KnockOut)
                  expected = values[i].result;
               else if (barrierType == DoubleBarrier.Type.KnockIn)
                  expected = (bsVanillaPrice - values[i].result);

               double calculated = doubleBarrierOption.NPV();
               double error = Math.Abs(calculated - expected);
               if (error > values[i].tol)
               {
                  REPORT_FAILURE_VANNAVOLGA("value", values[i].barrierType,
                                            values[i].barrier1, values[i].barrier2,
                                            values[i].rebate, payoff, exercise, values[i].s,
                                            values[i].q, values[i].r, today, values[i].vol25Put,
                                            values[i].volAtm, values[i].vol25Call, values[i].v,
                                            expected, calculated, error, values[i].tol);
               }

               vannaVolgaEngine = new VannaVolgaDoubleBarrierEngine(volAtmQuote, vol25PutQuote, vol25CallQuote,
                                                                    new Handle<Quote>(spot),
                                                                    new Handle<YieldTermStructure>(rTS),
                                                                    new Handle<YieldTermStructure>(qTS),
                                                                    (process, series) => new AnalyticDoubleBarrierEngine(process, series),
               true,
               bsVanillaPrice);
               doubleBarrierOption.setPricingEngine(vannaVolgaEngine);

               calculated = doubleBarrierOption.NPV();
               error = Math.Abs(calculated - expected);
               double maxtol = 5.0e-3; // different engines have somewhat different results
               if (error > maxtol)
               {
                  REPORT_FAILURE_VANNAVOLGA("value", values[i].barrierType,
                                            values[i].barrier1, values[i].barrier2,
                                            values[i].rebate, payoff, exercise, values[i].s,
                                            values[i].q, values[i].r, today, values[i].vol25Put,
                                            values[i].volAtm, values[i].vol25Call, values[i].v,
                                            expected, calculated, error, values[i].tol);
               }
            }
         }
      }
   }
}
