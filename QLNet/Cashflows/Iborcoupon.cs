/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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
   // Coupon paying a Libor-type index
   public class IborCoupon : FloatingRateCoupon
   {
      private IborIndex iborIndex_;

      public IborCoupon() { }

      public IborCoupon(double nominal, 
                        Date paymentDate, 
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
         base(nominal, paymentDate, startDate, endDate, fixingDays, iborIndex, gearing, spread,
                   refPeriodStart, refPeriodEnd, dayCounter, isInArrears)
      {
         iborIndex_ = iborIndex;
      }

      //! Implemented in order to manage the case of par coupon
      public override double indexFixing()
      {
#if QL_USE_INDEXED_COUPON
            return index_.fixing(fixingDate());
#else
         if (isInArrears())
         {
            return index_.fixing(fixingDate());
         }
         else
         {
            Date today = Settings.evaluationDate();
            Date fixingDate = this.fixingDate();

            TimeSeries<double> fixings = IndexManager.instance().getHistory(index_.name()).value();
            if (fixings.ContainsKey(fixingDate))
            {
               return fixings[fixingDate];
            }
            else
            {
               if (fixingDate < today)
               {
                  // must have been fixed
                  if (IndexManager.MissingPastFixingCallBack == null)
                  {
                     throw new ArgumentException("Missing " + index_.name() + " fixing for " + fixingDate);
                  }
                  else
                  {
                     // try to load missing fixing from external source
                     double fixing = IndexManager.MissingPastFixingCallBack(index_, fixingDate);
                     // add to history
                     index_.addFixing(fixingDate, fixing);
                     return fixing;
                  }
               }
               if (fixingDate == today)
               {
                  // might have been fixed
                  // fall through and forecast
               }
            }

            // forecast: 0) forecasting curve
            Handle<YieldTermStructure> termStructure = iborIndex_.forwardingTermStructure();
            if (termStructure.empty())
               throw new ApplicationException("null term structure set to this instance of " +
                                              index_.name());
            // forecast: 1) startDiscount
            Date fixingValueDate = index_.fixingCalendar().advance(fixingDate, index_.fixingDays(), TimeUnit.Days);
            double startDiscount = termStructure.link.discount(fixingValueDate);
            // forecast: 2) endDiscount
            Date nextFixingDate = index_.fixingCalendar().advance(accrualEndDate_, -fixingDays, TimeUnit.Days);
            Date nextFixingValueDate = index_.fixingCalendar().advance(nextFixingDate, index_.fixingDays(), TimeUnit.Days);
            double endDiscount = termStructure.link.discount(nextFixingValueDate);
            // forecast: 3) spanningTime
            double spanningTime = index_.dayCounter().yearFraction(fixingValueDate, nextFixingValueDate);
            if (!(spanningTime > 0.0))
               throw new ApplicationException("cannot calculate forward rate between " +
                      fixingValueDate + " and " + nextFixingValueDate +
                      ": non positive time using " + index_.dayCounter().name());
            // forecast: 4) implied fixing
            return (startDiscount / endDiscount - 1.0) / spanningTime;
         }
#endif
      }

      // Factory - for Leg generators
      public override CashFlow factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays,
                     InterestRateIndex index, double gearing, double spread,
                     Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears)
      {
         return new IborCoupon(nominal, paymentDate, startDate, endDate, fixingDays,
                    (IborIndex)index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);
      }
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
