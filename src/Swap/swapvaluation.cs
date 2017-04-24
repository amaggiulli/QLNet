/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)  
  
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

namespace Swap {
    /*  This example shows how to set up a Term Structure and then price a simple swap */
    class SwapValuation {
        static void Main(string[] args) {

            DateTime timer = DateTime.Now;

            /*********************
            ***  MARKET DATA  ***
            *********************/

            Calendar calendar = new TARGET();

            Date settlementDate = new Date(22, Month.September, 2004);
            // must be a business day
            settlementDate = calendar.adjust(settlementDate);

            int fixingDays = 2;
            Date todaysDate = calendar.advance(settlementDate, -fixingDays, TimeUnit.Days);
            // nothing to do with Date::todaysDate
            Settings.setEvaluationDate(todaysDate);


            todaysDate = Settings.evaluationDate();
            Console.WriteLine("Today: {0}, {1}", todaysDate.DayOfWeek, todaysDate);
            Console.WriteLine("Settlement date: {0}, {1}", settlementDate.DayOfWeek, settlementDate);


            // deposits
            double d1wQuote = 0.0382;
            double d1mQuote = 0.0372;
            double d3mQuote = 0.0363;
            double d6mQuote = 0.0353;
            double d9mQuote = 0.0348;
            double d1yQuote = 0.0345;
            // FRAs
            double fra3x6Quote = 0.037125;
            double fra6x9Quote = 0.037125;
            double fra6x12Quote = 0.037125;
            // futures
            double fut1Quote = 96.2875;
            double fut2Quote = 96.7875;
            double fut3Quote = 96.9875;
            double fut4Quote = 96.6875;
            double fut5Quote = 96.4875;
            double fut6Quote = 96.3875;
            double fut7Quote = 96.2875;
            double fut8Quote = 96.0875;
            // swaps
            double s2yQuote = 0.037125;
            double s3yQuote = 0.0398;
            double s5yQuote = 0.0443;
            double s10yQuote = 0.05165;
            double s15yQuote = 0.055175;


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
            // FRAs
            Quote fra3x6Rate = new SimpleQuote(fra3x6Quote);
            Quote fra6x9Rate = new SimpleQuote(fra6x9Quote);
            Quote fra6x12Rate = new SimpleQuote(fra6x12Quote);
            // futures
            Quote fut1Price = new SimpleQuote(fut1Quote);
            Quote fut2Price = new SimpleQuote(fut2Quote);
            Quote fut3Price = new SimpleQuote(fut3Quote);
            Quote fut4Price = new SimpleQuote(fut4Quote);
            Quote fut5Price = new SimpleQuote(fut5Quote);
            Quote fut6Price = new SimpleQuote(fut6Quote);
            Quote fut7Price = new SimpleQuote(fut7Quote);
            Quote fut8Price = new SimpleQuote(fut8Quote);
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

            RateHelper d1w = new DepositRateHelper(new Handle<Quote>(d1wRate), new Period(1, TimeUnit.Weeks),
                fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);
            RateHelper d1m = new DepositRateHelper(new Handle<Quote>(d1mRate), new Period(1, TimeUnit.Months),
                fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);
            RateHelper d3m = new DepositRateHelper(new Handle<Quote>(d3mRate), new Period(3, TimeUnit.Months), 
                fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);
            RateHelper d6m = new DepositRateHelper(new Handle<Quote>(d6mRate), new Period(6, TimeUnit.Months),
                fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);
            RateHelper d9m = new DepositRateHelper(new Handle<Quote>(d9mRate), new Period(9, TimeUnit.Months),
                fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);
            RateHelper d1y = new DepositRateHelper(new Handle<Quote>(d1yRate), new Period(1, TimeUnit.Years),
                fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);

            // setup FRAs
            RateHelper fra3x6 = new FraRateHelper(new Handle<Quote>(fra3x6Rate), 3, 6, fixingDays, calendar,
                        BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);
            RateHelper fra6x9 = new FraRateHelper(new Handle<Quote>(fra6x9Rate), 6, 9, fixingDays, calendar,
                        BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);
            RateHelper fra6x12 = new FraRateHelper(new Handle<Quote>(fra6x12Rate), 6, 12, fixingDays, calendar,
                        BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);


            // setup futures
            // Handle<Quote> convexityAdjustment = new Handle<Quote>(new SimpleQuote(0.0));
            int futMonths = 3;
            Date imm = IMM.nextDate(settlementDate);

            RateHelper fut1 = new FuturesRateHelper(new Handle<Quote>(fut1Price), imm, futMonths, calendar,
                    BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);

            imm = IMM.nextDate(imm + 1);
            RateHelper fut2 = new FuturesRateHelper(new Handle<Quote>(fut2Price), imm, futMonths, calendar,
                    BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);

            imm = IMM.nextDate(imm + 1);
            RateHelper fut3 = new FuturesRateHelper(new Handle<Quote>(fut3Price), imm, futMonths, calendar,
                    BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);

            imm = IMM.nextDate(imm + 1);
            RateHelper fut4 = new FuturesRateHelper(new Handle<Quote>(fut4Price), imm, futMonths, calendar,
                    BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);

            imm = IMM.nextDate(imm + 1);
            RateHelper fut5 = new FuturesRateHelper(new Handle<Quote>(fut5Price), imm, futMonths, calendar,
                    BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);

            imm = IMM.nextDate(imm + 1);
            RateHelper fut6 = new FuturesRateHelper(new Handle<Quote>(fut6Price), imm, futMonths, calendar,
                    BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);

            imm = IMM.nextDate(imm + 1);
            RateHelper fut7 = new FuturesRateHelper(new Handle<Quote>(fut7Price), imm, futMonths, calendar,
                    BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);

            imm = IMM.nextDate(imm + 1);
            RateHelper fut8 = new FuturesRateHelper(new Handle<Quote>(fut8Price), imm, futMonths, calendar,
                    BusinessDayConvention.ModifiedFollowing, true, depositDayCounter);


            // setup swaps
            Frequency swFixedLegFrequency = Frequency.Annual;
            BusinessDayConvention swFixedLegConvention = BusinessDayConvention.Unadjusted;
            DayCounter swFixedLegDayCounter = new Thirty360(Thirty360.Thirty360Convention.European);

            IborIndex swFloatingLegIndex = new Euribor6M();

            RateHelper s2y = new SwapRateHelper(new Handle<Quote>(s2yRate), new Period(2, TimeUnit.Years),
                calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex);
            RateHelper s3y = new SwapRateHelper(new Handle<Quote>(s3yRate), new Period(3, TimeUnit.Years), 
                calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex);
            RateHelper s5y = new SwapRateHelper(new Handle<Quote>(s5yRate), new Period(5, TimeUnit.Years), 
                calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex);
            RateHelper s10y = new SwapRateHelper(new Handle<Quote>(s10yRate), new Period(10, TimeUnit.Years), 
                calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex);
            RateHelper s15y = new SwapRateHelper(new Handle<Quote>(s15yRate), new Period(15, TimeUnit.Years), 
                calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex);



            /*********************
            **  CURVE BUILDING **
            *********************/

            // Any DayCounter would be fine.
            // ActualActual::ISDA ensures that 30 years is 30.0
            DayCounter termStructureDayCounter = new ActualActual(ActualActual.Convention.ISDA);

            double tolerance = 1.0e-15;

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
                        settlementDate, depoSwapInstruments, termStructureDayCounter, new List<Handle<Quote>>(), new List<Date>(), tolerance);


