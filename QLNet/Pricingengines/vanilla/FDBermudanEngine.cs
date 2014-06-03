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
    //! Finite-differences Bermudan engine
    /*! \ingroup vanillaengines */
    public class FDBermudanEngine : FDMultiPeriodEngine, IGenericEngine {
        protected double extraTermInBermudan;

        // constructor
        public FDBermudanEngine(GeneralizedBlackScholesProcess process, int timeSteps = 100, int gridPoints = 100, 
			  bool timeDependent = false)
            : base(process, timeSteps, gridPoints, timeDependent) { }

        public void calculate() {
            setupArguments(arguments_);
            base.calculate(results_);
        }

        protected override void initializeStepCondition() {
            stepCondition_ = new NullCondition<Vector>();
        }

        protected override void executeIntermediateStep(int i) {
            int size = intrinsicValues_.size();
            for (int j=0; j<size; j++)
                prices_.setValue(j, Math.Max(prices_.value(j), intrinsicValues_.value(j)));
        }

        #region IGenericEngine copy-cat
        protected OneAssetOption.Arguments arguments_ = new OneAssetOption.Arguments();
        protected OneAssetOption.Results results_ = new OneAssetOption.Results();

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
