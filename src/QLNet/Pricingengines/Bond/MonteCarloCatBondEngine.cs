//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// Monte Carlo pricing engine for cat bonds
   /// </summary>
   public class MonteCarloCatBondEngine : CatBond.Engine
   {
      public MonteCarloCatBondEngine(CatRisk catRisk,
                                     Handle<YieldTermStructure> discountCurve,
                                     bool? includeSettlementDateFlows = null)
      {
         catRisk_ = catRisk;
         discountCurve_ = discountCurve;
         includeSettlementDateFlows_ = includeSettlementDateFlows;
      }
      public override void calculate()
      {
         Utils.QL_REQUIRE(!discountCurve_.empty(), () =>
                          "discounting term structure handle is empty");

         results_.valuationDate = discountCurve_.link.referenceDate();

         bool includeRefDateFlows = includeSettlementDateFlows_.HasValue ?
                                    includeSettlementDateFlows_.Value :
                                    Settings.includeReferenceDateEvents;

         results_.value = npv(includeRefDateFlows,
                              results_.valuationDate,
                              results_.valuationDate,
                              out var lossProbability,
                              out var exhaustionProbability,
                              out var expectedLoss);

         results_.lossProbability = lossProbability;
         results_.exhaustionProbability = exhaustionProbability;
         results_.expectedLoss = expectedLoss;

         // a bond's cashflow on settlement date is never taken into
         // account, so we might have to play it safe and recalculate
         if (!includeRefDateFlows &&
             results_.valuationDate == arguments_.settlementDate)
         {
            // same parameters as above, we can avoid another call
            results_.settlementValue = results_.value;
         }
         else
         {
            // no such luck
            results_.settlementValue =
               npv(includeRefDateFlows, arguments_.settlementDate, arguments_.settlementDate,
                   out lossProbability, out exhaustionProbability, out expectedLoss);
         }
      }
      public Handle<YieldTermStructure> discountCurve()
      {
         return discountCurve_;
      }

      protected double cashFlowRiskyValue(CashFlow cf,  NotionalPath notionalPath)
      {
         return cf.amount() * notionalPath.notionalRate(cf.date()); //TODO: fix for more complicated cashflows
      }

      protected double npv(bool includeSettlementDateFlows,
                           Date settlementDate,
                           Date npvDate,
                           out double lossProbability,
                           out double exhaustionProbability,
                           out double expectedLoss)
      {
         const int MAX_PATHS = 10000; //TODO
         lossProbability =  0.0;
         exhaustionProbability = 0.0;
         expectedLoss = 0.0;
         if (arguments_.cashflows.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         double totalNPV = 0.0;
         Date effectiveDate = Date.Max(arguments_.startDate, settlementDate);
         Date maturityDate = arguments_.cashflows.Last().date();
         CatSimulation catSimulation = catRisk_.newSimulation(effectiveDate, maturityDate);
         List<KeyValuePair<Date, double> > eventsPath = new List<KeyValuePair<Date, double>>();
         NotionalPath notionalPath = new NotionalPath();
         double riskFreeNPV = pathNpv(includeSettlementDateFlows, settlementDate, notionalPath);
         int pathCount = 0;
         while (catSimulation.nextPath(eventsPath) && pathCount < MAX_PATHS)
         {
            arguments_.notionalRisk.updatePath(eventsPath, notionalPath);
            if (notionalPath.loss() > 0)
            {
               //optimization, most paths will not include any loss
               totalNPV += pathNpv(includeSettlementDateFlows, settlementDate, notionalPath);
               lossProbability += 1;
               if (notionalPath.loss().IsEqual(1))
                  exhaustionProbability += 1;
               expectedLoss += notionalPath.loss();
            }
            else
            {
               totalNPV += riskFreeNPV;
            }
            pathCount++;
         }

         lossProbability /= pathCount;
         exhaustionProbability /= pathCount;
         expectedLoss /= pathCount;
         return totalNPV / (pathCount * discountCurve_.link.discount(npvDate));

      }

      protected double pathNpv(bool includeSettlementDateFlows,
                               Date settlementDate,
                               NotionalPath notionalPath)
      {
         double totalNPV = 0.0;
         for (int i = 0; i < arguments_.cashflows.Count; ++i)
         {
            if (!arguments_.cashflows[i].hasOccurred(settlementDate, includeSettlementDateFlows))
            {
               double amount = cashFlowRiskyValue(arguments_.cashflows[i], notionalPath);
               totalNPV += amount * discountCurve_.link.discount(arguments_.cashflows[i].date());
            }
         }
         return totalNPV;
      }

      private CatRisk catRisk_;
      private Handle<YieldTermStructure> discountCurve_;
      private bool? includeSettlementDateFlows_;

   }
}
