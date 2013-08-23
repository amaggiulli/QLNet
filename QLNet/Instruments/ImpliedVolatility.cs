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
    //! helper class for one-asset implied-volatility calculation
    /*! The passed engine must be linked to the passed quote (see,
        e.g., VanillaOption to see how this can be achieved.) */
    public static class ImpliedVolatilityHelper {
        public static double calculate(Instrument instrument, IPricingEngine engine, SimpleQuote volQuote,
                                       double targetValue, double accuracy, int maxEvaluations, double minVol, double maxVol) {

            instrument.setupArguments(engine.getArguments());
            engine.getArguments().validate();

            PriceError f = new PriceError(engine, volQuote, targetValue);
            Brent solver = new Brent();
            solver.setMaxEvaluations(maxEvaluations);
            double guess = (minVol+maxVol)/2.0;
            double result = solver.solve(f, accuracy, guess, minVol, maxVol);
            return result;
        }

        public static GeneralizedBlackScholesProcess clone(GeneralizedBlackScholesProcess process, SimpleQuote volQuote) {
            Handle<Quote> stateVariable = process.stateVariable();
            Handle<YieldTermStructure> dividendYield = process.dividendYield();
            Handle<YieldTermStructure> riskFreeRate = process.riskFreeRate();

            Handle<BlackVolTermStructure> blackVol = process.blackVolatility();
            var volatility = new Handle<BlackVolTermStructure>(new BlackConstantVol(blackVol.link.referenceDate(),
                                                               blackVol.link.calendar(), new Handle<Quote>(volQuote),
                                                               blackVol.link.dayCounter()));

            return new GeneralizedBlackScholesProcess(stateVariable, dividendYield, riskFreeRate, volatility);
        }
    }

    public class PriceError : ISolver1d {
        private IPricingEngine engine_;
        private SimpleQuote vol_;
        private double targetValue_;
        private Instrument.Results results_;

        public PriceError(IPricingEngine engine, SimpleQuote vol, double targetValue) {
            engine_ = engine;
            vol_ = vol;
            targetValue_ = targetValue;
            
            results_ = engine_.getResults() as Instrument.Results;
            if (results_ == null)
                throw new ApplicationException("pricing engine does not supply needed results");
        }

        public override double value(double x) {
            vol_.setValue(x);
            engine_.calculate();
            return results_.value.GetValueOrDefault() - targetValue_;
        }
    }
}
