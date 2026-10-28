/*
 Copyright (C) 2008-2023 Andrea Maggiulli (a.maggiulli@gmail.com)

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

// Numerical lattice engines for callable/puttable bonds
namespace QLNet
{
   /// <summary>
   /// Numerical lattice engine for callable fixed rate bonds
   /// </summary>
   public class TreeCallableFixedRateBondEngine : LatticeShortRateModelEngine<CallableBond.Arguments, CallableBond.Results>
   {
      /// <summary>
      /// Ctor
      /// </summary>
      /// <remarks>
      /// The term structure is only needed when the short-rate
      /// model cannot provide one itself.
      /// </remarks>
      /// <param name="model"></param>
      /// <param name="timeSteps"></param>
      /// <param name="termStructure"></param>
      public TreeCallableFixedRateBondEngine(ShortRateModel model, int  timeSteps, Handle<YieldTermStructure> termStructure = null)
         : base(model, timeSteps)

      {
         termStructure_ = termStructure ?? new Handle<YieldTermStructure>();
         termStructure_.registerWith(update);
      }

      public TreeCallableFixedRateBondEngine(ShortRateModel model, TimeGrid timeGrid, Handle<YieldTermStructure> termStructure = null)
         : base(model, timeGrid)
      {
         termStructure_ = termStructure ?? new Handle<YieldTermStructure>();
         termStructure_.registerWith(update);
      }

      public override void calculate()
      {
         calculateWithSpread(arguments_.spread);
      }

      /// <summary>
      /// Drops the lattice cached for the <c>timeSteps</c> constructor, since a change in the
      /// model or the term structure invalidates its calibration.
      /// </summary>
      public override void update()
      {
         cachedLattice_ = null;
         cachedTimes_ = null;
         base.update();
      }

      private Handle<YieldTermStructure> termStructure_;

      // With the timeSteps constructor the lattice depends only on the model and on the bond's
      // mandatory times, not on the spread: it is reused across calculate() calls (e.g. every
      // iteration of an OAS solve) until either changes.
      private Lattice cachedLattice_;
      private List<double> cachedTimes_;

      private void calculateWithSpread(double s)
      {
         Utils.QL_REQUIRE(model_ != null,()=> "no model specified");
         ITermStructureConsistentModel tsmodel = (ITermStructureConsistentModel)base.model_.link;

         Handle<YieldTermStructure> discountCurve = tsmodel != null ? tsmodel.termStructure() : termStructure_;

         var callableBond = new DiscretizedCallableFixedRateBond(arguments_, discountCurve);
         Lattice lattice;

         if (lattice_ != null)
         {
            lattice = lattice_;
         }
         else
         {
            var times = callableBond.mandatoryTimes();
            if (cachedLattice_ == null || !times.SequenceEqual(cachedTimes_))
            {
               var timeGrid = new TimeGrid(times, times.Count, timeSteps_);
               cachedLattice_ = model_.link.tree(timeGrid);
               cachedTimes_ = times;
            }
            lattice = cachedLattice_;
         }

         // The spread is set even when zero: a reused lattice still carries the previous one.
         if (lattice is OneFactorModel.ShortRateTree sr)
            sr.setSpread(s);
         else
            Utils.QL_REQUIRE(s.IsEqual(0.0), () => "Spread is not supported for trees other than OneFactorModel");

         var referenceDate = discountCurve.link.referenceDate();
         var dayCounter = discountCurve.link.dayCounter();
         var redemptionTime = dayCounter.yearFraction(referenceDate, arguments_.redemptionDate);

         callableBond.initialize(lattice, redemptionTime);
         callableBond.rollback(0.0);

         results_.value = callableBond.presentValue();

         var d = discountCurve.link.discount(arguments_.settlementDate);
         results_.settlementValue = results_.value / d;
      }
   }

   /// <summary>
   /// Numerical lattice engine for callable zero coupon bonds
   /// </summary>
   public class TreeCallableZeroCouponBondEngine : TreeCallableFixedRateBondEngine
   {
      public TreeCallableZeroCouponBondEngine(ShortRateModel model, int timeSteps,
         Handle<YieldTermStructure> termStructure = null)
         : base(model, timeSteps, termStructure ?? new Handle<YieldTermStructure>())
      {}

      public TreeCallableZeroCouponBondEngine(ShortRateModel model, TimeGrid timeGrid,
         Handle<YieldTermStructure> termStructure = null)
         : base(model, timeGrid, termStructure ?? new Handle<YieldTermStructure>())
      { }
   };
}
