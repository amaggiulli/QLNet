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
   //! analytic european option pricer including stochastic interest rates
   /*! References:

       Brigo, Mercurio, Interest Rate Models

       \ingroup vanillaengines

       \test the correctness of the returned value is tested by
             reproducing results available in web/literature
   */
   public class AnalyticBSMHullWhiteEngine : GenericModelEngine<HullWhite,VanillaOption.Arguments,
      VanillaOption.Results>
   {
      public AnalyticBSMHullWhiteEngine(double equityShortRateCorrelation,
                                        GeneralizedBlackScholesProcess process,
                                        HullWhite model)
         :base(model)
      {
         rho_ = equityShortRateCorrelation;
         process_ = process;

         process_.registerWith(update);
      }

      public override void calculate()
      {
         Utils.QL_REQUIRE(process_.x0() > 0.0,()=> "negative or null underlying given");

         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-striked payoff given");

         Exercise exercise = arguments_.exercise;

         double t = process_.riskFreeRate().link.dayCounter().yearFraction(process_.riskFreeRate().link.referenceDate(),
            exercise.lastDate());

         double a = model_.link.parameters()[0];
         double sigma = model_.link.parameters()[1];
         double eta = process_.blackVolatility().link.blackVol(exercise.lastDate(),payoff.strike());

         double varianceOffset;
         if (a*t > Math.Pow(Const.QL_EPSILON, 0.25)) 
         {
            double v = sigma*sigma/(a*a) *(t + 2/a*Math.Exp(-a*t) - 1/(2*a)*Math.Exp(-2*a*t) - 3/(2*a));
            double mu = 2*rho_*sigma*eta/a*(t-1/a*(1-Math.Exp(-a*t)));

            varianceOffset = v + mu;
         }
         else 
         {
            // low-a algebraic limit
            double v = sigma*sigma*t*t*t*(1/3.0-0.25*a*t+7/60.0*a*a*t*t);
            double mu = rho_*sigma*eta*t*t*(1-a*t/3.0+a*a*t*t/12.0);

            varianceOffset = v + mu;
         }

         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(
              new ShiftedBlackVolTermStructure(varianceOffset,process_.blackVolatility()));

         GeneralizedBlackScholesProcess adjProcess =
                new GeneralizedBlackScholesProcess(process_.stateVariable(),
                                                   process_.dividendYield(),
                                                   process_.riskFreeRate(),
                                                   volTS);

         AnalyticEuropeanEngine bsmEngine = new AnalyticEuropeanEngine(adjProcess);

         VanillaOption option = new VanillaOption(payoff, exercise);
         option.setupArguments(bsmEngine.getArguments());
        
         bsmEngine.calculate();

         results_ = bsmEngine.getResults() as OneAssetOption.Results;
         
      }
      
      private double rho_;
      private GeneralizedBlackScholesProcess process_;
   }

           
   public class ShiftedBlackVolTermStructure :  BlackVolTermStructure 
   {
      public ShiftedBlackVolTermStructure( double varianceOffset,Handle<BlackVolTermStructure>  volTS)
         : base(volTS.link.referenceDate(),volTS.link.calendar(),BusinessDayConvention.Following,volTS.link.dayCounter())
      {
         varianceOffset_ = varianceOffset;
         volTS_ = volTS;
      }

      public override double minStrike() { return volTS_.link.minStrike(); }
      public override double maxStrike() { return volTS_.link.maxStrike(); }
      public override Date maxDate()  { return volTS_.link.maxDate(); }

      protected override double blackVarianceImpl(double t, double strike) 
      {
         return volTS_.link.blackVariance(t, strike, true)+varianceOffset_;
      }
            
      protected override double blackVolImpl(double t, double strike) 
      {
         double nonZeroMaturity = (t==0.0 ? 0.00001 : t);
         double var = blackVarianceImpl(nonZeroMaturity, strike);
         return Math.Sqrt(var/nonZeroMaturity);
      }
          
      private double varianceOffset_;
      private Handle<BlackVolTermStructure> volTS_;
        
   }

}
