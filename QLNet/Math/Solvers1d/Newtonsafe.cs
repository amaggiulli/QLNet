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
    class NewtonSafe : Solver1D {
        //! safe %Newton 1-D solver
        /*! \note This solver requires that the passed function object
                  implement a method <tt>Real derivative(Real)</tt>.
        */
        protected override double solveImpl(ISolver1d f, double xAccuracy) {
            /* The implementation of the algorithm was inspired by Press, Teukolsky, Vetterling, and Flannery,
               "Numerical Recipes in C", 2nd edition, Cambridge University Press */

            double froot, dfroot, dx, dxold;
            double xh, xl;

            // Orient the search so that f(xl) < 0
            if (fxMin_ < 0.0) {
                xl=xMin_;
                xh=xMax_;
            } else {
                xh=xMin_;
                xl=xMax_;
            }

            // the "stepsize before last"
            dxold=xMax_-xMin_;
            // it was dxold=std::fabs(xMax_-xMin_); in Numerical Recipes
            // here (xMax_-xMin_ > 0) is verified in the constructor

            // and the last step
            dx=dxold;

            froot = f.value(root_);
            dfroot = f.derivative(root_);
            if (dfroot == default(double))
                throw new ArgumentException("Newton requires function's derivative");
            evaluationNumber_++;

            while (evaluationNumber_<=maxEvaluations_) {
                // Bisect if (out of range || not decreasing fast enough)
                if ((((root_-xh)*dfroot-froot)*
                     ((root_-xl)*dfroot-froot) > 0.0)
                    || (Math.Abs(2.0*froot) > Math.Abs(dxold*dfroot))) {

                    dxold = dx;
                    dx = (xh-xl)/2.0;
                    root_=xl+dx;
                } else {
                    dxold=dx;
                    dx=froot/dfroot;
                    root_ -= dx;
                }
                // Convergence criterion
                if (Math.Abs(dx) < xAccuracy)
                    return root_;
                froot = f.value(root_);
                dfroot = f.derivative(root_);
                evaluationNumber_++;
                if (froot < 0.0)
                    xl=root_;
                else
                    xh=root_;
            }

            throw new ApplicationException("maximum number of function evaluations (" + maxEvaluations_ + ") exceeded");
        }
    }
}
