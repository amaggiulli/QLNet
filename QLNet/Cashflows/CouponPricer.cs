/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com) 
 
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
using System.Reflection;

namespace QLNet {

    //! generic pricer for floating-rate coupons
    public abstract class FloatingRateCouponPricer : IObservable, IObserver {
        //! \name required interface
        //@{
        public abstract double swapletPrice();
        public abstract double swapletRate();
        public abstract double capletPrice(double effectiveCap);
        public abstract double capletRate(double effectiveCap);
        public abstract double floorletPrice(double effectiveFloor);
        public abstract double floorletRate(double effectiveFloor);
        public abstract void initialize(FloatingRateCoupon coupon);
        protected abstract double optionletPrice(Option.Type optionType, double effStrike);

        #region Observer & observable
        public event Callback notifyObserversEvent;
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers() {
            Callback handler = notifyObserversEvent;
            if (handler != null) {
                handler();
            }
        }

        // observer interface
        public void update() { notifyObservers(); }
        #endregion
    }

    //! base pricer for capped/floored Ibor coupons
    public abstract class IborCouponPricer : FloatingRateCouponPricer {
        public IborCouponPricer()
            : this(new Handle<OptionletVolatilityStructure>()) {
        }
        public IborCouponPricer(Handle<OptionletVolatilityStructure> v) {
            capletVol_ = v;
            if (!capletVol_.empty())
                capletVol_.registerWith(update);
        }

        public Handle<OptionletVolatilityStructure> capletVolatility() {
            return capletVol_;
        }
        public void setCapletVolatility() {
            setCapletVolatility(new Handle<OptionletVolatilityStructure>());
        }
        public void setCapletVolatility(Handle<OptionletVolatilityStructure> v) {
            capletVol_.unregisterWith(update);
            capletVol_ = v;
            if (!capletVol_.empty())
                capletVol_.registerWith(update);

            update();
        }
        private Handle<OptionletVolatilityStructure> capletVol_;
    }

    //! Black-formula pricer for capped/floored Ibor coupons
    public class BlackIborCouponPricer : IborCouponPricer {
        public BlackIborCouponPricer()
            : this(new Handle<OptionletVolatilityStructure>()) {
        }
        public BlackIborCouponPricer(Handle<OptionletVolatilityStructure> v)
            : base(v) {
        }

        //===========================================================================//
        //                              BlackIborCouponPricer                        //
        //===========================================================================//

        public override void initialize(FloatingRateCoupon coupon) {
            coupon_ = coupon as IborCoupon;
            if (coupon_ == null) throw new ApplicationException("Libor coupon required");
            gearing_ = coupon_.gearing();
            spread_ = coupon_.spread();
            Date paymentDate = coupon_.date();
            IborIndex index = coupon_.index() as IborIndex;
            Handle<YieldTermStructure> rateCurve = index.forwardingTermStructure();

            if (paymentDate > rateCurve.link.referenceDate())
                discount_ = rateCurve.link.discount(paymentDate);
            else
                discount_ = 1.0;

            spreadLegValue_ = spread_ * coupon_.accrualPeriod() * discount_;
        }
        // 
        public override double swapletPrice() {
            // past or future fixing is managed in InterestRateIndex::fixing()

            double swapletPrice = adjustedFixing() * coupon_.accrualPeriod() * discount_;
            return gearing_ * swapletPrice + spreadLegValue_;
        }
        public override double swapletRate() {
            return swapletPrice() / (coupon_.accrualPeriod() * discount_);
        }
        public override double capletPrice(double effectiveCap) {
            double capletPrice = optionletPrice(Option.Type.Call, effectiveCap);
            return gearing_ * capletPrice;
        }
        public override double capletRate(double effectiveCap) {
            return capletPrice(effectiveCap) / (coupon_.accrualPeriod() * discount_);
        }
        public override double floorletPrice(double effectiveFloor) {
            double floorletPrice = optionletPrice(Option.Type.Put, effectiveFloor);
            return gearing_ * floorletPrice;
        }
        public override double floorletRate(double effectiveFloor) {
            return floorletPrice(effectiveFloor) / (coupon_.accrualPeriod() * discount_);
        }

        protected override double optionletPrice(Option.Type optionType, double effStrike) {
            Date fixingDate = coupon_.fixingDate();
            if (fixingDate <= Settings.evaluationDate()) {
                // the amount is determined
                double a;
                double b;
                if (optionType == Option.Type.Call) {
                    a = coupon_.indexFixing();
                    b = effStrike;
                } else {
                    a = effStrike;
                    b = coupon_.indexFixing();
                }
                return Math.Max(a - b, 0.0) * coupon_.accrualPeriod() * discount_;
            } else {
                // not yet determined, use Black model
                if (!(!capletVolatility().empty()))
                    throw new ApplicationException("missing optionlet volatility");


                double stdDev = Math.Sqrt(capletVolatility().link.blackVariance(fixingDate, effStrike));
                double fixing = Utils.blackFormula(optionType, effStrike, adjustedFixing(), stdDev);
                return fixing * coupon_.accrualPeriod() * discount_;
            }
        }

