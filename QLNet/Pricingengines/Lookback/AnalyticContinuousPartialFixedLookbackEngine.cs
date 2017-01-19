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
   //! Pricing engine for European continuous partial-time fixed-strike lookback options
   /*! Formula from "Option Pricing Formulas, Second Edition",
       E.G. Haug, 2006, p.148
   */
   public class AnalyticContinuousPartialFixedLookbackEngine : ContinuousPartialFixedLookbackOption.Engine
   {
      public AnalyticContinuousPartialFixedLookbackEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         process_.registerWith(update);
      }
      public override void calculate()
      {
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "Non-plain payoff given");

         Utils.QL_REQUIRE(process_.x0() > 0.0,()=> "negative or null underlying");

         switch (payoff.optionType()) 
         {
            case Option.Type.Call:
               Utils.QL_REQUIRE(payoff.strike()>=0.0,()=>"Strike must be positive or null");
               results_.value = A(1);
               break;
            case Option.Type.Put:
               Utils.QL_REQUIRE(payoff.strike()>0.0,()=>"Strike must be positive");
               results_.value = A(-1);
               break;
            default:
               Utils.QL_FAIL("Unknown type");
               break;
         }
      }
      
      private GeneralizedBlackScholesProcess process_;
      private CumulativeNormalDistribution f_ = new CumulativeNormalDistribution();
      // helper methods
      private double underlying() {return process_.x0();}
      private double strike() 
      {
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "Non-plain payoff given");
         return payoff.strike();
      }
      private double residualTime() { return process_.time( arguments_.exercise.lastDate() ); }
      private double volatility() { return process_.blackVolatility().link.blackVol( residualTime(), strike() ); }
      private double lookbackPeriodStartTime() { return process_.time( arguments_.lookbackPeriodStart ); }
      private double stdDeviation() {return volatility() * Math.Sqrt(residualTime());}
      private double riskFreeRate()
      {
         return process_.riskFreeRate().link.zeroRate( residualTime(), 
            Compounding.Continuous,Frequency.NoFrequency ).value();
      }
      private double riskFreeDiscount() { return process_.riskFreeRate().link.discount( residualTime() ); }
      private double dividendYield()
      {
         return process_.dividendYield().link.zeroRate( residualTime(),
            Compounding.Continuous, Frequency.NoFrequency ).value();
      }
      private double dividendDiscount() { return process_.dividendYield().link.discount( residualTime() ); }
      private double A(double eta)
      {
         bool differentStartOfLookback = lookbackPeriodStartTime() != residualTime();
         double carry = riskFreeRate() - dividendYield();

         double vol = volatility();
         double x = 2.0*carry/(vol*vol);
         double s = underlying()/strike();
         double ls = Math.Log(s);
         double d1 = ls/stdDeviation() + 0.5*(x+1.0)*stdDeviation();
         double d2 = d1 - stdDeviation();

         double e1 = 0, e2 = 0;
         if (differentStartOfLookback)
         {
            e1 = (carry + vol * vol / 2) * (residualTime() - lookbackPeriodStartTime()) / (vol * Math.Sqrt(residualTime() - lookbackPeriodStartTime()));
            e2 = e1 - vol * Math.Sqrt(residualTime() - lookbackPeriodStartTime());
         } 

         double f1 = (ls + (carry + vol * vol / 2) * lookbackPeriodStartTime()) / (vol * Math.Sqrt(lookbackPeriodStartTime()));
         double f2 = f1 - vol * Math.Sqrt(lookbackPeriodStartTime());

         double n1 = f_.value(eta*d1);
         double n2 = f_.value(eta*d2);

         BivariateCumulativeNormalDistributionWe04DP cnbn1 = new BivariateCumulativeNormalDistributionWe04DP(-1), 
            cnbn2= new BivariateCumulativeNormalDistributionWe04DP(0), 
            cnbn3= new BivariateCumulativeNormalDistributionWe04DP(0);
         if (differentStartOfLookback) {
            cnbn1 = new BivariateCumulativeNormalDistributionWe04DP (-Math.Sqrt(lookbackPeriodStartTime() / residualTime()));
            cnbn2 = new BivariateCumulativeNormalDistributionWe04DP (Math.Sqrt(1 - lookbackPeriodStartTime() / residualTime()));
            cnbn3 = new BivariateCumulativeNormalDistributionWe04DP (-Math.Sqrt(1 - lookbackPeriodStartTime() / residualTime()));
         }

         double n3 = cnbn1.value(eta*(d1-x*stdDeviation()), eta*(-f1+2.0* carry * Math.Sqrt(lookbackPeriodStartTime()) / vol));
         double n4 = cnbn2.value(eta*e1, eta*d1);
         double n5 = cnbn3.value(-eta*e1, eta*d1);
         double n6 = cnbn1.value(eta*f2, -eta*d2);
         double n7 = f_.value(eta*f1);
         double n8 = f_.value(-eta*e2);

         double pow_s = Math.Pow(s, -x);
         double carryDiscount = Math.Exp(-carry * (residualTime() - lookbackPeriodStartTime()));
         return eta*(underlying() * dividendDiscount() * n1 
                     - strike() * riskFreeDiscount() * n2
                     + underlying() * riskFreeDiscount() / x 
                     * (-pow_s * n3 + dividendDiscount() / riskFreeDiscount() * n4)
                     - underlying() * dividendDiscount() * n5 
                     - strike() * riskFreeDiscount() * n6 
                     + carryDiscount * dividendDiscount() 
                     * (1 - 0.5 * vol * vol / carry) * 
                     underlying() * n7 * n8);
      }
   }
}
