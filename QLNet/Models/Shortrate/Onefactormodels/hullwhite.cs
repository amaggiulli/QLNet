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

/*! \file hullwhite.hpp
    \brief Hull & White (HW) model
*/

namespace QLNet {

    //! Single-factor Hull-White (extended %Vasicek) model class.
    /*! This class implements the standard single-factor Hull-White model
        defined by
        \f[
            dr_t = (\theta(t) - \alpha r_t)dt + \sigma dW_t
        \f]
        where \f$ \alpha \f$ and \f$ \sigma \f$ are constants.

        \test calibration results are tested against cached values

        \bug When the term structure is relinked, the r0 parameter of
             the underlying Vasicek model is not updated.

        \ingroup shortrate
    */
    public class HullWhite : Vasicek, ITermStructureConsistentModel              
    {
        public HullWhite( Handle<YieldTermStructure> termStructure,
                        double a, double sigma)
            : base(termStructure.link.forwardRate(0.0, 0.0, Compounding.Continuous, Frequency.NoFrequency).rate(),
            a, 0.0, sigma, 0.0) {
            this.termStructure_ = termStructure;
            b_ = arguments_[1] = new NullParameter(); //to change
            lambda_ = arguments_[3] = new NullParameter();  //to change
            generateArguments();
            termStructure.registerWith(update);

        }
        public HullWhite(Handle<YieldTermStructure> termStructure,
                        double a)
            : this(termStructure, a, 0.01)
        {
        }

        public HullWhite(Handle<YieldTermStructure> termStructure)
            : this(termStructure, 0.1, 0.01)
        {
        }

        public override Lattice tree(TimeGrid grid) {
            TermStructureFittingParameter phi = new TermStructureFittingParameter(termStructure());
            ShortRateDynamics numericDynamics = new Dynamics(phi, a(), sigma());
            TrinomialTree trinomial = new TrinomialTree(numericDynamics.process(), grid);
            ShortRateTree numericTree = new ShortRateTree(trinomial, numericDynamics, grid);
            TermStructureFittingParameter.NumericalImpl  impl =
                (TermStructureFittingParameter.NumericalImpl)phi.implementation();
            impl.reset();
            for (int i=0; i<(grid.size() - 1); i++) {
                double discountBond = termStructure().link.discount(grid[i+1]);
                Vector  statePrices = numericTree.statePrices(i);
                int size = numericTree.size(i);
                double dt = numericTree.timeGrid().dt(i);
                double dx = trinomial.dx(i);
                double x = trinomial.underlying(i,0);
                double value = 0.0;
                for (int j=0; j<size; j++) {
                    value += statePrices[j]*Math.Exp(-x*dt);
                    x += dx;
                }
                value = Math.Log(value/discountBond)/dt;
                impl.setvalue(grid[i], value);
            }
            return numericTree;
        }

        public override ShortRateDynamics dynamics(){
            return new Dynamics(phi_, a(), sigma());
        }

        public override double discountBondOption(Option.Type type,
                                                double strike,
                                                double maturity,
                                                double bondMaturity){
            double _a = a();
            double v;
            if (_a < Math.Sqrt(Const.QL_Epsilon))
            {
                v = sigma() * B(maturity, bondMaturity) * Math.Sqrt(maturity);
            } else {
                v = sigma()*B(maturity, bondMaturity)*
                     Math.Sqrt(0.5*(1.0 - Math.Exp(-2.0*_a*maturity))/_a);
            }
            double f = termStructure().link.discount(bondMaturity);
            double k = termStructure().link.discount(maturity)*strike;

            return Utils.blackFormula(type, k, f, v);
        }

