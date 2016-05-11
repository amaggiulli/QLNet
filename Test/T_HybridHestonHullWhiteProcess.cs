//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   [TestClass()]
   public class T_HybridHestonHullWhiteProcess
   {
      [TestMethod()]
      public void testBsmHullWhiteEngine()
      {
         // Testing European option pricing for a BSM process with one-factor Hull-White model
         SavedSettings backup = new SavedSettings();

         DayCounter dc = new Actual365Fixed();

         Date today = Date.Today;
         Date maturity = today + new Period(20, TimeUnit.Years);

         Settings.setEvaluationDate(today);

         Handle<Quote> spot = new Handle<Quote>(new SimpleQuote(100.0));
         SimpleQuote qRate  = new SimpleQuote(0.04);
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, qRate, dc));
         SimpleQuote rRate = new SimpleQuote(0.0525);
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, rRate, dc));
         SimpleQuote vol = new SimpleQuote(0.25);
         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

         // FLOATING_POINT_EXCEPTION
         HullWhite hullWhiteModel = new HullWhite(new Handle<YieldTermStructure>(rTS), 0.00883, 0.00526);

         BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(spot, qTS, rTS, volTS);

         Exercise exercise = new EuropeanExercise(maturity);

         double fwd = spot.link.value()*qTS.link.discount(maturity)/rTS.link.discount(maturity);
         StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, fwd);

         EuropeanOption option = new EuropeanOption(payoff, exercise);

         double tol = 1e-8;
         double[] corr = {-0.75,-0.25,0.0,0.25,0.75};
         double[] expectedVol = {0.217064577,0.243995801,0.256402830,0.268236596,0.290461343};

         for (int i = 0; i < corr.Length; ++i)
         {
            IPricingEngine bsmhwEngine = new AnalyticBSMHullWhiteEngine(corr[i], stochProcess,hullWhiteModel);

            option.setPricingEngine(bsmhwEngine);
            double npv = option.NPV();

            Handle<BlackVolTermStructure> compVolTS = new Handle<BlackVolTermStructure>(
               Utilities.flatVol(today, expectedVol[i], dc));

            BlackScholesMertonProcess bsProcess = new BlackScholesMertonProcess(spot, qTS, rTS, compVolTS);
            IPricingEngine bsEngine = new AnalyticEuropeanEngine(bsProcess);

            EuropeanOption comp = new EuropeanOption(payoff, exercise);
            comp.setPricingEngine(bsEngine);

            double impliedVol = comp.impliedVolatility(npv, bsProcess, 1e-10, 100);

            if (Math.Abs(impliedVol - expectedVol[i]) > tol)
            {
               Assert.Fail("Failed to reproduce implied volatility"
                          + "\n    calculated: " + impliedVol
                          + "\n    expected  : " + expectedVol[i]);
            }
            if (Math.Abs((comp.NPV() - npv)/npv) > tol)
            {
               Assert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
            if (Math.Abs(comp.delta() - option.delta()) > tol)
            {
               Assert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
            if (Math.Abs((comp.gamma() - option.gamma())/npv) > tol)
            {
               Assert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
            if (Math.Abs((comp.theta() - option.theta())/npv) > tol)
            {
               Assert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
            if (Math.Abs((comp.vega() - option.vega())/npv) > tol)
            {
               Assert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
         }
      }

      [TestMethod()]
      public void testCompareBsmHWandHestonHW() 
      {
         // Comparing European option pricing for a BSM process with one-factor Hull-White model
         SavedSettings backup = new SavedSettings();
         DayCounter dc = new Actual365Fixed();
         Date today = Date.Today;
         Settings.setEvaluationDate(today);

         Handle<Quote> spot = new Handle<Quote>(new SimpleQuote(100.0));
         List<Date> dates = new List<Date>();
         List<double> rates = new List<double>(), divRates = new List<double>();

         for (int i = 0; i <= 40; ++i)
         {
            dates.Add(today + new Period(i, TimeUnit.Years));
            // FLOATING_POINT_EXCEPTION
            rates.Add(0.01 + 0.0002*Math.Exp(Math.Sin(i/4.0)));
            divRates.Add(0.02 + 0.0001*Math.Exp(Math.Sin(i/5.0)));
         }

         Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100));
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(
            new InterpolatedZeroCurve<Linear>(dates, rates, dc));
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(
            new InterpolatedZeroCurve<Linear>(dates, divRates, dc));

         SimpleQuote vol = new SimpleQuote(0.25);
         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

         BlackScholesMertonProcess bsmProcess = new BlackScholesMertonProcess(spot, qTS, rTS, volTS);

         HestonProcess hestonProcess = new HestonProcess(rTS, qTS, spot,
            vol.value()*vol.value(), 1.0, vol.value()*vol.value(), 1e-4, 0.0);

         HestonModel hestonModel = new HestonModel(hestonProcess);

         HullWhite hullWhiteModel = new HullWhite(new Handle<YieldTermStructure>(rTS), 0.01, 0.01);

         IPricingEngine bsmhwEngine = new AnalyticBSMHullWhiteEngine(0.0, bsmProcess, hullWhiteModel);

         IPricingEngine hestonHwEngine = new AnalyticHestonHullWhiteEngine(hestonModel, hullWhiteModel, 128);

         double tol = 1e-5;
         double[] strike = {0.25,0.5,0.75,0.8,0.9,1.0,1.1,1.2,1.5,2.0,4.0};
         int[] maturity = {1,2,3,5,10,15,20,25,30};
         Option.Type[] types = {Option.Type.Put,Option.Type.Call};

         for (int i = 0; i < types.Length; ++i)
         {
            for (int j = 0; j < strike.Length; ++j)
            {
               for (int l = 0; l < maturity.Length; ++l)
               {
                  Date maturityDate = today + new Period(maturity[l], TimeUnit.Years);

                  Exercise exercise = new EuropeanExercise(maturityDate);

                  double fwd = strike[j]*spot.link.value() 
                     * qTS.link.discount(maturityDate)/rTS.link.discount(maturityDate);

                  StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], fwd);

                  EuropeanOption option = new EuropeanOption(payoff, exercise);

                  option.setPricingEngine(bsmhwEngine);
                  double calculated = option.NPV();

                  option.setPricingEngine(hestonHwEngine);
                  double expected = option.NPV();

                  if (Math.Abs(calculated - expected) > calculated*tol &&
                      Math.Abs(calculated - expected) > tol)
                  {
                     Assert.Fail("Failed to reproduce npvs"
                                 + "\n    calculated: " + calculated
                                 + "\n    expected  : " + expected
                                 + "\n    strike    : " + strike[j]
                                 + "\n    maturity  : " + maturity[l]
                                 + "\n    type      : "
                                 + ((types[i] == QLNet.Option.Type.Put) ? "Put" : "Call"));
                  }
               }
            }
         }
      }
   }
}
