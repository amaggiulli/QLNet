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

namespace QLNet
{
   //! Pricing engine for double barrier european options using analytical formulae
   /*! The formulas are taken from "The complete guide to option pricing formulas 2nd Ed",
        E.G. Haug, McGraw-Hill, p.156 and following.
        Implements the Ikeda and Kunitomo series (see "Pricing Options with 
        Curved Boundaries" Mathematical Finance 2/1992").
        This code handles only flat barriers

       \ingroup barrierengines

       \note the formula holds only when strike is in the barrier range

       \test the correctness of the returned value is tested by
             reproducing results available in literature.
   */
   public class AnalyticDoubleBarrierEngine : DoubleBarrierOption.Engine
   {
      public AnalyticDoubleBarrierEngine(GeneralizedBlackScholesProcess process, int series = 5)
      {
         process_ = process;
         series_ = series;
         f_ = new CumulativeNormalDistribution();

         process_.registerWith(update);
      }
      public override void calculate()
      {
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=>
            "this engine handles only european options");

         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");

         double strike = payoff.strike();
         Utils.QL_REQUIRE(strike>0.0,()=> "strike must be positive");

         double spot = underlying();
         Utils.QL_REQUIRE(spot >= 0.0,()=> "negative or null underlying given");
         Utils.QL_REQUIRE(!triggered(spot),()=> "barrier(s) already touched");

         DoubleBarrier.Type barrierType = arguments_.barrierType;

         if (triggered(spot)) 
         {
            if (barrierType == DoubleBarrier.Type.KnockIn)
               results_.value = vanillaEquivalent();  // knocked in
            else
               results_.value = 0.0;  // knocked out
         } 
         else 
         {
            switch (payoff.optionType()) 
            {
               case Option.Type.Call:
                  switch (barrierType) 
                  {
                     case DoubleBarrier.Type.KnockIn:
                        results_.value = callKI();
                        break;
                     case DoubleBarrier.Type.KnockOut:
                        results_.value = callKO();
                        break;
                     case DoubleBarrier.Type.KIKO:
                     case DoubleBarrier.Type.KOKI:
                        Utils.QL_FAIL("unsupported double-barrier type: " + barrierType);
                        break;
                     default:
                        Utils.QL_FAIL("unknown double-barrier type: " + barrierType);
                        break;
                  }
                  break;
               case Option.Type.Put:
                  switch (barrierType) 
                  {
                     case DoubleBarrier.Type.KnockIn:
                        results_.value = putKI();
                        break;
                     case DoubleBarrier.Type.KnockOut:
                        results_.value = putKO();
                        break;
                     case DoubleBarrier.Type.KIKO:
                     case DoubleBarrier.Type.KOKI:
                        Utils.QL_FAIL("unsupported double-barrier type: " + barrierType);
                        break;
                     default:
                        Utils.QL_FAIL("unknown double-barrier type: " + barrierType);
                        break;
                  }
                  break;
               default:
                  Utils.QL_FAIL("unknown type");
                  break;
            }
         }
      }
      
