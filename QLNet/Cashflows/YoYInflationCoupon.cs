/*
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
   //! %Coupon paying a YoY-inflation type index
   public class YoYInflationCoupon : InflationCoupon
   {
      public YoYInflationCoupon(Date paymentDate,
                                double nominal,
                                Date startDate,
                                Date endDate,
                                int fixingDays,
                                YoYInflationIndex yoyIndex,
                                Period observationLag,
                                DayCounter dayCounter,
                                double gearing = 1.0,
                                double spread = 0.0,
                                Date refPeriodStart = null,
                                Date refPeriodEnd = null )
         :base(paymentDate, nominal, startDate, endDate,
               fixingDays, yoyIndex, observationLag,
               dayCounter, refPeriodStart, refPeriodEnd)
      {
         yoyIndex_ = yoyIndex; 
         gearing_ = gearing;
         spread_ = spread;
      }
 
      //! \name Inspectors
      //@{
      //! index gearing, i.e. multiplicative coefficient for the index
      public double gearing() { return gearing_; }
      //! spread paid over the fixing of the underlying index
      public double spread() { return spread_; }
      public double adjustedFixing() { return (rate() - spread()) / gearing(); }
      public YoYInflationIndex yoyIndex() { return yoyIndex_; }
      //@}

      private YoYInflationIndex yoyIndex_;
      protected double gearing_;
      protected double spread_;

      protected override bool checkPricerImpl(InflationCouponPricer i)
      {
         return (i is YoYInflationCouponPricer);
      }
   }


   //! Helper class building a sequence of capped/floored yoy inflation coupons
   //! payoff is: spread + gearing x index
   public class yoyInflationLeg : yoyInflationLegBase
   {
      public yoyInflationLeg(Schedule schedule,Calendar cal,
                             YoYInflationIndex index,
                             Period observationLag)
      {
         schedule_ = schedule;
         index_ = index;
         observationLag_ = observationLag;
         paymentAdjustment_ = BusinessDayConvention.ModifiedFollowing;
         paymentCalendar_ = cal;
      }


      public override List<CashFlow> value()
      {
         return CashFlowVectors.yoyInflationLeg(notionals_, 
                                                schedule_, 
                                                paymentAdjustment_, 
                                                index_, 
                                                gearings_, 
                                                spreads_, 
                                                paymentDayCounter_,
                                                caps_, 
                                                floors_ ,
                                                paymentCalendar_,
                                                fixingDays_,
                                                observationLag_);
      }

    };
}
