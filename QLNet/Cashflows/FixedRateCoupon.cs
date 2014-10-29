/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
   //! %Coupon paying a fixed interest rate
   public class FixedRateCoupon : Coupon 
   {
      // constructors
      public FixedRateCoupon(double nominal, Date paymentDate, double rate, DayCounter dayCounter,
                             Date accrualStartDate, Date accrualEndDate, 
                             Date refPeriodStart = null, Date refPeriodEnd = null,Date exCouponDate = null,double? amount = null)
			: base(nominal, paymentDate, accrualStartDate, accrualEndDate, refPeriodStart, refPeriodEnd, exCouponDate,amount) 
      {
         rate_ = new InterestRate(rate, dayCounter, Compounding.Simple,Frequency.Annual);
      }

      public FixedRateCoupon(double nominal, Date paymentDate, InterestRate interestRate, 
                             Date accrualStartDate, Date accrualEndDate,
									  Date refPeriodStart = null, Date refPeriodEnd = null, Date exCouponDate = null, double? amount = null) 
         : base(nominal, paymentDate, accrualStartDate, accrualEndDate, refPeriodStart, refPeriodEnd,exCouponDate, amount) 
      {
         rate_ = interestRate;
      }

      //! CashFlow interface
      public override double amount() 
      {
         if (amount_ != null)
            return amount_.Value;

            return nominal()*(rate_.compoundFactor(accrualStartDate_,
                                                   accrualEndDate_, refPeriodStart_, refPeriodEnd_) - 1.0); 
      }

      //! Coupon interface
      public override double rate() { return rate_.rate(); }
      public InterestRate interestRate() { return rate_; }
      public override DayCounter dayCounter() { return rate_.dayCounter(); }
      public override double accruedAmount(Date d) 
      {
         if (d <= accrualStartDate_ || d > paymentDate_)
            return 0;
			else if (tradingExCoupon(d)) 
			{
            return -nominal()*(rate_.compoundFactor(d,
                                                    accrualEndDate_,
                                                    refPeriodStart_,
                                                    refPeriodEnd_) - 1.0);
			}
         else
            return nominal() * (rate_.compoundFactor(accrualStartDate_, Date.Min(d, accrualEndDate_),
                                                     refPeriodStart_, refPeriodEnd_) - 1.0);
      }

      private InterestRate rate_;

   }

   //! helper class building a sequence of fixed rate coupons
   public class FixedRateLeg : RateLegBase 
   {
      // properties
      private List<InterestRate> couponRates_ = new List<InterestRate>();
      private DayCounter firstPeriodDC_ = null;
      private Calendar calendar_;
		private Period exCouponPeriod_;
      private   Calendar exCouponCalendar_;
      private   BusinessDayConvention exCouponAdjustment_;
      private   bool exCouponEndOfMonth_;

      // constructor
      public FixedRateLeg(Schedule schedule) 
      {
         schedule_ = schedule;
         calendar_ = schedule.calendar();
         paymentAdjustment_ = BusinessDayConvention.Following;
      }

      // other initializers
      public FixedRateLeg withCouponRates(double couponRate,DayCounter paymentDayCounter) 
      {
         return withCouponRates(couponRate,paymentDayCounter,Compounding.Simple,Frequency.Annual);
      }
      public FixedRateLeg withCouponRates(double couponRate,DayCounter paymentDayCounter,Compounding comp) 
      {
         return withCouponRates(couponRate,paymentDayCounter,comp,Frequency.Annual);
      }

      public FixedRateLeg withCouponRates(double couponRate,DayCounter paymentDayCounter,
                                          Compounding comp ,Frequency freq) 
      {
         couponRates_.Clear();
         couponRates_.Add(new InterestRate(couponRate, paymentDayCounter, comp, freq));
         return this;
      }


      public FixedRateLeg withCouponRates(List<double> couponRates, DayCounter paymentDayCounter)
      {
         return withCouponRates(couponRates, paymentDayCounter, Compounding.Simple, Frequency.Annual);
      }
      public FixedRateLeg withCouponRates(List<double> couponRates, DayCounter paymentDayCounter, Compounding comp)
      {
         return withCouponRates(couponRates, paymentDayCounter, comp, Frequency.Annual);
      }

      public FixedRateLeg withCouponRates(List<double> couponRates, DayCounter paymentDayCounter,
                                          Compounding comp, Frequency freq) 
      {
         couponRates_.Clear();
         foreach (double r in couponRates)
            couponRates_.Add(new InterestRate(r, paymentDayCounter, comp , freq));
         return this;
      }

      public FixedRateLeg withCouponRates(InterestRate couponRate)
      {
         couponRates_.Clear();
         couponRates_.Add(couponRate);
         return this;
      }

      public FixedRateLeg withCouponRates(List<InterestRate>couponRates) 
      {
         couponRates_ = couponRates;
         return this;
      }

      public FixedRateLeg withFirstPeriodDayCounter(DayCounter dayCounter) 
      {
         firstPeriodDC_ = dayCounter;
         return this;
      }

      public FixedRateLeg withPaymentCalendar(Calendar cal) 
      {
         calendar_ = cal;
         return this;
      }

		public FixedRateLeg withExCouponPeriod(Period period,Calendar cal,BusinessDayConvention convention,bool endOfMonth = false)
		{
			exCouponPeriod_ = period;
			exCouponCalendar_ = cal;
			exCouponAdjustment_ = convention;
			exCouponEndOfMonth_ = endOfMonth;
			return this;
		}

      // creator
      public override List<CashFlow> value() 
      {
      
         if (couponRates_.Count == 0) throw new ArgumentException("no coupon rates given");
         if (notionals_.Count == 0) throw new ArgumentException("no nominals given");

         List<CashFlow> leg = new List<CashFlow>();

         Calendar schCalendar = schedule_.calendar();

         // first period might be short or long
         Date start = schedule_[0], end = schedule_[1];
         Date paymentDate = calendar_.adjust(end, paymentAdjustment_);
			Date exCouponDate = null;
         InterestRate rate = couponRates_[0];
         double nominal = notionals_[0];

			if (exCouponPeriod_ != null)
			{
				exCouponDate = exCouponCalendar_.advance(paymentDate,
																	  -exCouponPeriod_,
																	  exCouponAdjustment_,
																	  exCouponEndOfMonth_);
			}
         if (schedule_.isRegular(1)) 
         {
            if (!(firstPeriodDC_ == null || firstPeriodDC_ == rate.dayCounter()))
                throw new ArgumentException("regular first coupon does not allow a first-period day count");
            leg.Add(new FixedRateCoupon(nominal, paymentDate, rate, start, end, start, end, exCouponDate));
         } 
         else 
         {
             Date refer = end - schedule_.tenor();
             refer = schCalendar.adjust(refer, schedule_.businessDayConvention());
             InterestRate r = new InterestRate(rate.rate(),
                                               (firstPeriodDC_ == null || firstPeriodDC_.empty()) ? rate.dayCounter() : firstPeriodDC_,
                                               rate.compounding(), rate.frequency());
				 leg.Add(new FixedRateCoupon(nominal, paymentDate, r, start, end, refer, end, exCouponDate));
         }

         // regular periods
         for (int i=2; i<schedule_.Count-1; ++i) 
         {
            start = end; end = schedule_[i];
            paymentDate = calendar_.adjust(end, paymentAdjustment_);
				if (exCouponPeriod_ != null)
				{
					exCouponDate = exCouponCalendar_.advance(paymentDate,
																		  -exCouponPeriod_,
																		  exCouponAdjustment_,
																		  exCouponEndOfMonth_);
				}
            if ((i - 1) < couponRates_.Count) rate = couponRates_[i - 1];
            else                              rate = couponRates_.Last();
            if ((i - 1) < notionals_.Count)   nominal = notionals_[i - 1];
            else                              nominal = notionals_.Last();

				leg.Add(new FixedRateCoupon(nominal, paymentDate, rate, start, end, start, end, exCouponDate));
         }

         if (schedule_.Count > 2) {
             // last period might be short or long
             int N = schedule_.Count;
             start = end; end = schedule_[N-1];
             paymentDate = calendar_.adjust(end, paymentAdjustment_);
				 if (exCouponPeriod_ != null)
				 {
					 exCouponDate = exCouponCalendar_.advance(paymentDate,
																			-exCouponPeriod_,
																			exCouponAdjustment_,
																			exCouponEndOfMonth_);
				 }

             if ((N - 2) < couponRates_.Count) rate = couponRates_[N - 2];
             else                              rate = couponRates_.Last();
             if ((N - 2) < notionals_.Count)   nominal = notionals_[N - 2];
             else                              nominal = notionals_.Last();

             if (schedule_.isRegular(N-1))
					 leg.Add(new FixedRateCoupon(nominal, paymentDate, rate, start, end, start, end, exCouponDate));
             else {
                 Date refer = start + schedule_.tenor();
                 refer = schCalendar.adjust(refer, schedule_.businessDayConvention());
					  leg.Add(new FixedRateCoupon(nominal, paymentDate, rate, start, end, start, refer, exCouponDate));
             }
         }
         return leg;
     }
    }
}
