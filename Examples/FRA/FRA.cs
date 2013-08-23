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

namespace FRA {
    class FRA {
        static void Main() {
            
            DateTime timer = DateTime.Now;

            /*********************
             ***  MARKET DATA  ***
             *********************/

            RelinkableHandle<YieldTermStructure> euriborTermStructure = new RelinkableHandle<YieldTermStructure>();
            IborIndex euribor3m = new Euribor3M(euriborTermStructure);

            Date todaysDate = new Date(23, Month.May, 2006);
            Settings.setEvaluationDate(todaysDate);

            Calendar calendar = euribor3m.fixingCalendar();
            int fixingDays = euribor3m.fixingDays();
            Date settlementDate = calendar.advance(todaysDate, fixingDays, TimeUnit.Days);

            Console.WriteLine("Today: " + todaysDate.DayOfWeek + ", " + todaysDate);
            Console.WriteLine("Settlement date: " + settlementDate.DayOfWeek + ", " + settlementDate);


            // 3 month term FRA quotes (index refers to monthsToStart)
            double[] threeMonthFraQuote = new double[10];

            threeMonthFraQuote[1]=0.030;
            threeMonthFraQuote[2]=0.031;
            threeMonthFraQuote[3]=0.032;
            threeMonthFraQuote[6]=0.033;
            threeMonthFraQuote[9]=0.034;

            /********************
             ***    QUOTES    ***
             ********************/

            // SimpleQuote stores a value which can be manually changed;
            // other Quote subclasses could read the value from a database
            // or some kind of data feed.


            // FRAs
            SimpleQuote fra1x4Rate = new SimpleQuote(threeMonthFraQuote[1]);
            SimpleQuote fra2x5Rate = new SimpleQuote(threeMonthFraQuote[2]);
            SimpleQuote fra3x6Rate = new SimpleQuote(threeMonthFraQuote[3]);
            SimpleQuote fra6x9Rate = new SimpleQuote(threeMonthFraQuote[6]);
            SimpleQuote fra9x12Rate = new SimpleQuote(threeMonthFraQuote[9]);

            RelinkableHandle<Quote> h1x4 = new RelinkableHandle<Quote>();  h1x4.linkTo(fra1x4Rate);
            RelinkableHandle<Quote> h2x5 = new RelinkableHandle<Quote>();  h2x5.linkTo(fra2x5Rate);
            RelinkableHandle<Quote> h3x6 = new RelinkableHandle<Quote>();  h3x6.linkTo(fra3x6Rate);
            RelinkableHandle<Quote> h6x9 = new RelinkableHandle<Quote>();  h6x9.linkTo(fra6x9Rate);
            RelinkableHandle<Quote> h9x12 = new RelinkableHandle<Quote>(); h9x12.linkTo(fra9x12Rate);

            /*********************
             ***  RATE HELPERS ***
             *********************/

            // RateHelpers are built from the above quotes together with
            // other instrument dependant infos.  Quotes are passed in
            // relinkable handles which could be relinked to some other
            // data source later.

            DayCounter fraDayCounter = euribor3m.dayCounter();
            BusinessDayConvention convention = euribor3m.businessDayConvention();
            bool endOfMonth = euribor3m.endOfMonth();

            RateHelper fra1x4 =  new FraRateHelper(h1x4, 1, 4,
                                                 fixingDays, calendar, convention,
                                                 endOfMonth, fraDayCounter);

            RateHelper fra2x5 = new FraRateHelper(h2x5, 2, 5,
                                                 fixingDays, calendar, convention,
                                                 endOfMonth, fraDayCounter);

            RateHelper fra3x6 = new FraRateHelper(h3x6, 3, 6,
                                                 fixingDays, calendar, convention,
                                                 endOfMonth, fraDayCounter);

            RateHelper fra6x9 = new FraRateHelper(h6x9, 6, 9,
                                                 fixingDays, calendar, convention,
                                                 endOfMonth, fraDayCounter);

            RateHelper fra9x12 = new FraRateHelper(h9x12, 9, 12,
                                                 fixingDays, calendar, convention,
                                                 endOfMonth, fraDayCounter);


            /*********************
             **  CURVE BUILDING **
             *********************/

            // Any DayCounter would be fine.
            // ActualActual::ISDA ensures that 30 years is 30.0
            DayCounter termStructureDayCounter = new ActualActual(ActualActual.Convention.ISDA);

            double tolerance = 1.0e-15;

            // A FRA curve
            List<RateHelper> fraInstruments = new List<RateHelper>();

            fraInstruments.Add(fra1x4);
            fraInstruments.Add(fra2x5);
            fraInstruments.Add(fra3x6);
            fraInstruments.Add(fra6x9);
            fraInstruments.Add(fra9x12);

            YieldTermStructure fraTermStructure = new PiecewiseYieldCurve<Discount,LogLinear>(
                                             settlementDate, fraInstruments, termStructureDayCounter,
                                             new List<Handle<Quote>>(), new List<Date>(), tolerance);


            // Term structures used for pricing/discounting
            RelinkableHandle<YieldTermStructure> discountingTermStructure = new RelinkableHandle<YieldTermStructure>();
            discountingTermStructure.linkTo(fraTermStructure);


            /***********************
             ***  construct FRA's ***
             ***********************/

            Calendar fraCalendar = euribor3m.fixingCalendar();
            BusinessDayConvention fraBusinessDayConvention = euribor3m.businessDayConvention();
            Position.Type fraFwdType = Position.Type.Long;
            double fraNotional = 100.0;
            const int FraTermMonths = 3;
            int[] monthsToStart = new [] { 1, 2, 3, 6, 9 };

            euriborTermStructure.linkTo(fraTermStructure);

            Console.WriteLine("\nTest FRA construction, NPV calculation, and FRA purchase\n");

            int i;
            for (i=0; i<monthsToStart.Length; i++) {

                Date fraValueDate = fraCalendar.advance(
                                           settlementDate,monthsToStart[i], TimeUnit.Months,
                                           fraBusinessDayConvention);

                Date fraMaturityDate = fraCalendar.advance(
                                                fraValueDate, FraTermMonths, TimeUnit.Months,
                                                fraBusinessDayConvention);

                double fraStrikeRate = threeMonthFraQuote[monthsToStart[i]];

                ForwardRateAgreement myFRA = new ForwardRateAgreement(fraValueDate, fraMaturityDate,
                                           fraFwdType,fraStrikeRate,
                                           fraNotional, euribor3m,
                                           discountingTermStructure);

                Console.WriteLine("3m Term FRA, Months to Start: " + monthsToStart[i]);

                Console.WriteLine("strike FRA rate: {0:0.00%}", fraStrikeRate);
                Console.WriteLine("FRA 3m forward rate: {0:0.00%}", myFRA.forwardRate());
                Console.WriteLine("FRA market quote: {0:0.00%}", threeMonthFraQuote[monthsToStart[i]]);
                Console.WriteLine("FRA spot value: " + myFRA.spotValue());
                Console.WriteLine("FRA forward value: " + myFRA.forwardValue());
                Console.WriteLine("FRA implied Yield: {0:0.00%}",
                     myFRA.impliedYield(myFRA.spotValue(), myFRA.forwardValue(), settlementDate, Compounding.Simple, fraDayCounter));
                Console.WriteLine("market Zero Rate: {0:0.00%}",
                     discountingTermStructure.link.zeroRate(fraMaturityDate, fraDayCounter, Compounding.Simple));
                Console.WriteLine("FRA NPV [should be zero]: {0}\n", myFRA.NPV());
            }



            Console.WriteLine("\n");
            Console.WriteLine("Now take a 100 basis-point upward shift in FRA quotes and examine NPV\n");


            const double BpsShift = 0.01;

            threeMonthFraQuote[1]=0.030+BpsShift;
            threeMonthFraQuote[2]=0.031+BpsShift;
            threeMonthFraQuote[3]=0.032+BpsShift;
            threeMonthFraQuote[6]=0.033+BpsShift;
            threeMonthFraQuote[9]=0.034+BpsShift;

            fra1x4Rate.setValue(threeMonthFraQuote[1]);
            fra2x5Rate.setValue(threeMonthFraQuote[2]);
            fra3x6Rate.setValue(threeMonthFraQuote[3]);
            fra6x9Rate.setValue(threeMonthFraQuote[6]);
            fra9x12Rate.setValue(threeMonthFraQuote[9]);


            for (i=0; i<monthsToStart.Length; i++) {

                Date fraValueDate = fraCalendar.advance(
                                           settlementDate, monthsToStart[i], TimeUnit.Months,
                                           fraBusinessDayConvention);

                Date fraMaturityDate = fraCalendar.advance(
                                                fraValueDate, FraTermMonths, TimeUnit.Months,
                                                fraBusinessDayConvention);

                double fraStrikeRate = threeMonthFraQuote[monthsToStart[i]] - BpsShift;

                ForwardRateAgreement myFRA = new ForwardRateAgreement(fraValueDate, fraMaturityDate,
                                           fraFwdType, fraStrikeRate,
                                           fraNotional, euribor3m,
                                           discountingTermStructure);

                Console.WriteLine("3m Term FRA, 100 notional, Months to Start: " + monthsToStart[i]);
                Console.WriteLine("strike FRA rate: {0:0.00%}", fraStrikeRate);
                Console.WriteLine("FRA 3m forward rate: {0:0.00%}", myFRA.forwardRate());
                Console.WriteLine("FRA market quote: {0:0.00%}", threeMonthFraQuote[monthsToStart[i]]);
                Console.WriteLine("FRA spot value: " + myFRA.spotValue());
                Console.WriteLine("FRA forward value: " + myFRA.forwardValue());
                Console.WriteLine("FRA implied Yield: {0:0.00%}",
                     myFRA.impliedYield(myFRA.spotValue(), myFRA.forwardValue(), settlementDate, Compounding.Simple, fraDayCounter));
                Console.WriteLine("market Zero Rate: {0:0.00%}",
                     discountingTermStructure.link.zeroRate(fraMaturityDate, fraDayCounter, Compounding.Simple));
                Console.WriteLine("FRA NPV [should be positive]: {0}\n", myFRA.NPV());
            }

            Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer);
            Console.WriteLine();

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }
    }
}
