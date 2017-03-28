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
   //! Pricing engine for European continuous fixed-strike lookback
   /*! Formula from "Option Pricing Formulas",
       E.G. Haug, McGraw-Hill, 1998, p.63-64
   */
   public class AnalyticContinuousFixedLookbackEngine : ContinuousFixedLookbackOption.Engine
   {
      public AnalyticContinuousFixedLookbackEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         process_.registerWith(update);
      }
      public override void calculate()
      {
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "Non-plain payoff given");

         Utils.QL_REQUIRE(process_.x0() > 0.0,()=> "negative or null underlying");

         double strike = payoff.strike();

         switch (payoff.optionType()) 
         {
            case Option.Type.Call:
               Utils.QL_REQUIRE(payoff.strike()>=0.0,()=>"Strike must be positive or null");
               if (strike <= minmax())
                  results_.value = A(1) + C(1);
               else
                  results_.value = B(1);
               break;
            case Option.Type.Put:
               Utils.QL_REQUIRE(payoff.strike()>0.0,()=>"Strike must be positive");
               if (strike >= minmax())
                  results_.value = A(-1) + C(-1);
               else
                  results_.value = B(-1);
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
      private double strike() 
      {
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "Non-plain payoff given");
         return payoff.strike();
      }
      private double residualTime() { return process_.time( arguments_.exercise.lastDate() ); }
      private double volatility() { return process_.blackVolatility().link.blackVol( residualTime(), strike() ); }
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
         double ss = underlying()/minmax();
         double d1 = Math.Log(ss)/stdDeviation() + 0.5*(lambda+1.0)*stdDeviation();
         double N1 = f_.value(eta*d1);
         double N2 = f_.value(eta*(d1-stdDeviation()));
         double N3 = f_.value(eta*(d1-lambda*stdDeviation()));
         double N4 = f_.value(eta*d1);
         double powss = Math.Pow(ss, -lambda);
         return eta*(underlying() * dividendDiscount() * N1 -
                     minmax() * riskFreeDiscount() * N2 -
                     underlying() * riskFreeDiscount() *
                     (powss * N3 - dividendDiscount()* N4/riskFreeDiscount())/lambda);
      }
      private double B( double eta )
      {
         double vol = volatility();
         double lambda = 2.0*(riskFreeRate() - dividendYield())/(vol*vol);
         double ss = underlying()/strike();
         double d1 = Math.Log(ss)/stdDeviation() + 0.5*(lambda+1.0)*stdDeviation();
         double N1 = f_.value(eta*d1);
         double N2 = f_.value(eta*(d1-stdDeviation()));
         double N3 = f_.value(eta*(d1-lambda*stdDeviation()));
         double N4 = f_.value(eta*d1);
         double powss = Math.Pow(ss, -lambda);
         return eta*(underlying() * dividendDiscount() * N1 -
                     strike() * riskFreeDiscount() * N2 -
                     underlying() * riskFreeDiscount() *
                     (powss * N3 - dividendDiscount()* N4/riskFreeDiscount())/lambda);
      }
      private double C( double eta )
      {
         return eta * ( riskFreeDiscount() * ( minmax() - strike() ) );
      }
   }
}
