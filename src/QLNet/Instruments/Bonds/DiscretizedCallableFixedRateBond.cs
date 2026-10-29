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
         adjustedCallabilityPrices_ = new List<double>(args.callabilityPrices); 

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

         // not snapped itself, but a snapped neighbour may have moved this coupon ahead
         for (var i = 0; i < callabilityTimes_.Count; i++)
         {
            for (var j = 0; j < couponTimes_.Count; j++)
            {
               if (couponAdjustments_[j] == CouponAdjustment.Pre &&
                   args.callabilityDates[i] == args.couponDates[j])
                  adjustedCallabilityPrices_[i] += args.couponAmounts[j];
            }
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
         checkGridTimes();
         for (var i = 0; i < couponTimes_.Count; i++)
         {
            if (couponAdjustments_[i] == CouponAdjustment.Pre)
            {
               var t = couponTimes_[i];
               if (t >= 0.0 && isOnGridTime(t, couponGridTimes_, i))
               {
                  addCoupon(i);
               }
            }
         }
      }
      protected override void postAdjustValuesImpl()
      {
         checkGridTimes();
         for (int i = 0; i < callabilityTimes_.Count; i++)
         {
            double t = callabilityTimes_[i];
            if (t >= 0.0 && isOnGridTime(t, callabilityGridTimes_, i))
            {
               applyCallability(i);
            }
         }
         for (int i = 0; i < couponTimes_.Count; i++)
         {
            if (couponAdjustments_[i] == CouponAdjustment.Post)
            {
               var t = couponTimes_[i];
               if (t >= 0.0 && isOnGridTime(t, couponGridTimes_, i))
               {
                  addCoupon(i);
               }
            }
         }
      }

      // Grid times of the coupon and callability events on the current lattice, filled on first use
      // (NaN until then) so each event's grid lookup runs once per lattice rather than at every step.
      private Lattice gridTimesMethod_;
      private double[] couponGridTimes_;
      private double[] callabilityGridTimes_;

      private void checkGridTimes()
      {
         if (ReferenceEquals(gridTimesMethod_, method()))
            return;
         couponGridTimes_ = newGridTimes(couponTimes_.Count);
         callabilityGridTimes_ = newGridTimes(callabilityTimes_.Count);
         gridTimesMethod_ = method();
      }

      private static double[] newGridTimes(int size)
      {
         var gridTimes = new double[size];
         for (int i = 0; i < size; i++)
            gridTimes[i] = double.NaN;
         return gridTimes;
      }

      private bool isOnGridTime(double t, double[] gridTimes, int i)
      {
         if (double.IsNaN(gridTimes[i]))
         {
            TimeGrid grid = method().timeGrid();
            gridTimes[i] = grid[grid.index(t)];
         }
         return Utils.close(gridTimes[i], time());
      }

      private void applyCallability(int i)
      {
         int j;
         switch (arguments_.putCallSchedule[i].type())
         {
            case Callability.Type.Call:
               for (j = 0; j < values_.size(); j++)
               {
                  values_[j] = Math.Min(adjustedCallabilityPrices_[i], values_[j]);
               }
               break;

            case Callability.Type.Put:
               for (j = 0; j < values_.size(); j++)
               {
                  values_[j] = Math.Max(values_[j], adjustedCallabilityPrices_[i]);
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
