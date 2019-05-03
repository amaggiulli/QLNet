//  Copyright (C) 2008-2019 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System;
using System.Collections.Generic;
#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;
using System.Diagnostics;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_TimeGrid
   {
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testConstructorAdditionalSteps()
      {
         // Testing TimeGrid construction with additional steps
         List<double> test_times = new List<double> {1.0, 2.0, 4.0};
         TimeGrid tg = new TimeGrid(test_times, 8);

         // Expect 8 evenly sized steps over the interval [0, 4].
         List<double> expected_times = new List<double>
         {
            0.0,
            0.5,
            1.0,
            1.5,
            2.0,
            2.5,
            3.0,
            3.5,
            4.0
         };

         QAssert.CollectionAreEqual(tg.Times(), expected_times);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testConstructorMandatorySteps()
      {
         // Testing TimeGrid construction with only mandatory points
         List<double> test_times = new List<double> {0.0, 1.0, 2.0, 4.0};
         TimeGrid tg = new TimeGrid(test_times);

         // Time grid must include all times from passed iterator.
         // Further no additional times can be added.
         QAssert.CollectionAreEqual(tg.Times(), test_times);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testConstructorEvenSteps()
      {
         // Testing TimeGrid construction with n evenly spaced points

         double end_time = 10;
         int steps = 5;
         TimeGrid tg = new TimeGrid(end_time, steps);
         List<double> expected_times = new List<double>
         {
            0.0,
            2.0,
            4.0,
            6.0,
            8.0,
            10.0
         };

         QAssert.CollectionAreEqual(tg.Times(), expected_times);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testConstructorEmptyIterator()
      {
         // Testing that the TimeGrid constructor raises an error for empty iterators

         List<double> times = new List<double>();

         QAssert.ThrowsException<ArgumentException>(() => new TimeGrid(times));
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testConstructorNegativeValuesInIterator()
      {
         // Testing that the TimeGrid constructor raises an error for negative time values
         List<double> times = new List<double> {-3.0, 1.0, 4.0, 5.0};
         QAssert.ThrowsException<ArgumentException>(() => new TimeGrid(times));
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testClosestIndex()
      {
         // Testing that the returned index is closest to the requested time
         List<double> test_times = new List<double> {1.0, 2.0, 5.0};
         TimeGrid tg = new TimeGrid(test_times);
         int expected_index = 3;

         QAssert.IsTrue(tg.closestIndex(4) == expected_index,
                        "Expected index: " + expected_index + ", which does not match " +
                        "the returned index: " + tg.closestIndex(4));
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testClosestTime()
      {
         // Testing that the returned time matches the requested index
         List<double> test_times = new List<double> {1.0, 2.0, 5.0};
         TimeGrid tg = new TimeGrid(test_times);
         int expected_time = 5;

         QAssert.IsTrue(tg.closestTime(4).IsEqual(expected_time),
                        "Expected time of: " + expected_time + ", which does not match " +
                        "the returned time: " + tg.closestTime(4));
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testMandatoryTimes()
      {
         // Testing that mandatory times are recalled correctly
         List<double> test_times = new List<double> {1.0, 2.0, 4.0};
         TimeGrid tg = new TimeGrid(test_times, 8);

         // Mandatory times are those provided by the original iterator.
         List<double> tg_mandatory_times = tg.mandatoryTimes();
         QAssert.CollectionAreEqual(tg_mandatory_times, test_times);
      }
   }
}
