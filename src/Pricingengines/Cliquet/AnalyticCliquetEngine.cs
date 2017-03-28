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

namespace QLNet
{
   //! Pricing engine for Cliquet options using analytical formulae
   /*! \ingroup cliquetengines

       \test
       - the correctness of the returned value is tested by
         reproducing results available in literature.
       - the correctness of the returned greeks is tested by
         reproducing numerical derivatives.
   */
   public class AnalyticCliquetEngine : CliquetOption.Engine
   {
      public AnalyticCliquetEngine( GeneralizedBlackScholesProcess process)
      {
         process_=process;
         process_.registerWith(update);
      }

      public override void calculate()
      {
         Utils.QL_REQUIRE(arguments_.accruedCoupon == null &&
            arguments_.lastFixing == null,()=> 
            "this engine cannot price options already started");
         Utils.QL_REQUIRE(arguments_.localCap == null &&
                   arguments_.localFloor == null &&
                   arguments_.globalCap == null &&
                   arguments_.globalFloor == null,()=>
                   "this engine cannot price capped/floored options");

         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=> "not an European option");

         PercentageStrikePayoff moneyness = arguments_.payoff as PercentageStrikePayoff;
         Utils.QL_REQUIRE(moneyness!=null,()=> "wrong payoff given");

         List<Date> resetDates = arguments_.resetDates;
         resetDates.Add(arguments_.exercise.lastDate());

         double underlying = process_.stateVariable().link.value();
         Utils.QL_REQUIRE(underlying > 0.0,()=> "negative or null underlying");
         double strike = underlying * moneyness.strike();
         StrikedTypePayoff payoff = new PlainVanillaPayoff(moneyness.optionType(),strike);

         results_.value = 0.0;
         results_.delta = results_.gamma = 0.0;
         results_.theta = 0.0;
         results_.rho = results_.dividendRho = 0.0;
         results_.vega = 0.0;

         for (int i = 1; i < resetDates.Count; i++) 
         {
            double weight = process_.dividendYield().link.discount(resetDates[i-1]);
            double discount = process_.riskFreeRate().link.discount(resetDates[i]) /
                              process_.riskFreeRate().link.discount(resetDates[i-1]);
            double qDiscount = process_.dividendYield().link.discount(resetDates[i]) /
                               process_.dividendYield().link.discount(resetDates[i-1]);
            double forward = underlying*qDiscount/discount;
            double variance = process_.blackVolatility().link.blackForwardVariance(resetDates[i-1],resetDates[i],strike);

            BlackCalculator black = new BlackCalculator(payoff, forward, Math.Sqrt(variance), discount);

            DayCounter rfdc  = process_.riskFreeRate().link.dayCounter();
            DayCounter divdc = process_.dividendYield().link.dayCounter();
            DayCounter voldc = process_.blackVolatility().link.dayCounter();

            results_.value += weight * black.value();
            results_.delta += weight * (black.delta(underlying) +
                                        moneyness.strike() * discount *
                                        black.beta());
            results_.gamma += 0.0;
            results_.theta += process_.dividendYield().link.forwardRate(
                resetDates[i-1], resetDates[i], rfdc, Compounding.Continuous, Frequency.NoFrequency).value() *
                weight * black.value();

            double dt = rfdc.yearFraction(resetDates[i-1],resetDates[i]);
            results_.rho += weight * black.rho(dt);

            double t = divdc.yearFraction( process_.dividendYield().link.referenceDate(),resetDates[i-1]);
            dt = divdc.yearFraction(resetDates[i-1],resetDates[i]);
            results_.dividendRho += weight * (black.dividendRho(dt) -
                                              t * black.value());

            dt = voldc.yearFraction(resetDates[i-1], resetDates[i]);
            results_.vega += weight * black.vega(dt);
         }
      }
      
      private GeneralizedBlackScholesProcess process_;
   }
}
