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
   //! B-spline basis functions
   /*! Follows treatment and notation from:

       Weisstein, Eric W. "B-Spline." From MathWorld--A Wolfram Web
       Resource.  <http://mathworld.wolfram.com/B-Spline.html>

       \f$ (p+1) \f$-th order B-spline (or p degree polynomial) basis
       functions \f$ N_{i,p}(x), i = 0,1,2 \ldots n \f$, with \f$ n+1 \f$
       control points, or equivalently, an associated knot vector
       of size \f$ p+n+2 \f$ defined at the increasingly sorted points
       \f$ (x_0, x_1 \ldots x_{n+p+1}) \f$. A linear B-spline has
       \f$ p=1 \f$, quadratic B-spline has \f$ p=2 \f$, a cubic
       B-spline has \f$ p=3 \f$, etc.

       The B-spline basis functions are defined recursively
       as follows:

       \f[
       \begin{array}{rcl}
       N_{i,0}(x) &=& 1   \textrm{\ if\ } x_{i} \leq x < x_{i+1} \\
                  &=& 0   \textrm{\ otherwise} \\
       N_{i,p}(x) &=& N_{i,p-1}(x) \frac{(x - x_{i})}{ (x_{i+p-1} - x_{i})} +
                      N_{i+1,p-1}(x) \frac{(x_{i+p} - x)}{(x_{i+p} - x_{i+1})}
       \end{array}
       \f]
   */
   public class BSpline
   {
      public BSpline(int p, int n, List<double> knots)
      {
         p_ = p;
         n_ = n;
         knots_ = knots;

         Utils.QL_REQUIRE( p >= 1,()=> "lowest degree B-spline has p = 1" );
         Utils.QL_REQUIRE( n >= 1,()=> "number of control points n+1 >= 2" );
         Utils.QL_REQUIRE( p <= n,()=> "must have p <= n" );

         Utils.QL_REQUIRE( knots.Count == p + n + 2,()=> "number of knots must equal p+n+2" );

         for ( int i = 0; i < knots.Count - 1; ++i )
         {
            Utils.QL_REQUIRE( knots[i] <= knots[i + 1],()=> "knots points must be nondecreasing" );
         }
      }

      public double value(int i, double x)
      {
         Utils.QL_REQUIRE( i <= n_,()=> "i must not be greater than n" );
         return N( i, p_, x );
      }

      // recursive definition of N, the B-spline basis function
      private double N(int i, int p, double x)
      {
         if (p==0) 
         {
            return (knots_[i] <= x && x < knots_[i+1]) ? 1.0 : 0.0;
         } 
         else 
         {
            return ((x - knots_[i])/(knots_[i+p] - knots_[i]))*N(i,p-1,x) +
                ((knots_[i+p+1]-x)/(knots_[i+p+1]-knots_[i+1]))* N(i+1,p-1,x);
        }
      
      }   

      // e.g. p_=2 is a quadratic B-spline, p_=3 is a cubic B-Spline, etc.
      private int p_;
      // n_ + 1 =  "control points" = max number of basis functions
      private int n_;
      private List<double> knots_;
   }
}
