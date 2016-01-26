// 
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
// 

using System;

namespace QLNet
{
   //! Pricing engine for spread option on two futures
   /*! This class implements formulae from
       "Correlation in the Energy Markets", E. Kirk
       Managing Energy Price Risk. 
       London: Risk Publications and Enron, pp. 71-78

       \ingroup basketengines

       \test the correctness of the returned value is tested by
             reproducing results available in literature.
   */
   public class KirkEngine : BasketOption.Engine
   {
      public KirkEngine( BlackProcess process1,
                         BlackProcess process2,
                         double correlation)
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

         SpreadBasketPayoff spreadPayoff = arguments_.payoff as SpreadBasketPayoff;
         Utils.QL_REQUIRE(spreadPayoff!=null,()=>" spread payoff expected");

         PlainVanillaPayoff payoff = spreadPayoff.basePayoff() as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!= null, ()=> "non-plain payoff given");
         double strike = payoff.strike();
        
         double f1 = process1_.stateVariable().link.value();
         double f2 = process2_.stateVariable().link.value();

         // use atm vols
         double variance1 = process1_.blackVolatility().link.blackVariance(exercise.lastDate(), f1);
         double variance2 = process2_.blackVolatility().link.blackVariance(exercise.lastDate(), f2);

         double riskFreeDiscount = process1_.riskFreeRate().link.discount(exercise.lastDate());

         Func<double, double> Square = x => x * x;
         double f = f1/(f2 + strike);
         double v = Math.Sqrt( variance1
                               + variance2 * Square( f2 / ( f2 + strike ) )
                               - 2*rho_*Math.Sqrt(variance1*variance2)
                               *(f2/(f2+strike)));
        
         BlackCalculator black = new BlackCalculator( new PlainVanillaPayoff(payoff.optionType(),1.0), f, v, riskFreeDiscount);
        
         results_.value = (f2 + strike)*black.value();
         
      }
      
      private BlackProcess process1_;
      private BlackProcess process2_;
      private  double rho_;
   }
}
