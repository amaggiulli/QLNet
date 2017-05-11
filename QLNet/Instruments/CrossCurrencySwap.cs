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
    //! Cross currency swap
    /*! The first leg holds the pay currency cashflows and second leg holds
        the receive currency cashflows.

                \ingroup instruments
    */
    public class CrossCurrencySwap : Swap 
    {
        protected InitializedList<Currency> currencies_;
        private InitializedList<double?> inCcyLegNPV_;
        private InitializedList<double?> inCcyLegBPS_;
        private InitializedList<double> npvDateDiscounts_;

        //! \name Constructors
        //@{
        //! First leg is paid and the second is received.
        public CrossCurrencySwap(List<CashFlow> firstLeg, 
                                 Currency firstLegCcy, 
                                 List<CashFlow> secondLeg, 
                                 Currency secondLegCcy)
                            : base(firstLeg, secondLeg) 
        {
            currencies_ = new InitializedList<Currency>(2);
            currencies_[0] = firstLegCcy;
            currencies_[1] = secondLegCcy;
        }

        /*! Multi leg constructor. */
        public CrossCurrencySwap(List<List<CashFlow>> legs, List<bool> payer, InitializedList<Currency> currencies)
            : base(legs, payer)
        {
            currencies_ = currencies;
            Utils.QL_REQUIRE(payer.Count == currencies_.Count, () => "Size mismatch between payer (" + payer.Count + ") and currencies (" + currencies_.Count + ")");                            
        }

        /*! This constructor can be used by derived classes that will
            build their legs themselves.
        */
        protected CrossCurrencySwap(int legs)
            : base(legs)
        {
            currencies_ = new InitializedList<Currency>(legs);
            inCcyLegNPV_ = new InitializedList<double?>(legs);
            inCcyLegBPS_ = new InitializedList<double?>(legs);
            npvDateDiscounts_ = new InitializedList<double>(legs);
        }
        
        //@}
        //! \name Instrument interface
        //@{
        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            CrossCurrencySwap.Arguments arguments = args as CrossCurrencySwap.Arguments;

            Utils.QL_REQUIRE(arguments != null, () =>  "The arguments are not of type cross currency swap");
            arguments.currencies = currencies_;
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);

            CrossCurrencySwap.Results results = r as CrossCurrencySwap.Results;
            Utils.QL_REQUIRE(results != null, () => "The results are not of type cross currency swap");

            if (!results.inCcyLegNPV.empty()) 
            {
                //Utils.QL_REQUIRE(results.inCcyLegNPV.Count == inCcyLegNPV_.Count, () => "Wrong number of in currency leg NPVs returned by engine");
                inCcyLegNPV_ = results.inCcyLegNPV;

            } 
            else 
            {
                inCcyLegNPV_ = new InitializedList<double?>(inCcyLegNPV_.Count);
            }

            if (!results.inCcyLegBPS.empty()) {
                //Utils.QL_REQUIRE(results.inCcyLegBPS.Count == inCcyLegBPS_.Count, () => "Wrong number of in currency leg BPSs returned by engine");
                legBPS_ = results.legBPS;

            } 
            else
            {
                inCcyLegBPS_ = new InitializedList<double?>(inCcyLegBPS_.Count);
            }

            if (!results.npvDateDiscounts.empty()) 
            {
                //Utils.QL_REQUIRE(results.npvDateDiscounts.Count == npvDateDiscounts_.Count, () => "Wrong number of npv date discounts returned by engine");
                npvDateDiscounts_ = results.npvDateDiscounts;
            } 
            else 
            {
                npvDateDiscounts_ = new InitializedList<double>(npvDateDiscounts_.Count);
            }
        }
        
        //@}
        //! \name Additional interface
        //@{
        Currency legCurrency(int j) 
        {
            Utils.QL_REQUIRE(j < legs_.Count,() => "leg# " + j + " doesn't exist!");
            return currencies_[j];
        }

        double? inCcyLegBPS(int j) 
        {
            Utils.QL_REQUIRE(j < legs_.Count,() => "leg# " + j + " doesn't exist!");
            calculate();
            return inCcyLegBPS_[j];
        }
        double? inCcyLegNPV(int j) 
        {
            Utils.QL_REQUIRE(j < legs_.Count,() => "leg# " + j + " doesn't exist!");
            calculate();
            return inCcyLegNPV_[j];
        }
        
        double npvDateDiscounts(int j) 
        {
            Utils.QL_REQUIRE(j < legs_.Count,() => "leg# " + j + " doesn't exist!");
            calculate();
            return npvDateDiscounts_[j];
        }

        //@}
        //! \name Instrument interface
        //@{
        protected override void setupExpired()
        {
            base.setupExpired();
            inCcyLegBPS_ = new InitializedList<double?>(inCcyLegBPS_.Count);
            inCcyLegNPV_ = new InitializedList<double?>(inCcyLegNPV_.Count);
            npvDateDiscounts_ = new InitializedList<double>(npvDateDiscounts_.Count);
        }
        //@}

        new public class Arguments : Swap.Arguments {
            public InitializedList<Currency> currencies;
            public override void validate() 
            {
                base.validate();
                Utils.QL_REQUIRE(legs.Count == currencies.Count, () => "Number of legs is not equal to number of currencies");
            }
        }

        new public class Results : Swap.Results {
            public InitializedList<double?> inCcyLegNPV = new InitializedList<double?>();
            public InitializedList<double?> inCcyLegBPS = new InitializedList<double?>();
            public InitializedList<double> npvDateDiscounts = new InitializedList<double>();

            public override void reset()
            {
                base.reset();
                inCcyLegNPV.Clear();
                inCcyLegBPS.Clear();
                npvDateDiscounts.Clear();
            }
        }

        public abstract class Engine : GenericEngine<CrossCurrencySwap.Arguments, CrossCurrencySwap.Results> { }
    }
}

