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
    public class Brent : Solver1D {
        protected override double solveImpl(ISolver1d f, double xAccuracy) {
            /* The implementation of the algorithm was inspired by Press, Teukolsky, Vetterling, and Flannery,
               "Numerical Recipes in C", 2nd edition, Cambridge University Press */

            double min1, min2;
            double froot, p, q, r, s, xAcc1, xMid;
            // dummy assignements to avoid compiler warning
            double d = 0.0, e = 0.0;

            root_ = xMax_;
            froot = fxMax_;
            while (evaluationNumber_ <= maxEvaluations_) {
                if ((froot > 0.0 && fxMax_ > 0.0) ||
                    (froot < 0.0 && fxMax_ < 0.0)) {

                    // Rename xMin_, root_, xMax_ and adjust bounds
                    xMax_ = xMin_;
                    fxMax_ = fxMin_;
                    e = d = root_ - xMin_;
                }
                if (Math.Abs(fxMax_) < Math.Abs(froot)) {
                    xMin_ = root_;
                    root_ = xMax_;
                    xMax_ = xMin_;
                    fxMin_ = froot;
                    froot = fxMax_;
                    fxMax_ = fxMin_;
                }
                // Convergence check
                xAcc1 = 2.0 * Const.QL_Epsilon * Math.Abs(root_) + 0.5 * xAccuracy;
                xMid = (xMax_ - root_) / 2.0;
                if (Math.Abs(xMid) <= xAcc1 || froot == 0.0)
                    return root_;
                if (Math.Abs(e) >= xAcc1 &&
                    Math.Abs(fxMin_) > Math.Abs(froot)) {

                    // Attempt inverse quadratic interpolation
                    s = froot / fxMin_;
                    if (xMin_ == xMax_) {
                        p = 2.0 * xMid * s;
                        q = 1.0 - s;
                    } else {
                        q = fxMin_ / fxMax_;
                        r = froot / fxMax_;
                        p = s * (2.0 * xMid * q * (q - r) - (root_ - xMin_) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }
                    if (p > 0.0) q = -q;  // Check whether in bounds
                    p = Math.Abs(p);
                    min1 = 3.0 * xMid * q - Math.Abs(xAcc1 * q);
                    min2 = Math.Abs(e * q);
                    if (2.0 * p < (min1 < min2 ? min1 : min2)) {
                        e = d;                // Accept interpolation
                        d = p / q;
                    } else {
                        d = xMid;  // Interpolation failed, use bisection
                        e = d;
                    }
                } else {
                    // Bounds decreasing too slowly, use bisection
                    d = xMid;
                    e = d;
                }
                xMin_ = root_;
                fxMin_ = froot;
                if (Math.Abs(d) > xAcc1)
                    root_ += d;
                else
                    root_ += Math.Abs(xAcc1) * Math.Sign(xMid);
                froot = f.value(root_);
                evaluationNumber_++;
            }
            throw new ArgumentException("maximum number of function evaluations (" + maxEvaluations_ + ") exceeded");
        }
    }
}
