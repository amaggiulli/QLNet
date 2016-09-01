/*
 Copyright (C) 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   public class T_Schedule
   {
      void check_dates(Schedule s, List<Date> expected)
      {
         if (s.Count != expected.Count)
         {
            QAssert.Fail("expected " + expected.Count + " dates, " + "found " + s.Count);
         }

         for (int i = 0; i < expected.Count; ++i)
         {
            if (s[i] != expected[i])
            {
               QAssert.Fail("expected " + expected[i] + " at index " + i + ", " + "found " + s[i]);

            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testDailySchedule()
      {
         // Testing schedule with daily frequency

         Date startDate = new Date(17, Month.January, 2012);

         Schedule s = new MakeSchedule().from(startDate).to(startDate + 7)
                      .withCalendar(new TARGET())
                      .withConvention(BusinessDayConvention.Preceding)
                      .withFrequency(Frequency.Daily).value();

         List<Date> expected = new List<Date>(6);
         // The schedule should skip Saturday 21st and Sunday 22rd.
         // Previously, it would adjust them to Friday 20th, resulting
         // in three copies of the same date.
         expected.Add(new Date(17, Month.January, 2012));
         expected.Add(new Date(18, Month.January, 2012));
         expected.Add(new Date(19, Month.January, 2012));
         expected.Add(new Date(20, Month.January, 2012));
         expected.Add(new Date(23, Month.January, 2012));
         expected.Add(new Date(24, Month.January, 2012));

         check_dates(s, expected);
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testEndDateWithEomAdjustment()
      {
         // Testing end date for schedule with end-of-month adjustment

         Schedule s = new MakeSchedule().from(new Date(30, Month.September, 2009))
                      .to(new Date(15, Month.June, 2012))
                      .withCalendar(new Japan())
                      .withTenor(new Period(6, TimeUnit.Months))
                      .withConvention(BusinessDayConvention.Following)
                      .withTerminationDateConvention(BusinessDayConvention.Following)
                      .forwards()
                      .endOfMonth().value();

         List<Date> expected = new List<Date>();
         // The end date is adjusted, so it should also be moved to the end
         // of the month.
         expected.Add(new Date(30, Month.September, 2009));
         expected.Add(new Date(31, Month.March, 2010));
         expected.Add(new Date(30, Month.September, 2010));
         expected.Add(new Date(31, Month.March, 2011));
         expected.Add(new Date(30, Month.September, 2011));
         expected.Add(new Date(30, Month.March, 2012));
         expected.Add(new Date(29, Month.June, 2012));

         check_dates(s, expected);

         // now with unadjusted termination date...
         s = new MakeSchedule().from(new Date(30, Month.September, 2009))
                           .to(new Date(15, Month.June, 2012))
                           .withCalendar(new Japan())
                           .withTenor(new Period(6, TimeUnit.Months))
                           .withConvention(BusinessDayConvention.Following)
                           .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                           .forwards()
                           .endOfMonth().value();
         // ...which should leave it alone.
         expected[6] = new Date(15, Month.June, 2012);

         check_dates(s, expected);
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testDatesPastEndDateWithEomAdjustment()
      {

         Schedule s = new MakeSchedule().from(new Date(28, Month.March, 2013))
                           .to(new Date(30, Month.March, 2015))
                           .withCalendar(new TARGET())
                           .withTenor(new Period(1, TimeUnit.Years))
                           .withConvention(BusinessDayConvention.Unadjusted)
                           .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                           .forwards()
                           .endOfMonth().value();

         List<Date> expected = new List<Date>();
         expected.Add(new Date(31, Month.March, 2013));
         expected.Add(new Date(31, Month.March, 2014));
         // March 31st 2015, coming from the EOM adjustment of March 28th,
         // should be discarded as past the end date.
         expected.Add(new Date(30, Month.March, 2015));

         check_dates(s, expected);
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testDatesSameAsEndDateWithEomAdjustment()
      {
         // Testing that next-to-last date same as end date is removed...

         Schedule s = new MakeSchedule().from(new Date(28, Month.March, 2013))
                                        .to(new Date(31, Month.March, 2015))
                                        .withCalendar(new TARGET())
                                        .withTenor(new Period(1, TimeUnit.Years))
                                        .withConvention(BusinessDayConvention.Unadjusted)
                                        .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                                        .forwards()
                                        .endOfMonth()
                                        .value();

         List<Date> expected = new List<Date>(3);
         expected.Add(new Date(31, Month.March, 2013));
         expected.Add(new Date(31, Month.March, 2014));
         // March 31st 2015, coming from the EOM adjustment of March 28th,
         // should be discarded as the same as the end date.
         expected.Add(new Date(31, Month.March, 2015));

         check_dates(s, expected);

         // also, the last period should be regular.
         if (!s.isRegular(2))
            QAssert.Fail("last period should be regular");
      }


#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testForwardDatesWithEomAdjustment()
      {
         // Testing that the last date is not adjusted for EOM when termination date convention is unadjusted

         Schedule s = new MakeSchedule().from(new Date(31, Month.August, 1996))
                           .to(new Date(15, Month.September, 1997))
                           .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
                           .withTenor(new Period(6, TimeUnit.Months))
                           .withConvention(BusinessDayConvention.Unadjusted)
                           .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                           .forwards()
                           .endOfMonth().value();

         List<Date> expected = new List<Date>();
         expected.Add(new Date(31, Month.August, 1996));
         expected.Add(new Date(28, Month.February, 1997));
         expected.Add(new Date(31, Month.August, 1997));
         expected.Add(new Date(15, Month.September, 1997));

         check_dates(s, expected);
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testBackwardDatesWithEomAdjustment()
      {
         // Testing that the first date is not adjusted for EOM going backward when termination date convention is unadjusted

         Schedule s = new MakeSchedule().from(new Date(22, Month.August, 1996))
                           .to(new Date(31, Month.August, 1997))
                           .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
                           .withTenor(new Period(6, TimeUnit.Months))
                           .withConvention(BusinessDayConvention.Unadjusted)
                           .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                           .backwards()
                           .endOfMonth().value();

         List<Date> expected = new List<Date>();
         expected.Add(new Date(22, Month.August, 1996));
         expected.Add(new Date(31, Month.August, 1996));
         expected.Add(new Date(28, Month.February, 1997));
         expected.Add(new Date(31, Month.August, 1997));

         check_dates(s, expected);
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testDoubleFirstDateWithEomAdjustment()
      {
         // Testing that the first date is not duplicated due to EOM convention when going backwards

         Schedule s = new MakeSchedule().from(new Date(22, Month.August, 1996))
                           .to(new Date(31, Month.August, 1997))
                           .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
                           .withTenor(new Period(6, TimeUnit.Months))
                           .withConvention(BusinessDayConvention.Following)
                           .withTerminationDateConvention(BusinessDayConvention.Following)
                           .backwards()
                           .endOfMonth().value();

         List<Date> expected = new List<Date>();
         expected.Add(new Date(30, Month.August, 1996));
         expected.Add(new Date(28, Month.February, 1997));
         expected.Add(new Date(29, Month.August, 1997));

         check_dates(s, expected);
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testDateConstructor()
      {
         // Testing the constructor taking a vector of dates and possibly additional meta information

         List<Date> dates = new List<Date>();
         dates.Add(new Date(16, Month.May, 2015));
         dates.Add(new Date(18, Month.May, 2015));
         dates.Add(new Date(18, Month.May, 2016));
         dates.Add(new Date(31, Month.December, 2017));

         // schedule without any additional information
         Schedule schedule1 = new Schedule(dates);
         if (schedule1.Count != dates.Count)
            QAssert.Fail( "schedule1 has size " + schedule1.Count + ", expected " + dates.Count );
         for (int i = 0; i < dates.Count; ++i)
            if (schedule1[i] != dates[i])
               QAssert.Fail("schedule1 has "+schedule1[i]+" at position "+i+", expected "+ dates[i]);
         if (schedule1.calendar() != new NullCalendar())
            QAssert.Fail("schedule1 has calendar "+schedule1.calendar().name()+", expected null calendar");
         if (schedule1.businessDayConvention() != BusinessDayConvention.Unadjusted)
            QAssert.Fail( "schedule1 has convention " + schedule1.businessDayConvention() + ", expected unadjusted" );

         // schedule with metadata
         List<bool> regular = new List<bool>();
         regular.Add(false);
         regular.Add(true);
         regular.Add(false);

         Schedule schedule2 = new Schedule(dates, new TARGET(), BusinessDayConvention.Following, BusinessDayConvention.ModifiedPreceding, new Period(1, TimeUnit.Years),
                            DateGeneration.Rule.Backward, true, regular);
         for (int i = 1; i < dates.Count; ++i)
            if (schedule2.isRegular(i) != regular[i - 1])
               QAssert.Fail( "schedule2 has a " + ( schedule2.isRegular( i ) ? "regular" : "irregular" ) + " period at position " + i + ", expected " + ( regular[i - 1] ? "regular" : "irregular" ) );
         if (schedule2.calendar() != new TARGET())
            QAssert.Fail( "schedule1 has calendar " + schedule2.calendar().name() + ", expected TARGET" );
         if (schedule2.businessDayConvention() != BusinessDayConvention.Following)
            QAssert.Fail( "schedule2 has convention " + schedule2.businessDayConvention() + ", expected Following" );
         if (schedule2.terminationDateBusinessDayConvention() != BusinessDayConvention.ModifiedPreceding)
            QAssert.Fail( "schedule2 has convention " + schedule2.terminationDateBusinessDayConvention() + ", expected Modified Preceding" );
         if (schedule2.tenor() != new Period(1, TimeUnit.Years))
            QAssert.Fail( "schedule2 has tenor " + schedule2.tenor() + ", expected 1Y" );
         if (schedule2.rule() != DateGeneration.Rule.Backward)
            QAssert.Fail( "schedule2 has rule " + schedule2.rule() + ", expected Backward" );
         if (schedule2.endOfMonth() != true)
            QAssert.Fail("schedule2 has end of month flag false, expected true");
      }
   }
}
