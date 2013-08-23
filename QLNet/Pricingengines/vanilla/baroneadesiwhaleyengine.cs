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
    //! Barone-Adesi and Whaley pricing engine for American options (1987)
    /*! \ingroup vanillaengines

        \test the correctness of the returned value is tested by
              reproducing results available in literature.
    */
    public class BaroneAdesiWhaleyApproximationEngine : VanillaOption.Engine {
        private GeneralizedBlackScholesProcess process_;

        public BaroneAdesiWhaleyApproximationEngine(GeneralizedBlackScholesProcess process) {
            process_ = process;

            process_.registerWith(update);
        }

        // critical commodity price
        //public static double criticalPrice(StrikedTypePayoff payoff, double riskFreeDiscount, double dividendDiscount,
        //                                   double variance, double tolerance = 1e-6);
        public static double criticalPrice(StrikedTypePayoff payoff, double riskFreeDiscount, double dividendDiscount,
                                           double variance, double tolerance) {

            // Calculation of seed value, Si
            double n= 2.0*Math.Log(dividendDiscount/riskFreeDiscount)/(variance);
            double m=-2.0*Math.Log(riskFreeDiscount)/(variance);
            double bT = Math.Log(dividendDiscount/riskFreeDiscount);

            double qu, Su, h, Si;
            switch (payoff.optionType()) {
                case Option.Type.Call:
                    qu = (-(n-1.0) + Math.Sqrt(((n-1.0)*(n-1.0)) + 4.0*m))/2.0;
                    Su = payoff.strike() / (1.0 - 1.0/qu);
                    h = -(bT + 2.0*Math.Sqrt(variance)) * payoff.strike() /
                        (Su - payoff.strike());
                    Si = payoff.strike() + (Su - payoff.strike()) *
                        (1.0 - Math.Exp(h));
                    break;
                case Option.Type.Put:
                    qu = (-(n-1.0) - Math.Sqrt(((n-1.0)*(n-1.0)) + 4.0*m))/2.0;
                    Su = payoff.strike() / (1.0 - 1.0/qu);
                    h = (bT - 2.0*Math.Sqrt(variance)) * payoff.strike() /
                        (payoff.strike() - Su);
                    Si = Su + (payoff.strike() - Su) * Math.Exp(h);
                    break;
                default:
                    throw new ArgumentException("unknown option type");
            }


            // Newton Raphson algorithm for finding critical price Si
            double Q, LHS, RHS, bi;
            double forwardSi = Si * dividendDiscount / riskFreeDiscount;
            double d1 = (Math.Log(forwardSi/payoff.strike()) + 0.5*variance) /
                Math.Sqrt(variance);
            CumulativeNormalDistribution cumNormalDist = new CumulativeNormalDistribution();
            double K = (riskFreeDiscount!=1.0 ? -2.0*Math.Log(riskFreeDiscount)/
                (variance*(1.0-riskFreeDiscount)) : 0.0);
            double temp = Utils.blackFormula(payoff.optionType(), payoff.strike(),
                    forwardSi, Math.Sqrt(variance))*riskFreeDiscount;
            switch (payoff.optionType()) {
                case Option.Type.Call:
                    Q = (-(n-1.0) + Math.Sqrt(((n-1.0)*(n-1.0)) + 4 * K)) / 2;
                    LHS = Si - payoff.strike();
                    RHS = temp + (1 - dividendDiscount * cumNormalDist.value(d1)) * Si / Q;
                    bi = dividendDiscount * cumNormalDist.value(d1) * (1 - 1 / Q) +
                        (1 - dividendDiscount *
                         cumNormalDist.derivative(d1) / Math.Sqrt(variance)) / Q;
                    while (Math.Abs(LHS - RHS)/payoff.strike() > tolerance) {
                        Si = (payoff.strike() + RHS - bi * Si) / (1 - bi);
                        forwardSi = Si * dividendDiscount / riskFreeDiscount;
                        d1 = (Math.Log(forwardSi/payoff.strike())+0.5*variance)
                            /Math.Sqrt(variance);
                        LHS = Si - payoff.strike();
                        double temp2 = Utils.blackFormula(payoff.optionType(), payoff.strike(),
                            forwardSi, Math.Sqrt(variance))*riskFreeDiscount;
                        RHS = temp2 + (1 - dividendDiscount * cumNormalDist.value(d1)) * Si / Q;
                        bi = dividendDiscount * cumNormalDist.value(d1) * (1 - 1 / Q)
                            + (1 - dividendDiscount *
                               cumNormalDist.derivative(d1) / Math.Sqrt(variance))
                            / Q;
                    }
                    break;
                case Option.Type.Put:
                    Q = (-(n-1.0) - Math.Sqrt(((n-1.0)*(n-1.0)) + 4 * K)) / 2;
                    LHS = payoff.strike() - Si;
                    RHS = temp - (1 - dividendDiscount * cumNormalDist.value(-d1)) * Si / Q;
                    bi = -dividendDiscount * cumNormalDist.value(-d1) * (1 - 1 / Q)
                        - (1 + dividendDiscount * cumNormalDist.derivative(-d1)
                           / Math.Sqrt(variance)) / Q;
                    while (Math.Abs(LHS - RHS)/payoff.strike() > tolerance) {
                        Si = (payoff.strike() - RHS + bi * Si) / (1 + bi);
                        forwardSi = Si * dividendDiscount / riskFreeDiscount;
                        d1 = (Math.Log(forwardSi/payoff.strike())+0.5*variance)
                            /Math.Sqrt(variance);
                        LHS = payoff.strike() - Si;
                        double temp2 = Utils.blackFormula(payoff.optionType(), payoff.strike(),
                            forwardSi, Math.Sqrt(variance))*riskFreeDiscount;
                        RHS = temp2 - (1 - dividendDiscount * cumNormalDist.value(-d1)) * Si / Q;
                        bi = -dividendDiscount * cumNormalDist.value(-d1) * (1 - 1 / Q)
                            - (1 + dividendDiscount * cumNormalDist.derivative(-d1)
                               / Math.Sqrt(variance)) / Q;
                    }
                    break;
                default:
                    throw new ArgumentException("unknown option type");
            }

            return Si;
        }

        public override void calculate() {

            if (!(arguments_.exercise.type() == Exercise.Type.American))
                throw new ApplicationException("not an American Option");

            AmericanExercise ex = arguments_.exercise as AmericanExercise;
            if (ex == null) throw new ApplicationException("non-American exercise given");

            if(ex.payoffAtExpiry()) throw new ApplicationException("payoff at expiry not handled");

            StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
            if (payoff == null) throw new ApplicationException("non-striked payoff given");

            double variance = process_.blackVolatility().link.blackVariance(ex.lastDate(), payoff.strike());
            double dividendDiscount = process_.dividendYield().link.discount(ex.lastDate());
            double riskFreeDiscount = process_.riskFreeRate().link.discount(ex.lastDate());
            double spot = process_.stateVariable().link.value();
            if (!(spot > 0.0)) throw new ApplicationException("negative or null underlying given");
            double forwardPrice = spot * dividendDiscount / riskFreeDiscount;
            BlackCalculator black = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance), riskFreeDiscount);

            if (dividendDiscount>=1.0 && payoff.optionType()==Option.Type.Call) {
                // early exercise never optimal
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
                // early exercise can be optimal
                CumulativeNormalDistribution cumNormalDist = new CumulativeNormalDistribution();
                double tolerance = 1e-6;
                double Sk = criticalPrice(payoff, riskFreeDiscount,
                    dividendDiscount, variance, tolerance);
                double forwardSk = Sk * dividendDiscount / riskFreeDiscount;
                double d1 = (Math.Log(forwardSk/payoff.strike()) + 0.5*variance)
                    /Math.Sqrt(variance);
                double n = 2.0*Math.Log(dividendDiscount/riskFreeDiscount)/variance;
                double K = -2.0*Math.Log(riskFreeDiscount)/
                    (variance*(1.0-riskFreeDiscount));
                double Q, a;
                switch (payoff.optionType()) {
                    case Option.Type.Call:
                        Q = (-(n-1.0) + Math.Sqrt(((n-1.0)*(n-1.0))+4.0*K))/2.0;
                        a =  (Sk/Q) * (1.0 - dividendDiscount * cumNormalDist.value(d1));
                        if (spot<Sk) {
                            results_.value = black.value() +
                                a * Math.Pow((spot/Sk), Q);
                        } else {
                            results_.value = spot - payoff.strike();
                        }
                        break;
                    case Option.Type.Put:
                        Q = (-(n-1.0) - Math.Sqrt(((n-1.0)*(n-1.0))+4.0*K))/2.0;
                        a = -(Sk/Q) *
                            (1.0 - dividendDiscount * cumNormalDist.value(-d1));
                        if (spot>Sk) {
                            results_.value = black.value() +
                                a * Math.Pow((spot / Sk), Q);
                        } else {
                            results_.value = payoff.strike() - spot;
                        }
                        break;
                    default:
                      throw new ApplicationException("unknown option type");
                }
            } // end of "early exercise can be optimal"
        }
    }
}
