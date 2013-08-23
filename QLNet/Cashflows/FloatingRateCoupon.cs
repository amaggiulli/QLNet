/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

namespace QLNet
{
   public class FloatingRateCoupon : Coupon, IObserver
   {
      protected InterestRateIndex index_;
      protected DayCounter dayCounter_;
      protected int fixingDays_;
      protected double gearing_;
      protected double spread_;
      protected bool isInArrears_;
      protected FloatingRateCouponPricer pricer_;

      // constructors
      public FloatingRateCoupon(double nominal, 
                                Date paymentDate, 
                                Date startDate, 
                                Date endDate, 
                                int fixingDays, 
                                InterestRateIndex index,
                                double gearing = 1.0, 
                                double spread = 0.0, 
                                Date refPeriodStart = null, 
                                Date refPeriodEnd = null, 
                                DayCounter dayCounter = null, 
                                bool isInArrears = false) 
         : base(nominal, paymentDate, startDate, endDate, refPeriodStart, refPeriodEnd)
      {
         index_ = index;
         dayCounter_ = dayCounter == null ? new DayCounter() : dayCounter ;
         fixingDays_ = fixingDays == default(int) ? index.fixingDays() : fixingDays;
         gearing_ = gearing;
         spread_ = spread;
         isInArrears_ = isInArrears;

         if (gearing_ == 0) throw new ArgumentException("Null gearing not allowed");

         if (dayCounter_.empty())
            dayCounter_ = index_.dayCounter();

         // add as observer
         index_.registerWith(update);
         Settings.registerWith(update);
      }

      // need by CashFlowVectors
      public FloatingRateCoupon() { }

      public virtual void setPricer(FloatingRateCouponPricer pricer)
      {
         if (pricer_ != null)   // remove from the old observable
            pricer_.unregisterWith(update);

         pricer_ = pricer;

         if (pricer_ != null)
            pricer_.registerWith(update);      // add to observers of new pricer

         update();                                   // fire the change event to notify observers of this
      }

      public FloatingRateCouponPricer pricer() { return pricer_; }


      //////////////////////////////////////////////////////////////////////////////////////
      // CashFlow interface
      public override double amount()
      {
         double result = rate() * accrualPeriod() * nominal();
         return result;
      }


      //////////////////////////////////////////////////////////////////////////////////////
      // Coupon interface
      public override double rate()
      {
         if (pricer_ == null) throw new ArgumentException("pricer not set");
         pricer_.initialize(this);
         double result = pricer_.swapletRate();
         return result;
      }
      public override DayCounter dayCounter() { return dayCounter_; }
      public override double accruedAmount(Date d)
      {
         if (d <= accrualStartDate_ || d > paymentDate_)
         {
            return 0;
         }
         else
         {
            return nominal() * rate() *
                   dayCounter().yearFraction(accrualStartDate_, Date.Min(d, accrualEndDate_), refPeriodStart_, refPeriodEnd_);
         }
      }


      //////////////////////////////////////////////////////////////////////////////////////
      // properties
      public InterestRateIndex index() { return index_; }              //! floating index
      public int fixingDays { get { return fixingDays_; } }                   //! fixing days
      public virtual Date fixingDate()
      {                                               //! fixing date
         // if isInArrears_ fix at the end of period
         Date refDate = isInArrears_ ? accrualEndDate_ : accrualStartDate_;
         return index_.fixingCalendar().advance(refDate, -fixingDays_, TimeUnit.Days, BusinessDayConvention.Preceding);
      }
      public double gearing() { return gearing_; }                     //! index gearing, i.e. multiplicative coefficient for the index
      public double spread() { return spread_; }                       //! spread paid over the fixing of the underlying index
      //! fixing of the underlying index
      public virtual double indexFixing() { return index_.fixing(fixingDate()); }
      //! convexity-adjusted fixing
      public double adjustedFixing { get { return (rate() - spread()) / gearing(); } }
      //! whether or not the coupon fixes in arrears
      public bool isInArrears() { return isInArrears_; }


      // Observer interface
      public void update() { notifyObservers(); }


      //////////////////////////////////////////////////////////////////////////////////////
      // methods
      public double price(YieldTermStructure yts)
      {
         return amount() * yts.discount(date());
      }

      //! convexity adjustment for the given index fixing
      protected double convexityAdjustmentImpl(double f)
      {
         return (gearing() == 0 ? 0 : adjustedFixing - f);
      }

      //! convexity adjustment
      public virtual double convexityAdjustment()
      {
         return convexityAdjustmentImpl(indexFixing());
      }


      // Factory - for Leg generators
      public virtual CashFlow factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays,
                     InterestRateIndex index, double gearing, double spread,
                     Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears)
      {
         return new FloatingRateCoupon(nominal, paymentDate, startDate, endDate, fixingDays,
                    index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);
      }
   }
}