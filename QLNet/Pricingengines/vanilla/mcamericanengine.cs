/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet {
    //! American Monte Carlo engine
    /*! References:

        \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in web/literature
    */
    public class MCAmericanEngine<RNG, S> : MCLongstaffSchwartzEngine<VanillaOption.Engine, SingleVariate, RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new() {

        private int polynomOrder_;
        private LsmBasisSystem.PolynomType polynomType_;

        //     int nCalibrationSamples = Null<Size>()) 
        public MCAmericanEngine(GeneralizedBlackScholesProcess process, int timeSteps, int timeStepsPerYear,
             bool antitheticVariate, bool controlVariate, int requiredSamples, double requiredTolerance,
             int maxSamples, ulong seed, int polynomOrder, LsmBasisSystem.PolynomType polynomType,
             int nCalibrationSamples) 
            : base(process, timeSteps, timeStepsPerYear, false, antitheticVariate, controlVariate, requiredSamples,
                   requiredTolerance, maxSamples, seed, nCalibrationSamples) {
            polynomOrder_ = polynomOrder;
            polynomType_ = polynomType;
        }


        public override void calculate() {
            base.calculate();
            if (controlVariate_) {
                // control variate might lead to small negative
                // option values for deep OTM options
                this.results_.value = Math.Max(0.0, this.results_.value.Value);
            }
        }
        
        protected override LongstaffSchwartzPathPricer<IPath> lsmPathPricer() {
            GeneralizedBlackScholesProcess process = process_ as GeneralizedBlackScholesProcess;
            if (process == null)
                throw new ApplicationException("generalized Black-Scholes process required");

            EarlyExercise exercise = arguments_.exercise as EarlyExercise;
            if (exercise == null)
                throw new ApplicationException("wrong exercise given");
            if(exercise.payoffAtExpiry())
                throw new ApplicationException("payoff at expiry not handled");

            AmericanPathPricer earlyExercisePathPricer = new AmericanPathPricer(arguments_.payoff, polynomOrder_, polynomType_);

            return new LongstaffSchwartzPathPricer<IPath>(timeGrid(), earlyExercisePathPricer, process.riskFreeRate());
        }

        protected override double controlVariateValue() {
            IPricingEngine controlPE = controlPricingEngine();

            if (controlPE == null)
                throw new ApplicationException("engine does not provide control variation pricing engine");

            VanillaOption.Arguments controlArguments = controlPE.getArguments() as VanillaOption.Arguments;
            controlArguments = arguments_;
            controlArguments.exercise = new EuropeanExercise(arguments_.exercise.lastDate());

            controlPE.calculate();

            VanillaOption.Results controlResults = controlPE.getResults() as VanillaOption.Results;

            return controlResults.value.GetValueOrDefault();
        }

        protected override IPricingEngine controlPricingEngine() {
            GeneralizedBlackScholesProcess process = process_ as GeneralizedBlackScholesProcess;
            if (process == null)
                throw new ApplicationException("generalized Black-Scholes process required");

            return new AnalyticEuropeanEngine(process);
        }

        protected override PathPricer<IPath> controlPathPricer() {
            StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
            if(payoff == null)
                throw new ApplicationException("StrikedTypePayoff needed for control variate");

            GeneralizedBlackScholesProcess process = process_ as GeneralizedBlackScholesProcess;
            if (process == null)
                throw new ApplicationException("generalized Black-Scholes process required");

            return new EuropeanPathPricer(payoff.optionType(), payoff.strike(),
                                          process.riskFreeRate().link.discount(timeGrid().Last()));
        }
    }


    public class AmericanPathPricer : IEarlyExercisePathPricer<IPath, double>  {
        protected double scalingValue_;
        protected Payoff payoff_;
        protected List<Func<double, double>> v_ = new List<Func<double,double>>();

        public AmericanPathPricer(Payoff payoff, int polynomOrder, LsmBasisSystem.PolynomType polynomType) {
            scalingValue_ = 1;
            payoff_ = payoff;
            v_ = LsmBasisSystem.pathBasisSystem(polynomOrder, polynomType);

            if (!(polynomType == LsmBasisSystem.PolynomType.Monomial
                  || polynomType == LsmBasisSystem.PolynomType.Laguerre
                  || polynomType == LsmBasisSystem.PolynomType.Hermite
                  || polynomType == LsmBasisSystem.PolynomType.Hyperbolic
                  || polynomType == LsmBasisSystem.PolynomType.Chebyshev2th))
                throw new ApplicationException("insufficient polynom type");

            // the payoff gives an additional value
            v_.Add(this.payoff);

            StrikedTypePayoff strikePayoff = payoff_ as StrikedTypePayoff;

            if (strikePayoff != null) {
                scalingValue_/=strikePayoff.strike();
            }
        }

        // scale values of the underlying to increase numerical stability
        public double state(IPath path, int t) { return (path as Path)[t]*scalingValue_; }
        public double value(IPath path, int t) { return payoff(state(path, t)); }
        public List<Func<double, double>> basisSystem() { return v_; }
        protected double payoff(double state) { return payoff_.value(state / scalingValue_); }
    }


    //! Monte Carlo American engine factory
    //template <class RNG = PseudoRandom, class S = Statistics>
    public class MakeMCAmericanEngine<RNG> : MakeMCAmericanEngine<RNG, Statistics>
        where RNG : IRSG, new() {
        public MakeMCAmericanEngine(GeneralizedBlackScholesProcess process) : base(process) { }
    }

    public class MakeMCAmericanEngine<RNG, S>
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new() {

        private GeneralizedBlackScholesProcess process_;
        private bool antithetic_, controlVariate_;
        private int steps_, stepsPerYear_;
        private int samples_, maxSamples_, calibrationSamples_;
        private double tolerance_;
        private ulong seed_;
        private int polynomOrder_;
        private LsmBasisSystem.PolynomType polynomType_;

        public MakeMCAmericanEngine(GeneralizedBlackScholesProcess process) {
            process_ = process;
            antithetic_ = false;
            controlVariate_ = false;
            steps_ = 0;
            stepsPerYear_ = 0;
            samples_ = 0;
            maxSamples_ = 0;
            calibrationSamples_ = 2048;
            tolerance_ = 0;
            seed_ = 0;
            polynomOrder_ = 2;
            polynomType_ = LsmBasisSystem.PolynomType.Monomial;
        }
        
        // named parameters
        public MakeMCAmericanEngine<RNG, S> withSteps(int steps) {
            steps_ = steps;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withStepsPerYear(int steps) {
            stepsPerYear_ = steps;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withSamples(int samples) {
            if (tolerance_ != 0)
                throw new ApplicationException("tolerance already set");
            samples_ = samples;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withAbsoluteTolerance(double tolerance) {
            if (samples_ != 0)
                throw new ApplicationException("number of samples already set");

            if (new RNG().allowsErrorEstimate == 0)
                throw new ApplicationException("chosen random generator policy does not allow an error estimate");
            tolerance_ = tolerance;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withMaxSamples(int samples) {
            maxSamples_ = samples;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withSeed(ulong seed) {
            seed_ = seed;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withAntitheticVariate() { return withAntitheticVariate(true); }
        public MakeMCAmericanEngine<RNG, S> withAntitheticVariate(bool b) {
            antithetic_ = b;
            return this;
        }
        //public MakeMCAmericanEngine withControlVariate(bool b = true);
        public MakeMCAmericanEngine<RNG, S> withControlVariate(bool b) {
            controlVariate_ = b;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withPolynomOrder(int polynomOrder) {
            polynomOrder_ = polynomOrder;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withBasisSystem(LsmBasisSystem.PolynomType polynomType) {
            polynomType_ = polynomType;
            return this;
        }
        public MakeMCAmericanEngine<RNG, S> withCalibrationSamples(int samples) {
            calibrationSamples_ = samples;
            return this;
        }

        // conversion to pricing engine
        public IPricingEngine value() {
            if (!(steps_ != 0 || stepsPerYear_ != 0))
                throw new ApplicationException("number of steps not given");
            if (!(steps_ == 0 || stepsPerYear_ == 0))
                throw new ApplicationException("number of steps overspecified");
            return new MCAmericanEngine<RNG, S>(process_, steps_, stepsPerYear_, antithetic_, controlVariate_, samples_, tolerance_,
                                                maxSamples_, seed_, polynomOrder_, polynomType_, calibrationSamples_);
        }
    }
}
