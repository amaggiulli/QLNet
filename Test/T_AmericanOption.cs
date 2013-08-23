/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Andrea Maggiulli
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://qlnet.sourceforge.net/License.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite {
    public struct AmericanOptionData {
      public Option.Type type;
      public double strike;
      public double s; // spot
      public double q; // dividend
      public double r; // risk-free rate
      public double t; // time to maturity
      public double v; // volatility
      public double result; // expected result

      public AmericanOptionData(Option.Type type_,
                         double strike_,
                         double s_,
                         double q_,
                         double r_,
                         double t_,
                         double v_,
                         double result_)
      {
         type = type_;
         strike= strike_;
         s = s_;
         q = q_;
         r = r_;
         t = t_;
         v = v_;
         result = result_;
      }
   }

    [TestClass()]
    public class T_AmericanOption {

            /* The data below are from
           An Approximate Formula for Pricing American Options
           Journal of Derivatives Winter 1999
           Ju, N.
        */
        AmericanOptionData[] juValues = new AmericanOptionData[] {
            //        type, strike,   spot,    q,    r,    t,     vol,   value, tol
            // These values are from Exhibit 3 - Short dated Put Options
            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.0833,  0.2,  0.006 ),
            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.3333,  0.2,  0.201 ),
            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.5833,  0.2,  0.433 ),

            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.0833,  0.2,  0.851 ),
            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.3333,  0.2,  1.576 ),
            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.5833,  0.2,  1.984 ),

            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.0833,  0.2,  5.000 ),
            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.3333,  0.2,  5.084 ),
            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.5833,  0.2,  5.260 ),

            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.0833,  0.3,  0.078 ),
            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.3333,  0.3,  0.697 ),
            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.5833,  0.3,  1.218 ),

            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.0833,  0.3,  1.309 ),
            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.3333,  0.3,  2.477 ),
            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.5833,  0.3,  3.161 ),

            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.0833,  0.3,  5.059 ),
            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.3333,  0.3,  5.699 ),
            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.5833,  0.3,  6.231 ),

            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.0833,  0.4,  0.247 ),
            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.3333,  0.4,  1.344 ),
            new AmericanOptionData( Option.Type.Put, 35.00,   40.00,  0.0,  0.0488, 0.5833,  0.4,  2.150 ),

            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.0833,  0.4,  1.767 ),
            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.3333,  0.4,  3.381 ),
            new AmericanOptionData( Option.Type.Put, 40.00,   40.00,  0.0,  0.0488, 0.5833,  0.4,  4.342 ),

            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.0833,  0.4,  5.288 ),
            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.3333,  0.4,  6.501 ),
            new AmericanOptionData( Option.Type.Put, 45.00,   40.00,  0.0,  0.0488, 0.5833,  0.4,  7.367 ),

            // Type in Exhibits 4 and 5 if you have some spare time ;-)

            //        type, strike,   spot,    q,    r,    t,     vol,   value, tol
            // values from Exhibit 6 - Long dated Call Options with dividends
            new AmericanOptionData( Option.Type.Call, 100.00,   80.00,  0.07,  0.03, 3.0,  0.2,   2.605 ),
            new AmericanOptionData( Option.Type.Call, 100.00,   90.00,  0.07,  0.03, 3.0,  0.2,   5.182 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  100.00,  0.07,  0.03, 3.0,  0.2,   9.065 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  110.00,  0.07,  0.03, 3.0,  0.2,  14.430 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  120.00,  0.07,  0.03, 3.0,  0.2,  21.398 ),

            new AmericanOptionData( Option.Type.Call, 100.00,   80.00,  0.07,  0.03, 3.0,  0.4,  11.336 ),
            new AmericanOptionData( Option.Type.Call, 100.00,   90.00,  0.07,  0.03, 3.0,  0.4,  15.711 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  100.00,  0.07,  0.03, 3.0,  0.4,  20.760 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  110.00,  0.07,  0.03, 3.0,  0.4,  26.440 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  120.00,  0.07,  0.03, 3.0,  0.4,  32.709 ),

            new AmericanOptionData( Option.Type.Call, 100.00,   80.00,  0.07,  0.00001, 3.0,  0.3,   5.552 ),
            new AmericanOptionData( Option.Type.Call, 100.00,   90.00,  0.07,  0.00001, 3.0,  0.3,   8.868 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  100.00,  0.07,  0.00001, 3.0,  0.3,  13.158 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  110.00,  0.07,  0.00001, 3.0,  0.3,  18.458 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  120.00,  0.07,  0.00001, 3.0,  0.3,  24.786 ),

            new AmericanOptionData( Option.Type.Call, 100.00,   80.00,  0.03,  0.07, 3.0,  0.3,  12.177 ),
            new AmericanOptionData( Option.Type.Call, 100.00,   90.00,  0.03,  0.07, 3.0,  0.3,  17.411 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  100.00,  0.03,  0.07, 3.0,  0.3,  23.402 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  110.00,  0.03,  0.07, 3.0,  0.3,  30.028 ),
            new AmericanOptionData( Option.Type.Call, 100.00,  120.00,  0.03,  0.07, 3.0,  0.3,  37.177 )
        };

        [TestMethod()]
        public void testBaroneAdesiWhaleyValues() {
            // ("Testing Barone-Adesi and Whaley approximation for American options...");

            /* The data below are from
               "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 pag 24

               The following values were replicated only up to the second digit
               by the VB code provided by Haug, which was used as base for the
               C++ implementation

            */
            AmericanOptionData[] values = {
                new AmericanOptionData(Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.10, 0.15,  0.0206) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.10, 0.15,  1.8771) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.10, 0.15, 10.0089) ,
                new AmericanOptionData(Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.10, 0.25,  0.3159) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.10, 0.25,  3.1280) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.10, 0.25, 10.3919) ,
                new AmericanOptionData(Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.10, 0.35,  0.9495) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.10, 0.35,  4.3777) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.10, 0.35, 11.1679) ,
                new AmericanOptionData(Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.50, 0.15,  0.8208) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.50, 0.15,  4.0842) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.50, 0.15, 10.8087) ,
                new AmericanOptionData(Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.50, 0.25,  2.7437) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.50, 0.25,  6.8015) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.50, 0.25, 13.0170) ,
                new AmericanOptionData(Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.50, 0.35,  5.0063) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.50, 0.35,  9.5106) ,
                new AmericanOptionData(Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.50, 0.35, 15.5689) ,
                new AmericanOptionData(Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.10, 0.15, 10.0000) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.10, 0.15,  1.8770) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.10, 0.15,  0.0410) ,
                new AmericanOptionData(Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.10, 0.25, 10.2533) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.10, 0.25,  3.1277) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.10, 0.25,  0.4562) ,
                new AmericanOptionData(Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.10, 0.35, 10.8787) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.10, 0.35,  4.3777) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.10, 0.35,  1.2402) ,
                new AmericanOptionData(Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.50, 0.15, 10.5595) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.50, 0.15,  4.0842) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.50, 0.15,  1.0822) ,
                new AmericanOptionData(Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.50, 0.25, 12.4419) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.50, 0.25,  6.8014) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.50, 0.25,  3.3226) ,
                new AmericanOptionData(Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.50, 0.35, 14.6945) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.50, 0.35,  9.5104) ,
                new AmericanOptionData(Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.50, 0.35,  5.8823)};

            Date today = Date.Today;
            DayCounter dc = new Actual360();
            SimpleQuote spot = new SimpleQuote(0.0);
            SimpleQuote qRate = new SimpleQuote(0.0);
            YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);

            SimpleQuote rRate = new SimpleQuote(0.0);
            YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
            SimpleQuote vol = new SimpleQuote(0.0);
            BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

            double tolerance = 3.0e-3;

            for (int i=0; i<values.Length; i++) {

                StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, values[i].strike);
                Date exDate = today + Convert.ToInt32(values[i].t*360+0.5);
                Exercise exercise = new AmericanExercise(today, exDate);

                spot .setValue(values[i].s);
                qRate.setValue(values[i].q);
                rRate.setValue(values[i].r);
                vol  .setValue(values[i].v);

                BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                              new Handle<YieldTermStructure>(qTS),
                                              new Handle<YieldTermStructure>(rTS),
                                              new Handle<BlackVolTermStructure>(volTS));

                IPricingEngine engine = new BaroneAdesiWhaleyApproximationEngine(stochProcess);

                VanillaOption option = new VanillaOption(payoff, exercise);
                option.setPricingEngine(engine);

                double calculated = option.NPV();
                double error = Math.Abs(calculated-values[i].result);
                if (error > tolerance) {
                    REPORT_FAILURE("value", payoff, exercise, values[i].s, values[i].q,
                                   values[i].r, today, values[i].v, values[i].result,
                                   calculated, error, tolerance);
                }
            }
        }

        [TestMethod()]
        public void testBjerksundStenslandValues() {
            // ("Testing Bjerksund and Stensland approximation for American options...");

            AmericanOptionData[] values = new AmericanOptionData[] {
                //      type, strike,   spot,    q,    r,    t,  vol,   value, tol
                // from "Option pricing formulas", Haug, McGraw-Hill 1998, pag 27
              new AmericanOptionData(Option.Type.Call,  40.00,  42.00, 0.08, 0.04, 0.75, 0.35,  5.2704),
                // from "Option pricing formulas", Haug, McGraw-Hill 1998, VBA code
              new AmericanOptionData(Option.Type.Put,   40.00,  36.00, 0.00, 0.06, 1.00, 0.20,  4.4531)
            };

            Date today = Date.Today;
            DayCounter dc = new Actual360();
            SimpleQuote spot = new SimpleQuote(0.0);
            SimpleQuote qRate = new SimpleQuote(0.0);
            YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);

            SimpleQuote rRate = new SimpleQuote(0.0);
            YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
            SimpleQuote vol = new SimpleQuote(0.0);
            BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

            double tolerance = 3.0e-3;

            for (int i=0; i<values.Length; i++) {

                StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, values[i].strike);
                Date exDate = today + Convert.ToInt32(values[i].t*360+0.5);
                Exercise exercise = new AmericanExercise(today, exDate);

                spot .setValue(values[i].s);
                qRate.setValue(values[i].q);
                rRate.setValue(values[i].r);
                vol  .setValue(values[i].v);

                BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                              new Handle<YieldTermStructure>(qTS),
                                              new Handle<YieldTermStructure>(rTS),
                                              new Handle<BlackVolTermStructure>(volTS));

                IPricingEngine engine = new BjerksundStenslandApproximationEngine(stochProcess);

                VanillaOption option = new VanillaOption(payoff, exercise);
                option.setPricingEngine(engine);

                double calculated = option.NPV();
                double error = Math.Abs(calculated-values[i].result);
                if (error > tolerance) {
                    REPORT_FAILURE("value", payoff, exercise, values[i].s, values[i].q,
                                   values[i].r, today, values[i].v, values[i].result,
                                   calculated, error, tolerance);
                }
            }
        }

        [TestMethod()]
        public void testJuValues() {

            // ("Testing Ju approximation for American options...");

            Date today = Date.Today;
            DayCounter dc = new Actual360();
            SimpleQuote spot = new SimpleQuote(0.0);
            SimpleQuote qRate = new SimpleQuote(0.0);
            YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);

            SimpleQuote rRate = new SimpleQuote(0.0);
            YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
            SimpleQuote vol = new SimpleQuote(0.0);
            BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

            double tolerance = 1.0e-3;

            for (int i = 0; i < juValues.Length; i++) {

                StrikedTypePayoff payoff = new PlainVanillaPayoff(juValues[i].type, juValues[i].strike);
                Date exDate = today + Convert.ToInt32(juValues[i].t*360+0.5);
                Exercise exercise = new AmericanExercise(today, exDate);

                spot .setValue(juValues[i].s);
                qRate.setValue(juValues[i].q);
                rRate.setValue(juValues[i].r);
                vol  .setValue(juValues[i].v);

                BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                              new Handle<YieldTermStructure>(qTS),
                                              new Handle<YieldTermStructure>(rTS),
                                              new Handle<BlackVolTermStructure>(volTS));

                IPricingEngine engine = new JuQuadraticApproximationEngine(stochProcess);

                VanillaOption option = new VanillaOption(payoff, exercise);
                option.setPricingEngine(engine);

                double calculated = option.NPV();
                double error = Math.Abs(calculated - juValues[i].result);
                if (error > tolerance) {
                    REPORT_FAILURE("value", payoff, exercise, juValues[i].s, juValues[i].q,
                                   juValues[i].r, today, juValues[i].v, juValues[i].result,
                                   calculated, error, tolerance);
                }
            }
        }

        [TestMethod()]
        public void testFdValues() {

            //("Testing finite-difference engine for American options...");

            Date today = Date.Today;
            DayCounter dc = new Actual360();
            SimpleQuote spot = new SimpleQuote(0.0);
            SimpleQuote qRate = new SimpleQuote(0.0);
            YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);

            SimpleQuote rRate = new SimpleQuote(0.0);
            YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
            SimpleQuote vol = new SimpleQuote(0.0);
            BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

            double tolerance = 8.0e-2;

            for (int i = 0; i < juValues.Length; i++) {

                StrikedTypePayoff payoff = new PlainVanillaPayoff(juValues[i].type, juValues[i].strike);
                Date exDate = today + Convert.ToInt32(juValues[i].t*360+0.5);
                Exercise exercise = new AmericanExercise(today, exDate);

                spot .setValue(juValues[i].s);
                qRate.setValue(juValues[i].q);
                rRate.setValue(juValues[i].r);
                vol  .setValue(juValues[i].v);

                BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                              new Handle<YieldTermStructure>(qTS),
                                              new Handle<YieldTermStructure>(rTS),
                                              new Handle<BlackVolTermStructure>(volTS));

                IPricingEngine engine = new FDAmericanEngine(stochProcess, 100,100);

                VanillaOption option = new VanillaOption(payoff, exercise);
                option.setPricingEngine(engine);

                double calculated = option.NPV();
                double error = Math.Abs(calculated - juValues[i].result);
                if (error > tolerance) {
                    REPORT_FAILURE("value", payoff, exercise, juValues[i].s, juValues[i].q,
                                   juValues[i].r, today, juValues[i].v, juValues[i].result,
                                   calculated, error, tolerance);
                }
            }
        }

        public void testFdGreeks<Engine>() where Engine : IFDEngine, new() {

            //SavedSettings backup;

            Dictionary<string, double> calculated = new Dictionary<string,double>(), 
                expected = new Dictionary<string,double>(), 
                tolerance = new Dictionary<string,double>();

            tolerance.Add("delta", 7.0e-4);
            tolerance.Add("gamma", 2.0e-4);
            //tolerance["theta"]  = 1.0e-4;

            Option.Type[] types = new Option.Type[] { Option.Type.Call, Option.Type.Put };
            double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
            double[] underlyings = { 100.0 };
            double[] qRates = { 0.04, 0.05, 0.06 };
            double[] rRates = { 0.01, 0.05, 0.15 };
            int[] years = { 1, 2 };
            double[] vols = { 0.11, 0.50, 1.20 };

            Date today = Date.Today;
            Settings.setEvaluationDate(today);

            DayCounter dc = new Actual360();
            SimpleQuote spot = new SimpleQuote(0.0);
            SimpleQuote qRate = new SimpleQuote(0.0);
            YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);

            SimpleQuote rRate = new SimpleQuote(0.0);
            YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
            SimpleQuote vol = new SimpleQuote(0.0);
            BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

            for (int i=0; i<types.Length; i++) {
              for (int j=0; j<strikes.Length; j++) {
                for (int k=0; k<years.Length; k++) {
                    Date exDate = today + new Period(years[k], TimeUnit.Years);
                    Exercise exercise = new AmericanExercise(today, exDate);
                    StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);
                    BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                  new Handle<YieldTermStructure>(qTS),
                                                  new Handle<YieldTermStructure>(rTS),
                                                  new Handle<BlackVolTermStructure>(volTS));

                    IPricingEngine engine = new Engine().factory(stochProcess);

                    VanillaOption option = new VanillaOption(payoff, exercise);
                    option.setPricingEngine(engine);

                    for (int l=0; l<underlyings.Length; l++) {
                      for (int m=0; m<qRates.Length; m++) {
                        for (int n=0; n<rRates.Length; n++) {
                          for (int p=0; p<vols.Length; p++) {
                            double u = underlyings[l];
                            double q = qRates[m],
                                 r = rRates[n];
                            double v = vols[p];
                            spot.setValue(u);
                            qRate.setValue(q);
                            rRate.setValue(r);
                            vol.setValue(v);

                            double value = option.NPV();
                            calculated.Add("delta", option.delta());
                            calculated.Add("gamma", option.gamma());
                            //calculated["theta"]  = option.theta();

                            if (value > spot.value()*1.0e-5) {
                                // perturb spot and get delta and gamma
                                double du = u*1.0e-4;
                                spot.setValue(u+du);
                                double value_p = option.NPV(),
                                     delta_p = option.delta();
                                spot.setValue(u-du);
                                double value_m = option.NPV(),
                                     delta_m = option.delta();
                                spot.setValue(u);
                                expected.Add("delta", (value_p - value_m)/(2*du));
                                expected.Add("gamma", (delta_p - delta_m)/(2*du));

                                /*
                                // perturb date and get theta
                                Time dT = dc.yearFraction(today-1, today+1);
                                Settings::instance().setEvaluationDate(today-1);
                                value_m = option.NPV();
                                Settings::instance().setEvaluationDate(today+1);
                                value_p = option.NPV();
                                Settings::instance().setEvaluationDate(today);
                                expected["theta"] = (value_p - value_m)/dT;
                                */

                                // compare
                                foreach (string greek in calculated.Keys) {
                                    double expct = expected  [greek],
                                        calcl = calculated[greek],
                                        tol   = tolerance [greek];
                                    double error = Utilities.relativeError(expct,calcl,u);
                                    if (error>tol) {
                                        REPORT_FAILURE(greek, payoff, exercise,
                                                       u, q, r, today, v,
                                                       expct, calcl, error, tol);
                                    }
                                }
                            }
                            calculated.Clear();
                            expected.Clear();
                          }
                        }
                      }
                    }
                }
              }
            }
        }

        [TestMethod()]
        public void testFdAmericanGreeks() {
            //("Testing finite-differences American option greeks...");
            testFdGreeks<FDAmericanEngine>();
        }

        [TestMethod()]
        public void testFdShoutGreeks() {
            // ("Testing finite-differences shout option greeks...");
            testFdGreeks<FDShoutEngine>();
        }

        void REPORT_FAILURE(string greekName, StrikedTypePayoff payoff, Exercise exercise, double s, double q, double r,
        Date today, double v, double expected, double calculated, double error, double tolerance) {
            Assert.Fail(exercise + " "
                   + payoff.optionType() + " option with "
                   + payoff + " payoff:\n"
                   + "    spot value:       " + s + "\n"
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
   }
}
