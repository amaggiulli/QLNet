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

namespace QLNet
{
    //!  Monte Carlo pricing engine for discrete arithmetic average price Asian
    /*!  Monte Carlo pricing engine for discrete arithmetic average price
         Asian options. It can use MCDiscreteGeometricAPEngine (Monte Carlo
         discrete arithmetic average price engine) and
         AnalyticDiscreteGeometricAveragePriceAsianEngine (analytic discrete
         arithmetic average price engine) for control variation.

         \ingroup asianengines

         \test the correctness of the returned value is tested by
               reproducing results available in literature.
    */
    //template <class RNG = PseudoRandom, class S = Statistics>
    public class MCDiscreteArithmeticAPEngine<RNG, S>
        : MCDiscreteAveragingAsianEngine<RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {

        // constructor
        public MCDiscreteArithmeticAPEngine(
             GeneralizedBlackScholesProcess process,
             int maxTimeStepPerYear,
             bool brownianBridge,
             bool antitheticVariate,
             bool controlVariate,
             int requiredSamples,
             double requiredTolerance,
             int maxSamples,
             ulong seed)
            : base(process, maxTimeStepPerYear, brownianBridge, antitheticVariate,
                   controlVariate, requiredSamples, requiredTolerance, maxSamples, seed)
        {
        }

        protected override PathPricer<IPath> pathPricer()
        {
            PlainVanillaPayoff payoff = (PlainVanillaPayoff)(this.arguments_.payoff);
            if (payoff == null)
                throw new ApplicationException("non-plain payoff given");

            EuropeanExercise exercise = (EuropeanExercise)this.arguments_.exercise;
            if (exercise == null)
                throw new ApplicationException("wrong exercise given");

            return (PathPricer<IPath>)new ArithmeticAPOPathPricer(
                        payoff.optionType(),
                        payoff.strike(),
                        this.process_.riskFreeRate().link.discount(this.timeGrid().Last()),
                        this.arguments_.runningAccumulator.GetValueOrDefault(),
                        this.arguments_.pastFixings.GetValueOrDefault());
        }

        protected override PathPricer<IPath> controlPathPricer()
        {
            PlainVanillaPayoff payoff = (PlainVanillaPayoff)this.arguments_.payoff;
            if (payoff == null)
                throw new ApplicationException("non-plain payoff given");

            EuropeanExercise exercise = (EuropeanExercise)this.arguments_.exercise;
            if (exercise == null)
                throw new ApplicationException("wrong exercise given");

            // for seasoned option the geometric strike might be rescaled
            // to obtain an equivalent arithmetic strike.
            // Any change applied here MUST be applied to the analytic engine too
            return (PathPricer<IPath>)new GeometricAPOPathPricer(  
                        payoff.optionType(),
                        payoff.strike(),
                        this.process_.riskFreeRate().link.discount(this.timeGrid().Last()));
        }

        protected override IPricingEngine controlPricingEngine()  {
            return new AnalyticDiscreteGeometricAveragePriceAsianEngine(this.process_);
        }
    }

    public class ArithmeticAPOPathPricer : PathPricer<Path>
    {

        private PlainVanillaPayoff payoff_;
        private double discount_;
        private double runningSum_;
        private int pastFixings_;

        public ArithmeticAPOPathPricer(Option.Type type,
                                double strike,
                                double discount,
                                double runningSum,
                                int pastFixings )
        {
            payoff_=new PlainVanillaPayoff(type, strike);
            discount_ = discount;
            runningSum_ = runningSum;
            pastFixings_ = pastFixings;
            if(!(strike>=0.0))
                throw new ApplicationException("strike less than zero not allowed");
        }

        public ArithmeticAPOPathPricer(Option.Type type,
                                double strike,
                                double discount,
                                double runningSum)
            : this(type, strike, discount, runningSum, 0) { }

        public ArithmeticAPOPathPricer(Option.Type type,
                                double strike,
                                double discount)
            : this(type, strike, discount, 0.0, 0) { }


        public double value(Path path)
        {
            int n = path.length();
            if(!(n>1))
               throw new ApplicationException("the path cannot be empty");

            double sum = runningSum_;
            int fixings;
            if (path.timeGrid().mandatoryTimes()[0]==0.0) {
                // include initial fixing
                //sum = std::accumulate(path.begin(),path.end(),runningSum_);  
                for(int i=0;i<path.length();i++ )
                    sum += path[i];
                fixings = pastFixings_ + n;
            } else {
                //sum = std::accumulate(path.begin()+1,path.end(),runningSum_);
                for (int i = 1; i < path.length(); i++)
                    sum += path[i];
                fixings = pastFixings_ + n - 1;
            }
            double averagePrice = sum/fixings;
            return discount_ * payoff_.value(averagePrice);

            }

        /*public double value(IPath path){
            if (!(path.length() > 0))
                throw new ApplicationException("the path cannot be empty");
            return payoff_.value((path as Path).back()) * discount_;
        }*/
    }
    //<class RNG = PseudoRandom, class S = Statistics>
    public class MakeMCDiscreteArithmeticAPEngine<RNG, S>
        where RNG : IRSG, new()
        where S : Statistics, new()
    {
        public MakeMCDiscreteArithmeticAPEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            antithetic_ = false;
            controlVariate_ = false;
            steps_= null;
            samples_ = null;
            maxSamples_ = null;
            tolerance_ = null;
            brownianBridge_ = true;
            seed_ = 0;
        }

        // named parameters
        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withStepsPerYear(int maxSteps){
            steps_ = maxSteps;
            return this;
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withBrownianBridge(bool b){
            brownianBridge_ = b;
            return this;
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withBrownianBridge(){
            return withBrownianBridge(true);
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withSamples(int samples){
           Utils.QL_REQUIRE( tolerance_ == null, () => "tolerance already set" );
            samples_ = samples;
            return this;
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withTolerance(double tolerance)
        {
           Utils.QL_REQUIRE( samples_ == null, () => "number of samples already set" );
            if ((new RNG().allowsErrorEstimate == 0))
                throw new ApplicationException("chosen random generator policy " +
                                               "does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withMaxSamples(int samples){
            maxSamples_ = samples;
            return this;
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withSeed(ulong seed){
            seed_ = seed;
            return this;
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withAntitheticVariate(bool b){
            antithetic_ = b;
            return this;
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withAntitheticVariate(){
            return this.withAntitheticVariate(true);
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withControlVariate(bool b){
            controlVariate_ = b;
            return this;
        }

        public MakeMCDiscreteArithmeticAPEngine<RNG, S> withControlVariate(){
            return this.withControlVariate(true);
        }


        // conversion to pricing engine
        public IPricingEngine value()
        {
            if (steps_ == null)
                throw new ApplicationException("max number of steps per year not given");
            return (IPricingEngine)new MCDiscreteArithmeticAPEngine<RNG, S>(process_,
                                                    steps_.Value,
                                                    brownianBridge_,
                                                    antithetic_, controlVariate_,
                                                    samples_.Value, tolerance_.Value,
                                                    maxSamples_.Value,
                                                    seed_);

        }

        private GeneralizedBlackScholesProcess process_;
        private bool antithetic_, controlVariate_;
        private int? steps_, samples_, maxSamples_;
        private double? tolerance_;
        private bool brownianBridge_;
        private ulong seed_;
    }

}
