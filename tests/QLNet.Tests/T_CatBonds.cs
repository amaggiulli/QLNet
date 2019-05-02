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
using Calendar = QLNet.Calendar;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_CatBonds
   {
      static KeyValuePair<Date, double>[] data =
      {
         new KeyValuePair<Date, double>(new Date(1, Month.February, 2012), 100),
         new KeyValuePair<Date, double>(new Date(1, Month.July, 2013), 150),
         new KeyValuePair<Date, double>(new Date(5, Month.January, 2014), 50)
      };

      List<KeyValuePair<Date, double>> sampleEvents = new List<KeyValuePair<Date, double>>(data);

      Date eventsStart = new Date(1, Month.January, 2011);
      Date eventsEnd = new Date(31, Month.December, 2014);

      private class CommonVars
      {
         // common data
         public Calendar calendar;
         public Date today;
         public double faceAmount;

         // setup
         public CommonVars()
         {
            calendar = new TARGET();
            today = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(today);
            faceAmount = 1000000.0;
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testEventSetForWholeYears()
      {
         // Testing that catastrophe events are split correctly for periods of whole years

         EventSet catRisk = new EventSet(sampleEvents, eventsStart, eventsEnd);
         CatSimulation simulation = catRisk.newSimulation(new Date(1, Month.January, 2015), new Date(31, Month.December, 2015));

         QAssert.Require(simulation);

         List<KeyValuePair<Date, double> > path = new List<KeyValuePair<Date, double>>();

         QAssert.Require(simulation.nextPath(path));
         QAssert.AreEqual(0, path.Count);

         QAssert.Require(simulation.nextPath(path));
         QAssert.AreEqual(1, path.Count);
         QAssert.AreEqual(new Date(1, Month.February, 2015), path[0].Key);
         QAssert.AreEqual(100, path[0].Value);

         QAssert.Require(simulation.nextPath(path));
         QAssert.AreEqual(1, path.Count);
         QAssert.AreEqual(new Date(1, Month.July, 2015), path[0].Key);
         QAssert.AreEqual(150, path[0].Value);

         QAssert.Require(simulation.nextPath(path));
         QAssert.AreEqual(1, path.Count);
         QAssert.AreEqual(new Date(5, Month.January, 2015), path[0].Key);
         QAssert.AreEqual(50, path[0].Value);

         QAssert.Require(!simulation.nextPath(path));
      }


#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testEventSetForIrregularPeriods()
      {
         // Testing that catastrophe events are split correctly for irregular periods

         EventSet catRisk = new EventSet(sampleEvents, eventsStart, eventsEnd);
         CatSimulation simulation = catRisk.newSimulation(new Date(2, Month.January, 2015), new Date(5, Month.January, 2016));

         QAssert.Require(simulation);

         List<KeyValuePair<Date, double> > path = new List<KeyValuePair<Date, double>>();

         QAssert.Require(simulation.nextPath(path));
         QAssert.AreEqual(0, path.Count);

         QAssert.Require(simulation.nextPath(path));
         QAssert.AreEqual(2, path.Count);
         QAssert.AreEqual(new Date(1, Month.July, 2015), path[0].Key);
         QAssert.AreEqual(150, path[0].Value);
         QAssert.AreEqual(new Date(5, Month.January, 2016), path[1].Key);
         QAssert.AreEqual(50, path[1].Value);

         QAssert.Require(!simulation.nextPath(path));
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testEventSetForNoEvents()
      {
         // Testing that catastrophe events are split correctly when there are no simulated events

         List<KeyValuePair<Date, double> >  emptyEvents = new List<KeyValuePair<Date, double>>();
         EventSet catRisk = new EventSet(emptyEvents, eventsStart, eventsEnd);
         CatSimulation simulation = catRisk.newSimulation(new Date(2, Month.January, 2015), new Date(5, Month.January, 2016));

         QAssert.Require(simulation);

         List<KeyValuePair<Date, double> > path = new List<KeyValuePair<Date, double>>();

         QAssert.Require(simulation.nextPath(path));
         QAssert.AreEqual(0, path.Count);

         QAssert.Require(simulation.nextPath(path));
         QAssert.AreEqual(0, path.Count);

         QAssert.Require(!simulation.nextPath(path));
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testRiskFreeAgainstFloatingRateBond()
      {
         // Testing floating-rate cat bond against risk-free floating-rate bond

         CommonVars vars = new CommonVars();

         Date today = new Date(22, Month.November, 2004);
         Settings.setEvaluationDate(today);

         int settlementDays = 1;

         Handle<YieldTermStructure> riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
         Handle<YieldTermStructure> discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

         IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
         int fixingDays = 1;

         double tolerance = 1.0e-6;

         IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

         // plain

         Schedule sch = new Schedule(new Date(30, Month.November, 2004),
                                     new Date(30, Month.November, 2008),
                                     new Period(Frequency.Semiannual),
                                     new UnitedStates(UnitedStates.Market.GovernmentBond),
                                     BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                     DateGeneration.Rule.Backward, false);

         CatRisk noCatRisk = new EventSet(new List<KeyValuePair<Date, double>>(),  new Date(1, Month.Jan, 2000), new Date(31, Month.Dec, 2010));

         EventPaymentOffset paymentOffset = new NoOffset();
         NotionalRisk notionalRisk = new DigitalNotionalRisk(paymentOffset, 100);

         FloatingRateBond bond1 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                                       index, new ActualActual(ActualActual.Convention.ISMA),
                                                       BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                       new List<double>(), new List<double>(),
                                                       new List < double? >(), new List < double? >(),
                                                       false,
                                                       100.0, new Date(30, Month.November, 2004));

         FloatingCatBond catBond1 = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                        index, new ActualActual(ActualActual.Convention.ISMA),
                                                        notionalRisk,
                                                        BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                        new List<double>(), new List<double>(),
                                                        new List < double? >(), new List < double? >(),
                                                        false,
                                                        100.0, new Date(30, Month.November, 2004));

         IPricingEngine bondEngine = new DiscountingBondEngine(riskFreeRate);
         bond1.setPricingEngine(bondEngine);
         Utils.setCouponPricer(bond1.cashflows(), pricer);

         IPricingEngine catBondEngine = new MonteCarloCatBondEngine(noCatRisk, riskFreeRate);
         catBond1.setPricingEngine(catBondEngine);
         Utils.setCouponPricer(catBond1.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
         double cachedPrice1 = 99.874645;
#else
         double cachedPrice1 = 99.874646;
#endif


         double price = bond1.cleanPrice();
         double catPrice = catBond1.cleanPrice();
         if (Math.Abs(price - cachedPrice1) > tolerance || Math.Abs(catPrice - price) > tolerance)
         {
            QAssert.Fail("failed to reproduce floating rate bond price:\n"
                         + "    floating bond: " + price + "\n"
                         + "    catBond bond: " + catPrice + "\n"
                         + "    expected:   " + cachedPrice1 + "\n"
                         + "    error:      " + (catPrice - price));
         }



         // different risk-free and discount curve

         FloatingRateBond bond2 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                                       index, new ActualActual(ActualActual.Convention.ISMA),
                                                       BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                       new List<double>(), new List<double>(),
                                                       new List < double? >(), new List < double? >(),
                                                       false,
                                                       100.0, new Date(30, Month.November, 2004));

         FloatingCatBond catBond2 = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                        index, new ActualActual(ActualActual.Convention.ISMA),
                                                        notionalRisk,
                                                        BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                        new List<double>(), new List<double>(),
                                                        new List < double? >(), new List < double? >(),
                                                        false,
                                                        100.0, new Date(30, Month.November, 2004));

         IPricingEngine bondEngine2 = new DiscountingBondEngine(discountCurve);
         bond2.setPricingEngine(bondEngine2);
         Utils.setCouponPricer(bond2.cashflows(), pricer);

         IPricingEngine catBondEngine2 = new MonteCarloCatBondEngine(noCatRisk, discountCurve);
         catBond2.setPricingEngine(catBondEngine2);
         Utils.setCouponPricer(catBond2.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
         double cachedPrice2 = 97.955904;
#else
         double cachedPrice2 = 97.955904;
#endif

         price = bond2.cleanPrice();
         catPrice = catBond2.cleanPrice();
         if (Math.Abs(price - cachedPrice2) > tolerance || Math.Abs(catPrice - price) > tolerance)
         {
            QAssert.Fail("failed to reproduce floating rate bond price:\n"
                         + "    floating bond: " + price + "\n"
                         + "    catBond bond: " + catPrice + "\n"
                         + "    expected:   " + cachedPrice2 + "\n"
                         + "    error:      " + (catPrice - price));
         }

         // varying spread

         List<double> spreads = new InitializedList<double>(4);
         spreads[0] = 0.001;
         spreads[1] = 0.0012;
         spreads[2] = 0.0014;
         spreads[3] = 0.0016;

         FloatingRateBond bond3 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                                       index, new ActualActual(ActualActual.Convention.ISMA),
                                                       BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                       new List<double>(), spreads,
                                                       new List < double? >(), new List < double? >(),
                                                       false,
                                                       100.0, new Date(30, Month.November, 2004));

         FloatingCatBond catBond3 = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                        index, new ActualActual(ActualActual.Convention.ISMA),
                                                        notionalRisk,
                                                        BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                        new List<double>(), spreads,
                                                        new List < double? >(), new List < double? >(),
                                                        false,
                                                        100.0, new Date(30, Month.November, 2004));

         bond3.setPricingEngine(bondEngine2);
         Utils.setCouponPricer(bond3.cashflows(), pricer);

         catBond3.setPricingEngine(catBondEngine2);
         Utils.setCouponPricer(catBond3.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
         double cachedPrice3 = 98.495458;
#else
         double cachedPrice3 = 98.495459;
#endif

         price = bond3.cleanPrice();
         catPrice = catBond3.cleanPrice();
         if (Math.Abs(price - cachedPrice3) > tolerance || Math.Abs(catPrice - price) > tolerance)
         {
            QAssert.Fail("failed to reproduce floating rate bond price:\n"
                         + "    floating bond: " + price + "\n"
                         + "    catBond bond: " + catPrice + "\n"
                         + "    expected:   " + cachedPrice2 + "\n"
                         + "    error:      " + (catPrice - price));
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCatBondInDoomScenario()
      {
         // Testing floating-rate cat bond in a doom scenario (certain default)

         CommonVars vars = new CommonVars();

         Date today = new Date(22, Month.November, 2004);
         Settings.setEvaluationDate(today);

         int settlementDays = 1;

         Handle<YieldTermStructure> riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
         Handle<YieldTermStructure> discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

         IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
         int fixingDays = 1;

         double tolerance = 1.0e-6;

         IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

         Schedule sch = new Schedule(new Date(30, Month.November, 2004),
                                     new Date(30, Month.November, 2008),
                                     new Period(Frequency.Semiannual),
                                     new UnitedStates(UnitedStates.Market.GovernmentBond),
                                     BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                     DateGeneration.Rule.Backward, false);

         List<KeyValuePair<Date, double> > events = new List<KeyValuePair<Date, double>>();
         events.Add(new KeyValuePair<Date, double>(new Date(30, Month.November, 2004), 1000));
         CatRisk doomCatRisk = new EventSet(events,
                                            new Date(30, Month.November, 2004), new Date(30, Month.November, 2008));

         EventPaymentOffset paymentOffset = new NoOffset();
         NotionalRisk notionalRisk = new DigitalNotionalRisk(paymentOffset, 100);

         FloatingCatBond catBond = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                       index, new ActualActual(ActualActual.Convention.ISMA),
                                                       notionalRisk,
                                                       BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                       new List<double>(), new List<double>(),
                                                       new List < double? >(), new List < double? >(),
                                                       false,
                                                       100.0, new Date(30, Month.November, 2004));

         IPricingEngine catBondEngine = new MonteCarloCatBondEngine(doomCatRisk, discountCurve);
         catBond.setPricingEngine(catBondEngine);
         Utils.setCouponPricer(catBond.cashflows(), pricer);

         double price = catBond.cleanPrice();
         QAssert.AreEqual(0, price);

         double lossProbability = catBond.lossProbability();
         double exhaustionProbability = catBond.exhaustionProbability();
         double expectedLoss = catBond.expectedLoss();

         QAssert.AreEqual(1.0, lossProbability, tolerance);
         QAssert.AreEqual(1.0, exhaustionProbability, tolerance);
         QAssert.AreEqual(1.0, expectedLoss, tolerance);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCatBondWithDoomOnceInTenYears()
      {
         // Testing floating-rate cat bond in a doom once in 10 years scenario
         CommonVars vars = new CommonVars();

         Date today = new Date(22, Month.November, 2004);
         Settings.setEvaluationDate(today);

         int settlementDays = 1;

         Handle<YieldTermStructure> riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
         Handle<YieldTermStructure> discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

         IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
         int fixingDays = 1;

         double tolerance = 1.0e-6;

         IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

         Schedule sch = new Schedule(new Date(30, Month.November, 2004),
                                     new Date(30, Month.November, 2008),
                                     new Period(Frequency.Semiannual),
                                     new UnitedStates(UnitedStates.Market.GovernmentBond),
                                     BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                     DateGeneration.Rule.Backward, false);

         List<KeyValuePair<Date, double> >  events = new List<KeyValuePair<Date, double>>();
         events.Add(new KeyValuePair<Date, double>(new Date(30, Month.November, 2008), 1000));
         CatRisk doomCatRisk = new EventSet(events, new Date(30, Month.November, 2004), new Date(30, Month.November, 2044));

         CatRisk noCatRisk = new EventSet(new List<KeyValuePair<Date, double>>(),
                                          new Date(1, Month.Jan, 2000), new Date(31, Month.Dec, 2010));

         EventPaymentOffset paymentOffset = new NoOffset();
         NotionalRisk notionalRisk = new DigitalNotionalRisk(paymentOffset, 100);

         FloatingCatBond catBond = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                       index, new ActualActual(ActualActual.Convention.ISMA),
                                                       notionalRisk,
                                                       BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                       new List<double>(), new List<double>(),
                                                       new List < double? >(), new List < double? >(),
                                                       false,
                                                       100.0, new Date(30, Month.November, 2004));

         IPricingEngine catBondEngine = new MonteCarloCatBondEngine(doomCatRisk, discountCurve);
         catBond.setPricingEngine(catBondEngine);
         Utils.setCouponPricer(catBond.cashflows(), pricer);

         double price = catBond.cleanPrice();
         double yield = catBond.yield(new ActualActual(ActualActual.Convention.ISMA), Compounding.Simple, Frequency.Annual);
         double lossProbability = catBond.lossProbability();
         double exhaustionProbability = catBond.exhaustionProbability();
         double expectedLoss = catBond.expectedLoss();

         QAssert.AreEqual(0.1, lossProbability, tolerance);
         QAssert.AreEqual(0.1, exhaustionProbability, tolerance);
         QAssert.AreEqual(0.1, expectedLoss, tolerance);

         IPricingEngine catBondEngineRF = new MonteCarloCatBondEngine(noCatRisk, discountCurve);
         catBond.setPricingEngine(catBondEngineRF);

         double riskFreePrice = catBond.cleanPrice();
         double riskFreeYield = catBond.yield(new ActualActual(ActualActual.Convention.ISMA), Compounding.Simple, Frequency.Annual);
         double riskFreeLossProbability = catBond.lossProbability();
         double riskFreeExhaustionProbability = catBond.exhaustionProbability();
         double riskFreeExpectedLoss = catBond.expectedLoss();

         QAssert.AreEqual(0.0, riskFreeLossProbability, tolerance);
         QAssert.AreEqual(0.0, riskFreeExhaustionProbability, tolerance);
         QAssert.IsTrue(Math.Abs(riskFreeExpectedLoss) < tolerance);

         QAssert.AreEqual(riskFreePrice * 0.9, price, tolerance);
         QAssert.IsTrue(riskFreeYield < yield);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCatBondWithDoomOnceInTenYearsProportional()
      {
         // Testing floating-rate cat bond in a doom once in 10 years scenario with proportional notional reduction

         CommonVars vars = new CommonVars();

         Date today = new Date(22, Month.November, 2004);
         Settings.setEvaluationDate(today);

         int settlementDays = 1;

         Handle<YieldTermStructure> riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
         Handle<YieldTermStructure> discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

         IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
         int fixingDays = 1;

         double tolerance = 1.0e-6;

         IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

         Schedule sch =
            new Schedule(new Date(30, Month.November, 2004),
                         new Date(30, Month.November, 2008),
                         new Period(Frequency.Semiannual),
                         new UnitedStates(UnitedStates.Market.GovernmentBond),
                         BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                         DateGeneration.Rule.Backward, false);

         List<KeyValuePair<Date, double> > events = new List<KeyValuePair<Date, double>>();
         events.Add(new KeyValuePair<Date, double>(new Date(30, Month.November, 2008), 1000));
         CatRisk doomCatRisk = new EventSet(events, new Date(30, Month.November, 2004), new Date(30, Month.November, 2044));

         CatRisk noCatRisk = new EventSet(new List<KeyValuePair<Date, double> > (),
                                          new Date(1, Month.Jan, 2000), new Date(31, Month.Dec, 2010));

         EventPaymentOffset paymentOffset = new NoOffset();
         NotionalRisk notionalRisk = new ProportionalNotionalRisk(paymentOffset, 500, 1500);

         FloatingCatBond catBond =
            new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                index, new ActualActual(ActualActual.Convention.ISMA),
                                notionalRisk,
                                BusinessDayConvention.ModifiedFollowing, fixingDays,
                                new List<double>(), new List<double>(),
                                new List < double? >(), new List < double? >(),
                                false,
                                100.0, new Date(30, Month.November, 2004));

         IPricingEngine catBondEngine = new MonteCarloCatBondEngine(doomCatRisk, discountCurve);
         catBond.setPricingEngine(catBondEngine);
         Utils.setCouponPricer(catBond.cashflows(), pricer);

         double price = catBond.cleanPrice();
         double yield = catBond.yield(new ActualActual(ActualActual.Convention.ISMA), Compounding.Simple, Frequency.Annual);
         double lossProbability = catBond.lossProbability();
         double exhaustionProbability = catBond.exhaustionProbability();
         double expectedLoss = catBond.expectedLoss();

         QAssert.AreEqual(0.1, lossProbability, tolerance);
         QAssert.AreEqual(0.0, exhaustionProbability, tolerance);
         QAssert.AreEqual(0.05, expectedLoss, tolerance);

         IPricingEngine catBondEngineRF = new MonteCarloCatBondEngine(noCatRisk, discountCurve);
         catBond.setPricingEngine(catBondEngineRF);

         double riskFreePrice = catBond.cleanPrice();
         double riskFreeYield = catBond.yield(new ActualActual(ActualActual.Convention.ISMA), Compounding.Simple, Frequency.Annual);
         double riskFreeLossProbability = catBond.lossProbability();
         double riskFreeExpectedLoss = catBond.expectedLoss();

         QAssert.AreEqual(0.0, riskFreeLossProbability, tolerance);
         QAssert.IsTrue(Math.Abs(riskFreeExpectedLoss) < tolerance);

         QAssert.AreEqual(riskFreePrice * 0.95, price, tolerance);
         QAssert.IsTrue(riskFreeYield < yield);
      }
   }
}
