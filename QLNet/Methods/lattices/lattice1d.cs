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
    //! One-dimensional tree-based lattice.
    /*! Derived classes must implement the following interface:
        \code
        Real underlying(Size i, Size index) const;
        \endcode

        \ingroup lattices */
    public class TreeLattice1D<T> : TreeLattice<T> where T : IGenericLattice{
        public TreeLattice1D(TimeGrid timeGrid, int n) : base(timeGrid, n) { }

        public override Vector grid(double t) {
            int i = timeGrid().index(t);
            Vector grid = new Vector(impl().size(i));
            for (int j=0; j<grid.size(); j++)
                grid[j] = impl().underlying(i,j);
            return grid;
        }
        public virtual double underlying(int i, int index) {
            return impl().underlying(i,index);
        }
    }
}
