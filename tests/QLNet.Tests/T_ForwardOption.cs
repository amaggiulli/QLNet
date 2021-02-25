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
   public class T_ForwardOption
   {
      void REPORT_FAILURE(string greekName,
                          StrikedTypePayoff payoff,
                          Exercise exercise,
                          double s,
                          double q,
                          double r,
                          Date today,
                          double v,
                          double moneyness,
                          Date reset,
                          double expected,
                          double calculated,
                          double error,
                          double tolerance)
      {
         QAssert.Fail("Forward " + exercise + " "
                      + payoff.optionType() + " option with "
                      + payoff + " payoff:\n"
                      + "    spot value:       " + s + "\n"
                      + "    strike:           " + payoff.strike() + "\n"
                      + "    moneyness:        " + moneyness + "\n"
                      + "    dividend yield:   " + q + "\n"
                      + "    risk-free rate:   " + r + "\n"
                      + "    reference date:   " + today + "\n"
                      + "    reset date:       " + reset + "\n"
                      + "    maturity:         " + exercise.lastDate() + "\n"
                      + "    volatility:       " + v + "\n\n"
                      + "    expected " + greekName + ":   " + expected + "\n"
                      + "    calculated " + greekName + ": " + calculated + "\n"
                      + "    error:            " + error + "\n"
                      + "    tolerance:        " + tolerance);
      }


      public class ForwardOptionData
      {
         public ForwardOptionData(Option.Type type_, double moneyness_, double s_, double q_, double r_, double start_,
                                  double t_, double v_, double result_, double tol_)
         {
            type = type_;
            moneyness = moneyness_;
            s = s_;
            q = q_;
            r = r_;
            start = start_;
            t = t_;
            v = v_;
            result = result_;
            tol = tol_;
         }

         public Option.Type type;
         public double moneyness;
         public double s;          // spot
         public double q;          // dividend
         public double r;          // risk-free rate
         public double start;      // time to reset
         public double t;          // time to maturity
         public double v;          // volatility
         public double result;     // expected result
         public double tol;        // tolerance
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testValues()
      {
         // Testing forward option values...

         /* The data below are from
            "Option pricing formulas", E.G. Haug, McGraw-Hill 1998
         */
         ForwardOptionData[] values =
         {
            //  type, moneyness, spot,  div, rate,start,   t,  vol, result, tol
            // "Option pricing formulas", pag. 37
            new ForwardOptionData(Option.Type.Call, 1.1, 60.0, 0.04, 0.08, 0.25, 1.0, 0.30, 4.4064, 1.0e-4),
            // "Option pricing formulas", VBA code
            new ForwardOptionData(Option.Type.Put, 1.1, 60.0, 0.04, 0.08, 0.25, 1.0, 0.30, 8.2971, 1.0e-4)
         };

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(0.0);
         SimpleQuote qRate = new SimpleQuote(0.0);
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, qRate, dc));
         SimpleQuote rRate = new SimpleQuote(0.0);
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, rRate, dc));
         SimpleQuote vol = new SimpleQuote(0.0);
         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

         BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                new Handle<YieldTermStructure>(qTS), new Handle<YieldTermStructure>(rTS),
                                                                                new Handle<BlackVolTermStructure>(volTS));

         IPricingEngine engine = new ForwardVanillaEngine(stochProcess, process => new AnalyticEuropeanEngine(process));  // AnalyticEuropeanEngine

         for (int i = 0; i < values.Length; i++)
         {

            StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, 0.0);
            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise exercise = new EuropeanExercise(exDate);
            Date reset = today + Convert.ToInt32(values[i].start * 360 + 0.5);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            ForwardVanillaOption option = new ForwardVanillaOption(values[i].moneyness, reset, payoff, exercise);
            option.setPricingEngine(engine);

            double calculated = option.NPV();
            double error = Math.Abs(calculated - values[i].result);
            double tolerance = 1e-4;
            if (error > tolerance)
            {
               REPORT_FAILURE("value", payoff, exercise, values[i].s,
                              values[i].q, values[i].r, today,
                              values[i].v, values[i].moneyness, reset,
                              values[i].result, calculated,
                              error, tolerance);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testPerformanceValues()
      {
         // Testing forward performance option values...

         /* The data below are the performance equivalent of the
            forward options tested above and taken from
            "Option pricing formulas", E.G. Haug, McGraw-Hill 1998
         */
         ForwardOptionData[] values =
         {
            //  type, moneyness, spot,  div, rate,start, maturity,  vol,                       result, tol
            new ForwardOptionData(Option.Type.Call, 1.1, 60.0, 0.04, 0.08, 0.25,      1.0, 0.30, 4.4064 / 60 * Math.Exp(-0.04 * 0.25), 1.0e-4),
            new ForwardOptionData(Option.Type.Put, 1.1, 60.0, 0.04, 0.08, 0.25,      1.0, 0.30, 8.2971 / 60 * Math.Exp(-0.04 * 0.25), 1.0e-4)
         };

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(0.0);
         SimpleQuote qRate = new SimpleQuote(0.0);
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, qRate, dc));
         SimpleQuote rRate = new SimpleQuote(0.0);
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, rRate, dc));
         SimpleQuote vol = new SimpleQuote(0.0);
         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

         BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                new Handle<YieldTermStructure>(qTS), new Handle<YieldTermStructure>(rTS),
                                                                                new Handle<BlackVolTermStructure>(volTS));

         IPricingEngine engine = new ForwardPerformanceVanillaEngine(stochProcess, process => new AnalyticEuropeanEngine(process));     // AnalyticEuropeanEngine

         for (int i = 0; i < values.Length; i++)
         {
            StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, 0.0);
            Date exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
            Exercise exercise = new EuropeanExercise(exDate);
            Date reset = today + Convert.ToInt32(values[i].start * 360 + 0.5);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            ForwardVanillaOption option = new ForwardVanillaOption(values[i].moneyness, reset, payoff, exercise);
            option.setPricingEngine(engine);

            double calculated = option.NPV();
            double error = Math.Abs(calculated - values[i].result);
            double tolerance = 1e-4;
            if (error > tolerance)
            {
               REPORT_FAILURE("value", payoff, exercise, values[i].s,
                              values[i].q, values[i].r, today,
                              values[i].v, values[i].moneyness, reset,
                              values[i].result, calculated,
                              error, tolerance);
            }
         }
      }

      private void testForwardGreeks(Type engine_type)
      {
         Dictionary<String, double> calculated = new Dictionary<string, double>(),
         expected = new Dictionary<string, double>(),
         tolerance = new Dictionary<string, double>();
         tolerance["delta"]   = 1.0e-5;
         tolerance["gamma"]   = 1.0e-5;
         tolerance["theta"]   = 1.0e-5;
         tolerance["rho"]     = 1.0e-5;
         tolerance["divRho"]  = 1.0e-5;
         tolerance["vega"]    = 1.0e-5;

         Option.Type[] types = { Option.Type.Call, Option.Type.Put };
         double[] moneyness = { 0.9, 1.0, 1.1 };
         double[] underlyings = { 100.0 };
         double[] qRates = { 0.04, 0.05, 0.06 };
         double[] rRates = { 0.01, 0.05, 0.15 };
         int[] lengths = { 1, 2 };
         int[] startMonths = { 6, 9 };
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

         BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

         IPricingEngine engine = engine_type == typeof(ForwardVanillaEngine) ? new ForwardVanillaEngine(stochProcess, process => new AnalyticEuropeanEngine(process)) :
                                 new ForwardPerformanceVanillaEngine(stochProcess, process => new AnalyticEuropeanEngine(process));

         for (int i = 0; i < types.Length; i++)
         {
            for (int j = 0; j < moneyness.Length; j++)
            {
               for (int k = 0; k < lengths.Length; k++)
               {
                  for (int h = 0; h < startMonths.Length; h++)
                  {

                     Date exDate = today + new Period(lengths[k], TimeUnit.Years);
                     Exercise exercise = new EuropeanExercise(exDate);

                     Date reset = today + new Period(startMonths[h], TimeUnit.Months);

                     StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], 0.0);

                     ForwardVanillaOption option = new ForwardVanillaOption(moneyness[j], reset, payoff, exercise);
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
                                 calculated["delta"]   = option.delta();
                                 calculated["gamma"]   = option.gamma();
                                 calculated["theta"]   = option.theta();
                                 calculated["rho"]     = option.rho();
                                 calculated["divRho"]  = option.dividendRho();
                                 calculated["vega"]    = option.vega();

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
                                    //std::map<std::string,double>::iterator it;
                                    foreach (KeyValuePair<string, double> it in calculated)
                                    {
                                       String greek = it.Key;
                                       double expct = expected  [greek],
                                              calcl = calculated[greek],
                                              tol   = tolerance [greek];
                                       double error = Utilities.relativeError(expct, calcl, u);
                                       if (error > tol)
                                       {
                                          REPORT_FAILURE(greek, payoff, exercise,
                                                         u, q, r, today, v,
                                                         moneyness[j], reset,
                                                         expct, calcl, error, tol);
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

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testGreeks()
      {
         // Testing forward option greeks
         SavedSettings backup = new SavedSettings();

         testForwardGreeks(typeof(ForwardVanillaEngine));
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testPerformanceGreeks()
      {
         // Testing forward performance option greeks
         SavedSettings backup = new SavedSettings();

         testForwardGreeks(typeof(ForwardPerformanceVanillaEngine));
      }

      class TestBinomialEngine : BinomialVanillaEngine<CoxRossRubinstein>
      {
         public TestBinomialEngine(GeneralizedBlackScholesProcess process):
            base(process, 300) // fixed steps
         {}
      }

      // verify than if engine
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testGreeksInitialization()
      {
         // Testing forward option greeks initialization
         DayCounter dc = new Actual360();
         SavedSettings backup = new SavedSettings();
         Date today = Date.Today;
         Settings.setEvaluationDate(today);

         SimpleQuote spot = new SimpleQuote(100.0);
         SimpleQuote qRate = new SimpleQuote(0.04);
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
         SimpleQuote rRate = new SimpleQuote(0.01);
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
         SimpleQuote vol = new SimpleQuote(0.11);
         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

         BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

         IPricingEngine engine = new ForwardVanillaEngine(stochProcess, process => new TestBinomialEngine(process));
         Date exDate = today + new Period(1, TimeUnit.Years);
         Exercise exercise = new EuropeanExercise(exDate);
         Date reset = today + new Period(6, TimeUnit.Months);
         StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, 0.0);

         ForwardVanillaOption option = new ForwardVanillaOption(0.9, reset, payoff, exercise);
         option.setPricingEngine(engine);

         IPricingEngine ctrlengine = new TestBinomialEngine(stochProcess);
         VanillaOption ctrloption = new VanillaOption(payoff, exercise);
         ctrloption.setPricingEngine(ctrlengine);

         double? delta = 0;
         try
         {
            delta = ctrloption.delta();
         }
         catch (Exception)
         {
            // if normal option can't calculate delta,
            // nor should forward
            try
            {
               delta   = option.delta();
            }
            catch (Exception)
            {
               delta = null;
            }
            Utils.QL_REQUIRE(delta == null, () => "Forward delta invalid");
         }

         double? rho  = 0;
         try
         {
            rho = ctrloption.rho();
         }
         catch (Exception)
         {
            // if normal option can't calculate rho,
            // nor should forward
            try
            {
               rho = option.rho();
            }
            catch (Exception)
            {
               rho = null;
            }
            Utils.QL_REQUIRE(rho == null, () => "Forward rho invalid");
         }

         double? divRho = 0;
         try
         {
            divRho = ctrloption.dividendRho();
         }
         catch (Exception)
         {
            // if normal option can't calculate divRho,
            // nor should forward
            try
            {
               divRho = option.dividendRho();
            }
            catch (Exception)
            {
               divRho = null;
            }
            Utils.QL_REQUIRE(divRho == null, () => "Forward dividendRho invalid");
         }

         double? vega = 0;
         try
         {
            vega = ctrloption.vega();
         }
         catch (Exception)
         {
            // if normal option can't calculate vega,
            // nor should forward
            try
            {
               vega = option.vega();
            }
            catch (Exception)
            {
               vega = null;
            }
            Utils.QL_REQUIRE(vega == null, () => "Forward vega invalid");
         }
      }
   }
}
