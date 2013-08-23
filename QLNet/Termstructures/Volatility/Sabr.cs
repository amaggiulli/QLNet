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
    public partial class Utils {
        public static double unsafeSabrVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
                                           double nu, double rho) {
            double oneMinusBeta = 1.0-beta;
            double A = Math.Pow(forward*strike, oneMinusBeta);
            double sqrtA= Math.Sqrt(A);
            double logM;

            if (!Utils.close(forward, strike))
                logM = Math.Log(forward/strike);
            else {
                double epsilon = (forward-strike)/strike;
                logM = epsilon - .5 * epsilon * epsilon ;
            }
            double z = (nu/alpha)*sqrtA*logM;
            double B = 1.0-2.0*rho*z+z*z;
            double C = oneMinusBeta*oneMinusBeta*logM*logM;
            double tmp = (Math.Sqrt(B)+z-rho)/(1.0-rho);
            double xx = Math.Log(tmp);
            double D = sqrtA*(1.0+C/24.0+C*C/1920.0);
            double d = 1.0 + expiryTime *
                        (oneMinusBeta*oneMinusBeta*alpha*alpha/(24.0*A)
                                            + 0.25*rho*beta*nu*alpha/sqrtA
                                                +(2.0-3.0*rho*rho)*(nu*nu/24.0));

            double multiplier;
            // computations become precise enough if the square of z worth slightly more than the precision machine (hence the m)
            const double m = 10;

            if (Math.Abs(z * z) > Const.QL_Epsilon * m)
                multiplier = z/xx;
            else {
                alpha = (0.5-rho*rho)/(1.0-rho);
                beta = alpha - .5;
                double gamma = rho/(1-rho);
                multiplier = 1.0 - beta*z + (gamma - alpha + beta*beta*.5)*z*z;
            }
            return (alpha/D)*multiplier*d;
        }

        public static void validateSabrParameters(double alpha, double beta, double nu, double rho) {
            if (!(alpha > 0.0))
                throw new ApplicationException("alpha must be positive: " + alpha + " not allowed");
            if (!(beta >= 0.0 && beta <= 1.0))
                throw new ApplicationException("beta must be in (0.0, 1.0): " + beta + " not allowed");
            if (!(nu >= 0.0))
                throw new ApplicationException("nu must be non negative: " + nu + " not allowed");
            if (!(rho * rho < 1.0))
                throw new ApplicationException("rho square must be less than one: " + rho + " not allowed");
            }

        public static double sabrVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
                                     double nu, double rho) {
            if (!(strike>0.0))
                throw new ApplicationException("strike must be positive: " + strike + " not allowed");
            if (!(forward>0.0))
                throw new ApplicationException("at the money forward rate must be: " + forward + " not allowed");
            if (!(expiryTime>=0.0))
                throw new ApplicationException("expiry time must be non-negative: " + expiryTime + " not allowed");
            validateSabrParameters(alpha, beta, nu, rho);
            return unsafeSabrVolatility(strike, forward, expiryTime, alpha, beta, nu, rho);
        }

    }
}
