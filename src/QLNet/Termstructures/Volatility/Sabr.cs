/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet {
    public enum SabrApproximationModel { Obloj2008 = 1, Hagan2002 = 0 };

    public partial class Utils {

        public static double unsafeSabrNormalVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
                                           double nu, double rho)
        {
            double oneMinusBeta = 1.0 - beta;
            double Fmid = forward * strike < 0.0 ? (forward + strike) * 0.5 : Math.Sqrt(forward * strike);
            double gamma1 = beta / Fmid;
            double gamma2 = -beta * oneMinusBeta / (Fmid * Fmid);
            double zeta = alpha / (nu * oneMinusBeta) * (Math.Pow(forward, oneMinusBeta) - Math.Pow(strike, oneMinusBeta));
            double D = Math.Log((Math.Sqrt(1.0 - 2.0 * rho * zeta + zeta * zeta) + zeta - rho) / (1.0 - rho));
            double epsilon = alpha * alpha * expiryTime;
            double M = forward - strike;
            double a = nu * Math.Pow(Fmid, beta) / alpha;
            double b = Math.Pow(a, 2.0);
            double d = 1.0 + ((2.0 * gamma2 - gamma1 * gamma1) / 24.0 * b
                                                            + rho * gamma1 / 4.0 * a
                                                            + (2.0 - 3.0 * rho * rho) / 24.0) * epsilon;

            return alpha * M / D * d;
        }

        public static double unsafeSabrVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
                                           double nu, double rho, SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002)
        {
            if (approximationModel == SabrApproximationModel.Hagan2002)
            {
                double oneMinusBeta = 1.0 - beta;
                double A = Math.Pow(forward * strike, oneMinusBeta);
                double sqrtA = Math.Sqrt(A);
                double logM;

                if (!close(forward, strike))
                    logM = Math.Log(forward / strike);
                else
                {
                    double epsilon = (forward - strike) / strike;
                    logM = epsilon - .5 * epsilon * epsilon;
                }
                double z = (nu / alpha) * sqrtA * logM;
                double B = 1.0 - 2.0 * rho * z + z * z;
                double C = oneMinusBeta * oneMinusBeta * logM * logM;
                double tmp = (Math.Sqrt(B) + z - rho) / (1.0 - rho);
                double xx = Math.Log(tmp);
                double D = sqrtA * (1.0 + C / 24.0 + C * C / 1920.0);
                double d = 1.0 + expiryTime *
                            (oneMinusBeta * oneMinusBeta * alpha * alpha / (24.0 * A)
                                                + 0.25 * rho * beta * nu * alpha / sqrtA
                                                    + (2.0 - 3.0 * rho * rho) * (nu * nu / 24.0));

                double multiplier;
                // computations become precise enough if the square of z worth slightly more than the precision machine (hence the m)
                const double m = 10;

                if (Math.Abs(z * z) > Const.QL_EPSILON * m)
                    multiplier = z / xx;
                else
                {
                    multiplier = 1.0 - 0.5 * rho * z - (3.0 * rho * rho - 2.0) * z * z / 12.0;
                }
                return (alpha / D) * multiplier * d;
            }
            else if (approximationModel == SabrApproximationModel.Obloj2008)
            {
                double oneMinusBeta = 1.0 - beta;
                double Fmid = Math.Sqrt(forward * strike);
                double gamma1 = beta / Fmid;
                double gamma2 = -beta * oneMinusBeta / (Fmid * Fmid);
                double zeta = alpha / (nu * oneMinusBeta) * (Math.Pow(forward, oneMinusBeta) - Math.Pow(strike, oneMinusBeta));
                double D = Math.Log((Math.Sqrt(1.0 - 2.0 * rho * zeta + zeta * zeta) + zeta - rho) / (1.0 - rho));
                double epsilon = alpha * alpha * expiryTime;

                double logM;

                if (!close(forward, strike))
                    logM = Math.Log(forward / strike);
                else
                {
                    double eps = (forward - strike) / strike;
                    logM = eps - .5 * eps * eps;
                }

                double a = nu * Math.Pow(Fmid, beta) / alpha;
                double b = Math.Pow(a, 2.0);
                double d = 1.0 + ((2.0 * gamma2 - gamma1 * gamma1 + 1 / (Fmid * Fmid)) / 24.0 * b
                                                                + rho * gamma1 / 4.0 * a
                                                                + (2.0 - 3.0 * rho * rho) / 24.0) * epsilon;

                return alpha * logM / D * d;
            }
            else
            {
                QL_FAIL("Unknown approximation model.");
                return 0.0;
            }
        }

        public static double unsafeShiftedSabrVolatility(double strike,
                                                  double forward,
                                                  double expiryTime,
                                                  double alpha,
                                                  double beta,
                                                  double nu,
                                                  double rho,
                                                  double shift, 
                                                  SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002)
        {

            return unsafeSabrVolatility(strike + shift, forward + shift, expiryTime,
                                        alpha, beta, nu, rho, approximationModel);
        }

        public static void validateSabrParameters(double alpha, double beta, double nu, double rho)
        {
            QL_REQUIRE(alpha > 0.0,()=> "alpha must be positive: " + alpha + " not allowed");
            QL_REQUIRE(beta >= 0.0 && beta <= 1.0,()=> "beta must be in (0.0, 1.0): " + beta + " not allowed");
            QL_REQUIRE(nu >= 0.0,()=> "nu must be non negative: " + nu + " not allowed");
            QL_REQUIRE(rho * rho < 1.0,()=> "rho square must be less than one: " + rho + " not allowed");
        }

        public static double sabrVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
                                     double nu, double rho, SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002)
        {
            QL_REQUIRE(strike>0.0,()=> "strike must be positive: " + strike + " not allowed");
            QL_REQUIRE(forward>0.0,()=> "at the money forward rate must be: " + forward + " not allowed");
            QL_REQUIRE(expiryTime>=0.0,()=> "expiry time must be non-negative: " + expiryTime + " not allowed");
            validateSabrParameters(alpha, beta, nu, rho);
            return unsafeSabrVolatility(strike, forward, expiryTime, alpha, beta, nu, rho, approximationModel);
        }

        public static double shiftedSabrVolatility(double strike,
                                     double forward,
                                     double expiryTime,
                                     double alpha,
                                     double beta,
                                     double nu,
                                     double rho,
                                     double shift,
                                     SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002) 
        {
            QL_REQUIRE(strike + shift > 0.0, () => "strike+shift must be positive: "
                       + strike + "+"  + shift + " not allowed");
            QL_REQUIRE(forward + shift > 0.0, () => "at the money forward rate + shift must be "
                       + "positive: " + forward + " " + shift + " not allowed");
            QL_REQUIRE(expiryTime >= 0.0, () => "expiry time must be non-negative: "
                                       + expiryTime + " not allowed");
            validateSabrParameters(alpha, beta, nu, rho);
            return unsafeShiftedSabrVolatility(strike, forward, expiryTime,
                                                 alpha, beta, nu, rho, shift, approximationModel);
        }

        public static double shiftedSabrNormalVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
                                     double nu, double rho, double shift = 0.0)
        {
            QL_REQUIRE(strike + shift > 0.0, () => "strike+shift must be positive: "
                       + strike + "+" + shift + " not allowed");
            QL_REQUIRE(forward + shift > 0.0, () => "at the money forward rate + shift must be "
                       + "positive: " + forward + " " + shift + " not allowed");
            QL_REQUIRE(expiryTime >= 0.0, () => "expiry time must be non-negative: " + expiryTime + " not allowed");
            validateSabrParameters(alpha, beta, nu, rho);
            return unsafeSabrNormalVolatility(strike + shift, forward + shift, expiryTime, alpha, beta, nu, rho);
        }


       public static double sabrNormalVolatility(double strike, double forward, double expiryTime, double alpha, double beta,
          double nu, double rho)
       {
          QL_REQUIRE(expiryTime >= 0.0, () => "expiry time must be non-negative: " + expiryTime + " not allowed");
          validateSabrParameters(alpha, beta, nu, rho);
          return unsafeSabrNormalVolatility(strike, forward, expiryTime, alpha, beta, nu, rho);
       }

   }
}
