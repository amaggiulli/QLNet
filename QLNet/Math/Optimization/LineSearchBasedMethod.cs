/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

namespace QLNet
{
   public class LineSearchBasedMethod : OptimizationMethod
   {
      public LineSearchBasedMethod( LineSearch lineSearch = null )
      {
         lineSearch_ = lineSearch ?? new ArmijoLineSearch();
      }

      public override EndCriteria.Type minimize(Problem P,EndCriteria endCriteria) 
      {
         // Initializations
         double ftol = endCriteria.functionEpsilon();
         int maxStationaryStateIterations_ = endCriteria.maxStationaryStateIterations();
         EndCriteria.Type ecType = EndCriteria.Type.None;   // reset end criteria
         P.reset();                                      // reset problem
         Vector x_ = P.currentValue();              // store the starting point
         int iterationNumber_ = 0;
         // dimension line search
         lineSearch_.searchDirection = new Vector( x_.size() );
         bool done = false;

         // function and squared norm of gradient values;
         double fnew, fold, gold2;
         double fdiff;
         // classical initial value for line-search step
         double t = 1.0;
         // Set gradient g at the size of the optimization problem
         // search direction
         int sz = lineSearch_.searchDirection.size();
         Vector prevGradient = new Vector( sz ), d = new Vector( sz ), sddiff = new Vector( sz ), direction = new Vector( sz );
         // Initialize cost function, gradient prevGradient and search direction
         P.setFunctionValue( P.valueAndGradient( prevGradient, x_ ) );
         P.setGradientNormValue( Vector.DotProduct( prevGradient, prevGradient ) );
         lineSearch_.searchDirection = prevGradient * -1;

         bool first_time = true;
         // Loop over iterations
         do
         {
            // Linesearch
            if ( !first_time )
               prevGradient = lineSearch_.lastGradient();
            t = ( lineSearch_.value( P, ref ecType, endCriteria, t ) );
            // don't throw: it can fail just because maxIterations exceeded
            //QL_REQUIRE(lineSearch_->succeed(), "line-search failed!");
            if ( lineSearch_.succeed() )
            {
               // Updates

               // New point
               x_ = lineSearch_.lastX();
               // New function value
               fold = P.functionValue();
               P.setFunctionValue( lineSearch_.lastFunctionValue() );
               // New gradient and search direction vectors

               // orthogonalization coef
               gold2 = P.gradientNormValue();
               P.setGradientNormValue( lineSearch_.lastGradientNorm2() );

               // conjugate gradient search direction
               direction = getUpdatedDirection( P, gold2, prevGradient );

               sddiff = direction - lineSearch_.searchDirection;
               lineSearch_.searchDirection = direction;
               // Now compute accuracy and check end criteria
               // Numerical Recipes exit strategy on fx (see NR in C++, p.423)
               fnew = P.functionValue();
               fdiff = 2.0 * Math.Abs( fnew - fold ) /
                       ( Math.Abs( fnew ) + Math.Abs( fold ) + Const.QL_EPSILON );
               if ( fdiff < ftol ||
                   endCriteria.checkMaxIterations( iterationNumber_, ref ecType ) )
               {
                  endCriteria.checkStationaryFunctionValue( 0.0, 0.0, ref maxStationaryStateIterations_, ref ecType );
                  endCriteria.checkMaxIterations( iterationNumber_, ref ecType );
                  return ecType;
               }
               P.setCurrentValue( x_ );      // update problem current value
               ++iterationNumber_;         // Increase iteration number
               first_time = false;
            }
            else
            {
               done = true;
            }
         }
         while ( !done );
         P.setCurrentValue( x_ );
         return ecType;
    }
      //! computes the new search direction
      protected virtual Vector getUpdatedDirection(Problem P,double gold2,Vector gradient)
      {
         throw new NotImplementedException();
      }

      //! line search
      protected LineSearch lineSearch_;

   }
}