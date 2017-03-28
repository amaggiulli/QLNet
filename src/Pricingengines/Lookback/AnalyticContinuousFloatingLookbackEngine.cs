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
   //! Pricing engine for European continuous floating-strike lookback
   /*! Formula from "Option Pricing Formulas",
       E.G. Haug, McGraw-Hill, 1998, p.61-62
   */
   public class AnalyticContinuousFloatingLookbackEngine : ContinuousFloatingLookbackOption.Engine
   {
      public AnalyticContinuousFloatingLookbackEngine(GeneralizedBlackScholesProcess process)
      {
         process_=process;
         process_.registerWith(update);
      }
      public override void calculate()
      {
         FloatingTypePayoff payoff = arguments_.payoff as FloatingTypePayoff;
         Utils.QL_REQUIRE(payoff != null,()=> "Non-floating payoff given");

         Utils.QL_REQUIRE(process_.x0() > 0.0,()=> "negative or null underlying");

         switch (payoff.optionType()) 
         {
            case Option.Type.Call:
               results_.value = A(1);
               break;
            case Option.Type.Put:
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
      private double underlying() { return process_.x0(); }
      private double residualTime() { return process_.time( arguments_.exercise.lastDate() ); }
      private double volatility() { return process_.blackVolatility().link.blackVol( residualTime(), minmax() ); }
      private double minmax() { return arguments_.minmax.GetValueOrDefault(); }
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
      private double A( double eta )
      {
         double vol = volatility();
         double lambda = 2.0*(riskFreeRate() - dividendYield())/(vol*vol);
         double s = underlying()/minmax();
         double d1 = Math.Log(s)/stdDeviation() + 0.5*(lambda+1.0)*stdDeviation();
         double n1 = f_.value(eta*d1);
         double n2 = f_.value(eta*(d1-stdDeviation()));
         double n3 = f_.value(eta*(-d1+lambda*stdDeviation()));
         double n4 = f_.value(eta*-d1);
         double pow_s = Math.Pow(s, -lambda);
         return eta*((underlying() * dividendDiscount() * n1 -
                    minmax() * riskFreeDiscount() * n2) +
                    (underlying() * riskFreeDiscount() *
                    (pow_s * n3 - dividendDiscount()* n4/riskFreeDiscount())/lambda));
      }
   }
}
