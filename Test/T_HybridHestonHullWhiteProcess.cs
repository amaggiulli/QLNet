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
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
   public class T_HybridHestonHullWhiteProcess : IDisposable
   {
      #region Initialize&Cleanup
      private SavedSettings backup;
      #if QL_DOTNET_FRAMEWORK
      [TestInitialize]
      public void testInitialize()
      {
      #else
      public T_HybridHestonHullWhiteProcess()
      {
      #endif

         backup = new SavedSettings();
      }
      #if QL_DOTNET_FRAMEWORK
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

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testBsmHullWhiteEngine()
      {
         // Testing European option pricing for a BSM process with one-factor Hull-White model
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
               QAssert.Fail("Failed to reproduce implied volatility"
                          + "\n    calculated: " + impliedVol
                          + "\n    expected  : " + expectedVol[i]);
            }
            if (Math.Abs((comp.NPV() - npv)/npv) > tol)
            {
               QAssert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
            if (Math.Abs(comp.delta() - option.delta()) > tol)
            {
               QAssert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
            if (Math.Abs((comp.gamma() - option.gamma())/npv) > tol)
            {
               QAssert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
            if (Math.Abs((comp.theta() - option.theta())/npv) > tol)
            {
               QAssert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
            if (Math.Abs((comp.vega() - option.vega())/npv) > tol)
            {
               QAssert.Fail("Failed to reproduce NPV"
                          + "\n    calculated: " + npv
                          + "\n    expected  : " + comp.NPV());
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testCompareBsmHWandHestonHW() 
      {
         // Comparing European option pricing for a BSM process with one-factor Hull-White model
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
                     QAssert.Fail("Failed to reproduce npvs"
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

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testZeroBondPricing() 
      {
         // Testing Monte-Carlo zero bond pricing

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         Settings.setEvaluationDate(today);

         // construct a strange yield curve to check drifts and discounting
         // of the joint stochastic process

         List<Date> dates = new List<Date>();
         List<double> times = new List<double>();
         List<double> rates = new List<double>();

         dates.Add(today);
         rates.Add(0.02);
         times.Add(0.0);
         for (int i = 120; i < 240; ++i)
         {
            dates.Add(today + new Period(i, TimeUnit.Months));
            rates.Add(0.02 + 0.0002*Math.Exp(Math.Sin(i/8.0)));
            times.Add(dc.yearFraction(today, dates.Last()));
         }

         Date maturity = dates.Last() + new Period(10, TimeUnit.Years);
         dates.Add(maturity);
         rates.Add(0.04);
         times.Add(dc.yearFraction(today, dates.Last()));

         Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100));

         Handle<YieldTermStructure> ts = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, rates, dc));
         Handle<YieldTermStructure> ds = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.0, dc));

         HestonProcess hestonProcess = new HestonProcess(ts, ds, s0, 0.02, 1.0, 0.2, 0.5, -0.8);
         HullWhiteForwardProcess hwProcess = new HullWhiteForwardProcess(ts, 0.05, 0.05);
         hwProcess.setForwardMeasureTime(dc.yearFraction(today, maturity));
         HullWhite hwModel = new HullWhite(ts, 0.05, 0.05);

         HybridHestonHullWhiteProcess jointProcess = new HybridHestonHullWhiteProcess(hestonProcess, hwProcess, -0.4);

         TimeGrid grid = new TimeGrid(times,times.Count - 1);

         int factors = jointProcess.factors();
         int steps = grid.size() - 1;
         SobolBrownianBridgeRsg rsg = new SobolBrownianBridgeRsg(factors, steps);
         MultiPathGenerator<SobolBrownianBridgeRsg> generator = new MultiPathGenerator<SobolBrownianBridgeRsg>(
            jointProcess, grid, rsg, false);

         int m = 90;
         List<GeneralStatistics> zeroStat = new InitializedList<GeneralStatistics>(m);
         List<GeneralStatistics> optionStat = new InitializedList<GeneralStatistics>(m);

         int nrTrails = 8191;
         int optionTenor = 24;
         double strike = 0.5;

         for (int i = 0; i < nrTrails; ++i)
         {
            Sample<IPath> path = generator.next();
            MultiPath value = path.value as MultiPath;
            Utils.QL_REQUIRE( value != null, () => "Invalid Path" );

            for (int j = 1; j < m; ++j)
            {
               double t = grid[j]; // zero end and option maturity
               double T = grid[j + optionTenor]; // maturity of zero bond
               // of option

               Vector states = new Vector(3);
               Vector optionStates = new Vector(3);
               for (int k = 0; k < jointProcess.size(); ++k)
               {
                  states[k] = value[k][j];
                  optionStates[k] = value[k][j + optionTenor];
               }

               double zeroBond
                  = 1.0/jointProcess.numeraire(t, states);
               double zeroOption = zeroBond * Math.Max(0.0, hwModel.discountBond(t, T, states[2]) - strike);

               zeroStat[j].add(zeroBond);
               optionStat[j].add(zeroOption);
            }
         }

         for (int j = 1; j < m; ++j)
         {
            double t = grid[j];
            double calculated = zeroStat[j].mean();
            double expected = ts.link.discount(t);

            if (Math.Abs(calculated - expected) > 0.03)
            {
               QAssert.Fail("Failed to reproduce expected zero bond prices"
                           + "\n   t:          " + t
                           + "\n   calculated: " + calculated
                           + "\n   expected:   " + expected);
            }

            double T = grid[j + optionTenor];

            calculated = optionStat[j].mean();
            expected = hwModel.discountBondOption(Option.Type.Call, strike, t, T);

            if (Math.Abs(calculated - expected) > 0.0035)
            {
               QAssert.Fail("Failed to reproduce expected zero bond option prices"
                           + "\n   t:          " + t
                           + "\n   T:          " + T
                           + "\n   calculated: " + calculated
                           + "\n   expected:   " + expected);
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testMcVanillaPricing() 
      {
        // Testing Monte-Carlo vanilla option pricing
         DayCounter dc = new Actual360();
         Date today = Date.Today;

         Settings.setEvaluationDate(today);

         // construct a strange yield curve to check drifts and discounting
         // of the joint stochastic process

         List<Date> dates = new List<Date>();
         List<double> times = new List<double>();
         List<double> rates = new List<double>(), divRates = new List<double>();

         for (int i = 0; i <= 40; ++i)
         {
            dates.Add(today + new Period(i, TimeUnit.Years));
            // FLOATING_POINT_EXCEPTION
            rates.Add(0.03 + 0.0003*Math.Exp(Math.Sin(i/4.0)));
            divRates.Add(0.02 + 0.0001*Math.Exp(Math.Sin(i/5.0)));
            times.Add(dc.yearFraction(today, dates.Last()));
         }

         Date maturity = today + new Period(20, TimeUnit.Years);

         Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100));
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>( new InterpolatedZeroCurve<Linear>( dates, 
            rates, dc ) );
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>( new InterpolatedZeroCurve<Linear>( dates, 
            divRates, dc ) );
         SimpleQuote vol = new SimpleQuote(0.25);
         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

         BlackScholesMertonProcess bsmProcess = new BlackScholesMertonProcess(s0, qTS, rTS, volTS);
         HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, 0.0625, 0.5, 0.0625, 1e-5, 0.3);
         HullWhiteForwardProcess hwProcess = new HullWhiteForwardProcess(rTS, 0.01, 0.01);
         hwProcess.setForwardMeasureTime(dc.yearFraction(today, maturity));

         double tol = 0.05;
         double[] corr = {-0.9,-0.5,0.0,0.5,0.9};
         double[] strike = {100};

         for (int i = 0; i < corr.Length; ++i)
         {
            for (int j = 0; j < strike.Length; ++j)
            {
               HybridHestonHullWhiteProcess jointProcess = new HybridHestonHullWhiteProcess(hestonProcess,
                  hwProcess, corr[i]);

               StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, strike[j]);
               Exercise exercise = new EuropeanExercise(maturity);

               VanillaOption optionHestonHW = new VanillaOption(payoff, exercise);
               IPricingEngine engine = new MakeMCHestonHullWhiteEngine<PseudoRandom, Statistics>( jointProcess )
                                          .withSteps(1)
                                          .withAntitheticVariate()
                                          .withControlVariate()
                                          .withAbsoluteTolerance(tol)
                                          .withSeed( 42 ).getAsPricingEngine();

               optionHestonHW.setPricingEngine(engine);

               HullWhite hwModel = new HullWhite(new Handle<YieldTermStructure>(rTS),
                  hwProcess.a(), hwProcess.sigma());

               VanillaOption optionBsmHW = new VanillaOption(payoff, exercise);
               optionBsmHW.setPricingEngine( new AnalyticBSMHullWhiteEngine(corr[i], bsmProcess,hwModel));

               double calculated = optionHestonHW.NPV();
               double error = optionHestonHW.errorEstimate();
               double expected = optionBsmHW.NPV();

               if ((corr[i] != 0.0 && Math.Abs(calculated - expected) > 3*error)
                   || (corr[i] == 0.0 && Math.Abs(calculated - expected) > 1e-4))
               {
                  QAssert.Fail("Failed to reproduce BSM-HW vanilla prices"
                              + "\n   corr:       " + corr[i]
                              + "\n   strike:     " + strike[j]
                              + "\n   calculated: " + calculated
                              + "\n   error:      " + error
                              + "\n   expected:   " + expected);
               }
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testMcPureHestonPricing() 
      {
         // Testing Monte-Carlo Heston option pricing
         DayCounter dc = new Actual360();
         Date today = Date.Today;

         Settings.setEvaluationDate(today);

         // construct a strange yield curve to check drifts and discounting
         // of the joint stochastic process

         List<Date> dates = new List<Date>();
         List<double> times = new List<double>();
         List<double> rates = new List<double>(), divRates = new List<double>();

         for (int i = 0; i <= 100; ++i)
         {
            dates.Add(today + new Period(i, TimeUnit.Months));
            // FLOATING_POINT_EXCEPTION
            rates.Add(0.02 + 0.0002*Math.Exp(Math.Sin(i/10.0)));
            divRates.Add(0.02 + 0.0001*Math.Exp(Math.Sin(i/20.0)));
            times.Add(dc.yearFraction(today, dates.Last()));
         }

         Date maturity = today + new Period(2, TimeUnit.Years);

         Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100));
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, rates, dc));
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, divRates, dc));

         HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, 0.08, 1.5, 0.0625, 0.5, -0.8);
         HullWhiteForwardProcess hwProcess = new HullWhiteForwardProcess(rTS, 0.1, 1e-8);
         hwProcess.setForwardMeasureTime(dc.yearFraction(today, maturity + new Period(1, TimeUnit.Years)));

         double tol = 0.001;
         double[] corr = {-0.45,0.45,0.25};
         double[] strike = {100,75,50,150};

         for (int i = 0; i < corr.Length; ++i)
         {
            for (int j = 0; j < strike.Length; ++j)
            {
               HybridHestonHullWhiteProcess jointProcess = new HybridHestonHullWhiteProcess( hestonProcess, hwProcess,
                  corr[i], HybridHestonHullWhiteProcess.Discretization.Euler);

               StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, strike[j]);
               Exercise exercise = new EuropeanExercise(maturity);

               VanillaOption optionHestonHW = new VanillaOption(payoff, exercise);
               VanillaOption optionPureHeston = new VanillaOption(payoff, exercise);
               optionPureHeston.setPricingEngine(new AnalyticHestonEngine(new HestonModel(hestonProcess)));

               double expected = optionPureHeston.NPV();

               optionHestonHW.setPricingEngine(
                  new MakeMCHestonHullWhiteEngine<PseudoRandom,Statistics>(jointProcess)
                     .withSteps(2)
                     .withAntitheticVariate()
                     .withControlVariate()
                     .withAbsoluteTolerance(tol)
                     .withSeed(42).getAsPricingEngine());

               double calculated = optionHestonHW.NPV();
               double error = optionHestonHW.errorEstimate();

               if (Math.Abs(calculated - expected) > 3*error
                   && Math.Abs(calculated - expected) > tol)
               {
                  QAssert.Fail("Failed to reproduce pure heston vanilla prices"
                              + "\n   corr:       " + corr[i]
                              + "\n   strike:     " + strike[j]
                              + "\n   calculated: " + calculated
                              + "\n   error:      " + error
                              + "\n   expected:   " + expected);
               }
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testAnalyticHestonHullWhitePricing() 
      {
         // Testing analytic Heston Hull-White option pricing
         DayCounter dc = new Actual360();
         Date today = Date.Today;

         Settings.setEvaluationDate(today);

         // construct a strange yield curve to check drifts and discounting
         // of the joint stochastic process

         List<Date> dates = new List<Date>();
         List<double> times = new List<double>();
         List<double> rates = new List<double>(), divRates = new List<double>();

         for (int i = 0; i <= 40; ++i)
         {
            dates.Add(today + new Period(i, TimeUnit.Years));
            // FLOATING_POINT_EXCEPTION
            rates.Add(0.03 + 0.0001*Math.Exp(Math.Sin(i/4.0)));
            divRates.Add(0.02 + 0.0002*Math.Exp(Math.Sin(i/3.0)));
            times.Add(dc.yearFraction(today, dates.Last()));
         }

         Date maturity = today + new Period(5, TimeUnit.Years);
         Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100));
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, rates, dc));
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, divRates, dc));

         HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, 0.08, 1.5, 0.0625, 0.5, -0.8);
         HestonModel hestonModel = new HestonModel(hestonProcess);

         HullWhiteForwardProcess hwFwdProcess = new HullWhiteForwardProcess(rTS, 0.01, 0.01);
         hwFwdProcess.setForwardMeasureTime(dc.yearFraction(today, maturity));
         HullWhite hullWhiteModel = new HullWhite(rTS, hwFwdProcess.a(), hwFwdProcess.sigma());

         double tol = 0.002;
         double[] strike = {80,120};
         Option.Type[] types = {Option.Type.Put,Option.Type.Call};

         for (int i = 0; i < types.Length; ++i)
         {
            for (int j = 0; j < strike.Length; ++j)
            {
               HybridHestonHullWhiteProcess jointProcess = new HybridHestonHullWhiteProcess(hestonProcess, 
                  hwFwdProcess, 0.0,HybridHestonHullWhiteProcess.Discretization.Euler);

               StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strike[j]);
               Exercise exercise = new EuropeanExercise(maturity);

               VanillaOption optionHestonHW = new VanillaOption(payoff, exercise);
               optionHestonHW.setPricingEngine( new MakeMCHestonHullWhiteEngine<PseudoRandom,Statistics>(jointProcess)
                     .withSteps(1)
                     .withAntitheticVariate()
                     .withControlVariate()
                     .withAbsoluteTolerance(tol)
                     .withSeed(42).getAsPricingEngine());

               VanillaOption optionPureHeston = new VanillaOption(payoff, exercise);
               optionPureHeston.setPricingEngine(new AnalyticHestonHullWhiteEngine(hestonModel,hullWhiteModel, 128));

               double calculated = optionHestonHW.NPV();
               double error = optionHestonHW.errorEstimate();
               double expected = optionPureHeston.NPV();

               if (Math.Abs(calculated - expected) > 3*error
                   && Math.Abs(calculated - expected) > tol)
               {
                  QAssert.Fail("Failed to reproduce hw heston vanilla prices"
                              + "\n   strike:     " + strike[j]
                              + "\n   calculated: " + calculated
                              + "\n   error:      " + error
                              + "\n   expected:   " + expected);
               }
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testCallableEquityPricing() 
      {
         // Testing the pricing of a callable equity product

         /*
          For the definition of the example product see
          Alexander Giese, On the Pricing of Auto-Callable Equity
          Structures in the Presence of Stochastic Volatility and
          Stochastic Interest Rates .
          http://workshop.mathfinance.de/2006/papers/giese/slides.pdf
         */

         int maturity = 7;
         DayCounter dc = new Actual365Fixed();
         Date today = Date.Today;

         Settings.setEvaluationDate(today);

         Handle<Quote> spot = new Handle<Quote>(new SimpleQuote(100.0));
         SimpleQuote qRate = new SimpleQuote(0.04);
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, qRate, dc));
         SimpleQuote rRate = new SimpleQuote(0.04);
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, rRate, dc));

         HestonProcess hestonProcess = new HestonProcess(rTS, qTS, spot, 0.0625, 1.0, 0.24*0.24, 1e-4, 0.0);
         // FLOATING_POINT_EXCEPTION
         HullWhiteForwardProcess hwProcess = new HullWhiteForwardProcess(rTS, 0.00883, 0.00526);
         hwProcess.setForwardMeasureTime(dc.yearFraction(today, today + new Period(maturity + 1, TimeUnit.Years)));

         HybridHestonHullWhiteProcess jointProcess = new HybridHestonHullWhiteProcess(hestonProcess, hwProcess, -0.4);

         Schedule schedule = new Schedule(today, today + new Period(maturity, TimeUnit.Years),new Period(1, TimeUnit.Years),
            new TARGET(),BusinessDayConvention.Following, BusinessDayConvention.Following, DateGeneration.Rule.Forward,false);

         List<double> times = new InitializedList<double>(maturity + 1);

         for (int i = 0; i <= maturity; ++i)
            times[i] = i;

         TimeGrid grid  = new TimeGrid(times,times.Count);

         List<double> redemption = new InitializedList<double>(maturity);
         for (int i = 0; i < maturity; ++i)
         {
            redemption[i] = 1.07 + 0.03*i;
         }

         ulong seed = 42;
         IRNG rsg = (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>
                                                                    ,InverseCumulativeNormal>)
            new PseudoRandom().make_sequence_generator(jointProcess.factors()*(grid.size() - 1), seed);

         MultiPathGenerator<IRNG> generator = new MultiPathGenerator<IRNG>(jointProcess, grid, rsg, false);
         GeneralStatistics stat = new GeneralStatistics();

         double antitheticPayoff = 0;
         int nrTrails = 40000;
         for (int i = 0; i < nrTrails; ++i)
         {
            bool antithetic = (i%2) != 0;

            Sample<IPath> path = antithetic ? generator.antithetic() : generator.next();
            MultiPath value = path.value as MultiPath;
            Utils.QL_REQUIRE( value != null, () => "Invalid Path" );

            double payoff = 0;
            for (int j = 1; j <= maturity; ++j)
            {
               if (value[0][j] > spot.link.value())
               {
                  Vector states = new Vector(3);
                  for (int k = 0; k < 3; ++k)
                  {
                     states[k] = value[k][j];
                  }
                  payoff = redemption[j - 1] / jointProcess.numeraire(grid[j], states);
                  break;
               }
               else if (j == maturity)
               {
                  Vector states = new Vector(3);
                  for (int k = 0; k < 3; ++k)
                  {
                     states[k] = value[k][j];
                  }
                  payoff = 1.0/jointProcess.numeraire(grid[j], states);
               }
            }

            if (antithetic)
            {
               stat.add(0.5*(antitheticPayoff + payoff));
            }
            else
            {
               antitheticPayoff = payoff;
            }
         }

         double expected = 0.938;
         double calculated = stat.mean();
         double error = stat.errorEstimate();

         if (Math.Abs(expected - calculated) > 3*error)
         {
            QAssert.Fail("Failed to reproduce auto-callable equity structure price"
                        + "\n   calculated: " + calculated
                        + "\n   error:      " + error
                        + "\n   expected:   " + expected);
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testDiscretizationError() 
      {
         // Testing the discretization error of the Heston Hull-White process
         DayCounter dc = new Actual360();
         Date today = Date.Today;

         Settings.setEvaluationDate(today);

         // construct a strange yield curve to check drifts and discounting
         // of the joint stochastic process

         List<Date> dates = new List<Date>();
         List<double> times = new List<double>();
         List<double> rates = new List<double>(), divRates = new List<double>();

         for (int i = 0; i <= 31; ++i)
         {
            dates.Add(today + new Period(i, TimeUnit.Years));
            // FLOATING_POINT_EXCEPTION
            rates.Add(0.04 + 0.0001*Math.Exp(Math.Sin(i)));
            divRates.Add(0.04 + 0.0001*Math.Exp(Math.Sin(i)));
            times.Add(dc.yearFraction(today, dates.Last()));
         }

         Date maturity = today + new Period(10, TimeUnit.Years);
         double v = 0.25;

         Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100));
         SimpleQuote vol = new SimpleQuote(v);
         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, rates, dc));
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, divRates, dc));

         BlackScholesMertonProcess bsmProcess = new BlackScholesMertonProcess(s0, qTS, rTS, volTS);

         HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, v*v, 1, v*v, 1e-6, -0.4);

         HullWhiteForwardProcess hwProcess = new HullWhiteForwardProcess(rTS, 0.01, 0.01);
         hwProcess.setForwardMeasureTime(20.1472222222222222);

         double tol = 0.05;
         double[] corr = {-0.85,0.5};
         double[] strike = {50,100,125};

         for (int i = 0; i < corr.Length; ++i)
         {
            for (int j = 0; j < strike.Length; ++j)
            {
               StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, strike[j]);
               Exercise exercise = new EuropeanExercise(maturity);

               VanillaOption optionBsmHW = new VanillaOption(payoff, exercise);
               HullWhite hwModel = new HullWhite(rTS, hwProcess.a(), hwProcess.sigma());
               optionBsmHW.setPricingEngine( new AnalyticBSMHullWhiteEngine(corr[i], bsmProcess,hwModel));

               double expected = optionBsmHW.NPV();

               VanillaOption optionHestonHW = new VanillaOption(payoff, exercise);
               HybridHestonHullWhiteProcess jointProcess = new HybridHestonHullWhiteProcess(hestonProcess,
                     hwProcess, corr[i]);
               optionHestonHW.setPricingEngine( 
                  new MakeMCHestonHullWhiteEngine<PseudoRandom,Statistics>(jointProcess)
                     .withSteps(1)
                     .withAntitheticVariate()
                     .withAbsoluteTolerance(tol)
                     .withSeed(42).getAsPricingEngine());

               double calculated = optionHestonHW.NPV();
               double error = optionHestonHW.errorEstimate();

               if ((Math.Abs(calculated - expected) > 3*error
                    && Math.Abs(calculated - expected) > 1e-5))
               {
                  QAssert.Fail("Failed to reproduce discretization error"
                              + "\n   corr:       " + corr[i]
                              + "\n   strike:     " + strike[j]
                              + "\n   calculated: " + calculated
                              + "\n   error:      " + error
                              + "\n   expected:   " + expected);
               }
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testH1HWPricingEngine() 
      {
         /*
          * Example taken from Lech Aleksander Grzelak,
          * Equity and Foreign Exchange Hybrid Models for Pricing Long-Maturity
          * Financial Derivatives,
          * http://repository.tudelft.nl/assets/uuid:a8e1a007-bd89-481a-aee3-0e22f15ade6b/PhDThesis_main.pdf
         */
         Date today = new Date(15, Month.July, 2012);
         Settings.setEvaluationDate(today);
         Date exerciseDate = new Date(13, Month.July, 2022);
         DayCounter dc = new Actual365Fixed();

         Exercise exercise = new EuropeanExercise(exerciseDate);

         Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100.0));

         double r = 0.02;
         double q = 0.00;
         double v0 = 0.05;
         double theta = 0.05;
         double kappa_v = 0.3;
         double[] sigma_v = {0.3,0.6};
         double rho_sv = -0.30;
         double rho_sr = 0.6;
         double kappa_r = 0.01;
         double sigma_r = 0.01;

         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, r, dc));
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, q, dc));

         Handle<BlackVolTermStructure> flatVolTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, 0.20, dc));
         GeneralizedBlackScholesProcess bsProcess = new GeneralizedBlackScholesProcess(s0, qTS, rTS, flatVolTS);

         HullWhiteProcess hwProcess = new HullWhiteProcess(rTS, kappa_r, sigma_r);
         HullWhite hullWhiteModel = new HullWhite(new Handle<YieldTermStructure>(rTS), kappa_r, sigma_r);

         double tol = 0.0001;
         double[] strikes = {40,80,100,120,180};
         double[][] expected =
         {
            new double[] {0.267503,0.235742,0.228223,0.223461,0.217855},
            new double[]  {0.263626,0.211625,0.199907,0.193502,0.190025}
         };

         for (int j = 0; j < sigma_v.Length; ++j)
         {
            HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, v0, kappa_v, theta,sigma_v[j], rho_sv);
            HestonModel hestonModel = new HestonModel(hestonProcess);

            for (int i = 0; i < strikes.Length; ++i)
            {
               StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, strikes[i]);

               VanillaOption option = new VanillaOption(payoff, exercise);

               IPricingEngine analyticH1HWEngine = new AnalyticH1HWEngine(hestonModel, hullWhiteModel,rho_sr, 144);
               option.setPricingEngine(analyticH1HWEngine);
               double impliedH1HW = option.impliedVolatility(option.NPV(), bsProcess);

               if (Math.Abs(expected[j][i] - impliedH1HW) > tol)
               {
                  QAssert.Fail("Failed to reproduce H1HW implied volatility"
                              + "\n   expected       : " + expected[j][i]
                              + "\n   calculated     : " + impliedH1HW
                              + "\n   tol            : " + tol
                              + "\n   strike         : " + strikes[i]
                              + "\n   sigma          : " + sigma_v[j]);
               }
            }
         }
      }
   }
}
