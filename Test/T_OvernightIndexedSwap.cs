/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   [TestClass()]
   public class T_OvernightIndexedSwap
   {
      public struct Datum
      {
         public int settlementDays;
         public int n;
         public TimeUnit unit;
         public double rate;
      };

      public struct FraDatum
      {
         public int settlementDays;
         public int nExpiry;
         public int nMaturity;
         public double rate;
         public FraDatum(int settlementDays_, int nExpiry_, int nMaturity_, double rate_)
         {
            settlementDays = settlementDays_;
            nExpiry = nExpiry_;
            nMaturity = nMaturity_;
            rate = rate_;
         }

      };

      public struct SwapDatum
      {
         public int settlementDays;
         public int nIndexUnits;
         public TimeUnit indexUnit;
         public int nTermUnits;
         public TimeUnit termUnit;
         public double rate;
         public SwapDatum(int settlementDays_, int nIndexUnits_, TimeUnit indexUnit_, int nTermUnits_, TimeUnit termUnit_, double rate_)
         {
            settlementDays = settlementDays_;
            nIndexUnits = nIndexUnits_;
            indexUnit = indexUnit_;
            nTermUnits = nTermUnits_;
            termUnit = termUnit_;
            rate = rate_;
         }

      };

      Datum[] depositData = new Datum[] {
        new Datum{ settlementDays = 0, n = 1, unit = TimeUnit.Days, rate = 1.10 },
        new Datum{ settlementDays = 1, n = 1, unit = TimeUnit.Days, rate = 1.10 },
        new Datum{ settlementDays = 2, n = 1, unit = TimeUnit.Weeks, rate = 1.40 },
        new Datum{ settlementDays = 2, n = 2, unit = TimeUnit.Weeks, rate = 1.50 },
        new Datum{ settlementDays = 2, n = 1, unit = TimeUnit.Months, rate = 1.70 },
        new Datum{ settlementDays = 2, n = 2, unit = TimeUnit.Months, rate = 1.90 },
        new Datum{ settlementDays = 2, n = 3, unit = TimeUnit.Months, rate = 2.05 },
        new Datum{ settlementDays = 2, n = 4, unit = TimeUnit.Months, rate = 2.08 },
        new Datum{ settlementDays = 2, n = 5, unit = TimeUnit.Months, rate = 2.11 },
        new Datum{ settlementDays = 2, n = 6, unit = TimeUnit.Months, rate = 2.13 }
      };

      Datum[] eoniaSwapData = new Datum[]{
        new Datum{ settlementDays = 2, n = 1, unit =  TimeUnit.Weeks, rate = 1.245 },
        new Datum{ settlementDays = 2,n =   2, unit = TimeUnit.Weeks, rate = 1.269 },
        new Datum{ settlementDays = 2,n =   3, unit = TimeUnit.Weeks, rate = 1.277 },
        new Datum{ settlementDays = 2,n =   1, unit = TimeUnit.Months, rate = 1.281 },
        new Datum{ settlementDays = 2,n =   2, unit = TimeUnit.Months, rate = 1.18 },
        new Datum{ settlementDays = 2,n =   3, unit = TimeUnit.Months, rate = 1.143 },
        new Datum{ settlementDays = 2,n =   4, unit = TimeUnit.Months, rate = 1.125 },
        new Datum{ settlementDays = 2,n =   5, unit = TimeUnit.Months, rate = 1.116 },
        new Datum{ settlementDays = 2,n =   6, unit = TimeUnit.Months, rate = 1.111 },
        new Datum{ settlementDays = 2,n =   7, unit = TimeUnit.Months, rate = 1.109 },
        new Datum{ settlementDays = 2,n =   8, unit = TimeUnit.Months, rate = 1.111 },
        new Datum{ settlementDays = 2,n =   9, unit = TimeUnit.Months, rate = 1.117 },
        new Datum{ settlementDays = 2,n =  10, unit = TimeUnit.Months, rate = 1.129 },
        new Datum{ settlementDays = 2,n =  11, unit = TimeUnit.Months, rate = 1.141 },
        new Datum{ settlementDays = 2,n =  12, unit = TimeUnit.Months, rate = 1.153 },
        new Datum{ settlementDays = 2,n =  15, unit = TimeUnit.Months, rate = 1.218 },
        new Datum{ settlementDays = 2,n =  18, unit = TimeUnit.Months, rate = 1.308 },
        new Datum{ settlementDays = 2,n =  21, unit = TimeUnit.Months, rate = 1.407 },
        new Datum{ settlementDays = 2,n =   2,  unit = TimeUnit.Years, rate = 1.510 },
        new Datum{ settlementDays = 2,n =   3,  unit = TimeUnit.Years, rate = 1.916 },
        new Datum{ settlementDays = 2,n =   4,  unit = TimeUnit.Years, rate = 2.254 },
        new Datum{ settlementDays = 2,n =   5,  unit = TimeUnit.Years, rate = 2.523 },
        new Datum{ settlementDays = 2,n =   6,  unit = TimeUnit.Years, rate = 2.746 },
        new Datum{ settlementDays = 2,n =   7,  unit = TimeUnit.Years, rate = 2.934 },
        new Datum{ settlementDays = 2,n =   8,  unit = TimeUnit.Years, rate = 3.092 },
        new Datum{ settlementDays = 2,n =   9,  unit = TimeUnit.Years, rate = 3.231 },
        new Datum{ settlementDays = 2,n =  10,  unit = TimeUnit.Years, rate = 3.380 },
        new Datum{ settlementDays = 2,n =  11,  unit = TimeUnit.Years, rate = 3.457 },
        new Datum{ settlementDays = 2,n =  12,  unit = TimeUnit.Years, rate = 3.544 },
        new Datum{ settlementDays = 2,n =  15,  unit = TimeUnit.Years, rate = 3.702 },
        new Datum{ settlementDays = 2,n =  20,  unit = TimeUnit.Years, rate = 3.703 },
        new Datum{ settlementDays = 2,n =  25,  unit = TimeUnit.Years, rate = 3.541 },
        new Datum{ settlementDays = 2,n =  30,  unit = TimeUnit.Years, rate = 3.369 }
      };

      FraDatum[] fraData = {
         new FraDatum( 2, 3, 6, 1.728 ),
         new FraDatum( 2, 6, 9, 1.702 )
      };

      SwapDatum[] swapData = {
        new SwapDatum( 2, 3, TimeUnit.Months,  1, TimeUnit.Years, 1.867 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 15, TimeUnit.Months, 1.879 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 18, TimeUnit.Months, 1.934 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 21, TimeUnit.Months, 2.005 ),
        new SwapDatum( 2, 3, TimeUnit.Months,  2, TimeUnit.Years, 2.091 ),
        new SwapDatum( 2, 3, TimeUnit.Months,  3, TimeUnit.Years, 2.435 ),
        new SwapDatum( 2, 3, TimeUnit.Months,  4, TimeUnit.Years, 2.733 ),
        new SwapDatum( 2, 3, TimeUnit.Months,  5, TimeUnit.Years, 2.971 ),
        new SwapDatum( 2, 3, TimeUnit.Months,  6, TimeUnit.Years, 3.174 ),
        new SwapDatum( 2, 3, TimeUnit.Months,  7, TimeUnit.Years, 3.345 ),
        new SwapDatum( 2, 3, TimeUnit.Months,  8, TimeUnit.Years, 3.491 ),
        new SwapDatum( 2, 3, TimeUnit.Months,  9, TimeUnit.Years, 3.620 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 10, TimeUnit.Years, 3.733 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 12, TimeUnit.Years, 3.910 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 15, TimeUnit.Years, 4.052 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 20, TimeUnit.Years, 4.073 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 25, TimeUnit.Years, 3.844 ),
        new SwapDatum( 2, 3, TimeUnit.Months, 30, TimeUnit.Years, 3.687 )
      };

      public class CommonVars
      {
         // global data
         public Date today, settlement;
         public OvernightIndexedSwap.Type type;
         public double nominal;
         public Calendar calendar;
         public int settlementDays;

         public Period fixedEoniaPeriod, floatingEoniaPeriod;
         public DayCounter fixedEoniaDayCount;
         public BusinessDayConvention fixedEoniaConvention, floatingEoniaConvention;
         public Eonia eoniaIndex;
         public RelinkableHandle<YieldTermStructure> eoniaTermStructure = new RelinkableHandle<YieldTermStructure>();

         public Frequency fixedSwapFrequency;
         public DayCounter fixedSwapDayCount;
         public BusinessDayConvention fixedSwapConvention;
         public IborIndex swapIndex;
         public RelinkableHandle<YieldTermStructure> swapTermStructure = new RelinkableHandle<YieldTermStructure>();

         // cleanup
         public SavedSettings backup;

         // utilities
         public OvernightIndexedSwap makeSwap(Period length,
                                       double fixedRate,
                                       double spread)
         {
            return new MakeOIS(length, eoniaIndex, fixedRate)
                .withEffectiveDate(settlement)
                .withOvernightLegSpread(spread)
                .withNominal(nominal)
                .withDiscountingTermStructure(eoniaTermStructure);
         }
         
         public CommonVars() 
         {
            type = OvernightIndexedSwap.Type.Payer;
            settlementDays = 2;
            nominal = 100.0;
            fixedEoniaConvention = BusinessDayConvention.ModifiedFollowing;
            floatingEoniaConvention = BusinessDayConvention.ModifiedFollowing;
            fixedEoniaPeriod = new Period(1, TimeUnit.Years);
            floatingEoniaPeriod = new Period(1, TimeUnit.Years);
            fixedEoniaDayCount = new Actual360();
            eoniaIndex = new Eonia(eoniaTermStructure);
            fixedSwapConvention = BusinessDayConvention.ModifiedFollowing;
            fixedSwapFrequency = Frequency.Annual;
            fixedSwapDayCount = new Thirty360();
            swapIndex = (IborIndex) new Euribor3M(swapTermStructure);
            calendar = eoniaIndex.fixingCalendar();
            today = new Date(5, Month.February, 2009);
            //today = calendar.adjust(Date::todaysDate());
            Settings.setEvaluationDate(today);
            settlement = calendar.advance(today,new Period(settlementDays,TimeUnit.Days),BusinessDayConvention.Following);
            eoniaTermStructure.linkTo(Utilities.flatRate(settlement, 0.05,new Actual365Fixed()));
        }
      }


      [TestMethod()]
      public void testFairRate()
      {
         // Testing Eonia-swap calculation of fair fixed rate...

         CommonVars vars = new CommonVars();

         Period[] lengths = new Period[] { new Period(1, TimeUnit.Years), new Period(2, TimeUnit.Years), new Period(5, TimeUnit.Years), new Period(10, TimeUnit.Years), new Period(20, TimeUnit.Years) };
         double[] spreads = { -0.001, -0.01, 0.0, 0.01, 0.001 };

         for (int i = 0; i < lengths.Length; i++)
         {
            for (int j = 0; j < spreads.Length; j++)
            {
               OvernightIndexedSwap swap = vars.makeSwap(lengths[i], 0.0, spreads[j]);

               swap = vars.makeSwap(lengths[i], swap.fairRate().Value, spreads[j]);

               if (Math.Abs(swap.NPV()) > 1.0e-10)
               {

                  Assert.Fail("recalculating with implied rate:\n"
                            + "    length: " + lengths[i] + " \n"
                            + "    floating spread: "
                            + (spreads[j]) + "\n"
                            + "    swap value: " + swap.NPV());

               }
            }
         }
      }

      [TestMethod()]
      public void testFairSpread() 
      {

         // Testing Eonia-swap calculation of fair floating spread...
         CommonVars vars = new CommonVars();

         Period[] lengths = { new Period(1,TimeUnit.Years), 
                              new Period(2,TimeUnit.Years), 
                              new Period(5,TimeUnit.Years), 
                              new Period(10,TimeUnit.Years), 
                              new Period(20,TimeUnit.Years) };
         double[] rates = { 0.04, 0.05, 0.06, 0.07 };

         for (int i=0; i<lengths.Length; i++) 
         {
            for (int j=0; j<rates.Length; j++) 
            {
               OvernightIndexedSwap swap = vars.makeSwap(lengths[i], rates[j], 0.0);
               double? fairSpread = swap.fairSpread();
               swap = vars.makeSwap(lengths[i], rates[j], fairSpread.Value);

               if (Math.Abs(swap.NPV()) > 1.0e-10) 
               {
                   Assert.Fail("Recalculating with implied spread:" +
                               "\n     length: " + lengths[i] +
                               "\n fixed rate: " + rates[j] +
                               "\nfair spread: " + fairSpread +
                               "\n swap value: " + swap.NPV());
               }
            }
         }
      }
      
      [TestMethod()]
      public void testCachedValue() 
      {
         // Testing Eonia-swap calculation against cached value...
         CommonVars vars = new CommonVars();

         Settings.setEvaluationDate(vars.today);
         vars.settlement = vars.calendar.advance(vars.today,vars.settlementDays,TimeUnit.Days);
         double flat = 0.05;
         vars.eoniaTermStructure.linkTo(Utilities.flatRate(vars.settlement,flat,new Actual360()));
         double fixedRate = Math.Exp(flat) - 1;
         OvernightIndexedSwap swap = vars.makeSwap(new Period(1,TimeUnit.Years), fixedRate, 0.0);
         double cachedNPV   = 0.001730450147;
         double tolerance = 1.0e-11;
    
         if (Math.Abs(swap.NPV()-cachedNPV) > tolerance)
            Assert.Fail("\nfailed to reproduce cached swap value:" +
                        "\ncalculated: " + swap.NPV() +
                        "\n  expected: " + cachedNPV +
                        "\n tolerance:" + tolerance);
      }

      [TestMethod()]
      public void testBootstrap() 
      {
         // Testing Eonia-swap curve building...
         CommonVars vars = new CommonVars();

         List<RateHelper> eoniaHelpers = new List<RateHelper>();
         List<RateHelper> swap3mHelpers = new List<RateHelper>();

         IborIndex euribor3m = new Euribor3M();
         Eonia eonia = new Eonia();

         for (int i = 0; i < depositData.Length; i++) 
         {
            double rate = 0.01 * depositData[i].rate;
            SimpleQuote simple = new SimpleQuote(rate);
            Handle<Quote> quote = new Handle<Quote>(simple);

            Period term = new Period(depositData[i].n , depositData[i].unit);
            RateHelper helper = new DepositRateHelper(quote,
                                         term,
                                         depositData[i].settlementDays,
                                         euribor3m.fixingCalendar(),
                                         euribor3m.businessDayConvention(),
                                         euribor3m.endOfMonth(),
                                         euribor3m.dayCounter());

         
            if (term <= new Period(2,TimeUnit.Days))
               eoniaHelpers.Add(helper);
            if (term <= new Period(3,TimeUnit.Months))
               swap3mHelpers.Add(helper);
         }


         for (int i = 0; i < fraData.Length; i++) 
         {
        
            double rate = 0.01 * fraData[i].rate;
            SimpleQuote simple = new SimpleQuote(rate);
            Handle<Quote> quote = new Handle<Quote>(simple);
            RateHelper helper = new FraRateHelper(quote,
                                             fraData[i].nExpiry,
                                             fraData[i].nMaturity,
                                             fraData[i].settlementDays,
                                             euribor3m.fixingCalendar(),
                                             euribor3m.businessDayConvention(),
                                             euribor3m.endOfMonth(),
                                             euribor3m.dayCounter());
            swap3mHelpers.Add(helper);
         }

         for (int i = 0; i < eoniaSwapData.Length; i++) 
         {
        
            double rate = 0.01 * eoniaSwapData[i].rate;
            SimpleQuote simple = new SimpleQuote(rate);
            Handle<Quote> quote = new Handle<Quote>(simple);
            Period term = new Period(eoniaSwapData[i].n , eoniaSwapData[i].unit);
            RateHelper helper = new OISRateHelper(eoniaSwapData[i].settlementDays,
                                                       term,
                                                       quote,
                                                       eonia);
            eoniaHelpers.Add(helper);
         }


         for (int i = 0; i < swapData.Length; i++) 
         {
            double rate = 0.01 * swapData[i].rate;
            SimpleQuote simple = new SimpleQuote(rate);
            Handle<Quote> quote = new Handle<Quote>(simple);
            Period tenor = new Period(swapData[i].nIndexUnits , swapData[i].indexUnit);
            Period term = new Period(swapData[i].nTermUnits , swapData[i].termUnit);

            RateHelper helper = new SwapRateHelper(quote,
                                                        term,
                                                        vars.calendar,
                                                        vars.fixedSwapFrequency,
                                                        vars.fixedSwapConvention,
                                                        vars.fixedSwapDayCount,
                                                        euribor3m);
            if (tenor == new Period(3,TimeUnit.Months))
               swap3mHelpers.Add(helper);
         }


         PiecewiseYieldCurve<Discount, LogLinear> eoniaTS = new PiecewiseYieldCurve<Discount, LogLinear>(vars.today, 
                                                                  eoniaHelpers, 
                                                                  new Actual365Fixed());

         PiecewiseYieldCurve<Discount, LogLinear> swapTS = new PiecewiseYieldCurve<Discount, LogLinear>(vars.today, 
                                                                 swap3mHelpers, 
                                                                 new Actual365Fixed());

         vars.eoniaTermStructure.linkTo(eoniaTS);

         // test curve consistency
         double tolerance = 1.0e-10;
         for (int i = 0; i < eoniaSwapData.Length; i++) 
         {
        
            double expected = eoniaSwapData[i].rate;
            Period term = new Period(eoniaSwapData[i].n , eoniaSwapData[i].unit);
            OvernightIndexedSwap swap = vars.makeSwap(term, 0.0, 0.0);
            double? calculated = 100.0 * swap.fairRate();

            if (Math.Abs(expected-calculated.Value) > tolerance)
               Assert.Fail("curve inconsistency:\n"
                           + "    swap length:     " + term + "\n"
                           + "    quoted rate:     " + expected + "\n"
                           + "    calculated rate: " + calculated);
         }
      }
   }
}