            // A depo-futures-swap curve
            List<RateHelper> depoFutSwapInstruments = new List<RateHelper>();
            depoFutSwapInstruments.Add(d1w);
            depoFutSwapInstruments.Add(d1m);
            depoFutSwapInstruments.Add(fut1);
            depoFutSwapInstruments.Add(fut2);
            depoFutSwapInstruments.Add(fut3);
            depoFutSwapInstruments.Add(fut4);
            depoFutSwapInstruments.Add(fut5);
            depoFutSwapInstruments.Add(fut6);
            depoFutSwapInstruments.Add(fut7);
            depoFutSwapInstruments.Add(fut8);
            depoFutSwapInstruments.Add(s3y);
            depoFutSwapInstruments.Add(s5y);
            depoFutSwapInstruments.Add(s10y);
            depoFutSwapInstruments.Add(s15y);
            YieldTermStructure depoFutSwapTermStructure = new PiecewiseYieldCurve<Discount,LogLinear>(
                    settlementDate, depoFutSwapInstruments, termStructureDayCounter, new List<Handle<Quote>>(), new List<Date>(), tolerance);


            // A depo-FRA-swap curve
            List<RateHelper> depoFRASwapInstruments = new List<RateHelper>();
            depoFRASwapInstruments.Add(d1w);
            depoFRASwapInstruments.Add(d1m);
            depoFRASwapInstruments.Add(d3m);
            depoFRASwapInstruments.Add(fra3x6);
            depoFRASwapInstruments.Add(fra6x9);
            depoFRASwapInstruments.Add(fra6x12);
            depoFRASwapInstruments.Add(s2y);
            depoFRASwapInstruments.Add(s3y);
            depoFRASwapInstruments.Add(s5y);
            depoFRASwapInstruments.Add(s10y);
            depoFRASwapInstruments.Add(s15y);
            YieldTermStructure depoFRASwapTermStructure = new PiecewiseYieldCurve<Discount,LogLinear>(
                    settlementDate, depoFRASwapInstruments, termStructureDayCounter, new List<Handle<Quote>>(), new List<Date>(), tolerance);

