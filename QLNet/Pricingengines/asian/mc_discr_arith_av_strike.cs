/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

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
using System.Text;

/*! \file mc_discr_arith_av_strike.hpp
    \brief Monte Carlo engine for discrete arithmetic average-strike Asian
*/

namespace QLNet {

    //!  Monte Carlo pricing engine for discrete arithmetic average-strike Asian
    /*!  \ingroup asianengines */
    //template <class RNG = PseudoRandom, class S = Statistics>
    public class MCDiscreteArithmeticASEngine<RNG,S>
        :  MCDiscreteAveragingAsianEngine<RNG,S>
        where RNG : IRSG, new()
        where S : Statistics, new(){
             
        // constructor
        public MCDiscreteArithmeticASEngine(
             GeneralizedBlackScholesProcess process,
             bool brownianBridge,
             bool antitheticVariate,
             int requiredSamples,
             double requiredTolerance,
             int maxSamples,
             ulong seed)
        :   base(process,1,brownianBridge,antitheticVariate,false,
                requiredSamples,requiredTolerance,maxSamples,seed) {}


        protected override PathPricer<IPath> pathPricer(){
            PlainVanillaPayoff payoff = (PlainVanillaPayoff)(this.arguments_.payoff);
            if (payoff == null)
                throw new ApplicationException("non-plain payoff given");

            EuropeanExercise exercise = (EuropeanExercise)this.arguments_.exercise;
            if (exercise == null)
                throw new ApplicationException("wrong exercise given");

            return (PathPricer<IPath>)new ArithmeticASOPathPricer(
                    payoff.optionType(),
                    this.process_.riskFreeRate().link.discount(this.timeGrid().Last()),
                    this.arguments_.runningAccumulator.GetValueOrDefault(),
                    this.arguments_.pastFixings.GetValueOrDefault());
        }
    }

    public class ArithmeticASOPathPricer : PathPricer<Path> {

        private Option.Type type_;
        private double discount_;
        private double runningSum_;
        private int pastFixings_;

        public ArithmeticASOPathPricer(Option.Type type,
                                double discount,
                                double runningSum,
                                int pastFixings)
        {
            type_ = type;
            discount_ = discount;
            runningSum_ = runningSum;
            pastFixings_ = pastFixings;    
        }

        public ArithmeticASOPathPricer(Option.Type type,
                                double discount,
                                double runningSum) 
        : this(type,discount,runningSum,0){}

        public ArithmeticASOPathPricer(Option.Type type,
                                double discount)
        : this(type,discount,0.0,0){}

        public double value(Path path){
            int n = path.length();
            if(!(n > 1))
                throw new ApplicationException("the path cannot be empty");
            double averageStrike=runningSum_;
            if (path.timeGrid().mandatoryTimes()[0]==0.0) {
                //averageStrike =
                //std::accumulate(path.begin(),path.end(),runningSum_)/(pastFixings_ + n)
                for(int i=0;i<path.length();i++ )
                    averageStrike += path[i];
                averageStrike /= (pastFixings_ + n);
            } else {
                //averageStrike =
                //std::accumulate(path.begin()+1,path.end(),runningSum_)/(pastFixings_+n-1);
                for(int i=1;i<path.length();i++ )
                    averageStrike += path[i];
                averageStrike /= (pastFixings_ + n-1);
            }
            return discount_
                * new PlainVanillaPayoff(type_, averageStrike).value(path.back());
        }
    }

    public class MakeMCDiscreteArithmeticASEngine<RNG, S>
        where RNG : IRSG, new()
        where S : Statistics, new()
    {
        public MakeMCDiscreteArithmeticASEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            antithetic_ = false;
            samples_ = null;
            maxSamples_= null;
            tolerance_ = null; 
            brownianBridge_ = true;
            seed_ = 0;
        }

        // named parameters
        public MakeMCDiscreteArithmeticASEngine<RNG, S> withBrownianBridge(bool b){
            brownianBridge_ = b;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withBrownianBridge(){
            return withBrownianBridge(true);
        }

       public MakeMCDiscreteArithmeticASEngine<RNG, S> withSamples(int samples)
       {
          Utils.QL_REQUIRE( tolerance_ == null, () => "tolerance already set" );
         samples_ = samples;
         return this;
       }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withTolerance(double tolerance)
        {

           Utils.QL_REQUIRE( samples_ == null, () => "number of samples already set" );
            if ((new RNG().allowsErrorEstimate == 0))
                throw new ApplicationException("chosen random generator policy " +
                                               "does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withMaxSamples(int samples){
            maxSamples_ = samples;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withSeed(ulong seed){
            seed_ = seed;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withAntitheticVariate(bool b){
            antithetic_ = b;
            return this;
        }

        public MakeMCDiscreteArithmeticASEngine<RNG, S> withAntitheticVariate(){
            return this.withAntitheticVariate(true);
        }

        // conversion to pricing engine
        public IPricingEngine value(){
            return new MCDiscreteArithmeticASEngine<RNG,S>(process_,
                                                    brownianBridge_,
                                                    antithetic_,
                                                    samples_.Value, tolerance_.Value,
                                                    maxSamples_.Value,
                                                    seed_);
        }
      
        private GeneralizedBlackScholesProcess process_;
        private bool antithetic_;
        private int?  samples_, maxSamples_;
        private double? tolerance_;
        private bool brownianBridge_;
        private ulong seed_;
    }
}


