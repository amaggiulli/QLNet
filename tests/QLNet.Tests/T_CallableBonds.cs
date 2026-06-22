/*
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

using QLNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Priority;

namespace TestSuite;

[Collection("QLNet CI Tests")]
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class CallableBondsTests
{
   private readonly ITestOutputHelper testOutputHelper;
   private readonly double _tolerance = 1.0e-3;

   public CallableBondsTests(ITestOutputHelper testOutputHelper)
   {
      this.testOutputHelper = testOutputHelper;
   }

   public class Globals
   {
      public Date today, settlement;
      public QLNet.Calendar calendar;
      public DayCounter dayCounter;
      public BusinessDayConvention rollingConvention;

      public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();
      public RelinkableHandle<ShortRateModel> model = new RelinkableHandle<ShortRateModel>();

      //SavedSettings backup = new SavedSettings();

      public Globals()
      {
         calendar = new TARGET();
         dayCounter = new Actual365Fixed();
         rollingConvention = BusinessDayConvention.ModifiedFollowing;

         today = new Date(02, 11, 2023);
         Settings.setEvaluationDate(today);
         settlement = calendar.advance(today, 2, TimeUnit.Days);
      }

      public Date issueDate()
      {
         // ensure that we're in mid-coupon
         return calendar.adjust(today - new Period(100, TimeUnit.Days));
      }

      public Date maturityDate()
      {
         // ensure that we're in mid-coupon
         return calendar.advance(issueDate(), 10, TimeUnit.Years);
      }

      public List<Date> evenYears()
      {
         List<Date> dates = new List<Date>();
         for (int i = 2; i < 10; i += 2)
            dates.Add(calendar.advance(issueDate(), i, TimeUnit.Years));
         return dates;
      }

      public List<Date> oddYears()
      {
         List<Date> dates = new List<Date>();
         for (int i = 1; i < 10; i += 2)
            dates.Add(calendar.advance(issueDate(), i, TimeUnit.Years));
         return dates;
      }

      public YieldTermStructure makeFlatCurve(double r)
      {
         return new FlatForward(settlement, r, dayCounter);
      }

      public YieldTermStructure makeFlatCurve(Quote r)
      {
         return new FlatForward(settlement, r, dayCounter);
      }
   }

   [Fact,Priority(0)]
   public void testInterplay()
   {
      // Testing interplay of callability and puttability for callable bonds
      var vars = new Globals();

      vars.termStructure.linkTo(vars.makeFlatCurve(0.03));
      vars.model.linkTo(new HullWhite(vars.termStructure));

      var timeSteps = 240;

      IPricingEngine engine = new TreeCallableZeroCouponBondEngine(vars.model, timeSteps, vars.termStructure);

      /* case 1: an earlier out-of-the-money callability must prevent
               a later in-the-money puttability
      */

      var callabilities = new CallabilitySchedule
      {
         new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean), Callability.Type.Call,
            vars.calendar.advance(vars.issueDate(), 4, TimeUnit.Years)),
         new Callability(new Bond.Price(1000.0, Bond.Price.Type.Clean), Callability.Type.Put,
            vars.calendar.advance(vars.issueDate(), 6, TimeUnit.Years))
      };

      var bond = new CallableZeroCouponBond(3, 100.0, vars.calendar, vars.maturityDate(),
         new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention, 100.0, vars.issueDate(), callabilities);
      bond.setPricingEngine(engine);

      double expected = callabilities[0].price().amount() *
                        vars.termStructure.link.discount(callabilities[0].date()) /
                        vars.termStructure.link.discount(bond.settlementDate());

      if (Math.Abs(bond.settlementValue() - expected) > 1.0e-2)
         QAssert.Fail("callability not exercised correctly:\n"
                      + "    calculated NPV: " + bond.settlementValue() + "\n"
                      + "    expected:       " + expected + "\n"
                      + "    difference:     " + (bond.settlementValue() - expected));

      // case 2: same as case 1, with an added callability later on

      callabilities.Add(new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean),
         Callability.Type.Call, vars.calendar.advance(vars.issueDate(), 8, TimeUnit.Years)));

      bond = new CallableZeroCouponBond(3, 100.0, vars.calendar,
         vars.maturityDate(), new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention, 100.0,
         vars.issueDate(), callabilities);
      bond.setPricingEngine(engine);

      if (Math.Abs(bond.settlementValue() - expected) > 1.0e-2)
         QAssert.Fail("callability not exercised correctly:\n"
                      + "    calculated NPV: " + bond.settlementValue() + "\n"
                      + "    expected:       " + expected + "\n"
                      + "    difference:     " + (bond.settlementValue() - expected));

      // case 3: an earlier in-the-money puttability must prevent
      // a later in-the-money callability

      callabilities.Clear();

      callabilities.Add(new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean),
         Callability.Type.Put, vars.calendar.advance(vars.issueDate(), 4, TimeUnit.Years)));

      callabilities.Add(new Callability(new Bond.Price(10.0, Bond.Price.Type.Clean),
         Callability.Type.Call, vars.calendar.advance(vars.issueDate(), 6, TimeUnit.Years)));

      bond = new CallableZeroCouponBond(3, 100.0, vars.calendar,
         vars.maturityDate(), new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention, 100.0,
         vars.issueDate(), callabilities);
      bond.setPricingEngine(engine);

      expected = callabilities[0].price().amount() *
                 vars.termStructure.link.discount(callabilities[0].date()) /
                 vars.termStructure.link.discount(bond.settlementDate());

      if (Math.Abs(bond.settlementValue() - expected) > 1.0e-2)
         QAssert.Fail("puttability not exercised correctly:\n"
                      + "    calculated NPV: " + bond.settlementValue() + "\n"
                      + "    expected:       " + expected + "\n"
                      + "    difference:     " + (bond.settlementValue() - expected));

      // case 4: same as case 3, with an added puttability later on

      callabilities.Add(new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean),
         Callability.Type.Put, vars.calendar.advance(vars.issueDate(), 8, TimeUnit.Years)));

      bond = new CallableZeroCouponBond(3, 100.0, vars.calendar,
         vars.maturityDate(), new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention, 100.0,
         vars.issueDate(), callabilities);
      bond.setPricingEngine(engine);

      if (Math.Abs(bond.settlementValue() - expected) > 1.0e-2)
         QAssert.Fail("puttability not exercised correctly:\n"
                      + "    calculated NPV: " + bond.settlementValue() + "\n"
                      + "    expected:       " + expected + "\n"
                      + "    difference:     " + (bond.settlementValue() - expected));
   }

   [Fact,Priority(1)]
   public void testConsistency()
   {
      // Testing consistency of callable bonds
      var vars = new Globals();
      vars.termStructure.linkTo(vars.makeFlatCurve(0.032));
      vars.model.linkTo(new HullWhite(vars.termStructure));

      var schedule = new MakeSchedule()
         .from(vars.issueDate())
         .to(vars.maturityDate())
         .withCalendar(vars.calendar)
         .withFrequency(Frequency.Semiannual)
         .withConvention(vars.rollingConvention)
         .withRule(DateGeneration.Rule.Backward).value();

      var coupons = new InitializedList<double>(1, 0.05);

      var bond = new FixedRateBond(3, 100.0, schedule, coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis));
      bond.setPricingEngine(new DiscountingBondEngine(vars.termStructure));

      var callabilities = new CallabilitySchedule();
      var callabilityDates = vars.evenYears();
      callabilities.AddRange(callabilityDates.Select(callabilityDate =>
         new Callability(new Bond.Price(110.0, Bond.Price.Type.Clean), Callability.Type.Call, callabilityDate)));

      var puttabilities = new CallabilitySchedule();
      var puttabilityDates = vars.oddYears();
      puttabilities.AddRange(puttabilityDates.Select(puttabilityDate =>
         new Callability(new Bond.Price(90.0, Bond.Price.Type.Clean), Callability.Type.Put, puttabilityDate)));

      var timeSteps = 240;

      IPricingEngine engine = new TreeCallableFixedRateBondEngine(vars.model, timeSteps, vars.termStructure);

      var callable = new CallableFixedRateBond(3, 100.0, schedule,
         coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention,
         100.0, vars.issueDate(),
         callabilities);
      callable.setPricingEngine(engine);

      var puttable = new CallableFixedRateBond(3, 100.0, schedule,
         coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention,
         100.0, vars.issueDate(),
         puttabilities);
      puttable.setPricingEngine(engine);

      if (bond.cleanPrice() <= callable.cleanPrice())
         QAssert.Fail("inconsistent prices:\n"
                      + "    plain bond: " + bond.cleanPrice() + "\n"
                      + "    callable:   " + callable.cleanPrice() + "\n"
                      + " (should be lower)");

      if (bond.cleanPrice() >= puttable.cleanPrice())
         QAssert.Fail("inconsistent prices:\n"
                      + "    plain bond: " + bond.cleanPrice() + "\n"
                      + "    puttable:   " + puttable.cleanPrice() + "\n"
                      + " (should be higher)");
   }

   [Fact,Priority(2)]
   public void testObservability()
   {
      // Testing observability of callable bonds
      var vars = new Globals();
      var observable = new SimpleQuote(0.03);
      var h = new Handle<Quote>(observable);
      vars.termStructure.linkTo(vars.makeFlatCurve(h));
      vars.model.linkTo(new HullWhite(vars.termStructure));

      var callabilities = new CallabilitySchedule();

      var callabilityDates = vars.evenYears();
      callabilities.AddRange(callabilityDates.Select(callabilityDate =>
         new Callability(new Bond.Price(110.0, Bond.Price.Type.Clean), Callability.Type.Call, callabilityDate)));

      var puttabilityDates = vars.oddYears();
      callabilities.AddRange(puttabilityDates.Select(puttabilityDate =>
         new Callability(new Bond.Price(90.0, Bond.Price.Type.Clean), Callability.Type.Put, puttabilityDate)));

      var bond = new CallableZeroCouponBond(3, 100.0, vars.calendar,
         vars.maturityDate(), new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention, 100.0,
         vars.issueDate(), callabilities);

      var timeSteps = 240;

      IPricingEngine engine = new TreeCallableFixedRateBondEngine(vars.model, timeSteps, vars.termStructure);

      bond.setPricingEngine(engine);

      double originalValue = bond.NPV();

      observable.setValue(0.04);

      if (bond.NPV().IsEqual(originalValue))
         QAssert.Fail("callable coupon bond was not notified of observable change");
   }

   [Fact,Priority(3)]
   public void testDegenerate()
   {
      // Repricing bonds using degenerate callable bonds
      var vars = new Globals();
      vars.termStructure.linkTo(vars.makeFlatCurve(0.034));
      vars.model.linkTo(new HullWhite(vars.termStructure));

      var schedule = new MakeSchedule()
         .from(vars.issueDate())
         .to(vars.maturityDate())
         .withCalendar(vars.calendar)
         .withFrequency(Frequency.Semiannual)
         .withConvention(vars.rollingConvention)
         .withRule(DateGeneration.Rule.Backward).value();

      var coupons = new InitializedList<double>(1, 0.05);

      var zeroCouponBond = new ZeroCouponBond(3, vars.calendar, 100.0, vars.maturityDate(), vars.rollingConvention, 100, null);

      var couponBond = new FixedRateBond(3, 100.0, schedule, coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis));

      // no callability
      var callabilities = new CallabilitySchedule();

      var bond1 = new CallableZeroCouponBond(3, 100.0, vars.calendar,
         vars.maturityDate(), new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention, 100.0,
         vars.issueDate(), callabilities);

      var bond2 = new CallableFixedRateBond(3, 100.0, schedule,
         coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention,
         100.0, vars.issueDate(),
         callabilities);

      IPricingEngine discountingEngine = new DiscountingBondEngine(vars.termStructure);

      zeroCouponBond.setPricingEngine(discountingEngine);
      couponBond.setPricingEngine(discountingEngine);

      var timeSteps = 240;

      IPricingEngine treeEngine = new TreeCallableFixedRateBondEngine(vars.model, timeSteps, vars.termStructure);

      bond1.setPricingEngine(treeEngine);
      bond2.setPricingEngine(treeEngine);

      var tolerance = 1.0e-4;

      if (Math.Abs(bond1.cleanPrice() - zeroCouponBond.cleanPrice()) > tolerance)
         QAssert.Fail("failed to reproduce zero-coupon bond price:\n"
                      + "    calculated: " + bond1.cleanPrice() + "\n"
                      + "    expected:   " + zeroCouponBond.cleanPrice());

      if (Math.Abs(bond2.cleanPrice() - couponBond.cleanPrice()) > tolerance)
         QAssert.Fail("failed to reproduce fixed-rate bond price:\n"
                      + "    calculated: " + bond2.cleanPrice() + "\n"
                      + "    expected:   " + couponBond.cleanPrice());

      // out-of-the-money callability

      var callabilityDates = vars.evenYears();
      callabilities.AddRange(callabilityDates.Select(callabilityDate =>
         new Callability(new Bond.Price(10000.0, Bond.Price.Type.Clean), Callability.Type.Call, callabilityDate)));
      var puttabilityDates = vars.oddYears();
      callabilities.AddRange(puttabilityDates.Select(puttabilityDate =>
         new Callability(new Bond.Price(0.0, Bond.Price.Type.Clean), Callability.Type.Put, puttabilityDate)));

      bond1 = new CallableZeroCouponBond(3, 100.0, vars.calendar,
         vars.maturityDate(), new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention, 100.0,
         vars.issueDate(), callabilities);

      bond2 = new CallableFixedRateBond(3, 100.0, schedule,
         coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention,
         100.0, vars.issueDate(),
         callabilities);

      bond1.setPricingEngine(treeEngine);
      bond2.setPricingEngine(treeEngine);

      if (Math.Abs(bond1.cleanPrice() - zeroCouponBond.cleanPrice()) > tolerance)
         QAssert.Fail("failed to reproduce zero-coupon bond price:\n"
                      + "    calculated: " + bond1.cleanPrice() + "\n"
                      + "    expected:   " + zeroCouponBond.cleanPrice());
      var calculated = bond2.cleanPrice();
      var expected = couponBond.cleanPrice();
      if (Math.Abs(calculated - expected) > tolerance)
         QAssert.Fail("failed to reproduce fixed-rate bond price:\n"
                      + "    calculated: " + calculated + "\n"
                      + "    expected:   " + expected);
   }

   [Fact,Priority(4)]
   public void testCached()
   {
      // Testing callable-bond value against cached values
      var vars = new Globals();
      vars.today = new Date(3,Month.June,2004);
      Settings.setEvaluationDate(vars.today);
      vars.settlement = vars.calendar.advance(vars.today,3,TimeUnit.Days);

      vars.termStructure.linkTo(vars.makeFlatCurve(0.032));
      vars.model.linkTo(new HullWhite(vars.termStructure));

      Schedule schedule = new MakeSchedule()
         .from(vars.issueDate())
         .to(vars.maturityDate())
         .withCalendar(vars.calendar)
         .withFrequency(Frequency.Semiannual)
         .withConvention(vars.rollingConvention)
         .withRule(DateGeneration.Rule.Backward).value();

      var coupons = new InitializedList<double>(1, 0.05);

      var callabilities = new CallabilitySchedule();
      var puttabilities = new CallabilitySchedule();
      var allExercises = new CallabilitySchedule();

      var callabilityDates = vars.evenYears();
      foreach (var callabilityDate in callabilityDates)
      {
         var exercise = new Callability(new Bond.Price(110.0, Bond.Price.Type.Clean), Callability.Type.Call, callabilityDate);
         callabilities.Add(exercise);
         allExercises.Add(exercise);
      }
      var puttabilityDates = vars.oddYears();
      foreach (var puttabilityDate in puttabilityDates)
      {
         var exercise = new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean), Callability.Type.Put, puttabilityDate);
         puttabilities.Add(exercise);
         allExercises.Add(exercise);
      }

      var timeSteps = 240;

      IPricingEngine engine = new TreeCallableFixedRateBondEngine(vars.model, timeSteps, vars.termStructure);

      var tolerance = 1.0e-8;

      var storedPrice1 = 110.60975477;
      var bond1 = new CallableFixedRateBond(3, 10000.0, schedule,
         coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention,
         100.0, vars.issueDate(),
         callabilities);
      bond1.setPricingEngine(engine);

      if (Math.Abs(bond1.cleanPrice() - storedPrice1) > tolerance)
         QAssert.Fail("failed to reproduce cached callable-bond price:\n"
                      + "    calculated: " + bond1.cleanPrice() + "\n"
                      + "    expected:   " + storedPrice1);

      var storedPrice2 = 115.16559362;
      var bond2 = new CallableFixedRateBond(3, 10000.0, schedule,
         coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention,
         100.0, vars.issueDate(),
         puttabilities);
      bond2.setPricingEngine(engine);

      if (Math.Abs(bond2.cleanPrice() - storedPrice2) > tolerance)
         QAssert.Fail("failed to reproduce cached puttable-bond price:\n"
                      + "    calculated: " + bond2.cleanPrice() + "\n"
                      + "    expected:   " + storedPrice2);

      var storedPrice3 = 110.97509625;
      var bond3 = new CallableFixedRateBond(3, 10000.0, schedule,
         coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention,
         100.0, vars.issueDate(),
         allExercises);
      bond3.setPricingEngine(engine);

      if (Math.Abs(bond3.cleanPrice() - storedPrice3) > tolerance)
         QAssert.Fail("failed to reproduce cached callable/puttable-bond price:\n"
                      + "    calculated: " + bond3.cleanPrice() + "\n"
                      + "    expected:   " + storedPrice3);
   }

   //[Fact(Skip = "To be fixed, Callable bond NPV looks wrong")]
   [Fact]
   public void testSnappingExerciseDate2ClosestCouponDate()
   {
      // Testing snap of callability dates to the closest coupon date

      /* This is a test case inspired by
      * https://github.com/lballabio/QuantLib/issues/930#issuecomment-853886024 */

      var today = new Date(18, Month.May, 2021);

      Settings.setEvaluationDate(today);

      var calendar = new UnitedStates(UnitedStates.Market.FederalReserve);
      var accrualDc = new Thirty360(Thirty360.Thirty360Convention.USA);
      var frequency = Frequency.Semiannual;
      var termStructure = new RelinkableHandle<YieldTermStructure>();
      termStructure.linkTo(new FlatForward(today, 0.02, new Actual365Fixed()));

      void MakeBonds(Date callDate,out FixedRateBond fixedRateBond, out CallableFixedRateBond callableBond)
      {
         var settlementDays = 2;
         var settlementDate = new Date(20, Month.May, 2021);
         var coupon = 0.05;
         var faceAmount = 100.00;
         var redemption = faceAmount;
         var maturityDate = new Date(14, Month.Feb, 2026);
         var issueDate = settlementDate - 2 * new Period(366 , TimeUnit.Days);
         var schedule = new MakeSchedule()
            .from(issueDate)
            .to(maturityDate)
            .withFrequency(frequency)
            .withCalendar(calendar)
            .withConvention(BusinessDayConvention.Unadjusted)
            .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
            .backwards()
            .endOfMonth(false).value();
         var coupons = new InitializedList<double>(schedule.size() - 1, coupon);

         var callabilitySchedule = new CallabilitySchedule
         {
            new(new Bond.Price(faceAmount, Bond.Price.Type.Clean), Callability.Type.Call, callDate)
         };

         callableBond = new CallableFixedRateBond(settlementDays, faceAmount, schedule, coupons, accrualDc,
            BusinessDayConvention.Following, redemption, issueDate, callabilitySchedule);

         var model = new HullWhite(termStructure, 1e-12, 0.003);
         var treeEngine = new TreeCallableFixedRateBondEngine(model, 40);
         callableBond.setPricingEngine(treeEngine);

         var fixedRateBondSchedule = schedule.until(callDate);
         var fixedRateBondCoupons = new InitializedList<double>(schedule.size() - 1, coupon);

         fixedRateBond = new FixedRateBond(settlementDays, faceAmount, fixedRateBondSchedule, fixedRateBondCoupons, accrualDc,
            BusinessDayConvention.Following, redemption, issueDate);
         var discountingEngine = new DiscountingBondEngine(termStructure);
         fixedRateBond.setPricingEngine(discountingEngine);

      };

      var initialCallDate = new Date(16, Month.Feb, 2022);
      var tolerance = 1e-10;
      var prevOAS = 0.0266;
      var expectedOasStep = 0.00005;

      for (var i = -10; i < 11; i++)
      {
         var callDate = initialCallDate + new Period(i ,TimeUnit.Days);
         if (calendar.isBusinessDay(callDate))
         {
            MakeBonds(callDate, out var fixedRateBond, out var callableBond);
            var npvFixedRateBond = fixedRateBond.NPV();
            var npvCallable = callableBond.NPV();

            if (Math.Abs(npvCallable - npvFixedRateBond) > tolerance)
            {
               QAssert.Fail("failed to reproduce bond price at "
                            + callDate + ":\n"
                            + "    calculated: " + npvCallable + "\n"
                            + "    expected:   " + npvFixedRateBond + " +/- "
                            + tolerance);
            }

            var cleanPrice = callableBond.cleanPrice() - 2.0;
            var oas = callableBond.OAS(cleanPrice, termStructure, accrualDc, Compounding.Continuous, frequency);
            if (prevOAS - oas < expectedOasStep)
            {
               QAssert.Fail("failed to get expected change in OAS at "
                            + callDate + ":\n"
                            + "    calculated: " + oas + "\n"
                            + "      previous: " + prevOAS + "\n"
                            + "  should at least change by " + expectedOasStep);
            }
            prevOAS = oas;
         }
      }
   }

   [Fact,Priority(5)]
   public void testBlackEngine()
   {
      // Testing Black engine for European callable bonds
      var vars = new Globals
      {
         today = new Date(20, Month.September, 2022)
      };
      Settings.setEvaluationDate(vars.today);
      vars.settlement = vars.calendar.advance(vars.today, 3, TimeUnit.Days);

      vars.termStructure.linkTo(vars.makeFlatCurve(0.03));

      var callabilities = new CallabilitySchedule {
         new(new Bond.Price(100.0, Bond.Price.Type.Clean), Callability.Type.Call,
            vars.calendar.advance(vars.issueDate(),4,TimeUnit.Years))
      };

      var bond = new CallableZeroCouponBond(3, 10000.0, vars.calendar,
         vars.maturityDate(), new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention, 100.0,
         vars.issueDate(), callabilities);

      bond.setPricingEngine(new BlackCallableZeroCouponBondEngine(new Handle<Quote>(new SimpleQuote(0.3)), vars.termStructure));

      var expected = 74.52915084;
      var calculated = bond.cleanPrice();

      if (Math.Abs(calculated - expected) > 1.0e-4)
         QAssert.Fail("failed to reproduce cached price:\n"
                      + "    calculated NPV: " + calculated + "\n"
                      + "    expected:       " + expected + "\n"
                      + "    difference:     " + (calculated - expected));
   }

   [Fact,Priority(0)]
   public void testImpliedVol()
   {
      // Testing implied-volatility calculation for callable bonds
      var vars = new Globals();
      vars.termStructure.linkTo(vars.makeFlatCurve(0.03));

      var schedule = new MakeSchedule()
         .from(vars.issueDate())
         .to(vars.maturityDate())
         .withCalendar(vars.calendar)
         .withFrequency(Frequency.Semiannual)
         .withConvention(vars.rollingConvention)
         .withRule(DateGeneration.Rule.Backward).value();

      var coupons = new InitializedList<double>(1, 0.01);


      var callabilities = new CallabilitySchedule{new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean),
         Callability.Type.Call, schedule.at(8))};

      var bond = new CallableFixedRateBond(3, 10000.0, schedule,
         coupons, new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         vars.rollingConvention,
         100.0, vars.issueDate(),
         callabilities);

      var targetPrice = new Bond.Price(78.50,Bond.Price.Type.Dirty);
      var volatility = bond.impliedVolatility(targetPrice,
         vars.termStructure,
         1e-8,  // accuracy
         200,   // max evaluations
         1e-4,  // min vol
         1.0);  // max vol

      bond.setPricingEngine(new BlackCallableZeroCouponBondEngine(new Handle<Quote>(
         new SimpleQuote(volatility)), vars.termStructure));

      if (Math.Abs(bond.dirtyPrice() - targetPrice.amount()) > 1.0e-4)
         QAssert.Fail("failed to reproduce target dirty price with implied volatility:\n"
                      + "    calculated price: " + bond.dirtyPrice() + "\n"
                      + "    expected:         " + targetPrice.amount() + "\n"
                      + "    difference:       " + (bond.dirtyPrice() - targetPrice.amount()));

      targetPrice = new Bond.Price(78.50, Bond.Price.Type.Clean);
      volatility = bond.impliedVolatility(targetPrice,
         vars.termStructure,
         1e-8,  // accuracy
         200,   // max evaluations
         1e-4,  // min vol
         1.0);  // max vol

      bond.setPricingEngine(new BlackCallableZeroCouponBondEngine(new Handle<Quote>(
         new SimpleQuote(volatility)), vars.termStructure));

      if (Math.Abs(bond.cleanPrice() - targetPrice.amount()) > 1.0e-4)
         QAssert.Fail("failed to reproduce target clean price with implied volatility:\n"
                      + "    calculated price: " + bond.cleanPrice() + "\n"
                      + "    expected:         " + targetPrice.amount() + "\n"
                      + "    difference:       " + (bond.cleanPrice() - targetPrice.amount()));


