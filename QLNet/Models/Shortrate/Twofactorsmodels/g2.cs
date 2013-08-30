/*
 Copyright (C) 2010 Philippe double (ph_real@hotmail.com)
  
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

/*! \file g2.hpp
    \brief Two-factor additive Gaussian Model G2++
*/

namespace QLNet
{

    //! Two-additive-factor gaussian model class.
    /*! This class implements a two-additive-factor model defined by
        \f[
            dr_t = \varphi(t) + x_t + y_t
        \f]
        where \f$ x_t \f$ and \f$ y_t \f$ are defined by
        \f[
            dx_t = -a x_t dt + \sigma dW^1_t, x_0 = 0
        \f]
        \f[
            dy_t = -b y_t dt + \sigma dW^2_t, y_0 = 0
        \f]
        and \f$ dW^1_t dW^2_t = \rho dt \f$.

        \bug This class was not tested enough to guarantee
             its functionality.

        \ingroup shortrate
    */
    public class G2 : TwoFactorModel,
                      IAffineModel,
                      ITermStructureConsistentModel
    {


        #region ITermStructureConsistentModel
        public Handle<YieldTermStructure> termStructure()
        {
            return termStructure_;
        }

        public Handle<YieldTermStructure> termStructure_ { get; set; }
        #endregion

        Parameter a_;
        Parameter sigma_;
        Parameter b_;
        Parameter eta_;
        Parameter rho_;
        Parameter phi_;

        public G2(Handle<YieldTermStructure> termStructure,
           double a,
           double sigma,
           double b,
           double eta,
           double rho)
            : base(5)
        {
            //TermStructureConsistentModel = termStructure;
            /* regroupement car plant!!
             * *a_ = arguments_[0];
            sigma_ = arguments_[1]; 
            b_ = arguments_[2];
            eta_ = arguments_[3];
            rho_ = arguments_[4];*/

            termStructure_ = termStructure;
            a_ = arguments_[0] = new ConstantParameter(a, new PositiveConstraint());
            sigma_ = arguments_[1] = new ConstantParameter(sigma, new PositiveConstraint());
            b_ = arguments_[2] = new ConstantParameter(b, new PositiveConstraint());
            eta_ =  arguments_[3] = new ConstantParameter(eta, new PositiveConstraint());
            rho_ = arguments_[4] = new ConstantParameter(rho, new BoundaryConstraint(-1.0, 1.0));

            generateArguments();
            termStructure.registerWith(update);
        }

        public G2(Handle<YieldTermStructure> termStructure,
           double a,
           double sigma,
           double b,
           double eta)
            : this(termStructure, a, sigma, b, eta, -0.75)
        { }

        public G2(Handle<YieldTermStructure> termStructure,
           double a,
           double sigma,
           double b)
            : this(termStructure, a, sigma, b, 0.01, -0.75)
        { }


        public G2(Handle<YieldTermStructure> termStructure,
           double a,
           double sigma)
            : this(termStructure, a, sigma, 0.1, 0.01, -0.75)
        { }

        public G2(Handle<YieldTermStructure> termStructure,
           double a)
            : this(termStructure, a, 0.01, 0.1, 0.01, -0.75)
        { }

        public G2(Handle<YieldTermStructure> termStructure)
            : this(termStructure, 0.1, 0.01, 0.1, 0.01, -0.75)
        { }

        public override ShortRateDynamics dynamics()
        {
            return new Dynamics(phi_, a(), sigma(), b(), eta(), rho());
        }

        public virtual double discountBond(double now,
                                  double maturity,
                                  Vector factors)
        {
            if (!(factors.size() > 1))
                throw new ApplicationException("g2 model needs two factors to compute discount bond");
            return discountBond(now, maturity, factors[0], factors[1]);
        }

        public virtual double discountBond(double t, double T, double x, double y)
        {
            return A(t, T) * Math.Exp(-B(a(), (T - t)) * x - B(b(), (T - t)) * y);
        }


        public virtual double discountBondOption(Option.Type type,
                                double strike,
                                double maturity,
                                double bondMaturity)
        {
            double v = sigmaP(maturity, bondMaturity);
            double f = termStructure().link.discount(bondMaturity);
            double k = termStructure().link.discount(maturity) * strike;

            return Utils.blackFormula(type, k, f, v);
        }

        public double discount(double t)
        {
            return termStructure().currentLink().discount(t);
        }

        public double swaption(Swaption.Arguments arguments,
                        double fixedRate,
                        double range,
                        int intervals){
        
        Date settlement = termStructure().link.referenceDate();
        DayCounter dayCounter = termStructure().link.dayCounter();
        double start = dayCounter.yearFraction(settlement,
                                             arguments.floatingResetDates[0]);
        double w = (arguments.type==VanillaSwap.Type.Payer ? 1 : -1 );

        List<double> fixedPayTimes = new  InitializedList<double>(arguments.fixedPayDates.Count);
        for (int i=0; i<fixedPayTimes.Count; ++i)
            fixedPayTimes[i] =
                dayCounter.yearFraction(settlement,
                                        arguments.fixedPayDates[i]);

        SwaptionPricingFunction function = new SwaptionPricingFunction(a(),
                                                sigma(), b(), eta(), rho(),
                                                w, start,
                                                fixedPayTimes,
                                                fixedRate,this);

        double upper = function.mux() + range*function.sigmax();
        double lower = function.mux() - range*function.sigmax();
        SegmentIntegral integrator = new SegmentIntegral(intervals);
        return arguments.nominal*w*termStructure().link.discount(start)*
            integrator.value(function.value, lower, upper);
        }

        #region protected
        protected override void generateArguments()
        {
            phi_ = new FittingParameter(termStructure(),
                a(), sigma(), b(), eta(), rho());
        }

        protected double A(double t, double T)
        {
            return termStructure().link.discount(T) / termStructure().link.discount(t) *
             Math.Exp(0.5 * (V(T - t) - V(T) + V(t)));
        }

        protected double B(double x, double t)
        {
            return (1.0 - Math.Exp(-x * t)) / x;
        }
#endregion

        #region private
        double sigmaP(double t, double s)
        {
            double temp = 1.0 - Math.Exp(-(a() + b()) * t);
            double temp1 = 1.0 - Math.Exp(-a() * (s - t));
            double temp2 = 1.0 - Math.Exp(-b() * (s - t));
            double a3 = a() * a() * a();
            double b3 = b() * b() * b();
            double sigma2 = sigma() * sigma();
            double eta2 = eta() * eta();
            double value =
                0.5 * sigma2 * temp1 * temp1 * (1.0 - Math.Exp(-2.0 * a() * t)) / a3 +
                0.5 * eta2 * temp2 * temp2 * (1.0 - Math.Exp(-2.0 * b() * t)) / b3 +
                2.0 * rho() * sigma() * eta() / (a() * b() * (a() + b())) *
                temp1 * temp2 * temp;
            return Math.Sqrt(value);
        }


        double V(double t)
        {
            double expat = Math.Exp(-a() * t);
            double expbt = Math.Exp(-b() * t);
            double cx = sigma() / a();
            double cy = eta() / b();
            double valuex = cx * cx * (t + (2.0 * expat - 0.5 * expat * expat - 1.5) / a());
            double valuey = cy * cy * (t + (2.0 * expbt - 0.5 * expbt * expbt - 1.5) / b());
            double value = 2.0 * rho() * cx * cy * (t + (expat - 1.0) / a()
                                             + (expbt - 1.0) / b()
                                             - (expat * expbt - 1.0) / (a() + b()));
            return valuex + valuey + value;
        }

        double a() { return a_.value(0.0); }
        double sigma() { return sigma_.value(0.0); }
        double b() { return b_.value(0.0); }
        double eta() { return eta_.value(0.0); }
        double rho() { return rho_.value(0.0); }

        #endregion

        public class Dynamics : ShortRateDynamics
        {

            Parameter fitting_;
            public Dynamics(Parameter fitting,
                     double a,
                     double sigma,
                     double b,
                     double eta,
                     double rho)
                : base((StochasticProcess1D)new OrnsteinUhlenbeckProcess(a, sigma),
                       (StochasticProcess1D)(new OrnsteinUhlenbeckProcess(b, eta)), rho)
            {
                fitting_ = fitting;
            }

            public override double shortRate(double t, double x, double y)
            {
                return fitting_.value(t) + x + y;
            }

            public override StochasticProcess process()
            {
                throw new NotImplementedException();
            }

            //! Analytical term-structure fitting parameter \f$ \varphi(t) \f$.
            /*! \f$ \varphi(t) \f$ is analytically defined by
                \f[
                    \varphi(t) = f(t) +
                         \frac{1}{2}(\frac{\sigma(1-e^{-at})}{a})^2 +
                         \frac{1}{2}(\frac{\eta(1-e^{-bt})}{b})^2 +
                         \rho\frac{\sigma(1-e^{-at})}{a}\frac{\eta(1-e^{-bt})}{b},
                \f]
                where \f$ f(t) \f$ is the instantaneous forward rate at \f$ t \f$.
            */
        }
        
        public class FittingParameter : TermStructureFittingParameter
            {

                private new class Impl : Parameter.Impl
                {


                    public Impl(Handle<YieldTermStructure> termStructure,
                         double a,
                         double sigma,
                         double b,
                         double eta,
                         double rho)
                    {
                        termStructure_ = termStructure;
                        a_ = a;
                        sigma_ = sigma;
                        b_ = b;
                        eta_ = eta;
                        rho_ = rho;
                    }

                    public override double value(Vector v, double t)
                    {
                        InterestRate forward = termStructure_.currentLink().forwardRate(t, t,
                                                                     Compounding.Continuous,
                                                                     Frequency.NoFrequency);
                        double temp1 = sigma_ * (1.0 - Math.Exp(-a_ * t)) / a_;
                        double temp2 = eta_ * (1.0 - Math.Exp(-b_ * t)) / b_;
                        double value = 0.5 * temp1 * temp1 + 0.5 * temp2 * temp2 +
                            rho_ * temp1 * temp2 + forward.value();
                        return value;
                    }
                    Handle<YieldTermStructure> termStructure_;
                    double a_, sigma_, b_, eta_, rho_;
                }

                public FittingParameter(Handle<YieldTermStructure> termStructure,
                                 double a,
                                 double sigma,
                                 double b,
                                 double eta,
                                 double rho)
                    : base((Parameter.Impl)(
                                      new FittingParameter.Impl(termStructure, a, sigma,
                                                                 b, eta, rho))) { }
            }

        public class SwaptionPricingFunction {
      
        #region private fields 
        double a_, sigma_, b_, eta_, rho_, w_;
        double T_;
        List<double> t_;
        double rate_;
        int size_;
        Vector A_, Ba_, Bb_;
        double mux_, muy_, sigmax_, sigmay_, rhoxy_;
        #endregion

        public SwaptionPricingFunction(double a, double sigma,
                                double b, double eta, double rho,
                                double w, double start,
                                List<double> payTimes,
                                double fixedRate, G2 model)
        {
            a_ = a;
            sigma_ = sigma;
            b_ = b;
            eta_ = eta;
            rho_ = rho;
            w_ = w;
            T_ = start;
            t_ = payTimes;
            rate_ = fixedRate;
            size_ = t_.Count();

            A_  = new Vector(size_);
            Ba_ = new Vector(size_);
            Bb_ = new Vector(size_); 


            sigmax_ = sigma_*Math.Sqrt(0.5*(1.0-Math.Exp(-2.0*a_*T_))/a_);
            sigmay_ =   eta_*Math.Sqrt(0.5*(1.0-Math.Exp(-2.0*b_*T_))/b_);
            rhoxy_ = rho_*eta_*sigma_*(1.0 - Math.Exp(-(a_+b_)*T_))/
                ((a_+b_)*sigmax_*sigmay_);

            double temp = sigma_*sigma_/(a_*a_);
            mux_ = -((temp+rho_*sigma_*eta_/(a_*b_))*(1.0 - Math.Exp(-a*T_)) -
                     0.5*temp*(1.0 - Math.Exp(-2.0*a_*T_)) -
                     rho_*sigma_*eta_/(b_*(a_+b_))*
                     (1.0- Math.Exp(-(b_+a_)*T_)));

            temp = eta_*eta_/(b_*b_);
            muy_ = -((temp+rho_*sigma_*eta_/(a_*b_))*(1.0 - Math.Exp(-b*T_)) -
                     0.5*temp*(1.0 - Math.Exp(-2.0*b_*T_)) -
                     rho_*sigma_*eta_/(a_*(a_+b_))*
                     (1.0- Math.Exp(-(b_+a_)*T_)));

            for (int i=0; i<size_; i++) {
                A_[i] = model.A(T_, t_[i]);
                Ba_[i] = model.B(a_, t_[i]-T_);
                Bb_[i] = model.B(b_, t_[i]-T_);
            }
        }

        internal double mux() { return mux_; }

        internal double sigmax() { return sigmax_; }
        
        public double value(double x)  
        {
            CumulativeNormalDistribution phi = new CumulativeNormalDistribution();
            double temp = (x - mux_)/sigmax_;
            double txy = Math.Sqrt(1.0 - rhoxy_*rhoxy_);

            Vector lambda = new Vector(size_);
            int i;
            for (i=0; i<size_; i++) {
                double tau = (i==0 ? t_[0] - T_ : t_[i] - t_[i-1]);
                double c = (i==size_-1 ? (1.0+rate_*tau) : rate_*tau);
                lambda[i] = c*A_[i]*Math.Exp(-Ba_[i]*x);
            }

            //SolvingFunction function = new SolvingFunction(lambda, Bb_);
            //verifier le polymorphisme
            SolvingFunction function = new SolvingFunction(lambda, Bb_);
            Brent s1d = new Brent();
            s1d.setMaxEvaluations(1000);
            double yb = s1d.solve(function, 1e-6, 0.00, -100.0, 100.0);

            double h1 = (yb - muy_)/(sigmay_*txy) -
                rhoxy_*(x  - mux_)/(sigmax_*txy);
            double value = phi.value(-w_*h1);


            for (i=0; i<size_; i++) {
                double h2 = h1 +
                    Bb_[i]*sigmay_*Math.Sqrt(1.0-rhoxy_*rhoxy_);
                double kappa = - Bb_[i] *
                    (muy_ - 0.5*txy*txy*sigmay_*sigmay_*Bb_[i] +
                     rhoxy_*sigmay_*(x-mux_)/sigmax_);
                value -= lambda[i] *Math.Exp(kappa)*phi.value(-w_*h2);
            }

            return Math.Exp(-0.5*temp*temp)*value/
                (sigmax_*Math.Sqrt(2.0*QLNet.Const.M_PI));
        }

        public class SolvingFunction : ISolver1d
        {
            
            Vector lambda_;
            Vector Bb_;
            
            public SolvingFunction(Vector lambda, Vector Bb)
            {
                lambda_ = lambda;
                Bb_ = Bb;
            }

            public override double value(double y) {
                double value = 1.0;
                for (int i=0; i<lambda_.size(); i++) {
                    value -= lambda_[i]*Math.Exp(-Bb_[i]*y);
                }
                return value;
            }

        }

    }

    }
}


