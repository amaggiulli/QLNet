/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
    //! fixed-coupon bond helper
    /*! \warning This class assumes that the reference date
                 does not change between calls of setTermStructure().
    */
    public class BondHelper : RelativeDateRateHelper {
        protected Bond bond_;
        protected bool useCleanPrice_;
        public Bond bond() { return bond_; }

        // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
        RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();


        /*! \warning Setting a pricing engine to the passed bond from
                     external code will cause the bootstrap to fail or
                     to give wrong results. It is advised to discard
                     the bond after creating the helper, so that the
                     helper has sole ownership of it.
        */
        public BondHelper(Handle<Quote> price, Bond bond, bool useCleanPrice = true) : base(price) {
            bond_ = bond;

            latestDate_ = bond_.maturityDate();
            initializeDates();

            IPricingEngine bondEngine = new DiscountingBondEngine(termStructureHandle_);
            bond_.setPricingEngine(bondEngine);

            useCleanPrice_ = useCleanPrice;
        }


        //! \name BootstrapHelper interface
        public override void setTermStructure(YieldTermStructure t) {
            // do not set the relinkable handle as an observer - force recalculation when needed
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }

        public override double impliedQuote() {
            if (termStructure_ == null)
                throw new ApplicationException("term structure not set");
            // we didn't register as observers - force calculation
            bond_.recalculate();
            if (useCleanPrice_)
                return bond_.cleanPrice();
            else
                return bond_.dirtyPrice();
        }

        protected override void initializeDates() {
           latestDate_ = bond_.cashflows().Last().date();
           earliestDate_ = bond_.nextCashFlowDate();
        }
    }


    public class FixedRateBondHelper : BondHelper {
        protected FixedRateBond fixedRateBond_;
        public FixedRateBond fixedRateBond() { return fixedRateBond_; }

        //public FixedRateBondHelper(Quote cleanPrice, int settlementDays, double faceAmount, Schedule schedule,
        //                   List<double> coupons, DayCounter dayCounter,
        //                   BusinessDayConvention paymentConv = Following,
        //                   double redemption = 100.0,
        //                   Date issueDate = null);
        public FixedRateBondHelper(Handle<Quote> price, int settlementDays, double faceAmount, Schedule schedule,
                                   List<double> coupons, DayCounter dayCounter, BusinessDayConvention paymentConvention,
                                   double redemption, Date issueDate, Calendar paymentCalendar = null,
			                       Period exCouponPeriod = null, Calendar exCouponCalendar = null,
                                   BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted, bool exCouponEndOfMonth = false,
                                   bool useCleanPrice = true)
            : base(price, new FixedRateBond(settlementDays, faceAmount, schedule,
                                                 coupons, dayCounter, paymentConvention,
                                                 redemption, issueDate, paymentCalendar, 
                                                 exCouponPeriod, exCouponCalendar, exCouponConvention, exCouponEndOfMonth), useCleanPrice)
        {
            fixedRateBond_ = bond_ as FixedRateBond;
        }
    }

    public class CPIBondHelper : BondHelper
    {
        protected CPIBond cpiBond_;
        public CPIBond cpiBond() { return cpiBond_; }

        public CPIBondHelper(Handle<Quote> price, int settlementDays, double faceAmount, bool growthOnly, double baseCPI, 
                                   Period observationLag, ZeroInflationIndex cpiIndex, InterpolationType observationInterpolation, 
                                   Schedule schedule, List<double> fixedRate, DayCounter dayCounter, BusinessDayConvention paymentConvention,
                                   double redemption, Date issueDate, Calendar paymentCalendar = null,
                                   Period exCouponPeriod = null, Calendar exCouponCalendar = null,
                                   BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted, bool exCouponEndOfMonth = false,
                                   bool useCleanPrice = true)
            : base(price, new CPIBond(settlementDays, faceAmount, growthOnly, baseCPI, observationLag, cpiIndex, observationInterpolation, schedule,
                                      fixedRate, dayCounter, paymentConvention, issueDate, paymentCalendar, exCouponPeriod, exCouponCalendar,
                                      exCouponConvention, exCouponEndOfMonth), useCleanPrice)
        {
            cpiBond_ = bond_ as CPIBond;
        }
    }
}
