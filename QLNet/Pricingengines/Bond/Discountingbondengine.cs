/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

namespace QLNet 
{
   public class DiscountingBondEngine : Bond.Engine 
   {
      private Handle<YieldTermStructure> discountCurve_;
      private bool? includeSettlementDateFlows_;

      public Handle<YieldTermStructure> discountCurve() {return discountCurve_; }

      public DiscountingBondEngine(Handle<YieldTermStructure> discountCurve, bool? includeSettlementDateFlows = null) 
      {
         discountCurve_ = discountCurve;
         discountCurve_.registerWith(update);
         includeSettlementDateFlows_ = includeSettlementDateFlows;
      }

      public override void calculate() 
      {
         Utils.QL_REQUIRE( !discountCurve_.empty(), () => "discounting term structure handle is empty" );

         results_.valuationDate = discountCurve_.link.referenceDate();
         bool includeRefDateFlows =
            includeSettlementDateFlows_.HasValue ?
            includeSettlementDateFlows_.Value :
            Settings.includeReferenceDateEvents;

         results_.value = CashFlows.npv(arguments_.cashflows,
                                        discountCurve_,
                                        includeRefDateFlows,
                                        results_.valuationDate,
                                        results_.valuationDate);

         results_.cash = CashFlows.cash(arguments_.cashflows, arguments_.settlementDate);

         // a bond's cashflow on settlement date is never taken into
         // account, so we might have to play it safe and recalculate
         if (!includeRefDateFlows && results_.valuationDate == arguments_.settlementDate)
         {
            // same parameters as above, we can avoid another call
            results_.settlementValue = results_.value;
         }
         else
         {
            // no such luck
            results_.settlementValue =
                CashFlows.npv(arguments_.cashflows,
                               discountCurve_,
                               false,
                               arguments_.settlementDate,
                               arguments_.settlementDate);
         }
      }
   }
}
