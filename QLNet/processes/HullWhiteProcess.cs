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
   //! Hull-White stochastic process
   public class HullWhiteProcess : StochasticProcess1D
   {
      public HullWhiteProcess(Handle<YieldTermStructure> h,double a,double sigma)
      {
         process_ = new OrnsteinUhlenbeckProcess(a, sigma, h.link.forwardRate(0.0,0.0,Compounding.Continuous,Frequency.NoFrequency).value());
         h_ = h;
         a_ = a;
         sigma_ = sigma;

         Utils.QL_REQUIRE( a_ >= 0.0,()=> "negative a given" );
         Utils.QL_REQUIRE( sigma_ >= 0.0,()=> "negative sigma given" );
      }
      //! \name StochasticProcess1D interface
      //@{
      public override double x0() { return process_.x0(); }
      public override double drift(double t, double x)
      {
         double alpha_drift = sigma_*sigma_/(2*a_)*(1-Math.Exp(-2*a_*t));
         double shift = 0.0001;
         double f = h_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).value();
         double fup = h_.link.forwardRate(t+shift, t+shift, Compounding.Continuous, Frequency.NoFrequency).value();
         double f_prime = (fup-f)/shift;
         alpha_drift += a_*f+f_prime;
         return process_.drift(t, x) + alpha_drift;
      }
      public override double diffusion( double t, double x ) { return process_.diffusion( t, x ); }
      public override double expectation(double t0, double x0, double dt)
      {
          return process_.expectation(t0, x0, dt)
             + alpha(t0 + dt) - alpha(t0)*Math.Exp(-a_*dt);
      }
      public override double stdDeviation( double t0, double x0, double dt ) { return process_.stdDeviation( t0, x0, dt ); }
      public override double variance( double t0, double x0, double dt ) { return process_.variance( t0, x0, dt ); }

      public double a() { return a_; }
      public double sigma() { return sigma_; }
      public double alpha(double t)
      {
         double alfa = a_ > Const.QL_EPSILON ?
                    (sigma_/a_)*(1 - Math.Exp(-a_*t)) :
                    sigma_*t;
         alfa *= 0.5*alfa;
         alfa += h_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).value();
         return alfa;
      }
      //@}
    
      protected OrnsteinUhlenbeckProcess process_;
      protected Handle<YieldTermStructure> h_;
      protected double a_, sigma_;
   }

   //! %Forward Hull-White stochastic process
   /*! \ingroup processes */
   public class HullWhiteForwardProcess: ForwardMeasureProcess1D 
   {
      public HullWhiteForwardProcess(Handle<YieldTermStructure> h,double a,double sigma)
      {
         process_ = new OrnsteinUhlenbeckProcess(a, sigma, h.link.forwardRate(0.0,0.0,
            Compounding.Continuous,Frequency.NoFrequency).value());
         h_ = h;
         a_ = a;
         sigma_ = sigma;
      }
      //! \name StochasticProcess1D interface
      //@{
      public override double x0() { return process_.x0(); }
      public override double drift(double t, double x)
      {
         double alpha_drift = sigma_*sigma_/(2*a_)*(1-Math.Exp(-2*a_*t));
         double shift = 0.0001;
         double f = h_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).value();
         double fup = h_.link.forwardRate(t+shift, t+shift, Compounding.Continuous, Frequency.NoFrequency).value();
         double f_prime = (fup-f)/shift;
         alpha_drift += a_*f+f_prime;
         return process_.drift(t, x) + alpha_drift - B(t, T_)*sigma_*sigma_;
      }
      public override double diffusion( double t, double x ) { return process_.diffusion( t, x ); }
      public override double expectation(double t0, double x0, double dt)
      {
         return process_.expectation(t0, x0, dt)
             + alpha(t0 + dt) - alpha(t0)*Math.Exp(-a_*dt)
             - M_T(t0, t0+dt, T_);
      }
      public override double stdDeviation( double t0, double x0, double dt ) { return process_.stdDeviation( t0, x0, dt ); }
      public override double variance( double t0, double x0, double dt ) { return process_.variance( t0, x0, dt ); }
      //@}

      public double a() { return a_; }
      public double sigma() { return sigma_; }
      public double alpha(double t)
      {
         double alfa = a_ > Const.QL_EPSILON ?
                    (sigma_/a_)*(1 - Math.Exp(-a_*t)) :
                    sigma_*t;
         alfa *= 0.5*alfa;
         alfa += h_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).value();

         return alfa;
      }
      public double M_T(double s, double t, double T)
      {
         if (a_ > Const.QL_EPSILON) 
         {
            double coeff = (sigma_*sigma_)/(a_*a_);
            double exp1 = Math.Exp(-a_*(t-s));
            double exp2 = Math.Exp(-a_*(T-t));
            double exp3 = Math.Exp(-a_*(T+t-2.0*s));
            return coeff*(1-exp1)-0.5*coeff*(exp2-exp3);
         } 
         else 
         {
            // low-a algebraic limit
            double coeff = (sigma_*sigma_)/2.0;
            return coeff*(t-s)*(2.0*T-t-s);
         }
      }
      public double B(double t, double T)
      {
         return a_ > Const.QL_EPSILON ?
               1/a_ * (1-Math.Exp(-a_*(T-t))) :
               T-t;
      }
   
      protected OrnsteinUhlenbeckProcess process_;
      protected Handle<YieldTermStructure> h_;
      protected double a_, sigma_;   
   }
}
