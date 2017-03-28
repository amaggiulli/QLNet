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
using System.Linq;
using System.Text;

namespace QLNet
{
   //! %Forward engine for vanilla options
   /*! \ingroup forwardengines

       \test
       - the correctness of the returned value is tested by
         reproducing results available in literature.
       - the correctness of the returned greeks is tested by
         reproducing numerical derivatives.
   */
   public class ForwardVanillaEngine : GenericEngine<ForwardVanillaOption.Arguments, OneAssetOption.Results>
   {

      public delegate IPricingEngine GetOriginalEngine( GeneralizedBlackScholesProcess process );

      public ForwardVanillaEngine( GeneralizedBlackScholesProcess process, GetOriginalEngine getEngine)
      {
         process_ = process;
         process_.registerWith(update);
         getOriginalEngine_ = getEngine;
      }
      public override void calculate()
      {
         setup();
         originalEngine_.calculate();
         getOriginalResults();
      }
      
      protected void setup()
      {
         StrikedTypePayoff argumentsPayoff = this.arguments_.payoff as StrikedTypePayoff;
         Utils.QL_REQUIRE(argumentsPayoff != null,()=> "wrong payoff given");

         StrikedTypePayoff payoff = new PlainVanillaPayoff(argumentsPayoff.optionType(),
            this.arguments_.moneyness * process_.x0());

         // maybe the forward value is "better", in some fashion
         // the right level is needed in order to interpolate
         // the vol
         Handle<Quote> spot = process_.stateVariable();
         Utils.QL_REQUIRE(spot.link.value() >= 0.0,()=> "negative or null underlting given");
         Handle<YieldTermStructure> dividendYield = new Handle<YieldTermStructure>(
           new ImpliedTermStructure(process_.dividendYield(),this.arguments_.resetDate));
         Handle<YieldTermStructure> riskFreeRate = new Handle<YieldTermStructure>(
               new ImpliedTermStructure(process_.riskFreeRate(),this.arguments_.resetDate));
         // The following approach is ok if the vol is at most
         // time dependant. It is plain wrong if it is asset dependant.
         // In the latter case the right solution would be stochastic
         // volatility or at least local volatility (which unfortunately
         // implies an unrealistic time-decreasing smile)
         Handle<BlackVolTermStructure> blackVolatility= new Handle<BlackVolTermStructure>(
                new ImpliedVolTermStructure(process_.blackVolatility(),this.arguments_.resetDate));

         GeneralizedBlackScholesProcess fwdProcess = new GeneralizedBlackScholesProcess(spot, dividendYield,
            riskFreeRate,blackVolatility);


         originalEngine_ = getOriginalEngine_(fwdProcess); 
         originalEngine_.reset();

         originalArguments_ = originalEngine_.getArguments() as Option.Arguments;
         Utils.QL_REQUIRE(originalArguments_!= null,()=>  "wrong engine type");
         originalResults_ = originalEngine_.getResults() as OneAssetOption.Results;
         Utils.QL_REQUIRE(originalResults_!= null,()=>  "wrong engine type");

         originalArguments_.payoff = payoff;
         originalArguments_.exercise = this.arguments_.exercise;

         originalArguments_.validate();
   
      }

      protected virtual void getOriginalResults()
      {
         DayCounter rfdc = process_.riskFreeRate().link.dayCounter();
         DayCounter divdc = process_.dividendYield().link.dayCounter();
         double resetTime = rfdc.yearFraction( process_.riskFreeRate().link.referenceDate(),this.arguments_.resetDate );
         double discQ = process_.dividendYield().link.discount(this.arguments_.resetDate );

         this.results_.value = discQ * originalResults_.value;
         // I need the strike derivative here ...
         if ( originalResults_.delta != null && originalResults_.strikeSensitivity != null )
         {
            this.results_.delta = discQ * ( originalResults_.delta +
                  this.arguments_.moneyness * originalResults_.strikeSensitivity );
         }
         this.results_.gamma = 0.0;
         this.results_.theta = process_.dividendYield().link.
             zeroRate( this.arguments_.resetDate, divdc, Compounding.Continuous, Frequency.NoFrequency ).value()
             * this.results_.value;
         if ( originalResults_.vega != null)
            this.results_.vega = discQ * originalResults_.vega;
         if ( originalResults_.rho != null )
            this.results_.rho = discQ * originalResults_.rho;
         if ( originalResults_.dividendRho != null )
         {
            this.results_.dividendRho = -resetTime * this.results_.value
               + discQ * originalResults_.dividendRho;
         }
      }
      protected GeneralizedBlackScholesProcess process_;
      protected IPricingEngine originalEngine_;
      protected Option.Arguments originalArguments_;
      protected OneAssetOption.Results originalResults_;
      protected GetOriginalEngine getOriginalEngine_;
   }
}
