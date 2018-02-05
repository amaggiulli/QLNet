/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   public struct Protection
   {
      public enum Side { Buyer, Seller }
   }

   public enum PricingModel
   {
      Midpoint,
      ISDA
   }

   /// <summary>
   /// Credit default swap
   /// <remarks>
   /// This instrument currently assumes that the issuer did
   /// not default until today's date.
   /// </remarks>
   /// <remarks>
   /// Settings.includeReferenceDateCashFlows()
   /// is set to true, payments occurring at the settlement date of the swap might be included in the
   /// NPV and therefore affect the fair-spread calculation. This might not be what you want.
   /// </remarks>
   /// <remarks>
   /// conventionalSpread (and impliedHazardRate) by default
   /// use the mid-point engine, which is not ISDA conform.
   /// </remarks>
   /// </summary>
   public class CreditDefaultSwap : Instrument
   {
      /// <summary>
      /// CDS quoted as running-spread only
      /// </summary>
      /// <param name="side">Whether the protection is bought or sold.</param>
      /// <param name="notional">Notional value</param>
      /// <param name="spread">Running spread in fractional units.</param>
      /// <param name="schedule">Coupon schedule.</param>
      /// <param name="convention">Business-day convention for payment-date adjustment.</param>
      /// <param name="dayCounter">Day-count convention for accrual.</param>
      /// <param name="settlesAccrual">Whether or not the accrued coupon is due in the event of a default.</param>
      /// <param name="paysAtDefaultTime">If set to true, any payments triggered by a default event are
      /// due at default time. If set to false, they are due at the end of the accrual period.</param>
      /// <param name="protectionStart">The first date where a default event will trigger the contract.</param>
      /// <param name="claim"></param>
      /// <param name="lastPeriodDayCounter">Day-count convention for accrual in last period</param>
      /// <param name="rebatesAccrual">The protection seller pays the accrued scheduled current coupon at the start
      /// of the contract. The rebate date is not provided but computed to be two days after protection start.</param>
      public CreditDefaultSwap(Protection.Side side,
                               double notional,
                               double spread,
                               Schedule schedule,
                               BusinessDayConvention convention,
                               DayCounter dayCounter,
                               bool settlesAccrual = true,
                               bool paysAtDefaultTime = true,
                               Date protectionStart = null,
                               Claim claim = null,
                               DayCounter lastPeriodDayCounter = null,
                               bool rebatesAccrual = true)
      {

         side_ = side;
         notional_ = notional;
         upfront_ = null;
         runningSpread_ = spread;
         settlesAccrual_ = settlesAccrual;
         paysAtDefaultTime_ = paysAtDefaultTime;
         claim_ = claim;
         protectionStart_ = protectionStart ?? schedule[0];

         Utils.QL_REQUIRE(protectionStart_ <= schedule[0] ||
                          schedule.rule() == DateGeneration.Rule.CDS ||
                          schedule.rule() == DateGeneration.Rule.CDS2015
                          , () => "protection can not start after accrual");

         leg_ = new FixedRateLeg(schedule)
         .withLastPeriodDayCounter(lastPeriodDayCounter)
         .withCouponRates(spread, dayCounter)
         .withNotionals(notional)
         .withPaymentAdjustment(convention);

         Date effectiveUpfrontDate = schedule.calendar().advance(protectionStart_, 2, TimeUnit.Days, convention);
         // '2' is used above since the protection start is assumed to be on trade_date + 1
         if (rebatesAccrual)
         {
            FixedRateCoupon firstCoupon = leg_[0] as FixedRateCoupon;

            Date rebateDate = effectiveUpfrontDate;
            accrualRebate_ = new SimpleCashFlow(firstCoupon.accruedAmount(protectionStart_), rebateDate);
         }

         upfrontPayment_ = new SimpleCashFlow(0.0, effectiveUpfrontDate);

         if (claim_ == null)
            claim_ = new FaceValueClaim();

         claim_.registerWith(update);
         maturity_ = schedule.dates().Last();
      }

      /// <summary>
      /// CDS quoted as upfront and running spread
      /// </summary>
      /// <param name="side">Whether the protection is bought or sold.</param>
      /// <param name="notional"> Notional value</param>
      /// <param name="upfront">Upfront in fractional units.</param>
      /// <param name="runningSpread">Running spread in fractional units.</param>
      /// <param name="schedule">Coupon schedule.</param>
      /// <param name="convention">Business-day convention for payment-date adjustment.</param>
      /// <param name="dayCounter">Day-count convention for accrual.</param>
      /// <param name="settlesAccrual">Whether or not the accrued coupon is due in the event of a default.</param>
      /// <param name="paysAtDefaultTime">If set to true, any payments triggered by a default event are
      /// due at default time. If set to false, they are due at the end of the accrual period.</param>
      /// <param name="protectionStart">The first date where a default event will trigger the contract.</param>
      /// <param name="upfrontDate">Settlement date for the upfront payment.</param>
      /// <param name="claim"></param>
      /// <param name="lastPeriodDayCounter">Day-count convention for accrual in last period</param>
      /// <param name="rebatesAccrual">The protection seller pays the accrued scheduled current coupon at the start
      /// of the contract. The rebate date is not provided but computed to be two days after protection start.</param>
      public CreditDefaultSwap(Protection.Side side,
                               double notional,
                               double upfront,
                               double runningSpread,
                               Schedule schedule,
                               BusinessDayConvention convention,
                               DayCounter dayCounter,
                               bool settlesAccrual = true,
                               bool paysAtDefaultTime = true,
                               Date protectionStart = null,
                               Date upfrontDate = null,
                               Claim claim = null,
                               DayCounter lastPeriodDayCounter = null,
                               bool rebatesAccrual = true)
      {

         side_ = side;
         notional_ = notional;
         upfront_ = upfront;
         runningSpread_ = runningSpread;
         settlesAccrual_ = settlesAccrual;
         paysAtDefaultTime_ = paysAtDefaultTime;
         claim_ = claim;
         protectionStart_ = protectionStart ?? schedule[0];

         Utils.QL_REQUIRE(protectionStart_ <= schedule[0] ||
                          schedule.rule() == DateGeneration.Rule.CDS
                          , () => "protection can not start after accrual");
         leg_ = new FixedRateLeg(schedule)
         .withLastPeriodDayCounter(lastPeriodDayCounter)
         .withCouponRates(runningSpread, dayCounter)
         .withNotionals(notional)
         .withPaymentAdjustment(convention);

         // If empty, adjust to T+3 standard settlement, alternatively add
         //  an arbitrary date to the constructor
         Date effectiveUpfrontDate = upfrontDate == null ?
                                     schedule.calendar().advance(protectionStart_, 2, TimeUnit.Days, convention) : upfrontDate;
         // '2' is used above since the protection start is assumed to be
         //   on trade_date + 1
         upfrontPayment_ = new SimpleCashFlow(notional * upfront, effectiveUpfrontDate);
         Utils.QL_REQUIRE(effectiveUpfrontDate >= protectionStart_, () => "upfront can not be due before contract start");

         if (rebatesAccrual)
         {
            FixedRateCoupon firstCoupon = leg_[0] as FixedRateCoupon;
            // adjust to T+3 standard settlement, alternatively add
            //  an arbitrary date to the constructor

            Date rebateDate = effectiveUpfrontDate;

            accrualRebate_ = new SimpleCashFlow(firstCoupon.accruedAmount(protectionStart_), rebateDate);
         }

         if (claim_ == null)
            claim_ = new FaceValueClaim();
         claim_.registerWith(update);

         maturity_ = schedule.dates().Last();
      }

      /// <summary>
      /// Instrument interface
      /// </summary>
      public override bool isExpired()
      {
         for (int i = leg_.Count; i > 0; --i)
            if (!leg_[i - 1].hasOccurred())
               return false;
         return true;
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         CreditDefaultSwap.Arguments arguments = args as CreditDefaultSwap.Arguments;
         Utils.QL_REQUIRE(arguments != null, () => "wrong argument type");

         arguments.side = side_;
         arguments.notional = notional_;
         arguments.leg = leg_;
         arguments.upfrontPayment = upfrontPayment_;
         arguments.accrualRebate = accrualRebate_;
         arguments.settlesAccrual = settlesAccrual_;
         arguments.paysAtDefaultTime = paysAtDefaultTime_;
         arguments.claim = claim_;
         arguments.upfront = upfront_;
         arguments.spread = runningSpread_;
         arguments.protectionStart = protectionStart_;
         arguments.maturity = maturity_;
      }

      public override void fetchResults(IPricingEngineResults r)
      {
         base.fetchResults(r);
         CreditDefaultSwap.Results results = r as CreditDefaultSwap.Results;
         Utils.QL_REQUIRE(results != null, () => "wrong result type");

         fairSpread_ = results.fairSpread;
         fairUpfront_ = results.fairUpfront;
         couponLegBPS_ = results.couponLegBPS;
         couponLegNPV_ = results.couponLegNPV;
         defaultLegNPV_ = results.defaultLegNPV;
         upfrontNPV_ = results.upfrontNPV;
         upfrontBPS_ = results.upfrontBPS;
         accrualRebateNPV_ = results.accrualRebateNPV;
      }

      // Inspectors
      public Protection.Side side() { return side_; }
      public double? notional() { return notional_; }
      public double runningSpread() { return runningSpread_; }
      public double? upfront() { return upfront_; }
      public bool settlesAccrual() { return settlesAccrual_; }
      public bool paysAtDefaultTime() { return paysAtDefaultTime_; }
      public List<CashFlow> coupons() { return leg_; }
      /// <summary>
      /// The first date for which defaults will trigger the contract
      /// </summary>
      /// <returns></returns>
      public Date protectionStartDate() { return protectionStart_; }
      /// <summary>
      /// The last date for which defaults will trigger the contract
      /// </summary>
      /// <returns></returns>
      public Date protectionEndDate() {return ((Coupon)(leg_.Last())).accrualEndDate();}
      public bool rebatesAccrual() {return accrualRebate_ != null;}
      // Results
      /// <summary>
      /// Returns the upfront spread that, given the running spread
      /// and the quoted recovery rate, will make the instrument
      /// have an NPV of 0.
      /// </summary>
      /// <returns></returns>
      public double fairUpfront()
      {
         calculate();
         Utils.QL_REQUIRE(fairUpfront_ != null, () => "fair upfront not available");
         return fairUpfront_.Value;
      }
      /// <summary>
      /// Returns the running spread that, given the quoted recovery
      /// rate, will make the running-only CDS have an NPV of 0.
      /// </summary>
      /// <remarks>
      /// This calculation does not take any upfront into
      /// account, even if one was given.
      /// </remarks>
      /// <returns></returns>
      public double fairSpread()
      {
         calculate();
         Utils.QL_REQUIRE(fairSpread_ != null, () => "fair spread not available");
         return fairSpread_.Value;
      }
      /*! Returns the variation of the fixed-leg value given a
          one-basis-point change in the running spread.
      */
      public double couponLegBPS()
      {
         calculate();
         Utils.QL_REQUIRE(couponLegBPS_ != null, () => "coupon-leg BPS not available");
         return couponLegBPS_.Value;
      }

      public double upfrontBPS()
      {
         calculate();
         Utils.QL_REQUIRE(upfrontBPS_ != null, () => "upfront BPS not available");
         return upfrontBPS_.Value;
      }

      public double couponLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(couponLegNPV_ != null, () => "coupon-leg NPV not available");
         return couponLegNPV_.Value;
      }

      public double defaultLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(defaultLegNPV_ != null, () => "default-leg NPV not available");
         return defaultLegNPV_.Value;
      }

      public double upfrontNPV()
      {
         calculate();
         Utils.QL_REQUIRE(upfrontNPV_ != null, () => "upfront NPV not available");
         return upfrontNPV_.Value;
      }

      public double accrualRebateNPV()
      {
         calculate();
         Utils.QL_REQUIRE(accrualRebateNPV_ != null, () => "accrual Rebate NPV not available");
         return accrualRebateNPV_.Value;
      }

      /// <summary>
      /// Implied hazard rate calculation
      /// </summary>
      /// <remarks>
      /// This method performs the calculation with the
      /// instrument characteristics. It will coincide with
      /// the ISDA calculation if your object has the standard
      ///  characteristics. Notably:
      /// - The calendar should have no bank holidays, just
      ///   weekends.
      /// - The yield curve should be LIBOR piecewise ant
      ///   in fwd rates, with a discount factor of 1 on the
      ///   calculation date, which coincides with the trade
      ///   date.
      /// - Convention should be Following for yield curve and
      ///   contract cashflows.
      /// - The CDS should pay accrued and mature on standard
      ///   IMM dates, settle on trade date +1 and upfront
      ///   settle on trade date +3.
      /// </remarks>
      /// <param name="targetNPV"></param>
      /// <param name="discountCurve"></param>
      /// <param name="dayCounter"></param>
      /// <param name="recoveryRate"></param>
      /// <param name="accuracy"></param>
      /// <returns></returns>
      public double impliedHazardRate(double targetNPV,
                                      Handle<YieldTermStructure> discountCurve,
                                      DayCounter dayCounter,
                                      double recoveryRate = 0.4,
                                      double accuracy = 1.0e-6,
                                      PricingModel model = PricingModel.Midpoint)
      {
         SimpleQuote flatRate = new SimpleQuote(0.0);

         Handle<DefaultProbabilityTermStructure> probability = new Handle<DefaultProbabilityTermStructure>(
            new FlatHazardRate(0, new WeekendsOnly(), new Handle<Quote>(flatRate), dayCounter));

         IPricingEngine engine = null;
         switch (model)
         {
            case PricingModel.Midpoint:
               engine = new MidPointCdsEngine(probability, recoveryRate, discountCurve);
               break;
            case PricingModel.ISDA:
               engine = new IsdaCdsEngine(probability, recoveryRate, discountCurve);
               break;
            default:
               Utils.QL_FAIL("unknown CDS pricing model: " + model);
               break;
         }


         setupArguments(engine.getArguments());
         CreditDefaultSwap.Results results = engine.getResults() as CreditDefaultSwap.Results;

         ObjectiveFunction f = new ObjectiveFunction(targetNPV, flatRate, engine, results);
         double guess = runningSpread_ / (1 - recoveryRate) * 365.0 / 360.0;
         double step = guess * 0.1;

         return new Brent().solve(f, accuracy, guess, step);
      }

      /// <summary>
      /// Conventional/standard upfront-to-spread conversion
      /// Under a standard ISDA model and a set of standardised
      /// instrument characteristics, it is the running only quoted
      /// spread that will make a CDS contract have an NPV of 0 when
      /// quoted for that running only spread.  Refer to: "ISDA
      /// Standard CDS converter specification." May 2009.
      /// <remarks>
      /// The conventional recovery rate to apply in the calculation
      /// is as specified by ISDA, not necessarily equal to the
      /// market-quoted one.  It is typically 0.4 for SeniorSec and
      /// 0.2 for subordinate.
      /// </remarks>
      /// <remarks>
      /// The conversion employs a flat hazard rate. As a result,
      /// you will not recover the market quotes.
      /// </remarks>
      /// <remarks>
      /// This method performs the calculation with the
      /// instrument characteristics. It will coincide with
      /// the ISDA calculation if your object has the standard
      ///  characteristics. Notably:
      ///  - The calendar should have no bank holidays, just
      ///    weekends.
      /// - The yield curve should be LIBOR piecewise ant
      ///   in fwd rates, with a discount factor of 1 on the
      ///   calculation date, which coincides with the trade date.
      /// - Convention should be Following for yield curve and contract cashflows.
      /// - The CDS should pay accrued and mature on standard
      ///   IMM dates, settle on trade date +1 and upfront settle on trade date +3.
      /// </remarks>
      /// </summary>
      /// <param name="conventionalRecovery"></param>
      /// <param name="discountCurve"></param>
      /// <param name="dayCounter"></param>
      /// <returns></returns>
      public double? conventionalSpread(double conventionalRecovery,
                                        Handle<YieldTermStructure> discountCurve,
                                        DayCounter dayCounter,
                                        PricingModel model = PricingModel.Midpoint)
      {
         SimpleQuote flatRate = new SimpleQuote(0.0);

         Handle<DefaultProbabilityTermStructure> probability = new Handle<DefaultProbabilityTermStructure>(
            new FlatHazardRate(0, new WeekendsOnly(), new Handle<Quote>(flatRate), dayCounter));

         IPricingEngine engine = null;
         switch (model)
         {
            case PricingModel.Midpoint:
               engine = new MidPointCdsEngine(probability, conventionalRecovery, discountCurve);
               break;
            case PricingModel.ISDA:
               engine = new IsdaCdsEngine(probability, conventionalRecovery, discountCurve);
               break;
            default:
               Utils.QL_FAIL("unknown CDS pricing model: " + model);
               break;
         }

         setupArguments(engine.getArguments());
         CreditDefaultSwap.Results results = engine.getResults() as CreditDefaultSwap.Results;

         ObjectiveFunction f = new ObjectiveFunction(0.0, flatRate, engine, results);
         double guess = runningSpread_ / (1 - conventionalRecovery) * 365.0 / 360.0;
         double step = guess * 0.1;

         new Brent().solve(f, 1e-9, guess, step);

         return results.fairSpread;
      }

      // Instrument interface
      protected override void setupExpired()
      {
         base.setupExpired();
         fairSpread_ = fairUpfront_ = 0.0;
         couponLegBPS_ = upfrontBPS_ = 0.0;
         couponLegNPV_ = defaultLegNPV_ = upfrontNPV_ = 0.0;
      }
      // data members
      protected Protection.Side side_;
      protected double? notional_;
      protected double? upfront_;
      protected double runningSpread_;
      protected bool settlesAccrual_, paysAtDefaultTime_;
      protected Claim claim_;
      protected List<CashFlow> leg_;
      protected CashFlow upfrontPayment_;
      protected CashFlow accrualRebate_;
      protected Date protectionStart_;
      protected Date maturity_;
      // results
      protected double? fairUpfront_;
      protected double? fairSpread_;
      protected double? couponLegBPS_, couponLegNPV_;
      protected double? upfrontBPS_, upfrontNPV_;
      protected double? defaultLegNPV_;
      protected double? accrualRebateNPV_;


      private class ObjectiveFunction : ISolver1d
      {
         public ObjectiveFunction(double target, SimpleQuote quote, IPricingEngine engine, CreditDefaultSwap.Results results)
         {
            target_ = target;
            quote_ = quote;
            engine_ = engine;
            results_ = results;
         }

         public override double value(double guess)
         {
            quote_.setValue(guess);
            engine_.calculate();
            return results_.value.GetValueOrDefault() - target_;
         }

         private double target_;
         private SimpleQuote quote_;
         private IPricingEngine engine_;
         private CreditDefaultSwap.Results results_;

      }

      public class Arguments : IPricingEngineArguments
      {
         public Arguments()
         {
            side = (Protection.Side)(-1);
            notional = null;
            spread = null;
         }

         public Protection.Side side { get; set; }
         public double? notional { get; set; }
         public double? upfront { get; set; }
         public double? spread { get; set; }
         public List<CashFlow> leg { get; set; }
         public CashFlow upfrontPayment { get; set; }
         public CashFlow accrualRebate { get; set; }
         public bool settlesAccrual { get; set; }
         public bool paysAtDefaultTime { get; set; }
         public Claim claim { get; set; }
         public Date protectionStart { get; set; }
         public Date maturity { get; set; }
         public void validate()
         {
            Utils.QL_REQUIRE(side != (Protection.Side)(-1), () => "side not set");
            Utils.QL_REQUIRE(notional != null, () => "notional not set");
            Utils.QL_REQUIRE(notional.IsNotEqual(0.0), () => "null notional set");
            Utils.QL_REQUIRE(spread != null, () => "spread not set");
            Utils.QL_REQUIRE(!leg.empty(), () => "coupons not set");
            Utils.QL_REQUIRE(upfrontPayment != null, () => "upfront payment not set");
            Utils.QL_REQUIRE(claim != null, () => "claim not set");
            Utils.QL_REQUIRE(protectionStart != null, () => "protection start date not set");
            Utils.QL_REQUIRE(maturity != null, () => "maturity date not set");
         }
      }


      public new class Results : Instrument.Results
      {
         public double? fairSpread { get; set; }
         public double? fairUpfront { get; set; }
         public double? couponLegBPS { get; set; }
         public double? couponLegNPV { get; set; }
         public double? defaultLegNPV { get; set; }
         public double? upfrontBPS { get; set; }
         public double? upfrontNPV { get; set; }
         public double? accrualRebateNPV { get; set; }
         public override void reset()
         {
            base.reset();
            fairSpread = null;
            fairUpfront = null;
            couponLegBPS = null;
            couponLegNPV = null;
            defaultLegNPV = null;
            upfrontBPS = null;
            upfrontNPV = null;
            accrualRebateNPV = null;
         }
      }

      public abstract class Engine : GenericEngine<CreditDefaultSwap.Arguments, CreditDefaultSwap.Results>
      {}

   }
}
