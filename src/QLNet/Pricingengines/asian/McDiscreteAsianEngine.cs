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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// Pricing engine for discrete average Asians using Monte Carlo simulation
   /// </summary>
   /// <typeparam name="RNG"></typeparam>
   /// <typeparam name="S"></typeparam>
   public class MCDiscreteAveragingAsianEngineBase<MC, RNG, S> : McSimulation<MC, RNG, S>, IGenericEngine
      where RNG : IRSG, new()
      where S : IGeneralStatistics, new()
   {
      // data members
      protected StochasticProcess process_;
      protected int? requiredSamples_, maxSamples_;
      protected int? timeSteps_, timeStepsPerYear_;
      protected double? requiredTolerance_;
      protected bool brownianBridge_;
      protected ulong seed_;

      // constructor
      public MCDiscreteAveragingAsianEngineBase(
         StochasticProcess process,
         bool brownianBridge,
         bool antitheticVariate,
         bool controlVariate,
         int? requiredSamples,
         double? requiredTolerance,
         int? maxSamples,
         ulong seed,
         int? timeSteps = null,
         int? timeStepsPerYear = null) : base(antitheticVariate, controlVariate)
      {
         process_ = process;
         requiredSamples_ = requiredSamples;
         maxSamples_ = maxSamples;
         timeSteps_ = timeSteps;
         timeStepsPerYear_ = timeStepsPerYear;
         requiredTolerance_ = requiredTolerance;
         brownianBridge_ = brownianBridge;
         seed_ = seed;
         process_.registerWith(update);
      }

      public void calculate()
      {
         base.calculate(requiredTolerance_, requiredSamples_, maxSamples_);
         results_.value = this.mcModel_.sampleAccumulator().mean();
         if (this.controlVariate_) {
            // control variate might lead to small negative
            // option values for deep OTM options
            this.results_.value = Math.Max(0.0, this.results_.value.Value);
         }
         if (FastActivator<RNG>.Create().allowsErrorEstimate != 0)
            results_.errorEstimate =
               this.mcModel_.sampleAccumulator().errorEstimate();

         // Allow inspection of the timeGrid via additional results
         this.results_.additionalResults["TimeGrid"] = this.timeGrid();
      }

      // McSimulation implementation
      protected override TimeGrid timeGrid()
      {
         List<double> fixingTimes = new InitializedList<double>(arguments_.fixingDates.Count);

         for (int i = 0; i < arguments_.fixingDates.Count; i++)
         {
            double t = process_.time(arguments_.fixingDates[i]);
            if (t>=0)
            {
               fixingTimes[i] = t;
            }
         }

         if (fixingTimes.empty() ||
             (fixingTimes.Count() == 1 && fixingTimes.First() == 0.0))
            Utils.QL_FAIL("all fixings are in the past");

         // Some models (eg. Heston) might request additional points in
         // the time grid to improve the accuracy of the discretization
         Date lastExerciseDate = this.arguments_.exercise.lastDate();
         double s = process_.time(lastExerciseDate);

         if (this.timeSteps_ != null)
         {
            return new TimeGrid(fixingTimes, timeSteps_.Value);
         }
         else if (this.timeStepsPerYear_ != null)
         {
            return new TimeGrid(fixingTimes, (int)(this.timeStepsPerYear_.Value*s));
         }
         // handle here maxStepsPerYear
         return new TimeGrid(fixingTimes);
      }

      protected override IPathGenerator<IRNG> pathGenerator()
      {
         int dimensions = process_.factors();
         TimeGrid grid = this.timeGrid();
         IRNG gen = (IRNG)new RNG().make_sequence_generator(dimensions * (grid.size() - 1), seed_);
         return new MultiPathGenerator<IRNG>(process_, grid, gen, brownianBridge_);
      }

      protected override double? controlVariateValue()
      {
         IPricingEngine controlPE = this.controlPricingEngine();
         Utils.QL_REQUIRE(controlPE != null, () => "engine does not provide control variation pricing engine");

         DiscreteAveragingAsianOption.Arguments controlArguments =
            (DiscreteAveragingAsianOption.Arguments)controlPE.getArguments();
         // Clone arguments
         controlArguments.exercise = arguments_.exercise;
         controlArguments.payoff = arguments_.payoff;
         controlArguments.averageType = arguments_.averageType; // Esempio: copia il tipo di media
         controlArguments.runningAccumulator = arguments_.runningAccumulator;
         controlArguments.pastFixings = arguments_.pastFixings;
         controlArguments.fixingDates = new List<Date>(arguments_.fixingDates);

         controlPE.calculate();

         DiscreteAveragingAsianOption.Results controlResults =
            (DiscreteAveragingAsianOption.Results)(controlPE.getResults());

         return controlResults.value;

      }

      protected override PathPricer<IPath> pathPricer()
      {
         throw new System.NotImplementedException();
      }

      #region PricingEngine
      protected DiscreteAveragingAsianOption.Arguments arguments_ = new DiscreteAveragingAsianOption.Arguments();
      protected DiscreteAveragingAsianOption.Results results_ = new DiscreteAveragingAsianOption.Results();

      public IPricingEngineArguments getArguments() { return arguments_; }
      public IPricingEngineResults getResults() { return results_; }
      public void reset() { results_.reset(); }

      #region Observer & Observable
      // observable interface
      private readonly WeakEventSource eventSource = new WeakEventSource();
      public event Callback notifyObserversEvent
      {
         add
         {
            eventSource.Subscribe(value);
         }
         remove
         {
            eventSource.Unsubscribe(value);
         }
      }

      public void registerWith(Callback handler) { notifyObserversEvent += handler; }
      public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
      protected void notifyObservers()
      {
         eventSource.Raise();
      }

      public void update() { notifyObservers(); }
      #endregion
      #endregion
   }
}
