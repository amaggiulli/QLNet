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
    //! %Lattice (tree, finite-differences) base class
    public abstract class Lattice {
        protected TimeGrid t_;
        public TimeGrid timeGrid() { return t_; }

        public Lattice(TimeGrid timeGrid) {
            t_ = timeGrid;
        }

        /*! \name Numerical method interface

            These methods are to be used by discretized assets and
            must be overridden by developers implementing numerical
            methods. Users are advised to use the corresponding
            methods of DiscretizedAsset instead.

            @{
        */

        //! initialize an asset at the given time.
        public abstract void initialize(DiscretizedAsset a, double time);

        /*! Roll back an asset until the given time, performing any needed adjustment. */
        public abstract void rollback(DiscretizedAsset a, double to);

        /*! Roll back an asset until the given time, but do not perform
            the final adjustment.

            \warning In version 0.3.7 and earlier, this method was called rollAlmostBack method and performed
                     pre-adjustment. This is no longer true; when migrating your code, you'll have to replace calls
                     such as:
                     \code
                     method->rollAlmostBack(asset,t);
                     \endcode
                     with the two statements:
                     \code
                     method->partialRollback(asset,t);
                     asset->preAdjustValues();
                     \endcode
        */
        public abstract void partialRollback(DiscretizedAsset a, double to);

        //! computes the present value of an asset.
        public abstract double presentValue(DiscretizedAsset a);

        // this is a smell, but we need it. We'll rethink it later.
        public virtual Vector grid(double t) { throw new NotImplementedException(); }
    }
}
