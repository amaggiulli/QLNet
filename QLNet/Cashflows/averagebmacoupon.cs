/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
  
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
    //! Average BMA coupon
    /*! %Coupon paying a BMA index, where the coupon rate is a
        weighted average of relevant fixings.

        The weighted average is computed based on the
        actual calendar days for which a given fixing is valid and
        contributing to the given interest period.

        Before weights are computed, the fixing schedule is adjusted
        for the index's fixing day gap. See rate() method for details.
    */
    public class AverageBMACoupon : FloatingRateCoupon {
        private Schedule fixingSchedule_;
        private int bmaCutoffDays = 0; // to be verified

        // double gearing = 1.0, double spread = 0.0, 
        // Date refPeriodStart = Date(), Date refPeriodEnd = Date(), DayCounter dayCounter = DayCounter());
        public AverageBMACoupon(double nominal, Date paymentDate, Date startDate, Date endDate, BMAIndex index,
                                double gearing, double spread, Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter)
            : base(nominal, paymentDate, startDate, endDate, index.fixingDays(), index, gearing, spread,
                         refPeriodStart, refPeriodEnd, dayCounter, false) {
            fixingSchedule_ = index.fixingSchedule(
                                index.fixingCalendar()
                                    .advance(startDate, new Period(-index.fixingDays() + bmaCutoffDays, TimeUnit.Days),
                                                   BusinessDayConvention.Preceding), endDate);
            setPricer(new AverageBMACouponPricer());
        }
        
        //! \name FloatingRateCoupon interface
        //@{
        //! not applicable here; use fixingDates() instead
        public override Date fixingDate() {
            throw new ApplicationException("no single fixing date for average-BMA coupon");
        }

        //! fixing dates of the rates to be averaged
        public List<Date> fixingDates() { return fixingSchedule_.dates(); }

        //! not applicable here; use indexFixings() instead
        public override double indexFixing() {
            throw new ApplicationException("no single fixing date for average-BMA coupon");
        }

        //! fixings of the underlying index to be averaged
        public List<double> indexFixings() { return fixingSchedule_.dates().Select(d => index_.fixing(d)).ToList(); }

        public override double convexityAdjustment() {
            throw new ApplicationException("not defined for average-BMA coupon");
        }
    }

    public class AverageBMACouponPricer : FloatingRateCouponPricer {
        private AverageBMACoupon coupon_;

        public override void initialize(FloatingRateCoupon coupon) {
            coupon_ = coupon as AverageBMACoupon;
            if (coupon_ == null)
                throw new ApplicationException("wrong coupon type");
        }

        public override double swapletRate() {
            List<Date> fixingDates = coupon_.fixingDates();
            InterestRateIndex index = coupon_.index();

            int cutoffDays = 0; // to be verified
            Date startDate = coupon_.accrualStartDate() - cutoffDays,
                 endDate = coupon_.accrualEndDate() - cutoffDays,
                 d1 = startDate,
                 d2 = startDate;

            if (!(fixingDates.Count > 0)) throw new ApplicationException("fixing date list empty");
            if (!(index.valueDate(fixingDates.First()) <= startDate))
                throw new ApplicationException("first fixing date valid after period start");
            if (!(index.valueDate(fixingDates.Last()) >= endDate))
                throw new ApplicationException("last fixing date valid before period end");

            double avgBMA = 0.0;
            int days = 0;
            for (int i=0; i<fixingDates.Count - 1; ++i) {
                Date valueDate = index.valueDate(fixingDates[i]);
                Date nextValueDate = index.valueDate(fixingDates[i+1]);

                if (fixingDates[i] >= endDate || valueDate >= endDate)
                    break;
                if (fixingDates[i+1] < startDate || nextValueDate <= startDate)
                    continue;

                d2 = Date.Min(nextValueDate, endDate);

                avgBMA += index.fixing(fixingDates[i]) * (d2 - d1);

                days += d2 - d1;
                d1 = d2;
            }
            avgBMA /= (endDate - startDate);

            if (!(days == endDate - startDate))
                throw new ApplicationException("averaging days " + days + " differ from " +
                      "interest days " + (endDate - startDate));

            return coupon_.gearing()*avgBMA + coupon_.spread();
        }

        public override double swapletPrice() {
            throw new ApplicationException("not available");
        }
        public override double capletPrice(double d) {
            throw new ApplicationException("not available");
        }
        public override double capletRate(double d) {
            throw new ApplicationException("not available");
        }
        public override double floorletPrice(double d) {
            throw new ApplicationException("not available");
        }
        public override double floorletRate(double d) {
            throw new ApplicationException("not available");
        }

        // recheck
        protected override double optionletPrice(Option.Type t, double d) {
            throw new ApplicationException("not available");
        }
    }

    //! helper class building a sequence of average BMA coupons
    public class AverageBMALeg : RateLegBase {
        private BMAIndex index_;
        private List<double> gearings_;
        private List<double> spreads_;

        public AverageBMALeg(Schedule schedule, BMAIndex index) {
            schedule_ = schedule;
            index_ = index;
            paymentAdjustment_ = BusinessDayConvention.Following;
        }

        public AverageBMALeg withPaymentDayCounter(DayCounter dayCounter) {
            paymentDayCounter_ = dayCounter;
            return this;
        }
        public AverageBMALeg withGearings(double gearing) {
            gearings_ = new List<double>() { gearing };
            return this;
        }
        public AverageBMALeg withGearings(List<double> gearings) {
            gearings_ = gearings;
            return this;
        }
        public AverageBMALeg withSpreads(double spread) {
            spreads_ = new List<double>() { spread };
            return this;
        }
        public AverageBMALeg withSpreads(List<double> spreads) {
            spreads_ = spreads;
            return this;
        }

        public override List<CashFlow> value() {
            if (notionals_.Count == 0)
                throw new ApplicationException("no notional given");

            List<CashFlow> cashflows = new List<CashFlow>();

            // the following is not always correct
            Calendar calendar = schedule_.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;

            int n = schedule_.Count-1;
            for (int i=0; i<n; ++i) {
                refStart = start = schedule_.date(i);
                refEnd   =   end = schedule_.date(i+1);
                paymentDate = calendar.adjust(end, paymentAdjustment_);
                if (i == 0 && !schedule_.isRegular(i+1))
                    refStart = calendar.adjust(end - schedule_.tenor(), paymentAdjustment_);
                if (i == n-1 && !schedule_.isRegular(i+1))
                    refEnd = calendar.adjust(start + schedule_.tenor(), paymentAdjustment_);

                cashflows.Add(new AverageBMACoupon(Utils.Get<double>(notionals_, i, notionals_.Last()),
                                                   paymentDate, start, end,
                                                   index_,
                                                   Utils.Get<double>(gearings_, i, 1.0),
                                                   Utils.Get<double>(spreads_, i, 0.0),
                                                   refStart, refEnd,
                                                   paymentDayCounter_));
            }

            return cashflows;
        }
    }
}
