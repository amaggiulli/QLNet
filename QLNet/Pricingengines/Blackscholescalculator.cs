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

	//! Black-Scholes 1973 calculator class
	public class BlackScholesCalculator : BlackCalculator
	{
        protected double spot_;
        protected double growth_;

        public BlackScholesCalculator(StrikedTypePayoff payoff, double spot, double growth, double stdDev, double discount)
            : base(payoff, spot * growth / discount, stdDev, discount)
		{
			spot_ = spot;
			growth_ = growth;

            if (!(spot_ >= 0.0))
                throw new ApplicationException("positive spot value required: " + spot_ + " not allowed");

            if (!(growth_ >= 0.0))
                throw new ApplicationException("positive growth value required: " + growth_ + " not allowed");
		}

		//! Sensitivity to change in the underlying spot price. 

		public double delta()
		{
			return base.delta(spot_);
		}

//        ! Sensitivity in percent to a percent change in the
//            underlying spot price. 
		public double elasticity()
		{
			return base.elasticity(spot_);
		}

//        ! Second order derivative with respect to change in the
//            underlying spot price. 
		public double gamma()
		{
			return base.gamma(spot_);
		}
		//! Sensitivity to time to maturity. 
		public double theta(double maturity)
		{
			return base.theta(spot_, maturity);
		}
//        ! Sensitivity to time to maturity per day
//            (assuming 365 day in a year). 
		public double thetaPerDay(double maturity)
		{
			return base.thetaPerDay(spot_, maturity);
		}
	}
}
