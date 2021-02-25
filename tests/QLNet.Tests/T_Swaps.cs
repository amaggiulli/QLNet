/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
   public class T_Swaps : IDisposable
   {
      #region Initialize&Cleanup
      private SavedSettings backup;
#if NET452
      [TestInitialize]
      public void testInitialize()
      {
#else
      public T_Swaps()
      {
#endif
         backup = new SavedSettings();
      }
#if NET452
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

      class CommonVars
      {
         // global data
         public Date today, settlement;
         public VanillaSwap.Type type;
         public double nominal;
         public Calendar calendar;
         public BusinessDayConvention fixedConvention, floatingConvention;
         public Frequency fixedFrequency, floatingFrequency;
         public DayCounter fixedDayCount;
         public IborIndex index;
         public int settlementDays;
         public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();

         // utilities
         public VanillaSwap makeSwap(int length, double fixedRate, double floatingSpread)
         {
            Date maturity = calendar.advance(settlement, length, TimeUnit.Years, floatingConvention);
            Schedule fixedSchedule = new Schedule(settlement, maturity, new Period(fixedFrequency),
                                                  calendar, fixedConvention, fixedConvention, DateGeneration.Rule.Forward, false);
            Schedule floatSchedule = new Schedule(settlement, maturity, new Period(floatingFrequency),
                                                  calendar, floatingConvention, floatingConvention, DateGeneration.Rule.Forward, false);
            VanillaSwap swap = new VanillaSwap(type, nominal, fixedSchedule, fixedRate, fixedDayCount,
                                               floatSchedule, index, floatingSpread, index.dayCounter());
            swap.setPricingEngine(new DiscountingSwapEngine(termStructure));
            return swap;
         }

         public CommonVars()
         {
            type = VanillaSwap.Type.Payer;
            settlementDays = 2;
            nominal = 100.0;
            fixedConvention = BusinessDayConvention.Unadjusted;
            floatingConvention = BusinessDayConvention.ModifiedFollowing;
            fixedFrequency = Frequency.Annual;
            floatingFrequency = Frequency.Semiannual;
            fixedDayCount = new Thirty360();

            index = new Euribor(new Period(floatingFrequency), termStructure);

            calendar = index.fixingCalendar();
            today = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(today);
            settlement = calendar.advance(today, settlementDays, TimeUnit.Days);

            termStructure.linkTo(Utilities.flatRate(settlement, 0.05, new Actual365Fixed()));
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFairRate()
      {
         // Testing vanilla-swap calculation of fair fixed rate

         CommonVars vars = new CommonVars();

         int[] lengths = new int[] { 1, 2, 5, 10, 20 };
         double[] spreads = new double[] { -0.001, -0.01, 0.0, 0.01, 0.001 };

         for (int i = 0; i < lengths.Length; i++)
         {
            for (int j = 0; j < spreads.Length; j++)
            {

               VanillaSwap swap = vars.makeSwap(lengths[i], 0.0, spreads[j]);
               swap = vars.makeSwap(lengths[i], swap.fairRate(), spreads[j]);
               if (Math.Abs(swap.NPV()) > 1.0e-10)
               {
                  QAssert.Fail("recalculating with implied rate:\n"
                               + "    length: " + lengths[i] + " years\n"
                               + "    floating spread: "
                               + spreads[j] + "\n"
                               + "    swap value: " + swap.NPV());
               }
            }
         }
      }
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFairSpread()
      {
         // Testing vanilla-swap calculation of fair floating spread
         CommonVars vars = new CommonVars();

         int[] lengths = new int[] { 1, 2, 5, 10, 20 };
         double[] rates = new double[] { 0.04, 0.05, 0.06, 0.07 };

         for (int i = 0; i < lengths.Length; i++)
         {
            for (int j = 0; j < rates.Length; j++)
            {

               VanillaSwap swap = vars.makeSwap(lengths[i], rates[j], 0.0);
               swap = vars.makeSwap(lengths[i], rates[j], swap.fairSpread());
               if (Math.Abs(swap.NPV()) > 1.0e-10)
               {
                  QAssert.Fail("recalculating with implied spread:\n"
                               + "    length: " + lengths[i] + " years\n"
                               + "    fixed rate: "
                               + rates[j] + "\n"
                               + "    swap value: " + swap.NPV());
               }
            }
         }
      }
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testRateDependency()
      {
         // Testing vanilla-swap dependency on fixed rate
         CommonVars vars = new CommonVars();

         int[] lengths = new int[] { 1, 2, 5, 10, 20 };
         double[] spreads = new double[] { -0.001, -0.01, 0.0, 0.01, 0.001 };
         double[] rates = new double[] { 0.03, 0.04, 0.05, 0.06, 0.07 };

         for (int i = 0; i < lengths.Length; i++)
         {
            for (int j = 0; j < spreads.Length; j++)
            {

               // store the results for different rates...
               List<double> swap_values = new List<double>();
               for (int k = 0; k < rates.Length; k++)
               {
                  VanillaSwap swap = vars.makeSwap(lengths[i], rates[k], spreads[j]);
                  swap_values.Add(swap.NPV());
               }

               // and check that they go the right way
               for (int z = 0; z < swap_values.Count - 1; z++)
               {
                  if (swap_values[z] < swap_values[z + 1])
                     QAssert.Fail(
                        "NPV is increasing with the fixed rate in a swap: \n"
                        + "    length: " + lengths[i] + " years\n"
                        + "    value:  " + swap_values[z]
                        + " paying fixed rate: " + rates[z] + "\n"
                        + "    value:  " + swap_values[z + 1]
                        + " paying fixed rate: " + rates[z + 1]);
               }
            }
         }
      }
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSpreadDependency()
      {
         // Testing vanilla-swap dependency on floating spread
         CommonVars vars = new CommonVars();

         int[] lengths = new int[] { 1, 2, 5, 10, 20 };
         double[] spreads = new double[] { -0.01, -0.002, -0.001, 0.0, 0.001, 0.002, 0.01 };
         double[] rates = new double[] { 0.04, 0.05, 0.06, 0.07 };

         for (int i = 0; i < lengths.Length; i++)
         {
            for (int j = 0; j < rates.Length; j++)
            {

               // store the results for different rates...
               List<double> swap_values = new List<double>();
               for (int k = 0; k < spreads.Length; k++)
               {
                  VanillaSwap swap = vars.makeSwap(lengths[i], rates[j], spreads[k]);
                  swap_values.Add(swap.NPV());
               }

               // and check that they go the right way
               for (int z = 0; z < swap_values.Count - 1; z++)
               {
                  if (swap_values[z] > swap_values[z + 1])
                     QAssert.Fail(
                        "NPV is decreasing with the floating spread in a swap: \n"
                        + "    length: " + lengths[i] + " years\n"
                        + "    value:  " + swap_values[z]
                        + " receiving spread: " + rates[z] + "\n"
                        + "    value:  " + swap_values[z + 1]
                        + " receiving spread: " + rates[z + 1]);
               }
            }
         }
      }
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testInArrears()
      {
         // Testing in-arrears swap calculation
         CommonVars vars = new CommonVars();

         /* See Hull, 4th ed., page 550
            Note: the calculation in the book is wrong (work out the adjustment and you'll get 0.05 + 0.000115 T1) */
         Date maturity = vars.today + new Period(5, TimeUnit.Years);
         Calendar calendar = new NullCalendar();
         Schedule schedule = new Schedule(vars.today, maturity, new Period(Frequency.Annual), calendar,
                                          BusinessDayConvention.Following, BusinessDayConvention.Following,
                                          DateGeneration.Rule.Forward, false);
         DayCounter dayCounter = new SimpleDayCounter();

         List<double> nominals = new List<double>() { 100000000.0 };

         IborIndex index = new IborIndex("dummy", new Period(1, TimeUnit.Years), 0, new EURCurrency(), calendar,
                                         BusinessDayConvention.Following, false, dayCounter, vars.termStructure);
         double oneYear = 0.05;
         double r = Math.Log(1.0 + oneYear);
         vars.termStructure.linkTo(Utilities.flatRate(vars.today, r, dayCounter));

         List<double> coupons = new List<double>() { oneYear };
         List<CashFlow> fixedLeg = new FixedRateLeg(schedule)
         .withCouponRates(coupons, dayCounter)
         .withNotionals(nominals);

         List<double> gearings = new List<double>();
         List<double> spreads = new List<double>();
         int fixingDays = 0;

         double capletVolatility = 0.22;
         var vol = new Handle<OptionletVolatilityStructure>(
            new ConstantOptionletVolatility(vars.today, new NullCalendar(),
                                            BusinessDayConvention.Following, capletVolatility, dayCounter));
         IborCouponPricer pricer = new BlackIborCouponPricer(vol);

         List<CashFlow> floatingLeg = new IborLeg(schedule, index)
         .withPaymentDayCounter(dayCounter)
         .withFixingDays(fixingDays)
         .withGearings(gearings)
         .withSpreads(spreads)
         .inArrears()
         .withNotionals(nominals);
         Utils.setCouponPricer(floatingLeg, pricer);

         Swap swap = new Swap(floatingLeg, fixedLeg);
         swap.setPricingEngine(new DiscountingSwapEngine(vars.termStructure));

         double storedValue = -144813.0;
         double tolerance = 1.0;

         if (Math.Abs(swap.NPV() - storedValue) > tolerance)
            QAssert.Fail("Wrong NPV calculation:\n"
                         + "    expected:   " + storedValue + "\n"
                         + "    calculated: " + swap.NPV());
      }
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCachedValue()
      {
         // Testing vanilla-swap calculation against cached value
         CommonVars vars = new CommonVars();

         vars.today = new Date(17, Month.June, 2002);
         Settings.setEvaluationDate(vars.today);
         vars.settlement = vars.calendar.advance(vars.today, vars.settlementDays, TimeUnit.Days);
         vars.termStructure.linkTo(Utilities.flatRate(vars.settlement, 0.05, new Actual365Fixed()));

         VanillaSwap swap = vars.makeSwap(10, 0.06, 0.001);
#if QL_USE_INDEXED_COUPON
         double cachedNPV   = -5.872342992212;
#else
         double cachedNPV = -5.872863313209;
#endif

         if (Math.Abs(swap.NPV() - cachedNPV) > 1.0e-11)
            QAssert.Fail("failed to reproduce cached swap value:\n"
                         + "    calculated: " + swap.NPV() + "\n"
                         + "    expected:   " + cachedNPV);
      }
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFixing()
      {
         Date tradeDate = new Date(17, Month.April, 2015);
         Calendar calendar = new UnitedKingdom();
         Date settlementDate = calendar.advance(tradeDate, 2, TimeUnit.Days, BusinessDayConvention.Following);
         Date maturityDate = calendar.advance(settlementDate, 5, TimeUnit.Years, BusinessDayConvention.Following);

         Date valueDate = new Date(20, Month.April, 2015);
         Settings.setEvaluationDate(valueDate);

         List<Date> dates = new List<Date>();
         dates.Add(valueDate);
         dates.Add(valueDate + new Period(1, TimeUnit.Years));
         dates.Add(valueDate + new Period(2, TimeUnit.Years));
         dates.Add(valueDate + new Period(5, TimeUnit.Years));
         dates.Add(valueDate + new Period(10, TimeUnit.Years));
         dates.Add(valueDate + new Period(20, TimeUnit.Years));

         List<double> rates = new List<double>();
         rates.Add(0.01);
         rates.Add(0.01);
         rates.Add(0.01);
         rates.Add(0.01);
         rates.Add(0.01);
         rates.Add(0.01);

         var discountCurveHandle = new RelinkableHandle<YieldTermStructure>();
         var forecastCurveHandle = new RelinkableHandle<YieldTermStructure>();
         GBPLibor index = new GBPLibor(new Period(6, TimeUnit.Months), forecastCurveHandle);
         InterpolatedZeroCurve<Linear> zeroCurve = new InterpolatedZeroCurve<Linear>(dates, rates, new Actual360(), new Linear());
         var fixedSchedule = new Schedule(settlementDate, maturityDate, new Period(1, TimeUnit.Years), calendar, BusinessDayConvention.Following, BusinessDayConvention.Following, DateGeneration.Rule.Forward, false);
         var floatSchedule = new Schedule(settlementDate, maturityDate, index.tenor(), calendar, BusinessDayConvention.Following, BusinessDayConvention.Following, DateGeneration.Rule.Forward, false);
         VanillaSwap swap = new VanillaSwap(VanillaSwap.Type.Payer, 1000000, fixedSchedule, 0.01, new Actual360(), floatSchedule, index, 0, new Actual360());
         discountCurveHandle.linkTo(zeroCurve);
         forecastCurveHandle.linkTo(zeroCurve);
         var swapEngine = new DiscountingSwapEngine(discountCurveHandle, false, null);
         swap.setPricingEngine(swapEngine);

         try
         {
            double npv = swap.NPV();
         }
         catch (Exception ex)
         {
            QAssert.Fail(ex.Message);
         }
      }

   }

}
