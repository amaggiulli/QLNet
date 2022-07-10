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
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System;

namespace QLNet
{
   // RateHelper for bootstrapping over overnight compounding futures
   public class OvernightIndexFutureRateHelper : RateHelper
   {
      private OvernightIndexFuture future_;
      RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

      public OvernightIndexFutureRateHelper(Handle<Quote> price,
         // first day of reference period
         Date valueDate,
         // delivery date
         Date maturityDate,
         OvernightIndex overnightIndex,
         Handle<Quote> convexityAdjustment = null,
         RateAveragingType averagingMethod = RateAveragingType.Compound)
      :base(price)
      {
         OvernightIndex index = overnightIndex.clone(termStructureHandle_);
         future_ = new OvernightIndexFuture(index, valueDate, maturityDate, convexityAdjustment ?? new Handle<Quote>(), averagingMethod);
         earliestDate_ = valueDate;
         latestDate_ = maturityDate;
      }

      // RateHelper interface
      public override double impliedQuote()
      {
         future_.recalculate();
         return future_.NPV();
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer -
         // force recalculation when needed
         bool observer = false;
         termStructureHandle_.linkTo(t, observer);
         base.setTermStructure(t);
      }

      public double convexityAdjustment()
      {
         return future_.convexityAdjustment();
      }

   }

   // RateHelper for bootstrapping over CME SOFR futures
   /* It compounds overnight SOFR rates from the third Wednesday
      of the reference month/year (inclusive) to the third Wednesday
      of the month one Month/Quarter later (exclusive).

      It requires the index history to be populated when the
      reference period starts in the past.
   */
   public class SofrFutureRateHelper : OvernightIndexFutureRateHelper
   {
      public SofrFutureRateHelper(Handle<Quote> price, Month referenceMonth, int referenceYear, Frequency referenceFreq,
         Handle<Quote> convexityAdjustment = null)
         : base(price,
            getValidSofrStart((int)referenceMonth, referenceYear, referenceFreq),
            getValidSofrEnd((int)referenceMonth, referenceYear, referenceFreq), new Sofr(),
            convexityAdjustment ?? new Handle<Quote>(),
            referenceFreq == Frequency.Quarterly ? RateAveragingType.Compound : RateAveragingType.Simple)
      {
         Utils.QL_REQUIRE(referenceFreq == Frequency.Quarterly || referenceFreq == Frequency.Monthly, () =>
            "only monthly and quarterly SOFR futures accepted");
         if (referenceFreq == Frequency.Quarterly)
         {
            Utils.QL_REQUIRE(referenceMonth == Month.Mar || referenceMonth == Month.Jun ||
                             referenceMonth == Month.Sep ||
                             referenceMonth == Month.Dec,
               () => "quarterly SOFR futures can only start in Mar,Jun,Sep,Dec");
         }
      }

      public SofrFutureRateHelper(double price, Month referenceMonth, int referenceYear, Frequency referenceFreq,
         double convexityAdjustment = 0.0)
         : base(new Handle<Quote>(new SimpleQuote(price)),
            getValidSofrStart((int)referenceMonth, referenceYear, referenceFreq),
            getValidSofrEnd((int)referenceMonth, referenceYear, referenceFreq), new Sofr(),
            new Handle<Quote>(new SimpleQuote(convexityAdjustment)),
            referenceFreq == Frequency.Quarterly ? RateAveragingType.Compound : RateAveragingType.Simple)
      {
         Utils.QL_REQUIRE(referenceFreq == Frequency.Quarterly || referenceFreq == Frequency.Monthly, () =>
            "only monthly and quarterly SOFR futures accepted");
         if (referenceFreq == Frequency.Quarterly)
         {
            Utils.QL_REQUIRE(referenceMonth == Month.Mar || referenceMonth == Month.Jun ||
                             referenceMonth == Month.Sep ||
                             referenceMonth == Month.Dec,
               () => "quarterly SOFR futures can only start in Mar,Jun,Sep,Dec");
         }
      }


      private static Date getValidSofrStart(int referenceMonth, int referenceYear, Frequency referenceFreq)
      {
         return referenceFreq == Frequency.Monthly
            ? new UnitedStates(UnitedStates.Market.GovernmentBond).adjust(new Date(1, referenceMonth, referenceYear))
            : Date.nthWeekday(3, DayOfWeek.Wednesday, referenceMonth, referenceYear);
      }

      private static Date getValidSofrEnd(int referenceMonth, int referenceYear, Frequency referenceFreq)
      {
         if (referenceFreq == Frequency.Monthly)
         {
            Calendar dc = new UnitedStates(UnitedStates.Market.GovernmentBond);
            Date d = dc.endOfMonth(new Date(1, referenceMonth, referenceYear));
            return dc.advance(d, new Period(1, TimeUnit.Days));
         }
         else
         {
            Date d = getValidSofrStart(referenceMonth, referenceYear, referenceFreq) + new Period(referenceFreq);
            return Date.nthWeekday(3, DayOfWeek.Wednesday, d.month(), d.year());
         }
      }
   }
}
