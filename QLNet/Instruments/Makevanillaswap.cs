/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet {
    // helper class
    // This class provides a more comfortable way to instantiate standard market swap.
    public class MakeVanillaSwap {
        private Period forwardStart_, swapTenor_;
        private IborIndex iborIndex_;
        private double? fixedRate_;
        private int settlementDays_;
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

        public MakeVanillaSwap(Period swapTenor, IborIndex index, double? fixedRate = null, Period forwardStart = null) 
        {
            swapTenor_ = swapTenor;
            iborIndex_ = index;
            fixedRate_ = fixedRate;
            forwardStart_ = forwardStart?? new Period(0,TimeUnit.Days);
            settlementDays_ = iborIndex_.fixingDays();
            fixedCalendar_ = floatCalendar_ = index.fixingCalendar();
            
            type_ = VanillaSwap.Type.Payer;
            nominal_ = 1.0;
            //fixedTenor_ = new Period(1, TimeUnit.Years);
            floatTenor_ = index.tenor();
            fixedConvention_ = fixedTerminationDateConvention_ = BusinessDayConvention.ModifiedFollowing;
            floatConvention_ = floatTerminationDateConvention_ = index.businessDayConvention();
            fixedRule_ = floatRule_ = DateGeneration.Rule.Backward;
            fixedEndOfMonth_ = floatEndOfMonth_ = false;
            fixedFirstDate_ = fixedNextToLastDate_ = floatFirstDate_ = floatNextToLastDate_ = null;
            floatSpread_ = 0.0;
            //fixedDayCount_ = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
            floatDayCount_ = index.dayCounter();

            //engine_ = new DiscountingSwapEngine(index.forwardingTermStructure());
        }

        public MakeVanillaSwap receiveFixed(bool flag = true) {
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
        public MakeVanillaSwap withSettlementDays( int settlementDays )
        {
           settlementDays_ = settlementDays;
           effectiveDate_ = null;
           return this;
        }

        public MakeVanillaSwap withEffectiveDate(Date effectiveDate) {
            effectiveDate_ = effectiveDate;
            return this;
        }

        public MakeVanillaSwap withTerminationDate(Date terminationDate) {
            terminationDate_ = terminationDate;
            swapTenor_ = null;
            return this;
        }

        public MakeVanillaSwap withRule(DateGeneration.Rule r) {
            fixedRule_ = r;
            floatRule_ = r;
            return this;
        }

        public MakeVanillaSwap withDiscountingTermStructure(Handle<YieldTermStructure> discountingTermStructure) {
            engine_ = new DiscountingSwapEngine(discountingTermStructure,false);
            return this;
        }

        public MakeVanillaSwap withPricingEngine(IPricingEngine engine) 
        {
           engine_ = engine;
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

        public MakeVanillaSwap withFixedLegEndOfMonth(bool flag = true ) {
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

        public MakeVanillaSwap withFloatingLegEndOfMonth(bool flag = true) {
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
                //int fixingDays = iborIndex_.fixingDays();
                Date refDate = Settings.evaluationDate();
                // if the evaluation date is not a business day
                // then move to the next business day
                refDate = floatCalendar_.adjust( refDate );
                Date spotDate = floatCalendar_.advance( refDate, new Period( settlementDays_, TimeUnit.Days ) );
                startDate = spotDate + forwardStart_;
                if ( forwardStart_.length() < 0 )
                   startDate = floatCalendar_.adjust( startDate,BusinessDayConvention.Preceding);
                else
                   startDate = floatCalendar_.adjust( startDate,BusinessDayConvention.Following );
            }

           Date endDate = terminationDate_;
           if ( endDate == null )
              if ( floatEndOfMonth_ )
                 endDate = floatCalendar_.advance( startDate,
                                                  swapTenor_,
                                                  BusinessDayConvention.ModifiedFollowing,
                                                  floatEndOfMonth_ );
              else
                 endDate = startDate + swapTenor_;

           Currency curr = iborIndex_.currency();
           Period fixedTenor = null ;
           if (fixedTenor_ != null)
              fixedTenor = fixedTenor_;
           else 
           {
              if ((curr == new EURCurrency()) ||
                  (curr == new USDCurrency()) ||
                  (curr == new CHFCurrency()) ||
                  (curr == new SEKCurrency()) ||
                  (curr == new GBPCurrency() && swapTenor_ <= new Period( 1 , TimeUnit.Years)))
                  fixedTenor = new Period(1, TimeUnit.Years);
              else if ((curr == new GBPCurrency() && swapTenor_ > new Period(1 , TimeUnit.Years) ||
                       (curr == new JPYCurrency()) ||
                       (curr == new AUDCurrency() && swapTenor_ >= new Period(4 , TimeUnit.Years))))
                  fixedTenor = new Period(6, TimeUnit.Months);
              else if ((curr == new HKDCurrency() ||
                     (curr == new AUDCurrency() && swapTenor_ < new Period(4 , TimeUnit.Years))))
                  fixedTenor = new Period(3, TimeUnit.Months);
              else
                Utils.QL_FAIL("unknown fixed leg default tenor for " + curr);
           }

            Schedule fixedSchedule = new Schedule(startDate, endDate,
                                   fixedTenor, fixedCalendar_,
                                   fixedConvention_, fixedTerminationDateConvention_,
                                   fixedRule_, fixedEndOfMonth_,
                                   fixedFirstDate_, fixedNextToLastDate_);

            Schedule floatSchedule = new Schedule(startDate, endDate,
                                   floatTenor_, floatCalendar_,
                                   floatConvention_, floatTerminationDateConvention_,
                                   floatRule_, floatEndOfMonth_,
                                   floatFirstDate_, floatNextToLastDate_);

            DayCounter fixedDayCount = null;
            if (fixedDayCount_ != null)
               fixedDayCount = fixedDayCount_;
            else 
            {
               if (curr == new USDCurrency())
                  fixedDayCount = new Actual360();
               else if (curr == new EURCurrency() || curr == new CHFCurrency() || curr == new SEKCurrency())
                  fixedDayCount = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
               else if (curr == new GBPCurrency() || curr == new JPYCurrency() || curr == new AUDCurrency() || 
                        curr == new HKDCurrency())
                  fixedDayCount = new Actual365Fixed();
               else
                  Utils.QL_FAIL("unknown fixed leg day counter for " + curr);
            }

            double? usedFixedRate = fixedRate_;
            if (fixedRate_ == null) 
            {       
                VanillaSwap temp = new VanillaSwap(type_, nominal_, fixedSchedule, 0.0, fixedDayCount,
                                                   floatSchedule, iborIndex_, floatSpread_, floatDayCount_);

                if (engine_ == null) 
                {
                   Handle<YieldTermStructure> disc = iborIndex_.forwardingTermStructure();
                   Utils.QL_REQUIRE(!disc.empty(),()=> 
                           "null term structure set to this instance of " + iborIndex_.name());
                   bool includeSettlementDateFlows = false;
                   IPricingEngine engine = new DiscountingSwapEngine(disc, includeSettlementDateFlows);
                   temp.setPricingEngine(engine);
                } 
                else
                   temp.setPricingEngine(engine_);

                usedFixedRate = temp.fairRate();            
            }

            VanillaSwap swap = new VanillaSwap(type_, nominal_, fixedSchedule, usedFixedRate.Value, fixedDayCount,
                                               floatSchedule, iborIndex_, floatSpread_, floatDayCount_);

           if (engine_ == null) 
           {
               Handle<YieldTermStructure> disc = iborIndex_.forwardingTermStructure();
               bool includeSettlementDateFlows = false;
               IPricingEngine engine = new DiscountingSwapEngine(disc, includeSettlementDateFlows);
               swap.setPricingEngine(engine);
           } 
           else
               swap.setPricingEngine(engine_);            
           
           return swap;
        }
    }
}
