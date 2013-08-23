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
    //! swap paying Libor against BMA coupons
    public class BMASwap : Swap {
        public enum Type { Receiver = -1, Payer = 1 };
        
        private Type type_;
        public Type type() { return type_; }

        private double nominal_;
        public double nominal() { return nominal_; }

        private double liborFraction_;
        public double liborFraction() { return liborFraction_; }

        private double liborSpread_;
        public double liborSpread() { return liborSpread_; }


        public BMASwap(Type type, double nominal,
                // Libor leg
                Schedule liborSchedule, double liborFraction, double liborSpread, IborIndex liborIndex, DayCounter liborDayCount,
                // BMA leg
                Schedule bmaSchedule, BMAIndex bmaIndex, DayCounter bmaDayCount)
            : base(2) {
            type_ = type;
            nominal_ = nominal;
            liborFraction_ = liborFraction;
            liborSpread_ = liborSpread;

            BusinessDayConvention convention = liborSchedule.businessDayConvention();

            legs_[0] = new IborLeg(liborSchedule, liborIndex)
                        .withPaymentDayCounter(liborDayCount)
                        .withFixingDays(liborIndex.fixingDays())
                        .withGearings(liborFraction)
                        .withSpreads(liborSpread)
                        .withNotionals(nominal)
                        .withPaymentAdjustment(convention);

            legs_[1] = new AverageBMALeg(bmaSchedule, bmaIndex)
                        .withPaymentDayCounter(bmaDayCount)
                        .withNotionals(nominal)
                        .withPaymentAdjustment(bmaSchedule.businessDayConvention());

            for (int j=0; j<2; ++j) {
                for (int i=0; i<legs_[j].Count; i++)
                    legs_[j][i].registerWith(update);
            }

            switch (type_) {
                case Type.Payer:
                    payer_[0] = +1.0;
                    payer_[1] = -1.0;
                    break;
                case Type.Receiver:
                    payer_[0] = -1.0;
                    payer_[1] = +1.0;
                    break;
                default:
                    throw new ApplicationException("Unknown BMA-swap type");
            }
        }


        public List<CashFlow> liborLeg() { return legs_[0]; }
        public List<CashFlow> bmaLeg() { return legs_[1]; }

        public double liborLegBPS() {
            calculate();
            if (legBPS_[0] == null)
                throw new ApplicationException("result not available");
            return legBPS_[0].GetValueOrDefault();
        }

        public double liborLegNPV() {
            calculate();
            if (legNPV_[0] == null)
                throw new ApplicationException("result not available");
            return legNPV_[0].GetValueOrDefault();
        }

        public double fairLiborFraction() {
            const double basisPoint = 1.0e-4;

            double spreadNPV = (liborSpread_/basisPoint)*liborLegBPS();
            double pureLiborNPV = liborLegNPV() - spreadNPV;
            if (pureLiborNPV == 0.0)
                throw new ApplicationException("result not available (null libor NPV)");
            return -liborFraction_ * (bmaLegNPV() + spreadNPV) / pureLiborNPV;
        }

        public double fairLiborSpread() {
            const double basisPoint = 1.0e-4;
            return liborSpread_ - NPV()/(liborLegBPS()/basisPoint);
        }

        public double bmaLegBPS() {
            calculate();
            if (legBPS_[1] == null)
                throw new ApplicationException("result not available");
            return legBPS_[1].GetValueOrDefault();
        }
        
        public double bmaLegNPV() {
            calculate();
            if (legNPV_[1] == null)
                throw new ApplicationException("result not available");
            return legNPV_[1].GetValueOrDefault();
        }
    }
}
