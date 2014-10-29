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
   public class OvernightIndexedCouponPricer : FloatingRateCouponPricer
   {
      private OvernightIndexedCoupon coupon_;

      public override void initialize(FloatingRateCoupon coupon)
      {
         coupon_ = coupon as OvernightIndexedCoupon;
         if (coupon_ == null)
            throw new ApplicationException("wrong coupon type");
      }
            
      public override double swapletRate()
      {
         OvernightIndex index = coupon_.index() as OvernightIndex;

         List<Date> fixingDates = coupon_.fixingDates();
         List<double> dt = coupon_.dt();

         int n = dt.Count();
         int i = 0;

         double compoundFactor = 1.0;

         // already fixed part
         Date today = Settings.evaluationDate();
         while (fixingDates[i]<today && i<n) 
         {
            // rate must have been fixed
            double pastFixing = IndexManager.instance().getHistory(
               index.name()).value()[fixingDates[i]];

            if (pastFixing == default(double) )
               throw new ApplicationException("Missing " + index.name() + " fixing for " 
                                              + fixingDates[i].ToString());

            compoundFactor *= (1.0 + pastFixing*dt[i]);
            ++i;
         }

         // today is a border case
         if (fixingDates[i] == today && i<n) 
         {
            // might have been fixed
            try 
            {
               double pastFixing = IndexManager.instance().getHistory(
                                             index.name()).value()[fixingDates[i]];
                     
               if (pastFixing != default(double)) 
               {
                  compoundFactor *= (1.0 + pastFixing*dt[i]);
                  ++i;
               } 
               else 
               {
                  ;   // fall through and forecast
               }
            } 
            catch (Exception) 
            {
               ;       // fall through and forecast
            }
         }
         
         // forward part using telescopic property in order
         // to avoid the evaluation of multiple forward fixings
         if (i<n) 
         {
            Handle<YieldTermStructure> curve = index.forwardingTermStructure();
            if (curve.empty())
               throw new ArgumentException("null term structure set to this instance of" +
                                           index.name());
                  
               List<Date> dates = coupon_.valueDates();
               double startDiscount = curve.link.discount(dates[i]);
               double endDiscount = curve.link.discount(dates[n]);

               compoundFactor *= startDiscount/endDiscount;
         }

         double rate = (compoundFactor - 1.0) / coupon_.accrualPeriod();
         return coupon_.gearing() * rate + coupon_.spread();
      }

      public override double swapletPrice() 
         { throw new ApplicationException("swapletPrice not available");  }
      public override double capletPrice(double d) 
         { throw new ApplicationException("capletPrice not available"); }
      public override double capletRate(double d) 
         { throw new ApplicationException("capletRate not available"); }
      public override double floorletPrice(double d) 
         { throw new ApplicationException("floorletPrice not available"); }
      public override double floorletRate(double d) 
         { throw new ApplicationException("floorletRate not available"); }
      protected override double optionletPrice(Option.Type t, double d)
      { throw new ApplicationException("optionletPrice not available"); }

   }

   public class OvernightIndexedCoupon : FloatingRateCoupon
   {
      public OvernightIndexedCoupon(
               Date paymentDate,
               double nominal,
               Date startDate,
               Date endDate,
               OvernightIndex overnightIndex,
               double gearing = 1.0,
               double spread = 0.0,
               Date refPeriodStart = null,
               Date refPeriodEnd = null,
               DayCounter dayCounter = null)
         : base(nominal, paymentDate,startDate, endDate,
                         overnightIndex.fixingDays(), overnightIndex,
                         gearing, spread,
                         refPeriodStart, refPeriodEnd,
                         dayCounter, false) 
      {
         // value dates
         Schedule sch = new MakeSchedule()
                      .from(startDate)
                      .to(endDate)
                      .withTenor(new Period(1,TimeUnit.Days))
                      .withCalendar(overnightIndex.fixingCalendar())
                      .withConvention(overnightIndex.businessDayConvention())
                      .backwards()
                      .value();
            
         valueDates_ = sch.dates();
         if (valueDates_.Count < 2)
            throw new ArgumentException("degenerate schedule");

         // fixing dates
         n_ = valueDates_.Count()-1;
         if (overnightIndex.fixingDays()==0) 
         {
            fixingDates_ = new List<Date>(valueDates_);
         }
         else 
         {
            fixingDates_ = new List<Date>(n_);
            for (int i=0; i<n_; ++i)
                fixingDates_[i] = overnightIndex.fixingDate(valueDates_[i]);
         }

         // accrual (compounding) periods
         dt_ = new List<double>(n_);
         DayCounter dc = overnightIndex.dayCounter();
         for (int i=0; i<n_; ++i)
            dt_.Add(dc.yearFraction(valueDates_[i], valueDates_[i+1]));

         setPricer(new OvernightIndexedCouponPricer());
   
      }

      public List<double> indexFixings()
      {
        fixings_ = new List<double>(n_);
        for (int i=0; i<n_; ++i)
            fixings_[i] = index_.fixing(fixingDates_[i]);
        return fixings_;
      }


      //! fixing dates for the rates to be compounded
      public List<Date> fixingDates() { return fixingDates_; }
      //! accrual (compounding) periods
      public List<double> dt() { return dt_; }
      //! value dates for the rates to be compounded
      public List<Date> valueDates() { return valueDates_; }
   
      private List<Date> valueDates_, fixingDates_;
      private List<double> fixings_;
      int n_;
      List<double> dt_;
   }

    //! helper class building a sequence of overnight coupons
   public class OvernightLeg : RateLegBase
    {
       public OvernightLeg(Schedule schedule,OvernightIndex overnightIndex)
       {
          schedule_ = schedule;
          overnightIndex_ = overnightIndex;
          paymentAdjustment_ = BusinessDayConvention.Following;
       }
       public new OvernightLeg withNotionals(double notional)
       {
          notionals_ = new List<double>(); notionals_.Add(notional);
          return this;
       }
       public new OvernightLeg withNotionals(List<double> notionals)
       {
          notionals_ = notionals;
          return this;
       }
       public OvernightLeg withPaymentDayCounter(DayCounter dayCounter)
       {
          paymentDayCounter_ = dayCounter;
          return this;
       }
       public new OvernightLeg withPaymentAdjustment(BusinessDayConvention convention)
       {
          paymentAdjustment_ = convention;
          return this;
       }
       public OvernightLeg withGearings(double gearing)
       {
          gearings_ = new List<double>(); gearings_.Add(gearing);
          return this;
       }
       public OvernightLeg withGearings(List<double> gearings)
       {
          gearings_ = gearings;
          return this;
       }
       public OvernightLeg withSpreads(double spread)
       {
          spreads_ = new List<double>(); spreads_.Add(spread);
          return this;
       }
       public OvernightLeg withSpreads(List<double> spreads)
       {
          spreads_ = spreads;
          return this;
       }

       public override List<CashFlow> value()
       {
          return CashFlowVectors.OvernightLeg(notionals_, schedule_, paymentAdjustment_, overnightIndex_, gearings_, spreads_, paymentDayCounter_);
       }

       //private Schedule schedule_;
       private OvernightIndex overnightIndex_;
       //private List<double> notionals_;
       //private DayCounter paymentDayCounter_;
       //private BusinessDayConvention paymentAdjustment_;
       private List<double> gearings_;
       private List<double> spreads_;
    };

}
