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
using System.Linq;
using System.Text;

namespace QLNet 
{
   public class DiscountingSwapEngine : Swap.SwapEngine 
   {
      private Handle<YieldTermStructure> discountCurve_;
      private bool? includeSettlementDateFlows_;
      private Date settlementDate_, npvDate_;

      public DiscountingSwapEngine(Handle<YieldTermStructure> discountCurve,bool? includeSettlementDateFlows = null,
                                   Date settlementDate = null, Date npvDate = null) 
      {
         discountCurve_ = discountCurve;
         discountCurve_.registerWith(update);
         includeSettlementDateFlows_ = includeSettlementDateFlows;
         settlementDate_ = settlementDate;
         npvDate_ = npvDate;
      }

      // Instrument interface
      public override void calculate() 
      {
         Utils.QL_REQUIRE( !discountCurve_.empty(), () => "discounting term structure handle is empty" );

         results_.value = results_.cash = 0;
         results_.errorEstimate = null;

         Date refDate = discountCurve_.link.referenceDate();

         Date settlementDate = settlementDate_;
         if (settlementDate_== null ) 
         {
            settlementDate = refDate;
         } 
         else 
         {
            Utils.QL_REQUIRE( settlementDate >= refDate, () =>
                       "settlement date (" + settlementDate + ") before " +
                       "discount curve reference date (" + refDate + ")");
         }

         results_.valuationDate = npvDate_;
         if (npvDate_== null ) 
         {
            results_.valuationDate = refDate;
         } 
         else 
         {
            Utils.QL_REQUIRE( npvDate_ >= refDate, () =>
                       "npv date (" + npvDate_  + ") before "+
                       "discount curve reference date (" + refDate + ")");
         }

         results_.npvDateDiscount = discountCurve_.link.discount(results_.valuationDate);
         
         int n = arguments_.legs.Count;
         
         results_.legNPV = new InitializedList<double?>(n);
         results_.legBPS= new InitializedList<double?>(n);
         results_.startDiscounts= new InitializedList<double?>(n);
         results_.endDiscounts = new InitializedList<double?>(n);

         bool includeRefDateFlows =
            includeSettlementDateFlows_.HasValue ?
            includeSettlementDateFlows_.Value :
            Settings.includeReferenceDateEvents;

         for (int i=0; i< n; ++i) 
         {
            try 
            {
               YieldTermStructure discount_ref = discountCurve_.currentLink();
               double npv = 0, bps = 0;
               CashFlows.npvbps(arguments_.legs[i],
                                  discount_ref,
                                  includeRefDateFlows,
                                  settlementDate,
                                  results_.valuationDate,
                                  out npv,
                                  out bps);
                results_.legNPV[i] = npv *arguments_.payer[i];
                results_.legBPS[i] = bps *arguments_.payer[i];

                Date d1 = CashFlows.startDate(arguments_.legs[i]);
                if (d1>=refDate)
                   results_.startDiscounts[i] = discountCurve_.link.discount(d1);
                else
                   results_.startDiscounts[i] = null;

                Date d2 = CashFlows.maturityDate(arguments_.legs[i]);
                if (d2>=refDate)
                   results_.endDiscounts[i] = discountCurve_.link.discount(d2);
                else
                   results_.endDiscounts[i] = null;

            } 
            catch (Exception e) 
            {
                Utils.QL_FAIL( (i+1) + " leg: " + e.Message);
            }
            results_.value += results_.legNPV[i];

            //results_.legNPV[i] = arguments_.payer[i] * CashFlows.npv(arguments_.legs[i], discountCurve_);
            //results_.legBPS[i] = arguments_.payer[i] * CashFlows.bps(arguments_.legs[i], discountCurve_);
            //results_.value += results_.legNPV[i];
            //results_.cash += arguments_.payer[i] * CashFlows.cash(arguments_.legs[i]);
            //try 
            //{
            //   Date d = CashFlows.startDate(arguments_.legs[i]);
            //   startDiscounts[i] = discountCurve_.link.discount(d);
            //} 
            //catch 
            //{
            //   startDiscounts[i] = null;
            //}
         }
         //results_.additionalResults.Add("startDiscounts", startDiscounts);
      }
    }
}
