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
    //! Black-Scholes-Merton differential operator
    /*! \ingroup findiff */
    public class BSMOperator : TridiagonalOperator {
        public BSMOperator() { }

        public BSMOperator(int size, double dx, double r, double q, double sigma) : base(size) {
            double sigma2 = sigma*sigma;
            double nu = r-q-sigma2/2;
            double pd = -(sigma2/dx-nu)/(2*dx);
            double pu = -(sigma2/dx+nu)/(2*dx);
            double pm = sigma2/(dx*dx)+r;
            setMidRows(pd,pm,pu);
        }

        public BSMOperator(Vector grid, GeneralizedBlackScholesProcess process, double residualTime) : base(grid.size()) {
            //PdeBSM::grid_type  logGrid(grid);
            LogGrid logGrid = new LogGrid(grid);
            var cc = new PdeConstantCoeff<PdeBSM>(process, residualTime, process.stateVariable().link.value());
            cc.generateOperator(residualTime, logGrid, this);
        }
    }
}
