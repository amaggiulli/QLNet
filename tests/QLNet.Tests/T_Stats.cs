/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_Stats
   {

      double[] data = { 3.0, 4.0, 5.0, 2.0, 3.0, 4.0, 5.0, 6.0, 4.0, 7.0 };
      double[] weights = { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testStatistics()
      {
         check<IncrementalStatistics>("IncrementalStatistics");
         check<RiskStatistics>("Statistics");
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSequenceStatistics()
      {
         //("Testing sequence statistics...");

         checkSequence<IncrementalStatistics>("IncrementalStatistics", 5);
         checkSequence<RiskStatistics>("Statistics", 5);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testConvergenceStatistics()
      {

         //("Testing convergence statistics...");

         checkConvergence<IncrementalStatistics>("IncrementalStatistics");
         checkConvergence<RiskStatistics>("Statistics");
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testIncrementalStatistics()
      {
         // Testing incremental statistics

         MersenneTwisterUniformRng mt = new MersenneTwisterUniformRng(42);

         IncrementalStatistics stat = new IncrementalStatistics();

         for (int i = 0; i < 500000; ++i)
         {
            double x = 2.0 * (mt.nextReal() - 0.5) * 1234.0;
            double w = mt.nextReal();
            stat.add(x, w);
         }

         if (stat.samples() != 500000)
            QAssert.Fail("stat.samples()  (" + stat.samples() + ") can not be reproduced against cached result (" + 500000 + ")");

         TEST_INC_STAT(stat.weightSum(), 2.5003623600676749e+05);
         TEST_INC_STAT(stat.mean(), 4.9122325964293845e-01);
         TEST_INC_STAT(stat.variance(), 5.0706503959683329e+05);
         TEST_INC_STAT(stat.standardDeviation(), 7.1208499464378076e+02);
         TEST_INC_STAT(stat.errorEstimate(), 1.0070402569876076e+00);
         TEST_INC_STAT(stat.skewness(), -1.7360169326720038e-03);
         TEST_INC_STAT(stat.kurtosis(), -1.1990742562085395e+00);
         TEST_INC_STAT(stat.min(), -1.2339945045639761e+03);
         TEST_INC_STAT(stat.max(), 1.2339958308008499e+03);
         TEST_INC_STAT(stat.downsideVariance(), 5.0786776146975247e+05);
         TEST_INC_STAT(stat.downsideDeviation(), 7.1264841364431061e+02);


         // This is a test for numerical stability, actual implementation fails

         //InverseCumulativeRng<MersenneTwisterUniformRng,InverseCumulativeNormal> normal_gen =
         //   new InverseCumulativeRng<MersenneTwisterUniformRng, InverseCumulativeNormal>(mt);

         //IncrementalStatistics stat2 = new IncrementalStatistics();

         //for (int i = 0; i < 500000; ++i)
         //{
         //   double x = normal_gen.next().value * 1E-1 + 1E8;
         //   double w = 1.0;
         //   stat2.add(x, w);
         //}

         //double tol = 1E-5;

         //if(Math.Abs( stat2.variance() - 1E-2 ) > tol)
         //   QAssert.Fail("variance (" + stat2.variance() + ") out of expected range " + 1E-2 + " +- " + tol);

      }

      public void TEST_INC_STAT(double expr, double expected)
      {
         if (!Utils.close_enough(expr, expected))
            QAssert.Fail(" (" + expr + ") can not be reproduced against cached result (" + expected + ")");

      }
      void check<S>(string name) where S : IGeneralStatistics, new ()
      {
         S s = FastActivator<S>.Create();

         for (int i = 0; i < data.Length; i++)
            s.add(data[i], weights[i]);

         double calculated, expected;
         double tolerance;

         if (s.samples() != data.Length)
            QAssert.Fail(name + ": wrong number of samples\n"
                         + "    calculated: " + s.samples() + "\n"
                         + "    expected:   " + data.Length);

         expected = weights.Sum();
         calculated = s.weightSum();
         if (calculated != expected)
            QAssert.Fail(name + ": wrong sum of weights\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + expected);

         expected = data.Min();
         calculated = s.min();
         if (calculated != expected)
            QAssert.Fail(name + ": wrong minimum value\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + expected);

         expected = data.Max();
         calculated = s.max();
         if (calculated != expected)
            QAssert.Fail(name + ": wrong maximum value\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + expected);

         expected = 4.3;
         tolerance = 1.0e-9;
         calculated = s.mean();
         if (Math.Abs(calculated - expected) > tolerance)
            QAssert.Fail(name + ": wrong mean value\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + expected);

         expected = 2.23333333333;
         calculated = s.variance();
         if (Math.Abs(calculated - expected) > tolerance)
            QAssert.Fail(name + ": wrong variance\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + expected);

         expected = 1.4944341181;
         calculated = s.standardDeviation();
         if (Math.Abs(calculated - expected) > tolerance)
            QAssert.Fail(name + ": wrong standard deviation\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + expected);

         expected = 0.359543071407;
         calculated = s.skewness();
         if (Math.Abs(calculated - expected) > tolerance)
            QAssert.Fail(name + ": wrong skewness\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + expected);

         expected = -0.151799637209;
         calculated = s.kurtosis();
         if (Math.Abs(calculated - expected) > tolerance)
            QAssert.Fail(name + ": wrong kurtosis\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + expected);
      }

      void checkSequence<S>(string name, int dimension) where S : IGeneralStatistics, new ()
      {

         GenericSequenceStatistics<S> ss = new GenericSequenceStatistics<S>(dimension);
         int i;
         for (i = 0; i < data.Length; i++)
         {
            List<double> temp = new InitializedList<double>(dimension, data[i]);
            ss.add(temp, weights[i]);
         }

         List<double> calculated;
         double expected, tolerance;

         if (ss.samples() != data.Length)
            QAssert.Fail("SequenceStatistics<" + name + ">: "
                         + "wrong number of samples\n"
                         + "    calculated: " + ss.samples() + "\n"
                         + "    expected:   " + data.Length);

         expected = weights.Sum();
         if (ss.weightSum() != expected)
            QAssert.Fail("SequenceStatistics<" + name + ">: "
                         + "wrong sum of weights\n"
                         + "    calculated: " + ss.weightSum() + "\n"
                         + "    expected:   " + expected);

         expected = data.Min();
         calculated = ss.min();
         for (i = 0; i < dimension; i++)
         {
            if (calculated[i] != expected)
               QAssert.Fail("SequenceStatistics<" + name + ">: "
                            + (i + 1) + " dimension: "
                            + "wrong minimum value\n"
                            + "    calculated: " + calculated[i] + "\n"
                            + "    expected:   " + expected);
         }

         expected = data.Max();
         calculated = ss.max();
         for (i = 0; i < dimension; i++)
         {
            if (calculated[i] != expected)
               QAssert.Fail("SequenceStatistics<" + name + ">: "
                            + (i + 1) + " dimension: "
                            + "wrong maximun value\n"
                            + "    calculated: " + calculated[i] + "\n"
                            + "    expected:   " + expected);
         }

         expected = 4.3;
         tolerance = 1.0e-9;
         calculated = ss.mean();
         for (i = 0; i < dimension; i++)
         {
            if (Math.Abs(calculated[i] - expected) > tolerance)
               QAssert.Fail("SequenceStatistics<" + name + ">: "
                            + (i + 1) + " dimension: "
                            + "wrong mean value\n"
                            + "    calculated: " + calculated[i] + "\n"
                            + "    expected:   " + expected);
         }

         expected = 2.23333333333;
         calculated = ss.variance();
         for (i = 0; i < dimension; i++)
         {
            if (Math.Abs(calculated[i] - expected) > tolerance)
               QAssert.Fail("SequenceStatistics<" + name + ">: "
                            + (i + 1) + " dimension: "
                            + "wrong variance\n"
                            + "    calculated: " + calculated[i] + "\n"
                            + "    expected:   " + expected);
         }

         expected = 1.4944341181;
         calculated = ss.standardDeviation();
         for (i = 0; i < dimension; i++)
         {
            if (Math.Abs(calculated[i] - expected) > tolerance)
               QAssert.Fail("SequenceStatistics<" + name + ">: "
                            + (i + 1) + " dimension: "
                            + "wrong standard deviation\n"
                            + "    calculated: " + calculated[i] + "\n"
                            + "    expected:   " + expected);
         }

         expected = 0.359543071407;
         calculated = ss.skewness();
         for (i = 0; i < dimension; i++)
         {
            if (Math.Abs(calculated[i] - expected) > tolerance)
               QAssert.Fail("SequenceStatistics<" + name + ">: "
                            + (i + 1) + " dimension: "
                            + "wrong skewness\n"
                            + "    calculated: " + calculated[i] + "\n"
                            + "    expected:   " + expected);
         }

         expected = -0.151799637209;
         calculated = ss.kurtosis();
         for (i = 0; i < dimension; i++)
         {
            if (Math.Abs(calculated[i] - expected) > tolerance)
               QAssert.Fail("SequenceStatistics<" + name + ">: "
                            + (i + 1) + " dimension: "
                            + "wrong kurtosis\n"
                            + "    calculated: " + calculated[i] + "\n"
                            + "    expected:   " + expected);
         }
      }

      void checkConvergence<S>(string name) where S : IGeneralStatistics, new ()
      {

         ConvergenceStatistics<S> stats = new ConvergenceStatistics<S>();

         stats.add(1.0);
         stats.add(2.0);
         stats.add(3.0);
         stats.add(4.0);
         stats.add(5.0);
         stats.add(6.0);
         stats.add(7.0);
         stats.add(8.0);

         const int expectedSize1 = 3;
         int calculatedSize = stats.convergenceTable().Count;
         if (calculatedSize != expectedSize1)
            QAssert.Fail("ConvergenceStatistics<" + name + ">: "
                         + "\nwrong convergence-table size"
                         + "\n    calculated: " + calculatedSize
                         + "\n    expected:   " + expectedSize1);

         const double expectedValue1 = 4.0;
         const double tolerance = 1.0e-9;
         double calculatedValue = stats.convergenceTable().Last().Value;
         if (Math.Abs(calculatedValue - expectedValue1) > tolerance)
            QAssert.Fail("wrong last value in convergence table"
                         + "\n    calculated: " + calculatedValue
                         + "\n    expected:   " + expectedValue1);

         const int expectedSampleSize1 = 7;
         int calculatedSamples = stats.convergenceTable().Last().Key;
         if (calculatedSamples != expectedSampleSize1)
            QAssert.Fail("wrong number of samples in convergence table"
                         + "\n    calculated: " + calculatedSamples
                         + "\n    expected:   " + expectedSampleSize1);

         stats.reset();
         stats.add(1.0);
         stats.add(2.0);
         stats.add(3.0);
         stats.add(4.0);

         const int expectedSize2 = 2;
         calculatedSize = stats.convergenceTable().Count;
         if (calculatedSize != expectedSize2)
            QAssert.Fail("wrong convergence-table size"
                         + "\n    calculated: " + calculatedSize
                         + "\n    expected:   " + expectedSize2);

         const double expectedValue2 = 2.0;
         calculatedValue = stats.convergenceTable().Last().Value;
         if (Math.Abs(calculatedValue - expectedValue2) > tolerance)
            QAssert.Fail("wrong last value in convergence table"
                         + "\n    calculated: " + calculatedValue
                         + "\n    expected:   " + expectedValue2);

         const int expectedSampleSize2 = 3;
         calculatedSamples = stats.convergenceTable().Last().Key;
         if (calculatedSamples != expectedSampleSize2)
            QAssert.Fail("wrong number of samples in convergence table"
                         + "\n    calculated: " + calculatedSamples
                         + "\n    expected:   " + expectedSampleSize2);
      }
   }
}
