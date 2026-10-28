/*
 Copyright (C) 2008-2025  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System;
using System.Collections.Generic;
using System.Linq;
using Leg = System.Collections.Generic.List<QLNet.CashFlow>;

namespace QLNet
{
   /// <summary>
   /// Callable bond base class
   /// <remarks>
   /// Base callable bond class for fixed and zero coupon bonds.
   /// Defines commonalities between fixed and zero coupon callable
   /// bonds. At present, only European and Bermudan put/call schedules
   /// supported (no American optionality), as defined by the Callability
   /// class.
   /// </remarks>
   /// </summary>
   public class CallableBond : Bond
   {
      protected DayCounter paymentDayCounter_;
      protected Frequency frequency_;
      protected CallabilitySchedule putCallSchedule_;
      protected double faceAmount_;
      protected Schedule mainSchedule_;
      protected List<double> coupons_;
      protected List<double> notionalsByPeriod_;
      protected bool hasAmortizingSchedule_;
      protected BusinessDayConvention paymentConvention_;

      /// <summary>
      /// Ctor
      /// </summary>
      /// <param name="settlementDays"></param>
      /// <param name="maturityDate"></param>
      /// <param name="calendar"></param>
      /// <param name="paymentDayCounter"></param>
      /// <param name="faceAmount"></param>
      /// <param name="issueDate"></param>
      /// <param name="putCallSchedule"></param>
      protected CallableBond(int settlementDays, Date maturityDate, Calendar calendar, DayCounter paymentDayCounter,
         double faceAmount, Date issueDate = null, CallabilitySchedule putCallSchedule = null)
         : base(settlementDays, calendar, issueDate)
      {
         paymentDayCounter_ = paymentDayCounter;
         putCallSchedule_ = putCallSchedule ?? new CallabilitySchedule();
         maturityDate_ = maturityDate;
         faceAmount_ = faceAmount;

         if (putCallSchedule_.empty()) return;

         var finalOptionDate = Date.minDate();
         foreach (var t in putCallSchedule_)
         {
            finalOptionDate = Date.Max(finalOptionDate, t.date());
         }

         Utils.QL_REQUIRE(finalOptionDate <= maturityDate_, () => "Bond cannot mature before last call/put date");

         // derived classes must set cashflows_ and frequency_
      }

      /// <summary>
      /// accrued interest used internally
      /// <remarks>
      /// accrued interest used internally, where includeToday = false
      /// same as Bond::accruedAmount() but with enable early
      /// payments true.  Forces accrued to be calculated in a
      /// consistent way for future put/ call dates, which can be
      /// problematic in lattice engines when option dates are also
      /// coupon dates.
      /// </remarks>
      /// </summary>
      /// <param name="settlement"></param>
      /// <returns></returns>
      private double accrued(Date settlement)
      {
         if (settlement == null)
            settlement = settlementDate();

         bool IncludeToday = false;
         for (int i = 0; i < cashflows_.Count; ++i)
         {
            // the first coupon paying after d is the one we're after
            if (!cashflows_[i].hasOccurred(settlement, IncludeToday))
            {
               Coupon coupon = cashflows_[i] as Coupon;
               if (coupon != null)
                  // !!!
                  return coupon.accruedAmount(settlement) /
                     notional(settlement) * 100.0;
               else
                  return 0.0;
            }
         }

         return 0.0;
      }

      private double callAccrued(Date callDate)
      {
         foreach (var cashflow in cashflows_)
         {
            if (!cashflow.hasOccurred(callDate, false))
            {
               if (cashflow is Coupon coupon)
               {
                  var acc = coupon.accruedAmount(callDate);
                  if (coupon.tradingExCoupon(callDate))
                  {
                     acc = coupon.amount() + acc;
                  }

                  return acc / notional(callDate) * 100.0;
               }

               return 0.0;
            }
         }

         return 0.0;
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);
         CallableBond.Arguments arguments = args as CallableBond.Arguments;

         Utils.QL_REQUIRE(arguments != null, () => "no arguments given");

         Date settlement = arguments.settlementDate;

         arguments.faceAmount = faceAmount_;
         arguments.redemption = redemption().amount();
         arguments.redemptionDate = redemption().date();
         arguments.callabilityPrices = new List<double>(putCallSchedule_.Count);
         arguments.callabilityDates = new List<Date>(putCallSchedule_.Count);
         arguments.paymentDayCounter = paymentDayCounter_;
         arguments.frequency = frequency_;
         arguments.putCallSchedule = putCallSchedule_;

         if (!HasAmortizingSchedule())
         {
            List<CashFlow> cfs = cashflows();

            arguments.couponDates = new List<Date>(cfs.Count - 1);
            arguments.couponAmounts = new List<double>(cfs.Count - 1);

            for (int i = 0; i < cfs.Count; i++)
            {
               if (!cfs[i].hasOccurred(settlement, false))
               {
                  if (cfs[i] is QLNet.FixedRateCoupon)
                  {
                     arguments.couponDates.Add(cfs[i].date());
                     arguments.couponAmounts.Add(cfs[i].amount());
                  }
               }
            }

            for (int i = 0; i < putCallSchedule_.Count; i++)
            {
               if (!putCallSchedule_[i].hasOccurred(settlement, false))
               {
                  arguments.callabilityDates.Add(putCallSchedule_[i].date());
                  arguments.callabilityPrices.Add(putCallSchedule_[i].price().amount());

                  if (putCallSchedule_[i].price().type() == Bond.Price.Type.Clean)
                  {
                     arguments.callabilityPrices[arguments.callabilityPrices.Count - 1] +=
                        callAccrued(putCallSchedule_[i].date());
                  }
               }
            }

            return;
         }

         var deterministicCashflows = AggregateDeterministicCashflows(settlement);
         arguments.couponDates = deterministicCashflows.Select(item => item.Date).ToList();
         arguments.couponAmounts = deterministicCashflows.Select(item => item.Amount).ToList();

         for (int i = 0; i < putCallSchedule_.Count; i++)
         {
            if (!putCallSchedule_[i].hasOccurred(settlement, false))
            {
               arguments.callabilityDates.Add(putCallSchedule_[i].date());
               arguments.callabilityPrices.Add(CalculateCallabilityPrice(putCallSchedule_[i]));
            }
         }
      }

      /// <summary>
      /// Return the bond's put/call schedule
      /// </summary>
      /// <returns></returns>
      public CallabilitySchedule callability()
      {
         return putCallSchedule_;
      }

      /// <summary>
      /// returns the Black implied forward yield volatility
      /// the forward yield volatility, see Hull, Fourth Edition,
      /// Chapter 20, pg 536). Relevant only to European put/call
      /// schedules
      /// </summary>
      /// <param name="targetPrice"></param>
      /// <param name="discountCurve"></param>
      /// <param name="accuracy"></param>
      /// <param name="maxEvaluations"></param>
      /// <param name="minVol"></param>
      /// <param name="maxVol"></param>
      /// <returns></returns>
      public double impliedVolatility(Bond.Price targetPrice, Handle<YieldTermStructure> discountCurve,
         double accuracy, int maxEvaluations, double minVol, double maxVol)
      {
         Utils.QL_REQUIRE(!isExpired(), () => "instrument expired");

         double dirtyTargetPrice = default;
         switch (targetPrice.type())
         {
            case Price.Type.Dirty:
               dirtyTargetPrice = targetPrice.amount();
               break;
            case Price.Type.Clean:
               dirtyTargetPrice = targetPrice.amount() + accruedAmount();
               break;
            default:
               Utils.QL_FAIL("unknown price type");
               break;
         }

         var targetValue = dirtyTargetPrice * faceAmount_ / 100.0;
         var guess = 0.5 * (minVol + maxVol);
         var f = new ImpliedVolHelper(this, discountCurve, targetValue, false);
         var solver = new Brent();
         solver.setMaxEvaluations(maxEvaluations);
         return solver.solve(f, accuracy, guess, minVol, maxVol);
      }

      /// <summary>
      /// Returns the Black implied forward yield volatility
      /// <remarks>
      /// the forward yield volatility, see Hull, Fourth Edition,
      /// Chapter 20, pg 536). Relevant only to European put/call
      /// schedules
      /// </remarks>
      /// </summary>
      /// <param name="targetValue"></param>
      /// <param name="discountCurve"></param>
      /// <param name="accuracy"></param>
      /// <param name="maxEvaluations"></param>
      /// <param name="minVol"></param>
      /// <param name="maxVol"></param>
      /// <returns></returns>
      [Obsolete]
      public double impliedVolatility(double targetValue,
         Handle<YieldTermStructure> discountCurve,
         double accuracy,
         int maxEvaluations,
         double minVol,
         double maxVol)
      {
         Utils.QL_REQUIRE(!isExpired(), () => "instrument expired");
         double guess = 0.5 * (minVol + maxVol);
         ImpliedVolHelper f = new ImpliedVolHelper(this, discountCurve, targetValue, true);
         Brent solver = new Brent();
         solver.setMaxEvaluations(maxEvaluations);
         return solver.solve(f, accuracy, guess, minVol, maxVol);
      }

      /// <summary>
      /// Calculate the Option Adjusted Spread (OAS)
      /// <remarks>
      /// Calculates the spread that needs to be added to the the
      /// reference curve so that the theoretical model value
      /// matches the marketPrice.
      /// </remarks>
      /// </summary>
      /// <param name="cleanPrice"></param>
      /// <param name="engineTS"></param>
      /// <param name="dayCounter"></param>
      /// <param name="compounding"></param>
      /// <param name="frequency"></param>
      /// <param name="settlement"></param>
      /// <param name="accuracy"></param>
      /// <param name="maxIterations"></param>
      /// <param name="guess"></param>
      /// <returns></returns>
      public double OAS(double cleanPrice,
         Handle<YieldTermStructure> engineTS,
         DayCounter dayCounter,
         Compounding compounding,
         Frequency frequency,
         Date settlement = null,
         double accuracy = 1.0e-10,
         int maxIterations = 100,
         double guess = 0.0)
      {
         if (settlement == null)
            settlement = settlementDate();

         double dirtyPrice = cleanPrice + accruedAmount(settlement);

         var f = new NpvSpreadHelper(this);
         OasHelper obj = new OasHelper(f, dirtyPrice);

         Brent solver = new Brent();
         solver.setMaxEvaluations(maxIterations);

         double step = 0.001;
         double oas = solver.solve(obj, accuracy, guess, step);

         return continuousToConv(oas,
            this,
            engineTS,
            dayCounter,
            compounding,
            frequency);
      }

      /// <summary>
      /// Calculate the clean price based on the given
      /// option-adjust-spread (oas) over the given yield term
      /// structure (engineTS)
      /// </summary>
      /// <param name="oas"></param>
      /// <param name="engineTS"></param>
      /// <param name="dayCounter"></param>
      /// <param name="compounding"></param>
      /// <param name="frequency"></param>
      /// <param name="settlement"></param>
      /// <returns></returns>
      public double cleanPriceOAS(double oas,
         Handle<YieldTermStructure> engineTS,
         DayCounter dayCounter,
         Compounding compounding,
         Frequency frequency,
         Date settlement = null)
      {
         if (settlement == null)
            settlement = settlementDate();

         oas = convToContinuous(oas, this, engineTS, dayCounter, compounding, frequency);

         var f = new NpvSpreadHelper(this);

         double P = f.value(oas) - accruedAmount(settlement);

         return P;
      }

      /// <summary>
      /// Calculate the effective duration
      /// <remarks>
      /// Calculate the effective duration, i.e., the first
      /// differential of the dirty price w.r.t. a parallel shift of
      /// the yield term structure divided by current dirty price
      /// </remarks>
      /// </summary>
      /// <param name="oas"></param>
      /// <param name="engineTS"></param>
      /// <param name="dayCounter"></param>
      /// <param name="compounding"></param>
      /// <param name="frequency"></param>
      /// <param name="bump"></param>
      /// <returns></returns>
      public double effectiveDuration(double oas,
         Handle<YieldTermStructure> engineTS,
         DayCounter dayCounter,
         Compounding compounding,
         Frequency frequency,
         double bump = 2e-4)
      {
         double P = cleanPriceOAS(oas, engineTS, dayCounter, compounding, frequency);

         double Ppp = cleanPriceOAS(oas + bump, engineTS, dayCounter, compounding, frequency);

         double Pmm = cleanPriceOAS(oas - bump, engineTS, dayCounter, compounding, frequency);

         if (P.IsEqual(0.0))
            return 0;

         return (Pmm - Ppp) / (2 * P * bump);
      }

      /// <summary>
      /// Calculate the effective convexity
      /// <remarks>
      /// Calculate the effective convexity, i.e., the second
      /// differential of the dirty price w.r.t. a parallel shift of
      /// the yield term structure divided by current dirty price
      /// </remarks>
      /// </summary>
      /// <param name="oas"></param>
      /// <param name="engineTS"></param>
      /// <param name="dayCounter"></param>
      /// <param name="compounding"></param>
      /// <param name="frequency"></param>
      /// <param name="bump"></param>
      /// <returns></returns>
      public double effectiveConvexity(double oas,
         Handle<YieldTermStructure> engineTS,
         DayCounter dayCounter,
         Compounding compounding,
         Frequency frequency,
         double bump = 2e-4)
      {
         double P = cleanPriceOAS(oas, engineTS, dayCounter, compounding, frequency);

         double Ppp = cleanPriceOAS(oas + bump, engineTS, dayCounter, compounding, frequency);

         double Pmm = cleanPriceOAS(oas - bump, engineTS, dayCounter, compounding, frequency);

         if (P.IsEqual(0.0))
            return 0;

         return (Ppp + Pmm - 2 * P) / (Math.Pow(bump, 2) * P);
      }

      /// <summary>
      /// Calculate yield for each callability date
      /// must be implemented in derived classes
      /// </summary>
      /// <param name="settlement"></param>
      /// <param name="price"></param>
      /// <param name="frequency"></param>
      /// <param name="accuracy"></param>
      /// <returns></returns>
      /// 
      public virtual CallableCalcs[] yieldToCalls(Date settlement, double price, Frequency frequency, double accuracy)
      {

         throw new NotImplementedException("YieldsToCall not implemented for the given bond");
      }

      protected CallableCalcs[] yieldToCallsInternal(Date settlement, double price, CouponType couponType,
         Frequency frequency, double accuracy = 1.0e-10)
      {
         var cc = new List<CallableCalcs>();
         var bonds = GetCallableBonds(couponType,settlement);
         var calls = putCallSchedule_.ToList();

         for (var i = 0; i < bonds.Length; i++)
         {
            var bond = bonds[i];
            var call = calls[i];
            // Skip not tradable bonds
            if (bond.maturityDate() <= settlement) continue;
            var comp = GetSecurityCompounding(bond, couponType, settlement,frequency);
            try
            {
               var yield = bond.yield(price, paymentDayCounter_, comp, frequency, settlement, accuracy);
               if (yield == 0.0) continue;
               var modDuration = BondFunctions.duration(bond, yield, paymentDayCounter_,
                  comp, frequency, Duration.Type.Modified, settlement);

               cc.Add(new CallableCalcs()
               {
                  CallDate = bond.maturityDate(),
                  CallPrice = call.price().amount(),
                  CalcYield = yield,
                  CalcModifiedDuration = modDuration
               });
            }
            catch (Exception ex)
            {
               cc.Add(new CallableCalcs()
               {
                  CallDate = bond.maturityDate(),
                  CallPrice = call.price().amount(),
                  ErrorMessage = ex.Message,
               });
            }
         }

         return cc.ToArray();
      }

      /// <summary>
      /// Calculate clean price for each callability date
      /// must be implemented in derived classes
      /// </summary>
      /// <param name="settlement"></param>
      /// <param name="price"></param>
      /// <param name="frequency"></param>
      /// <returns></returns>
      public virtual CallableCalcs[] priceToCalls(Date settlement, double price, Frequency frequency)
      {
         throw new NotImplementedException("PriceToCall not implemented for the given bond");
      }

      protected CallableCalcs[] priceToCallsInternal(Date settlement, double price, CouponType couponType,
         Frequency frequency)
      {
         var cc = new List<CallableCalcs>();
         var bonds = GetCallableBonds(couponType,settlement);
         var calls = putCallSchedule_.ToList();

         for (var i = 0; i < bonds.Length; i++)
         {
            var bond = bonds[i];
            var call = calls[i];
            // Skip not tradable bonds
            if (bond.maturityDate() <= settlement) continue;
            var comp = GetSecurityCompounding(bond, couponType, settlement, frequency);
            try
            {
               var priceToCall = bond.cleanPrice(price, paymentDayCounter_, comp,
                  frequency, settlement);

               cc.Add(new CallableCalcs()
               {
                  CallDate = bond.maturityDate(), CallPrice = call.price().amount(),
                  CalcPrice = priceToCall
               });
            }
            catch (Exception ex)
            {
               cc.Add(new CallableCalcs()
               {
                  CallDate = bond.maturityDate(), CallPrice = call.price().amount(),
                  ErrorMessage = ex.Message
               });
            }
         }

         return cc.ToArray();
      }

      public virtual double yieldAt(Date settlementDate, double price, Frequency frequency, double accuracy,
         Date maturityDate = null, double? redemption = null)
      {
         throw new NotImplementedException("yieldToDate not implemented for the given bond");
      }

      protected double yieldAtInternal(CouponType couponType, Date settlementDate, double price, Frequency frequency,
         double accuracy, Date maturityDate = null, double? redemption = null)
      {
         Bond bond = this;
         if (maturityDate != null && redemption is > 0)
         {
            bond = CreateFixedRateBond(maturityDate, redemption.Value, couponType);
         }

         var comp = GetSecurityCompounding(bond, couponType, settlementDate, frequency);
         return bond.yield(price, paymentDayCounter_, comp, frequency, settlementDate, accuracy);
      }


      public virtual double priceAt(Date settlementDate, Date maturityDate, double? redemption, double yield,
         Frequency frequency)
      {

         throw new NotImplementedException("yieldToDate not implemented for the given bond");
      }

      protected double priceAtInternal(CouponType couponType, Date settlementDate, Date maturityDate,
         double? redemption, double yield,
         Frequency frequency)
      {
         Bond bond = this;
         if (maturityDate != null && redemption is > 0)
         {
            bond = CreateFixedRateBond(maturityDate, redemption.Value, couponType);
         }

         var comp = GetSecurityCompounding(bond, couponType, settlementDate, frequency);
         return bond.cleanPrice(yield, paymentDayCounter_, comp, frequency, settlementDate);
      }

      public virtual double durationAt(Date settlementDate, Date maturityDate, double? redemption, double yield,
         Frequency frequency, Duration.Type durationType)
      {

         throw new NotImplementedException("yieldToDate not implemented for the given bond");
      }

      protected double durationAtInternal(CouponType couponType, Date settlementDate, Date maturityDate,
         double? redemption, double yield, Frequency frequency, Duration.Type durationType)
      {
         Bond bond = this;
         if (maturityDate != null && redemption is > 0)
         {
            bond = CreateFixedRateBond(maturityDate, redemption.Value, couponType);
         }

         var comp = GetSecurityCompounding(bond, couponType, settlementDate, frequency);

         return BondFunctions.duration(bond, yield, paymentDayCounter_, comp, frequency, durationType, settlementDate);
      }

      /// <summary>
      /// helper class for Black implied volatility calculation
      /// </summary>
      protected class ImpliedVolHelper : ISolver1d
      {
         public ImpliedVolHelper(CallableBond bond, Handle<YieldTermStructure> discountCurve, double targetValue,
            bool matchNPV)
         {
            targetValue_ = targetValue;
            matchNPV_ = matchNPV;

            vol_ = new SimpleQuote(0.0);
            engine_ = new BlackCallableFixedRateBondEngine(new Handle<Quote>(vol_), discountCurve);

            bond.setupArguments(engine_.getArguments());
            results_ = engine_.getResults() as CallableBond.Results;
         }

         public override double value(double x)
         {
            vol_.setValue(x);
            engine_.calculate(); // get the Black NPV based on vol x
            var value = matchNPV_ ? results_.value : results_.settlementValue;
            return value.GetValueOrDefault() - targetValue_;
         }

         private IPricingEngine engine_;
         private double targetValue_;
         private bool matchNPV_;
         private SimpleQuote vol_;
         private CallableBond.Results results_;
      }

      /// <summary>
      /// Helper class for option adjusted spread calculations
      /// </summary>
      protected class NpvSpreadHelper
      {
         public NpvSpreadHelper(CallableBond bond)
         {
            bond_ = bond;
            bond.setupArguments(bond.engine_.getArguments());
            results_ = bond.engine_.getResults() as CallableBond.Results;
         }

         public double value(double x)
         {
            var args = bond_.engine_.getArguments() as CallableBond.Arguments;
            // Pops the original value when function finishes
            double originalSpread = args.spread;
            args.spread = x;
            bond_.engine_.calculate();
            args.spread = originalSpread;

            var currentNotional = bond_.notional(args.settlementDate);
            if (currentNotional.IsEqual(0.0))
            {
               return 0.0;
            }

            return results_.settlementValue.Value * 100.0 / currentNotional;
         }

         private CallableBond bond_;
         private CallableBond.Results results_;
      }

      protected class OasHelper : ISolver1d
      {
         public OasHelper(NpvSpreadHelper npvhelper, double targetValue)
         {
            npvhelper_ = npvhelper;
            targetValue_ = targetValue;

         }

         public override double value(double v)
         {
            return targetValue_ - npvhelper_.value(v);
         }

         private NpvSpreadHelper npvhelper_;
         private double targetValue_;
      }

      public new class Arguments : Bond.Arguments
      {
         public List<Date> couponDates { get; set; }
         public List<double> couponAmounts { get; set; }

         public double faceAmount { get; set; }

         // redemption = face amount * redemption / 100.
         public double redemption { get; set; }
         public Date redemptionDate { get; set; }
         public DayCounter paymentDayCounter { get; set; }
         public Frequency frequency { get; set; }

         public CallabilitySchedule putCallSchedule { get; set; }

         /// <summary>
         /// Full, dirty, or cash bond prices associated with the callability schedule.
         /// </summary>
         public List<double> callabilityPrices { get; set; }
         public List<Date> callabilityDates { get; set; }

         /// <summary>
         /// Spread to apply to the valuation.
         /// <remarks>
         /// This is a continuously
         /// componded rate added to the model. Currently only applied
         /// by the TreeCallableFixedRateBondEngine
         /// </remarks>
         /// </summary>
         public double spread { get; set; }

         public override void validate()
         {
            Utils.QL_REQUIRE(settlementDate != null, () => "null settlement date");
            Utils.QL_REQUIRE(redemption >= 0.0, () => "positive redemption required: " + redemption + " not allowed");
            Utils.QL_REQUIRE(callabilityDates.Count == callabilityPrices.Count,
               () => "different number of callability dates and prices");
            Utils.QL_REQUIRE(couponDates.Count == couponAmounts.Count,
               () => "different number of coupon dates and amounts");
         }
      }

      /// <summary>
      /// results for a callable bond calculation
      /// </summary>
      public new class Results : Bond.Results
      {
         // no extra results set yet
      }

      /// <summary>
      /// base class for callable fixed rate bond engine
      /// </summary>
      public new class Engine : GenericEngine<CallableBond.Arguments, CallableBond.Results>
      {
      }

      /// <summary>
      /// Convert a continuous spread to a conventional spread to a
      /// reference yield curve
      /// </summary>
      /// <param name="oas"></param>
      /// <param name="b"></param>
      /// <param name="yts"></param>
      /// <param name="dayCounter"></param>
      /// <param name="compounding"></param>
      /// <param name="frequency"></param>
      /// <returns></returns>
      private double continuousToConv(double oas,
         Bond b,
         Handle<YieldTermStructure> yts,
         DayCounter dayCounter,
         Compounding compounding,
         Frequency frequency)
      {
         double zz = yts.link.zeroRate(b.maturityDate(), dayCounter, Compounding.Continuous, Frequency.NoFrequency)
            .value();

         InterestRate baseRate = new InterestRate(zz, dayCounter, Compounding.Continuous, Frequency.NoFrequency);

         InterestRate spreadedRate =
            new InterestRate(oas + zz, dayCounter, Compounding.Continuous, Frequency.NoFrequency);

         double br = baseRate
            .equivalentRate(dayCounter, compounding, frequency, yts.link.referenceDate(), b.maturityDate()).rate();

         double sr = spreadedRate
            .equivalentRate(dayCounter, compounding, frequency, yts.link.referenceDate(), b.maturityDate()).rate();

         // Return the spread
         return sr - br;
      }

      /// <summary>
      /// Convert a conventional spread to a reference yield curve to a
      /// continuous spread
      /// </summary>
      /// <param name="oas"></param>
      /// <param name="b"></param>
      /// <param name="yts"></param>
      /// <param name="dayCounter"></param>
      /// <param name="compounding"></param>
      /// <param name="frequency"></param>
      /// <returns></returns>
      private double convToContinuous(double oas,
         Bond b,
         Handle<YieldTermStructure> yts,
         DayCounter dayCounter,
         Compounding compounding,
         Frequency frequency)
      {
         double zz = yts.link.zeroRate(b.maturityDate(), dayCounter, compounding, frequency).value();

         InterestRate baseRate = new InterestRate(zz, dayCounter, compounding, frequency);

         InterestRate spreadedRate = new InterestRate(oas + zz, dayCounter, compounding, frequency);

         double br = baseRate.equivalentRate(dayCounter, Compounding.Continuous, Frequency.NoFrequency,
            yts.link.referenceDate(), b.maturityDate()).rate();

         double sr = spreadedRate.equivalentRate(dayCounter, Compounding.Continuous, Frequency.NoFrequency,
            yts.link.referenceDate(), b.maturityDate()).rate();

         // Return the spread
         return sr - br;
      }

      protected Bond[] GetCallableBonds(CouponType couponType, Date settlementDate)
      {
         var bonds = new List<Bond>();
         var calls = putCallSchedule_.ToList();
         for (var i = 0; i < putCallSchedule_.Count; i++)
         {
            var call = putCallSchedule_[i];
            if (call.date() <= settlementDate) continue;
            var bond = CreateFixedRateBond(call.date(), call.price().amount(), couponType);
            bonds.Add(bond);

         }

         return bonds.ToArray();
      }

      protected Bond CreateFixedRateBond(Date maturityDate, double redemption, CouponType couponType)
      {
         if (couponType == CouponType.FixedRate)
         {
            var fixedRateBondSchedule = mainSchedule_.until(maturityDate);
            var fixedRateBondCoupons = coupons_.Take(fixedRateBondSchedule.size() - 1).ToList();
            Bond bond;

            if (HasAmortizingSchedule())
            {
               var fixedRateBondNotionals = notionalsByPeriod_.Take(fixedRateBondSchedule.size() - 1).ToList();
               var redemptionMultipliers = CreateRedemptionMultipliers(fixedRateBondSchedule, fixedRateBondNotionals, redemption);
               bond = new CallableSupportFixedRateBond(settlementDays_, fixedRateBondSchedule, fixedRateBondCoupons,
                  fixedRateBondNotionals, paymentDayCounter_, paymentConvention_, issueDate_, redemptionMultipliers);
            }
            else
            {
               bond = new FixedRateBond(settlementDays_, faceAmount_, fixedRateBondSchedule, fixedRateBondCoupons,
                  paymentDayCounter_, BusinessDayConvention.Unadjusted, redemption, issueDate_);
            }

            return bond;
         }
         else
         {
            var bond = new ZeroCouponBond(settlementDays_, calendar_, faceAmount_, maturityDate,
               BusinessDayConvention.Unadjusted, redemption, issueDate_);
            return bond;
         }
      }

      protected Compounding GetSecurityCompounding(Bond bond, CouponType couponType, DateTime settlementDate,
         Frequency frequency)
      {
         if (bond.nextCashFlowDate(settlementDate) == bond.maturityDate())
         {
            if (couponType is not CouponType.ZeroCoupon)
               return Compounding.Simple;

            if ((Date)settlementDate + new Period(frequency) >= bond.maturityDate())
               return Compounding.Simple;
         }

         return Compounding.Compounded;
      }

      /// <summary>
      /// Determines the appropriate compounding method based on cashflows and maturity date.
      /// Uses Simple compounding for final period, Compounded otherwise.
      /// </summary>
      protected Compounding GetCompounding(Leg cashflows, Date settlementDate, Date maturityDate, CouponType couponType)
      {
         var nextCashFlowDate = CashFlows.nextCashFlowDate(cashflows, false, settlementDate);

         if (nextCashFlowDate == maturityDate)
         {
            if (couponType != CouponType.ZeroCoupon)
               return Compounding.Simple;

            if ((Date)settlementDate + new Period(Frequency.Semiannual) >= maturityDate)
               return Compounding.Simple;
         }

         return Compounding.Compounded;
      }

      /// <summary>
      /// Builds cashflows for a specific maturity date and redemption amount without creating Bond objects.
      /// Returns the cashflows, compounding method, and effective maturity date.
      /// </summary>
      protected (Leg cashflows, Compounding comp, Date maturityDate) BuildCashflowsForMaturity(
         CouponType couponType, Date settlementDate, Date targetMaturityDate, double? redemption)
      {
         // Build cashflows directly without creating Bond object
         var effectiveMaturityDate = targetMaturityDate ?? maturityDate_;

         Leg cashflows;

         if (couponType == CouponType.FixedRate)
         {
            // For fixed rate bonds, truncate schedule to maturity date
            var truncatedSchedule = mainSchedule_.until(effectiveMaturityDate);

            // Use FixedRateLeg to create coupon cashflows (including stubs)
            var truncatedCoupons = coupons_.Take(truncatedSchedule.size() - 1).ToList();
            cashflows = new FixedRateLeg(truncatedSchedule)
               .withCouponRates(truncatedCoupons, paymentDayCounter_)
               .withPaymentCalendar(calendar_)
               .withNotionals(HasAmortizingSchedule() ? notionalsByPeriod_.Take(truncatedSchedule.size() - 1).ToList() : [faceAmount_])
               .withPaymentAdjustment(BusinessDayConvention.Unadjusted);

            effectiveMaturityDate = truncatedSchedule.endDate();
         }
         else
         {
            // Zero coupon: just the redemption (no schedule needed)
            cashflows = [];
         }

         if (HasAmortizingSchedule())
         {
            var redemptionMultipliers = CreateRedemptionMultipliers(
               mainSchedule_.until(effectiveMaturityDate),
               notionalsByPeriod_.Take(mainSchedule_.until(effectiveMaturityDate).size() - 1).ToList(),
               redemption.GetValueOrDefault(100.0));
            AddPrincipalCashflows(cashflows, redemptionMultipliers);
         }
         else
         {
            var redemptionAmount = redemption.GetValueOrDefault(100.0) * faceAmount_ / 100.0;
            cashflows.Add(new SimpleCashFlow(redemptionAmount, effectiveMaturityDate));
         }

         // Determine compounding based on maturity
         var comp = GetCompounding(cashflows, settlementDate, effectiveMaturityDate, couponType);

         return (cashflows, comp, effectiveMaturityDate);
      }

      public enum CouponType
      {
         FixedRate,
         ZeroCoupon
      }

      public class CallableCalcs
      {
         public Date CallDate { get; set; }
         public double CallPrice { get; set; }
         public double? CalcYield { get; set; }
         public double? CalcPrice { get; set; }
         public double? CalcModifiedDuration { get; set; }
         public string ErrorMessage { get; set; }
      }

      protected bool HasAmortizingSchedule()
      {
         return hasAmortizingSchedule_;
      }

      protected List<(Date Date, double Amount)> AggregateDeterministicCashflows(Date settlement)
      {
         return cashflows_
            .Where(cashflow => !cashflow.hasOccurred(settlement, false) && cashflow != redemption())
            .GroupBy(cashflow => cashflow.date())
            .Select(group => (group.Key, group.Sum(cashflow => cashflow.amount())))
            .OrderBy(item => item.Key)
            .ToList();
      }

      protected double CalculateCallabilityPrice(Callability call)
      {
         var callPrice = call.price().amount();
         if (HasAmortizingSchedule())
         {
            callPrice *= GetOutstandingNotionalAtExercise(call.date()) / faceAmount_;
         }

         if (call.price().type() != Bond.Price.Type.Clean)
         {
            return callPrice;
         }

         if (HasAmortizingSchedule())
         {
            return callPrice + GetAccruedAmountAt(call.date()) * 100.0 / faceAmount_;
         }

         return callPrice + callAccrued(call.date());
      }

      protected double GetOutstandingNotionalAtExercise(Date exerciseDate)
      {
         if (!HasAmortizingSchedule())
         {
            return faceAmount_;
         }

         for (var i = 0; i < mainSchedule_.Count - 1; i++)
         {
            if (exerciseDate <= mainSchedule_[i + 1])
            {
               return notionalsByPeriod_[Math.Min(i, notionalsByPeriod_.Count - 1)];
            }
         }

         return notionalsByPeriod_.Last();
      }

      protected double GetAccruedAmountAt(Date settlement)
      {
         const bool includeToday = false;
         for (int i = 0; i < cashflows_.Count; ++i)
         {
            if (!cashflows_[i].hasOccurred(settlement, includeToday))
            {
               if (cashflows_[i] is Coupon coupon)
               {
                  return coupon.accruedAmount(settlement);
               }

               return 0.0;
            }
         }

         return 0.0;
      }

      protected List<double> CreateRedemptionMultipliers(Schedule schedule, List<double> notionals, double finalRedemption)
      {
         var redemptionMultipliers = new List<double> { 100.0 };
         for (var i = 0; i < schedule.Count - 1; i++)
         {
            var currentNotional = notionals[Math.Min(i, notionals.Count - 1)];
            var nextNotional = i + 1 < notionals.Count ? notionals[i + 1] : 0.0;
            if (nextNotional < currentNotional)
            {
               redemptionMultipliers.Add(i == schedule.Count - 2 ? finalRedemption : 100.0);
            }
         }

         return redemptionMultipliers;
      }

      protected void AddPrincipalCashflows(List<CashFlow> cashflows, List<double> redemptionMultipliers)
      {
         var notionals = new List<double>();
         var notionalSchedule = new List<Date>();
         Date lastPaymentDate = new Date();
         notionalSchedule.Add(new Date());

         foreach (var cashflow in cashflows)
         {
            if (cashflow is not Coupon coupon)
            {
               continue;
            }

            var notional = coupon.nominal();
            if (notionals.empty())
            {
               notionals.Add(notional);
               lastPaymentDate = coupon.date();
            }
            else if (!Utils.close(notional, notionals.Last()))
            {
               notionals.Add(notional);
               notionalSchedule.Add(lastPaymentDate);
               lastPaymentDate = coupon.date();
            }
            else
            {
               lastPaymentDate = coupon.date();
            }
         }

         notionals.Add(0.0);
         notionalSchedule.Add(lastPaymentDate);

         for (int i = 1; i < notionalSchedule.Count; ++i)
         {
            var redemptionMultiplier = i < redemptionMultipliers.Count ? redemptionMultipliers[i] : redemptionMultipliers.Last();
            var amount = (redemptionMultiplier / 100.0) * (notionals[i - 1] - notionals[i]);
            CashFlow payment = i < notionalSchedule.Count - 1
               ? new AmortizingPayment(amount, notionalSchedule[i])
               : new Redemption(amount, notionalSchedule[i]);
            cashflows.Add(payment);
         }

         var orderedCashflows = cashflows.OrderBy(cashflow => cashflow.date()).ToList();
         cashflows.Clear();
         cashflows.AddRange(orderedCashflows);
      }

      protected class CallableSupportFixedRateBond : Bond
      {
         public CallableSupportFixedRateBond(
            int settlementDays,
            Schedule schedule,
            List<double> coupons,
            List<double> notionals,
            DayCounter accrualDayCounter,
            BusinessDayConvention paymentConvention,
            Date issueDate,
            List<double> redemptionMultipliers)
            : base(settlementDays, schedule.calendar(), issueDate)
         {
            maturityDate_ = schedule.endDate();

            cashflows_ = new FixedRateLeg(schedule)
               .withCouponRates(coupons, accrualDayCounter)
               .withNotionals(notionals)
               .withPaymentAdjustment(paymentConvention)
               .value();

            calculateNotionalsFromCashflows();
            AddPrincipalCashflows(redemptionMultipliers);
         }

         private void AddPrincipalCashflows(List<double> redemptionMultipliers)
         {
            for (int i = 1; i < notionalSchedule_.Count; ++i)
            {
               var redemptionMultiplier = i < redemptionMultipliers.Count ? redemptionMultipliers[i] : redemptionMultipliers.Last();
               var amount = (redemptionMultiplier / 100.0) * (notionals_[i - 1] - notionals_[i]);
               CashFlow payment = i < notionalSchedule_.Count - 1
                  ? new AmortizingPayment(amount, notionalSchedule_[i])
                  : new Redemption(amount, notionalSchedule_[i]);

               cashflows_.Add(payment);
               if (payment is Redemption redemption)
               {
                  redemptions_.Add(redemption);
               }
            }

            cashflows_ = cashflows_.OrderBy(cashflow => cashflow.date()).ToList();
         }
      }
   }

   /// <summary>
   /// Callable fixed rate bond class.
   /// </summary>
   public class CallableFixedRateBond : CallableBond
   {
      protected double redemption_;

      public CallableFixedRateBond(int settlementDays,
         double faceAmount,
         Schedule schedule,
         List<double> coupons,
         DayCounter accrualDayCounter,
         BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
         double redemption = 100.0,
         Date issueDate = null,
         CallabilitySchedule putCallSchedule = null,
         Period exCouponPeriod = null,
         Calendar exCouponCalendar = null,
         BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted,
         bool exCouponEndOfMonth = false)
         : base(settlementDays, schedule.dates().Last(), schedule.calendar(), accrualDayCounter, faceAmount, issueDate,
            putCallSchedule)
      {
         mainSchedule_ = schedule;
         coupons_ = coupons;
         hasAmortizingSchedule_ = false;
         redemption_ = redemption;
         frequency_ = schedule.hasTenor() ? schedule.tenor().frequency() : Frequency.NoFrequency;
         paymentConvention_ = paymentConvention;
         cashflows_ = new FixedRateLeg(schedule)
           .withCouponRates(coupons, accrualDayCounter)
           .withExCouponPeriod(exCouponPeriod, exCouponCalendar, exCouponConvention, exCouponEndOfMonth)
           .withNotionals(faceAmount)
           .withPaymentAdjustment(paymentConvention);

         addRedemptionsToCashflows([redemption]);
      }

      public CallableFixedRateBond(int settlementDays,
         double faceAmount,
         Schedule schedule,
         List<double> coupons,
         List<double> notionals,
         DayCounter accrualDayCounter,
         BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
         double redemption = 100.0,
         Date issueDate = null,
         CallabilitySchedule putCallSchedule = null,
         Period exCouponPeriod = null,
         Calendar exCouponCalendar = null,
         BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted,
         bool exCouponEndOfMonth = false)
         : base(settlementDays, schedule.dates().Last(), schedule.calendar(), accrualDayCounter, faceAmount, issueDate,
           putCallSchedule)
      {
         mainSchedule_ = schedule;
         coupons_ = coupons;
         Utils.QL_REQUIRE(notionals is { Count: > 0 }, () => "no notionals provided");
         notionalsByPeriod_ = notionals;
         hasAmortizingSchedule_ = true;
         redemption_ = redemption;
         frequency_ = schedule.hasTenor() ? schedule.tenor().frequency() : Frequency.NoFrequency;
         paymentConvention_ = paymentConvention;
         cashflows_ = new FixedRateLeg(schedule)
           .withCouponRates(coupons, accrualDayCounter)
           .withExCouponPeriod(exCouponPeriod, exCouponCalendar, exCouponConvention, exCouponEndOfMonth)
           .withNotionals(notionals)
           .withPaymentAdjustment(paymentConvention)
           .value();

         calculateNotionalsFromCashflows();
         AddPrincipalCashflows(cashflows_, CreateRedemptionMultipliers(schedule, notionals, redemption));
         redemptions_.Clear();
         redemptions_.Add(cashflows_.OfType<Redemption>().Last());
      }

      public override CallableCalcs[] yieldToCalls(Date settlement, double price, Frequency frequency,
         double accuracy = 1.0e-10)
      {
         return yieldToCallsInternal(settlement, price, CouponType.FixedRate, frequency, accuracy);
      }

      public override CallableCalcs[] priceToCalls(Date settlement, double price, Frequency frequency)
      {
         return priceToCallsInternal(settlement, price, CouponType.FixedRate, frequency);
      }

      public override double yieldAt(Date settlementDate, double price, Frequency frequency, double accuracy,
         Date maturityDate = null, double? redemption = null)
      {
         return yieldAtInternal(CouponType.FixedRate, settlementDate, price, frequency, accuracy, maturityDate,
            redemption);
      }

      public override double priceAt(Date settlementDate, Date maturityDate, double? redemption, double yield,
         Frequency frequency)
      {
         return priceAtInternal(CouponType.FixedRate, settlementDate, maturityDate, redemption, yield, frequency);
      }

      public override double durationAt(Date settlementDate, Date maturityDate, double? redemption, double yield,
         Frequency frequency, Duration.Type durationType)
      {
         return durationAtInternal(CouponType.FixedRate, settlementDate, maturityDate, redemption, yield, frequency,
            durationType);
      }
   }

   /// <summary>
   /// Callable zero coupon bond class.
   /// </summary>
   public class CallableZeroCouponBond : CallableBond
   {
      public CallableZeroCouponBond(int settlementDays,
         double faceAmount,
         Calendar calendar,
         Date maturityDate,
         DayCounter dayCounter,
         BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
         double redemption = 100.0,
         Date issueDate = null,
         CallabilitySchedule putCallSchedule = null)
         : base(settlementDays, maturityDate, calendar, dayCounter, faceAmount, issueDate, putCallSchedule)
      {
         frequency_ = Frequency.Once;

         var redemptionDate = calendar_.adjust(maturityDate_, paymentConvention);
         setSingleRedemption(faceAmount, redemption, redemptionDate);
      }

      public override CallableCalcs[] yieldToCalls(Date settlement, double price, Frequency frequency,
         double accuracy = 1.0e-10)
      {
         return yieldToCallsInternal(settlement, price, CouponType.ZeroCoupon, frequency, accuracy);
      }

      public override CallableCalcs[] priceToCalls(Date settlement, double price, Frequency frequency)
      {
         return priceToCallsInternal(settlement, price, CouponType.ZeroCoupon, frequency);
      }

      public override double yieldAt(Date settlementDate, double price, Frequency frequency, double accuracy,
         Date maturityDate, double? redemption = null)
      {
         return yieldAtInternal(CouponType.ZeroCoupon, settlementDate, price, frequency, accuracy, maturityDate,
            redemption);
      }

      public override double priceAt(Date settlementDate, Date maturityDate, double? redemption, double yield,
         Frequency frequency)
      {
         return priceAtInternal(CouponType.ZeroCoupon, settlementDate, maturityDate, redemption, yield, frequency);
      }

      public override double durationAt(Date settlementDate, Date maturityDate, double? redemption, double yield,
         Frequency frequency, Duration.Type durationType)
      {
         return durationAtInternal(CouponType.ZeroCoupon, settlementDate, maturityDate, redemption, yield, frequency,
            durationType);
      }

   }
}
