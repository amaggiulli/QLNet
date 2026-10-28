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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// Heston MC pricing engine for discrete arithmetic average price Asian
   /// </summary>
   /// <remarks>
   /// By default, the MC discretization will use 1 time step per fixing date, but
   /// this can be controlled via timeSteps or timeStepsPerYear parameter, which
   /// will provide additional timesteps. The grid tries to space as evenly as it
   /// can and does not guarantee to match an exact number of steps, the precise
   /// grid used can be found in results_.additionalResults["TimeGrid"]
   /// </remarks>
   public class MCDiscreteArithmeticAPHestonEngine<RNG, S>
      : MCDiscreteAveragingAsianEngineBase<MultiVariate, RNG, S>
      where RNG : IRSG, new()
      where S : IGeneralStatistics, new()
   {
      public MCDiscreteArithmeticAPHestonEngine(HestonProcess process, bool antitheticVariate, int? requiredSamples,
         double? requiredTolerance, int? maxSamples, ulong seed, int? timeSteps = null, int? timeStepsPerYear = null,
         bool controlVariate = false)
         : base(process, false, antitheticVariate, controlVariate, requiredSamples, requiredTolerance, maxSamples, seed,
            timeSteps, timeStepsPerYear, true)

      {
         Utils.QL_REQUIRE(timeSteps == null || timeStepsPerYear == null, ()=>
            "both time steps and time steps per year were provided");
      }

      protected override PathPricer<IPath> pathPricer()
      {
         // Keep track of the fixing indices, the path pricer will need to sum only these
         var timeGrid = this.timeGrid();
         var fixingTimes = timeGrid.mandatoryTimes();
         var fixingIndexes = fixingTimes.Select(fixingTime => timeGrid.closestIndex(fixingTime)).ToList();

         var payoff = this.arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff != null,()=> "non-plain payoff given");

         var exercise = this.arguments_.exercise as EuropeanExercise;
         Utils.QL_REQUIRE(exercise!=null,()=> "wrong exercise given");

         var process = this.process_ as HestonProcess;
         Utils.QL_REQUIRE(process!=null,()=> "Heston like process required");

         return new ArithmeticAPOHestonPathPricer(payoff.optionType(), payoff.strike(),
            process.riskFreeRate().link.discount(exercise.lastDate()), fixingIndexes,
            this.arguments_.runningAccumulator, this.arguments_.pastFixings);
      }

      // Use the experimental analytic geometric asian option as a control variate.
      protected override PathPricer<IPath> controlPathPricer()
      {
         // Keep track of the fixing indices, the path pricer will need to prod only these
         var timeGrid = this.timeGrid();
         var fixingTimes = timeGrid.mandatoryTimes();
         var fixingIndexes = fixingTimes.Select(fixingTime => timeGrid.closestIndex(fixingTime)).ToList();

         var payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");

         var exercise = arguments_.exercise as EuropeanExercise;
         Utils.QL_REQUIRE(exercise!=null,()=> "wrong exercise given");

         var process = process_ as HestonProcess;
         Utils.QL_REQUIRE(process!= null,()=> "Heston like process required");

         // TODO: Currently the analytic pricer does not support seasoned asian
         // options (coming soon). Once that is available, we will be able to
         // pass seasoning details to the path pricer (NB. NEED to pass them to
         // the analytic pricer as well in that case).

         return new GeometricAPOHestonPathPricer(payoff.optionType(), payoff.strike(),
            process.riskFreeRate().link.discount(exercise.lastDate()), fixingIndexes);
      }

      protected override IPricingEngine controlPricingEngine()
      {
         var process = this.process_ as HestonProcess;
         Utils.QL_REQUIRE(process!=null,()=> "Heston-like process required");

         return new AnalyticDiscreteGeometricAveragePriceAsianHestonEngine(process);
      }
   }

   public class MakeMCDiscreteArithmeticAPHestonEngine<RNG, S>
     where RNG : IRSG, new()
      where S : IGeneralStatistics, new()
   {
      private HestonProcess process_;
      private bool antithetic_ = false, controlVariate_ = false;
      private int? samples_, maxSamples_, steps_, stepsPerYear_;
      private double? tolerance_;
      private ulong seed_ = 0;

      public MakeMCDiscreteArithmeticAPHestonEngine(HestonProcess process)
      {
         process_ = process;
         samples_ = null;
         maxSamples_= null;
         steps_ = null;
         stepsPerYear_ = null;
         tolerance_ = null;
      }

      public MakeMCDiscreteArithmeticAPHestonEngine<RNG, S> withSamples(int samples)
      {
         Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
         samples_ = samples;
         return this;
      }

      public MakeMCDiscreteArithmeticAPHestonEngine<RNG, S> withAbsoluteTolerance(double tolerance)
      {
         Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
         Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
            "chosen random generator policy does not allow an error estimate");
         tolerance_ = tolerance;
         return this;
      }

      public MakeMCDiscreteArithmeticAPHestonEngine<RNG, S> withMaxSamples(int samples)
      {
         maxSamples_ = samples;
         return this;
      }

      public MakeMCDiscreteArithmeticAPHestonEngine<RNG, S> withSeed(ulong seed)
      {
         seed_ = seed;
         return this;
      }

      public MakeMCDiscreteArithmeticAPHestonEngine<RNG, S> withAntitheticVariate(bool b = true)
      {
         antithetic_ = b;
         return this;
      }

      public MakeMCDiscreteArithmeticAPHestonEngine<RNG, S> withSteps(int? steps)
      {
         Utils.QL_REQUIRE(stepsPerYear_ == null,()=> "number of steps per year already set");
         steps_ = steps;
         return this;
      }

      public MakeMCDiscreteArithmeticAPHestonEngine<RNG, S> withStepsPerYear(int? steps)
      {
         Utils.QL_REQUIRE(steps_ == null,()=> "number of steps already set");
         stepsPerYear_ = steps;
         return this;
      }

      public MakeMCDiscreteArithmeticAPHestonEngine<RNG, S> withControlVariate(bool b = true)
      {
         controlVariate_ = b;
         return this;
      }

      public IPricingEngine value()
      {
         return new MCDiscreteArithmeticAPHestonEngine<RNG,S>(process_, antithetic_, samples_,
            tolerance_, maxSamples_, seed_, steps_, stepsPerYear_, controlVariate_);
      }
   }

   class ArithmeticAPOHestonPathPricer : PathPricer<IPath>
   {
      private PlainVanillaPayoff payoff_;
      private double discount_;
      private List<int> fixingIndices_;
      private double? runningSum_;
      private int? pastFixings_;

      public ArithmeticAPOHestonPathPricer(Option.Type type, double strike,
         double discount, List<int> fixingIndices, double? runningSum = 0.0,
         int? pastFixings = 0)
      {
         payoff_ = new PlainVanillaPayoff(type, strike);
         discount_ = discount;
         fixingIndices_ = fixingIndices;
         runningSum_ = runningSum;
         pastFixings_ = pastFixings;
         Utils.QL_REQUIRE(strike>=0.0,()=> "strike less than zero not allowed");
      }

      public double value(IPath multiPath)
      {
         var p = multiPath as MultiPath;
         var path = p![0];
         var n = p.pathSize();
         Utils.QL_REQUIRE(n>0,()=> "the path cannot be empty");

         var sum = runningSum_;
         var fixings = pastFixings_ + fixingIndices_.Count;

         sum += fixingIndices_.Sum(fixingIndice => path[fixingIndice]);

         var averagePrice = sum/fixings;
         return discount_ * payoff_.value(averagePrice.GetValueOrDefault());
      }

   };
}
