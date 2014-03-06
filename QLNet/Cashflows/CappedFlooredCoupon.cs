/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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

namespace QLNet
{

   //! Capped and/or floored floating-rate coupon
   //    ! The payoff \f$ P \f$ of a capped floating-rate coupon is:
   //        \f[ P = N \times T \times \min(a L + b, C). \f]
   //        The payoff of a floored floating-rate coupon is:
   //        \f[ P = N \times T \times \max(a L + b, F). \f]
   //        The payoff of a collared floating-rate coupon is:
   //        \f[ P = N \times T \times \min(\max(a L + b, F), C). \f]
   //
   //        where \f$ N \f$ is the notional, \f$ T \f$ is the accrual
   //        time, \f$ L \f$ is the floating rate, \f$ a \f$ is its
   //        gearing, \f$ b \f$ is the spread, and \f$ C \f$ and \f$ F \f$
   //        the strikes.
   //
   //        They can be decomposed in the following manner.
   //        Decomposition of a capped floating rate coupon:
   //        \f[
   //        R = \min(a L + b, C) = (a L + b) + \min(C - b - \xi |a| L, 0)
   //        \f]
   //        where \f$ \xi = sgn(a) \f$. Then:
   //        \f[
   //        R = (a L + b) + |a| \min(\frac{C - b}{|a|} - \xi L, 0)
   //        \f]
   //    
   public class CappedFlooredCoupon : FloatingRateCoupon
   {
      // data
      protected FloatingRateCoupon underlying_;
      protected bool isCapped_;
      protected bool isFloored_;
      protected double? cap_;
      protected double? floor_;

      // need by CashFlowVectors
      public CappedFlooredCoupon() { }

      public CappedFlooredCoupon(FloatingRateCoupon underlying, double? cap = null, double? floor = null)
         : base(underlying.nominal(), underlying.date(), underlying.accrualStartDate(), underlying.accrualEndDate(), underlying.fixingDays, underlying.index(), underlying.gearing(), underlying.spread(), underlying.refPeriodStart, underlying.refPeriodEnd, underlying.dayCounter(), underlying.isInArrears())
      {
         underlying_ = underlying;
         isCapped_ = false;
         isFloored_ = false;

         if (gearing_ > 0)
         {
            if (cap != null)
            {
               isCapped_ = true;
               cap_ = cap;
            }
            if (floor != null)
            {
               floor_ = floor;
               isFloored_ = true;
            }
         }
         else
         {
            if (cap != null)
            {
               floor_ = cap;
               isFloored_ = true;
            }
            if (floor != null)
            {
               isCapped_ = true;
               cap_ = floor;
            }
         }
         if (isCapped_ && isFloored_)
            if (!(cap >= floor))
               throw new ApplicationException("cap level (" + cap + ") less than floor level (" + floor + ")");
         underlying.registerWith(update);
      }
      //! \name Coupon interface
      //@{
      public override double rate()
      {
         if (underlying_.pricer() == null)
            throw new ApplicationException("pricer not set");

         double swapletRate = underlying_.rate();
         double floorletRate = 0.0;
         if (isFloored_)
            floorletRate = underlying_.pricer().floorletRate(effectiveFloor().Value);
         double capletRate = 0.0;
         if (isCapped_)
            capletRate = underlying_.pricer().capletRate(effectiveCap().Value);
         return swapletRate + floorletRate - capletRate;
      }
      public override double convexityAdjustment()
      {
         return underlying_.convexityAdjustment();
      }
      //@}
      //! cap
      public double cap()
      {
         if ((gearing_ > 0) && isCapped_)
            return cap_.GetValueOrDefault();
         if ((gearing_ < 0) && isFloored_)
            return floor_.GetValueOrDefault();
         return 0.0;
      }
      //! floor
      public double floor()
      {
         if ((gearing_ > 0) && isFloored_)
            return floor_.GetValueOrDefault();
         if ((gearing_ < 0) && isCapped_)
            return cap_.GetValueOrDefault();
         return 0.0;
      }
      //! effective cap of fixing
      public double? effectiveCap()
      {
         return isCapped_ ? (cap_.Value - spread()) / gearing() : (double?)null;
      }

      //! effective floor of fixing
      public double? effectiveFloor()
      {
         return isFloored_ ? (floor_.Value - spread()) / gearing() : (double?)null;
      }

      public bool isCapped()
      {
         return isCapped_;
      }
      public bool isFloored()
      {
         return isFloored_;
      }

      public override void setPricer(FloatingRateCouponPricer pricer)
      {
         base.setPricer(pricer);
         underlying_.setPricer(pricer);
      }

      // Factory - for Leg generators
      public virtual CashFlow factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays, InterestRateIndex index, double gearing, double spread, double? cap, double? floor, Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears)
      {
         return new CappedFlooredCoupon(new FloatingRateCoupon(nominal, paymentDate, startDate, endDate, fixingDays, index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears), cap, floor);
      }
   }

   public class CappedFlooredIborCoupon : CappedFlooredCoupon
   {
      // need by CashFlowVectors
      public CappedFlooredIborCoupon() { }

      public CappedFlooredIborCoupon(double nominal, 
                                     Date paymentDate, 
                                     Date startDate, 
                                     Date endDate, 
                                     int fixingDays, 
                                     IborIndex index, 
                                     double gearing = 1.0, 
                                     double spread = 0.0, 
                                     double? cap = null, 
                                     double? floor = null, 
                                     Date refPeriodStart = null, 
                                     Date refPeriodEnd = null, 
                                     DayCounter dayCounter = null, 
                                     bool isInArrears = false)
         : base(new IborCoupon(nominal, paymentDate, startDate, endDate, fixingDays, index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears) as FloatingRateCoupon, cap, floor)
      {
      }

      // Factory - for Leg generators
      public virtual CashFlow factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays, IborIndex index, double gearing, double spread, double? cap, double? floor, Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears)
      {
         return new CappedFlooredIborCoupon(nominal, paymentDate, startDate, endDate, fixingDays, index, gearing, spread, cap, floor, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);
      }
   }

   public class CappedFlooredCmsCoupon : CappedFlooredCoupon
   {
      // need by CashFlowVectors
      public CappedFlooredCmsCoupon() { }

      public CappedFlooredCmsCoupon(double nominal, 
                                    Date paymentDate, 
                                    Date startDate, 
                                    Date endDate, 
                                    int fixingDays, 
                                    SwapIndex index, 
                                    double gearing = 1.0, 
                                    double spread = 0.0, 
                                    double? cap = null, 
                                    double? floor = null, 
                                    Date refPeriodStart = null, 
                                    Date refPeriodEnd = null, 
                                    DayCounter dayCounter = null, 
                                    bool isInArrears = false)
         : base(new CmsCoupon(nominal, paymentDate, startDate, endDate, fixingDays, index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears) as FloatingRateCoupon, cap, floor)
      {
      }

      // Factory - for Leg generators
      public override CashFlow factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays, InterestRateIndex index, double gearing, double spread, double? cap, double? floor, Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears)
      {
         return new CappedFlooredCmsCoupon(nominal, paymentDate, startDate, endDate, fixingDays, (SwapIndex)index, gearing, spread, cap, floor, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);
      }
   }

}
