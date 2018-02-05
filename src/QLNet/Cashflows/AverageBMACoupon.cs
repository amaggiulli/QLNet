/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// Average BMA coupon
   /// <para>Coupon paying a BMA index, where the coupon rate is a
   /// weighted average of relevant fixings.</para>
   /// </summary>
   /// <remarks>
   /// The weighted average is computed based on the
   /// actual calendar days for which a given fixing is valid and
   /// contributing to the given interest period.
   ///
   /// Before weights are computed, the fixing schedule is adjusted
   /// for the index's fixing day gap. See rate() method for details.
   /// </remarks>
   public class AverageBMACoupon : FloatingRateCoupon
   {

      public AverageBMACoupon(Date paymentDate,
                              double nominal,
                              Date startDate,
                              Date endDate,
                              BMAIndex index,
                              double gearing = 1.0,
                              double spread = 0.0,
                              Date refPeriodStart = null,
                              Date refPeriodEnd = null,
                              DayCounter dayCounter = null)
         : base(paymentDate, nominal, startDate, endDate, index.fixingDays(), index, gearing, spread,
                refPeriodStart, refPeriodEnd, dayCounter)
      {
         fixingSchedule_ = index.fixingSchedule(
                              index.fixingCalendar()
                              .advance(startDate, new Period(-index.fixingDays(), TimeUnit.Days),
                                       BusinessDayConvention.Preceding), endDate);
         setPricer(new AverageBMACouponPricer());
      }

      /// <summary>
      /// Get the fixing date
      /// </summary>
      /// <remarks>FloatingRateCoupon interface not applicable here; use <c>fixingDates()</c> instead
      /// </remarks>
      public override Date fixingDate()
      {
         Utils.QL_FAIL("no single fixing date for average-BMA coupon");
         return null;
      }

      /// <summary>
      /// Get the fixing dates of the rates to be averaged
      /// </summary>
      /// <returns>A list of dates</returns>
      public List<Date> fixingDates() { return fixingSchedule_.dates(); }

      /// <summary>
      /// not applicable here; use indexFixings() instead
      /// </summary>
      public override double indexFixing()
      {
         Utils.QL_FAIL("no single fixing for average-BMA coupon");
         return 0;
      }

      /// <summary>
      /// fixings of the underlying index to be averaged
      /// </summary>
      /// <returns>A list of double</returns>
      public List<double> indexFixings() { return fixingSchedule_.dates().Select(d => index_.fixing(d)).ToList(); }

      /// <summary>
      /// not applicable here
      /// </summary>
      public override double convexityAdjustment()
      {
         Utils.QL_FAIL("not defined for average-BMA coupon");
         return 0;
      }

      private Schedule fixingSchedule_;
   }

   public class AverageBMACouponPricer : FloatingRateCouponPricer
   {
      public override void initialize(FloatingRateCoupon coupon)
      {
         coupon_ = coupon as AverageBMACoupon;
         Utils.QL_REQUIRE(coupon_ != null, () => "wrong coupon type");
      }

      public override double swapletRate()
      {
         List<Date> fixingDates = coupon_.fixingDates();
         InterestRateIndex index = coupon_.index();

         int cutoffDays = 0; // to be verified
         Date startDate = coupon_.accrualStartDate() - cutoffDays,
              endDate = coupon_.accrualEndDate() - cutoffDays,
              d1 = startDate;

         Utils.QL_REQUIRE(fixingDates.Count > 0, () => "fixing date list empty");
         Utils.QL_REQUIRE(index.valueDate(fixingDates.First()) <= startDate, () => "first fixing date valid after period start");
         Utils.QL_REQUIRE(index.valueDate(fixingDates.Last()) >= endDate, () => "last fixing date valid before period end");

         double avgBMA = 0.0;
         int days = 0;
         for (int i = 0; i < fixingDates.Count - 1; ++i)
         {
            Date valueDate = index.valueDate(fixingDates[i]);
            Date nextValueDate = index.valueDate(fixingDates[i + 1]);

            if (fixingDates[i] >= endDate || valueDate >= endDate)
               break;
            if (fixingDates[i + 1] < startDate || nextValueDate <= startDate)
               continue;

            Date d2 = Date.Min(nextValueDate, endDate);

            avgBMA += index.fixing(fixingDates[i]) * (d2 - d1);

            days += d2 - d1;
            d1 = d2;
         }
         avgBMA /= (endDate - startDate);

         Utils.QL_REQUIRE(days == endDate - startDate, () =>
                          "averaging days " + days + " differ from " + "interest days " + (endDate - startDate));

         return coupon_.gearing() * avgBMA + coupon_.spread();
      }

      /// <summary>
      /// not applicable here
      /// </summary>
      public override double swapletPrice()
      {
         Utils.QL_FAIL("not available");
         return 0;
      }

      /// <summary>
      /// not applicable here
      /// </summary>
      public override double capletPrice(double d)
      {
         Utils.QL_FAIL("not available");
         return 0;
      }

      /// <summary>
      /// not applicable here
      /// </summary>
      public override double capletRate(double d)
      {
         Utils.QL_FAIL("not available");
         return 0;
      }

      /// <summary>
      /// not applicable here
      /// </summary>
      public override double floorletPrice(double d)
      {
         Utils.QL_FAIL("not available");
         return 0;
      }

      /// <summary>
      /// not applicable here
      /// </summary>
      public override double floorletRate(double d)
      {
         Utils.QL_FAIL("not available");
         return 0;
      }

      // recheck
      //protected override double optionletPrice( Option.Type t, double d )
      //{
      //   throw new Exception( "not available" );
      //}

      private AverageBMACoupon coupon_;
   }

   /// <summary>
   /// Helper class building a sequence of average BMA coupons
   /// </summary>
   public class AverageBMALeg : RateLegBase
   {
      private BMAIndex index_;
      private List<double> gearings_;
      private List<double> spreads_;

      public AverageBMALeg(Schedule schedule, BMAIndex index)
      {
         schedule_ = schedule;
         index_ = index;
         paymentAdjustment_ = BusinessDayConvention.Following;
      }

      public AverageBMALeg withPaymentDayCounter(DayCounter dayCounter)
      {
         paymentDayCounter_ = dayCounter;
         return this;
      }
      public AverageBMALeg withGearings(double gearing)
      {
         gearings_ = new List<double>() { gearing };
         return this;
      }
      public AverageBMALeg withGearings(List<double> gearings)
      {
         gearings_ = gearings;
         return this;
      }
      public AverageBMALeg withSpreads(double spread)
      {
         spreads_ = new List<double>() { spread };
         return this;
      }
      public AverageBMALeg withSpreads(List<double> spreads)
      {
         spreads_ = spreads;
         return this;
      }

      public override List<CashFlow> value()
      {
         Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");

         List<CashFlow> cashflows = new List<CashFlow>();

         // the following is not always correct
         Calendar calendar = schedule_.calendar();

         Date refStart, start, refEnd, end;
         Date paymentDate;

         int n = schedule_.Count - 1;
         for (int i = 0; i < n; ++i)
         {
            refStart = start = schedule_.date(i);
            refEnd = end = schedule_.date(i + 1);
            paymentDate = calendar.adjust(end, paymentAdjustment_);
            if (i == 0 && !schedule_.isRegular(i + 1))
               refStart = calendar.adjust(end - schedule_.tenor(), paymentAdjustment_);
            if (i == n - 1 && !schedule_.isRegular(i + 1))
               refEnd = calendar.adjust(start + schedule_.tenor(), paymentAdjustment_);

            cashflows.Add(new AverageBMACoupon(paymentDate,
                                               Utils.Get(notionals_, i, notionals_.Last()),
                                               start, end,
                                               index_,
                                               Utils.Get(gearings_, i, 1.0),
                                               Utils.Get(spreads_, i, 0.0),
                                               refStart, refEnd,
                                               paymentDayCounter_));
         }

         return cashflows;
      }
   }
}
