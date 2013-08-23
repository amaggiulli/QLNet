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
    //! Integral of a one-dimensional function
    /*! \test the correctness of the result is tested by checking it
              against known good values.
    */
    public class SimpsonIntegral : TrapezoidIntegral<Default> {
        public SimpsonIntegral(double accuracy, int maxIterations) : base(accuracy, maxIterations) { }

        protected override double integrate (Func<double,double> f, double a, double b) {
            // start from the coarsest trapezoid...
            int N = 1;
            double I = (f(a)+f(b))*(b-a)/2.0, newI;
            double adjI = I, newAdjI;
            // ...and refine it
            int i = 1;

            IIntegrationPolicy ip = new Default();
            do {
                newI = ip.integrate(f, a, b, I, N);
                N *= 2;
                newAdjI = (4.0*newI-I)/3.0;
                // good enough? Also, don't run away immediately
                if (Math.Abs(adjI-newAdjI) <= absoluteAccuracy() && i > 5)
                    // ok, exit
                    return newAdjI;
                // oh well. Another step.
                I = newI;
                adjI = newAdjI;
                i++;
            } while (i < maxEvaluations());
        throw new ApplicationException("max number of iterations reached");
        }
    }
}
