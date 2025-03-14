/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
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

namespace QLNet
{
   /// <summary>
   /// Monte Carlo pricing engine for discrete geometric average price Asian
   /// </summary>
   /// <typeparam name="RNG"></typeparam>
   /// <typeparam name="S"></typeparam>
   public class MCDiscreteGeometricAPEngine<RNG, S>
      : MCDiscreteAveragingAsianEngineBase<SingleVariate, RNG, S>
        where RNG : IRSG, new ()
        where S : IGeneralStatistics, new ()
   {
      public MCDiscreteGeometricAPEngine(
         GeneralizedBlackScholesProcess process,
         bool brownianBridge,
         bool antitheticVariate,
         int? requiredSamples,
         double? requiredTolerance,
         int? maxSamples,
         ulong seed)
         : base(process, brownianBridge, antitheticVariate,
                false, requiredSamples, requiredTolerance, maxSamples, seed)
      { }

      // conversion to pricing engine
      protected override PathPricer<IPath> pathPricer()
      {
         PlainVanillaPayoff payoff = (PlainVanillaPayoff)(this.arguments_.payoff);
         Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

         EuropeanExercise exercise = (EuropeanExercise) this.arguments_.exercise;
         Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");

         GeneralizedBlackScholesProcess process = (GeneralizedBlackScholesProcess) this.process_;
         Utils.QL_REQUIRE(process != null,() =>  "Black-Scholes process required");

         return (PathPricer<IPath>) new GeometricAPOPathPricer(
                   payoff.optionType(),
                   payoff.strike(),
                   process.riskFreeRate().link.discount(exercise!.lastDate()),
                   this.arguments_.runningAccumulator.GetValueOrDefault(),
                   this.arguments_.pastFixings.GetValueOrDefault());
      }
   }

   public class GeometricAPOPathPricer : PathPricer<IPath>
   {
      private PlainVanillaPayoff payoff_;
      private double discount_;
      private double runningProduct_;
      private int pastFixings_;

      public GeometricAPOPathPricer(Option.Type type,
                                    double strike,
                                    double discount,
                                    double runningProduct = 1.0,
                                    int pastFixings = 0)
      {
         payoff_ = new PlainVanillaPayoff(type, strike);
         discount_ = discount;
         runningProduct_ = runningProduct;
         pastFixings_ = pastFixings;
         Utils.QL_REQUIRE(strike >= 0.0, () => "negative strike given");
      }

      public double value(Path path)
      {
         int n = path.length() - 1;
         Utils.QL_REQUIRE(n > 0, () => "the path cannot be empty");

         double averagePrice;
         double product = runningProduct_;
         int fixings = n + pastFixings_;
         if (path.timeGrid().mandatoryTimes()[0].IsEqual(0.0))
         {
            fixings += 1;
            product *= path.front();
         }
         // care must be taken not to overflow product
         double maxValue = double.MaxValue;
         averagePrice = 1.0;
         for (int i = 1; i < n + 1; i++)
         {
            double price = path[i];
            if (product < maxValue / price)
            {
               product *= price;
            }
            else
            {
               averagePrice *= Math.Pow(product, 1.0 / (double) fixings);
               product = price;
            }
         }
         averagePrice *= Math.Pow(product, 1.0 / fixings);
         return discount_ * payoff_.value(averagePrice);
      }

      public double value(IPath path)
      {
         return value((Path)path);
      }
   }

   //<class RNG = PseudoRandom, class S = Statistics>
   public class MakeMCDiscreteGeometricAPEngine<RNG, S>
      where RNG : IRSG, new ()
      where S : Statistics, new ()
   {
      private GeneralizedBlackScholesProcess process_;
      private bool antithetic_;
      private int? samples_, maxSamples_;
      private double? tolerance_;
      private bool brownianBridge_ = true;
      private ulong seed_ = 0;

      public MakeMCDiscreteGeometricAPEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         antithetic_ = false;
         samples_ = null;
         maxSamples_ = null;
         tolerance_ = null;
      }

      // named parameters
      public MakeMCDiscreteGeometricAPEngine<RNG, S> withBrownianBridge(bool b = true)
      {
         brownianBridge_ = b;
         return this;
      }

      public MakeMCDiscreteGeometricAPEngine<RNG, S> withSamples(int samples)
      {
         Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
         samples_ = samples;
         return this;
      }

      public MakeMCDiscreteGeometricAPEngine<RNG, S> withAbsoluteTolerance(double tolerance)
      {
         Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
         Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
                          "chosen random generator policy " + "does not allow an error estimate");
         tolerance_ = tolerance;
         return this;
      }

      public MakeMCDiscreteGeometricAPEngine<RNG, S> withMaxSamples(int samples)
      {
         maxSamples_ = samples;
         return this;
      }

      public MakeMCDiscreteGeometricAPEngine<RNG, S> withSeed(ulong seed)
      {
         seed_ = seed;
         return this;
      }

      public MakeMCDiscreteGeometricAPEngine<RNG, S> withAntitheticVariate(bool b = true)
      {
         antithetic_ = b;
         return this;
      }

      // conversion to pricing engine
      public IPricingEngine value()
      {
         return (IPricingEngine) new MCDiscreteGeometricAPEngine<RNG, S>(process_,
                                                                         brownianBridge_,
                                                                         antithetic_, 
                                                                         samples_,
                                                                         tolerance_,
                                                                         maxSamples_,
                                                                         seed_);
      }


   }
}

