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

namespace QLNet
{
   /// <summary>
   /// Monte Carlo pricing engine for discrete arithmetic average price Asian
   /// </summary>
   /// <remarks>
   /// Monte Carlo pricing engine for discrete arithmetic average price
   /// Asian options. It can use MCDiscreteGeometricAPEngine (Monte Carlo
   /// discrete arithmetic average price engine) and
   /// AnalyticDiscreteGeometricAveragePriceAsianEngine (analytic discrete
   ///  arithmetic average price engine) for control variation.
   /// </remarks>
   /// <typeparam name="RNG"></typeparam>
   /// <typeparam name="S"></typeparam>
   public class MCDiscreteArithmeticAPEngine<RNG, S>
      : MCDiscreteAveragingAsianEngineBase<SingleVariate, RNG, S>
        where RNG : IRSG, new ()
        where S : IGeneralStatistics, new ()
   {

      // constructor
      public MCDiscreteArithmeticAPEngine(
         GeneralizedBlackScholesProcess process,
         bool brownianBridge,
         bool antitheticVariate,
         bool controlVariate,
         int? requiredSamples,
         double? requiredTolerance,
         int? maxSamples,
         ulong seed)
         : base(process, brownianBridge, antitheticVariate,
                controlVariate, requiredSamples, requiredTolerance, maxSamples, seed)
      {}

      protected override PathPricer<IPath> pathPricer()
      {
         PlainVanillaPayoff payoff = (PlainVanillaPayoff)(this.arguments_.payoff);
         Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

         EuropeanExercise exercise = (EuropeanExercise)this.arguments_.exercise;
         Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");

         GeneralizedBlackScholesProcess process = (GeneralizedBlackScholesProcess) this.process_;
         Utils.QL_REQUIRE(process != null, ()=> "Black-Scholes process required");

         return (PathPricer<IPath>)new ArithmeticAPOPathPricer(
                   payoff.optionType(),
                   payoff.strike(),
                   process.riskFreeRate().link.discount(exercise!.lastDate()),
                   this.arguments_.runningAccumulator.GetValueOrDefault(),
                   this.arguments_.pastFixings.GetValueOrDefault());
      }
      protected override PathPricer<IPath> controlPathPricer()
      {
         PlainVanillaPayoff payoff = (PlainVanillaPayoff)this.arguments_.payoff;
         Utils.QL_REQUIRE(payoff != null, () => "non-plain payoff given");

         EuropeanExercise exercise = (EuropeanExercise)this.arguments_.exercise;
         Utils.QL_REQUIRE(exercise != null, () => "wrong exercise given");

         GeneralizedBlackScholesProcess process = (GeneralizedBlackScholesProcess) this.process_;
         Utils.QL_REQUIRE(process != null, ()=> "Black-Scholes process required");

         // for seasoned option the geometric strike might be rescaled
         // to obtain an equivalent arithmetic strike.
         // Any change applied here MUST be applied to the analytic engine too
         return (PathPricer<IPath>)new GeometricAPOPathPricer(
                   payoff.optionType(),
                   payoff.strike(),
                   process!.riskFreeRate().link.discount(this.timeGrid().Last()));
      }
      protected override IPricingEngine controlPricingEngine()
      {
         GeneralizedBlackScholesProcess process =  (GeneralizedBlackScholesProcess) this.process_;
         Utils.QL_REQUIRE(process != null, ()=> "Black-Scholes process required");
         var engine = new AnalyticDiscreteGeometricAveragePriceAsianEngine(process);
         engine.setupArguments(this.arguments_);
         return engine;
      }
   }

   public class ArithmeticAPOPathPricer : PathPricer<IPath>
   {

      private PlainVanillaPayoff payoff_;
      private double discount_;
      private double runningSum_;
      private int pastFixings_;

      public ArithmeticAPOPathPricer(Option.Type type,
                                     double strike,
                                     double discount,
                                     double runningSum = 0.0,
                                     int pastFixings = 0)
      {
         payoff_ = new PlainVanillaPayoff(type, strike);
         discount_ = discount;
         runningSum_ = runningSum;
         pastFixings_ = pastFixings;
         Utils.QL_REQUIRE(strike >= 0.0, () => "strike less than zero not allowed");
      }

      public double value(Path path)
      {
         int n = path.length();
         Utils.QL_REQUIRE(n > 1, () => "the path cannot be empty");

         double sum = runningSum_;
         int fixings;
         if (path.timeGrid().mandatoryTimes()[0].IsEqual(0.0))
         {
            // include initial fixing
            for (int i = 0; i < path.length(); i++)
               sum += path[i];
            fixings = pastFixings_ + n;
         }
         else
         {
            for (int i = 1; i < path.length(); i++)
               sum += path[i];
            fixings = pastFixings_ + n - 1;
         }
         double averagePrice = sum / fixings;
         return discount_ * payoff_.value(averagePrice);

      }

      public double value(IPath path)
      {
         return value((Path)path);
      }
   }

   public class MakeMCDiscreteArithmeticAPEngine<RNG, S>
      where RNG : IRSG, new ()
      where S : Statistics, new ()
   {
      private GeneralizedBlackScholesProcess process_;
      private bool antithetic_ = false, controlVariate_ = false;
      private int? samples_, maxSamples_;
      private double? tolerance_;
      private bool brownianBridge_ = true;
      private ulong seed_ = 0;

      public MakeMCDiscreteArithmeticAPEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         samples_ = null;
         maxSamples_ = null;
         tolerance_ = null;
      }

      // named parameters
      public MakeMCDiscreteArithmeticAPEngine<RNG, S> withBrownianBridge(bool b = true)
      {
         brownianBridge_ = b;
         return this;
      }

      public MakeMCDiscreteArithmeticAPEngine<RNG, S> withSamples(int samples)
      {
         Utils.QL_REQUIRE(tolerance_ == null, () => "tolerance already set");
         samples_ = samples;
         return this;
      }

      public MakeMCDiscreteArithmeticAPEngine<RNG, S> withAbsoluteTolerance(double tolerance)
      {
         Utils.QL_REQUIRE(samples_ == null, () => "number of samples already set");
         Utils.QL_REQUIRE(FastActivator<RNG>.Create().allowsErrorEstimate != 0, () =>
                          "chosen random generator policy does not allow an error estimate");
         tolerance_ = tolerance;
         return this;
      }

      public MakeMCDiscreteArithmeticAPEngine<RNG, S> withMaxSamples(int samples)
      {
         maxSamples_ = samples;
         return this;
      }

      public MakeMCDiscreteArithmeticAPEngine<RNG, S> withSeed(ulong seed)
      {
         seed_ = seed;
         return this;
      }

      public MakeMCDiscreteArithmeticAPEngine<RNG, S> withAntitheticVariate(bool b = true)
      {
         antithetic_ = b;
         return this;
      }


      public MakeMCDiscreteArithmeticAPEngine<RNG, S> withControlVariate(bool b = true)
      {
         controlVariate_ = b;
         return this;
      }


      // conversion to pricing engine
      public IPricingEngine value()
      {
         return (IPricingEngine)new MCDiscreteArithmeticAPEngine<RNG, S>(process_,
                                                                         brownianBridge_,
                                                                         antithetic_, controlVariate_,
                                                                         samples_, tolerance_,
                                                                         maxSamples_,
                                                                         seed_);
      }
   }
}
