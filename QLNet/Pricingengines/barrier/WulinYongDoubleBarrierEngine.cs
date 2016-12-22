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
   //! Pricing engine for barrier options using analytical formulae
   /*! The formulas are taken from "Barrier Option Pricing",
        Wulin Suo, Yong Wang.

       \ingroup barrierengines

       \test the correctness of the returned value is tested by
             reproducing results available in literature.
   */
   public class WulinYongDoubleBarrierEngine : DoubleBarrierOption.Engine
   {
      public WulinYongDoubleBarrierEngine(GeneralizedBlackScholesProcess process,int series = 5)
      {
         process_ = process;
         series_ = series;
         f_ = new CumulativeNormalDistribution();

         process_.registerWith(update);
      }
      public override void calculate()
      {
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");
         Utils.QL_REQUIRE(payoff.strike()>0.0,()=> "strike must be positive");

         double K = payoff.strike();
         double S = process_.x0();
         Utils.QL_REQUIRE(S >= 0.0,()=> "negative or null underlying given");
         Utils.QL_REQUIRE(!triggered(S),()=> "barrier touched");

         DoubleBarrier.Type barrierType = arguments_.barrierType;
         Utils.QL_REQUIRE(barrierType == DoubleBarrier.Type.KnockOut || 
                          barrierType == DoubleBarrier.Type.KnockIn,()=>
            "only KnockIn and KnockOut options supported");

         double L = arguments_.barrier_lo.GetValueOrDefault();
         double H = arguments_.barrier_hi.GetValueOrDefault();
         double K_up = Math.Min(H, K);
         double K_down = Math.Max(L, K);
         double T = residualTime();
         double rd = riskFreeRate();
         double dd = riskFreeDiscount();
         double rf = dividendYield();
         double df = dividendDiscount();
         double vol = volatility();
         double mu = rd - rf - vol*vol/2.0;
         double sgn = mu > 0 ? 1.0 :(mu < 0 ? -1.0: 0.0);
         //rebate
         double R_L = arguments_.rebate.GetValueOrDefault();
         double R_H = arguments_.rebate.GetValueOrDefault();

         //european option
         EuropeanOption europeanOption = new EuropeanOption(payoff, arguments_.exercise);
         IPricingEngine analyticEuropeanEngine = new AnalyticEuropeanEngine(process_);
         europeanOption.setPricingEngine(analyticEuropeanEngine);
         double european = europeanOption.NPV();

         double barrierOut = 0;
         double rebateIn = 0;
         for(int n = -series_; n < series_; n++){
            double d1 = D(S/H*Math.Pow(L/H, 2.0*n), vol*vol+mu, vol, T);
            double d2 = d1 - vol*Math.Sqrt(T);
            double g1 = D(H/S*Math.Pow(L/H, 2.0*n - 1.0), vol*vol+mu, vol, T);
            double g2 = g1 - vol*Math.Sqrt(T);
            double h1 = D(S/H*Math.Pow(L/H, 2.0*n - 1.0), vol*vol+mu, vol, T);
            double h2 = h1 - vol*Math.Sqrt(T);
            double k1 = D(L/S*Math.Pow(L/H, 2.0*n - 1.0), vol*vol+mu, vol, T);
            double k2 = k1 - vol*Math.Sqrt(T);
            double d1_down = D(S/K_down*Math.Pow(L/H, 2.0*n), vol*vol+mu, vol, T);
            double d2_down = d1_down - vol*Math.Sqrt(T);
            double d1_up = D(S/K_up*Math.Pow(L/H, 2.0*n), vol*vol+mu, vol, T);
            double d2_up = d1_up - vol*Math.Sqrt(T);
            double k1_down = D((H*H)/(K_down*S)*Math.Pow(L/H, 2.0*n), vol*vol+mu, vol, T);
            double k2_down = k1_down - vol*Math.Sqrt(T);
            double k1_up = D((H*H)/(K_up*S)*Math.Pow(L/H, 2.0*n), vol*vol+mu, vol, T);
            double k2_up = k1_up - vol*Math.Sqrt(T);

            if( payoff.optionType() == Option.Type.Call) 
            {
               barrierOut += Math.Pow(L/H, 2.0 * n * mu/(vol*vol))*
                           (df*S*Math.Pow(L/H, 2.0*n)*(f_.value(d1_down)-f_.value(d1))
                           -dd*K*(f_.value(d2_down)-f_.value(d2))
                           -df*Math.Pow(L/H, 2.0*n)*H*H/S*Math.Pow(H/S, 2.0*mu/(vol*vol))*(f_.value(k1_down)-f_.value(k1))
                           +dd*K*Math.Pow(H/S,2.0*mu/(vol*vol))*(f_.value(k2_down)-f_.value(k2)));
            }
            else if(payoff.optionType() == Option.Type.Put)
            {
               barrierOut += Math.Pow(L/H, 2.0 * n * mu/(vol*vol))*
                           (dd*K*(f_.value(h2)-f_.value(d2_up))
                           -df*S*Math.Pow(L/H, 2.0*n)*(f_.value(h1)-f_.value(d1_up))
                           -dd*K*Math.Pow(H/S,2.0*mu/(vol*vol))*(f_.value(g2)-f_.value(k2_up))
                           +df*Math.Pow(L/H, 2.0*n)*H*H/S*Math.Pow(H/S, 2.0*mu/(vol*vol))*(f_.value(g1)-f_.value(k1_up)));
            }
            else 
            {
               Utils.QL_FAIL("option type not recognized");
            }

            double v1 = D(H/S*Math.Pow(H/L, 2.0*n), -mu, vol, T);
            double v2 = D(H/S*Math.Pow(H/L, 2.0*n), mu, vol, T);
            double v3 = D(S/L*Math.Pow(H/L, 2.0*n), -mu, vol, T);
            double v4 = D(S/L*Math.Pow(H/L, 2.0*n), mu, vol, T);
            rebateIn +=  dd * R_H * sgn * (Math.Pow(L/H, 2.0*n*mu/(vol*vol)) * f_.value(sgn * v1) - Math.Pow(H/S, 2.0*mu/(vol*vol)) * f_.value(-sgn * v2))
                        + dd * R_L * sgn * (Math.Pow(L/S, 2.0*mu/(vol*vol)) * f_.value(-sgn * v3) - Math.Pow(H/L, 2.0*n*mu/(vol*vol)) * f_.value(sgn * v4));
         }

         //rebate paid at maturity
         if(barrierType == DoubleBarrier.Type.KnockOut)
            results_.value = barrierOut ;
         else
            results_.value = european - barrierOut;

         results_.additionalResults["vanilla"] = european;
         results_.additionalResults["barrierOut"] = barrierOut;
         results_.additionalResults["barrierIn"] = european - barrierOut;

      }
      
      private GeneralizedBlackScholesProcess process_;
      private int series_;
      private CumulativeNormalDistribution f_;
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
      //private double barrier() { return 0; }
      //private double rebate() {return arguments_.rebate.GetValueOrDefault();}
      //private double stdDeviation() {return volatility() * Math.Sqrt(residualTime());}
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
      private double dividendDiscount() { return process_.dividendYield().link.discount( residualTime() ); }
      private double D(double X, double lambda, double sigma, double T)
      {
         return (Math.Log(X) + lambda * T)/(sigma * Math.Sqrt(T));
      }
   }
}
