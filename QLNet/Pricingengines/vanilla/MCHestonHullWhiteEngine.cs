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

namespace QLNet
{
   public class MCHestonHullWhiteEngine<RNG, S> : MCVanillaEngine<MultiVariate, RNG, S>
      where RNG : IRSG, new()
      where S : IGeneralStatistics, new()
   {
      public MCHestonHullWhiteEngine( HybridHestonHullWhiteProcess process,
                                      int? timeSteps,
                                      int? timeStepsPerYear,
                                      bool antitheticVariate,
                                      bool controlVariate,
                                      int? requiredSamples,
                                      double? requiredTolerance,
                                      int? maxSamples,
                                      ulong seed)
         :base(process, timeSteps, timeStepsPerYear,false, antitheticVariate,controlVariate, requiredSamples,
               requiredTolerance, maxSamples, seed)
      {
         process_ =  process;
      }

      public override void calculate()
      {
         base.calculate();

         if (this.controlVariate_) 
         {
            // control variate might lead to small negative
            // option values for deep OTM options
            this.results_.value = Math.Max(0.0, this.results_.value.GetValueOrDefault());
         }
      }
        
      protected override PathPricer<IPath> pathPricer()
      {
         Exercise exercise = this.arguments_.exercise as Exercise;

         Utils.QL_REQUIRE(exercise.type() == Exercise.Type.European,()=> "only european exercise is supported");

         double exerciseTime = process_.time(exercise.lastDate());

         return new HestonHullWhitePathPricer(exerciseTime,this.arguments_.payoff,(HybridHestonHullWhiteProcess) process_);
      }

      protected override PathPricer<IPath> controlPathPricer()
      {
         HybridHestonHullWhiteProcess process = process_ as HybridHestonHullWhiteProcess;
         Utils.QL_REQUIRE( process != null, () => "invalid process" );

         HestonProcess hestonProcess = process.hestonProcess() ;

         Utils.QL_REQUIRE(hestonProcess != null ,()=> 
            "first constituent of the joint stochastic process need to be of type HestonProcess");

         Exercise exercise = this.arguments_.exercise;

         Utils.QL_REQUIRE(exercise.type() == Exercise.Type.European,()=>"only european exercise is supported");

         double exerciseTime = process.time(exercise.lastDate());

         return new HestonHullWhitePathPricer(exerciseTime,this.arguments_.payoff,process) ;

      }
      protected override IPricingEngine controlPricingEngine()
      {
         HybridHestonHullWhiteProcess process = process_ as HybridHestonHullWhiteProcess;
         Utils.QL_REQUIRE( process != null, () => "invalid process" );

         HestonProcess hestonProcess = process.hestonProcess();

         HullWhiteForwardProcess hullWhiteProcess = process.hullWhiteProcess();

         HestonModel hestonModel = new HestonModel(hestonProcess);
        
         HullWhite hwModel = new HullWhite(hestonProcess.riskFreeRate(),
                                           hullWhiteProcess.a(),
                                           hullWhiteProcess.sigma());

         return new AnalyticHestonHullWhiteEngine(hestonModel, hwModel, 144);
      }

      protected override IPathGenerator<IRNG> controlPathGenerator()
      {
         int dimensions = process_.factors();
         TimeGrid grid = this.timeGrid();
         IRNG generator = (IRNG)new  RNG().make_sequence_generator(dimensions*(grid.size()-1),this.seed_);
         HybridHestonHullWhiteProcess process = process_ as HybridHestonHullWhiteProcess;
         Utils.QL_REQUIRE( process != null , ()=> "invalid process");
         HybridHestonHullWhiteProcess cvProcess = new HybridHestonHullWhiteProcess( process.hestonProcess(),
            process.hullWhiteProcess(), 0.0, process.discretization() );

         return  new MultiPathGenerator<IRNG>(cvProcess, grid, generator, false) ;
      }
   }
   
