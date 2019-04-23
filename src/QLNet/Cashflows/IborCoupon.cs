/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   // Coupon paying a Libor-type index
   public class IborCoupon : FloatingRateCoupon
   {
      public IborCoupon() { }

      public IborCoupon(Date paymentDate,
                        double nominal,
                        Date startDate,
                        Date endDate,
                        int fixingDays,
                        IborIndex iborIndex,
                        double gearing = 1.0,
                        double spread = 0.0,
                        Date refPeriodStart = null,
                        Date refPeriodEnd = null,
                        DayCounter dayCounter = null,
                        bool isInArrears = false) :
         base(paymentDate, nominal, startDate, endDate, fixingDays, iborIndex, gearing, spread,
              refPeriodStart, refPeriodEnd, dayCounter, isInArrears)
      {
         iborIndex_ = iborIndex;

         fixingDate_ = fixingDate();

         Calendar fixingCalendar = index_.fixingCalendar();
         int indexFixingDays = index_.fixingDays();

         fixingValueDate_ = fixingCalendar.advance(fixingDate_, indexFixingDays, TimeUnit.Days);

#if QL_USE_INDEXED_COUPON
         fixingEndDate_ = index_->maturityDate(fixingValueDate_);
#else
         if (isInArrears_)
            fixingEndDate_ = index_.maturityDate(fixingValueDate_);
         else
         {
            // par coupon approximation
            Date nextFixingDate = fixingCalendar.advance(accrualEndDate_, -fixingDays_, TimeUnit.Days);
            fixingEndDate_ = fixingCalendar.advance(nextFixingDate, indexFixingDays, TimeUnit.Days);
         }
#endif

         DayCounter dc = index_.dayCounter();
         spanningTime_ = dc.yearFraction(fixingValueDate_, fixingEndDate_);
         Utils.QL_REQUIRE(spanningTime_ > 0.0, () =>
                          "\n cannot calculate forward rate between " +
                          fixingValueDate_ + " and " + fixingEndDate_ +
                          ":\n non positive time (" + spanningTime_ +
                          ") using " + dc.name() + " daycounter");
      }

      // Inspectors
      public IborIndex iborIndex()  {return iborIndex_;}

      //! FloatingRateCoupon interface
      //! Implemented in order to manage the case of par coupon
      public override double indexFixing()
      {
         /* instead of just returning index_->fixing(fixingValueDate_)
           its logic is duplicated here using a specialized iborIndex
           forecastFixing overload which
           1) allows to save date/time recalculations, and
           2) takes into account par coupon needs
         */
         Date today = Settings.evaluationDate();

         if (fixingDate_ > today)
            return iborIndex_.forecastFixing(fixingValueDate_, fixingEndDate_, spanningTime_);

         if (fixingDate_ < today || Settings.enforcesTodaysHistoricFixings)
         {
            // do not catch exceptions
            double? result = index_.pastFixing(fixingDate_);
            Utils.QL_REQUIRE(result != null, () => "Missing " + index_.name() + " fixing for " + fixingDate_);
            return result.Value;
         }

         try
         {
            double? result = index_.pastFixing(fixingDate_);
            if (result != null)
               return result.Value;

         }
         catch (Exception)
         {
            // fall through and forecast
         }
         return iborIndex_.forecastFixing(fixingValueDate_, fixingEndDate_, spanningTime_);

      }

      // Factory - for Leg generators
      public override CashFlow factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays,
                                       InterestRateIndex index, double gearing, double spread,
                                       Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears)
      {
         return new IborCoupon(paymentDate, nominal, startDate, endDate, fixingDays,
                               (IborIndex)index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);
      }

      private IborIndex iborIndex_;
      private Date fixingDate_, fixingValueDate_, fixingEndDate_;
      private double spanningTime_;
   }

   //! helper class building a sequence of capped/floored ibor-rate coupons
   public class IborLeg : FloatingLegBase
   {
      // constructor
      public IborLeg(Schedule schedule, IborIndex index)
      {
         schedule_ = schedule;
         index_ = index;
         paymentAdjustment_ = BusinessDayConvention.Following;
         inArrears_ = false;
         zeroPayments_ = false;
      }

      public override List<CashFlow> value()
      {
         List<CashFlow> cashflows = CashFlowVectors.FloatingLeg<IborIndex, IborCoupon, CappedFlooredIborCoupon>(
                                       notionals_, schedule_, index_ as IborIndex, paymentDayCounter_,
                                       paymentAdjustment_, fixingDays_, gearings_, spreads_,
                                       caps_, floors_, inArrears_, zeroPayments_);

         if (caps_.Count == 0 && floors_.Count == 0 && !inArrears_)
         {
            Utils.setCouponPricer(cashflows, new BlackIborCouponPricer());
         }
         return cashflows;
      }
   }
}
