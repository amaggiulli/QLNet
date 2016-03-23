/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Generic;

namespace QLNet
{
   public class SABRWrapper : IWrapper
   {
      public SABRWrapper( double t, double forward, List<double?> param )
      {
         t_ = t;
         forward_ = forward;
         params_ = param;
         Utils.validateSabrParameters( param[0].Value, param[1].Value, param[2].Value, param[3].Value );
      }
      public double volatility( double x )
      {
         return Utils.sabrVolatility( x, forward_, t_, params_[0].Value, params_[1].Value, params_[2].Value, params_[3].Value );
      }

      private double t_, forward_;
      private List<double?> params_;
   }

   public struct SABRSpecs : IModel
   {
      //private SABRWrapper wrapper;

      public int dimension() { return 4; }
      public void defaultValues( List<double?> param, List<bool> b, double forward, double expiryTIme )
      {
         if ( param[1] == null )
            param[1] = 0.5;
         if ( param[0] == null )
            // adapt alpha to beta level
            param[0] = 0.2 * ( param[1] < 0.9999 ? Math.Pow( forward, 1.0 - param[1].Value ) : 1.0 );
         if ( param[2] == null )
            param[2] = Math.Sqrt( 0.4 );
         if ( param[3] == null )
            param[3] = 0.0;
      }

      public void guess( Vector values, List<bool> paramIsFixed, double forward, double expiryTime, List<double> r )
      {
         int j = 0;
         if ( !paramIsFixed[1] )
            values[1] = ( 1.0 - 2E-6 ) * r[j++] + 1E-6;
         if ( !paramIsFixed[0] )
         {
            values[0] = ( 1.0 - 2E-6 ) * r[j++] + 1E-6; // lognormal vol guess
            // adapt this to beta level
            if ( values[1] < 0.999 )
               values[0] *= Math.Pow( forward, 1.0 - values[1] );
         }
         if ( !paramIsFixed[2] )
            values[2] = 1.5 * r[j++] + 1E-6;
         if ( !paramIsFixed[3] )
            values[3] = ( 2.0 * r[j++] - 1.0 ) * ( 1.0 - 1E-6 );
      }

      public double eps1() { return .0000001; }
      public double eps2() { return .9999; }
      public double dilationFactor() { return 0.001; }
      public Vector inverse( Vector y, List<bool> b, List<double?> c, double d )
      {
         Vector x = new Vector( 4 );
         x[0] = y[0] < 25.0 + eps1() ? Math.Sqrt( y[0] - eps1() )
                                     : ( y[0] - eps1() + 25.0 ) / 10.0;
         // y_[1] = std::tan(M_PI*(x[1] - 0.5))/dilationFactor();
         x[1] = Math.Sqrt( -Math.Log( y[1] ) );
         x[2] = y[2] < 25.0 + eps1() ? Math.Sqrt( y[2] - eps1() )
                                       : ( y[2] - eps1() + 25.0 ) / 10.0;
         x[3] = Math.Asin( y[3] / eps2() );
         return x;
      }
      public Vector direct( Vector x, List<bool> b, List<double?> c, double d )
      {
         Vector y = new Vector( 4 );
         y[0] = Math.Abs( x[0] ) < 5.0
                     ? x[0] * x[0] + eps1()
                     : ( 10.0 * Math.Abs( x[0] ) - 25.0 ) + eps1();
         // y_[1] = std::atan(dilationFactor_*x[1])/M_PI + 0.5;
         y[1] = Math.Abs( x[1] ) < Math.Sqrt( -Math.Log( eps1() ) )
                     ? Math.Exp( -( x[1] * x[1] ) )
                     : eps1();
         y[2] = Math.Abs( x[2] ) < 5.0
                     ? x[2] * x[2] + eps1()
                     : ( 10.0 * Math.Abs( x[2] ) - 25.0 ) + eps1();
         y[3] = Math.Abs( x[3] ) < 2.5 * Const.M_PI
                     ? eps2() * Math.Sin( x[3] )
                     : eps2() * ( x[3] > 0.0 ? 1.0 : ( -1.0 ) );
         return y;
      }
      public IWrapper instance( double t, double forward, List<double?> param )
      {
         return new SABRWrapper( t, forward, param );
      }
      public SABRWrapper modelInstance_ { get; set; }
   }

