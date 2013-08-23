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
    //! Levenberg-Marquardt optimization method
    /*! This implementation is based on MINPACK
        (<http://www.netlib.org/minpack>,
        <http://www.netlib.org/cephes/linalg.tgz>)
    */
    public class LevenbergMarquardt : OptimizationMethod {
        private Problem currentProblem_;
        private Vector initCostValues_;
        
        private int info_;
        public int getInfo() { return info_; }

        private double epsfcn_, xtol_, gtol_;

        //public LevenbergMarquardt(double epsfcn = 1.0e-8, double xtol = 1.0e-8, double gtol = 1.0e-8) {
        public LevenbergMarquardt() : this(1.0e-8, 1.0e-8, 1.0e-8) { }
        public LevenbergMarquardt(double epsfcn, double xtol, double gtol) {
            info_ = 0;
            epsfcn_ = epsfcn;
            xtol_ = xtol;
            gtol_ = gtol;
        }

        public override EndCriteria.Type minimize(Problem P, EndCriteria endCriteria) {
            EndCriteria.Type ecType = EndCriteria.Type.None;
            P.reset();
            Vector x_ = P.currentValue();
            currentProblem_ = P;
            initCostValues_ = P.costFunction().values(x_);
            int m = initCostValues_.size();
            int n = x_.size();

            Vector xx = new Vector(x_);
            Vector fvec = new Vector(m), diag = new Vector(n);

            int mode = 1;
            double factor = 1;
            int nprint = 0;
            int info = 0;
            int nfev =0;

            Matrix fjac = new Matrix(m, n);

            int ldfjac = m;
            
            List<int> ipvt = new InitializedList<int>(n);
            Vector qtf = new Vector(n), wa1 = new Vector(n), wa2 = new Vector(n), wa3 = new Vector(n), wa4 = new Vector(m);

            // call lmdif to minimize the sum of the squares of m functions
            // in n variables by the Levenberg-Marquardt algorithm.
            MINPACK.lmdif(m, n, xx, ref fvec,
                                     endCriteria.functionEpsilon(),
                                     xtol_,
                                     gtol_,
                                     endCriteria.maxIterations(),
                                     epsfcn_,
                                     diag, mode, factor,
                                     nprint, ref info, ref nfev, ref fjac,
                                     ldfjac, ref ipvt, ref qtf,
                                     wa1, wa2, wa3, wa4,
                                     fcn);
            info_ = info;
            // check requirements & endCriteria evaluation
            if(info == 0) throw new ApplicationException("MINPACK: improper input parameters");
            //if(info == 6) throw new ApplicationException("MINPACK: ftol is too small. no further " +
            //                                             "reduction in the sum of squares is possible.");

            if (info != 6) ecType = EndCriteria.Type.StationaryFunctionValue;
            //QL_REQUIRE(info != 5, "MINPACK: number of calls to fcn has reached or exceeded maxfev.");
            endCriteria.checkMaxIterations(nfev, ref ecType);
            if(info == 7) throw new ApplicationException("MINPACK: xtol is too small. no further " +
                                           "improvement in the approximate " +
                                           "solution x is possible.");
            if(info == 8) throw new ApplicationException("MINPACK: gtol is too small. fvec is " +
                                           "orthogonal to the columns of the " +
                                           "jacobian to machine precision.");
            // set problem
            x_ = new Vector(xx.GetRange(0, n));
            P.setCurrentValue(x_);
            P.setFunctionValue(P.costFunction().value(x_));

            return ecType;
        }

        public Vector fcn(int m, int n, Vector x, int iflag) {
            Vector xt = new Vector(x);
            Vector fvec;
            // constraint handling needs some improvement in the future:
            // starting point should not be close to a constraint violation
            if (currentProblem_.constraint().test(xt)) {
                fvec = new Vector(currentProblem_.values(xt));
            } else {
                fvec = new Vector(initCostValues_);
            }
            return fvec;
        }
    }
}
