/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)  
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet 
{
   //! %coupon accruing over a fixed period
   //! This class implements part of the CashFlow interface but it is
   //  still abstract and provides derived classes with methods for accrual period calculations.
   public abstract class Coupon : CashFlow 
   {
      protected double nominal_;
      protected double? amount_ = null;
		protected Date paymentDate_, accrualStartDate_, accrualEndDate_, refPeriodStart_, refPeriodEnd_, exCouponDate_;

      // access to properties
		public override Date exCouponDate() { return exCouponDate_; }
      public double nominal() { return nominal_; }
      public override Date date() { return paymentDate_; }
      public Date accrualStartDate() { return accrualStartDate_; }
      public Date accrualEndDate() { return accrualEndDate_; }
      public Date refPeriodStart { get { return refPeriodStart_; } }
      public Date refPeriodEnd { get { return refPeriodEnd_; } }

      // virtual get methods to be defined in derived classes
      public abstract double rate();                   //! accrued rate
      public abstract DayCounter dayCounter();         //! day counter for accrual calculation
      public abstract double accruedAmount(Date d);         //! accrued amount at the given date
      //public virtual FloatingRateCouponPricer pricer() { return null; }


      // Constructors
      public Coupon() { }       // default constructor
      // coupon does not adjust the payment date which must already be a business day
      public Coupon(double nominal, Date paymentDate, Date accrualStartDate, Date accrualEndDate, 
                    Date refPeriodStart = null, Date refPeriodEnd = null, Date exCouponDate = null, double? amount = null) 
      {
         nominal_ = nominal;
         amount_ = amount;
         paymentDate_ = paymentDate;
         accrualStartDate_ = accrualStartDate;
         accrualEndDate_ = accrualEndDate;
         refPeriodStart_ = refPeriodStart;
         refPeriodEnd_ = refPeriodEnd;
			exCouponDate_ = exCouponDate;

         if (refPeriodStart_ == null) refPeriodStart_ = accrualStartDate_;
         if (refPeriodEnd_ == null) refPeriodEnd_ = accrualEndDate_;
      }


      //! accrual period as fraction of year
      public double accrualPeriod() 
      {
         return dayCounter().yearFraction(accrualStartDate_, accrualEndDate_, refPeriodStart_, refPeriodEnd_);
      }

      //! accrual period in days
      public int accrualDays() 
      { 
         return dayCounter().dayCount(accrualStartDate_, accrualEndDate_); 
      }


      //! accrued period as fraction of year at the given date
      public double accruedPeriod(Date d) 
      {
        if (d <= accrualStartDate_ || d > paymentDate_) 
           return 0.0;
        else 
           return dayCounter().yearFraction(accrualStartDate_,
                                             Date.Min(d, accrualEndDate_),
                                             refPeriodStart_,
                                             refPeriodEnd_);

      }
      //! accrued days at the given date
      public int accruedDays(Date d) 
      {
         if (d <= accrualStartDate_ || d > paymentDate_) 
            return 0;
         else 
            return dayCounter().dayCount(accrualStartDate_,Date.Min(d, accrualEndDate_));
      }

   }
}
