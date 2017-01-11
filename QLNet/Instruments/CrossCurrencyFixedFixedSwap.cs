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

/*! \file crossccyswap.cs
    \brief Swap instrument with legs involving two currencies

        \ingroup instruments
*/

using System;
using System.Collections.Generic;

namespace QLNet
{
    //! Cross currency Fixed - Fixed swap
    /*! The first leg holds the pay currency cashflows and second leg holds
        the receive currency cashflows.

            \ingroup instruments
    */
    public class CrossCurrencyFixedFixedSwap : CrossCurrencySwap
    {
        private double payFixedRate_;
        private double recFixedRate_;
        private double payNominal_;
        private double recNominal_;
        private Currency payCurrency_;
        private Currency recCurrency_;

        private Schedule payFixedSchedule_;
        public Schedule PayFixedSchedule() { return payFixedSchedule_; }

        private DayCounter payFixedDayCount_;
        public DayCounter PayFixedDayCount() { return payFixedDayCount_; }

        private Schedule recFixedSchedule_;
        public Schedule RecFixedSchedule() { return recFixedSchedule_; }

        private DayCounter recFixedDayCount_;
        public DayCounter RecFixedDayCount() { return recFixedDayCount_; }

        private BusinessDayConvention payPaymentConvention_;
        private BusinessDayConvention recPaymentConvention_;

        // results
        private double? payFairRate_;
        private double? recFairRate_;

        //! \name Constructors
        //@{
        //! First leg is paid and the second is received.
        public CrossCurrencyFixedFixedSwap(double payNominal, 
                                        Currency payCurrency,
                                        Schedule paySchedule, 
                                        double payFixedRate,
                                        DayCounter payDayCount,
                                        double recNominal,
                                        Currency recCurrency, 
                                        Schedule recSchedule, 
                                        double recFixedRate,
                                        DayCounter recDayCount,
                                        BusinessDayConvention? payPaymentConvention = null,
                                        BusinessDayConvention? recPaymentConvention = null)
            : base(2)
        {
            payNominal_ = payNominal;
            payCurrency_  = payCurrency;
            payFixedRate_ = payFixedRate;
            payFixedSchedule_ = paySchedule;
            payFixedDayCount_ = payDayCount;

            if (payPaymentConvention.HasValue)
                payPaymentConvention_ = payPaymentConvention.Value;
            else
                payPaymentConvention = BusinessDayConvention.Unadjusted;

            recNominal_ = recNominal;
            recCurrency_ = recCurrency;
            recFixedRate_ = recFixedRate;
            recFixedSchedule_ = recSchedule;
            recFixedDayCount_ = recDayCount;

            if (recPaymentConvention.HasValue)
                recPaymentConvention_ = recPaymentConvention.Value;
            else
                recPaymentConvention_ = BusinessDayConvention.Unadjusted;

            initialize();
        }

        private void initialize() 
        {
            // Pay leg
            legs_[0] = new FixedRateLeg(payFixedSchedule_)
                                     .withCouponRates(payFixedRate_, payFixedDayCount_)
                                     .withPaymentAdjustment(payPaymentConvention_)
                                     .withNotionals(payNominal_);

            payer_[0] = -1.0;
            currencies_[0] = payCurrency_;

            // Pay leg notional exchange at start.
            Date initialPayDate = legs_[0][0].date();
            CashFlow initialPayCF = new SimpleCashFlow(-payNominal_, initialPayDate);
            legs_[0].Insert(0, initialPayCF);

            // Pay leg notional exchange at end.
            Date finalPayDate = legs_[0][legs_[0].Count - 1].date();
            CashFlow finalPayCF = new SimpleCashFlow(payNominal_, finalPayDate);
            legs_[0].Insert(legs_[0].Count - 1, finalPayCF);

            // Receive leg
            legs_[1] = new FixedRateLeg(recFixedSchedule_)
                                     .withCouponRates(recFixedRate_, recFixedDayCount_)
                                     .withPaymentAdjustment(recPaymentConvention_)
                                     .withNotionals(recNominal_);

            payer_[1] = +1.0;
            currencies_[1] = recCurrency_;

            // Receive leg notional exchange at start.
            Date initialRecDate = legs_[1][0].date();
            CashFlow initialRecCF = new SimpleCashFlow(-recNominal_, initialRecDate);
            legs_[1].Insert(0, initialRecCF);

            // Receive leg notional exchange at end.
            Date finalRecDate = legs_[1][legs_[1].Count - 1].date();
            CashFlow finalRecCF  = new SimpleCashFlow(recNominal_, finalRecDate);
            legs_[1].Insert(legs_[1].Count - 1, finalRecCF);

            // Register the instrument with all cashflows on each leg.
            for (int legNo = 0; legNo < 2; legNo++) {
                foreach(CashFlow cf in legs_[legNo])
                {
                    cf.registerWith(update);
                }
            }
        }

        //@}
        //! \name Instrument interface
        //@{
        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            CrossCurrencyFixedFixedSwap.Arguments arguments = args as CrossCurrencyFixedFixedSwap.Arguments;

            /* Returns here if e.g. args is CrossCcySwap::arguments which
               is the case if PricingEngine is a CrossCcySwap::engine. */
            if (arguments == null)
                return;

            arguments.payNominal = payNominal_;
            arguments.recNominal = recNominal_;

            List<CashFlow> payFixedCoupons = payLeg();
            List<CashFlow> recFixedCoupons = recLeg();

            arguments.payFixedResetDates = new InitializedList<Date>(payFixedCoupons.Count);
            arguments.payFixedPayDates = new InitializedList<Date>(payFixedCoupons.Count);
            arguments.payFixedCoupons = new InitializedList<double>(payFixedCoupons.Count);

