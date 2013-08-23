/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

namespace QLNet {
    //! Single-factor short-rate model abstract class
    /*! \ingroup shortrate */
    public abstract class OneFactorModel : ShortRateModel {
        public OneFactorModel(int nArguments) : base(nArguments) { }

        //! Base class describing the short-rate dynamics
        public abstract class ShortRateDynamics {                        
            private StochasticProcess1D process_;
            //! Returns the risk-neutral dynamics of the state variable
            public StochasticProcess1D process() { return process_; }

            public ShortRateDynamics(StochasticProcess1D process) {
                process_ = process;
            }

            //! Compute state variable from short rate
            public abstract double variable(double t, double r);

            //! Compute short rate from state variable
            public abstract double shortRate(double t, double variable);

        }

        // public class ShortRateTree;

        //! returns the short-rate dynamics
        public abstract ShortRateDynamics dynamics();

        //! Return by default a trinomial recombining tree
        public override Lattice tree(TimeGrid grid)
        {
            //throw new NotImplementedException();
            TrinomialTree trinomial = new TrinomialTree(dynamics().process(), grid);
            return new ShortRateTree(trinomial, dynamics(), grid);
        }
        //! Recombining trinomial tree discretizing the state variable
        public class ShortRateTree : TreeLattice1D<ShortRateTree> ,IGenericLattice
        {
            protected override ShortRateTree impl()
            {
                return this;
            }

            //! Plain tree build-up from short-rate dynamics
            public ShortRateTree(TrinomialTree tree,
                          ShortRateDynamics dynamics,
                          TimeGrid timeGrid)
                : base(timeGrid, tree.size(1))
            {
                tree_ = tree;
                dynamics_ = dynamics;
            }

            //! Tree build-up + numerical fitting to term-structure
            public ShortRateTree(TrinomialTree tree,
                          ShortRateDynamics dynamics,
                          TermStructureFittingParameter.NumericalImpl theta,
                          TimeGrid timeGrid)
                : base(timeGrid, tree.size(1))
            {
                tree_ = tree;
                dynamics_ = dynamics;
                theta.reset();
                double value = 1.0;
                double vMin = -100.0;
                double vMax = 100.0;
                for (int i = 0; i < (timeGrid.size() - 1); i++)
                {
                    double discountBond = theta.termStructure().link.discount(t_[i + 1]);
                    Helper finder = new Helper(i, discountBond, theta, this);
                    Brent s1d = new Brent();
                    s1d.setMaxEvaluations(1000);
                    value = s1d.solve(finder, 1e-7, value, vMin, vMax);
                    // vMin = value - 1.0;
                    // vMax = value + 1.0;
                    theta.change(value);
                }

            }

            public int size(int i)
            {
                return tree_.size(i);
            }
            public double discount(int i, int index)
            {
                double x = tree_.underlying(i, index);
                double r = dynamics_.shortRate(timeGrid()[i], x);
                return Math.Exp(-r * timeGrid().dt(i));
            }
            public override double underlying(int i, int index)
            {
                return tree_.underlying(i, index);
            }
            public int descendant(int i, int index, int branch)
            {
                return tree_.descendant(i, index, branch);
            }
            public double probability(int i, int index, int branch)
            {
                return tree_.probability(i, index, branch);
            }

            private TrinomialTree tree_;
            private ShortRateDynamics dynamics_;

            public class Helper : ISolver1d
            {

                private int size_;
                private int i_;
                private Vector statePrices_;
                private double discountBondPrice_;
                private TermStructureFittingParameter.NumericalImpl theta_;
                private ShortRateTree tree_;

                public Helper(int i,
                                double discountBondPrice,
                                TermStructureFittingParameter.NumericalImpl theta,
                                ShortRateTree tree)
                {
                    size_ = tree.size(i);
                    i_ = i;
                    statePrices_ = tree.statePrices(i);
                    discountBondPrice_ = discountBondPrice;
                    theta_ = theta;
                    tree_ = tree;
                    theta_.setvalue(tree.timeGrid()[i], 0.0);
                }

                public override double value(double theta)
                {
                    double value = discountBondPrice_;
                    theta_.change(theta);
                    for (int j = 0; j < size_; j++)
                        value -= statePrices_[j] * tree_.discount(i_, j);
                    return value;
                }
            }

        }
    }
    
    public abstract class OneFactorAffineModel : OneFactorModel,
                                                 IAffineModel
    {
        public OneFactorAffineModel(int nArguments)
            : base(nArguments) { }

        public virtual double discountBond(double now,
                                  double maturity,
                                  Vector factors)
        {
            return discountBond(now, maturity, factors[0]);
        }

        public double discountBond(double now, double maturity, double rate)
        {
            return A(now, maturity) * Math.Exp(-B(now, maturity) * rate);
        }

        public double discount(double t)
        {
            double x0 = dynamics().process().x0();
            double r0 = dynamics().shortRate(0.0, x0);
            return discountBond(0.0, t, r0);
        }
        //double discountBond(double now, double maturity, Vector factors);
        public virtual double discountBondOption(Option.Type type,
                                            double strike,
                                            double maturity,
                                            double bondMaturity) { throw new NotImplementedException(); }

        protected abstract double A(double t, double T);//{throw new NotImplementedException();}
        protected abstract double B(double t, double T);//{throw new NotImplementedException();}
    }
}
