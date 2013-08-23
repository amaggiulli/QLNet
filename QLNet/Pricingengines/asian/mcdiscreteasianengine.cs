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
    //! Pricing engine for discrete average Asians using Monte Carlo simulation
    /*! \warning control-variate calculation is disabled under VC++6.

        \ingroup asianengines
    */
  
    public class MCDiscreteAveragingAsianEngine<RNG,S> : McSimulation<SingleVariate, RNG, S>, IGenericEngine
            //DiscreteAveragingAsianOption.Engine,
            //McSimulation<SingleVariate,RNG,S> 
            where RNG : IRSG, new()
            where S : IGeneralStatistics, new()
    {     
        /*typedef
        typename McSimulation<SingleVariate,RNG,S>::path_generator_type
            path_generator_type;
        typedef typename McSimulation<SingleVariate,RNG,S>::path_pricer_type
            path_pricer_type;
        typedef typename McSimulation<SingleVariate,RNG,S>::stats_type
            stats_type;
         */
        
        // data members
        protected GeneralizedBlackScholesProcess process_;
        protected int maxTimeStepsPerYear_;
        protected int requiredSamples_, maxSamples_;
        double requiredTolerance_;
        bool brownianBridge_;
        ulong seed_;

        // constructor
        public MCDiscreteAveragingAsianEngine(
             GeneralizedBlackScholesProcess process,
             int maxTimeStepsPerYear,
             bool brownianBridge,
             bool antitheticVariate,
             bool controlVariate,
             int requiredSamples,
             double requiredTolerance,
             int maxSamples,
             ulong seed) : base(controlVariate,antitheticVariate)
        {      
            process_=process;
            maxTimeStepsPerYear_ = maxTimeStepsPerYear;
            requiredSamples_=requiredSamples;
            maxSamples_ = maxSamples;
            requiredTolerance_=requiredTolerance;
            brownianBridge_ = brownianBridge;
            seed_=seed;
            process_.registerWith(update);
        }

        public void calculate() {
            base.calculate(requiredTolerance_,requiredSamples_,maxSamples_);
            results_.value = this.mcModel_.sampleAccumulator().mean();
            if (new RNG().allowsErrorEstimate!=0)
            results_.errorEstimate =
                this.mcModel_.sampleAccumulator().errorEstimate();
        }
      
        // McSimulation implementation
        protected override TimeGrid timeGrid() {
            Date referenceDate = process_.riskFreeRate().link.referenceDate();
            DayCounter voldc = process_.blackVolatility().link.dayCounter() ;
            List<double> fixingTimes = new  InitializedList<double>(arguments_.fixingDates.Count());
            
            for (int i=0; i<arguments_.fixingDates.Count(); i++) {
                if (arguments_.fixingDates[i]>=referenceDate) {
                    double t = voldc.yearFraction(referenceDate,
                        arguments_.fixingDates[i]);
                    fixingTimes.Add( t);
                }
            }
            // handle here maxStepsPerYear
            return new TimeGrid(fixingTimes.Last(), fixingTimes.Count());
        }

        protected override PathGenerator<IRNG> pathGenerator() {

            TimeGrid grid = this.timeGrid();
            IRNG gen = (IRNG)new  RNG().make_sequence_generator(grid.size()-1,seed_);
            return new PathGenerator<IRNG>(process_, grid,
                                           gen, brownianBridge_);
        }

        protected override double controlVariateValue() {
            IPricingEngine controlPE = this.controlPricingEngine(); 
            if(controlPE==null)
                throw new ApplicationException( "engine does not provide " +
                                                "control variation pricing engine");

            DiscreteAveragingAsianOption.Arguments controlArguments =
                    (DiscreteAveragingAsianOption.Arguments)controlPE.getArguments();
            controlArguments = arguments_;
            controlPE.calculate();

            DiscreteAveragingAsianOption.Results controlResults =
                (DiscreteAveragingAsianOption.Results)(controlPE.getResults());

            return controlResults.value.GetValueOrDefault();
    
        }

        protected override PathPricer<IPath> pathPricer() { 
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
        public event Callback notifyObserversEvent;
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers() {
            Callback handler = notifyObserversEvent;
            if (handler != null) {
                handler();
            }
        }

        public void update() { notifyObservers(); }
        #endregion 
        #endregion
    }
}
