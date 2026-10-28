/*
 Copyright (C) 2008-2025 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
using System.Numerics;

namespace QLNet
{
   /// <summary>
   /// Pricing engine for European discrete geometric average-price Asian option
   /// </summary>
   /// <remarks>
   /// This class implements a discrete geometric average price
   /// Asian option with European exercise under the Heston stochastic
   /// vol model where spot and variance follow the processes
   /// </remarks>
   public class AnalyticDiscreteGeometricAveragePriceAsianHestonEngine
      :DiscreteAveragingAsianOption.Engine
   {
      // Initial process params
      private double v0_, rho_, kappa_, theta_, sigma_, logS0_;
      private Handle<YieldTermStructure> dividendYield_;
      private Handle<YieldTermStructure> riskFreeRate_;
      private Handle<Quote> s0_;
      HestonProcess process_;
      // A lookup table for the reuslts of omega_tilde() to avoid repeated calls for given Phi call
      Dictionary<int, Complex> omegaTildeLookupTable_;

      // Cutoff parameter for integral in Eqs (23) and (24)
      double xiRightLimit_;

      // Integrator for equation (23) and (24)
      GaussLegendreIntegration integrator_;

      // We need to set up several variables inside calculate as they depend on fixing times. Rather
      // than pass them between a, omega, F etc. which makes for very messy method signatures, we
      // make them mutable class properties instead.
      double tr_t_;
      double Tr_T_;
      List<double> tkr_tk_;

      // Equation (11)
      private Complex F(Complex z1, Complex z2, double tau)
      {
         var temp = Complex.Sqrt(kappa_*kappa_-2.0*z1*sigma_*sigma_);
         if (Math.Abs(kappa_*kappa_-2.0*sigma_*sigma_) < 1e-8)
            return 1.0 + 0.5*(kappa_-z2*sigma_*sigma_);
         
         return Complex.Cosh(0.5*tau*temp) + (kappa_-z2*sigma_*sigma_)*Complex.Sinh(0.5*tau*temp)/temp;
      }

      private Complex F_tilde(Complex z1, Complex z2, double tau)
      {
         var temp = Complex.Sqrt(kappa_*kappa_ - 2.0*z1*sigma_*sigma_);
         return 0.5*temp*Complex.Sinh(0.5*tau*temp) + 0.5*(kappa_ - z2*sigma_*sigma_)*Complex.Cosh(0.5*tau*temp);
      }

      // Equation (14)
      private Complex z(Complex s, Complex w, int k, int n)
      {
         var k_ = (double)k;
         var n_ = (double)n;
         var term1 = (2*rho_*kappa_ - sigma_)*((n_-k_+1)*s + n_*w)/(2*sigma_*n_);
         var term2 = (1-rho_*rho_)*Complex.Pow(((n_-k_+1)*s + n_*w), 2)/(2*n_*n_);

         return term1 + term2;
      }

      // Equation (15)
      private Complex omega(Complex s, Complex w, int k, int kStar, int n)
      {
         if (k==kStar)
            return 0;
         if (k==n+1) 
            return rho_*w/sigma_;
          
         return rho_*s/(sigma_*n);
      }

      // Equation (16)
      private Complex a(Complex s, Complex w, double t, double T, int kStar, List<double> t_n)
      {
         var kStar_ = (double)kStar;
         var n_ = (double)t_n.Count;
         var temp = -rho_*kappa_*theta_/sigma_;

         var summation = 0.0;
         var summation2 = 0.0;
         for (var i=kStar+1; i<=t_n.Count; i++)
         {
            summation += t_n[i-1];
            summation2 += tkr_tk_[i-1];
         }
         // This is Eq (16) modified for non-constant rates
         var term1 = (s*(n_-kStar_)/n_ + w)*(logS0_ - rho_*v0_/sigma_ - t*temp - tr_t_);
         var term2 = temp*(s*summation/n_ + w*T) + w*Tr_T_ + summation2*s/n_;

         return term1 + term2;
      }

      // Equation (19)
      private Complex omega_tilde(Complex s, Complex w, int k, int kStar, int n, List<double> tauK)
      {
         var omega_k = omega(s, w, k, kStar, n);
         if (k==n+1) 
            return omega_k;
         
         var dTauk = tauK[k+1] - tauK[k];
         var z_kp1 = z(s, w, k+1, n);

         Complex omega_kp1;
         if (!omegaTildeLookupTable_.TryGetValue(k + 1, out omega_kp1))
         {
            omega_kp1 = omega_tilde(s, w, k + 1, kStar, n, tauK);
         }

         var ratio = F_tilde(z_kp1,omega_kp1,dTauk)/F(z_kp1,omega_kp1,dTauk);
         var result = omega_k + kappa_/Complex.Pow(sigma_,2) - 2.0*ratio/Complex.Pow(sigma_,2);

         // Store this value in our mutable lookup map
         omegaTildeLookupTable_[k] = result;

         return result;
      }

      public AnalyticDiscreteGeometricAveragePriceAsianHestonEngine(HestonProcess process, double xiRightLimit = 100.0)
      {
         process_ = process;
         xiRightLimit_ = xiRightLimit;
         integrator_ = new GaussLegendreIntegration(128);
         process_.registerWith(update);

         v0_ = process_.v0();
         rho_ = process_.rho();
         kappa_ = process_.kappa();
         theta_ = process_.theta();
         sigma_ = process_.sigma();
         s0_ = process_.s0();
         logS0_ = Math.Log(s0_.link.value());

         riskFreeRate_ = process_.riskFreeRate();
         dividendYield_ = process_.dividendYield();
      }

      public override void calculate()
      {
        /* this engine cannot really check for the averageType==Geometric
           since it can be used as control variate for the Arithmetic version
        QL_REQUIRE(arguments_.averageType == Average::Geometric,
                   "not a geometric average option");
        */
        Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,
           ()=> "not an European Option");

        double runningLog;
        int pastFixings;
        if (arguments_.averageType == Average.Type.Geometric)
        {
            Utils.QL_REQUIRE(arguments_.runningAccumulator>0.0,()=> "positive running product required: "
                                                                    + arguments_.runningAccumulator + " not allowed");
            runningLog = Math.Log(arguments_.runningAccumulator.GetValueOrDefault());
            pastFixings = arguments_.pastFixings.GetValueOrDefault();
        } else
        {
           // it is being used as control variate
           runningLog = 0.0;
           pastFixings = 0;
        }

        var payoff = arguments_.payoff as PlainVanillaPayoff;
        Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");

        var strike = payoff.strike();
        var exercise = arguments_.exercise.lastDate();

        var expiryTime = this.process_.time(exercise);
        Utils.QL_REQUIRE(expiryTime >= 0.0,()=> "Expiry Date cannot be in the past");

        var expiryDcf = riskFreeRate_.link.discount(expiryTime);

        var startTime = 0.0;
        var fixingTimes = new List<double>();
        var tauK = new List<double>();
        foreach (var fixingDate in arguments_.fixingDates)
        {
            fixingTimes.Add(this.process_.time(fixingDate));
        }
        fixingTimes.Sort();
        fixingTimes.ForEach(time => tauK.Add(time));
        
        // tauK is just a vector of the sorted future fixing times (ie. from the kStar element
        // onwards), with t pushed on the front and T pushed on the back!
        tauK.Insert(0, startTime);
        tauK.Add(expiryTime);

        // In the paper, seasoned asians are dealt with by letting the start time variable be greater
        // than 0. We can achieve the same by fixing the start time to 0.0, but attaching 'dummy'
        // fixing times at t=-1 for each past fixing, at the front of the fixing times arrays
        for (var i=0; i<pastFixings; i++)
        {
            fixingTimes.Insert(0, -1.0);
            tauK.Insert(0, -1.0);
        }

        var kStar = pastFixings;

        // Need the log of some discount factors to calculate the r-adjusted a factor (Eq 16)
        tr_t_ = 0;
        Tr_T_ = 0;
        tkr_tk_ = new List<double>();
        tr_t_ = -Math.Log(riskFreeRate_.link.discount(startTime) / dividendYield_.link.discount(startTime));
        Tr_T_ = -Math.Log(riskFreeRate_.link.discount(expiryTime) / dividendYield_.link.discount(expiryTime));
        foreach (var fixingTime in fixingTimes)
        {
            if (fixingTime < 0)
            {
                tkr_tk_.Add(1.0);
            }
            else
            {
                tkr_tk_.Add(-Math.Log(riskFreeRate_.link.discount(fixingTime) /
                  dividendYield_.link.discount(fixingTime)));
            }
        }

        // To account for seasoning, we need to calculate an 'adjusted' strike (Eq 6)
        var prefactor = Math.Exp(runningLog / fixingTimes.Count);
        var adjustedStrike = strike / prefactor;

        // Calculate the two terms in eq (23) - Phi(1,0) is real (asian forward) but need to type convert
        var term1 = 0.5 * (Phi(1,0, startTime, expiryTime, kStar, fixingTimes, tauK).Real - adjustedStrike);

        var integrand = new Integrand(startTime, expiryTime, kStar, fixingTimes, tauK, adjustedStrike, this, xiRightLimit_);
        var term2 = integrator_.value(integrand.value) / Math.PI;

        // Apply the payoff functions
        var value = 0.0;
        switch (payoff.optionType())
        {
            case Option.Type.Call:
                value = expiryDcf * prefactor * (term1 + term2);
                break;
            case Option.Type.Put:
                value = expiryDcf * prefactor * (-term1 + term2);
                break;
            default:
                Utils.QL_FAIL("unknown option type");
                break;
        }

        results_.value = value;

        results_.additionalResults["dcf"] = expiryDcf;
        results_.additionalResults["s0"] = s0_.link.value();
        results_.additionalResults["strike"] = strike;
        results_.additionalResults["expiryTime"] = expiryTime;
        results_.additionalResults["term1"] = term1;
        results_.additionalResults["term2"] = term2;
        results_.additionalResults["xiRightLimit"] = xiRightLimit_;
        results_.additionalResults["fixingTimes"] = fixingTimes;
        results_.additionalResults["tauK"] = tauK;
        results_.additionalResults["adjustedStrike"] = adjustedStrike;
        results_.additionalResults["prefactor"] = prefactor;
        results_.additionalResults["kStar"] = kStar;
      }

      // Equation (21) - must be public so the integrand can access it.
      public Complex Phi(Complex s, Complex w, double t, double T, int kStar, List<double> t_n, List<double> tauK)
      {
         // Clear the mutable lookup map before evaluating Phi
         omegaTildeLookupTable_ = new Dictionary<int, Complex>();

         var n = t_n.Count;
         var aTerm = a(s, w, t, T, kStar, t_n);
         var omegaTerm = v0_*omega_tilde(s, w, kStar, kStar, n, tauK);
         var term3 = kappa_*kappa_*theta_*(T-t)/Complex.Pow(sigma_,2);

         Complex summation = 0.0;
         for (var i=kStar+1; i<=n+1; i++)
         {
            var dTau = tauK[i] - tauK[i-1];
            var z_k = z(s, w, i, n);
            var omega_tilde_k = omega_tilde(s, w, i, kStar, n, tauK);

            summation += Complex.Log(F(z_k, omega_tilde_k, dTau));
         }
         var term4 = 2*kappa_*theta_*summation/Complex.Pow(sigma_,2);

         return Complex.Exp(aTerm + omegaTerm + term3 - term4);
      }

      private class Integrand
      {
         private double t_, T_, K_, logK_;
         private int kStar_;
         private List<double> t_n_, tauK_;
         private  AnalyticDiscreteGeometricAveragePriceAsianHestonEngine parent_;
         private double xiRightLimit_;
         private Complex i_;

         public Integrand(double t, double T, int kStar, List<double> t_n, List<double> tauK,double K,
            AnalyticDiscreteGeometricAveragePriceAsianHestonEngine parent, double xiRightLimit)
         {
            t_ = t;
            T_ = T;
            K_ = K;
            logK_ = Math.Log(K);
            kStar_ = kStar;
            t_n_ = t_n;
            tauK_ = tauK;
            parent_ = parent;
            xiRightLimit_ = xiRightLimit;
            i_ = new Complex(0.0, 1.0);
         }

         public double value(double xi)
         {
            var xiDash = (0.5+1e-8+0.5*xi) * xiRightLimit_; // Map xi to full range

            Complex inner1 = parent_.Phi(1.0 + xiDash*i_, 0, t_, T_, kStar_, t_n_, tauK_);
            Complex inner2 = -K_*parent_.Phi(xiDash*i_, 0, t_, T_, kStar_, t_n_, tauK_);

            return 0.5*xiRightLimit_*((inner1 + inner2) * Complex.Exp(-xiDash*logK_*i_) / (xiDash*i_)).Real;
         }
      }
   }
}
