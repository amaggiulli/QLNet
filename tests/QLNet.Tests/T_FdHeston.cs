/*
 Copyright (C) 2020 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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
#if NET40 || NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if NET40 || NET452
   [TestClass()]
#endif
   public class T_FdHeston
   {
      public struct NewBarrierOptionData
      {
         public Barrier.Type barrierType;
         public double barrier;
         public double rebate;
         public Option.Type type;
         public double strike;
         public double s;        // spot
         public double q;        // dividend
         public double r;        // risk-free rate
         public double t;        // time to maturity
         public double v;        // volatility

         public NewBarrierOptionData(Barrier.Type _barrierType,
                                     double _barrier,
                                     double _rebate,
                                     Option.Type _type,
                                     double _strike,
                                     double _s,
                                     double _q,
                                     double _r,
                                     double _t,
                                     double _v)
         {
            barrierType = _barrierType;
            barrier = _barrier;
            rebate = _rebate;
            type = _type;
            strike = _strike;
            s = _s;
            q = _q;
            r = _r;
            t = _t;
            v = _v;
         }
      }

      public struct HestonTestData
      {
         public double kappa;
         public double theta;
         public double sigma;
         public double rho;
         public double r;
         public double q;
         public double T;
         public double K;

         public HestonTestData(double _kappa,
                               double _theta,
                               double _sigma,
                               double _rho,
                               double _r,
                               double _q,
                               double _T,
                               double _K)
         {
            kappa = _kappa;
            theta = _theta;
            sigma = _sigma;
            rho = _rho;
            r = _r;
            q = _q;
            T = _T;
            K = _K;
         }
      }

      public class ParableLocalVolatility : LocalVolTermStructure
      {
         public ParableLocalVolatility(Date referenceDate,
                                       double s0,
                                       double alpha,
                                       DayCounter dayCounter)
            : base(referenceDate, null, BusinessDayConvention.Following, dayCounter)
         {
            s0_ = s0;
            alpha_ = alpha;
         }

         public override Date maxDate() { return Date.maxDate(); }
         public override double minStrike() { return 0.0; }
         public override double maxStrike() { return Double.MaxValue; }

         protected override double localVolImpl(double t, double s)
         {
            return alpha_ * (Math.Pow((s0_ - s), 2.0) + 25.0);
         }

         protected double s0_, alpha_;
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmHestonVarianceMesher()
      {
         //Testing FDM Heston variance mesher...
         using (SavedSettings backup = new SavedSettings())
         {

            Date today = new Date(22, 2, 2018);
            DayCounter dc = new Actual365Fixed();
            Settings.setEvaluationDate(today);

            HestonProcess process = new HestonProcess(new Handle<YieldTermStructure>(Utilities.flatRate(0.02, dc)),
                                                      new Handle<YieldTermStructure>(Utilities.flatRate(0.02, dc)),
                                                      new Handle<Quote>(new SimpleQuote(100.0)),
                                                      0.09, 1.0, 0.09, 0.2, -0.5);

            FdmHestonVarianceMesher mesher = new FdmHestonVarianceMesher(5, process, 1.0);
            List<double> locations = mesher.locations();

            double[] expected = new double[] { 0.0, 6.652314e-02, 9.000000e-02, 1.095781e-01, 2.563610e-01 };

            double tol = 1e-6;
            double diff;
            for (int i = 0; i < locations.Count; ++i)
            {
               diff = Math.Abs(expected[i] - locations[i]);

               if (diff > tol)
               {
                  QAssert.Fail("Failed to reproduce Heston variance mesh"
                               + "\n    calculated: " + locations[i]
                               + "\n    expected:   " + expected[i]
                               + "\n    difference  " + diff
                               + "\n    tolerance:  " + tol);
               }
            }

            LocalVolTermStructure lVol = new LocalConstantVol(today, 2.5, dc);
            FdmHestonLocalVolatilityVarianceMesher constSlvMesher = new FdmHestonLocalVolatilityVarianceMesher(5, process, lVol, 1.0);

            double expectedVol = 2.5 * mesher.volaEstimate();
            double calculatedVol = constSlvMesher.volaEstimate();

            diff = Math.Abs(calculatedVol - expectedVol);
            if (diff > tol)
            {
               QAssert.Fail("Failed to reproduce Heston local volatility variance estimate"
                            + "\n    calculated: " + calculatedVol
                            + "\n    expected:   " + expectedVol
                            + "\n    difference  " + diff
                            + "\n    tolerance:  " + tol);
            }

            double alpha = 0.01;
            LocalVolTermStructure leverageFct = new ParableLocalVolatility(today, 100.0, alpha, dc);

            FdmHestonLocalVolatilityVarianceMesher slvMesher
               = new FdmHestonLocalVolatilityVarianceMesher(5, process, leverageFct, 0.5, 1, 0.01);

            double initialVolEstimate = new FdmHestonVarianceMesher(5, process, 0.5, 1, 0.01).volaEstimate();

            // double vEst = leverageFct.currentLink().localVol(0, 100) * initialVolEstimate;
            // Mathematica solution
            //    N[Integrate[
            //      alpha*((100*Exp[vEst*x*Sqrt[0.5]] - 100)^2 + 25)*
            //       PDF[NormalDistribution[0, 1], x], {x ,
            //       InverseCDF[NormalDistribution[0, 1], 0.01],
            //       InverseCDF[NormalDistribution[0, 1], 0.99]}]]

            double leverageAvg = 0.455881 / (1 - 0.02);

            double volaEstExpected =
               0.5 * (leverageAvg + leverageFct.localVol(0, 100)) * initialVolEstimate;

            double volaEstCalculated = slvMesher.volaEstimate();

            if (Math.Abs(volaEstExpected - volaEstCalculated) > 0.001)
            {
               QAssert.Fail("Failed to reproduce Heston local volatility variance estimate"
                            + "\n    calculated: " + calculatedVol
                            + "\n    expected:   " + expectedVol
                            + "\n    difference  " + Math.Abs(volaEstExpected - volaEstCalculated)
                            + "\n    tolerance:  " + tol);
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmHestonBarrierVsBlackScholes()
      {
         //Testing FDM with barrier option in Heston model...
         using (SavedSettings backup = new SavedSettings())
         {
            NewBarrierOptionData[] values = new NewBarrierOptionData[]
            {
               /* The data below are from
                 "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 pag. 72
               */
               //                          barrierType,        barrier, rebate,         type,  strike,     s,    q,    r,    t,    v
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call,     90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call,    100, 100.0, 0.00, 0.08, 1.00, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call,    110, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call,     90, 100.0, 0.00, 0.08, 0.25, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call,    100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call,    110, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call,     90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call,    100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call,    110, 100.0, 0.04, 0.08, 0.50, 0.25),

               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call,    90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call,   100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call,   110, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call,    90, 100.0, 0.00, 0.08, 0.25, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call,   100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call,   110, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call,    90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call,   100, 100.0, 0.00, 0.08, 0.40, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call,   110, 100.0, 0.04, 0.08, 0.50, 0.15),

               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call,   100, 100.0, 0.00, 0.08, 0.40, 0.35),
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call,   110, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call,    90, 100.0, 0.04, 0.08, 0.50, 0.15),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call,   110, 100.0, 0.00, 0.00, 1.00, 0.20),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call,   110, 100.0, 0.04, 0.08, 0.50, 0.30),

               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call,   110, 100.0, 0.00, 0.08, 1.00, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call,   110, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call,   110, 100.0, 0.04, 0.08, 0.50, 0.30),

               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.25),

               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.25),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put,   110, 100.0, 0.00, 0.04, 1.00, 0.15),

               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.30),

               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 1.00, 0.15),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put,    90, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put,   100, 100.0, 0.04, 0.08, 0.50, 0.30),
               new NewBarrierOptionData(Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put,   110, 100.0, 0.04, 0.08, 0.50, 0.30)
            };

            DayCounter dc = new Actual365Fixed();
            Date todaysDate = new Date(28, 3, 2004);
            Date exerciseDate = new Date(28, 3, 2005);
            Settings.setEvaluationDate(todaysDate);

            Handle<Quote> spot = new Handle<Quote>(new SimpleQuote(0.0));
            SimpleQuote qRate = new SimpleQuote(0.0);
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
            SimpleQuote rRate = new SimpleQuote(0.0);
            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
            SimpleQuote vol = new SimpleQuote(0.0);
            Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

            BlackScholesMertonProcess bsProcess = new BlackScholesMertonProcess(spot, qTS, rTS, volTS);

            IPricingEngine analyticEngine = new AnalyticBarrierEngine(bsProcess);

            for (int i = 0; i < values.Length; i++)
            {
               Date exDate = todaysDate + Convert.ToInt32(values[i].t * 365 + 0.5);
               Exercise exercise = new EuropeanExercise(exDate);

               (spot.currentLink() as SimpleQuote).setValue(values[i].s);
               qRate.setValue(values[i].q);
               rRate.setValue(values[i].r);
               vol.setValue(values[i].v);

               StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, values[i].strike);

               BarrierOption barrierOption = new BarrierOption(values[i].barrierType, values[i].barrier,
                                                               values[i].rebate, payoff, exercise);

               double v0 = vol.value() * vol.value();
               HestonProcess hestonProcess =
                  new HestonProcess(rTS, qTS, spot, v0, 1.0, v0, 0.005, 0.0);

               barrierOption.setPricingEngine(new FdHestonBarrierEngine(new HestonModel(hestonProcess), 200, 101, 3));

               double calculatedHE = barrierOption.NPV();

               barrierOption.setPricingEngine(analyticEngine);
               double expected = barrierOption.NPV();

               double tol = 0.0025;
               if (Math.Abs(calculatedHE - expected) / expected > tol)
               {
                  QAssert.Fail("Failed to reproduce expected Heston npv"
                               + "\n    calculated: " + calculatedHE
                               + "\n    expected:   " + expected
                               + "\n    tolerance:  " + tol);
               }
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmHestonBarrier()
      {
         //Testing FDM with barrier option for Heston model vs Black-Scholes model...
         using (SavedSettings backup = new SavedSettings())
         {

            Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100.0));

            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.05, new Actual365Fixed()));
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, new Actual365Fixed()));

            HestonProcess hestonProcess =
               new HestonProcess(rTS, qTS, s0, 0.04, 2.5, 0.04, 0.66, -0.8);

            Settings.setEvaluationDate(new Date(28, 3, 2004));
            Date exerciseDate = new Date(28, 3, 2005);

            Exercise exercise = new EuropeanExercise(exerciseDate);
            StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, 100);

            BarrierOption barrierOption = new BarrierOption(Barrier.Type.UpOut, 135, 0.0, payoff, exercise);
            barrierOption.setPricingEngine(new FdHestonBarrierEngine(new HestonModel(hestonProcess), 50, 400, 100));

            double tol = 0.01;
            double npvExpected = 9.1530;
            double deltaExpected = 0.5218;
            double gammaExpected = -0.0354;

            if (Math.Abs(barrierOption.NPV() - npvExpected) > tol)
            {
               QAssert.Fail("Failed to reproduce expected npv"
                            + "\n    calculated: " + barrierOption.NPV()
                            + "\n    expected:   " + npvExpected
                            + "\n    tolerance:  " + tol);
            }
            if (Math.Abs(barrierOption.delta() - deltaExpected) > tol)
            {
               QAssert.Fail("Failed to reproduce expected delta"
                            + "\n    calculated: " + barrierOption.delta()
                            + "\n    expected:   " + deltaExpected
                            + "\n    tolerance:  " + tol);
            }
            if (Math.Abs(barrierOption.gamma() - gammaExpected) > tol)
            {
               QAssert.Fail("Failed to reproduce expected gamma"
                            + "\n    calculated: " + barrierOption.gamma()
                            + "\n    expected:   " + gammaExpected
                            + "\n    tolerance:  " + tol);
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmHestonAmerican()
      {

         //Testing FDM with American option in Heston model...

         using (SavedSettings backup = new SavedSettings())
         {

            Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100.0));

            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.05, new Actual365Fixed()));
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, new Actual365Fixed()));

            HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, 0.04, 2.5, 0.04, 0.66, -0.8);

            Settings.setEvaluationDate(new Date(28, 3, 2004));
            Date exerciseDate = new Date(28, 3, 2005);

            Exercise exercise = new AmericanExercise(exerciseDate);
            StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, 100);

            VanillaOption option = new VanillaOption(payoff, exercise);
            IPricingEngine engine = new FdHestonVanillaEngine(new HestonModel(hestonProcess), 200, 100, 50);
            option.setPricingEngine(engine);

            double tol = 0.01;
            double npvExpected = 5.66032;
            double deltaExpected = -0.30065;
            double gammaExpected = 0.02202;

            if (Math.Abs(option.NPV() - npvExpected) > tol)
            {
               QAssert.Fail("Failed to reproduce expected npv"
                            + "\n    calculated: " + option.NPV()
                            + "\n    expected:   " + npvExpected
                            + "\n    tolerance:  " + tol);
            }
            if (Math.Abs(option.delta() - deltaExpected) > tol)
            {
               QAssert.Fail("Failed to reproduce expected delta"
                            + "\n    calculated: " + option.delta()
                            + "\n    expected:   " + deltaExpected
                            + "\n    tolerance:  " + tol);
            }
            if (Math.Abs(option.gamma() - gammaExpected) > tol)
            {
               QAssert.Fail("Failed to reproduce expected gamma"
                            + "\n    calculated: " + option.gamma()
                            + "\n    expected:   " + gammaExpected
                            + "\n    tolerance:  " + tol);
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmHestonIkonenToivanen()
      {

         //Testing FDM Heston for Ikonen and Toivanen tests...

         /* check prices of american puts as given in:
            From Efficient numerical methods for pricing American options under
            stochastic volatility, Samuli Ikonen, Jari Toivanen,
            http://users.jyu.fi/~tene/papers/reportB12-05.pdf
         */
         using (SavedSettings backup = new SavedSettings())
         {
            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.10, new Actual360()));
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, new Actual360()));

            Settings.setEvaluationDate(new Date(28, 3, 2004));
            Date exerciseDate = new Date(26, 6, 2004);
            Exercise exercise = new AmericanExercise(exerciseDate);
            StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, 10);
            VanillaOption option = new VanillaOption(payoff, exercise);

            double[] strikes = new double[] { 8, 9, 10, 11, 12 };
            double[] expected = new double[] { 2.00000, 1.10763, 0.520038, 0.213681, 0.082046 };
            double tol = 0.001;

            for (int i = 0; i < strikes.Length; ++i)
            {
               Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(strikes[i]));
               HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, 0.0625, 5, 0.16, 0.9, 0.1);
               IPricingEngine engine = new FdHestonVanillaEngine(new HestonModel(hestonProcess), 100, 400);
               option.setPricingEngine(engine);

               double calculated = option.NPV();
               if (Math.Abs(calculated - expected[i]) > tol)
               {
                  QAssert.Fail("Failed to reproduce expected npv"
                               + "\n    strike:     " + strikes[i]
                               + "\n    calculated: " + calculated
                               + "\n    expected:   " + expected[i]
                               + "\n    tolerance:  " + tol);
               }
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmHestonBlackScholes()
      {
         //Testing FDM Heston with Black Scholes model...
         using (SavedSettings backup = new SavedSettings())
         {
            Settings.setEvaluationDate(new Date(28, 3, 2004));
            Date exerciseDate = new Date(26, 6, 2004);

            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.10, new Actual360()));
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, new Actual360()));
            Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(rTS.currentLink().referenceDate(), 0.25, rTS.currentLink().dayCounter()));

            Exercise exercise = new EuropeanExercise(exerciseDate);
            StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, 10);
            VanillaOption option = new VanillaOption(payoff, exercise);

            double[] strikes = new double[] { 8, 9, 10, 11, 12 };
            double tol = 0.0001;

            for (int i = 0; i < strikes.Length; ++i)
            {
               Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(strikes[i]));
               GeneralizedBlackScholesProcess bsProcess = new GeneralizedBlackScholesProcess(s0, qTS, rTS, volTS);
               option.setPricingEngine(new AnalyticEuropeanEngine(bsProcess));

               double expected = option.NPV();

               HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, 0.0625, 1, 0.0625, 0.0001, 0.0);

               // Hundsdorfer scheme
               option.setPricingEngine(new FdHestonVanillaEngine(new HestonModel(hestonProcess),
                                                                 100, 400, 3));

               double calculated = option.NPV();
               if (Math.Abs(calculated - expected) > tol)
               {
                  QAssert.Fail("Failed to reproduce expected npv"
                               + "\n    strike:     " + strikes[i]
                               + "\n    calculated: " + calculated
                               + "\n    expected:   " + expected
                               + "\n    tolerance:  " + tol);
               }

               // Explicit scheme
               option.setPricingEngine(new FdHestonVanillaEngine(new HestonModel(hestonProcess),
                                                                 4000, 400, 3, 0,
                                                                 new FdmSchemeDesc().ExplicitEuler()));

               calculated = option.NPV();
               if (Math.Abs(calculated - expected) > tol)
               {
                  QAssert.Fail("Failed to reproduce expected npv"
                               + "\n    strike:     " + strikes[i]
                               + "\n    calculated: " + calculated
                               + "\n    expected:   " + expected
                               + "\n    tolerance:  " + tol);
               }
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmHestonEuropeanWithDividends()
      {
         //Testing FDM with European option with dividends in Heston model...
         using (SavedSettings backup = new SavedSettings())
         {
            Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100.0));

            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.05, new Actual365Fixed()));
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, new Actual365Fixed()));

            HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, 0.04, 2.5, 0.04, 0.66, -0.8);

            Settings.setEvaluationDate(new Date(28, 3, 2004));
            Date exerciseDate = new Date(28, 3, 2005);
            Exercise exercise = new AmericanExercise(exerciseDate);
            StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, 100);

            List<double> dividends = new InitializedList<double>(1, 5);
            List<Date> dividendDates = new InitializedList<Date>(1, new Date(28, 9, 2004));

            DividendVanillaOption option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
            IPricingEngine engine = new FdHestonVanillaEngine(new HestonModel(hestonProcess), 50, 100, 50);
            option.setPricingEngine(engine);

            double tol = 0.01;
            double gammaTol = 0.001;
            double npvExpected = 7.365075;
            double deltaExpected = -0.396678;
            double gammaExpected = 0.027681;

            if (Math.Abs(option.NPV() - npvExpected) > tol)
            {
               QAssert.Fail("Failed to reproduce expected npv"
                            + "\n    calculated: " + option.NPV()
                            + "\n    expected:   " + npvExpected
                            + "\n    tolerance:  " + tol);
            }
            if (Math.Abs(option.delta() - deltaExpected) > tol)
            {
               QAssert.Fail("Failed to reproduce expected delta"
                            + "\n    calculated: " + option.delta()
                            + "\n    expected:   " + deltaExpected
                            + "\n    tolerance:  " + tol);
            }
            if (Math.Abs(option.gamma() - gammaExpected) > gammaTol)
            {
               QAssert.Fail("Failed to reproduce expected gamma"
                            + "\n    calculated: " + option.gamma()
                            + "\n    expected:   " + gammaExpected
                            + "\n    tolerance:  " + tol);
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmHestonConvergence()
      {

         /* convergence tests based on
            ADI finite difference schemes for option pricing in the
            Heston model with correlation, K.J. in t'Hout and S. Foulon
         */

         //Testing FDM Heston convergence...

         using (SavedSettings backup = new SavedSettings())
         {

            HestonTestData[] values = new HestonTestData[]
            {
               new HestonTestData(1.5, 0.04, 0.3, -0.9, 0.025, 0.0, 1.0, 100),
               new HestonTestData(3.0, 0.12, 0.04, 0.6, 0.01, 0.04, 1.0, 100),
               new HestonTestData(0.6067, 0.0707, 0.2928, -0.7571, 0.03, 0.0, 3.0, 100),
               new HestonTestData(2.5, 0.06, 0.5, -0.1, 0.0507, 0.0469, 0.25, 100)
            };

            FdmSchemeDesc[] schemes = new FdmSchemeDesc[]
            {
               new FdmSchemeDesc().Hundsdorfer(),
               new FdmSchemeDesc().ModifiedCraigSneyd(),
               new FdmSchemeDesc().ModifiedHundsdorfer(),
               new FdmSchemeDesc().CraigSneyd(),
               new FdmSchemeDesc().TrBDF2(),
               new FdmSchemeDesc().CrankNicolson(),
            };

            int[] tn = new int[] { 60 };
            double[] v0 = new double[] { 0.04 };

            Date todaysDate = new Date(28, 3, 2004);
            Settings.setEvaluationDate(todaysDate);

            Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(75.0));

            for (int l = 0; l < schemes.Length; ++l)
            {
               for (int i = 0; i < values.Length; ++i)
               {
                  for (int j = 0; j < tn.Length; ++j)
                  {
                     for (int k = 0; k < v0.Length; ++k)
                     {
                        Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(
                           Utilities.flatRate(values[i].r, new Actual365Fixed()));
                        Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(
                           Utilities.flatRate(values[i].q, new Actual365Fixed()));

                        HestonProcess hestonProcess =
                           new HestonProcess(rTS, qTS, s0,
                                             v0[k],
                                             values[i].kappa,
                                             values[i].theta,
                                             values[i].sigma,
                                             values[i].rho);

                        Date exerciseDate = todaysDate
                                            + new Period(Convert.ToInt32(values[i].T * 365), TimeUnit.Days);

                        Exercise exercise = new EuropeanExercise(exerciseDate);
                        StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, values[i].K);

                        VanillaOption option = new VanillaOption(payoff, exercise);
                        IPricingEngine engine =
                           new FdHestonVanillaEngine(
                           new HestonModel(hestonProcess),
                           tn[j], 101, 51, 0,
                           schemes[l]);

                        option.setPricingEngine(engine);

                        double calculated = option.NPV();

                        IPricingEngine analyticEngine =
                           new AnalyticHestonEngine(
                           new HestonModel(hestonProcess), 144);

                        option.setPricingEngine(analyticEngine);
                        double expected = option.NPV();
                        if (Math.Abs(expected - calculated) / expected > 0.02
                            && Math.Abs(expected - calculated) > 0.002)
                        {
                           QAssert.Fail("Failed to reproduce expected npv"
                                        + "\n    calculated: " + calculated
                                        + "\n    expected:   " + expected
                                        + "\n    tolerance:  " + 0.01);
                        }
                     }
                  }
               }
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testMethodOfLinesAndCN()
      {
         //Testing method of lines to solve Heston PDEs...

         using (SavedSettings backup = new SavedSettings())
         {
            DayCounter dc = new Actual365Fixed();
            Date today = new Date(21, 2, 2018);

            Settings.setEvaluationDate(today);

            Handle<Quote> spot = new Handle<Quote>(new SimpleQuote(100.0));
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.0, dc));
            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.0, dc));

            double v0 = 0.09;
            double kappa = 1.0;
            double theta = v0;
            double sigma = 0.4;
            double rho = -0.75;

            Date maturity = today + new Period(3, TimeUnit.Months);

            HestonModel model =
               new HestonModel(
               new HestonProcess(rTS, qTS, spot, v0, kappa, theta, sigma, rho));

            int xGrid = 21;
            int vGrid = 7;

            IPricingEngine fdmDefault =
               new FdHestonVanillaEngine(model, 10, xGrid, vGrid, 0);

            IPricingEngine fdmMol =
               new FdHestonVanillaEngine(
               model, 10, xGrid, vGrid, 0, new FdmSchemeDesc().MethodOfLines());

            PlainVanillaPayoff payoff =
               new PlainVanillaPayoff(Option.Type.Put, spot.currentLink().value());

            VanillaOption option = new VanillaOption(payoff, new AmericanExercise(maturity));

            option.setPricingEngine(fdmMol);
            double calculatedMoL = option.NPV();

            option.setPricingEngine(fdmDefault);
            double expected = option.NPV();

            double tol = 0.005;
            double diffMoL = Math.Abs(expected - calculatedMoL);

            if (diffMoL > tol)
            {
               QAssert.Fail("Failed to reproduce european option values with MOL"
                            + "\n    calculated: " + calculatedMoL
                            + "\n    expected:   " + expected
                            + "\n    difference: " + diffMoL
                            + "\n    tolerance:  " + tol);
            }

            IPricingEngine fdmCN =
               new FdHestonVanillaEngine(model, 10, xGrid, vGrid, 0, new FdmSchemeDesc().CrankNicolson());
            option.setPricingEngine(fdmCN);

            double calculatedCN = option.NPV();
            double diffCN = Math.Abs(expected - calculatedCN);

            if (diffCN > tol)
            {
               QAssert.Fail("Failed to reproduce european option values with Crank-Nicolson"
                            + "\n    calculated: " + calculatedCN
                            + "\n    expected:   " + expected
                            + "\n    difference: " + diffCN
                            + "\n    tolerance:  " + tol);
            }

            BarrierOption barrierOption =
               new BarrierOption(Barrier.Type.DownOut, 85.0, 10.0,
                                 payoff, new EuropeanExercise(maturity));

            barrierOption.setPricingEngine(new FdHestonBarrierEngine(model, 100, 31, 11));

            double expectedBarrier = barrierOption.NPV();

            barrierOption.setPricingEngine(new FdHestonBarrierEngine(model, 100, 31, 11, 0, new FdmSchemeDesc().MethodOfLines()));

            double calculatedBarrierMoL = barrierOption.NPV();

            double barrierTol = 0.01;
            double barrierDiffMoL = Math.Abs(expectedBarrier - calculatedBarrierMoL);

            if (barrierDiffMoL > barrierTol)
            {
               QAssert.Fail("Failed to reproduce barrier option values with MOL"
                            + "\n    calculated: " + calculatedBarrierMoL
                            + "\n    expected:   " + expectedBarrier
                            + "\n    difference: " + barrierDiffMoL
                            + "\n    tolerance:  " + barrierTol);
            }

            barrierOption.setPricingEngine(new FdHestonBarrierEngine(model, 100, 31, 11, 0, new FdmSchemeDesc().CrankNicolson()));

            double calculatedBarrierCN = barrierOption.NPV();
            double barrierDiffCN = Math.Abs(expectedBarrier - calculatedBarrierCN);

            if (barrierDiffCN > barrierTol)
            {
               QAssert.Fail("Failed to reproduce barrier option values with Crank-Nicolson"
                            + "\n    calculated: " + calculatedBarrierCN
                            + "\n    expected:   " + expectedBarrier
                            + "\n    difference: " + barrierDiffCN
                            + "\n    tolerance:  " + barrierTol);
            }
         }
      }

