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
   //! %Abcd functional form for instantaneous volatility
   /*! \f[ f(T-t) = [ a + b(T-t) ] e^{-c(T-t)} + d \f]
       following Rebonato's notation. */
   public class AbcdFunction : AbcdMathFunction
   {
      public AbcdFunction(double a = -0.06, double b =  0.17, double c =  0.54, double d =  0.17)
         :base(a , b, c , d)
      {}

      //! maximum value of the volatility function
      public double maximumVolatility() { return maximumValue(); }

      //! volatility function value at time 0: \f[ f(0) \f]
      public double shortTermVolatility() { return new AbcdFunction().value( 0.0 ); }

      //! volatility function value at time +inf: \f[ f(\inf) \f]
      public double longTermVolatility() { return longTermValue(); }

      /*! instantaneous covariance function at time t between T-fixing and
         S-fixing rates \f[ f(T-t)f(S-t) \f] */
      public double covariance( double t, double T, double S )
      {
         return new AbcdFunction().value( T - t ) * new AbcdFunction().value( S - t );
      }

      /*! integral of the instantaneous covariance function between
         time t1 and t2 for T-fixing and S-fixing rates
         \f[ \int_{t1}^{t2} f(T-t)f(S-t)dt \f] */
      public double covariance( double t1, double t2, double T, double S )
      {
         Utils.QL_REQUIRE(t1<=t2,()=> "integrations bounds (" + t1 + "," + t2 + ") are in reverse order");
         double cutOff = Math.Min(S,T);
         if (t1>=cutOff) 
         {
            return 0.0;
         } 
         else 
         {
            cutOff = Math.Min(t2, cutOff);
            return primitive(cutOff, T, S) - primitive(t1, T, S);
         }
      }

      /*! average volatility in [tMin,tMax] of T-fixing rate:
         \f[ \sqrt{ \frac{\int_{tMin}^{tMax} f^2(T-u)du}{tMax-tMin} } \f] */
      public double volatility( double tMin, double tMax, double T )
      {
         if (tMax==tMin)
            return instantaneousVolatility(tMax, T);
         Utils.QL_REQUIRE(tMax>tMin,()=> "tMax must be > tMin");
         return Math.Sqrt(variance(tMin, tMax, T)/(tMax-tMin));
      }

      /*! variance between tMin and tMax of T-fixing rate:
         \f[ \frac{\int_{tMin}^{tMax} f^2(T-u)du}{tMax-tMin} \f] */
      public double variance( double tMin, double tMax, double T )
      {
         return covariance( tMin, tMax, T, T );
      }
        

        
      // INSTANTANEOUS
      /*! instantaneous volatility at time t of the T-fixing rate:
         \f[ f(T-t) \f] */
      public double instantaneousVolatility( double u, double T )
      {
          return Math.Sqrt(instantaneousVariance(u, T));
      }

      /*! instantaneous variance at time t of T-fixing rate:
         \f[ f(T-t)f(T-t) \f] */
      public double instantaneousVariance( double u, double T )
      {
         return instantaneousCovariance( u, T, T );
      }

      /*! instantaneous covariance at time t between T and S fixing rates:
         \f[ f(T-u)f(S-u) \f] */
      public double instantaneousCovariance( double u, double T, double S )
      {
         return new AbcdFunction().value( T - u ) * new AbcdFunction().value( S - u );
      }

      // PRIMITIVE
      /*! indefinite integral of the instantaneous covariance function at
         time t between T-fixing and S-fixing rates
         \f[ \int f(T-t)f(S-t)dt \f] */
      public double primitive( double t, double T, double S )
      {
         if (T<t || S<t) return 0.0;

         if (Utils.close(c_,0.0)) 
         {
            double v = a_+d_;
            return t*(v*v+v*b_*S+v*b_*T-v*b_*t+b_*b_*S*T-0.5*b_*b_*t*(S+T)+b_*b_*t*t/3.0);
         }

         double k1=Math.Exp(c_*t), 
                k2=Math.Exp(c_*S), 
                k3=Math.Exp(c_*T);

         return (b_*b_*(-1 - 2*c_*c_*S*T - c_*(S + T)
            + k1*k1*(1 + c_*(S + T - 2*t) + 2*c_*c_*(S - t)*(T - t)))
            + 2*c_*c_*(2*d_*a_*(k2 + k3)*(k1 - 1)
            +a_*a_*(k1*k1 - 1)+2*c_*d_*d_*k2*k3*t)
            + 2*b_*c_*(a_*(-1 - c_*(S + T) + k1*k1*(1 + c_*(S + T - 2*t)))
            -2*d_*(k3*(1 + c_*S) + k2*(1 + c_*T)
            - k1*k3*(1 + c_*(S - t))
            - k1*k2*(1 + c_*(T - t)))
            )
            ) / (4*c_*c_*c_*k2*k3);
      }
   }

   // Helper class used by unit tests
   public class AbcdSquared 
   {
      public AbcdSquared( double a, double b, double c, double d, double T, double S )
      {
         abcd_ = new AbcdFunction(a, b, c, d);
         T_ = T;
         S_ = S;
      }

      public double value(double t)
      {
         return abcd_.covariance( t, T_, S_ );
      }
      
      private AbcdFunction abcd_;
      private double T_, S_;
    }
}
