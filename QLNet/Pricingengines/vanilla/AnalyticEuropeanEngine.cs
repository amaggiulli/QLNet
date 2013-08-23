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
    //! Pricing engine for European vanilla options using analytical formulae
    /*! \ingroup vanillaengines

        \test
        - the correctness of the returned value is tested by
          reproducing results available in literature.
        - the correctness of the returned greeks is tested by
          reproducing results available in literature.
        - the correctness of the returned greeks is tested by
          reproducing numerical derivatives.
        - the correctness of the returned implied volatility is tested
          by using it for reproducing the target value.
        - the implied-volatility calculation is tested by checking
          that it does not modify the option.
        - the correctness of the returned value in case of
          cash-or-nothing digital payoff is tested by reproducing
          results available in literature.
        - the correctness of the returned value in case of
          asset-or-nothing digital payoff is tested by reproducing
          results available in literature.
        - the correctness of the returned value in case of gap digital
          payoff is tested by reproducing results available in
          literature.
        - the correctness of the returned greeks in case of
          cash-or-nothing digital payoff is tested by reproducing
          numerical derivatives.
    */
    public class AnalyticEuropeanEngine : VanillaOption.Engine {
        private GeneralizedBlackScholesProcess process_;

        public AnalyticEuropeanEngine(GeneralizedBlackScholesProcess process) {
            process_ = process;

            process_.registerWith(update);
        }

        public override void calculate() {

            if(arguments_.exercise.type() != Exercise.Type.European)
                throw new ApplicationException("not an European option");

            StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
            if (payoff == null)
                throw new ApplicationException("non-striked payoff given");

            double variance = process_.blackVolatility().link.blackVariance(arguments_.exercise.lastDate(), payoff.strike());
            double dividendDiscount = process_.dividendYield().link.discount(arguments_.exercise.lastDate());
            double riskFreeDiscount = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate());
            double spot = process_.stateVariable().link.value();
            if (!(spot > 0.0))
                throw new ApplicationException("negative or null underlying given");
            double forwardPrice = spot * dividendDiscount / riskFreeDiscount;

            BlackCalculator black = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance), riskFreeDiscount);

            results_.value = black.value();
            results_.delta = black.delta(spot);
            results_.deltaForward = black.deltaForward();
            results_.elasticity = black.elasticity(spot);
            results_.gamma = black.gamma(spot);

            DayCounter rfdc  = process_.riskFreeRate().link.dayCounter();
            DayCounter divdc = process_.dividendYield().link.dayCounter();
            DayCounter voldc = process_.blackVolatility().link.dayCounter();
            double t = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(), arguments_.exercise.lastDate());
            results_.rho = black.rho(t);

            t = divdc.yearFraction(process_.dividendYield().link.referenceDate(), arguments_.exercise.lastDate());
            results_.dividendRho = black.dividendRho(t);

            t = voldc.yearFraction(process_.blackVolatility().link.referenceDate(), arguments_.exercise.lastDate());
            results_.vega = black.vega(t);
            try {
                results_.theta = black.theta(spot, t);
                results_.thetaPerDay = black.thetaPerDay(spot, t);
            } catch {
                results_.theta = null;
                results_.thetaPerDay = null;
            }

            results_.strikeSensitivity  = black.strikeSensitivity();
            results_.itmCashProbability = black.itmCashProbability();
        }
    }
}
