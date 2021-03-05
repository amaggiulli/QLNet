﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
   public class SwaptionHelper : CalibrationHelper
   {
      public SwaptionHelper(Period maturity,
                            Period length,
                            Handle<Quote> volatility,
                            IborIndex index,
                            Period fixedLegTenor,
                            DayCounter fixedLegDayCounter,
                            DayCounter floatingLegDayCounter,
                            Handle<YieldTermStructure> termStructure,
                            CalibrationErrorType errorType = CalibrationErrorType.RelativePriceError,
                            double? strike = null,
                            double nominal = 1.0,
                            VolatilityType type = VolatilityType.ShiftedLognormal,
                            double shift = 0.0)
      : base(volatility, termStructure, errorType, type, shift)
      {
         exerciseDate_ = null;
         endDate_ = null;
         maturity_ = maturity;
         length_ = length;
         fixedLegTenor_ = fixedLegTenor;
         index_ = index;
         fixedLegDayCounter_ = fixedLegDayCounter;
         floatingLegDayCounter_ = floatingLegDayCounter;
         strike_ = strike;
         nominal_ = nominal;

         index_.registerWith(update);
      }

      public SwaptionHelper(Date exerciseDate,
                            Period length,
                            Handle<Quote> volatility,
                            IborIndex index,
                            Period fixedLegTenor,
                            DayCounter fixedLegDayCounter,
                            DayCounter floatingLegDayCounter,
                            Handle<YieldTermStructure> termStructure,
                            CalibrationErrorType errorType = CalibrationErrorType.RelativePriceError,
                            double? strike = null,
                            double nominal = 1.0,
                            VolatilityType type = VolatilityType.ShiftedLognormal,
                            double shift = 0.0)
      : base(volatility, termStructure, errorType, type, shift)
      {
         exerciseDate_ = exerciseDate;
         endDate_ = null;
         maturity_ = new Period(0, TimeUnit.Days);
         length_ = length;
         fixedLegTenor_ = fixedLegTenor;
         index_ = index;
         fixedLegDayCounter_ = fixedLegDayCounter;
         floatingLegDayCounter_ = floatingLegDayCounter;
         strike_ = strike;
         nominal_ = nominal;

         index_.registerWith(update);
      }

      public SwaptionHelper(Date exerciseDate,
                            Date endDate,
                            Handle<Quote> volatility,
                            IborIndex index,
                            Period fixedLegTenor,
                            DayCounter fixedLegDayCounter,
                            DayCounter floatingLegDayCounter,
                            Handle<YieldTermStructure> termStructure,
                            CalibrationErrorType errorType = CalibrationErrorType.RelativePriceError,
                            double? strike = null,
                            double nominal = 1.0,
                            VolatilityType type = VolatilityType.ShiftedLognormal,
                            double shift = 0.0)
      : base(volatility, termStructure, errorType, type, shift)
      {
         exerciseDate_ = exerciseDate;
         endDate_ = endDate;
         maturity_ = new Period(0, TimeUnit.Days);
         length_ = new Period(0, TimeUnit.Days);
         fixedLegTenor_ = fixedLegTenor;
         index_ = index;
         fixedLegDayCounter_ = fixedLegDayCounter;
         floatingLegDayCounter_ = floatingLegDayCounter;
         strike_ = strike;
         nominal_ = nominal;

         index_.registerWith(update);
      }


      public override void addTimesTo(List<double> times)
      {
         calculate();
         Swaption.Arguments args = new Swaption.Arguments();
         swaption_.setupArguments(args);

         List<double> swaptionTimes =
            new DiscretizedSwaption(args,
                                    termStructure_.link.referenceDate(),
                                    termStructure_.link.dayCounter()).mandatoryTimes();

         for (int i = 0; i < swaptionTimes.Count; i++)
            times.Insert(times.Count, swaptionTimes[i]);
      }

      public override double modelValue()
      {
         calculate();
         swaption_.setPricingEngine(engine_);
         return swaption_.NPV();
      }

      public override double blackPrice(double sigma)
      {
         calculate();
         SimpleQuote sq = new SimpleQuote(sigma);
         Handle<Quote> vol = new Handle<Quote>(sq);

         IPricingEngine engine = null;
         switch (volatilityType_)
         {
            case VolatilityType.ShiftedLognormal:
               engine = new BlackSwaptionEngine(termStructure_, vol, new Actual365Fixed(), shift_);
               break;

            case VolatilityType.Normal:
               engine = new BachelierSwaptionEngine(termStructure_, vol, new Actual365Fixed());
               break;

            default:
               Utils.QL_FAIL("can not construct engine: " + volatilityType_);
               break;
         }

         swaption_.setPricingEngine(engine);
         double value = swaption_.NPV();
         swaption_.setPricingEngine(engine_);
         return value;
      }

      public VanillaSwap underlyingSwap() { calculate(); return swap_; }
      public Swaption swaption() { calculate(); return swaption_; }

      protected override void performCalculations()
      {
         Calendar calendar = index_.fixingCalendar();
         int fixingDays = index_.fixingDays();

         Date exerciseDate = exerciseDate_;
         if (exerciseDate == null)
            exerciseDate = calendar.advance(termStructure_.link.referenceDate(),
                                            maturity_,
                                            index_.businessDayConvention());

         Date startDate = calendar.advance(exerciseDate,
                                           fixingDays, TimeUnit.Days,
                                           index_.businessDayConvention());

         Date endDate = endDate_;
         if (endDate == null)
            endDate = calendar.advance(startDate, length_,
                                       index_.businessDayConvention());

         Schedule fixedSchedule = new Schedule(startDate, endDate, fixedLegTenor_, calendar,
                                               index_.businessDayConvention(),
                                               index_.businessDayConvention(),
                                               DateGeneration.Rule.Forward, false);
         Schedule floatSchedule = new Schedule(startDate, endDate, index_.tenor(), calendar,
                                               index_.businessDayConvention(),
                                               index_.businessDayConvention(),
                                               DateGeneration.Rule.Forward, false);

         IPricingEngine swapEngine = new DiscountingSwapEngine(termStructure_, false);

         VanillaSwap.Type type = VanillaSwap.Type.Receiver;

         VanillaSwap temp = new VanillaSwap(VanillaSwap.Type.Receiver, nominal_,
                                            fixedSchedule, 0.0, fixedLegDayCounter_,
                                            floatSchedule, index_, 0.0, floatingLegDayCounter_);
         temp.setPricingEngine(swapEngine);
         double forward = temp.fairRate();
         if (!strike_.HasValue)
         {
            exerciseRate_ = forward;
         }
         else
         {
            exerciseRate_ = strike_.Value;
            type = strike_ <= forward ? VanillaSwap.Type.Receiver : VanillaSwap.Type.Payer;
            // ensure that calibration instrument is out of the money
         }
         swap_ = new VanillaSwap(type, nominal_,
                                 fixedSchedule, exerciseRate_, fixedLegDayCounter_,
                                 floatSchedule, index_, 0.0, floatingLegDayCounter_);
         swap_.setPricingEngine(swapEngine);

         Exercise exercise = new EuropeanExercise(exerciseDate);

         swaption_ = new Swaption(swap_, exercise);

         base.performCalculations();
      }

      private Date exerciseDate_, endDate_;
      private Period maturity_, length_, fixedLegTenor_;
      private IborIndex index_;
      private DayCounter fixedLegDayCounter_, floatingLegDayCounter_;
      private double? strike_;
      private double nominal_;
      private double exerciseRate_;
      private VanillaSwap swap_;
      private Swaption swaption_;
   }
}
