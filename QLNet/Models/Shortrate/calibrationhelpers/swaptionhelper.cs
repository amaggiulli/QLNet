/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
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
    public class SwaptionHelper : CalibrationHelper {

      private double exerciseRate_;
      private VanillaSwap swap_;
      private Swaption swaption_;

      public  SwaptionHelper(Period maturity,
                       Period length,
                       Handle<Quote> volatility,
                       IborIndex index,
                       Period fixedLegTenor,
                       DayCounter fixedLegDayCounter,
                       DayCounter floatingLegDayCounter,
                       Handle<YieldTermStructure> termStructure,
                       bool calibrateVolatility /*= false*/)
       : base(volatility,termStructure, calibrateVolatility) {

        Calendar calendar = index.fixingCalendar();
        Period indexTenor = index.tenor();
        int fixingDays = index.fixingDays();

        Date exerciseDate   = calendar.advance(termStructure.link.referenceDate(),
                                                maturity,
                                                index.businessDayConvention());
        Date startDate      = calendar.advance(exerciseDate,
                                                fixingDays, TimeUnit.Days,
                                                index.businessDayConvention());
        Date endDate        = calendar.advance(startDate, length,
                                                index.businessDayConvention());

        Schedule fixedSchedule=new Schedule(startDate, endDate, fixedLegTenor, calendar,
                                            index.businessDayConvention(),
                                            index.businessDayConvention(),
                                            DateGeneration.Rule.Forward, false);
        Schedule floatSchedule=new Schedule(startDate, endDate, index.tenor(), calendar,
                                            index.businessDayConvention(),
                                            index.businessDayConvention(),
                                            DateGeneration.Rule.Forward, false);

        IPricingEngine swapEngine=new DiscountingSwapEngine(termStructure);

        VanillaSwap temp=new VanillaSwap(VanillaSwap.Type.Receiver, 1.0,
                                        fixedSchedule, 0.0, fixedLegDayCounter,
                                        floatSchedule, index, 0.0, floatingLegDayCounter);
        temp.setPricingEngine(swapEngine);
        exerciseRate_ = temp.fairRate();
        swap_ = new VanillaSwap(VanillaSwap.Type.Receiver, 1.0,
                            fixedSchedule, exerciseRate_, fixedLegDayCounter,
                            floatSchedule, index, 0.0, floatingLegDayCounter);
        swap_.setPricingEngine(swapEngine);

        Exercise exercise=new EuropeanExercise(exerciseDate);
        swaption_ = new Swaption(swap_, exercise);
        marketValue_ = blackPrice(volatility_.link.value());
}

      public override void addTimesTo(List<double> times) {        
        Swaption.Arguments args=new Swaption.Arguments();
        swaption_.setupArguments(args);

          List<double> swaptionTimes =
            new DiscretizedSwaption(args,
                                    termStructure_.link.referenceDate(),
                                    termStructure_.link.dayCounter()).mandatoryTimes();
          /*times.insert(times.end(),
                     swaptionTimes.begin(), swaptionTimes.end());*/
          for(int i=0;i<swaptionTimes.Count;i++) 
            times.Insert(times.Count, swaptionTimes[i]);  
      }

      public override double modelValue() {
          swaption_.setPricingEngine(engine_);
          return swaption_.NPV();
      }

      public override double blackPrice(double sigma){
            SimpleQuote sq=new SimpleQuote(sigma);
            Handle<Quote> vol = new Handle<Quote>(sq);
            IPricingEngine black=new BlackSwaptionEngine(termStructure_, vol);
            swaption_.setPricingEngine(black);
            double value = swaption_.NPV();
            swaption_.setPricingEngine(engine_);
            return value;
        }
    }
}
