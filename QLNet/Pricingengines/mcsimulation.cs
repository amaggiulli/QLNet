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
    //! base class for Monte Carlo engines
    /*! Eventually this class might offer greeks methods.  Deriving a
        class from McSimulation gives an easy way to write a Monte
        Carlo engine.

        See McVanillaEngine as an example.
    */
    public abstract class McSimulation<MC, RNG, S> where S : IGeneralStatistics, new() {
        //typedef typename MonteCarloModel<MC,RNG,S>::path_generator_type path_generator_type;
        //typedef typename MonteCarloModel<MC,RNG,S>::path_pricer_type path_pricer_type;
        //typedef typename MonteCarloModel<MC,RNG,S>::stats_type stats_type;
        //typedef typename MonteCarloModel<MC,RNG,S>::result_type result_type;

        protected MonteCarloModel<MC,RNG,S> mcModel_;
        protected bool antitheticVariate_, controlVariate_;


        protected McSimulation(bool antitheticVariate, bool controlVariate) {
            antitheticVariate_ = antitheticVariate;
            controlVariate_ = controlVariate;
        }


        //! add samples until the required absolute tolerance is reached
        public double value(double tolerance) { return value(tolerance, int.MaxValue, 1023); }
        public double value(double tolerance, int maxSamples) { return value(tolerance, maxSamples, 1023); }
        public double value(double tolerance, int maxSamples, int minSamples) {
            int sampleNumber = mcModel_.sampleAccumulator().samples();
            if (sampleNumber<minSamples) {
                mcModel_.addSamples(minSamples-sampleNumber);
                sampleNumber = mcModel_.sampleAccumulator().samples();
            }

            int nextBatch;
            double order;
            double error = mcModel_.sampleAccumulator().errorEstimate();
            while (maxError(error) > tolerance) {
                if (!(sampleNumber<maxSamples))
                    throw new ApplicationException("max number of samples (" + maxSamples
                           + ") reached, while error (" + error
                           + ") is still above tolerance (" + tolerance + ")");

                // conservative estimate of how many samples are needed
                order = maxError(error*error)/tolerance/tolerance;
                nextBatch = (int)Math.Max(sampleNumber * order * 0.8 - sampleNumber, minSamples);

                // do not exceed maxSamples
                nextBatch = Math.Min(nextBatch, maxSamples-sampleNumber);
                sampleNumber += nextBatch;
                mcModel_.addSamples(nextBatch);
                error = mcModel_.sampleAccumulator().errorEstimate();
            }

            return mcModel_.sampleAccumulator().mean();
        }

        //! simulate a fixed number of samples
        public double valueWithSamples(int samples) {

            int sampleNumber = mcModel_.sampleAccumulator().samples();

            if (!(samples>=sampleNumber))
                throw new ApplicationException("number of already simulated samples (" + sampleNumber
                       + ") greater than requested samples (" + samples + ")");

            mcModel_.addSamples(samples-sampleNumber);

            return mcModel_.sampleAccumulator().mean();
        }

        //! error estimated using the samples simulated so far
        public double errorEstimate() { return mcModel_.sampleAccumulator().errorEstimate(); }

        //! access to the sample accumulator for richer statistics
        public S sampleAccumulator() { return mcModel_.sampleAccumulator(); }
        
        //! basic calculate method provided to inherited pricing engines
        public void calculate(double requiredTolerance, int requiredSamples, int maxSamples) {

            if (!(requiredTolerance != 0 || requiredSamples != 0))
                throw new ApplicationException("neither tolerance nor number of samples set");

            //! Initialize the one-factor Monte Carlo
            if (this.controlVariate_) {

                double controlVariateValue = this.controlVariateValue();
                if (controlVariateValue == 0)
                    throw new ApplicationException("engine does not provide control-variation price");

                PathPricer<IPath> controlPP = this.controlPathPricer();
                if (controlPP == null)
                    throw new ApplicationException("engine does not provide control-variation path pricer");

                PathGenerator<IRNG> controlPG = this.controlPathGenerator();

                this.mcModel_ = new MonteCarloModel<MC,RNG,S>(pathGenerator(), pathPricer(), new S(), antitheticVariate_, 
                                                              controlPP, controlVariateValue, controlPG);
            } else {
                this.mcModel_ = new MonteCarloModel<MC,RNG,S>(pathGenerator(), pathPricer(), new S(), antitheticVariate_);
            }

            if (requiredTolerance != 0) {
                if (maxSamples != 0)
                    value(requiredTolerance, maxSamples);
                else
                    value(requiredTolerance);
            } else {
                valueWithSamples(requiredSamples);
            }
        }


        protected abstract PathPricer<IPath> pathPricer();
        protected abstract PathGenerator<IRNG> pathGenerator();
        protected abstract TimeGrid timeGrid();
        protected virtual PathPricer<IPath> controlPathPricer() { return null; }
        protected virtual PathGenerator<IRNG> controlPathGenerator() { return null; }
        protected virtual IPricingEngine controlPricingEngine() { return null; }
        protected virtual double controlVariateValue() { return 0; }

        protected static double maxError(List<double> sequence) { return sequence.Max(); }
        protected static double maxError(double error) { return error; }
    }
}
