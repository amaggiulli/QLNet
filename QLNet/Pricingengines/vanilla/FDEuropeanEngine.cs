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
    //! Pricing engine for European options using finite-differences
    /*! \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              checking it against analytic results.
    */
    public class FDEuropeanEngine : FDVanillaEngine, IGenericEngine {
        private SampledCurve prices_;

        //public FDEuropeanEngine(GeneralizedBlackScholesProcess process,
        //                        Size timeSteps=100, Size gridPoints=100, bool timeDependent = false) {
        public FDEuropeanEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints)
            : this(process, timeSteps, gridPoints, false) { }
        public FDEuropeanEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent) {
            prices_ = new SampledCurve(gridPoints);

            process.registerWith(update);
        }

        public void calculate() {
            setupArguments(arguments_);
            setGridLimits();
            initializeInitialCondition();
            initializeOperator();
            initializeBoundaryConditions();

            var model = new FiniteDifferenceModel<CrankNicolson<TridiagonalOperator>>(finiteDifferenceOperator_, BCs_);

            prices_ = (SampledCurve)intrinsicValues_.Clone();

            // this is a workaround for pointers to avoid unsafe code
            // in the grid calculation Vector temp goes through many operations
            object temp = prices_.values();
            model.rollback(ref temp, getResidualTime(), 0, timeSteps_);
            prices_.setValues((Vector)temp);

            results_.value = prices_.valueAtCenter();
            results_.delta = prices_.firstDerivativeAtCenter();
            results_.gamma = prices_.secondDerivativeAtCenter();
            results_.theta = Utils.blackScholesTheta(process_,
                                                     results_.value.GetValueOrDefault(),
                                                     results_.delta.GetValueOrDefault(),
                                                     results_.gamma.GetValueOrDefault());
            results_.additionalResults.Add("priceCurve", prices_);
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