            // Term structures that will be used for pricing:
            // the one used for discounting cash flows
            RelinkableHandle<YieldTermStructure> discountingTermStructure = new RelinkableHandle<YieldTermStructure>();
            // the one used for forward rate forecasting
            RelinkableHandle<YieldTermStructure> forecastingTermStructure = new RelinkableHandle<YieldTermStructure>();


            /*********************
            * SWAPS TO BE PRICED *
            **********************/

            // constant nominal 1,000,000 Euro
            double nominal = 1000000.0;
            // fixed leg
            Frequency fixedLegFrequency = Frequency.Annual;
            BusinessDayConvention fixedLegConvention = BusinessDayConvention.Unadjusted;
            BusinessDayConvention floatingLegConvention = BusinessDayConvention.ModifiedFollowing;
            DayCounter fixedLegDayCounter = new Thirty360(Thirty360.Thirty360Convention.European);
            double fixedRate = 0.04;
            DayCounter floatingLegDayCounter = new Actual360();

            // floating leg
            Frequency floatingLegFrequency = Frequency.Semiannual;
            IborIndex euriborIndex = new Euribor6M(forecastingTermStructure);
            double spread = 0.0;

            int lenghtInYears = 5;
            VanillaSwap.Type swapType = VanillaSwap.Type.Payer;

            Date maturity = settlementDate + new Period(lenghtInYears, TimeUnit.Years);
            Schedule fixedSchedule = new Schedule(settlementDate, maturity, new Period(fixedLegFrequency),
                                     calendar, fixedLegConvention, fixedLegConvention, DateGeneration.Rule.Forward, false);
            Schedule floatSchedule = new Schedule(settlementDate, maturity, new Period(floatingLegFrequency),
                                     calendar, floatingLegConvention, floatingLegConvention, DateGeneration.Rule.Forward, false);
            VanillaSwap spot5YearSwap = new VanillaSwap(swapType, nominal, fixedSchedule, fixedRate, fixedLegDayCounter,
                                        floatSchedule, euriborIndex, spread, floatingLegDayCounter);

            Date fwdStart = calendar.advance(settlementDate, 1, TimeUnit.Years);
            Date fwdMaturity = fwdStart + new Period(lenghtInYears, TimeUnit.Years);
            Schedule fwdFixedSchedule = new Schedule(fwdStart, fwdMaturity, new Period(fixedLegFrequency), 
                                        calendar, fixedLegConvention, fixedLegConvention, DateGeneration.Rule.Forward, false);
            Schedule fwdFloatSchedule = new Schedule(fwdStart, fwdMaturity, new Period(floatingLegFrequency),
                                        calendar, floatingLegConvention, floatingLegConvention, DateGeneration.Rule.Forward, false);
            VanillaSwap oneYearForward5YearSwap = new VanillaSwap(swapType, nominal, fwdFixedSchedule, fixedRate, fixedLegDayCounter,
                                        fwdFloatSchedule, euriborIndex, spread, floatingLegDayCounter);


