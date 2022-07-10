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
namespace QLNet
{
   /// <summary>
   /// Future on a compounded overnight index investment.
   ///
   /// Compatible with SOFR futures and Sonia futures available on
   /// CME and ICE exchanges.
   /// </summary>
   public class OvernightIndexFuture : Instrument
   {
      private OvernightIndex overnightIndex_;
      private Date valueDate_, maturityDate_;
      private Handle<Quote> convexityAdjustment_;
      private RateAveragingType averagingMethod_;

      public OvernightIndexFuture(OvernightIndex overnightIndex, Date valueDate, Date maturityDate,
         Handle<Quote> convexityAdjustment = null, RateAveragingType averagingMethod = RateAveragingType.Compound)
      {
         overnightIndex_ = overnightIndex;
         valueDate_ = valueDate;
         maturityDate_ = maturityDate;
         convexityAdjustment_ = convexityAdjustment ?? new Handle<Quote>();
         averagingMethod_ = averagingMethod;

         Utils.QL_REQUIRE(overnightIndex_ != null, () => "null overnight index");
         overnightIndex_?.registerWith(update);
      }

      public double convexityAdjustment()
      {
         return convexityAdjustment_.empty() ? 0.0 : convexityAdjustment_.link.value();
      }

      public override bool isExpired()
      {
         return new simple_event(maturityDate_).hasOccurred();
      }

      public OvernightIndex overnightIndex() { return overnightIndex_; }
      public Date valueDate() { return valueDate_; }
      public Date maturityDate() { return maturityDate_; }

      protected override void performCalculations()
      {
         double? R = convexityAdjustment() + rate();
         NPV_ = 100.0 * (1.0 - R);
      }

      private double? rate()
      {
         switch (averagingMethod_)
         {
            case RateAveragingType.Simple:
               return averagedRate();
            case RateAveragingType.Compound:
               return compoundedRate();
            default:
               Utils.QL_FAIL("unknown compounding convention (" + averagingMethod_ + ")");
               break;
         }

         return null;
      }

      private double? averagedRate()
      {
         Date today = Settings.evaluationDate();
         Calendar calendar = overnightIndex_.fixingCalendar();
         DayCounter dayCounter = overnightIndex_.dayCounter();
         Handle<YieldTermStructure> forwardCurve = overnightIndex_.forwardingTermStructure();
         double? avg = 0;
         Date d1 = valueDate_;
         TimeSeries<double?> history = IndexManager.instance().getHistory(overnightIndex_.name());
         double? fwd;
         while (d1 < maturityDate_)
         {
            Date d2 = calendar.advance(d1, 1, TimeUnit.Days);
            if (d1 < today)
            {
               fwd = history[d1];
               Utils.QL_REQUIRE(fwd != null, () =>"missing rate on " + d1 + " for index " + overnightIndex_.name());
            }
            else
            {
               fwd = forwardCurve.link.forwardRate(d1, d2, dayCounter, Compounding.Simple).rate();
            }
            avg += fwd * dayCounter.yearFraction(d1, d2);
            d1 = d2;
         }

         return avg / dayCounter.yearFraction(valueDate_, maturityDate_);
      }

      private double? compoundedRate()
      {
         Date today = Settings.evaluationDate();
         Calendar calendar = overnightIndex_.fixingCalendar();
         DayCounter dayCounter = overnightIndex_.dayCounter();
         Handle<YieldTermStructure> forwardCurve = overnightIndex_.forwardingTermStructure();
         double? prod = 1;
         if (today > valueDate_)
         {
            // can't value on a weekend inside reference period because we
            // won't know the reset rate until start of next business day.
            // user can supply an estimate if they really want to do this
            today = calendar.adjust(today);
            // for valuations inside the reference period, index quotes
            // must have been populated in the history
            TimeSeries<double?> history = IndexManager.instance().getHistory(overnightIndex_.name());
            Date d1 = valueDate_;
            while (d1 < today)
            {
               double? r = history[d1];
               Utils.QL_REQUIRE(r != null, ()=> "missing rate on " + d1 + " for index " + overnightIndex_.name());
               Date d2 = calendar.advance(d1, 1, TimeUnit.Days);
               prod *= 1 + r * dayCounter.yearFraction(d1, d2);
               d1 = d2;
            }
         }
         double forwardDiscount = forwardCurve.link.discount(maturityDate_);
         if (valueDate_ > today)
         {
            forwardDiscount /= forwardCurve.link.discount(valueDate_);
         }
         prod /= forwardDiscount;

         return (prod - 1) / dayCounter.yearFraction(valueDate_, maturityDate_);
      }
   }
}
