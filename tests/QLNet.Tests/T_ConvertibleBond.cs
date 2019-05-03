//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
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
   public class T_ConvertibleBond
   {
      private class CommonVars
      {
         // global data
         public Date today, issueDate, maturityDate;
         public Calendar calendar;
         public DayCounter dayCounter;
         public Frequency frequency;
         public int settlementDays;

         public RelinkableHandle<Quote> underlying = new RelinkableHandle<Quote>();
         public RelinkableHandle<YieldTermStructure> dividendYield = new RelinkableHandle<YieldTermStructure>(),
         riskFreeRate = new RelinkableHandle<YieldTermStructure>();
         public RelinkableHandle<BlackVolTermStructure> volatility = new RelinkableHandle<BlackVolTermStructure>();
         public BlackScholesMertonProcess process;

         public RelinkableHandle<Quote> creditSpread = new RelinkableHandle<Quote>();

         public CallabilitySchedule no_callability = new CallabilitySchedule();
         public DividendSchedule no_dividends = new DividendSchedule();

         public double faceAmount, redemption, conversionRatio;


         // setup
         public CommonVars()
         {
            calendar = new TARGET();

            today = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(today);

            dayCounter = new Actual360();
            frequency = Frequency.Annual;
            settlementDays = 3;

            issueDate = calendar.advance(today, 2, TimeUnit.Days);
            maturityDate = calendar.advance(issueDate, 10, TimeUnit.Years);
            // reset to avoid inconsistencies as the schedule is backwards
            issueDate = calendar.advance(maturityDate, -10, TimeUnit.Years);

            underlying.linkTo(new SimpleQuote(50.0));
            dividendYield.linkTo(Utilities.flatRate(today, 0.02, dayCounter));
            riskFreeRate.linkTo(Utilities.flatRate(today, 0.05, dayCounter));
            volatility.linkTo(Utilities.flatVol(today, 0.15, dayCounter));

            process = new BlackScholesMertonProcess(underlying, dividendYield, riskFreeRate, volatility);

            creditSpread.linkTo(new SimpleQuote(0.005));

            // it fails with 1000000
            // faceAmount = 1000000.0;
            faceAmount = 100.0;
            redemption = 100.0;
            conversionRatio = redemption / underlying.link.value();
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testBond()
      {

         /* when deeply out-of-the-money, the value of the convertible bond
            should equal that of the underlying plain-vanilla bond. */

         // Testing out-of-the-money convertible bonds against vanilla bonds

         CommonVars vars = new CommonVars();

         vars.conversionRatio = 1.0e-16;

         Exercise euExercise = new EuropeanExercise(vars.maturityDate);
         Exercise amExercise = new AmericanExercise(vars.issueDate, vars.maturityDate);

         int timeSteps = 1001;
         IPricingEngine engine = new BinomialConvertibleEngine<CoxRossRubinstein>(vars.process, timeSteps);

         Handle<YieldTermStructure> discountCurve = new Handle<YieldTermStructure>(new ForwardSpreadedTermStructure(vars.riskFreeRate, vars.creditSpread));

         // zero-coupon

         Schedule schedule = new MakeSchedule().from(vars.issueDate)
         .to(vars.maturityDate)
         .withFrequency(Frequency.Once)
         .withCalendar(vars.calendar)
         .backwards().value();

         ConvertibleZeroCouponBond euZero = new ConvertibleZeroCouponBond(euExercise, vars.conversionRatio,
                                                                          vars.no_dividends, vars.no_callability,
                                                                          vars.creditSpread,
                                                                          vars.issueDate, vars.settlementDays,
                                                                          vars.dayCounter, schedule,
                                                                          vars.redemption);
         euZero.setPricingEngine(engine);

         ConvertibleZeroCouponBond amZero = new ConvertibleZeroCouponBond(amExercise, vars.conversionRatio,
                                                                          vars.no_dividends, vars.no_callability,
                                                                          vars.creditSpread,
                                                                          vars.issueDate, vars.settlementDays,
                                                                          vars.dayCounter, schedule,
                                                                          vars.redemption);
         amZero.setPricingEngine(engine);

         ZeroCouponBond zero = new ZeroCouponBond(vars.settlementDays, vars.calendar,
                                                  100.0, vars.maturityDate,
                                                  BusinessDayConvention.Following, vars.redemption, vars.issueDate);

         IPricingEngine bondEngine = new DiscountingBondEngine(discountCurve);
         zero.setPricingEngine(bondEngine);

         double tolerance = 1.0e-2 * (vars.faceAmount / 100.0);

         double error = Math.Abs(euZero.NPV() - zero.settlementValue());
         if (error > tolerance)
         {
            QAssert.Fail("failed to reproduce zero-coupon bond price:"
                         + "\n    calculated: " + euZero.NPV()
                         + "\n    expected:   " + zero.settlementValue()
                         + "\n    error:      " + error);
         }

         error = Math.Abs(amZero.NPV() - zero.settlementValue());
         if (error > tolerance)
         {
            QAssert.Fail("failed to reproduce zero-coupon bond price:"
                         + "\n    calculated: " + amZero.NPV()
                         + "\n    expected:   " + zero.settlementValue()
                         + "\n    error:      " + error);
         }

         // coupon

         List<double> coupons = new InitializedList<double>(1, 0.05);

         schedule = new MakeSchedule().from(vars.issueDate)
         .to(vars.maturityDate)
         .withFrequency(vars.frequency)
         .withCalendar(vars.calendar)
         .backwards().value();

         ConvertibleFixedCouponBond euFixed = new ConvertibleFixedCouponBond(euExercise, vars.conversionRatio,
                                                                             vars.no_dividends, vars.no_callability,
                                                                             vars.creditSpread,
                                                                             vars.issueDate, vars.settlementDays,
                                                                             coupons, vars.dayCounter,
                                                                             schedule, vars.redemption);
         euFixed.setPricingEngine(engine);

         ConvertibleFixedCouponBond amFixed = new ConvertibleFixedCouponBond(amExercise, vars.conversionRatio,
                                                                             vars.no_dividends, vars.no_callability,
                                                                             vars.creditSpread,
                                                                             vars.issueDate, vars.settlementDays,
                                                                             coupons, vars.dayCounter,
                                                                             schedule, vars.redemption);
         amFixed.setPricingEngine(engine);

         FixedRateBond fixedBond = new FixedRateBond(vars.settlementDays, vars.faceAmount, schedule,
                                                     coupons, vars.dayCounter, BusinessDayConvention.Following,
                                                     vars.redemption, vars.issueDate);

         fixedBond.setPricingEngine(bondEngine);

         tolerance = 2.0e-2 * (vars.faceAmount / 100.0);

         error = Math.Abs(euFixed.NPV() - fixedBond.settlementValue());
         if (error > tolerance)
         {
            QAssert.Fail("failed to reproduce fixed-coupon bond price:"
                         + "\n    calculated: " + euFixed.NPV()
                         + "\n    expected:   " + fixedBond.settlementValue()
                         + "\n    error:      " + error);
         }

         error = Math.Abs(amFixed.NPV() - fixedBond.settlementValue());
         if (error > tolerance)
         {
            QAssert.Fail("failed to reproduce fixed-coupon bond price:"
                         + "\n    calculated: " + amFixed.NPV()
                         + "\n    expected:   " + fixedBond.settlementValue()
                         + "\n    error:      " + error);
         }

         // floating-rate

         IborIndex index = new Euribor1Y(discountCurve);
         int fixingDays = 2;
         List<double> gearings = new InitializedList<double>(1, 1.0);
         List<double> spreads = new List<double>();

         ConvertibleFloatingRateBond euFloating = new ConvertibleFloatingRateBond(euExercise, vars.conversionRatio,
                                                                                  vars.no_dividends, vars.no_callability,
                                                                                  vars.creditSpread,
                                                                                  vars.issueDate, vars.settlementDays,
                                                                                  index, fixingDays, spreads,
                                                                                  vars.dayCounter, schedule,
                                                                                  vars.redemption);
         euFloating.setPricingEngine(engine);

         ConvertibleFloatingRateBond amFloating = new ConvertibleFloatingRateBond(amExercise, vars.conversionRatio,
                                                                                  vars.no_dividends, vars.no_callability,
                                                                                  vars.creditSpread,
                                                                                  vars.issueDate, vars.settlementDays,
                                                                                  index, fixingDays, spreads,
                                                                                  vars.dayCounter, schedule,
                                                                                  vars.redemption);
         amFloating.setPricingEngine(engine);

         IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

         Schedule floatSchedule = new Schedule(vars.issueDate, vars.maturityDate,
                                               new Period(vars.frequency),
                                               vars.calendar, BusinessDayConvention.Following, BusinessDayConvention.Following,
                                               DateGeneration.Rule.Backward, false);

         FloatingRateBond floating = new FloatingRateBond(vars.settlementDays, vars.faceAmount, floatSchedule,
                                                          index, vars.dayCounter, BusinessDayConvention.Following, fixingDays,
                                                          gearings, spreads,
                                                          new List < double? >(), new List < double? >(),
                                                          false,
                                                          vars.redemption, vars.issueDate);

         floating.setPricingEngine(bondEngine);
         Utils.setCouponPricer(floating.cashflows(), pricer);

         tolerance = 2.0e-2 * (vars.faceAmount / 100.0);

         error = Math.Abs(euFloating.NPV() - floating.settlementValue());
         if (error > tolerance)
         {
            QAssert.Fail("failed to reproduce floating-rate bond price:"
                         + "\n    calculated: " + euFloating.NPV()
                         + "\n    expected:   " + floating.settlementValue()
                         + "\n    error:      " + error);
         }

         error = Math.Abs(amFloating.NPV() - floating.settlementValue());
         if (error > tolerance)
         {
            QAssert.Fail("failed to reproduce floating-rate bond price:"
                         + "\n    calculated: " + amFloating.NPV()
                         + "\n    expected:   " + floating.settlementValue()
                         + "\n    error:      " + error);
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testOption()
      {

         /* a zero-coupon convertible bond with no credit spread is
          equivalent to a call option. */

         // Testing zero-coupon convertible bonds against vanilla option

         CommonVars vars = new CommonVars();

         Exercise euExercise = new EuropeanExercise(vars.maturityDate);

         vars.settlementDays = 0;

         int timeSteps = 2001;
         IPricingEngine engine = new BinomialConvertibleEngine<CoxRossRubinstein>(vars.process, timeSteps);
         IPricingEngine vanillaEngine = new BinomialVanillaEngine<CoxRossRubinstein>(vars.process, timeSteps);

         vars.creditSpread.linkTo(new SimpleQuote(0.0));

         double conversionStrike = vars.redemption / vars.conversionRatio;
         StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, conversionStrike);

         Schedule schedule = new MakeSchedule().from(vars.issueDate)
         .to(vars.maturityDate)
         .withFrequency(Frequency.Once)
         .withCalendar(vars.calendar)
         .backwards().value();

         ConvertibleZeroCouponBond euZero = new ConvertibleZeroCouponBond(euExercise, vars.conversionRatio,
                                                                          vars.no_dividends, vars.no_callability,
                                                                          vars.creditSpread,
                                                                          vars.issueDate, vars.settlementDays,
                                                                          vars.dayCounter, schedule,
                                                                          vars.redemption);
         euZero.setPricingEngine(engine);

         VanillaOption euOption = new VanillaOption(payoff, euExercise);
         euOption.setPricingEngine(vanillaEngine);

         double tolerance = 5.0e-2 * (vars.faceAmount / 100.0);

         double expected = vars.faceAmount / 100.0 *
                           (vars.redemption * vars.riskFreeRate.link.discount(vars.maturityDate)
                            + vars.conversionRatio * euOption.NPV());
         double error = Math.Abs(euZero.NPV() - expected);
         if (error > tolerance)
         {
            QAssert.Fail("failed to reproduce plain-option price:"
                         + "\n    calculated: " + euZero.NPV()
                         + "\n    expected:   " + expected
                         + "\n    error:      " + error
                         + "\n    tolerance:      " + tolerance);
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testRegression()
      {

         // Testing fixed-coupon convertible bond in known regression case

         Date today = new Date(23, Month.December, 2008);
         Date tomorrow = today + 1;

         Settings.setEvaluationDate(tomorrow);

         Handle<Quote> u = new Handle<Quote>(new SimpleQuote(2.9084382818797443));

         List<Date> dates = new InitializedList<Date>(25);
         List<double> forwards = new InitializedList<double>(25);
         dates[0]  = new Date(29, Month.December, 2008);  forwards[0]  = 0.0025999342800;
         dates[1]  = new Date(5, Month.January, 2009);   forwards[1]  = 0.0025999342800;
         dates[2]  = new Date(29, Month.January, 2009);   forwards[2]  = 0.0053123275500;
         dates[3]  = new Date(27, Month.February, 2009);  forwards[3]  = 0.0197049598721;
         dates[4]  = new Date(30, Month.March, 2009);     forwards[4]  = 0.0220524845296;
         dates[5]  = new Date(29, Month.June, 2009);      forwards[5]  = 0.0217076395643;
         dates[6]  = new Date(29, Month.December, 2009);  forwards[6]  = 0.0230349627478;
         dates[7]  = new Date(29, Month.December, 2010);  forwards[7]  = 0.0087631647476;
         dates[8]  = new Date(29, Month.December, 2011);  forwards[8]  = 0.0219084299499;
         dates[9]  = new Date(31, Month.December, 2012);  forwards[9]  = 0.0244798766219;
         dates[10] = new Date(30, Month.December, 2013);  forwards[10] = 0.0267885498456;
         dates[11] = new Date(29, Month.December, 2014);  forwards[11] = 0.0266922867562;
         dates[12] = new Date(29, Month.December, 2015);  forwards[12] = 0.0271052126386;
         dates[13] = new Date(29, Month.December, 2016);  forwards[13] = 0.0268829891648;
         dates[14] = new Date(29, Month.December, 2017);  forwards[14] = 0.0264594744498;
         dates[15] = new Date(31, Month.December, 2018);  forwards[15] = 0.0273450367424;
         dates[16] = new Date(30, Month.December, 2019);  forwards[16] = 0.0294852614749;
         dates[17] = new Date(29, Month.December, 2020);  forwards[17] = 0.0285556119719;
         dates[18] = new Date(29, Month.December, 2021);  forwards[18] = 0.0305557764659;
         dates[19] = new Date(29, Month.December, 2022);  forwards[19] = 0.0292244738422;
         dates[20] = new Date(29, Month.December, 2023);  forwards[20] = 0.0263917004194;
         dates[21] = new Date(29, Month.December, 2028);  forwards[21] = 0.0239626970243;
         dates[22] = new Date(29, Month.December, 2033);  forwards[22] = 0.0216417108090;
         dates[23] = new Date(29, Month.December, 2038);  forwards[23] = 0.0228343838422;
         dates[24] = new Date(31, Month.December, 2199);  forwards[24] = 0.0228343838422;

         Handle<YieldTermStructure> r = new Handle<YieldTermStructure>(new InterpolatedForwardCurve<BackwardFlat>(dates, forwards, new Actual360()));

         Handle<BlackVolTermStructure> sigma = new Handle<BlackVolTermStructure>(new BlackConstantVol(tomorrow, new NullCalendar(), 21.685235548092248,
                                                                                 new Thirty360(Thirty360.Thirty360Convention.BondBasis)));

         BlackProcess process = new BlackProcess(u, r, sigma);

         Handle<Quote> spread = new Handle<Quote>(new SimpleQuote(0.11498700678012874));

         Date issueDate = new Date(23, Month.July, 2008);
         Date maturityDate = new Date(1, Month.August, 2013);
         Calendar calendar = new UnitedStates();
         Schedule schedule = new MakeSchedule().from(issueDate)
         .to(maturityDate)
         .withTenor(new Period(6, TimeUnit.Months))
         .withCalendar(calendar)
         .withConvention(BusinessDayConvention.Unadjusted).value();
         int settlementDays = 3;
         Exercise exercise = new EuropeanExercise(maturityDate);
         double conversionRatio = 100.0 / 20.3175;
         List<double> coupons = new InitializedList<double>(schedule.size() - 1, 0.05);
         DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
         CallabilitySchedule no_callability = new CallabilitySchedule();
         DividendSchedule no_dividends = new DividendSchedule();
         double redemption = 100.0;

         ConvertibleFixedCouponBond bond = new ConvertibleFixedCouponBond(exercise, conversionRatio,
                                                                          no_dividends, no_callability,
                                                                          spread, issueDate, settlementDays,
                                                                          coupons, dayCounter,
                                                                          schedule, redemption);
         bond.setPricingEngine(new BinomialConvertibleEngine<CoxRossRubinstein> (process, 600));

         try
         {
            double x = bond.NPV();  // should throw; if not, an INF was not detected.
            QAssert.Fail("INF result was not detected: " + x + " returned");
         }
         catch (Exception)
         {
            // as expected. Do nothing.

            // Note: we're expecting an Error we threw, not just any
            // exception.  If something else is thrown, then there's
            // another problem and the test must fail.
         }
      }
   }
}
