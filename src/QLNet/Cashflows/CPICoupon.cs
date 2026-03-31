/*
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System;
using System.Collections.Generic;

namespace QLNet
{
   /// <summary>
   /// Describes how index values are interpolated between fixings when an index is observed.
   /// </summary>
   public enum InterpolationType
   {
      /// <summary>
      /// Uses the same interpolation as the index.
      /// </summary>
      AsIndex,

      /// <summary>
      /// Uses a flat interpolation from the previous fixing.
      /// </summary>
      Flat,

      /// <summary>
      /// Interpolates linearly between the bracketing fixings.
      /// </summary>
      Linear
   }

   /// <summary>
   /// Coupon paying the performance of a CPI (zero inflation) index
   /// </summary>
   /// <remarks>
   /// The performance is relative to the index value on the base date.
   /// The other inflation value is taken from the refPeriodEnd date
   /// with observation lag, so any roll/calendar etc. will be built
   /// in by the caller.  By default this is done in the
   /// InflationCoupon which uses ModifiedPreceding with fixing days
   /// assumed positive meaning earlier, i.e. always stay in same
   /// month (relative to referencePeriodEnd).
   /// This is more sophisticated than an IndexedCashFlow because it
   /// does date calculations itself.
   /// We do not do any convexity adjustment for lags different
   /// to the natural ZCIIS lag that was used to create the
   /// forward inflation curve.
   /// </remarks>
   public class CPICoupon : InflationCoupon
   {
      protected double baseCPI_;
      protected double fixedRate_;
      protected double spread_;
      protected InterpolationType observationInterpolation_;

      protected override bool checkPricerImpl(InflationCouponPricer pricer)
      {
         CPICouponPricer p = pricer as CPICouponPricer;
         return (p != null);
      }

      // use to calculate for fixing date, allows change of
      // interpolation w.r.t. index.  Can also be used ahead of time
      protected double indexFixing(Date d)
      {
         // you may want to modify the interpolation of the index
         // this gives you the chance

         double I1;
         // what interpolation do we use? Index / flat / linear
         if (observationInterpolation() == InterpolationType.AsIndex)
         {
            I1 = cpiIndex().fixing(d);
         }
         else
         {
            // work out what it should be
            KeyValuePair<Date, Date> dd = Utils.inflationPeriod(d, cpiIndex().frequency());
            double indexStart = cpiIndex().fixing(dd.Key);
            if (observationInterpolation() == InterpolationType.Linear)
            {
               double indexEnd = cpiIndex().fixing(dd.Value + new Period(1, TimeUnit.Days));
               // linear interpolation
               I1 = indexStart + (indexEnd - indexStart) * (d - dd.Key)
                    / (double)((dd.Value + new Period(1, TimeUnit.Days)) - dd.Key); // can't get to next period's value within current period
            }
            else
            {
               // no interpolation, i.e. flat = constant, so use start-of-period value
               I1 = indexStart;
            }

         }
         return I1;
      }

      public CPICoupon(double baseCPI, // user provided, could be arbitrary
                       Date paymentDate,
                       double nominal,
                       Date startDate,
                       Date endDate,
                       int fixingDays,
                       ZeroInflationIndex index,
                       Period observationLag,
                       InterpolationType observationInterpolation,
                       DayCounter dayCounter,
                       double fixedRate, // aka gearing
                       double spread = 0.0,
                       Date refPeriodStart = null,
                       Date refPeriodEnd = null,
                       Date exCouponDate = null)
         : base(paymentDate, nominal, startDate, endDate, fixingDays, index,
                observationLag, dayCounter, refPeriodStart, refPeriodEnd, exCouponDate)
      {

         baseCPI_ = baseCPI;
         fixedRate_ = fixedRate;
         spread_ = spread;
         observationInterpolation_ = observationInterpolation;
         Utils.QL_REQUIRE(Math.Abs(baseCPI_) > 1e-16, () => "|baseCPI_| < 1e-16, future divide-by-zero problem");
      }

      // Inspectors
      // fixed rate that will be inflated by the index ratio
      public double fixedRate() { return fixedRate_; }
      /// <summary>
      /// Returns the spread paid over the fixing of the underlying index.
      /// </summary>
      public double spread() { return spread_; }

      /// <summary>
      /// Returns the adjusted fixing, already divided by the base fixing.
      /// </summary>
      public double adjustedFixing() { return (rate() - spread()) / fixedRate(); }

      /// <summary>
      /// Returns the fixing, allowing for a different interpolation from the index.
      /// </summary>
      public override double indexFixing() { return indexFixing(fixingDate()); }
      /// <summary>
      /// base value for the CPI index
      /// </summary>
      /// <remarks>
      /// Warning: make sure that the interpolation used to create
      /// this is what you are using for the fixing,
      /// i.e. the observationInterpolation.
      /// </remarks>
      public double baseCPI() { return baseCPI_; }
      /// <summary>
      /// Returns how the coupon observes the index.
      /// </summary>
      public InterpolationType observationInterpolation() { return observationInterpolation_; }

      /// <summary>
      /// Returns the index observation for the given date.
      /// </summary>
      public double indexObservation(Date onDate) { return indexFixing(onDate); }

      /// <summary>
      /// Returns the underlying CPI index.
      /// </summary>
      public ZeroInflationIndex cpiIndex() { return index() as ZeroInflationIndex; }
   }

   /// <summary>
   /// Cash flow paying the performance of a CPI (zero inflation) index
   /// </summary>
   /// <remarks>
   /// It is NOT a coupon, i.e. no accruals.
   /// </remarks>
   public class CPICashFlow : IndexedCashFlow
   {
      public CPICashFlow(double notional,
                         ZeroInflationIndex index,
                         Date baseDate,
                         double baseFixing,
                         Date fixingDate,
                         Date paymentDate,
                         bool growthOnly = false,
                         InterpolationType interpolation = InterpolationType.AsIndex,
                         Frequency frequency = Frequency.NoFrequency)
         : base(notional, index, baseDate, fixingDate, paymentDate, growthOnly)
      {
         baseFixing_ = baseFixing;
         interpolation_ = interpolation;
         frequency_ = frequency;

         Utils.QL_REQUIRE(Math.Abs(baseFixing_) > 1e-16, () => "|baseFixing|<1e-16, future divide-by-zero error");

         if (interpolation_ != InterpolationType.AsIndex)
         {
            Utils.QL_REQUIRE(frequency_ != Frequency.NoFrequency, () => "non-index interpolation w/o frequency");
         }
      }

      /// <summary>
      /// Returns the value used on the base date.
      /// </summary>
      /// <remarks>
      /// This does not have to agree with the index on that date.
      /// </remarks>
      public virtual double baseFixing() {return baseFixing_;}

      /// <summary>
      /// Returns the base date.
      /// </summary>
      /// <remarks>
      /// A valid base date may not be available.
      /// </remarks>
      public override Date baseDate()
      {
         Utils.QL_FAIL("no base date specified");
         return null;
      }

      /// <summary>
      /// Returns the interpolation used for future data.
      /// </summary>
      public virtual InterpolationType interpolation() { return interpolation_; }

      public virtual Frequency frequency() { return frequency_; }

      /// <summary>
      /// Returns the cash-flow amount using the base fixing and interpolation.
      /// </summary>
      public override double amount()
      {
         double I0 = baseFixing();
         double I1;

         // what interpolation do we use? Index / flat / linear
         if (interpolation() == InterpolationType.AsIndex)
         {
            I1 = index().fixing(fixingDate());
         }
         else
         {
            // work out what it should be
            KeyValuePair<Date, Date> dd = Utils.inflationPeriod(fixingDate(), frequency());
            double indexStart = index().fixing(dd.Key);
            if (interpolation() == InterpolationType.Linear)
            {
               double indexEnd = index().fixing(dd.Value + new Period(1, TimeUnit.Days));
               // linear interpolation
               I1 = indexStart + (indexEnd - indexStart) * (fixingDate() - dd.Key)
                    / ((dd.Value + new Period(1, TimeUnit.Days)) - dd.Key); // can't get to next period's value within current period
            }
            else
            {
               // no interpolation, i.e. flat = constant, so use start-of-period value
               I1 = indexStart;
            }

         }

         if (growthOnly())
            return notional() * (I1 / I0 - 1.0);
         else
            return notional() * (I1 / I0);
      }

      protected double baseFixing_;
      protected InterpolationType interpolation_;
      protected Frequency frequency_;
   }

   /// <summary>
   /// Helper class building a sequence of capped/floored CPI coupons.
   /// </summary>
   /// <remarks>
   /// Also allowing for the inflated notional at the end...
   /// especially if there is only one date in the schedule.
   /// If a fixedRate is zero you get a FixedRateCoupon, otherwise
   /// you get a ZeroInflationCoupon.
   /// payoff is: spread + fixedRate x index
   /// </remarks>
   public class CPILeg : CPILegBase
   {
      public CPILeg(Schedule schedule,
                    ZeroInflationIndex index,
                    double baseCPI,
                    Period observationLag)
      {
         schedule_ = schedule;
         index_ = index;
         baseCPI_ = baseCPI;
         observationLag_ = observationLag;
         paymentDayCounter_ = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
         paymentAdjustment_ = BusinessDayConvention.ModifiedFollowing;
         paymentCalendar_ = schedule.calendar();
         fixingDays_ = new List<int>() { 0 };
         observationInterpolation_ = InterpolationType.AsIndex;
         subtractInflationNominal_ = true;
         spreads_ = new List<double>() { 0 };
      }

      public override List<CashFlow> value()
      {
         Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");

         int n = schedule_.Count - 1;
         List<CashFlow> leg = new List<CashFlow>(n + 1);

         if (n > 0)
         {
            Utils.QL_REQUIRE(!fixedRates_.empty() || !spreads_.empty(), () => "no fixedRates or spreads given");

            Date refStart, start, refEnd, end;

            for (int i = 0; i < n; ++i)
            {
               refStart = start = schedule_.date(i);
               refEnd = end = schedule_.date(i + 1);
               Date paymentDate = paymentCalendar_.adjust(end, paymentAdjustment_);

               Date exCouponDate = null;
               if (exCouponPeriod_ != null)
               {
                  exCouponDate = exCouponCalendar_.advance(paymentDate,
                                                           -exCouponPeriod_,
                                                           exCouponAdjustment_,
                                                           exCouponEndOfMonth_);
               }

               if (i == 0 && !schedule_.isRegular(i + 1))
               {
                  BusinessDayConvention bdc = schedule_.businessDayConvention();
                  refStart = schedule_.calendar().adjust(end - schedule_.tenor(), bdc);
               }
               if (i == n - 1 && !schedule_.isRegular(i + 1))
               {
                  BusinessDayConvention bdc = schedule_.businessDayConvention();
                  refEnd = schedule_.calendar().adjust(start + schedule_.tenor(), bdc);
               }
               if (Utils.Get(fixedRates_, i, 1.0).IsEqual(0.0))
               {
                  // fixed coupon
                  leg.Add(new FixedRateCoupon(paymentDate, Utils.Get(notionals_, i, 0.0),
                                              Utils.effectiveFixedRate(spreads_, caps_, floors_, i),
                                              paymentDayCounter_, start, end, refStart, refEnd, exCouponDate));
               }
               else
               {
                  // zero inflation coupon
                  if (Utils.noOption(caps_, floors_, i))
                  {
                     // just swaplet
                     CPICoupon coup;

                     coup = new CPICoupon(baseCPI_,    // all have same base for ratio
                                          paymentDate,
                                          Utils.Get(notionals_, i, 0.0),
                                          start, end,
                                          Utils.Get(fixingDays_, i, 0),
                                          index_, observationLag_,
                                          observationInterpolation_,
                                          paymentDayCounter_,
                                          Utils.Get(fixedRates_, i, 0.0),
                                          Utils.Get(spreads_, i, 0.0),
                                          refStart, refEnd, exCouponDate);

                     // in this case you can set a pricer
                     // straight away because it only provides computation - not data
                     CPICouponPricer pricer = new CPICouponPricer();
                     coup.setPricer(pricer);
                     leg.Add(coup);

                  }
                  else
                  {
                     // cap/floorlet
                     Utils.QL_FAIL("caps/floors on CPI coupons not implemented.");
                  }
               }
            }
         }

         // in CPI legs you always have a notional flow of some sort
         Date pDate = paymentCalendar_.adjust(schedule_.date(n), paymentAdjustment_);
         Date fixingDate = pDate - observationLag_;
         CashFlow xnl = new CPICashFlow
         (Utils.Get(notionals_, n, 0.0), index_,
          new Date(), // is fake, i.e. you do not have one
          baseCPI_, fixingDate, pDate,
          subtractInflationNominal_, observationInterpolation_,
          index_.frequency());

         leg.Add(xnl);

         return leg;
      }
   }
}
