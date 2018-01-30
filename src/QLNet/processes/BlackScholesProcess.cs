﻿/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
   //! Generalized Black-Scholes stochastic process
   /*! This class describes the stochastic process \f$ S \f$ governed by
       \f[
           d\ln S(t) = (r(t) - q(t) - \frac{\sigma(t, S)^2}{2}) dt
                    + \sigma dW_t.
       \f]

       \warning while the interface is expressed in terms of \f$ S \f$,
                the internal calculations work on \f$ ln S \f$.

       \ingroup processes
   */

   public class GeneralizedBlackScholesProcess : StochasticProcess1D
   {
      public GeneralizedBlackScholesProcess(Handle<Quote> x0, Handle<YieldTermStructure> dividendTS,
         Handle<YieldTermStructure> riskFreeTS, Handle<BlackVolTermStructure> blackVolTS, IDiscretization1D disc = null)
         : base(disc ?? new EulerDiscretization())
      {
         x0_ = x0;
         riskFreeRate_ = riskFreeTS;
         dividendYield_ = dividendTS;
         blackVolatility_ = blackVolTS;
         updated_ = false;

         x0_.registerWith(update);
         riskFreeRate_.registerWith(update);
         dividendYield_.registerWith(update);
         blackVolatility_.registerWith(update);
      }

      public GeneralizedBlackScholesProcess(Handle<Quote> x0, Handle<YieldTermStructure> dividendTS,
        Handle<YieldTermStructure> riskFreeTS, Handle<BlackVolTermStructure> blackVolTS,
        RelinkableHandle<LocalVolTermStructure> localVolTS, IDiscretization1D disc = null)
          : base(disc ?? new EulerDiscretization())
      {
         x0_ = x0;
         riskFreeRate_ = riskFreeTS;
         dividendYield_ = dividendTS;
         blackVolatility_ = blackVolTS;
         localVolatility_ = localVolTS != null ? (localVolTS.empty() ? new RelinkableHandle<LocalVolTermStructure>() : localVolTS)
                                               : new RelinkableHandle<LocalVolTermStructure>();
         updated_ = !localVolatility_.empty();

         x0_.registerWith(update);
         riskFreeRate_.registerWith(update);
         dividendYield_.registerWith(update);
         blackVolatility_.registerWith(update);
         localVolatility_.registerWith(update);
      }

      public override double x0()
      {
         return x0_.link.value();
      }

      /*! \todo revise extrapolation */

      public override double drift(double t, double x)
      {
         double sigma = diffusion(t, x);
         // we could be more anticipatory if we know the right dt for which the drift will be used
         double t1 = t + 0.0001;
         return riskFreeRate_.link.forwardRate(t, t1, Compounding.Continuous, Frequency.NoFrequency, true).rate()
                - dividendYield_.link.forwardRate(t, t1, Compounding.Continuous, Frequency.NoFrequency, true).rate()
                - 0.5 * sigma * sigma;
      }

      /*! \todo revise extrapolation */

      public override double diffusion(double t, double x)
      {
         return localVolatility().link.localVol(t, x, true);
      }

      public override double apply(double x0, double dx)
      {
         return x0 * Math.Exp(dx);
      }

      /*! \warning raises a "not implemented" exception.  It should
             be rewritten to return the expectation E(S) of
             the process, not exp(E(log S)).
      */

      public override double expectation(double t0, double x0, double dt)
      {
         localVolatility(); // trigger update
         if (isStrikeIndependent_)
         {
            // exact value for curves
            return x0 *
                   Math.Exp(dt * (riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous,
                                                        Frequency.NoFrequency, true).value() -
                             dividendYield_.link.forwardRate(
                                 t0, t0 + dt, Compounding.Continuous, Frequency.NoFrequency, true).value()));
         }
         else
         {
            Utils.QL_FAIL("not implemented");
            return 0;
         }
      }

      public override double stdDeviation(double t0, double x0, double dt)
      {
         localVolatility(); // trigger update
         if (isStrikeIndependent_)
         {
            // exact value for curves
            return Math.Sqrt(variance(t0, x0, dt));
         }
         else
         {
            return discretization_.diffusion(this, t0, x0, dt);
         }
      }

      public override double variance(double t0, double x0, double dt)
      {
         localVolatility(); // trigger update
         if (isStrikeIndependent_)
         {
            // exact value for curves
            return blackVolatility_.link.blackVariance(t0 + dt, 0.01) -
                   blackVolatility_.link.blackVariance(t0, 0.01);
         }
         else
         {
            return discretization_.variance(this, t0, x0, dt);
         }
      }

      public override double evolve(double t0, double x0, double dt, double dw)
      {
         localVolatility(); // trigger update
         if (isStrikeIndependent_)
         {
            // exact value for curves
            double var = variance(t0, x0, dt);
            double drift = (riskFreeRate_.link.forwardRate(t0, t0 + dt, Compounding.Continuous,
                                                     Frequency.NoFrequency, true).value() -
                          dividendYield_.link.forwardRate(t0, t0 + dt, Compounding.Continuous,
                                                      Frequency.NoFrequency, true).value()) *
                             dt - 0.5 * var;
            return apply(x0, Math.Sqrt(var) * dw + drift);
         }
         else
            return apply(x0, discretization_.drift(this, t0, x0, dt) + stdDeviation(t0, x0, dt) * dw);
      }

      public override double time(Date d)
      {
         return riskFreeRate_.link.dayCounter().yearFraction(riskFreeRate_.link.referenceDate(), d);
      }

      public override void update()
      {
         updated_ = false;
         base.update();
      }

      public Handle<Quote> stateVariable()
      {
         return x0_;
      }

      public Handle<YieldTermStructure> dividendYield()
      {
         return dividendYield_;
      }

      public Handle<YieldTermStructure> riskFreeRate()
      {
         return riskFreeRate_;
      }

      public Handle<BlackVolTermStructure> blackVolatility()
      {
         return blackVolatility_;
      }

      public Handle<LocalVolTermStructure> localVolatility()
      {
         if (!updated_)
         {
            isStrikeIndependent_ = true;

            // constant Black vol?
            BlackConstantVol constVol = blackVolatility().link as BlackConstantVol;
            if (constVol != null)
            {
               // ok, the local vol is constant too.
               localVolatility_.linkTo(new LocalConstantVol(constVol.referenceDate(),
                  constVol.blackVol(0.0, x0_.link.value()),
                  constVol.dayCounter()));
               updated_ = true;
               return localVolatility_;
            }

            // ok, so it's not constant. Maybe it's strike-independent?
            BlackVarianceCurve volCurve = blackVolatility().link as BlackVarianceCurve;
            if (volCurve != null)
            {
               // ok, we can use the optimized algorithm
               localVolatility_.linkTo(new LocalVolCurve(new Handle<BlackVarianceCurve>(volCurve)));
               updated_ = true;
               return localVolatility_;
            }

            // ok, so it's strike-dependent. Never mind.
            localVolatility_.linkTo(new LocalVolSurface(blackVolatility_, riskFreeRate_, dividendYield_,
               x0_.link.value()));
            updated_ = true;
            isStrikeIndependent_ = false;
            return localVolatility_;
         }
         else
         {
            return localVolatility_;
         }
      }

      private Handle<Quote> x0_;
      private Handle<YieldTermStructure> riskFreeRate_, dividendYield_;
      private Handle<BlackVolTermStructure> blackVolatility_;
      private RelinkableHandle<LocalVolTermStructure> localVolatility_ = new RelinkableHandle<LocalVolTermStructure>();
      private bool updated_, isStrikeIndependent_;
   }

   //! Black-Scholes (1973) stochastic process
   /*! This class describes the stochastic process S for a stock given by
       \f[
           dS(t, S) = (r(t) - \frac{\sigma(t, S)^2}{2}) dt + \sigma dW_t.
       \f]

       \ingroup processes
   */

   public class BlackScholesProcess : GeneralizedBlackScholesProcess
   {
      public BlackScholesProcess(Handle<Quote> x0,
         Handle<YieldTermStructure> riskFreeTS,
         Handle<BlackVolTermStructure> blackVolTS)
         : this(x0, riskFreeTS, blackVolTS, new EulerDiscretization())
      { }

      public BlackScholesProcess(Handle<Quote> x0,
         Handle<YieldTermStructure> riskFreeTS,
         Handle<BlackVolTermStructure> blackVolTS,
         IDiscretization1D d)
         : base(x0,
            // no dividend yield
            new Handle<YieldTermStructure>(new FlatForward(0, new NullCalendar(), 0.0, new Actual365Fixed())),
            riskFreeTS, blackVolTS, d)
      { }
   }

   //! Merton (1973) extension to the Black-Scholes stochastic process
   /*! This class describes the stochastic process for a stock or
       stock index paying a continuous dividend yield given by
       \f[
           dS(t, S) = (r(t) - q(t) - \frac{\sigma(t, S)^2}{2}) dt
                    + \sigma dW_t.
       \f]

       \ingroup processes
   */

   public class BlackScholesMertonProcess : GeneralizedBlackScholesProcess
   {
      public BlackScholesMertonProcess(Handle<Quote> x0,
         Handle<YieldTermStructure> dividendTS,
         Handle<YieldTermStructure> riskFreeTS,
         Handle<BlackVolTermStructure> blackVolTS)
         : this(x0, dividendTS, riskFreeTS, blackVolTS, new EulerDiscretization())
      { }

      public BlackScholesMertonProcess(Handle<Quote> x0,
         Handle<YieldTermStructure> dividendTS,
         Handle<YieldTermStructure> riskFreeTS,
         Handle<BlackVolTermStructure> blackVolTS,
         IDiscretization1D d)
         : base(x0, dividendTS, riskFreeTS, blackVolTS, d)
      { }
   }

   //! Black (1976) stochastic process
   /*! This class describes the stochastic process for a forward or
       futures contract given by
       \f[
           dS(t, S) = \frac{\sigma(t, S)^2}{2} dt + \sigma dW_t.
       \f]

       \ingroup processes
   */

   public class BlackProcess : GeneralizedBlackScholesProcess
   {
      public BlackProcess(Handle<Quote> x0,
         Handle<YieldTermStructure> riskFreeTS,
         Handle<BlackVolTermStructure> blackVolTS)
         : this(x0, riskFreeTS, blackVolTS, new EulerDiscretization())
      { }

      public BlackProcess(Handle<Quote> x0,
         Handle<YieldTermStructure> riskFreeTS,
         Handle<BlackVolTermStructure> blackVolTS,
         IDiscretization1D d)
         : base(x0, riskFreeTS, riskFreeTS, blackVolTS, d)
      { }
   }

   //! Garman-Kohlhagen (1983) stochastic process
   /*! This class describes the stochastic process for an exchange
       rate given by
       \f[
           dS(t, S) = (r(t) - r_f(t) - \frac{\sigma(t, S)^2}{2}) dt
                    + \sigma dW_t.
       \f]

       \ingroup processes
   */

   public class GarmanKohlagenProcess : GeneralizedBlackScholesProcess
   {
      public GarmanKohlagenProcess(Handle<Quote> x0,
         Handle<YieldTermStructure> foreignRiskFreeTS,
         Handle<YieldTermStructure> domesticRiskFreeTS,
         Handle<BlackVolTermStructure> blackVolTS)
         : this(x0, foreignRiskFreeTS, domesticRiskFreeTS, blackVolTS, new EulerDiscretization())
      { }

      public GarmanKohlagenProcess(Handle<Quote> x0, Handle<YieldTermStructure> foreignRiskFreeTS,
         Handle<YieldTermStructure> domesticRiskFreeTS,
         Handle<BlackVolTermStructure> blackVolTS, IDiscretization1D d)
         : base(x0, foreignRiskFreeTS, domesticRiskFreeTS, blackVolTS, d)
      { }

      public GarmanKohlagenProcess(Handle<Quote> x0, Handle<YieldTermStructure> foreignRiskFreeTS,
        Handle<YieldTermStructure> domesticRiskFreeTS,
        Handle<BlackVolTermStructure> blackVolTS,
        RelinkableHandle<LocalVolTermStructure> localVolTS,
        IDiscretization1D d = null)
        : base(x0, foreignRiskFreeTS, domesticRiskFreeTS, blackVolTS, localVolTS, d)
      { }
   }
}
