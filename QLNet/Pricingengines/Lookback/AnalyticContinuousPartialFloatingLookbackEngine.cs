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
   //! Pricing engine for European continuous partial-time floating-strike lookback option
   /*! Formula from "Option Pricing Formulas, Second Edition",
       E.G. Haug, 2006, p.146

   */
   public class AnalyticContinuousPartialFloatingLookbackEngine : ContinuousPartialFloatingLookbackOption.Engine
   {
      public AnalyticContinuousPartialFloatingLookbackEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         process_.registerWith( update );
      }
      public override void calculate()
      {
         FloatingTypePayoff payoff = arguments_.payoff as FloatingTypePayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "Non-floating payoff given");

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
      private double lambda() { return arguments_.lambda; }
      private double lookbackPeriodEndTime() { return process_.time( arguments_.lookbackPeriodEnd ); }
      private double stdDeviation() {return volatility() * Math.Sqrt(residualTime());}
      private double riskFreeRate()
      {
         return process_.riskFreeRate().link.zeroRate( residualTime(), Compounding.Continuous,
            Frequency.NoFrequency ).value();
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
         bool fullLookbackPeriod = lookbackPeriodEndTime() == residualTime();
         double carry = riskFreeRate() - dividendYield();
         double vol = volatility();
         double x = 2.0*carry/(vol*vol);
         double s = underlying()/minmax();

         double ls = Math.Log(s);
         double d1 = ls/stdDeviation() + 0.5*(x+1.0)*stdDeviation();
         double d2 = d1 - stdDeviation();

         double e1 = 0, e2 = 0;
         if (!fullLookbackPeriod)
         {
            e1 = (carry + vol * vol / 2) * (residualTime() - lookbackPeriodEndTime()) / (vol * Math.Sqrt(residualTime() - lookbackPeriodEndTime()));
            e2 = e1 - vol * Math.Sqrt(residualTime() - lookbackPeriodEndTime());
         } 

         double f1 = (ls + (carry + vol * vol / 2) * lookbackPeriodEndTime()) / (vol * Math.Sqrt(lookbackPeriodEndTime()));
         double f2 = f1 - vol * Math.Sqrt(lookbackPeriodEndTime());

         double l1 = Math.Log(lambda()) / vol;
         double g1 = l1 / Math.Sqrt(residualTime());
         double g2 = 0;
         if (!fullLookbackPeriod) g2 = l1 / Math.Sqrt(residualTime() - lookbackPeriodEndTime());
        
         double n1 = f_.value(eta*(d1 - g1));
         double n2 = f_.value(eta*(d2 - g1));

         BivariateCumulativeNormalDistributionWe04DP cnbn1 = new BivariateCumulativeNormalDistributionWe04DP(1), 
            cnbn2 = new BivariateCumulativeNormalDistributionWe04DP(0), 
            cnbn3 = new BivariateCumulativeNormalDistributionWe04DP(-1);
         if (!fullLookbackPeriod) 
         {
            cnbn1 = new BivariateCumulativeNormalDistributionWe04DP (Math.Sqrt(lookbackPeriodEndTime() / residualTime()));
            cnbn2 = new BivariateCumulativeNormalDistributionWe04DP (-Math.Sqrt(1 - lookbackPeriodEndTime() / residualTime()));
            cnbn3 = new BivariateCumulativeNormalDistributionWe04DP (-Math.Sqrt(lookbackPeriodEndTime() / residualTime()));
         }

         double n3 = cnbn1.value(eta*(-f1+2.0* carry * Math.Sqrt(lookbackPeriodEndTime()) / vol), eta*(-d1+x*stdDeviation()-g1));
         double n4 = 0, n5 = 0, n6 = 0, n7 = 0;
         if (!fullLookbackPeriod)
         {
            n4 = cnbn2.value(-eta*(d1+g1), eta*(e1 + g2));
            n5 = cnbn2.value(-eta*(d1-g1), eta*(e1 - g2));
            n6 = cnbn3.value(eta*-f2, eta*(d2 - g1));
            n7 = f_.value(eta*(e2 - g2));
         }
         else
         {
            n4 = f_.value(-eta*(d1+g1));
         }

         double n8 = f_.value(-eta*f1);
         double pow_s = Math.Pow(s, -x);
         double pow_l = Math.Pow(lambda(), x);

         if (!fullLookbackPeriod)
         {
            return eta*(underlying() * dividendDiscount() * n1 -
                        lambda() * minmax() * riskFreeDiscount() * n2 + 
                        underlying() * riskFreeDiscount() * lambda() / x *
                        (pow_s * n3 - dividendDiscount() / riskFreeDiscount() * pow_l * n4)
                        + underlying() * dividendDiscount() * n5 + 
                        riskFreeDiscount() * lambda() * minmax() * n6 -
                        Math.Exp(-carry * (residualTime() - lookbackPeriodEndTime())) * 
                        dividendDiscount() * (1 + 0.5 * vol * vol / carry) * lambda() * 
                        underlying() * n7 * n8);
         }
         else
         {
            //Simpler calculation
            return eta*(underlying() * dividendDiscount() * n1 -
                        lambda() * minmax() * riskFreeDiscount() * n2 + 
                        underlying() * riskFreeDiscount() * lambda() / x *
                        (pow_s * n3 - dividendDiscount() / riskFreeDiscount() * pow_l * n4));
        }
      }
   }
}
