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
   public class T_DigitalOption
   {
      struct DigitalOptionData
      {
         public Option.Type type;
         public double strike;
         public double s;        // spot
         public double q;        // dividend
         public double r;        // risk-free rate
         public double t;        // time to maturity
         public double v;        // volatility
         public double result;   // expected result
         public double tol;      // tolerance
         public bool knockin;    // true if knock-in
         public DigitalOptionData(Option.Type type_, double strike_, double s_, double q_, double r_, double t_, double v_,
                                  double result_, double tol_, bool knockin_)
         {
            type = type_;
            strike = strike_;
            s = s_;
            q = q_;
            r = r_;
            t = t_;
            v = v_;
            result = result_;
            tol = tol_;
            knockin = knockin_;
         }
      }

      void REPORT_FAILURE(string greekName, StrikedTypePayoff payoff, Exercise exercise, double s, double q, double r,
                          Date today, double v, double expected, double calculated, double error, double tolerance,
                          bool knockin)
      {
         QAssert.Fail(exercise + " "
                      + payoff.optionType() + " option with "
                      + payoff + " payoff:\n"
                      + "    spot value:       " + s + "\n"
                      + "    strike:           " + payoff.strike() + "\n"
                      + "    dividend yield:   " + q + "\n"
                      + "    risk-free rate:   " + r + "\n"
                      + "    reference date:   " + today + "\n"
                      + "    maturity:         " + exercise.lastDate() + "\n"
                      + "    volatility:       " + v + "\n\n"
                      + "    expected          " + greekName + ":   " + expected + "\n"
                      + "    calculated        " + greekName + ": " + calculated + "\n"
                      + "    error:            " + error + "\n"
                      + "    tolerance:        " + tolerance + "\n"
                      + "    knock_in:         " + knockin);

      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCashOrNothingEuropeanValues()
      {
         // Testing European cash-or-nothing digital option

         DigitalOptionData[] values =
         {
            // "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 - pag 88
            //        type, strike,  spot,    q,    r,    t,  vol,  value, tol
            new DigitalOptionData(Option.Type.Put,   80.00, 100.0, 0.06, 0.06, 0.75, 0.35, 2.6710, 1e-4, true)
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
            StrikedTypePayoff payoff = new CashOrNothingPayoff(values[i].type, values[i].strike, 10.0);

            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise exercise = new EuropeanExercise(exDate);

            spot.setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol.setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS),
                                                                                   new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);

            VanillaOption opt = new VanillaOption(payoff, exercise);
            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated - values[i].result);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("value", payoff, exercise, values[i].s, values[i].q,
                              values[i].r, today, values[i].v, values[i].result,
                              calculated, error, values[i].tol, values[i].knockin);

            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testAssetOrNothingEuropeanValues()
      {

         // Testing European asset-or-nothing digital option

         // "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 - pag 90
         DigitalOptionData[] values =
         {
            //        type, strike, spot,    q,    r,    t,  vol,   value, tol
            new  DigitalOptionData(Option.Type.Put,   65.00, 70.0, 0.05, 0.07, 0.50, 0.27, 20.2069, 1e-4, true),
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
            StrikedTypePayoff payoff = new AssetOrNothingPayoff(values[i].type, values[i].strike);

            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise exercise = new EuropeanExercise(exDate);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS),
                                                                                   new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);

            VanillaOption opt = new VanillaOption(payoff, exercise);
            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated - values[i].result);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("value", payoff, exercise, values[i].s, values[i].q,
                              values[i].r, today, values[i].v, values[i].result,
                              calculated, error, values[i].tol, values[i].knockin);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testGapEuropeanValues()
      {
         // Testing European gap digital option

         // "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 - pag 88
         DigitalOptionData[] values =
         {
            //        type, strike, spot,    q,    r,    t,  vol,   value, tol
            new DigitalOptionData(Option.Type.Call,  50.00, 50.0, 0.00, 0.09, 0.50, 0.20, -0.0053, 1e-4, true),
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
            StrikedTypePayoff payoff = new GapPayoff(values[i].type, values[i].strike, 57.00);

            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise exercise = new EuropeanExercise(exDate);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS),
                                                                                   new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);

            VanillaOption opt = new VanillaOption(payoff, exercise);
            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated - values[i].result);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("value", payoff, exercise, values[i].s, values[i].q,
                              values[i].r, today, values[i].v, values[i].result,
                              calculated, error, values[i].tol, values[i].knockin);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCashAtHitOrNothingAmericanValues()
      {
         // Testing American cash-(at-hit)-or-nothing digital option

         DigitalOptionData[] values =
         {
            //        type, strike,   spot,    q,    r,   t,  vol,   value, tol
            // "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 - pag 95, case 1,2
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.00, 0.10, 0.5, 0.20,  9.7264, 1e-4,  true),
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 11.6553, 1e-4,  true),

            // the following cases are not taken from a reference paper or book
            // in the money options (guaranteed immediate payoff)
            new DigitalOptionData(Option.Type.Call, 100.00, 105.00, 0.00, 0.10, 0.5, 0.20, 15.0000, 1e-16, true),
            new DigitalOptionData(Option.Type.Put,  100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 15.0000, 1e-16, true),
            // non null dividend (cross-tested with MC simulation)
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.20, 0.10, 0.5, 0.20, 12.2715, 1e-4,  true),
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.20, 0.10, 0.5, 0.20,  8.9109, 1e-4,  true),
            new DigitalOptionData(Option.Type.Call, 100.00, 105.00, 0.20, 0.10, 0.5, 0.20, 15.0000, 1e-16, true),
            new DigitalOptionData(Option.Type.Put,  100.00,  95.00, 0.20, 0.10, 0.5, 0.20, 15.0000, 1e-16, true)
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
            StrikedTypePayoff payoff = new CashOrNothingPayoff(values[i].type, values[i].strike, 15.00);

            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise amExercise = new AmericanExercise(today, exDate);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS),
                                                                                   new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticDigitalAmericanEngine(stochProcess);

            VanillaOption opt = new VanillaOption(payoff, amExercise);
            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated - values[i].result);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("value", payoff, amExercise, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              values[i].result, calculated, error, values[i].tol, values[i].knockin);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testAssetAtHitOrNothingAmericanValues()
      {
         // Testing American asset-(at-hit)-or-nothing "digital option

         DigitalOptionData[] values =
         {
            //        type, strike,   spot,    q,    r,   t,  vol,   value, tol
            // "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 - pag 95, case 3,4
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.00, 0.10, 0.5, 0.20, 64.8426, 1e-04, true),   // Haug value is wrong here, Haug VBA code is right
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 77.7017, 1e-04, true),   // Haug value is wrong here, Haug VBA code is right
            // data from Haug VBA code results
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.01, 0.10, 0.5, 0.20, 65.7811, 1e-04, true),
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.01, 0.10, 0.5, 0.20, 76.8858, 1e-04, true),
            // in the money options  (guaranteed immediate payoff = spot)
            new DigitalOptionData(Option.Type.Call, 100.00, 105.00, 0.00, 0.10, 0.5, 0.20, 105.0000, 1e-16, true),
            new DigitalOptionData(Option.Type.Put,  100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 95.0000, 1e-16, true),
            new DigitalOptionData(Option.Type.Call, 100.00, 105.00, 0.01, 0.10, 0.5, 0.20, 105.0000, 1e-16, true),
            new DigitalOptionData(Option.Type.Put,  100.00,  95.00, 0.01, 0.10, 0.5, 0.20, 95.0000, 1e-16, true)
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
            StrikedTypePayoff payoff = new AssetOrNothingPayoff(values[i].type, values[i].strike);

            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise amExercise = new AmericanExercise(today, exDate);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS),
                                                                                   new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticDigitalAmericanEngine(stochProcess);

            VanillaOption opt = new VanillaOption(payoff, amExercise);
            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated - values[i].result);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("value", payoff, amExercise, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              values[i].result, calculated, error, values[i].tol, values[i].knockin);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCashAtExpiryOrNothingAmericanValues()
      {
         // Testing American cash-(at-expiry)-or-nothing digital option

         DigitalOptionData[] values =
         {
            //        type, strike,   spot,    q,    r,   t,  vol,   value, tol
            // "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 - pag 95, case 5,6,9,10
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.00, 0.10, 0.5, 0.20,  9.3604, 1e-4, true),
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 11.2223, 1e-4, true),
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.00, 0.10, 0.5, 0.20,  4.9081, 1e-4, false),
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.00, 0.10, 0.5, 0.20,  3.0461, 1e-4, false),
            // in the money options (guaranteed discounted payoff)
            new DigitalOptionData(Option.Type.Call, 100.00, 105.00, 0.00, 0.10, 0.5, 0.20, 15.0000 * Math.Exp(-0.05), 1e-12, true),
            new DigitalOptionData(Option.Type.Put,  100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 15.0000 * Math.Exp(-0.05), 1e-12, true),
            // out of bonds case
            new DigitalOptionData(Option.Type.Call,   2.37,   2.33, 0.07, 0.43, 0.19, 0.005,  0.0000, 1e-4, false),
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
            StrikedTypePayoff payoff = new CashOrNothingPayoff(values[i].type, values[i].strike, 15.0);

            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise amExercise = new AmericanExercise(today, exDate, true);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS),
                                                                                   new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine;
            if (values[i].knockin)
               engine = new AnalyticDigitalAmericanEngine(stochProcess);
            else
               engine = new AnalyticDigitalAmericanKOEngine(stochProcess);

            VanillaOption opt = new VanillaOption(payoff, amExercise);
            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated - values[i].result);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("value", payoff, amExercise, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              values[i].result, calculated, error, values[i].tol, values[i].knockin);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testAssetAtExpiryOrNothingAmericanValues()
      {

         // Testing American asset-(at-expiry)-or-nothing digital option

         DigitalOptionData[] values =
         {
            //        type, strike,   spot,    q,    r,   t,  vol,   value, tol
            // "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 - pag 95, case 7,8,11,12
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.00, 0.10, 0.5, 0.20, 64.8426, 1e-04, true),
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 77.7017, 1e-04, true),
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.00, 0.10, 0.5, 0.20, 40.1574, 1e-04, false),
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 17.2983, 1e-04, false),
            // data from Haug VBA code results
            new DigitalOptionData(Option.Type.Put,  100.00, 105.00, 0.01, 0.10, 0.5, 0.20, 65.5291, 1e-04, true),
            new DigitalOptionData(Option.Type.Call, 100.00,  95.00, 0.01, 0.10, 0.5, 0.20, 76.5951, 1e-04, true),
            // in the money options (guaranteed discounted payoff = forward * riskFreeDiscount
            //                                                    = spot * dividendDiscount)
            new DigitalOptionData(Option.Type.Call, 100.00, 105.00, 0.00, 0.10, 0.5, 0.20, 105.0000, 1e-12, true),
            new DigitalOptionData(Option.Type.Put,  100.00,  95.00, 0.00, 0.10, 0.5, 0.20, 95.0000, 1e-12, true),
            new DigitalOptionData(Option.Type.Call, 100.00, 105.00, 0.01, 0.10, 0.5, 0.20, 105.0000 * Math.Exp(-0.005), 1e-12, true),
            new DigitalOptionData(Option.Type.Put,  100.00,  95.00, 0.01, 0.10, 0.5, 0.20, 95.0000 * Math.Exp(-0.005), 1e-12, true)
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

            StrikedTypePayoff payoff = new AssetOrNothingPayoff(values[i].type, values[i].strike);

            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise amExercise = new AmericanExercise(today, exDate, true);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS),
                                                                                   new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine;
            if (values[i].knockin)
               engine = new AnalyticDigitalAmericanEngine(stochProcess);
            else
               engine = new AnalyticDigitalAmericanKOEngine(stochProcess);

            VanillaOption opt = new VanillaOption(payoff, amExercise);
            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated - values[i].result);
            if (error > values[i].tol)
            {
               REPORT_FAILURE("value", payoff, amExercise, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              values[i].result, calculated, error, values[i].tol, values[i].knockin);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCashAtHitOrNothingAmericanGreeks()
      {

         // Testing American cash-(at-hit)-or-nothing digital option greeks

         using (SavedSettings backup = new SavedSettings())
         {
            SortedDictionary<string, double> calculated = new SortedDictionary<string, double>();
            SortedDictionary<string, double> expected = new SortedDictionary<string, double>();
            SortedDictionary<string, double> tolerance = new SortedDictionary<string, double>(); // std::map<std::string,Real> calculated, expected, tolerance;

            tolerance["delta"]  = 5.0e-5;
            tolerance["gamma"]  = 5.0e-5;
            tolerance["rho"]    = 5.0e-5;

            Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
            double[] strikes = { 50.0, 99.5, 100.5, 150.0 };
            double cashPayoff = 100.0;
            double[] underlyings = { 100 };
            double[] qRates = { 0.04, 0.05, 0.06 };
            double[] rRates = { 0.01, 0.05, 0.15 };
            double[] vols = { 0.11, 0.5, 1.2 };

            DayCounter dc = new Actual360();
            Date today = Date.Today;
            Settings.setEvaluationDate(today);

            SimpleQuote spot = new SimpleQuote(0.0);
            SimpleQuote qRate = new SimpleQuote(0.0);
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
            SimpleQuote rRate = new SimpleQuote(0.0);
            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
            SimpleQuote vol = new SimpleQuote(0.0);
            Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

            // there is no cycling on different residual times
            Date exDate = today + 360;
            Exercise exercise = new EuropeanExercise(exDate);
            Exercise amExercise = new AmericanExercise(today, exDate, false);
            Exercise[] exercises = { exercise, amExercise };

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

            IPricingEngine euroEngine = new AnalyticEuropeanEngine(stochProcess);

            IPricingEngine amEngine = new AnalyticDigitalAmericanEngine(stochProcess);

            IPricingEngine[] engines = { euroEngine, amEngine };

            bool knockin = true;
            for (int j = 0; j < engines.Length; j++)
            {
               for (int i1 = 0; i1 < types.Length; i1++)
               {
                  for (int i6 = 0; i6 < strikes.Length; i6++)
                  {
                     StrikedTypePayoff payoff = new CashOrNothingPayoff(types[i1], strikes[i6], cashPayoff);

                     VanillaOption opt = new VanillaOption(payoff, exercises[j]);
                     opt.setPricingEngine(engines[j]);

                     for (int i2 = 0; i2 < underlyings.Length; i2++)
                     {
                        for (int i4 = 0; i4 < qRates.Length; i4++)
                        {
                           for (int i3 = 0; i3 < rRates.Length; i3++)
                           {
                              for (int i7 = 0; i7 < vols.Length; i7++)
                              {
                                 // test data
                                 double u = underlyings[i2];
                                 double q = qRates[i4];
                                 double r = rRates[i3];
                                 double v = vols[i7];
                                 spot.setValue(u);
                                 qRate.setValue(q);
                                 rRate.setValue(r);
                                 vol.setValue(v);

                                 // theta, dividend rho and vega are not available for
                                 // digital option with american exercise. Greeks of
                                 // digital options with european payoff are tested
                                 // in the europeanoption.cpp test
                                 double value = opt.NPV();
                                 calculated["delta"]  = opt.delta();
                                 calculated["gamma"]  = opt.gamma();
                                 calculated["rho"]    = opt.rho();

                                 if (value > 1.0e-6)
                                 {
                                    // perturb spot and get delta and gamma
                                    double du = u * 1.0e-4;
                                    spot.setValue(u + du);
                                    double value_p = opt.NPV(),
                                           delta_p = opt.delta();
                                    spot.setValue(u - du);
                                    double value_m = opt.NPV(),
                                           delta_m = opt.delta();
                                    spot.setValue(u);
                                    expected["delta"] = (value_p - value_m) / (2 * du);
                                    expected["gamma"] = (delta_p - delta_m) / (2 * du);

                                    // perturb rates and get rho and dividend rho
                                    double dr = r * 1.0e-4;
                                    rRate.setValue(r + dr);
                                    value_p = opt.NPV();
                                    rRate.setValue(r - dr);
                                    value_m = opt.NPV();
                                    rRate.setValue(r);
                                    expected["rho"] = (value_p - value_m) / (2 * dr);

                                    // check
                                    //std::map<std::string,Real>::iterator it;
                                    foreach (var it in calculated)
                                    {
                                       string greek = it.Key;
                                       double expct = expected  [greek],
                                              calcl = calculated[greek],
                                              tol   = tolerance [greek];
                                       double error = Utilities.relativeError(expct, calcl, value);
                                       if (error > tol)
                                       {
                                          REPORT_FAILURE(greek, payoff, exercise,
                                                         u, q, r, today, v,
                                                         expct, calcl, error, tol, knockin);
                                       }
                                    }
                                 }
                              }
                           }
                        }
                     }
                  }
               }
            }
         }
      }

      //[TestMethod()]
      //public void testMCCashAtHit()
      //{

      //   // Testing Monte Carlo cash-(at-hit)-or-nothing American engine

      //   using (SavedSettings backup = new SavedSettings())
      //   {

      //   DigitalOptionData[] values = {
      //   //                                 type, strike,   spot,    q,    r,   t,  vol,   value, tol
      //   new DigitalOptionData( Option.Type.Put,  100.00, 105.00, 0.20, 0.10, 0.5, 0.20, 12.2715, 1e-2, true ),
      //   new DigitalOptionData( Option.Type.Call, 100.00,  95.00, 0.20, 0.10, 0.5, 0.20,  8.9109, 1e-2, true ),
      //   };

      //   DayCounter dc = new Actual360();
      //   Date today = Date.Today;

      //   SimpleQuote spot = new SimpleQuote(0.0);
      //   SimpleQuote qRate = new SimpleQuote(0.0);
      //   YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
      //   SimpleQuote rRate = new SimpleQuote(0.0);
      //   YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
      //   SimpleQuote vol = new SimpleQuote(0.0);
      //   BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

      //   int timeStepsPerYear = 90;
      //   int maxSamples = 1000000;
      //   int seed = 1;

      //   for (int i=0; i< values.Length; i++)
      //   {
      //      StrikedTypePayoff payoff = new CashOrNothingPayoff(values[i].type, values[i].strike, 15.0);
      //      //FLOATING_POINT_EXCEPTION
      //      Date exDate = today + Convert.ToInt32(values[i].t*360+0.5);
      //      Exercise amExercise = new AmericanExercise(today, exDate);

      //      spot .setValue(values[i].s);
      //      qRate.setValue(values[i].q);
      //      rRate.setValue(values[i].r);
      //      vol  .setValue(values[i].v);

      //      BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
      //         new Handle<YieldTermStructure>(qTS),
      //         new Handle<YieldTermStructure>(rTS),
      //         new Handle<BlackVolTermStructure>(volTS));

      //      int requiredSamples = (int)(Math.Pow(2.0, 14)-1);
      //      IPricingEngine mcldEngine = MakeMCDigitalEngine<LowDiscrepancy>(stochProcess)
      //         .withStepsPerYear(timeStepsPerYear)
      //         .withBrownianBridge()
      //         .withSamples(requiredSamples)
      //         .withMaxSamples(maxSamples)
      //         .withSeed(seed);

      //      VanillaOption opt = new VanillaOption(payoff, amExercise);
      //      opt.setPricingEngine(mcldEngine);

      //      double calculated = opt.NPV();
      //      double error = Math.Abs(calculated-values[i].result);
      //      if (error > values[i].tol)
      //      {
      //         REPORT_FAILURE("value", payoff, amExercise, values[i].s,
      //                        values[i].q, values[i].r, today, values[i].v,
      //                        values[i].result, calculated, error, values[i].tol, values[i].knockin);
      //      }
      //   }
      //}
      //}
   }
}
