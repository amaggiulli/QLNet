/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
   /// <summary>
   /// Year-on-year inflation coupon with an optional cap, floor, or collar.
   /// </summary>
   /// <remarks>
   /// This is the year-on-year inflation counterpart of the capped or floored coupon types used for nominal rates.
   /// It applies optional cap and floor strikes to the coupon rate produced by the underlying inflation observation,
   /// after gearing and spread have been taken into account.
   ///
   /// In practical terms:
   /// a capped coupon limits the adjusted inflation rate to the cap,
   /// a floored coupon ensures the adjusted inflation rate does not fall below the floor,
   /// and a collared coupon keeps the adjusted inflation rate between the floor and the cap.
   ///
   /// When <c>paysWithin</c> is <c>false</c>, the inverse form is used so the coupon can be represented consistently
   /// for cap/floor style pricing.
   /// </remarks>
   public class CappedFlooredYoYInflationCoupon : YoYInflationCoupon
   {
      // we may watch an underlying coupon ...
      public CappedFlooredYoYInflationCoupon(YoYInflationCoupon underlying,
                                             double? cap = null,
                                             double? floor = null)
      : base(underlying.date(),
             underlying.nominal(),
             underlying.accrualStartDate(),
             underlying.accrualEndDate(),
             underlying.fixingDays(),
             underlying.yoyIndex(),
             underlying.observationLag(),
             underlying.dayCounter(),
             underlying.gearing(),
             underlying.spread(),
             underlying.referencePeriodStart,
             underlying.referencePeriodEnd)

      {
         underlying_ = underlying;
         isFloored_ = false;
         isCapped_ = false;
         setCommon(cap, floor);
         underlying.registerWith(update);
      }


      // ... or not
      public CappedFlooredYoYInflationCoupon(Date paymentDate,
                                             double nominal,
                                             Date startDate,
                                             Date endDate,
                                             int fixingDays,
                                             YoYInflationIndex index,
                                             Period observationLag,
                                             DayCounter dayCounter,
                                             double gearing = 1.0,
                                             double spread = 0.0,
                                             double? cap = null,
                                             double? floor = null,
                                             Date refPeriodStart = null,
                                             Date refPeriodEnd = null)
      : base(paymentDate, nominal, startDate, endDate,
             fixingDays, index, observationLag,  dayCounter,
             gearing, spread, refPeriodStart, refPeriodEnd)
      {
         isFloored_ = false;
         isCapped_ = false;
         setCommon(cap, floor);
      }

      // augmented Coupon interface
      // swap(let) rate
      public override double rate()
      {
         double swapletRate = underlying_ != null ? underlying_.rate() : base.rate();

         if (isFloored_ || isCapped_)
         {
            if (underlying_ != null)
            {
               Utils.QL_REQUIRE(underlying_.pricer() != null, () => "pricer not set");
            }
            else
            {
               Utils.QL_REQUIRE(pricer_ != null, () => "pricer not set");
            }
         }

         double floorletRate = 0.0;
         if (isFloored_)
         {
            floorletRate =
               underlying_ != null?
               underlying_.pricer().floorletRate(effectiveFloor()) :
               pricer().floorletRate(effectiveFloor())
               ;
         }
         double capletRate = 0.0;
         if (isCapped_)
         {
            capletRate =
               underlying_ != null ?
               underlying_.pricer().capletRate(effectiveCap()) :
               pricer().capletRate(effectiveCap())
               ;
         }

         return swapletRate + floorletRate - capletRate;

      }
      /// <summary>
      /// Returns the cap, if any.
      /// </summary>
      public double? cap()
      {
         if ((gearing_ > 0) && isCapped_)
            return cap_;

         if ((gearing_ < 0) && isFloored_)
            return floor_;

         return null;
      }
      /// <summary>
      /// Returns the floor, if any.
      /// </summary>
      public double? floor()
      {
         if ((gearing_ > 0) && isFloored_)
            return floor_;

         if ((gearing_ < 0) && isCapped_)
            return cap_;

         return null;
      }
      /// <summary>
      /// Returns the effective cap of the fixing.
      /// </summary>
      public double effectiveCap()
      {
         return (cap_ - spread()) / gearing();
      }
      /// <summary>
      /// Returns the effective floor of the fixing.
      /// </summary>
      public double effectiveFloor()
      {
         return (floor_ - spread()) / gearing();
      }

      public bool isCapped() { return isCapped_; }
      public bool isFloored() { return isFloored_; }


      public void setPricer(YoYInflationCouponPricer pricer)
      {
         base.setPricer(pricer);
         if (underlying_ != null)
            underlying_.setPricer(pricer);
      }

      protected virtual void setCommon(double? cap, double? floor)
      {
         isCapped_ = false;
         isFloored_ = false;

         if (gearing_ > 0)
         {
            if (cap != null)
            {
               isCapped_ = true;
               cap_ = cap.Value;
            }
            if (floor != null)
            {
               floor_ = floor.Value;
               isFloored_ = true;
            }
         }
         else
         {
            if (cap != null)
            {
               floor_ = cap.Value;
               isFloored_ = true;
            }
            if (floor != null)
            {
               isCapped_ = true;
               cap_ = floor.Value;
            }
         }

         if (isCapped_ && isFloored_)
         {
            Utils.QL_REQUIRE(cap >= floor, () => "cap level (" + cap + ") less than floor level (" + floor + ")");
         }

      }

      // data, we only use underlying_ if it was constructed that way,
      // generally we use the shared_ptr conversion to boolean to test
      protected YoYInflationCoupon underlying_;
      protected bool isFloored_, isCapped_;
      protected double cap_, floor_;
   }
}
