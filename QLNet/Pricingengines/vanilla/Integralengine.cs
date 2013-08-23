/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

    public class Integrand
    {
        private Payoff payoff_;
        private double s0_;
        private double drift_;
        private double variance_;

        public Integrand(Payoff payoff, double s0, double drift, double variance)
        {
            payoff_ = payoff;
            s0_ = s0;
            drift_ = drift;
            variance_ = variance;
        }
        public double value(double x)
        {
            double temp = s0_ * Math.Exp(x);
            double result = payoff_.value(temp);
            return result * Math.Exp(-(x - drift_) * (x - drift_) / (2.0 * variance_));
        }
    }

	//! Pricing engine for European vanilla options using integral approach
//    ! \todo define tolerance for calculate()
//
//        \ingroup vanillaengines
//    
    public class IntegralEngine : VanillaOption.Engine
	{
        private GeneralizedBlackScholesProcess process_;

		public IntegralEngine(GeneralizedBlackScholesProcess process)
		{
			process_ = process;

            process_.registerWith(update);
		}

		public override void calculate()
		{
            if (!(arguments_.exercise.type() == Exercise.Type.European))
                throw new ApplicationException("not an European Option");

            StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;

            if (payoff == null)
                throw new ApplicationException("not an European Option");

			double variance = process_.blackVolatility().link.blackVariance(arguments_.exercise.lastDate(), payoff.strike());

            double dividendDiscount = process_.dividendYield().link.discount(arguments_.exercise.lastDate());
            double riskFreeDiscount = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate());
			double drift = Math.Log(dividendDiscount/riskFreeDiscount)-0.5 *variance;

            Integrand f = new Integrand(arguments_.payoff, process_.stateVariable().link.value(), drift, variance);
			SegmentIntegral integrator = new SegmentIntegral(5000);
	
			double infinity = 10.0 *Math.Sqrt(variance);
            results_.value = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate()) / 
                             Math.Sqrt(2.0 * Math.PI * variance) * integrator.value(f.value, drift - infinity, drift + infinity);
		}
	}

}
