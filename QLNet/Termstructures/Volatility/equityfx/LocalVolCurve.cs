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
    //! Local volatility curve derived from a Black curve
    public class LocalVolCurve : LocalVolTermStructure {
        private Handle<BlackVarianceCurve> blackVarianceCurve_;

        public LocalVolCurve(Handle<BlackVarianceCurve> curve)
            : base(curve.link.businessDayConvention(), curve.link.dayCounter()) {
            blackVarianceCurve_ = curve;

            blackVarianceCurve_.registerWith(update);
        }

        //! \name TermStructure interface
        public override Date referenceDate() { return blackVarianceCurve_.link.referenceDate(); }
        public override Calendar calendar()  { return blackVarianceCurve_.link.calendar();   }
        public override DayCounter dayCounter() { return blackVarianceCurve_.link.dayCounter(); }
        public override Date maxDate() { return blackVarianceCurve_.link.maxDate(); }

        //! \name VolatilityTermStructure interface
        public override double minStrike() { return double.MinValue; }
        public override double maxStrike() { return double.MaxValue; }

        /*! The relation
        \f[
            \int_0^T \sigma_L^2(t)dt = \sigma_B^2 T
        \f]
        holds, where \f$ \sigma_L(t) \f$ is the local volatility at
        time \f$ t \f$ and \f$ \sigma_B(T) \f$ is the Black
        volatility for maturity \f$ T \f$. From the above, the formula
        \f[
            \sigma_L(t) = \sqrt{\frac{\mathrm{d}}{\mathrm{d}t}\sigma_B^2(t)t}
        \f]
        can be deduced which is here implemented. */
        protected override double localVolImpl(double t, double dummy) {

            double dt = (1.0/365.0);
            double var1 = blackVarianceCurve_.link.blackVariance(t, dummy, true);
            double var2 = blackVarianceCurve_.link.blackVariance(t+dt, dummy, true);
            double derivative = (var2-var1)/dt;
            return Math.Sqrt(derivative);
        }
    }
}
