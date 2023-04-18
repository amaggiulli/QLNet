//  Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.using System;
using System.Linq;
namespace QLNet
{
   // Rate helper for bootstrapping over ibor-ibor basis swaps
   /* The swap is assumed to pay baseIndex + basis and receive
      otherIndex.  The helper can be used to bootstrap the forecast
      curve for baseIndex (in which case you'll have to pass
      bootstrapBaseCurve = true and provide otherIndex with a
      forecast curve) or the forecast curve for otherIndex (in which
      case bootstrapBaseCurve = false and baseIndex will need a
      forecast curve).
      In both cases, an exogenous discount curve is required.
   */
   public class IborIborBasisSwapRateHelper : RelativeDateRateHelper
   {
      private Period tenor_;
      private int settlementDays_;
      private Calendar calendar_;
      private BusinessDayConvention convention_;
      private bool endOfMonth_;
      private IborIndex baseIndex_;
      private IborIndex otherIndex_;
      private Handle<YieldTermStructure> discountHandle_;
      private bool bootstrapBaseCurve_;
      private Swap swap_;
      private RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

      public IborIborBasisSwapRateHelper(Handle<Quote> basis, Period tenor, int settlementDays, Calendar calendar,
         BusinessDayConvention convention,
         bool endOfMonth, IborIndex baseIndex, IborIndex otherIndex, Handle<YieldTermStructure> discountHandle,
         bool bootstrapBaseCurve) : base(basis)
      {
         tenor_ = tenor;
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         convention_ = convention;
         endOfMonth_ = endOfMonth;
         discountHandle_ = discountHandle;
         bootstrapBaseCurve_ = bootstrapBaseCurve;

         // we need to clone the index whose forecast curve we want to bootstrap
         // and copy the other one
         if (bootstrapBaseCurve_)
         {
            baseIndex_ = baseIndex.clone(termStructureHandle_);
            baseIndex_.unregisterWith(update);
            otherIndex_ = otherIndex;
         }
         else
         {
            baseIndex_ = baseIndex;
            otherIndex_ = otherIndex.clone(termStructureHandle_);
            otherIndex_.unregisterWith(update);
         }

         baseIndex_.registerWith(update);
         otherIndex_.registerWith(update);
         discountHandle_.registerWith(update);

         initializeDates();
      }

      public override double impliedQuote()
      {
         swap_.recalculate();
         return (double)(-(swap_.NPV() / swap_.legBPS(0)) * 1.0e-4);
      }

      protected sealed override void initializeDates()
      {
         Date today = Settings.evaluationDate();
         earliestDate_ = calendar_.advance(today, new Period(settlementDays_, TimeUnit.Days), BusinessDayConvention.Following);
         maturityDate_ = calendar_.advance(earliestDate_, tenor_, convention_);

         Schedule baseSchedule =
            new MakeSchedule().from(earliestDate_).to(maturityDate_)
               .withTenor(baseIndex_.tenor())
               .withCalendar(calendar_)
               .withConvention(convention_)
               .endOfMonth(endOfMonth_)
               .forwards().value();
         var baseLeg = new IborLeg(baseSchedule, baseIndex_).withNotionals(100.0);
         var lastBaseCoupon = baseLeg.value().LastOrDefault() as IborCoupon;

         Schedule otherSchedule =
            new MakeSchedule().from(earliestDate_).to(maturityDate_)
               .withTenor(otherIndex_.tenor())
               .withCalendar(calendar_)
               .withConvention(convention_)
               .endOfMonth(endOfMonth_)
               .forwards().value();
         var otherLeg = new IborLeg(otherSchedule, otherIndex_).withNotionals(100.0);
         var lastOtherCoupon = otherLeg.value().LastOrDefault() as IborCoupon;

         latestRelevantDate_ = Date.Max(maturityDate_, Date.Max(lastBaseCoupon.fixingEndDate(), lastOtherCoupon.fixingEndDate()));
         pillarDate_ = latestRelevantDate_;

         swap_ = new Swap(baseLeg, otherLeg);
         swap_.setPricingEngine(new DiscountingSwapEngine(discountHandle_));
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer -
         // force recalculation when needed---the index is not lazy
         bool observer = false;
         termStructureHandle_.linkTo(t, observer);
         base.setTermStructure(t);
      }
   }

   //! Rate helper for bootstrapping over overnight-ibor basis swaps
   /*! The swap is assumed to pay baseIndex + basis and receive
       otherIndex.  This helper can be used to bootstrap the forecast
       curve for otherIndex; baseIndex will need an existing forecast
       curve.  An exogenous discount curve can be passed; if not,
       the overnight-index curve will be used.
   */
   public class OvernightIborBasisSwapRateHelper : RelativeDateRateHelper
   {
      private Period tenor_;
      private int settlementDays_;
      private Calendar calendar_;
      private BusinessDayConvention convention_;
      private bool endOfMonth_;
      private OvernightIndex baseIndex_;
      private IborIndex otherIndex_;
      private Handle<YieldTermStructure> discountHandle_;
      private Swap swap_;
      private RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

      public OvernightIborBasisSwapRateHelper(Handle<Quote> basis, Period tenor, int settlementDays, Calendar calendar,
         BusinessDayConvention convention, bool endOfMonth, OvernightIndex baseIndex, IborIndex otherIndex,
         Handle<YieldTermStructure> discountHandle = null)
      :base(basis)
      {
         tenor_ = tenor;
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         convention_ = convention;
         endOfMonth_ = endOfMonth;
         discountHandle_ = discountHandle;

         // we need to clone the index whose forecast curve we want to bootstrap
         // and copy the other one
         baseIndex_ = baseIndex;
         otherIndex_ = otherIndex.clone(termStructureHandle_);
         otherIndex_.unregisterWith(update);

         baseIndex_.registerWith(update);
         otherIndex_.registerWith(update);
         discountHandle_.registerWith(update);

         initializeDates();
      }

      protected override void initializeDates()
      {
         Date today = Settings.evaluationDate();
         earliestDate_ = calendar_.advance(today, new Period(settlementDays_,TimeUnit.Days), BusinessDayConvention.Following);
         maturityDate_ = calendar_.advance(earliestDate_, tenor_, convention_);

         Schedule schedule =
            new MakeSchedule().from(earliestDate_).to(maturityDate_)
               .withTenor(otherIndex_.tenor())
               .withCalendar(calendar_)
               .withConvention(convention_)
               .endOfMonth(endOfMonth_)
               .forwards().value();

         var baseLeg = new OvernightLeg(schedule, baseIndex_).withNotionals(100.0);

         var otherLeg = new IborLeg(schedule, otherIndex_).withNotionals(100.0);
         var lastOtherCoupon = otherLeg.value().LastOrDefault() as IborCoupon;

         latestRelevantDate_ = Date.Max(maturityDate_, lastOtherCoupon.fixingEndDate());
         pillarDate_ = latestRelevantDate_;

         swap_ = new Swap(baseLeg, otherLeg);
         swap_.setPricingEngine(new DiscountingSwapEngine(discountHandle_.empty() ? termStructureHandle_ : discountHandle_));
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer -
         // force recalculation when needed---the index is not lazy
         bool observer = false;
         termStructureHandle_.linkTo(t, observer);
         base.setTermStructure(t);
      }

      public override double impliedQuote()
      {
         swap_.recalculate();
         return (double)(- (swap_.NPV() / swap_.legBPS(0)) * 1.0e-4);
      }
   }
}
