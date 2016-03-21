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

namespace QLNet
{
   //! Analytic pricing engine for American vanilla options with digital payoff
   /*! \ingroup vanillaengines

       \todo add more greeks (as of now only delta and rho available)

       \test
       - the correctness of the returned value in case of
         cash-or-nothing at-hit digital payoff is tested by
         reproducing results available in literature.
       - the correctness of the returned value in case of
         asset-or-nothing at-hit digital payoff is tested by
         reproducing results available in literature.
       - the correctness of the returned value in case of
         cash-or-nothing at-expiry digital payoff is tested by
         reproducing results available in literature.
       - the correctness of the returned value in case of
         asset-or-nothing at-expiry digital payoff is tested by
         reproducing results available in literature.
       - the correctness of the returned greeks in case of
         cash-or-nothing at-hit digital payoff is tested by
         reproducing numerical derivatives.
   */
   public class AnalyticDigitalAmericanEngine : VanillaOption.Engine
   {
      public AnalyticDigitalAmericanEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         process_.registerWith(update);
      }

      public override void calculate()
      {
         AmericanExercise ex = arguments_.exercise as AmericanExercise;
         Utils.QL_REQUIRE(ex != null,()=>  "non-American exercise given");
         Utils.QL_REQUIRE(ex.dates()[0] <= process_.blackVolatility().link.referenceDate(),()=>
                     "American option with window exercise not handled yet");

         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-striked payoff given");

         double spot = process_.stateVariable().link.value();
         Utils.QL_REQUIRE(spot > 0.0, ()=> "negative or null underlying given");

         double variance = process_.blackVolatility().link.blackVariance(ex.lastDate(),payoff.strike());
         double dividendDiscount = process_.dividendYield().link.discount(ex.lastDate());
         double riskFreeDiscount = process_.riskFreeRate().link.discount(ex.lastDate());

         if ( ex.payoffAtExpiry() )
         {
            AmericanPayoffAtExpiry pricer = new AmericanPayoffAtExpiry( spot, riskFreeDiscount,
                                          dividendDiscount, variance,
                                          payoff, knock_in() );
            results_.value = pricer.value();
         }
         else
         {
            AmericanPayoffAtHit pricer = new AmericanPayoffAtHit( spot, riskFreeDiscount, dividendDiscount, variance, payoff );
            results_.value = pricer.value();
            results_.delta = pricer.delta();
            results_.gamma = pricer.gamma();

            DayCounter rfdc = process_.riskFreeRate().link.dayCounter();
            double t = rfdc.yearFraction( process_.riskFreeRate().link.referenceDate(),
                                          arguments_.exercise.lastDate() );
            results_.rho = pricer.rho( t );
         }
   
      }
      public virtual bool knock_in() {return true;}
      
      private GeneralizedBlackScholesProcess process_;
   }

   //! Analytic pricing engine for American Knock-out options with digital payoff
   /*! \ingroup vanillaengines

        \todo add more greeks (as of now only delta and rho available)

        \test
        - the correctness of the returned value in case of
          cash-or-nothing at-hit digital payoff is tested by
          reproducing results available in literature.
        - the correctness of the returned value in case of
          asset-or-nothing at-hit digital payoff is tested by
          reproducing results available in literature.
        - the correctness of the returned value in case of
          cash-or-nothing at-expiry digital payoff is tested by
          reproducing results available in literature.
        - the correctness of the returned value in case of
          asset-or-nothing at-expiry digital payoff is tested by
          reproducing results available in literature.
        - the correctness of the returned greeks in case of
          cash-or-nothing at-hit digital payoff is tested by
          reproducing numerical derivatives.
   */

   public class AnalyticDigitalAmericanKOEngine : AnalyticDigitalAmericanEngine 
   {
      public AnalyticDigitalAmericanKOEngine(GeneralizedBlackScholesProcess engine):
        base(engine) {}

      public override bool knock_in() {return false;}
    
   }

}
