/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2025 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QLNet;


namespace TestSuite
{
   [Collection("QLNet CI Tests")]
   public class T_AsianOptions
   {
      internal void REPORT_FAILURE(string greekName, Average.Type averageType,
                                 double? runningAccumulator, int? pastFixings,
                                 List<Date> fixingDates, StrikedTypePayoff payoff,
                                 Exercise exercise, double s, double q, double r,
                                 Date today, double v, double expected,
                                 double calculated, double tolerance)
      {
         QAssert.Fail(exercise + " "
                      + exercise
                      + " Asian option with "
                      + averageType + " and "
                      + payoff + " payoff:\n"
                      + "    running variable: "
                      + runningAccumulator + "\n"
                      + "    past fixings:     "
                      + pastFixings + "\n"
                      + "    future fixings:   " + fixingDates.Count() + "\n"
                      + "    underlying value: " + s + "\n"
                      + "    strike:           " + payoff.strike() + "\n"
                      + "    dividend yield:   " + q + "\n"
                      + "    risk-free rate:   " + r + "\n"
                      + "    reference date:   " + today + "\n"
                      + "    maturity:         " + exercise.lastDate() + "\n"
                      + "    volatility:       " + v + "\n\n"
                      + "    expected   " + greekName + ": " + expected + "\n"
                      + "    calculated " + greekName + ": " + calculated + "\n"
                      + "    error:            " + Math.Abs(expected - calculated)
                      + "\n"
                      + "    tolerance:        " + tolerance);
      }

      public string averageTypeToString(Average.Type averageType)
      {

         if (averageType == Average.Type.Geometric)
            return "Geometric Averaging";
         else if (averageType == Average.Type.Arithmetic)
            return "Arithmetic Averaging";
         else
            Utils.QL_FAIL("unknown averaging");

         return String.Empty;
      }

      public struct DiscreteAverageData(
         Option.Type Type,
         double Underlying,
         double Strike,
         double DividendYield,
         double RiskFreeRate,
         double First,
         double Length,
         int Fixings,
         double Volatility,
         bool ControlVariate,
         double Result)
      {
         public Option.Type type = Type;
         public double underlying = Underlying;
         public double strike = Strike;
         public double dividendYield = DividendYield;
         public double riskFreeRate = RiskFreeRate;
         public double first = First;
         public double length = Length;
         public int fixings = Fixings;
         public double volatility = Volatility;
         public bool controlVariate = ControlVariate;
         public double result = Result;
      }

      public struct ContinuousAverageData(
         Option.Type Type,
         double Spot,
         double CurrentAverage,
         double Strike,
         double DividendYield,
         double RiskFreeRate,
         double Volatility,
         int Length,
         double Elapsed,
         double Result)
      {
         public Option.Type type = Type;
         public double spot = Spot;
         public double currentAverage = CurrentAverage;
         public double strike = Strike;
         public double dividendYield = DividendYield;
         public double riskFreeRate = RiskFreeRate;
         public double volatility = Volatility;
         public int length = Length;
         public double elapsed = Elapsed;
         public double result = Result;
      }

      public struct DiscreteAverageDataTermStructure (
            Option.Type Type,
            double Underlying,
            double Strike,
            double B,
            double RiskFreeRate,
            double First,
            double Expiry,
            int Fixing,
            double Volatility,
            string Slope,
            double Result)
      {
         public Option.Type type = Type;
         public double underlying = Underlying;
         public double strike = Strike;
         public double b = B;
         public double riskFreeRate = RiskFreeRate;
         public double first = First; // t1
         public double expiry= Expiry;
         public int fixings = Fixing;
         public double volatility = Volatility;
         public string slope = Slope;
         public double result = Result;
      }

      public struct VecerData (
         double Spot,
         double RiskFreeRate,
         double Volatility,
         double Strike,
         int Length,
         double Result,
         double Tolerance)
      {
         public double spot = Spot;
         public double riskFreeRate = RiskFreeRate;
         public double volatility = Volatility;
         public double strike = Strike;
         public int length = Length;
         public double result = Result;
         public double tolerance = Tolerance;
      }


      [Fact]
      public void testAnalyticContinuousGeometricAveragePrice()
      {
         // Testing analytic continuous geometric average-price Asians
         // data from "Option Pricing Formulas", Haug, pag.96-97

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(80.0);
         SimpleQuote qRate = new SimpleQuote(-0.03);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.05);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.20);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         BlackScholesMertonProcess stochProcess = new
         BlackScholesMertonProcess(new Handle<Quote>(spot),
                                   new Handle<YieldTermStructure>(qTS),
                                   new Handle<YieldTermStructure>(rTS),
                                   new Handle<BlackVolTermStructure>(volTS));

         IPricingEngine engine = new
         AnalyticContinuousGeometricAveragePriceAsianEngine(stochProcess);

         Average.Type averageType = Average.Type.Geometric;
         Option.Type type = Option.Type.Put;
         double strike = 85.0;
         Date exerciseDate = today + 90;

         int? pastFixings = null;
         double? runningAccumulator = null;

         StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

