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
   //! Integral of a one-dimensional function
   /*! Given a target accuracy \f$ \epsilon \f$, the integral of
       a function \f$ f \f$ between \f$ a \f$ and \f$ b \f$ is
       calculated by means of the Gauss-Lobatto formula
   */

   /*! References:
      This algorithm is a C++ implementation of the algorithm outlined in

      W. Gander and W. Gautschi, Adaptive Quadrature - Revisited.
      BIT, 40(1):84-101, March 2000. CS technical report:
      ftp.inf.ethz.ch/pub/publications/tech-reports/3xx/306.ps.gz

      The original MATLAB version can be downloaded here
      http://www.inf.ethz.ch/personal/gander/adaptlob.m
   */
   public class GaussLobattoIntegral : Integrator
   {
      

      public GaussLobattoIntegral( int maxIterations,
                                   double? absAccuracy,
                                   double? relAccuracy = null,
                                   bool useConvergenceEstimate = true)
         :base(absAccuracy, maxIterations)
      {
         relAccuracy_ = relAccuracy;
         useConvergenceEstimate_ = useConvergenceEstimate;
      }

      protected override double integrate (Func<double, double> f, double a, double b)
      {
         setNumberOfEvaluations( 0 );
         double calcAbsTolerance = calculateAbsTolerance( f, a, b );

         increaseNumberOfEvaluations( 2 );
         return adaptivGaussLobattoStep( f, a, b, f( a ), f( b ), calcAbsTolerance );
      }

      protected double adaptivGaussLobattoStep(Func<double, double> f,double a, double b, double fa, double fb, double acc)
      {
         Utils.QL_REQUIRE(numberOfEvaluations() < maxEvaluations(),()=> "max number of iterations reached");
        
         double h=(b-a)/2; 
         double m=(a+b)/2;
        
         double mll=m-alpha_*h; 
         double ml =m-beta_*h; 
         double mr =m+beta_*h; 
         double mrr=m+alpha_*h;
        
         double fmll= f(mll);
         double fml = f(ml);
         double fm  = f(m);
         double fmr = f(mr);
         double fmrr= f(mrr);
         increaseNumberOfEvaluations(5);
        
         double integral2=(h/6)*(fa+fb+5*(fml+fmr));
         double integral1=(h/1470)*(77*(fa+fb)
                                       +432*(fmll+fmrr)+625*(fml+fmr)+672*fm);
        
         // avoid 80 bit logic on x86 cpu
         double dist = acc + (integral1-integral2);
         if( dist==acc || mll<=a || b<=mrr) 
         {
            Utils.QL_REQUIRE(m>a && b>m,()=> "Interval contains no more machine number");
            return integral1;
         }
         else 
         {
            return  adaptivGaussLobattoStep(f,a,mll,fa,fmll,acc)  
                    + adaptivGaussLobattoStep(f,mll,ml,fmll,fml,acc)
                    + adaptivGaussLobattoStep(f,ml,m,fml,fm,acc)
                    + adaptivGaussLobattoStep(f,m,mr,fm,fmr,acc)
                    + adaptivGaussLobattoStep(f,mr,mrr,fmr,fmrr,acc)
                    + adaptivGaussLobattoStep(f,mrr,b,fmrr,fb,acc);
         }

      }

      protected double calculateAbsTolerance( Func<double, double> f, double a, double b )
      {
         double relTol = Math.Max(relAccuracy_ ?? 0, Const.QL_EPSILON);
        
         double m = (a+b)/2; 
         double h = (b-a)/2;
         double y1 = f(a);
         double y3 = f(m-alpha_*h);
         double y5 = f(m-beta_*h);
         double y7 = f(m);
         double y9 = f(m+beta_*h);
         double y11= f(m+alpha_*h);
         double y13= f(b);

         double f1 = f(m-x1_*h);
         double f2 = f(m+x1_*h);
         double f3 = f(m-x2_*h);
         double f4 = f(m+x2_*h);
         double f5 = f(m-x3_*h);
         double f6 = f(m+x3_*h);

         double acc=h*(0.0158271919734801831*(y1+y13)
                       +0.0942738402188500455*(f1+f2)
                       +0.1550719873365853963*(y3+y11)
                       +0.1888215739601824544*(f3+f4)
                       +0.1997734052268585268*(y5+y9) 
                       +0.2249264653333395270*(f5+f6)
                       +0.2426110719014077338*y7);  
        
         increaseNumberOfEvaluations(13);
         if (acc == 0.0 && (   f1 != 0.0 || f2 != 0.0 || f3 != 0.0
                           || f4 != 0.0 || f5 != 0.0 || f6 != 0.0)) 
         {
            Utils.QL_FAIL("can not calculate absolute accuracy from relative accuracy");
         }

         double r = 1.0;
         if (useConvergenceEstimate_) 
         {
            double integral2 = (h/6)*(y1+y13+5*(y5+y9));
            double integral1 = (h/1470)*(77*(y1+y13)+432*(y3+y11)+
                                             625*(y5+y9)+672*y7);
        
            if (Math.Abs(integral2-acc) != 0.0) 
                  r = Math.Abs(integral1-acc)/Math.Abs(integral2-acc);
            if (r == 0.0 || r > 1.0)
                  r = 1.0;
         }

         if (relAccuracy_ != null)
            if ( absoluteAccuracy() != null )
               return Math.Min(absoluteAccuracy().GetValueOrDefault(), acc*relTol)/(r*Const.QL_EPSILON);
            else
               return ( acc * relTol ) / ( r * Const.QL_EPSILON );
         else 
            return absoluteAccuracy().GetValueOrDefault()/(r*Const.QL_EPSILON);

      }

      protected double? relAccuracy_;
      protected bool useConvergenceEstimate_;
      protected static double alpha_ = Math.Sqrt( 2.0 / 3.0 );
      protected static double beta_ = 1.0 / Math.Sqrt( 5.0 );
      protected static double x1_ = 0.94288241569547971906;
      protected static double x2_ = 0.64185334234578130578;
      protected static double x3_ = 0.23638319966214988028;
   }
}