   //! %SABR smile interpolation between discrete volatility points.
   public class SABRInterpolation : Interpolation
   {
      public SABRInterpolation( List<double> xBegin,  // x = strikes
                               int xEnd,
                               List<double> yBegin,  // y = volatilities
                               double t,             // option expiry
                               double forward,
                               double? alpha,
                               double? beta,
                               double? nu,
                               double? rho,
                               bool alphaIsFixed,
                               bool betaIsFixed,
                               bool nuIsFixed,
                               bool rhoIsFixed,
                               bool vegaWeighted = true,
                               EndCriteria endCriteria = null,
                               OptimizationMethod optMethod = null,
                               double errorAccept = 0.0020,
                               bool useMaxError = false,
                               int maxGuesses = 50 ) 
      {

            impl_ = new XABRInterpolationImpl<SABRSpecs>(
                    xBegin, xEnd, yBegin, t, forward,
                    new List<double?>(){alpha,beta,nu,rho},
                    //boost::assign::list_of(alpha)(beta)(nu)(rho),
                    new List<bool>(){alphaIsFixed,betaIsFixed,nuIsFixed,rhoIsFixed},
                    //boost::assign::list_of(alphaIsFixed)(betaIsFixed)(nuIsFixed)(rhoIsFixed),
                    vegaWeighted, endCriteria, optMethod, errorAccept, useMaxError,
                    maxGuesses);
            coeffs_ = (impl_ as XABRInterpolationImpl<SABRSpecs>).coeff_;
        }
      public double expiry() { return coeffs_.t_; }
      public double forward() { return coeffs_.forward_; }
      public double alpha() { return coeffs_.params_[0].Value; }
      public double beta() { return coeffs_.params_[1].Value; }
      public double nu() { return coeffs_.params_[2].Value; }
      public double rho() { return coeffs_.params_[3].Value; }
      public double rmsError() { return coeffs_.error_.Value; }
      public double maxError() { return coeffs_.maxError_.Value; }
      public List<double> interpolationWeights() { return coeffs_.weights_; }
      public EndCriteria.Type endCriteria() { return coeffs_.XABREndCriteria_; }

      private XABRCoeffHolder<SABRSpecs> coeffs_;
   }

   //! %SABR interpolation factory and traits
   public class SABR 
   {
      public SABR(double t, double forward, double alpha, double beta, double nu, double rho,
                  bool alphaIsFixed, bool betaIsFixed, bool nuIsFixed, bool rhoIsFixed,
                  bool vegaWeighted = false,
                  EndCriteria endCriteria = null,
                  OptimizationMethod optMethod = null,
                  double errorAccept = 0.0020, bool useMaxError = false,int maxGuesses = 50)
      {
         t_ = t; 
         forward_ = forward;
         alpha_ = alpha; 
         beta_ = beta; 
         nu_ = nu; 
         rho_ = rho;
         alphaIsFixed_ = alphaIsFixed; 
         betaIsFixed_ = betaIsFixed;
         nuIsFixed_ = nuIsFixed; 
         rhoIsFixed_ = rhoIsFixed;
         vegaWeighted_ = vegaWeighted;
         endCriteria_ = endCriteria;
         optMethod_ = optMethod; 
         errorAccept_ = errorAccept;
         useMaxError_ = useMaxError; 
         maxGuesses_ = maxGuesses;
      }

      Interpolation interpolate(List<double> xBegin, int xEnd,List<double> yBegin) 
      {
         return new SABRInterpolation( xBegin, xEnd, yBegin, t_, forward_, alpha_, beta_, nu_, rho_,
                alphaIsFixed_, betaIsFixed_, nuIsFixed_, rhoIsFixed_, vegaWeighted_,
                endCriteria_, optMethod_, errorAccept_, useMaxError_, maxGuesses_);
        }
        public static bool global = true;
      
      
      private double t_;
      private double forward_;
      private double alpha_, beta_, nu_, rho_;
      private bool alphaIsFixed_, betaIsFixed_, nuIsFixed_, rhoIsFixed_;
      private bool vegaWeighted_;
      private EndCriteria endCriteria_;
      private OptimizationMethod optMethod_;
      private double errorAccept_;
      private bool useMaxError_;
      private int maxGuesses_;
    }
}

