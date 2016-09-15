/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <https://github.com/amaggiulli/qlnetLicense.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
   public class T_DayCounters
   {
      public struct SingleCase
      {
         public SingleCase(ActualActual.Convention convention, Date start, Date end, Date refStart, Date refEnd, double result)
         {
            _convention = convention;
            _start = start;
            _end = end;
            _refStart = refStart;
            _refEnd = refEnd;
            _result = result;
         }
         public SingleCase(ActualActual.Convention convention, Date start, Date end, double result)
         {
            _convention = convention;
            _start = start;
            _end = end;
            _refStart = new Date();
            _refEnd = new Date();
            _result = result;
         }
         public ActualActual.Convention _convention;
         public Date _start;
         public Date _end;
         public Date _refStart;
         public Date _refEnd;
         public double _result;
      }
#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testActualActual()
      {
         SingleCase[] testCases = 
         {
            // first example
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(1,Month.November,2003), new Date(1,Month.May,2004),
                           0.497724380567),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(1,Month.November,2003), new Date(1,Month.May,2004),
                           new Date(1,Month.November,2003), new Date(1,Month.May,2004),
                           0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(1,Month.November,2003), new Date(1,Month.May,2004),
                           0.497267759563),
            // short first calculation period (first period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(1,Month.February,1999), new Date(1,Month.July,1999),
                           0.410958904110),
            new SingleCase(ActualActual.Convention.ISMA,
                   new Date(1,Month.February,1999), new Date(1,Month.July,1999),
                   new Date(1,Month.July,1998), new Date(1,Month.July,1999),
                   0.410958904110),
            new SingleCase(ActualActual.Convention.AFB,
                   new Date(1,Month.February,1999), new Date(1,Month.July,1999),
                   0.410958904110),
            // short first calculation period (second period)
            new SingleCase(ActualActual.Convention.ISDA,
                   new Date(1,Month.July,1999), new Date(1,Month.July,2000),
                   1.001377348600),
            new SingleCase(ActualActual.Convention.ISMA,
                   new Date(1,Month.July,1999), new Date(1,Month.July,2000),
                   new Date(1,Month.July,1999), new Date(1,Month.July,2000),
                   1.000000000000),
            new SingleCase(ActualActual.Convention.AFB,
                   new Date(1,Month.July,1999), new Date(1,Month.July,2000),
                   1.000000000000),
            // long first calculation period (first period)
            new SingleCase(ActualActual.Convention.ISDA,
                   new Date(15,Month.August,2002), new Date(15,Month.July,2003),
                   0.915068493151),
            new SingleCase(ActualActual.Convention.ISMA,
                   new Date(15,Month.August,2002), new Date(15,Month.July,2003),
                   new Date(15,Month.January,2003), new Date(15,Month.July,2003),
                   0.915760869565),
            new SingleCase(ActualActual.Convention.AFB,
                   new Date(15,Month.August,2002), new Date(15,Month.July,2003),
                   0.915068493151),
            // long first calculation period (second period)
            /* Warning: the ISDA case is in disagreement with mktc1198.pdf */
            new SingleCase(ActualActual.Convention.ISDA,
                   new Date(15,Month.July,2003), new Date(15,Month.January,2004),
                   0.504004790778),
            new SingleCase(ActualActual.Convention.ISMA,
                   new Date(15,Month.July,2003), new Date(15,Month.January,2004),
                   new Date(15,Month.July,2003), new Date(15,Month.January,2004),
                   0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                   new Date(15,Month.July,2003), new Date(15,Month.January,2004),
                   0.504109589041),
            // short final calculation period (penultimate period)
            new SingleCase(ActualActual.Convention.ISDA,
                   new Date(30,Month.July,1999), new Date(30,Month.January,2000),
                   0.503892506924),
            new SingleCase(ActualActual.Convention.ISMA,
                   new Date(30,Month.July,1999), new Date(30,Month.January,2000),
                   new Date(30,Month.July,1999), new Date(30,Month.January,2000),
                   0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                   new Date(30,Month.July,1999), new Date(30,Month.January,2000),
                   0.504109589041),
            // short final calculation period (final period)
            new SingleCase(ActualActual.Convention.ISDA,
                   new Date(30,Month.January,2000), new Date(30,Month.June,2000),
                   0.415300546448),
            new SingleCase(ActualActual.Convention.ISMA,
                   new Date(30,Month.January,2000), new Date(30,Month.June,2000),
                   new Date(30,Month.January,2000), new Date(30,Month.July,2000),
                   0.417582417582),
            new SingleCase(ActualActual.Convention.AFB,
                   new Date(30,Month.January,2000), new Date(30,Month.June,2000),
                   0.41530054644)
             };

         int n = testCases.Length; /// sizeof(SingleCase);
         for (int i = 0; i < n; i++)
         {
            ActualActual dayCounter = new ActualActual(testCases[i]._convention);
            Date d1 = testCases[i]._start;
            Date d2 = testCases[i]._end;
            Date rd1 = testCases[i]._refStart;
            Date rd2 = testCases[i]._refEnd;
            double calculated = dayCounter.yearFraction(d1, d2, rd1, rd2);

            if (Math.Abs(calculated - testCases[i]._result) > 1.0e-10)
            {
               QAssert.Fail(dayCounter.name() + "period: " + d1 + " to " + d2 +
                           "    calculated: " + calculated + "    expected:   " + testCases[i]._result); 
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testSimple()
      {
         Period[] p = { new Period(3, TimeUnit.Months), new Period(6, TimeUnit.Months), new Period(1, TimeUnit.Years) };
         double[] expected = { 0.25, 0.5, 1.0 };
         int n = p.Length;

         // 4 years should be enough
         Date first= new Date(1,Month.January,2002), last = new Date(31,Month.December,2005);
         DayCounter dayCounter = new SimpleDayCounter();

          for (Date start = first; start <= last; start++) 
          {
              for (int i=0; i<n; i++) 
              {
                  Date end = start + p[i];
                  double calculated = dayCounter.yearFraction(start,end,null ,null );
                  if (Math.Abs(calculated-expected[i]) > 1.0e-12) 
                  {
                      QAssert.Fail ("from " + start + " to " + end +
                                   "Calculated: " + calculated +
                                   "Expected:   " + expected[i]);
                  }
              }
          }

      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testOne()
      {
          Period[] p = { new Period(3,TimeUnit.Months), new Period(6,TimeUnit.Months), new Period(1,TimeUnit.Years) };
          double[] expected = { 1.0, 1.0, 1.0 };
          int n = p.Length;

          // 1 years should be enough
          Date first = new Date(1,Month.January,2004), last= new Date (31,Month.December,2004);
          DayCounter dayCounter = new OneDayCounter();

          for (Date start = first; start <= last; start++) 
          {
              for (int i=0; i<n; i++) 
              {
                  Date end = start + p[i];
                  double calculated = dayCounter.yearFraction(start,end,null,null);
                  if (Math.Abs(calculated-expected[i]) > 1.0e-12) 
                  {
                      QAssert.Fail("from " + start + " to " + end +
                                  "Calculated: " + calculated +
                                  "Expected:   " + expected[i]);
                  }
              }
          }

      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testBusiness252() 
      {
         // Testing business/252 day counter

         List<Date> testDates = new List<Date>();
         testDates.Add(new Date(1,Month.February,2002));
         testDates.Add(new Date(4,Month.February,2002));
         testDates.Add(new Date(16,Month.May,2003));
         testDates.Add(new Date(17,Month.December,2003));
         testDates.Add(new Date(17,Month.December,2004));
         testDates.Add(new Date(19,Month.December,2005));
         testDates.Add(new Date(2,Month.January,2006));
         testDates.Add(new Date(13,Month.March,2006));
         testDates.Add(new Date(15,Month.May,2006));
         testDates.Add(new Date(17,Month.March,2006));
         testDates.Add(new Date(15,Month.May,2006));
         testDates.Add(new Date(26,Month.July,2006));
         testDates.Add(new Date(28,Month.June,2007));
         testDates.Add(new Date(16,Month.September,2009));
         testDates.Add(new Date(26,Month.July,2016));

         double[] expected = {
            0.0039682539683,
            1.2738095238095,
            0.6031746031746,
            0.9960317460317,
            1.0000000000000,
            0.0396825396825,
            0.1904761904762,
            0.1666666666667,
            -0.1507936507937,
            0.1507936507937,
            0.2023809523810,
            0.912698412698,
            2.214285714286,
            6.84126984127
            };

         DayCounter dayCounter1 = new Business252(new Brazil());

         double calculated;

         for (int i=1; i<testDates.Count; i++) 
         {
            calculated = dayCounter1.yearFraction(testDates[i-1],testDates[i]);
            if (Math.Abs(calculated-expected[i-1]) > 1.0e-12) 
            {
               QAssert.Fail("from " + testDates[i-1]
                                   + " to " + testDates[i] + ":\n"
                                   + "    calculated: " + calculated + "\n"
                                   + "    expected:   " + expected[i-1]);
            }
         }

         DayCounter dayCounter2 = new Business252();

         for (int i=1; i<testDates.Count; i++) 
         {
            calculated = dayCounter2.yearFraction(testDates[i-1],testDates[i]);
            if (Math.Abs(calculated-expected[i-1]) > 1.0e-12) 
            {
               QAssert.Fail("from " + testDates[i-1]
                                   + " to " + testDates[i] + ":\n"
                                   + "    calculated: " + calculated + "\n"
                                   + "    expected:   " + expected[i-1]);
         
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testThirty360_BondBasis() 
      {
         // Testing thirty/360 day counter (Bond Basis)
         // http://www.isda.org/c_and_a/docs/30-360-2006ISDADefs.xls
         // Source: 2006 ISDA Definitions, Sec. 4.16 (f)
         // 30/360 (or Bond Basis)

         DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
         List<Date> testStartDates = new List<Date>();
         List<Date> testEndDates = new List<Date>();
         int calculated;

         // ISDA - Example 1: End dates do not involve the last day of February
         testStartDates.Add(new Date(20, Month.August, 2006));   testEndDates.Add(new Date(20, Month.February, 2007));
         testStartDates.Add(new Date(20, Month.February, 2007)); testEndDates.Add(new Date(20, Month.August, 2007));
         testStartDates.Add(new Date(20, Month.August, 2007));   testEndDates.Add(new Date(20, Month.February, 2008));
         testStartDates.Add(new Date(20, Month.February, 2008)); testEndDates.Add(new Date(20, Month.August, 2008));
         testStartDates.Add(new Date(20, Month.August, 2008));   testEndDates.Add(new Date(20, Month.February, 2009));
         testStartDates.Add(new Date(20, Month.February, 2009)); testEndDates.Add(new Date(20, Month.August, 2009));

         // ISDA - Example 2: End dates include some end-February dates
         testStartDates.Add(new Date(31, Month.August, 2006));   testEndDates.Add(new Date(28, Month.February, 2007));
         testStartDates.Add(new Date(28, Month.February, 2007)); testEndDates.Add(new Date(31, Month.August, 2007));
         testStartDates.Add(new Date(31, Month.August, 2007));   testEndDates.Add(new Date(29, Month.February, 2008));
         testStartDates.Add(new Date(29, Month.February, 2008)); testEndDates.Add(new Date(31, Month.August, 2008));
         testStartDates.Add(new Date(31, Month.August, 2008));   testEndDates.Add(new Date(28, Month.February, 2009));
         testStartDates.Add(new Date(28, Month.February, 2009)); testEndDates.Add(new Date(31, Month.August, 2009));
                                                                             
         //// ISDA - Example 3: Miscellaneous calculations
         testStartDates.Add(new Date(31, Month.January, 2006));   testEndDates.Add(new Date(28, Month.February, 2006));
         testStartDates.Add(new Date(30, Month.January, 2006));   testEndDates.Add(new Date(28, Month.February, 2006));
         testStartDates.Add(new Date(28, Month.February, 2006));  testEndDates.Add(new Date(3,  Month.March, 2006));
         testStartDates.Add(new Date(14, Month.February, 2006));  testEndDates.Add(new Date(28, Month.February, 2006));
         testStartDates.Add(new Date(30, Month.September, 2006)); testEndDates.Add(new Date(31, Month.October, 2006));
         testStartDates.Add(new Date(31, Month.October, 2006));   testEndDates.Add(new Date(28, Month.November, 2006));
         testStartDates.Add(new Date(31, Month.August, 2007));    testEndDates.Add(new Date(28, Month.February, 2008));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(28, Month.August, 2008));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(30, Month.August, 2008));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(31, Month.August, 2008));
         testStartDates.Add(new Date(26, Month.February, 2007));  testEndDates.Add(new Date(28, Month.February, 2008));
         testStartDates.Add(new Date(26, Month.February, 2007));  testEndDates.Add(new Date(29, Month.February, 2008));
         testStartDates.Add(new Date(29, Month.February, 2008));  testEndDates.Add(new Date(28, Month.February, 2009));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(30, Month.March, 2008));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(31, Month.March, 2008));

         int[] expected = { 180, 180, 180, 180, 180, 180,
                           178, 183, 179, 182, 178, 183,
                           28,  28,   5,  14,  30,  28,
                           178, 180, 182, 183, 362, 363,
                           359,  32,  33};

         for (int i = 0; i < testStartDates.Count; i++) 
         {
            calculated = dayCounter.dayCount(testStartDates[i], testEndDates[i]);
            if (calculated != expected[i]) 
            {
               QAssert.Fail("from " + testStartDates[i]
                                   + " to " + testEndDates[i] + ":\n"
                                   + "    calculated: " + calculated + "\n"
                                   + "    expected:   " + expected[i]);
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testThirty360_EurobondBasis() 
      {
         // Testing thirty/360 day counter (Eurobond Basis)
         // Source: ISDA 2006 Definitions 4.16 (g)
         // 30E/360 (or Eurobond Basis)
         // Based on ICMA (Rule 251) and FBF; this is the version of 30E/360 used by Excel

         DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.EurobondBasis);
         List<Date> testStartDates = new List<Date>();
         List<Date> testEndDates = new List<Date>();
         int calculated;

         // ISDA - Example 1: End dates do not involve the last day of February
         testStartDates.Add(new Date(20, Month.August, 2006));   testEndDates.Add(new Date(20, Month.February, 2007));
         testStartDates.Add(new Date(20, Month.February, 2007)); testEndDates.Add(new Date(20, Month.August, 2007));
         testStartDates.Add(new Date(20, Month.August, 2007));   testEndDates.Add(new Date(20, Month.February, 2008));
         testStartDates.Add(new Date(20, Month.February, 2008)); testEndDates.Add(new Date(20, Month.August, 2008));
         testStartDates.Add(new Date(20, Month.August, 2008));   testEndDates.Add(new Date(20, Month.February, 2009));
         testStartDates.Add(new Date(20, Month.February, 2009)); testEndDates.Add(new Date(20, Month.August, 2009));

         //// ISDA - Example 2: End dates include some end-February dates
         testStartDates.Add(new Date(28, Month.February, 2006)); testEndDates.Add(new Date(31, Month.August, 2006));
         testStartDates.Add(new Date(31, Month.August, 2006));   testEndDates.Add(new Date(28, Month.February, 2007));
         testStartDates.Add(new Date(28, Month.February, 2007)); testEndDates.Add(new Date(31, Month.August, 2007));
         testStartDates.Add(new Date(31, Month.August, 2007));   testEndDates.Add(new Date(29, Month.February, 2008));
         testStartDates.Add(new Date(29, Month.February, 2008)); testEndDates.Add(new Date(31, Month.August, 2008));
         testStartDates.Add(new Date(31, Month.August, 2008));   testEndDates.Add(new Date(28, Month.Feb, 2009));
         testStartDates.Add(new Date(28, Month.February, 2009)); testEndDates.Add(new Date(31, Month.August, 2009));
         testStartDates.Add(new Date(31, Month.August, 2009));   testEndDates.Add(new Date(28, Month.Feb, 2010));
         testStartDates.Add(new Date(28, Month.February, 2010)); testEndDates.Add(new Date(31, Month.August, 2010));
         testStartDates.Add(new Date(31, Month.August, 2010));   testEndDates.Add(new Date(28, Month.Feb, 2011));
         testStartDates.Add(new Date(28, Month.February, 2011)); testEndDates.Add(new Date(31, Month.August, 2011));
         testStartDates.Add(new Date(31, Month.August, 2011));   testEndDates.Add(new Date(29, Month.Feb, 2012));

         //// ISDA - Example 3: Miscellaneous calculations
         testStartDates.Add(new Date(31, Month.January, 2006));   testEndDates.Add(new Date(28, Month.February, 2006));
         testStartDates.Add(new Date(30, Month.January, 2006));   testEndDates.Add(new Date(28, Month.February, 2006));
         testStartDates.Add(new Date(28, Month.February, 2006));  testEndDates.Add(new Date(3,  Month.March, 2006));
         testStartDates.Add(new Date(14, Month.February, 2006));  testEndDates.Add(new Date(28, Month.February, 2006));
         testStartDates.Add(new Date(30, Month.September, 2006)); testEndDates.Add(new Date(31, Month.October, 2006));
         testStartDates.Add(new Date(31, Month.October, 2006));   testEndDates.Add(new Date(28, Month.November, 2006));
         testStartDates.Add(new Date(31, Month.August, 2007));    testEndDates.Add(new Date(28, Month.February, 2008));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(28, Month.August, 2008));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(30, Month.August, 2008));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(31, Month.August, 2008));
         testStartDates.Add(new Date(26, Month.February, 2007));  testEndDates.Add(new Date(28, Month.February, 2008));
         testStartDates.Add(new Date(26, Month.February, 2007));  testEndDates.Add(new Date(29, Month.February, 2008));
         testStartDates.Add(new Date(29, Month.February, 2008));  testEndDates.Add(new Date(28, Month.February, 2009));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(30, Month.March, 2008));
         testStartDates.Add(new Date(28, Month.February, 2008));  testEndDates.Add(new Date(31, Month.March, 2008));

         int[] expected = { 180, 180, 180, 180, 180, 180,
                           182, 178, 182, 179, 181, 178,
                           182, 178, 182, 178, 182, 179,
                           28,  28,   5,  14,  30,  28,
                           178, 180, 182, 182, 362, 363,
                           359,  32,  32 };

         for (int i = 0; i < testStartDates.Count; i++) 
         {
            calculated = dayCounter.dayCount(testStartDates[i], testEndDates[i]);
            if (calculated != expected[i]) 
            {
               QAssert.Fail("from " + testStartDates[i]
                                   + " to " + testEndDates[i] + ":\n"
                                   + "    calculated: " + calculated + "\n"
                                   + "    expected:   " + expected[i]);
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testIntraday() 
      {
         // Testing intraday behavior of day counter

         Date d1 = new Date(12, Month.February, 2015);
         Date d2 = new Date(14, Month.February, 2015, 12, 34, 17, 1);

         double tol = 100*Const.QL_EPSILON;

         DayCounter[] dayCounters = { new ActualActual(), new Actual365Fixed(), new Actual360() };

         for (int i=0; i < dayCounters.Length; ++i) 
         {
            DayCounter dc = dayCounters[i];

            double expected = ((12*60 + 34)*60 + 17 + 0.001)
                                 * dc.yearFraction(d1, d1+1)/86400
                                 + dc.yearFraction(d1, d1+2);

            QAssert.IsTrue( Math.Abs(dc.yearFraction(d1, d2) - expected) < tol,
                           "can not reproduce result for day counter " + dc.name());

            QAssert.IsTrue( Math.Abs(dc.yearFraction(d2, d1) + expected) < tol,
                           "can not reproduce result for day counter " + dc.name());
         }
      }
   }
}
