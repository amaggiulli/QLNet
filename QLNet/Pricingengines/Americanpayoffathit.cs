/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
  
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

    //! Analytic formula for American exercise payoff at-hit options
    //! \todo calculate greeks 
    public class AmericanPayoffAtHit {
        private double spot_;
        private double discount_;
        private double dividendDiscount_;
        private double variance_;
        private double stdDev_;

        private double strike_;
        private double K_;
        //private double DKDstrike_;

        private double mu_;
        private double lambda_;
        private double muPlusLambda_;
        private double muMinusLambda_;
        private double log_H_S_;

        private double D1_;
        private double D2_;

        private double alpha_;
        private double beta_;
        private double DalphaDd1_;
        private double DbetaDd2_;

        private bool inTheMoney_;
        private double forward_;
        private double X_;
        //private double DXDstrike_;

        public AmericanPayoffAtHit(double spot, double discount, double dividendDiscount, double variance, StrikedTypePayoff payoff) {
            spot_ = spot;
            discount_ = discount;
            dividendDiscount_ = dividendDiscount;
            variance_ = variance;

            if (!(spot_ > 0.0))
                throw new ApplicationException("positive spot value required");

            if (!(discount_ > 0.0))
                throw new ApplicationException("positive discount required");

            if (!(dividendDiscount_ > 0.0))
                throw new ApplicationException("positive dividend discount required");

            if (!(variance_ >= 0.0))
                throw new ApplicationException("negative variance not allowed");

            stdDev_ = Math.Sqrt(variance_);

            Option.Type type = payoff.optionType();
            strike_ = payoff.strike();


            log_H_S_ = Math.Log(strike_ / spot_);

            double n_d1;
            double n_d2;
            double cum_d1_;
            double cum_d2_;
            if (variance_ >= Const.QL_Epsilon) {
                if (discount_ == 0.0 && dividendDiscount_ == 0.0) {
                    mu_ = -0.5;
                    lambda_ = 0.5;
                } else if (discount_ == 0.0) {
                    throw new ApplicationException("null discount not handled yet");
                } else {
                    mu_ = Math.Log(dividendDiscount_ / discount_) / variance_ - 0.5;
                    lambda_ = Math.Sqrt(mu_ * mu_ - 2.0 * Math.Log(discount_) / variance_);
                }
                D1_ = log_H_S_ / stdDev_ + lambda_ * stdDev_;
                D2_ = D1_ - 2.0 * lambda_ * stdDev_;
                CumulativeNormalDistribution f = new CumulativeNormalDistribution();
                cum_d1_ = f.value(D1_);
                cum_d2_ = f.value(D2_);
                n_d1 = f.derivative(D1_);
                n_d2 = f.derivative(D2_);
            } else {
                // not tested yet
                mu_ = Math.Log(dividendDiscount_ / discount_) / variance_ - 0.5;
                lambda_ = Math.Sqrt(mu_ * mu_ - 2.0 * Math.Log(discount_) / variance_);
                if (log_H_S_ > 0) {
                    cum_d1_ = 1.0;
                    cum_d2_ = 1.0;
                } else {
                    cum_d1_ = 0.0;
                    cum_d2_ = 0.0;
                }
                n_d1 = 0.0;
                n_d2 = 0.0;
            }


            switch (type) {
                // up-and-in cash-(at-hit)-or-nothing option
                // a.k.a. american call with cash-or-nothing payoff
                case Option.Type.Call:
                    if (strike_ > spot_) {
                        alpha_ = 1.0 - cum_d1_; // N(-d1)
                        DalphaDd1_ = -n_d1; // -n( d1)
                        beta_ = 1.0 - cum_d2_; // N(-d2)
                        DbetaDd2_ = -n_d2; // -n( d2)
                    } else {
                        alpha_ = 0.5;
                        DalphaDd1_ = 0.0;
                        beta_ = 0.5;
                        DbetaDd2_ = 0.0;
                    }
                    break;
                // down-and-in cash-(at-hit)-or-nothing option
                // a.k.a. american put with cash-or-nothing payoff
                case Option.Type.Put:
                    if (strike_ < spot_) {
                        alpha_ = cum_d1_; // N(d1)
                        DalphaDd1_ = n_d1; // n(d1)
                        beta_ = cum_d2_; // N(d2)
                        DbetaDd2_ = n_d2; // n(d2)
                    } else {
                        alpha_ = 0.5;
                        DalphaDd1_ = 0.0;
                        beta_ = 0.5;
                        DbetaDd2_ = 0.0;
                    }
                    break;
                default:
                    throw new ApplicationException("invalid option type");
            }


            muPlusLambda_ = mu_ + lambda_;
            muMinusLambda_ = mu_ - lambda_;
            inTheMoney_ = (type == Option.Type.Call && strike_ < spot_) || (type == Option.Type.Put && strike_ > spot_);

            if (inTheMoney_) {
                forward_ = 1.0;
                X_ = 1.0;
                //DXDstrike_ = 0.0;
            } else {
                forward_ = Math.Pow(strike_ / spot_, muPlusLambda_);
                X_ = Math.Pow(strike_ / spot_, muMinusLambda_);
                //            DXDstrike_ = ......;
            }


            // Binary Cash-Or-Nothing payoff?
            CashOrNothingPayoff coo = payoff as CashOrNothingPayoff;
            if (coo != null) {
                K_ = coo.cashPayoff();
                //DKDstrike_ = 0.0;
            }

            // Binary Asset-Or-Nothing payoff?
            AssetOrNothingPayoff aoo = payoff as AssetOrNothingPayoff;

            if (aoo != null) {
                if (inTheMoney_) {
                    K_ = spot_;
                    //DKDstrike_ = 0.0;
                } else {
                    K_ = aoo.strike();
                    //DKDstrike_ = 1.0;
                }
            }
        }

        // inline definitions
        public double value() {
            return K_ * (forward_ * alpha_ + X_ * beta_);
        }

        public double delta() {
            double tempDelta = -spot_ * stdDev_;
            double DalphaDs = DalphaDd1_ / tempDelta;
            double DbetaDs = DbetaDd2_ / tempDelta;

            double DforwardDs;
            double DXDs;
            if (inTheMoney_) {
                DforwardDs = 0.0;
                DXDs = 0.0;
            } else {
                DforwardDs = -muPlusLambda_ * forward_ / spot_;
                DXDs = -muMinusLambda_ * X_ / spot_;
            }

            return K_ * (DalphaDs * forward_ + alpha_ * DforwardDs + DbetaDs * X_ + beta_ * DXDs);
        }

        public double gamma() {
            double tempDelta = -spot_ * stdDev_;
            double DalphaDs = DalphaDd1_ / tempDelta;
            double DbetaDs = DbetaDd2_ / tempDelta;
            double D2alphaDs2 = -DalphaDs / spot_ * (1 - D1_ / stdDev_);
            double D2betaDs2 = -DbetaDs / spot_ * (1 - D2_ / stdDev_);

            double DforwardDs;
            double DXDs;
            double D2forwardDs2;
            double D2XDs2;
            if (inTheMoney_) {
                DforwardDs = 0.0;
                DXDs = 0.0;
                D2forwardDs2 = 0.0;
                D2XDs2 = 0.0;
            } else {
                DforwardDs = -muPlusLambda_ * forward_ / spot_;
                DXDs = -muMinusLambda_ * X_ / spot_;
                D2forwardDs2 = muPlusLambda_ * forward_ / (spot_ * spot_) * (1 + muPlusLambda_);
                D2XDs2 = muMinusLambda_ * X_ / (spot_ * spot_) * (1 + muMinusLambda_);
            }

            return K_ * (D2alphaDs2 * forward_ + DalphaDs * DforwardDs + DalphaDs * DforwardDs + alpha_ * D2forwardDs2 + D2betaDs2 * X_ + DbetaDs * DXDs + DbetaDs * DXDs + beta_ * D2XDs2);

        }

        public double rho(double maturity) {
            if (!(maturity >= 0.0))
                throw new ApplicationException("negative maturity not allowed");

            // actually D.Dr / T
            double DalphaDr = -DalphaDd1_ / (lambda_ * stdDev_) * (1.0 + mu_);
            double DbetaDr = DbetaDd2_ / (lambda_ * stdDev_) * (1.0 + mu_);
            double DforwardDr;
            double DXDr;
            if (inTheMoney_) {
                DforwardDr = 0.0;
                DXDr = 0.0;
            } else {
                DforwardDr = forward_ * (1.0 + (1.0 + mu_) / lambda_) * log_H_S_ / variance_;
                DXDr = X_ * (1.0 - (1.0 + mu_) / lambda_) * log_H_S_ / variance_;
            }

            return maturity * K_ * (DalphaDr * forward_ + alpha_ * DforwardDr + DbetaDr * X_ + beta_ * DXDr);
        }
    }

}
