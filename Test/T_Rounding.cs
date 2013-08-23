/*
 Copyright (C) 2008 Andrea Maggiulli
  
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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;
using System.Diagnostics;

namespace TestSuite
{
   [TestClass()]
   public class T_Rounding
   {
      public struct TestCase
      {
         public double x;
         public int precision;
         public double closest;
         public double up;
         public double down;
         public double floor;
         public double ceiling;

         public TestCase(double x, int precision, double closest, double up, double down, double floor, double ceiling)
         {
            this.x = x;
            this.precision = precision;
            this.closest = closest;
            this.up = up;
            this.down = down;
            this.floor = floor;
            this.ceiling = ceiling;
         }

      }

      public static TestCase[] testData = { 
           new TestCase( 0.86313513, 5, 0.86314, 0.86314, 0.86313, 0.86314, 0.86313 ), 
           new TestCase( -7.64555346, 1, -7.6, -7.7, -7.6, -7.6, -7.6 ), 
           new TestCase(0.13961605, 2, 0.14, 0.14, 0.13, 0.14, 0.13 ), 
           new TestCase(0.14344179, 4, 0.1434, 0.1435, 0.1434, 0.1434, 0.1434 ), 
           new TestCase(-4.74315016, 2, -4.74, -4.75, -4.74, -4.74, -4.74 ), 
           new TestCase(-7.82772074, 5, -7.82772, -7.82773, -7.82772, -7.82772, -7.82772 ), 
           new TestCase( 2.74137947, 3, 2.741, 2.742, 2.741, 2.741, 2.741 ), 
           new TestCase(2.13056714, 1, 2.1, 2.2, 2.1, 2.1, 2.1 ), 
           new TestCase(-1.06228670, 1, -1.1, -1.1, -1.0, -1.0, -1.1 ), 
           new TestCase(8.29234094, 4, 8.2923, 8.2924, 8.2923, 8.2923, 8.2923 ), 
           new TestCase(7.90185598, 2, 7.90, 7.91, 7.90, 7.90, 7.90 ), 
           new TestCase(-0.26738058, 1, -0.3, -0.3, -0.2, -0.2, -0.3 ), 
           new TestCase(1.78128713, 1, 1.8, 1.8, 1.7, 1.8, 1.7 ), 
           new TestCase(4.23537260, 1, 4.2, 4.3, 4.2, 4.2, 4.2 ), 
           new TestCase(3.64369953, 4, 3.6437, 3.6437, 3.6436, 3.6437, 3.6436 ), 
           new TestCase(6.34542470, 2, 6.35, 6.35, 6.34, 6.35, 6.34 ),
           new TestCase(-0.84754962, 4, -0.8475, -0.8476, -0.8475, -0.8475, -0.8475 ), 
           new TestCase(4.60998652, 1, 4.6, 4.7, 4.6, 4.6, 4.6 ), 
           new TestCase(6.28794223, 3, 6.288, 6.288, 6.287, 6.288, 6.287 ), 
           new TestCase(7.89428221, 2, 7.89, 7.90, 7.89, 7.89, 7.89 ) 
        };

      [TestMethod()]
      public void testClosest()
      {
         for (int i = 0; i < testData.Length; i++)
         {
            int precision = testData[i].precision;
            ClosestRounding closest = new ClosestRounding(precision);
            double calculated = closest.Round(testData[i].x);
            double expected = testData[i].closest;
            if (!Utils.close(calculated, expected, 1))
               Assert.Fail("Original number: " + testData[i].x + "Expected: " + expected + "Calculated: " + calculated);
         }
      }
      [TestMethod()]
      public void testUp()
      {
         for (int i = 0; i < testData.Length; i++)
         {
            int digits = testData[i].precision;
            UpRounding up = new UpRounding(digits);
            double calculated = up.Round(testData[i].x);
            double expected = testData[i].up;
            if (!Utils.close(calculated, expected, 1))
               Assert.Fail("Original number: " + testData[i].x + "Expected: " + expected + "Calculated: " + calculated);
         }
      }

      [TestMethod()]
      public void testDown()
      {
         for (int i = 0; i < testData.Length; i++)
         {
            int digits = testData[i].precision;
            DownRounding down = new DownRounding(digits);
            double calculated = down.Round(testData[i].x);
            double expected = testData[i].down;
            if (!Utils.close(calculated, expected, 1))
               Assert.Fail("Original number: " + testData[i].x + "Expected: " + expected + "Calculated: " + calculated);
         }
      }

      [TestMethod()]
      public void testFloor()
      {
         for (int i = 0; i < testData.Length; i++)
         {
            int digits = testData[i].precision;
            FloorTruncation floor = new FloorTruncation(digits);
            double calculated = floor.Round(testData[i].x);
            double expected = testData[i].floor;
            if (!Utils.close(calculated, expected, 1))
               Assert.Fail("Original number: " + testData[i].x + "Expected: " + expected + "Calculated: " + calculated);
         }
      }

      [TestMethod()]
      public void testCeiling()
      {
         for (int i = 0; i < testData.Length; i++)
         {
            int digits = testData[i].precision;
            CeilingTruncation ceiling = new CeilingTruncation(digits);
            double calculated = ceiling.Round(testData[i].x);
            double expected = testData[i].ceiling;
            if (!Utils.close(calculated, expected, 1))
               Assert.Fail("Original number: " + testData[i].x + "Expected: " + expected + "Calculated: " + calculated);
         }
      }

   }
}