        protected double adjustedFixing() { return adjustedFixing(null);  }
        protected virtual double adjustedFixing(double? fixing_) {

            double adjustement = 0.0;
            double fixing = (fixing_ == null) ? coupon_.indexFixing() : fixing_.GetValueOrDefault();

            if (!coupon_.isInArrears()) {
                adjustement = 0.0;
            } else {
                // see Hull, 4th ed., page 550
                if (!(!capletVolatility().empty()))
                    throw new ApplicationException("missing optionlet volatility");

                Date d1 = coupon_.fixingDate();
                Date referenceDate = capletVolatility().link.referenceDate();
                if (d1 <= referenceDate) {
                    adjustement = 0.0;
                } else {
                    Date d2 = coupon_.index().maturityDate(d1);
                    double tau = coupon_.index().dayCounter().yearFraction(d1, d2);
                    double variance = capletVolatility().link.blackVariance(d1, fixing);
                    adjustement = fixing * fixing * variance * tau / (1.0 + fixing * tau);
                }
            }
            return fixing + adjustement;
        }

        private IborCoupon coupon_;
        private double discount_;
        private double gearing_;
        private double spread_;
        private double spreadLegValue_;
    }

    //! base pricer for vanilla CMS coupons
    public abstract class CmsCouponPricer : FloatingRateCouponPricer {
       public CmsCouponPricer(Handle<SwaptionVolatilityStructure> v = null) {
          if (v.link == null)
             swaptionVol_ = new Handle<SwaptionVolatilityStructure>();
          else
            swaptionVol_ = v;

          swaptionVol_.registerWith(update);
        }

        public Handle<SwaptionVolatilityStructure> swaptionVolatility() {
            return swaptionVol_;
        }
        public void setSwaptionVolatility() {
            setSwaptionVolatility(new Handle<SwaptionVolatilityStructure>());
        }
        public void setSwaptionVolatility(Handle<SwaptionVolatilityStructure> v) {
            if (swaptionVol_ != null)
                swaptionVol_.unregisterWith(update);
            swaptionVol_ = v;
            if (swaptionVol_ != null)
                swaptionVol_.registerWith(update);
            update();
        }
        private Handle<SwaptionVolatilityStructure> swaptionVol_;
    }


    //===========================================================================//
    //                         CouponSelectorToSetPricer                         //
    //===========================================================================//

    public class PricerSetter : IAcyclicVisitor {
        private FloatingRateCouponPricer pricer_;
        public PricerSetter(FloatingRateCouponPricer pricer) {
            pricer_ = pricer;
        }

        public void visit(object o) {
            Type[] types = new Type[] { o.GetType() };
            MethodInfo methodInfo = this.GetType().GetMethod("visit", types);
            if (methodInfo != null) {
                methodInfo.Invoke(this, new object[] { o });
            }
        }

        public void visit(CashFlow c) {
            // nothing to do
        }
        public void visit(Coupon c) {
            // nothing to do
        }
        public void visit(IborCoupon c) {
            IborCouponPricer pricer = pricer_ as IborCouponPricer;
            if (pricer == null)
                throw new ApplicationException("pricer not compatible with Ibor coupon");
            c.setPricer(pricer);
        }
        public void visit(CappedFlooredIborCoupon c) {
            IborCouponPricer pricer = pricer_ as IborCouponPricer;
            if (pricer == null)
                throw new ApplicationException("pricer not compatible with Ibor coupon");
            c.setPricer(pricer);
        }
        public void visit(DigitalIborCoupon c) {
            IborCouponPricer pricer = pricer_ as IborCouponPricer;
            if (pricer == null)
                throw new ApplicationException("pricer not compatible with Ibor coupon");
            c.setPricer(pricer);
        }
        public void visit(CmsCoupon c) {
            CmsCouponPricer pricer = pricer_ as CmsCouponPricer;
            if (pricer == null)
                throw new ApplicationException("pricer not compatible with CMS coupon");
            c.setPricer(pricer);
        }
        public void visit(CappedFlooredCoupon c)
        {
           CmsCouponPricer pricer = pricer_ as CmsCouponPricer;
           if (pricer == null)
              throw new ApplicationException("pricer not compatible with CMS coupon");
           c.setPricer(pricer);
        }
        public void visit(CappedFlooredCmsCoupon c) {
            CmsCouponPricer pricer = pricer_ as CmsCouponPricer;
            if (pricer == null)
                throw new ApplicationException("pricer not compatible with CMS coupon");
            c.setPricer(pricer);
        }
        public void visit(DigitalCmsCoupon c) {
            CmsCouponPricer pricer = pricer_ as CmsCouponPricer;
            if (pricer == null)
                throw new ApplicationException("pricer not compatible with CMS coupon");
            c.setPricer(pricer);
        }
        //public void visit(RangeAccrualFloatersCoupon c)
        //{
        //    if (!(pricer_ is RangeAccrualPricer))
        //        throw new ApplicationException("pricer not compatible with range-accrual coupon");
        //    c.setPricer(pricer_ as RangeAccrualPricer);
        //}
    }

    partial class Utils {
        public static void setCouponPricer(List<CashFlow> leg, FloatingRateCouponPricer pricer) {
            PricerSetter setter = new PricerSetter(pricer);
            foreach (CashFlow cf in leg) {
                cf.accept(setter);
            }
        }

        public static void setCouponPricers(List<CashFlow> leg, List<FloatingRateCouponPricer> pricers) {
            throw new NotImplementedException();
            //int nCashFlows = leg.Count;
            //if (!(nCashFlows > 0))
            //    throw new ApplicationException("no cashflows");

            //int nPricers = pricers.Count;
            //if (!(nCashFlows >= nPricers))
            //    throw new ApplicationException("mismatch between leg size (" + nCashFlows + ") and number of pricers (" + nPricers + ")");

            //for (int i = 0; i < nCashFlows; ++i)
            //{
            //    PricerSetter[] setter = new PricerSetter[i](i < nPricers ? pricers : pricers[nPricers - 1]);
            //    leg[i].accept(setter);
            //}
        }
    }
}
