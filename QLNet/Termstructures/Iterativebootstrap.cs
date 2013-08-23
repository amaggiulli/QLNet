/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
    public interface IBootStrap {
        void setup(PiecewiseYieldCurve ts);
        void calculate();
    }

    //! Universal piecewise-term-structure boostrapper.
    public class IterativeBootstrap : IBootStrap {
        
        private bool validCurve_ = false;
        private PiecewiseYieldCurve ts_; // yes, it is a workaround

        public void setup(PiecewiseYieldCurve ts) {
            ts_ = ts;

            int n = ts_.instruments_.Count;
            if (!(n+1 >= ts_.interpolator_.requiredPoints))
                throw new ArgumentException("not enough instruments: " + n + " provided, " +
                       (ts_.interpolator_.requiredPoints-1) + " required");

            ts_.instruments_.ForEach(x => x.registerWith(ts_.update));
        }

        public void calculate() {

            //prepare instruments
            int n = ts_.instruments_.Count, i;

            // ensure rate helpers are sorted
            ts_.instruments_.Sort((x, y) => x.latestDate().CompareTo(y.latestDate()));

            // check that there is no instruments with the same maturity
            for (i = 1; i < n; ++i) {
                Date m1 = ts_.instruments_[i - 1].latestDate(),
                     m2 = ts_.instruments_[i].latestDate();
                if (m1 == m2) throw new ArgumentException("two instruments have the same maturity (" + m1 + ")");
            }

            // check that there is no instruments with invalid quote
            if ((i = ts_.instruments_.FindIndex(x => !x.quoteIsValid())) != -1)
                throw new ArgumentException("instrument " + i + " (maturity: " + ts_.instruments_[i].latestDate() +
                       ") has an invalid quote");

            // setup instruments and register with them
            ts_.instruments_.ForEach(x => x.setTermStructure(ts_));

            // calculate dates and times
            ts_.dates_ = new InitializedList<Date>(n + 1);
            ts_.times_ = new InitializedList<double>(n + 1);
            ts_.dates_[0] = ts_.initialDate(ts_);
            ts_.times_[0] = ts_.timeFromReference(ts_.dates_[0]);
            for (i = 0; i < n; ++i) {
                ts_.dates_[i + 1] = ts_.instruments_[i].latestDate();
                ts_.times_[i + 1] = ts_.timeFromReference(ts_.dates_[i + 1]);
            }

            // set initial guess only if the current curve cannot be used as guess
            if (validCurve_) {
                if (ts_.data_.Count != n + 1)
                    throw new ArgumentException("dimension mismatch: expected " + n + 1 + ", actual " + ts_.data_.Count);
            } else {
                ts_.data_ = new InitializedList<double>(n + 1);
                ts_.data_[0] = ts_.initialValue(ts_);
                for (i=0; i<n; ++i)
                    ts_.data_[i+1] = ts_.initialGuess();
            }

            Brent solver = new Brent();
            int maxIterations = ts_.maxIterations();

            for (int iteration=0; ; ++iteration) {
                List<double> previousData = ts_.data();
                // restart from the previous interpolation
                if (validCurve_) {
                    ts_.interpolation_ = ts_.interpolator_.interpolate(ts_.times_, ts_.times_.Count, ts_.data_);
                }
                for (i=1; i<n+1; ++i) {
                    // calculate guess before extending interpolation to ensure that any extrapolation is performed
                    // using the curve bootstrapped so far and no more
                    RateHelper instrument = ts_.instruments_[i-1];
                    double guess = 0;
                    if (validCurve_ || iteration>0) {
                        guess = ts_.data_[i];
                    } else if (i==1) {
                        guess = ts_.initialGuess();
                    } else {
                        // most traits extrapolate
                        guess = ts_.guess(ts_, ts_.dates_[i]);
                    }

                    // bracket
                    double min = ts_.minValueAfter(i, ts_.data_);
                    double max = ts_.maxValueAfter(i, ts_.data_);
                    if (guess <= min || guess >= max)
                        guess = (min + max) / 2.0;

                    if (!validCurve_ && iteration == 0) {
                        // extend interpolation a point at a time
                        try {
                            ts_.interpolation_ = ts_.interpolator_.interpolate(ts_.times_, i + 1, ts_.data_);
                        } catch {
                            if (!ts_.interpolator_.global)
                                throw; // no chance to fix it in a later iteration

                            // otherwise, if the target interpolation is not usable yet
                           ts_.interpolation_ = new Linear().interpolate(ts_.times_, i + 1, ts_.data_);
                        }
                    }

                    // required because we just changed the data
                    // is it really required?
                    ts_.interpolation_.update();

                    try {
                        var error = new BootstrapError(ts_, instrument, i);
                        double r = solver.solve(error, ts_.accuracy_, guess, min, max);
                        // redundant assignment (as it has been already performed by BootstrapError in solve procedure), but safe
                        ts_.data_[i] = r;
                    } catch (Exception e) {
                        validCurve_ = false;
                        throw new ArgumentException(" iteration: " + iteration + 1 +
                                "could not bootstrap the " + i + " instrument, maturity " + ts_.dates_[i] +
                                ": " + e.Message);
                    }
                }

                if (!ts_.interpolator_.global)
                    break;      // no need for convergence loop
                else if (!validCurve_ && iteration == 0) {
                    // ensure the target interpolation is used
                   ts_.interpolation_ = ts_.interpolator_.interpolate(ts_.times_, ts_.times_.Count, ts_.data_);
                    // at least one more iteration is needed to check convergence
                    continue;
                }

                // exit conditions
                double improvement = 0.0;
                for (i=1; i<n+1; ++i)
                    improvement = Math.Max(improvement, Math.Abs(ts_.data_[i]-previousData[i]));
                if (improvement<=ts_.accuracy_)  // convergence reached
                    break;

                if (!(iteration+1 < maxIterations))
                    throw new ArgumentException("convergence not reached after " + iteration+1 + " iterations; last improvement " +
                        improvement + ", required accuracy " + ts_.accuracy_);
            }
            validCurve_ = true;
        }
    }
}
