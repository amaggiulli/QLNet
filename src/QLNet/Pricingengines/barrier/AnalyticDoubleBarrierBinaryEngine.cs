/*
 Copyright (C) 2015 Thema Consulting SA
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Linq;

namespace QLNet
{
   //! Analytic pricing engine for double barrier binary options
   /*! This engine implements C.H.Hui series ("One-Touch Double Barrier
       Binary Option Values", Applied Financial Economics 6/1996), as
       described in "The complete guide to option pricing formulas 2nd Ed",
       E.G. Haug, McGraw-Hill, p.180

       The Knock In part of KI+KO and KO+KI options pays at hit, while the
       Double Knock In pays at end.
       This engine thus requires European esercise for Double Knock options,
       and American exercise for KIKO/KOKI.

       \ingroup barrierengines

       greeks are calculated by simple numeric derivation

       \test
       - the correctness of the returned value is tested by reproducing
         results available in literature.
   */

   // calc helper object
   public class AnalyticDoubleBarrierBinaryEngineHelper
   {
      public AnalyticDoubleBarrierBinaryEngineHelper(
         GeneralizedBlackScholesProcess process,
         CashOrNothingPayoff payoff,
         DoubleBarrierOption.Arguments arguments)
      {
         process_ = process;
         payoff_ = payoff;
         arguments_ = arguments;
      }

      // helper object methods
      public double payoffAtExpiry(double spot, double variance,
                                   DoubleBarrier.Type barrierType,
                                   int maxIteration = 100,
                                   double requiredConvergence = 1e-8)
      {
         Utils.QL_REQUIRE(spot > 0.0,
                          () => "positive spot value required");

         Utils.QL_REQUIRE(variance >= 0.0,
                          () => "negative variance not allowed");

         double residualTime = process_.time(arguments_.exercise.lastDate());
         Utils.QL_REQUIRE(residualTime > 0.0,
                          () => "expiration time must be > 0");

         // Option::Type type   = payoff_->optionType(); // this is not used ?
         double cash = payoff_.cashPayoff();
         double barrier_lo = arguments_.barrier_lo.Value;
         double barrier_hi = arguments_.barrier_hi.Value;

         double sigmaq = variance / residualTime;
         double r = process_.riskFreeRate().currentLink().zeroRate(residualTime, Compounding.Continuous,
                                                                   Frequency.NoFrequency).rate();
         double q = process_.dividendYield().currentLink().zeroRate(residualTime,
                                                                    Compounding.Continuous, Frequency.NoFrequency).rate();
         double b = r - q;

         double alpha = -0.5 * (2 * b / sigmaq - 1);
         double beta = -0.25 * Math.Pow((2 * b / sigmaq - 1), 2) - 2 * r / sigmaq;
         double Z = Math.Log(barrier_hi / barrier_lo);
         double factor = ((2 * Const.M_PI * cash) / Math.Pow(Z, 2)); // common factor
         double lo_alpha = Math.Pow(spot / barrier_lo, alpha);
         double hi_alpha = Math.Pow(spot / barrier_hi, alpha);

         double tot = 0, term = 0;
         for (int i = 1 ; i < maxIteration ; ++i)
         {
            double term1 = (lo_alpha - Math.Pow(-1.0, i) * hi_alpha) /
                           (Math.Pow(alpha, 2) + Math.Pow(i * Const.M_PI / Z, 2));
            double term2 = Math.Sin(i * Const.M_PI / Z * Math.Log(spot / barrier_lo));
            double term3 = Math.Exp(-0.5 * (Math.Pow(i * Const.M_PI / Z, 2) - beta) * variance);
            term = factor * i * term1 * term2 * term3;
            tot += term;
         }

         // Check if convergence is sufficiently fast (for extreme parameters with big alpha the convergence can be very
         // poor, see for example Hui "One-touch double barrier binary option value")
         Utils.QL_REQUIRE(Math.Abs(term) < requiredConvergence, () => "serie did not converge sufficiently fast");

         if (barrierType == DoubleBarrier.Type.KnockOut)
            return Math.Max(tot, 0.0); // KO
         else
         {
            double discount = process_.riskFreeRate().currentLink().discount(
               arguments_.exercise.lastDate());
            Utils.QL_REQUIRE(discount>0.0,
                             () => "positive discount required");
            return Math.Max(cash * discount - tot, 0.0); // KI
         }
      }
      // helper object methods
      public double payoffKIKO(double spot, double variance,
                               DoubleBarrier.Type barrierType,
                               int maxIteration = 1000,
                               double requiredConvergence = 1e-8)
      {
         Utils.QL_REQUIRE(spot > 0.0,
                          () => "positive spot value required");

         Utils.QL_REQUIRE(variance >= 0.0,
                          () => "negative variance not allowed");

         double residualTime = process_.time(arguments_.exercise.lastDate());
         Utils.QL_REQUIRE(residualTime > 0.0,
                          () => "expiration time must be > 0");

         double cash = payoff_.cashPayoff();
         double barrier_lo = arguments_.barrier_lo.Value;
         double barrier_hi = arguments_.barrier_hi.Value;
         if (barrierType == DoubleBarrier.Type.KOKI)
            Utils.swap(ref barrier_lo, ref barrier_hi);

         double sigmaq = variance / residualTime;
         double r = process_.riskFreeRate().currentLink().zeroRate(residualTime, Compounding.Continuous,
                                                                   Frequency.NoFrequency).rate();
         double q = process_.dividendYield().currentLink().zeroRate(residualTime,
                                                                    Compounding.Continuous, Frequency.NoFrequency).rate();
         double b = r - q;

         double alpha = -0.5 * (2 * b / sigmaq - 1);
         double beta = -0.25 * Math.Pow((2 * b / sigmaq - 1), 2) - 2 * r / sigmaq;
         double Z = Math.Log(barrier_hi / barrier_lo);
         double log_S_L = Math.Log(spot / barrier_lo);

         double tot = 0, term = 0;
         for (int i = 1 ; i < maxIteration ; ++i)
         {
            double factor = Math.Pow(i * Const.M_PI / Z, 2) - beta;
            double term1 = (beta - Math.Pow(i * Const.M_PI / Z, 2) * Math.Exp(-0.5 * factor * variance)) / factor;
            double term2 = Math.Sin(i * Const.M_PI / Z * log_S_L);
            term = (2.0 / (i * Const.M_PI)) * term1 * term2;
            tot += term;
         }
         tot += 1 - log_S_L / Z;
         tot *= cash * Math.Pow(spot / barrier_lo, alpha);

         // Check if convergence is sufficiently fast
         Utils.QL_REQUIRE(Math.Abs(term) < requiredConvergence, () => "serie did not converge sufficiently fast");

         return Math.Max(tot, 0.0);
      }

      protected GeneralizedBlackScholesProcess process_;
      protected CashOrNothingPayoff payoff_;
      protected DoubleBarrierOption.Arguments arguments_;
   }
   public class AnalyticDoubleBarrierBinaryEngine : DoubleBarrierOption.Engine
   {
      public AnalyticDoubleBarrierBinaryEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         process_.registerWith(update);
      }

      public override void calculate()
      {
         if (arguments_.barrierType == DoubleBarrier.Type.KIKO ||
             arguments_.barrierType == DoubleBarrier.Type.KOKI)
         {
            AmericanExercise ex = arguments_.exercise as AmericanExercise;
            Utils.QL_REQUIRE(ex != null, () => "KIKO/KOKI options must have American exercise");
            Utils.QL_REQUIRE(ex.dates()[0] <=
                             process_.blackVolatility().currentLink().referenceDate(),
                             () => "American option with window exercise not handled yet");
         }
         else
         {
            EuropeanExercise ex = arguments_.exercise as EuropeanExercise;
            Utils.QL_REQUIRE(ex != null, () => "non-European exercise given");
         }
         CashOrNothingPayoff payoff = arguments_.payoff as CashOrNothingPayoff;
         Utils.QL_REQUIRE(payoff != null, () => "a cash-or-nothing payoff must be given");

         double spot = process_.stateVariable().currentLink().value();
         Utils.QL_REQUIRE(spot > 0.0, () => "negative or null underlying given");

         double variance =
            process_.blackVolatility().currentLink().blackVariance(
               arguments_.exercise.lastDate(),
               payoff.strike());
         double barrier_lo = arguments_.barrier_lo.Value;
         double barrier_hi = arguments_.barrier_hi.Value;
         DoubleBarrier.Type barrierType = arguments_.barrierType;
         Utils.QL_REQUIRE(barrier_lo > 0.0,
                          () => "positive low barrier value required");
         Utils.QL_REQUIRE(barrier_hi > 0.0,
                          () => "positive high barrier value required");
         Utils.QL_REQUIRE(barrier_lo < barrier_hi,
                          () => "barrier_lo must be < barrier_hi");
         Utils.QL_REQUIRE(barrierType == DoubleBarrier.Type.KnockIn ||
                          barrierType == DoubleBarrier.Type.KnockOut ||
                          barrierType == DoubleBarrier.Type.KIKO ||
                          barrierType == DoubleBarrier.Type.KOKI,
                          () => "Unsupported barrier type");

         // degenerate cases
         switch (barrierType)
         {
            case DoubleBarrier.Type.KnockOut:
               if (spot <= barrier_lo || spot >= barrier_hi)
               {
                  // knocked out, no value
                  results_.value = 0;
                  results_.delta = 0;
                  results_.gamma = 0;
                  results_.vega = 0;
                  results_.rho = 0;
                  return;
               }
               break;

            case DoubleBarrier.Type.KnockIn:
               if (spot <= barrier_lo || spot >= barrier_hi)
               {
                  // knocked in - pays
                  results_.value = payoff.cashPayoff();
                  results_.delta = 0;
                  results_.gamma = 0;
                  results_.vega = 0;
                  results_.rho = 0;
                  return;
               }
               break;

            case DoubleBarrier.Type.KIKO:
               if (spot >= barrier_hi)
               {
                  // knocked out, no value
                  results_.value = 0;
                  results_.delta = 0;
                  results_.gamma = 0;
                  results_.vega = 0;
                  results_.rho = 0;
                  return;
               }
               else if (spot <= barrier_lo)
               {
                  // knocked in, pays
                  results_.value = payoff.cashPayoff();
                  results_.delta = 0;
                  results_.gamma = 0;
                  results_.vega = 0;
                  results_.rho = 0;
                  return;
               }
               break;

            case DoubleBarrier.Type.KOKI:
               if (spot <= barrier_lo)
               {
                  // knocked out, no value
                  results_.value = 0;
                  results_.delta = 0;
                  results_.gamma = 0;
                  results_.vega = 0;
                  results_.rho = 0;
                  return;
               }
               else if (spot >= barrier_hi)
               {
                  // knocked in, pays
                  results_.value = payoff.cashPayoff();
                  results_.delta = 0;
                  results_.gamma = 0;
                  results_.vega = 0;
                  results_.rho = 0;
                  return;
               }
               break;
         }

         AnalyticDoubleBarrierBinaryEngineHelper helper = new AnalyticDoubleBarrierBinaryEngineHelper(process_,
               payoff, arguments_);
         switch (barrierType)
         {
            case DoubleBarrier.Type.KnockOut:
            case DoubleBarrier.Type.KnockIn:
               results_.value = helper.payoffAtExpiry(spot, variance, barrierType);
               break;

            case DoubleBarrier.Type.KIKO:
            case DoubleBarrier.Type.KOKI:
               results_.value = helper.payoffKIKO(spot, variance, barrierType);
               break;
            default:
               results_.value = null;
               break;
         }
      }

      protected GeneralizedBlackScholesProcess process_;
   }
}
