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
   //! helper class
   /*! This class provides a more comfortable way
       to instantiate overnight indexed swaps.
   */
   public class MakeOIS
   {
      private Period swapTenor_;
      private OvernightIndex overnightIndex_;
      private double? fixedRate_;
      private Period forwardStart_;

      private int fixingDays_;
      private Date effectiveDate_, terminationDate_;
      private Frequency paymentFrequency_;
      DateGeneration.Rule rule_;
      private bool endOfMonth_;

      private OvernightIndexedSwap.Type type_;
      private double nominal_;

      private double overnightSpread_;
      private DayCounter fixedDayCount_;

      private IPricingEngine engine_;

      public MakeOIS(Period swapTenor, OvernightIndex overnightIndex,
                     double? fixedRate) : this(swapTenor,overnightIndex,fixedRate,new Period (0,TimeUnit.Days))
      {}

      public MakeOIS(Period swapTenor, OvernightIndex overnightIndex) 
         : this(swapTenor, overnightIndex, null, new Period(0, TimeUnit.Days))
      {}

      public MakeOIS(Period swapTenor, OvernightIndex overnightIndex,
                     double? fixedRate, Period fwdStart)
      {
         swapTenor_=swapTenor;
         overnightIndex_ = overnightIndex;
         fixedRate_= fixedRate;
         forwardStart_= fwdStart;
         fixingDays_ = 2;
         paymentFrequency_ = Frequency.Annual;
         rule_ = DateGeneration.Rule.Backward;
         endOfMonth_ = (new Period(1,TimeUnit.Months)<=swapTenor && swapTenor<=new Period(2,TimeUnit.Years) ? true : false);
         type_ = OvernightIndexedSwap.Type.Payer;
         nominal_ = 1.0;
         overnightSpread_ = 0.0;
         fixedDayCount_ = overnightIndex.dayCounter();
         engine_ = new DiscountingSwapEngine(overnightIndex_.forwardingTermStructure());

      }

      public MakeOIS receiveFixed(bool flag) 
      {
        type_ = flag ? OvernightIndexedSwap.Type.Receiver : OvernightIndexedSwap.Type.Payer ;
        return this;
      }

      public MakeOIS withType(OvernightIndexedSwap.Type type) 
      {
         type_ = type;
         return this;
      }

      public MakeOIS withSettlementDays(int fixingDays) 
      {
         fixingDays_ = fixingDays;
         effectiveDate_ = null; // new Date();
         return this;
      }

      public MakeOIS withEffectiveDate(Date effectiveDate)
      {
         effectiveDate_ = effectiveDate;
         return this;
      }

      public MakeOIS withTerminationDate(Date terminationDate) 
      {
         terminationDate_ = terminationDate;
         swapTenor_ = new Period();
         return this;
      }

      public MakeOIS withPaymentFrequency(Frequency f)
      {
         paymentFrequency_ = f;
         if (paymentFrequency_== Frequency.Once)
            rule_ = DateGeneration.Rule.Zero;
         return this;
      }

      public MakeOIS withRule(DateGeneration.Rule r) 
      {
         rule_ = r;
         if (r==DateGeneration.Rule.Zero)
            paymentFrequency_ = Frequency.Once;
         return this;
      }

      public MakeOIS withOvernightLegSpread(double sp)
      {
         overnightSpread_ = sp;
         return this;
      }

      public MakeOIS withNominal(double n)
      {
         nominal_ = n;
         return this;
      }

      public MakeOIS withDiscountingTermStructure(Handle<YieldTermStructure> discountingTermStructure)
      {

         engine_ = (IPricingEngine) new DiscountingSwapEngine(discountingTermStructure);

         return this;
      }

      public MakeOIS withFixedLegDayCount(DayCounter dc) 
      {
         fixedDayCount_ = dc;
         return this;
      }

      public MakeOIS withEndOfMonth(bool flag) 
      {
         endOfMonth_ = flag;
         return this;
      }

      // OIswap creator
      public static implicit operator OvernightIndexedSwap(MakeOIS o) { return o.value(); }

      public OvernightIndexedSwap value()
      {
         Calendar calendar = overnightIndex_.fixingCalendar();
         Date startDate;

         if (effectiveDate_ != null)
            startDate = effectiveDate_;
         else
         {
            Date referenceDate = Settings.evaluationDate();
            Date spotDate = calendar.advance(referenceDate,
                                             new Period(fixingDays_,TimeUnit.Days));
            startDate = spotDate+forwardStart_;
         }

         Date endDate;
         if (terminationDate_ != null)
            endDate = terminationDate_;
         else
         {
            if (endOfMonth_)
            {
               endDate = calendar.advance(startDate, swapTenor_,
                                          BusinessDayConvention.ModifiedFollowing,
                                          endOfMonth_);
            }
            else
            {
               endDate = startDate + swapTenor_;
            }

         }

        Schedule schedule = new Schedule(startDate, endDate,
                          new Period(paymentFrequency_),
                          calendar,
                          BusinessDayConvention.ModifiedFollowing,
                          BusinessDayConvention.ModifiedFollowing,
                          rule_,
                          endOfMonth_);

        double? usedFixedRate = fixedRate_;
        if (fixedRate_ == null) 
        {
           if (overnightIndex_.forwardingTermStructure().empty())
           {
              throw new ApplicationException("null term structure set to this instance of " 
                                             + overnightIndex_.name());
           }

           OvernightIndexedSwap temp = new OvernightIndexedSwap(type_, nominal_,
                                           schedule,
                                           0.0, // fixed rate
                                           fixedDayCount_,
                                           overnightIndex_, overnightSpread_);

            // ATM on the forecasting curve
            //bool includeSettlementDateFlows = false;
            temp.setPricingEngine(new DiscountingSwapEngine(
                                   overnightIndex_.forwardingTermStructure()));
            usedFixedRate = temp.fairRate();
        }

        OvernightIndexedSwap ois = new OvernightIndexedSwap(type_, nominal_,
                                 schedule,
                                 usedFixedRate.Value, fixedDayCount_,
                                 overnightIndex_, overnightSpread_);

        ois.setPricingEngine(engine_);
        return ois;
      }
   }
}
