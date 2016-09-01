/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   public class T_BusinessDayConvention
   {
      struct SingleCase 
      {
         public SingleCase( Calendar calendar_,
                   BusinessDayConvention convention_,
                   Date start_,
                   Period period_,
                   bool endOfMonth_,
                   Date result_)
         {
            calendar = calendar_; 
            convention = convention_; 
            start = start_; 
            period = period_; 
            endOfMonth = endOfMonth_; 
            result = result_;
         }
         public Calendar calendar;
         public BusinessDayConvention convention;
         public Date start;
         public Period period;
         public bool endOfMonth;
         public Date result;
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testConventions() 
      {
         // Testing business day conventions...

         SingleCase[] testCases = 
         {
              // Following
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Following, new Date(3,Month.February,2015), new Period(1,TimeUnit.Months), false, new Date(3,Month.March,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Following, new Date(3,Month.February,2015), new Period(4,TimeUnit.Days), false,   new Date(9,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Following, new Date(31,Month.January,2015), new Period(1,TimeUnit.Months), true,  new Date(27,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Following, new Date(31,Month.January,2015), new Period(1,TimeUnit.Months), false, new Date(2,Month.March,2015)),

              //ModifiedFollowing
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedFollowing, new Date(3,Month.February,2015), new Period(1,TimeUnit.Months), false, new Date(3,Month.March,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedFollowing, new Date(3,Month.February,2015), new Period(4,TimeUnit.Days), false,   new Date(9,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedFollowing, new Date(31,Month.January,2015), new Period(1,TimeUnit.Months), true,  new Date(27,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedFollowing, new Date(31,Month.January,2015), new Period(1,TimeUnit.Months), false, new Date(27,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedFollowing, new Date(25,Month.March,2015),   new Period(1,TimeUnit.Months), false, new Date(28,Month.April,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedFollowing, new Date(7,Month.February,2015), new Period(1,TimeUnit.Months), false, new Date(9,Month.March,2015)),

              //Preceding
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Preceding, new Date(3,Month.March,2015),    new Period(-1,TimeUnit.Months), false, new Date(3,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Preceding, new Date(3,Month.February,2015), new Period(-2,TimeUnit.Days), false,   new Date(30,Month.January,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Preceding, new Date(1,Month.March,2015),    new Period(-1,TimeUnit.Months), true,  new Date(30,Month.January,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Preceding, new Date(1,Month.March,2015),    new Period(-1,TimeUnit.Months), false, new Date(30,Month.January,2015)),

              //ModifiedPreceding
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedPreceding, new Date(3,Month.March,2015),    new Period(-1,TimeUnit.Months), false, new Date(3,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedPreceding, new Date(3,Month.February,2015), new Period(-2,TimeUnit.Days), false,   new Date(30,Month.January,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedPreceding, new Date(1,Month.March,2015),    new Period(-1,TimeUnit.Months), true,  new Date(2,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.ModifiedPreceding, new Date(1,Month.March,2015),    new Period(-1,TimeUnit.Months), false, new Date(2,Month.February,2015)),

              //Unadjusted
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Unadjusted, new Date(3,Month.February,2015), new Period(1,TimeUnit.Months), false, new Date(3,Month.March,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Unadjusted, new Date(3,Month.February,2015), new Period(4,TimeUnit.Days), false,   new Date(9,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Unadjusted, new Date(31,Month.January,2015), new Period(1,TimeUnit.Months), true,  new Date(27,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Unadjusted, new Date(31,Month.January,2015), new Period(1,TimeUnit.Months), false, new Date(28,Month.February,2015)),

              //HalfMonthModifiedFollowing
              new SingleCase(new SouthAfrica(), BusinessDayConvention.HalfMonthModifiedFollowing, new Date(3,Month.February,2015), new Period(1,TimeUnit.Months), false, new Date(3,Month.March,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.HalfMonthModifiedFollowing, new Date(3,Month.February,2015), new Period(4,TimeUnit.Days), false,   new Date(9,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.HalfMonthModifiedFollowing, new Date(31,Month.January,2015), new Period(1,TimeUnit.Months), true,  new Date(27,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.HalfMonthModifiedFollowing, new Date(31,Month.January,2015), new Period(1,TimeUnit.Months), false, new Date(27,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.HalfMonthModifiedFollowing, new Date(3,Month.January,2015),  new Period(1,TimeUnit.Weeks), false,  new Date(12,Month.January,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.HalfMonthModifiedFollowing, new Date(21,Month.March,2015),   new Period(1,TimeUnit.Weeks), false,  new Date(30,Month.March,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.HalfMonthModifiedFollowing, new Date(7,Month.February,2015), new Period(1,TimeUnit.Months), false, new Date(9,Month.March,2015)),

              //Nearest
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Nearest, new Date(3,Month.February,2015), new Period(1,TimeUnit.Months), false, new Date(3,Month.March,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Nearest, new Date(3,Month.February,2015), new Period(4,TimeUnit.Days), false,   new Date(9,Month.February,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Nearest, new Date(16,Month.April,2015),   new Period(1,TimeUnit.Months), false, new Date(15,Month.May,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Nearest, new Date(17,Month.April,2015),   new Period(1,TimeUnit.Months), false, new Date(18,Month.May,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Nearest, new Date(4,Month.March,2015),    new Period(1,TimeUnit.Months), false, new Date(2,Month.April,2015)),
              new SingleCase(new SouthAfrica(), BusinessDayConvention.Nearest, new Date(2,Month.April,2015),    new Period(1,TimeUnit.Months), false, new Date(4,Month.May,2015))
          };

          int n = testCases.Length;
          for (int i=0; i<n; i++) 
          {
              Calendar calendar = new Calendar(testCases[i].calendar);
              Date result = calendar.advance( testCases[i].start, testCases[i].period, testCases[i].convention, testCases[i].endOfMonth);

              QAssert.IsTrue( result == testCases[i].result,
                            "\ncase " + i + ":\n" //<< j << " ("<< desc << "): "
                            + "start date: " + testCases[i].start + "\n"
                            + "calendar: " + calendar + "\n"
                            + "period: " + testCases[i].period + ", end of month: " + testCases[i].endOfMonth + "\n"
                            + "convention: " + testCases[i].convention + "\n"
                            + "expected: " + testCases[i].result + " vs. actual: " + result);
    
          }
      }
   }
}
