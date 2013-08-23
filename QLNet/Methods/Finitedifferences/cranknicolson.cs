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
    //! Crank-Nicolson scheme for finite difference methods
    /*! In this implementation, the passed operator must be derived
        from either TimeConstantOperator or TimeDependentOperator.
        Also, it must implement at least the following interface:

        \warning The differential operator must be linear for
                 this evolver to work.

        \ingroup findiff
    */
    public class CrankNicolson<Operator> : MixedScheme<Operator>, ISchemeFactory where Operator : IOperator {
        // constructors
        public CrankNicolson() { }  // required for generics
        public CrankNicolson(Operator L, List<BoundaryCondition<IOperator>> bcs)
            : base(L, 0.5, bcs) { }

        public IMixedScheme factory(object L, object bcs) {
            return new CrankNicolson<Operator>((Operator)L, (List<BoundaryCondition<IOperator>>)bcs);
        }
    }
}
