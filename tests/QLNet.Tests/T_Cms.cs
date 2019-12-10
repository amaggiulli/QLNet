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
   public class T_Cms
   {
      private class CommonVars
      {
         // global data
         public RelinkableHandle<YieldTermStructure> termStructure;

         public IborIndex iborIndex;

         public Handle<SwaptionVolatilityStructure> atmVol;
         public Handle<SwaptionVolatilityStructure> SabrVolCube1;
         public Handle<SwaptionVolatilityStructure> SabrVolCube2;

         public List<GFunctionFactory.YieldCurveModel> yieldCurveModels;
         public List<CmsCouponPricer> numericalPricers;
         public List<CmsCouponPricer> analyticPricers;

         // cleanup
         public SavedSettings backup;

         // setup
         public CommonVars()
         {
            backup = new SavedSettings();

            Calendar calendar = new TARGET();

            Date referenceDate = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(referenceDate);

            termStructure = new RelinkableHandle<YieldTermStructure>();
            termStructure.linkTo(Utilities.flatRate(referenceDate, 0.05, new Actual365Fixed()));

            // ATM Volatility structure
            List<Period> atmOptionTenors = new List<Period>();
            atmOptionTenors.Add(new Period(1, TimeUnit.Months));
            atmOptionTenors.Add(new Period(6, TimeUnit.Months));
            atmOptionTenors.Add(new Period(1, TimeUnit.Years));
            atmOptionTenors.Add(new Period(5, TimeUnit.Years));
            atmOptionTenors.Add(new Period(10, TimeUnit.Years));
            atmOptionTenors.Add(new Period(30, TimeUnit.Years));

            List<Period> atmSwapTenors = new List<Period>();
            atmSwapTenors.Add(new Period(1, TimeUnit.Years));
            atmSwapTenors.Add(new Period(5, TimeUnit.Years));
            atmSwapTenors.Add(new Period(10, TimeUnit.Years));
            atmSwapTenors.Add(new Period(30, TimeUnit.Years));

            Matrix m = new Matrix(atmOptionTenors.Count, atmSwapTenors.Count);
            m[0, 0] = 0.1300; m[0, 1] = 0.1560; m[0, 2] = 0.1390; m[0, 3] = 0.1220;
            m[1, 0] = 0.1440; m[1, 1] = 0.1580; m[1, 2] = 0.1460; m[1, 3] = 0.1260;
            m[2, 0] = 0.1600; m[2, 1] = 0.1590; m[2, 2] = 0.1470; m[2, 3] = 0.1290;
            m[3, 0] = 0.1640; m[3, 1] = 0.1470; m[3, 2] = 0.1370; m[3, 3] = 0.1220;
            m[4, 0] = 0.1400; m[4, 1] = 0.1300; m[4, 2] = 0.1250; m[4, 3] = 0.1100;
            m[5, 0] = 0.1130; m[5, 1] = 0.1090; m[5, 2] = 0.1070; m[5, 3] = 0.0930;

            atmVol = new Handle<SwaptionVolatilityStructure>(
               new SwaptionVolatilityMatrix(calendar, BusinessDayConvention.Following, atmOptionTenors,
                                            atmSwapTenors, m, new Actual365Fixed()));

            // Vol cubes
            List<Period> optionTenors = new List<Period>();
            optionTenors.Add(new Period(1, TimeUnit.Years));
            optionTenors.Add(new Period(10, TimeUnit.Years));
            optionTenors.Add(new Period(30, TimeUnit.Years));

            List<Period> swapTenors = new List<Period>();
            swapTenors.Add(new Period(2, TimeUnit.Years));
            swapTenors.Add(new Period(10, TimeUnit.Years));
            swapTenors.Add(new Period(30, TimeUnit.Years));

            List<double> strikeSpreads = new List<double>();
            strikeSpreads.Add(-0.020);
            strikeSpreads.Add(-0.005);
            strikeSpreads.Add(+0.000);
            strikeSpreads.Add(+0.005);
            strikeSpreads.Add(+0.020);

            int nRows = optionTenors.Count * swapTenors.Count;
            int nCols = strikeSpreads.Count;
            Matrix volSpreadsMatrix = new Matrix(nRows, nCols);
            volSpreadsMatrix[0, 0] = 0.0599;
            volSpreadsMatrix[0, 1] = 0.0049;
            volSpreadsMatrix[0, 2] = 0.0000;
            volSpreadsMatrix[0, 3] = -0.0001;
            volSpreadsMatrix[0, 4] = 0.0127;

            volSpreadsMatrix[1, 0] = 0.0729;
            volSpreadsMatrix[1, 1] = 0.0086;
            volSpreadsMatrix[1, 2] = 0.0000;
            volSpreadsMatrix[1, 3] = -0.0024;
            volSpreadsMatrix[1, 4] = 0.0098;

            volSpreadsMatrix[2, 0] = 0.0738;
            volSpreadsMatrix[2, 1] = 0.0102;
            volSpreadsMatrix[2, 2] = 0.0000;
            volSpreadsMatrix[2, 3] = -0.0039;
            volSpreadsMatrix[2, 4] = 0.0065;

            volSpreadsMatrix[3, 0] = 0.0465;
            volSpreadsMatrix[3, 1] = 0.0063;
            volSpreadsMatrix[3, 2] = 0.0000;
            volSpreadsMatrix[3, 3] = -0.0032;
            volSpreadsMatrix[3, 4] = -0.0010;

            volSpreadsMatrix[4, 0] = 0.0558;
            volSpreadsMatrix[4, 1] = 0.0084;
            volSpreadsMatrix[4, 2] = 0.0000;
            volSpreadsMatrix[4, 3] = -0.0050;
            volSpreadsMatrix[4, 4] = -0.0057;

            volSpreadsMatrix[5, 0] = 0.0576;
            volSpreadsMatrix[5, 1] = 0.0083;
            volSpreadsMatrix[5, 2] = 0.0000;
            volSpreadsMatrix[5, 3] = -0.0043;
            volSpreadsMatrix[5, 4] = -0.0014;

            volSpreadsMatrix[6, 0] = 0.0437;
            volSpreadsMatrix[6, 1] = 0.0059;
            volSpreadsMatrix[6, 2] = 0.0000;
            volSpreadsMatrix[6, 3] = -0.0030;
            volSpreadsMatrix[6, 4] = -0.0006;

            volSpreadsMatrix[7, 0] = 0.0533;
            volSpreadsMatrix[7, 1] = 0.0078;
            volSpreadsMatrix[7, 2] = 0.0000;
            volSpreadsMatrix[7, 3] = -0.0045;
            volSpreadsMatrix[7, 4] = -0.0046;

            volSpreadsMatrix[8, 0] = 0.0545;
            volSpreadsMatrix[8, 1] = 0.0079;
            volSpreadsMatrix[8, 2] = 0.0000;
            volSpreadsMatrix[8, 3] = -0.0042;
            volSpreadsMatrix[8, 4] = -0.0020;

            List<List<Handle<Quote>>> volSpreads = new InitializedList<List<Handle<Quote>>>(nRows);
            for (int i = 0; i < nRows; ++i)
            {
               volSpreads[i] = new InitializedList<Handle<Quote>>(nCols);
               for (int j = 0; j < nCols; ++j)
               {
                  volSpreads[i][j] = new Handle<Quote>(new SimpleQuote(volSpreadsMatrix[i, j]));
               }
            }

            iborIndex = new Euribor6M(termStructure);
            SwapIndex swapIndexBase = new EuriborSwapIsdaFixA(new Period(10, TimeUnit.Years), termStructure);
            SwapIndex shortSwapIndexBase = new EuriborSwapIsdaFixA(new Period(2, TimeUnit.Years), termStructure);

            bool vegaWeightedSmileFit = false;

            SabrVolCube2 = new Handle<SwaptionVolatilityStructure>(
               new SwaptionVolCube2(atmVol,
                                    optionTenors,
                                    swapTenors,
                                    strikeSpreads,
                                    volSpreads,
                                    swapIndexBase,
                                    shortSwapIndexBase,
                                    vegaWeightedSmileFit));
            SabrVolCube2.link.enableExtrapolation();

            List<List<Handle<Quote>>> guess = new InitializedList<List<Handle<Quote>>>(nRows);
            for (int i = 0; i < nRows; ++i)
            {
               guess[i] = new InitializedList<Handle<Quote>>(4);
               guess[i][0] = new Handle<Quote>(new SimpleQuote(0.2));
               guess[i][1] = new Handle<Quote>(new SimpleQuote(0.5));
               guess[i][2] = new Handle<Quote>(new SimpleQuote(0.4));
               guess[i][3] = new Handle<Quote>(new SimpleQuote(0.0));
            }
            List<bool> isParameterFixed = new InitializedList<bool>(4, false);
            isParameterFixed[1] = true;

            // FIXME
            bool isAtmCalibrated = false;

            SabrVolCube1 = new Handle<SwaptionVolatilityStructure>(
               new SwaptionVolCube1x(atmVol,
                                     optionTenors,
                                     swapTenors,
                                     strikeSpreads,
                                     volSpreads,
                                     swapIndexBase,
                                     shortSwapIndexBase,
                                     vegaWeightedSmileFit,
                                     guess,
                                     isParameterFixed,
                                     isAtmCalibrated));
            SabrVolCube1.link.enableExtrapolation();

            yieldCurveModels = new List<GFunctionFactory.YieldCurveModel>();
            yieldCurveModels.Add(GFunctionFactory.YieldCurveModel.Standard);
            yieldCurveModels.Add(GFunctionFactory.YieldCurveModel.ExactYield);
            yieldCurveModels.Add(GFunctionFactory.YieldCurveModel.ParallelShifts);
            yieldCurveModels.Add(GFunctionFactory.YieldCurveModel.NonParallelShifts);
            yieldCurveModels.Add(GFunctionFactory.YieldCurveModel.NonParallelShifts);   // for linear tsr model

            Handle<Quote> zeroMeanRev = new Handle<Quote>(new SimpleQuote(0.0));

            numericalPricers = new List<CmsCouponPricer>();
            analyticPricers = new List<CmsCouponPricer>();
            for (int j = 0; j < yieldCurveModels.Count; ++j)
            {
               if (j < yieldCurveModels.Count - 1)
                  numericalPricers.Add(new NumericHaganPricer(atmVol, yieldCurveModels[j], zeroMeanRev));
               else
                  numericalPricers.Add(new LinearTsrPricer(atmVol, zeroMeanRev));

               analyticPricers.Add(new AnalyticHaganPricer(atmVol, yieldCurveModels[j], zeroMeanRev));
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFairRate()
      {
         // Testing Hagan-pricer flat-vol equivalence for coupons
         CommonVars vars = new CommonVars();

         SwapIndex swapIndex = new SwapIndex("EuriborSwapIsdaFixA",
                                             new Period(10, TimeUnit.Years),
                                             vars.iborIndex.fixingDays(),
                                             vars.iborIndex.currency(),
                                             vars.iborIndex.fixingCalendar(),
                                             new Period(1, TimeUnit.Years),
                                             BusinessDayConvention.Unadjusted,
                                             vars.iborIndex.dayCounter(),//??
                                             vars.iborIndex);

         // FIXME
         //shared_ptr<SwapIndex> swapIndex(new
         //    EuriborSwapIsdaFixA(10*Years, vars.iborIndex->termStructure()));
         Date startDate = vars.termStructure.link.referenceDate() + new Period(20, TimeUnit.Years);
         Date paymentDate = startDate + new Period(1, TimeUnit.Years);
         Date endDate = paymentDate;
         double nominal = 1.0;
         double? infiniteCap = null;
         double? infiniteFloor = null;
         double gearing = 1.0;
         double spread = 0.0;
         CappedFlooredCmsCoupon coupon = new CappedFlooredCmsCoupon(nominal, paymentDate,
                                                                    startDate, endDate,
                                                                    swapIndex.fixingDays(), swapIndex,
                                                                    gearing, spread,
                                                                    infiniteCap, infiniteFloor,
                                                                    startDate, endDate,
                                                                    vars.iborIndex.dayCounter());

         for (int j = 0; j < vars.yieldCurveModels.Count; ++j)
         {
            vars.numericalPricers[j].setSwaptionVolatility(vars.atmVol);
            coupon.setPricer(vars.numericalPricers[j]);
            double rate0 = coupon.rate();

            vars.analyticPricers[j].setSwaptionVolatility(vars.atmVol);
            coupon.setPricer(vars.analyticPricers[j]);
            double rate1 = coupon.rate();

            double difference =  Math.Abs(rate1 - rate0);
            double tol = 2.0e-4;
            bool linearTsr = j == vars.yieldCurveModels.Count - 1;

            if (difference > tol)
               QAssert.Fail("\nCoupon payment date: " + paymentDate +
                            "\nCoupon start date:   " + startDate +
                            "\nCoupon floor:        " + (infiniteFloor) +
                            "\nCoupon gearing:      " + (gearing) +
                            "\nCoupon swap index:   " + swapIndex.name() +
                            "\nCoupon spread:       " + (spread) +
                            "\nCoupon cap:          " + (infiniteCap) +
                            "\nCoupon DayCounter:   " + vars.iborIndex.dayCounter() +
                            "\nYieldCurve Model:    " + vars.yieldCurveModels[j] +
                            "\nNumerical Pricer:    " + (rate0) + (linearTsr ? " (Linear TSR Model)" : "") +
                            "\nAnalytic Pricer:     " + (rate1) +
                            "\ndifference:          " + (difference) +
                            "\ntolerance:           " + (tol));
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCmsSwap()
      {
         // Testing Hagan-pricer flat-vol equivalence for swaps
         CommonVars vars = new CommonVars();

         SwapIndex swapIndex = new SwapIndex("EuriborSwapIsdaFixA",
                                             new Period(10, TimeUnit.Years),
                                             vars.iborIndex.fixingDays(),
                                             vars.iborIndex.currency(),
                                             vars.iborIndex.fixingCalendar(),
                                             new Period(1, TimeUnit.Years),
                                             BusinessDayConvention.Unadjusted,
                                             vars.iborIndex.dayCounter(),//??
                                             vars.iborIndex);
         // FIXME
         //shared_ptr<SwapIndex> swapIndex(new
         //    EuriborSwapIsdaFixA(10*Years, vars.iborIndex->termStructure()));
         double spread = 0.0;
         List<int> swapLengths = new List<int>();
         swapLengths.Add(1);
         swapLengths.Add(5);
         swapLengths.Add(6);
         swapLengths.Add(10);
         int n = swapLengths.Count;
         List<Swap> cms = new List<Swap>(n);
         for (int i = 0; i < n; ++i)
            // no cap, floor
            // no gearing, spread
            cms.Add(new MakeCms(new Period(swapLengths[i], TimeUnit.Years),
                                swapIndex,
                                vars.iborIndex, spread,
                                new Period(10, TimeUnit.Days)).value());

         for (int j = 0; j < vars.yieldCurveModels.Count; ++j)
         {
            vars.numericalPricers[j].setSwaptionVolatility(vars.atmVol);
            vars.analyticPricers[j].setSwaptionVolatility(vars.atmVol);
            for (int sl = 0; sl < n; ++sl)
            {
               Utils.setCouponPricer(cms[sl].leg(0), vars.numericalPricers[j]);
               double priceNum = cms[sl].NPV();
               Utils.setCouponPricer(cms[sl].leg(0), vars.analyticPricers[j]);
               double priceAn = cms[sl].NPV();

               double difference = Math.Abs(priceNum - priceAn);
               double tol = 2.0e-4;
               bool linearTsr = j == vars.yieldCurveModels.Count - 1;
               if (difference > tol)
                  QAssert.Fail("\nLength in Years:  " + swapLengths[sl] +
                               "\nswap index:       " + swapIndex.name() +
                               "\nibor index:       " + vars.iborIndex.name() +
                               "\nspread:           " + (spread) +
                               "\nYieldCurve Model: " + vars.yieldCurveModels[j] +
                               "\nNumerical Pricer: " + (priceNum) + (linearTsr ? " (Linear TSR Model)" : "") +
                               "\nAnalytic Pricer:  " + (priceAn) +
                               "\ndifference:       " + (difference) +
                               "\ntolerance:        " + (tol));
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testParity()
      {
         // Testing put-call parity for capped-floored CMS coupons

         CommonVars vars = new CommonVars();

         List<Handle<SwaptionVolatilityStructure> > swaptionVols = new List<Handle<SwaptionVolatilityStructure>>();
         swaptionVols.Add(vars.atmVol);
         swaptionVols.Add(vars.SabrVolCube1);
         swaptionVols.Add(vars.SabrVolCube2);

         SwapIndex swapIndex = new EuriborSwapIsdaFixA(new Period(10, TimeUnit.Years),
                                                       vars.iborIndex.forwardingTermStructure());
         Date startDate = vars.termStructure.link.referenceDate() + new Period(20, TimeUnit.Years);
         Date paymentDate = startDate + new Period(1, TimeUnit.Years);
         Date endDate = paymentDate;
         double nominal = 1.0;
         double? infiniteCap = null;
         double? infiniteFloor = null;
         double gearing = 1.0;
         double spread = 0.0;
         double discount = vars.termStructure.link.discount(paymentDate);
         CappedFlooredCmsCoupon swaplet = new CappedFlooredCmsCoupon(nominal, paymentDate,
                                                                     startDate, endDate, swapIndex.fixingDays(), swapIndex, gearing, spread, infiniteCap, infiniteFloor,
                                                                     startDate, endDate, vars.iborIndex.dayCounter());
         for (double strike = .02; strike < .12; strike += 0.05)
         {
            CappedFlooredCmsCoupon caplet = new CappedFlooredCmsCoupon(nominal, paymentDate,
                                                                       startDate, endDate, swapIndex.fixingDays(), swapIndex, gearing, spread, strike, infiniteFloor,
                                                                       startDate, endDate, vars.iborIndex.dayCounter());
            CappedFlooredCmsCoupon floorlet = new CappedFlooredCmsCoupon(nominal, paymentDate,
                                                                         startDate, endDate, swapIndex.fixingDays(), swapIndex, gearing, spread, infiniteCap, strike,
                                                                         startDate, endDate, vars.iborIndex.dayCounter());

            for (int i = 0; i < swaptionVols.Count; ++i)
            {
               for (int j = 0; j < vars.yieldCurveModels.Count; ++j)
               {
                  vars.numericalPricers[j].setSwaptionVolatility(swaptionVols[i]);
                  vars.analyticPricers[j].setSwaptionVolatility(swaptionVols[i]);
                  List<CmsCouponPricer> pricers = new List<CmsCouponPricer>(2);
                  pricers.Add(vars.numericalPricers[j]);
                  pricers.Add(vars.analyticPricers[j]);
                  for (int k = 0; k < pricers.Count; ++k)
                  {
                     swaplet.setPricer(pricers[k]);
                     caplet.setPricer(pricers[k]);
                     floorlet.setPricer(pricers[k]);
                     double swapletPrice = swaplet.price(vars.termStructure) +
                                           nominal * swaplet.accrualPeriod() * strike * discount;
                     double capletPrice = caplet.price(vars.termStructure);
                     double floorletPrice = floorlet.price(vars.termStructure);
                     double difference = Math.Abs(capletPrice + floorletPrice - swapletPrice);
                     double tol = 2.0e-5;
                     bool linearTsr = k == 0 && j == vars.yieldCurveModels.Count - 1;
                     if (linearTsr)
                        tol = 1.0e-7;
                     if (difference > tol)
                        QAssert.Fail("\nCoupon payment date: " + paymentDate +
                                     "\nCoupon start date:   " + startDate +
                                     "\nCoupon gearing:      " + (gearing) +
                                     "\nCoupon swap index:   " + swapIndex.name() +
                                     "\nCoupon spread:       " + (spread) +
                                     "\nstrike:              " + (strike) +
                                     "\nCoupon DayCounter:   " + vars.iborIndex.dayCounter() +
                                     "\nYieldCurve Model:    " + vars.yieldCurveModels[j] +
                                     (k == 0 ? "\nNumerical Pricer" : "\nAnalytic Pricer") +
                                     (linearTsr ? " (Linear TSR Model)" : "") +
                                     "\nSwaplet price:       " + (swapletPrice) +
                                     "\nCaplet price:        " + (capletPrice) +
                                     "\nFloorlet price:      " + (floorletPrice) +
                                     "\ndifference:          " + difference +
                                     "\ntolerance:           " + (tol));

                  }
               }

            }

         }
      }
   }
}
