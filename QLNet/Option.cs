/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet {
    //! base option class
    public class Option : Instrument {
        public enum Type { Put = -1, Call = 1 }

        // arguments
        protected Payoff payoff_;
        public Payoff payoff() { return payoff_; }

        protected Exercise exercise_;
        public Exercise exercise() { return exercise_; }

        public Option(Payoff payoff, Exercise exercise) {
            payoff_ = payoff;
            exercise_ = exercise;
        }

        public override void setupArguments(IPricingEngineArguments args) {
            Option.Arguments arguments = args as Option.Arguments;

            if (arguments == null)
                throw new ApplicationException("wrong argument type");

            arguments.payoff = payoff_;
            arguments.exercise = exercise_;
        }


        //! basic %option %arguments
        public class Arguments : IPricingEngineArguments {
            public Payoff payoff;
            public Exercise exercise;

           public virtual void validate() {
                if (payoff == null) throw new ApplicationException("no payoff given");
                if (exercise == null) throw new ApplicationException("no exercise given");
            }
        }
    }

    //! additional %option results
    // class Greeks
    // public double? delta, gamma, theta, vega, rho, dividendRho;
    // reset method should include the following line
    // delta = gamma = theta = vega = rho = dividendRho = null;

    //! more additional %option results
    // class MoreGreeks
    // public double? itmCashProbability, deltaForward, elasticity, thetaPerDay, strikeSensitivity;
    // reset method should include the following line
    // itmCashProbability = deltaForward = elasticity = thetaPerDay = strikeSensitivity = null;
}