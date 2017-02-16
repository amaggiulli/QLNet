/*
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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
using System.Diagnostics;

namespace TestSuite {
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
    public class T_Calendars {
#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testModifiedCalendars() {
            Calendar c1 = new TARGET();
            Calendar c2 = new UnitedStates(UnitedStates.Market.NYSE);
            Date d1 = new Date(1, Month.May, 2004);      // holiday for both calendars
            Date d2 = new Date(26, Month.April, 2004);   // business day

            QAssert.IsTrue(c1.isHoliday(d1), "wrong assumption---correct the test");
            QAssert.IsTrue(c1.isBusinessDay(d2), "wrong assumption---correct the test");

            QAssert.IsTrue(c2.isHoliday(d1), "wrong assumption---correct the test");
            QAssert.IsTrue(c2.isBusinessDay(d2), "wrong assumption---correct the test");

            // modify the TARGET calendar
            c1.removeHoliday(d1);
            c1.addHoliday(d2);

            // test
            QAssert.IsFalse(c1.isHoliday(d1), d1 + " still a holiday for original TARGET instance");
            QAssert.IsFalse(c1.isBusinessDay(d2), d2 + " still a business day for original TARGET instance");

            // any instance of TARGET should be modified...
            Calendar c3 = new TARGET();
            QAssert.IsFalse(c3.isHoliday(d1), d1 + " still a holiday for generic TARGET instance");
            QAssert.IsFalse(c3.isBusinessDay(d2), d2 + " still a business day for generic TARGET instance");

            // ...but not other calendars
            QAssert.IsFalse(c2.isBusinessDay(d1), d1 + " business day for New York");
            QAssert.IsFalse(c2.isHoliday(d2), d2 + " holiday for New York");

            // restore original holiday set---test the other way around
            c3.addHoliday(d1);
            c3.removeHoliday(d2);

            QAssert.IsFalse(c1.isBusinessDay(d1), d1 + " still a business day");
            QAssert.IsFalse(c1.isHoliday(d2), d2 + " still a holiday");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testJointCalendars() {
            Calendar c1 = new TARGET(),
                     c2 = new UnitedKingdom(),
                     c3 = new UnitedStates(UnitedStates.Market.NYSE),
                     c4 = new Japan();

            Calendar c12h = new JointCalendar(c1, c2, JointCalendar.JointCalendarRule.JoinHolidays),
                     c12b = new JointCalendar(c1,c2,JointCalendar.JointCalendarRule.JoinBusinessDays),
                     c123h = new JointCalendar(c1,c2,c3,JointCalendar.JointCalendarRule.JoinHolidays),
                     c123b = new JointCalendar(c1,c2,c3,JointCalendar.JointCalendarRule.JoinBusinessDays),
                     c1234h = new JointCalendar(c1,c2,c3,c4,JointCalendar.JointCalendarRule.JoinHolidays),
                     c1234b = new JointCalendar(c1,c2,c3,c4,JointCalendar.JointCalendarRule.JoinBusinessDays);

            // test one year, starting today
            Date firstDate = Date.Today,
                 endDate = firstDate + new Period(1, TimeUnit.Years);

            for (Date d = firstDate; d < endDate; d++) {

                bool b1 = c1.isBusinessDay(d),
                     b2 = c2.isBusinessDay(d),
                     b3 = c3.isBusinessDay(d),
                     b4 = c4.isBusinessDay(d);

                if ((b1 && b2) != c12h.isBusinessDay(d))
                    QAssert.Fail("At date " + d + ":\n"
                               + "    inconsistency between joint calendar "
                               + c12h.name() + " (joining holidays)\n"
                               + "    and its components");

                if ((b1 || b2) != c12b.isBusinessDay(d))
                    QAssert.Fail("At date " + d + ":\n"
                               + "    inconsistency between joint calendar "
                               + c12b.name() + " (joining business days)\n"
                               + "    and its components");

                if ((b1 && b2 && b3) != c123h.isBusinessDay(d))
                    QAssert.Fail("At date " + d + ":\n"
                               + "    inconsistency between joint calendar "
                               + c123h.name() + " (joining holidays)\n"
                               + "    and its components");

                if ((b1 || b2 || b3) != c123b.isBusinessDay(d))
                    QAssert.Fail("At date " + d + ":\n"
                               + "    inconsistency between joint calendar "
                               + c123b.name() + " (joining business days)\n"
                               + "    and its components");

                if ((b1 && b2 && b3 && b4) != c1234h.isBusinessDay(d))
                    QAssert.Fail("At date " + d + ":\n"
                               + "    inconsistency between joint calendar "
                               + c1234h.name() + " (joining holidays)\n"
                               + "    and its components");

                if ((b1 || b2 || b3 || b4) != c1234b.isBusinessDay(d))
                    QAssert.Fail("At date " + d + ":\n"
                               + "    inconsistency between joint calendar "
                               + c1234b.name() + " (joining business days)\n"
                               + "    and its components");

            }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testUSSettlement() {
            // Testing US settlement holiday list
            List<Date> expectedHol = new List<Date>();

            expectedHol.Add(new Date(1, Month.January, 2004));
            expectedHol.Add(new Date(19, Month.January, 2004));
            expectedHol.Add(new Date(16, Month.February, 2004));
            expectedHol.Add(new Date(31, Month.May, 2004));
            expectedHol.Add(new Date(5, Month.July, 2004));
            expectedHol.Add(new Date(6, Month.September, 2004));
            expectedHol.Add(new Date(11, Month.October, 2004));
            expectedHol.Add(new Date(11, Month.November, 2004));
            expectedHol.Add(new Date(25, Month.November, 2004));
            expectedHol.Add(new Date(24, Month.December, 2004));

            expectedHol.Add(new Date(31, Month.December, 2004));
            expectedHol.Add(new Date(17, Month.January, 2005));
            expectedHol.Add(new Date(21, Month.February, 2005));
            expectedHol.Add(new Date(30, Month.May, 2005));
            expectedHol.Add(new Date(4, Month.July, 2005));
            expectedHol.Add(new Date(5, Month.September, 2005));
            expectedHol.Add(new Date(10, Month.October, 2005));
            expectedHol.Add(new Date(11, Month.November, 2005));
            expectedHol.Add(new Date(24, Month.November, 2005));
            expectedHol.Add(new Date(26, Month.December, 2005));

            Calendar c = new UnitedStates(UnitedStates.Market.Settlement);
            List<Date> hol = Calendar.holidayList(c, new Date(1, Month.January, 2004),
                                                     new Date(31, Month.December, 2005));
            if ( hol.Count != expectedHol.Count )
               QAssert.Fail( "there were " + expectedHol.Count
                          + " expected holidays, while there are " + hol.Count
                          + " calculated holidays" );

            for (int i = 0; i <hol.Count; i++) {
                if (hol[i] != expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i] + " while calculated holiday is " + hol[i]);
            }
            
            // before Uniform Monday Holiday Act
            expectedHol = new List<Date>();
            expectedHol.Add(new Date(2,Month.January,1961));
            expectedHol.Add(new Date(22,Month.February,1961));
            expectedHol.Add(new Date(30,Month.May,1961));
            expectedHol.Add(new Date(4,Month.July,1961));
            expectedHol.Add(new Date(4,Month.September,1961));
            expectedHol.Add(new Date(10,Month.November,1961));
            expectedHol.Add(new Date(23,Month.November,1961));
            expectedHol.Add(new Date(25,Month.December,1961));

            hol = Calendar.holidayList( c, new Date( 1, Month.January, 1961 ), new Date( 31, Month.December, 1961 ) );

            if (hol.Count != expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count +
                             " expected holidays, while there are " + hol.Count +
                             " calculated holidays");
            
            for ( int i = 0; i < hol.Count; i++ )
            {
               if ( hol[i] != expectedHol[i] )
                  QAssert.Fail( "expected holiday was " + expectedHol[i]
                             + " while calculated holiday is " + hol[i] );
            }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testUSGovernmentBondMarket() {

            List<Date> expectedHol = new List<Date>();
            expectedHol.Add(new Date(1, Month.January, 2004));
            expectedHol.Add(new Date(19, Month.January, 2004));
            expectedHol.Add(new Date(16, Month.February, 2004));
            expectedHol.Add(new Date(9, Month.April, 2004));
            expectedHol.Add(new Date(31, Month.May, 2004));
            expectedHol.Add(new Date(5, Month.July, 2004));
            expectedHol.Add(new Date(6, Month.September, 2004));
            expectedHol.Add(new Date(11, Month.October, 2004));
            expectedHol.Add(new Date(11, Month.November, 2004));
            expectedHol.Add(new Date(25, Month.November, 2004));
            expectedHol.Add(new Date(24, Month.December, 2004));

            Calendar c = new UnitedStates(UnitedStates.Market.GovernmentBond);
            List<Date> hol = Calendar.holidayList(c, new Date(1, Month.January, 2004), new Date(31, Month.December, 2004));

            for (int i = 0; i < Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i] != expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i] + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count != expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count +
                            " expected holidays, while there are " + hol.Count +
                            " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testUSNewYorkStockExchange() {

            List<Date> expectedHol = new List<Date>();
            expectedHol.Add(new Date(1, Month.January, 2004));
            expectedHol.Add(new Date(19, Month.January, 2004));
            expectedHol.Add(new Date(16, Month.February, 2004));
            expectedHol.Add(new Date(9, Month.April, 2004));
            expectedHol.Add(new Date(31, Month.May, 2004));
            expectedHol.Add(new Date(11, Month.June, 2004));
            expectedHol.Add(new Date(5, Month.July, 2004));
            expectedHol.Add(new Date(6, Month.September, 2004));
            expectedHol.Add(new Date(25, Month.November, 2004));
            expectedHol.Add(new Date(24, Month.December, 2004));

            expectedHol.Add(new Date(17, Month.January, 2005));
            expectedHol.Add(new Date(21, Month.February, 2005));
            expectedHol.Add(new Date(25, Month.March, 2005));
            expectedHol.Add(new Date(30, Month.May, 2005));
            expectedHol.Add(new Date(4, Month.July, 2005));
            expectedHol.Add(new Date(5, Month.September, 2005));
            expectedHol.Add(new Date(24, Month.November, 2005));
            expectedHol.Add(new Date(26, Month.December, 2005));

            expectedHol.Add(new Date(2, Month.January, 2006));
            expectedHol.Add(new Date(16, Month.January, 2006));
            expectedHol.Add(new Date(20, Month.February, 2006));
            expectedHol.Add(new Date(14, Month.April, 2006));
            expectedHol.Add(new Date(29, Month.May, 2006));
            expectedHol.Add(new Date(4, Month.July, 2006));
            expectedHol.Add(new Date(4, Month.September, 2006));
            expectedHol.Add(new Date(23, Month.November, 2006));
            expectedHol.Add(new Date(25, Month.December, 2006));

            Calendar c = new UnitedStates(UnitedStates.Market.NYSE);
            List<Date> hol = Calendar.holidayList(c, new Date(1, Month.January, 2004), new Date(31, Month.December, 2006));

            int i;
            for (i = 0; i < Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i] != expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i] + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count != expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count +
                            " expected holidays, while there are " + hol.Count +
                            " calculated holidays");

            List<Date> histClose = new List<Date>();
            histClose.Add(new Date(30, Month.October, 2012));  // Hurricane Sandy
            histClose.Add(new Date(29, Month.October, 2012));  // Hurricane Sandy
            histClose.Add(new Date(11, Month.June, 2004));     // Reagan's funeral
            histClose.Add(new Date(14, Month.September, 2001));// September 11, 2001
            histClose.Add(new Date(13, Month.September, 2001));// September 11, 2001
            histClose.Add(new Date(12, Month.September, 2001));// September 11, 2001
            histClose.Add(new Date(11, Month.September, 2001));// September 11, 2001
            histClose.Add(new Date(27, Month.April, 1994 ) );  // Nixon's funeral.
            histClose.Add(new Date(27, Month.September,1985)); // Hurricane Gloria
            histClose.Add(new Date(14, Month.July, 1977));     // 1977 Blackout
            histClose.Add(new Date(25, Month.January, 1973));  // Johnson's funeral.
            histClose.Add(new Date(28, Month.December, 1972)); // Truman's funeral
            histClose.Add(new Date(21, Month.July, 1969));     // Lunar exploration nat. day
            histClose.Add(new Date(31, Month.March, 1969));    // Eisenhower's funeral
            histClose.Add(new Date(10, Month.February, 1969)); // heavy snow
            histClose.Add(new Date(5, Month.July, 1968));      // Day after Independence Day
            histClose.Add( new Date( 9, Month.April, 1968 ) );     // Mourning for MLK
            histClose.Add( new Date( 24, Month.December, 1965 ) ); // Christmas Eve
            histClose.Add( new Date( 25, Month.November, 1963 ) ); // Kennedy's funeral
            histClose.Add( new Date( 29, Month.May, 1961 ) );      // Day before Decoration Day
            histClose.Add( new Date( 26, Month.December, 1958 ) ); // Day after Christmas
            histClose.Add( new Date( 24, Month.December, 1956 ) ); // Christmas Eve
            histClose.Add( new Date( 24, Month.December, 1954 ) ); // Christmas Eve
            // June 12-Dec. 31, 1968
            // Four day week (closed on Wednesdays) - Paperwork Crisis
            histClose.Add(new Date(12, Month.Jun, 1968));
            histClose.Add(new Date(19, Month.Jun, 1968));
            histClose.Add(new Date(26, Month.Jun, 1968));
            histClose.Add(new Date(3, Month.Jul, 1968));
            histClose.Add(new Date(10, Month.Jul, 1968));
            histClose.Add(new Date(17, Month.Jul, 1968));
            histClose.Add(new Date(20, Month.Nov, 1968));
            histClose.Add(new Date(27, Month.Nov, 1968));
            histClose.Add(new Date(4, Month.Dec, 1968));
            histClose.Add(new Date(11, Month.Dec, 1968));
            histClose.Add(new Date(18, Month.Dec, 1968));
            // Presidential election days
            histClose.Add(new Date(4, Month.Nov, 1980));
            histClose.Add(new Date(2, Month.Nov, 1976));
            histClose.Add(new Date(7, Month.Nov, 1972));
            histClose.Add(new Date(5, Month.Nov, 1968));
            histClose.Add(new Date(3, Month.Nov, 1964));

            for (i = 0; i < histClose.Count; i++) {
                if (!c.isHoliday(histClose[i]))
                    QAssert.Fail(histClose[i] + " should be holiday (historical close)");
            }

        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testTARGET() {
            List<Date> expectedHol = new List<Date>();
            expectedHol.Add(new Date(1,Month.January,1999));
            expectedHol.Add(new Date(31, Month.December, 1999));

            expectedHol.Add(new Date(21, Month.April, 2000));
            expectedHol.Add(new Date(24, Month.April, 2000));
            expectedHol.Add(new Date(1, Month.May, 2000));
            expectedHol.Add(new Date(25, Month.December, 2000));
            expectedHol.Add(new Date(26, Month.December, 2000));

            expectedHol.Add(new Date(1, Month.January, 2001));
            expectedHol.Add(new Date(13, Month.April, 2001));
            expectedHol.Add(new Date(16, Month.April, 2001));
            expectedHol.Add(new Date(1, Month.May, 2001));
            expectedHol.Add(new Date(25, Month.December, 2001));
            expectedHol.Add(new Date(26, Month.December, 2001));
            expectedHol.Add(new Date(31, Month.December, 2001));

            expectedHol.Add(new Date(1, Month.January, 2002));
            expectedHol.Add(new Date(29, Month.March, 2002));
            expectedHol.Add(new Date(1, Month.April, 2002));
            expectedHol.Add(new Date(1, Month.May, 2002));
            expectedHol.Add(new Date(25, Month.December, 2002));
            expectedHol.Add(new Date(26, Month.December, 2002));

            expectedHol.Add(new Date(1, Month.January, 2003));
            expectedHol.Add(new Date(18, Month.April, 2003));
            expectedHol.Add(new Date(21, Month.April, 2003));
            expectedHol.Add(new Date(1, Month.May, 2003));
            expectedHol.Add(new Date(25, Month.December, 2003));
            expectedHol.Add(new Date(26, Month.December, 2003));

            expectedHol.Add(new Date(1, Month.January, 2004));
            expectedHol.Add(new Date(9, Month.April, 2004));
            expectedHol.Add(new Date(12, Month.April, 2004));

            expectedHol.Add(new Date(25, Month.March, 2005));
            expectedHol.Add(new Date(28, Month.March, 2005));
            expectedHol.Add(new Date(26, Month.December, 2005));

            expectedHol.Add(new Date(14, Month.April, 2006));
            expectedHol.Add(new Date(17, Month.April, 2006));
            expectedHol.Add(new Date(1, Month.May, 2006));
            expectedHol.Add(new Date(25, Month.December, 2006));
            expectedHol.Add(new Date(26, Month.December, 2006));

            Calendar c = new TARGET();
            List<Date> hol = Calendar.holidayList(c, new Date(1, Month.January, 1999), new Date(31, Month.December, 2006));

            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");

        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testGermanyFrankfurt() {
            List<Date> expectedHol = new List<Date>();

            expectedHol.Add(new Date(1, Month.January,2003));
            expectedHol.Add(new Date(18,Month.April,2003));
            expectedHol.Add(new Date(21,Month.April,2003));
            expectedHol.Add(new Date(1,Month.May,2003));
            expectedHol.Add(new Date(24,Month.December,2003));
            expectedHol.Add(new Date(25,Month.December,2003));
            expectedHol.Add(new Date(26,Month.December,2003));
            expectedHol.Add(new Date(31,Month.December,2003));

            expectedHol.Add(new Date(1,Month.January,2004));
            expectedHol.Add(new Date(9,Month.April,2004));
            expectedHol.Add(new Date(12,Month.April,2004));
            expectedHol.Add(new Date(24,Month.December,2004));
            expectedHol.Add(new Date(31,Month.December,2004));

            Calendar c = new Germany(Germany.Market.FrankfurtStockExchange);
            List<Date> hol = Calendar.holidayList(c, new Date(1,Month.January,2003), new Date(31,Month.December,2004));
            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testGermanyEurex() {
            List<Date> expectedHol = new List<Date>();

            expectedHol.Add(new Date(1,Month.January,2003));
            expectedHol.Add(new Date(18,Month.April,2003));
            expectedHol.Add(new Date(21,Month.April,2003));
            expectedHol.Add(new Date(1,Month.May,2003));
            expectedHol.Add(new Date(24,Month.December,2003));
            expectedHol.Add(new Date(25,Month.December,2003));
            expectedHol.Add(new Date(26,Month.December,2003));
            expectedHol.Add(new Date(31,Month.December,2003));

            expectedHol.Add(new Date(1,Month.January,2004));
            expectedHol.Add(new Date(9,Month.April,2004));
            expectedHol.Add(new Date(12,Month.April,2004));
            expectedHol.Add(new Date(24,Month.December,2004));
            expectedHol.Add(new Date(31,Month.December,2004));

            Calendar c = new Germany(Germany.Market.Eurex);
            List<Date> hol = Calendar.holidayList(c, new Date(1,Month.January,2003),
                                                             new Date(31,Month.December,2004));
            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testGermanyXetra() {
            List<Date> expectedHol = new List<Date>();

            expectedHol.Add(new Date(1,Month.January,2003));
            expectedHol.Add(new Date(18,Month.April,2003));
            expectedHol.Add(new Date(21,Month.April,2003));
            expectedHol.Add(new Date(1,Month.May,2003));
            expectedHol.Add(new Date(24,Month.December,2003));
            expectedHol.Add(new Date(25,Month.December,2003));
            expectedHol.Add(new Date(26,Month.December,2003));
            expectedHol.Add(new Date(31,Month.December,2003));

            expectedHol.Add(new Date(1,Month.January,2004));
            expectedHol.Add(new Date(9,Month.April,2004));
            expectedHol.Add(new Date(12,Month.April,2004));
            expectedHol.Add(new Date(24,Month.December,2004));
            expectedHol.Add(new Date(31,Month.December,2004));

            Calendar c = new Germany(Germany.Market.Xetra);
            List<Date> hol = Calendar.holidayList(c, new Date(1,Month.January,2003), new Date(31,Month.December,2004));
            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i] + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testUKSettlement() {
            //BOOST_MESSAGE("Testing UK settlement holiday list...");

            List<Date> expectedHol = new List<Date>();

            expectedHol.Add(new Date(1,Month.January,2004));
            expectedHol.Add(new Date(9,Month.April,2004));
            expectedHol.Add(new Date(12,Month.April,2004));
            expectedHol.Add(new Date(3,Month.May,2004));
            expectedHol.Add(new Date(31,Month.May,2004));
            expectedHol.Add(new Date(30,Month.August,2004));
            expectedHol.Add(new Date(27,Month.December,2004));
            expectedHol.Add(new Date(28,Month.December,2004));

            expectedHol.Add(new Date(3,Month.January,2005));
            expectedHol.Add(new Date(25,Month.March,2005));
            expectedHol.Add(new Date(28,Month.March,2005));
            expectedHol.Add(new Date(2,Month.May,2005));
            expectedHol.Add(new Date(30,Month.May,2005));
            expectedHol.Add(new Date(29,Month.August,2005));
            expectedHol.Add(new Date(26,Month.December,2005));
            expectedHol.Add(new Date(27,Month.December,2005));

            expectedHol.Add(new Date(2,Month.January,2006));
            expectedHol.Add(new Date(14,Month.April,2006));
            expectedHol.Add(new Date(17,Month.April,2006));
            expectedHol.Add(new Date(1,Month.May,2006));
            expectedHol.Add(new Date(29,Month.May,2006));
            expectedHol.Add(new Date(28,Month.August,2006));
            expectedHol.Add(new Date(25,Month.December,2006));
            expectedHol.Add(new Date(26,Month.December,2006));

            expectedHol.Add(new Date(1,Month.January,2007));
            expectedHol.Add(new Date(6,Month.April,2007));
            expectedHol.Add(new Date(9,Month.April,2007));
            expectedHol.Add(new Date(7,Month.May,2007));
            expectedHol.Add(new Date(28,Month.May,2007));
            expectedHol.Add(new Date(27,Month.August,2007));
            expectedHol.Add(new Date(25,Month.December,2007));
            expectedHol.Add(new Date(26,Month.December,2007));

            Calendar c = new UnitedKingdom(UnitedKingdom.Market.Settlement);
            List<Date> hol = Calendar.holidayList(c, new Date(1,Month.January,2004), new Date(31,Month.December,2007));
            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testUKExchange() {
            //BOOST_MESSAGE("Testing London Stock Exchange holiday list...");

            List<Date> expectedHol = new List<Date>();

            expectedHol.Add(new Date(1,Month.January,2004));
            expectedHol.Add(new Date(9,Month.April,2004));
            expectedHol.Add(new Date(12,Month.April,2004));
            expectedHol.Add(new Date(3,Month.May,2004));
            expectedHol.Add(new Date(31,Month.May,2004));
            expectedHol.Add(new Date(30,Month.August,2004));
            expectedHol.Add(new Date(27,Month.December,2004));
            expectedHol.Add(new Date(28,Month.December,2004));

            expectedHol.Add(new Date(3,Month.January,2005));
            expectedHol.Add(new Date(25,Month.March,2005));
            expectedHol.Add(new Date(28,Month.March,2005));
            expectedHol.Add(new Date(2,Month.May,2005));
            expectedHol.Add(new Date(30,Month.May,2005));
            expectedHol.Add(new Date(29,Month.August,2005));
            expectedHol.Add(new Date(26,Month.December,2005));
            expectedHol.Add(new Date(27,Month.December,2005));

            expectedHol.Add(new Date(2,Month.January,2006));
            expectedHol.Add(new Date(14,Month.April,2006));
            expectedHol.Add(new Date(17,Month.April,2006));
            expectedHol.Add(new Date(1,Month.May,2006));
            expectedHol.Add(new Date(29,Month.May,2006));
            expectedHol.Add(new Date(28,Month.August,2006));
            expectedHol.Add(new Date(25,Month.December,2006));
            expectedHol.Add(new Date(26,Month.December,2006));

            expectedHol.Add(new Date(1,Month.January,2007));
            expectedHol.Add(new Date(6,Month.April,2007));
            expectedHol.Add(new Date(9,Month.April,2007));
            expectedHol.Add(new Date(7,Month.May,2007));
            expectedHol.Add(new Date(28,Month.May,2007));
            expectedHol.Add(new Date(27,Month.August,2007));
            expectedHol.Add(new Date(25,Month.December,2007));
            expectedHol.Add(new Date(26,Month.December,2007));

            Calendar c = new UnitedKingdom(UnitedKingdom.Market.Exchange);
            List<Date> hol = Calendar.holidayList(c, new Date(1,Month.January,2004), new Date(31,Month.December,2007));
            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testUKMetals() {
            //BOOST_MESSAGE("Testing London Metals Exchange holiday list...");

            List<Date> expectedHol = new List<Date>();

            expectedHol.Add(new Date(1,Month.January,2004));
            expectedHol.Add(new Date(9,Month.April,2004));
            expectedHol.Add(new Date(12,Month.April,2004));
            expectedHol.Add(new Date(3,Month.May,2004));
            expectedHol.Add(new Date(31,Month.May,2004));
            expectedHol.Add(new Date(30,Month.August,2004));
            expectedHol.Add(new Date(27,Month.December,2004));
            expectedHol.Add(new Date(28,Month.December,2004));

            expectedHol.Add(new Date(3,Month.January,2005));
            expectedHol.Add(new Date(25,Month.March,2005));
            expectedHol.Add(new Date(28,Month.March,2005));
            expectedHol.Add(new Date(2,Month.May,2005));
            expectedHol.Add(new Date(30,Month.May,2005));
            expectedHol.Add(new Date(29,Month.August,2005));
            expectedHol.Add(new Date(26,Month.December,2005));
            expectedHol.Add(new Date(27,Month.December,2005));

            expectedHol.Add(new Date(2,Month.January,2006));
            expectedHol.Add(new Date(14,Month.April,2006));
            expectedHol.Add(new Date(17,Month.April,2006));
            expectedHol.Add(new Date(1,Month.May,2006));
            expectedHol.Add(new Date(29,Month.May,2006));
            expectedHol.Add(new Date(28,Month.August,2006));
            expectedHol.Add(new Date(25,Month.December,2006));
            expectedHol.Add(new Date(26,Month.December,2006));

            expectedHol.Add(new Date(1,Month.January,2007));
            expectedHol.Add(new Date(6,Month.April,2007));
            expectedHol.Add(new Date(9,Month.April,2007));
            expectedHol.Add(new Date(7,Month.May,2007));
            expectedHol.Add(new Date(28,Month.May,2007));
            expectedHol.Add(new Date(27,Month.August,2007));
            expectedHol.Add(new Date(25,Month.December,2007));
            expectedHol.Add(new Date(26,Month.December,2007));

            Calendar c = new UnitedKingdom(UnitedKingdom.Market.Metals);
            List<Date> hol = Calendar.holidayList(c, new Date(1,Month.January,2004), new Date(31,Month.December,2007));
            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testItalyExchange() {
            //BOOST_MESSAGE("Testing Milan Stock Exchange holiday list...");

            List<Date> expectedHol = new List<Date>();

            expectedHol.Add(new Date(1,Month.January,2002));
            expectedHol.Add(new Date(29,Month.March,2002));
            expectedHol.Add(new Date(1,Month.April,2002));
            expectedHol.Add(new Date(1,Month.May,2002));
            expectedHol.Add(new Date(15,Month.August,2002));
            expectedHol.Add(new Date(24,Month.December,2002));
            expectedHol.Add(new Date(25,Month.December,2002));
            expectedHol.Add(new Date(26,Month.December,2002));
            expectedHol.Add(new Date(31,Month.December,2002));

            expectedHol.Add(new Date(1,Month.January,2003));
            expectedHol.Add(new Date(18,Month.April,2003));
            expectedHol.Add(new Date(21,Month.April,2003));
            expectedHol.Add(new Date(1,Month.May,2003));
            expectedHol.Add(new Date(15,Month.August,2003));
            expectedHol.Add(new Date(24,Month.December,2003));
            expectedHol.Add(new Date(25,Month.December,2003));
            expectedHol.Add(new Date(26,Month.December,2003));
            expectedHol.Add(new Date(31,Month.December,2003));

            expectedHol.Add(new Date(1,Month.January,2004));
            expectedHol.Add(new Date(9,Month.April,2004));
            expectedHol.Add(new Date(12,Month.April,2004));
            expectedHol.Add(new Date(24,Month.December,2004));
            expectedHol.Add(new Date(31,Month.December,2004));

            Calendar c = new Italy(Italy.Market.Exchange);
            List<Date> hol = Calendar.holidayList(c, new Date(1,Month.January,2002), new Date(31,Month.December,2004));
            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testBrazil() {
            //BOOST_MESSAGE("Testing Brazil holiday list...");

            List<Date> expectedHol = new List<Date>();

            //expectedHol.Add(new Date(1,January,2005)); // Saturday
            expectedHol.Add(new Date(7,Month.February,2005));
            expectedHol.Add(new Date(8,Month.February,2005));
            expectedHol.Add(new Date(25,Month.March,2005));
            expectedHol.Add(new Date(21,Month.April,2005));
            //expectedHol.Add(new Date(1,May,2005)); // Sunday
            expectedHol.Add(new Date(26,Month.May,2005));
            expectedHol.Add(new Date(7,Month.September,2005));
            expectedHol.Add(new Date(12,Month.October,2005));
            expectedHol.Add(new Date(2,Month.November,2005));
            expectedHol.Add(new Date(15,Month.November,2005));
            //expectedHol.Add(new Date(25,December,2005)); // Sunday

            //expectedHol.Add(new Date(1,January,2006)); // Sunday
            expectedHol.Add(new Date(27,Month.February,2006));
            expectedHol.Add(new Date(28,Month.February,2006));
            expectedHol.Add(new Date(14,Month.April,2006));
            expectedHol.Add(new Date(21,Month.April,2006));
            expectedHol.Add(new Date(1,Month.May,2006));
            expectedHol.Add(new Date(15,Month.June,2006));
            expectedHol.Add(new Date(7,Month.September,2006));
            expectedHol.Add(new Date(12,Month.October,2006));
            expectedHol.Add(new Date(2,Month.November,2006));
            expectedHol.Add(new Date(15,Month.November,2006));
            expectedHol.Add(new Date(25,Month.December,2006));

            Calendar c = new Brazil();
            List<Date> hol = Calendar.holidayList(c, new Date(1,Month.January,2005), new Date(31,Month.December,2006));
            for (int i=0; i<Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i]!=expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count!=expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testSouthKoreanSettlement() {
            //("Testing South-Korean settlement holiday list...");

            List<Date> expectedHol = new List<Date>();
            expectedHol.Add(new Date(1, Month.January, 2004));
            expectedHol.Add(new Date(21, Month.January, 2004));
            expectedHol.Add(new Date(22, Month.January, 2004));
            expectedHol.Add(new Date(23, Month.January, 2004));
            expectedHol.Add(new Date(1, Month.March, 2004));
            expectedHol.Add(new Date(5, Month.April, 2004));
            expectedHol.Add(new Date(15, Month.April, 2004)); // election day
            //    expectedHol.Add(new Date(1, Month.May,2004)); // Saturday
            expectedHol.Add(new Date(5, Month.May, 2004));
            expectedHol.Add(new Date(26, Month.May, 2004));
            //    expectedHol.Add(new Date(6, Month.June,2004)); // Sunday
            //    expectedHol.Add(new Date(17, Month.July,2004)); // Saturday
            //    expectedHol.Add(new Date(15, Month.August,2004)); // Sunday
            expectedHol.Add(new Date(27, Month.September, 2004));
            expectedHol.Add(new Date(28, Month.September, 2004));
            expectedHol.Add(new Date(29, Month.September, 2004));
            //    expectedHol.Add(new Date(3, Month.October,2004)); // Sunday
            //    expectedHol.Add(new Date(25,December,2004)); // Saturday

            //    expectedHol.Add(new Date(1, Month.January,2005)); // Saturday
            expectedHol.Add(new Date(8, Month.February, 2005));
            expectedHol.Add(new Date(9, Month.February, 2005));
            expectedHol.Add(new Date(10, Month.February, 2005));
            expectedHol.Add(new Date(1, Month.March, 2005));
            expectedHol.Add(new Date(5, Month.April, 2005));
            expectedHol.Add(new Date(5, Month.May, 2005));
            //    expectedHol.Add(new Date(15, Month.May,2005)); // Sunday
            expectedHol.Add(new Date(6, Month.June, 2005));
            //    expectedHol.Add(new Date(17, Month.July,2005)); // Sunday
            expectedHol.Add(new Date(15, Month.August, 2005));
            //    expectedHol.Add(new Date(17, Month.September,2005)); // Saturday
            //    expectedHol.Add(new Date(18, Month.September,2005)); // Sunday
            expectedHol.Add(new Date(19, Month.September, 2005));
            expectedHol.Add(new Date(3, Month.October, 2005));
            //    expectedHol.Add(new Date(25,December,2005)); // Sunday

            //    expectedHol.Add(new Date(1, Month.January,2006)); // Sunday
            //    expectedHol.Add(new Date(28, Month.January,2006)); // Saturday
            //    expectedHol.Add(new Date(29, Month.January,2006)); // Sunday
            expectedHol.Add(new Date(30, Month.January, 2006));
            expectedHol.Add(new Date(1, Month.March, 2006));
            expectedHol.Add(new Date(1, Month.May, 2006));
            expectedHol.Add(new Date(5, Month.May, 2006));
            expectedHol.Add(new Date(31, Month.May, 2006)); // election
            expectedHol.Add(new Date(6, Month.June, 2006));
            expectedHol.Add(new Date(17, Month.July, 2006));
            expectedHol.Add(new Date(15, Month.August, 2006));
            expectedHol.Add(new Date(3, Month.October, 2006));
            expectedHol.Add(new Date(5, Month.October, 2006));
            expectedHol.Add(new Date(6, Month.October, 2006));
            //    expectedHol.Add(new Date(7, Month.October,2006)); // Saturday
            expectedHol.Add(new Date(25, Month.December, 2006));

            expectedHol.Add(new Date(1, Month.January, 2007));
            //    expectedHol.Add(new Date(17, Month.February,2007)); // Saturday
            //    expectedHol.Add(new Date(18, Month.February,2007)); // Sunday
            expectedHol.Add(new Date(19, Month.February, 2007));
            expectedHol.Add(new Date(1, Month.March, 2007));
            expectedHol.Add(new Date(1, Month.May, 2007));
            //    expectedHol.Add(new Date(5, Month.May,2007)); // Saturday
            expectedHol.Add(new Date(24, Month.May, 2007));
            expectedHol.Add(new Date(6, Month.June, 2007));
            expectedHol.Add(new Date(17, Month.July, 2007));
            expectedHol.Add(new Date(15, Month.August, 2007));
            expectedHol.Add(new Date(24, Month.September, 2007));
            expectedHol.Add(new Date(25, Month.September, 2007));
            expectedHol.Add(new Date(26, Month.September, 2007));
            expectedHol.Add(new Date(3, Month.October, 2007));
            expectedHol.Add(new Date(19, Month.December, 2007)); // election
            expectedHol.Add(new Date(25, Month.December, 2007));

            Calendar c = new SouthKorea(SouthKorea.Market.Settlement);
            List<Date> hol = Calendar.holidayList(c, new Date(1, Month.January, 2004),
                                                             new Date(31, Month.December, 2007));
            for (int i = 0; i < Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i] != expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i] + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count != expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testKoreaStockExchange() {
            //("Testing Korea Stock Exchange holiday list...");

            List<Date> expectedHol = new List<Date>();
            expectedHol.Add(new Date(1, Month.January, 2004));
            expectedHol.Add(new Date(21, Month.January, 2004));
            expectedHol.Add(new Date(22, Month.January, 2004));
            expectedHol.Add(new Date(23, Month.January, 2004));
            expectedHol.Add(new Date(1, Month.March, 2004));
            expectedHol.Add(new Date(5, Month.April, 2004));
            expectedHol.Add(new Date(15, Month.April, 2004)); //election day
            //    expectedHol.Add(new Date(1, Month.May,2004)); // Saturday
            expectedHol.Add(new Date(5, Month.May, 2004));
            expectedHol.Add(new Date(26, Month.May, 2004));
            //    expectedHol.Add(new Date(6, Month.June,2004)); // Sunday
            //    expectedHol.Add(new Date(17, Month.July,2004)); // Saturday
            //    expectedHol.Add(new Date(15, Month.August,2004)); // Sunday
            expectedHol.Add(new Date(27, Month.September, 2004));
            expectedHol.Add(new Date(28, Month.September, 2004));
            expectedHol.Add(new Date(29, Month.September, 2004));
            //    expectedHol.Add(new Date(3, Month.October,2004)); // Sunday
            //    expectedHol.Add(new Date(25,December,2004)); // Saturday
            expectedHol.Add(new Date(31, Month.December, 2004));

            //    expectedHol.Add(new Date(1, Month.January,2005)); // Saturday
            expectedHol.Add(new Date(8, Month.February, 2005));
            expectedHol.Add(new Date(9, Month.February, 2005));
            expectedHol.Add(new Date(10, Month.February, 2005));
            expectedHol.Add(new Date(1, Month.March, 2005));
            expectedHol.Add(new Date(5, Month.April, 2005));
            expectedHol.Add(new Date(5, Month.May, 2005));
            //    expectedHol.Add(new Date(15, Month.May,2005)); // Sunday
            expectedHol.Add(new Date(6, Month.June, 2005));
            //    expectedHol.Add(new Date(17, Month.July,2005)); // Sunday
            expectedHol.Add(new Date(15, Month.August, 2005));
            //    expectedHol.Add(new Date(17, Month.September,2005)); // Saturday
            //    expectedHol.Add(new Date(18, Month.September,2005)); // Sunday
            expectedHol.Add(new Date(19, Month.September, 2005));
            expectedHol.Add(new Date(3, Month.October, 2005));
            //    expectedHol.Add(new Date(25,December,2005)); // Sunday
            expectedHol.Add(new Date(30, Month.December, 2005));

            //    expectedHol.Add(new Date(1, Month.January,2006)); // Sunday
            //    expectedHol.Add(new Date(28, Month.January,2006)); // Saturday
            //    expectedHol.Add(new Date(29, Month.January,2006)); // Sunday
            expectedHol.Add(new Date(30, Month.January, 2006));
            expectedHol.Add(new Date(1, Month.March, 2006));
            expectedHol.Add(new Date(1, Month.May, 2006));
            expectedHol.Add(new Date(5, Month.May, 2006));
            expectedHol.Add(new Date(31, Month.May, 2006)); // election
            expectedHol.Add(new Date(6, Month.June, 2006));
            expectedHol.Add(new Date(17, Month.July, 2006));
            expectedHol.Add(new Date(15, Month.August, 2006));
            expectedHol.Add(new Date(3, Month.October, 2006));
            expectedHol.Add(new Date(5, Month.October, 2006));
            expectedHol.Add(new Date(6, Month.October, 2006));
            //    expectedHol.Add(new Date(7, Month.October,2006)); // Saturday
            expectedHol.Add(new Date(25, Month.December, 2006));
            expectedHol.Add(new Date(29, Month.December, 2006));

            expectedHol.Add(new Date(1, Month.January, 2007));
            //    expectedHol.Add(new Date(17, Month.February,2007)); // Saturday
            //    expectedHol.Add(new Date(18, Month.February,2007)); // Sunday
            expectedHol.Add(new Date(19, Month.February, 2007));
            expectedHol.Add(new Date(1, Month.March, 2007));
            expectedHol.Add(new Date(1, Month.May, 2007));
            //    expectedHol.Add(new Date(5, Month.May,2007)); // Saturday
            expectedHol.Add(new Date(24, Month.May, 2007));
            expectedHol.Add(new Date(6, Month.June, 2007));
            expectedHol.Add(new Date(17, Month.July, 2007));
            expectedHol.Add(new Date(15, Month.August, 2007));
            expectedHol.Add(new Date(24, Month.September, 2007));
            expectedHol.Add(new Date(25, Month.September, 2007));
            expectedHol.Add(new Date(26, Month.September, 2007));
            expectedHol.Add(new Date(3, Month.October, 2007));
            expectedHol.Add(new Date(19, Month.December, 2007)); // election
            expectedHol.Add(new Date(25, Month.December, 2007));
            expectedHol.Add(new Date(31, Month.December, 2007));

            Calendar c = new SouthKorea(SouthKorea.Market.KRX);
            List<Date> hol = Calendar.holidayList(c, new Date(1, Month.January, 2004),
                                                             new Date(31, Month.December, 2007));

            for (int i = 0; i < Math.Min(hol.Count, expectedHol.Count); i++) {
                if (hol[i] != expectedHol[i])
                    QAssert.Fail("expected holiday was " + expectedHol[i]
                               + " while calculated holiday is " + hol[i]);
            }
            if (hol.Count != expectedHol.Count)
                QAssert.Fail("there were " + expectedHol.Count
                           + " expected holidays, while there are " + hol.Count
                           + " calculated holidays");
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
   public void testChinaSSE() 
   {
      // Testing China Shanghai Stock Exchange holiday list

      List<Date> expectedHol = new List<Date>();

      // China Shanghai Securities Exchange holiday list in the year 2014
      expectedHol.Add(new Date(1, Month.Jan, 2014));
      expectedHol.Add(new Date(31, Month.Jan, 2014));
      expectedHol.Add(new Date(3, Month.Feb, 2014));
      expectedHol.Add(new Date(4, Month.Feb, 2014));
      expectedHol.Add(new Date(5, Month.Feb, 2014));
      expectedHol.Add(new Date(6, Month.Feb, 2014));
      expectedHol.Add(new Date(7, Month.Apr, 2014));
      expectedHol.Add(new Date(1, Month.May, 2014));
      expectedHol.Add(new Date(2, Month.May, 2014));
      expectedHol.Add(new Date(2, Month.Jun, 2014));
      expectedHol.Add(new Date(8, Month.Sep, 2014));
      expectedHol.Add(new Date(1, Month.Oct, 2014));
      expectedHol.Add(new Date(2, Month.Oct, 2014));
      expectedHol.Add(new Date(3, Month.Oct, 2014));
      expectedHol.Add(new Date(6, Month.Oct, 2014));
      expectedHol.Add(new Date(7, Month.Oct, 2014));

      // China Shanghai Securities Exchange holiday list in the year 2015
      expectedHol.Add(new Date(1, Month.Jan, 2015));
      expectedHol.Add(new Date(2, Month.Jan, 2015));
      expectedHol.Add(new Date(18,Month.Feb, 2015));
      expectedHol.Add(new Date(19,Month.Feb, 2015));
      expectedHol.Add(new Date(20,Month.Feb, 2015));
      expectedHol.Add(new Date(23,Month.Feb, 2015));
      expectedHol.Add(new Date(24,Month.Feb, 2015));
      expectedHol.Add(new Date(6, Month.Apr, 2015));
      expectedHol.Add(new Date(1, Month.May, 2015));
      expectedHol.Add(new Date(22,Month.Jun, 2015));
      expectedHol.Add(new Date(3, Month.Sep, 2015));
      expectedHol.Add(new Date(4, Month.Sep, 2015));
      expectedHol.Add(new Date(1, Month.Oct, 2015));
      expectedHol.Add(new Date(2, Month.Oct, 2015));
      expectedHol.Add(new Date(5, Month.Oct, 2015));
      expectedHol.Add(new Date(6, Month.Oct, 2015));
      expectedHol.Add(new Date(7, Month.Oct, 2015));

      // China Shanghai Securities Exchange holiday list in the year 2016
      expectedHol.Add(new Date(1, Month.Jan, 2016));
      expectedHol.Add(new Date(8, Month.Feb, 2016));
      expectedHol.Add(new Date(9, Month.Feb, 2016));
      expectedHol.Add(new Date(10,Month.Feb, 2016));
      expectedHol.Add(new Date(11,Month.Feb, 2016));
      expectedHol.Add(new Date(12,Month.Feb, 2016));
      expectedHol.Add(new Date(4, Month.Apr, 2016));
      expectedHol.Add(new Date(2, Month.May, 2016));
      expectedHol.Add(new Date(9, Month.Jun, 2016));
      expectedHol.Add(new Date(10,Month.Jun, 2016));
      expectedHol.Add(new Date(15,Month.Sep, 2016));
      expectedHol.Add(new Date(16,Month.Sep, 2016));
      expectedHol.Add(new Date(3, Month.Oct, 2016));
      expectedHol.Add(new Date(4, Month.Oct, 2016));
      expectedHol.Add(new Date(5, Month.Oct, 2016));
      expectedHol.Add(new Date(6, Month.Oct, 2016));
      expectedHol.Add(new Date(7, Month.Oct, 2016));


      Calendar c = new China(China.Market.SSE);
      List<Date> hol = Calendar.holidayList(c, new Date(1, Month.January, 2014),
         new Date(31, Month.December, 2016));

      for (int i = 0; i < Math.Min(hol.Count, expectedHol.Count); i++) 
      {
         if (hol[i] != expectedHol[i])
               QAssert.Fail("expected holiday was " + expectedHol[i]
                  + " while calculated holiday is " + hol[i]);
      }
      if (hol.Count != expectedHol.Count)
            QAssert.Fail("there were " + expectedHol.Count
            + " expected holidays, while there are " + hol.Count
            + " calculated holidays");
}

#if QL_DOTNET_FRAMEWORK
   [TestMethod()]
#else
   [Fact]
#endif
   public void testChinaIB() 
   {
      // Testing China Inter Bank working weekends list
      List<Date> expectedWorkingWeekEnds = new List<Date>();

      // China Inter Bank working weekends list in the year 2014
      expectedWorkingWeekEnds.Add(new Date(26,Month. Jan, 2014));
      expectedWorkingWeekEnds.Add(new Date(8, Month.Feb, 2014));
      expectedWorkingWeekEnds.Add(new Date(4, Month.May, 2014));
      expectedWorkingWeekEnds.Add(new Date(28,Month. Sep, 2014));
      expectedWorkingWeekEnds.Add(new Date(11,Month. Oct, 2014));

      // China Inter Bank working weekends list in the year 2015
      expectedWorkingWeekEnds.Add(new Date(4, Month.Jan, 2015));
      expectedWorkingWeekEnds.Add(new Date(15,Month. Feb, 2015));
      expectedWorkingWeekEnds.Add(new Date(28,Month. Feb, 2015));
      expectedWorkingWeekEnds.Add(new Date(6, Month.Sep, 2015));
      expectedWorkingWeekEnds.Add(new Date(10,Month. Oct, 2015));

      // China Inter Bank working weekends list in the year 2016
      expectedWorkingWeekEnds.Add(new Date(6, Month.Feb, 2016));
      expectedWorkingWeekEnds.Add(new Date(14,Month. Feb, 2016));
      expectedWorkingWeekEnds.Add(new Date(12,Month. Jun, 2016));
      expectedWorkingWeekEnds.Add(new Date(18,Month. Sep, 2016));
      expectedWorkingWeekEnds.Add(new Date(8, Month.Oct, 2016));
      expectedWorkingWeekEnds.Add(new Date(9, Month.Oct, 2016));

      Calendar c = new China(China.Market.IB);
      Date start = new Date(1, Month.Jan, 2014);
      Date end = new Date(31, Month.Dec, 2016);

      int k = 0;

      while (start <= end) 
      {
         if (c.isBusinessDay(start) && c.isWeekend(start.DayOfWeek)) 
         {
            if (expectedWorkingWeekEnds[k] != start)
                  QAssert.Fail("expected working weekend was " + expectedWorkingWeekEnds[k]
                     + " while calculated working weekend is " + start);
            ++k;
         }
         ++start;
      }
    
      if (k != (expectedWorkingWeekEnds.Count))
         QAssert.Fail("there were " + expectedWorkingWeekEnds.Count
            + " expected working weekends, while there are " + k
            + " calculated holidays");
}
#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testEndOfMonth() {
            //BOOST_MESSAGE("Testing end-of-month calculation...");

            Calendar c = new TARGET(); // any calendar would be OK

            Date eom, counter = Date.minDate();
            Date last = Date.maxDate() - new Period(2, TimeUnit.Months);

            while (counter<=last) {
                eom = c.endOfMonth(counter);
                // check that eom is eom
                if (!c.isEndOfMonth(eom))
                    QAssert.Fail("\n  "
                               + eom.weekday() + " " + eom
                               + " is not the last business day in "
                               + eom.month() + " " + eom.year()
                               + " according to " + c.name());
                // check that eom is in the same month as counter
                if (eom.month()!=counter.month())
                    QAssert.Fail("\n  "
                               + eom
                               + " is not in the same month as "
                               + counter);
                counter = counter + 1;
            }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testBusinessDaysBetween() {

            // Testing calculation of business days between dates

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

            long[] expected = {
                1,
                321,
                152,
                251,
                252,
                10,
                48,
                42,
                -38,
                38,
                51
            };

            Calendar calendar = new Brazil();

            for (int i=1; i<testDates.Count; i++) {
                int calculated = calendar.businessDaysBetween(testDates[i-1], testDates[i]);
                if (calculated != expected[i-1]) {
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
        public void testBespokeCalendars() {

            // Testing bespoke calendars

            BespokeCalendar a1 = new BespokeCalendar();
            BespokeCalendar b1 = new BespokeCalendar();

            Date testDate1 = new Date(4, Month.October, 2008); // Saturday
            Date testDate2 = new Date(5, Month.October, 2008); // Sunday
            Date testDate3 = new Date(6, Month.October, 2008); // Monday
            Date testDate4 = new Date(7, Month.October, 2008); // Tuesday

            if (!a1.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " erroneously detected as holiday");
            if (!a1.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " erroneously detected as holiday");
            if (!a1.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " erroneously detected as holiday");
            if (!a1.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " erroneously detected as holiday");

            if (!b1.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " erroneously detected as holiday");

            a1.addWeekend(DayOfWeek.Sunday);

            if (!a1.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " erroneously detected as holiday");
            if (a1.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " (Sunday) not detected as weekend");
            if (!a1.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " erroneously detected as holiday");
            if (!a1.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " erroneously detected as holiday");

            if (!b1.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " erroneously detected as holiday");

            a1.addHoliday(testDate3);

            if (!a1.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " erroneously detected as holiday");
            if (a1.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " (Sunday) not detected as weekend");
            if (a1.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " (marked as holiday) not detected");
            if (!a1.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " erroneously detected as holiday");

            if (!b1.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " erroneously detected as holiday");
            if (!b1.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " erroneously detected as holiday");

            BespokeCalendar a2 = a1;  // linked to a1

            a2.addWeekend(DayOfWeek.Saturday);

            if (a1.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " (Saturday) not detected as weekend");
            if (a1.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " (Sunday) not detected as weekend");
            if (a1.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " (marked as holiday) not detected");
            if (!a1.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " erroneously detected as holiday");

            if (a2.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " (Saturday) not detected as weekend");
            if (a2.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " (Sunday) not detected as weekend");
            if (a2.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " (marked as holiday) not detected");
            if (!a2.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " erroneously detected as holiday");

            a2.addHoliday(testDate4);

            if (a1.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " (Saturday) not detected as weekend");
            if (a1.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " (Sunday) not detected as weekend");
            if (a1.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " (marked as holiday) not detected");
            if (a1.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " (marked as holiday) not detected");

            if (a2.isBusinessDay(testDate1))
                QAssert.Fail(testDate1 + " (Saturday) not detected as weekend");
            if (a2.isBusinessDay(testDate2))
                QAssert.Fail(testDate2 + " (Sunday) not detected as weekend");
            if (a2.isBusinessDay(testDate3))
                QAssert.Fail(testDate3 + " (marked as holiday) not detected");
            if (a2.isBusinessDay(testDate4))
                QAssert.Fail(testDate4 + " (marked as holiday) not detected");
        }
    }
}
