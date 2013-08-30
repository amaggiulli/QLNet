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

    //! Analytic formula for American exercise payoff at-expiry options
    //! \todo calculate greeks 
    public class AmericanPayoffAtExpiry {
        private double spot_;
        private double discount_;
        private double dividendDiscount_;
        private double variance_;

        private double forward_;
        private double stdDev_;

        private double strike_;
        private double K_;
        //private double DKDstrike_;

        private double mu_;
        private double log_H_S_;

        private double D1_;
        private double D2_;

        private double alpha_;
        private double beta_;
        private double DalphaDd1_;
        private double DbetaDd2_;

        private bool inTheMoney_;
        private double Y_;
        //private double DYDstrike_;
        private double X_;
        //private double DXDstrike_;

        public AmericanPayoffAtExpiry(double spot, double discount, double dividendDiscount, double variance, StrikedTypePayoff payoff) {
            spot_ = spot;
            discount_ = discount;
            dividendDiscount_ = dividendDiscount;
            variance_ = variance;

            if (!(spot_ > 0.0))
                throw new ApplicationException("positive spot_ value required");

            forward_ = spot_ * dividendDiscount_ / discount_;

            if (!(discount_ > 0.0))
                throw new ApplicationException("positive discount required");

            if (!(dividendDiscount_ > 0.0))
                throw new ApplicationException("positive dividend discount_ required");

            if (!(variance_ >= 0.0))
                throw new ApplicationException("negative variance_ not allowed");

            stdDev_ = Math.Sqrt(variance_);

            Option.Type type = payoff.optionType();
            strike_ = payoff.strike();


            mu_ = Math.Log(dividendDiscount_ / discount_) / variance_ - 0.5;

            // binary cash-or-nothing payoff?
            CashOrNothingPayoff coo = payoff as CashOrNothingPayoff;
            if (coo != null) {
                K_ = coo.cashPayoff();
                //DKDstrike_ = 0.0;
            }

            // binary asset-or-nothing payoff?
            AssetOrNothingPayoff aoo = payoff as AssetOrNothingPayoff;
            if (aoo != null) {
                K_ = forward_;
                //DKDstrike_ = 0.0;
                mu_ += 1.0;
            }


            log_H_S_ = Math.Log(strike_ / spot_);

            double n_d1;
            double n_d2;
            double cum_d1_;
            double cum_d2_;
            if (variance_ >= Const.QL_Epsilon) {
                D1_ = log_H_S_ / stdDev_ + mu_ * stdDev_;
                D2_ = D1_ - 2.0 * mu_ * stdDev_;
                CumulativeNormalDistribution f = new CumulativeNormalDistribution();
                cum_d1_ = f.value(D1_);
                cum_d2_ = f.value(D2_);
                n_d1 = f.derivative(D1_);
                n_d2 = f.derivative(D2_);
            } else {
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
                        alpha_ = 1.0 - cum_d2_; // N(-d2)
                        DalphaDd1_ = -n_d2; // -n( d2)
                        beta_ = 1.0 - cum_d1_; // N(-d1)
                        DbetaDd2_ = -n_d1; // -n( d1)
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
                        alpha_ = cum_d2_; // N(d2)
                        DalphaDd1_ = n_d2; // n(d2)
                        beta_ = cum_d1_; // N(d1)
                        DbetaDd2_ = n_d1; // n(d1)
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


            inTheMoney_ = (type == Option.Type.Call && strike_ < spot_) || (type == Option.Type.Put && strike_ > spot_);
            if (inTheMoney_) {
                Y_ = 1.0;
                X_ = 1.0;
                //DYDstrike_ = 0.0;
                //DXDstrike_ = 0.0;
            } else {
                Y_ = 1.0;
                X_ = Math.Pow((double)(strike_ / spot_), (double)(2.0 * mu_));
                //            DXDstrike_ = ......;
            }

        }

        public double value() {
            return discount_ * K_ * (Y_ * alpha_ + X_ * beta_);
        }
    }
}