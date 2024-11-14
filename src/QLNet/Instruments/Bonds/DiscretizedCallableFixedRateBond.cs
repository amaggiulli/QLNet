/*
 Copyright (C) 2008-2023  Andrea Maggiulli (a.maggiulli@gmail.com)

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

   public enum  CouponAdjustment { Pre, Post }

   public class DiscretizedCallableFixedRateBond : DiscretizedAsset
   {
      private CallableBond.Arguments arguments_;
      private double redemptionTime_;
      private List<double> couponTimes_ = new List<double>();
      private List<CouponAdjustment> couponAdjustments_;
      private List<double> callabilityTimes_;
      private List<double> adjustedCallabilityPrices_;

      public DiscretizedCallableFixedRateBond(CallableBond.Arguments args, Handle<YieldTermStructure> termStructure)
      {
         arguments_ = args;
         adjustedCallabilityPrices_ = args.callabilityPrices;

         var dayCounter = termStructure.link.dayCounter();
         var referenceDate = termStructure.link.referenceDate();
         redemptionTime_ = dayCounter.yearFraction(referenceDate, args.redemptionDate);

         /* By default the coupon adjustment should take place in
          * DiscretizedCallableFixedRateBond::postAdjustValuesImpl(). */
         couponAdjustments_ = new InitializedList<CouponAdjustment>(args.couponDates.Count, CouponAdjustment.Post);

         foreach (var t in args.couponDates)
            couponTimes_.Add(dayCounter.yearFraction(referenceDate, t));

         callabilityTimes_ = new InitializedList<double>(args.callabilityDates.Count,0);
         for (var i = 0; i < args.callabilityDates.Count; i++)
         {
            var callabilityDate = args.callabilityDates[i];
            var callabilityTime = dayCounter.yearFraction(referenceDate, args.callabilityDates[i]);

            // To avoid mispricing, we snap exercise dates to the closest coupon date.
            for (int j = 0; j < couponTimes_.Count; j++)
            {
               var couponTime = couponTimes_[j];
               var couponDate = args.couponDates[j];

               if (withinNextWeek(callabilityTime, couponTime) && callabilityDate < couponDate)
               {
                  // Snap the exercise date.
                  callabilityTime = couponTime;

                 /* The order of events must be changed here. In
                  * DiscretizedCallableFixedRateBond::postAdjustValuesImpl() the callability is
                  * done before adding of the coupon. However from the
                  * DiscretizedAsset::rollback(Time to) perspective the coupon must be added
                  * before the callability as it is later in time. */
                 couponAdjustments_[j] = CouponAdjustment.Pre;

                  /* We snapped the callabilityTime so we need to take into account the missing
                   * discount factor including any possible spread e.g. set in the OAS
                   * calculation. */
                 var spread  = arguments_.spread;

                 double CalcDiscountFactorInclSpread(Date date)
                 {
                    var time = termStructure.link.timeFromReference(date);
                    var zeroRateInclSpread = termStructure.link.zeroRate(date, termStructure.link.dayCounter(), Compounding.Continuous, Frequency.NoFrequency).value() + spread;
                    var df = Math.Exp(-zeroRateInclSpread * time);
                    return df;
                 }

                 var dfTillCallDate = CalcDiscountFactorInclSpread(callabilityDate);
                 var dfTillCouponDate = CalcDiscountFactorInclSpread(couponDate);
                 adjustedCallabilityPrices_[i] *= dfTillCallDate / dfTillCouponDate;

                 break;
               }
            }

            adjustedCallabilityPrices_[i] *= arguments_.faceAmount / 100.0;
            callabilityTimes_[i] = callabilityTime;
         }
          
      }

      public override void reset(int size)
      {
         values_ = new Vector(size, arguments_.redemption);
         adjustValues();
      }
      public override List<double> mandatoryTimes()
      {
         List<double> times = new List<double>();
         double t;
         int i;

         t = redemptionTime_;
         if (t >= 0.0)
         {
            times.Add(t);
         }

         for (i = 0; i < couponTimes_.Count ; i++)
         {
            t = couponTimes_[i];
            if (t >= 0.0)
            {
               times.Add(t);
            }
         }

         for (i = 0; i < callabilityTimes_.Count; i++)
         {
            t = callabilityTimes_[i];
            if (t >= 0.0)
            {
               times.Add(t);
            }
         }

         return times;
      }

      protected override void preAdjustValuesImpl()
      {
         for (var i = 0; i < couponTimes_.Count; i++)
         {
            if (couponAdjustments_[i] == CouponAdjustment.Pre)
            {
               var t = couponTimes_[i];
               if (t >= 0.0 && isOnTime(t))
               {
                  addCoupon(i);
               }
            }
         }
      }
      protected override void postAdjustValuesImpl()
      {
         for (int i = 0; i < callabilityTimes_.Count; i++)
         {
            double t = callabilityTimes_[i];
            if (t >= 0.0 && isOnTime(t))
            {
               applyCallability(i);
            }
         }
         for (int i = 0; i < couponTimes_.Count; i++)
         {
            double t = couponTimes_[i];
            if (t >= 0.0 && isOnTime(t))
            {
               addCoupon(i);
            }
         }
      }

      private void applyCallability(int i)
      {
         int j;
         switch (arguments_.putCallSchedule[i].type())
         {
            case Callability.Type.Call:
               for (j = 0; j < values_.size(); j++)
               {
                  values_[j] = Math.Min(arguments_.callabilityPrices[i], values_[j]);
               }
               break;

            case Callability.Type.Put:
               for (j = 0; j < values_.size(); j++)
               {
                  values_[j] = Math.Max(values_[j], arguments_.callabilityPrices[i]);
               }
               break;

            default:
               Utils.QL_FAIL("unknown callability type");
               break;
         }
      }
      private void addCoupon(int i)
      {
         values_ += arguments_.couponAmounts[i];
      }
      private static bool withinNextWeek(double t1, double t2)
      {
         var dt = 1.0 / 52;
         return t1 <= t2 && t2 <= t1 + dt;
      }

   }
}
