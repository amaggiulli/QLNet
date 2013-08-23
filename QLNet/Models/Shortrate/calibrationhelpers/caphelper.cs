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
    public class CapHelper :  CalibrationHelper {
        
        private Cap cap_;

        public  CapHelper(Period length,
                      Handle<Quote> volatility,
                      IborIndex index,
                      // data for ATM swap-rate calculation
                      Frequency fixedLegFrequency,
                      DayCounter fixedLegDayCounter,
                      bool includeFirstSwaplet,
                      Handle<YieldTermStructure> termStructure,
                      bool calibrateVolatility /*= false*/)
            : base(volatility, termStructure, calibrateVolatility)
        {        
            Period indexTenor = index.tenor();
            double fixedRate = 0.04; // dummy value
            Date startDate, maturity;
            if (includeFirstSwaplet) {
                startDate = termStructure.link.referenceDate();
                maturity = termStructure.link.referenceDate() + length;
            } else {
                startDate = termStructure.link.referenceDate() + indexTenor;
                maturity = termStructure.link.referenceDate() + length;
            }
            IborIndex dummyIndex=new
                IborIndex("dummy",
                          indexTenor,
                          index.fixingDays(),
                          index.currency(),
                          index.fixingCalendar(),
                          index.businessDayConvention(),
                          index.endOfMonth(),
                          termStructure.link.dayCounter(),
                          termStructure);

            List<double> nominals = new InitializedList<double>(1,1.0);

            Schedule floatSchedule=new Schedule(startDate, maturity,
                                   index.tenor(), index.fixingCalendar(),
                                   index.businessDayConvention(),
                                   index.businessDayConvention(),
                                   DateGeneration.Rule.Forward, false);
            List<CashFlow> floatingLeg;
            IborLeg iborLeg = (IborLeg) new IborLeg(floatSchedule, index)
                                            .withFixingDays(0)
                                            .withNotionals(nominals)
                                            .withPaymentAdjustment(index.businessDayConvention());
            floatingLeg = iborLeg.value();
            Schedule fixedSchedule=new Schedule(startDate, maturity, new Period(fixedLegFrequency),
                                   index.fixingCalendar(),
                                   BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                   DateGeneration.Rule.Forward, false);
            List<CashFlow> fixedLeg = new FixedRateLeg(fixedSchedule)
                .withCouponRates(fixedRate, fixedLegDayCounter)
                .withNotionals(nominals)
                .withPaymentAdjustment(index.businessDayConvention());

            Swap swap = new Swap(floatingLeg, fixedLeg);
            swap.setPricingEngine(new DiscountingSwapEngine(termStructure));
            double bp = 1.0e-4;
            double fairRate = fixedRate - (double)(swap.NPV()/(swap.legBPS(1) / bp));
            List<double> exerciceRate = new InitializedList<double>(1,fairRate);
            cap_ = new Cap(floatingLeg, exerciceRate);
            marketValue_ = blackPrice(volatility_.link.value());
        }

        public override void addTimesTo(List<double> times)
        {        
            CapFloor.Arguments args=new CapFloor.Arguments();
            cap_.setupArguments(args);
            List<double> capTimes =
            new DiscretizedCapFloor(args,
                                termStructure_.link.referenceDate(),
                                termStructure_.link.dayCounter()).mandatoryTimes();
            for (int i = 0; i < capTimes.Count; i++)
                times.Insert(times.Count, capTimes[i]);  
        }

        public override double modelValue()
        {
            cap_.setPricingEngine(engine_);
            return cap_.NPV();
        }

        public override double blackPrice(double sigma)
        {       
            Quote vol=new SimpleQuote(sigma);
            IPricingEngine black= new BlackCapFloorEngine(termStructure_,
                                                           new Handle<Quote>(vol));
            cap_.setPricingEngine(black);
            double value = cap_.NPV();
            cap_.unregisterWith(update);  
            cap_.setPricingEngine(engine_);
            return value;
        }
    }
}
