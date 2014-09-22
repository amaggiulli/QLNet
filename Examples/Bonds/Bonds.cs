/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)  
 * 
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
using QLNet;

namespace Bonds {
    class Bonds {
        static void Main(string[] args) {

            DateTime timer = DateTime.Now;

            /*********************
             ***  MARKET DATA  ***
             *********************/

            Calendar calendar = new TARGET();

            Date settlementDate = new Date(18, Month.September, 2008);
            // must be a business day
            settlementDate = calendar.adjust(settlementDate);

            int fixingDays = 3;
            int settlementDays = 3;

            Date todaysDate = calendar.advance(settlementDate, -fixingDays, TimeUnit.Days);
            // nothing to do with Date::todaysDate
            Settings.setEvaluationDate(todaysDate);

            Console.WriteLine("Today: {0}, {1}", todaysDate.DayOfWeek, todaysDate);
            Console.WriteLine("Settlement date: {0}, {1}", settlementDate.DayOfWeek, settlementDate);


            // Building of the bonds discounting yield curve

            /*********************
             ***  RATE HELPERS ***
             *********************/

            // RateHelpers are built from the above quotes together with
            // other instrument dependant infos.  Quotes are passed in
            // relinkable handles which could be relinked to some other
            // data source later.

            // Common data

            // ZC rates for the short end
             double zc3mQuote=0.0096;
             double zc6mQuote=0.0145;
             double zc1yQuote=0.0194;

             Quote zc3mRate = new SimpleQuote(zc3mQuote);
             Quote zc6mRate = new SimpleQuote(zc6mQuote);
             Quote zc1yRate = new SimpleQuote(zc1yQuote);

             DayCounter zcBondsDayCounter = new Actual365Fixed();

             RateHelper zc3m = new DepositRateHelper(new Handle<Quote>(zc3mRate),
                                                          new Period(3, TimeUnit.Months), fixingDays,
                                                          calendar, BusinessDayConvention.ModifiedFollowing,
                                                          true, zcBondsDayCounter);
             RateHelper zc6m = new DepositRateHelper(new Handle<Quote>(zc6mRate),
                                                          new Period(6, TimeUnit.Months), fixingDays,
                                                          calendar, BusinessDayConvention.ModifiedFollowing,
                                                          true, zcBondsDayCounter);
             RateHelper zc1y = new DepositRateHelper(new Handle<Quote>(zc1yRate),
                                                          new Period(1, TimeUnit.Years), fixingDays,
                                                          calendar, BusinessDayConvention.ModifiedFollowing,
                                                          true, zcBondsDayCounter);

            // setup bonds
            double redemption = 100.0;

            const int numberOfBonds = 5;

            Date[] issueDates = {
                    new Date (15, Month.March, 2005),
                    new Date (15, Month.June, 2005),
                    new Date (30, Month.June, 2006),
                    new Date (15, Month.November, 2002),
                    new Date (15, Month.May, 1987)
            };

            Date[] maturities = {
                    new Date (31, Month.August, 2010),
                    new Date (31, Month.August, 2011),
                    new Date (31, Month.August, 2013),
                    new Date (15, Month.August, 2018),
                    new Date (15, Month.May, 2038)
            };

            double[] couponRates = {
                    0.02375,
                    0.04625,
                    0.03125,
                    0.04000,
                    0.04500
            };

            double[] marketQuotes = {
                    100.390625,
                    106.21875,
                    100.59375,
                    101.6875,
                    102.140625
            };

            List<SimpleQuote> quote = new List<SimpleQuote>();
            for (int i=0; i<numberOfBonds; i++) {
                SimpleQuote cp = new SimpleQuote(marketQuotes[i]);
                quote.Add(cp);
            }

            List<RelinkableHandle<Quote>> quoteHandle = new InitializedList<RelinkableHandle<Quote>>(numberOfBonds);
            for (int i=0; i<numberOfBonds; i++) {
                quoteHandle[i].linkTo(quote[i]);
            }

            // Definition of the rate helpers
            List<FixedRateBondHelper> bondsHelpers = new List<FixedRateBondHelper>();
            for (int i=0; i<numberOfBonds; i++) {

                Schedule schedule = new Schedule(issueDates[i], maturities[i], new Period(Frequency.Semiannual), 
                                                 new UnitedStates(UnitedStates.Market.GovernmentBond),
                                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, 
                                                 DateGeneration.Rule.Backward, false);

                FixedRateBondHelper bondHelper = new FixedRateBondHelper(quoteHandle[i],
                                                                         settlementDays,
                                                                         100.0,
                                                                         schedule,
                                                                         new List<double>() { couponRates[i] },
                                                                         new ActualActual(ActualActual.Convention.Bond),
                                                                         BusinessDayConvention.Unadjusted,
                                                                         redemption,
                                                                         issueDates[i]);

                bondsHelpers.Add(bondHelper);
            }

            /*********************
             **  CURVE BUILDING **
             *********************/

             // Any DayCounter would be fine.
             // ActualActual::ISDA ensures that 30 years is 30.0
             DayCounter termStructureDayCounter = new ActualActual(ActualActual.Convention.ISDA);

             double tolerance = 1.0e-15;

             // A depo-bond curve
             List<RateHelper> bondInstruments = new List<RateHelper>();

             // Adding the ZC bonds to the curve for the short end
             bondInstruments.Add(zc3m);
             bondInstruments.Add(zc6m);
             bondInstruments.Add(zc1y);

             // Adding the Fixed rate bonds to the curve for the long end
             for (int i=0; i<numberOfBonds; i++) {
                 bondInstruments.Add(bondsHelpers[i]);
             }

             YieldTermStructure bondDiscountingTermStructure = new PiecewiseYieldCurve<Discount,LogLinear>(
                                                                     settlementDate, bondInstruments,
                                                                     termStructureDayCounter,
                                                                     new List<Handle<Quote>>(),
                                                                     new List<Date>(),
                                                                     tolerance);

             // Building of the Libor forecasting curve
             // deposits
             double d1wQuote=0.043375;
             double d1mQuote=0.031875;
             double d3mQuote=0.0320375;
             double d6mQuote=0.03385;
             double d9mQuote=0.0338125;
             double d1yQuote=0.0335125;
             // swaps
             double s2yQuote=0.0295;
             double s3yQuote=0.0323;
             double s5yQuote=0.0359;
             double s10yQuote=0.0412;
             double s15yQuote=0.0433;


             /********************
              ***    QUOTES    ***
              ********************/

             // SimpleQuote stores a value which can be manually changed;
             // other Quote subclasses could read the value from a database
             // or some kind of data feed.

             // deposits
             Quote d1wRate = new SimpleQuote(d1wQuote);
             Quote d1mRate = new SimpleQuote(d1mQuote);
             Quote d3mRate = new SimpleQuote(d3mQuote);
             Quote d6mRate = new SimpleQuote(d6mQuote);
             Quote d9mRate = new SimpleQuote(d9mQuote);
             Quote d1yRate = new SimpleQuote(d1yQuote);
             // swaps
             Quote s2yRate = new SimpleQuote(s2yQuote);
             Quote s3yRate = new SimpleQuote(s3yQuote);
             Quote s5yRate = new SimpleQuote(s5yQuote);
             Quote s10yRate = new SimpleQuote(s10yQuote);
             Quote s15yRate = new SimpleQuote(s15yQuote);

             /*********************
              ***  RATE HELPERS ***
              *********************/

             // RateHelpers are built from the above quotes together with
             // other instrument dependant infos.  Quotes are passed in
             // relinkable handles which could be relinked to some other
             // data source later.

             // deposits
             DayCounter depositDayCounter = new Actual360();

             RateHelper d1w = new DepositRateHelper(
                     new Handle<Quote>(d1wRate),
                     new Period(1, TimeUnit.Weeks), fixingDays,
                     calendar, BusinessDayConvention.ModifiedFollowing,
                     true, depositDayCounter);
             RateHelper d1m = new DepositRateHelper(
                     new Handle<Quote>(d1mRate),
                     new Period(1, TimeUnit.Months), fixingDays,
                     calendar, BusinessDayConvention.ModifiedFollowing,
                     true, depositDayCounter);
             RateHelper d3m = new DepositRateHelper(
                     new Handle<Quote>(d3mRate),
                     new Period(3, TimeUnit.Months), fixingDays,
                     calendar, BusinessDayConvention.ModifiedFollowing,
                     true, depositDayCounter);
             RateHelper d6m = new DepositRateHelper(
                     new Handle<Quote>(d6mRate),
                     new Period(6, TimeUnit.Months), fixingDays,
                     calendar, BusinessDayConvention.ModifiedFollowing,
                     true, depositDayCounter);
             RateHelper d9m = new DepositRateHelper(
                     new Handle<Quote>(d9mRate),
                     new Period(9, TimeUnit.Months), fixingDays,
                     calendar, BusinessDayConvention.ModifiedFollowing,
                     true, depositDayCounter);
             RateHelper d1y = new DepositRateHelper(
                     new Handle<Quote>(d1yRate),
                     new Period(1, TimeUnit.Years), fixingDays,
                     calendar, BusinessDayConvention.ModifiedFollowing,
                     true, depositDayCounter);

             // setup swaps
             Frequency swFixedLegFrequency =Frequency.Annual;
             BusinessDayConvention swFixedLegConvention = BusinessDayConvention.Unadjusted;
             DayCounter swFixedLegDayCounter = new Thirty360(Thirty360.Thirty360Convention.European);
             IborIndex swFloatingLegIndex = new Euribor6M();

             Period forwardStart = new Period(1, TimeUnit.Days);

             RateHelper s2y = new SwapRateHelper(
                     new Handle<Quote>(s2yRate), new Period(2, TimeUnit.Years),
                     calendar, swFixedLegFrequency,
                     swFixedLegConvention, swFixedLegDayCounter,
                     swFloatingLegIndex, new Handle<Quote>(),forwardStart);
             RateHelper s3y = new SwapRateHelper(
                     new Handle<Quote>(s3yRate), new Period(3, TimeUnit.Years),
                     calendar, swFixedLegFrequency,
                     swFixedLegConvention, swFixedLegDayCounter,
                     swFloatingLegIndex, new Handle<Quote>(),forwardStart);
             RateHelper s5y = new SwapRateHelper(
                     new Handle<Quote>(s5yRate), new Period(5, TimeUnit.Years),
                     calendar, swFixedLegFrequency,
                     swFixedLegConvention, swFixedLegDayCounter,
                     swFloatingLegIndex, new Handle<Quote>(),forwardStart);
             RateHelper s10y = new SwapRateHelper(
                     new Handle<Quote>(s10yRate), new Period(10, TimeUnit.Years),
                     calendar, swFixedLegFrequency,
                     swFixedLegConvention, swFixedLegDayCounter,
                     swFloatingLegIndex, new Handle<Quote>(),forwardStart);
             RateHelper s15y = new SwapRateHelper(
                     new Handle<Quote>(s15yRate), new Period(15, TimeUnit.Years),
                     calendar, swFixedLegFrequency,
                     swFixedLegConvention, swFixedLegDayCounter,
                     swFloatingLegIndex, new Handle<Quote>(),forwardStart);


             /*********************
              **  CURVE BUILDING **
              *********************/

             // Any DayCounter would be fine.
             // ActualActual::ISDA ensures that 30 years is 30.0

             // A depo-swap curve
             List<RateHelper> depoSwapInstruments = new List<RateHelper>();
             depoSwapInstruments.Add(d1w);
             depoSwapInstruments.Add(d1m);
             depoSwapInstruments.Add(d3m);
             depoSwapInstruments.Add(d6m);
             depoSwapInstruments.Add(d9m);
             depoSwapInstruments.Add(d1y);
             depoSwapInstruments.Add(s2y);
             depoSwapInstruments.Add(s3y);
             depoSwapInstruments.Add(s5y);
             depoSwapInstruments.Add(s10y);
             depoSwapInstruments.Add(s15y);
             YieldTermStructure depoSwapTermStructure = new PiecewiseYieldCurve<Discount,LogLinear>(
                             settlementDate, depoSwapInstruments,
                             termStructureDayCounter,
                             new List<Handle<Quote> >(),
                             new List<Date>(),
                             tolerance);

             // Term structures that will be used for pricing:
             // the one used for discounting cash flows
             RelinkableHandle<YieldTermStructure> discountingTermStructure = new RelinkableHandle<YieldTermStructure>();
             // the one used for forward rate forecasting
             RelinkableHandle<YieldTermStructure> forecastingTermStructure = new RelinkableHandle<YieldTermStructure>();

             /*********************
              * BONDS TO BE PRICED *
              **********************/

             // Common data
             double faceAmount = 100;

             // Pricing engine
             IPricingEngine bondEngine = new DiscountingBondEngine(discountingTermStructure);

             // Zero coupon bond
             ZeroCouponBond zeroCouponBond = new ZeroCouponBond(
                     settlementDays,
                     new UnitedStates(UnitedStates.Market.GovernmentBond),
                     faceAmount,
                     new Date(15, Month.August,2013),
                     BusinessDayConvention.Following,
                     116.92,
                     new Date(15, Month.August,2003));

             zeroCouponBond.setPricingEngine(bondEngine);

             // Fixed 4.5% US Treasury Note
             Schedule fixedBondSchedule = new Schedule(new Date(15, Month.May, 2007),
                     new Date(15,Month.May,2017), new Period(Frequency.Semiannual),
                     new UnitedStates(UnitedStates.Market.GovernmentBond),
                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false);

             FixedRateBond fixedRateBond = new FixedRateBond(
                     settlementDays,
                     faceAmount,
                     fixedBondSchedule,
                     new List<double>() { 0.045 },
                     new ActualActual(ActualActual.Convention.Bond),
                     BusinessDayConvention.ModifiedFollowing,
                     100.0, new Date(15, Month.May, 2007));

             fixedRateBond.setPricingEngine(bondEngine);

             // Floating rate bond (3M USD Libor + 0.1%)
             // Should and will be priced on another curve later...

             RelinkableHandle<YieldTermStructure> liborTermStructure = new RelinkableHandle<YieldTermStructure>();
             IborIndex libor3m = new USDLibor(new Period(3, TimeUnit.Months), liborTermStructure);
             libor3m.addFixing(new Date(17, Month.July, 2008),0.0278625);

             Schedule floatingBondSchedule = new Schedule(new Date(21, Month.October, 2005),
                     new Date(21, Month.October, 2010), new Period(Frequency.Quarterly),
                     new UnitedStates(UnitedStates.Market.NYSE),
                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, true);

             FloatingRateBond floatingRateBond = new FloatingRateBond(
                     settlementDays,
                     faceAmount,
                     floatingBondSchedule,
                     libor3m,
                     new Actual360(),
                     BusinessDayConvention.ModifiedFollowing,
                     2,
                     // Gearings
                     new List<double>() { 1.0 },
                     // Spreads
                     new List<double>() { 0.001 },
                     // Caps
                     new List<double>(),
                     // Floors
                     new List<double>(),
                     // Fixing in arrears
                     true,
                     100.0,
                     new Date(21, Month.October, 2005));

             floatingRateBond.setPricingEngine(bondEngine);

             // Coupon pricers
             IborCouponPricer pricer = new BlackIborCouponPricer();

             // optionLet volatilities
             double volatility = 0.0;
             Handle<OptionletVolatilityStructure> vol;
             vol = new Handle<OptionletVolatilityStructure>(
                                new ConstantOptionletVolatility(
                                     settlementDays,
                                     calendar,
                                     BusinessDayConvention.ModifiedFollowing,
                                     volatility,
                                     new Actual365Fixed()));

             pricer.setCapletVolatility(vol);
             Utils.setCouponPricer(floatingRateBond.cashflows(),pricer);

             // Yield curve bootstrapping
             forecastingTermStructure.linkTo(depoSwapTermStructure);
             discountingTermStructure.linkTo(bondDiscountingTermStructure);

             // We are using the depo & swap curve to estimate the future Libor rates
             liborTermStructure.linkTo(depoSwapTermStructure);

             /***************
              * BOND PRICING *
              ****************/

             // write column headings
             int[] widths = { 18, 10, 10, 10 };

            Console.WriteLine("{0,18}{1,10}{2,10}{3,10}", "", "ZC", "Fixed", "Floating");

            int width = widths[0]
                                 + widths[1]
                                          + widths[2]
                                                   + widths[3];
            string rule = "".PadLeft(width, '-'), dblrule = "".PadLeft(width, '=');
            string tab = "".PadLeft(8, ' ');

            Console.WriteLine(rule);

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
                floatingRateBond.yield(floatingRateBond.cleanPrice(),new Actual360(), Compounding.Compounded, Frequency.Annual,
                                       settlementDate));

            /* "Yield to Price"
               "Price to Yield" */

            Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer);
            Console.WriteLine();

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }
    }
}