      private GeneralizedBlackScholesProcess process_;
      private CumulativeNormalDistribution f_ ;
      private int series_;
      // helper methods
      private double underlying() { return process_.x0(); }
      private double strike()
      {
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");
         return payoff.strike();
      }
      private double residualTime() { return process_.time( arguments_.exercise.lastDate() ); }
      private double volatility() { return process_.blackVolatility().link.blackVol( residualTime(), strike() ); }
      private double volatilitySquared() { return volatility() * volatility(); }
      private double barrierLo() { return arguments_.barrier_lo.GetValueOrDefault(); }
      private double barrierHi() { return arguments_.barrier_hi.GetValueOrDefault(); }
      private double rebate() { return arguments_.rebate.GetValueOrDefault(); }
      private double stdDeviation() {return volatility() * Math.Sqrt(residualTime());}
      private double riskFreeRate()
      {
         return process_.riskFreeRate().link.zeroRate( 
            residualTime(), Compounding.Continuous,Frequency.NoFrequency ).value();
      }
      private double riskFreeDiscount() { return process_.riskFreeRate().link.discount( residualTime() ); }
      private double dividendYield()
      {
         return process_.dividendYield().link.zeroRate( 
            residualTime(),Compounding.Continuous, Frequency.NoFrequency ).value();
      }
      private double costOfCarry() { return riskFreeRate() - dividendYield(); }
      private double dividendDiscount() { return process_.dividendYield().link.discount( residualTime() ); }
      private double vanillaEquivalent() 
      {
         // Call KI equates to vanilla - callKO
         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
         double forwardPrice = underlying() * dividendDiscount() / riskFreeDiscount();
         BlackCalculator black = new BlackCalculator(payoff, forwardPrice, stdDeviation(), riskFreeDiscount());
         double vanilla = black.value();
         if (vanilla < 0.0)
            vanilla = 0.0;
         return vanilla;
      }
      private double callKO()
      {
         // N.B. for flat barriers mu3=mu1 and mu2=0
         double mu1 = 2 * costOfCarry() / volatilitySquared() + 1;
         double bsigma = (costOfCarry() + volatilitySquared() / 2.0) * residualTime() / stdDeviation();

         double acc1 = 0;
         double acc2 = 0;
         for (int n = -series_ ; n <= series_ ; ++n) 
         {
            double L2n = Math.Pow(barrierLo(), 2 * n);
            double U2n = Math.Pow(barrierHi(), 2 * n);
            double d1 = Math.Log( underlying()* U2n / (strike() * L2n) ) / stdDeviation() + bsigma;
            double d2 = Math.Log( underlying()* U2n / (barrierHi() * L2n) ) / stdDeviation() + bsigma;
            double d3 = Math.Log( Math.Pow(barrierLo(), 2 * n + 2) / (strike() * underlying() * U2n) ) / stdDeviation() + bsigma;
            double d4 = Math.Log( Math.Pow(barrierLo(), 2 * n + 2) / (barrierHi() * underlying() * U2n) ) / stdDeviation() + bsigma;

            acc1 += Math.Pow( Math.Pow(barrierHi(), n) / Math.Pow(barrierLo(), n), mu1 ) * 
                  (f_.value(d1) - f_.value(d2)) -
                  Math.Pow( Math.Pow(barrierLo(), n+1) / (Math.Pow(barrierHi(), n) * underlying()), mu1 ) * 
                  (f_.value(d3) - f_.value(d4));

            acc2 += Math.Pow( Math.Pow(barrierHi(), n) / Math.Pow(barrierLo(), n), mu1-2) * 
                  (f_.value(d1 - stdDeviation()) - f_.value(d2 - stdDeviation())) -
                  Math.Pow( Math.Pow(barrierLo(), n+1) / (Math.Pow(barrierHi(), n) * underlying()), mu1-2 ) * 
                  (f_.value(d3-stdDeviation()) - f_.value(d4-stdDeviation()));
         }

         double rend = Math.Exp(-dividendYield() * residualTime());
         double kov = underlying() * rend * acc1 - strike() * riskFreeDiscount() * acc2;
         return Math.Max(0.0, kov);

      }

      private double putKO()
      {
      
         double mu1 = 2 * costOfCarry() / volatilitySquared() + 1;
         double bsigma = (costOfCarry() + volatilitySquared() / 2.0) * residualTime() / stdDeviation();

         double acc1 = 0;
         double acc2 = 0;
         for (int n = -series_ ; n <= series_ ; ++n) {
            double L2n = Math.Pow(barrierLo(), 2 * n);
            double U2n = Math.Pow(barrierHi(), 2 * n);
            double y1 = Math.Log( underlying()* U2n / (Math.Pow(barrierLo(), 2 * n + 1)) ) / stdDeviation() + bsigma;
            double y2 = Math.Log( underlying()* U2n / (strike() * L2n) ) / stdDeviation() + bsigma;
            double y3 = Math.Log( Math.Pow(barrierLo(), 2 * n + 2) / (barrierLo() * underlying() * U2n) ) / stdDeviation() + bsigma;
            double y4 = Math.Log( Math.Pow(barrierLo(), 2 * n + 2) / (strike() * underlying() * U2n) ) / stdDeviation() + bsigma;

            acc1 += Math.Pow( Math.Pow(barrierHi(), n) / Math.Pow(barrierLo(), n), mu1-2) * 
                  (f_.value(y1 - stdDeviation()) - f_.value(y2 - stdDeviation())) -
                  Math.Pow( Math.Pow(barrierLo(), n+1) / (Math.Pow(barrierHi(), n) * underlying()), mu1-2 ) * 
                  (f_.value(y3-stdDeviation()) - f_.value(y4-stdDeviation()));

            acc2 += Math.Pow( Math.Pow(barrierHi(), n) / Math.Pow(barrierLo(), n), mu1 ) * 
                  (f_.value(y1) - f_.value(y2)) -
                  Math.Pow( Math.Pow(barrierLo(), n+1) / (Math.Pow(barrierHi(), n) * underlying()), mu1 ) * 
                  (f_.value(y3) - f_.value(y4));

         }

         double rend = Math.Exp(-dividendYield() * residualTime());
         double kov = strike() * riskFreeDiscount() * acc1 - underlying() * rend  * acc2;
         return Math.Max(0.0, kov);

      }

      private double callKI()
      {
         // Call KI equates to vanilla - callKO
         return Math.Max(0.0, vanillaEquivalent() - callKO());
      }

      private double putKI()
      {
         // Put KI equates to vanilla - putKO
         return Math.Max(0.0, vanillaEquivalent() - putKO());
      }
   }
}
