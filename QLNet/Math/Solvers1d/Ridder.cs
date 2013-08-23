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
    public class Ridder : Solver1D {
        protected override double solveImpl(ISolver1d f, double xAcc)
        {

            /* The implementation of the algorithm was inspired by
               Press, Teukolsky, Vetterling, and Flannery,
               "Numerical Recipes in C", 2nd edition, Cambridge
               University Press
            */

            double fxMid, froot, s, xMid, nextRoot;

            // test on Black-Scholes implied volatility show that
            // Ridder solver algorithm actually provides an
            // accuracy 100 times below promised
            double xAccuracy = xAcc/100.0;

            // Any highly unlikely value, to simplify logic below
            root_ = double.MinValue;

            while (evaluationNumber_ <= maxEvaluations_) {
                xMid = 0.5*(xMin_ + xMax_);
                // First of two function evaluations per iteraton
                fxMid = f.value(xMid);
                evaluationNumber_++;
                s = Math.Sqrt(fxMid*fxMid - fxMin_*fxMax_);
                if (s == 0.0)
                    return root_;
                // Updating formula
                nextRoot = xMid + (xMid - xMin_) *
                    ((fxMin_  >= fxMax_ ? 1.0 : -1.0) * fxMid / s);
                if (Math.Abs(nextRoot-root_) <= xAccuracy)
                    return root_;

                root_ = nextRoot;
                // Second of two function evaluations per iteration
                froot = f.value(root_);
                evaluationNumber_++;
                if (froot == 0.0)
                    return root_;

                // Bookkeeping to keep the root bracketed on next iteration
                if (sign(fxMid,froot) != fxMid) {
                    xMin_ = xMid;
                    fxMin_ = fxMid;
                    xMax_ = root_;
                    fxMax_ = froot;
                } else if (sign(fxMin_,froot) != fxMin_) {
                    xMax_ = root_;
                    fxMax_ = froot;
                } else if (sign(fxMax_,froot) != fxMax_) {
                    xMin_ = root_;
                    fxMin_ = froot;
                } else {
                    throw new Exception("never get here.");
                }

                if (Math.Abs(xMax_-xMin_) <= xAccuracy) return root_;
            }

             throw new ArgumentException("maximum number of function evaluations (" + maxEvaluations_ + ") exceeded");
        }
        private double sign(double a, double b)
        {
            return b >= 0.0 ? Math.Abs(a) : -Math.Abs(a);
        }

    }
}
