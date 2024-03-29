/*
 Copyright (C) 2008-2022  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Globalization;
using System.Numerics;
using TestSuite;
using Xunit;

namespace QLNet.Tests
{
   [Collection("QLNet CI Tests")]
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

      public struct Thirty360Case
      {
         public Thirty360Case(Date start, Date end, int expected)
         {
            _start = start;
            _end = end;
            _expected = expected;
         }
         public Date _start;
         public Date _end;
         public int _expected;
      }

      private double actualActualDaycountComputation(Schedule schedule,
                                                     Date start, Date end)
      {

         DayCounter daycounter = new ActualActual(ActualActual.Convention.ISMA, schedule);
         double yearFraction = 0.0;

         for (int i = 1; i < schedule.size() - 1; i++)
         {
            Date referenceStart = schedule.date(i);
            Date referenceEnd = schedule.date(i + 1);
            if (start < referenceEnd && end > referenceStart)
            {
               yearFraction += ISMAYearFractionWithReferenceDates(
                                  daycounter,
                                  (start > referenceStart) ? start : referenceStart,
                                  (end < referenceEnd) ? end : referenceEnd,
                                  referenceStart,
                                  referenceEnd
                               );
            };
         }
         return yearFraction;
      }

      private double ISMAYearFractionWithReferenceDates(DayCounter dayCounter, Date start, Date end,
                                                        Date refStart, Date refEnd)
      {
         double referenceDayCount = dayCounter.dayCount(refStart, refEnd);
         // guess how many coupon periods per year:
         int couponsPerYear = (int)(0.5 + 365.0 / referenceDayCount);
         // the above is good enough for annual or semi annual payments.
         return (dayCounter.dayCount(start, end))
                / (referenceDayCount * couponsPerYear);
      }

      [Fact]
      public void testActualActual()
      {
         SingleCase[] testCases =
         {
            // first example
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(1, Month.November, 2003), new Date(1, Month.May, 2004),
                           0.497724380567),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(1, Month.November, 2003), new Date(1, Month.May, 2004),
                           new Date(1, Month.November, 2003), new Date(1, Month.May, 2004),
                           0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(1, Month.November, 2003), new Date(1, Month.May, 2004),
                           0.497267759563),
            // short first calculation period (first period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(1, Month.February, 1999), new Date(1, Month.July, 1999),
                           0.410958904110),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(1, Month.February, 1999), new Date(1, Month.July, 1999),
                           new Date(1, Month.July, 1998), new Date(1, Month.July, 1999),
                           0.410958904110),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(1, Month.February, 1999), new Date(1, Month.July, 1999),
                           0.410958904110),
            // short first calculation period (second period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(1, Month.July, 1999), new Date(1, Month.July, 2000),
                           1.001377348600),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(1, Month.July, 1999), new Date(1, Month.July, 2000),
                           new Date(1, Month.July, 1999), new Date(1, Month.July, 2000),
                           1.000000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(1, Month.July, 1999), new Date(1, Month.July, 2000),
                           1.000000000000),
            // long first calculation period (first period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(15, Month.August, 2002), new Date(15, Month.July, 2003),
                           0.915068493151),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(15, Month.August, 2002), new Date(15, Month.July, 2003),
                           new Date(15, Month.January, 2003), new Date(15, Month.July, 2003),
                           0.915760869565),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(15, Month.August, 2002), new Date(15, Month.July, 2003),
                           0.915068493151),
            // long first calculation period (second period)
            /* Warning: the ISDA case is in disagreement with mktc1198.pdf */
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(15, Month.July, 2003), new Date(15, Month.January, 2004),
                           0.504004790778),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(15, Month.July, 2003), new Date(15, Month.January, 2004),
                           new Date(15, Month.July, 2003), new Date(15, Month.January, 2004),
                           0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(15, Month.July, 2003), new Date(15, Month.January, 2004),
                           0.504109589041),
            // short final calculation period (penultimate period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(30, Month.July, 1999), new Date(30, Month.January, 2000),
                           0.503892506924),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(30, Month.July, 1999), new Date(30, Month.January, 2000),
                           new Date(30, Month.July, 1999), new Date(30, Month.January, 2000),
                           0.500000000000),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(30, Month.July, 1999), new Date(30, Month.January, 2000),
                           0.504109589041),
            // short final calculation period (final period)
            new SingleCase(ActualActual.Convention.ISDA,
                           new Date(30, Month.January, 2000), new Date(30, Month.June, 2000),
                           0.415300546448),
            new SingleCase(ActualActual.Convention.ISMA,
                           new Date(30, Month.January, 2000), new Date(30, Month.June, 2000),
                           new Date(30, Month.January, 2000), new Date(30, Month.July, 2000),
                           0.417582417582),
            new SingleCase(ActualActual.Convention.AFB,
                           new Date(30, Month.January, 2000), new Date(30, Month.June, 2000),
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

      [Fact]
      public void testActualActualWithSemiannualSchedule()
      {

         // Testing actual/actual with schedule for undefined semiannual reference periods

         Calendar calendar = new UnitedStates();
         Date fromDate = new Date(10, Month.January, 2017);
         Date firstCoupon = new Date(31, Month.August, 2017);
         Date quasiCoupon = new Date(28, Month.February, 2017);
         Date quasiCoupon2 = new Date(31, Month.August, 2016);

         Schedule schedule = new MakeSchedule()
         .from(fromDate)
         .withFirstDate(firstCoupon)
         .to(new Date(28, Month.February, 2026))
         .withFrequency(Frequency.Semiannual)
         .withCalendar(calendar)
         .withConvention(BusinessDayConvention.Unadjusted)
         .backwards().endOfMonth(true).value();

         Date testDate = schedule.date(1);
         DayCounter dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);
         DayCounter dayCounterNoSchedule = new ActualActual(ActualActual.Convention.ISMA);

         Date referencePeriodStart = schedule.date(1);
         Date referencePeriodEnd = schedule.date(2);

         // Test
         QAssert.IsTrue(dayCounter.yearFraction(referencePeriodStart,
                                                referencePeriodStart).IsEqual(0.0), "This should be zero.");

         QAssert.IsTrue(dayCounterNoSchedule.yearFraction(referencePeriodStart,
                                                          referencePeriodStart).IsEqual(0.0), "This should be zero");

         QAssert.IsTrue(dayCounterNoSchedule.yearFraction(referencePeriodStart,
                                                          referencePeriodStart, referencePeriodStart, referencePeriodStart).IsEqual(0.0),
                        "This should be zero");

         QAssert.IsTrue(dayCounter.yearFraction(referencePeriodStart,
                                                referencePeriodEnd).IsEqual(0.5),
                        "This should be exact using schedule; "
                        + referencePeriodStart + " to " + referencePeriodEnd
                        + "Should be 0.5");

         QAssert.IsTrue(dayCounterNoSchedule.yearFraction(referencePeriodStart,
                                                          referencePeriodEnd, referencePeriodStart, referencePeriodEnd).IsEqual(0.5),
                        "This should be exact for explicit reference periods with no schedule");

         while (testDate < referencePeriodEnd)
         {
            double difference =
               dayCounter.yearFraction(testDate, referencePeriodEnd,
                                       referencePeriodStart, referencePeriodEnd) -
               dayCounter.yearFraction(testDate, referencePeriodEnd);
            if (Math.Abs(difference) > 1.0e-10)
            {
               QAssert.Fail("Failed to correctly use the schedule to find the reference period for Act/Act");
            }
            testDate = calendar.advance(testDate, 1, TimeUnit.Days);
         }

         //Test long first coupon
         double calculatedYearFraction =
            dayCounter.yearFraction(fromDate, firstCoupon);
         double expectedYearFraction =
            0.5 + ((double)dayCounter.dayCount(fromDate, quasiCoupon))
            / (2 * dayCounter.dayCount(quasiCoupon2, quasiCoupon));

         QAssert.IsTrue(Math.Abs(calculatedYearFraction - expectedYearFraction) < 1.0e-10,
                        "Failed to compute the expected year fraction " +
                        "\n expected:   " + expectedYearFraction +
                        "\n calculated: " + calculatedYearFraction);

         // test multiple periods

         schedule = new MakeSchedule()
         .from(new Date(10, Month.January, 2017))
         .withFirstDate(new Date(31, Month.August, 2017))
         .to(new Date(28, Month.February, 2026))
         .withFrequency(Frequency.Semiannual)
         .withCalendar(calendar)
         .withConvention(BusinessDayConvention.Unadjusted)
         .backwards().endOfMonth(false).value();

         Date periodStartDate = schedule.date(1);
         Date periodEndDate = schedule.date(2);

         dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);

         while (periodEndDate < schedule.date(schedule.size() - 2))
         {
            double expected =
               actualActualDaycountComputation(schedule,
                                               periodStartDate,
                                               periodEndDate);
            double calculated = dayCounter.yearFraction(periodStartDate,
                                                        periodEndDate);

            if (Math.Abs(expected - calculated) > 1e-8)
            {
               QAssert.Fail("Failed to compute the correct year fraction " +
                            "given a schedule: " + periodStartDate +
                            " to " + periodEndDate +
                            "\n expected: " + expected +
                            " calculated: " + calculated);
            }
            periodEndDate = calendar.advance(periodEndDate, 1, TimeUnit.Days);
         }
      }

      [Fact]
      public void testActualActualWithAnnualSchedule()
      {
         // Testing actual/actual with schedule "for undefined annual reference periods

         // Now do an annual schedule
         Calendar calendar = new UnitedStates();
         Schedule schedule = new MakeSchedule()
         .from(new Date(10, Month.January, 2017))
         .withFirstDate(new Date(31, Month.August, 2017))
         .to(new Date(28, Month.February, 2026))
         .withFrequency(Frequency.Annual)
         .withCalendar(calendar)
         .withConvention(BusinessDayConvention.Unadjusted)
         .backwards().endOfMonth(false).value();

         Date referencePeriodStart = schedule.date(1);
         Date referencePeriodEnd = schedule.date(2);

         Date testDate = schedule.date(1);
         DayCounter dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);

         while (testDate < referencePeriodEnd)
         {
            double difference =
               ISMAYearFractionWithReferenceDates(dayCounter,
                                                  testDate, referencePeriodEnd,
                                                  referencePeriodStart, referencePeriodEnd) -
               dayCounter.yearFraction(testDate, referencePeriodEnd);
            if (Math.Abs(difference) > 1.0e-10)
            {
               QAssert.Fail("Failed to correctly use the schedule " +
                            "to find the reference period for Act/Act:\n"
                            + testDate + " to " + referencePeriodEnd
                            + "\n Ref: " + referencePeriodStart
                            + " to " + referencePeriodEnd);
            }
            testDate = calendar.advance(testDate, 1, TimeUnit.Days);
         }
      }

      [Fact]
      public void testActualActualWithSchedule()
      {

         // Testing actual/actual day counter with schedule

         // long first coupon
         Date issueDateExpected = new Date(17, Month.January, 2017);
         Date firstCouponDateExpected = new Date(31, Month.August, 2017);

         Schedule schedule =
            new MakeSchedule()
         .from(issueDateExpected)
         .withFirstDate(firstCouponDateExpected)
         .to(new Date(28, Month.February, 2026))
         .withFrequency(Frequency.Semiannual)
         .withCalendar(new Canada())
         .withConvention(BusinessDayConvention.Unadjusted)
         .backwards()
         .endOfMonth().value();

         Date issueDate = schedule.date(0);
         Utils.QL_REQUIRE(issueDate == issueDateExpected, () =>
                          "This is not the expected issue date " + issueDate
                          + " expected " + issueDateExpected);
         Date firstCouponDate = schedule.date(1);
         Utils.QL_REQUIRE(firstCouponDate == firstCouponDateExpected, () =>
                          "This is not the expected first coupon date " + firstCouponDate
                          + " expected: " + firstCouponDateExpected);

         //Make thw quasi coupon dates:
         Date quasiCouponDate2 = schedule.calendar().advance(firstCouponDate,
                                                             -schedule.tenor(),
                                                             schedule.businessDayConvention(),
                                                             schedule.endOfMonth());
         Date quasiCouponDate1 = schedule.calendar().advance(quasiCouponDate2,
                                                             -schedule.tenor(),
                                                             schedule.businessDayConvention(),
                                                             schedule.endOfMonth());

         Date quasiCouponDate1Expected = new Date(31, Month.August, 2016);
         Date quasiCouponDate2Expected = new Date(28, Month.February, 2017);

         Utils.QL_REQUIRE(quasiCouponDate2 == quasiCouponDate2Expected, () =>
                          "Expected " + quasiCouponDate2Expected
                          + " as the later quasi coupon date but received "
                          + quasiCouponDate2);
         Utils.QL_REQUIRE(quasiCouponDate1 == quasiCouponDate1Expected, () =>
                          "Expected " + quasiCouponDate1Expected
                          + " as the earlier quasi coupon date but received "
                          + quasiCouponDate1);

         DayCounter dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);

         // full coupon
         double t_with_reference = dayCounter.yearFraction(
                                      issueDate, firstCouponDate,
                                      quasiCouponDate2, firstCouponDate
                                   );
         double t_no_reference = dayCounter.yearFraction(
                                    issueDate,
                                    firstCouponDate
                                 );
         double t_total =
            ISMAYearFractionWithReferenceDates(dayCounter,
                                               issueDate, quasiCouponDate2,
                                               quasiCouponDate1, quasiCouponDate2)
            + 0.5;
         double expected = 0.6160220994;


         if (Math.Abs(t_total - expected) > 1.0e-10)
         {
            QAssert.Fail("Failed to reproduce expected time:\n"
                         + "    calculated: " + t_total + "\n"
                         + "    expected:   " + expected);
         }
         if (Math.Abs(t_with_reference - expected) > 1.0e-10)
         {
            QAssert.Fail("Failed to reproduce expected time:\n"
                         + "    calculated: " + t_with_reference + "\n"
                         + "    expected:   " + expected);
         }
         if (Math.Abs(t_no_reference - t_with_reference) > 1.0e-10)
         {
            QAssert.Fail("Should produce the same time whether or not references are present");
         }

         // settlement date in the first quasi-period
         Date settlementDate = new Date(29, Month.January, 2017);

         t_with_reference = ISMAYearFractionWithReferenceDates(
                               dayCounter,
                               issueDate, settlementDate,
                               quasiCouponDate1, quasiCouponDate2
                            );
         t_no_reference = dayCounter.yearFraction(issueDate, settlementDate);
         double t_expected_first_qp = 0.03314917127071823; //12.0/362
         if (Math.Abs(t_with_reference - t_expected_first_qp) > 1.0e-10)
         {
            QAssert.Fail("Failed to reproduce expected time:\n"
                         + "    calculated: " + t_no_reference + "\n"
                         + "    expected:   " + t_expected_first_qp);
         }
         if (Math.Abs(t_no_reference - t_with_reference) > 1.0e-10)
         {
            QAssert.Fail("Should produce the same time whether or not references are present");
         }
         double t2 = dayCounter.yearFraction(settlementDate, firstCouponDate);
         if (Math.Abs(t_expected_first_qp + t2 - expected) > 1.0e-10)
         {
            QAssert.Fail("Sum of quasiperiod2 split is not consistent");
         }

         // settlement date in the second quasi-period
         settlementDate = new Date(29, Month.July, 2017);
         t_no_reference = dayCounter.yearFraction(issueDate, settlementDate);
         t_with_reference = ISMAYearFractionWithReferenceDates(
                               dayCounter,
                               issueDate, quasiCouponDate2,
                               quasiCouponDate1, quasiCouponDate2
                            ) + ISMAYearFractionWithReferenceDates(
                               dayCounter,
                               quasiCouponDate2, settlementDate,
                               quasiCouponDate2, firstCouponDate
                            );
         if (Math.Abs(t_no_reference - t_with_reference) > 1.0e-10)
         {
            QAssert.Fail("These two cases should be identical");
         }
         t2 = dayCounter.yearFraction(settlementDate, firstCouponDate);
         if (Math.Abs(t_total - (t_no_reference + t2)) > 1.0e-10)
         {
            QAssert.Fail("Failed to reproduce expected time:\n"
                         + "    calculated: " + t_total + "\n"
                         + "    expected:   " + t_no_reference + t2);
         }
      }

      [Fact]
      public void testSimple()
      {
         Period[] p = { new Period(3, TimeUnit.Months), new Period(6, TimeUnit.Months), new Period(1, TimeUnit.Years) };
         double[] expected = { 0.25, 0.5, 1.0 };
         int n = p.Length;

         // 4 years should be enough
         Date first = new Date(1, Month.January, 2002), last = new Date(31, Month.December, 2005);
         DayCounter dayCounter = new SimpleDayCounter();

         for (Date start = first; start <= last; start++)
         {
            for (int i = 0; i < n; i++)
            {
               Date end = start + p[i];
               double calculated = dayCounter.yearFraction(start, end, null, null);
               if (Math.Abs(calculated - expected[i]) > 1.0e-12)
               {
                  QAssert.Fail("from " + start + " to " + end +
                               "Calculated: " + calculated +
                               "Expected:   " + expected[i]);
               }
            }
         }

      }

      [Fact]
      public void testOne()
      {
         Period[] p = { new Period(3, TimeUnit.Months), new Period(6, TimeUnit.Months), new Period(1, TimeUnit.Years) };
         double[] expected = { 1.0, 1.0, 1.0 };
         int n = p.Length;

         // 1 years should be enough
         Date first = new Date(1, Month.January, 2004), last = new Date(31, Month.December, 2004);
         DayCounter dayCounter = new OneDayCounter();

         for (Date start = first; start <= last; start++)
         {
            for (int i = 0; i < n; i++)
            {
               Date end = start + p[i];
               double calculated = dayCounter.yearFraction(start, end, null, null);
               if (Math.Abs(calculated - expected[i]) > 1.0e-12)
               {
                  QAssert.Fail("from " + start + " to " + end +
                               "Calculated: " + calculated +
                               "Expected:   " + expected[i]);
               }
            }
         }

      }

      [Fact]
      public void testBusiness252()
      {
         // Testing business/252 day counter

         List<Date> testDates = new List<Date>();
         testDates.Add(new Date(1, Month.February, 2002));
         testDates.Add(new Date(4, Month.February, 2002));
         testDates.Add(new Date(16, Month.May, 2003));
         testDates.Add(new Date(17, Month.December, 2003));
         testDates.Add(new Date(17, Month.December, 2004));
         testDates.Add(new Date(19, Month.December, 2005));
         testDates.Add(new Date(2, Month.January, 2006));
         testDates.Add(new Date(13, Month.March, 2006));
         testDates.Add(new Date(15, Month.May, 2006));
         testDates.Add(new Date(17, Month.March, 2006));
         testDates.Add(new Date(15, Month.May, 2006));
         testDates.Add(new Date(26, Month.July, 2006));
         testDates.Add(new Date(28, Month.June, 2007));
         testDates.Add(new Date(16, Month.September, 2009));
         testDates.Add(new Date(26, Month.July, 2016));

         double[] expected =
         {
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

         for (int i = 1; i < testDates.Count; i++)
         {
            calculated = dayCounter1.yearFraction(testDates[i - 1], testDates[i]);
            if (Math.Abs(calculated - expected[i - 1]) > 1.0e-12)
            {
               QAssert.Fail("from " + testDates[i - 1]
                            + " to " + testDates[i] + ":\n"
                            + "    calculated: " + calculated + "\n"
                            + "    expected:   " + expected[i - 1]);
            }
         }

         DayCounter dayCounter2 = new Business252();

         for (int i = 1; i < testDates.Count; i++)
         {
            calculated = dayCounter2.yearFraction(testDates[i - 1], testDates[i]);
            if (Math.Abs(calculated - expected[i - 1]) > 1.0e-12)
            {
               QAssert.Fail("from " + testDates[i - 1]
                            + " to " + testDates[i] + ":\n"
                            + "    calculated: " + calculated + "\n"
                            + "    expected:   " + expected[i - 1]);

            }
         }
      }

      [Fact]
      public void testThirty365()
      {
         // Testing 30/365 day counter

         Date d1 = new(17,Month.June,2011), d2 = new(30,Month.December,2012);
         DayCounter dayCounter = new Thirty365();

         BigInteger days = dayCounter.dayCount(d1,d2);
         if (days != 553)
         {
            QAssert.Fail("from " + d1 + " to " + d2 + ":\n"
                       + "    calculated: " + days + "\n"
                       + "    expected:   " + 553);
         }

         var t = dayCounter.yearFraction(d1,d2);
         var expected = 553/365.0;
         if (Math.Abs(t-expected) > 1.0e-12)
         {
            QAssert.Fail("from " + d1 + " to " + d2 + ":\n"
                         + "    calculated: " + t + "\n"
                         + "    expected:   " + expected);
         }
      }

      [Fact]
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
                            359,  32,  33
                          };

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

      [Fact]
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
                            359,  32,  32
                          };

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

      [Fact]
      public void testThirty360_ISDA()
      {
         // Testing 30/360 day counter (ISDA)
         // See https://www.isda.org/2008/12/22/30-360-day-count-conventions/

         Thirty360Case[] data1 =
         {
           // Example 1: End dates do not involve the last day of February
           new Thirty360Case(new Date(20, Month.August, 2006), new Date(20, Month.February, 2007), 180),
           new Thirty360Case(new Date(20, Month.February, 2007),  new Date(20, Month.August, 2007),   180),
           new Thirty360Case(new Date(20, Month.August, 2007),    new Date(20, Month.February, 2008), 180),
           new Thirty360Case(new Date(20, Month.February, 2008),  new Date(20, Month.August, 2008),   180),
           new Thirty360Case(new Date(20, Month.August, 2008),    new Date(20, Month.February, 2009), 180),
           new Thirty360Case(new Date(20, Month.February, 2009),  new Date(20, Month.August, 2009),   180)
         };                                                                                                

         var terminationDate = new Date(20, Month.August, 2009);
         var dayCounter = new Thirty360(Thirty360.Thirty360Convention.ISDA, terminationDate);

         foreach (var x in data1)
         {
           var calculated = dayCounter.dayCount(x._start, x._end);
           if (calculated != x._expected)
           {
             QAssert.Fail("from " + x._start
                         + " to " + x._end + ":\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + x._expected);
           }
         }

         Thirty360Case[] data2 =
         {
            // Example 2: End dates include some end-February dates
            new Thirty360Case( new Date(28, Month.February, 2006),  new Date(31, Month.August, 2006),   180),
            new Thirty360Case( new Date(31, Month.August, 2006),    new Date(28, Month.February, 2007), 180),
            new Thirty360Case( new Date(28, Month.February, 2007),  new Date(31, Month.August, 2007),   180),
            new Thirty360Case( new Date(31, Month.August, 2007),    new Date(29, Month.February, 2008), 180),
            new Thirty360Case( new Date(29, Month.February, 2008),  new Date(31, Month.August, 2008),   180),
            new Thirty360Case( new Date(31, Month.August, 2008),    new Date(28, Month.February, 2009), 180),
            new Thirty360Case( new Date(28, Month.February, 2009),  new Date(31, Month.August, 2009),   180),
            new Thirty360Case( new Date(31, Month.August, 2009),    new Date(28, Month.February, 2010), 180),
            new Thirty360Case( new Date(28, Month.February, 2010),  new Date(31, Month.August, 2010),   180),
            new Thirty360Case( new Date(31, Month.August, 2010),    new Date(28, Month.February, 2011), 180),
            new Thirty360Case( new Date(28, Month.February, 2011),  new Date(31, Month.August, 2011),   180),
            new Thirty360Case( new Date(31, Month.August, 2011),    new Date(29, Month.February, 2012), 179)
         };                                                                                                 

         terminationDate = new Date(29, Month.February, 2012);
         dayCounter = new Thirty360(Thirty360.Thirty360Convention.ISDA, terminationDate);

         foreach (var x in data2)
         {
            var calculated = dayCounter.dayCount(x._start, x._end);
            if (calculated != x._expected)
            {
               QAssert.Fail("from " + x._start
                         + " to " + x._end + ":\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + x._expected);
            }
         }

         Thirty360Case[] data3 =
         {
              // Example 3: Miscellaneous calculations
              new Thirty360Case( new  Date(31, Month.January, 2006),   new Date(28, Month.February, 2006),  30),
              new Thirty360Case( new  Date(30, Month.January, 2006),   new Date(28, Month.February, 2006),  30),
              new Thirty360Case( new  Date(28, Month.February, 2006),  new Date(3,  Month.March, 2006),      3),
              new Thirty360Case( new  Date(14, Month.February, 2006),  new Date(28, Month.February, 2006),  16),
              new Thirty360Case( new  Date(30, Month.September, 2006), new Date(31, Month.October, 2006),   30),
              new Thirty360Case( new  Date(31, Month.October, 2006),   new Date(28, Month.November, 2006),  28),
              new Thirty360Case( new  Date(31, Month.August, 2007),    new Date(28, Month.February, 2008), 178),
              new Thirty360Case( new  Date(28, Month.February, 2008),  new Date(28, Month.August, 2008),   180),
              new Thirty360Case( new  Date(28, Month.February, 2008),  new Date(30, Month.August, 2008),   182),
              new Thirty360Case( new  Date(28, Month.February, 2008),  new Date(31, Month.August, 2008),   182),
              new Thirty360Case( new  Date(28, Month.February, 2007),  new Date(28, Month.February, 2008), 358),
              new Thirty360Case( new  Date(28, Month.February, 2007),  new Date(29, Month.February, 2008), 359),
              new Thirty360Case( new  Date(29, Month.February, 2008),  new Date(28, Month.February, 2009), 360),
              new Thirty360Case( new  Date(29, Month.February, 2008),  new Date(30, Month.March, 2008),     30),
              new Thirty360Case( new  Date(29, Month.February, 2008),  new Date(31, Month.March, 2008),     30)
         };

         terminationDate = new Date(29, Month.February, 2008);
         dayCounter = new Thirty360(Thirty360.Thirty360Convention.ISDA, terminationDate);

         foreach (var x in data3)
         {
            var calculated = dayCounter.dayCount(x._start, x._end);
            if (calculated != x._expected)
            {
               QAssert.Fail("from " + x._start
                         + " to " + x._end + ":\n"
                         + "    calculated: " + calculated + "\n"
                         + "    expected:   " + x._expected);
            }
         }
      }

      [Fact]
      public void testActual365_Canadian()
      {
         // Testing that Actual/365 (Canadian) throws when needed
         var dayCounter = new Actual365Fixed(Actual365Fixed.Convention.Canadian);

         try
         {
            // no reference period
            dayCounter.yearFraction( new Date(10, Month.September, 2018), new Date(10, Month.September, 2019));
            QAssert.Fail("Invalid call to yearFraction failed to throw");
         }
         catch 
         {
            ;  // expected
         }

         try
         {
            // reference period shorter than a month
            dayCounter.yearFraction( new Date(10, Month.September, 2018),
               new Date(12, Month.September, 2018),
               new Date(10, Month.September, 2018),
               new Date(15, Month.September, 2018));
            QAssert.Fail("Invalid call to yearFraction failed to throw");
         }
         catch
         {
            ;  // expected
         }
      }

      [Fact]
      public void testIntraday()
      {
         // Testing intraday behavior of day counter

         Date d1 = new Date(12, Month.February, 2015);
         Date d2 = new Date(14, Month.February, 2015, 12, 34, 17, 1);

         double tol = 100 * Const.QL_EPSILON;

         DayCounter[] dayCounters = { new ActualActual(), new Actual365Fixed(), new Actual360() };

         for (int i = 0; i < dayCounters.Length; ++i)
         {
            DayCounter dc = dayCounters[i];

            double expected = ((12 * 60 + 34) * 60 + 17 + 0.001)
                              * dc.yearFraction(d1, d1 + 1) / 86400
                              + dc.yearFraction(d1, d1 + 2);

            QAssert.IsTrue(Math.Abs(dc.yearFraction(d1, d2) - expected) < tol,
                           "can not reproduce result for day counter " + dc.name());

            QAssert.IsTrue(Math.Abs(dc.yearFraction(d2, d1) + expected) < tol,
                           "can not reproduce result for day counter " + dc.name());
         }
      }

      /// <summary>
      /// https://www.isda.org/book/actualactual-day-count-fraction/
      /// </summary>
      /// <param name="isEndOfMonth"></param>
      /// <param name="frequency"></param>
      /// <param name="interestAccrualDateAsString"></param>
      /// <param name="maturityDateAsString"></param>
      /// <param name="firstCouponDateAsString"></param>
      /// <param name="penultimateCouponDateAsString"></param>
      /// <param name="d1AsString"></param>
      /// <param name="d2AsString"></param>
      /// <param name="expectedYearFraction"></param>
      [Theory]
      [InlineData(false, Frequency.Semiannual, "2003-05-01", "2005-05-01", "2003-11-01", "2004-11-01", "2003-11-01", "2004-05-01", 182.0 / (182.0 * 2))] // example a: regular calculation period
      [InlineData(false, Frequency.Annual, "1999-02-01", "2002-07-01", "1999-07-01", "2001-07-01", "1999-02-01", "1999-07-01", 150.0 / (365.0 * 1))] // example b: short first calculation period - first period
      [InlineData(false, Frequency.Annual, "1999-02-01", "2002-07-01", "1999-07-01", "2001-07-01", "1999-07-01", "2000-07-01", 366.0 / (366.0 * 1))] // example b: short first calculation period - second period
      [InlineData(false, Frequency.Semiannual, "2002-08-15", "2005-07-15", "2003-07-15", "2004-07-15", "2002-08-15", "2003-07-15", (181.0 / (181.0 * 2)) + (153.0 / (184.0 * 2)))] // example c: long first calculation period - first period
      [InlineData(false, Frequency.Semiannual, "2002-08-15", "2005-07-15", "2003-07-15", "2004-07-15", "2003-07-15", "2004-01-15", 184.0 / (184.0 * 2))] // example c: long first calculation period - second period
      [InlineData(false, Frequency.Semiannual, "1999-01-30", "2000-06-30", "1999-07-30", "2000-01-30", "1999-07-30", "2000-01-30", 184.0 / (184.0 * 2))] // example d: short final calculation period - penultimate period
      [InlineData(false, Frequency.Semiannual, "1999-01-30", "2000-06-30", "1999-07-30", "2000-01-30", "2000-01-30", "2000-06-30", 152.0 / (182.0 * 2))] // example d: short final calculation period - final period
      [InlineData(true, Frequency.Quarterly, "1999-05-31", "2000-04-30", "1999-08-31", "1999-11-30", "1999-11-30", "2000-04-30", (91.0 / (91.0 * 4)) + (61.0 / (92.0 * 4)))] // example e: long final calculation period
      [InlineData(false, Frequency.Quarterly, "1999-05-31", "2000-04-30", "1999-08-31", "1999-11-30", "1999-11-30", "2000-04-30", (91.0 / (91.0 * 4)) + (61.0 / (90.0 * 4)))] // example e: long final calculation period - not end of month
      public void testActualActualIsma(bool isEndOfMonth, Frequency frequency, string interestAccrualDateAsString, string maturityDateAsString, string firstCouponDateAsString, string penultimateCouponDateAsString, string d1AsString, string d2AsString, double expectedYearFraction)
      {
         // Example from ISDA Paper: The actual/actual day count fraction, paper for use with the ISDA Market Conventions Survey, 3rd June, 1999
         var interestAccrualDate = new Date(DateTime.ParseExact(interestAccrualDateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));
         var maturityDate = new Date(DateTime.ParseExact(maturityDateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));
         var firstCouponDate = new Date(DateTime.ParseExact(firstCouponDateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));
         var penultimateCouponDate = new Date(DateTime.ParseExact(penultimateCouponDateAsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));

         var d1 = new Date(DateTime.ParseExact(d1AsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));
         var d2 = new Date(DateTime.ParseExact(d2AsString, "yyyy-MM-dd", CultureInfo.InvariantCulture));

         var schedule = new MakeSchedule()
             .from(interestAccrualDate)
             .to(maturityDate)
             .withFrequency(frequency)
             .withFirstDate(firstCouponDate)
             .withNextToLastDate(penultimateCouponDate)
             .endOfMonth(isEndOfMonth)
             .value();

         var dayCounter = new ActualActual(ActualActual.Convention.ISMA, schedule);

         var t = dayCounter.yearFraction(d1, d2);

         Assert.Equal(expectedYearFraction, t);
      }

      [Fact]
      public void testActualActualOutOfScheduleRange()
      {
         var today = new Date(10, Month.November, 2020);
         var temp = Settings.evaluationDate();
         Settings.setEvaluationDate(today);

         var effectiveDate = new Date(21, Month.May, 2019);
         var terminationDate = new Date(21, Month.May, 2029);
         var tenor = new Period(1, TimeUnit.Years);
         var calendar = new China(China.Market.IB);
         var convention = BusinessDayConvention.Unadjusted;
         var terminationDateConvention = convention;
         var rule = DateGeneration.Rule.Backward;
         var endOfMonth = false;

         var schedule = new Schedule(effectiveDate, terminationDate, tenor, calendar, convention,
            terminationDateConvention, rule, endOfMonth);
         var dayCounter = new ActualActual(ActualActual.Convention.Bond, schedule);
         var raised = false;

         try
         {
            dayCounter.yearFraction(today, today + new Period(9, TimeUnit.Years));
         }
         catch
         {
            raised = true;
         }
       
         if (!raised)
         {
            QAssert.Fail("Exception expected but did not happen!");
         }

         Settings.setEvaluationDate(temp);
      }

      [Fact]
      public void testAct366()
      {
         // Testing Act/366 day counter

         Date[] testDates = {
            new Date(1,  Month.February, 2002),
            new Date(4,  Month.February, 2002),
            new Date(16, Month.May, 2003),
            new Date(17, Month.December, 2003),
            new Date(17, Month.December, 2004),
            new Date(19, Month.December, 2005),
            new Date(2,  Month.January, 2006),
            new Date(13, Month.March, 2006),
            new Date(15, Month.May, 2006),
            new Date(17, Month.March, 2006),
            new Date(15, Month.May, 2006),
            new Date(26, Month.July, 2006),
            new Date(28, Month.June, 2007),
            new Date(16, Month.September, 2009),
            new Date(26, Month.July, 2016)
         };

         double[] expected = {
            0.00819672131147541,
            1.27322404371585,
            0.587431693989071,
            1.0000000000000,
            1.00273224043716,
            0.0382513661202186,
            0.191256830601093,
            0.172131147540984,
            -0.16120218579235,
            0.16120218579235,
            0.19672131147541,
            0.920765027322404,
            2.21584699453552,
            6.84426229508197
         };

         DayCounter dayCounter = new Actual366();

         for (var i=1; i<testDates.Length; i++)
         {
            var calculated = dayCounter.yearFraction(testDates[i-1],testDates[i]);
            QAssert.IsTrue (Math.Abs(calculated-expected[i-1]) <= 1.0e-12, 
               "from " + testDates[i-1]
               + " to " + testDates[i] + ":\n"
               + "    calculated: " + calculated + "\n"
               + "    expected:   " + expected[i-1]);
         }
      }

      [Fact]
      public void testAct36525()
      {
         // Testing Act/365.25 day counter

         Date[] testDates =
         {
            new Date(1,  Month.February, 2002),
            new Date(4,  Month.February, 2002),
            new Date(16, Month.May, 2003),
            new Date(17, Month.December, 2003),
            new Date(17, Month.December, 2004),
            new Date(19, Month.December, 2005),
            new Date(2,  Month.January, 2006),
            new Date(13, Month.March, 2006),
            new Date(15, Month.May, 2006),
            new Date(17, Month.March, 2006),
            new Date(15, Month.May, 2006),
            new Date(26, Month.July, 2006),
            new Date(28, Month.June, 2007),
            new Date(16, Month.September, 2009),
            new Date(26, Month.July, 2016)
         };

         double[] expected =
         {
            0.0082135523613963,
            1.27583846680356,
            0.588637919233402,
            1.00205338809035,
            1.00479123887748,
            0.0383299110198494,
            0.191649555099247,
            0.172484599589322,
            -0.161533196440794,
            0.161533196440794,
            0.197125256673511,
            0.922655715263518,
            2.22039698836413,
            6.85831622176591
         };

         var dayCounter = new Actual36525();

         for (var i=1; i<testDates.Length; i++)
         {
            var calculated = dayCounter.yearFraction(testDates[i-1],testDates[i]);
            if (Math.Abs(calculated-expected[i-1]) > 1.0e-12)
            {
               QAssert.Fail("from " + testDates[i-1]
                                    + " to " + testDates[i] + ":\n"
                                    + "    calculated: " + calculated + "\n"
                                    + "    expected:   " + expected[i-1]);
            }
         }
      }
   }
}
