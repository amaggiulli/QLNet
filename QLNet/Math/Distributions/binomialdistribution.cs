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
    //! Binomial probability distribution function
    /*! formula here ...
        Given an integer k it returns its probability in a Binomial
        distribution with parameters p and n.
    */
    public class BinomialDistribution {
        private int n_;
        private double logP_, logOneMinusP_;

        public BinomialDistribution(double p, int n) {
            n_ = n;

            if (p==0.0) {
                logOneMinusP_ = 0.0;
            } else if (p==1.0) {
                logP_ = 0.0;
            } else {
                if (!(p>0)) throw new ApplicationException("negative p not allowed");
                if (!(p < 1.0)) throw new ApplicationException("p>1.0 not allowed");

                logP_ = Math.Log(p);
                logOneMinusP_ = Math.Log(1.0 - p);
            }
        }

        // function
        public double value (int k) {
            if (k > n_) return 0.0;

            // p==1.0
            if (logP_==0.0)
                return (k==n_ ? 1.0 : 0.0);
            // p==0.0
            else if (logOneMinusP_==0.0)
                return (k==0 ? 1.0 : 0.0);
            else
                return Math.Exp(Utils.binomialCoefficientLn(n_, k) + k * logP_ + (n_ - k) * logOneMinusP_);
        }
    }

    //! Cumulative binomial distribution function
    /*! Given an integer k it provides the cumulative probability
        of observing kk<=k:
        formula here ...
    */
    public class CumulativeBinomialDistribution {
        private int n_;
        private double p_;

        public CumulativeBinomialDistribution(double p, int n) {
            n_ = n;
            p_ = p;

            if (!(p >= 0)) throw new ApplicationException("negative p not allowed");
            if (!(p <= 1.0)) throw new ApplicationException("p>1.0 not allowed");
        }
        
        // function
        public double value(long k) {
            if (k >= n_)
                return 1.0;
            else
                return 1.0 - Utils.incompleteBetaFunction(k+1, n_-k, p_);
        }
    }

    public static partial class Utils {
        /*! Given an odd integer n and a real number z it returns p such that:
        1 - CumulativeBinomialDistribution((n-1)/2, n, p) =
                               CumulativeNormalDistribution(z)

        \pre n must be odd
        */
        public static double PeizerPrattMethod2Inversion(double z, int n) {

            if (!(n%2==1)) throw new ApplicationException("n must be an odd number: " + n + " not allowed");

            double result = (z/(n+1.0/3.0+0.1/(n+1.0)));
            result *= result;
            result = Math.Exp(-result*(n+1.0/6.0));
            result = 0.5 + (z>0 ? 1 : -1) * Math.Sqrt((0.25 * (1.0-result)));
            return result;
        }

        public static double binomialCoefficientLn(int n, int k) {
            if (!(n>=k)) throw new ApplicationException("n<k not allowed");

            return Factorial.ln(n)-Factorial.ln(k)-Factorial.ln(n-k);
        }

        public static double binomialCoefficient(int n, int k) {
            return Math.Floor(0.5+Math.Exp(Utils.binomialCoefficientLn(n, k)));
        }
    }
}
