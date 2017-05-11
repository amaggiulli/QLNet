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
using System.Collections.Generic;

namespace QLNet {
    //! Cross currency swap engine

    /*! This class implements an engine for pricing swaps comprising legs that
        invovlve two currencies. The npv is expressed in ccy1. The given currencies
        ccy1 and ccy2 are matched to the correct swap legs. The evaluation date is the
        reference date of either discounting curve (which must be equal).

                \ingroup engines
    */
    public class DiscountingCrossCurrencySwapEngine : CrossCurrencySwap.Engine
    {
        
        private Currency ccy1_;
        private Handle<YieldTermStructure> currency1Discountcurve_;
        private Currency ccy2_;
        private Handle<YieldTermStructure> currency2Discountcurve_;
        private Handle<Quote> spotFX_;
        private bool includeSettlementDateFlows_;
        private Date settlementDate_;
        private Date npvDate_;

        //! \name Constructors
        //@{
        /*! \param ccy1
                   Currency 1
            \param currency1DiscountCurve
                   Discount curve for cash flows in currency 1
            \param ccy2
                   Currency 2
            \param currency2DiscountCurve
                   Discount curve for cash flows in currency 2
            \param spotFX
                   The market spot rate quote, given as units of ccy1
                   for one unit of cc2. The spot rate must be given
                   w.r.t. a settlement equal to the npv date.
            \param includeSettlementDateFlows, settlementDate
                   If includeSettlementDateFlows is true (false), cashflows
                   on the settlementDate are (not) included in the NPV.
                   If not given the settlement date is set to the
                   npv date.
            \param npvDate
                   Discount to this date. If not given the npv date
                   is set to the evaluation date
        */
        public DiscountingCrossCurrencySwapEngine(Currency ccy1, 
                                            Handle<YieldTermStructure> currency1DiscountCurve,
                                            Currency ccy2, 
                                            Handle<YieldTermStructure> currency2DiscountCurve,
                                            Handle<Quote> spotFX, 
                                            bool includeSettlementDateFlows = false,
                                            Date settlementDate = null, 
                                            Date npvDate = null)
        {
            ccy1_ = ccy1;
            currency1Discountcurve_ = currency1DiscountCurve;
            ccy2_ = ccy2;
            currency2Discountcurve_ = currency2DiscountCurve;
            spotFX_ = spotFX;
            includeSettlementDateFlows_ = includeSettlementDateFlows;
            settlementDate_ = settlementDate;
            npvDate_ = npvDate;

            currency1Discountcurve_.registerWith(update);
            currency2Discountcurve_.registerWith(update);
            spotFX_.registerWith(update);
        }
        //@}

