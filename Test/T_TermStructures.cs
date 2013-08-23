/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
   public class T_TermStructures
   {
      public class CommonVars
      {
         #region Values
         public struct Datum
         {
            public int n;
            public TimeUnit units;
            public double rate;
         }

         public Datum[] depositData = new Datum[] {
                new Datum { n = 1, units = TimeUnit.Months, rate = 4.581 },
                new Datum { n = 2, units = TimeUnit.Months, rate = 4.573 },
                new Datum { n = 3, units = TimeUnit.Months, rate = 4.557 },
                new Datum { n = 6, units = TimeUnit.Months, rate = 4.496 },
                new Datum { n = 9, units = TimeUnit.Months, rate = 4.490 }
            };

         public Datum[] swapData = new Datum[] {
                new Datum { n =  1, units = TimeUnit.Years, rate = 4.54 },
                new Datum { n =  5, units = TimeUnit.Years, rate = 4.99 },
                new Datum { n = 10, units = TimeUnit.Years, rate = 5.47 },
                new Datum { n = 20, units = TimeUnit.Years, rate = 5.89 },
                new Datum { n = 30, units = TimeUnit.Years, rate = 5.96 }
            };
         #endregion

         // common data
         public Calendar calendar;
         public int settlementDays;
         public YieldTermStructure termStructure;
         public YieldTermStructure dummyTermStructure;

         // cleanup
         // SavedSettings backup;

         // setup
         public CommonVars()
         {
            calendar = new TARGET();
            settlementDays = 2;
            Date today = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(today);
            Date settlement = calendar.advance(today, settlementDays, TimeUnit.Days);

            int deposits = depositData.Length,
                swaps = swapData.Length;

            var instruments = new List<RateHelper>(deposits + swaps);
            for (int i = 0; i < deposits; i++)
            {
               instruments.Add(new DepositRateHelper(depositData[i].rate / 100, new Period(depositData[i].n, depositData[i].units),
                               settlementDays, calendar, BusinessDayConvention.ModifiedFollowing, true, new Actual360()));
            }

            IborIndex index = new IborIndex("dummy", new Period(6, TimeUnit.Months), settlementDays, new Currency(),
                                            calendar, BusinessDayConvention.ModifiedFollowing, false, new Actual360());
            for (int i = 0; i < swaps; ++i)
            {
               instruments.Add(new SwapRateHelper(swapData[i].rate / 100, new Period(swapData[i].n, swapData[i].units),
                               calendar, Frequency.Annual, BusinessDayConvention.Unadjusted, new Thirty360(), index));
            }
            termStructure = new PiecewiseYieldCurve<Discount, LogLinear>(settlement, instruments, new Actual360());
            dummyTermStructure = new PiecewiseYieldCurve<Discount, LogLinear>(settlement, instruments, new Actual360());
         }
      }

      [TestMethod()]
      public void testReferenceChange()
      {
         // ("Testing term structure against evaluation date change...");

         CommonVars vars = new CommonVars();

         SimpleQuote flatRate = new SimpleQuote();
         Handle<Quote> flatRateHandle = new Handle<Quote>(flatRate);
         vars.termStructure = new FlatForward(vars.settlementDays, new NullCalendar(), flatRateHandle, new Actual360());
         flatRate.setValue(.03);

         int[] days = new int[] { 10, 30, 60, 120, 360, 720 };

         Date today = Settings.evaluationDate();
         List<double> expected = new InitializedList<double>(days.Length);
         for (int i = 0; i < days.Length; i++)
            expected[i] = vars.termStructure.discount(today + days[i]);

         Settings.setEvaluationDate(today + 30);
         List<double> calculated = new InitializedList<double>(days.Length);
         for (int i = 0; i < days.Length; i++)
            calculated[i] = vars.termStructure.discount(today + 30 + days[i]);

         for (int i = 0; i < days.Length; i++)
         {
            if (!Utils.close(expected[i], calculated[i]))
               Console.WriteLine("\n  Discount at " + days[i] + " days:\n"
                           + "    before date change: " + expected[i] + "\n"
                           + "    after date change:  " + calculated[i]);
         }
      }

      [TestMethod()]
      public void testImplied()
      {
         // ("Testing consistency of implied term structure...");

         CommonVars vars = new CommonVars();

         double tolerance = 1.0e-10;
         Date today = Settings.evaluationDate();
         Date newToday = today + new Period(3, TimeUnit.Years);
         Date newSettlement = vars.calendar.advance(newToday, vars.settlementDays, TimeUnit.Days);
         Date testDate = newSettlement + new Period(5, TimeUnit.Years);
         YieldTermStructure implied = new ImpliedTermStructure(new Handle<YieldTermStructure>(vars.termStructure), newSettlement);
         double baseDiscount = vars.termStructure.discount(newSettlement);
         double discount = vars.termStructure.discount(testDate);
         double impliedDiscount = implied.discount(testDate);
         if (Math.Abs(discount - baseDiscount * impliedDiscount) > tolerance)
            Console.WriteLine("unable to reproduce discount from implied curve\n"
                + "    calculated: " + baseDiscount * impliedDiscount + "\n"
                + "    expected:   " + discount);
      }

      [TestMethod()]
      public void testImpliedObs()
      {
         // ("Testing observability of implied term structure...");

         CommonVars vars = new CommonVars();

         Date today = Settings.evaluationDate();
         Date newToday = today + new Period(3, TimeUnit.Years);
         Date newSettlement = vars.calendar.advance(newToday, vars.settlementDays, TimeUnit.Days);
         RelinkableHandle<YieldTermStructure> h = new RelinkableHandle<YieldTermStructure>();
         YieldTermStructure implied = new ImpliedTermStructure(h, newSettlement);
         Flag flag = new Flag();
         implied.registerWith(flag.update);
         h.linkTo(vars.termStructure);
         if (!flag.isUp())
            Console.WriteLine("Observer was not notified of term structure change");
      }

      [TestMethod()]
      public void testFSpreaded()
      {
         //("Testing consistency of forward-spreaded term structure...");
         CommonVars vars = new CommonVars();

         double tolerance = 1.0e-10;
         Quote me = new SimpleQuote(0.01);
         Handle<Quote> mh = new Handle<Quote>(me);
         YieldTermStructure spreaded = new ForwardSpreadedTermStructure(new Handle<YieldTermStructure>(vars.termStructure), mh);
         Date testDate = vars.termStructure.referenceDate() + new Period(5, TimeUnit.Years);
         DayCounter tsdc = vars.termStructure.dayCounter();
         DayCounter sprdc = spreaded.dayCounter();
         double forward = vars.termStructure.forwardRate(testDate, testDate, tsdc, Compounding.Continuous,
                                                         Frequency.NoFrequency).rate();
         double spreadedForward = spreaded.forwardRate(testDate, testDate, sprdc, Compounding.Continuous,
                                                         Frequency.NoFrequency).rate();
         if (Math.Abs(forward - (spreadedForward - me.value())) > tolerance)
            Console.WriteLine("unable to reproduce forward from spreaded curve\n"
                + "    calculated: "
                + (spreadedForward - me.value()) + "\n"
                + "    expected:   " + forward);
      }

      [TestMethod()]
      public void testFSpreadedObs()
      {
         // ("Testing observability of forward-spreaded term structure...");

         CommonVars vars = new CommonVars();

         SimpleQuote me = new SimpleQuote(0.01);
         Handle<Quote> mh = new Handle<Quote>(me);
         RelinkableHandle<YieldTermStructure> h = new RelinkableHandle<YieldTermStructure>(); //(vars.dummyTermStructure);
         YieldTermStructure spreaded = new ForwardSpreadedTermStructure(h, mh);
         Flag flag = new Flag();
         spreaded.registerWith(flag.update);
         h.linkTo(vars.termStructure);
         if (!flag.isUp())
            Console.WriteLine("Observer was not notified of term structure change");
         flag.lower();
         me.setValue(0.005);
         if (!flag.isUp())
            Console.WriteLine("Observer was not notified of spread change");
      }

      [TestMethod()]
      public void testZSpreaded()
      {
         // ("Testing consistency of zero-spreaded term structure...");

         CommonVars vars = new CommonVars();

         double tolerance = 1.0e-10;
         Quote me = new SimpleQuote(0.01);
         Handle<Quote> mh = new Handle<Quote>(me);
         YieldTermStructure spreaded = new ZeroSpreadedTermStructure(new Handle<YieldTermStructure>(vars.termStructure), mh);
         Date testDate = vars.termStructure.referenceDate() + new Period(5, TimeUnit.Years);
         DayCounter rfdc = vars.termStructure.dayCounter();
         double zero = vars.termStructure.zeroRate(testDate, rfdc, Compounding.Continuous, Frequency.NoFrequency).rate();
         double spreadedZero = spreaded.zeroRate(testDate, rfdc, Compounding.Continuous, Frequency.NoFrequency).rate();
         if (Math.Abs(zero - (spreadedZero - me.value())) > tolerance)
            Console.WriteLine("unable to reproduce zero yield from spreaded curve\n"
                + "    calculated: " + (spreadedZero - me.value()) + "\n"
                + "    expected:   " + zero);
      }

      [TestMethod()]
      public void testZSpreadedObs()
      {
         // ("Testing observability of zero-spreaded term structure...");

         CommonVars vars = new CommonVars();

         SimpleQuote me = new SimpleQuote(0.01);
         Handle<Quote> mh = new Handle<Quote>(me);
         RelinkableHandle<YieldTermStructure> h = new RelinkableHandle<YieldTermStructure>(vars.dummyTermStructure);

         YieldTermStructure spreaded = new ZeroSpreadedTermStructure(h, mh);
         Flag flag = new Flag();
         spreaded.registerWith(flag.update);
         h.linkTo(vars.termStructure);
         if (!flag.isUp())
            Console.WriteLine("Observer was not notified of term structure change");
         flag.lower();
         me.setValue(0.005);
         if (!flag.isUp())
            Console.WriteLine("Observer was not notified of spread change");
      }

      public void suite()
      {
         testReferenceChange();
         testImplied();
         testImpliedObs();
         testFSpreaded();
         testFSpreadedObs();
         testZSpreaded();
         testZSpreadedObs();
      }
   }
}
