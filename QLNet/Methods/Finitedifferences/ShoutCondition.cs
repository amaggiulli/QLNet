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
    //! Shout option condition
    /*! A shout option is an option where the holder has the right to
        lock in a minimum value for the payoff at one (shout) time
        during the option's life. The minimum value is the option's
        intrinsic value at the shout time.
    */
    public class ShoutCondition : CurveDependentStepCondition<Vector> {
        double resTime_;
        double rate_;
        double disc_;
        
        public ShoutCondition(Option.Type type, double strike, double resTime, double rate) 
            : base(type, strike) {
            resTime_ = resTime;
            rate_ = rate;
        }

        public ShoutCondition(Vector intrinsicValues, double resTime, double rate)
            : base(intrinsicValues) {
            resTime_ = resTime;
            rate_ = rate;
        }

        public void applyTo(Vector a, double t)  {
            disc_ = Math.Exp(-rate_ * (t - resTime_));
            base.applyTo(a, t);
        }
        
        protected override double applyToValue(double current, double intrinsic) {
            return Math.Max(current, disc_ * intrinsic );
        }
    }
}