   //! Monte Carlo Heston/Hull-White engine factory
   public class MakeMCHestonHullWhiteEngine <RNG , S> 
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
   {
      public MakeMCHestonHullWhiteEngine( HybridHestonHullWhiteProcess process)
      {
         process_ = process;
         steps_ = null;
         stepsPerYear_ = null;
         samples_ = null;
         maxSamples_ = null;
         antithetic_ = false;
         controlVariate_ = false;
         tolerance_ = null;
         seed_ = 0;
      }
      // named parameters
      public MakeMCHestonHullWhiteEngine<RNG, S> withSteps( int steps )
      {
         steps_ = steps;
         return this;
      }
      public MakeMCHestonHullWhiteEngine<RNG, S> withStepsPerYear( int steps )
      {
         stepsPerYear_ = steps;
         return this;
      }
      public MakeMCHestonHullWhiteEngine<RNG, S> withAntitheticVariate( bool b = true )
      {
         antithetic_ = b;
         return this;
      }
      public MakeMCHestonHullWhiteEngine<RNG, S> withControlVariate( bool b = true )
      {
         controlVariate_ = b;
         return this;
      }
      public MakeMCHestonHullWhiteEngine<RNG, S> withSamples( int samples )
      {
         Utils.QL_REQUIRE( tolerance_ == null,()=> "tolerance already set" );
         samples_ = samples;
         return this;
      }
      public MakeMCHestonHullWhiteEngine<RNG, S> withAbsoluteTolerance( double tolerance )
      {
         Utils.QL_REQUIRE(samples_ == null,()=> "number of samples already set");
         Utils.QL_REQUIRE( new RNG().allowsErrorEstimate != 0, () => 
            "chosen random generator policy does not allow an error estimate" );
         tolerance_ = tolerance;
         return this;
      }
      public MakeMCHestonHullWhiteEngine<RNG, S> withMaxSamples( int samples )
      {
         maxSamples_ = samples;
         return this;
      }
      public MakeMCHestonHullWhiteEngine<RNG, S> withSeed( ulong seed )
      {
         seed_ = seed;
         return this;
      }
      // conversion to pricing engine
      public IPricingEngine getAsPricingEngine()
      {
         Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null,()=> "number of steps not given");
         Utils.QL_REQUIRE(steps_ == null || stepsPerYear_ == null,()=> "number of steps overspecified");
         return new MCHestonHullWhiteEngine<RNG,S>(process_,
                                           steps_,
                                           stepsPerYear_,
                                           antithetic_,
                                           controlVariate_,
                                           samples_,
                                           tolerance_,
                                           maxSamples_,
                                           seed_);
      }
   
      private HybridHestonHullWhiteProcess process_;
      private int? steps_, stepsPerYear_, samples_, maxSamples_;
      private bool antithetic_, controlVariate_;
      private double? tolerance_;
      private ulong seed_;
   }

   public class HestonHullWhitePathPricer : PathPricer<IPath> 
   {
      public HestonHullWhitePathPricer( double exerciseTime,Payoff payoff,HybridHestonHullWhiteProcess process)
      {
         exerciseTime_ = exerciseTime;
         payoff_ = payoff;
         process_ = process;
      }

      public double value(IPath path)
      {
         MultiPath p = path as MultiPath;
         Utils.QL_REQUIRE( p != null , ()=> "invalid path");

         Utils.QL_REQUIRE(p.pathSize() > 0,()=> "the path cannot be empty");

         Vector states = new Vector(p.assetNumber());
         for (int j=0; j < states.size(); ++j) 
         {
            states[j] = p[j][p.pathSize()-1];
         }

         double df = 1.0/process_.numeraire(exerciseTime_, states);
         return payoff_.value(states[0])*df;
      }

      private double exerciseTime_;
      private Payoff payoff_;
      private HybridHestonHullWhiteProcess process_;
   }
}
