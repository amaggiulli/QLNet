/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

namespace QLNet
{
   public interface ISwaptionEngineSpec
   {
      VolatilityType type();

      double value(Option.Type type, double strike, double atmForward, double stdDev, double annuity,
                   double displacement = 0.0);

      double vega(double strike, double atmForward, double stdDev, double exerciseTime, double annuity,
                  double displacement = 0.0);
   }

   /*! Generic Black-style-formula swaption engine
       This is the base class for the Black and Bachelier swaption engines */
   public class BlackStyleSwaptionEngine<Spec> : SwaptionEngine
      where Spec : ISwaptionEngineSpec, new ()
   {
      public enum CashAnnuityModel
      {
         SwapRate,
         DiscountCurve
      };

      public BlackStyleSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                      double vol,
                                      DayCounter dc = null,
                                      double? displacement = 0.0,
                                      CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
      {
         dc = dc == null ? new Actual365Fixed() : dc;
         discountCurve_ = discountCurve;
         vol_ = new Handle<SwaptionVolatilityStructure>(new ConstantSwaptionVolatility(0, new NullCalendar(),
                                                                                       BusinessDayConvention.Following, vol, dc, new Spec().type(), displacement));
         model_ = model;
         displacement_ = displacement;
         discountCurve_.registerWith(update);
      }

      public BlackStyleSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                      Handle<Quote> vol,
                                      DayCounter dc = null,
                                      double? displacement = 0.0,
                                      CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
      {
         dc = dc == null ? new Actual365Fixed() : dc;
         discountCurve_ = discountCurve;
         vol_ = new Handle<SwaptionVolatilityStructure>(new ConstantSwaptionVolatility(0, new NullCalendar(),
                                                                                       BusinessDayConvention.Following, vol, dc, new Spec().type(), displacement));
         model_ = model;
         displacement_ = displacement;
         discountCurve_.registerWith(update);
         vol_.registerWith(update);
      }

      public BlackStyleSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                      Handle<SwaptionVolatilityStructure> volatility,
                                      double? displacement = 0.0,
                                      CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
      {
         discountCurve_ = discountCurve;
         vol_ = volatility;
         model_ = model;
         displacement_ = displacement;
         discountCurve_.registerWith(update);
         vol_.registerWith(update);
      }

      public override void calculate()
      {

         Date exerciseDate = arguments_.exercise.date(0);

         // the part of the swap preceding exerciseDate should be truncated
         // to avoid taking into account unwanted cashflows
         // for the moment we add a check avoiding this situation
         VanillaSwap swap = arguments_.swap;

         double strike = swap.fixedRate;
         List<CashFlow> fixedLeg = swap.fixedLeg();
         FixedRateCoupon firstCoupon = fixedLeg[0] as FixedRateCoupon;

         Utils.QL_REQUIRE(firstCoupon != null, () => "wrong coupon type");

         Utils.QL_REQUIRE(firstCoupon.accrualStartDate() >= exerciseDate,
                          () => "swap start (" + firstCoupon.accrualStartDate() + ") before exercise date ("
                          + exerciseDate + ") not supported in Black swaption engine");

         // using the forecasting curve
         swap.setPricingEngine(new DiscountingSwapEngine(swap.iborIndex().forwardingTermStructure()));
         double atmForward = swap.fairRate();

         // Volatilities are quoted for zero-spreaded swaps.
         // Therefore, any spread on the floating leg must be removed
         // with a corresponding correction on the fixed leg.
         if (swap.spread.IsNotEqual(0.0))
         {
            double correction = swap.spread * Math.Abs(swap.floatingLegBPS() / swap.fixedLegBPS());
            strike -= correction;
            atmForward -= correction;
            results_.additionalResults["spreadCorrection"] = correction;
         }
         else
         {
            results_.additionalResults["spreadCorrection"] = 0.0;
         }
         results_.additionalResults["strike"] = strike;
         results_.additionalResults["atmForward"] = atmForward;

         // using the discounting curve
         swap.setPricingEngine(new DiscountingSwapEngine(discountCurve_, false));
         double annuity = 0;
         if (arguments_.settlementType == Settlement.Type.Physical ||
             (arguments_.settlementType == Settlement.Type.Cash &&
              arguments_.settlementMethod == Settlement.Method.CollateralizedCashPrice))
         {
            annuity = Math.Abs(swap.fixedLegBPS()) / Const.BASIS_POINT;
         }
         else if (arguments_.settlementType == Settlement.Type.Cash &&
                  arguments_.settlementMethod == Settlement.Method.ParYieldCurve)
         {
            DayCounter dayCount = firstCoupon.dayCounter();

            // we assume that the cash settlement date is equal
            // to the swap start date
            Date discountDate = model_ == CashAnnuityModel.DiscountCurve
                                ? firstCoupon.accrualStartDate()
                                : discountCurve_.link.referenceDate();

            double fixedLegCashBPS =
               CashFlows.bps(fixedLeg,
                             new InterestRate(atmForward, dayCount, Compounding.Compounded, Frequency.Annual), false,
                             discountDate);

            annuity = Math.Abs(fixedLegCashBPS / Const.BASIS_POINT) * discountCurve_.link.discount(discountDate);
         }
         else
         {
            Utils.QL_FAIL("unknown settlement type");
         }
         results_.additionalResults["annuity"] = annuity;

         double swapLength = vol_.link.swapLength(swap.floatingSchedule().dates().First(),
                                                  swap.floatingSchedule().dates().Last());
         results_.additionalResults["swapLength"] = swapLength;

         double variance = vol_.link.blackVariance(exerciseDate,
                                                   swapLength,
                                                   strike);
         double displacement = displacement_ == null
                               ? vol_.link.shift(exerciseDate, swapLength)
                               : Convert.ToDouble(displacement_);
         double stdDev = Math.Sqrt(variance);
         results_.additionalResults["stdDev"] = stdDev;
         Option.Type w = (arguments_.type == VanillaSwap.Type.Payer) ? Option.Type.Call : Option.Type.Put;
         results_.value = new Spec().value(w, strike, atmForward, stdDev, annuity, displacement);

         double exerciseTime = vol_.link.timeFromReference(exerciseDate);
         results_.additionalResults["vega"] =
            new Spec().vega(strike, atmForward, stdDev, exerciseTime, annuity, displacement);
      }

      public Handle<YieldTermStructure> termStructure()
      {
         return discountCurve_;
      }

      public Handle<SwaptionVolatilityStructure> volatility()
      {
         return vol_;
      }

      private Handle<YieldTermStructure> discountCurve_;
      private Handle<SwaptionVolatilityStructure> vol_;
      private CashAnnuityModel model_;
      private double? displacement_;
   }

