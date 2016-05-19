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
using System.Numerics;

namespace QLNet
{
   //! Analytic Heston-Hull-White engine based on the H1-HW approximation
   /*! This class is pricing a european option under the following process

       \f[
       \begin{array}{rcl}
       dS(t, S)  &=& (r-d) S dt +\sqrt{v} S dW_1 \\
       dv(t, S)  &=& \kappa (\theta - v) dt + \sigma \sqrt{v} dW_2 \\
       dr(t)     &=& (\theta(t) - a r) dt + \eta dW_3 \\
       dW_1 dW_2 &=& \rho_{S,v} dt, \rho_{S,r} >= 0 \\
       dW_1 dW_3 &=& \rho_{S.r} dt \\
       dW_2 dW_3 &=& 0 dt \\
       \end{array}
       \f]
   */

   /*! References:

       Lech A. Grzelak, Cornelis W. Oosterlee,
       On The Heston Model with Stochastic,
       http://papers.ssrn.com/sol3/papers.cfm?abstract_id=1382902

       Lech A. Grzelak,
       Equity and Foreign Exchange Hybrid Models for
       Pricing Long-Maturity Financial Derivatives,
       http://repository.tudelft.nl/assets/uuid:a8e1a007-bd89-481a-aee3-0e22f15ade6b/PhDThesis_main.pdf

       \ingroup vanillaengines

       \test the correctness of the returned value is tested by
             reproducing results available in web/literature, testing
             against QuantLib's analytic Heston,
             the Black-Scholes-Merton Hull-White engine and
             the finite difference Heston-Hull-White engine
   */
   public class AnalyticH1HWEngine : AnalyticHestonHullWhiteEngine
   {
      public AnalyticH1HWEngine( HestonModel model, HullWhite hullWhiteModel, double rhoSr, int integrationOrder = 144 )
         :base(model, hullWhiteModel, integrationOrder)
      {
         rhoSr_ = rhoSr;
         Utils.QL_REQUIRE(rhoSr_ >= 0.0,()=> "Fourier integration is not stable if " +
            "the equity interest rate correlation is negative");
      }

      public AnalyticH1HWEngine(HestonModel model,HullWhite hullWhiteModel,double rhoSr, double relTolerance, 
         int maxEvaluations)
         : base( model, hullWhiteModel,relTolerance, maxEvaluations )
      {
         rhoSr_ = rhoSr;
      }

      protected override Complex addOnTerm(double u, double t, int j)
      {
         return  base.addOnTerm(u, t, j)
               + new Fj_Helper(model_, hullWhiteModel_, rhoSr_, t, 0.0, j).value(u);
      }

      private class Fj_Helper
      {
         public Fj_Helper( Handle<HestonModel> hestonModel, HullWhite hullWhiteModel, double rhoSr, double term, 
            double strike, int j)
         {
            j_ = j;
            lambda_ = hullWhiteModel.a();
            eta_ = hullWhiteModel.sigma();
            v0_ = hestonModel.link.v0();
            kappa_ = hestonModel.link.kappa();
            theta_ = hestonModel.link.theta();
            gamma_ = hestonModel.link.sigma();
            d_ = 4.0*kappa_*theta_/(gamma_*gamma_);
            rhoSr_ = rhoSr;
            term_ = term;
         }

         public Complex value(double u)
         {
            double gamma2 = gamma_*gamma_;

            double a, b, c;
            if (8.0*kappa_*theta_/gamma2 > 1.0)
            {
               a = Math.Sqrt(theta_ - gamma2/(8.0*kappa_));
               b = Math.Sqrt(v0_) - a;
               c = -Math.Log((LambdaApprox(1.0) - a)/b);
            }
            else
            {
               a = Math.Sqrt(gamma2/(2.0*kappa_))
                   *Math.Exp(GammaFunction.logValue(0.5*(d_ + 1.0))
                           - GammaFunction.logValue(0.5*d_));

               double t1 = 0.0;
               double t2 = 1.0/kappa_;

               double Lambda_t1 = Math.Sqrt(v0_);
               double Lambda_t2 = Lambda(t2);

               c = Math.Log((Lambda_t2 - a)/(Lambda_t1 - a))/(t1 - t2);
               b = Math.Exp(c*t1)*(Lambda_t1 - a);
            }

            Complex I4 =
               -1.0/lambda_*new Complex(u*u, ((j_ == 1u) ? -u : u))
               *(b/c*(1.0 - Math.Exp(-c*term_))
                 + a*term_
                 + a/lambda_*(Math.Exp(-lambda_*term_) - 1.0)
                 + b/(c - lambda_)*Math.Exp(-c*term_)
                 *(1.0 - Math.Exp(-term_*(lambda_ - c))));

            return eta_*rhoSr_*I4;
         }

      
         private double c(double t) { return gamma_*gamma_/(4*kappa_)*(1.0-Math.Exp(-kappa_*t));}
         private double lambda(double t)
         {
            return  4.0*kappa_*v0_*Math.Exp(-kappa_*t) / ( gamma_ * gamma_ * ( 1.0 - Math.Exp( -kappa_ * t ) ) );
         }
         private double Lambda(double t)
         {
            //const GammaFunction g = new GammaFunction();
            int maxIter = 1000;
            double lambdaT = lambda(t);

            int i=0;
            double retVal = 0.0, s;

            do 
            {
               double k = i;
               s=Math.Exp(k*Math.Log(0.5*lambdaT) + GammaFunction.logValue(0.5*(1+d_)+k)
                        - GammaFunction.logValue(k+1) - GammaFunction.logValue(0.5*d_+k));
               retVal += s;
            } 
            while (s > Double.Epsilon && ++i < maxIter);

            Utils.QL_REQUIRE(i < maxIter,()=> "can not calculate Lambda");

            retVal *= Math.Sqrt(2*c(t)) * Math.Exp(-0.5*lambdaT);
            return retVal;
         }

         private double LambdaApprox(double t)
         {
            return Math.Sqrt( c(t)*(lambda(t)-1.0) + c(t)*d_*(1.0 + 1.0/(2.0*(d_+lambda(t)))));
         }

         private int j_;
         private double lambda_, eta_;
         private double v0_, kappa_, theta_, gamma_;
         private double d_;
         private double rhoSr_;
         private double term_;
      }

      private double rhoSr_;
   }
}
