/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

 * This file is part of QLNet Project http://qlnet.sourceforge.net/

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
   //! Basis swap. Simple Libor swap vs Libor swap
   public class BasisSwap : Swap
   {
      public enum Type { Receiver = -1, Payer = 1 };

      private Type type_;
      private double spread1_,spread2_;
      private double nominal_;

      private Schedule floating1Schedule_;
      public Schedule floating1Schedule() { return floating1Schedule_; }

      private DayCounter floating1DayCount_;

      private Schedule floating2Schedule_;
      public Schedule floating2Schedule() { return floating2Schedule_; }

      private IborIndex iborIndex1_,iborIndex2_;
      private DayCounter floating2DayCount_;
      private BusinessDayConvention paymentConvention_;

      // results
      //private double? fairSpread1_;
      //private double? fairSpread2_;


      // constructor
      public BasisSwap(Type type, double nominal,
                         Schedule float1Schedule, IborIndex iborIndex1, double spread1, DayCounter float1DayCount,
                         Schedule float2Schedule, IborIndex iborIndex2, double spread2, DayCounter float2DayCount)
         : this(type, nominal, float1Schedule, iborIndex1, spread1, float1DayCount,
                               float2Schedule, iborIndex2, spread2, float2DayCount, null) { }
      public BasisSwap(Type type, double nominal,
                         Schedule float1Schedule, IborIndex iborIndex1, double spread1, DayCounter float1DayCount,
                         Schedule float2Schedule, IborIndex iborIndex2, double spread2, DayCounter float2DayCount,
                         BusinessDayConvention? paymentConvention) :
         base(2)
      {
         type_ = type;
         nominal_ = nominal;
         floating1Schedule_ = float1Schedule;
         spread1_ = spread1;
         floating1DayCount_ = float1DayCount;
         iborIndex1_ = iborIndex1;
         floating2Schedule_ = float2Schedule;
         spread2_ = spread2;
         floating2DayCount_ = float2DayCount;
         iborIndex2_ = iborIndex2;

         if (paymentConvention.HasValue)
            paymentConvention_ = paymentConvention.Value;
         else
            paymentConvention_ = floating1Schedule_.businessDayConvention();

         List<CashFlow> floating1Leg = new IborLeg(float1Schedule, iborIndex1)
                                     .withPaymentDayCounter(float1DayCount)
                                     .withSpreads(spread1)
                                     .withNotionals(nominal)
                                     .withPaymentAdjustment(paymentConvention_);

         List<CashFlow> floating2Leg = new IborLeg(float2Schedule, iborIndex2)
                                     .withPaymentDayCounter(float2DayCount)
                                     .withSpreads(spread2)
                                     .withNotionals(nominal)
                                     .withPaymentAdjustment(paymentConvention_);

         foreach (var cf in floating1Leg)
            cf.registerWith(update);
         foreach (var cf in floating2Leg)
            cf.registerWith(update);


         legs_[0] = floating1Leg;
         legs_[1] = floating2Leg;
         if (type_ == Type.Payer)
         {
            payer_[0] = -1;
            payer_[1] = +1;
         }
         else
         {
            payer_[0] = +1;
            payer_[1] = -1;
         }
      }


      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         BasisSwap.Arguments arguments = args as BasisSwap.Arguments;
         if (arguments == null)  // it's a swap engine...
            return;

         arguments.type = type_;
         arguments.nominal = nominal_;


         List<CashFlow> floating1Coupons = floating1Leg();

         arguments.floating1ResetDates = new InitializedList<Date>(floating1Coupons.Count);
         arguments.floating1PayDates = new InitializedList<Date>(floating1Coupons.Count);
         arguments.floating1FixingDates = new InitializedList<Date>(floating1Coupons.Count);
         arguments.floating1AccrualTimes = new InitializedList<double>(floating1Coupons.Count);
         arguments.floating1Spreads = new InitializedList<double>(floating1Coupons.Count);
         arguments.floating1Coupons = new InitializedList<double>(floating1Coupons.Count);
         for (int i = 0; i < floating1Coupons.Count; ++i)
         {
            IborCoupon coupon = (IborCoupon)floating1Coupons[i];

            arguments.floating1ResetDates[i] = coupon.accrualStartDate();
            arguments.floating1PayDates[i] = coupon.date();

            arguments.floating1FixingDates[i] = coupon.fixingDate();
            arguments.floating1AccrualTimes[i] = coupon.accrualPeriod();
            arguments.floating1Spreads[i] = coupon.spread();
            try
            {
               arguments.floating1Coupons[i] = coupon.amount();
            }
            catch
            {
               arguments.floating1Coupons[i] = default(double);
            }
         }

         List<CashFlow> floating2Coupons = floating2Leg();

         arguments.floating2ResetDates = new InitializedList<Date>(floating2Coupons.Count);
         arguments.floating2PayDates = new InitializedList<Date>(floating2Coupons.Count);
         arguments.floating2FixingDates = new InitializedList<Date>(floating2Coupons.Count);
         arguments.floating2AccrualTimes = new InitializedList<double>(floating2Coupons.Count);
         arguments.floating2Spreads = new InitializedList<double>(floating2Coupons.Count);
         arguments.floating2Coupons = new InitializedList<double>(floating2Coupons.Count);
         for (int i = 0; i < floating2Coupons.Count; ++i)
         {
            IborCoupon coupon = (IborCoupon)floating2Coupons[i];

            arguments.floating2ResetDates[i] = coupon.accrualStartDate();
            arguments.floating2PayDates[i] = coupon.date();

            arguments.floating2FixingDates[i] = coupon.fixingDate();
            arguments.floating2AccrualTimes[i] = coupon.accrualPeriod();
            arguments.floating2Spreads[i] = coupon.spread();
            try
            {
               arguments.floating2Coupons[i] = coupon.amount();
            }
            catch
            {
               arguments.floating2Coupons[i] = default(double);
            }
         }
      }


      ///////////////////////////////////////////////////
      // results
      public double floating1LegBPS()
      {
         calculate();
         if (legBPS_[0] == null) throw new ArgumentException("result not available");
         return legBPS_[0].GetValueOrDefault();
      }
      public double floating1LegNPV()
      {
         calculate();
         if (legNPV_[0] == null) throw new ArgumentException("result not available");
         return legNPV_[0].GetValueOrDefault();
      }

      public double floating2LegBPS()
      {
         calculate();
         if (legBPS_[1] == null) throw new ArgumentException("result not available");
         return legBPS_[1].GetValueOrDefault();
      }
      public double floating2LegNPV()
      {
         calculate();
         if (legNPV_[1] == null) throw new ArgumentException("result not available");
         return legNPV_[1].GetValueOrDefault();
      }

      public IborIndex iborIndex1() {return iborIndex1_;}
      public IborIndex iborIndex2() { return iborIndex2_; }
      public double spread1 { get { return spread1_; } }
      public double spread2 { get { return spread2_; } }
      public double nominal { get { return nominal_; } }
      public Type swapType { get { return type_; } }
      public List<CashFlow> floating1Leg() { return legs_[0]; }
      public List<CashFlow> floating2Leg() { return legs_[1]; }


      protected override void setupExpired()
      {
         base.setupExpired();
         legBPS_[0] = legBPS_[1] = 0.0;
      }

      public override void fetchResults(IPricingEngineResults r)
      {
         base.fetchResults(r);
         BasisSwap.Results results = r as BasisSwap.Results;
      }


      //! %Arguments for simple swap calculation
      new public class Arguments : Swap.Arguments
      {
         public Type type;
         public double nominal;

         public List<Date> floating1ResetDates;
         public List<Date> floating1PayDates;
         public List<double> floating1Coupons;

         public List<Date> floating2ResetDates;
         public List<Date> floating2PayDates;
         public List<double> floating2Coupons;

         // ****

         public List<double> floating1AccrualTimes;
         public List<Date> floating1FixingDates;
         public List<double> floating1Spreads;

         public List<double> floating2AccrualTimes;
         public List<Date> floating2FixingDates;
         public List<double> floating2Spreads;


         public Arguments()
         {
            type = Type.Receiver;
            nominal = default(double);
         }

         public override void validate()
         {
            base.validate();

            if (nominal == default(double)) throw new ArgumentException("nominal null or not set");
            if (floating1ResetDates.Count != floating1PayDates.Count)
               throw new ArgumentException("number of floating1 start dates different from number of floating1 payment dates");
            if (floating1PayDates.Count != floating1Coupons.Count)
               throw new ArgumentException("number of floating1 payment dates different from number of floating1 coupon amounts");
            if (floating2ResetDates.Count != floating2PayDates.Count)
               throw new ArgumentException("number of floating2 start dates different from number of floating2 payment dates");
            if (floating2PayDates.Count != floating2Coupons.Count)
               throw new ArgumentException("number of floating2 payment dates different from number of floating2 coupon amounts");


            if (floating1FixingDates.Count != floating1PayDates.Count)
               throw new ArgumentException("number of floating1 fixing dates different from number of floating1 payment dates");
            if (floating1AccrualTimes.Count != floating1PayDates.Count)
               throw new ArgumentException("number of floating1 accrual Times different from number of floating1 payment dates");
            if (floating1Spreads.Count != floating1PayDates.Count)
               throw new ArgumentException("number of floating1 spreads different from number of floating1 payment dates");

            if (floating2FixingDates.Count != floating2PayDates.Count)
               throw new ArgumentException("number of floating2 fixing dates different from number of floating2 payment dates");
            if (floating2AccrualTimes.Count != floating2PayDates.Count)
               throw new ArgumentException("number of floating2 accrual Times different from number of floating2 payment dates");
            if (floating2Spreads.Count != floating2PayDates.Count)
               throw new ArgumentException("number of floating2 spreads different from number of floating2 payment dates");

         }
      }

      //! %Results from simple swap calculation
      new class Results : Swap.Results
      {
         public override void reset()
         {
            base.reset();
         }
      }
   }
}
