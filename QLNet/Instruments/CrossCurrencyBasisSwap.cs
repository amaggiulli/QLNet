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
    public class CrossCurrencyBasisSwap : CrossCurrencySwap
    {
        private double payNominal_;
        private Currency payCurrency_;
        private Schedule paySchedule_;
        private IborIndex payIndex_;
        private double paySpread_;

        private double recNominal_;
        private Currency recCurrency_;
        private Schedule recSchedule_;
        private IborIndex recIndex_;
        private double recSpread_;

        private double? fairPaySpread_;
        private double? fairRecSpread_;

        //! \name Constructors
        //@{
        //! First leg is paid and the second is received.
        public CrossCurrencyBasisSwap(double payNominal, 
                                        Currency payCurrency, 
                                        Schedule paySchedule,
                                        IborIndex payIndex, 
                                        double paySpread, 
                                        double recNominal,
                                        Currency recCurrency, 
                                        Schedule recSchedule,
                                        IborIndex recIndex, 
                                        double recSpread)
            : base(2)
        {
            payNominal_ = payNominal;
            payCurrency_  = payCurrency;
            paySchedule_  = paySchedule;
            payIndex_  = payIndex;
            paySpread_  = paySpread;
            recNominal_ = recNominal;
            recCurrency_ = recCurrency;
            recSchedule_ = recSchedule; 
            recIndex_ = recIndex;
            recSpread_ = recSpread;
            payIndex.registerWith(update);
            recIndex.registerWith(update);
            initialize();
        }

        private void initialize() 
        {
            // Pay leg
            legs_[0] = new IborLeg(paySchedule_, payIndex_)
                        .withSpreads(paySpread_)
                        .withNotionals(payNominal_);

            payer_[0] = -1.0;
            currencies_[0] = payCurrency_;

            // Pay leg notional exchange at start.
            Date initialPayDate = paySchedule_.dates()[0];
            CashFlow initialPayCF = new SimpleCashFlow(-payNominal_, initialPayDate);
            legs_[0].Insert(0, initialPayCF);

            // Pay leg notional exchange at end.
            Date finalPayDate = paySchedule_.dates()[paySchedule_.dates().Count - 1];
            CashFlow finalPayCF = new SimpleCashFlow(payNominal_, finalPayDate);
            legs_[0].Insert(legs_[0].Count - 1, finalPayCF);

            // Receive leg
            legs_[1] = new IborLeg(recSchedule_, recIndex_)
                            .withSpreads(recSpread_)
                            .withNotionals(recNominal_);

            payer_[1] = +1.0;
            currencies_[1] = recCurrency_;

            // Receive leg notional exchange at start.
            Date initialRecDate = recSchedule_.dates()[0];
            CashFlow initialRecCF = new SimpleCashFlow(-recNominal_, initialRecDate);
            legs_[1].Insert(0, initialRecCF);

            // Receive leg notional exchange at end.
            Date finalRecDate = recSchedule_.dates()[recSchedule_.dates().Count - 1];
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

            CrossCurrencyBasisSwap.Arguments arguments = args as CrossCurrencyBasisSwap.Arguments;

            /* Returns here if e.g. args is CrossCcySwap::arguments which
               is the case if PricingEngine is a CrossCcySwap::engine. */
            if (arguments == null)
                return;

            arguments.paySpread = paySpread_;
            arguments.recSpread = recSpread_;
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);

            CrossCurrencyBasisSwap.Results results = r as CrossCurrencyBasisSwap.Results;
            
            if (results != null) 
            {
                /* If PricingEngine::results are of type
                   CrossCcyBasisSwap::results */
                fairPaySpread_ = results.fairPaySpread;
                fairRecSpread_ = results.fairRecSpread;
            } 
            else 
            {
                /* If not, e.g. if the engine is a CrossCcySwap::engine */
                fairPaySpread_ = null;
                fairRecSpread_ = null;
            }

            /* Calculate the fair pay and receive spreads if they are null */
            double basisPoint = 1.0e-4;
            if (fairPaySpread_ == null) {
                if (legBPS_[0] != null)
                    fairPaySpread_ = paySpread_ - NPV_ / (legBPS_[0] / basisPoint);
            }
            if (fairRecSpread_ == null) {
                if (legBPS_[1] != null)
                    fairRecSpread_ = recSpread_ - NPV_ / (legBPS_[1] / basisPoint);
            }
        }

        //@}
        //! \name Inspectors
        //@{
        double payNominal() { return payNominal_; }
        Currency payCurrency() { return payCurrency_; }
        Schedule paySchedule() { return paySchedule_; }
        IborIndex payIndex() { return payIndex_; }
        double paySpread() { return paySpread_; }

        double recNominal() { return recNominal_; }
        Currency recCurrency() { return recCurrency_; }
        Schedule recSchedule() { return recSchedule_; }
        IborIndex recIndex() { return recIndex_; }
        double recSpread() { return recSpread_; }
        //@}

        //! \name Additional interface
        //@{
        public double? fairPaySpread() 
        {
            calculate();
            Utils.QL_REQUIRE(fairPaySpread_ != null, () => "Fair pay spread is not available");
            return fairPaySpread_;
        }

        public double? fairRecSpread() 
        {
            calculate();
            Utils.QL_REQUIRE(fairRecSpread_ != null, () => "Fair pay spread is not available");
            return fairRecSpread_;
        }
        //@}

        //@}
        //! \name Instrument interface
        //@{
        protected override void setupExpired()
        {
            base.setupExpired();
            fairPaySpread_ = null;
            fairRecSpread_ = null;
        }
        //@}

        new public class Arguments : CrossCurrencySwap.Arguments
        {
            public double? paySpread;
            public double? recSpread;

            public override void validate()
            {
                base.validate();
                Utils.QL_REQUIRE(paySpread != null, () => "Pay spread cannot be null");
                Utils.QL_REQUIRE(recSpread != null, () => "Rec spread cannot be null");
            }
        }

        new public class Results : CrossCurrencySwap.Results
        {
            public double? fairPaySpread;
            public double? fairRecSpread;

            public override void reset()
            {
                base.reset();
                fairPaySpread = null;
                fairRecSpread = null;
            }
        }
    }
}

