/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

    //! Integral of a one-dimensional function
    //    ! Given a number \f$ N \f$ of intervals, the integral of
    //        a function \f$ f \f$ between \f$ a \f$ and \f$ b \f$ is
    //        calculated by means of the trapezoid formula
    //        \f[
    //        \int_{a}^{b} f \mathrm{d}x =
    //        \frac{1}{2} f(x_{0}) + f(x_{1}) + f(x_{2}) + \dots
    //        + f(x_{N-1}) + \frac{1}{2} f(x_{N})
    //        \f]
    //        where \f$ x_0 = a \f$, \f$ x_N = b \f$, and
    //        \f$ x_i = a+i \Delta x \f$ with
    //        \f$ \Delta x = (b-a)/N \f$.
    //
    //        \test the correctness of the result is tested by checking it
    //              against known good values.
    //    
    public class SegmentIntegral : Integrator {
        private int intervals_;

        public SegmentIntegral(int intervals)
            : base(1, 1) {
            intervals_ = intervals;

            if (!(intervals > 0))
                throw new ApplicationException("at least 1 interval needed, 0 given");
        }

        // inline and template definitions
        protected override double integrate(Func<double, double> f, double a, double b) {
            double dx = (b - a) / intervals_;
            double sum = 0.5 * (f(a) + f(b));
            double end = b - 0.5 * dx;
            for (double x = a + dx; x < end; x += dx)
                sum += f(x);
            return sum * dx;
        }
    }

}
