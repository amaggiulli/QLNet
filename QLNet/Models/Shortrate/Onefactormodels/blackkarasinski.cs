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

namespace QLNet
{

    public class BlackKarasinski :   OneFactorModel,
                                     ITermStructureConsistentModel
   {
        private double a() { return a_.value(0.0); }
        private double sigma()  { return sigma_.value(0.0); }

        Parameter a_;
        Parameter sigma_;
        
        public BlackKarasinski( Handle<YieldTermStructure> termStructure,
                                double a, double sigma )
            : base(2)
        {         
            a_=arguments_[0];
            sigma_=arguments_[1];
            a_ = arguments_[0] = new ConstantParameter(a, new PositiveConstraint());
            sigma_ = arguments_[1] = new ConstantParameter(sigma, new PositiveConstraint());
            termStructure_ = new Handle<YieldTermStructure>();
            termStructure_ = termStructure;
            termStructure.registerWith(update);
        }

        public BlackKarasinski(Handle<YieldTermStructure> termStructure)
            : this(termStructure, 0.1, 0.1)
        { }

        public override Lattice tree( TimeGrid grid)  
        {
            TermStructureFittingParameter phi= new TermStructureFittingParameter(termStructure());

            ShortRateDynamics numericDynamics=
                                    new Dynamics(phi, a(), sigma());

            TrinomialTree trinomial=
                             new TrinomialTree(numericDynamics.process(), grid);
            ShortRateTree numericTree =
                             new ShortRateTree(trinomial, numericDynamics, grid);

            TermStructureFittingParameter.NumericalImpl impl = 
                (TermStructureFittingParameter.NumericalImpl)phi.implementation();
            impl.reset();
            double value = 1.0;
            double vMin = -50.0;
            double vMax = 50.0;
            for (int i=0; i<(grid.size() - 1); i++) {
                double discountBond = termStructure().link.discount(grid[i+1]);
                double xMin = trinomial.underlying(i, 0);
                double dx = trinomial.dx(i);
                Helper finder = new Helper(i, xMin, dx, discountBond, numericTree);
                Brent s1d = new Brent();
                s1d.setMaxEvaluations(1000);
                value = s1d.solve(finder, 1e-7, value, vMin, vMax);
                impl.setvalue(grid[i], value);
                // vMin = value - 10.0;
                // vMax = value + 10.0;
            }
            return numericTree;
        }

        public override ShortRateDynamics dynamics()  {
            throw new NotImplementedException("no defined process for Black-Karasinski");
        }
      
        #region ITermStructureConsistentModel
        public Handle<YieldTermStructure> termStructure(){
            return termStructure_;
        }

        public Handle<YieldTermStructure> termStructure_ { get; set; }
        
        #endregion
    }

    //! Short-rate dynamics in the Black-Karasinski model
    /*! The short-rate is here
        \f[
            r_t = e^{\varphi(t) + x_t}
         \f]
         where \f$ \varphi(t) \f$ is the deterministic time-dependent
         parameter (which can not be determined analytically)
         used for term-structure fitting and \f$ x_t \f$ is the state
         variable following an Ornstein-Uhlenbeck process.
    */
    public class Dynamics : OneFactorModel.ShortRateDynamics 
    {
      
        public Dynamics(Parameter fitting, double alpha, double sigma)
        : base((StochasticProcess1D)(new OrnsteinUhlenbeckProcess(alpha, sigma))){
          fitting_=fitting; }

        public override double variable(double t, double r)
        {
            return Math.Log(r) - fitting_.value(t);
        }

        public override double shortRate(double t, double x)
        {
            return Math.Exp(x + fitting_.value(t));
        }
        
        private Parameter fitting_;
    }
    
    // Private function used by solver to determine time-dependent parameter
    public class Helper : ISolver1d
    {
        private int size_;
        private double dt_;
        private double xMin_, dx_;
        private Vector statePrices_;
        private double discountBondPrice_;

        public Helper(int i, double xMin, double dx,
               double discountBondPrice,
               OneFactorModel.ShortRateTree tree){
          size_=tree.size(i);
          dt_=tree.timeGrid().dt(i);
          xMin_=xMin;
          dx_=dx;
          statePrices_=tree.statePrices(i);
          discountBondPrice_=discountBondPrice;
        }

        public override double value(double theta){
            double value = discountBondPrice_;
            double x = xMin_;
            for (int j=0; j<size_; j++) {
                double discount = Math.Exp(-Math.Exp(theta+x)*dt_);
                value -= statePrices_[j]*discount;
                x += dx_;
            }
            return value;
        }
    }
}
