//  Copyright (C) 2015 Thema Consulting SA
//  Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
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
   public class T_DoubleBinaryOption
   {
      private void REPORT_FAILURE(string greekName,
                                  StrikedTypePayoff payoff,
                                  Exercise exercise,
                                  DoubleBarrier.Type barrierType,
                                  double barrier_lo,
                                  double barrier_hi,
                                  double s,
                                  double q,
                                  double r,
                                  Date today,
                                  double v,
                                  double expected,
                                  double calculated,
                                  double error,
                                  double tolerance)
      {
         QAssert.Fail(payoff.optionType() + " option with "
                      + barrierType + " barrier type:\n"
                      + "    barrier_lo:          " + barrier_lo + "\n"
                      + "    barrier_hi:          " + barrier_hi + "\n"
                      + payoff + " payoff:\n"
                      + exercise + " "
                      + payoff.optionType()
                      + "    spot value: " + s + "\n"
                      + "    strike:           " + payoff.strike() + "\n"
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

      private struct DoubleBinaryOptionData
      {
         public DoubleBarrier.Type barrierType;
         public double barrier_lo;
         public double barrier_hi;
         public double cash;     // cash payoff for cash-or-nothing
         public double s;        // spot
         public double q;        // dividend
         public double r;        // risk-free rate
         public double t;        // time to maturity
         public double v;        // volatility
         public double result;   // expected result
         public double tol;      // tolerance

         public DoubleBinaryOptionData(DoubleBarrier.Type barrierType, double barrier_lo, double barrier_hi, double cash,
                                       double s, double q, double r, double t, double v, double result, double tol) : this()
         {
            this.barrierType = barrierType;
            this.barrier_lo = barrier_lo;
            this.barrier_hi = barrier_hi;
            this.cash = cash;
            this.s = s;
            this.q = q;
            this.r = r;
            this.t = t;
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
      public void testHaugValues()
      {
         // Testing cash-or-nothing double barrier options against Haug's values

         DoubleBinaryOptionData[] values =
         {
            /* The data below are from
                "Option pricing formulas 2nd Ed.", E.G. Haug, McGraw-Hill 2007 pag. 181
                Note: book uses cost of carry b, instead of dividend rate q
            */
            //                                  barrierType,          bar_lo, bar_hi, cash,   spot,   q,    r,    t,    vol,   value, tol
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  9.8716, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  8.9307, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  6.3272, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  1.9094, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  9.7961, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  7.2300, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  3.7100, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  0.4271, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  8.9054, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  3.6752, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  0.7960, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  0.0059, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  3.6323, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  0.0911, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  0.0002, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  0.0000, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.0000, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  0.2402, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  1.4076, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  3.8160, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.0075, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  0.9910, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  2.8098, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  4.6612, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.2656, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  2.7954, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  4.4024, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  4.9266, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  2.6285, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  4.7523, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  4.9096, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  4.9675, 1e-4),

            // following values calculated with haug's VBA code
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.0042, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  0.9450, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  3.5486, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  7.9663, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.0797, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  2.6458, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  6.1658, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  9.4486, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.9704, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  6.2006, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  9.0798, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  9.8699, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  6.2434, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  9.7847, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  9.8756, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  9.8758, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.0041, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  0.7080, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  2.1581, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       80.00, 120.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  4.2061, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.0723, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  1.6663, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  3.3930, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       85.00, 115.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  4.8679, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  0.7080, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  3.4424, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  4.7496, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       90.00, 110.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  5.0475, 1e-4),

            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.10,  3.6524, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.20,  5.1256, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.30,  5.0763, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       95.00, 105.00, 10.00, 100.00, 0.02, 0.05, 0.25, 0.50,  5.0275, 1e-4),

            // degenerate cases
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   95.00, 105.00, 10.00,  80.00, 0.02, 0.05, 0.25, 0.10,  0.0000, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockOut,   95.00, 105.00, 10.00, 110.00, 0.02, 0.05, 0.25, 0.10,  0.0000, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    95.00, 105.00, 10.00,  80.00, 0.02, 0.05, 0.25, 0.10, 10.0000, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KnockIn,    95.00, 105.00, 10.00, 110.00, 0.02, 0.05, 0.25, 0.10, 10.0000, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       95.00, 105.00, 10.00,  80.00, 0.02, 0.05, 0.25, 0.10, 10.0000, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KIKO,       95.00, 105.00, 10.00, 110.00, 0.02, 0.05, 0.25, 0.10,  0.0000, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       95.00, 105.00, 10.00,  80.00, 0.02, 0.05, 0.25, 0.10,  0.0000, 1e-4),
            new DoubleBinaryOptionData(DoubleBarrier.Type.KOKI,       95.00, 105.00, 10.00, 110.00, 0.02, 0.05, 0.25, 0.10, 10.0000, 1e-4),
         };

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(100.0);
         SimpleQuote qRate = new SimpleQuote(0.04);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.01);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.25);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         for (int i = 0; i < values.Length; i++)
         {
            StrikedTypePayoff payoff = new CashOrNothingPayoff(Option.Type.Call, 0, values[i].cash);

            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise exercise;
            if (values[i].barrierType == DoubleBarrier.Type.KIKO ||
                values[i].barrierType == DoubleBarrier.Type.KOKI)
               exercise = new AmericanExercise(today, exDate, true);
            else
               exercise = new EuropeanExercise(exDate);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
               new Handle<Quote>(spot),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticDoubleBarrierBinaryEngine(stochProcess);

            DoubleBarrierOption opt = new DoubleBarrierOption(values[i].barrierType,
                                                              values[i].barrier_lo,
                                                              values[i].barrier_hi,
                                                              0,
                                                              payoff,
                                                              exercise);

            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double expected = values[i].result;
            double error = Math.Abs(calculated - values[i].result);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("value", payoff, exercise, values[i].barrierType,
                              values[i].barrier_lo, values[i].barrier_hi, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              values[i].result, calculated, error, values[i].tol);
            }

            int steps = 500;
            // checking with binomial engine
            engine = new BinomialDoubleBarrierEngine(
               (d, end, step, strike) => new CoxRossRubinstein(d, end, step, strike),
            (args, process, grid) => new DiscretizedDoubleBarrierOption(args, process, grid),
            stochProcess, steps);
            opt.setPricingEngine(engine);
            calculated = opt.NPV();
            expected = values[i].result;
            error = Math.Abs(calculated - expected);
            double tol = 0.22;
            if (error > tol)
            {
               REPORT_FAILURE("Binomial value", payoff, exercise, values[i].barrierType,
                              values[i].barrier_lo, values[i].barrier_hi, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              values[i].result, calculated, error, tol);
            }
         }
      }
   }
}
