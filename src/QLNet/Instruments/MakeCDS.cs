//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
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
   /// This class provides a more comfortable way to instantiate standard cds.
   /// </summary>
   public class MakeCreditDefaultSwap
   {
      public MakeCreditDefaultSwap(Period tenor, double couponRate)
      {
         side_ = Protection.Side.Buyer;
         nominal_ = 1.0;
         tenor_ = tenor;
         couponTenor_ = new Period(3, TimeUnit.Months);
         couponRate_ = couponRate;
         upfrontRate_ = 0.0;
         dayCounter_ = new Actual360();
         lastPeriodDayCounter_ = new Actual360(true);
      }
      public MakeCreditDefaultSwap(Date termDate, double couponRate)
      {
         side_ = Protection.Side.Buyer;
         nominal_ = 1.0;
         termDate_ = termDate;
         couponTenor_ = new Period(3, TimeUnit.Months);
         couponRate_ = couponRate;
         upfrontRate_ = 0.0;
         dayCounter_ = new Actual360();
         lastPeriodDayCounter_ = new Actual360(true);
      }

      public CreditDefaultSwap value()
      {
         Date evaluation = Settings.evaluationDate();
         Date start = evaluation + 1;
         Date upfrontDate = new WeekendsOnly().advance(evaluation, new Period(3, TimeUnit.Days));
         Date end;
         if (tenor_ != null)
         {
            end = start + tenor_;
         }
         else
         {
            end = termDate_;
         }

         Schedule schedule = new Schedule(start, end, couponTenor_, new WeekendsOnly(),
                                          BusinessDayConvention.Following, BusinessDayConvention.Unadjusted, DateGeneration.Rule.CDS,
                                          false, null, null);

         CreditDefaultSwap cds = new CreditDefaultSwap(side_, nominal_, upfrontRate_, couponRate_, schedule,
                                                       BusinessDayConvention.Following, dayCounter_, true, true, start, upfrontDate, null, lastPeriodDayCounter_);

         cds.setPricingEngine(engine_);
         return cds;
      }

      public MakeCreditDefaultSwap withUpfrontRate(double upfrontRate)
      {
         upfrontRate_ = upfrontRate;
         return this;
      }
      public MakeCreditDefaultSwap withSide(Protection.Side side)
      {
         side_ = side;
         return this;
      }
      public MakeCreditDefaultSwap withNominal(double nominal)
      {
         nominal_ = nominal;
         return this;
      }
      public MakeCreditDefaultSwap withCouponTenor(Period couponTenor)
      {
         couponTenor_ = couponTenor;
         return this;
      }
      public MakeCreditDefaultSwap withDayCounter(DayCounter dayCounter)
      {
         dayCounter_ = dayCounter;
         return this;
      }
      public MakeCreditDefaultSwap withLastPeriodDayCounter(DayCounter lastPeriodDayCounter)
      {
         lastPeriodDayCounter_ = lastPeriodDayCounter;
         return this;
      }

      public MakeCreditDefaultSwap withPricingEngine(IPricingEngine engine)
      {
         engine_ = engine;
         return this;
      }

      private Protection.Side side_;
      double nominal_;
      Period tenor_;
      Date termDate_;
      Period couponTenor_;
      double couponRate_;
      double upfrontRate_;
      DayCounter dayCounter_;
      DayCounter lastPeriodDayCounter_;

      IPricingEngine engine_;
   }
}
