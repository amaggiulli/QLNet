/*
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
 
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

//! default bivariate implementation
using BivariateCumulativeNormalDistribution = QLNet.BivariateCumulativeNormalDistributionWe04DP;

namespace QLNet
{
   //! Pricing engine for 2D European Baskets
   /*! This class implements formulae from
       "Options on the Minimum or the Maximum of Two Risky Assets",
           Rene Stulz,
           Journal of Financial Ecomomics (1982) 10, 161-185.

       \ingroup basketengines

       \test the correctness of the returned value is tested by
             reproducing results available in literature.
   */
   public class StulzEngine : BasketOption.Engine
   {
      public StulzEngine(GeneralizedBlackScholesProcess process1,GeneralizedBlackScholesProcess process2,double correlation)
      {
         process1_ = process1;
         process2_ = process2;
         rho_ = correlation;
         process1_.registerWith(update);
         process2_.registerWith(update);
      }
      public override void calculate()
      {
                 
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=> "not an European Option");

         EuropeanExercise exercise = arguments_.exercise as EuropeanExercise;
         Utils.QL_REQUIRE(exercise!=null,()=> "not an European Option");

         BasketPayoff basket_payoff = arguments_.payoff as BasketPayoff;

         MinBasketPayoff min_basket = arguments_.payoff as MinBasketPayoff;

         MaxBasketPayoff max_basket = arguments_.payoff as MaxBasketPayoff;
        
         Utils.QL_REQUIRE(min_basket != null || max_basket!= null, ()=> "unknown basket type");

         PlainVanillaPayoff payoff = basket_payoff.basePayoff() as PlainVanillaPayoff;
        
         Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");

         double strike = payoff.strike();

         double variance1 = process1_.blackVolatility().link.blackVariance(exercise.lastDate(), strike);
         double variance2 = process2_.blackVolatility().link.blackVariance(exercise.lastDate(), strike);

         double riskFreeDiscount = process1_.riskFreeRate().link.discount(exercise.lastDate());

         // cannot handle non zero dividends, so don't believe this...
         double dividendDiscount1 = process1_.dividendYield().link.discount(exercise.lastDate());
         double dividendDiscount2 = process2_.dividendYield().link.discount(exercise.lastDate());

         double forward1 = process1_.stateVariable().link.value() * dividendDiscount1 / riskFreeDiscount;
         double forward2 = process2_.stateVariable().link.value() * dividendDiscount2 / riskFreeDiscount;

         if (max_basket!=null) 
         {
            switch (payoff.optionType()) 
            {
               // euro call on a two asset max basket
               case Option.Type.Call:
                  results_.value = euroTwoAssetMaxBasketCall(forward1, forward2, strike,
                                                riskFreeDiscount,
                                                variance1, variance2,
                                                rho_);

                  break;
               // euro put on a two asset max basket
               case Option.Type.Put:
                  results_.value = strike * riskFreeDiscount -
                     euroTwoAssetMaxBasketCall(forward1, forward2, 0.0,
                                                riskFreeDiscount,
                                                variance1, variance2, rho_) +
                     euroTwoAssetMaxBasketCall(forward1, forward2, strike,
                                                riskFreeDiscount,
                                                variance1, variance2, rho_);
                  break;
               default:
                  Utils.QL_FAIL("unknown option type");
                  break;
            
            }
         } 
         else if (min_basket != null) 
         {
            switch (payoff.optionType()) 
            {
               // euro call on a two asset min basket
               case Option.Type.Call:
                  results_.value = euroTwoAssetMinBasketCall(forward1, forward2, strike,
                                                riskFreeDiscount,
                                                variance1, variance2,
                                                rho_);
                  break;
               // euro put on a two asset min basket
               case Option.Type.Put:
                  results_.value = strike * riskFreeDiscount -
                     euroTwoAssetMinBasketCall(forward1, forward2, 0.0,
                                                riskFreeDiscount,
                                                variance1, variance2, rho_) +
                     euroTwoAssetMinBasketCall(forward1, forward2, strike,
                                                riskFreeDiscount,
                                                variance1, variance2, rho_);
                  break;
               default:
                  Utils.QL_FAIL("unknown option type");
                  break;
            }
         
         } 
         else 
         {
            Utils.QL_FAIL("unknown type");
         }

      }

      // calculate the value of euro min basket call
      private double euroTwoAssetMinBasketCall( double forward1, double forward2,double strike,double riskFreeDiscount,
                                                double variance1, double variance2,double rho ) 
      {
         double stdDev1 = Math.Sqrt(variance1);
         double stdDev2 = Math.Sqrt(variance2);

         double variance = variance1 + variance2 - 2*rho*stdDev1*stdDev2;
         double stdDev = Math.Sqrt(variance);

         double modRho1 = (rho * stdDev2 - stdDev1) / stdDev;
         double modRho2 = (rho * stdDev1 - stdDev2) / stdDev;

         double D1 = (Math.Log(forward1/forward2) + 0.5*variance) / stdDev;

         double alfa, beta, gamma;
         if (strike != 0.0) 
         {
            BivariateCumulativeNormalDistribution bivCNorm = new BivariateCumulativeNormalDistribution(rho);
            BivariateCumulativeNormalDistribution bivCNormMod2 = new BivariateCumulativeNormalDistribution(modRho2);
            BivariateCumulativeNormalDistribution bivCNormMod1 = new BivariateCumulativeNormalDistribution(modRho1);

            double D1_1 = (Math.Log(forward1/strike) + 0.5*variance1) / stdDev1;
            double D1_2 = (Math.Log(forward2/strike) + 0.5*variance2) / stdDev2;
            alfa = bivCNormMod1.value(D1_1, -D1);
            beta = bivCNormMod2.value(D1_2, D1 - stdDev);
            gamma = bivCNorm.value(D1_1 - stdDev1, D1_2 - stdDev2);
         } 
         else 
         {
               CumulativeNormalDistribution cum = new CumulativeNormalDistribution();
               alfa = cum.value(-D1);
               beta = cum.value(D1 - stdDev);
               gamma = 1.0;
         }

         return riskFreeDiscount * (forward1*alfa + forward2*beta - strike*gamma);
      }

              
      // calculate the value of euro max basket call
      private double euroTwoAssetMaxBasketCall(double forward1, double forward2,double strike,double riskFreeDiscount,
                                               double variance1, double variance2,double rho) 
      {
         StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, strike);

         double black1 = Utils.blackFormula(payoff.optionType(), payoff.strike(),forward1, 
                                            Math.Sqrt(variance1)) * riskFreeDiscount;

         double black2 = Utils.blackFormula(payoff.optionType(), payoff.strike(),forward2, 
                                            Math.Sqrt(variance2)) * riskFreeDiscount;

         return black1 + black2 -
               euroTwoAssetMinBasketCall(forward1, forward2, strike,
                                       riskFreeDiscount,
                                       variance1, variance2, rho);
        }

      private GeneralizedBlackScholesProcess process1_;
      private GeneralizedBlackScholesProcess process2_;
      private double rho_;
   }
}
