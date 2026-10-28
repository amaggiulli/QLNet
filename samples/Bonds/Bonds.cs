/*
 Copyright (C) 2008-2024 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using QLNet;

namespace Bonds;

public class Bonds
{
   public static void Main(string[] args)
   {
      var timer = DateTime.Now;
      Calendar calendar = new TARGET();
      var settlementDate = new Date(18, Month.September, 2008);
      const int settlementDays = 3;
      var todaysDate = calendar.advance(settlementDate, -settlementDays, TimeUnit.Days);
      Settings.setEvaluationDate(todaysDate);

      Console.WriteLine("Today: {0}, {1}", todaysDate.DayOfWeek, todaysDate);
      Console.WriteLine("Settlement date: {0}, {1}", settlementDate.DayOfWeek, settlementDate);

      /***************************************
       * BUILDING THE DISCOUNTING BOND CURVE *
       ***************************************/

      // RateHelpers are built from the quotes together with
      // other instrument-dependent info.  Quotes are passed in
      // relinkable handles which could be relinked to some other
      // data source later.

      // Note that bootstrapping might not be the optimal choice for
      // bond curves, since it requires to select a set of bonds
      // with maturities that are not too close.  For alternatives,
      // see the FittedBondCurve example.

      var redemption = 100.0;

      const int numberOfBonds = 5;

      Date[] issueDates =
      [
         new(15, Month.March, 2005),
         new(15, Month.June, 2005),
         new(30, Month.June, 2006),
         new(15, Month.November, 2002),
         new(15, Month.May, 1987)
      ];

      Date[] maturities =
      [
         new(31, Month.August, 2010),
         new(31, Month.August, 2011),
         new(31, Month.August, 2013),
         new(15, Month.August, 2018),
         new(15, Month.May, 2038)
      ];

      double[] couponRates =
      [
         0.02375,
         0.04625,
         0.03125,
         0.04000,
         0.04500
      ];

      double[] marketQuotes =
      [
         100.390625,
         106.21875,
         100.59375,
         101.6875,
         102.140625
      ];

      var quote = new List<SimpleQuote>();
      for (var i = 0; i < numberOfBonds; i++)
         quote.Add(new SimpleQuote(marketQuotes[i]));

      var quoteHandle = new InitializedList<RelinkableHandle<Quote>>(numberOfBonds);
      for (var i = 0; i < numberOfBonds; i++)
         quoteHandle[i].linkTo(quote[i]);

      // Definition of the rate helpers
      var bondsHelpers = new List<RateHelper>();
      for (var i = 0; i < numberOfBonds; i++)
      {
         var schedule = new Schedule(issueDates[i], maturities[i], new Period(Frequency.Semiannual),
            calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
            DateGeneration.Rule.Backward, false);

         var bondHelper = new FixedRateBondHelper(quoteHandle[i],
            settlementDays,
            100.0,
            schedule,
            [couponRates[i]],
            new ActualActual(ActualActual.Convention.Bond),
            BusinessDayConvention.Unadjusted,
            redemption,
            issueDates[i]);

         // Th bond helper could also be done by creating a
         // FixedRateBond instance and writing:
         //
         // var bondHelper = new BondHelper(quoteHandle[i], bond);
         //
         // This would also work for bonds that still don't have a
         // specialized helper, such as floating-rate bonds.

         bondsHelpers.Add(bondHelper);
      }

      // The term structure uses its day counter internally to
      // convert between dates and times; it's not required to equal
      // the day counter of the bonds.  In fact, a regular day
      // counter is probably more appropriate.
      DayCounter termStructureDayCounter = new Actual365Fixed();

      // The reference date of the term structure can be the
      // settlement date of the bonds (since, during pricing, it
      // won't be required to discount behind that date) but it can
      // also be today's date.  This allows one to calculate both
      // the price of the bond (based on the settlement date) and
      // the NPV, that is, the value as of today's date of holding
      // the bond and receiving its payments.
      YieldTermStructure bondDiscountingTermStructure = new PiecewiseYieldCurve<Discount, LogLinear>(
         todaysDate, bondsHelpers, termStructureDayCounter);

      //
      // BUILDING THE EURIBOR FORECASTING CURVE
      //

      // 6m deposits
      var d6mQuote = 0.03385;
      // swaps, fixed vs 6m
      var s2yQuote = 0.0295;
      var s3yQuote = 0.0323;
      var s5yQuote = 0.0359;
      var s10yQuote = 0.0412;
      var s15yQuote = 0.0433;

      Quote d6mRate = new SimpleQuote(d6mQuote);
      Quote s2yRate = new SimpleQuote(s2yQuote);
      Quote s3yRate = new SimpleQuote(s3yQuote);
      Quote s5yRate = new SimpleQuote(s5yQuote);
      Quote s10yRate = new SimpleQuote(s10yQuote);
      Quote s15yRate = new SimpleQuote(s15yQuote);

      // setup deposits
      DayCounter depositDayCounter = new Actual360();
      var fixingDays = 2;

      RateHelper d6m = new DepositRateHelper(
         new Handle<Quote>(d6mRate),
         new Period(6, TimeUnit.Months), fixingDays,
         calendar, BusinessDayConvention.ModifiedFollowing,
         true, depositDayCounter);

      // setup swaps
      var swFixedLegFrequency = Frequency.Annual;
      var swFixedLegConvention = BusinessDayConvention.Unadjusted;
      DayCounter swFixedLegDayCounter = new Thirty360(Thirty360.Thirty360Convention.European);
      IborIndex swFloatingLegIndex = new Euribor6M();

      RateHelper s2y = new SwapRateHelper(
         new Handle<Quote>(s2yRate), new Period(2, TimeUnit.Years),
         calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter,
         swFloatingLegIndex);
      RateHelper s3y = new SwapRateHelper(
         new Handle<Quote>(s3yRate), new Period(3, TimeUnit.Years),
         calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter,
         swFloatingLegIndex);
      RateHelper s5y = new SwapRateHelper(
         new Handle<Quote>(s5yRate), new Period(5, TimeUnit.Years),
         calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter,
         swFloatingLegIndex);
      RateHelper s10y = new SwapRateHelper(
         new Handle<Quote>(s10yRate), new Period(10, TimeUnit.Years),
         calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter,
         swFloatingLegIndex);
      RateHelper s15y = new SwapRateHelper(
         new Handle<Quote>(s15yRate), new Period(15, TimeUnit.Years),
         calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter,
         swFloatingLegIndex);

      var depoSwapInstruments = new List<RateHelper> { d6m, s2y, s3y, s5y, s10y, s15y };

      // The start of the curve can be today's date or spot, depending on your preferences.
      // Here we're picking spot (mostly because we picked today's date for the bond curve).
      var spotDate = calendar.advance(todaysDate, fixingDays, TimeUnit.Days);

      YieldTermStructure depoSwapTermStructure = new PiecewiseYieldCurve<Discount, LogLinear>(
         spotDate, depoSwapInstruments, termStructureDayCounter);

      // PRICING
      var discountingTermStructure = new RelinkableHandle<YieldTermStructure>();
      var forecastingTermStructure = new RelinkableHandle<YieldTermStructure>();

      // bonds to be priced

      // Common data
      double faceAmount = 100;

      // Pricing engine
      IPricingEngine bondEngine = new DiscountingBondEngine(discountingTermStructure);

      // Zero coupon bond
      var zeroCouponBond = new ZeroCouponBond(
         settlementDays,
         calendar,
         faceAmount,
         new Date(15, Month.August, 2013),
         BusinessDayConvention.Following,
         116.92,
         new Date(15, Month.August, 2003));

      zeroCouponBond.setPricingEngine(bondEngine);

      // Fixed 4.5% bond
      var fixedBondSchedule = new Schedule(new Date(15, Month.May, 2007),
         new Date(15, Month.May, 2017), new Period(Frequency.Annual),
         calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false);

      var fixedRateBond = new FixedRateBond(
         settlementDays,
         faceAmount,
         fixedBondSchedule,
         [0.045],
         new ActualActual(ActualActual.Convention.Bond),
         BusinessDayConvention.ModifiedFollowing,
         100.0, new Date(15, Month.May, 2007));

      fixedRateBond.setPricingEngine(bondEngine);

      // Floating rate bond (6M Euribor + 0.1%)
      var liborTermStructure = new RelinkableHandle<YieldTermStructure>();
      IborIndex euribor6m = new Euribor(new Period(6, TimeUnit.Months), forecastingTermStructure);
      euribor6m.addFixing(new Date(18, Month.October, 2007), 0.026);
      euribor6m.addFixing(new Date(17, Month.April, 2008), 0.028);

      var floatingBondSchedule = new Schedule(new Date(21, Month.October, 2005),
         new Date(21, Month.October, 2010), new Period(Frequency.Semiannual),
         calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, true);

      var floatingRateBond = new FloatingRateBond(
         settlementDays,
         faceAmount,
         floatingBondSchedule,
         euribor6m,
         new Actual360(),
         BusinessDayConvention.ModifiedFollowing,
         2,
         // Gearings
         [1.0],
         // Spreads
         [0.001],
         // Caps
         [],
         // Floors
         [],
         // Fixing in arrears
         false,
         100.0,
         new Date(21, Month.October, 2005));

      floatingRateBond.setPricingEngine(bondEngine);

      // Coupon pricers
      IborCouponPricer pricer = new BlackIborCouponPricer();

      // optionLet volatilities
      var volatility = 0.0;
      var vol = new Handle<OptionletVolatilityStructure>(
         new ConstantOptionletVolatility(
            settlementDays,
            calendar,
            BusinessDayConvention.ModifiedFollowing,
            volatility,
            new Actual365Fixed()));

      pricer.setCapletVolatility(vol);
      Utils.setCouponPricer(floatingRateBond.cashflows(), pricer);

      forecastingTermStructure.linkTo(depoSwapTermStructure);
      discountingTermStructure.linkTo(bondDiscountingTermStructure);

      Console.WriteLine();

      // write column headings
      int[] widths = [18, 10, 10, 10];

      Console.WriteLine("{0,18}{1,10}{2,10}{3,10}", "", "ZC", "Fixed", "Floating");

      var width = widths[0]
                  + widths[1]
                  + widths[2]
                  + widths[3];
      string rule = "".PadLeft(width, '-'), dblrule = "".PadLeft(width, '=');

      Console.WriteLine(rule);

      floatingRateBond.NPV();

      Console.WriteLine("Net present value".PadLeft(widths[0]) + "{0,10:n2}{1,10:n2}{2,10:n2}",
         zeroCouponBond.NPV(),
         fixedRateBond.NPV(),
         floatingRateBond.NPV());

      Console.WriteLine("Clean price".PadLeft(widths[0]) + "{0,10:n2}{1,10:n2}{2,10:n2}",
         zeroCouponBond.cleanPrice(),
         fixedRateBond.cleanPrice(),
         floatingRateBond.cleanPrice());

      Console.WriteLine("Dirty price".PadLeft(widths[0]) + "{0,10:n2}{1,10:n2}{2,10:n2}",
         zeroCouponBond.dirtyPrice(),
         fixedRateBond.dirtyPrice(),
         floatingRateBond.dirtyPrice());

      Console.WriteLine("Accrued coupon".PadLeft(widths[0]) + "{0,10:n2}{1,10:n2}{2,10:n2}",
         zeroCouponBond.accruedAmount(),
         fixedRateBond.accruedAmount(),
         floatingRateBond.accruedAmount());

      Console.WriteLine("Previous coupon".PadLeft(widths[0]) + "{0,10:0.00%}{1,10:0.00%}{2,10:0.00%}",
         "N/A",
         fixedRateBond.previousCouponRate(),
         floatingRateBond.previousCouponRate());

      Console.WriteLine("Next coupon".PadLeft(widths[0]) + "{0,10:0.00%}{1,10:0.00%}{2,10:0.00%}",
         "N/A",
         fixedRateBond.nextCouponRate(),
         floatingRateBond.nextCouponRate());

      Console.WriteLine("Yield".PadLeft(widths[0]) + "{0,10:0.00%}{1,10:0.00%}{2,10:0.00%}",
         zeroCouponBond.yield(new Actual360(), Compounding.Compounded, Frequency.Annual),
         fixedRateBond.yield(new Actual360(), Compounding.Compounded, Frequency.Annual),
         floatingRateBond.yield(new Actual360(), Compounding.Compounded, Frequency.Annual));

      Console.WriteLine();

      // Other computations
      Console.WriteLine("Sample indirect computations (for the floating rate bond): ");
      Console.WriteLine(rule);

      Console.WriteLine("Yield to Clean Price: {0:n2}",
         floatingRateBond.cleanPrice(floatingRateBond.yield(new Actual360(), Compounding.Compounded, Frequency.Annual),
            new Actual360(), Compounding.Compounded, Frequency.Annual,
            settlementDate));

      Console.WriteLine("Clean Price to Yield: {0:0.00%}",
         floatingRateBond.yield(floatingRateBond.cleanPrice(), new Actual360(), Compounding.Compounded, Frequency.Annual,
            settlementDate));

      /* "Yield to Price"
         "Price to Yield" */

      Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer);
      Console.WriteLine();

      Console.Write("Press any key to continue ...");
      Console.ReadKey();
   }
}
