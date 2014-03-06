/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   [TestClass()]
   public class T_Bonds
   {
      class CommonVars
      {
         // common data
         public Calendar calendar;
         public Date today;
         public double faceAmount;

         // todo
         // cleanup
         // SavedSettings backup;

         // setup
         public CommonVars()
         {
            calendar = new TARGET();
            today = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(today);
            faceAmount = 1000000.0;
         }
      }

      [TestMethod()]
      public void testYield()
      {

         //"Testing consistency of bond price/yield calculation...");

         CommonVars vars = new CommonVars();

         double tolerance = 1.0e-7;
         int maxEvaluations = 100;

         int[] issueMonths = new int[] { -24, -18, -12, -6, 0, 6, 12, 18, 24 };
         int[] lengths = new int[] { 3, 5, 10, 15, 20 };
         int settlementDays = 3;
         double[] coupons = new double[] { 0.02, 0.05, 0.08 };
         Frequency[] frequencies = new Frequency[] { Frequency.Semiannual, Frequency.Annual };
         DayCounter bondDayCount = new Thirty360();
         BusinessDayConvention accrualConvention = BusinessDayConvention.Unadjusted;
         BusinessDayConvention paymentConvention = BusinessDayConvention.ModifiedFollowing;
         double redemption = 100.0;

         double[] yields = new double[] { 0.03, 0.04, 0.05, 0.06, 0.07 };
         Compounding[] compounding = new Compounding[] { Compounding.Compounded, Compounding.Continuous };

         for (int i = 0; i < issueMonths.Length; i++)
         {
            for (int j = 0; j < lengths.Length; j++)
            {
               for (int k = 0; k < coupons.Length; k++)
               {
                  for (int l = 0; l < frequencies.Length; l++)
                  {
                     for (int n = 0; n < compounding.Length; n++)
                     {

                        Date dated = vars.calendar.advance(vars.today, issueMonths[i], TimeUnit.Months);
                        Date issue = dated;
                        Date maturity = vars.calendar.advance(issue, lengths[j], TimeUnit.Years);

                        Schedule sch = new Schedule(dated, maturity, new Period(frequencies[l]), vars.calendar,
                                                    accrualConvention, accrualConvention, DateGeneration.Rule.Backward, false);

                        FixedRateBond bond = new FixedRateBond(settlementDays, vars.faceAmount, sch,
                                                               new List<double>() { coupons[k] },
                                                               bondDayCount, paymentConvention,
                                                               redemption, issue);

                        for (int m = 0; m < yields.Length; m++)
                        {

                           double price = bond.cleanPrice(yields[m], bondDayCount, compounding[n], frequencies[l]);
                           double calculated = bond.yield(price, bondDayCount, compounding[n], frequencies[l], null,
                                                          tolerance, maxEvaluations);

                           if (Math.Abs(yields[m] - calculated) > tolerance)
                           {
                              // the difference might not matter
                              double price2 = bond.cleanPrice(calculated, bondDayCount, compounding[n], frequencies[l]);
                              if (Math.Abs(price - price2) / price > tolerance)
                              {
                                 Assert.Fail("yield recalculation failed:\n"
                                     + "    issue:     " + issue + "\n"
                                     + "    maturity:  " + maturity + "\n"
                                     + "    coupon:    " + coupons[k] + "\n"
                                     + "    frequency: " + frequencies[l] + "\n\n"
                                     + "    yield:  " + yields[m] + " "
                                     + (compounding[n] == Compounding.Compounded ? "compounded" : "continuous") + "\n"
                                     + "    price:  " + price + "\n"
                                     + "    yield': " + calculated + "\n"
                                     + "    price': " + price2);
                              }
                           }
                        }
                     }
                  }
               }
            }
         }
      }
      [TestMethod()]
      public void testTheoretical()
      {
         // "Testing theoretical bond price/yield calculation...");

         CommonVars vars = new CommonVars();

         double tolerance = 1.0e-7;
         int maxEvaluations = 100;

         int[] lengths = new int[] { 3, 5, 10, 15, 20 };
         int settlementDays = 3;
         double[] coupons = new double[] { 0.02, 0.05, 0.08 };
         Frequency[] frequencies = new Frequency[] { Frequency.Semiannual, Frequency.Annual };
         DayCounter bondDayCount = new Actual360();
         BusinessDayConvention accrualConvention = BusinessDayConvention.Unadjusted;
         BusinessDayConvention paymentConvention = BusinessDayConvention.ModifiedFollowing;
         double redemption = 100.0;

         double[] yields = new double[] { 0.03, 0.04, 0.05, 0.06, 0.07 };

         for (int j = 0; j < lengths.Length; j++)
         {
            for (int k = 0; k < coupons.Length; k++)
            {
               for (int l = 0; l < frequencies.Length; l++)
               {

                  Date dated = vars.today;
                  Date issue = dated;
                  Date maturity = vars.calendar.advance(issue, lengths[j], TimeUnit.Years);

                  SimpleQuote rate = new SimpleQuote(0.0);
                  var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(vars.today, rate, bondDayCount));

                  Schedule sch = new Schedule(dated, maturity, new Period(frequencies[l]), vars.calendar,
                                              accrualConvention, accrualConvention, DateGeneration.Rule.Backward, false);

                  FixedRateBond bond = new FixedRateBond(settlementDays, vars.faceAmount, sch, new List<double>() { coupons[k] },
                                                         bondDayCount, paymentConvention, redemption, issue);

                  IPricingEngine bondEngine = new DiscountingBondEngine(discountCurve);
                  bond.setPricingEngine(bondEngine);

                  for (int m = 0; m < yields.Length; m++)
                  {

                     rate.setValue(yields[m]);

                     double price = bond.cleanPrice(yields[m], bondDayCount, Compounding.Continuous, frequencies[l]);
                     double calculatedPrice = bond.cleanPrice();

                     if (Math.Abs(price - calculatedPrice) > tolerance)
                     {
                        Assert.Fail("price calculation failed:"
                            + "\n    issue:     " + issue
                            + "\n    maturity:  " + maturity
                            + "\n    coupon:    " + coupons[k]
                            + "\n    frequency: " + frequencies[l] + "\n"
                            + "\n    yield:     " + yields[m]
                            + "\n    expected:    " + price
                            + "\n    calculated': " + calculatedPrice
                            + "\n    error':      " + (price - calculatedPrice));
                     }

                     double calculatedYield = bond.yield(bondDayCount, Compounding.Continuous, frequencies[l],
                                              tolerance, maxEvaluations);
                     if (Math.Abs(yields[m] - calculatedYield) > tolerance)
                     {
                        Assert.Fail("yield calculation failed:"
                            + "\n    issue:     " + issue
                            + "\n    maturity:  " + maturity
                            + "\n    coupon:    " + coupons[k]
                            + "\n    frequency: " + frequencies[l] + "\n"
                            + "\n    yield:  " + yields[m]
                            + "\n    price:  " + price
                            + "\n    yield': " + calculatedYield);
                     }
                  }
               }
            }
         }
      }
      [TestMethod()]
      public void testCached()
      {
         // ("Testing bond price/yield calculation against cached values...");

         CommonVars vars = new CommonVars();

         // with implicit settlement calculation:
         Date today = new Date(22, Month.November, 2004);
         Settings.setEvaluationDate(today);

         Calendar bondCalendar = new NullCalendar();
         DayCounter bondDayCount = new ActualActual(ActualActual.Convention.ISMA);
         int settlementDays = 1;

         var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, new SimpleQuote(0.03), new Actual360()));

         // actual market values from the evaluation date
         Frequency freq = Frequency.Semiannual;
         Schedule sch1 = new Schedule(new Date(31, Month.October, 2004), new Date(31, Month.October, 2006), new Period(freq),
                                      bondCalendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                      DateGeneration.Rule.Backward, false);

         FixedRateBond bond1 = new FixedRateBond(settlementDays, vars.faceAmount, sch1, new List<double>() { 0.025 },
                                 bondDayCount, BusinessDayConvention.ModifiedFollowing, 100.0, new Date(1, Month.November, 2004));

         IPricingEngine bondEngine = new DiscountingBondEngine(discountCurve);
         bond1.setPricingEngine(bondEngine);

         double marketPrice1 = 99.203125;
         double marketYield1 = 0.02925;

         Schedule sch2 = new Schedule(new Date(15, Month.November, 2004), new Date(15, Month.November, 2009), new Period(freq),
                                      bondCalendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                      DateGeneration.Rule.Backward, false);

         FixedRateBond bond2 = new FixedRateBond(settlementDays, vars.faceAmount, sch2, new List<double>() { 0.035 },
                                      bondDayCount, BusinessDayConvention.ModifiedFollowing,
                                      100.0, new Date(15, Month.November, 2004));

         bond2.setPricingEngine(bondEngine);

         double marketPrice2 = 99.6875;
         double marketYield2 = 0.03569;

         // calculated values
         double cachedPrice1a = 99.204505, cachedPrice2a = 99.687192;
         double cachedPrice1b = 98.943393, cachedPrice2b = 101.986794;
         double cachedYield1a = 0.029257, cachedYield2a = 0.035689;
         double cachedYield1b = 0.029045, cachedYield2b = 0.035375;
         double cachedYield1c = 0.030423, cachedYield2c = 0.030432;

         // check
         double tolerance = 1.0e-6;
         double price, yield;

         price = bond1.cleanPrice(marketYield1, bondDayCount, Compounding.Compounded, freq);
         if (Math.Abs(price - cachedPrice1a) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:"
                       + "\n    calculated: " + price
                       + "\n    expected:   " + cachedPrice1a
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (price - cachedPrice1a));
         }

         price = bond1.cleanPrice();
         if (Math.Abs(price - cachedPrice1b) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:"
                       + "\n    calculated: " + price
                       + "\n    expected:   " + cachedPrice1b
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (price - cachedPrice1b));
         }

         yield = bond1.yield(marketPrice1, bondDayCount, Compounding.Compounded, freq);
         if (Math.Abs(yield - cachedYield1a) > tolerance)
         {
            Assert.Fail("failed to reproduce cached compounded yield:"
                       + "\n    calculated: " + yield
                       + "\n    expected:   " + cachedYield1a
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (yield - cachedYield1a));
         }

         yield = bond1.yield(marketPrice1, bondDayCount, Compounding.Continuous, freq);
         if (Math.Abs(yield - cachedYield1b) > tolerance)
         {
            Assert.Fail("failed to reproduce cached continuous yield:"
                       + "\n    calculated: " + yield
                       + "\n    expected:   " + cachedYield1b
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (yield - cachedYield1b));
         }

         yield = bond1.yield(bondDayCount, Compounding.Continuous, freq);
         if (Math.Abs(yield - cachedYield1c) > tolerance)
         {
            Assert.Fail("failed to reproduce cached continuous yield:"
                       + "\n    calculated: " + yield
                       + "\n    expected:   " + cachedYield1c
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (yield - cachedYield1c));
         }


         price = bond2.cleanPrice(marketYield2, bondDayCount, Compounding.Compounded, freq);
         if (Math.Abs(price - cachedPrice2a) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:"
                       + "\n    calculated: " + price
                       + "\n    expected:   " + cachedPrice2a
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (price - cachedPrice2a));
         }

         price = bond2.cleanPrice();
         if (Math.Abs(price - cachedPrice2b) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:"
                       + "\n    calculated: " + price
                       + "\n    expected:   " + cachedPrice2b
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (price - cachedPrice2b));
         }

         yield = bond2.yield(marketPrice2, bondDayCount, Compounding.Compounded, freq);
         if (Math.Abs(yield - cachedYield2a) > tolerance)
         {
            Assert.Fail("failed to reproduce cached compounded yield:"
                       + "\n    calculated: " + yield
                       + "\n    expected:   " + cachedYield2a
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (yield - cachedYield2a));
         }

         yield = bond2.yield(marketPrice2, bondDayCount, Compounding.Continuous, freq);
         if (Math.Abs(yield - cachedYield2b) > tolerance)
         {
            Assert.Fail("failed to reproduce cached continuous yield:"
                       + "\n    calculated: " + yield
                       + "\n    expected:   " + cachedYield2b
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (yield - cachedYield2b));
         }

         yield = bond2.yield(bondDayCount, Compounding.Continuous, freq);
         if (Math.Abs(yield - cachedYield2c) > tolerance)
         {
            Assert.Fail("failed to reproduce cached continuous yield:"
                       + "\n    calculated: " + yield
                       + "\n    expected:   " + cachedYield2c
                       + "\n    tolerance:  " + tolerance
                       + "\n    error:      " + (yield - cachedYield2c));
         }

         // with explicit settlement date:
         Schedule sch3 = new Schedule(new Date(30, Month.November, 2004), new Date(30, Month.November, 2006), new Period(freq),
                                      new UnitedStates(UnitedStates.Market.GovernmentBond), BusinessDayConvention.Unadjusted,
                                      BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false);

         FixedRateBond bond3 = new FixedRateBond(settlementDays, vars.faceAmount, sch3, new List<double>() { 0.02875 },
                             new ActualActual(ActualActual.Convention.ISMA),
                             BusinessDayConvention.ModifiedFollowing, 100.0, new Date(30, Month.November, 2004));

         bond3.setPricingEngine(bondEngine);

         double marketYield3 = 0.02997;

         Date settlementDate = new Date(30, Month.November, 2004);
         double cachedPrice3 = 99.764759;

         price = bond3.cleanPrice(marketYield3, bondDayCount, Compounding.Compounded, freq, settlementDate);
         if (Math.Abs(price - cachedPrice3) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:"
                       + "\n    calculated: " + price + ""
                       + "\n    expected:   " + cachedPrice3 + ""
                       + "\n    error:      " + (price - cachedPrice3));
         }

         // this should give the same result since the issue date is the
         // earliest possible settlement date
         Settings.setEvaluationDate(new Date(22, Month.November, 2004));

         price = bond3.cleanPrice(marketYield3, bondDayCount, Compounding.Compounded, freq);
         if (Math.Abs(price - cachedPrice3) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:"
                       + "\n    calculated: " + price + ""
                       + "\n    expected:   " + cachedPrice3 + ""
                       + "\n    error:      " + (price - cachedPrice3));
         }
      }
      [TestMethod()]
      public void testCachedZero()
      {
         Console.WriteLine("Testing zero-coupon bond prices against cached values...");

         CommonVars vars = new CommonVars();

         Date today = new Date(22, Month.November, 2004);
         Settings.setEvaluationDate(today);

         int settlementDays = 1;

         var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

         double tolerance = 1.0e-6;

         // plain
         ZeroCouponBond bond1 = new ZeroCouponBond(settlementDays, new UnitedStates(UnitedStates.Market.GovernmentBond),
                              vars.faceAmount, new Date(30, Month.November, 2008), BusinessDayConvention.ModifiedFollowing,
                              100.0, new Date(30, Month.November, 2004));

         IPricingEngine bondEngine = new DiscountingBondEngine(discountCurve);
         bond1.setPricingEngine(bondEngine);

         double cachedPrice1 = 88.551726;

         double price = bond1.cleanPrice();
         if (Math.Abs(price - cachedPrice1) > tolerance)
         {
            Console.WriteLine("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice1 + "\n"
                       + "    error:      " + (price - cachedPrice1));
         }

         ZeroCouponBond bond2 = new ZeroCouponBond(settlementDays, new UnitedStates(UnitedStates.Market.GovernmentBond),
                              vars.faceAmount, new Date(30, Month.November, 2007), BusinessDayConvention.ModifiedFollowing,
                              100.0, new Date(30, Month.November, 2004));

         bond2.setPricingEngine(bondEngine);

         double cachedPrice2 = 91.278949;

         price = bond2.cleanPrice();
         if (Math.Abs(price - cachedPrice2) > tolerance)
         {
            Console.WriteLine("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice2 + "\n"
                       + "    error:      " + (price - cachedPrice2));
         }

         ZeroCouponBond bond3 = new ZeroCouponBond(settlementDays, new UnitedStates(UnitedStates.Market.GovernmentBond),
                              vars.faceAmount, new Date(30, Month.November, 2006), BusinessDayConvention.ModifiedFollowing,
                              100.0, new Date(30, Month.November, 2004));

         bond3.setPricingEngine(bondEngine);

         double cachedPrice3 = 94.098006;

         price = bond3.cleanPrice();
         if (Math.Abs(price - cachedPrice3) > tolerance)
         {
            Console.WriteLine("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice3 + "\n"
                       + "    error:      " + (price - cachedPrice3));
         }
      }
      [TestMethod()]
      public void testCachedFixed()
      {
         // "Testing fixed-coupon bond prices against cached values...");

         CommonVars vars = new CommonVars();

         Date today = new Date(22, Month.November, 2004);
         Settings.setEvaluationDate(today);

         int settlementDays = 1;

         var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

         double tolerance = 1.0e-6;

         // plain
         Schedule sch = new Schedule(new Date(30, Month.November, 2004),
                      new Date(30, Month.November, 2008), new Period(Frequency.Semiannual),
                      new UnitedStates(UnitedStates.Market.GovernmentBond),
                      BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false);

         FixedRateBond bond1 = new FixedRateBond(settlementDays, vars.faceAmount, sch, new List<double>() { 0.02875 },
                             new ActualActual(ActualActual.Convention.ISMA), BusinessDayConvention.ModifiedFollowing,
                             100.0, new Date(30, Month.November, 2004));

         IPricingEngine bondEngine = new DiscountingBondEngine(discountCurve);
         bond1.setPricingEngine(bondEngine);

         double cachedPrice1 = 99.298100;

         double price = bond1.cleanPrice();
         if (Math.Abs(price - cachedPrice1) > tolerance)
         {
            Console.WriteLine("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice1 + "\n"
                       + "    error:      " + (price - cachedPrice1));
         }

         // varying coupons
         InitializedList<double> couponRates = new InitializedList<double>(4);
         couponRates[0] = 0.02875;
         couponRates[1] = 0.03;
         couponRates[2] = 0.03125;
         couponRates[3] = 0.0325;

         FixedRateBond bond2 = new FixedRateBond(settlementDays, vars.faceAmount, sch, couponRates,
                               new ActualActual(ActualActual.Convention.ISMA),
                               BusinessDayConvention.ModifiedFollowing,
                               100.0, new Date(30, Month.November, 2004));

         bond2.setPricingEngine(bondEngine);

         double cachedPrice2 = 100.334149;

         price = bond2.cleanPrice();
         if (Math.Abs(price - cachedPrice2) > tolerance)
         {
            Console.WriteLine("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice2 + "\n"
                       + "    error:      " + (price - cachedPrice2));
         }

         // stub date
         Schedule sch3 = new Schedule(new Date(30, Month.November, 2004),
                       new Date(30, Month.March, 2009), new Period(Frequency.Semiannual),
                       new UnitedStates(UnitedStates.Market.GovernmentBond),
                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false,
                       null, new Date(30, Month.November, 2008));

         FixedRateBond bond3 = new FixedRateBond(settlementDays, vars.faceAmount, sch3,
                               couponRates, new ActualActual(ActualActual.Convention.ISMA),
                               BusinessDayConvention.ModifiedFollowing,
                               100.0, new Date(30, Month.November, 2004));

         bond3.setPricingEngine(bondEngine);

         double cachedPrice3 = 100.382794;

         price = bond3.cleanPrice();
         if (Math.Abs(price - cachedPrice3) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice3 + "\n"
                       + "    error:      " + (price - cachedPrice3));
         }
      }
      [TestMethod()]
      public void testCachedFloating()
      {
         // "Testing floating-rate bond prices against cached values...");

         CommonVars vars = new CommonVars();

         Date today = new Date(22, Month.November, 2004);
         Settings.setEvaluationDate(today);

         int settlementDays = 1;

         var riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
         var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

         IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
         int fixingDays = 1;

         double tolerance = 1.0e-6;

         IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

         // plain
         Schedule sch = new Schedule(new Date(30, Month.November, 2004), new Date(30, Month.November, 2008),
                                     new Period(Frequency.Semiannual), new UnitedStates(UnitedStates.Market.GovernmentBond),
                                     BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                     DateGeneration.Rule.Backward, false);

         FloatingRateBond bond1 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                index, new ActualActual(ActualActual.Convention.ISMA),
                                BusinessDayConvention.ModifiedFollowing, fixingDays,
                                new List<double>(), new List<double>(),
                                new List<double>(), new List<double>(),
                                false,
                                100.0, new Date(30, Month.November, 2004));

         IPricingEngine bondEngine = new DiscountingBondEngine(riskFreeRate);
         bond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(bond1.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
            double cachedPrice1 = 99.874645;
#else
         double cachedPrice1 = 99.874646;
#endif


         double price = bond1.cleanPrice();
         if (Math.Abs(price - cachedPrice1) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice1 + "\n"
                       + "    error:      " + (price - cachedPrice1));
         }

         // different risk-free and discount curve
         FloatingRateBond bond2 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                index, new ActualActual(ActualActual.Convention.ISMA),
                                BusinessDayConvention.ModifiedFollowing, fixingDays,
                                new List<double>(), new List<double>(),
                                new List<double>(), new List<double>(),
                                false,
                                100.0, new Date(30, Month.November, 2004));

         IPricingEngine bondEngine2 = new DiscountingBondEngine(discountCurve);
         bond2.setPricingEngine(bondEngine2);

         Utils.setCouponPricer(bond2.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
            double cachedPrice2 = 97.955904;
#else
         double cachedPrice2 = 97.955904;
#endif

         price = bond2.cleanPrice();
         if (Math.Abs(price - cachedPrice2) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice2 + "\n"
                       + "    error:      " + (price - cachedPrice2));
         }

         // varying spread
         InitializedList<double> spreads = new InitializedList<double>(4);
         spreads[0] = 0.001;
         spreads[1] = 0.0012;
         spreads[2] = 0.0014;
         spreads[3] = 0.0016;

         FloatingRateBond bond3 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                index, new ActualActual(ActualActual.Convention.ISMA),
                                BusinessDayConvention.ModifiedFollowing, fixingDays,
                                new List<double>(), spreads,
                                new List<double>(), new List<double>(),
                                false,
                                100.0, new Date(30, Month.November, 2004));

         bond3.setPricingEngine(bondEngine2);

         Utils.setCouponPricer(bond3.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
            double cachedPrice3 = 98.495458;
#else
         double cachedPrice3 = 98.495459;
#endif

         price = bond3.cleanPrice();
         if (Math.Abs(price - cachedPrice3) > tolerance)
         {
            Assert.Fail("failed to reproduce cached price:\n"
                       + "    calculated: " + price + "\n"
                       + "    expected:   " + cachedPrice3 + "\n"
                       + "    error:      " + (price - cachedPrice3));
         }
      }
      [TestMethod()]
      public void testBrazilianCached()
      {
         //("Testing Brazilian public bond prices against cached values...");

         CommonVars vars = new CommonVars();

         double faceAmount = 1000.0;
         double redemption = 100.0;
         Date issueDate = new Date(1,Month.January,2007);

         Date today = new Date(6, Month.June, 2007);
         Settings.setEvaluationDate(today);

         // NTN-F maturity dates
         InitializedList<Date> maturityDates = new InitializedList<Date>(6);
         maturityDates[0] = new Date(1, Month.January, 2008);
         maturityDates[1] = new Date(1, Month.January, 2010);
         maturityDates[2] = new Date(1, Month.July, 2010);
         maturityDates[3] = new Date(1, Month.January, 2012);
         maturityDates[4] = new Date(1, Month.January, 2014);
         maturityDates[5] = new Date(1, Month.January, 2017);

         // NTN-F yields
         InitializedList<double> yields = new InitializedList<double>(6);
         yields[0] = 0.114614;
         yields[1] = 0.105726;
         yields[2] = 0.105328;
         yields[3] = 0.104283;
         yields[4] = 0.103218;
         yields[5] = 0.102948;

         // NTN-F prices
         InitializedList<double> prices = new InitializedList<double>(6);
         prices[0] = 1034.63031372;
         prices[1] = 1030.09919487;
         prices[2] = 1029.98307160;
         prices[3] = 1028.13585068;
         prices[4] = 1028.33383817;
         prices[5] = 1026.19716497;

         int settlementDays = 1;
         vars.faceAmount = 1000.0;

         // The tolerance is high because Andima truncate yields
         double tolerance = 1.0e-4;

         InitializedList<InterestRate> couponRates = new InitializedList<InterestRate>(1);
         couponRates[0] = new InterestRate(0.1, new Thirty360(), Compounding.Compounded, Frequency.Annual);

         for (int bondIndex = 0; bondIndex < maturityDates.Count; bondIndex++)
         {

            // plain
            InterestRate yield = new InterestRate(yields[bondIndex], new Business252(new Brazil()),
                                                  Compounding.Compounded, Frequency.Annual);

            Schedule schedule = new Schedule(new Date(1, Month.January, 2007),
                              maturityDates[bondIndex], new Period(Frequency.Semiannual),
                              new Brazil(Brazil.Market.Settlement),
                              BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                              DateGeneration.Rule.Backward, false);


            FixedRateBond bond = new FixedRateBond(settlementDays,
                                                   faceAmount,
                                                   schedule,
                                                   couponRates,
                                                   BusinessDayConvention.Following,
                                                   redemption,
                                                   issueDate);

            double cachedPrice = prices[bondIndex];

            double price = vars.faceAmount * (bond.cleanPrice(yield.rate(),
                                                         yield.dayCounter(),
                                                         yield.compounding(),
                                                         yield.frequency(),
                                                         today) + bond.accruedAmount(today)) / 100;
            if (Math.Abs(price - cachedPrice) > tolerance)
            {
               Assert.Fail("failed to reproduce cached price:\n"
                           + "    calculated: " + price + "\n"
                           + "    expected:   " + cachedPrice + "\n"
                           + "    error:      " + (price - cachedPrice) + "\n"
                           );
            }
         }
      }

      [TestMethod()]
      public void testAmortizingFixedBond()
      {
         Date startDate = new Date(2, 1, 2007);
         Settings.setEvaluationDate(startDate);

         Period bondLength = new Period(12, TimeUnit.Months);
         DayCounter dCounter = new Thirty360();
         Frequency payFrequency = Frequency.Monthly;
         double amount = 400000000;
         double rate = 0.06;
         var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(startDate, new SimpleQuote(rate), new Thirty360()));

         AmortizingFixedRateBond bond = BondFactory.makeAmortizingFixedBond(startDate, bondLength, dCounter, payFrequency, amount, rate);
         IPricingEngine bondEngine = new DiscountingBondEngine(discountCurve);
         bond.setPricingEngine(bondEngine);

         // cached values
         int totCashflow = 24;
         int totNotionals = 13;
         double PVDifference = 13118862.59;
         double[] notionals = {400000000,367573428,334984723,302233075,269317669,236237685,202992302,169580691,136002023,
                               102255461,68340166,34255295,0 };

         // test total cashflow count 
         Assert.AreEqual(bond.cashflows().Count, totCashflow, "Cashflow size different");

         // test notional cashflow count
         Assert.AreEqual(bond.notionals().Count, totNotionals, "Notionals size different");

         // test notional amortization values
         for (int i = 0; i < totNotionals; i++)
         {
            Assert.AreEqual(bond.notionals()[i], notionals[i], 1, "Notionals " + i + "is different");
         }

         // test PV difference
         double cash = bond.CASH();
         Assert.AreEqual(cash - amount, PVDifference, 0.1, "PV Difference wrong");

      }

      
      [TestMethod()]
      public void testMBSFixedBondCached()
      {
         // Test MBS Bond against cached values
         // from Fabozzi MBS Products Structuring and Analytical Techniques
         // Second Edition - WILEY ISBN 978-1-118-00469-2
         // pag 58,61,63

         #region Cached Values
         double[] OutstandingBalance = {400000000,399396651,398724866,397984841,397176808,396301034,395357823,394347512,393270474,
                                        392127117,390917882,389643247,388303720,386899847,385432204,383901402,382308084,380652925,
                                        378936631,377159941,375323622,373428474,371475324,369465030,367398478,365276580,363100276,
                                        360870534,358588346,356317966,354059336,351812393,349577078,347353331,345141093,342940305,
                                        340750907,338572840,336406048,334250471,332106052,329972733,327850458,325739170,323638812,
                                        321549327,319470661,317402757,315345560,313299014,311263066,309237660,307222743,305218260,
                                        303224158,301240383,299266882,297303602,295350492,293407497,291474567,289551650,287638694,
                                        285735647,283842460,281959081,280085459,278221545,276367288,274522640,272687550,270861969,
                                        269045848,267239140,265441794,263653764,261875001,260105457,258345086,256593839,254851671,
                                        253118533,251394381,249679167,247972846,246275371,244586698,242906782,241235576,239573036,
                                        237919118,236273777,234636970,233008651,231388779,229777308,228174197,226579401,224992878,
                                        223414586,221844483,220282525,218728672,217182881,215645111,214115321,212593469,211079516,
                                        209573419,208075140,206584637,205101871,203626802,202159389,200699595,199247379,197802703,
                                        196365528,194935815,193513525,192098622,190691066,189290820,187897846,186512107,185133566,
                                        183762186,182397929,181040759,179690640,178347535,177011409,175682225,174359947,173044541,
                                        171735971,170434201,169139197,167850924,166569347,165294431,164026143,162764449,161509314,
                                        160260704,159018587,157782929,156553696,155330855,154114374,152904220,151700360,150502762,
                                        149311394,148126224,146947219,145774348,144607580,143446882,142292225,141143576,140000905,
                                        138864182,137733374,136608453,135489388,134376148,133268704,132167026,131071083,129980848,
                                        128896290,127817380,126744089,125676388,124614249,123557642,122506539,121460913,120420734,
                                        119385975,118356608,117332605,116313939,115300582,114292506,113289685,112292092,111299699,
                                        110312480,109330408,108353458,107381601,106414813,105453067,104496337,103544598,102597823,
                                        101655987,100719066,99787032 ,98859862 ,97937530 ,97020012 ,96107282 ,95199317 ,94296091 ,
                                        93397579 ,92503759 ,91614606 ,90730095 ,89850202 ,88974905 ,88104180 ,87238002 ,86376348 ,
                                        85519196 ,84666522 ,83818303 ,82974516 ,82135138 ,81300146 ,80469519 ,79643233 ,78821266 ,
                                        78003597 ,77190202 ,76381060 ,75576149 ,74775447 ,73978932 ,73186584 ,72398380 ,71614300 ,
                                        70834321 ,70058424 ,69286586 ,68518787 ,67755007 ,66995224 ,66239418 ,65487568 ,64739655 ,
                                        63995657 ,63255556 ,62519330 ,61786959 ,61058425 ,60333707 ,59612785 ,58895641 ,58182253 ,
                                        57472605 ,56766675 ,56064445 ,55365895 ,54671008 ,53979764 ,53292143 ,52608129 ,51927702 ,
                                        51250843 ,50577534 ,49907758 ,49241495 ,48578728 ,47919438 ,47263609 ,46611221 ,45962258 ,
                                        45316701 ,44674534 ,44035738 ,43400297 ,42768192 ,42139408 ,41513926 ,40891731 ,40272804 ,
                                        39657129 ,39044690 ,38435469 ,37829450 ,37226618 ,36626954 ,36030443 ,35437069 ,34846816 ,
                                        34259667 ,33675607 ,33094619 ,32516689 ,31941799 ,31369935 ,30801080 ,30235220 ,29672339 ,
                                        29112422 ,28555453 ,28001417 ,27450300 ,26902085 ,26356759 ,25814305 ,25274711 ,24737960 ,
                                        24204038 ,23672931 ,23144624 ,22619103 ,22096353 ,21576360 ,21059110 ,20544589 ,20032782 ,
                                        19523677 ,19017258 ,18513512 ,18012425 ,17513984 ,17018175 ,16524985 ,16034399 ,15546405 ,
                                        15060989 ,14578138 ,14097838 ,13620077 ,13144842 ,12672119 ,12201896 ,11734160 ,11268897 ,
                                        10806096 ,10345743 ,9887826  ,9432332  ,8979249  ,8528565  ,8080267  ,7634342  ,7190780  ,
                                        6749566  ,6310691  ,5874140  ,5439903  ,5007967  ,4578321  ,4150953  ,3725851  ,3303003  ,
                                        2882398  ,2464024  ,2047870  ,1633925  ,1222176  ,812614   ,405225   };
         double[] Prepayments = { 200350,266975,333463,399780,465892,531764,597362,662652,727600,792172,856336,920057,983303,
                                  1046041,1108239,1169864,1230887,1291274,1350996,1410023,1468325,1525872,1582637,1638590,1693706,1747956,
                                  1801315,1853758,1842021,1830345,1818729,1807174,1795678,1784241,1772864,1761546,1750286,1739085,1727941,
                                  1716855,1705827,1694856,1683941,1673083,1662281,1651536,1640845,1630210,1619631,1609106,1598635,1588219,
                                  1577856,1567548,1557292,1547090,1536941,1526844,1516799,1506807,1496866,1486977,1477139,1467352,1457616,
                                  1447930,1438294,1428708,1419172,1409686,1400248,1390859,1381519,1372228,1362985,1353789,1344641,1335541,
                                  1326488,1317481,1308522,1299608,1290741,1281920,1273145,1264415,1255731,1247091,1238497,1229947,1221441,
                                  1212979,1204562,1196187,1187857,1179569,1171325,1163123,1154964,1146847,1138773,1130740,1122749,1114799,
                                  1106891,1099023,1091197,1083411,1075665,1067960,1060295,1052669,1045083,1037537,1030029,1022561,1015131,
                                  1007740,1000388,993073,985797,978558,971357,964193,957067,949977,942924,935908,928929,921985,915078,
                                  908207,901371,894571,887806,881077,874382,867722,861097,854506,847950,841427,834939,828484,822063,
                                  815675,809320,802998,796710,790454,784230,778039,771880,765753,759658,753595,747563,741563,735594,
                                  729656,723749,717872,712026,706211,700426,694671,688946,683251,677585,671949,666342,660765,655216,
                                  649697,644206,638744,633310,627904,622527,617178,611856,606563,601297,596058,590847,585662,580505,
                                  575375,570271,565194,560144,555120,550122,545150,540204,535284,530390,525521,520677,515859,511066,
                                  506298,501555,496836,492142,487473,482828,478207,473611,469038,464490,459965,455463,450986,446531,
                                  442100,437692,433307,428945,424606,420289,415995,411724,407474,403247,399042,394860,390698,386559,
                                  382442,378345,374271,370217,366185,362174,358184,354215,350266,346339,342431,338545,334678,330832,
                                  327006,323200,319414,315648,311901,308174,304467,300779,297110,293461,289831,286220,282627,279054,
                                  275499,271963,268445,264946,261466,258003,254559,251133,247724,244334,240961,237606,234269,230949,
                                  227647,224362,221094,217844,214610,211394,208194,205012,201845,198696,195563,192447,189347,186263,
                                  183195,180144,177109,174089,171086,168098,165126,162170,159229,156304,153394,150500,147620,144756,
                                  141907,139073,136254,133450,130660,127885,125125,122380,119648,116932,114229,111541,108867,106207,
                                  103561,100930,98312,95707,93117,90540,87977,85428,82891,80369,77859,75363,72880,70410,67954,65510,
                                  63079,60661,58256,55863,53483,51116,48761,46419,44089,41772,39466,37173,34893,32624,30367,28122,
                                  25889,23668,21459,19261,17075,14901,12738,10587,8447,6318,4201,2095,0};
         double[] NetInterest = { 1833333,1830568,1827489,1824097,1820394,1816380,1812057,1807426,1802490,1797249,1791707,1785865,
                                  1779725,1773291,1766564,1759548,1752245,1744659,1736793,1728650,1720233,1711547,1702595,1693381,
                                  1683910,1674184,1664210,1653990,1643530,1633124,1622772,1612473,1602228,1592036,1581897,1571810,
                                  1561775,1551792,1541861,1531981,1522153,1512375,1502648,1492971,1483345,1473768,1464241,1454763,
                                  1445334,1435954,1426622,1417339,1408104,1398917,1389777,1380685,1371640,1362642,1353690,1344784,
                                  1335925,1327112,1318344,1309622,1300945,1292312,1283725,1275182,1266683,1258229,1249818,1241451,
                                  1233127,1224846,1216608,1208413,1200260,1192150,1184082,1176055,1168070,1160127,1152224,1144363,
                                  1136542,1128762,1121022,1113323,1105663,1098043,1090463,1082921,1075419,1067956,1060532,1053146,
                                  1045798,1038489,1031217,1023984,1016787,1009628,1002506,995422,988373,981362,974387,967448,960545,
                                  953678,946846,940050,933290,926564,919873,913217,906596,900009,893456,886937,880452,874001,867583,
                                  861198,854847,848529,842243,835991,829770,823582,817426,811302,805210,799150,793121,787123,781157,
                                  775221,769317,763443,757599,751786,746004,740251,734528,728835,723172,717538,711933,706358,700811,
                                  695293,689804,684344,678912,673508,668132,662785,657465,652173,646908,641671,636461,631278,626122,
                                  620993,615891,610815,605766,600742,595746,590775,585830,580910,576017,571149,566306,561488,556696,
                                  551928,547186,542468,537774,533106,528461,523841,519244,514672,510124,505599,501098,496620,492166,
                                  487735,483327,478942,474579,470240,465923,461629,457357,453108,448880,444675,440492,436330,432190,
                                  428072,423976,419900,415846,411813,407802,403811,399841,395892,391963,388055,384167,380300,376453,
                                  372626,368819,365031,361264,357516,353788,350080,346391,342721,339070,335439,331826,328232,324657,
                                  321101,317564,314044,310544,307061,303597,300151,296723,293313,289921,286547,283190,279851,276529,
                                  273225,269938,266669,263416,260181,256962,253760,250575,247407,244256,241121,238002,234900,231814,
                                  228744,225690,222653,219631,216625,213635,210660,207702,204758,201830,198918,196021,193139,190272,
                                  187420,184584,181762,178955,176163,173385,170622,167874,165140,162420,159715,157023,154347,151684,
                                  149035,146400,143779,141172,138578,135998,133432,130879,128340,125814,123301,120802,118316,115842,
                                  113382,110935,108501,106080,103671,101275,98892,96521,94163,91817,89484,87162,84854,82557,80272,
                                  78000,75740,73491,71254,69030,66816,64615,62425,60247,58081,55925,53782,51649,49528,47418,45319,
                                  43232,41155,39089,37035,34991,32958,30936,28924,26923,24933,22953,20984,19025,17077,15139,13211,
                                  11293,9386,7489,5602,3724,1857};
         double[] ScheduledPrincipal = { 402998,404810,406562,408253,409882,411447,412949,414386,415758,417063,418300,419470,420571,
                                         421602,422563,423454,424273,425020,425694,426296,426824,427278,427657,427962,428192,428347,
                                         428427,428430,428358,428286,428213,428141,428069,427997,427924,427852,427780,427708,427636,
                                         427564,427491,427419,427347,427275,427203,427131,427059,426987,426915,426843,426771,426699,
                                         426627,426555,426483,426411,426339,426267,426195,426123,426051,425979,425907,425835,425764,
                                         425692,425620,425548,425476,425405,425333,425261,425189,425118,425046,424974,424902,424831,
                                         424759,424687,424616,424544,424472,424401,424329,424258,424186,424114,424043,423971,423900,
                                         423828,423757,423685,423614,423542,423471,423399,423328,423256,423185,423114,423042,422971,
                                         422900,422828,422757,422685,422614,422543,422472,422400,422329,422258,422187,422115,422044,
                                         421973,421902,421830,421759,421688,421617,421546,421475,421404,421332,421261,421190,421119,
                                         421048,420977,420906,420835,420764,420693,420622,420551,420480,420409,420338,420267,420196,
                                         420126,420055,419984,419913,419842,419771,419700,419630,419559,419488,419417,419346,419276,
                                         419205,419134,419064,418993,418922,418851,418781,418710,418639,418569,418498,418428,418357,
                                         418286,418216,418145,418075,418004,417934,417863,417793,417722,417652,417581,417511,417440,
                                         417370,417299,417229,417159,417088,417018,416947,416877,416807,416736,416666,416596,416526,
                                         416455,416385,416315,416245,416174,416104,416034,415964,415893,415823,415753,415683,415613,
                                         415543,415473,415403,415332,415262,415192,415122,415052,414982,414912,414842,414772,414702,
                                         414632,414562,414492,414422,414352,414282,414213,414143,414073,414003,413933,413863,413793,
                                         413724,413654,413584,413514,413444,413375,413305,413235,413165,413096,413026,412956,412887,
                                         412817,412747,412678,412608,412538,412469,412399,412330,412260,412191,412121,412051,411982,
                                         411912,411843,411773,411704,411635,411565,411496,411426,411357,411287,411218,411149,411079,
                                         411010,410941,410871,410802,410733,410663,410594,410525,410455,410386,410317,410248,410178,
                                         410109,410040,409971,409902,409833,409763,409694,409625,409556,409487,409418,409349,409280,
                                         409211,409142,409073,409003,408934,408865,408796,408728,408659,408590,408521,408452,408383,
                                         408314,408245,408176,408107,408038,407970,407901,407832,407763,407694,407625,407557,407488,
                                         407419,407350,407282,407213,407144,407076,407007,406938,406870,406801,406732,406664,406595,
                                         406526,406458,406389,406321,406252,406184,406115,406047,405978,405910,405841,405773,405704,
                                         405636,405567,405499,405430,405362,405294,405225};
         #endregion

         Date startDate = new Date(1, 2, 2007);
         Settings.setEvaluationDate(startDate);

         Period bondLength = new Period(358, TimeUnit.Months);
         Period originalLenght = new Period(360, TimeUnit.Months);
         DayCounter dCounter = new Thirty360();
         Frequency payFrequency = Frequency.Monthly;
         double amount = 400000000;
         double WACrate = 0.06;
         double PassThroughRate = 0.055;
         PSACurve psa100 = new PSACurve(startDate);

         var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(startDate, new SimpleQuote(WACrate), new Thirty360()));

         // 400 Million Pass-Through with a 5.5% Pass-through Rate, a WAC of 6.0%, and a WAM of 358 Months,
         // Assuming 100% PSA
         MBSFixedRateBond bond = BondFactory.makeMBSFixedBond(startDate,
                                                               bondLength,
                                                               originalLenght,
                                                               dCounter,
                                                               payFrequency,
                                                               amount,
                                                               WACrate,
                                                               PassThroughRate,
                                                               psa100);

         IPricingEngine bondEngine = new DiscountingBondEngine(discountCurve);
         bond.setPricingEngine(bondEngine);

         // Calculate Monthly Expecting Cashflow
         List<CashFlow> cf = bond.expectedCashflows();

         // Outstanding Balance
         int i = 0;
         foreach (CashFlow c in cf)
         {
            if (c is QLNet.FixedRateCoupon)
            {
               FixedRateCoupon frc = c as FixedRateCoupon;
               Assert.AreEqual(OutstandingBalance[i], frc.nominal(), 1, "Outstanding Balance " + i++ + "is different");
            }
         }

         // Prepayments
         i = 0;
         foreach (CashFlow c in cf)
         {
            if (c is QLNet.VoluntaryPrepay)
            {
               Assert.AreEqual(Prepayments[i], c.amount(), 1, "Prepayments " + i++ + "is different");
            }
         }

         // Net Interest
         i = 0;
         foreach (CashFlow c in cf)
         {
            if (c is QLNet.FixedRateCoupon)
            {
               FixedRateCoupon frc = c as FixedRateCoupon;
               Assert.AreEqual(NetInterest[i], frc.amount(), 1, "Net Interest " + i++ + "is different");
            }
         }

         // Scheduled Principal
         i = 0;
         foreach (CashFlow c in cf)
         {
            if (c is QLNet.AmortizingPayment)
            {
               Assert.AreEqual(ScheduledPrincipal[i], c.amount(), 1, "Scheduled Principal " + i++ + "is different");
            }
         }

         // Monthly Yield
         Assert.AreEqual(0.00458333333333381, bond.MonthlyYield(), 0.000000001, "MonthlyYield is different");

         // Bond Equivalent Yield
         Assert.AreEqual(0.0556, bond.BondEquivalentYield(), 0.0001, " Bond Equivalent Yield is different");
      }

      [TestMethod()]
      public void testAmortizingBond1()
      {
         // Input Values
         double faceValue = 40000;
         double marketValue = 43412;
         double couponRate = 0.06;
         Date issueDate = new Date(1, Month.January, 2001);
         Date maturirtyDate = new Date(1, Month.January, 2005);
         Date tradeDate = new Date(1, Month.January, 2004);
         Frequency paymentFrequency = Frequency.Semiannual;
         DayCounter dc = new Thirty360(Thirty360.Thirty360Convention.USA);

         // Build Bond
         AmortizingBond bond = BondFactory.makeAmortizingBond(faceValue, marketValue, couponRate,
            issueDate, maturirtyDate, tradeDate, paymentFrequency, dc, AmortizingMethod.EffectiveInterestRate);

         // Amortizing Yield ( Effective Rate )
         double y1 = bond.Yield() ;
         Assert.AreEqual(-0.0236402, y1, 0.001, "Amortizing Yield is different");

         // Amortized Cost at Date
         double Amort1 = bond.AmortizationValue(new Date(31, Month.August, 2004));
         Assert.AreEqual(41126.01, Amort1, 100, "Amortized Cost at 08/31/2004 is different");

         double Amort2 = bond.AmortizationValue(new Date(30, Month.September, 2004));
         Assert.AreEqual(40842.83, Amort2, 100, "Amortized Cost at 09/30/2004 is different");


      }

      [TestMethod()]
      public void testAmortizingBond2()
      {
         // Par – 500,000
         // Cost – 471,444
         // Coupon Rate - .0520
         // Issue Date – March 15 1999
         // Maturity Date – May 15 2028
         // Trade Date – December 31 2007
         // Payment Frequency - Semi-Annual; May/Nov
         // Day Count Method - 30/360

         // Input Values
         double faceValue = 500000;
         double marketValue = 471444;
         double couponRate = 0.0520;
         Date issueDate = new Date(15, Month.March, 1999);
         Date maturirtyDate = new Date(15, Month.May, 2028);
         Date tradeDate = new Date(31, Month.December, 2007);
         Frequency paymentFrequency = Frequency.Semiannual;
         DayCounter dc = new Thirty360(Thirty360.Thirty360Convention.USA);

         // Build Bond
         AmortizingBond bond = BondFactory.makeAmortizingBond(faceValue, marketValue, couponRate,
            issueDate, maturirtyDate, tradeDate, paymentFrequency, dc, AmortizingMethod.EffectiveInterestRate);

         // Amortizing Yield ( Effective Rate )
         double y1 = bond.Yield();
         Assert.AreEqual(0.0575649, y1, 0.001, "Amortizing Yield is different");

         // Amortized Cost at Date
         double Amort1 = bond.AmortizationValue(new Date(30, Month.November, 2012));
         Assert.AreEqual(475698.12, Amort1, 100, "Amortized Cost at 11/30/2012 is different");

         double Amort2 = bond.AmortizationValue(new Date(30, Month.December, 2012));
         Assert.AreEqual(475779.55, Amort1, 100, "Amortized Cost at 12/30/2012 is different");


      }

   }
}
