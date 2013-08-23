/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet {
    //! floating-rate bond (possibly capped and/or floored)
    //! \test calculations are tested by checking results against cached values.
    public class FloatingRateBond : Bond {
        public FloatingRateBond(int settlementDays, double faceAmount, Schedule schedule, IborIndex index, 
                                DayCounter paymentDayCounter)
            : this(settlementDays, faceAmount, schedule, index, paymentDayCounter, BusinessDayConvention.Following,
                   0, new List<double>() { 1 }, new List<double>() { 0 }, new List<double>(), new List<double>(),
                   false, 100, null) { }
        public FloatingRateBond(int settlementDays, double faceAmount, Schedule schedule, IborIndex index,
                                DayCounter paymentDayCounter, BusinessDayConvention paymentConvention, int fixingDays, 
                                List<double> gearings, List<double> spreads)
            : this(settlementDays, faceAmount, schedule, index, paymentDayCounter, BusinessDayConvention.Following,
                   fixingDays, gearings, spreads, new List<double>(), new List<double>(), false, 100, null) { }
        public FloatingRateBond(int settlementDays, double faceAmount, Schedule schedule, IborIndex index, DayCounter paymentDayCounter,
                                BusinessDayConvention paymentConvention, int fixingDays, List<double> gearings, List<double> spreads,
                                List<double> caps, List<double> floors, bool inArrears, double redemption, Date issueDate)
            : base(settlementDays, schedule.calendar(), issueDate) {
            maturityDate_ = schedule.endDate();
            cashflows_ = new IborLeg(schedule, index)
                            .withPaymentDayCounter(paymentDayCounter)
                            .withFixingDays(fixingDays)
                            .withGearings(gearings)
                            .withSpreads(spreads)
                            .withCaps(caps)
                            .withFloors(floors)
                            .inArrears(inArrears)
                            .withNotionals(faceAmount)
                            .withPaymentAdjustment(paymentConvention);

            addRedemptionsToCashflows(new List<double>() { redemption });

            if (cashflows().Count == 0)
                throw new ApplicationException("bond with no cashflows!");
            if (redemptions_.Count != 1)
                throw new ApplicationException("multiple redemptions created");

            index.registerWith(update);
        }

        //public FloatingRateBond(int settlementDays, double faceAmount, Date startDate, Date maturityDate, Frequency couponFrequency,
        //                        Calendar calendar, IborIndex index, DayCounter accrualDayCounter,
        //                        BusinessDayConvention accrualConvention = Following,
        //                        BusinessDayConvention paymentConvention = Following,
        //                        int fixingDays = Null<Natural>(),
        //                        List<double> gearings = std::vector<Real>(1, 1.0),
        //                        List<double> spreads = std::vector<Spread>(1, 0.0),
        //                        List<double> caps = std::vector<Rate>(),
        //                        List<double> floors = std::vector<Rate>(),
        //                        bool inArrears = false,
        //                        double redemption = 100.0,
        //                        Date issueDate = Date(),
        //                        Date stubDate = Date(),
        //                        DateGeneration.Rule rule = DateGeneration::Backward,
        //                        bool endOfMonth = false)
        public FloatingRateBond(int settlementDays, double faceAmount, Date startDate, Date maturityDate, Frequency couponFrequency,
                                Calendar calendar, IborIndex index, DayCounter accrualDayCounter,
                                BusinessDayConvention accrualConvention, BusinessDayConvention paymentConvention,
                                int fixingDays, List<double> gearings, List<double> spreads, List<double> caps,
                                List<double> floors, bool inArrears, double redemption, Date issueDate,
                                Date stubDate, DateGeneration.Rule rule, bool endOfMonth)
            : base(settlementDays, calendar, issueDate) {

            maturityDate_ = maturityDate;

            Date firstDate, nextToLastDate;
            switch (rule) {
                case DateGeneration.Rule.Backward:
                    firstDate = null;
                    nextToLastDate = stubDate;
                    break;
                case DateGeneration.Rule.Forward:
                    firstDate = stubDate;
                    nextToLastDate = null;
                    break;
              case DateGeneration.Rule.Zero:
              case DateGeneration.Rule.ThirdWednesday:
              case DateGeneration.Rule.Twentieth:
              case DateGeneration.Rule.TwentiethIMM:
                    throw new ApplicationException("stub date (" + stubDate + ") not allowed with " + rule + " DateGeneration::Rule");
                default:
                    throw new ApplicationException("unknown DateGeneration::Rule (" + rule + ")");
            }

            Schedule schedule = new Schedule(startDate, maturityDate_, new Period(couponFrequency), calendar_,
                                             accrualConvention, accrualConvention, rule, endOfMonth, firstDate, nextToLastDate);

            cashflows_ = new IborLeg(schedule, index)
                            .withPaymentDayCounter(accrualDayCounter)
                            .withFixingDays(fixingDays)
                            .withGearings(gearings)
                            .withSpreads(spreads)
                            .withCaps(caps)
                            .withFloors(floors)
                            .inArrears(inArrears)
                            .withNotionals(faceAmount)
                            .withPaymentAdjustment(paymentConvention);

            addRedemptionsToCashflows(new List<double>() { redemption });

            if (cashflows().Count == 0)
                throw new ApplicationException("bond with no cashflows!");
            if (redemptions_.Count != 1)
                throw new ApplicationException("multiple redemptions created");

            index.registerWith(update);
        }

    }
}
