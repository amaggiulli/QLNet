/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   // Interest rate swap
   // The cash flows belonging to the first leg are paid; the ones belonging to the second leg are received.
   public class Swap : Instrument
   {
      #region Data members

      protected List<List<CashFlow>> legs_;
      protected List<double> payer_;
      protected List < double? > legNPV_;
      protected List < double? > legBPS_;
      protected List < double? > startDiscounts_;
      protected List < double? > endDiscounts_;
      protected double? npvDateDiscount_;

      public Arguments arguments { get; set; }
      public Results results { get; set; }
      public SwapEngine engine { get; set; }

      #endregion

      #region Constructors

      // The cash flows belonging to the first leg are paid
      // the ones belonging to the second leg are received.
      public Swap(List<CashFlow> firstLeg, List<CashFlow> secondLeg)
      {
         legs_ = new InitializedList<List<CashFlow>>(2);
         legs_[0] = firstLeg;
         legs_[1] = secondLeg;
         payer_ = new InitializedList<double>(2);
         payer_[0] = -1.0;
         payer_[1] = 1.0;
         legNPV_ = new InitializedList < double? >(2);
         legBPS_ = new InitializedList < double? >(2);

         startDiscounts_ = new InitializedList < double? >(2);
         endDiscounts_ = new InitializedList < double? >(2);
         npvDateDiscount_ = 0.0;

         for (int i = 0; i < legs_.Count; i++)
            for (int j = 0; j < legs_[i].Count; j++)
               legs_[i][j].registerWith(update);
      }

      // Multi leg constructor.
      public Swap(List<List<CashFlow>> legs, List<bool> payer)
      {
         legs_ = (InitializedList<List<CashFlow>>)legs;
         payer_ = new InitializedList<double>(legs.Count, 1.0);
         legNPV_ = new InitializedList < double? >(legs.Count, 0.0);
         legBPS_ = new InitializedList < double? >(legs.Count, 0.0);
         startDiscounts_ = new InitializedList < double? >(legs.Count, 0.0);
         endDiscounts_ = new InitializedList < double? >(legs.Count, 0.0);
         npvDateDiscount_ = 0.0;

         Utils.QL_REQUIRE(payer.Count == legs_.Count, () => "size mismatch between payer (" + payer.Count +
                          ") and legs (" + legs_.Count + ")");
         for (int i = 0; i < legs_.Count; ++i)
         {
            if (payer[i])
               payer_[i] = -1;
            for (int j = 0; j < legs_[i].Count; j++)
               legs_[i][j].registerWith(update);
         }
      }

      /*! This constructor can be used by derived classes that will
            build their legs themselves.
      */
      protected Swap(int legs)
      {
         legs_ = new InitializedList<List<CashFlow>>(legs);
         payer_ = new InitializedList<double>(legs);
         legNPV_ = new InitializedList < double? >(legs, 0.0);
         legBPS_ = new InitializedList < double? >(legs, 0.0);
         startDiscounts_ = new InitializedList < double? >(legs, 0.0);
         endDiscounts_ = new InitializedList < double? >(legs, 0.0);
         npvDateDiscount_ = 0.0;
      }
      #endregion

      #region Instrument interface

      public override bool isExpired()
      {
         Date today = Settings.evaluationDate();
         return !legs_.Any<List<CashFlow>>(leg => leg.Any<CashFlow>(cf => !cf.hasOccurred(today)));
      }

      protected override void setupExpired()
      {
         base.setupExpired();
         legBPS_ = new InitializedList < double? >(legBPS_.Count);
         legNPV_ = new InitializedList < double? >(legNPV_.Count);
         startDiscounts_ = new InitializedList < double? >(startDiscounts_.Count);
         endDiscounts_ = new InitializedList < double? >(endDiscounts_.Count);
         npvDateDiscount_ = 0.0;
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         Swap.Arguments arguments = args as Swap.Arguments;
         Utils.QL_REQUIRE(arguments != null, () => "wrong argument type");

         arguments.legs = legs_;
         arguments.payer = payer_;
      }

      public override void fetchResults(IPricingEngineResults r)
      {
         base.fetchResults(r);

         Swap.Results results = r as Swap.Results;
         Utils.QL_REQUIRE(results != null, () => "wrong result type");

         if (!results.legNPV.empty())
         {
            Utils.QL_REQUIRE(results.legNPV.Count == legNPV_.Count, () => "wrong number of leg NPV returned");
            legNPV_ = new List < double? >(results.legNPV);
         }
         else
         {
            legNPV_ = new InitializedList < double? >(legNPV_.Count);
         }

         if (!results.legBPS.empty())
         {
            Utils.QL_REQUIRE(results.legBPS.Count == legBPS_.Count, () => "wrong number of leg BPS returned");
            legBPS_ =  new List < double? >(results.legBPS);
         }
         else
         {
            legBPS_ = new InitializedList < double? >(legBPS_.Count);
         }

         if (!results.startDiscounts.empty())
         {
            Utils.QL_REQUIRE(results.startDiscounts.Count == startDiscounts_.Count, () => "wrong number of leg start discounts returned");
            startDiscounts_ =  new List < double? >(results.startDiscounts);
         }
         else
         {
            startDiscounts_ = new InitializedList < double? >(startDiscounts_.Count);
         }

         if (!results.endDiscounts.empty())
         {
            Utils.QL_REQUIRE(results.endDiscounts.Count == endDiscounts_.Count, () => "wrong number of leg end discounts returned");
            endDiscounts_ =  new List < double? >(results.endDiscounts);
         }
         else
         {
            endDiscounts_ = new InitializedList < double? >(endDiscounts_.Count);
         }

         if (results.npvDateDiscount != null)
         {
            npvDateDiscount_ = results.npvDateDiscount;
         }
         else
         {
            npvDateDiscount_ = null;
         }
      }

      #endregion

      #region Additional interface

      public Date startDate()
      {
         Utils.QL_REQUIRE(!legs_.empty(), () => "no legs given");
         return legs_.Min(leg => CashFlows.startDate(leg));
      }

      public Date maturityDate()
      {
         Utils.QL_REQUIRE(!legs_.empty(), () => "no legs given");
         return legs_.Max(leg => CashFlows.maturityDate(leg));
      }

      public double? legBPS(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg# " + j + " doesn't exist!");
         calculate();
         return legBPS_[j];
      }

      public double? legNPV(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg# " + j + " doesn't exist!");
         calculate();
         return legNPV_[j];
      }

      public double? startDiscounts(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         calculate();
         return startDiscounts_[j];
      }

      public double? endDiscounts(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         calculate();
         return endDiscounts_[j];
      }

      public double? npvDateDiscount()
      {
         calculate();
         return npvDateDiscount_;
      }

      public List<CashFlow> leg(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         return legs_[j];
      }

      public double payer(int j)
      {
         Utils.QL_REQUIRE(j < legs_.Count, () => "leg #" + j + " doesn't exist!");
         return payer_[j];
      }

      #endregion

      ////////////////////////////////////////////////////////////////
      // arguments, results, pricing engine
      public class Arguments : IPricingEngineArguments
      {
         public List<List<CashFlow >> legs { get; set; }
         public List<double> payer { get; set; }
         public virtual void validate()
         {
            Utils.QL_REQUIRE(legs.Count == payer.Count, () => "number of legs and multipliers differ");
         }
      }

      public new class Results : Instrument.Results
      {
         public List < double? > legNPV { get; set; }
         public List < double? > legBPS { get; set; }
         public List < double? > startDiscounts { get; set; }
         public List < double? > endDiscounts { get; set; }
         public double? npvDateDiscount { get; set; }
         public override void reset()
         {
            base.reset();
            // clear all previous results
            if (legNPV == null)
               legNPV = new List < double? >();
            else
               legNPV.Clear();

            if (legBPS == null)
               legBPS = new List < double? >();
            else
               legBPS.Clear();

            if (startDiscounts == null)
               startDiscounts = new List < double? >();
            else
               startDiscounts.Clear();

            if (endDiscounts == null)
               endDiscounts = new List < double? >();
            else
               endDiscounts.Clear();

            npvDateDiscount = null;
         }
      }

      public abstract class SwapEngine : GenericEngine<Swap.Arguments, Swap.Results> { }
   }
}