            /***************
            * SWAP PRICING *
            ****************/

            // utilities for reporting
            List<string> headers = new List<string>();
            headers.Add("term structure");
            headers.Add("net present value");
            headers.Add("fair spread");
            headers.Add("fair fixed rate");
            string separator = " | ";
            int width = headers[0].Length + separator.Length
                       + headers[1].Length + separator.Length
                       + headers[2].Length + separator.Length
                       + headers[3].Length + separator.Length - 1;
            string rule = string.Format("").PadLeft(width, '-'), dblrule = string.Format("").PadLeft(width, '=');
            string tab = string.Format("").PadLeft(8, ' ');

            // calculations

            Console.WriteLine(dblrule);
            Console.WriteLine("5-year market swap-rate = {0:0.00%}", s5yRate.value());
            Console.WriteLine(dblrule);

            Console.WriteLine(tab + "5-years swap paying {0:0.00%}", fixedRate);
            Console.WriteLine(headers[0] + separator
                      + headers[1] + separator
                      + headers[2] + separator
                      + headers[3] + separator);
            Console.WriteLine(rule);

            double NPV;
            double fairRate;
            double fairSpread;

            IPricingEngine swapEngine = new DiscountingSwapEngine(discountingTermStructure);

            spot5YearSwap.setPricingEngine(swapEngine);
            oneYearForward5YearSwap.setPricingEngine(swapEngine);

            // Of course, you're not forced to really use different curves
            forecastingTermStructure.linkTo(depoSwapTermStructure);
            discountingTermStructure.linkTo(depoSwapTermStructure);

