//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
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
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite
{
   #if QL_DOTNET_FRAMEWORK
      [TestClass()]
   #endif
   public class T_BinaryOption
   {
      private void REPORT_FAILURE( string greekName,
                                   StrikedTypePayoff payoff,
                                   Exercise exercise,
                                   Barrier.Type barrierType,
                                   double barrier,
                                   double s,
                                   double q,
                                   double r,
                                   Date today,
                                   double v,
                                   double expected,
                                   double calculated,
                                   double error,
                                   double tolerance )
      {
         QAssert.Fail( payoff.optionType() + " option with " 
                  + barrierType + " barrier type:\n"
                  + "    barrier:          " + barrier + "\n"
                  + payoff + " payoff:\n"
                  + exercise + " "
                  + payoff.optionType() 
                  + "    spot value: " + s + "\n"
                  + "    strike:           " + payoff.strike() + "\n"
                  + "    dividend yield:   " + q + "\n"
                  + "    risk-free rate:   " + r + "\n"
                  + "    reference date:   " + today + "\n"
                  + "    maturity:         " + exercise.lastDate() + "\n"
                  + "    volatility:       " + v + "\n\n"
                  + "    expected " + greekName + ":   " + expected + "\n"
                  + "    calculated " + greekName + ": " + calculated + "\n"
                  + "    error:            " + error + "\n"
                  + "    tolerance:        " + tolerance );
      }

      private struct BinaryOptionData
      {
         public Barrier.Type barrierType;
         public double barrier;
         public double cash;     // cash payoff for cash-or-nothing
         public Option.Type type;
         public double strike;
         public double s;        // spot
         public double q;        // dividend
         public double r;        // risk-free rate
         public double t;        // time to maturity
         public double v;  // volatility
         public double result;   // expected result
         public double tol;      // tolerance

         public BinaryOptionData(Barrier.Type barrierType, double barrier, double cash, Option.Type type, double strike,
            double s, double q, double r, double t, double v, double result, double tol) : this()
         {
            this.barrierType = barrierType;
            this.barrier = barrier;
            this.cash = cash;
            this.type = type;
            this.strike = strike;
            this.s = s;
            this.q = q;
            this.r = r;
            this.t = t;
            this.v = v;
            this.result = result;
            this.tol = tol;
         }
      }

      #if QL_DOTNET_FRAMEWORK
              [TestMethod()]
      #else
             [Fact]
      #endif
      public void testCashOrNothingHaugValues() 
      {
         // Testing cash-or-nothing barrier options against Haug's values

         BinaryOptionData[] values = {
            /* The data below are from
               "Option pricing formulas 2nd Ed.", E.G. Haug, McGraw-Hill 2007 pag. 180 - cases 13,14,17,18,21,22,25,26
               Note:
               q is the dividend rate, while the book gives b, the cost of carry (q=r-b)
            */
            //    barrierType, barrier,  cash,         type, strike,   spot,    q,    r,   t,  vol,   value, tol
            new BinaryOptionData( Barrier.Type.DownIn,  100.00, 15.00, Option.Type.Call, 102.00, 105.00, 0.00, 0.10, 0.5, 0.20,  4.9289, 1e-4 ), 
            new BinaryOptionData( Barrier.Type.DownIn,  100.00, 15.00, Option.Type.Call,  98.00, 105.00, 0.00, 0.10, 0.5, 0.20,  6.2150, 1e-4 ),
            // following value is wrong in book. 
            new BinaryOptionData( Barrier.Type.UpIn,    100.00, 15.00, Option.Type.Call, 102.00,  95.00, 0.00, 0.10, 0.5, 0.20,  5.8926, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00, 15.00, Option.Type.Call,  98.00,  95.00, 0.00, 0.10, 0.5, 0.20,  7.4519, 1e-4 ),
            // 17,18
            new BinaryOptionData( Barrier.Type.DownIn,  100.00, 15.00, Option.Type.Put,  102.00, 105.00, 0.00, 0.10, 0.5, 0.20,  4.4314, 1e-4 ),
            new BinaryOptionData( Barrier.Type.DownIn,  100.00, 15.00, Option.Type.Put,   98.00, 105.00, 0.00, 0.10, 0.5, 0.20,  3.1454, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00, 15.00, Option.Type.Put,  102.00,  95.00, 0.00, 0.10, 0.5, 0.20,  5.3297, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00, 15.00, Option.Type.Put,   98.00,  95.00, 0.00, 0.10, 0.5, 0.20,  3.7704, 1e-4 ),
            // 21,22
            new BinaryOptionData( Barrier.Type.DownOut, 100.00, 15.00, Option.Type.Call, 102.00, 105.00, 0.00, 0.10, 0.5, 0.20,  4.8758, 1e-4 ),
            new BinaryOptionData( Barrier.Type.DownOut, 100.00, 15.00, Option.Type.Call,  98.00, 105.00, 0.00, 0.10, 0.5, 0.20,  4.9081, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00, 15.00, Option.Type.Call, 102.00,  95.00, 0.00, 0.10, 0.5, 0.20,  0.0000, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00, 15.00, Option.Type.Call,  98.00,  95.00, 0.00, 0.10, 0.5, 0.20,  0.0407, 1e-4 ),
            // 25,26
            new BinaryOptionData( Barrier.Type.DownOut, 100.00, 15.00, Option.Type.Put,  102.00, 105.00, 0.00, 0.10, 0.5, 0.20,  0.0323, 1e-4 ),
            new BinaryOptionData( Barrier.Type.DownOut, 100.00, 15.00, Option.Type.Put,   98.00, 105.00, 0.00, 0.10, 0.5, 0.20,  0.0000, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00, 15.00, Option.Type.Put,  102.00,  95.00, 0.00, 0.10, 0.5, 0.20,  3.0461, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00, 15.00, Option.Type.Put,   98.00,  95.00, 0.00, 0.10, 0.5, 0.20,  3.0054, 1e-4 ),

            // other values calculated with book vba
            new BinaryOptionData( Barrier.Type.UpIn,    100.00, 15.00, Option.Type.Call, 102.00,  95.00,-0.14, 0.10, 0.5, 0.20,  8.6806, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00, 15.00, Option.Type.Call, 102.00,  95.00, 0.03, 0.10, 0.5, 0.20,  5.3112, 1e-4 ),
            // degenerate conditions (barrier touched)
            new BinaryOptionData( Barrier.Type.DownIn,  100.00, 15.00, Option.Type.Call,  98.00,  95.00, 0.00, 0.10, 0.5, 0.20,  7.4926, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00, 15.00, Option.Type.Call,  98.00, 105.00, 0.00, 0.10, 0.5, 0.20, 11.1231, 1e-4 ),
            // 17,18
            new BinaryOptionData( Barrier.Type.DownIn,  100.00, 15.00, Option.Type.Put,  102.00,  98.00, 0.00, 0.10, 0.5, 0.20,  7.1344, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00, 15.00, Option.Type.Put,  102.00, 101.00, 0.00, 0.10, 0.5, 0.20,  5.9299, 1e-4 ),
            // 21,22
            new BinaryOptionData( Barrier.Type.DownOut, 100.00, 15.00, Option.Type.Call,  98.00,  99.00, 0.00, 0.10, 0.5, 0.20,  0.0000, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00, 15.00, Option.Type.Call,  98.00, 101.00, 0.00, 0.10, 0.5, 0.20,  0.0000, 1e-4 ),
            // 25,26
            new BinaryOptionData( Barrier.Type.DownOut, 100.00, 15.00, Option.Type.Put,   98.00,  99.00, 0.00, 0.10, 0.5, 0.20,  0.0000, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00, 15.00, Option.Type.Put,   98.00, 101.00, 0.00, 0.10, 0.5, 0.20,  0.0000, 1e-4 ),
         };

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(100.0);
         SimpleQuote qRate = new SimpleQuote(0.04);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.01);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.25);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         for (int i=0; i<values.Length; i++) 
         {
            StrikedTypePayoff payoff = new CashOrNothingPayoff(values[i].type, values[i].strike, values[i].cash);

            Date exDate = today + Convert.ToInt32(values[i].t*360+0.5);
            Exercise amExercise = new AmericanExercise(today,exDate,true);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
               new Handle<Quote>(spot),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));
            
            IPricingEngine engine = new AnalyticBinaryBarrierEngine(stochProcess);

            BarrierOption opt = new BarrierOption(values[i].barrierType,values[i].barrier, 0,payoff,amExercise);

            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated-values[i].result);
            if (error > values[i].tol) {
               REPORT_FAILURE("value", payoff, amExercise, values[i].barrierType, 
                              values[i].barrier, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              values[i].result, calculated, error, values[i].tol);
            }
         }
      }

      #if QL_DOTNET_FRAMEWORK
              [TestMethod()]
      #else
             [Fact]
      #endif
      public void testAssetOrNothingHaugValues() 
      {
         // Testing asset-or-nothing barrier options against Haug's values

         BinaryOptionData[] values = {
            /* The data below are from
               "Option pricing formulas 2nd Ed.", E.G. Haug, McGraw-Hill 2007 pag. 180 - cases 15,16,19,20,23,24,27,28
               Note:
               q is the dividend rate, while the book gives b, the cost of carry (q=r-b)
            */
            //    barrierType, barrier,  cash,         type, strike,   spot,    q,    r,   t,  vol,   value, tol
            new BinaryOptionData( Barrier.Type.DownIn,  100.00,  0.00, Option.Type.Call, 102.00, 105.00, 0.00, 0.10, 0.5, 0.20, 37.2782, 1e-4 ),
            new BinaryOptionData( Barrier.Type.DownIn,  100.00,  0.00, Option.Type.Call,  98.00, 105.00, 0.00, 0.10, 0.5, 0.20, 45.8530, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00,  0.00, Option.Type.Call, 102.00,  95.00, 0.00, 0.10, 0.5, 0.20, 44.5294, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00,  0.00, Option.Type.Call,  98.00,  95.00, 0.00, 0.10, 0.5, 0.20, 54.9262, 1e-4 ),
            // 19,20
            new BinaryOptionData( Barrier.Type.DownIn,  100.00,  0.00, Option.Type.Put,  102.00, 105.00, 0.00, 0.10, 0.5, 0.20, 27.5644, 1e-4 ),
            new BinaryOptionData( Barrier.Type.DownIn,  100.00,  0.00, Option.Type.Put,   98.00, 105.00, 0.00, 0.10, 0.5, 0.20, 18.9896, 1e-4 ),
            // following value is wrong in book. 
            new BinaryOptionData( Barrier.Type.UpIn,    100.00,  0.00, Option.Type.Put,  102.00,  95.00, 0.00, 0.10, 0.5, 0.20, 33.1723, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpIn,    100.00,  0.00, Option.Type.Put,   98.00,  95.00, 0.00, 0.10, 0.5, 0.20, 22.7755, 1e-4 ),
            // 23,24
            new BinaryOptionData( Barrier.Type.DownOut, 100.00,  0.00, Option.Type.Call, 102.00, 105.00, 0.00, 0.10, 0.5, 0.20, 39.9391, 1e-4 ),
            new BinaryOptionData( Barrier.Type.DownOut, 100.00,  0.00, Option.Type.Call,  98.00, 105.00, 0.00, 0.10, 0.5, 0.20, 40.1574, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00,  0.00, Option.Type.Call, 102.00,  95.00, 0.00, 0.10, 0.5, 0.20,  0.0000, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00,  0.00, Option.Type.Call,  98.00,  95.00, 0.00, 0.10, 0.5, 0.20,  0.2676, 1e-4 ),
            // 27,28
            new BinaryOptionData( Barrier.Type.DownOut, 100.00,  0.00, Option.Type.Put,  102.00, 105.00, 0.00, 0.10, 0.5, 0.20,  0.2183, 1e-4 ),
            new BinaryOptionData( Barrier.Type.DownOut, 100.00,  0.00, Option.Type.Put,   98.00, 105.00, 0.00, 0.10, 0.5, 0.20,  0.0000, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00,  0.00, Option.Type.Put,  102.00,  95.00, 0.00, 0.10, 0.5, 0.20, 17.2983, 1e-4 ),
            new BinaryOptionData( Barrier.Type.UpOut,   100.00,  0.00, Option.Type.Put,   98.00,  95.00, 0.00, 0.10, 0.5, 0.20, 17.0306, 1e-4 ),
         };

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(100.0);
         SimpleQuote qRate = new SimpleQuote(0.04);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.01);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.25);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         for (int i=0; i<values.Length; i++) 
         {
            StrikedTypePayoff payoff = new AssetOrNothingPayoff(values[i].type, values[i].strike);
            Date exDate = today + Convert.ToInt32(values[i].t*360+0.5);
            Exercise amExercise = new AmericanExercise(today,exDate,true);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
               new Handle<Quote>(spot),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));
           
            IPricingEngine engine = new AnalyticBinaryBarrierEngine(stochProcess);

            BarrierOption opt = new BarrierOption(values[i].barrierType,values[i].barrier, 0,payoff,amExercise);

            opt.setPricingEngine(engine);

            double calculated = opt.NPV();
            double error = Math.Abs(calculated-values[i].result);
            if (error > values[i].tol) 
            {
               REPORT_FAILURE("value", payoff, amExercise, values[i].barrierType, 
                              values[i].barrier, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              values[i].result, calculated, error, values[i].tol);
            }
         }
      }  
   }
}
