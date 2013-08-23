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
    //! Finite-differences pricing engine for American-style vanilla options
    public class FDStepConditionEngine : FDConditionEngineTemplate {
        protected TridiagonalOperator controlOperator_;
        protected List<BoundaryCondition<IOperator>> controlBCs_;
        protected SampledCurve controlPrices_;

        // required for generics
        public FDStepConditionEngine() { }
        // required for template inheritance
        public override FDVanillaEngine factory(GeneralizedBlackScholesProcess process,
                                                int timeSteps, int gridPoints, bool timeDependent) {
            return new FDStepConditionEngine(process, timeSteps, gridPoints, timeDependent);
        }

        //public FDStepConditionEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints,
        //     bool timeDependent = false)
        public FDStepConditionEngine(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent) {
            controlBCs_ = new InitializedList<BoundaryCondition<IOperator>>(2);
            controlPrices_ = new SampledCurve(gridPoints);
        }

        public override void calculate(IPricingEngineResults r) {
            OneAssetOption.Results results = r as OneAssetOption.Results;
            setGridLimits();
            initializeInitialCondition();
            initializeOperator();
            initializeBoundaryConditions();
            initializeStepCondition();

            // typedef StandardSystemFiniteDifferenceModel model_type;

            List<IOperator> operatorSet = new List<IOperator>();
            List<Vector> arraySet = new List<Vector>();
            BoundaryConditionSet bcSet = new BoundaryConditionSet();
            StepConditionSet<Vector> conditionSet = new StepConditionSet<Vector>();

            prices_ = (SampledCurve)intrinsicValues_.Clone();

            controlPrices_ = (SampledCurve)intrinsicValues_.Clone();
            controlOperator_ = (TridiagonalOperator)finiteDifferenceOperator_.Clone();
            controlBCs_[0] = BCs_[0];
            controlBCs_[1] = BCs_[1];

            operatorSet.Add(finiteDifferenceOperator_);
            operatorSet.Add(controlOperator_);

            arraySet.Add(prices_.values());
            arraySet.Add(controlPrices_.values());

            bcSet.Add(BCs_);
            bcSet.Add(controlBCs_);

            conditionSet.Add(stepCondition_);
            conditionSet.Add(new NullCondition<Vector>());

            var model = new FiniteDifferenceModel<ParallelEvolver<CrankNicolson<TridiagonalOperator>>>(operatorSet, bcSet);

            object temp = arraySet;
            model.rollback(ref temp, getResidualTime(), 0.0, timeSteps_, conditionSet);
            arraySet = (List<Vector>)temp;

            prices_.setValues(arraySet[0]);
            controlPrices_.setValues(arraySet[1]);

            StrikedTypePayoff striked_payoff = payoff_ as StrikedTypePayoff;
            if (striked_payoff == null)
                throw new ApplicationException("non-striked payoff given");

            double variance = process_.blackVolatility().link.blackVariance(exerciseDate_, striked_payoff.strike());
            double dividendDiscount = process_.dividendYield().link.discount(exerciseDate_);
            double riskFreeDiscount = process_.riskFreeRate().link.discount(exerciseDate_);
            double spot = process_.stateVariable().link.value();
            double forwardPrice = spot * dividendDiscount / riskFreeDiscount;

            BlackCalculator black = new BlackCalculator(striked_payoff, forwardPrice, Math.Sqrt(variance), riskFreeDiscount);

            results.value = prices_.valueAtCenter()
                - controlPrices_.valueAtCenter()
                + black.value();
            results.delta = prices_.firstDerivativeAtCenter()
                - controlPrices_.firstDerivativeAtCenter()
                + black.delta(spot);
            results.gamma = prices_.secondDerivativeAtCenter()
                - controlPrices_.secondDerivativeAtCenter()
                + black.gamma(spot);
            results.additionalResults.Add("priceCurve", prices_);
        } 
    }
}
