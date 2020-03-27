﻿/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   //! %callability leaving to the holder the possibility to convert
   public class SoftCallability : Callability
   {
      public SoftCallability(Bond.Price price, Date date, double trigger)
         : base(price, Callability.Type.Call, date)
      {
         trigger_ = trigger;
      }

      public double trigger()
      {
         return trigger_;
      }

      private double trigger_;
   }

   //! base class for convertible bonds
   public class ConvertibleBond : Bond
   {
      public class option : OneAssetOption
      {
         public new class Arguments : OneAssetOption.Arguments
         {
            public Arguments()
            {
               conversionRatio = null;
               settlementDays = null;
               redemption = null;
            }

            public double? conversionRatio { get; set; }
            public Handle<Quote> creditSpread { get; set; }
            public DividendSchedule dividends { get; set; }
            public List<Date> dividendDates { get; set; }
            public List<Date> callabilityDates { get; set; }
            public List<Callability.Type> callabilityTypes { get; set; }
            public List<double> callabilityPrices { get; set; }
            public List < double? > callabilityTriggers { get; set; }
            public List<Date> couponDates { get; set; }
            public List<double> couponAmounts { get; set; }
            public Date issueDate { get; set; }
            public Date settlementDate { get; set; }

            public int? settlementDays { get; set; }
            public double? redemption { get; set; }

            public override void validate()
            {
               base.validate();

               Utils.QL_REQUIRE(conversionRatio != null, () => "null conversion ratio");
               Utils.QL_REQUIRE(conversionRatio > 0.0,
                                () => "positive conversion ratio required: " + conversionRatio + " not allowed");

               Utils.QL_REQUIRE(redemption != null, () => "null redemption");
               Utils.QL_REQUIRE(redemption >= 0.0, () => "positive redemption required: " + redemption + " not allowed");

               Utils.QL_REQUIRE(settlementDate != null, () => "null settlement date");

               Utils.QL_REQUIRE(settlementDays != null, () => "null settlement days");

               Utils.QL_REQUIRE(callabilityDates.Count == callabilityTypes.Count,
                                () => "different number of callability dates and types");
               Utils.QL_REQUIRE(callabilityDates.Count == callabilityPrices.Count,
                                () => "different number of callability dates and prices");
               Utils.QL_REQUIRE(callabilityDates.Count == callabilityTriggers.Count,
                                () => "different number of callability dates and triggers");

               Utils.QL_REQUIRE(couponDates.Count == couponAmounts.Count,
                                () => "different number of coupon dates and amounts");
            }
         }
         public new class Engine : GenericEngine<ConvertibleBond.option.Arguments,
            ConvertibleBond.option.Results> {}

         public option(ConvertibleBond bond,
                       Exercise exercise,
                       double conversionRatio,
                       DividendSchedule dividends,
                       CallabilitySchedule callability,
                       Handle<Quote> creditSpread,
                       List<CashFlow> cashflows,
                       DayCounter dayCounter,
                       Schedule schedule,
                       Date issueDate,
                       int settlementDays,
                       double redemption)
            : base(new PlainVanillaPayoff(Option.Type.Call, (bond.notionals()[0]) / 100.0 * redemption / conversionRatio),
                   exercise)
         {
            bond_ = bond;
            conversionRatio_ = conversionRatio;
            callability_ = callability;
            dividends_ = dividends;
            creditSpread_ = creditSpread;
            cashflows_ = cashflows;
            dayCounter_ = dayCounter;
            issueDate_ = issueDate;
            schedule_ = schedule;
            settlementDays_ = settlementDays;
            redemption_ = redemption;
         }

         public override void setupArguments(IPricingEngineArguments args)
         {
            base.setupArguments(args);

            ConvertibleBond.option.Arguments moreArgs = args as Arguments;
            Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument type");

            moreArgs.conversionRatio = conversionRatio_;

            Date settlement = bond_.settlementDate();

            int n = callability_.Count;
            if (moreArgs.callabilityDates == null)
               moreArgs.callabilityDates = new List<Date>();
            else
               moreArgs.callabilityDates.Clear();

            if (moreArgs.callabilityTypes == null)
               moreArgs.callabilityTypes = new List<Callability.Type>();
            else
               moreArgs.callabilityTypes.Clear();

            if (moreArgs.callabilityPrices == null)
               moreArgs.callabilityPrices = new List<double>();
            else
               moreArgs.callabilityPrices.Clear();

            if (moreArgs.callabilityTriggers == null)
               moreArgs.callabilityTriggers = new List < double? >();
            else
               moreArgs.callabilityTriggers.Clear();

            for (int i = 0; i < n; i++)
            {
               if (!callability_[i].hasOccurred(settlement, false))
               {
                  moreArgs.callabilityTypes.Add(callability_[i].type());
                  moreArgs.callabilityDates.Add(callability_[i].date());

                  if (callability_[i].price().type() == Bond.Price.Type.Clean)
                     moreArgs.callabilityPrices.Add(callability_[i].price().amount() +
                                                    bond_.accruedAmount(callability_[i].date()));
                  else
                     moreArgs.callabilityPrices.Add(callability_[i].price().amount());

                  SoftCallability softCall = callability_[i] as SoftCallability;
                  if (softCall != null)
                     moreArgs.callabilityTriggers.Add(softCall.trigger());
                  else
                     moreArgs.callabilityTriggers.Add(null);
               }
            }

            List<CashFlow> cashflows = bond_.cashflows();

            if (moreArgs.couponDates == null)
               moreArgs.couponDates = new List<Date>();
            else
               moreArgs.couponDates.Clear();

            if (moreArgs.couponAmounts == null)
               moreArgs.couponAmounts = new List<double>();
            else
               moreArgs.couponAmounts.Clear();

            for (int i = 0; i < cashflows.Count - 1; i++)
            {
               if (!cashflows[i].hasOccurred(settlement, false))
               {
                  moreArgs.couponDates.Add(cashflows[i].date());
                  moreArgs.couponAmounts.Add(cashflows[i].amount());
               }
            }

            if (moreArgs.dividends == null)
               moreArgs.dividends = new DividendSchedule();
            else
               moreArgs.dividends.Clear();

            if (moreArgs.dividendDates == null)
               moreArgs.dividendDates = new List<Date>();
            else
               moreArgs.dividendDates.Clear();

            for (int i = 0; i < dividends_.Count; i++)
            {
               if (!dividends_[i].hasOccurred(settlement, false))
               {
                  moreArgs.dividends.Add(dividends_[i]);
                  moreArgs.dividendDates.Add(dividends_[i].date());
               }
            }

            moreArgs.creditSpread = creditSpread_;
            moreArgs.issueDate = issueDate_;
            moreArgs.settlementDate = settlement;
            moreArgs.settlementDays = settlementDays_;
            moreArgs.redemption = redemption_;
         }

         private ConvertibleBond bond_;
         private double conversionRatio_;
         private CallabilitySchedule callability_;
         private DividendSchedule dividends_;
         private Handle<Quote> creditSpread_;
         private List<CashFlow> cashflows_;
         private DayCounter dayCounter_;
         private Date issueDate_;
         private Schedule schedule_;
         private int settlementDays_;
         private double redemption_;
      }

      public double conversionRatio()
      {
         return conversionRatio_;
      }

      public DividendSchedule dividends()
      {
         return dividends_;
      }

      public CallabilitySchedule callability()
      {
         return callability_;
      }

      public Handle<Quote> creditSpread()
      {
         return creditSpread_;
      }

      protected ConvertibleBond(Exercise exercise,
                                double conversionRatio,
                                DividendSchedule dividends,
                                CallabilitySchedule callability,
                                Handle<Quote> creditSpread,
                                Date issueDate,
                                int settlementDays,
                                Schedule schedule,
                                double redemption)
         : base(settlementDays, schedule.calendar(), issueDate)
      {
         conversionRatio_ = conversionRatio;
         callability_ = callability;
         dividends_ = dividends;
         creditSpread_ = creditSpread;

         maturityDate_ = schedule.endDate();

         if (!callability.empty())
         {
            Utils.QL_REQUIRE(callability.Last().date() <= maturityDate_, () =>
                             "last callability date ("
                             + callability.Last().date()
                             + ") later than maturity ("
                             + maturityDate_.ToShortDateString() + ")");
         }

         creditSpread.registerWith(update);
      }

      protected override void performCalculations()
      {
         option_.setPricingEngine(engine_);
         NPV_ = settlementValue_ = option_.NPV();
         errorEstimate_ = null;
      }

      protected double conversionRatio_;
      protected CallabilitySchedule callability_;
      protected DividendSchedule dividends_;
      protected Handle<Quote> creditSpread_;
      protected option option_;
   }

   //! convertible zero-coupon bond
   /*! \warning Most methods inherited from Bond (such as yield or
               the yield-based dirtyPrice and cleanPrice) refer to
               the underlying plain-vanilla bond and do not take
               convertibility and callability into account.
   */

   public class ConvertibleZeroCouponBond : ConvertibleBond
   {
      public ConvertibleZeroCouponBond(Exercise exercise,
                                       double conversionRatio,
                                       DividendSchedule dividends,
                                       CallabilitySchedule callability,
                                       Handle<Quote> creditSpread,
                                       Date issueDate,
                                       int settlementDays,
                                       DayCounter dayCounter,
                                       Schedule schedule,
                                       double redemption = 100)
         : base(
              exercise, conversionRatio, dividends, callability, creditSpread, issueDate, settlementDays, schedule,
              redemption)
      {
         cashflows_ = new List<CashFlow>();

         // !!! notional forcibly set to 100
         setSingleRedemption(100.0, redemption, maturityDate_);

         option_ = new option(this, exercise, conversionRatio, dividends, callability, creditSpread, cashflows_,
                              dayCounter, schedule,
                              issueDate, settlementDays, redemption);
      }
   }

   //! convertible fixed-coupon bond
   /*! \warning Most methods inherited from Bond (such as yield or
                the yield-based dirtyPrice and cleanPrice) refer to
                the underlying plain-vanilla bond and do not take
                convertibility and callability into account.
   */

   public class ConvertibleFixedCouponBond : ConvertibleBond
   {
      public ConvertibleFixedCouponBond(Exercise exercise,
                                        double conversionRatio,
                                        DividendSchedule dividends,
                                        CallabilitySchedule callability,
                                        Handle<Quote> creditSpread,
                                        Date issueDate,
                                        int settlementDays,
                                        List<double> coupons,
                                        DayCounter dayCounter,
                                        Schedule schedule,
                                        double redemption = 100)
         : base(
              exercise, conversionRatio, dividends, callability, creditSpread, issueDate, settlementDays, schedule,
              redemption)
      {
         // !!! notional forcibly set to 100
         cashflows_ = new FixedRateLeg(schedule)
         .withCouponRates(coupons, dayCounter)
         .withNotionals(100.0)
         .withPaymentAdjustment(schedule.businessDayConvention());

         addRedemptionsToCashflows(new List<double>() {redemption});

         Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");

         option_ = new option(this, exercise, conversionRatio, dividends, callability, creditSpread, cashflows_,
                              dayCounter, schedule,
                              issueDate, settlementDays, redemption);
      }
   }

   //! convertible floating-rate bond
   /*! \warning Most methods inherited from Bond (such as yield or
                the yield-based dirtyPrice and cleanPrice) refer to
                the underlying plain-vanilla bond and do not take
                convertibility and callability into account.
   */

   public class ConvertibleFloatingRateBond : ConvertibleBond
   {
      public ConvertibleFloatingRateBond(Exercise exercise,
                                         double conversionRatio,
                                         DividendSchedule dividends,
                                         CallabilitySchedule callability,
                                         Handle<Quote> creditSpread,
                                         Date issueDate,
                                         int settlementDays,
                                         IborIndex index,
                                         int fixingDays,
                                         List<double> spreads,
                                         DayCounter dayCounter,
                                         Schedule schedule,
                                         double redemption = 100)
         : base(
              exercise, conversionRatio, dividends, callability, creditSpread, issueDate, settlementDays, schedule,
              redemption)

      {
         // !!! notional forcibly set to 100
         cashflows_ = new IborLeg(schedule, index)
         .withPaymentDayCounter(dayCounter)
         .withFixingDays(fixingDays)
         .withSpreads(spreads)
         .withNotionals(100.0)
         .withPaymentAdjustment(schedule.businessDayConvention());

         addRedemptionsToCashflows(new List<double> {redemption});

         Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");

         option_ = new option(this, exercise, conversionRatio, dividends, callability, creditSpread, cashflows_,
                              dayCounter, schedule,
                              issueDate, settlementDays, redemption);
      }
   }
}