        /*! Futures convexity bias (i.e., the difference between
            futures implied rate and forward rate) calculated as in
            G. Kirikos, D. Novak, "Convexity Conundrums", Risk
            Magazine, March 1997.

            \note t and T should be expressed in yearfraction using
                  deposit day counter, F_quoted is futures' market price.
        */
        public static double convexityBias(double futuresPrice,
                                    double t,
                                    double T,
                                    double sigma,
                                    double a){
        if(!(futuresPrice>=0.0))
            throw new ApplicationException("negative futures price (" + futuresPrice + ") not allowed");
        if(!(t>=0.0))
            throw new ApplicationException("negative t (" + t + ") not allowed");
        if(!(T>=t))
            throw new ApplicationException("T (" + T + ") must not be less than t (" + t + ")");
        if(!(sigma>=0.0))
            throw new ApplicationException("negative sigma (" + sigma + ") not allowed");
        if(!(a>=0.0))
            throw new ApplicationException("negative a (" + a + ") not allowed");

        double deltaT = (T-t);
        double tempDeltaT = (1.0-Math.Exp(-a*deltaT)) / a;
        double halfSigmaSquare = sigma*sigma/2.0;

        // lambda adjusts for the fact that the underlying is an interest rate
        double lambda = halfSigmaSquare * (1.0-Math.Exp(-2.0*a*t)) / a *
            tempDeltaT * tempDeltaT;

        double tempT = (1.0 - Math.Exp(-a*t)) / a;

        // phi is the MtM adjustment
        double phi = halfSigmaSquare * tempDeltaT * tempT * tempT;

        // the adjustment
        double z = lambda + phi;

        double futureRate = (100.0-futuresPrice)/100.0;
        return (1.0-Math.Exp(-z)) * (futureRate + 1.0/(T-t));
        }

        //a voir pour le sealed sinon pb avec classe mere vasicek et constructeur class Hullwithe
        protected override void generateArguments() {
            phi_ = new FittingParameter(termStructure(), a(), sigma());
        }

        protected override double A(double t, double T)
        {
            double discount1 = termStructure().link.discount(t);
            double discount2 = termStructure().link.discount(T);
            double forward = termStructure().link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).rate();
            double temp = sigma()*B(t,T);
            double value = B(t,T)*forward - 0.25*temp*temp*B(0.0,2.0*t);
            return Math.Exp(value)*discount2/discount1;
        }

        
        //private class Dynamics;
        //private class FittingParameter;
        private Parameter phi_;

        //! Short-rate dynamics in the Hull-White model
        /*! The short-rate is here
            \f[
                r_t = \varphi(t) + x_t
            \f]
            where \f$ \varphi(t) \f$ is the deterministic time-dependent
            parameter used for term-structure fitting and \f$ x_t \f$ is the
            state variable following an Ornstein-Uhlenbeck process.
        */
        public new class Dynamics : ShortRateDynamics {
          
            public Dynamics(Parameter fitting,double a,double sigma)
            : base(new OrnsteinUhlenbeckProcess(a, sigma)){
                fitting_ = fitting;
            }

            public override double variable(double t, double r) {
                return r - fitting_.value(t);
            }

            public override double shortRate(double t, double x) {
                return x + fitting_.value(t);
            }
          
            private Parameter fitting_;
        }

        //! Analytical term-structure fitting parameter \f$ \varphi(t) \f$.
        /*! \f$ \varphi(t) \f$ is analytically defined by
            \f[
                \varphi(t) = f(t) + \frac{1}{2}[\frac{\sigma(1-e^{-at})}{a}]^2,
            \f]
            where \f$ f(t) \f$ is the instantaneous forward rate at \f$ t \f$.
        */
        public class FittingParameter : TermStructureFittingParameter {
           
            private new class Impl : Parameter.Impl {
                private Handle<YieldTermStructure> termStructure_;
                private double a_, sigma_;
                
                public Impl(Handle<YieldTermStructure> termStructure,
                            double a, double sigma){
                    termStructure_ = termStructure;
                    a_ = a;
                    sigma_ = sigma;
                }

                public override double value(Vector v, double t) {
                    double forwardRate =
                        termStructure_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).rate();
                    double temp = a_ < Math.Sqrt(Const.QL_Epsilon) ?
                                sigma_*t :
                                sigma_*(1.0 - Math.Exp(-a_*t))/a_;
                    return (forwardRate + 0.5*temp*temp);
                }
            }
          
            public FittingParameter(Handle<YieldTermStructure> termStructure,
                             double a, double sigma)
            : base(new FittingParameter.Impl(termStructure, a, sigma)) {}
        }

        #region ITermStructureConsistentModel
        public Handle<YieldTermStructure> termStructure(){
            return termStructure_;
        }

        public Handle<YieldTermStructure> termStructure_ { get; set; }
        
        #endregion
    
    }
}
