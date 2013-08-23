/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   class IncrementalGaussianStatistics : GenericGaussianStatistics<IncrementalStatistics>
   {
      public double downsideVariance() { return ((IncrementalStatistics)impl_).downsideVariance(); }
   }

   [TestClass()]
   public class T_RiskStats
   {
      [TestMethod()]
      public void RiskStatisticsTest()
      {
         //    ("Testing risk measures...");

         IncrementalGaussianStatistics igs = new IncrementalGaussianStatistics();
         RiskStatistics s = new RiskStatistics();

         double[] averages = { -100.0, -1.0, 0.0, 1.0, 100.0 };
         double[] sigmas = { 0.1, 1.0, 100.0 };
         int i, j, k, N;
         N = (int)Math.Pow(2, 16) - 1;
         double dataMin, dataMax;
         List<double> data = new InitializedList<double>(N), weights = new InitializedList<double>(N);

         for (i = 0; i < averages.Length; i++)
         {
            for (j = 0; j < sigmas.Length; j++)
            {

               NormalDistribution normal = new NormalDistribution(averages[i], sigmas[j]);
               CumulativeNormalDistribution cumulative = new CumulativeNormalDistribution(averages[i], sigmas[j]);
               InverseCumulativeNormal inverseCum = new InverseCumulativeNormal(averages[i], sigmas[j]);

               SobolRsg rng = new SobolRsg(1);
               dataMin = double.MaxValue;
               dataMax = double.MinValue;
               for (k = 0; k < N; k++)
               {
                  data[k] = inverseCum.value(rng.nextSequence().value[0]);
                  dataMin = Math.Min(dataMin, data[k]);
                  dataMax = Math.Max(dataMax, data[k]);
                  weights[k] = 1.0;
               }

               igs.addSequence(data, weights);
               s.addSequence(data, weights);

               // checks
               double calculated, expected;
               double tolerance;

               if (igs.samples() != N)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong number of samples\n"
                             + "    calculated: " + igs.samples() + "\n"
                             + "    expected:   " + N);
               if (s.samples() != N)
                  Assert.Fail("RiskStatistics: wrong number of samples\n"
                             + "    calculated: " + s.samples() + "\n"
                             + "    expected:   " + N);


               // weightSum()
               tolerance = 1e-10;
               expected = weights.Sum();
               calculated = igs.weightSum();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong sum of weights\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.weightSum();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong sum of weights\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // min
               tolerance = 1e-12;
               expected = dataMin;
               calculated = igs.min();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong minimum value\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.min();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: "
                             + "wrong minimum value\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // max
               expected = dataMax;
               calculated = igs.max();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong maximum value\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.max();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: "
                             + "wrong maximum value\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // mean
               expected = averages[i];
               tolerance = (expected == 0.0 ? 1.0e-13 :
                                              Math.Abs(expected) * 1.0e-13);
               calculated = igs.mean();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong mean value"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.mean();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong mean value"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // variance
               expected = sigmas[j] * sigmas[j];
               tolerance = expected * 1.0e-1;
               calculated = igs.variance();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong variance"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.variance();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong variance"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // standardDeviation
               expected = sigmas[j];
               tolerance = expected * 1.0e-1;
               calculated = igs.standardDeviation();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong standard deviation"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.standardDeviation();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong standard deviation"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // missing errorEstimate() test

               // skewness
               expected = 0.0;
               tolerance = 1.0e-4;
               calculated = igs.skewness();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong skewness"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.skewness();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong skewness"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // kurtosis
               expected = 0.0;
               tolerance = 1.0e-1;
               calculated = igs.kurtosis();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong kurtosis"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.kurtosis();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong kurtosis"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // percentile
               expected = averages[i];
               tolerance = (expected == 0.0 ? 1.0e-3 :
                                              Math.Abs(expected * 1.0e-3));
               calculated = igs.gaussianPercentile(0.5);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong Gaussian percentile"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.gaussianPercentile(0.5);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong Gaussian percentile"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.percentile(0.5);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong percentile"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);



               // potential upside
               double upper_tail = averages[i] + 2.0 * sigmas[j],
                    lower_tail = averages[i] - 2.0 * sigmas[j];
               double twoSigma = cumulative.value(upper_tail);

               expected = Math.Max(upper_tail, 0.0);
               tolerance = (expected == 0.0 ? 1.0e-3 :
                                              Math.Abs(expected * 1.0e-3));
               calculated = igs.gaussianPotentialUpside(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong Gaussian potential upside"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.gaussianPotentialUpside(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong Gaussian potential upside"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.potentialUpside(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong potential upside"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // just to check that GaussianStatistics<StatsHolder> does work
               StatsHolder h = new StatsHolder(s.mean(), s.standardDeviation());
               GenericGaussianStatistics<StatsHolder> test = new GenericGaussianStatistics<StatsHolder>(h);
               expected = s.gaussianPotentialUpside(twoSigma);
               calculated = test.gaussianPotentialUpside(twoSigma);
               if (calculated != expected)
                  Assert.Fail("GenericGaussianStatistics<StatsHolder> fails"
                              + "\n  calculated: " + calculated
                              + "\n  expected: " + expected);


               // value-at-risk
               expected = -Math.Min(lower_tail, 0.0);
               tolerance = (expected == 0.0 ? 1.0e-3 :
                                              Math.Abs(expected * 1.0e-3));
               calculated = igs.gaussianValueAtRisk(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong Gaussian value-at-risk"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.gaussianValueAtRisk(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong Gaussian value-at-risk"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.valueAtRisk(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong value-at-risk"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);

               if (averages[i] > 0.0 && sigmas[j] < averages[i])
               {
                  // no data will miss the targets:
                  // skip the rest of this iteration
                  igs.reset();
                  s.reset();
                  continue;
               }


               // expected shortfall
               expected = -Math.Min(averages[i]
                                          - sigmas[j] * sigmas[j]
                                          * normal.value(lower_tail) / (1.0 - twoSigma),
                                          0.0);
               tolerance = (expected == 0.0 ? 1.0e-4
                                            : Math.Abs(expected) * 1.0e-2);
               calculated = igs.gaussianExpectedShortfall(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong Gaussian expected shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.gaussianExpectedShortfall(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong Gaussian expected shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.expectedShortfall(twoSigma);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong expected shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // shortfall
               expected = 0.5;
               tolerance = (expected == 0.0 ? 1.0e-3 :
                                              Math.Abs(expected * 1.0e-3));
               calculated = igs.gaussianShortfall(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong Gaussian shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.gaussianShortfall(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong Gaussian shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.shortfall(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // average shortfall
               expected = sigmas[j] / Math.Sqrt(2.0 * Const.M_PI) * 2.0;
               tolerance = expected * 1.0e-3;
               calculated = igs.gaussianAverageShortfall(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong Gaussian average shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.gaussianAverageShortfall(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong Gaussian average shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.averageShortfall(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: wrong average shortfall"
                             + " for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // regret
               expected = sigmas[j] * sigmas[j];
               tolerance = expected * 1.0e-1;
               calculated = igs.gaussianRegret(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong Gaussian regret(" + averages[i] + ") "
                             + "for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.gaussianRegret(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: "
                             + "wrong Gaussian regret(" + averages[i] + ") "
                             + "for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = s.regret(averages[i]);
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("RiskStatistics: "
                             + "wrong regret(" + averages[i] + ") "
                             + "for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);


               // downsideVariance
               expected = s.downsideVariance();
               tolerance = (expected == 0.0 ? 1.0e-3 :
                                              Math.Abs(expected * 1.0e-3));
               calculated = igs.downsideVariance();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong downside variance"
                             + "for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);
               calculated = igs.gaussianDownsideVariance();
               if (Math.Abs(calculated - expected) > tolerance)
                  Assert.Fail("IncrementalGaussianStatistics: "
                             + "wrong Gaussian downside variance"
                             + "for N(" + averages[i] + ", "
                             + sigmas[j] + ")\n"
                             + "    calculated: " + calculated + "\n"
                             + "    expected:   " + expected + "\n"
                             + "    tolerance:  " + tolerance);

               // downsideVariance
               if (averages[i] == 0.0)
               {
                  expected = sigmas[j] * sigmas[j];
                  tolerance = expected * 1.0e-3;
                  calculated = igs.downsideVariance();
                  if (Math.Abs(calculated - expected) > tolerance)
                     Assert.Fail("IncrementalGaussianStatistics: "
                                + "wrong downside variance"
                                + "for N(" + averages[i] + ", "
                                + sigmas[j] + ")\n"
                                + "    calculated: " + calculated + "\n"
                                + "    expected:   " + expected + "\n"
                                + "    tolerance:  " + tolerance);
                  calculated = igs.gaussianDownsideVariance();
                  if (Math.Abs(calculated - expected) > tolerance)
                     Assert.Fail("IncrementalGaussianStatistics: "
                                + "wrong Gaussian downside variance"
                                + "for N(" + averages[i] + ", "
                                + sigmas[j] + ")\n"
                                + "    calculated: " + calculated + "\n"
                                + "    expected:   " + expected + "\n"
                                + "    tolerance:  " + tolerance);
                  calculated = s.downsideVariance();
                  if (Math.Abs(calculated - expected) > tolerance)
                     Assert.Fail("RiskStatistics: wrong downside variance"
                                + "for N(" + averages[i] + ", "
                                + sigmas[j] + ")\n"
                                + "    calculated: " + calculated + "\n"
                                + "    expected:   " + expected + "\n"
                                + "    tolerance:  " + tolerance);
                  calculated = s.gaussianDownsideVariance();
                  if (Math.Abs(calculated - expected) > tolerance)
                     Assert.Fail("RiskStatistics: wrong Gaussian downside variance"
                                + "for N(" + averages[i] + ", "
                                + sigmas[j] + ")\n"
                                + "    calculated: " + calculated + "\n"
                                + "    expected:   " + expected + "\n"
                                + "    tolerance:  " + tolerance);
               }

               igs.reset();
               s.reset();
            }
         }
      }

   }
}
