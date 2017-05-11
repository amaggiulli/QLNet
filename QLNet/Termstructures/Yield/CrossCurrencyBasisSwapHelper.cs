/*
 Copyright (C) 2016 Quaternion Risk Management Ltd All rights reserved. http://opensourcerisk.org
               2017 Adapted in C# for QLNet by Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
  
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

namespace QLNet
{
    //! Cross Ccy Basis Swap Rate Helper
    /*! Rate helper for bootstrapping over cross currency basis swap spreads

        Assumes that you have, at a minimum, either:
        - flatIndex with attached YieldTermStructure and flatDiscountCurve
        - spreadIndex with attached YieldTermStructure and spreadDiscountCurve

        The other leg is then solved for i.e. index curve (if no
        YieldTermStructure is attached to its index) or discount curve (if
        its Handle is empty) or both.

        The currencies are deduced from the ibor indexes. The spotFx
        be be quoted with either of these currencies, this is determined
        by the flatIsDomestic flag. The settlement date of the spot is
        assumed to be equal to the settlement date of the swap itself.

                \ingroup termstructures
    */
   public class CrossCurrencyBasisSwapHelper : RelativeDateRateHelper
   {
       public CrossCurrencyBasisSwapHelper(Handle<Quote> spreadQuote, Handle<Quote> spotFX, 
                            int settlementDays,
                            Period swapTenor,
                            Calendar settlementCalendar, 
                            BusinessDayConvention rollConvention,
                            IborIndex flatIndex,
                            IborIndex spreadIndex,
                            Handle<YieldTermStructure> flatDiscountCurve,
                            Handle<YieldTermStructure> spreadDiscountCurve, 
                            bool eom = false,
                            bool flatIsDomestic = true)
         : base(spreadQuote)
        {
            spotFX_ = spotFX;
            settlementDays_ = settlementDays;
            settlementCalendar_ = settlementCalendar;
            swapTenor_ = swapTenor;
            rollConvention_ = rollConvention;
            flatIndex_ = flatIndex; 
            spreadIndex_ = spreadIndex;
            flatDiscountCurve_ = flatDiscountCurve;
            spreadDiscountCurve_ = spreadDiscountCurve;
            eom_ = eom;
            flatIsDomestic_ = flatIsDomestic;

            flatLegCurrency_ = flatIndex_.currency();
            spreadLegCurrency_ = spreadIndex_.currency();

            bool flatIndexHasCurve = !flatIndex_.forwardingTermStructure().empty();
            bool spreadIndexHasCurve = !spreadIndex_.forwardingTermStructure().empty();
            bool haveFlatDiscountCurve = !flatDiscountCurve_.empty();
            bool haveSpreadDiscountCurve = !spreadDiscountCurve_.empty();

            Utils.QL_REQUIRE(!(flatIndexHasCurve && spreadIndexHasCurve && haveFlatDiscountCurve && haveSpreadDiscountCurve),
                                                                             () => "Have all curves, nothing to solve for.");

            /* Link the curve being bootstrapped to the index if the index has
            no projection curve */
            if (flatIndexHasCurve && haveFlatDiscountCurve) 
            {
                if (!spreadIndexHasCurve) 
                {
                    spreadIndex_ = spreadIndex_.clone(termStructureHandle_);
                    //spreadIndex_.unregisterWith(termStructureHandle_.link.update);
                }
            } 
            else if (spreadIndexHasCurve && haveSpreadDiscountCurve) 
            {
                if (!flatIndexHasCurve) 
                {
                    flatIndex_ = flatIndex_.clone(termStructureHandle_);
                    //flatIndex_.unregisterWith(termStructureHandle_.link.update);
                }
            } 
            else 
            {
                Utils.QL_FAIL("Need one leg of the cross currency basis swap to have all of its curves.");
            }

            spotFX_.registerWith(update);
            flatIndex_.registerWith(update);
            spreadIndex_.registerWith(update);
            flatDiscountCurve_.registerWith(update);
            spreadDiscountCurve_.registerWith(update);

            initializeDates();
        }

        //! \name RateHelper interface
        //@{
        
        public override double impliedQuote() 
        {
            Utils.QL_REQUIRE(termStructure_ != null, () => "Term structure needs to be set");
            swap_.recalculate();
            return Convert.ToDouble(swap_.fairPaySpread());
        }

        //@}
        //! \name inspectors
        //@{
        public CrossCurrencyBasisSwap swap() { return swap_; }
        //@}

        protected override void initializeDates() 
        {
            Date settlementDate = settlementCalendar_.advance(evaluationDate_, settlementDays_, TimeUnit.Days);
            Date maturityDate = settlementDate + swapTenor_;

            Period flatLegTenor = flatIndex_.tenor();
            Schedule flatLegSchedule = new MakeSchedule()
                                        .from(settlementDate)
                                        .to(maturityDate)
                                        .withTenor(flatLegTenor)
                                        .withCalendar(settlementCalendar_)
                                        .withConvention(rollConvention_)
                                        .endOfMonth(eom_)
                                        .value();

            Period spreadLegTenor = spreadIndex_.tenor();
            Schedule spreadLegSchedule = new MakeSchedule()
                                    .from(settlementDate)
                                    .to(maturityDate)
                                    .withTenor(spreadLegTenor)
                                    .withCalendar(settlementCalendar_)
                                    .withConvention(rollConvention_)
                                    .endOfMonth(eom_)
                                    .value();

            double flatLegNominal = 1.0;
            double spreadLegNominal = 1.0;
            if (flatIsDomestic_) {
                flatLegNominal = spotFX_.link.value();
            } else {
                spreadLegNominal = spotFX_.link.value();
            }

            /* Arbitrarily set the spread leg as the pay leg */
            swap_ = new CrossCurrencyBasisSwap(spreadLegNominal, spreadLegCurrency_, spreadLegSchedule, spreadIndex_, 0.0,
                            flatLegNominal, flatLegCurrency_, flatLegSchedule, flatIndex_, 0.0);

            Handle<Quote> spotFX = new Handle<Quote>(spotFX_);
            IPricingEngine engine;
            
            if (flatIsDomestic_) {
                engine = new DiscountingCrossCurrencySwapEngine(flatLegCurrency_, 
                                                                flatDiscountRLH_, 
                                                                spreadLegCurrency_, 
                                                                spreadDiscountRLH_,
                                                                spotFX, false, settlementDate, settlementDate);
                
                engine.reset();
            } else {
                engine = new DiscountingCrossCurrencySwapEngine(spreadLegCurrency_, 
                                                                spreadDiscountRLH_, 
                                                                flatLegCurrency_, 
                                                                flatDiscountRLH_,
                                                                spotFX, false, settlementDate, settlementDate);
                engine.reset();
            }
            swap_.setPricingEngine(engine);

            earliestDate_ = swap_.startDate();
            latestDate_ = swap_.maturityDate();

            /* May need to adjust latestDate_ if you are projecting libor based
            on tenor length rather than from accrual date to accrual date. */
            //#if QL_USE_INDEXED_COUPON
            if (termStructureHandle_ == spreadIndex_.forwardingTermStructure()) {
                int numCashflows = swap_.leg(0).Count;
                if (numCashflows > 2) {
                    FloatingRateCoupon lastFloating = swap_.leg(0)[numCashflows - 1] as FloatingRateCoupon;
                    Date fixingValueDate = spreadIndex_.valueDate(lastFloating.fixingDate());
                    Date endValueDate = spreadIndex_.maturityDate(fixingValueDate);
                    latestDate_ = Date.Max(latestDate_, endValueDate);
                }
            }

            if (termStructureHandle_ == flatIndex_.forwardingTermStructure()) {
                int numCashflows = swap_.leg(1).Count;
                if (numCashflows > 2) {
                    FloatingRateCoupon lastFloating = swap_.leg(1)[numCashflows - 1] as FloatingRateCoupon;
                    Date fixingValueDate = flatIndex_.valueDate(lastFloating.fixingDate());
                    Date endValueDate = flatIndex_.maturityDate(fixingValueDate);
                    latestDate_ = Date.Max(latestDate_, endValueDate);
                }
            }
            //#endif
        }

        public override void setTermStructure(YieldTermStructure t) 
        {
            bool observer = false;
            YieldTermStructure temp = t;

            termStructureHandle_.linkTo(temp, observer);

            if (flatDiscountCurve_.empty())
                flatDiscountRLH_.linkTo(temp, observer);
            else
                flatDiscountRLH_.linkTo(flatDiscountCurve_, observer);

            if (spreadDiscountCurve_.empty())
                spreadDiscountRLH_.linkTo(temp, observer);
            else
                spreadDiscountRLH_.linkTo(spreadDiscountCurve_, observer);

            base.setTermStructure(t);
        }

        protected Handle<Quote> spotFX_;
        protected int settlementDays_;
        protected Calendar settlementCalendar_;
        protected Period swapTenor_;
        protected BusinessDayConvention rollConvention_;
        protected IborIndex flatIndex_;
        protected IborIndex spreadIndex_;
        protected Handle<YieldTermStructure> flatDiscountCurve_;
        protected Handle<YieldTermStructure> spreadDiscountCurve_;
        protected bool eom_, flatIsDomestic_;

        protected Currency flatLegCurrency_;
        protected Currency spreadLegCurrency_;
        protected CrossCurrencyBasisSwap swap_;

        protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();
        protected RelinkableHandle<YieldTermStructure> flatDiscountRLH_ = new RelinkableHandle<YieldTermStructure>();
        protected RelinkableHandle<YieldTermStructure> spreadDiscountRLH_ = new RelinkableHandle<YieldTermStructure>();
   }
}
