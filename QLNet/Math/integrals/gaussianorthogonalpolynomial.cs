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
    //! orthogonal polynomial for Gaussian quadratures
    /*! References:
        Gauss quadratures and orthogonal polynomials

        G.H. Gloub and J.H. Welsch: Calculation of Gauss quadrature rule.
        Math. Comput. 23 (1986), 221-230

        "Numerical Recipes in C", 2nd edition,
        Press, Teukolsky, Vetterling, Flannery,

        The polynomials are defined by the three-term recurrence relation
        \f[
        P_{k+1}(x)=(x-\alpha_k) P_k(x) - \beta_k P_{k-1}(x)
        \f]
        and
        \f[
        \mu_0 = \int{w(x)dx}
        \f]
    */
    public abstract class GaussianOrthogonalPolynomial {
        public abstract double mu_0();
        public abstract double alpha(int i);
        public abstract double beta(int i);
        public abstract double w(double x);

        public double value(int n, double x) {
            if (n > 1) {
                return  (x-alpha(n-1)) * value(n-1, x)
                           - beta(n-1) * value(n-2, x);
            }
            else if (n == 1) {
                return x-alpha(0);
            }
            return 1;
        }

        public double weightedValue(int n, double x) {
            return Math.Sqrt(w(x))*value(n, x);
        }
    }

    //! Gauss-Laguerre polynomial
    public class GaussLaguerrePolynomial : GaussianOrthogonalPolynomial {
        private double s_;

        public GaussLaguerrePolynomial() : this(0.0) { }
        public GaussLaguerrePolynomial(double s) {
            s_ = s;
            if (!(s > -1.0))
                throw new ApplicationException("s must be bigger than -1");
        }

        public override double mu_0() { return Math.Exp(GammaFunction.logValue(s_+1)); }
        public override double alpha(int i) { return 2 * i + 1 + s_; }
        public override double beta(int i) { return i * (i + s_); }
        public override double w(double x) { return Math.Pow(x, s_) * Math.Exp(-x); }
    }

    //! Gauss-Hermite polynomial
    public class GaussHermitePolynomial : GaussianOrthogonalPolynomial {
        private double mu_;

        public GaussHermitePolynomial() : this(0.0) { }
        public GaussHermitePolynomial(double mu) {
            mu_ = mu;
            if (!(mu > -0.5))
                throw new ApplicationException("mu must be bigger than -0.5");
        }

        public override double mu_0() { return Math.Exp(GammaFunction.logValue(mu_+0.5)); }
        public override double alpha(int i) { return 0.0; }
        public override double beta(int i) { return (i % 2 != 0) ? i / 2.0 + mu_ : i / 2.0; }
        public override double w(double x) { return Math.Pow(Math.Abs(x), 2*mu_)*Math.Exp(-x*x); }
    }

    //! Gauss-Jacobi polynomial
    public class GaussJacobiPolynomial : GaussianOrthogonalPolynomial {
        private double alpha_;
        private double beta_;

        public GaussJacobiPolynomial(double alpha, double beta) {
            alpha_ = alpha;
            beta_  = beta;

            if (!(alpha_+beta_ > -2.0))
                throw new ApplicationException("alpha+beta must be bigger than -2");
            if (!(alpha_       > -1.0))
                throw new ApplicationException("alpha must be bigger than -1");
            if (!(beta_        > -1.0))
                throw new ApplicationException("beta  must be bigger than -1");
        }

        public override double mu_0() {
            return Math.Pow(2.0, alpha_+beta_+1)
                * Math.Exp( GammaFunction.logValue(alpha_+1)
                            +GammaFunction.logValue(beta_ +1)
                            -GammaFunction.logValue(alpha_+beta_+2));
        }
        public override double alpha(int i) {
            double num = beta_*beta_ - alpha_*alpha_;
            double denom = (2.0*i+alpha_+beta_)*(2.0*i+alpha_+beta_+2);

            if (denom == 0) {
                if (num != 0) {
                    throw new ApplicationException("can't compute a_k for jacobi integration\n");
                }
                else {
                    // l'Hospital
                    num  = 2*beta_;
                    denom= 2*(2.0*i+alpha_+beta_+1);

                    if(denom != 0)
                        throw new ApplicationException("can't compute a_k for jacobi integration\n");
                }
            }

            return num / denom;
        }
        public override double beta(int i) {
            double num = 4.0*i*(i+alpha_)*(i+beta_)*(i+alpha_+beta_);
            double denom = (2.0*i+alpha_+beta_)*(2.0*i+alpha_+beta_)
                       * ((2.0*i+alpha_+beta_)*(2.0*i+alpha_+beta_)-1);

            if (denom == 0) {
                if (num != 0) {
                    throw new ApplicationException("can't compute b_k for jacobi integration\n");
                } else {
                    // l'Hospital
                    num  = 4.0*i*(i+beta_)* (2.0*i+2*alpha_+beta_);
                    denom= 2.0*(2.0*i+alpha_+beta_);
                    denom*=denom-1;
                    if(denom != 0)
                        throw new ApplicationException("can't compute b_k for jacobi integration\n");
                }
            }
            return num / denom;
        }
        public override double w(double x) {
            return Math.Pow(1 - x, alpha_) * Math.Pow(1 + x, beta_);
        }
    }

    //! Gauss-Legendre polynomial
    public class GaussLegendrePolynomial : GaussJacobiPolynomial {
        public GaussLegendrePolynomial() : base(0.0, 0.0) { }
    }

        //! Gauss-Chebyshev polynomial
    public class GaussChebyshevPolynomial : GaussJacobiPolynomial {
        public GaussChebyshevPolynomial() : base(-0.5, -0.5) { }
    }

    //! Gauss-Chebyshev polynomial (second kind)
    public class GaussChebyshev2ndPolynomial : GaussJacobiPolynomial {
        public GaussChebyshev2ndPolynomial() : base(0.5, 0.5) { }
    }

    //! Gauss-Gegenbauer polynomial
    public class GaussGegenbauerPolynomial : GaussJacobiPolynomial {
        public GaussGegenbauerPolynomial(double lambda) : base(lambda - 0.5, lambda - 0.5) { }
    }

    //! Gauss hyperbolic polynomial
    public class GaussHyperbolicPolynomial : GaussianOrthogonalPolynomial {
        public override double mu_0() { return Const.M_PI; }
        public override double alpha(int i) { return 0.0; }
        public override double beta(int i) { return i != 0 ? Const.M_PI_2 * Const.M_PI_2 * i * i : Const.M_PI; }
        public override double w(double x) { return 1/Math.Cosh(x); }
    }
}