#if NET40 || NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSpuriousOscillations()
      {
         //Testing for spurious oscillations when solving the Heston PDEs...
         using (SavedSettings backup = new SavedSettings())
         {
            DayCounter dc = new Actual365Fixed();
            Date today = new Date(7, 6, 2018);

            Settings.setEvaluationDate(today);

            Handle<Quote> spot = new Handle<Quote>(new SimpleQuote(100.0));
            Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.1, dc));
            Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.0, dc));

            double v0 = 0.005;
            double kappa = 1.0;
            double theta = 0.005;
            double sigma = 0.4;
            double rho = -0.75;

            Date maturity = today + new Period(1, TimeUnit.Years);

            HestonProcess process =
               new HestonProcess(
               rTS, qTS, spot, v0, kappa, theta, sigma, rho);

            HestonModel model =
               new HestonModel(process);

            FdHestonVanillaEngine hestonEngine =
               new FdHestonVanillaEngine(
               model, 6, 200, 13, 0, new FdmSchemeDesc().TrBDF2());

            VanillaOption option = new VanillaOption(new PlainVanillaPayoff(Option.Type.Call, spot.currentLink().value()),
                                                     new EuropeanExercise(maturity));

            option.setupArguments(hestonEngine.getArguments());

            List<Tuple<FdmSchemeDesc, string, bool>> descs = new List<Tuple<FdmSchemeDesc, string, bool>>();
            descs.Add(new Tuple<FdmSchemeDesc, string, bool>(new FdmSchemeDesc().CraigSneyd(), "Craig-Sneyd", true));
            descs.Add(new Tuple<FdmSchemeDesc, string, bool>(new FdmSchemeDesc().Hundsdorfer(), "Hundsdorfer", true));
            descs.Add(new Tuple<FdmSchemeDesc, string, bool>(new FdmSchemeDesc().ModifiedHundsdorfer(), "Mod. Hundsdorfer", true));
            descs.Add(new Tuple<FdmSchemeDesc, string, bool>(new FdmSchemeDesc().Douglas(), "Douglas", true));
            descs.Add(new Tuple<FdmSchemeDesc, string, bool>(new FdmSchemeDesc().CrankNicolson(), "Crank-Nicolson", true));
            descs.Add(new Tuple<FdmSchemeDesc, string, bool>(new FdmSchemeDesc().ImplicitEuler(), "Implicit", false));
            descs.Add(new Tuple<FdmSchemeDesc, string, bool>(new FdmSchemeDesc().TrBDF2(), "TR-BDF2", false));

            for (int j = 0; j < descs.Count; ++j)
            {
               FdmHestonSolver solver =
                  new FdmHestonSolver(new Handle<HestonProcess>(process),
                                      hestonEngine.getSolverDesc(1.0),
                                      descs[j].Item1);

               List<double> gammas = new List<double>();
               for (double x = 99; x < 101.001; x += 0.1)
               {
                  gammas.Add(solver.gammaAt(x, v0));
               }

               double maximum = Double.MinValue;
               for (int i = 1; i < gammas.Count; ++i)
               {
                  double diff = Math.Abs(gammas[i] - gammas[i - 1]);
                  if (diff > maximum)
                     maximum = diff;
               }

               double tol = 0.01;
               bool hasSpuriousOscillations = maximum > tol;

               if (hasSpuriousOscillations != descs[j].Item3)
               {
                  QAssert.Fail("unable to reproduce spurious oscillation behaviour "
                               + "\n   scheme name          : " + descs[j].Item2
                               + "\n   oscillations observed: "
                               + hasSpuriousOscillations
                               + "\n   oscillations expected: " + descs[j].Item3
                              );
               }
            }
         }
      }
   }
}