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
    //! \f$ D_{0} \f$ matricial representation
    /*! The differential operator \f$ D_{0} \f$ discretizes the
        first derivative with the second-order formula
        \f[ \frac{\partial u_{i}}{\partial x} \approx
            \frac{u_{i+1}-u_{i-1}}{2h} = D_{0} u_{i}
        \f]

        \ingroup findiff

        \test the correctness of the returned values is tested by
              checking them against numerical calculations.
    */
    public class DZero : TridiagonalOperator {
        public DZero(int gridPoints, double h)
            : base(gridPoints) {
            setFirstRow(-1 / h, 1 / h);                  // linear extrapolation
            setMidRows(-1 / (2 * h), 0.0, 1 / (2 * h));
            setLastRow(-1 / h, 1 / h);                   // linear extrapolation
        }
    }
}
