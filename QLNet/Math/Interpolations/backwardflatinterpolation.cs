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
    public class BackwardFlatInterpolationImpl : Interpolation.templateImpl {
        private List<double> primitive_;

        public BackwardFlatInterpolationImpl(List<double> xBegin, int size, List<double> yBegin) : base(xBegin, size, yBegin) {
            primitive_ = new InitializedList<double>(size_);
        }

        public override void update() {
            primitive_[0] = 0.0;
            for (int i=1; i<size_; i++) {
                double dx = xBegin_[i]-xBegin_[i-1];
                primitive_[i] = primitive_[i-1] + dx*yBegin_[i];
            }
        }

        public override double value(double x) {
            if (x <= xBegin_[0])
                return yBegin_[0];
            int i = locate(x);
            if (x == xBegin_[i])
                return yBegin_[i];
            else
                return yBegin_[i+1];
        }

        public override double primitive(double x) {
            int i = locate(x);
            double dx = x-xBegin_[i];
            return primitive_[i] + dx*yBegin_[i+1];
        }

        public override double derivative(double x) { return 0.0; }
        public override double secondDerivative(double x) { return 0.0; }
    }


    //! Backward-flat interpolation between discrete points
    public class BackwardFlatInterpolation : Interpolation {
        /*! \pre the \f$ x \f$ values must be sorted. */
        public BackwardFlatInterpolation(List<double> xBegin, int size, List<double> yBegin) {
            impl_ = new BackwardFlatInterpolationImpl(xBegin, size, yBegin);
            impl_.update();
        }
    }

    //! Backward-flat interpolation factory and traits
    public class BackwardFlat : IInterpolationFactory {
        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) {
			  return new BackwardFlatInterpolation( xBegin, size, yBegin );
        }
        public bool global { get { return false; } }
        public int requiredPoints { get { return 2; } }
    }
}
