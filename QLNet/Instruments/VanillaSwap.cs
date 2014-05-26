/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 * 
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
   //! Plain-vanilla swap: fix vs floating leg
   /*! \warning if <tt>Settings::includeReferenceDateCashFlows()</tt>
                 is set to <tt>true</tt>, payments occurring at the
                 settlement date of the swap might be included in the
                 NPV and therefore affect the fair-rate and
                 fair-spread calculation. This might not be what you
                 want.
     
   \test  
   - the correctness of the returned value is tested by checking
   - that the price of a swap paying the fair fixed rate is null.
   - that the price of a swap receiving the fair floating-rate spread is null.
   - that the price of a swap decreases with the paid fixed rate.
   - that the price of a swap increases with the received floating-rate spread.
   - the correctness of the returned value is tested by checking it against a known good value.
*/
   public class VanillaSwap : Swap
   {
      public enum Type { Receiver = -1, Payer = 1 };

      private Type type_;
      private double fixedRate_;
      private double spread_;
      private double nominal_;

      private Schedule fixedSchedule_;
      public Schedule fixedSchedule() { return fixedSchedule_; }

      private DayCounter fixedDayCount_;
      public DayCounter fixedDayCount() { return fixedDayCount_; }

      private Schedule floatingSchedule_;
      public Schedule floatingSchedule() { return floatingSchedule_; }

      private IborIndex iborIndex_;
      private DayCounter floatingDayCount_;
      public DayCounter floatingDayCount() { return floatingDayCount_; }
      private BusinessDayConvention paymentConvention_;

      // results
      private double? fairRate_;
      private double? fairSpread_;

      // constructor
      public VanillaSwap(Type type, double nominal,
                         Schedule fixedSchedule, double fixedRate, DayCounter fixedDayCount,
                         Schedule floatSchedule, IborIndex iborIndex, double spread, DayCounter floatingDayCount,
                         BusinessDayConvention? paymentConvention = null) :
         base(2)
      {
         type_ = type;
         nominal_ = nominal;
         fixedSchedule_ = fixedSchedule;
         fixedRate_ = fixedRate;
         fixedDayCount_ = fixedDayCount;
         floatingSchedule_ = floatSchedule;
         iborIndex_ = iborIndex;
         spread_ = spread;
         floatingDayCount_ = floatingDayCount;

         if (paymentConvention.HasValue)
            paymentConvention_ = paymentConvention.Value;
         else
            paymentConvention_ = floatingSchedule_.businessDayConvention();

         legs_[0] = new FixedRateLeg(fixedSchedule)
                                     .withCouponRates(fixedRate, fixedDayCount)
                                     .withPaymentAdjustment(paymentConvention_)
                                     .withNotionals(nominal);

         legs_[1] = new IborLeg(floatSchedule, iborIndex)
                                     .withPaymentDayCounter(floatingDayCount)
            //.withFixingDays(iborIndex.fixingDays())
                                     .withSpreads(spread)
                                     .withNotionals(nominal)
                                     .withPaymentAdjustment(paymentConvention_);

         foreach (var cf in legs_[1])
            cf.registerWith(update);

         switch (type_)
         {
            case Type.Payer:
               payer_[0] = -1.0;
               payer_[1] = +1.0;
               break;
            case Type.Receiver:
               payer_[0] = +1.0;
               payer_[1] = -1.0;
               break;
            default:
               throw new ApplicationException("Unknown vanilla-swap type");
         }
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         VanillaSwap.Arguments arguments = args as VanillaSwap.Arguments;
         if (arguments == null)  // it's a swap engine...
            return;

         arguments.type = type_;
         arguments.nominal = nominal_;

         List<CashFlow> fixedCoupons = fixedLeg();

         arguments.fixedResetDates = new InitializedList<Date>(fixedCoupons.Count);
         arguments.fixedPayDates = new InitializedList<Date>(fixedCoupons.Count);
         arguments.fixedCoupons = new InitializedList<double>(fixedCoupons.Count);

         for (int i = 0; i < fixedCoupons.Count; ++i)
         {
            FixedRateCoupon coupon = (FixedRateCoupon)fixedCoupons[i];

            arguments.fixedPayDates[i] = coupon.date();
            arguments.fixedResetDates[i] = coupon.accrualStartDate();
            arguments.fixedCoupons[i] = coupon.amount();
         }

         List<CashFlow> floatingCoupons = floatingLeg();

         arguments.floatingResetDates = new InitializedList<Date>(floatingCoupons.Count);
         arguments.floatingPayDates = new InitializedList<Date>(floatingCoupons.Count);
         arguments.floatingFixingDates = new InitializedList<Date>(floatingCoupons.Count);
         arguments.floatingAccrualTimes = new InitializedList<double>(floatingCoupons.Count);
         arguments.floatingSpreads = new InitializedList<double>(floatingCoupons.Count);
         arguments.floatingCoupons = new InitializedList<double>(floatingCoupons.Count);
         for (int i = 0; i < floatingCoupons.Count; ++i)
         {
            IborCoupon coupon = (IborCoupon)floatingCoupons[i];

            arguments.floatingResetDates[i] = coupon.accrualStartDate();
            arguments.floatingPayDates[i] = coupon.date();

            arguments.floatingFixingDates[i] = coupon.fixingDate();
            arguments.floatingAccrualTimes[i] = coupon.accrualPeriod();
            arguments.floatingSpreads[i] = coupon.spread();
            try
            {
               arguments.floatingCoupons[i] = coupon.amount();
            }
            catch
            {
               arguments.floatingCoupons[i] = default(double);
            }
         }
      }


      ///////////////////////////////////////////////////
      // results
      public double fairRate()
      {
         calculate();
         if (fairRate_ == null) throw new ArgumentException("result not available");
         return fairRate_.GetValueOrDefault();
      }

      public double fairSpread()
      {
         calculate();
         if (fairSpread_ == null) throw new ArgumentException("result not available");
         return fairSpread_.GetValueOrDefault();
      }

      public double fixedLegBPS()
      {
         calculate();
         if (legBPS_[0] == null) throw new ArgumentException("result not available");
         return legBPS_[0].GetValueOrDefault();
      }
      public double fixedLegNPV()
      {
         calculate();
         if (legNPV_[0] == null) throw new ArgumentException("result not available");
         return legNPV_[0].GetValueOrDefault();
      }

      public double floatingLegBPS()
      {
         calculate();
         if (legBPS_[1] == null) throw new ArgumentException("result not available");
         return legBPS_[1].GetValueOrDefault();
      }
      public double floatingLegNPV()
      {
         calculate();
         if (legNPV_[1] == null) throw new ArgumentException("result not available");
         return legNPV_[1].GetValueOrDefault();
      }

      public IborIndex iborIndex()
      {
         return iborIndex_;
      }

      public double fixedRate { get { return fixedRate_; } }
      public double spread { get { return spread_; } }
      public double nominal { get { return nominal_; } }
      public Type swapType { get { return type_; } }
      public List<CashFlow> fixedLeg() { return legs_[0]; }
      public List<CashFlow> floatingLeg() { return legs_[1]; }


      protected override void setupExpired()
      {
         base.setupExpired();
         legBPS_[0] = legBPS_[1] = 0.0;
         fairRate_ = fairSpread_ = null;
      }

      public override void fetchResults(IPricingEngineResults r)
      {
         const double basisPoint = 1.0e-4;

         base.fetchResults(r);
         VanillaSwap.Results results = r as VanillaSwap.Results;

         if (results != null)
         { // might be a swap engine, so no error is thrown
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
               fairRate_ = fixedRate_ - NPV_.GetValueOrDefault() / (legBPS_[0] / basisPoint);
         }
         if (fairSpread_ == null)
         {
            // ditto
            if (legBPS_[1] != null)
               fairSpread_ = spread_ - NPV_.GetValueOrDefault() / (legBPS_[1] / basisPoint);
         }
      }


      //! %Arguments for simple swap calculation
      new public class Arguments : Swap.Arguments
      {
         public Type type;
         public double nominal;

         public List<Date> fixedResetDates;
         public List<Date> fixedPayDates;
         public List<double> floatingAccrualTimes;
         public List<Date> floatingResetDates;
         public List<Date> floatingFixingDates;
         public List<Date> floatingPayDates;

         public List<double> fixedCoupons;
         public List<double> floatingSpreads;
         public List<double> floatingCoupons;

         public Arguments()
         {
            type = Type.Receiver;
            nominal = default(double);
         }

         public override void validate()
         {
            base.validate();

            if (nominal == default(double)) throw new ArgumentException("nominal null or not set");
            if (fixedResetDates.Count != fixedPayDates.Count)
               throw new ArgumentException("number of fixed start dates different from number of fixed payment dates");
            if (fixedPayDates.Count != fixedCoupons.Count)
               throw new ArgumentException("number of fixed payment dates different from number of fixed coupon amounts");
            if (floatingResetDates.Count != floatingPayDates.Count)
               throw new ArgumentException("number of floating start dates different from number of floating payment dates");
            if (floatingFixingDates.Count != floatingPayDates.Count)
               throw new ArgumentException("number of floating fixing dates different from number of floating payment dates");
            if (floatingAccrualTimes.Count != floatingPayDates.Count)
               throw new ArgumentException("number of floating accrual Times different from number of floating payment dates");
            if (floatingSpreads.Count != floatingPayDates.Count)
               throw new ArgumentException("number of floating spreads different from number of floating payment dates");
            if (floatingPayDates.Count != floatingCoupons.Count)
               throw new ArgumentException("number of floating payment dates different from number of floating coupon amounts");
         }
      }

      //! %Results from simple swap calculation
      public new class Results : Swap.Results
      {
         public double? fairRate, fairSpread;
         public override void reset()
         {
            base.reset();
            fairRate = fairSpread = null;
         }
      }
   }
}
