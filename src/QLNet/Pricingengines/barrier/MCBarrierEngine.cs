/*
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   //! Pricing engine for barrier options using Monte Carlo simulation
   /*! Uses the Brownian-bridge correction for the barrier found in
       <i>
       Going to Extremes: Correcting Simulation Bias in Exotic
       Option Valuation - D.R. Beaglehole, P.H. Dybvig and G. Zhou
       Financial Analysts Journal; Jan/Feb 1997; 53, 1. pg. 62-68
       </i>
       and
       <i>
       Simulating path-dependent options: A new approach -
       M. El Babsiri and G. Noel
       Journal of Derivatives; Winter 1998; 6, 2; pg. 65-83
       </i>

       \ingroup barrierengines

       \test the correctness of the returned value is tested by
             reproducing results available in literature.
   */
   public class MCBarrierEngine<RNG, S> : McSimulation<SingleVariate, RNG, S>, IGenericEngine
      where RNG : IRSG, new ()
      where S : IGeneralStatistics, new ()
   {
      public MCBarrierEngine(GeneralizedBlackScholesProcess process,
                             int? timeSteps,
                             int? timeStepsPerYear,
                             bool brownianBridge,
                             bool antitheticVariate,
                             int? requiredSamples,
                             double? requiredTolerance,
                             int? maxSamples,
                             bool isBiased,
                             ulong seed)
      : base(antitheticVariate, false)
      {
         process_ = process;
         timeSteps_ = timeSteps;
         timeStepsPerYear_ = timeStepsPerYear;
         requiredSamples_ = requiredSamples;
         maxSamples_ = maxSamples;
         requiredTolerance_ = requiredTolerance;
         isBiased_ = isBiased;
         brownianBridge_ = brownianBridge;
         seed_ = seed;

         Utils.QL_REQUIRE(timeSteps != null || timeStepsPerYear != null, () => "no time steps provided");
         Utils.QL_REQUIRE(timeSteps == null || timeStepsPerYear == null, () => "both time steps and time steps per year were provided");

         if (timeSteps != null)
            Utils.QL_REQUIRE(timeSteps > 0, () => "timeSteps must be positive, " + timeSteps + " not allowed");

         if (timeStepsPerYear != null)
            Utils.QL_REQUIRE(timeStepsPerYear > 0, () => "timeStepsPerYear must be positive, " + timeStepsPerYear + " not allowed");

         process_.registerWith(update);
      }

      public void calculate()
      {
         double spot = process_.x0();
         Utils.QL_REQUIRE(spot >= 0.0, () => "negative or null underlying given");
         Utils.QL_REQUIRE(!triggered(spot), () => "barrier touched");
         base.calculate(requiredTolerance_,
                        requiredSamples_,
                        maxSamples_);
         results_.value = this.mcModel_.sampleAccumulator().mean();
         if (new RNG().allowsErrorEstimate > 0)
            results_.errorEstimate =
               this.mcModel_.sampleAccumulator().errorEstimate();
      }

      protected override IPathGenerator<IRNG> pathGenerator()
      {
         TimeGrid grid = timeGrid();
         IRNG gen = new RNG().make_sequence_generator(grid.size() - 1, seed_);
         return new PathGenerator<IRNG>(process_, grid, gen, brownianBridge_);
      }

      protected override TimeGrid timeGrid()
      {
         double residualTime = process_.time(arguments_.exercise.lastDate());
         if (timeSteps_ > 0)
         {
            return new TimeGrid(residualTime, timeSteps_.Value);
         }
         else if (timeStepsPerYear_ > 0)
         {
            int steps = (int)(timeStepsPerYear_.Value * residualTime);
            return new TimeGrid(residualTime, Math.Max(steps, 1));
         }
         else
         {
            Utils.QL_FAIL("time steps not specified");
            return null;
         }
      }

      protected override PathPricer<IPath> pathPricer()
      {
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

         TimeGrid grid = timeGrid();
         List<double> discounts = new InitializedList<double>(grid.size());
         for (int i = 0; i < grid.size(); i++)
            discounts[i] = process_.riskFreeRate().currentLink().discount(grid[i]);

         // do this with template parameters?
         if (isBiased_)
         {
            return new BiasedBarrierPathPricer(arguments_.barrierType,
                                               arguments_.barrier,
                                               arguments_.rebate,
                                               payoff.optionType(),
                                               payoff.strike(),
                                               discounts);
         }
         else
         {
            IRNG sequenceGen = new RandomSequenceGenerator<MersenneTwisterUniformRng>(grid.size() - 1, 5);
            return new BarrierPathPricer(arguments_.barrierType,
                                         arguments_.barrier,
                                         arguments_.rebate,
                                         payoff.optionType(),
                                         payoff.strike(),
                                         discounts,
                                         process_,
                                         sequenceGen);
         }
      }

      protected bool triggered(double underlying)
      {
         switch (arguments_.barrierType)
         {
            case Barrier.Type.DownIn:
            case Barrier.Type.DownOut:
               return underlying < arguments_.barrier;
            case Barrier.Type.UpIn:
            case Barrier.Type.UpOut:
               return underlying > arguments_.barrier;
            default:
               Utils.QL_FAIL("unknown type");
               return false;
         }
      }

      // data members
      protected GeneralizedBlackScholesProcess process_;
      protected int? timeSteps_, timeStepsPerYear_;
      protected int? requiredSamples_, maxSamples_;
      protected double? requiredTolerance_;
      protected bool isBiased_;
      protected bool brownianBridge_;
      protected ulong seed_;

      #region PricingEngine
      protected BarrierOption.Arguments arguments_ = new BarrierOption.Arguments();
      protected BarrierOption.Results results_ = new BarrierOption.Results();

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

      public void registerWith(Callback handler) {} public void XXXregisterWith(Callback handler) { notifyObserversEvent += handler; }
      public void unregisterWith(Callback handler) {} public void XXXunregisterWith(Callback handler) { notifyObserversEvent -= handler; }
      protected void notifyObservers()
      {
         eventSource.Raise();
      }

      public void update() { notifyObservers(); }
      #endregion
      #endregion
   }


   public class BarrierPathPricer : PathPricer<IPath>
   {
      public BarrierPathPricer(
         Barrier.Type barrierType,
         double? barrier,
         double? rebate,
         Option.Type type,
         double strike,
         List<double> discounts,
         StochasticProcess1D diffProcess,
         IRNG sequenceGen)
      {
         barrierType_ = barrierType;
         barrier_ = barrier;
         rebate_ = rebate;
         diffProcess_ = diffProcess;
         sequenceGen_ = sequenceGen;
         payoff_ = new PlainVanillaPayoff(type, strike);
         discounts_ = discounts;
         Utils.QL_REQUIRE(strike >= 0.0, () => "strike less than zero not allowed");
         Utils.QL_REQUIRE(barrier > 0.0, () => "barrier less/equal zero not allowed");
      }
      public double value(IPath path)
      {
         int n = path.length();
         Utils.QL_REQUIRE(n > 1, () => "the path cannot be empty");

         bool isOptionActive = false;
         int? knockNode = null;
         double asset_price = (path as Path).front();
         double new_asset_price;
         double x, y;
         double vol;
         TimeGrid timeGrid = (path as Path).timeGrid();
         double dt;
         List<double> u = sequenceGen_.nextSequence().value;
         int i;

         switch (barrierType_)
         {
            case Barrier.Type.DownIn:
               isOptionActive = false;
               for (i = 0; i < n - 1; i++)
               {
                  new_asset_price = (path as Path)[i + 1];
                  // terminal or initial vol?
                  vol = diffProcess_.diffusion(timeGrid[i], asset_price);
                  dt = timeGrid.dt(i);

                  x = Math.Log(new_asset_price / asset_price);
                  y = 0.5 * (x - Math.Sqrt(x * x - 2 * vol * vol * dt * Math.Log(u[i])));
                  y = asset_price * Math.Exp(y);
                  if (y <= barrier_)
                  {
                     isOptionActive = true;
                     if (knockNode == null)
                        knockNode = i + 1;
                  }
                  asset_price = new_asset_price;
               }
               break;
            case Barrier.Type.UpIn:
               isOptionActive = false;
               for (i = 0; i < n - 1; i++)
               {
                  new_asset_price = (path as Path)[i + 1];
                  // terminal or initial vol?
                  vol = diffProcess_.diffusion(timeGrid[i], asset_price);
                  dt = timeGrid.dt(i);

                  x = Math.Log(new_asset_price / asset_price);
                  y = 0.5 * (x + Math.Sqrt(x * x - 2 * vol * vol * dt * Math.Log((1 - u[i]))));
                  y = asset_price * Math.Exp(y);
                  if (y >= barrier_)
                  {
                     isOptionActive = true;
                     if (knockNode == null)
                        knockNode = i + 1;
                  }
                  asset_price = new_asset_price;
               }
               break;
            case Barrier.Type.DownOut:
               isOptionActive = true;
               for (i = 0; i < n - 1; i++)
               {
                  new_asset_price = (path as Path)[i + 1];
                  // terminal or initial vol?
                  vol = diffProcess_.diffusion(timeGrid[i], asset_price);
                  dt = timeGrid.dt(i);

                  x = Math.Log(new_asset_price / asset_price);
                  y = 0.5 * (x - Math.Sqrt(x * x - 2 * vol * vol * dt * Math.Log(u[i])));
                  y = asset_price * Math.Exp(y);
                  if (y <= barrier_)
                  {
                     isOptionActive = false;
                     if (knockNode == null)
                        knockNode = i + 1;
                  }
                  asset_price = new_asset_price;
               }
               break;
            case Barrier.Type.UpOut:
               isOptionActive = true;
               for (i = 0; i < n - 1; i++)
               {
                  new_asset_price = (path as Path)[i + 1];
                  // terminal or initial vol?
                  vol = diffProcess_.diffusion(timeGrid[i], asset_price);
                  dt = timeGrid.dt(i);

                  x = Math.Log(new_asset_price / asset_price);
                  y = 0.5 * (x + Math.Sqrt(x * x - 2 * vol * vol * dt * Math.Log((1 - u[i]))));
                  y = asset_price * Math.Exp(y);
                  if (y >= barrier_)
                  {
                     isOptionActive = false;
                     if (knockNode == null)
                        knockNode = i + 1;
                  }
                  asset_price = new_asset_price;
               }
               break;
            default:
               Utils.QL_FAIL("unknown barrier type");
               break;
         }

         if (isOptionActive)
         {
            return payoff_.value(asset_price) * discounts_.Last();
         }
         else
         {
            switch (barrierType_)
            {
               case Barrier.Type.UpIn:
               case Barrier.Type.DownIn:
                  return rebate_.GetValueOrDefault() * discounts_.Last();
               case Barrier.Type.UpOut:
               case Barrier.Type.DownOut:
                  return rebate_.GetValueOrDefault() * discounts_[(int)knockNode];
               default:
                  Utils.QL_FAIL("unknown barrier type");
                  return -1;
            }
         }
      }

      protected Barrier.Type barrierType_;
      protected double? barrier_;
      protected double? rebate_;
      protected StochasticProcess1D diffProcess_;
      protected IRNG sequenceGen_;
      protected PlainVanillaPayoff payoff_;
      protected List<double> discounts_;
   }


   public class BiasedBarrierPathPricer : PathPricer<IPath>
   {
      public BiasedBarrierPathPricer(Barrier.Type barrierType,
                                     double? barrier,
                                     double? rebate,
                                     Option.Type type,
                                     double strike,
                                     List<double> discounts)
      : base()
      {
         barrierType_ = barrierType;
         barrier_ = barrier;
         rebate_ = rebate;
         payoff_ = new PlainVanillaPayoff(type, strike);
         discounts_ = discounts;

         Utils.QL_REQUIRE(strike >= 0.0,
                          () => "strike less than zero not allowed");
         Utils.QL_REQUIRE(barrier > 0.0,
                          () => "barrier less/equal zero not allowed");
      }

      public double value(IPath path)
      {
         int n = path.length();
         Utils.QL_REQUIRE(n > 1, () => "the path cannot be empty");

         bool isOptionActive = false;
         int? knockNode = null;
         double asset_price = (path as Path).front();
         int i;

         switch (barrierType_)
         {
            case Barrier.Type.DownIn:
               isOptionActive = false;
               for (i = 1; i < n; i++)
               {
                  asset_price = (path as Path)[i];
                  if (asset_price <= barrier_)
                  {
                     isOptionActive = true;
                     if (knockNode == null)
                        knockNode = i;
                  }
               }
               break;
            case Barrier.Type.UpIn:
               isOptionActive = false;
               for (i = 1; i < n; i++)
               {
                  asset_price = (path as Path)[i];
                  if (asset_price >= barrier_)
                  {
                     isOptionActive = true;
                     if (knockNode == null)
                        knockNode = i;
                  }
               }
               break;
            case Barrier.Type.DownOut:
               isOptionActive = true;
               for (i = 1; i < n; i++)
               {
                  asset_price = (path as Path)[i];
                  if (asset_price <= barrier_)
                  {
                     isOptionActive = false;
                     if (knockNode == null)
                        knockNode = i;
                  }
               }
               break;
            case Barrier.Type.UpOut:
               isOptionActive = true;
               for (i = 1; i < n; i++)
               {
                  asset_price = (path as Path)[i];
                  if (asset_price >= barrier_)
                  {
                     isOptionActive = false;
                     if (knockNode == null)
                        knockNode = i;
                  }
               }
               break;
            default:
               Utils.QL_FAIL("unknown barrier type");
               break;
         }

         if (isOptionActive)
         {
            return payoff_.value(asset_price) * discounts_.Last();
         }
         else
         {
            switch (barrierType_)
            {
               case Barrier.Type.UpIn:
               case Barrier.Type.DownIn:
                  return rebate_.GetValueOrDefault() * discounts_.Last();
               case Barrier.Type.UpOut:
               case Barrier.Type.DownOut:
                  return rebate_.GetValueOrDefault() * discounts_[(int)knockNode];
               default:
                  Utils.QL_FAIL("unknown barrier type");
                  return -1;
            }
         }
      }

      protected Barrier.Type barrierType_;
      protected double? barrier_;
      protected double? rebate_;
      protected PlainVanillaPayoff payoff_;
      protected List<double> discounts_;
   }

   //! Monte Carlo barrier-option engine factory
   public class MakeMCBarrierEngine<RNG, S>
      where RNG : IRSG, new ()
      where S : IGeneralStatistics, new ()
   {
      public MakeMCBarrierEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         brownianBridge_ = false;
         antithetic_ = false;
         biased_ = false;
         steps_ = null;
         stepsPerYear_ = null;
         samples_ = null;
         maxSamples_ = null;
         tolerance_ = null;
         seed_ = 0;
      }
      // named parameters
      public MakeMCBarrierEngine<RNG, S> withSteps(int steps)
      {
         steps_ = steps;
         return this;
      }
      public MakeMCBarrierEngine<RNG, S> withStepsPerYear(int steps)
      {
         stepsPerYear_ = steps;
         return this;
      }
      public MakeMCBarrierEngine<RNG, S> withBrownianBridge(bool b = true)
      {
         brownianBridge_ = b;
         return this;
      }
      public MakeMCBarrierEngine<RNG, S> withAntitheticVariate(bool b = true)
      {
         antithetic_ = b;
         return this;
      }
      public MakeMCBarrierEngine<RNG, S> withSamples(int samples)
      {
         Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
         samples_ = samples;
         return this;
      }
      public MakeMCBarrierEngine<RNG, S> withAbsoluteTolerance(double tolerance)
      {
         Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
         Utils.QL_REQUIRE(new RNG().allowsErrorEstimate > 0, () => "chosen random generator policy does not allow an error estimate");
         tolerance_ = tolerance;
         return this;
      }
      public MakeMCBarrierEngine<RNG, S> withMaxSamples(int samples)
      {
         maxSamples_ = samples;
         return this;
      }
      public MakeMCBarrierEngine<RNG, S> withBias(bool b = true)
      {
         biased_ = b;
         return this;
      }
      public MakeMCBarrierEngine<RNG, S> withSeed(ulong seed)
      {
         seed_ = seed;
         return this;
      }
      // conversion to pricing engine
      public IPricingEngine getAsPricingEngine()
      {
         Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null, () => "number of steps not given");
         Utils.QL_REQUIRE(steps_ == null || stepsPerYear_ == null, () => "number of steps overspecified");
         return new MCBarrierEngine<RNG, S>(process_,
                                            steps_,
                                            stepsPerYear_,
                                            brownianBridge_,
                                            antithetic_,
                                            samples_,
                                            tolerance_,
                                            maxSamples_,
                                            biased_,
                                            seed_);
      }

      protected GeneralizedBlackScholesProcess process_;
      protected bool brownianBridge_, antithetic_, biased_;
      protected int? steps_, stepsPerYear_, samples_, maxSamples_;
      ulong seed_;
      protected double? tolerance_;
   }
}