         Exercise exercise = new EuropeanExercise(exerciseDate);

         ContinuousAveragingAsianOption option = new ContinuousAveragingAsianOption(averageType, payoff, exercise);
         option.setPricingEngine(engine);

         double calculated = option.NPV();
         double expected = 4.6922;
         double tolerance = 1.0e-4;
         if (Math.Abs(calculated - expected) > tolerance)
         {
            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                           new List<Date>(), payoff, exercise, spot.value(),
                           qRate.value(), rRate.value(), today,
                           vol.value(), expected, calculated, tolerance);
         }

         // trying to approximate the continuous version with the discrete version
         runningAccumulator = 1.0;
         pastFixings = 0;
         List<Date> fixingDates = new InitializedList<Date>(exerciseDate - today + 1);
         for (int i = 0; i < fixingDates.Count; i++)
         {
            fixingDates[i] = today + i;
         }
         IPricingEngine engine2 = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

         DiscreteAveragingAsianOption option2 = new DiscreteAveragingAsianOption(averageType, runningAccumulator,
                                                                                 pastFixings, fixingDates, payoff, exercise);

         option2.setPricingEngine(engine2);

         calculated = option2.NPV();
         tolerance = 3.0e-3;
         if (Math.Abs(calculated - expected) > tolerance)
         {
            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                           fixingDates, payoff, exercise, spot.value(),
                           qRate.value(), rRate.value(), today,
                           vol.value(), expected, calculated, tolerance);
         }

      }

      [Fact]
      public void testAnalyticContinuousGeometricAveragePriceGreeks()
      {
         // Testing analytic continuous geometric average-price Asian greeks
         using (SavedSettings backup = new SavedSettings())
         {
            Dictionary<string, double> calculated, expected, tolerance;
            calculated = new Dictionary<string, double>(6);
            expected = new Dictionary<string, double>(6);
            tolerance = new Dictionary<string, double>(6);
            tolerance["delta"]  = 1.0e-5;
            tolerance["gamma"]  = 1.0e-5;
            tolerance["theta"]  = 1.0e-5;
            tolerance["rho"]    = 1.0e-5;
            tolerance["divRho"] = 1.0e-5;
            tolerance["vega"]   = 1.0e-5;

            Option.Type[] types = { Option.Type.Call, Option.Type.Put };
            double[] underlyings = { 100.0 };
            double[] strikes = { 90.0, 100.0, 110.0 };
            double[] qRates = { 0.04, 0.05, 0.06 };
            double[] rRates = { 0.01, 0.05, 0.15 };
            int[] lengths = { 1, 2 };
            double[] vols = { 0.11, 0.50, 1.20 };

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

            BlackScholesMertonProcess process = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

            for (int i = 0; i < types.Length; i++)
            {
               for (int j = 0; j < strikes.Length; j++)
               {
                  for (int k = 0; k < lengths.Length; k++)
                  {

                     EuropeanExercise maturity = new EuropeanExercise(today + new Period(lengths[k], TimeUnit.Years));
                     PlainVanillaPayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                     IPricingEngine engine = new AnalyticContinuousGeometricAveragePriceAsianEngine(process);

                     ContinuousAveragingAsianOption option = new ContinuousAveragingAsianOption(Average.Type.Geometric,
                                                                                                payoff, maturity);
                     option.setPricingEngine(engine);

                     int? pastFixings = null;
                     double? runningAverage = null;

                     for (int l = 0; l < underlyings.Length; l++)
                     {
                        for (int m = 0; m < qRates.Length; m++)
                        {
                           for (int n = 0; n < rRates.Length; n++)
                           {
                              for (int p = 0; p < vols.Length; p++)
                              {
                                 double u = underlyings[l];
                                 double q = qRates[m],
                                        r = rRates[n];
                                 double v = vols[p];
                                 spot.setValue(u);
                                 qRate.setValue(q);
                                 rRate.setValue(r);
                                 vol.setValue(v);

                                 double value = option.NPV();
                                 calculated["delta"] = option.delta();
                                 calculated["gamma"] = option.gamma();
                                 calculated["theta"] = option.theta();
                                 calculated["rho"] = option.rho();
                                 calculated["divRho"] = option.dividendRho();
                                 calculated["vega"] = option.vega();

                                 if (value > spot.value() * 1.0e-5)
                                 {
                                    // perturb spot and get delta and gamma
                                    double du = u * 1.0e-4;
                                    spot.setValue(u + du);
                                    double value_p = option.NPV(),
                                           delta_p = option.delta();
                                    spot.setValue(u - du);
                                    double value_m = option.NPV(),
                                           delta_m = option.delta();
                                    spot.setValue(u);
                                    expected["delta"] = (value_p - value_m) / (2 * du);
                                    expected["gamma"] = (delta_p - delta_m) / (2 * du);

                                    // perturb rates and get rho and dividend rho
                                    double dr = r * 1.0e-4;
                                    rRate.setValue(r + dr);
                                    value_p = option.NPV();
                                    rRate.setValue(r - dr);
                                    value_m = option.NPV();
                                    rRate.setValue(r);
                                    expected["rho"] = (value_p - value_m) / (2 * dr);

                                    double dq = q * 1.0e-4;
                                    qRate.setValue(q + dq);
                                    value_p = option.NPV();
                                    qRate.setValue(q - dq);
                                    value_m = option.NPV();
                                    qRate.setValue(q);
                                    expected["divRho"] = (value_p - value_m) / (2 * dq);

                                    // perturb volatility and get vega
                                    double dv = v * 1.0e-4;
                                    vol.setValue(v + dv);
                                    value_p = option.NPV();
                                    vol.setValue(v - dv);
                                    value_m = option.NPV();
                                    vol.setValue(v);
                                    expected["vega"] = (value_p - value_m) / (2 * dv);

                                    // perturb date and get theta
                                    double dT = dc.yearFraction(today - 1, today + 1);
                                    Settings.setEvaluationDate(today - 1);
                                    value_m = option.NPV();
                                    Settings.setEvaluationDate(today + 1);
                                    value_p = option.NPV();
                                    Settings.setEvaluationDate(today);
                                    expected["theta"] = (value_p - value_m) / dT;

                                    // compare
                                    foreach (KeyValuePair<string, double> kvp in calculated)
                                    {
                                       string greek = kvp.Key;
                                       double expct = expected[greek],
                                              calcl = calculated[greek],
                                              tol = tolerance[greek];
                                       double error = Utilities.relativeError(expct, calcl, u);
                                       if (error > tol)
                                       {
                                          REPORT_FAILURE(greek, Average.Type.Geometric,
                                                         runningAverage, pastFixings,
                                                         new List<Date>(),
                                                         payoff, maturity,
                                                         u, q, r, today, v,
                                                         expct, calcl, tol);
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

      [Fact]
      public void testAnalyticDiscreteGeometricAveragePrice()
      {
         // Testing analytic discrete geometric average-price Asians
         // data from "Implementing Derivatives Model",
         // Clewlow, Strickland, p.118-123

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(100.0);
         SimpleQuote qRate = new SimpleQuote(0.03);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.06);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.20);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         BlackScholesMertonProcess stochProcess = new
         BlackScholesMertonProcess(new Handle<Quote>(spot),
                                   new Handle<YieldTermStructure>(qTS),
                                   new Handle<YieldTermStructure>(rTS),
                                   new Handle<BlackVolTermStructure>(volTS));

         IPricingEngine engine = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

         Average.Type averageType = Average.Type.Geometric;
         double runningAccumulator = 1.0;
         int pastFixings = 0;
         int futureFixings = 10;
         Option.Type type = Option.Type.Call;
         double strike = 100.0;
         StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

         Date exerciseDate = today + 360;
         Exercise exercise = new EuropeanExercise(exerciseDate);

         List<Date> fixingDates = new InitializedList<Date>(futureFixings);
         int dt = (int)(360 / futureFixings + 0.5);
         fixingDates[0] = today + dt;
         for (int j = 1; j < futureFixings; j++)
            fixingDates[j] = fixingDates[j - 1] + dt;

         DiscreteAveragingAsianOption option = new DiscreteAveragingAsianOption(averageType, runningAccumulator,
                                                                                pastFixings, fixingDates, payoff, exercise);
         option.setPricingEngine(engine);

         double calculated = option.NPV();
         double expected = 5.3425606635;
         double tolerance = 1e-10;
         if (Math.Abs(calculated - expected) > tolerance)
         {
            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                           fixingDates, payoff, exercise, spot.value(),
                           qRate.value(), rRate.value(), today,
                           vol.value(), expected, calculated, tolerance);
         }
      }

      [Fact]
      public void testAnalyticDiscreteGeometricAverageStrike()
      {
         // Testing analytic discrete geometric average-strike Asians

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot  = new SimpleQuote(100.0);
         SimpleQuote qRate = new SimpleQuote(0.03);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.06);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.20);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                new Handle<YieldTermStructure>(qTS),
                                                                                new Handle<YieldTermStructure>(rTS),
                                                                                new Handle<BlackVolTermStructure>(volTS));

         IPricingEngine engine = new AnalyticDiscreteGeometricAverageStrikeAsianEngine(stochProcess);

         Average.Type averageType = Average.Type.Geometric;
         double runningAccumulator = 1.0;
         int pastFixings = 0;
         int futureFixings = 10;
         Option.Type type = Option.Type.Call;
         double strike = 100.0;
         StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

         Date exerciseDate = today + 360;
         Exercise exercise = new EuropeanExercise(exerciseDate);

         List<Date> fixingDates = new InitializedList<Date>(futureFixings);
         int dt = (int)(360 / futureFixings + 0.5);
         fixingDates[0] = today + dt;
         for (int j = 1; j < futureFixings; j++)
            fixingDates[j] = fixingDates[j - 1] + dt;

         DiscreteAveragingAsianOption option = new DiscreteAveragingAsianOption(averageType, runningAccumulator,
                                                                                pastFixings, fixingDates, payoff, exercise);
         option.setPricingEngine(engine);

         double calculated = option.NPV();
         double expected = 4.97109;
         double tolerance = 1e-5;
         if (Math.Abs(calculated - expected) > tolerance)
         {
            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                           fixingDates, payoff, exercise, spot.value(),
                           qRate.value(), rRate.value(), today,
                           vol.value(), expected, calculated, tolerance);
         }

      }

      [Fact]
      public void testMCDiscreteGeometricAveragePrice()
      {
         // Testing Monte Carlo discrete geometric average-price Asians
         // data from "Implementing Derivatives Model",
         // Clewlow, Strickland, p.118-123

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(100.0);
         SimpleQuote qRate = new SimpleQuote(0.03);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.06);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.20);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         BlackScholesMertonProcess stochProcess =
            new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                          new Handle<YieldTermStructure>(qTS),
                                          new Handle<YieldTermStructure>(rTS),
                                          new Handle<BlackVolTermStructure>(volTS));

         double tolerance = 4.0e-3;

         IPricingEngine engine =
            new MakeMCDiscreteGeometricAPEngine
         <LowDiscrepancy, Statistics>(stochProcess)
         .withSamples(8191)
         .value();

         Average.Type averageType = Average.Type.Geometric;
         double runningAccumulator = 1.0;
         int pastFixings = 0;
         int futureFixings = 10;
         Option.Type type = Option.Type.Call;
         double strike = 100.0;
         StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

         Date exerciseDate = today + 360;
         Exercise exercise = new EuropeanExercise(exerciseDate);

         List<Date> fixingDates = new InitializedList<Date>(futureFixings);
         int dt = (int)(360 / futureFixings + 0.5);
         fixingDates[0] = today + dt;
         for (int j = 1; j < futureFixings; j++)
            fixingDates[j] = fixingDates[j - 1] + dt;

         DiscreteAveragingAsianOption option =
            new DiscreteAveragingAsianOption(averageType, runningAccumulator,
                                             pastFixings, fixingDates,
                                             payoff, exercise);
         option.setPricingEngine(engine);

         double calculated = option.NPV();

         IPricingEngine engine2 = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);
         option.setPricingEngine(engine2);
         double expected = option.NPV();

         if (Math.Abs(calculated - expected) > tolerance)
         {
            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                           fixingDates, payoff, exercise, spot.value(),
                           qRate.value(), rRate.value(), today,
                           vol.value(), expected, calculated, tolerance);
         }
      }

      private void testDiscreteGeometricAveragePriceHeston(IPricingEngine engine, double[] tol)
      {
         // data from "A Recursive Method for Discretely Monitored Geometric Asian Option
         // Prices", Kim, Kim, Kim & Wee, Bull. Korean Math. Soc. 53, 733-749, 2016
         int[] days = [
           30, 91, 182, 365, 730, 1095,
           30, 91, 182, 365, 730, 1095,
           30, 91, 182, 365, 730, 1095
         ];
         double[] strikes = [
           90, 90, 90, 90, 90, 90,
           100, 100, 100, 100, 100, 100,
           110, 110, 110, 110, 110, 110
         ];

         // Prices from Tables 1, 2 and 3
         double[] prices = [
           10.2732, 10.9554, 11.9916, 13.6950, 16.1773, 18.0146,
           2.4389, 3.7881, 5.2132, 7.2243, 9.9948, 12.0639,
           0.1012, 0.5949, 1.4444, 2.9479, 5.3531, 7.3315
         ];

         DayCounter dc = new Actual365Fixed();
         var today = new Date(16, 09, 2015);
         Settings. setEvaluationDate(today);


         var spot = new SimpleQuote(100);
         var qRate = new SimpleQuote(0.0);
         var rRate = new SimpleQuote(0.05);

         var v0 = 0.09;

         var type = Option.Type.Call;
         var averageType = Average.Type.Geometric;

         var runningAccumulator = 1.0;
         var pastFixings = 0;

         for (var i=0; i<strikes.Length; i++)
         {
           var strike = strikes[i];
           var day = days[i];
           var expected = prices[i];
           var tolerance = tol[i];

           var futureFixings = (int)Math.Floor(day/7.0);
           List<Date> fixingDates = new InitializedList<Date>(futureFixings);

           var expiryDate = today + new Period(day,TimeUnit.Days);

           // I suppose "weekly fixings" roughly means this?
           for (var j=futureFixings-1; j>=0; j--)
           {
               fixingDates[j] = expiryDate - j * 7;
           }

           var europeanExercise = new EuropeanExercise(expiryDate);
           var payoff = new PlainVanillaPayoff(type, strike);

           var  option = new DiscreteAveragingAsianOption(averageType, runningAccumulator, pastFixings,
              fixingDates, payoff, europeanExercise);
           option.setPricingEngine(engine);

           var calculated = option.NPV();

           if (Math.Abs(calculated-expected) > tolerance)
           {
               REPORT_FAILURE("value", averageType, 1.0, (int?)0.0,
                          [], payoff, europeanExercise, spot.value(),
                          qRate.value(), rRate.value(), today,
                          Math.Sqrt(v0), expected, calculated, tolerance);
           }
         }
      }

      [Fact]
      private void testMCDiscreteGeometricAveragePriceHeston()
      {
         // Testing MC discrete geometric average-price Asians under Heston

         // 30-day options need wider tolerance due to uncertainty around what "weekly
         // fixing" dates mean over a 30-day month!
         double[] tol = [
            4.0e-2, 2.0e-2, 2.0e-2, 4.0e-2, 8.0e-2, 2.0e-1,
            1.0e-1, 4.0e-2, 3.0e-2, 2.0e-2, 9.0e-2, 2.0e-1,
            2.0e-2, 1.0e-2, 2.0e-2, 2.0e-2, 7.0e-2, 2.0e-1
         ];

         DayCounter dc = new Actual365Fixed();
         var today = new Date(16, 09, 2015);
         Settings.setEvaluationDate(today);

         var spot = new Handle<Quote>(new SimpleQuote(100));
         var qRate = new SimpleQuote(0.0);
         var qTS = Utilities.flatRate(today, qRate, dc);
         var rRate = new SimpleQuote(0.05);
         var rTS = Utilities.flatRate(today, rRate, dc);

         var v0 = 0.09;
         var kappa = 1.15;
         var theta = 0.0348;
         var sigma = 0.39;
         var rho = -0.64;

         var hestonProcess = new HestonProcess(new Handle<YieldTermStructure>(rTS), new Handle<YieldTermStructure>(qTS),
               spot, v0, kappa, theta, sigma, rho);

         IPricingEngine engine = new MakeMCDiscreteGeometricAPHestonEngine<LowDiscrepancy, Statistics>(hestonProcess)
               .withSamples(8191)
               .withSeed(43)
               .value();

         testDiscreteGeometricAveragePriceHeston(engine, tol);
      }


      [Fact]
      public void testAnalyticDiscreteGeometricAveragePriceGreeks()
      {
         // Testing discrete-averaging geometric Asian greeks

         using (SavedSettings backup = new SavedSettings())
         {
            Dictionary<string, double> calculated, expected, tolerance;
            calculated = new Dictionary<string, double>(6);
            expected = new Dictionary<string, double>(6);
            tolerance = new Dictionary<string, double>(6);
            tolerance["delta"]  = 1.0e-5;
            tolerance["gamma"]  = 1.0e-5;
            tolerance["theta"]  = 1.0e-5;
            tolerance["rho"]    = 1.0e-5;
            tolerance["divRho"] = 1.0e-5;
            tolerance["vega"]   = 1.0e-5;

            Option.Type[] types = { Option.Type.Call, Option.Type.Put };
            double[] underlyings = { 100.0 };
            double[] strikes = { 90.0, 100.0, 110.0 };
            double[] qRates = { 0.04, 0.05, 0.06 };
            double[] rRates = { 0.01, 0.05, 0.15 };
            int[] lengths = { 1, 2 };
            double[] vols = { 0.11, 0.50, 1.20 };

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

            BlackScholesMertonProcess process = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

            for (int i = 0; i < types.Length; i++)
            {
               for (int j = 0; j < strikes.Length; j++)
               {
                  for (int k = 0; k < lengths.Length; k++)
                  {
                     EuropeanExercise maturity = new EuropeanExercise(today + new Period(lengths[k], TimeUnit.Years));

                     PlainVanillaPayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                     double runningAverage = 120;
                     int pastFixings = 1;

                     List<Date> fixingDates = new List<Date>();
                     for (Date d = today + new Period(3, TimeUnit.Months);
                          d <= maturity.lastDate();
                          d += new Period(3, TimeUnit.Months))
                        fixingDates.Add(d);


                     IPricingEngine engine = new AnalyticDiscreteGeometricAveragePriceAsianEngine(process);

                     DiscreteAveragingAsianOption option = new DiscreteAveragingAsianOption(Average.Type.Geometric,
                                                                                            runningAverage, pastFixings, fixingDates, payoff, maturity);

                     option.setPricingEngine(engine);

                     for (int l = 0; l < underlyings.Length; l++)
                     {
                        for (int m = 0; m < qRates.Length; m++)
                        {
                           for (int n = 0; n < rRates.Length; n++)
                           {
                              for (int p = 0; p < vols.Length; p++)
                              {

                                 double u = underlyings[l];
                                 double q = qRates[m],
                                        r = rRates[n];
                                 double v = vols[p];
                                 spot.setValue(u);
                                 qRate.setValue(q);
                                 rRate.setValue(r);
                                 vol.setValue(v);

                                 double value = option.NPV();
                                 calculated["delta"] = option.delta();
                                 calculated["gamma"] = option.gamma();
                                 calculated["theta"] = option.theta();
                                 calculated["rho"] = option.rho();
                                 calculated["divRho"] = option.dividendRho();
                                 calculated["vega"] = option.vega();

                                 if (value > spot.value() * 1.0e-5)
                                 {
                                    // perturb spot and get delta and gamma
                                    double du = u * 1.0e-4;
                                    spot.setValue(u + du);
                                    double value_p = option.NPV(),
                                           delta_p = option.delta();
                                    spot.setValue(u - du);
                                    double value_m = option.NPV(),
                                           delta_m = option.delta();
                                    spot.setValue(u);
                                    expected["delta"] = (value_p - value_m) / (2 * du);
                                    expected["gamma"] = (delta_p - delta_m) / (2 * du);

                                    // perturb rates and get rho and dividend rho
                                    double dr = r * 1.0e-4;
                                    rRate.setValue(r + dr);
                                    value_p = option.NPV();
                                    rRate.setValue(r - dr);
                                    value_m = option.NPV();
                                    rRate.setValue(r);
                                    expected["rho"] = (value_p - value_m) / (2 * dr);

                                    double dq = q * 1.0e-4;
                                    qRate.setValue(q + dq);
                                    value_p = option.NPV();
                                    qRate.setValue(q - dq);
                                    value_m = option.NPV();
                                    qRate.setValue(q);
                                    expected["divRho"] = (value_p - value_m) / (2 * dq);

                                    // perturb volatility and get vega
                                    double dv = v * 1.0e-4;
                                    vol.setValue(v + dv);
                                    value_p = option.NPV();
                                    vol.setValue(v - dv);
                                    value_m = option.NPV();
                                    vol.setValue(v);
                                    expected["vega"] = (value_p - value_m) / (2 * dv);

                                    // perturb date and get theta
                                    double dT = dc.yearFraction(today - 1, today + 1);
                                    Settings.setEvaluationDate(today - 1);
                                    value_m = option.NPV();
                                    Settings.setEvaluationDate(today + 1);
                                    value_p = option.NPV();
                                    Settings.setEvaluationDate(today);
                                    expected["theta"] = (value_p - value_m) / dT;

                                    // compare
                                    foreach (KeyValuePair<string, double> kvp in calculated)
                                    {
                                       string greek = kvp.Key;
                                       double expct = expected[greek],
                                              calcl = calculated[greek],
                                              tol = tolerance[greek];
                                       double error = Utilities.relativeError(expct, calcl, u);
                                       if (error > tol)
                                       {
                                          REPORT_FAILURE(greek, Average.Type.Geometric,
                                                         runningAverage, pastFixings,
                                                         new List<Date>(),
                                                         payoff, maturity,
                                                         u, q, r, today, v,
                                                         expct, calcl, tol);
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

      [Fact]
      public void testMCDiscreteArithmeticAveragePrice()
      {
         // Testing Monte Carlo discrete arithmetic average-price Asians

         // data from "Asian Option", Levy, 1997
         // in "Exotic Options: The State of the Art",
         // edited by Clewlow, Strickland

         DiscreteAverageData[] cases4 =
         [
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 2,0.13, true, 1.3942835683),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 4,0.13, true, 1.5852442983),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 8,0.13, true, 1.66970673),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 12,0.13, true, 1.6980019214),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 26,0.13, true, 1.7255070456),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 52,0.13, true, 1.7401553533),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 100,0.13, true, 1.7478303712),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 250,0.13, true, 1.7490291943),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 500,0.13, true, 1.7515113291),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 1000,0.13, true, 1.7537344885),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 2,0.13, true, 1.8496053697),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 4,0.13, true, 2.0111495205),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 8,0.13, true, 2.0852138818),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 12,0.13, true, 2.1105094397),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 26,0.13, true, 2.1346526695),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 52,0.13, true, 2.147489651),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 100,0.13, true, 2.154728109),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 250,0.13, true, 2.1564276565),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 500,0.13, true, 2.1594238588),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 1000,0.13, true, 2.1595367326),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 2,0.13, true, 2.63315092584),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 4,0.13, true, 2.76723962361),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 8,0.13, true, 2.83124836881),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 12,0.13, true, 2.84290301412),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 26,0.13, true, 2.88179560417),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 52,0.13, true, 2.88447044543),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 100,0.13, true, 2.89985329603),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 250,0.13, true, 2.90047296063),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 500,0.13, true, 2.89813412160),
              new(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 1000,0.13, true, 2.89703362437)
         ];

         DayCounter dc = new Actual360();
         var today = new Date(16, 09, 2015);
         Settings. setEvaluationDate(today);
         var spot = new SimpleQuote(100.0);
         var qRate = new SimpleQuote(0.03);
         var qTS = Utilities.flatRate(today, qRate, dc);
         var rRate = new SimpleQuote(0.06);
         var rTS = Utilities.flatRate(today, rRate, dc);
         var vol = new SimpleQuote(0.20);
         var volTS = Utilities.flatVol(today, vol, dc);

         var averageType = Average.Type.Arithmetic;
         var runningSum = 0.0;
         var pastFixings = 0;

         foreach (var l in cases4)
         {
            StrikedTypePayoff payoff = new PlainVanillaPayoff(l.type, l.strike);

            var dt = l.length / (l.fixings - 1);
            List<double> timeIncrements = new InitializedList<double>(l.fixings);
            List<Date> fixingDates = new InitializedList<Date>(l.fixings);
            timeIncrements[0] = l.first;
            fixingDates[0] = today + Utilities.timeToDays(timeIncrements[0]);
            for (var i = 1; i < l.fixings; i++)
            {
               timeIncrements[i] = i * dt + l.first;
               fixingDates[i] = today + Utilities.timeToDays(timeIncrements[i]);
            }
            Exercise exercise = new EuropeanExercise(fixingDates[l.fixings - 1]);

            spot.setValue(l.underlying);
            qRate.setValue(l.dividendYield);
            rRate.setValue(l.riskFreeRate);
            vol.setValue(l.volatility);

            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine =
                new MakeMCDiscreteArithmeticAPEngine<LowDiscrepancy, Statistics>(stochProcess)
                    .withSamples(2047)
                    .withControlVariate(l.controlVariate)
                    .value();
            var option = new DiscreteAveragingAsianOption(averageType, runningSum, pastFixings, fixingDates, payoff, exercise);
            option.setPricingEngine(engine);

            double calculated = option.NPV();
            double expected = l.result;
            double tolerance = 2.0e-2;
            if (Math.Abs(calculated - expected) > tolerance)
            {
               REPORT_FAILURE("value", averageType, runningSum, pastFixings,
                           fixingDates, payoff, exercise, spot.value(),
                           qRate.value(), rRate.value(), today,
                           vol.value(), expected, calculated, tolerance);
            }
         }
      }

      [Fact]
      public void testMCDiscreteArithmeticAverageStrike()
      {

         // Testing Monte Carlo discrete arithmetic average-strike Asians

         // data from "Asian Option", Levy, 1997
         // in "Exotic Options: The State of the Art",
         // edited by Clewlow, Strickland
         DiscreteAverageData[] cases5 =
         [
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 2, 0.13, true, 1.51917595129 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 4, 0.13, true, 1.67940165674 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 8, 0.13, true, 1.75371215251 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 12, 0.13, true, 1.77595318693 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 26, 0.13, true, 1.81430536630 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 52, 0.13, true, 1.82269246898 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 100, 0.13, true, 1.83822402464 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 250, 0.13, true, 1.83875059026 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 500, 0.13, true, 1.83750703638 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 1000, 0.13, true, 1.83887181884 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 2, 0.13, true, 1.51154400089 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 4, 0.13, true, 1.67103508506 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 8, 0.13, true, 1.74529684070 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 12, 0.13, true, 1.76667074564 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 26, 0.13, true, 1.80528400613 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 52, 0.13, true, 1.81400883891 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 100, 0.13, true, 1.82922901451 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 250, 0.13, true, 1.82937111773 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 500, 0.13, true, 1.82826193186 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 1000, 0.13, true, 1.82967846654 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 2, 0.13, true, 1.49648170891 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 4, 0.13, true, 1.65443100462 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 8, 0.13, true, 1.72817806731 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 12, 0.13, true, 1.74877367895 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 26, 0.13, true, 1.78733801988 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 52, 0.13, true, 1.79624826757 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 100, 0.13, true, 1.81114186876 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 250, 0.13, true, 1.81101152587 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 500, 0.13, true, 1.81002311939 ),
            new(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 1000, 0.13, true, 1.81145760308 )
         ];

         DayCounter dc = new Actual360();
         var today = new Date(16, 09, 2015);
         Settings. setEvaluationDate(today);

         var spot = new SimpleQuote(100.0);
         var qRate = new SimpleQuote(0.03);
         var qTS = Utilities.flatRate(today, qRate, dc);
         var rRate = new SimpleQuote(0.06);
         var rTS = Utilities.flatRate(today, rRate, dc);
         var vol = new SimpleQuote(0.20);
         var volTS = Utilities.flatVol(today, vol, dc);

         var averageType = QLNet.Average.Type.Arithmetic;
         var runningSum = 0.0;
         var pastFixings = 0;
         for (var l = 0; l < cases5.Length; l++)
         {
            StrikedTypePayoff payoff = new PlainVanillaPayoff(cases5[l].type, cases5[l].strike);

            var dt = cases5[l].length / (cases5[l].fixings - 1);
            List<double> timeIncrements = new InitializedList<double>(cases5[l].fixings);
            List<Date> fixingDates = new InitializedList<Date>(cases5[l].fixings);
            timeIncrements[0] = cases5[l].first;
            fixingDates[0] = today + (int)(timeIncrements[0] * 360 + 0.5);
            for (var i = 1; i < cases5[l].fixings; i++)
            {
               timeIncrements[i] = i * dt + cases5[l].first;
               fixingDates[i] = today + Utilities.timeToDays(timeIncrements[i]);
            }
            Exercise exercise = new EuropeanExercise(fixingDates[cases5[l].fixings - 1]);

            spot.setValue(cases5[l].underlying);
            qRate.setValue(cases5[l].dividendYield);
            rRate.setValue(cases5[l].riskFreeRate);
            vol.setValue(cases5[l].volatility);

            BlackScholesMertonProcess stochProcess =
                new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                            new Handle<YieldTermStructure>(qTS),
                                            new Handle<YieldTermStructure>(rTS),
                                            new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine =
                new MakeMCDiscreteArithmeticASEngine<LowDiscrepancy, Statistics>(stochProcess)
                .withSeed(3456789)
                .withSamples(1023)
                .value();

            DiscreteAveragingAsianOption option =
                new DiscreteAveragingAsianOption(averageType, runningSum,
                                                pastFixings, fixingDates,
                                                payoff, exercise);
            option.setPricingEngine(engine);

            var calculated = option.NPV();
            var expected = cases5[l].result;
            var tolerance = 2.0e-2;
            if (Math.Abs(calculated - expected) > tolerance)
            {
               REPORT_FAILURE("value", averageType, runningSum, pastFixings,
                              fixingDates, payoff, exercise, spot.value(),
                              qRate.value(), rRate.value(), today,
                              vol.value(), expected, calculated, tolerance);
            }
         }
      }

      [Fact]
      public void testPastFixings()
      {
         // Testing use of past fixings in Asian options...");
         DayCounter dc = new Actual360();
         var today = new Date(16, 09, 2015);
         Settings.setEvaluationDate(today);

         SimpleQuote spot = new SimpleQuote(100.0);
         SimpleQuote qRate = new SimpleQuote(0.03);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.06);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.20);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, 100.0);


         Exercise exercise = new EuropeanExercise(today + new Period(1, TimeUnit.Years));

         BlackScholesMertonProcess stochProcess =
             new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                           new Handle<YieldTermStructure>(qTS),
                                           new Handle<YieldTermStructure>(rTS),
                                           new Handle<BlackVolTermStructure>(volTS));

         // MC arithmetic average-price
         double runningSum = 0.0;
         int pastFixings = 0;
         List<Date> fixingDates1 = new InitializedList<Date>();
         for (int i = 0; i <= 12; ++i)
            fixingDates1.Add(today + new Period(i, TimeUnit.Months));

         DiscreteAveragingAsianOption option1 =
             new DiscreteAveragingAsianOption(Average.Type.Arithmetic, runningSum,
                                              pastFixings, fixingDates1,
                                              payoff, exercise);

         pastFixings = 2;
         runningSum = pastFixings * spot.value() * 0.8;
         List<Date> fixingDates2 = new InitializedList<Date>();
         for (int i = -2; i <= 12; ++i)
            fixingDates2.Add(today + new Period(i, TimeUnit.Months));

         DiscreteAveragingAsianOption option2 =
             new DiscreteAveragingAsianOption(Average.Type.Arithmetic, runningSum,
                                              pastFixings, fixingDates2,
                                              payoff, exercise);

         IPricingEngine engine =
            new MakeMCDiscreteArithmeticAPEngine<LowDiscrepancy, Statistics>(stochProcess)
             .withSamples(2047)
             .value();

         option1.setPricingEngine(engine);
         option2.setPricingEngine(engine);

         double price1 = option1.NPV();
         double price2 = option2.NPV();

         if (Utils.close(price1, price2))
         {
            QAssert.Fail(
                 "past fixings had no effect on arithmetic average-price option"
                 + "\n  without fixings: " + price1
                 + "\n  with fixings:    " + price2);
         }

         // MC arithmetic average-strike
         engine = new MakeMCDiscreteArithmeticASEngine<LowDiscrepancy, Statistics>(stochProcess)
             .withSamples(2047)
             .value();

         option1.setPricingEngine(engine);
         option2.setPricingEngine(engine);

         price1 = option1.NPV();
         price2 = option2.NPV();

         if (Utils.close(price1, price2))
         {
            QAssert.Fail(
                 "past fixings had no effect on arithmetic average-strike option"
                 + "\n  without fixings: " + price1
                 + "\n  with fixings:    " + price2);
         }

         // analytic geometric average-price
         double runningProduct = 1.0;
         pastFixings = 0;

         DiscreteAveragingAsianOption option3 =
             new DiscreteAveragingAsianOption(Average.Type.Geometric, runningProduct,
                                              pastFixings, fixingDates1,
                                              payoff, exercise);

         pastFixings = 2;
         runningProduct = spot.value() * spot.value();

         DiscreteAveragingAsianOption option4 =
             new DiscreteAveragingAsianOption(Average.Type.Geometric, runningProduct,
                                              pastFixings, fixingDates2,
                                              payoff, exercise);

         engine = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

         option3.setPricingEngine(engine);
         option4.setPricingEngine(engine);

         double price3 = option3.NPV();
         double price4 = option4.NPV();

         if (Utils.close(price3, price4))
         {
            QAssert.Fail(
                 "past fixings had no effect on geometric average-price option"
                 + "\n  without fixings: " + price3
                 + "\n  with fixings:    " + price4);
         }

         // MC geometric average-price
         engine = new MakeMCDiscreteGeometricAPEngine<LowDiscrepancy, Statistics>(stochProcess)
                     .withSamples(2047)
                     .value();

         option3.setPricingEngine(engine);
         option4.setPricingEngine(engine);

         price3 = option3.NPV();
         price4 = option4.NPV();

         if (Utils.close(price3, price4))
         {
            QAssert.Fail(
                 "past fixings had no effect on geometric average-price option"
                 + "\n  without fixings: " + price3
                 + "\n  with fixings:    " + price4);
         }
      }
   }
}
