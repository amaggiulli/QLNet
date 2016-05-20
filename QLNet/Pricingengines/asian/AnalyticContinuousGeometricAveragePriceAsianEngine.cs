/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

namespace QLNet
{
   //! Pricing engine for European continuous geometric average price Asian
   /*! This class implements a continuous geometric average price
       Asian option with European exercise.  The formula is from
       "Option Pricing Formulas", E. G. Haug (1997) pag 96-97.

       \ingroup asianengines

       \test
       - the correctness of the returned value is tested by
         reproducing results available in literature, and results
         obtained using a discrete average approximation.
       - the correctness of the returned greeks is tested by
         reproducing numerical derivatives.

       \todo handle seasoned options
   */
   public class AnalyticContinuousGeometricAveragePriceAsianEngine : ContinuousAveragingAsianOption.Engine
   {
      public AnalyticContinuousGeometricAveragePriceAsianEngine( GeneralizedBlackScholesProcess process )
      {
         process_ = process;
         process_.registerWith( update );
      }
      public override void calculate()
      {
         Utils.QL_REQUIRE(arguments_.averageType == Average.Type.Geometric,()=> "not a geometric average option");
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=> "not an European Option");

         Date exercise = arguments_.exercise.lastDate();

         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE( payoff != null,()=> "non-plain payoff given" );

         double volatility = process_.blackVolatility().link.blackVol( exercise, payoff.strike() );
         double variance = process_.blackVolatility().link.blackVariance( exercise,payoff.strike() );
         double riskFreeDiscount = process_.riskFreeRate().link.discount( exercise );

         DayCounter rfdc = process_.riskFreeRate().link.dayCounter();
         DayCounter divdc = process_.dividendYield().link.dayCounter();
         DayCounter voldc = process_.blackVolatility().link.dayCounter();

         double dividendYield = 0.5 * (
             process_.riskFreeRate().link.zeroRate( exercise, rfdc,
                                                    Compounding.Continuous,
                                                    Frequency.NoFrequency ).rate() +
             process_.dividendYield().link.zeroRate( exercise, divdc,
                                                     Compounding.Continuous,
                                                     Frequency.NoFrequency ).rate() +
             volatility * volatility / 6.0 );

         double t_q = divdc.yearFraction( process_.dividendYield().link.referenceDate(), exercise );
         double dividendDiscount = Math.Exp( -dividendYield * t_q );

         double spot = process_.stateVariable().link.value();
         Utils.QL_REQUIRE( spot > 0.0,()=> "negative or null underlying" );

         double forward = spot * dividendDiscount / riskFreeDiscount;

         BlackCalculator black = new BlackCalculator( payoff, forward, Math.Sqrt( variance / 3.0 ), riskFreeDiscount );

         results_.value = black.value();
         results_.delta = black.delta( spot );
         results_.gamma = black.gamma( spot );

         results_.dividendRho = black.dividendRho( t_q ) / 2.0;

         double t_r = rfdc.yearFraction( process_.riskFreeRate().link.referenceDate(),arguments_.exercise.lastDate() );
         results_.rho = black.rho( t_r ) + 0.5 * black.dividendRho( t_q );

         double t_v = voldc.yearFraction(process_.blackVolatility().link.referenceDate(),arguments_.exercise.lastDate() );
         results_.vega = black.vega( t_v ) / Math.Sqrt( 3.0 ) +
                         black.dividendRho( t_q ) * volatility / 6.0;
         try
         {
            results_.theta = black.theta( spot, t_v );
         }
         catch ( Exception /*Error*/)
         {
            results_.theta = null; //Null<Real>();
         }
      }

      private GeneralizedBlackScholesProcess process_;
   }
}
