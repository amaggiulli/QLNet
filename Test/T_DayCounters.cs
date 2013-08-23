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
      [TestMethod()]
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
               Assert.Fail(dayCounter.name() + "period: " + d1 + " to " + d2 +
                           "    calculated: " + calculated + "    expected:   " + testCases[i]._result); 
            }
         }
      }

      [TestMethod()]
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
                      Assert.Fail ("from " + start + " to " + end +
                                   "Calculated: " + calculated +
                                   "Expected:   " + expected[i]);
                  }
              }
          }

      }
      [TestMethod()]
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
                      Assert.Fail("from " + start + " to " + end +
                                  "Calculated: " + calculated +
                                  "Expected:   " + expected[i]);
                  }
              }
          }

      }

   }
}
