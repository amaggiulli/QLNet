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
   //! Exponential-splines fitting method
   /*! Fits a discount function to the exponential form
       \f[
       d(t) = \sum_{i=1}^9 c_i \exp^{-kappa i t}
       \f]
       where the constants \f$ c_i \f$ and \f$ \kappa \f$ are to be
       determined.  See:Li, B., E. DeWetering, G. Lucas, R. Brenner
       and A. Shapiro (2001): "Merrill Lynch Exponential Spline
       Model." Merrill Lynch Working Paper

       \warning convergence may be slow
   */
   public class ExponentialSplinesFitting : FittedBondDiscountCurve.FittingMethod
   {
      public ExponentialSplinesFitting( bool constrainAtZero = true,
                                        Vector weights = null,
                                        OptimizationMethod optimizationMethod = null)
         :base(constrainAtZero, weights, optimizationMethod)
      {}
       
      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return constrainAtZero_ ? 9 : 10; }

      internal override double discountFunction(Vector x, double t)
      {
         double d = 0.0;
         int N = size();
         double kappa = x[N-1];
         double coeff = 0;

         if (!constrainAtZero_) 
         {
            for (int i=0; i<N-1; ++i) 
            {
               d += x[i]* Math.Exp(-kappa * (i+1) * t);
            }
         } 
         else 
         {
            //  notation:
            //  d(t) = coeff* exp(-kappa*1*t) + x[0]* exp(-kappa*2*t) +
            //  x[1]* exp(-kappa*3*t) + ..+ x[7]* exp(-kappa*9*t)
            for (int i=0; i<N-1; i++) 
            {
               d += x[i]* Math.Exp(-kappa * (i+2) * t);
               coeff += x[i];
            }
            coeff = 1.0- coeff;
            d += coeff * Math.Exp(-kappa * t);
         }
         return d;
      }
   }

   //! Nelson-Siegel fitting method
   /*! Fits a discount function to the form
       \f$ d(t) = \exp^{-r t}, \f$ where the zero rate \f$r\f$ is defined as
       \f[
       r \equiv c_0 + (c_0 + c_1)*(1 - exp^{-\kappa*t}/(\kappa t) -
       c_2 exp^{ - \kappa t}.
       \f]
       See: Nelson, C. and A. Siegel (1985): "Parsimonious modeling of yield
       curves for US Treasury bills." NBER Working Paper Series, no 1594.
   */
   public class NelsonSiegelFitting :  FittedBondDiscountCurve.FittingMethod 
   {
      public NelsonSiegelFitting(Vector weights = null, OptimizationMethod optimizationMethod = null)
         :base(true, weights, optimizationMethod)
      {}

      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return 4; }

      internal override double discountFunction(Vector x, double t)
      {
         double kappa = x[size()-1];
         double zeroRate = x[0] + (x[1] + x[2])*
                          (1.0 - Math.Exp(-kappa*t))/
                          ((kappa+Const.QL_EPSILON)*(t+Const.QL_EPSILON)) -
                          (x[2])*Math.Exp(-kappa*t);
        double d = Math.Exp(-zeroRate * t) ;
        return d;
      }
   }

   //! Svensson Fitting method
   /*! Fits a discount function to the form
       \f$ d(t) = \exp^{-r t}, \f$ where the zero rate \f$r\f$ is defined as
       \f[
       r \equiv c_0 + (c_0 + c_1)(\frac {1 - exp^{-\kappa t}}{\kappa t})
       - c_2exp^{ - \kappa t}
       + c_3{(\frac{1 - exp^{-\kappa_1 t}}{\kappa_1 t} -exp^{-\kappa_1 t})}.
       \f]
       See: Svensson, L. (1994). Estimating and interpreting forward
       interest rates: Sweden 1992-4.
       Discussion paper, Centre for Economic Policy Research(1051).
   */
   public class SvenssonFitting : FittedBondDiscountCurve.FittingMethod 
   {
      public SvenssonFitting(Vector weights = null, OptimizationMethod optimizationMethod = null)
         :base(true, weights, optimizationMethod)
      {}

      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod ;
      }

      public override int size() { return 6; }

      internal override double discountFunction(Vector x, double t)
      {
         double kappa = x[size()-2];
         double kappa_1 = x[size()-1];

         double zeroRate = x[0] + (x[1] + x[2])*
                           (1.0 - Math.Exp(-kappa*t))/
                           ((kappa+Const.QL_EPSILON)*(t+Const.QL_EPSILON)) -
                           (x[2])*Math.Exp(-kappa*t) +
                           x[3]* (((1.0 - Math.Exp(-kappa_1*t))/((kappa_1+Const.QL_EPSILON)*(t+Const.QL_EPSILON)))- Math.Exp(-kappa_1*t));
        double d = Math.Exp(-zeroRate * t) ;
        return d;
      }
    
   }

   //! CubicSpline B-splines fitting method
   /*! Fits a discount function to a set of cubic B-splines
       \f$ N_{i,3}(t) \f$, i.e.,
       \f[
       d(t) = \sum_{i=0}^{n}  c_i * N_{i,3}(t)
       \f]

       See: McCulloch, J. 1971, "Measuring the Term Structure of
       Interest Rates." Journal of Business, 44: 19-31

       McCulloch, J. 1975, "The tax adjusted yield curve."
       Journal of Finance, XXX811-30

       \warning "The results are extremely sensitive to the number
                 and location of the knot points, and there is no
                 optimal way of selecting them." James, J. and
                 N. Webber, "Interest Rate Modelling" John Wiley,
                 2000, pp. 440.
   */
   public class CubicBSplinesFitting : FittedBondDiscountCurve.FittingMethod 
   {
      public CubicBSplinesFitting( List<double> knots, bool constrainAtZero = true, Vector weights = null,
         OptimizationMethod optimizationMethod = null)
         :base(constrainAtZero, weights, optimizationMethod)
      {
         splines_ = new BSpline(3, knots.Count-5, knots);

         Utils.QL_REQUIRE(knots.Count >= 8,()=> "At least 8 knots are required" );
         int basisFunctions = knots.Count - 4;

         if (constrainAtZero) 
         {
            size_ = basisFunctions-1;

            // Note: A small but nonzero N_th basis function at t=0 may
            // lead to an ill conditioned problem
            N_ = 1;

            Utils.QL_REQUIRE(Math.Abs(splines_.value(N_, 0.0)) > Const.QL_EPSILON,()=>
               "N_th cubic B-spline must be nonzero at t=0");
         } 
         else 
         {
            size_ = basisFunctions;
            N_ = 0;
         }

      }
      
      //! cubic B-spline basis functions
      public double basisFunction( int i, double t ) { return splines_.value( i, t ); }
      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return size_; }

      internal override double discountFunction(Vector x, double t)
      {
         double d = 0.0;

         if ( !constrainAtZero_ )
         {
            for ( int i = 0; i < size_; ++i )
            {
               d += x[i] * splines_.value( i, t );
            }
         }
         else
         {
            double T = 0.0;
            double sum = 0.0;
            for ( int i = 0; i < size_; ++i )
            {
               if ( i < N_ )
               {
                  d += x[i] * splines_.value( i, t );
                  sum += x[i] * splines_.value( i, T );
               }
               else
               {
                  d += x[i] * splines_.value( i + 1, t );
                  sum += x[i] * splines_.value( i + 1, T );
               }
            }
            double coeff = 1.0 - sum;
            coeff /= splines_.value( N_, T );
            d += coeff * splines_.value( N_, t );
         }

         return d;
   
      }

      private BSpline splines_;
      private int size_;
      //! N_th basis function coefficient to solve for when d(0)=1
      private int N_;
   }

   //! Simple polynomial fitting method
      /*  Fits a discount function to the simple polynomial form:
         \f[
         d(t) = \sum_{i=0}^{degree}  c_i * t^{i}
         \f]
         where the constants \f$ c_i \f$ are to be determined.

         This is a simple/crude, but fast and robust, means of fitting
         a yield curve.
   */
   public class SimplePolynomialFitting : FittedBondDiscountCurve.FittingMethod 
   {
      public SimplePolynomialFitting( int degree,
                                      bool constrainAtZero = true,
                                      Vector weights = null,
                                      OptimizationMethod optimizationMethod = null)
         :base(constrainAtZero, weights, optimizationMethod)
      {
         size_ = constrainAtZero ? degree : degree + 1;
      }
      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return size_; }

      internal override double discountFunction(Vector x, double t)
      {
         double d = 0.0;

         if (!constrainAtZero_) 
         {
            for (int i=0; i<size_; ++i)
               d += x[i] * BernsteinPolynomial.get( (uint)i, (uint)i, t );
         } 
         else 
         {
            d = 1.0;
            for (int i=0; i<size_; ++i)
               d += x[i] * BernsteinPolynomial.get( (uint)i + 1, (uint)i + 1, t );
         }
         return d;
      }

      private int size_;
   }

   //! Spread fitting method helper
   /*  Fits a spread curve on top of a discount function according to given parametric method
   */
   public class SpreadFittingMethod : FittedBondDiscountCurve.FittingMethod 
   {
      public SpreadFittingMethod( FittedBondDiscountCurve.FittingMethod method, Handle<YieldTermStructure> discountCurve )
         :base(method != null ? method.constrainAtZero() : true, 
               method != null ? method.weights() : null, 
					method != null ? method.optimizationMethod() : null)
      {
         method_ = method;
         discountingCurve_ = discountCurve;

         Utils.QL_REQUIRE( method != null,()=> "Fitting method is empty" );
         Utils.QL_REQUIRE( !discountingCurve_.empty(),()=> "Discounting curve cannot be empty" );
      }

      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      internal override void init()
      {
         //In case discount curve has a different reference date,
		   //discount to this curve's reference date
		   if (curve_.referenceDate() != discountingCurve_.link.referenceDate())
         {
			   rebase_ = discountingCurve_.link.discount(curve_.referenceDate());
		   }  
		   else
         {
			   rebase_ = 1.0;
		   }

		   //Call regular init
		   base.init();
      }

      public override int size() { return method_.size(); }

      internal override double discountFunction(Vector x, double t)
      {
         return method_.discount( x, t ) * discountingCurve_.link.discount( t, true ) / rebase_;
      }
		// underlying parametric method
      private FittedBondDiscountCurve.FittingMethod method_;
      // adjustment in case underlying discount curve has different reference date
      private double rebase_;
      // discount curve from on top of which the spread will be calculated
      private Handle<YieldTermStructure> discountingCurve_;
    
   }
}
