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
   //! %Abcd functional form
   /*! \f[ f(t) = [ a + b*t ] e^{-c*t} + d \f]
       following Rebonato's notation. */
   public class AbcdMathFunction
   {
      public AbcdMathFunction(double a = 0.002,double b = 0.001, double c = 0.16,double d = 0.0005)
      {
         a_ = a;
         b_ = b;
         c_ = c;
         d_ = d;
         abcd_ = new InitializedList<double>(4);
         dabcd_ = new InitializedList<double>(4);
         abcd_[0] = a_;
         abcd_[1] = b_;
         abcd_[2] = c_;
         abcd_[3] = d_;
         initialize_();
      }

      public AbcdMathFunction( List<double> abcd)
      {
         abcd_ = new List<double>(abcd);
         dabcd_= new InitializedList<double>(4);
         a_ = abcd_[0];
         b_ = abcd_[1];
         c_ = abcd_[2];
         d_ = abcd_[3];
         initialize_();
      }

      //! function value at time t: \f[ f(t) \f]
      public double value(double t)
      {
         return t<0 ? 0.0 : (a_ + b_*t)*Math.Exp(-c_*t) + d_;
      }

      //! time at which the function reaches maximum (if any)
      public double maximumLocation()
      {
         if ( b_ == 0.0 )
         {
            if ( a_ >= 0.0 )
               return 0.0;
            else
               return Double.MaxValue;
         }

         // stationary point
         // TODO check if minimum
         // TODO check if maximum at +inf
         double zeroFirstDerivative = 1.0 / c_ - a_ / b_;
         return ( zeroFirstDerivative > 0.0 ? zeroFirstDerivative : 0.0 );
      }

      //! maximum value of the function
      public double maximumValue()
      {
         if (b_==0.0 || a_<=0.0)
            return d_;
        return this.value(maximumLocation());
      }

      //! function value at time +inf: \f[ f(\inf) \f]
      public double longTermValue()  { return d_; }

      /*! first derivative of the function at time t
         \f[ f'(t) = [ (b-c*a) + (-c*b)*t) ] e^{-c*t} \f] */
      public double derivative(double t)
      {
         return t<0 ? 0.0 : (da_ + db_*t)*Math.Exp(-c_*t);
      }
        
      /*! indefinite integral of the function at time t
         \f[ \int f(t)dt = [ (-a/c-b/c^2) + (-b/c)*t ] e^{-c*t} + d*t \f] */
      public double primitive(double t)
      {
         return t<0 ? 0.0 : (pa_ + pb_*t)*Math.Exp(-c_*t) + d_*t + K_;
      }
        
      /*! definite integral of the function between t1 and t2
         \f[ \int_{t1}^{t2} f(t)dt \f] */
      public double definiteIntegral(double t1, double t2)
      {
         return primitive( t2 ) - primitive( t1 );
      }

      /*! Inspectors */
      public double a()  { return a_; }
      public double b()  { return b_; }
      public double c()  { return c_; }
      public double d()  { return d_; }
      public List<double> coefficients() { return abcd_; }
      public List<double> derivativeCoefficients() { return dabcd_; }
      // the primitive is not abcd

      /*! coefficients of a AbcdMathFunction defined as definite
         integral on a rolling window of length tau, with tau = t2-t */
      public List<double> definiteIntegralCoefficients(double t, double t2)
      {
         double dt = t2 - t;
         double expcdt = Math.Exp(-c_*dt);
         List<double> result = new InitializedList<double>(4);
         result[0] = diacplusbcc_ - (diacplusbcc_ + dibc_*dt)*expcdt;
         result[1] = dibc_ * (1.0 - expcdt);
         result[2] = c_;
         result[3] = d_*dt;
         return result;
      }

      /*! coefficients of a AbcdMathFunction defined as definite
         derivative on a rolling window of length tau, with tau = t2-t */
      public List<double> definiteDerivativeCoefficients(double t, double t2)
      {
         double dt = t2 - t;
         double expcdt = Math.Exp(-c_*dt);
         List<double> result = new InitializedList<double>(4);
         result[1] = b_*c_/(1.0-expcdt);
         result[0] = a_*c_ - b_ + result[1]*dt*expcdt;
         result[0] /= 1.0-expcdt;
         result[2] = c_;
         result[3] = d_/dt;
         return result;
      }

      public static void validate(double a, double b, double c, double d)
      {
         Utils.QL_REQUIRE(c>0,()=> "c (" + c + ") must be positive");
         Utils.QL_REQUIRE(d>=0,()=> "d (" + d + ") must be non negative");
         Utils.QL_REQUIRE(a+d>=0,()=> "a+d (" + a + "+" + d + ") must be non negative");

         if (b>=0.0)
            return;

         // the one and only stationary point...
         double zeroFirstDerivative = 1.0/c-a/b;
         if (zeroFirstDerivative>=0.0) 
         {
            // ... is a minimum
            // must be abcd(zeroFirstDerivative)>=0
            Utils.QL_REQUIRE(b>=-(d*c)/Math.Exp(c*a/b-1.0),()=>
               "b (" + b + ") less than " + 
               -(d*c)/Math.Exp(c*a/b-1.0) + ": negative function value at stationary point " + zeroFirstDerivative);
         }
      }

      
      protected double a_, b_, c_, d_;
      
      private void initialize_()
      {
         validate( a_, b_, c_, d_ );
         da_ = b_ - c_ * a_;
         db_ = -c_ * b_;
         dabcd_[0] = da_;
         dabcd_[1] = db_;
         dabcd_[2] = c_;
         dabcd_[3] = 0.0;

         pa_ = -( a_ + b_ / c_ ) / c_;
         pb_ = -b_ / c_;
         K_ = 0.0;

         dibc_ = b_ / c_;
         diacplusbcc_ = a_ / c_ + dibc_ / c_;
      }

      private List<double> abcd_;
      private List<double> dabcd_;
      private double da_, db_;
      private double pa_, pb_, K_;
      private double dibc_, diacplusbcc_;

   }
}
