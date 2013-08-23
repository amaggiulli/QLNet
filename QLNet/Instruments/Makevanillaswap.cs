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
using System.Linq;
using System.Text;

namespace QLNet {
    // helper class
    // This class provides a more comfortable way to instantiate standard market swap.
    public class MakeVanillaSwap {
        private Period forwardStart_, swapTenor_;
        private IborIndex iborIndex_;
        private double? fixedRate_;

        private Date effectiveDate_, terminationDate_;
        private Calendar fixedCalendar_, floatCalendar_;

        private VanillaSwap.Type type_;
        private double nominal_;
        private Period fixedTenor_, floatTenor_;
        private BusinessDayConvention fixedConvention_, fixedTerminationDateConvention_;
        private BusinessDayConvention floatConvention_, floatTerminationDateConvention_;
        private DateGeneration.Rule fixedRule_, floatRule_;
        private bool fixedEndOfMonth_, floatEndOfMonth_;
        private Date fixedFirstDate_, fixedNextToLastDate_;
        private Date floatFirstDate_, floatNextToLastDate_;
        private double floatSpread_;
        private DayCounter fixedDayCount_, floatDayCount_;

        IPricingEngine engine_;


        public MakeVanillaSwap(Period swapTenor, IborIndex index) :
            this(swapTenor, index, null, new Period(0, TimeUnit.Days)) { }
        public MakeVanillaSwap(Period swapTenor, IborIndex index, double? fixedRate) :
            this(swapTenor, index, fixedRate, new Period(0, TimeUnit.Days)) { }
        public MakeVanillaSwap(Period swapTenor, IborIndex index, double? fixedRate, Period forwardStart) {
            swapTenor_ = swapTenor;
            iborIndex_ = index;
            fixedRate_ = fixedRate;
            forwardStart_ = forwardStart;
            effectiveDate_ = null;
            fixedCalendar_ = floatCalendar_ = index.fixingCalendar();
            
            type_ = VanillaSwap.Type.Payer;
            nominal_ = 1.0;
            fixedTenor_ = new Period(1, TimeUnit.Years);
            floatTenor_ = index.tenor();
            fixedConvention_ = fixedTerminationDateConvention_ = BusinessDayConvention.ModifiedFollowing;
            floatConvention_ = floatTerminationDateConvention_ = index.businessDayConvention();
            fixedRule_ = floatRule_ = DateGeneration.Rule.Backward;
            fixedEndOfMonth_ = floatEndOfMonth_ = false;
            fixedFirstDate_ = fixedNextToLastDate_ = floatFirstDate_ = floatNextToLastDate_ = null;
            floatSpread_ = 0.0;
            fixedDayCount_ = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
            floatDayCount_ = index.dayCounter();

            engine_ = new DiscountingSwapEngine(index.forwardingTermStructure());
        }

        public MakeVanillaSwap receiveFixed() { return receiveFixed(true); }
        public MakeVanillaSwap receiveFixed(bool flag) {
            type_ = flag ? VanillaSwap.Type.Receiver : VanillaSwap.Type.Payer;
            return this;
        }
        public MakeVanillaSwap withType(VanillaSwap.Type type) {
            type_ = type;
            return this;
        }
        public MakeVanillaSwap withNominal(double n) {
            nominal_ = n;
            return this;
        }
        public MakeVanillaSwap withEffectiveDate(Date effectiveDate) {
            effectiveDate_ = effectiveDate;
            return this;
        }

        public MakeVanillaSwap withTerminationDate(Date terminationDate) {
            terminationDate_ = terminationDate;
            return this;
        }

        public MakeVanillaSwap withRule(DateGeneration.Rule r) {
            fixedRule_ = r;
            floatRule_ = r;
            return this;
        }

        public MakeVanillaSwap withDiscountingTermStructure(Handle<YieldTermStructure> discountingTermStructure) {
            engine_ = new DiscountingSwapEngine(discountingTermStructure);
            return this;
        }

