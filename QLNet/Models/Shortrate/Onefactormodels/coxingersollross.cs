/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
  
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

/*! \file coxingersollross.hpp
    \brief Cox-Ingersoll-Ross model
*/

namespace QLNet {

    //! Cox-Ingersoll-Ross model class.
    /*! This class implements the Cox-Ingersoll-Ross model defined by
        \f[
            dr_t = k(\theta - r_t)dt + \sqrt{r_t}\sigma dW_t .
        \f]

        \bug this class was not tested enough to guarantee
             its functionality.

        \ingroup shortrate
    */
    public class CoxIngersollRoss : OneFactorAffineModel {

        private Parameter theta_;
        private Parameter k_;
        private Parameter sigma_;
        private Parameter r0_;

        public CoxIngersollRoss(double r0 = 0.05,
                         double theta = 0.1,
                         double k = 0.1,
                         double sigma = 0.1)
            :base(4)
        {
           theta_ = arguments_[0];
           k_=arguments_[1];
           sigma_=arguments_[2];
           r0_ = arguments_[3];
           //theta_ = new ConstantParameter(theta, new PositiveConstraint());
           //k_ = new ConstantParameter(k, new PositiveConstraint());
           //sigma_ = new ConstantParameter(sigma, new VolatilityConstraint(k,theta));
           //r0_ = new ConstantParameter(r0, new PositiveConstraint());
        }

        public override double discountBondOption(Option.Type type,
                                        double strike,
                                        double maturity,
                                        double bondMaturity)
        {
            if (!(strike > 0.0))
                throw new ApplicationException("strike must be positive");
            double discountT = discountBond(0.0, maturity, x0());
            double discountS = discountBond(0.0, bondMaturity, x0());

            if (maturity < Const.QL_Epsilon)
            {
                switch (type){
                    case Option.Type.Call:
                        return Math.Max(discountS - strike, 0.0);
                    case Option.Type.Put:
                        return Math.Max(strike - discountS, 0.0);
                    default:
                       throw new ApplicationException("unsupported option type");
                }
            }
            double sigma2 = sigma()*sigma();
            double h = Math.Sqrt(k()*k() + 2.0*sigma2);
            double b = B(maturity,bondMaturity);

            double rho = 2.0*h/(sigma2*(Math.Exp(h*maturity) - 1.0));
            double psi = (k() + h)/sigma2;

            double df = 4.0*k()*theta()/sigma2;
            double ncps = 2.0*rho*rho*x0()*Math.Exp(h*maturity)/(rho+psi+b);
            double ncpt = 2.0*rho*rho*x0()*Math.Exp(h*maturity)/(rho+psi);

            NonCentralChiSquareDistribution chis = new NonCentralChiSquareDistribution(df, ncps);
            NonCentralChiSquareDistribution chit = new NonCentralChiSquareDistribution(df, ncpt);

            double z = Math.Log(A(maturity,bondMaturity)/strike)/b;
            double call = discountS*chis.value(2.0*z*(rho+psi+b)) -
                strike*discountT*chit.value(2.0*z*(rho+psi));

            if (type == Option.Type.Call)
                return call;
            else
                return call - discountS + strike*discountT;
        }

        public override ShortRateDynamics dynamics(){
            return new Dynamics(theta(), k() , sigma(), x0());
        }

        public override Lattice tree(TimeGrid grid){
            TrinomialTree trinomial = new TrinomialTree(dynamics().process(), grid, true);
            return new ShortRateTree(trinomial, dynamics(), grid);
        }
  
        protected override double A(double t, double T){
            double sigma2 = sigma()*sigma();
            double h = Math.Sqrt(k()*k() + 2.0*sigma2);
            double numerator = 2.0*h*Math.Exp(0.5*(k()+h)*(T-t));
            double denominator = 2.0*h + (k()+h)*(Math.Exp((T-t)*h) - 1.0);
            double value = Math.Log(numerator / denominator) *
                2.0*k()*theta()/sigma2;
            return Math.Exp(value);
        }

        protected override double B(double t, double T){
            double h = Math.Sqrt(k()*k() + 2.0*sigma()*sigma());
            double temp = Math.Exp((T - t) * h) - 1.0;
            double numerator = 2.0*temp;
            double denominator = 2.0*h + (k()+h)*temp;
            double value = numerator/denominator;
            return value;
        }

        protected double theta()  { return theta_.value	(0.0); }
        protected double k()  { return k_.value	(0.0); }
        protected double sigma()  { return sigma_.value	(0.0); }
        protected double x0()  { return r0_.value	(0.0); }

        public class HelperProcess : StochasticProcess1D {
          
            private double y0_, theta_, k_, sigma_;

            public HelperProcess(double theta, double k, double sigma, double y0){
                y0_=y0; 
                theta_=theta;
                k_ = k;
                sigma_=sigma;
            }

            public override double x0() {return y0_;}

            public override double drift(double d, double y) {
                return (0.5*theta_*k_ - 0.125*sigma_*sigma_)/y
                    - 0.5*k_*y;
            }

            public override double diffusion(double d1, double d2) {
                return 0.5*sigma_;
            }
        }

        //! %Dynamics of the short-rate under the Cox-Ingersoll-Ross model
        /*! The state variable \f$ y_t \f$ will here be the square-root of the
            short-rate. It satisfies the following stochastic equation
            \f[
                dy_t=\left[
                        (\frac{k\theta }{2}+\frac{\sigma ^2}{8})\frac{1}{y_t}-
                        \frac{k}{2}y_t \right] d_t+ \frac{\sigma }{2}dW_{t}
            \f].
        */
        public class Dynamics : ShortRateDynamics {
          public Dynamics(double theta,
                     double k,
                     double sigma,
                     double x0)
            : base(new HelperProcess(theta, k, sigma, Math.Sqrt(x0))) {}

            public override double variable(double d, double r) {
                return Math.Sqrt(r);
            }
            public override double shortRate(double d, double y) {
                return y*y;
            }
        }
    }
}