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
    public static partial class Utils {
		//! default theta calculation for Black-Scholes options
		public static double blackScholesTheta(GeneralizedBlackScholesProcess p, double value, double delta, double gamma)
		{

			double u = p.stateVariable().currentLink().value();
            double r = p.riskFreeRate().currentLink().zeroRate(0.0, Compounding.Continuous).rate();
            double q = p.dividendYield().currentLink().zeroRate(0.0, Compounding.Continuous).rate();
            double v = p.localVolatility().currentLink().localVol(0.0, u, false);

			return r *value -(r-q)*u *delta - 0.5 *v *v *u *u *gamma;
		}

		//! default theta-per-day calculation
		public static double defaultThetaPerDay(double theta)
		{
			return theta/365.0;
		}
    }
}
