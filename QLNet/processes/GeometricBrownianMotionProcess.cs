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
    //! Geometric brownian-motion process
    /*! This class describes the stochastic process governed by
        \f[
            dS(t, S)= \mu S dt + \sigma S dW_t.
        \f]

        \ingroup processes
    */
    public class GeometricBrownianMotionProcess : StochasticProcess1D {
        protected double initialValue_;
        protected double mue_;
        protected double sigma_;

        public GeometricBrownianMotionProcess(double initialValue, double mue, double sigma)
            : base(new EulerDiscretization()) {
            initialValue_ = initialValue;
            mue_ = mue;
            sigma_ = sigma;
        }

        public override double x0() {
            return initialValue_;
        }

        public override double drift(double t, double x) {
            return mue_ * x;
        }

        public override double diffusion(double t, double x) {
            return sigma_ * x;
        }
    }
}
