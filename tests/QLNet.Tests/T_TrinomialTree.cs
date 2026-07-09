/*
 Copyright (C) 2026 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System;
using System.Collections.Generic;
using Xunit;
using QLNet;

namespace TestSuite;

[Collection("QLNet CI Tests")]
public class T_TrinomialTree
{
   [Fact]
   public void testSmallMandatoryGapDoesNotExplode()
   {
      // Testing trinomial tree node count stays bounded with a small mandatory time gap
      var today = new Date(15, Month.January, 2026);
      Settings.setEvaluationDate(today);

      var termStructure = new Handle<YieldTermStructure>(
         Utilities.flatRate(today, 0.05, new Actual365Fixed()));
      var model = new HullWhite(termStructure, 0.1, 0.01);
      var process = model.dynamics().process();

      var mandatoryTimes = new List<double> { 1.0, 1.0 + 1.0e-3, 2.0, 3.0 };
      var grid = new TimeGrid(mandatoryTimes);

      var tree = new TrinomialTree(process, grid);

      var maxNodes = 0;
      for (var i = 0; i < grid.size(); ++i)
         maxNodes = Math.Max(maxNodes, tree.size(i));

      var nSteps = grid.size() - 1;
      var expectedBound = 2 * nSteps + 1;
      QAssert.IsTrue(maxNodes <= expectedBound,
         "trinomial tree exceeded derived bound: max node count "
         + maxNodes + " > 2*nSteps+1 = " + expectedBound
         + " (dx-floor fix may be broken)");
   }

   [Fact]
   public void testFloorThresholdBoundary()
   {
      // Testing trinomial tree behaviour at the floor activation threshold
      var today = new Date(15, Month.January, 2026);
      Settings.setEvaluationDate(today);

      var termStructure = new Handle<YieldTermStructure>(
         Utilities.flatRate(today, 0.05, new Actual365Fixed()));
      var model = new HullWhite(termStructure, 0.1, 0.01);
      var process = model.dynamics().process();

      (int maxNodes, int nSteps, double shortStepDx, double shortStepNaturalDx) runProbe(double gapRatio)
      {
         var dtMax = 1.0;
         var times = new List<double> { 1.0, 1.0 + gapRatio * dtMax, 2.0, 3.0 };
         var grid = new TimeGrid(times);
         var tree = new TrinomialTree(process, grid);

         var shortIdx = 0;
         for (var i = 1; i < grid.size() - 1; ++i)
         {
            if (grid.dt(i) < grid.dt(shortIdx))
               shortIdx = i;
         }

         var shortDt = grid.dt(shortIdx);
         var shortV2 = process.variance(grid[shortIdx], 0.0, shortDt);
         var shortNatural = Math.Sqrt(shortV2) * Math.Sqrt(3.0);

         var maxNodes = 0;
         for (var i = 0; i < grid.size(); ++i)
            maxNodes = Math.Max(maxNodes, tree.size(i));

         return (maxNodes, grid.size() - 1, tree.dx(shortIdx + 1), shortNatural);
      }

      const double below = 0.0099;
      const double above = 0.0101;

      var belowResult = runProbe(below);
      var belowBound = 2 * belowResult.nSteps + 1;
      QAssert.IsTrue(belowResult.maxNodes <= belowBound,
         "below-threshold node count " + belowResult.maxNodes
         + " exceeded derived bound 2*nSteps+1 = " + belowBound);
      QAssert.IsTrue(belowResult.shortStepDx > belowResult.shortStepNaturalDx,
         "below-threshold floor did not activate: dx="
         + belowResult.shortStepDx
         + " natural=" + belowResult.shortStepNaturalDx
         + " (FloorThreshold may have drifted down)");

      var aboveResult = runProbe(above);
      QAssert.AreEqual(aboveResult.shortStepNaturalDx, aboveResult.shortStepDx);
   }
}
