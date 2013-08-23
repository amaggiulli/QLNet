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

    // it is an abstract class for solver evaluations
    // it should be an interface but to avoid optional derivate method, it is made as abstract class
    public abstract class ISolver1d : IValue {
        public abstract double value(double v);
        public virtual double derivative(double x) { return 0; }
    }

    //! Base class for 1-D solvers
    /*! Before calling <tt>solveImpl</tt>, the base class will set its protected data members so that:
        - <tt>xMin_</tt> and  <tt>xMax_</tt> form a valid bracket;
        - <tt>fxMin_</tt> and <tt>fxMax_</tt> contain the values of the function in <tt>xMin_</tt> and <tt>xMax_</tt>;
        - <tt>root_</tt> is a valid initial guess.
        The implementation of <tt>solveImpl</tt> can safely assume all of the above.
    */
    public abstract class Solver1D {
        const int MAX_FUNCTION_EVALUATIONS = 100;

        protected double root_, xMin_, xMax_, fxMin_, fxMax_;
        protected int maxEvaluations_ = MAX_FUNCTION_EVALUATIONS;
        protected int evaluationNumber_;

        private double lowerBound_, upperBound_;
        private bool lowerBoundEnforced_ = false, upperBoundEnforced_ = false;


        /*! This method returns the zero of the function \f$ f \f$, determined with the given accuracy \f$ \epsilon \f$;
            depending on the particular solver, this might mean that the returned \f$ x \f$ is such that \f$ |f(x)| < \epsilon
            \f$, or that \f$ |x-\xi| < \epsilon \f$ where \f$ \xi \f$ is the real zero.

            This method contains a bracketing routine to which an initial guess must be supplied as well as a step used to
            scan the range of the possible bracketing values.
        */
        public double solve(ISolver1d f, double accuracy, double guess, double step) {

            if (accuracy <= 0.0)
                throw new ArgumentException("accuracy (" + accuracy + ") must be positive");

            // check whether we really want to use epsilon
            accuracy = Math.Max(accuracy, Const.QL_Epsilon);

            const double growthFactor = 1.6;
            int flipflop = -1;

            root_ = guess;
            fxMax_ = f.value(root_);

            // monotonically crescent bias, as in optionValue(volatility)
            if (fxMax_ == 0.0)
                return root_;
            else if (fxMax_ > 0.0) {
                xMin_ = enforceBounds_(root_ - step);
                fxMin_ = f.value(xMin_);
                xMax_ = root_;
            } else {
                xMin_ = root_;
                fxMin_ = fxMax_;
                xMax_ = enforceBounds_(root_ + step);
                fxMax_ = f.value(xMax_);
            }

            evaluationNumber_ = 2;
            while (evaluationNumber_ <= maxEvaluations_) {
                if (fxMin_ * fxMax_ <= 0.0) {
                    if (fxMin_ == 0.0) return xMin_;
                    if (fxMax_ == 0.0) return xMax_;
                    root_ = (xMax_ + xMin_) / 2.0;
                    return solveImpl(f, accuracy);
                }
                if (Math.Abs(fxMin_) < Math.Abs(fxMax_)) {
                    xMin_ = enforceBounds_(xMin_ + growthFactor * (xMin_ - xMax_));
                    fxMin_ = f.value(xMin_);
                } else if (Math.Abs(fxMin_) > Math.Abs(fxMax_)) {
                    xMax_ = enforceBounds_(xMax_ + growthFactor * (xMax_ - xMin_));
                    fxMax_ = f.value(xMax_);
                } else if (flipflop == -1) {
                    xMin_ = enforceBounds_(xMin_ + growthFactor * (xMin_ - xMax_));
                    fxMin_ = f.value(xMin_);
                    evaluationNumber_++;
                    flipflop = 1;
                } else if (flipflop == 1) {
                    xMax_ = enforceBounds_(xMax_ + growthFactor * (xMax_ - xMin_));
                    fxMax_ = f.value(xMax_);
                    flipflop = -1;
                }
                evaluationNumber_++;
            }

            throw new ArgumentException("unable to bracket root in " + maxEvaluations_
                + " function evaluations (last bracket attempt: " + "f[" + xMin_ + "," + xMax_ + "] "
                + "-> [" + fxMin_ + "," + fxMax_ + "])");
        }

        /*! This method returns the zero of the function \f$ f \f$, determined with the given accuracy \f$ \epsilon \f$;
            depending on the particular solver, this might mean that the returned \f$ x \f$ is such that \f$ |f(x)| < \epsilon
            \f$, or that \f$ |x-\xi| < \epsilon \f$ where \f$ \xi \f$ is the real zero.

            An initial guess must be supplied, as well as two values \f$ x_\mathrm{min} \f$ and \f$ x_\mathrm{max} \f$ which
            must bracket the zero (i.e., either \f$ f(x_\mathrm{min}) \leq 0 \leq f(x_\mathrm{max}) \f$, or \f$
            f(x_\mathrm{max}) \leq 0 \leq f(x_\mathrm{min}) \f$ must be true).
        */
        public double solve(ISolver1d f, double accuracy, double guess, double xMin, double xMax) {

            if (accuracy <= 0.0)
                throw new ArgumentException("accuracy (" + accuracy + ") must be positive");

            // check whether we really want to use epsilon
            accuracy = Math.Max(accuracy, Const.QL_Epsilon);

            xMin_ = xMin;
            xMax_ = xMax;

            if (!(xMin_ < xMax_))
                throw new ArgumentException("invalid range: xMin_ (" + xMin_ + ") >= xMax_ (" + xMax_ + ")");
            if (!(!lowerBoundEnforced_ || xMin_ >= lowerBound_))
                throw new ArgumentException("xMin_ (" + xMin_ + ") < enforced low bound (" + lowerBound_ + ")");
            if (!(!upperBoundEnforced_ || xMax_ <= upperBound_))
                throw new ArgumentException("xMax_ (" + xMax_ + ") > enforced hi bound (" + upperBound_ + ")");

            fxMin_ = f.value(xMin_);
            if (fxMin_ == 0.0) return xMin_;

            fxMax_ = f.value(xMax_);
            if (fxMax_ == 0.0) return xMax_;

            evaluationNumber_ = 2;

            if (!(fxMin_ * fxMax_ < 0.0))
                throw new ArgumentException("root not bracketed: f[" + xMin_ + "," + xMax_ + "] -> [" + fxMin_ + "," + fxMax_ + "]");
            if (!(guess > xMin_))
                throw new ArgumentException("guess (" + guess + ") < xMin_ (" + xMin_ + ")");
            if (!(guess < xMax_))
                throw new ArgumentException("guess (" + guess + ") > xMax_ (" + xMax_ + ")");

            root_ = guess;

            return solveImpl(f, accuracy);
        }

        /*! This method sets the maximum number of function evaluations for the bracketing routine. An error is thrown
            if a bracket is not found after this number of evaluations.
        */
        public void setMaxEvaluations(int evaluations) {
            maxEvaluations_ = evaluations;
        }

        //! sets the lower bound for the function domain
        public void setLowerBound(double lowerBound) {
            lowerBound_ = lowerBound;
            lowerBoundEnforced_ = true;
        }

        //! sets the upper bound for the function domain
        public void setUpperBound(double upperBound) {
            upperBound_ = upperBound;
            upperBoundEnforced_ = true;
        }

        private double enforceBounds_(double x) {
            if (lowerBoundEnforced_ && x < lowerBound_) return lowerBound_;
            if (upperBoundEnforced_ && x > upperBound_) return upperBound_;
            return x;
        }

        protected abstract double solveImpl(ISolver1d f, double xAccuracy);
    }
}
