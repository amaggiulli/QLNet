/*
 Copyright (C) 2008-2023 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using Xunit.Priority;
using QLNet;

namespace TestSuite;

[Collection("QLNet CI Tests")]
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class CallableBondsTests
{
   public class Globals
   {
      public Date today, settlement;
      public Calendar calendar;
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

         today = Settings.evaluationDate();
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

      if (Math.Abs(bond2.cleanPrice() - couponBond.cleanPrice()) > tolerance)
         QAssert.Fail("failed to reproduce fixed-rate bond price:\n"
                      + "    calculated: " + bond2.cleanPrice() + "\n"
                      + "    expected:   " + couponBond.cleanPrice());
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

   [Fact(Skip = "To be fixed, Callable bond NPV looks wrong")]
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

   [Fact,Priority(6)]
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


      var callabilities = new CallabilitySchedule{new (new Bond.Price(100.0, Bond.Price.Type.Clean),
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
}
