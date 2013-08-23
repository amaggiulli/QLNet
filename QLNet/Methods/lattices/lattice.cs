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
    //! Tree-based lattice-method base class
    /*! This class defines a lattice method that is able to rollback
        (with discount) a discretized asset object. It will be based
        on one or more trees.

        Derived classes must implement the following interface:
        \code
        public:
          DiscountFactor discount(Size i, Size index) const;
          Size descendant(Size i, Size index, Size branch) const;
          Real probability(Size i, Size index, Size branch) const;
        \endcode
        and may implement the following:
        \code
        public:
          void stepback(Size i,
                        const Array& values,
                        Array& newValues) const;
        \endcode

        \ingroup lattices
    */
    public interface IGenericLattice
    {
        int size(int i);
        double discount(int i, int j);
        void stepback(int i, Vector values, Vector newValues);
        double underlying(int i, int index);
        int descendant(int i, int index, int branch);
        double probability(int i, int index, int branch);
    }

    public class TreeLattice<T> : Lattice where T : IGenericLattice{
        // this should be overriden in the dering class
        protected virtual T impl() { throw new NotSupportedException(); }

        private int n_;
        private int statePricesLimit_;

        // Arrow-Debrew state prices
        protected List<Vector> statePrices_;

        public TreeLattice(TimeGrid timeGrid, int n) : base(timeGrid) {
            n_ = n;
            if (!(n>0)) throw new ApplicationException("there is no zeronomial lattice!");
            statePrices_ = new InitializedList<Vector>(1, new Vector(1, 1.0));
            statePricesLimit_ = 0;
        }


        //! \name Lattice interface
        public override void initialize(DiscretizedAsset asset, double t) {
            int i = t_.index(t);
            asset.setTime(t);
            asset.reset(impl().size(i));
        }

        public override void rollback(DiscretizedAsset asset, double to) {
            partialRollback(asset,to);
            asset.adjustValues();
        }

        public override void partialRollback(DiscretizedAsset asset, double to) {
            double from = asset.time();

            if (Utils.close(from,to)) return;

            if (!(from > to)) throw new ApplicationException("cannot roll the asset back to" + to
                       + " (it is already at t = " + from + ")");

            int iFrom = t_.index(from);
            int iTo = t_.index(to);

            for (int i=iFrom-1; i>=iTo; --i) {
                Vector newValues = new Vector(impl().size(i));
                impl().stepback(i, asset.values(), newValues);
                asset.setTime(t_[i]);
                asset.setValues(newValues);
                // skip the very last adjustment
                if (i != iTo)
                    asset.adjustValues();
            }
        }

        //! Computes the present value of an asset using Arrow-Debrew prices
        public override double presentValue(DiscretizedAsset asset) {
            int i = t_.index(asset.time());
            return Vector.DotProduct(asset.values(), statePrices(i));
        }

        public Vector statePrices(int i) {
            if (i > statePricesLimit_) computeStatePrices(i);
            return statePrices_[i];
        }

        public virtual void stepback(int i, Vector values, Vector newValues) {
            for (int j=0; j<impl().size(i); j++) {
                double value = 0.0;
                for (int l=0; l<n_; l++) 
                {
                   double d1, d2;
                   d1 = impl().probability(i, j, l);
                   d2 = values[impl().descendant(i, j, l)];
                    value += impl().probability(i,j,l) * values[impl().descendant(i,j,l)];
                }
                value *= impl().discount(i,j);
                newValues[j] = value;
            }
        }

        protected void computeStatePrices(int until) {
            for (int i = statePricesLimit_; i < until; i++) {
                statePrices_.Add(new Vector(impl().size(i+1), 0.0));
                for (int j = 0; j < impl().size(i); j++) {
                    double disc = impl().discount(i,j);
                    double statePrice = statePrices_[i][j];
                    for (int l=0; l<n_; l++) {
                        statePrices_[i+1][impl().descendant(i,j,l)] += statePrice*disc*impl().probability(i,j,l);
                    }
                }
            }
            statePricesLimit_ = until;
        }
    }
}
