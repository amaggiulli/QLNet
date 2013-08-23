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
    //! Bjerksund and Stensland pricing engine for American options (1993)
    /*! \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in literature.
    */
    public class BjerksundStenslandApproximationEngine : VanillaOption.Engine {
        private GeneralizedBlackScholesProcess process_;

        public BjerksundStenslandApproximationEngine(GeneralizedBlackScholesProcess process) {
            process_ = process;

            process_.registerWith(update);
        }

        public override void calculate() {

            if (!(arguments_.exercise.type() == Exercise.Type.American))
                throw new ApplicationException("not an American Option");

            AmericanExercise ex = arguments_.exercise as AmericanExercise;
            if(ex == null) throw new ApplicationException("non-American exercise given");
            
            if(ex.payoffAtExpiry()) throw new ApplicationException("payoff at expiry not handled");

            PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
            if(payoff == null) throw new ApplicationException("non-plain payoff given");

            double variance = process_.blackVolatility().link.blackVariance(ex.lastDate(), payoff.strike());
            double dividendDiscount = process_.dividendYield().link.discount(ex.lastDate());
            double riskFreeDiscount = process_.riskFreeRate().link.discount(ex.lastDate());
            
            double spot = process_.stateVariable().link.value();
            if (!(spot > 0.0)) throw new ApplicationException("negative or null underlying given");
            
            double strike = payoff.strike();

            if (payoff.optionType()==Option.Type.Put) {
                // use put-call simmetry
                Utils.swap<double>(ref spot, ref strike);
                Utils.swap<double>(ref riskFreeDiscount, ref dividendDiscount);
                payoff = new PlainVanillaPayoff(Option.Type.Call, strike);
            }

            if (dividendDiscount>=1.0) {
                // early exercise is never optimal - use Black formula
                double forwardPrice = spot * dividendDiscount / riskFreeDiscount;
                BlackCalculator black = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance), riskFreeDiscount);

                results_.value        = black.value();
                results_.delta        = black.delta(spot);
                results_.deltaForward = black.deltaForward();
                results_.elasticity   = black.elasticity(spot);
                results_.gamma        = black.gamma(spot);

                DayCounter rfdc  = process_.riskFreeRate().link.dayCounter();
                DayCounter divdc = process_.dividendYield().link.dayCounter();
                DayCounter voldc = process_.blackVolatility().link.dayCounter();
                double t = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(), arguments_.exercise.lastDate());
                results_.rho = black.rho(t);

                t = divdc.yearFraction(process_.dividendYield().link.referenceDate(), arguments_.exercise.lastDate());
                results_.dividendRho = black.dividendRho(t);

                t = voldc.yearFraction(process_.blackVolatility().link.referenceDate(), arguments_.exercise.lastDate());
                results_.vega        = black.vega(t);
                results_.theta       = black.theta(spot, t);
                results_.thetaPerDay = black.thetaPerDay(spot, t);

                results_.strikeSensitivity  = black.strikeSensitivity();
                results_.itmCashProbability = black.itmCashProbability();
            } else {
                // early exercise can be optimal - use approximation
                results_.value = americanCallApproximation(spot, strike, riskFreeDiscount, dividendDiscount, variance);
            }
        }


        // helper functions
        private CumulativeNormalDistribution cumNormalDist = new CumulativeNormalDistribution();

        double phi(double S, double gamma, double H, double I, double rT, double bT, double variance) {

            double lambda = (-rT + gamma * bT + 0.5 * gamma * (gamma - 1.0) * variance);
            double d = -(Math.Log(S / H) + (bT + (gamma - 0.5) * variance) ) / Math.Sqrt(variance);
            double kappa = 2.0 * bT / variance + (2.0 * gamma - 1.0);
            return Math.Exp(lambda) * Math.Pow(S, gamma) * (cumNormalDist.value(d) - Math.Pow((I / S), kappa) *
                cumNormalDist.value(d - 2.0 * Math.Log(I/S) / Math.Sqrt(variance)));
        }


        double americanCallApproximation(double S, double X, double rfD, double dD, double variance) {

            double bT = Math.Log(dD/rfD);
            double rT = Math.Log(1.0/rfD);

            double beta = (0.5 - bT/variance) + Math.Sqrt(Math.Pow((bT/variance - 0.5), 2.0) + 2.0 * rT/variance);
            double BInfinity = beta / (beta - 1.0) * X;
            // Real B0 = std::max(X, std::log(rfD) / std::log(dD) * X);
            double B0 = Math.Max(X, rT / (rT - bT) * X);
            double ht = -(bT + 2.0*Math.Sqrt(variance)) * B0 / (BInfinity - B0);

            // investigate what happen to I for dD->0.0
            double I = B0 + (BInfinity - B0) * (1 - Math.Exp(ht));
            if (!(I >= X))
                throw new ApplicationException("Bjerksund-Stensland approximation not applicable to this set of parameters");
            if (S >= I) {
                return S - X;
            } else {
                // investigate what happen to alpha for dD->0.0
                double alpha = (I - X) * Math.Pow(I, (-beta));
                return alpha * Math.Pow(S, beta)
                    - alpha * phi(S, beta, I, I, rT, bT, variance)
                    +         phi(S,  1.0, I, I, rT, bT, variance)
                    -         phi(S,  1.0, X, I, rT, bT, variance)
                    -    X *  phi(S,  0.0, I, I, rT, bT, variance)
                    +    X *  phi(S,  0.0, X, I, rT, bT, variance);
            }
        }

    }
}
