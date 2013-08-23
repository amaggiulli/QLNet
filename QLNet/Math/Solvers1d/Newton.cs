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
    //! %Newton 1-D solver
    /*! \note This solver requires that the passed function object
              implement a method <tt>Real derivative(Real)</tt>.
    */
    public class Newton : Solver1D {
        protected override double solveImpl(ISolver1d f, double xAccuracy) {
            /* The implementation of the algorithm was inspired by Press, Teukolsky, Vetterling, and Flannery,
               "Numerical Recipes in C", 2nd edition, Cambridge University Press */

            double froot, dfroot, dx;

            froot = f.value(root_);
            dfroot = f.derivative(root_);

            if (dfroot == default(double))
                throw new ArgumentException("Newton requires function's derivative");
            evaluationNumber_++;

            while (evaluationNumber_<=maxEvaluations_) {
                dx=froot/dfroot;
                root_ -= dx;
                // jumped out of brackets, switch to NewtonSafe
                if ((xMin_-root_)*(root_-xMax_) < 0.0) {
                    NewtonSafe s = new NewtonSafe();
                    s.setMaxEvaluations(maxEvaluations_-evaluationNumber_);
                    return s.solve(f, xAccuracy, root_+dx, xMin_, xMax_);
                }
                if (Math.Abs(dx) < xAccuracy)
                    return root_;
                froot = f.value(root_);
                dfroot = f.derivative(root_);
                evaluationNumber_++;
            }

            throw new ApplicationException("maximum number of function evaluations (" + maxEvaluations_ + ") exceeded");
        }
    }
}