            for (int i = 0; i < payFixedCoupons.Count; ++i)
            {
                FixedRateCoupon coupon = (FixedRateCoupon)payFixedCoupons[i];

                arguments.payFixedPayDates[i] = coupon.date();
                arguments.payFixedResetDates[i] = coupon.accrualStartDate();
                arguments.payFixedCoupons[i] = coupon.amount();
            }

            arguments.recFixedResetDates = new InitializedList<Date>(recFixedCoupons.Count);
            arguments.recFixedPayDates = new InitializedList<Date>(recFixedCoupons.Count);
            arguments.recFixedCoupons = new InitializedList<double>(recFixedCoupons.Count);

            for (int i = 0; i < recFixedCoupons.Count; ++i)
            {
                FixedRateCoupon coupon = (FixedRateCoupon)recFixedCoupons[i];

                arguments.recFixedPayDates[i] = coupon.date();
                arguments.recFixedResetDates[i] = coupon.accrualStartDate();
                arguments.recFixedCoupons[i] = coupon.amount();
            }
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            const double basisPoint = 1.0e-4;
            base.fetchResults(r);

            CrossCurrencyFixedFixedSwap.Results results = r as CrossCurrencyFixedFixedSwap.Results;
            
            if (results != null) 
            {
                /* If PricingEngine::results are of type
                   CrossCcyBasisSwap::results */
                payFairRate_ = results.payFairRate;
            } 
            else 
            {
                /* If not, e.g. if the engine is a CrossCcySwap::engine */
                payFairRate_ = null;
            }
            if (payFairRate_ == null)
            {
                // calculate it from other results
                if (legBPS_[0] != null)
                    payFairRate_ = payFixedRate_ - NPV_.GetValueOrDefault() / (legBPS_[0] / basisPoint);
            }
            if (recFairRate_ == null)
            {
                // calculate it from other results
                if (legBPS_[0] != null)
                    recFairRate_ = recFixedRate_ - NPV_.GetValueOrDefault() / (legBPS_[1] / basisPoint);
            }
        }

        //@}
        //! \name Inspectors
        //@{
        double payNominal() { return payNominal_; }
        Currency payCurrency() { return payCurrency_; }

        double recNominal() { return recNominal_; }
        Currency recCurrency() { return recCurrency_; }

        ///////////////////////////////////////////////////
        // results
        public double payFairRate()
        {
            calculate();
            if (payFairRate_ == null) throw new ArgumentException("result not available");
            return payFairRate_.GetValueOrDefault();
        }

        public double recFairRate()
        {
            calculate();
            if (recFairRate_ == null) throw new ArgumentException("result not available");
            return recFairRate_.GetValueOrDefault();
        }

        public double payLegBPS()
        {
            calculate();
            if (legBPS_[0] == null) throw new ArgumentException("result not available");
            return legBPS_[0].GetValueOrDefault();
        }

        public double payLegNPV()
        {
            calculate();
            if (legNPV_[0] == null) throw new ArgumentException("result not available");
            return legNPV_[0].GetValueOrDefault();
        }

        public double recLegBPS()
        {
            calculate();
            if (legBPS_[1] == null) throw new ArgumentException("result not available");
            return legBPS_[1].GetValueOrDefault();
        }

        public double recLegNPV()
        {
            calculate();
            if (legNPV_[1] == null) throw new ArgumentException("result not available");
            return legNPV_[1].GetValueOrDefault();
        }

        public double payFixedRate { get { return payFixedRate_; } }
        public double recFixedRate { get { return recFixedRate_; } }
        public List<CashFlow> payLeg() { return legs_[0]; }
        public List<CashFlow> recLeg() { return legs_[1]; }
        //@}

        //@}
        //! \name Instrument interface
        //@{
        protected override void setupExpired()
        {
            base.setupExpired();
            legBPS_[0] = legBPS_[1] = 0.0;
            payFairRate_ = recFairRate_ = null;
        }
        //@}

        new public class Arguments : CrossCurrencySwap.Arguments
        {
            public double payNominal;
            public double recNominal;

            public List<Date> payFixedResetDates;
            public List<Date> payFixedPayDates;
            public List<Date> recFixedResetDates;
            public List<Date> recFixedPayDates;

            public List<double> payFixedCoupons;
            public List<double> recFixedCoupons;

            public Arguments()
            {
                payNominal = default(double);
                recNominal = default(double);
            }

            public override void validate()
            {
                base.validate();

                if (payNominal == default(double)) throw new ArgumentException("Pay nominal null or not set");
                if (recNominal == default(double)) throw new ArgumentException("Rec nominal null or not set");
                if (payFixedResetDates.Count != payFixedPayDates.Count)
                    throw new ArgumentException("number of paying fixed start dates different from number of fixed payment dates");
                if (payFixedPayDates.Count != payFixedCoupons.Count)
                    throw new ArgumentException("number of paying fixed payment dates different from number of fixed coupon amounts");
                if (recFixedResetDates.Count != recFixedPayDates.Count)
                    throw new ArgumentException("number of receiving fixed start dates different from number of fixed payment dates");
                if (recFixedPayDates.Count != recFixedCoupons.Count)
                    throw new ArgumentException("number of receiving fixed payment dates different from number of fixed coupon amounts");
            }
        }

        new public class Results : CrossCurrencySwap.Results
        {
            public double? payFairRate;
            public double? recFairRate;

            public override void reset()
            {
                base.reset();
                payFairRate = null;
                recFairRate = null;
            }
        }
    }
}

