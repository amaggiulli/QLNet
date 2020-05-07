/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Linq;

/*! \file fdmblackscholesmesher.cpp
    \brief 1-d mesher for the Black-Scholes process (in ln(S))
*/

namespace QLNet
{
   public class FdmHestonVarianceMesher : Fdm1dMesher
   {
      public FdmHestonVarianceMesher(int size,
                                     HestonProcess process,
                                     double maturity,
                                     int tAvgSteps = 10,
                                     double epsilon = 0.0001)
         : base(size)
      {
         List<double> vGrid = new InitializedList<double>(size, 0.0);
         List<double> pGrid = new InitializedList<double>(size, 0.0);

         double df = 4.0 * process.theta() * process.kappa() /
                     Math.Pow(process.sigma(), 2);
         try
         {
            List<pair_double> grid = new List<pair_double>();

            for (int l = 1; l <= tAvgSteps; ++l)
            {
               double t = (maturity * l) / tAvgSteps;
               double ncp = 4 * process.kappa() * Math.Exp(-process.kappa() * t)
                            / (Math.Pow(process.sigma(), 2)
                               * (1 - Math.Exp(-process.kappa() * t))) * process.v0();
               double k = Math.Pow(process.sigma(), 2)
                          * (1 - Math.Exp(-process.kappa() * t)) / (4 * process.kappa());

               double qMin = 0.0; // v_min = 0.0;
               double qMax = Math.Max(process.v0(),
                                      k * new InverseNonCentralCumulativeChiSquareDistribution(
                                         df, ncp, 100, 1e-8).value(1 - epsilon));

               double minVStep = (qMax - qMin) / (50 * size);
               double ps, p = 0.0;

               double vTmp = qMin;
               grid.Add(new pair_double(qMin, epsilon));

               for (int i = 1; i < size; ++i)
               {
                  ps = (1 - epsilon - p) / (size - i);
                  p += ps;
                  double tmp = k * new InverseNonCentralCumulativeChiSquareDistribution(
                                  df, ncp, 100, 1e-8).value(p);

                  double vx = Math.Max(vTmp + minVStep, tmp);
                  p = new NonCentralCumulativeChiSquareDistribution(df, ncp).value(vx / k);
                  vTmp = vx;
                  grid.Add(new pair_double(vx, p));
               }
            }
            Utils.QL_REQUIRE(grid.Count == size * tAvgSteps,
                             () => "something wrong with the grid size");

            grid.Sort();

            List<Pair<double, double>> tp = new List<Pair<double, double>>(grid);

            for (int i = 0; i < size; ++i)
            {
               int b = (i * tp.Count) / size;
               int e = ((i + 1) * tp.Count) / size;
               for (int j = b; j < e; ++j)
               {
                  vGrid[i] += tp[j].first / (e - b);
                  pGrid[i] += tp[j].second / (e - b);
               }
            }
         }
         catch (Exception)
         {
            // use default mesh
            double vol = process.sigma() *
                         Math.Sqrt(process.theta() / (2 * process.kappa()));

            double mean = process.theta();
            double upperBound = Math.Max(process.v0() + 4 * vol, mean + 4 * vol);
            double lowerBound
               = Math.Max(0.0, Math.Min(process.v0() - 4 * vol, mean - 4 * vol));

            for (int i = 0; i < size; ++i)
            {
               pGrid[i] = i / (size - 1.0);
               vGrid[i] = lowerBound + i * (upperBound - lowerBound) / (size - 1.0);
            }
         }

         double skewHint = ((process.kappa() != 0.0)
                            ? Math.Max(1.0, process.sigma() / process.kappa()) : 1.0);

         pGrid.Sort();
         volaEstimate_ = new GaussLobattoIntegral(100000, 1e-4).value(
            new interpolated_volatility(pGrid, vGrid).value,
            pGrid.First(),
            pGrid.Last()) * Math.Pow(skewHint, 1.5);

         double v0 = process.v0();
         for (int i = 1; i < vGrid.Count; ++i)
         {
            if (vGrid[i - 1] <= v0 && vGrid[i] >= v0)
            {
               if (Math.Abs(vGrid[i - 1] - v0) < Math.Abs(vGrid[i] - v0))
                  vGrid[i - 1] = v0;
               else
                  vGrid[i] = v0;
            }
         }
         locations_ = vGrid.Select(x => x).ToList();
         for (int i = 0; i < size - 1; ++i)
         {
            dminus_[i + 1] = dplus_[i] = vGrid[i + 1] - vGrid[i];
         }
         dplus_[dplus_.Count - 1] = null;
         dminus_[0] = null;
      }

      public double volaEstimate() { return volaEstimate_; }

      protected double volaEstimate_;
   }


   public class FdmHestonLocalVolatilityVarianceMesher : Fdm1dMesher
   {
      public FdmHestonLocalVolatilityVarianceMesher(int size,
                                                    HestonProcess process,
                                                    LocalVolTermStructure leverageFct,
                                                    double maturity,
                                                    int tAvgSteps = 10,
                                                    double epsilon = 0.0001)
         : base(size)
      {
         leverageFct_ = leverageFct;
         FdmHestonVarianceMesher mesher = new FdmHestonVarianceMesher(size, process, maturity, tAvgSteps, epsilon);

         for (int i = 0; i < size; ++i)
         {
            dplus_[i] = mesher.dplus(i);
            dminus_[i] = mesher.dminus(i);
            locations_[i] = mesher.location(i);
         }

         volaEstimate_ = mesher.volaEstimate();

         if (leverageFct != null)
         {
            double s0 = process.s0().currentLink().value();

            List<double> acc = new List<double>();
            acc.Add(leverageFct.localVol(0.0, s0, true));

            Handle<YieldTermStructure> rTS = process.riskFreeRate();
            Handle<YieldTermStructure> qTS = process.dividendYield();

            for (int l = 1; l <= tAvgSteps; ++l)
            {
               double t = (maturity * l) / tAvgSteps;
               double vol = volaEstimate_ * acc.Average();
               double fwd = s0 * qTS.currentLink().discount(t) / rTS.currentLink().discount(t);
               int sAvgSteps = 50;
               Vector u = new Vector(sAvgSteps), sig = new Vector(sAvgSteps);

               for (int i = 0; i < sAvgSteps; ++i)
               {
                  u[i] = epsilon + ((1.0 - 2.0 * epsilon) / (sAvgSteps - 1.0)) * i;
                  double x = new InverseCumulativeNormal().value(u[i]);
                  double gf = x * vol * Math.Sqrt(t);
                  double f = fwd * Math.Exp(gf);
                  sig[i] = Math.Pow(leverageFct.localVol(t, f, true), 2.0);
               }

               double leverageAvg = new GaussLobattoIntegral(10000, 1E-4).value(new interpolated_volatility(u, sig).value,
                                                                                u.First(),
                                                                                u.Last())
               / (1.0 - 2.0 * epsilon);

               acc.Add(leverageAvg);
            }

            volaEstimate_ *= acc.Average();
         }
      }

      public double volaEstimate() { return volaEstimate_; }

      protected double volaEstimate_;
      protected LocalVolTermStructure leverageFct_;
   }

   public class interpolated_volatility
   {
      public interpolated_volatility(List<double> pGrid,
                                     List<double> vGrid)
      {
         variance = new LinearInterpolation(pGrid, pGrid.Count, vGrid);
      }

      public double value(double x)
      {
         return Math.Sqrt(variance.value(x));
      }

      LinearInterpolation variance;
   }
}
