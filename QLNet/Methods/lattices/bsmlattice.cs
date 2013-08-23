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
    // this is just a wrapper for QL compatibility
    public class BlackScholesLattice<T> : BlackScholesLattice where T : ITree {
        public BlackScholesLattice(ITree tree, double riskFreeRate, double end, int steps)
            : base(tree, riskFreeRate, end, steps) { }
    }

    //! Simple binomial lattice approximating the Black-Scholes model
    /*! \ingroup lattices */
    public class BlackScholesLattice : TreeLattice1D<BlackScholesLattice>, IGenericLattice {
        private ITree tree_;
        private double discount_;
        private double pd_, pu_;

        public BlackScholesLattice(ITree tree, double riskFreeRate, double end, int steps)
            : base(new TimeGrid(end, steps), 2) {
            tree_ = tree;
            discount_ = Math.Exp(-riskFreeRate*(end/steps));
            pd_ = tree.probability(0,0,0);
            pu_ = tree.probability(0,0,1);
        }

        public int size(int i) { return tree_.size(i); }
        public double discount(int i, int j) { return discount_; }

        public override void stepback(int i, Vector values, Vector newValues){
            for (int j=0; j<size(i); j++)
                newValues[j] = (pd_*values[j] + pu_*values[j+1])*discount_;
        }

        public override double underlying(int i, int index) { return tree_.underlying(i, index); }
        public int descendant(int i, int index, int branch) { return tree_.descendant(i, index, branch); }
        public double probability(int i, int index, int branch) { return tree_.probability(i, index, branch); }

        // this is a workaround for CuriouslyRecurringTemplate of TreeLattice
        // recheck it
        protected override BlackScholesLattice impl() { return this; }
    }
}
