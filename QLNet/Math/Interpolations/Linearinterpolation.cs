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
    /* linear interpolation between discrete points */
    public class LinearInterpolationImpl : Interpolation.templateImpl {
        private InitializedList<double> primitiveConst_, s_;

        public LinearInterpolationImpl(List<double> xBegin, int size, List<double> yBegin)
            : base(xBegin, size, yBegin) {
           primitiveConst_ = new InitializedList<double>(size_);
           s_ = new InitializedList<double>(size_);
        }

        public override void update() {
            primitiveConst_[0] = 0.0;
            for (int i = 1; i < size_; ++i) {
                double dx = xBegin_[i] - xBegin_[i - 1];
                s_[i - 1] = (yBegin_[i] - yBegin_[i - 1]) / dx;
                primitiveConst_[i] = primitiveConst_[i - 1] + dx * (yBegin_[i - 1] + 0.5 * dx * s_[i - 1]);
            }
        }

        public override double value(double x) {
            int i = locate(x);
            double result = yBegin_[i] + (x - xBegin_[i]) * s_[i];
            return result;
        }
        public override double primitive(double x) {
            int i = locate(x);
            double dx = x - xBegin_[i];
            return primitiveConst_[i] + dx * (yBegin_[i] + 0.5 * dx * s_[i]);
        }
        public override double derivative(double x) {
            int i = locate(x);
            return s_[i];
        }
        public override double secondDerivative(double x) { return 0.0; }
    }

    //! %Linear interpolation between discrete points
    public class LinearInterpolation : Interpolation {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public LinearInterpolation(List<double> xBegin, int size, List<double> yBegin) {
            impl_ = new LinearInterpolationImpl(xBegin, size, yBegin);
            impl_.update();
        }
    };

    //! %Linear-interpolation factory and traits
    public class Linear : IInterpolationFactory {
        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) {
            return new LinearInterpolation(xBegin, size, yBegin);
        }
        public bool global { get { return false; } }
        public int requiredPoints { get { return 2; } }
    };
}
