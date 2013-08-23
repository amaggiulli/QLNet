/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
 This file is part of QLNet Project http://www.qlnet.org

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://trac2.assembla.com/QLNet/wiki/License>.
  
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

namespace CallableBonds
{
   class CallableBonds
   {
      
      static YieldTermStructure flatRate(Date today,
                                  double forward,
                                  DayCounter dc,
                                  Compounding compounding,
                                  Frequency frequency) 
   
      {
         return new FlatForward(today, forward, dc, compounding, frequency);
         //FlatForward flatRate = new FlatForward(settlementDate, r.rate(), r.dayCounter(), r.compounding(), r.frequency());
      }

      static void Main(string[] args)
      {
         // boost::timer timer;

         Date today = new Date(16,Month.October,2007);
         Settings.setEvaluationDate(today);

         Console.WriteLine();
         Console.WriteLine("Pricing a callable fixed rate bond using");
         Console.WriteLine("Hull White model w/ reversion parameter = 0.03");
         Console.WriteLine("BAC4.65 09/15/12  ISIN: US06060WBJ36");
         Console.WriteLine("roughly five year tenor, ");
         Console.WriteLine("quarterly coupon and call dates");
         Console.WriteLine("reference date is : " + today );

         /* Bloomberg OAS1: "N" model (Hull White)
           varying volatility parameter

           The curve entered into Bloomberg OAS1 is a flat curve,
           at constant yield = 5.5%, semiannual compounding.
           Assume here OAS1 curve uses an ACT/ACT day counter,
           as documented in PFC1 as a "default" in the latter case.
         */

         // set up a flat curve corresponding to Bloomberg flat curve

         double bbCurveRate = 0.055;
         DayCounter bbDayCounter = new ActualActual(ActualActual.Convention.Bond);
         InterestRate bbIR = new InterestRate(bbCurveRate,bbDayCounter,Compounding.Compounded ,Frequency.Semiannual);

         Handle<YieldTermStructure> termStructure = new Handle<YieldTermStructure>(flatRate( today,
                                                              bbIR.rate(),
                                                              bbIR.dayCounter(),
                                                              bbIR.compounding(),
                                                              bbIR.frequency()));
         // set up the call schedule

         CallabilitySchedule callSchedule = new CallabilitySchedule();
         double callPrice = 100.0;
         int numberOfCallDates = 24;
         Date callDate = new Date(15,Month.September,2006);

         for (int i=0; i< numberOfCallDates; i++) 
         {
            Calendar nullCalendar = new NullCalendar();

            Callability.Price myPrice = new Callability.Price(callPrice, Callability.Price.Type.Clean);
            callSchedule.Add( new Callability(myPrice,Callability.Type.Call, callDate ));
            callDate = nullCalendar.advance(callDate, 3, TimeUnit.Months);
         }

         // set up the callable bond

         Date dated = new Date(16,Month.September,2004);
         Date issue = dated;
         Date maturity = new Date(15,Month.September,2012);
         int settlementDays = 3;  // Bloomberg OAS1 settle is Oct 19, 2007
         Calendar bondCalendar = new UnitedStates(UnitedStates.Market.GovernmentBond);
         double coupon = .0465;
         Frequency frequency = Frequency.Quarterly;
         double redemption = 100.0;
         double faceAmount = 100.0;

         /* The 30/360 day counter Bloomberg uses for this bond cannot
            reproduce the US Bond/ISMA (constant) cashflows used in PFC1.
            Therefore use ActAct(Bond)
         */
         DayCounter bondDayCounter = new ActualActual(ActualActual.Convention.Bond);

         // PFC1 shows no indication dates are being adjusted
         // for weekends/holidays for vanilla bonds
         BusinessDayConvention accrualConvention = BusinessDayConvention.Unadjusted;
         BusinessDayConvention paymentConvention = BusinessDayConvention.Unadjusted;

         Schedule sch = new Schedule( dated, maturity, new Period(frequency), bondCalendar,
                                      accrualConvention, accrualConvention,
                                      DateGeneration.Rule.Backward, false);

         int maxIterations = 1000;
         double accuracy = 1e-8;
         int gridIntervals = 40;
         double reversionParameter = .03;

         // output price/yield results for varying volatility parameter

         double sigma = Const.QL_Epsilon; // core dumps if zero on Cygwin

         ShortRateModel hw0 = new HullWhite(termStructure,reversionParameter,sigma);

         IPricingEngine engine0 = new TreeCallableFixedRateBondEngine(hw0, gridIntervals, termStructure);

         CallableFixedRateBond callableBond = new CallableFixedRateBond( settlementDays, faceAmount, sch,
                                                                         new InitializedList<double>(1, coupon),
                                                                         bondDayCounter, paymentConvention,
                                                                         redemption, issue, callSchedule);
         callableBond.setPricingEngine(engine0);

         Console.WriteLine("sigma/vol (%) = {0:0.00}", (100.0 * sigma));

         Console.WriteLine("QuantLib price/yld (%)  ");
         Console.WriteLine(  "{0:0.00} / {1:0.00} ", callableBond.cleanPrice() ,
                                                     100.0 * callableBond.yield(bondDayCounter,
                                                                                Compounding.Compounded,
                                                                                frequency,
                                                                                accuracy,
                                                                                maxIterations));
         Console.WriteLine("Bloomberg price/yld (%) ");
         Console.WriteLine("96.50 / 5.47");
             
         //
         
         sigma = .01;

         Console.WriteLine("sigma/vol (%) = {0:0.00}", (100.0 * sigma));

         ShortRateModel hw1 = new HullWhite(termStructure,reversionParameter,sigma);

         IPricingEngine engine1 = new TreeCallableFixedRateBondEngine(hw1,gridIntervals,termStructure);

         callableBond.setPricingEngine(engine1);

         Console.WriteLine("QuantLib price/yld (%)  ");
         Console.WriteLine(  "{0:0.00} / {1:0.00} ", callableBond.cleanPrice() ,
                                                     100.0 * callableBond.yield(bondDayCounter,
                                                                                Compounding.Compounded,
                                                                                frequency,
                                                                                accuracy,
                                                                                maxIterations));

         Console.WriteLine("Bloomberg price/yld (%) ");
         Console.WriteLine("95.68 / 5.66");

         //

         sigma = .03;

         Console.WriteLine("sigma/vol (%) = {0:0.00}", (100.0 * sigma));

         ShortRateModel hw2 = new HullWhite(termStructure, reversionParameter, sigma);

         IPricingEngine engine2 = new TreeCallableFixedRateBondEngine(hw2, gridIntervals, termStructure);

         callableBond.setPricingEngine(engine2);

         Console.WriteLine("QuantLib price/yld (%)  ");
         Console.WriteLine("{0:0.00} / {1:0.00} ", callableBond.cleanPrice(),
                                                     100.0 * callableBond.yield(bondDayCounter,
                                                                                Compounding.Compounded,
                                                                                frequency,
                                                                                accuracy,
                                                                                maxIterations));

         Console.WriteLine("Bloomberg price/yld (%) ");
         Console.WriteLine("92.34 / 6.49");

         //

         sigma = .06;

         Console.WriteLine("sigma/vol (%) = {0:0.00}", (100.0 * sigma));

         ShortRateModel hw3 = new HullWhite(termStructure, reversionParameter, sigma);

         IPricingEngine engine3 = new TreeCallableFixedRateBondEngine(hw3, gridIntervals, termStructure);

         callableBond.setPricingEngine(engine3);

         Console.WriteLine("QuantLib price/yld (%)  ");
         Console.WriteLine("{0:0.00} / {1:0.00} ", callableBond.cleanPrice(),
                                                     100.0 * callableBond.yield(bondDayCounter,
                                                                                Compounding.Compounded,
                                                                                frequency,
                                                                                accuracy,
                                                                                maxIterations));

         Console.WriteLine("Bloomberg price/yld (%) ");
         Console.WriteLine("87.16 / 7.83");
         
         //

         sigma = .12;

         Console.WriteLine("sigma/vol (%) = {0:0.00}", (100.0 * sigma));

         ShortRateModel hw4 = new HullWhite(termStructure, reversionParameter, sigma);

         IPricingEngine engine4 = new TreeCallableFixedRateBondEngine(hw4, gridIntervals, termStructure);

         callableBond.setPricingEngine(engine4);

         Console.WriteLine("QuantLib price/yld (%)  ");
         Console.WriteLine("{0:0.00} / {1:0.00} ", callableBond.cleanPrice(),
                                                     100.0 * callableBond.yield(bondDayCounter,
                                                                                Compounding.Compounded,
                                                                                frequency,
                                                                                accuracy,
                                                                                maxIterations));

         Console.WriteLine("Bloomberg price/yld (%) ");
         Console.WriteLine("77.31 / 10.65");
      }
   }
}
