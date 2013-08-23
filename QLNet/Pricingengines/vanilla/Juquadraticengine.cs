/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
  
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

	//! Pricing engine for American options with Ju quadratic approximation
//    ! Reference:
//        An Approximate Formula for Pricing American Options,
//        Journal of Derivatives Winter 1999,
//        Ju, N.
//
//        \warning Barone-Adesi-Whaley critical commodity price
//                 calculation is used, it has not been modified to see
//                 whether the method of Ju is faster. Ju does not say
//                 how he solves the equation for the critical stock
//                 price, e.g. Newton method. He just gives the
//                 solution.  The method of BAW gives answers to the
//                 same accuracy as in Ju (1999).
//
//        \ingroup vanillaengines
//
//        \test the correctness of the returned value is tested by
//              reproducing results available in literature.
//    
	public class JuQuadraticApproximationEngine : VanillaOption.Engine
	{

	//     An Approximate Formula for Pricing American Options
	//        Journal of Derivatives Winter 1999
	//        Ju, N.
	//    

        private GeneralizedBlackScholesProcess process_;
	
		public JuQuadraticApproximationEngine(GeneralizedBlackScholesProcess process)
		{
			process_ = process;
			//registerWith(process_);
            process_.registerWith(update);
		}

        public override void calculate()
		{
            if (!(arguments_.exercise.type() == Exercise.Type.American))
                throw new ApplicationException("not an American Option");
	
			AmericanExercise ex = arguments_.exercise as AmericanExercise;

            if (ex == null)
                throw new ApplicationException("non-American exercise given");

            if (ex.payoffAtExpiry())
                throw new ApplicationException("payoff at expiry not handled");
	
			StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
            if (payoff == null)
                throw new ApplicationException("non-striked payoff given");

			double variance = process_.blackVolatility().link.blackVariance(ex.lastDate(), payoff.strike());
            double dividendDiscount = process_.dividendYield().link.discount(ex.lastDate());
            double riskFreeDiscount = process_.riskFreeRate().link.discount(ex.lastDate());
            double spot = process_.stateVariable().link.value();

            if (!(spot > 0.0))
                throw new ApplicationException("negative or null underlying given");

			double forwardPrice = spot * dividendDiscount / riskFreeDiscount;
			BlackCalculator black = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance), riskFreeDiscount);
	
			if (dividendDiscount>=1.0 && payoff.optionType()==Option.Type.Call)
			{
				// early exercise never optimal
				results_.value = black.value();
				results_.delta = black.delta(spot);
				results_.deltaForward = black.deltaForward();
				results_.elasticity = black.elasticity(spot);
				results_.gamma = black.gamma(spot);

                DayCounter rfdc = process_.riskFreeRate().link.dayCounter();
                DayCounter divdc = process_.dividendYield().link.dayCounter();
                DayCounter voldc = process_.blackVolatility().link.dayCounter();
                double t = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(), arguments_.exercise.lastDate());
				results_.rho = black.rho(t);

                t = divdc.yearFraction(process_.dividendYield().link.referenceDate(), arguments_.exercise.lastDate());
				results_.dividendRho = black.dividendRho(t);

                t = voldc.yearFraction(process_.blackVolatility().link.referenceDate(), arguments_.exercise.lastDate());
				results_.vega = black.vega(t);
				results_.theta = black.theta(spot, t);
				results_.thetaPerDay = black.thetaPerDay(spot, t);
	
				results_.strikeSensitivity = black.strikeSensitivity();
				results_.itmCashProbability = black.itmCashProbability();
			}
			else
			{
				// early exercise can be optimal
				CumulativeNormalDistribution cumNormalDist = new CumulativeNormalDistribution();
				NormalDistribution normalDist = new NormalDistribution();
	
				double tolerance = 1e-6;
				double Sk = BaroneAdesiWhaleyApproximationEngine.criticalPrice(payoff, riskFreeDiscount, dividendDiscount, variance, tolerance);
	
				double forwardSk = Sk * dividendDiscount / riskFreeDiscount;
	
				double alpha = -2.0 *Math.Log(riskFreeDiscount)/(variance);
				double beta = 2.0 *Math.Log(dividendDiscount/riskFreeDiscount)/ (variance);
				double h = 1 - riskFreeDiscount;
				double phi;
				switch (payoff.optionType())
				{
					case Option.Type.Call:
						phi = 1;
						break;
					case Option.Type.Put:
						phi = -1;
						break;
					default:
                        throw new ArgumentException("invalid option type");
				}
				//it can throw: to be fixed
				// FLOATING_POINT_EXCEPTION
				double temp_root = Math.Sqrt ((beta-1)*(beta-1) + (4 *alpha)/h);
				double lambda = (-(beta-1) + phi * temp_root) / 2;
				double lambda_prime = - phi * alpha / (h *h * temp_root);

                double black_Sk = Utils.blackFormula(payoff.optionType(), payoff.strike(), forwardSk, Math.Sqrt(variance)) * riskFreeDiscount;
				double hA = phi * (Sk - payoff.strike()) - black_Sk;
	
				double d1_Sk = (Math.Log(forwardSk/payoff.strike()) + 0.5 *variance) /Math.Sqrt(variance);
				double d2_Sk = d1_Sk - Math.Sqrt(variance);
                double part1 = forwardSk * normalDist.value(d1_Sk) / (alpha * Math.Sqrt(variance));
				double part2 = - phi * forwardSk * cumNormalDist.value(phi * d1_Sk) * Math.Log(dividendDiscount) / Math.Log(riskFreeDiscount);
				double part3 = + phi * payoff.strike() * cumNormalDist.value(phi * d2_Sk);
				double V_E_h = part1 + part2 + part3;
	
				double b = (1-h) * alpha * lambda_prime / (2*(2 *lambda + beta - 1));
				double c = - ((1 - h) * alpha / (2 * lambda + beta - 1)) * (V_E_h / (hA) + 1 / h + lambda_prime / (2 *lambda + beta - 1));
				double temp_spot_ratio = Math.Log(spot / Sk);
				double chi = temp_spot_ratio * (b * temp_spot_ratio + c);
	
				if (phi*(Sk-spot) > 0)
				{
					results_.value = black.value() + hA * Math.Pow((spot/Sk), lambda) / (1 - chi);
				}
				else
				{
					results_.value = phi * (spot - payoff.strike());
				}
	
				double temp_chi_prime = (2 * b / spot) * Math.Log(spot/Sk);
				double chi_prime = temp_chi_prime + c / spot;
				double chi_double_prime = 2 *b/(spot *spot) - temp_chi_prime / spot - c / (spot *spot);
				results_.delta = phi * dividendDiscount * cumNormalDist.value (phi * d1_Sk) + (lambda / (spot * (1 - chi)) + chi_prime / ((1 - chi)*(1 - chi))) * (phi * (Sk - payoff.strike()) - black_Sk) * Math.Pow((spot/Sk), lambda);
	
				results_.gamma = phi * dividendDiscount * normalDist.value (phi *d1_Sk) / (spot * Math.Sqrt(variance)) + (2 * lambda * chi_prime / (spot * (1 - chi) * (1 - chi)) + 2 * chi_prime * chi_prime / ((1 - chi) * (1 - chi) * (1 - chi)) + chi_double_prime / ((1 - chi) * (1 - chi)) + lambda * (1 - lambda) / (spot * spot * (1 - chi))) * (phi * (Sk - payoff.strike()) - black_Sk) * Math.Pow((spot/Sk), lambda);
	
			} // end of "early exercise can be optimal"
		}
	}

}
