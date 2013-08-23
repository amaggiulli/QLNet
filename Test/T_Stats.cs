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

namespace Test2008
{
   [TestClass()]
   public class T_Stats
   {

      double[] data = { 3.0, 4.0, 5.0, 2.0, 3.0, 4.0, 5.0, 6.0, 4.0, 7.0 };
      double[] weights = { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

      [TestMethod()]
      public void testStatistics()
      {
         check<IncrementalStatistics>("IncrementalStatistics");
         check<RiskStatistics>("Statistics");
      }

      [TestMethod()]
      public void testSequenceStatistics()
      {
         //("Testing sequence statistics...");

         checkSequence<IncrementalStatistics>("IncrementalStatistics", 5);
         checkSequence<RiskStatistics>("Statistics", 5);
      }

      [TestMethod()]
      public void testConvergenceStatistics()
      {

         //("Testing convergence statistics...");

         checkConvergence<IncrementalStatistics>("IncrementalStatistics");
         checkConvergence<RiskStatistics>("Statistics");
      }

      void check<S>(string name) where S : IGeneralStatistics, new()
      {
         S s = new S();

         for (int i = 0; i < data.Length; i++)
            s.add(data[i], weights[i]);

         double calculated, expected;
         double tolerance;

         if (s.samples() != data.Length)
            Assert.Fail(name + ": wrong number of samples\n"
                       + "    calculated: " + s.samples() + "\n"
                       + "    expected:   " + data.Length);

         expected = weights.Sum();
         calculated = s.weightSum();
         if (calculated != expected)
            Assert.Fail(name + ": wrong sum of weights\n"
                       + "    calculated: " + calculated + "\n"
                       + "    expected:   " + expected);

         expected = data.Min();
         calculated = s.min();
         if (calculated != expected)
            Assert.Fail(name + ": wrong minimum value\n"
                       + "    calculated: " + calculated + "\n"
                       + "    expected:   " + expected);

         expected = data.Max();
         calculated = s.max();
         if (calculated != expected)
            Assert.Fail(name + ": wrong maximum value\n"
                       + "    calculated: " + calculated + "\n"
                       + "    expected:   " + expected);

         expected = 4.3;
         tolerance = 1.0e-9;
         calculated = s.mean();
         if (Math.Abs(calculated - expected) > tolerance)
            Assert.Fail(name + ": wrong mean value\n"
                       + "    calculated: " + calculated + "\n"
                       + "    expected:   " + expected);

         expected = 2.23333333333;
         calculated = s.variance();
         if (Math.Abs(calculated - expected) > tolerance)
            Assert.Fail(name + ": wrong variance\n"
                       + "    calculated: " + calculated + "\n"
                       + "    expected:   " + expected);

         expected = 1.4944341181;
         calculated = s.standardDeviation();
         if (Math.Abs(calculated - expected) > tolerance)
            Assert.Fail(name + ": wrong standard deviation\n"
                       + "    calculated: " + calculated + "\n"
                       + "    expected:   " + expected);

         expected = 0.359543071407;
         calculated = s.skewness();
         if (Math.Abs(calculated - expected) > tolerance)
            Assert.Fail(name + ": wrong skewness\n"
                       + "    calculated: " + calculated + "\n"
                       + "    expected:   " + expected);

         expected = -0.151799637209;
         calculated = s.kurtosis();
         if (Math.Abs(calculated - expected) > tolerance)
            Assert.Fail(name + ": wrong kurtosis\n"
                       + "    calculated: " + calculated + "\n"
                       + "    expected:   " + expected);
      }

      void checkSequence<S>(string name, int dimension) where S : IGeneralStatistics, new()
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
            Assert.Fail("SequenceStatistics<" + name + ">: "
                       + "wrong number of samples\n"
                       + "    calculated: " + ss.samples() + "\n"
                       + "    expected:   " + data.Length);

         expected = weights.Sum();
         if (ss.weightSum() != expected)
            Assert.Fail("SequenceStatistics<" + name + ">: "
                       + "wrong sum of weights\n"
                       + "    calculated: " + ss.weightSum() + "\n"
                       + "    expected:   " + expected);

         expected = data.Min();
         calculated = ss.min();
         for (i = 0; i < dimension; i++)
         {
            if (calculated[i] != expected)
               Assert.Fail("SequenceStatistics<" + name + ">: "
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
               Assert.Fail("SequenceStatistics<" + name + ">: "
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
               Assert.Fail("SequenceStatistics<" + name + ">: "
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
               Assert.Fail("SequenceStatistics<" + name + ">: "
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
               Assert.Fail("SequenceStatistics<" + name + ">: "
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
               Assert.Fail("SequenceStatistics<" + name + ">: "
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
               Assert.Fail("SequenceStatistics<" + name + ">: "
                          + (i + 1) + " dimension: "
                          + "wrong kurtosis\n"
                          + "    calculated: " + calculated[i] + "\n"
                          + "    expected:   " + expected);
         }
      }

      void checkConvergence<S>(string name) where S : IGeneralStatistics, new()
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
            Assert.Fail("ConvergenceStatistics<" + name + ">: "
                       + "\nwrong convergence-table size"
                       + "\n    calculated: " + calculatedSize
                       + "\n    expected:   " + expectedSize1);

         const double expectedValue1 = 4.0;
         const double tolerance = 1.0e-9;
         double calculatedValue = stats.convergenceTable().Last().Value;
         if (Math.Abs(calculatedValue - expectedValue1) > tolerance)
            Assert.Fail("wrong last value in convergence table"
                       + "\n    calculated: " + calculatedValue
                       + "\n    expected:   " + expectedValue1);

         const int expectedSampleSize1 = 7;
         int calculatedSamples = stats.convergenceTable().Last().Key;
         if (calculatedSamples != expectedSampleSize1)
            Assert.Fail("wrong number of samples in convergence table"
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
            Assert.Fail("wrong convergence-table size"
                       + "\n    calculated: " + calculatedSize
                       + "\n    expected:   " + expectedSize2);

         const double expectedValue2 = 2.0;
         calculatedValue = stats.convergenceTable().Last().Value;
         if (Math.Abs(calculatedValue - expectedValue2) > tolerance)
            Assert.Fail("wrong last value in convergence table"
                       + "\n    calculated: " + calculatedValue
                       + "\n    expected:   " + expectedValue2);

         const int expectedSampleSize2 = 3;
         calculatedSamples = stats.convergenceTable().Last().Key;
         if (calculatedSamples != expectedSampleSize2)
            Assert.Fail("wrong number of samples in convergence table"
                       + "\n    calculated: " + calculatedSamples
                       + "\n    expected:   " + expectedSampleSize2);
      }
   }
}
