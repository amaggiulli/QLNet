﻿/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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

   public class DiscretizedCallableFixedRateBond : DiscretizedAsset
   {
      public DiscretizedCallableFixedRateBond(CallableBond.Arguments args,
                                              Date referenceDate,
                                              DayCounter dayCounter)
      {
         arguments_ = args;
         redemptionTime_ = dayCounter.yearFraction(referenceDate, args.redemptionDate);

         for (int i = 0; i < args.couponDates.Count; ++i)
            couponTimes_.Add(dayCounter.yearFraction(referenceDate, args.couponDates[i]));

         for (int i = 0; i < args.callabilityDates.Count; ++i)
            callabilityTimes_.Add(dayCounter.yearFraction(referenceDate, args.callabilityDates[i]));

         // similar to the tree swaption engine, we collapse similar coupon
         // and exercise dates to avoid mispricing. Delete if unnecessary.

         for (int i = 0; i < callabilityTimes_.Count; i++)
         {
            double exerciseTime = callabilityTimes_[i];
            for (int j = 0; j < couponTimes_.Count; j++)
            {
               if (withinNextWeek(exerciseTime, couponTimes_[j]))
                  couponTimes_[j] = exerciseTime;
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

         for (i = 0; i < couponTimes_.Count; i++)
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
         // Nothing to do here
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

      private CallableBond.Arguments arguments_;
      private double redemptionTime_;
      private List<double> couponTimes_ = new List<double>();
      private List<double> callabilityTimes_ = new List<double>();
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

      private bool withinNextWeek(double t1, double t2)
      {
         double dt = 1.0 / 52;
         return t1 <= t2 && t2 <= t1 + dt;
      }

   }
}