#pragma warning disable CS0612
      var targetNPV = 7850.0;
      volatility = bond.impliedVolatility(targetNPV,
         vars.termStructure,
         1e-8,  // accuracy
         200,   // max evaluations
         1e-4,  // min vol
         1.0);  // max vol
#pragma warning restore CS0612

      bond.setPricingEngine(new BlackCallableZeroCouponBondEngine(new Handle<Quote>(
         new SimpleQuote(volatility)), vars.termStructure));

      if (Math.Abs(bond.NPV() - targetNPV) > 1.0e-4)
         QAssert.Fail("failed to reproduce target NPV with implied volatility:\n"
                      + "    calculated NPV: " + bond.NPV() + "\n"
                      + "    expected:       " + targetNPV + "\n"
                      + "    difference:     " + (bond.NPV() - targetNPV));
   }

   [Fact]
   public void testYieldToCallFixedRatesWithKnownValues()
   {
      var settlementDate = new Date(27, 06, 2022);
      Settings.setEvaluationDate(settlementDate);
      var accrualDate = new Date(29,01,2020);
      var maturityDate = new Date(01, 07, 2050);
      var firstCouponDate = new Date(01, 07, 2020);
      var calendar = new TARGET();
      var dc = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
      var price = 110.096;
      var accuracy = 1.0e-06;
      var expectedYtm = 0.043715026855468755;
      var expectedYtc1 = 0.03590721130371094;
      var expectedYtc2 = 0.034762252807617189;
      var expectedModDuration1 = 5.8830214161503545;
      var expectedModDuration2 = 6.2283054674626808;
      var frequency = Frequency.Semiannual;


      var sch = new Schedule( accrualDate, maturityDate, new Period(frequency),
         calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Forward,
         false, firstCouponDate);

      var callSchedule = new CallabilitySchedule
      {
         new Callability( new Bond.Price(101.7209999,Bond.Price.Type.Clean),Callability.Type.Call, new Date (31,07,2029)),
         new Callability( new Bond.Price(100,Bond.Price.Type.Clean),Callability.Type.Call, new Date (31,01,2030))
      };

      var callableBond = new CallableFixedRateBond(0, 1000, sch, [0.05],dc,
         BusinessDayConvention.Unadjusted, 100, accrualDate, callSchedule);

      var ytm = callableBond.yield(price, dc, Compounding.Compounded, Frequency.Semiannual, settlementDate, accuracy);

      var cc = callableBond.yieldToCalls(settlementDate, price, frequency, accuracy);

      QAssert.AreEqual(expectedYtm, ytm);
      QAssert.AreEqual(expectedYtc1, cc[0].CalcYield);
      QAssert.AreEqual(expectedYtc2, cc[1].CalcYield);
      QAssert.AreEqual(expectedModDuration1, cc[0].CalcModifiedDuration);
      QAssert.AreEqual(expectedModDuration2, cc[1].CalcModifiedDuration);

   }


   [Fact]
   public void testYieldToCallZeroCouponWithKnownValues()
   {
      var settlementDate = new Date(22, 09, 2017);
      Settings.setEvaluationDate(settlementDate);
      var maturityDate = new Date(01, 08, 2030);
      var calendar = new TARGET();
      var dc = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
      var price = 48.349;
      var accuracy = 1.0e-06;
      var expectedYtm = 0.057324401855468748;
      var expectedYtc = 0.01350558258056641;
      var expectedModDuration = 3.8324535742169465;

      var callSchedule = new CallabilitySchedule
      {
         new Callability( new Bond.Price(50.9262336,Bond.Price.Type.Clean),Callability.Type.Call, new Date (01,08,2021)),
      };

      var callableBond = new CallableZeroCouponBond(0, 1000, calendar, maturityDate,dc, BusinessDayConvention.Unadjusted , 100, null,
        callSchedule);

      var ytm = callableBond.yield(price, dc, Compounding.Compounded, Frequency.Semiannual, settlementDate, accuracy);

      var cc = callableBond.yieldToCalls(settlementDate, price, Frequency.Semiannual, accuracy);

      QAssert.AreEqual(expectedYtm, ytm);
      QAssert.AreEqual(expectedYtc, cc[0].CalcYield);
      QAssert.AreEqual(expectedModDuration, cc[0].CalcModifiedDuration);
   }

   [Fact]
   public void testPriceToCallZeroCouponWithKnownValues()
   {
      var settlementDate = new Date(28, 09, 2017);
      Settings.setEvaluationDate(settlementDate);
      var maturityDate = new Date(15, 02, 2028);
      var calendar = new TARGET();
      var dc = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
      var price = 0.02423;
      var expectedPrice = 71.963680745821122;

      var callSchedule = new CallabilitySchedule
      {
         new Callability( new Bond.Price(79.9710438,Bond.Price.Type.Clean),Callability.Type.Call, new Date (15,02,2022)),
      };

      var callableBond = new CallableZeroCouponBond(0, 1000, calendar, maturityDate,dc, BusinessDayConvention.Unadjusted , 100, null,
         callSchedule);

      var pp = callableBond.priceToCalls(settlementDate, price, Frequency.Semiannual);

      QAssert.AreEqual(expectedPrice, pp[0].CalcPrice);
   }

   [Theory]
   [InlineData(0, CallableBond.CouponType.ZeroCoupon, "06/01/2061", "09/08/2017", 3.7101424, "02/07/2006", 1.5307142, 7.7, "10/13/2017", 3, 8.2, null, 8.2, null)]
   [InlineData(5, CallableBond.CouponType.FixedRate, "07/01/2060", "07/01/2025", 100d, "09/09/2015", 103.2828783, 4.58, "10/13/2017", 111.7, 4.391, 3.272, 3.272, null)]
   [InlineData(5, CallableBond.CouponType.FixedRate, "10/01/2021", "10/01/2020", 100d, "04/21/2010", 114.91, 3.3, "09/07/2018", 106.709, 2.705, 1.683, 1.683, 21.666)]
   public void testYieldAt(double coupon, CallableBond.CouponType couponType, string MaturityDate, string NextCallDate, double? nextCallPrice,
                           string AccrualDate, double? originalPrice, decimal originalYield,
                           string SettlementDate, double price, double expectedYTM, double? expectedYTC,
                           double expectedYTW, double? expectedAccruedInterest)
   {
      var settlementDate = Convert.ToDateTime(SettlementDate, new CultureInfo("en-US"));
      Settings.setEvaluationDate(settlementDate);
      Date maturityDate = Convert.ToDateTime(MaturityDate, new CultureInfo("en-US"));
      Date accrualDate = Convert.ToDateTime(AccrualDate, new CultureInfo("en-US"));
      var nextCallDate = new Date();
      if (NextCallDate != "") nextCallDate = Convert.ToDateTime(NextCallDate, new CultureInfo("en-US"));
      var calendar = new TARGET();
      var dc = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
      var frequency = Frequency.Semiannual;
      var accuracy = 1.0e-06;
      CallableBond callableBond = null;

      var callSchedule = new CallabilitySchedule
      {
         new Callability( new Bond.Price(nextCallPrice.GetValueOrDefault(),Bond.Price.Type.Clean),Callability.Type.Call, nextCallDate),
      };

      if ( couponType == CallableBond.CouponType.FixedRate)
      {
         var sch = new Schedule( accrualDate, maturityDate, new Period(frequency), calendar, BusinessDayConvention.Unadjusted,
            BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false, null);

         callableBond = new CallableFixedRateBond(0, 1000, sch, [coupon/100], dc, BusinessDayConvention.Unadjusted,
            100, accrualDate, callSchedule);
      }
      else
      {
         callableBond = new CallableZeroCouponBond(0, 1000, calendar, maturityDate,dc, BusinessDayConvention.Unadjusted,
            100, null, callSchedule);
      }

      var yieldToMaturity = callableBond.yieldAt(settlementDate, price, frequency, accuracy) * 100;

      QAssert.IsTrue(Math.Abs(yieldToMaturity - expectedYTM) <= _tolerance,
         $"testYieldAt: YTM calculation failed, expected: {expectedYTM}, calculated: {yieldToMaturity}");

      var yieldToCall = callableBond.yieldAt(settlementDate, price, frequency, accuracy, nextCallDate, nextCallPrice) * 100;

      QAssert.IsTrue(Math.Abs(yieldToCall - expectedYTC.GetValueOrDefault()) <= _tolerance,
         $"testYieldAt: YTC calculation failed, expected: {expectedYTC}, calculated: {yieldToCall}");

   }

   [Fact(Skip = "Manual reporting test — run locally to inspect Taylor approximation accuracy table")]
   public void testYieldApproximationAccuracyByPriceDelta()
   {
      // Sweep over increasing price moves and print how the Taylor approximation error grows,
      // so desk traders can decide when basisPointValue is good enough vs running the full solver.

      var vars = new Globals();
      vars.termStructure.linkTo(vars.makeFlatCurve(0.05));
      vars.model.linkTo(new HullWhite(vars.termStructure));

      var dc = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
      var compounding = Compounding.Compounded;
      var freq = Frequency.Semiannual;

      var schedule = new MakeSchedule()
         .from(vars.issueDate())
         .to(vars.maturityDate())
         .withCalendar(vars.calendar)
         .withFrequency(freq)
         .withConvention(vars.rollingConvention)
         .withRule(DateGeneration.Rule.Backward).value();

      var coupons = new InitializedList<double>(1, 0.05);
      var callabilities = new CallabilitySchedule();
      callabilities.AddRange(vars.evenYears().Select(d =>
         new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean), Callability.Type.Call, d)));

      var bond = new CallableFixedRateBond(3, 100.0, schedule, coupons, dc,
         vars.rollingConvention, 100.0, vars.issueDate(), callabilities);
      var engine = new TreeCallableFixedRateBondEngine(vars.model, 240, vars.termStructure);
      bond.setPricingEngine(engine);

      var price0  = bond.cleanPrice();
      var yield0  = bond.yield(price0, dc, compounding, freq, vars.settlement);
      var bpv     = BondFunctions.basisPointValue(bond, yield0, dc, compounding, freq, vars.settlement);
      var modDur  = BondFunctions.duration(bond, yield0, dc, compounding, freq, Duration.Type.Modified, vars.settlement);
      var conv    = BondFunctions.convexity(bond, yield0, dc, compounding, freq, vars.settlement);

      // Price deltas to sweep: 1, 5, 10, 25, 50, 100, 200, 500, 1000 bps of price
      double[] deltaBps = { 1, 5, 10, 25, 50, 100, 200, 500, 1000 };

      testOutputHelper.WriteLine("");
      testOutputHelper.WriteLine("=== Taylor Approximation Accuracy — Callable 5% 10Y Bond ===");
      testOutputHelper.WriteLine($"  Base price  : {price0:F4}");
      testOutputHelper.WriteLine($"  Base yield  : {yield0 * 100:F4}%");
      testOutputHelper.WriteLine($"  Mod. Dur.   : {modDur:F4}");
      testOutputHelper.WriteLine($"  Convexity   : {conv:F4}");
      testOutputHelper.WriteLine($"  BPV (1bp)   : {bpv:F6}");
      testOutputHelper.WriteLine("");
      testOutputHelper.WriteLine($"  {"ΔPrice(bps)",12}  {"ΔPrice",8}  {"New Price",10}  {"Taylor Yield%",14}  {"Exact Yield%",13}  {"Error(bps)",11}  {"Usable?",8}");
      testOutputHelper.WriteLine($"  {new string('-', 87)}");

      foreach (var dBps in deltaBps)
      {
         foreach (var sign in new[] { +1.0, -1.0 })
         {
            var deltaPrice = sign * dBps * 0.01;
            var price2     = price0 + deltaPrice;

            var yieldApprox = yield0 + (deltaPrice / bpv) * 0.0001;
            var yieldExact  = bond.yield(price2, dc, compounding, freq, vars.settlement);
            var errorBps    = (yieldApprox - yieldExact) * 10000.0;
            var usable      = Math.Abs(errorBps) < 0.5 ? "✓ YES" : (Math.Abs(errorBps) < 2.0 ? "~ MARGINAL" : "✗ NO");

            testOutputHelper.WriteLine($"  {sign * dBps,+12:+0;-0}  {deltaPrice,+8:+0.00;-0.00}  {price2,10:F4}  {yieldApprox * 100,14:F6}  {yieldExact * 100,13:F6}  {errorBps,+11:+0.0000;-0.0000}  {usable,8}");
         }
      }
      testOutputHelper.WriteLine("");
      testOutputHelper.WriteLine("  Rule of thumb: Taylor (BPV) is reliable within ±50bps price move.");
      testOutputHelper.WriteLine("  Beyond that, run bond.yield() iterative solver.");
   }

   [Fact]
   public void testYieldApproximationVsDurationConvexity()
   {
      // Build a callable fixed-rate bond, price it with a Hull-White tree engine,
      // then verify that the BPV (2nd-order Taylor via BondFunctions.basisPointValue) yield
      // approximation for a +10bps price move is within 0.5bps of the iterative bond.yield() result.
      // Modified Duration and Convexity are also computed for reporting purposes only;
      // they are NOT needed for the approximation because BPV already encapsulates both.

      var vars = new Globals();
      vars.termStructure.linkTo(vars.makeFlatCurve(0.05));
      vars.model.linkTo(new HullWhite(vars.termStructure));

      var dc = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
      var compounding = Compounding.Compounded;
      var freq = Frequency.Semiannual;

      // 10Y semiannual schedule with 5% coupon
      var schedule = new MakeSchedule()
         .from(vars.issueDate())
         .to(vars.maturityDate())
         .withCalendar(vars.calendar)
         .withFrequency(freq)
         .withConvention(vars.rollingConvention)
         .withRule(DateGeneration.Rule.Backward).value();

      var coupons = new InitializedList<double>(1, 0.05);

      // Call schedule: callable at par every even year
      var callabilities = new CallabilitySchedule();
      callabilities.AddRange(vars.evenYears().Select(d =>
         new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean), Callability.Type.Call, d)));

      var bond = new CallableFixedRateBond(3, 100.0, schedule, coupons, dc,
         vars.rollingConvention, 100.0, vars.issueDate(), callabilities);

      var engine = new TreeCallableFixedRateBondEngine(vars.model, 240, vars.termStructure);
      bond.setPricingEngine(engine);

      // Step 1: engine price and exact yield via iterative solver
      var price1 = bond.cleanPrice();
      var yield1 = bond.yield(price1, dc, compounding, freq, vars.settlement);

      // Step 2: Modified Duration and Convexity at yield1 — for reporting only, not used in the approximation
      var modDur = BondFunctions.duration(bond, yield1, dc, compounding, freq,
         Duration.Type.Modified, vars.settlement);
      var convexity = BondFunctions.convexity(bond, yield1, dc, compounding, freq, vars.settlement);

      // Step 3: +10bps price move (10 bps of price = +0.10 on a 100-par bond)
      var deltaPrice = 0.10;
      var price2 = price1 + deltaPrice;

      // Step 4: BPV-based Taylor approximation using BondFunctions.basisPointValue
      //   bpv = price change for +1bp yield move (2nd-order Taylor encapsulating ModDur+Convexity)
      //   Inverting: Δyield = (ΔPrice / bpv) × 0.0001
      var bpv = BondFunctions.basisPointValue(bond, yield1, dc, compounding, freq, vars.settlement);
      var yieldApprox = yield1 + (deltaPrice / bpv) * 0.0001;

      // Step 5: exact yield at price2 via iterative solver (reference)
      var yieldExact = bond.yield(price2, dc, compounding, freq, vars.settlement);

      // The approximation error must be below 0.5 bps
      var errorBps = Math.Abs(yieldApprox - yieldExact) * 10000.0;

      QAssert.IsTrue(errorBps < 0.5,
         $"BPV approximation error {errorBps:F4} bps exceeds 0.5 bps tolerance\n" +
         $"    price1        : {price1:F6}\n" +
         $"    yield1        : {yield1 * 100.0:F6}%\n" +
         $"    modifiedDur   : {modDur:F6}\n" +
         $"    convexity     : {convexity:F6}\n" +
         $"    bpv           : {bpv:F6}\n" +
         $"    price2        : {price2:F6}  (+{deltaPrice * 100.0:F0} bps)\n" +
         $"    yieldApprox   : {yieldApprox * 100.0:F6}%\n" +
         $"    yieldExact    : {yieldExact * 100.0:F6}%\n" +
         $"    error         : {errorBps:F4} bps");
   }

   [Fact]
   public void testSinkableCallableMatchesReferenceDurations()
   {
      var settlementDate = new DateTime(2026, 6, 11);
      var maturityDate = new DateTime(2052, 7, 1);
      var firstCallDate = new DateTime(2036, 7, 1);
      var faceAmount = 39755000.0;
      const double baseOas = 0.0;
      const double referenceCleanPrice = 104.582;
      const double referenceEffectiveDuration = 7.91;
      const double effectiveDurationTolerance = 0.15;
      const double referenceOasDuration = 13.94;
      const double oasDurationTolerance = 0.02;

      Settings.setEvaluationDate(settlementDate);

      var curveDayCounter = new ActualActual(ActualActual.Convention.Bond);
      var curve = buildReferenceCurve(settlementDate, curveDayCounter);
      var termStructure = new Handle<YieldTermStructure>(curve);
      var model = new HullWhite(termStructure, 0.03, 1.0e-12);

      var scheduleStart = maturityDate;
      while (scheduleStart > settlementDate)
         scheduleStart = scheduleStart.AddMonths(-6);

      var schedule = new Schedule(
         scheduleStart,
         maturityDate,
         new Period(Frequency.Semiannual),
         new TARGET(),
         BusinessDayConvention.Unadjusted,
         BusinessDayConvention.Unadjusted,
         DateGeneration.Rule.Backward,
         false);

      var notionals = buildSinkableNotionals(schedule, faceAmount);
      var callSchedule = new CallabilitySchedule();
      for (var callDate = firstCallDate; callDate <= maturityDate; callDate = callDate.AddMonths(6))
      {
         callSchedule.Add(new Callability(new Bond.Price(100.0, Bond.Price.Type.Clean), Callability.Type.Call, callDate));
      }

      var bond = new CallableFixedRateBond(
         0,
         faceAmount,
         schedule,
         [0.05],
         notionals,
         new Thirty360(Thirty360.Thirty360Convention.BondBasis),
         BusinessDayConvention.Unadjusted,
         100.0,
         new Date(),
         callSchedule);

      bond.setPricingEngine(new TreeCallableFixedRateBondEngine(model, 240, termStructure));

      var effectiveDuration = bond.effectiveDuration(baseOas, termStructure, curveDayCounter, Compounding.Compounded, Frequency.Semiannual);
      var oas = bond.OAS(referenceCleanPrice, termStructure, curveDayCounter, Compounding.Compounded, Frequency.Semiannual, settlementDate);
      var oasDuration = bond.effectiveDuration(oas, termStructure, curveDayCounter, Compounding.Compounded, Frequency.Semiannual);

      if (Math.Abs(effectiveDuration - referenceEffectiveDuration) > effectiveDurationTolerance)
         QAssert.Fail("failed to reproduce sinkable callable effective duration:\n"
                      + "    calculated: " + effectiveDuration + "\n"
                      + "    expected:   " + referenceEffectiveDuration + " +/- " + effectiveDurationTolerance);

      if (Math.Abs(oasDuration - referenceOasDuration) > oasDurationTolerance)
         QAssert.Fail("failed to reproduce sinkable callable OAS duration:\n"
                      + "    calculated: " + oasDuration + "\n"
                      + "    expected:   " + referenceOasDuration + " +/- " + oasDurationTolerance);
   }

   private static InterpolatedZeroCurve<Linear> buildReferenceCurve(DateTime settlementDate, DayCounter dayCounter)
   {
      var dates = new List<Date>
      {
         new Date(settlementDate),
         new Date(settlementDate.AddMonths(3)),
         new Date(settlementDate.AddMonths(6)),
      };

      var rates = new List<double>
      {
         0.0253,
         0.0253,
         0.0239,
      };

      var parCurveRates = new[]
      {
         2.38, 2.40, 2.42, 2.50, 2.56, 2.64, 2.72, 2.77, 2.86, 2.95,
         3.04, 3.12, 3.19, 3.22, 3.26, 3.36, 3.46, 3.59, 3.73, 3.85,
         3.93, 4.00, 4.05, 4.11, 4.13, 4.16, 4.17, 4.20, 4.21, 4.23
      };

      for (var year = 1; year <= parCurveRates.Length; year++)
      {
         dates.Add(new Date(settlementDate.AddYears(year)));
         rates.Add(parCurveRates[year - 1] / 100.0);
      }

      return new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter);
   }

   private static List<double> buildSinkableNotionals(Schedule schedule, double faceAmount)
   {
      var sinkTerms = new List<(DateTime SinkDate, double Amount)>
      {
         (new DateTime(2050, 7, 1), 11905000.0),
         (new DateTime(2051, 7, 1), 12505000.0),
         (new DateTime(2052, 7, 1), 14145000.0),
      };

      var notionals = new List<double>(schedule.Count - 1);
      for (var periodIndex = 0; periodIndex < schedule.Count - 1; periodIndex++)
      {
         var periodEndDate = DateTime.SpecifyKind(schedule[periodIndex + 1], DateTimeKind.Utc);
         var outstandingPrincipal = sinkTerms
            .Where(sinkTerm => sinkTerm.SinkDate >= periodEndDate)
            .Sum(sinkTerm => sinkTerm.Amount);

         notionals.Add(faceAmount * outstandingPrincipal / faceAmount);
      }

      return notionals;
   }
}