   // shifted lognormal type engine
   public class Black76Spec : ISwaptionEngineSpec
   {
      private VolatilityType type_;

      public VolatilityType type()
      {
         return type_;
      }

      public Black76Spec()
      {
         type_ = VolatilityType.ShiftedLognormal;
      }

      public double value(Option.Type type, double strike, double atmForward, double stdDev, double annuity,
                          double displacement = 0.0)
      {
         return Utils.blackFormula(type, strike, atmForward, stdDev, annuity, displacement);
      }

      public double vega(double strike, double atmForward, double stdDev, double exerciseTime, double annuity,
                         double displacement = 0.0)
      {
         return Math.Sqrt(exerciseTime) *
                Utils.blackFormulaStdDevDerivative(strike, atmForward, stdDev, annuity, displacement);
      }
   }

   // shifted lognormal type engine
   public class BachelierSpec : ISwaptionEngineSpec
   {
      private VolatilityType type_;

      public VolatilityType type()
      {
         return type_;
      }

      public BachelierSpec()
      {
         type_ = VolatilityType.Normal;
      }

      public double value(Option.Type type, double strike, double atmForward, double stdDev, double annuity,
                          double displacement = 0.0)
      {
         return Utils.bachelierBlackFormula(type, strike, atmForward, stdDev, annuity);
      }

      public double vega(double strike, double atmForward, double stdDev, double exerciseTime, double annuity,
                         double displacement = 0.0)
      {
         return Math.Sqrt(exerciseTime) *
                Utils.bachelierBlackFormulaStdDevDerivative(strike, atmForward, stdDev, annuity);
      }
   }

   public class BlackSwaptionEngine : BlackStyleSwaptionEngine<Black76Spec>
   {
      public BlackSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                 double vol, DayCounter dc = null,
                                 double? displacement = 0.0,
                                 CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
      : base(discountCurve, vol, dc, displacement, model)
      { }

      public BlackSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                 Handle<Quote> vol, DayCounter dc = null,
                                 double? displacement = 0.0,
                                 CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
      : base(discountCurve, vol, dc, displacement, model)
      { }

      public BlackSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                 Handle<SwaptionVolatilityStructure> vol,
                                 double? displacement = null,
                                 CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
      : base(discountCurve, vol, displacement, model)
      {
         Utils.QL_REQUIRE(vol.link.volatilityType() == VolatilityType.ShiftedLognormal,
                          () => "BlackSwaptionEngine requires (shifted) lognormal input volatility");
      }
   }

   public class BachelierSwaptionEngine : BlackStyleSwaptionEngine<BachelierSpec>
   {
      public BachelierSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                     double vol, DayCounter dc = null,
                                     CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
         : base(discountCurve, vol, dc, 0.0, model)
      { }

      public BachelierSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                     Handle<Quote> vol, DayCounter dc = null,
                                     CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
         : base(discountCurve, vol, dc, 0.0, model)
      { }

      public BachelierSwaptionEngine(Handle<YieldTermStructure> discountCurve,
                                     Handle<SwaptionVolatilityStructure> vol,
                                     CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
         : base(discountCurve, vol, 0.0, model)
      {
         Utils.QL_REQUIRE(vol.link.volatilityType() == VolatilityType.Normal,
                          () => "BachelierSwaptionEngine requires normal input volatility");
      }
   }
}
