/*
 Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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
   /*! Base default-probability bootstrap helper
   @param tenor  CDS tenor.
   @param frequency  Coupon frequency.
   @param settlementDays  The number of days from today's date
                          to the start of the protection period.
                          Does not refer to initial cash settlements
                          (upfront and/or rebates) which are typically
                          on T+3
   @param paymentConvention The payment convention applied to
                            coupons schedules, settlement dates
                            and protection period calculations.
   */
   public class CdsHelper : RelativeDateBootstrapHelper<DefaultProbabilityTermStructure>
   {
      public CdsHelper(Handle<Quote> quote,
                       Period tenor,
                       int settlementDays,
                       Calendar calendar,
                       Frequency frequency,
                       BusinessDayConvention paymentConvention,
                       DateGeneration.Rule rule,
                       DayCounter dayCounter,
                       double recoveryRate,
                       Handle<YieldTermStructure> discountCurve,
                       bool settlesAccrual = true,
                       bool paysAtDefaultTime = true,
                       Date startDate = null,
                       DayCounter lastPeriodDayCounter = null,
                       bool rebatesAccrual = true,
                       CreditDefaultSwap.PricingModel model = CreditDefaultSwap.PricingModel.Midpoint)
         : base(quote)
      {
         tenor_ = tenor;
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         frequency_ = frequency;
         paymentConvention_ = paymentConvention;
         rule_ = rule;
         dayCounter_ = dayCounter;
         recoveryRate_ = recoveryRate;
         discountCurve_ = discountCurve;
         settlesAccrual_  = settlesAccrual;
         paysAtDefaultTime_ = paysAtDefaultTime;
         lastPeriodDC_  = lastPeriodDayCounter;
         rebatesAccrual_ = rebatesAccrual;
         model_ = model;
         startDate_ = startDate;
         schedule_ = new Schedule();

         initializeDates();
         discountCurve_.registerWith(update);
      }

      public CdsHelper(double quote,
                       Period tenor,
                       int settlementDays,
                       Calendar calendar,
                       Frequency frequency,
                       BusinessDayConvention paymentConvention,
                       DateGeneration.Rule rule,
                       DayCounter dayCounter,
                       double recoveryRate,
                       Handle<YieldTermStructure> discountCurve,
                       bool settlesAccrual = true,
                       bool paysAtDefaultTime = true,
                       Date startDate = null,
                       DayCounter lastPeriodDayCounter = null,
                       bool rebatesAccrual = true,
                       CreditDefaultSwap.PricingModel model = CreditDefaultSwap.PricingModel.Midpoint)
         : base(quote)
      {
         tenor_ = tenor;
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         frequency_ = frequency;
         paymentConvention_ = paymentConvention;
         rule_ = rule;
         dayCounter_ = dayCounter;
         recoveryRate_ = recoveryRate;
         discountCurve_ = discountCurve;
         settlesAccrual_  = settlesAccrual;
         paysAtDefaultTime_ = paysAtDefaultTime;
         lastPeriodDC_  = lastPeriodDayCounter;
         rebatesAccrual_ = rebatesAccrual;
         model_ = model;
         startDate_ = startDate;
         schedule_ = new Schedule();

         initializeDates();
         discountCurve_.registerWith(update);
      }

      public override void setTermStructure(DefaultProbabilityTermStructure ts)
      {
         base.setTermStructure(ts);
         probability_.linkTo(ts, false);
         resetEngine();
      }

      public CreditDefaultSwap swap()
      {
         return swap_;
      }

      public override void update()
      {
         base.update();
         resetEngine();
      }

      protected override void initializeDates()
      {
         protectionStart_ = evaluationDate_ + settlementDays_;
         Date startDate, endDate;
         if (startDate_ == null)
         {
            startDate = calendar_.adjust(protectionStart_,
                                         paymentConvention_);
            if (rule_ == DateGeneration.Rule.CDS || rule_ == DateGeneration.Rule.CDS2015)   // for standard CDS ..
            {
               // .. the start date is not adjusted
               startDate = protectionStart_;
            }
            // .. and (in any case) the end date rolls by 3 month as
            //  soon as the trade date falls on an IMM date,
            // or the March or September IMM date in case of the CDS2015 rule.
            endDate = protectionStart_ + tenor_;

         }
         else
         {
            if (!schedule_.empty())
               return; //no need to update schedule
            startDate = calendar_.adjust(startDate_, paymentConvention_);
            endDate = startDate_ + settlementDays_ + tenor_;
         }
         schedule_ =
            new MakeSchedule().from(startDate)
         .to(endDate)
         .withFrequency(frequency_)
         .withCalendar(calendar_)
         .withConvention(paymentConvention_)
         .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
         .withRule(rule_)
         .value();
         earliestDate_ = schedule_.dates().First();
         latestDate_   = calendar_.adjust(schedule_.dates().Last(),
                                          paymentConvention_);
         if (model_ == CreditDefaultSwap.PricingModel.ISDA)
            ++latestDate_;
      }

      protected virtual void resetEngine() { }

      protected Period tenor_;
      protected int settlementDays_;
      protected Calendar calendar_;
      protected Frequency frequency_;
      protected BusinessDayConvention paymentConvention_;
      protected DateGeneration.Rule rule_;
      protected DayCounter dayCounter_;
      protected double recoveryRate_;
      protected Handle<YieldTermStructure> discountCurve_;
      protected bool settlesAccrual_;
      protected bool paysAtDefaultTime_;
      protected DayCounter lastPeriodDC_;
      protected bool rebatesAccrual_;
      protected CreditDefaultSwap.PricingModel model_;

      protected Schedule schedule_;
      protected CreditDefaultSwap swap_;
      protected RelinkableHandle<DefaultProbabilityTermStructure> probability_ = new RelinkableHandle<DefaultProbabilityTermStructure>();
      //! protection effective date.
      protected Date protectionStart_;
      protected Date startDate_;
   }

   //! Spread-quoted CDS hazard rate bootstrap helper.
   public class SpreadCdsHelper : CdsHelper
   {
      public SpreadCdsHelper(Handle<Quote> runningSpread,
                             Period tenor,
                             int settlementDays,
                             Calendar calendar,
                             Frequency frequency,
                             BusinessDayConvention paymentConvention,
                             DateGeneration.Rule rule,
                             DayCounter dayCounter,
                             double recoveryRate,
                             Handle<YieldTermStructure> discountCurve,
                             bool settlesAccrual = true,
                             bool paysAtDefaultTime = true,
                             Date startDate = null,
                             DayCounter lastPeriodDayCounter = null,
                             bool rebatesAccrual = true,
                             CreditDefaultSwap.PricingModel model = CreditDefaultSwap.PricingModel.Midpoint)
         : base(runningSpread, tenor, settlementDays, calendar,
                frequency, paymentConvention, rule, dayCounter,
                recoveryRate, discountCurve, settlesAccrual, paysAtDefaultTime,
                startDate, lastPeriodDayCounter, rebatesAccrual, model)
      { }

      public SpreadCdsHelper(double runningSpread,
                             Period tenor,
                             int settlementDays, // ISDA: 1
                             Calendar calendar,
                             Frequency frequency, // ISDA: Quarterly
                             BusinessDayConvention paymentConvention,//ISDA:Following
                             DateGeneration.Rule rule, // ISDA: CDS
                             DayCounter dayCounter, // ISDA: Actual/360
                             double recoveryRate,
                             Handle<YieldTermStructure> discountCurve,
                             bool settlesAccrual = true,
                             bool paysAtDefaultTime = true,
                             Date startDate = null,
                             DayCounter lastPeriodDayCounter = null, // ISDA: Actual/360(inc)
                             bool rebatesAccrual = true, // ISDA: true
                             CreditDefaultSwap.PricingModel model = CreditDefaultSwap.PricingModel.Midpoint)
         : base(runningSpread, tenor, settlementDays, calendar,
                frequency, paymentConvention, rule, dayCounter,
                recoveryRate, discountCurve, settlesAccrual, paysAtDefaultTime,
                startDate, lastPeriodDayCounter, rebatesAccrual, model)
      { }

      public override double impliedQuote()
      {
         swap_.recalculate();
         return swap_.fairSpread();
      }

      protected override void resetEngine()
      {
         swap_ = new CreditDefaultSwap(CreditDefaultSwap.Protection.Side.Buyer, 100.0, 0.01, schedule_, paymentConvention_,
                                       dayCounter_, settlesAccrual_, paysAtDefaultTime_, protectionStart_,
                                       null, lastPeriodDC_, rebatesAccrual_);

         switch (model_)
         {
            case CreditDefaultSwap.PricingModel.ISDA:
               swap_.setPricingEngine(new IsdaCdsEngine(
                                         probability_, recoveryRate_, discountCurve_, false,
                                         IsdaCdsEngine.NumericalFix.Taylor, IsdaCdsEngine.AccrualBias.HalfDayBias,
                                         IsdaCdsEngine.ForwardsInCouponPeriod.Piecewise));
               break;
            case CreditDefaultSwap.PricingModel.Midpoint:
               swap_.setPricingEngine(new MidPointCdsEngine(
                                         probability_, recoveryRate_, discountCurve_));
               break;
            default:
               Utils.QL_FAIL("unknown CDS pricing model: " + model_);
               break;
         }
      }
   }

   //! Upfront-quoted CDS hazard rate bootstrap helper.
   public class UpfrontCdsHelper : CdsHelper
   {
      /*! \note the upfront must be quoted in fractional units. */
      public UpfrontCdsHelper(Handle<Quote> upfront,
                              double runningSpread,
                              Period tenor,
                              int settlementDays,
                              Calendar calendar,
                              Frequency frequency,
                              BusinessDayConvention paymentConvention,
                              DateGeneration.Rule rule,
                              DayCounter dayCounter,
                              double recoveryRate,
                              Handle<YieldTermStructure> discountCurve,
                              int upfrontSettlementDays = 0,
                              bool settlesAccrual = true,
                              bool paysAtDefaultTime = true,
                              Date startDate = null,
                              DayCounter lastPeriodDayCounter = null,
                              bool rebatesAccrual = true,
                              CreditDefaultSwap.PricingModel model =
                                 CreditDefaultSwap.PricingModel.Midpoint)
         : base(upfront, tenor, settlementDays, calendar,
                frequency, paymentConvention, rule, dayCounter,
                recoveryRate, discountCurve, settlesAccrual, paysAtDefaultTime,
                startDate, lastPeriodDayCounter, rebatesAccrual, model)
      {
         upfrontSettlementDays_ = upfrontSettlementDays;
         runningSpread_ = runningSpread;
         initializeDates();
      }

      /*! \note the upfront must be quoted in fractional units. */
      public UpfrontCdsHelper(double upfront,
                              double runningSpread,
                              Period tenor,
                              int settlementDays,
                              Calendar calendar,
                              Frequency frequency,
                              BusinessDayConvention paymentConvention,
                              DateGeneration.Rule rule,
                              DayCounter dayCounter,
                              double recoveryRate,
                              Handle<YieldTermStructure> discountCurve,
                              int upfrontSettlementDays = 0,
                              bool settlesAccrual = true,
                              bool paysAtDefaultTime = true,
                              Date startDate = null,
                              DayCounter lastPeriodDayCounter = null,
                              bool rebatesAccrual = true,
                              CreditDefaultSwap.PricingModel model =
                                 CreditDefaultSwap.PricingModel.Midpoint)
         : base(upfront, tenor, settlementDays, calendar,
                frequency, paymentConvention, rule, dayCounter,
                recoveryRate, discountCurve, settlesAccrual, paysAtDefaultTime,
                startDate, lastPeriodDayCounter, rebatesAccrual, model)
      {
         upfrontSettlementDays_ = upfrontSettlementDays;
         runningSpread_ = runningSpread;
         initializeDates();
      }

      public override double impliedQuote()
      {
         SavedSettings backup = new SavedSettings(); ;
         Settings.includeTodaysCashFlows = true;
         swap_.recalculate();
         return swap_.fairUpfront();
      }

      protected override void initializeDates()
      {
         base.initializeDates();
         upfrontDate_ = calendar_.advance(evaluationDate_,
                                          upfrontSettlementDays_, TimeUnit.Days,
                                          paymentConvention_);
      }

      protected override void resetEngine()
      {
         swap_ = new CreditDefaultSwap(
            CreditDefaultSwap.Protection.Side.Buyer, 100.0, 0.01, runningSpread_, schedule_,
            paymentConvention_, dayCounter_, settlesAccrual_,
            paysAtDefaultTime_, protectionStart_, upfrontDate_,
            null, lastPeriodDC_, rebatesAccrual_);
         switch (model_)
         {
            case CreditDefaultSwap.PricingModel.ISDA:
               swap_.setPricingEngine(new IsdaCdsEngine(
                                         probability_, recoveryRate_, discountCurve_, false,
                                         IsdaCdsEngine.NumericalFix.Taylor, IsdaCdsEngine.AccrualBias.HalfDayBias,
                                         IsdaCdsEngine.ForwardsInCouponPeriod.Piecewise));
               break;
            case CreditDefaultSwap.PricingModel.Midpoint:
               swap_.setPricingEngine(new MidPointCdsEngine(
                                         probability_, recoveryRate_, discountCurve_));
               break;
            default:
               Utils.QL_FAIL("unknown CDS pricing model: " + model_);
               break;
         }
      }

      protected int upfrontSettlementDays_;
      protected Date upfrontDate_;
      protected double runningSpread_;
   }
}
