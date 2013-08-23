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
    /*! Given a target accuracy \f$ \epsilon \f$, the integral of
        a function \f$ f \f$ between \f$ a \f$ and \f$ b \f$ is
        calculated by means of the trapezoid formula
        \f[
        \int_{a}^{b} f \mathrm{d}x =
        \frac{1}{2} f(x_{0}) + f(x_{1}) + f(x_{2}) + \dots
        + f(x_{N-1}) + \frac{1}{2} f(x_{N})
        \f]
        where \f$ x_0 = a \f$, \f$ x_N = b \f$, and
        \f$ x_i = a+i \Delta x \f$ with
        \f$ \Delta x = (b-a)/N \f$. The number \f$ N \f$ of intervals
        is repeatedly increased until the target accuracy is reached.

        \test the correctness of the result is tested by checking it
              against known good values.
    */
    public class TrapezoidIntegral<IntegrationPolicy> : Integrator where IntegrationPolicy : IIntegrationPolicy, new() {
        public TrapezoidIntegral(double accuracy, int maxIterations) : base(accuracy, maxIterations){ }

        protected override double integrate (Func<double,double> f, double a, double b) {
            // start from the coarsest trapezoid...
            int N = 1;
            double I = (f(a)+f(b))*(b-a)/2.0, newI;
            // ...and refine it
            int i = 1;

            IntegrationPolicy ip = new IntegrationPolicy();
            do {
                newI = ip.integrate(f, a, b, I, N);
                N *= ip.nbEvalutions();
                // good enough? Also, don't run away immediately
                if (Math.Abs(I-newI) <= absoluteAccuracy() && i > 5)
                    // ok, exit
                    return newI;
                // oh well. Another step.
                I = newI;
                i++;
            } while (i < maxEvaluations());
            throw new ApplicationException("max number of iterations reached");
        }
    }

    public interface IIntegrationPolicy {
        double integrate(Func<double, double> f, double a, double b, double I, int N);
        int nbEvalutions();
    }

    // Integration policies
    public struct Default : IIntegrationPolicy {
        public double integrate(Func<double,double> f, double a, double b, double I, int N) {
            double sum = 0.0;
            double dx = (b-a)/N;
            double x = a + dx/2.0;
            for (int i=0; i<N; x += dx, ++i)
                sum += f(x);
            return (I + dx*sum)/2.0;
        }
        public int nbEvalutions() { return 2;}
    }

    public struct MidPoint : IIntegrationPolicy {
        public double integrate(Func<double,double> f, double a, double b, double I, int N) {
            double sum = 0.0;
            double dx = (b - a) / N;
            double x = a + dx / 6.0;
            double D = 2.0 * dx / 3.0;
            for (int i=0; i<N; x += dx, ++i)
                sum += f(x) + f(x+D);
            return (I + dx*sum)/3.0;
        }

        public int nbEvalutions() { return 3; }
    }

}
