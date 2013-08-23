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
    //! Base class for options on a single asset
    public class OneAssetOption : Option {
        // results
        protected double? delta_, deltaForward_, elasticity_, gamma_, theta_,
            thetaPerDay_, vega_, rho_, dividendRho_, strikeSensitivity_,
            itmCashProbability_;

        public OneAssetOption(Payoff payoff, Exercise exercise) : base(payoff, exercise) {}

        public override bool isExpired() { return exercise_.lastDate() < Settings.evaluationDate(); }

        public double delta() {
            calculate();
            if (delta_ == null) throw new ApplicationException("delta not provided");
            return delta_.GetValueOrDefault();
        }

        public double deltaForward() {
            calculate();
            if (deltaForward_ == null) throw new ApplicationException("forward delta not provided");
            return deltaForward_.GetValueOrDefault();
        }

        public double elasticity() {
            calculate();
            if (elasticity_ == null) throw new ApplicationException("elasticity not provided");
            return elasticity_.GetValueOrDefault();
        }

        public double gamma() {
            calculate();
            if (gamma_ == null) throw new ApplicationException("gamma not provided");
            return gamma_.GetValueOrDefault();
        }

        public double theta() {
            calculate();
            if (theta_ == null) throw new ApplicationException("theta not provided");
            return theta_.GetValueOrDefault();
        }

        public double thetaPerDay() {
            calculate();
            if (thetaPerDay_ == null) throw new ApplicationException("theta per-day not provided");
            return thetaPerDay_.GetValueOrDefault();
        }

        public double vega() {
            calculate();
            if (vega_ == null) throw new ApplicationException("vega not provided");
            return vega_.GetValueOrDefault();
        }

        public double rho() {
            calculate();
            if (rho_ == null) throw new ApplicationException("rho not provided");
            return rho_.GetValueOrDefault();
        }

        public double dividendRho() {
            calculate();
            if (dividendRho_ == null) throw new ApplicationException("dividend rho not provided");
            return dividendRho_.GetValueOrDefault();
        }

        public double strikeSensitivity() {
            calculate();
            if (strikeSensitivity_ == null) throw new ApplicationException("strike sensitivity not provided");
            return strikeSensitivity_.GetValueOrDefault();
        }

        public double itmCashProbability() {
            calculate();
            if (itmCashProbability_ == null) throw new ApplicationException("in-the-money cash probability not provided");
            return itmCashProbability_.GetValueOrDefault();
        }

        protected override void setupExpired() {
            base.setupExpired();
            delta_ = deltaForward_ = elasticity_ = gamma_ = theta_ = thetaPerDay_ = vega_ = rho_ = dividendRho_ =
                strikeSensitivity_ = itmCashProbability_ = 0.0;
        }

        public override void fetchResults(IPricingEngineResults r) {
            base.fetchResults(r);

            Results results = r as Results;
            if (results == null)
                throw new ApplicationException("no greeks returned from pricing engine");
            /* no check on null values - just copy.
               this allows:
               a) to decide in derived options what to do when null
               results are returned (throw? numerical calculation?)
               b) to implement slim engines which only calculate the
               value---of course care must be taken not to call
               the greeks methods when using these.
            */
            delta_          = results.delta;
            gamma_          = results.gamma;
            theta_          = results.theta;
            vega_           = results.vega;
            rho_            = results.rho;
            dividendRho_    = results.dividendRho;

            // QL_ENSURE(moreResults != 0, "no more greeks returned from pricing engine");
            /* no check on null values - just copy.
               this allows:
               a) to decide in derived options what to do when null
               results are returned (throw? numerical calculation?)
               b) to implement slim engines which only calculate the
               value---of course care must be taken not to call
               the greeks methods when using these.
            */
            deltaForward_       = results.deltaForward;
            elasticity_         = results.elasticity;
            thetaPerDay_        = results.thetaPerDay;
            strikeSensitivity_  = results.strikeSensitivity;
            itmCashProbability_ = results.itmCashProbability;
        }


        //! %Results from single-asset option calculation
        new public class Results : Instrument.Results {
            public double? delta, gamma, theta, vega, rho, dividendRho;
            public double? itmCashProbability, deltaForward, elasticity, thetaPerDay, strikeSensitivity;

            public override void reset() {
                base.reset();
                // Greeks::reset();
                delta = gamma = theta = vega = rho = dividendRho = null;
                // MoreGreeks::reset();
                itmCashProbability = deltaForward = elasticity = thetaPerDay = strikeSensitivity = null;
            }
        }

        public class Engine : GenericEngine<OneAssetOption.Arguments, OneAssetOption.Results> {
        }
    }
}
