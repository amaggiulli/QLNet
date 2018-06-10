/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// One-dimensional grid mesher
   /// </summary>
   public class FdmSimpleProcess1DMesher : Fdm1dMesher
   {
      public FdmSimpleProcess1DMesher(int size,
                                      StochasticProcess1D process,
                                      double maturity, int tAvgSteps = 10,
                                      double epsilon = 0.0001,
                                      double? mandatoryPoint = null)
      : base(size)
      {
         locations_ = new InitializedList<double>(locations_.Count, 0.0);
         for (int l = 1; l <= tAvgSteps; ++l)
         {
            double t = (maturity * l) / tAvgSteps;

            double mp = (mandatoryPoint != null) ? mandatoryPoint.Value
                        : process.x0();

            double qMin = Math.Min(Math.Min(mp, process.x0()),
                                   process.evolve(0, process.x0(), t,
                                                  new InverseCumulativeNormal().value(epsilon)));
            double qMax = Math.Max(Math.Max(mp, process.x0()),
                                   process.evolve(0, process.x0(), t,
                                                  new InverseCumulativeNormal().value(1 - epsilon)));

            double dp = (1 - 2 * epsilon) / (size - 1);
            double p = epsilon;
            locations_[0] += qMin;

            for (int i = 1; i < size - 1; ++i)
            {
               p += dp;
               locations_[i] += process.evolve(0, process.x0(), t,
                                               new InverseCumulativeNormal().value(p));
            }
            locations_[locations_.Count - 1] += qMax;
         }
         locations_ = locations_.Select(x => x / tAvgSteps).ToList();
         for (int i = 0; i < size - 1; ++i)
         {
            dminus_[i + 1] = dplus_[i] = locations_[i + 1] - locations_[i];
         }

         dplus_[dplus_.Count - 1] = null;
         dminus_[0] = null;
      }
   }
}