        public MakeVanillaSwap withFixedLegTenor(Period t) {
            fixedTenor_ = t;
            return this;
        }
        public MakeVanillaSwap withFixedLegCalendar(Calendar cal) {
            fixedCalendar_ = cal;
            return this;
        }
        public MakeVanillaSwap withFixedLegConvention(BusinessDayConvention bdc) {
            fixedConvention_ = bdc;
            return this;
        }
        public MakeVanillaSwap withFixedLegTerminationDateConvention(BusinessDayConvention bdc) {
            fixedTerminationDateConvention_ = bdc;
            return this;
        }
        public MakeVanillaSwap withFixedLegRule(DateGeneration.Rule r) {
            fixedRule_ = r;
            return this;
        }
        public MakeVanillaSwap withFixedLegEndOfMonth() { return withFixedLegEndOfMonth(true); }
        public MakeVanillaSwap withFixedLegEndOfMonth(bool flag) {
            fixedEndOfMonth_ = flag;
            return this;
        }
        public MakeVanillaSwap withFixedLegFirstDate(Date d) {
            fixedFirstDate_ = d;
            return this;
        }
        public MakeVanillaSwap withFixedLegNextToLastDate(Date d) {
            fixedNextToLastDate_ = d;
            return this;
        }
        public MakeVanillaSwap withFixedLegDayCount(DayCounter dc) {
            fixedDayCount_ = dc;
            return this;
        }

        public MakeVanillaSwap withFloatingLegTenor(Period t) {
            floatTenor_ = t;
            return this;
        }
        public MakeVanillaSwap withFloatingLegCalendar(Calendar cal) {
            floatCalendar_ = cal;
            return this;
        }
        public MakeVanillaSwap withFloatingLegConvention(BusinessDayConvention bdc) {
            floatConvention_ = bdc;
            return this;
        }
        public MakeVanillaSwap withFloatingLegTerminationDateConvention(BusinessDayConvention bdc) {
            floatTerminationDateConvention_ = bdc;
            return this;
        }
        public MakeVanillaSwap withFloatingLegRule(DateGeneration.Rule r) {
            floatRule_ = r;
            return this;
        }
        public MakeVanillaSwap withFloatingLegEndOfMonth() { return withFloatingLegEndOfMonth(true); }
        public MakeVanillaSwap withFloatingLegEndOfMonth(bool flag) {
            floatEndOfMonth_ = flag;
            return this;
        }
        public MakeVanillaSwap withFloatingLegFirstDate(Date d) {
            floatFirstDate_ = d;
            return this;
        }
        public MakeVanillaSwap withFloatingLegNextToLastDate(Date d) {
            floatNextToLastDate_ = d;
            return this;
        }
        public MakeVanillaSwap withFloatingLegDayCount(DayCounter dc) {
            floatDayCount_ = dc;
            return this;
        }
        public MakeVanillaSwap withFloatingLegSpread(double sp) {
            floatSpread_ = sp;
            return this;
        }


        // swap creator
        public static implicit operator VanillaSwap(MakeVanillaSwap o) { return o.value(); }
        public VanillaSwap value() {
            Date startDate;

            if (effectiveDate_ != null)
                startDate = effectiveDate_;
            else {
                int fixingDays = iborIndex_.fixingDays();
                Date referenceDate = Settings.evaluationDate();
                Date spotDate = floatCalendar_.advance(referenceDate, new Period(fixingDays, TimeUnit.Days));
                startDate = spotDate + forwardStart_;
            }

            Date endDate;
            if (terminationDate_ != null)
                endDate = terminationDate_;
            else
                endDate = startDate + swapTenor_;


            Schedule fixedSchedule = new Schedule(startDate, endDate,
                                   fixedTenor_, fixedCalendar_,
                                   fixedConvention_, fixedTerminationDateConvention_,
                                   fixedRule_, fixedEndOfMonth_,
                                   fixedFirstDate_, fixedNextToLastDate_);

            Schedule floatSchedule = new Schedule(startDate, endDate,
                                   floatTenor_, floatCalendar_,
                                   floatConvention_, floatTerminationDateConvention_,
                                   floatRule_, floatEndOfMonth_,
                                   floatFirstDate_, floatNextToLastDate_);

            double? usedFixedRate = fixedRate_;
            if (fixedRate_ == null) {       // calculate a fair fixed rate if no fixed rate is provided
               if (iborIndex_.forwardingTermStructure().empty())
                    throw new ArgumentException("no forecasting term structure set to " + iborIndex_.name());
                VanillaSwap temp = new VanillaSwap(type_, nominal_, fixedSchedule, 0.0, fixedDayCount_,
                                                   floatSchedule, iborIndex_, floatSpread_, floatDayCount_);
                temp.setPricingEngine(new DiscountingSwapEngine(iborIndex_.forwardingTermStructure()));
                usedFixedRate = temp.fairRate();
            }

            VanillaSwap swap = new VanillaSwap(type_, nominal_, fixedSchedule, usedFixedRate.Value, fixedDayCount_,
                                               floatSchedule, iborIndex_, floatSpread_, floatDayCount_);
            swap.setPricingEngine(engine_);
            return swap;
        }
    }
}