        //! \name PricingEngine interface
        //@{
        public override void calculate() 
        {
            Utils.QL_REQUIRE(!currency1Discountcurve_.empty() && !currency2Discountcurve_.empty(), () =>
                                                                "Discounting term structure handle is empty.");

            Utils.QL_REQUIRE(!spotFX_.empty(), () => "FX spot quote handle is empty.");

            Utils.QL_REQUIRE(currency1Discountcurve_.link.referenceDate() == currency2Discountcurve_.link.referenceDate(), () =>
                                                                "Term structures should have the same reference date.");

            Date referenceDate = currency1Discountcurve_.link.referenceDate();
            Date settlementDate = settlementDate_;
            if (settlementDate_ == null) {
                settlementDate = referenceDate;
            } else {
                Utils.QL_REQUIRE(settlementDate >= referenceDate, () =>          "Settlement date (" + settlementDate
                                                                                 + ") cannot be before discount curve "
                                                                                 + "reference date (" + referenceDate + ")");
            }

            int numLegs = arguments_.legs.Count;
            // - Instrument::Results
            if (npvDate_ == null) {
                results_.valuationDate = referenceDate;
            } else {
                Utils.QL_REQUIRE(npvDate_ >= referenceDate, () => "NPV date (" + npvDate_ + ") cannot be before "
                                                                + "discount curve reference date ("
                                                                + referenceDate + ")");
                results_.valuationDate = npvDate_;
            }
            results_.value = 0.0;
            results_.errorEstimate = null;

            // - Swap::Results
            results_.legNPV = new InitializedList<double?>(numLegs);
            results_.legBPS = new InitializedList<double?>(numLegs);
            results_.startDiscounts = new InitializedList<double?>(numLegs);
            results_.endDiscounts = new InitializedList<double?>(numLegs);
            // - CrossCcySwap::Results
            results_.inCcyLegNPV = new InitializedList<double?>(numLegs);
            results_.inCcyLegBPS = new InitializedList<double?>(numLegs);
            results_.npvDateDiscounts = new InitializedList<double>(numLegs);

            bool includeReferenceDateFlows =
                includeSettlementDateFlows_ ? includeSettlementDateFlows_ : Settings.includeReferenceDateEvents;

            for (int legNo = 0; legNo < numLegs; legNo++)
            {
                try
                {
                    // Choose the correct discount curve for the leg.
                    Handle<YieldTermStructure> legDiscountCurve;
                    if (arguments_.currencies[legNo] == ccy1_)
                    {
                        legDiscountCurve = currency1Discountcurve_;
                    }
                    else
                    {
                        Utils.QL_REQUIRE(arguments_.currencies[legNo] == ccy2_, () => "leg ccy (" + arguments_.currencies[legNo]
                                                                                      + ") must be ccy1 (" + ccy1_
                                                                                      + ") or ccy2 (" + ccy2_ + ")");
                        legDiscountCurve = currency2Discountcurve_;
                    }
                    results_.npvDateDiscounts[legNo] = legDiscountCurve.link.discount(results_.valuationDate);
                    double npv = 0.0, bps = 0.0;
                    // Calculate the NPV and BPS of each leg in its currency.
                    CashFlows.npvbps(arguments_.legs[legNo], legDiscountCurve, includeReferenceDateFlows, settlementDate,
                                      results_.valuationDate, out npv, out bps);
                    results_.inCcyLegNPV[legNo] = npv * arguments_.payer[legNo];
                    results_.inCcyLegBPS[legNo] = bps * arguments_.payer[legNo];

                    results_.legNPV[legNo] = results_.inCcyLegNPV[legNo];
                    results_.legBPS[legNo] = results_.inCcyLegBPS[legNo];

                    // Convert to NPV currency if necessary.
                    if (arguments_.currencies[legNo] != ccy1_)
                    {
                        results_.legNPV[legNo] *= spotFX_.link.value();
                        results_.legBPS[legNo] *= spotFX_.link.value();
                    }

                    // Get start date and end date discount for the leg
                    Date startDate = CashFlows.startDate(arguments_.legs[legNo]);
                    if (startDate >= currency1Discountcurve_.link.referenceDate())
                    {
                        results_.startDiscounts[legNo] = legDiscountCurve.link.discount(startDate);
                    }
                    else
                    {
                        results_.startDiscounts[legNo] = null;
                    }

                    Date maturityDate = CashFlows.maturityDate(arguments_.legs[legNo]);
                    if (maturityDate >= currency1Discountcurve_.link.referenceDate())
                    {
                        results_.endDiscounts[legNo] = legDiscountCurve.link.discount(maturityDate);
                    }
                    else
                    {
                        results_.endDiscounts[legNo] = null;
                    }

                }
                catch (Exception e)
                {
                    Utils.QL_FAIL(" leg: " + e.Message);
                }

                results_.value += results_.legNPV[legNo];
            }
        }
        //@}

        //! \name Inspectors
        //@{
        public Handle<YieldTermStructure> currency1DiscountCurve() { return currency1Discountcurve_; }
        public Handle<YieldTermStructure> currency2DiscountCurve() { return currency2Discountcurve_; }

        public Currency currency1() { return ccy1_; }
        public Currency currency2() { return ccy2_; }

        public Handle<Quote> spotFX() { return spotFX_; }
        //@}
    }
}
