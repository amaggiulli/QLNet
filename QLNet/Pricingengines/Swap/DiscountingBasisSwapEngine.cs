/*
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
    public class DiscountingBasisSwapEngine : Swap.SwapEngine {
        private List<Handle<YieldTermStructure>> discountCurve_;

        public DiscountingBasisSwapEngine(Handle<YieldTermStructure> discountCurve1 , 
                                     Handle<YieldTermStructure> discountCurve2) 
        {
           discountCurve_ = new List<Handle<YieldTermStructure>>();
           discountCurve_.Add(discountCurve1);
           discountCurve_.Add(discountCurve2);
        }

        // Instrument interface
        public override void calculate() {
            if (discountCurve_.empty()) throw new ArgumentException("no discounting term structure set");
            if (discountCurve_.Count != arguments_.legs.Count) throw new ArgumentException("no discounting term structure set for all legs");

            results_.value = results_.cash = 0;
            results_.errorEstimate = null;
            results_.legNPV = new InitializedList<double?>(arguments_.legs.Count);
            results_.legBPS = new InitializedList<double?>(arguments_.legs.Count);
            List<double?> startDiscounts = new InitializedList<double?>(arguments_.legs.Count);
            for (int i=0; i<arguments_.legs.Count; ++i) {
               //results_.legNPV[i] = arguments_.payer[i] * CashFlows.npv(arguments_.legs[i], discountCurve_[i]);
               //results_.legBPS[i] = arguments_.payer[i] * CashFlows.bps(arguments_.legs[i], discountCurve_[i]);
                results_.value += results_.legNPV[i];
                results_.cash += arguments_.payer[i] * CashFlows.cash(arguments_.legs[i]);
                try {
                    Date d = CashFlows.startDate(arguments_.legs[i]);
                    startDiscounts[i] = discountCurve_[i].link.discount(d);
                } catch {
                    startDiscounts[i] = null;
                }
            }
            results_.additionalResults.Add("startDiscounts", startDiscounts);
        }
    }
}