            NPV = spot5YearSwap.NPV();
            fairSpread = spot5YearSwap.fairSpread();
            fairRate = spot5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            // let's check that the 5 years swap has been correctly re-priced
            if (!(Math.Abs(fairRate-s5yQuote)<1e-8))
                throw new ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate-s5yQuote));


            forecastingTermStructure.linkTo(depoFutSwapTermStructure);
            discountingTermStructure.linkTo(depoFutSwapTermStructure);

            NPV = spot5YearSwap.NPV();
            fairSpread = spot5YearSwap.fairSpread();
            fairRate = spot5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-fut-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            if (!(Math.Abs(fairRate-s5yQuote)<1e-8))
                throw new ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate-s5yQuote));

            forecastingTermStructure.linkTo(depoFRASwapTermStructure);
            discountingTermStructure.linkTo(depoFRASwapTermStructure);

            NPV = spot5YearSwap.NPV();
            fairSpread = spot5YearSwap.fairSpread();
            fairRate = spot5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-FRA-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            if (!(Math.Abs(fairRate-s5yQuote)<1e-8))
                throw new ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate-s5yQuote));

            Console.WriteLine(rule);

            // now let's price the 1Y forward 5Y swap
            Console.WriteLine(tab + "5-years, 1-year forward swap paying {0:0.00%}", fixedRate);
            Console.WriteLine(headers[0] + separator
                      + headers[1] + separator
                      + headers[2] + separator
                      + headers[3] + separator);
            Console.WriteLine(rule);

            forecastingTermStructure.linkTo(depoSwapTermStructure);
            discountingTermStructure.linkTo(depoSwapTermStructure);

            NPV = oneYearForward5YearSwap.NPV();
            fairSpread = oneYearForward5YearSwap.fairSpread();
            fairRate = oneYearForward5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            forecastingTermStructure.linkTo(depoFutSwapTermStructure);
            discountingTermStructure.linkTo(depoFutSwapTermStructure);

            NPV = oneYearForward5YearSwap.NPV();
            fairSpread = oneYearForward5YearSwap.fairSpread();
            fairRate = oneYearForward5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-fut-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            forecastingTermStructure.linkTo(depoFRASwapTermStructure);
            discountingTermStructure.linkTo(depoFRASwapTermStructure);

            NPV = oneYearForward5YearSwap.NPV();
            fairSpread = oneYearForward5YearSwap.fairSpread();
            fairRate = oneYearForward5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-FRA-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            // now let's say that the 5-years swap rate goes up to 4.60%.
            // A smarter market element--say, connected to a data source-- would
            // notice the change itself. Since we're using SimpleQuotes,
            // we'll have to change the value manually--which forces us to
            // downcast the handle and use the SimpleQuote
            // interface. In any case, the point here is that a change in the
            // value contained in the Quote triggers a new bootstrapping
            // of the curve and a repricing of the swap.

            SimpleQuote fiveYearsRate = s5yRate as SimpleQuote;
            fiveYearsRate.setValue(0.0460);

            Console.WriteLine(dblrule);
            Console.WriteLine("5-year market swap-rate = {0:0.00%}", s5yRate.value());
            Console.WriteLine(dblrule);

            Console.WriteLine(tab + "5-years swap paying {0:0.00%}", fixedRate);
            Console.WriteLine(headers[0] + separator
                      + headers[1] + separator
                      + headers[2] + separator
                      + headers[3] + separator);
            Console.WriteLine(rule);

            // now get the updated results
            forecastingTermStructure.linkTo(depoSwapTermStructure);
            discountingTermStructure.linkTo(depoSwapTermStructure);

            NPV = spot5YearSwap.NPV();
            fairSpread = spot5YearSwap.fairSpread();
            fairRate = spot5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            if (!(Math.Abs(fairRate-s5yRate.value())<1e-8))
                throw new ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate-s5yRate.value()));

            forecastingTermStructure.linkTo(depoFutSwapTermStructure);
            discountingTermStructure.linkTo(depoFutSwapTermStructure);

            NPV = spot5YearSwap.NPV();
            fairSpread = spot5YearSwap.fairSpread();
            fairRate = spot5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-fut-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            if (!(Math.Abs(fairRate-s5yRate.value())<1e-8))
                throw new ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate-s5yRate.value()));

            forecastingTermStructure.linkTo(depoFRASwapTermStructure);
            discountingTermStructure.linkTo(depoFRASwapTermStructure);

            NPV = spot5YearSwap.NPV();
            fairSpread = spot5YearSwap.fairSpread();
            fairRate = spot5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-FRA-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            if (!(Math.Abs(fairRate-s5yRate.value())<1e-8))
                throw new ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate-s5yRate.value()));

            Console.WriteLine(rule);

            // the 1Y forward 5Y swap changes as well

            Console.WriteLine(tab + "5-years, 1-year forward swap paying {0:0.00%}", fixedRate);
            Console.WriteLine(headers[0] + separator
                      + headers[1] + separator
                      + headers[2] + separator
                      + headers[3] + separator);
            Console.WriteLine(rule);

            forecastingTermStructure.linkTo(depoSwapTermStructure);
            discountingTermStructure.linkTo(depoSwapTermStructure);

            NPV = oneYearForward5YearSwap.NPV();
            fairSpread = oneYearForward5YearSwap.fairSpread();
            fairRate = oneYearForward5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            forecastingTermStructure.linkTo(depoFutSwapTermStructure);
            discountingTermStructure.linkTo(depoFutSwapTermStructure);

            NPV = oneYearForward5YearSwap.NPV();
            fairSpread = oneYearForward5YearSwap.fairSpread();
            fairRate = oneYearForward5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-fut-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);

            forecastingTermStructure.linkTo(depoFRASwapTermStructure);
            discountingTermStructure.linkTo(depoFRASwapTermStructure);

            NPV = oneYearForward5YearSwap.NPV();
            fairSpread = oneYearForward5YearSwap.fairSpread();
            fairRate = oneYearForward5YearSwap.fairRate();

            Console.Write("{0," + headers[0].Length + ":0.00}" + separator, "depo-FRA-swap");
            Console.Write("{0," + headers[1].Length + ":0.00}" + separator, NPV);
            Console.Write("{0," + headers[2].Length + ":0.00%}" + separator, fairSpread);
            Console.WriteLine("{0," + headers[3].Length + ":0.00%}" + separator, fairRate);


            Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer);

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }
    }
}
