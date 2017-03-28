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
   // Goldstein and Price line-search class
   public class GoldsteinLineSearch : LineSearch
   {
      //! Default constructor
      public GoldsteinLineSearch(double eps = 1e-8,double alpha = 0.05,double beta = 0.65,double extrapolation = 1.5)
         : base(eps)
      {
         alpha_ = alpha;
         beta_ = beta;
         extrapolation_ = extrapolation;
      }

      //! Perform line search
      public override double value(Problem P,             // Optimization problem
                                   ref EndCriteria.Type ecType,
                                   EndCriteria endCriteria,
                                   double t_ini)      // initial value of line-search step
      {
         Constraint  constraint = P.constraint();
         succeed_ = true;
         bool maxIter = false;
         double t = t_ini;
         int loopNumber = 0;

         double q0 = P.functionValue();
         double qp0 = P.gradientNormValue();

         double tl = 0.0;
         double tr = 0.0;

         qt_ = q0;
         qpt_ = ( gradient_.empty() ) ? qp0 : -Vector.DotProduct( gradient_, searchDirection_ );

         // Initialize gradient
         gradient_ = new Vector( P.currentValue().size() );
         // Compute new point
         xtd_ = P.currentValue();
         t = update( ref xtd_, searchDirection_, t, constraint );
         // Compute function value at the new point
         qt_ = P.value( xtd_ );

         while ( ( qt_ - q0 ) < -beta_ * t * qpt_ || ( qt_ - q0 ) > -alpha_ * t * qpt_ )
         {
            if ( ( qt_ - q0 ) > -alpha_ * t * qpt_ )
               tr = t;
            else
               tl = t;
            ++loopNumber;

            // calculate the new step
            if ( Utils.close_enough( tr, 0.0 ) )
               t *= extrapolation_;
            else
               t = ( tl + tr ) / 2.0;

            // New point value
            xtd_ = P.currentValue();
            t = update( ref xtd_, searchDirection_, t, constraint );

            // Compute function value at the new point
            qt_ = P.value( xtd_ );
            P.gradient( gradient_, xtd_ );
            // and it squared norm
            maxIter = endCriteria.checkMaxIterations( loopNumber, ref ecType );

            if ( maxIter )
               break;
         }

         if ( maxIter )
            succeed_ = false;

         // Compute new gradient
         P.gradient( gradient_, xtd_ );
         // and it squared norm
         qpt_ = Vector.DotProduct( gradient_, gradient_ );

         // Return new step value
         return t;

      }
      private double alpha_, beta_;
      private double extrapolation_;
   }
}
