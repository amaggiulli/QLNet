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
   /// Pricing engine for European continuous geometric average price Asian
   /// </summary>
   /// <remarks>
   /// This class implements a continuous geometric average price
   /// Asian option with European exercise under the Heston stochastic
   /// vol model where spot and variance follow the processes
   ///
   /// Implements the analytical solution for continuous geometric Asian
   /// options developed in "Pricing of geometric Asian options under
   /// Heston's stochastic volatility model", B. Kim &amp; I. S. Wee, Quantative
   /// Finance 14:10, 1795-1809 (2014)
   /// </remarks>
   public class AnalyticContinuousGeometricAveragePriceAsianHestonEngine
      : ContinuousAveragingAsianOption.Engine
   {
      // Initial process params
      private double v0_, rho_, kappa_, theta_, sigma_;
      private Handle<YieldTermStructure> dividendYield_;
      private Handle<YieldTermStructure> riskFreeRate_;
      private Handle<Quote> s0_;
      private HestonProcess process_;
      // Some intermediate calculation constant parameters
      private double a1_, a2_;
      private double a3_ = 0.0, a4_ = 0.0, a5_ = 0.0;
      private Dictionary<int, Complex> fLookupTable_;

      // Cutoff parameters for summation (19), (20) and for integral (29)
      private int summationCutoff_;
      private double xiRightLimit_;

      // Integrator for equation (29)
      private GaussLegendreIntegration integrator_;

      // Integrands
      private class Integrand
      {
         private double t_ = 0.0, T_, K_, logK_;
         int cutoff_;
         AnalyticContinuousGeometricAveragePriceAsianHestonEngine parent_;
         double xiRightLimit_;
         Complex i_;

         public Integrand(double T, int cutoff, double K,
            AnalyticContinuousGeometricAveragePriceAsianHestonEngine parent,
            double xiRightLimit)
         {
            T_ = T;
            K_ = K;
            logK_= Math.Log(K);
            cutoff_ = cutoff;
            parent_ = parent;
            xiRightLimit_ = xiRightLimit;
            i_ = new Complex(0.0, 1.0);
         }

         public double value(double xi)
         {
            var xiDash = (0.5+1e-8+0.5*xi) * xiRightLimit_; // Map xi to full range

            var inner1 = parent_.Phi(1.0 + xiDash*i_, 0, T_, t_, cutoff_);
            var inner2 = - K_*parent_.Phi(xiDash*i_, 0, T_, t_, cutoff_);

            return 0.5*xiRightLimit_*((inner1 + inner2) * Complex.Exp(-xiDash*logK_*i_) / (xiDash*i_)).Real;
         }
      }

      private class DcfIntegrand
      {
         private double t_, T_, denominator_;
         private Handle<YieldTermStructure> riskFreeRate_;
         private Handle<YieldTermStructure> dividendYield_;

         public DcfIntegrand(double t, double T, Handle<YieldTermStructure> riskFreeRate,
            Handle<YieldTermStructure> dividendYield)
         {
            t_ = t;
            T_ = T;
            riskFreeRate_ = riskFreeRate;
            dividendYield_ = dividendYield;
            denominator_ = Math.Log(riskFreeRate_.link.discount(t_)) - Math.Log(dividendYield_.link.discount(t_));
         }

         public double value(double u)
         {
            var uDash = (0.5 + 1e-8 + 0.5 * u) * (T_ - t_) + t_; // Map u to full range
            return 0.5 * (T_ - t_) * (-Math.Log(riskFreeRate_.link.discount(uDash)) +
                                      Math.Log(dividendYield_.link.discount(uDash)) + denominator_);
         }
      }

      // Equations (13)
      private Complex z1_f(Complex s, Complex w, double T)
      {
         return s * s * (1 - rho_ * rho_) / (2 * T * T);
      }

      private Complex z2_f(Complex s, Complex w, double T)
      {
         return s * (2 * rho_ * kappa_ - sigma_) / (2 * sigma_ * T) + s * w * (1 - rho_ * rho_) / T;
      }

      private Complex z3_f(Complex s, Complex w, double T)
      {
         return s * rho_ / (sigma_ * T) + 0.5 * w * (2 * rho_ * kappa_ - sigma_) / sigma_ +
                0.5 * w * w * (1 - rho_ * rho_);
      }

      private Complex z4_f(Complex s, Complex w)
      {
         return w * rho_ / sigma_;
      }

      // Equations (19), (20)
      Pair<Complex, Complex> F_F_tilde(Complex z1, Complex z2, Complex z3, Complex z4, double tau, int cutoff = 50)
      {
         Complex temp = 0.0;
         Complex runningSum1 = 0.0;
         Complex runningSum2 = 0.0;

         for (var i = 0; i < cutoff; i++)
         {
            temp = f(z1, z2, z3, z4, i, tau);
            runningSum1 += temp;
            runningSum2 += temp * (double)(i) / tau;
         }

         var result = new Pair<Complex, Complex>(runningSum1, runningSum2);

         return result;
      }

      // Equation (21)
      private Complex f(Complex z1, Complex z2, Complex z3, Complex z4, int n, double tau)
      {
         Complex result;

         // This equation is highly recursive, use dynamic programming with a mutable variable
         // to record the results of previous calls
         if (n < 2)
         {
            if (n < 0)
            {
               result = 0.0;
            }
            else if (n == 0)
            {
               result = 1.0;
            }
            else
            {
               result = 0.5 * (kappa_ - z4 * sigma_ * sigma_) * tau;
            }
         }
         else
         {
            Complex[] fMinusN = [0, 0, 0, 0];
            double prefactor = -0.5 * sigma_ * sigma_ * tau * tau / (n * (n - 1));

            // For each offset, look up the value in the map and only evaluate function if it's not there
            for (var offset = 1; offset < 5; offset++)
            {
               var location = n - offset;
               var isValid = fLookupTable_.TryGetValue(location, out var position);
               if (isValid)
               {
                  fMinusN[offset - 1] = position;
               }
               else
               {
                  fMinusN[offset - 1] = f(z1, z2, z3, z4, location, tau);
               }
            }

            result = prefactor * (z1 * tau * tau * fMinusN[3] + z2 * tau * fMinusN[2] +
                                  (z3 - 0.5 * kappa_ * kappa_ / (sigma_ * sigma_)) * fMinusN[1]);
         }

         // Store this value in our mutable lookup map
         fLookupTable_[n] = result;

         return result;
      }

      public AnalyticContinuousGeometricAveragePriceAsianHestonEngine(HestonProcess process, int summationCutoff = 50,
         double xiRightLimit = 100.0)
      {
         process_ = process;
         summationCutoff_ = summationCutoff;
         xiRightLimit_ = xiRightLimit;
         integrator_ = new GaussLegendreIntegration(128);
         process_.registerWith(update);

         v0_ = process_.v0();
         rho_ = process_.rho();
         kappa_ = process_.kappa();
         theta_ = process_.theta();
         sigma_ = process_.sigma();
         s0_ = process_.s0();

         riskFreeRate_ = process_.riskFreeRate();
         dividendYield_ = process_.dividendYield();

         // Some of the required constant intermediate variables can be calculated now
         // (although anything depending on T will need to be calculated dynamically later)
         a1_ = 2.0 * v0_ / (sigma_ * sigma_);
         a2_ = 2.0 * kappa_ * theta_ / (sigma_ * sigma_);
      }

      public override void calculate()
      {
         Utils.QL_REQUIRE(arguments_.averageType == Average.Type.Geometric, () => "not a geometric average option");
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European, () => "not an European Option");

         var payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

         var strike = payoff.strike();
         var exercise = arguments_.exercise.lastDate();

         var expiryTime = this.process_.time(exercise);
         Utils.QL_REQUIRE(expiryTime >= 0.0, () => "Expiry Date cannot be in the past");

         var expiryDcf = riskFreeRate_.link.discount(expiryTime);
         var expiryDividendDiscount = dividendYield_.link.discount(expiryTime);

         // TODO: extend to cover seasoned options (discussed in paper)
         var startTime = 0.0;

         // These parameters only need to be calculated once per pricing, but are
         // functions of t and T so need to be reset in calculate()
         var t = startTime;
         var T = expiryTime;
         var tau = T - t;
         var logS0 = Math.Log(s0_.link.value());

         // To deal with non-constant rates and dividends, we reformulate Eq.s (14) to (17) with
         // r_ --> (r(t) - q(t)), which gives the new expressions for a3 and a4 used below
         var dcf = riskFreeRate_.link.discount(T) / riskFreeRate_.link.discount(t);
         var qdcf = dividendYield_.link.discount(T) / dividendYield_.link.discount(t);
         var dcfIntegrand = new DcfIntegrand(t, T, riskFreeRate_, dividendYield_);
         var integratedDcf = integrator_.value(dcfIntegrand.value);

         a3_ = (tau * logS0 + integratedDcf) / T - kappa_ * theta_ * rho_ * tau * tau / (2 * sigma_ * T) -
               rho_ * tau * v0_ / (sigma_ * T);
         a4_ = logS0 * qdcf / dcf - rho_ * v0_ / sigma_ + rho_ * kappa_ * theta_ * tau / sigma_;
         a5_ = (kappa_ * v0_ + kappa_ * kappa_ * theta_ * tau) / (sigma_ * sigma_);

         // Calculate the two terms in eq (29) - Phi(1,0) is real (asian forward) but need to type convert
         var term1 = 0.5 * ((Phi(1, 0, T, t, summationCutoff_)).Real - strike);

         var integrand = new Integrand(T, summationCutoff_, strike, this, xiRightLimit_);
         var term2 = integrator_.value(integrand.value) / Const.M_PI;

         // Apply the payoff functions
         var value = 0.0;
         switch (payoff.optionType())
         {
            case Option.Type.Call:
               value = expiryDcf * (term1 + term2);
               break;
            case Option.Type.Put:
               value = expiryDcf * (-term1 + term2);
               break;
            default:
               Utils.QL_FAIL("unknown option type");
               break;
         }

         results_.value = value;

         results_.additionalResults["dcf"] = expiryDcf;
         results_.additionalResults["qf"] = expiryDividendDiscount;
         results_.additionalResults["s0"] = s0_.link.value();
         results_.additionalResults["strike"] = strike;
         results_.additionalResults["expiryTime"] = expiryTime;
         results_.additionalResults["exercise"] = exercise;

         results_.additionalResults["term1"] = term1;
         results_.additionalResults["term2"] = term2;
         results_.additionalResults["xiRightLimit"] = xiRightLimit_;
         results_.additionalResults["summationCutoff"] = summationCutoff_;

         results_.additionalResults["a1"] = a1_;
         results_.additionalResults["a2"] = a2_;
         results_.additionalResults["a3"] = a3_;
         results_.additionalResults["a4"] = a4_;
         results_.additionalResults["a5"] = a5_;
      }

      // Phi, defined in eq (25). Must be public so the integrand can access it (Could
      // use friend functions I think, but perhaps overkill?)
      public Complex Phi(Complex s, Complex w, double T, double t = 0.0, int cutoff = 50)
      {
         var tau = T - t;

         Complex z1 = z1_f(s, w, T);
         Complex z2 = z2_f(s, w, T);
         Complex z3 = z3_f(s, w, T);
         Complex z4 = z4_f(s, w);

         // Clear the mutable lookup map before calling fLookupTable
         fLookupTable_ = new Dictionary<int, Complex>();
         var temp = F_F_tilde(z1, z2, z3, z4, tau, cutoff);

         Complex F, F_tilde;
         F = temp.first;
         F_tilde = temp.second;

         return Complex.Exp(-a1_ * F_tilde / F - a2_ * Complex.Log(F) + a3_ * s + a4_ * w + a5_);
      }

    }
}
