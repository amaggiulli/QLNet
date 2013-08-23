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
   // helper class
   // This class provides a more comfortable way to instantiate standard basis swap.
   public class MakeBasisSwap
   {
      private Period forwardStart_, swapTenor_;
      private IborIndex iborIndex1_, iborIndex2_;

      private Date effectiveDate_, terminationDate_;
      private Calendar float1Calendar_, float2Calendar_;

      private BasisSwap.Type type_;
      private double nominal_;
      private Period float1Tenor_, float2Tenor_;
      private BusinessDayConvention float1Convention_, float1TerminationDateConvention_;
      private BusinessDayConvention float2Convention_, float2TerminationDateConvention_;
      private DateGeneration.Rule float1Rule_, float2Rule_;
      private bool float1EndOfMonth_, float2EndOfMonth_;
      private Date float1FirstDate_, float1NextToLastDate_;
      private Date float2FirstDate_, float2NextToLastDate_;
      private double float1Spread_, float2Spread_;
      private DayCounter float1DayCount_, float2DayCount_;

      IPricingEngine engine_;


      public MakeBasisSwap(Period swapTenor, IborIndex index1, IborIndex index2) :
         this(swapTenor, index1, index2, new Period(0, TimeUnit.Days)) { }
      public MakeBasisSwap(Period swapTenor, IborIndex index1, IborIndex index2, Period forwardStart)
      {
         swapTenor_ = swapTenor;
         iborIndex1_ = index1;
         iborIndex2_ = index2;
         forwardStart_ = forwardStart;
         effectiveDate_ = null;
         float1Calendar_ = float2Calendar_ = index1.fixingCalendar();

         type_ = BasisSwap.Type.Payer;
         nominal_ = 1.0;
         float1Tenor_ = index1.tenor();
         float2Tenor_ = index2.tenor();
         float1Convention_ = float1TerminationDateConvention_ = index1.businessDayConvention();
         float2Convention_ = float2TerminationDateConvention_ = index2.businessDayConvention();
         float1Rule_ = float2Rule_ = DateGeneration.Rule.Backward;
         float1EndOfMonth_ = float2EndOfMonth_ = false;
         float1FirstDate_ = float1NextToLastDate_ = float2FirstDate_ = float2NextToLastDate_ = null;
         float1Spread_ = float2Spread_ = 0.0;
         float1DayCount_ = index1.dayCounter();
         float2DayCount_ = index2.dayCounter();

         engine_ = new DiscountingBasisSwapEngine(index1.forwardingTermStructure(), index2.forwardingTermStructure());
      }

      public MakeBasisSwap receiveFixed() { return receiveFixed(true); }
      public MakeBasisSwap receiveFixed(bool flag)
      {
         type_ = flag ? BasisSwap.Type.Receiver : BasisSwap.Type.Payer;
         return this;
      }
      public MakeBasisSwap withType(BasisSwap.Type type)
      {
         type_ = type;
         return this;
      }
      public MakeBasisSwap withNominal(double n)
      {
         nominal_ = n;
         return this;
      }
      public MakeBasisSwap withEffectiveDate(Date effectiveDate)
      {
         effectiveDate_ = effectiveDate;
         return this;
      }

      public MakeBasisSwap withTerminationDate(Date terminationDate)
      {
         terminationDate_ = terminationDate;
         return this;
      }

      public MakeBasisSwap withRule(DateGeneration.Rule r)
      {
         float1Rule_ = r;
         float2Rule_ = r;
         return this;
      }

      public MakeBasisSwap withDiscountingTermStructure(Handle<YieldTermStructure> discountingTermStructure1,
                                                        Handle<YieldTermStructure> discountingTermStructure2)  
      {
         engine_ = new DiscountingBasisSwapEngine(discountingTermStructure1, discountingTermStructure2);
         return this;
      }

      public MakeBasisSwap withFloating1LegTenor(Period t)
      {
         float1Tenor_ = t;
         return this;
      }
      public MakeBasisSwap withFloating1Calendar(Calendar cal)
      {
         float1Calendar_ = cal;
         return this;
      }
      public MakeBasisSwap withFloating1LegConvention(BusinessDayConvention bdc)
      {
         float1Convention_ = bdc;
         return this;
      }
      public MakeBasisSwap withFloating1LegTerminationDateConvention(BusinessDayConvention bdc)
      {
         float1TerminationDateConvention_ = bdc;
         return this;
      }
      public MakeBasisSwap withFloating1LegRule(DateGeneration.Rule r)
      {
         float1Rule_ = r;
         return this;
      }
      public MakeBasisSwap withFloating1LegEndOfMonth() { return withFloating1LegEndOfMonth(true); }
      public MakeBasisSwap withFloating1LegEndOfMonth(bool flag)
      {
         float1EndOfMonth_ = flag;
         return this;
      }
      public MakeBasisSwap withFloating1LegFirstDate(Date d)
      {
         float1FirstDate_ = d;
         return this;
      }
      public MakeBasisSwap withFloating1LegNextToLastDate(Date d)
      {
         float1NextToLastDate_ = d;
         return this;
      }
      public MakeBasisSwap withFloating1LegDayCount(DayCounter dc)
      {
         float1DayCount_ = dc;
         return this;
      }


      // *****

      public MakeBasisSwap withFloating2LegTenor(Period t)
      {
         float2Tenor_ = t;
         return this;
      }
      public MakeBasisSwap withFloating2LegCalendar(Calendar cal)
      {
         float2Calendar_ = cal;
         return this;
      }
      public MakeBasisSwap withFloating2LegConvention(BusinessDayConvention bdc)
      {
         float2Convention_ = bdc;
         return this;
      }
      public MakeBasisSwap withFloating2LegTerminationDateConvention(BusinessDayConvention bdc)
      {
         float2TerminationDateConvention_ = bdc;
         return this;
      }
      public MakeBasisSwap withFloating2LegRule(DateGeneration.Rule r)
      {
         float2Rule_ = r;
         return this;
      }
      public MakeBasisSwap withFloating2LegEndOfMonth() { return withFloating2LegEndOfMonth(true); }
      public MakeBasisSwap withFloating2LegEndOfMonth(bool flag)
      {
         float2EndOfMonth_ = flag;
         return this;
      }
      public MakeBasisSwap withFloating2LegFirstDate(Date d)
      {
         float2FirstDate_ = d;
         return this;
      }
      public MakeBasisSwap withFloating2LegNextToLastDate(Date d)
      {
         float2NextToLastDate_ = d;
         return this;
      }
      public MakeBasisSwap withFloating2LegDayCount(DayCounter dc)
      {
         float2DayCount_ = dc;
         return this;
      }
      public MakeBasisSwap withFloating2LegSpread(double sp)
      {
         float2Spread_ = sp;
         return this;
      }


      // swap creator
      public static implicit operator BasisSwap(MakeBasisSwap o) { return o.value(); }
      public BasisSwap value()
      {
         Date startDate;

         if (effectiveDate_ != null)
            startDate = effectiveDate_;
         else
         {
            int fixingDays = iborIndex1_.fixingDays();
            Date referenceDate = Settings.evaluationDate();
            Date spotDate = float1Calendar_.advance(referenceDate, new Period(fixingDays, TimeUnit.Days));
            startDate = spotDate + forwardStart_;
         }

         Date endDate;
         if (terminationDate_ != null)
            endDate = terminationDate_;
         else
            endDate = startDate + swapTenor_;


         Schedule float1Schedule = new Schedule(startDate, endDate,
                                float1Tenor_, float1Calendar_,
                                float1Convention_, float1TerminationDateConvention_,
                                float1Rule_, float1EndOfMonth_,
                                float1FirstDate_, float1NextToLastDate_);

         Schedule float2Schedule = new Schedule(startDate, endDate,
                                float2Tenor_, float2Calendar_,
                                float2Convention_, float2TerminationDateConvention_,
                                float2Rule_, float2EndOfMonth_,
                                float2FirstDate_, float2NextToLastDate_);


         BasisSwap swap = new BasisSwap(type_, nominal_, 
                                        float1Schedule, iborIndex1_, float1Spread_,float1DayCount_,
                                        float2Schedule, iborIndex2_, float2Spread_, float2DayCount_);
         swap.setPricingEngine(engine_);
         return swap;
      }
   }
}
