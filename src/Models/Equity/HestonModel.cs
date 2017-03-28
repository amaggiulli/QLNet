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
   //! Heston model for the stochastic volatility of an asset
   /*! References:

       Heston, Steven L., 1993. A Closed-Form Solution for Options
       with Stochastic Volatility with Applications to Bond and
       Currency Options.  The review of Financial Studies, Volume 6,
       Issue 2, 327-343.

       \test calibration is tested against known good values.
   */
   public class HestonModel : CalibratedModel
   {
      public HestonModel(HestonProcess process)
         :base(5)
      {
         process_ = process;

         arguments_[0] = new ConstantParameter( process.theta(), new PositiveConstraint() );
         arguments_[1] = new ConstantParameter( process.kappa(), new PositiveConstraint() );
         arguments_[2] = new ConstantParameter( process.sigma(), new PositiveConstraint() );
         arguments_[3] = new ConstantParameter( process.rho(), new BoundaryConstraint( -1.0, 1.0 ) );
         arguments_[4] = new ConstantParameter( process.v0(), new PositiveConstraint() );
         generateArguments();

         process_.riskFreeRate().registerWith(update) ;
         process_.dividendYield().registerWith( update) ;
         process_.s0().registerWith( update ) ;

      }

      // variance mean version level
      public double theta() { return arguments_[0].value(0.0); }
      // variance mean reversion speed
      public double kappa() { return arguments_[1].value(0.0); }
      // volatility of the volatility
      public double sigma() { return arguments_[2].value(0.0); }
      // correlation
      public double rho() { return arguments_[3].value(0.0); }
      // spot variance
      public double v0() { return arguments_[4].value(0.0); }

      // underlying process
      public HestonProcess process() { return process_; }

      public class FellerConstraint : Constraint
      {
         private class Impl : IConstraint 
         {
            public bool test(Vector param) 
            {
               double theta = param[0];
               double kappa = param[1];
               double sigma = param[2];

               return (sigma >= 0.0 && sigma*sigma < 2.0*kappa*theta);
            }

            public Vector upperBound(Vector parameters)
            {
               return new Vector( parameters.size(), Double.MaxValue );
            }

            public Vector lowerBound(Vector parameters)
            {
               return new Vector( parameters.size(), Double.MinValue );
            }
         }
      
         public FellerConstraint()
            : base(new FellerConstraint.Impl()) 
         {}
    
      }

      protected override void generateArguments()
      {
         process_ = new HestonProcess(  process_.riskFreeRate(),
                                        process_.dividendYield(),
                                        process_.s0(),
                                        v0(), kappa(), theta(),
                                        sigma(), rho() ) ;
      }
      protected HestonProcess process_;
   }
}
