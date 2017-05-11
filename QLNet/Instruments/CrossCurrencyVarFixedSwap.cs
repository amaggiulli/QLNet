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
    //! Cross currency basis swap
    /*! The first leg holds the pay currency cashflows and second leg holds
        the receive currency cashflows.

            \ingroup instruments
    */
    public class CrossCurrencyVarFixedSwap : CrossCurrencySwap
    {
        public enum Type { Receiver = -1, Payer = 1 };

        private Type type_;
        private double fixedRate_;
        private double spread_;
        private double fixedNominal_;
        private double floatNominal_;

        private Schedule fixedSchedule_;
        public Schedule fixedSchedule() { return fixedSchedule_; }
        private Currency fixedCurrency_;

        private DayCounter fixedDayCount_;
        public DayCounter fixedDayCount() { return fixedDayCount_; }

        private Schedule floatingSchedule_;
        public Schedule floatingSchedule() { return floatingSchedule_; }
        private Currency floatingCurrency_;

        private IborIndex iborIndex_;
        private DayCounter floatingDayCount_;
        public DayCounter floatingDayCount() { return floatingDayCount_; }
        private BusinessDayConvention paymentConvention_;

        // results
        private double? fairRate_;
        private double? fairSpread_;

        //! \name Constructors
        //@{
        //! First leg is paid and the second is received.
        public CrossCurrencyVarFixedSwap(Type type, double fixedNominal,
                         Schedule fixedSchedule, double fixedRate, DayCounter fixedDayCount, Currency fixedCurrency, double floatNominal,
                         Schedule floatSchedule, IborIndex iborIndex, double spread, DayCounter floatingDayCount, Currency floatingCurrency,
                         BusinessDayConvention? paymentConvention = null) :
            base(2)
        {

            if (floatingCurrency != iborIndex.currency()) throw new ArgumentException("Floating Index have not the same currency than Floating Currency");

            type_ = type;
            fixedNominal_ = fixedNominal;
            floatNominal_ = floatNominal;
            fixedSchedule_ = fixedSchedule;
            fixedRate_ = fixedRate;
            fixedDayCount_ = fixedDayCount;
            floatingSchedule_ = floatSchedule;
            iborIndex_ = iborIndex;
            spread_ = spread;
            floatingDayCount_ = floatingDayCount;
            fixedCurrency_ = fixedCurrency;
            floatingCurrency_ = floatingCurrency;

            if (paymentConvention.HasValue)
                paymentConvention_ = paymentConvention.Value;
            else
                paymentConvention_ = floatingSchedule_.businessDayConvention();

            legs_[0] = new FixedRateLeg(fixedSchedule)
                                        .withCouponRates(fixedRate, fixedDayCount)
                                        .withPaymentAdjustment(paymentConvention_)
                                        .withNotionals(fixedNominal);

            legs_[1] = new IborLeg(floatSchedule, iborIndex)
                                        .withPaymentDayCounter(floatingDayCount)
                //.withFixingDays(iborIndex.fixingDays())
                                        .withSpreads(spread)
                                        .withNotionals(floatNominal)
                                        .withPaymentAdjustment(paymentConvention_);

            foreach (var cf in legs_[1])
                cf.registerWith(update);

            switch (type_)
            {
                case Type.Payer:
                    payer_[0] = -1.0;
                    payer_[1] = +1.0;
                    break;
                case Type.Receiver:
                    payer_[0] = +1.0;
                    payer_[1] = -1.0;
                    break;
                default:
                    throw new Exception("Unknown vanilla-swap type");
            }

            initialize();
        }

        private void initialize() 
        {
            // Fixed leg
            currencies_[0] = fixedCurrency_;

            // Fixed leg notional exchange at start.
            Date initialPayDate = legs_[0][0].date();
            CashFlow initialPayCF = new SimpleCashFlow(-payer_[0] * fixedNominal_, initialPayDate);
            legs_[0].Insert(0, initialPayCF);

            // Fixed leg notional exchange at end.
            Date finalPayDate = legs_[0][legs_[0].Count - 1].date();
            CashFlow finalPayCF = new SimpleCashFlow(payer_[0] * fixedNominal_, finalPayDate);
            legs_[0].Insert(legs_[0].Count - 1, finalPayCF);

            // Float leg
            currencies_[1] = floatingCurrency_;

            // Float leg notional exchange at start.
            Date initialRecDate = legs_[1][0].date();
            CashFlow initialRecCF = new SimpleCashFlow(-payer_[0] * floatNominal_, initialRecDate);
            legs_[1].Insert(0, initialRecCF);

            // Float leg notional exchange at end.
            Date finalRecDate = legs_[1][legs_[1].Count - 1].date();
            CashFlow finalRecCF = new SimpleCashFlow(payer_[0] * floatNominal_, finalRecDate);
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

            CrossCurrencyVarFixedSwap.Arguments arguments = args as CrossCurrencyVarFixedSwap.Arguments;

            /* Returns here if e.g. args is CrossCcySwap::arguments which
               is the case if PricingEngine is a CrossCcySwap::engine. */
            if (arguments == null)
                return;

            arguments.type = type_;
            arguments.fixedNominal = fixedNominal_;
            arguments.floatNominal = floatNominal_;

            List<CashFlow> fixedCoupons = fixedLeg();

            arguments.fixedResetDates = new InitializedList<Date>(fixedCoupons.Count);
            arguments.fixedPayDates = new InitializedList<Date>(fixedCoupons.Count);
            arguments.fixedCoupons = new InitializedList<double>(fixedCoupons.Count);

            for (int i = 0; i < fixedCoupons.Count; ++i)
            {
                FixedRateCoupon coupon = (FixedRateCoupon)fixedCoupons[i];

                arguments.fixedPayDates[i] = coupon.date();
                arguments.fixedResetDates[i] = coupon.accrualStartDate();
                arguments.fixedCoupons[i] = coupon.amount();
            }

            List<CashFlow> floatingCoupons = floatingLeg();

            arguments.floatingResetDates = new InitializedList<Date>(floatingCoupons.Count);
            arguments.floatingPayDates = new InitializedList<Date>(floatingCoupons.Count);
            arguments.floatingFixingDates = new InitializedList<Date>(floatingCoupons.Count);
            arguments.floatingAccrualTimes = new InitializedList<double>(floatingCoupons.Count);
            arguments.floatingSpreads = new InitializedList<double>(floatingCoupons.Count);
            arguments.floatingCoupons = new InitializedList<double>(floatingCoupons.Count);
            for (int i = 0; i < floatingCoupons.Count; ++i)
            {
                IborCoupon coupon = (IborCoupon)floatingCoupons[i];

                arguments.floatingResetDates[i] = coupon.accrualStartDate();
                arguments.floatingPayDates[i] = coupon.date();

                arguments.floatingFixingDates[i] = coupon.fixingDate();
                arguments.floatingAccrualTimes[i] = coupon.accrualPeriod();
                arguments.floatingSpreads[i] = coupon.spread();
                try
                {
                    arguments.floatingCoupons[i] = coupon.amount();
                }
                catch
                {
                    arguments.floatingCoupons[i] = default(double);
                }
            }
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            const double basisPoint = 1.0e-4;
            base.fetchResults(r);

            CrossCurrencyVarFixedSwap.Results results = r as CrossCurrencyVarFixedSwap.Results;

            if (results != null)
            { // might be a swap engine, so no error is thrown
                fairRate_ = results.fairRate;
                fairSpread_ = results.fairSpread;
            }
            else
            {
                fairRate_ = null;
                fairSpread_ = null;
            }

            if (fairRate_ == null)
            {
                // calculate it from other results
                if (legBPS_[0] != null)
                    fairRate_ = fixedRate_ - NPV_.GetValueOrDefault() / (legBPS_[0] / basisPoint);
            }
            if (fairSpread_ == null)
            {
                // ditto
                if (legBPS_[1] != null)
                    fairSpread_ = spread_ - NPV_.GetValueOrDefault() / (legBPS_[1] / basisPoint);
            }
        }

        //@}
        //! \name Inspectors
        //@{
        ///////////////////////////////////////////////////
        // results
        public double fairRate()
        {
            calculate();
            if (fairRate_ == null) throw new ArgumentException("result not available");
            return fairRate_.GetValueOrDefault();
        }

        public double fairSpread()
        {
            calculate();
            if (fairSpread_ == null) throw new ArgumentException("result not available");
            return fairSpread_.GetValueOrDefault();
        }

        public double fixedLegBPS()
        {
            calculate();
            if (legBPS_[0] == null) throw new ArgumentException("result not available");
            return legBPS_[0].GetValueOrDefault();
        }
        public double fixedLegNPV()
        {
            calculate();
            if (legNPV_[0] == null) throw new ArgumentException("result not available");
            return legNPV_[0].GetValueOrDefault();
        }

        public double floatingLegBPS()
        {
            calculate();
            if (legBPS_[1] == null) throw new ArgumentException("result not available");
            return legBPS_[1].GetValueOrDefault();
        }
        public double floatingLegNPV()
        {
            calculate();
            if (legNPV_[1] == null) throw new ArgumentException("result not available");
            return legNPV_[1].GetValueOrDefault();
        }

        public IborIndex iborIndex()
        {
            return iborIndex_;
        }

        public double fixedRate { get { return fixedRate_; } }
        public double spread { get { return spread_; } }
        public double fixedNominal { get { return fixedNominal_; } }
        public double floatNominal { get { return floatNominal_; } }
        public Type swapType { get { return type_; } }
        public List<CashFlow> fixedLeg() { return legs_[0]; }
        public List<CashFlow> floatingLeg() { return legs_[1]; }
        //@}

        //@}
        //! \name Instrument interface
        //@{
        protected override void setupExpired()
        {
            base.setupExpired();
            legBPS_[0] = legBPS_[1] = 0.0;
            fairRate_ = fairSpread_ = null;
        }
        //@}

        new public class Arguments : CrossCurrencySwap.Arguments
        {
            public Type type;
            public double fixedNominal;
            public double floatNominal;

            public List<Date> fixedResetDates;
            public List<Date> fixedPayDates;
            public List<double> floatingAccrualTimes;
            public List<Date> floatingResetDates;
            public List<Date> floatingFixingDates;
            public List<Date> floatingPayDates;

            public List<double> fixedCoupons;
            public List<double> floatingSpreads;
            public List<double> floatingCoupons;

            public Arguments()
            {
                type = Type.Receiver;
                fixedNominal = floatNominal = default(double);
            }

            public override void validate()
            {
                base.validate();

                if (fixedNominal == default(double)) throw new ArgumentException("fixed nominal null or not set");
                if (floatNominal == default(double)) throw new ArgumentException("float nominal null or not set");
                if (fixedResetDates.Count != fixedPayDates.Count)
                    throw new ArgumentException("number of fixed start dates different from number of fixed payment dates");
                if (fixedPayDates.Count != fixedCoupons.Count)
                    throw new ArgumentException("number of fixed payment dates different from number of fixed coupon amounts");
                if (floatingResetDates.Count != floatingPayDates.Count)
                    throw new ArgumentException("number of floating start dates different from number of floating payment dates");
                if (floatingFixingDates.Count != floatingPayDates.Count)
                    throw new ArgumentException("number of floating fixing dates different from number of floating payment dates");
                if (floatingAccrualTimes.Count != floatingPayDates.Count)
                    throw new ArgumentException("number of floating accrual Times different from number of floating payment dates");
                if (floatingSpreads.Count != floatingPayDates.Count)
                    throw new ArgumentException("number of floating spreads different from number of floating payment dates");
                if (floatingPayDates.Count != floatingCoupons.Count)
                    throw new ArgumentException("number of floating payment dates different from number of floating coupon amounts");
            }
        }

        new public class Results : CrossCurrencySwap.Results
        {
            public double? fairRate, fairSpread;

            public override void reset()
            {
                base.reset();
                fairRate = fairSpread = null;
            }
        }
    }
}

