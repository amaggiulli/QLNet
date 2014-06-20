/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 * 
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
   //! Base inflation-coupon class
   /*! The day counter is usually obtained from the inflation term
       structure that the inflation index uses for forecasting.
       There is no gearing or spread because these are relevant for
       YoY coupons but not zero inflation coupons.

       \note inflation indices do not contain day counters or calendars.
   */
   public class InflationCoupon : Coupon,IObserver
   {

      public InflationCoupon(Date paymentDate,
                             double nominal,
                             Date startDate,
                             Date endDate,
                             int fixingDays,
                             InflationIndex index,
                             Period observationLag,
                             DayCounter dayCounter,
                             Date refPeriodStart = null,
                             Date refPeriodEnd = null,
                             Date exCouponDate = null)
         : base(nominal, paymentDate, startDate, endDate, refPeriodStart, refPeriodEnd, exCouponDate)  // ref period is before lag
      {
         index_ = index;
         observationLag_ = observationLag;
         dayCounter_= dayCounter;
         fixingDays_ = fixingDays;

         index_.registerWith(update);
         Settings.registerWith(update);
      }

      //! \name CashFlow interface
      //@{
      public override double amount()
      {
         return rate() * accrualPeriod() * nominal();
      }
     //! \name Coupon interface
     //@{
     double price(Handle<YieldTermStructure> discountingCurve) 
     {
        return amount() * discountingCurve.link.discount(date());
     }
     public override DayCounter dayCounter() { return dayCounter_; }
     public override double accruedAmount(Date d) 
     {
        if (d <= accrualStartDate_ || d > paymentDate_) {
            return 0.0;
        } else {
            return nominal() * rate() *
            dayCounter().yearFraction(accrualStartDate_,
                                      d < accrualEndDate_ ? d : accrualEndDate_, //Math.Min(d, accrualEndDate_),
                                      refPeriodStart_,
                                      refPeriodEnd_);
        }
     }
     public override double rate()
     {
        if (pricer_ == null)
           throw new ApplicationException("pricer not set");

        // we know it is the correct type because checkPricerImpl checks on setting
        // in general pricer_ will be a derived class, as will *this on calling
        pricer_.initialize(this);
        return pricer_.swapletRate();
     }
     //@}

      //! \name Inspectors
     //@{
     //! yoy inflation index
     public InflationIndex index() { return index_; }
     //! how the coupon observes the index
     public Period observationLag() { return observationLag_; }
     //! fixing days
     public int fixingDays() { return fixingDays_; }
     //! fixing date
     public virtual Date fixingDate() 
     {
        // fixing calendar is usually the null calendar for inflation indices
        return index_.fixingCalendar().advance(refPeriodEnd_ - observationLag_,
                        -(fixingDays_), TimeUnit.Days,BusinessDayConvention.ModifiedPreceding);
     }
     //! fixing of the underlying index, as observed by the coupon
     public virtual double indexFixing()
     {
        return index_.fixing(fixingDate());
     }
     //@}


      public void update() { notifyObservers(); }

      public void setPricer(InflationCouponPricer pricer) 
      {
         if (!checkPricerImpl(pricer))
            throw new ApplicationException("pricer given is wrong type");

         if (pricer_ != null)
            pricer_.unregisterWith(update);
        pricer_ = pricer;
        if (pricer_ != null)
           pricer_.registerWith(update);
        update();
      }

      public InflationCouponPricer pricer() {return pricer_;}

      protected InflationCouponPricer pricer_;
      protected InflationIndex index_;
      protected Period observationLag_;
      protected DayCounter dayCounter_;
      protected int fixingDays_;

      //! makes sure you were given the correct type of pricer
      // this can also done in external pricer setter classes via
      // accept/visit mechanism
      protected virtual bool checkPricerImpl(InflationCouponPricer i) { return false; }

   }
}
