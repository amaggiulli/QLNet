/*
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
using System.Numerics;

namespace QLNet
{

   public static partial class Utils
   {
      /*  Compute modified Bessel functions I_nv(x) and K_nv(x)

      Based on series expansion outlined in e.g.
      http://www.mhtlab.uwaterloo.ca/courses/me755/web_chap4.pdf

      The exponentially weighted versions return the function value
      times exp(-x) resp exp(-z)
      */

      public interface Weight<T>
      {
         T weightSmallX( T x );
         T weight1LargeX( T x );
         T weight2LargeX( T x );
      }

      public class doubleUnweighted : Weight<double>
      {
         public double weightSmallX( double x ) { return 1.0; }
         public double weight1LargeX( double x ) { return Math.Exp( x ); }
         public double weight2LargeX( double x ) { return Math.Exp( -x ); }
      }

      public class complexUnweighted : Weight<Complex>
      {
         public Complex weightSmallX( Complex x ) { return 1.0; }
         public Complex weight1LargeX( Complex x ) { return Complex.Exp( x ); }
         public Complex weight2LargeX( Complex x ) { return Complex.Exp( -x ); }
      }

      public class doubleExponentiallyWeighted : Weight<double>
      {
         public double weightSmallX( double x ) { return Math.Exp( -x ); }
         public double weight1LargeX( double x ) { return 1.0; }
         public double weight2LargeX( double x ) { return Math.Exp( -2.0 * x ); }
      }

      public class complexExponentiallyWeighted : Weight<Complex>
      {
         public Complex weightSmallX( Complex x ) { return Complex.Exp( -x ); }
         public Complex weight1LargeX( Complex x ) { return 1.0; }
         public Complex weight2LargeX( Complex x ) { return Complex.Exp( -2.0 * x ); }
      }

      public interface baseValue<T>
      {
         T value();
      }

      public class doubleValue : baseValue<double>
      {
         public double value() { return 0.0; }
      }

      public class complexValue : baseValue<Complex>
      {
         public Complex value() { return new Complex( 0.0, 1.0 ); }
      }

      // Implementation

      public static double modifiedBesselFunction_i( double nu, double x )
      {
         Utils.QL_REQUIRE( x >= 0.0, () => "negative argument requires complex version of modifiedBesselFunction" );
         return modifiedBesselFunction_i_impl<doubleUnweighted, doubleValue>( nu, x );
      }

      public static Complex modifiedBesselFunction_i( double nu, Complex z )
      {
         return modifiedBesselFunction_i_impl<complexUnweighted, complexValue>( nu, z );
      }

      public static double modifiedBesselFunction_k( double nu, double x )
      {
         return modifiedBesselFunction_k_impl<doubleUnweighted, doubleValue>( nu, x );
      }

      public static Complex modifiedBesselFunction_k(double nu, Complex z) 
      {
         return modifiedBesselFunction_k_impl<complexUnweighted, complexValue>( nu, z );
      }

      public static double modifiedBesselFunction_i_exponentiallyWeighted(double nu, double x) 
      {
         Utils.QL_REQUIRE(x >= 0.0, ()=>"negative argument requires complex version of modifiedBesselFunction");
         return modifiedBesselFunction_i_impl<doubleExponentiallyWeighted, doubleValue>( nu, x );
      }

      public static Complex modifiedBesselFunction_i_exponentiallyWeighted(double nu, Complex z) 
      {
         return modifiedBesselFunction_i_impl<complexExponentiallyWeighted, complexValue>( nu, z );
      }

      public static double modifiedBesselFunction_k_exponentiallyWeighted( double nu, double x )
      {
         return modifiedBesselFunction_k_impl<doubleExponentiallyWeighted, doubleValue>( nu, x );
      }

      public static Complex modifiedBesselFunction_k_exponentiallyWeighted(double nu, Complex z) 
      {
         return modifiedBesselFunction_k_impl<complexExponentiallyWeighted, complexValue>( nu, z );
      }

      public static double modifiedBesselFunction_i_impl<T, I>( double nu, double x )
         where T : Weight<double>, new()
         where I : baseValue<double>, new()
      {
         if ( Math.Abs( x ) < 13.0 )
         {
            double alpha = Math.Pow( 0.5 * x, nu ) / GammaFunction.value( 1.0 + nu );
            double Y = 0.25 * x * x;
            int k = 1;
            double sum = alpha, B_k = alpha;

            while ( Math.Abs( B_k *= Y / ( k * ( k + nu ) ) ) > Math.Abs( sum ) * Const.QL_EPSILON )
            {
               sum += B_k;
               Utils.QL_REQUIRE( ++k < 1000, () => "max iterations exceeded" );
            }
            return sum * new T().weightSmallX( x );
         }
         else
         {
            double na_k = 1.0, sign = 1.0;
            double da_k = 1.0;

            double s1 = 1.0, s2 = 1.0;
            for ( int k = 1; k < 30; ++k )
            {
               sign *= -1;
               na_k *= ( 4.0 * nu * nu -
                        ( 2.0 * k - 1.0 ) *
                           ( 2.0 * k - 1.0 ) );
               da_k *= ( 8.0 * k ) * x;
               double a_k = na_k / da_k;

               s2 += a_k;
               s1 += sign * a_k;
            }

            double i = new I().value();
            return 1.0 / Math.Sqrt( 2 * Const.M_PI * x ) *
               ( new T().weight1LargeX( x ) * s1 +
               i * Math.Exp( i * nu * Const.M_PI ) * new T().weight2LargeX( x ) * s2 );
         }
      }

      public static Complex modifiedBesselFunction_i_impl<T, I>( double nu, Complex x )
         where T : Weight<Complex>, new()
         where I : baseValue<Complex>, new()
      {
         if ( Complex.Abs( x ) < 13.0 )
         {
            Complex alpha = Complex.Pow( 0.5 * x, nu ) / GammaFunction.value( 1.0 + nu );
            Complex Y = 0.25 * x * x;
            int k = 1;
            Complex sum = alpha, B_k = alpha;

            while ( Complex.Abs( B_k *= Y / ( k * ( k + nu ) ) ) > Complex.Abs( sum ) * Const.QL_EPSILON )
            {
               sum += B_k;
               Utils.QL_REQUIRE( ++k < 1000, () => "max iterations exceeded" );
            }
            return sum * new T().weightSmallX( x );
         }
         else
         {
            double na_k = 1.0, sign = 1.0;
            Complex da_k = new Complex( 1.0, 0.0 );

            Complex s1 = new Complex( 1.0, 0.0 ), s2 = new Complex( 1.0, 0.0 );
            for ( int k = 1; k < 30; ++k )
            {
               sign *= -1;
               na_k *= ( 4.0 * nu * nu -
                        ( 2.0 * (double)k - 1.0 ) *
                            ( 2.0 * (double)k - 1.0 ) );
               da_k *= ( 8.0 * k ) * x;
               Complex a_k = na_k / da_k;

               s2 += a_k;
               s1 += sign * a_k;
            }

            Complex i = new I().value();
            return 1.0 / Complex.Sqrt( 2 * Const.M_PI * x ) *
                ( new T().weight1LargeX( x ) * s1 +
                 i * Complex.Exp( i * nu * Const.M_PI ) * new T().weight2LargeX( x ) * s2 );
         }
      }

      public static double modifiedBesselFunction_k_impl<T, I>( double nu, double x )
         where T : Weight<double>, new()
         where I : baseValue<double>, new()
      {
         return Const.M_PI_2 * ( modifiedBesselFunction_i_impl<T, I>( -nu, x ) -
                                modifiedBesselFunction_i_impl<T, I>( nu, x ) ) /
                                Math.Sin( Const.M_PI * nu );
      }


      public static Complex modifiedBesselFunction_k_impl<T, I>( double nu, Complex x )
         where T : Weight<Complex>, new()
         where I : baseValue<Complex>, new()
      {
         return Const.M_PI_2 * ( modifiedBesselFunction_i_impl<T, I>( -nu, x ) -
                                modifiedBesselFunction_i_impl<T, I>( nu, x ) ) /
                                Math.Sin( Const.M_PI * nu );
      }
   }
}
