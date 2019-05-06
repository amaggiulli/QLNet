//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   //! zero-inflation-indexed swap,
   /*! fixed x zero-inflation, i.e. fixed x CPI(i'th fixing)/CPI(base)
       versus floating + spread

       Note that this does ony the inflation-vs-floating-leg.
       Extension to inflation-vs-fixed-leg.  is simple - just replace
       the floating leg with a fixed leg.

       Typically there are notional exchanges at the end: either
       inflated-notional vs notional; or just (inflated-notional -
       notional) vs zero.  The latter is perhaphs more typical.
       \warning Setting subtractInflationNominal to true means that
       the original inflation nominal is subtracted from both
       nominals before they are exchanged, even if they are
       different.

       This swap can mimic a ZCIIS where [(1+q)^n - 1] is exchanged
       against (cpi ratio - 1), by using differnt nominals on each
       leg and setting subtractInflationNominal to true.  ALSO -
       there must be just one date in each schedule.

       The two legs can have different schedules, fixing (days vs
       lag), settlement, and roll conventions.  N.B. accrual
       adjustment periods are already in the schedules.  Trade date
       and swap settlement date are outside the scope of the
       instrument.
   */
   public class CPISwap : Swap
   {
      public enum Type { Receiver = -1, Payer = 1 }
      public new class Arguments : Swap.Arguments
      {
         public Arguments()
         {
            type = Type.Receiver;
            nominal = null;
         }

         public Type type { get; set; }
         public double? nominal { get; set; }

      }

      public new class Results : Swap.Results
      {
         public double? fairRate { get; set; }
         public double? fairSpread { get; set; }
         public override void reset()
         {
            base.reset();
            fairRate = null;
            fairSpread = null;
         }
      }

      public class Engine :  GenericEngine<CPISwap.Arguments, CPISwap.Results>
      {}

      public CPISwap(Type type,
                     double nominal,
                     bool subtractInflationNominal,
                     // float+spread leg
                     double spread,
                     DayCounter floatDayCount,
                     Schedule floatSchedule,
                     BusinessDayConvention floatPaymentRoll,
                     int fixingDays,
                     IborIndex floatIndex,
                     // fixed x inflation leg
                     double fixedRate,
                     double baseCPI,
                     DayCounter fixedDayCount,
                     Schedule fixedSchedule,
                     BusinessDayConvention fixedPaymentRoll,
                     Period observationLag,
                     ZeroInflationIndex fixedIndex,
                     InterpolationType observationInterpolation = InterpolationType.AsIndex,
                     double? inflationNominal = null)
      : base(2)
      {
         type_ = type;
         nominal_ = nominal;
         subtractInflationNominal_ = subtractInflationNominal;
         spread_ = spread;
         floatDayCount_ = floatDayCount;
         floatSchedule_ = floatSchedule;
         floatPaymentRoll_ = floatPaymentRoll;
         fixingDays_ = fixingDays;
         floatIndex_ = floatIndex;
         fixedRate_ = fixedRate;
         baseCPI_ = baseCPI;
         fixedDayCount_ = fixedDayCount;
         fixedSchedule_ = fixedSchedule;
         fixedPaymentRoll_ = fixedPaymentRoll;
         fixedIndex_ = fixedIndex;
         observationLag_ = observationLag;
         observationInterpolation_ = observationInterpolation;

         Utils.QL_REQUIRE(floatSchedule_.Count > 0, () => "empty float schedule");
         Utils.QL_REQUIRE(fixedSchedule_.Count > 0, () => "empty fixed schedule");
         // todo if roll!=unadjusted then need calendars ...

         inflationNominal_ = inflationNominal ?? nominal_;

         List<CashFlow> floatingLeg;
         if (floatSchedule_.Count > 1)
         {
            floatingLeg = new IborLeg(floatSchedule_, floatIndex_)
            .withFixingDays(fixingDays_)
            .withPaymentDayCounter(floatDayCount_)
            .withSpreads(spread_)
            .withNotionals(nominal_)
            .withPaymentAdjustment(floatPaymentRoll_);
         }
         else
            floatingLeg = new List<CashFlow>();

         if (floatSchedule_.Count == 1 ||
             !subtractInflationNominal_ ||
             (subtractInflationNominal && Math.Abs(nominal_ - inflationNominal_) > 0.00001)
            )
         {
            Date payNotional;
            if (floatSchedule_.Count == 1)
            {
               // no coupons
               payNotional = floatSchedule_[0];
               payNotional = floatSchedule_.calendar().adjust(payNotional, floatPaymentRoll_);
            }
            else
            {
               // use the pay date of the last coupon
               payNotional = floatingLeg.Last().date();
            }

            double floatAmount = subtractInflationNominal_ ? nominal_ - inflationNominal_ : nominal_;
            CashFlow nf = new SimpleCashFlow(floatAmount, payNotional);
            floatingLeg.Add(nf);
         }

         // a CPIleg know about zero legs and inclusion of base inflation notional
         List<CashFlow> cpiLeg = new CPILeg(fixedSchedule_, fixedIndex_, baseCPI_, observationLag_)
         .withFixedRates(fixedRate_)
         .withPaymentDayCounter(fixedDayCount_)
         .withObservationInterpolation(observationInterpolation_)
         .withSubtractInflationNominal(subtractInflationNominal_)
         .withNotionals(inflationNominal_)
         .withPaymentAdjustment(fixedPaymentRoll_);

         foreach (CashFlow cashFlow in cpiLeg)
         {
            cashFlow.registerWith(update);
         }

         if (floatingLeg.Count > 0)
         {
            foreach (CashFlow cashFlow in floatingLeg)
            {
               cashFlow.registerWith(update);
            }

         }

         legs_[0] = cpiLeg;
         legs_[1] = floatingLeg;


         if (type_ == Type.Payer)
         {
            payer_[0] = 1.0;
            payer_[1] = -1.0;
         }
         else
         {
            payer_[0] = -1.0;
            payer_[1] = 1.0;
         }
      }

      // results
      // float+spread
      public virtual double floatLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[1] != null, () => "result not available");
         return legNPV_[1].GetValueOrDefault();
      }

      public virtual double fairSpread()
      {
         calculate();
         Utils.QL_REQUIRE(fairSpread_ != null, () => "result not available");
         return fairSpread_.GetValueOrDefault();
      }
      // fixed rate x inflation
      public virtual double fixedLegNPV()
      {
         calculate();
         Utils.QL_REQUIRE(legNPV_[0] != null, () => "result not available");
         return legNPV_[0].GetValueOrDefault();
      }
      public virtual double fairRate()
      {
         calculate();
         Utils.QL_REQUIRE(fairRate_ != null, () => "result not available");
         return fairRate_.GetValueOrDefault();
      }

      // inspectors
      public virtual Type type() {return type_; }
      public virtual double nominal() {return nominal_;}
      public virtual bool subtractInflationNominal() {return subtractInflationNominal_;}

      // float+spread
      public virtual double spread() {return spread_; }
      public virtual DayCounter floatDayCount() {return floatDayCount_;}
      public virtual Schedule floatSchedule() {return floatSchedule_;}
      public virtual BusinessDayConvention floatPaymentRoll() {return floatPaymentRoll_;}
      public virtual int fixingDays() { return fixingDays_;}
      public virtual IborIndex floatIndex() {return floatIndex_;}

      // fixed rate x inflation
      public virtual double fixedRate() {return fixedRate_;}
      public virtual double baseCPI() {return baseCPI_;}
      public virtual DayCounter fixedDayCount() {return fixedDayCount_; }
      public virtual Schedule fixedSchedule() {return fixedSchedule_; }
      public virtual BusinessDayConvention fixedPaymentRoll() {return fixedPaymentRoll_;}
      public virtual Period observationLag() {return observationLag_; }
      public virtual ZeroInflationIndex fixedIndex() {return fixedIndex_;}
      public virtual InterpolationType observationInterpolation() {return observationInterpolation_;}
      public virtual double inflationNominal() {return inflationNominal_;}

      // legs
      public virtual List<CashFlow>  cpiLeg() {return legs_[0];}
      public virtual List<CashFlow> floatLeg() {return legs_[1];}

      // other
      public override void fetchResults(IPricingEngineResults r)
      {         
         // copy from VanillaSwap
         // works because similarly simple instrument
         // that we always expect to be priced with a swap engine

         base.fetchResults(r);

         CPISwap.Results results = r as CPISwap.Results;

         if (results != null)
         {
            // might be a swap engine, so no error is thrown
            fairRate_ = results.fairRate;
            fairSpread_ = results.fairSpread;
         }
         else
         {
            fairRate_ = null;
            fairSpread_ = null;
         }

         if (fairRate_ == null)
         {
            // calculate it from other results
            if (legBPS_[0] != null)
               fairRate_ = fixedRate_ - NPV_ / (legBPS_[0] / Const.BASIS_POINT);
         }
         if (fairSpread_ == null)
         {
            // ditto
            if (legBPS_[1] != null)
               fairSpread_ = spread_ - NPV_ / (legBPS_[1] / Const.BASIS_POINT);
         }
      }

      protected override void setupExpired()
      {
         base.setupExpired();
         legBPS_[0] = legBPS_[1] = 0.0;
         fairRate_ = null;
         fairSpread_ = null;
      }

      private Type type_;
      private double nominal_;
      private bool subtractInflationNominal_;

      // float+spread leg
      private double spread_;
      private DayCounter floatDayCount_;
      private Schedule floatSchedule_;
      private BusinessDayConvention floatPaymentRoll_;
      private int fixingDays_;
      private IborIndex floatIndex_;

      // fixed x inflation leg
      private double fixedRate_;
      private double baseCPI_;
      private DayCounter fixedDayCount_;
      private Schedule fixedSchedule_;
      private BusinessDayConvention fixedPaymentRoll_;
      private ZeroInflationIndex fixedIndex_;
      private Period observationLag_;
      private InterpolationType observationInterpolation_;
      private double inflationNominal_;
      // results
      private double? fairSpread_;
      private double? fairRate_;
   }
}
