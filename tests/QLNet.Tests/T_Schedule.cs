/*
 Copyright (C) 2012-2025  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Generic;
using Xunit;
using QLNet;
using InputData = System.Collections.Generic.Dictionary<(QLNet.Date,QLNet.Period), (QLNet.Date,QLNet.Date)>;
using System;
using System.Linq;


namespace TestSuite
{
   [Collection("QLNet CI Tests")]
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

      [Fact]
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

      [Fact]
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
         expected.Add(new Date(15, Month.June, 2012));

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

      [Fact]
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
         expected.Add(new Date(28, Month.March, 2013));
         expected.Add(new Date(31, Month.March, 2014));
         // March 31st 2015, coming from the EOM adjustment of March 28th,
         // should be discarded as past the end date.
         expected.Add(new Date(30, Month.March, 2015));

         check_dates(s, expected);
      }

      [Fact]
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
         expected.Add(new Date(28, Month.March, 2013));
         expected.Add(new Date(31, Month.March, 2014));
         // March 31st 2015, coming from the EOM adjustment of March 28th,
         // should be discarded as the same as the end date.
         expected.Add(new Date(31, Month.March, 2015));

         check_dates(s, expected);

         // also, the last period should be regular.
         if (!s.isRegular(2))
            QAssert.Fail("last period should be regular");
      }

      [Fact]
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

      [Fact]
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

      [Fact]
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
         expected.Add(new Date(22, Month.August, 1996));
         expected.Add(new Date(30, Month.August, 1996));
         expected.Add(new Date(28, Month.February, 1997));
         expected.Add(new Date(02, Month.September, 1997));

         check_dates(s, expected);
      }

      [Fact]
      public void testAccrualDateWithEomAdjustment()
      {
         // Testing accrual date is not changed when end-of-month adjustment
         var s = new MakeSchedule().from(new Date(21, Month.November, 2024))
            .to(new Date(31, Month.January, 2030))
            .withCalendar(new TARGET())
            .withTenor(new Period(6, TimeUnit.Months))
            .withConvention(BusinessDayConvention.Unadjusted)
            .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
            .withFirstDate(new Date(31, Month.July, 2025))
            .forwards()
            .endOfMonth().value();

         var expected = new List<Date>
         {
            // The end date is adjusted, so it should also be moved to the end
            // of the month.
            new(21, Month.November, 2024),
            new(31, Month.July, 2025),
            new(31, Month.January, 2026),
            new(31, Month.July, 2026),
            new(31, Month.January, 2027),
            new(31, Month.July, 2027),
            new(31, Month.January, 2028),
            new(31, Month.July, 2028),
            new(31, Month.January, 2029),
            new(31, Month.July, 2029),
            new(31, Month.January, 2030)
         };

         check_dates(s, expected);
      }

      [Fact]
      public void testFirstDateWithEomAdjustment()
      {
         // Testing schedule with first date and EOM adjustments

         var schedule = new MakeSchedule()
            .from(new Date(10, Month.August, 1996))
            .to(new Date(10, Month.August, 1998))
            .withFirstDate(new Date(28, Month.February, 1997))
            .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
            .withTenor(new Period(6 , TimeUnit.Months))
            .withConvention(BusinessDayConvention.Following)
            .withTerminationDateConvention(BusinessDayConvention.Following)
            .forwards()
            .endOfMonth().value();

         var expected = new List<Date>
         {
            new(12, Month.August, 1996),
            new(28, Month.February, 1997),
            new(29, Month.August, 1997),
            new(27, Month.February, 1998),
            new(10, Month.August, 1998)
         };

         check_dates(schedule, expected);
      }

      [Fact]
      public void testNextToLastWithEomAdjustment()
      {
         // Testing schedule with next to last date and EOM adjustments

         var schedule = new MakeSchedule()
            .from(new Date(10, Month.August, 1996))
            .to(new Date(10, Month.August, 1998))
            .withNextToLastDate(new Date(28, Month.February, 1998))
            .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
            .withTenor(new Period(6 , TimeUnit.Months))
            .withConvention(BusinessDayConvention.Following)
            .withTerminationDateConvention(BusinessDayConvention.Following)
            .backwards()
            .endOfMonth().value();

         var expected = new List<Date>
         {
            new(12, Month.August, 1996),
            new(30, Month.August, 1996),
            new(28, Month.February, 1997),
            new(29, Month.August, 1997),
            new(27, Month.February, 1998),
            new(10, Month.August, 1998)
         };

         check_dates(schedule, expected);
      }

      [Fact]
      public void testEffectiveDateWithEomAdjustment()
      {
         // Testing forward schedule with EOM adjustment and effective date and first date in the same month

         var s = new MakeSchedule().from(new Date(16,Month.January,2023))
            .to(new Date(16,Month.March,2023))
            .withFirstDate(new Date(31,Month.January,2023))
            .withCalendar(new NullCalendar())
            .withTenor(new Period(1,TimeUnit.Months))
            .withConvention(BusinessDayConvention.Unadjusted)
            .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
            .forwards()
            .endOfMonth().value();

         var expected = new List<Date>
         {
            // check that the effective date is not moved at the end of the month
            new(16, Month.January, 2023),
            new(31, Month.January, 2023),
            new(28,Month.February,2023),
            new(16,Month.March,2023)
         };

         check_dates(s, expected);
      }

      #region CDS Tests

      // Helper method to build a schedule
      public Schedule makeCdsSchedule(Date from, Date to, DateGeneration.Rule rule)
      {
         return new MakeSchedule()
            .from(from)
            .to(to)
            .withCalendar(new WeekendsOnly())
            .withTenor(new Period(3,TimeUnit.Months))
            .withConvention(BusinessDayConvention.Following)
            .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
            .withRule(rule).value();
      }

      private void testCDSConventions(InputData inputs, DateGeneration.Rule rule)
      {
         // Test the generated start and end date against the expected start and end date.
         foreach (var input in inputs)
         {
            Date from = input.Key.Item1;
            Period tenor = input.Key.Item2;

            Date maturity = CreditDefaultSwap.cdsMaturity(from, tenor, rule);
            Date expEnd = input.Value.Item2;
            QAssert.AreEqual(maturity, expEnd);

            Schedule s = makeCdsSchedule(from, maturity, rule);

            Date expStart = input.Value.Item1;
            Date start = s.startDate();
            Date end = s.endDate();
            QAssert.AreEqual(start, expStart);
            QAssert.AreEqual(end, expEnd);
         }
      }

      [Fact]
      public void testCDS2015Convention()
      {
         // Testing CDS2015 semi-annual rolling convention
         var rule = DateGeneration.Rule.CDS2015;
         var tenor = new Period(5, TimeUnit.Years);

         // From September 20th 2016 to March 19th 2017 of the next year, end date is December 20th 2021 for a 5 year CDS.
         // To get the correct schedule, you can first use the cdsMaturity function to get the maturity from the tenor.
         var tradeDate = new Date (12, Month.Dec, 2016);
         var maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         var expStart = new Date(20, Month.Sep, 2016);
         var expMaturity = new Date(20, Month.Dec, 2021);
         QAssert.AreEqual(maturity, expMaturity);
         var s = makeCdsSchedule(tradeDate, maturity, rule);
         QAssert.AreEqual(s.startDate(), expStart);
         QAssert.AreEqual(s.endDate(), expMaturity);

         // If we just use 12 Dec 2016 + 5Y = 12 Dec 2021 as termination date in the schedule, the schedule constructor can 
         // use any of the allowable CDS dates i.e. 20 Mar, Jun, Sep and Dec. In the constructor, we just use the next one 
         // here i.e. 20 Dec 2021. We get the same results as above.
         maturity = tradeDate + tenor;
         s = makeCdsSchedule(tradeDate, maturity, rule);
         QAssert.AreEqual(s.startDate(), expStart);
         QAssert.AreEqual(s.endDate(), expMaturity);

         // We do the same tests but with a trade date of 1 Mar 2017. Using cdsMaturity to get maturity date from 5Y tenor, 
         // we get the same maturity as above.
         tradeDate = new Date(1, Month.Mar, 2017);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         QAssert.AreEqual(maturity, expMaturity);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         expStart = new Date(20, Month.Dec, 2016);
         QAssert.AreEqual(s.startDate(), expStart);
         QAssert.AreEqual(s.endDate(), expMaturity);

         // Using 1 Mar 2017 + 5Y = 1 Mar 2022 as termination date in the schedule, the constructor just uses the next 
         // allowable CDS date i.e. 20 Mar 2022. We must update the expected maturity.
         maturity = tradeDate + tenor;
         s = makeCdsSchedule(tradeDate, maturity, rule);
         QAssert.AreEqual(s.startDate(), expStart);
         expMaturity = new Date(20, Month.Mar, 2022);
         QAssert.AreEqual(s.endDate(), expMaturity);

         // From March 20th 2017 to September 19th 2017, end date is June 20th 2022 for a 5 year CDS.
         tradeDate = new Date(20, Month.Mar, 2017);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         expStart = new Date(20, Month.Mar, 2017);
         expMaturity = new Date(20, Month.Jun, 2022);
         QAssert.AreEqual(maturity, expMaturity);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         QAssert.AreEqual(s.startDate(), expStart);
         QAssert.AreEqual(s.endDate(), expMaturity);
      }

      [Fact]
      public void testCDS2015ConventionGrid()
      {
         // Testing against section 11 of ISDA doc FAQs Amending when Single Name CDS roll to new on-the-run contracts
         // December 20, 2015 Go-Live

         // Testing CDS2015 convention against ISDA doc

         // Test inputs and expected outputs
         // The map key is a pair with 1st element equal to trade date and 2nd element equal to CDS tenor.
         // The map value is a pair with 1st and 2nd element equal to expected start and end date respectively.
         // The trade dates are from the transition dates in the doc i.e. 20th Mar, Jun, Sep and Dec in 2016 and a day 
         // either side. The tenors are selected tenors from the doc i.e. short quarterly tenors less than 1Y, 1Y and 5Y.
         var inputs = new InputData
         {
            { (new Date(19, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Mar, 2016))},
            { (new Date(20, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Sep, 2016))},
            { (new Date(21, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Sep, 2016))},
            { (new Date(19, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Sep, 2016))},
            { (new Date(20, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2016))},
            { (new Date(21, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2016))},
            { (new Date(19, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2016))},
            { (new Date(20, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(21, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(19, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(20, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(21, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(19, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Jun, 2016))},
            { (new Date(20, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Dec, 2016))},
            { (new Date(21, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Dec, 2016))},
            { (new Date(19, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Dec, 2016))},
            { (new Date(20, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Dec, 2016))},
            { (new Date(21, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Dec, 2016))},
            { (new Date(19, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Dec, 2016))},
            { (new Date(20, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(21, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(19, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(20, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(21, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(19, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Sep, 2016))},
            { (new Date(20, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Mar, 2017))},
            { (new Date(21, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(19, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(20, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(21, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(19, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Mar, 2017))},
            { (new Date(20, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Sep, 2017))},
            { (new Date(21, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Sep, 2017))},
            { (new Date(19, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Sep, 2017))},
            { (new Date(20, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Sep, 2017))},
            { (new Date(21, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Sep, 2017))},
            { (new Date(19, Month.Mar, 2016), new Period(1 , TimeUnit.Years)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Dec, 2016))},
            { (new Date(20, Month.Mar, 2016), new Period(1 , TimeUnit.Years)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Jun, 2017))},
            { (new Date(21, Month.Mar, 2016), new Period(1 , TimeUnit.Years)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(19, Month.Jun, 2016), new Period(1 , TimeUnit.Years)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(20, Month.Jun, 2016), new Period(1 , TimeUnit.Years)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(21, Month.Jun, 2016), new Period(1 , TimeUnit.Years)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(19, Month.Sep, 2016), new Period(1 , TimeUnit.Years)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2017))},
            { (new Date(20, Month.Sep, 2016), new Period(1 , TimeUnit.Years)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2017))},
            { (new Date(21, Month.Sep, 2016), new Period(1 , TimeUnit.Years)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2017))},
            { (new Date(19, Month.Dec, 2016), new Period(1 , TimeUnit.Years)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2017))},
            { (new Date(20, Month.Dec, 2016), new Period(1 , TimeUnit.Years)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Dec, 2017))},
            { (new Date(21, Month.Dec, 2016), new Period(1 , TimeUnit.Years)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Dec, 2017))},
            { (new Date(19, Month.Mar, 2016), new Period(5 , TimeUnit.Years)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Dec, 2020))},
            { (new Date(20, Month.Mar, 2016), new Period(5 , TimeUnit.Years)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Jun, 2021))},
            { (new Date(21, Month.Mar, 2016), new Period(5 , TimeUnit.Years)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2021))},
            { (new Date(19, Month.Jun, 2016), new Period(5 , TimeUnit.Years)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2021))},
            { (new Date(20, Month.Jun, 2016), new Period(5 , TimeUnit.Years)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2021))},
            { (new Date(21, Month.Jun, 2016), new Period(5 , TimeUnit.Years)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2021))},
            { (new Date(19, Month.Sep, 2016), new Period(5 , TimeUnit.Years)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2021))},
            { (new Date(20, Month.Sep, 2016), new Period(5 , TimeUnit.Years)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2021))},
            { (new Date(21, Month.Sep, 2016), new Period(5 , TimeUnit.Years)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2021))},
            { (new Date(19, Month.Dec, 2016), new Period(5 , TimeUnit.Years)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2021))},
            { (new Date(20, Month.Dec, 2016), new Period(5 , TimeUnit.Years)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Dec, 2021))},
            { (new Date(21, Month.Dec, 2016), new Period(5 , TimeUnit.Years)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Dec, 2021))},
            { (new Date(20, Month.Mar, 2016), new Period(0 , TimeUnit.Months)),(new Date(21, Month.Dec, 2015), new Date(20, Month.Jun, 2016))},
            { (new Date(21, Month.Mar, 2016), new Period(0 , TimeUnit.Months)),(new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2016))},
            { (new Date(19, Month.Jun, 2016), new Period(0 , TimeUnit.Months)),(new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2016))},
            { (new Date(20, Month.Sep, 2016), new Period(0 , TimeUnit.Months)),(new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2016))},
            { (new Date(21, Month.Sep, 2016), new Period(0 , TimeUnit.Months)),(new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2016))},
            { (new Date(19, Month.Dec, 2016), new Period(0 , TimeUnit.Months)),(new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2016))}
         };

          testCDSConventions(inputs, DateGeneration.Rule.CDS2015);
      }

      [Fact]
      public void testCDSConventionGrid()
      {
         // Testing against section 11 of ISDA doc FAQs Amending when Single Name CDS roll to new on-the-run contracts
         // December 20, 2015 Go-Live. Amended the dates in the doc to the pre-2015 expected maturity dates.
         // Testing CDS convention against ISDA doc

         // Test inputs and expected outputs
         // The map key is a pair with 1st element equal to trade date and 2nd element equal to CDS tenor.
         // The map value is a pair with 1st and 2nd element equal to expected start and end date respectively.
         // The trade dates are from the transition dates in the doc i.e. 20th Mar, Jun, Sep and Dec in 2016 and a day 
         // either side. The tenors are selected tenors from the doc i.e. short quarterly tenors less than 1Y, 1Y and 5Y.
         var inputs = new InputData
         {
            { (new Date(19, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Jun, 2016)) },
            { (new Date(20, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Sep, 2016)) },
            { (new Date(21, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(19, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(20, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(21, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(19, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(20, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(21, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(19, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(20, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(21, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(19, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Sep, 2016)) },
            { (new Date(20, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Dec, 2016)) },
            { (new Date(21, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(19, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(20, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(21, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(19, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(20, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(21, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(19, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(20, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(21, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(19, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Dec, 2016)) },
            { (new Date(20, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Mar, 2017)) },
            { (new Date(21, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(19, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(20, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(21, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(19, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(20, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(21, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(19, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(20, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(21, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(19, Month.Mar, 2016), new Period(1 , TimeUnit.Years)),  (new Date(21, Month.Dec, 2015), new Date(20, Month.Mar, 2017)) },
            { (new Date(20, Month.Mar, 2016), new Period(1 , TimeUnit.Years)),  (new Date(21, Month.Dec, 2015), new Date(20, Month.Jun, 2017)) },
            { (new Date(21, Month.Mar, 2016), new Period(1 , TimeUnit.Years)),  (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(19, Month.Jun, 2016), new Period(1 , TimeUnit.Years)),  (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(20, Month.Jun, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(21, Month.Jun, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(19, Month.Sep, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(20, Month.Sep, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(21, Month.Sep, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(19, Month.Dec, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(20, Month.Dec, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2018)) },
            { (new Date(21, Month.Dec, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2018)) },
            { (new Date(19, Month.Mar, 2016), new Period(5 , TimeUnit.Years)),  (new Date(21, Month.Dec, 2015), new Date(20, Month.Mar, 2021)) },
            { (new Date(20, Month.Mar, 2016), new Period(5 , TimeUnit.Years)),  (new Date(21, Month.Dec, 2015), new Date(20, Month.Jun, 2021)) },
            { (new Date(21, Month.Mar, 2016), new Period(5 , TimeUnit.Years)),  (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2021)) },
            { (new Date(19, Month.Jun, 2016), new Period(5 , TimeUnit.Years)),  (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2021)) },
            { (new Date(20, Month.Jun, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2021)) },
            { (new Date(21, Month.Jun, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2021)) },
            { (new Date(19, Month.Sep, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2021)) },
            { (new Date(20, Month.Sep, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2021)) },
            { (new Date(21, Month.Sep, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2021)) },
            { (new Date(19, Month.Dec, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2021)) },
            { (new Date(20, Month.Dec, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2022)) },
            { (new Date(21, Month.Dec, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2022)) },
            { (new Date(19, Month.Mar, 2016), new Period(0 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Mar, 2016)) },
            { (new Date(20, Month.Mar, 2016), new Period(0 , TimeUnit.Months)), (new Date(21, Month.Dec, 2015), new Date(20, Month.Jun, 2016)) },
            { (new Date(21, Month.Mar, 2016), new Period(0 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2016)) },
            { (new Date(19, Month.Jun, 2016), new Period(0 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2016)) },
            { (new Date(20, Month.Jun, 2016), new Period(0 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(21, Month.Jun, 2016), new Period(0 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(19, Month.Sep, 2016), new Period(0 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(20, Month.Sep, 2016), new Period(0 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(21, Month.Sep, 2016), new Period(0 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(19, Month.Dec, 2016), new Period(0 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(20, Month.Dec, 2016), new Period(0 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(21, Month.Dec, 2016), new Period(0 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2017)) }
         };

         testCDSConventions(inputs, DateGeneration.Rule.CDS);
      }

      [Fact]
      public void testOldCDSConventionGrid()
      {
         // Testing against section 11 of ISDA doc FAQs Amending when Single Name CDS roll to new on-the-run contracts
         // December 20, 2015 Go-Live. Amended the dates in the doc to the pre-2009 expected start and maturity dates.
         // Testing old CDS convention...");

         // Test inputs and expected outputs
         // The map key is a pair with 1st element equal to trade date and 2nd element equal to CDS tenor.
         // The map value is a pair with 1st and 2nd element equal to expected start and end date respectively.
         // The trade dates are from the transition dates in the doc i.e. 20th Mar, Jun, Sep and Dec in 2016 and a day 
         // either side. The tenors are selected tenors from the doc i.e. short quarterly tenors less than 1Y, 1Y and 5Y.

         var inputs = new InputData
         {
            { (new Date(19, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(19, Month.Mar, 2016), new Date(20, Month.Jun, 2016)) },
            { (new Date(20, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Mar, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(21, Month.Mar, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(19, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(19, Month.Jun, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(20, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(21, Month.Jun, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Jun, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(19, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(19, Month.Sep, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(20, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(21, Month.Sep, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Sep, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(19, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(19, Month.Dec, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(20, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(21, Month.Dec, 2016), new Period(3 , TimeUnit.Months)), (new Date(21, Month.Dec, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(19, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(19, Month.Mar, 2016), new Date(20, Month.Sep, 2016)) },
            { (new Date(20, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Mar, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(21, Month.Mar, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(19, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(19, Month.Jun, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(20, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(21, Month.Jun, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Jun, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(19, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(19, Month.Sep, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(20, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(21, Month.Sep, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Sep, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(19, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(19, Month.Dec, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(20, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(21, Month.Dec, 2016), new Period(6 , TimeUnit.Months)), (new Date(21, Month.Dec, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(19, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(19, Month.Mar, 2016), new Date(20, Month.Dec, 2016)) },
            { (new Date(20, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Mar, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(21, Month.Mar, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Mar, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(19, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(19, Month.Jun, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(20, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Jun, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(21, Month.Jun, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Jun, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(19, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(19, Month.Sep, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(20, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Sep, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(21, Month.Sep, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Sep, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(19, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(19, Month.Dec, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(20, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(20, Month.Dec, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(21, Month.Dec, 2016), new Period(9 , TimeUnit.Months)), (new Date(21, Month.Dec, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(19, Month.Mar, 2016), new Period(1 , TimeUnit.Years)),  (new Date(19, Month.Mar, 2016), new Date(20, Month.Mar, 2017)) },
            { (new Date(20, Month.Mar, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Mar, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(21, Month.Mar, 2016), new Period(1 , TimeUnit.Years)),  (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(19, Month.Jun, 2016), new Period(1 , TimeUnit.Years)),  (new Date(19, Month.Jun, 2016), new Date(20, Month.Jun, 2017)) },
            { (new Date(20, Month.Jun, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(21, Month.Jun, 2016), new Period(1 , TimeUnit.Years)),  (new Date(21, Month.Jun, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(19, Month.Sep, 2016), new Period(1 , TimeUnit.Years)),  (new Date(19, Month.Sep, 2016), new Date(20, Month.Sep, 2017)) },
            { (new Date(20, Month.Sep, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(21, Month.Sep, 2016), new Period(1 , TimeUnit.Years)),  (new Date(21, Month.Sep, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(19, Month.Dec, 2016), new Period(1 , TimeUnit.Years)),  (new Date(19, Month.Dec, 2016), new Date(20, Month.Dec, 2017)) },
            { (new Date(20, Month.Dec, 2016), new Period(1 , TimeUnit.Years)),  (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2018)) },
            { (new Date(21, Month.Dec, 2016), new Period(1 , TimeUnit.Years)),  (new Date(21, Month.Dec, 2016), new Date(20, Month.Mar, 2018)) },
            { (new Date(19, Month.Mar, 2016), new Period(5 , TimeUnit.Years)),  (new Date(19, Month.Mar, 2016), new Date(20, Month.Mar, 2021)) },
            { (new Date(20, Month.Mar, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Mar, 2016), new Date(20, Month.Jun, 2021)) },
            { (new Date(21, Month.Mar, 2016), new Period(5 , TimeUnit.Years)),  (new Date(21, Month.Mar, 2016), new Date(20, Month.Jun, 2021)) },
            { (new Date(19, Month.Jun, 2016), new Period(5 , TimeUnit.Years)),  (new Date(19, Month.Jun, 2016), new Date(20, Month.Jun, 2021)) },
            { (new Date(20, Month.Jun, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Jun, 2016), new Date(20, Month.Sep, 2021)) },
            { (new Date(21, Month.Jun, 2016), new Period(5 , TimeUnit.Years)),  (new Date(21, Month.Jun, 2016), new Date(20, Month.Sep, 2021)) },
            { (new Date(19, Month.Sep, 2016), new Period(5 , TimeUnit.Years)),  (new Date(19, Month.Sep, 2016), new Date(20, Month.Sep, 2021)) },
            { (new Date(20, Month.Sep, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Sep, 2016), new Date(20, Month.Dec, 2021)) },
            { (new Date(21, Month.Sep, 2016), new Period(5 , TimeUnit.Years)),  (new Date(21, Month.Sep, 2016), new Date(20, Month.Dec, 2021)) },
            { (new Date(19, Month.Dec, 2016), new Period(5 , TimeUnit.Years)),  (new Date(19, Month.Dec, 2016), new Date(20, Month.Dec, 2021)) },
            { (new Date(20, Month.Dec, 2016), new Period(5 , TimeUnit.Years)),  (new Date(20, Month.Dec, 2016), new Date(20, Month.Mar, 2022)) },
            { (new Date(21, Month.Dec, 2016), new Period(5 , TimeUnit.Years)),  (new Date(21, Month.Dec, 2016), new Date(20, Month.Mar, 2022)) }
         };

         testCDSConventions(inputs, DateGeneration.Rule.OldCDS);
      }

      [Fact]
      public void testCDS2015ConventionSampleDates()
      {
         // Testing all dates in sample CDS schedule(s) for rule CDS2015
         var rule = DateGeneration.Rule.CDS2015;
         var tenor = new Period(1, TimeUnit.Years);

         // trade date = Fri 18 Sep 2015.
         var tradeDate = new Date(18, Month.Sep, 2015);
         var maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         var s = makeCdsSchedule(tradeDate, maturity, rule);
         var expDates = new List<Date>
         {
            new (22, Month.Jun, 2015), new (21, Month.Sep, 2015), new (21, Month.Dec, 2015),
            new (21, Month.Mar, 2016), new (20, Month.Jun, 2016)
         };
         check_dates(s, expDates);

         // trade date = Sat 19 Sep 2015, no change.
         tradeDate = new Date(19, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         check_dates(s, expDates);

         // trade date = Sun 20 Sep 2015. Roll to new maturity. Trade date still before next coupon payment
         // date of Mon 21 Sep 2015, so keep the first period from 22 Jun 2015 to 21 Sep 2015 in schedule.
         tradeDate = new Date(20, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         expDates.Add(new Date(20, Month.Sep, 2016));
         expDates.Add(new Date(20, Month.Dec, 2016));
         check_dates(s, expDates);

         // trade date = Mon 21 Sep 2015, first period drops out of schedule.
         tradeDate = new Date(21, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         expDates.RemoveAt(0);
         check_dates(s, expDates);

         // Another sample trade date, Sat 20 Jun 2009.
         tradeDate = new Date(20, Month.Jun, 2009);
         maturity = new Date(20, Month.Dec, 2009);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         var tmp = new List<Date>
         {
            new (20, Month.Mar, 2009), new (22, Month.Jun, 2009), new (21, Month.Sep, 2009), new (20, Month.Dec, 2009)
         };
         expDates = new List<Date>(tmp);
         check_dates(s, expDates);

         // Move forward to Sun 21 Jun 2009
         tradeDate = new Date(21, Month.Jun, 2009);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         check_dates(s, expDates);

         // Move forward to Mon 22 Jun 2009
         tradeDate = new Date(22, Month.Jun, 2009);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         expDates.RemoveAt(0);
         check_dates(s, expDates);
      }

      [Fact]
      public void testCDSConventionSampleDates()
      {
         // Testing all dates in sample CDS schedule(s) for rule CDS
         var rule = DateGeneration.Rule.CDS;
         var tenor = new Period(1, TimeUnit.Years);

         var tradeDate = new Date(18, Month.Sep, 2015);
         var maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         var s = makeCdsSchedule(tradeDate, maturity, rule);
         var expDates = new List<Date>
         {
            new(22, Month.Jun, 2015), new(21, Month.Sep, 2015), new(21, Month.Dec, 2015),
            new(21, Month.Mar, 2016), new(20, Month.Jun, 2016), new(20, Month.Sep, 2016)
         };
         check_dates(s, expDates);

         // trade date = Sat 19 Sep 2015, no change.
         tradeDate = new Date(19, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         check_dates(s, expDates);

         // trade date = Sun 20 Sep 2015. Roll to new maturity. Trade date still before next coupon payment
         // date of Mon 21 Sep 2015, so keep the first period from 22 Jun 2015 to 21 Sep 2015 in schedule.
         tradeDate = new Date(20, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         expDates.Add(new Date(20, Month.Dec, 2016));
         check_dates(s, expDates);

         // trade date = Mon 21 Sep 2015, first period drops out of schedule.
         tradeDate = new Date(21, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDate, tenor, rule);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         expDates.RemoveAt(0);
         check_dates(s, expDates);

         // Another sample trade date, Sat 20 Jun 2009.
         tradeDate = new Date(20, Month.Jun, 2009);
         maturity = new Date(20, Month.Dec, 2009);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         var tmp = new List<Date>{ new (20, Month.Mar, 2009), new (22, Month.Jun, 2009), new (21, Month.Sep, 2009), new (20, Month.Dec, 2009) };
         expDates = new List<Date>(tmp);
         check_dates(s, expDates);

         // Move forward to Sun 21 Jun 2009
         tradeDate = new Date(21, Month.Jun, 2009);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         check_dates(s, expDates);

         // Move forward to Mon 22 Jun 2009
         tradeDate = new Date(22, Month.Jun, 2009);
         s = makeCdsSchedule(tradeDate, maturity, rule);
         expDates.RemoveAt(0);
         check_dates(s, expDates);
      }

      [Fact]
      public void testOldCDSConventionSampleDates()
      {
         // Testing all dates in sample CDS schedule(s) for rule OldCDS
         var rule = DateGeneration.Rule.OldCDS;
         var tenor = new Period(1, TimeUnit.Years);

         // trade date plus 1D = Fri 18 Sep 2015.
         var tradeDatePlusOne = new Date(18, Month.Sep, 2015);
         var maturity = CreditDefaultSwap.cdsMaturity(tradeDatePlusOne, tenor, rule);
         var s = makeCdsSchedule(tradeDatePlusOne, maturity, rule);
         var expDates = new List<Date>
         {
            new(18, Month.Sep, 2015), new(21, Month.Dec, 2015),
            new(21, Month.Mar, 2016), new(20, Month.Jun, 2016), new(20, Month.Sep, 2016)
         };
         check_dates(s, expDates);

         // trade date plus 1D = Sat 19 Sep 2015, no change.
         // OldCDS, schedule start date is not adjusted (kept this).
         expDates[0] = tradeDatePlusOne = new Date(19, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDatePlusOne, tenor, rule);
         s = makeCdsSchedule(tradeDatePlusOne, maturity, rule);
         check_dates(s, expDates);

         // trade date plus 1D = Sun 20 Sep 2015, roll.
         expDates[0] = tradeDatePlusOne = new Date(20, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDatePlusOne, tenor, rule);
         s = makeCdsSchedule(tradeDatePlusOne, maturity, rule);
         expDates.Add(new Date(20, Month.Dec, 2016));
         check_dates(s, expDates);

         // trade date plus 1D = Mon 21 Sep 2015, no change.
         expDates[0] = tradeDatePlusOne = new Date(21, Month.Sep, 2015);
         maturity = CreditDefaultSwap.cdsMaturity(tradeDatePlusOne, tenor, rule);
         s = makeCdsSchedule(tradeDatePlusOne, maturity, rule);
         check_dates(s, expDates);

         // Check the 30 day stub rule by moving closer to the first coupon payment date of Mon 21 Dec 2015.
         // The test here requires long first stub when trade date plus 1D = 21 Nov 2015. The condition in the schedule 
         // generation code is if: effective date + 30D > next 20th _unadjusted_. Not sure if we should refer to the actual 
         // coupon payment date here i.e. the next 20th _adjusted_ when making the decision.

         // 19 Nov 2015 + 30D = 19 Dec 2015 <= 20 Dec 2015 => short front stub.
         expDates[0] = tradeDatePlusOne = new Date(19, Month.Nov, 2015);
         s = makeCdsSchedule(tradeDatePlusOne, maturity, rule);
         check_dates(s, expDates);

         // 20 Nov 2015 + 30D = 20 Dec 2015 <= 20 Dec 2015 => short front stub.
         expDates[0] = tradeDatePlusOne = new Date(20, Month.Nov, 2015);
         s = makeCdsSchedule(tradeDatePlusOne, maturity, rule);
         check_dates(s, expDates);

         // 21 Nov 2015 + 30D = 21 Dec 2015 > 20 Dec 2015 => long front stub.
         // Note that if we reffered to the next coupon payment date of 21 Dec 2015, it would still be short front.
         expDates[0] = tradeDatePlusOne = new Date(21, Month.Nov, 2015);
         s = makeCdsSchedule(tradeDatePlusOne, maturity, rule);
         expDates.RemoveAt(1);
         check_dates(s, expDates);
      }

      [Fact]
      public void testCDS2015ZeroMonthsMatured()
      {
         // Testing 0M tenor for CDS2015 where matured
         var rule = DateGeneration.Rule.CDS2015;
         var tenor = new Period(0, TimeUnit.Months);

         // Move through selected trade dates from 20 Dec 2015 to 20 Dec 2016 checking that the 0M CDS is matured.
         var inputs = new List<Date>
         {
            new(20, Month.Dec, 2015),
            new(15, Month.Feb, 2016),
            new(19, Month.Mar, 2016),
            new(20, Month.Jun, 2016),
            new(15, Month.Aug, 2016),
            new(19, Month.Sep, 2016),
            new(20, Month.Dec, 2016)
         };

         foreach (var input in inputs)
            QAssert.AreEqual(CreditDefaultSwap.cdsMaturity(input, tenor, rule), new Date());
      }

      [Fact]
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
            QAssert.Fail("schedule1 has size " + schedule1.Count + ", expected " + dates.Count);
         for (int i = 0; i < dates.Count; ++i)
            if (schedule1[i] != dates[i])
               QAssert.Fail("schedule1 has " + schedule1[i] + " at position " + i + ", expected " + dates[i]);
         if (schedule1.calendar() != new NullCalendar())
            QAssert.Fail("schedule1 has calendar " + schedule1.calendar().name() + ", expected null calendar");
         if (schedule1.businessDayConvention() != BusinessDayConvention.Unadjusted)
            QAssert.Fail("schedule1 has convention " + schedule1.businessDayConvention() + ", expected unadjusted");

         // schedule with metadata
         List<bool> regular = new List<bool>();
         regular.Add(false);
         regular.Add(true);
         regular.Add(false);

         Schedule schedule2 = new Schedule(dates, new TARGET(), BusinessDayConvention.Following, BusinessDayConvention.ModifiedPreceding, new Period(1, TimeUnit.Years),
                                           DateGeneration.Rule.Backward, true, regular);
         for (int i = 1; i < dates.Count; ++i)
            if (schedule2.isRegular(i) != regular[i - 1])
               QAssert.Fail("schedule2 has a " + (schedule2.isRegular(i) ? "regular" : "irregular") + " period at position " + i + ", expected " + (regular[i - 1] ? "regular" : "irregular"));
         if (schedule2.calendar() != new TARGET())
            QAssert.Fail("schedule1 has calendar " + schedule2.calendar().name() + ", expected TARGET");
         if (schedule2.businessDayConvention() != BusinessDayConvention.Following)
            QAssert.Fail("schedule2 has convention " + schedule2.businessDayConvention() + ", expected Following");
         if (schedule2.terminationDateBusinessDayConvention() != BusinessDayConvention.ModifiedPreceding)
            QAssert.Fail("schedule2 has convention " + schedule2.terminationDateBusinessDayConvention() + ", expected Modified Preceding");
         if (schedule2.tenor() != new Period(1, TimeUnit.Years))
            QAssert.Fail("schedule2 has tenor " + schedule2.tenor() + ", expected 1Y");
         if (schedule2.rule() != DateGeneration.Rule.Backward)
            QAssert.Fail("schedule2 has rule " + schedule2.rule() + ", expected Backward");
         if (schedule2.endOfMonth() != true)
            QAssert.Fail("schedule2 has end of month flag false, expected true");
      }

      [Fact]
      public void testFourWeeksTenor()
      {
         // Testing that a four-weeks tenor works
         try
         {
            var s = new MakeSchedule().from(new Date(13,Month.January,2016))
                  .to(new Date(4,Month.May,2016))
                  .withCalendar(new TARGET())
                  .withTenor(new Period(4,TimeUnit.Weeks))
                  .withConvention(BusinessDayConvention.Following)
                  .forwards().value();
         }
         catch (Exception e)
         {
            QAssert.IsTrue(false,"A four-weeks tenor caused an exception: " + e.Message);
         }
      }

      [Fact]
      public void testOnceFrequency()
      {
         // Testing that Once frequency works
         var s = new MakeSchedule().from(new Date(13,Month.January,2016))
               .to(new Date(13,Month.January,2019))
               .withFrequency(Frequency.Once)
               .forwards().value();

         QAssert.IsTrue(s.size() == 2);
         QAssert.IsTrue(s[0] == new Date(13,Month.January,2016));
         QAssert.IsTrue(s[1] == new Date(13,Month.January,2019));
      }

      [Fact]
      public void testScheduleAlwaysHasAStartDate()
      {
         // Testing that variations of MakeSchedule always produce a schedule with a start date
         // Attempt to establish whether the first coupoun payment date is
         // always the second element of the constructor.
         Calendar calendar = new UnitedStates(UnitedStates.Market.GovernmentBond);
         var schedule = new MakeSchedule()
            .from(new Date(10, Month.January, 2017))
            .withFirstDate(new Date(31, Month.August, 2017))
            .to(new Date(28, Month.February, 2026))
            .withFrequency(Frequency.Semiannual)
            .withCalendar(calendar)
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().endOfMonth(false).value();
            QAssert.AreEqual(schedule.date(0) , new Date(10, Month.January, 2017), "The first element should always be the start date");

         schedule = new MakeSchedule()
            .from(new Date(10, Month.January, 2017))
            .to(new Date(28, Month.February, 2026))
            .withFrequency(Frequency.Semiannual)
            .withCalendar(calendar)
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().endOfMonth(false).value();
         QAssert.AreEqual(schedule.date(0), new Date(10, Month.January, 2017), "The first element should always be the start date");

         schedule = new MakeSchedule()
            .from(new Date(31, Month.August, 2017))
            .to(new Date(28, Month.February, 2026))
            .withFrequency(Frequency.Semiannual)
            .withCalendar(calendar)
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().endOfMonth(false).value();
         QAssert.AreEqual(schedule.date(0), new Date(31, Month.August, 2017), "The first element should always be the start date");
      }

      [Fact]
      public void testShortEomSchedule()
      {
         // Testing short end-of-month schedule
         var s = new MakeSchedule()
            .from(new Date(21, Month.Feb, 2019))
            .to(new Date(28, Month.Feb, 2019))
            .withCalendar(new TARGET())
            .withTenor(new Period(1 , TimeUnit.Years))
            .withConvention(BusinessDayConvention.ModifiedFollowing)
            .withTerminationDateConvention(BusinessDayConvention.ModifiedFollowing)
            .backwards()
            .endOfMonth(true).value();
         QAssert.IsTrue(s.size() == 2);
         QAssert.IsTrue(s[0] == new Date(21, Month.Feb, 2019));
         QAssert.IsTrue(s[1] == new Date(28, Month.Feb, 2019));
      }

      [Fact]
      public void testFirstDateOnMaturity()
      {
         // Testing schedule with first date on maturity
         var schedule = new MakeSchedule()
            .from(new Date(20, Month.September, 2016))
            .to(new Date(20, Month.December, 2016))
            .withFirstDate(new Date(20, Month.December, 2016))
            .withFrequency(Frequency.Quarterly)
            .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().value();

         var expected = new List<Date>
         {
            new (20, Month.September, 2016),
            new (20, Month.December, 2016)
         };

         check_dates(schedule, expected);

         schedule = new MakeSchedule()
            .from(new Date(20, Month.September, 2016))
            .to(new Date(20, Month.December, 2016))
            .withFirstDate(new Date(20, Month.December, 2016))
            .withFrequency(Frequency.Quarterly)
            .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
            .withConvention(BusinessDayConvention.Unadjusted)
            .forwards().value();

         check_dates(schedule, expected);
      }

      [Fact]
      public void testNextToLastDateOnStart()
      {
         // Testing schedule with next-to-last date on start date
         var schedule = new MakeSchedule()
            .from(new Date(20, Month.September, 2016))
            .to(new Date(20, Month.December, 2016))
            .withNextToLastDate(new Date(20, Month.September, 2016))
            .withFrequency(Frequency.Quarterly)
            .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().value();

         var expected = new List<Date>
         {
            new (20, Month.September, 2016),
            new (20, Month.December, 2016)
         };

         check_dates(schedule, expected);

         schedule = new MakeSchedule()
            .from(new Date(20, Month.September, 2016))
            .to(new Date(20, Month.December, 2016))
            .withNextToLastDate(new Date(20, Month.September, 2016))
            .withFrequency(Frequency.Quarterly)
            .withCalendar(new UnitedStates(UnitedStates.Market.GovernmentBond))
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().value();

         check_dates(schedule, expected);
      }

      [Fact]
      public void testTruncation()
      {
         // Testing schedule truncation
         var s = new MakeSchedule().from(new Date(30, Month.September, 2009))
           .to(new Date(15, Month.June, 2020))
           .withCalendar(new Japan())
           .withTenor(new Period(6 , TimeUnit.Months))
           .withConvention(BusinessDayConvention.Following)
           .withTerminationDateConvention(BusinessDayConvention.Following)
           .forwards()
           .endOfMonth().value();

         // Until
         var t = s.until(new Date(1, Month.Jan, 2014));
         var expected = new List<Date>();
         expected.Add(new Date(30,Month. September, 2009));
         expected.Add(new Date(31,Month. March, 2010));
         expected.Add(new Date(30,Month. September, 2010));
         expected.Add(new Date(31,Month. March, 2011));
         expected.Add(new Date(30,Month. September, 2011));
         expected.Add(new Date(30,Month. March, 2012));
         expected.Add(new Date(28,Month. September, 2012));
         expected.Add(new Date(29,Month. March, 2013));
         expected.Add(new Date(30,Month. September, 2013));
         expected.Add(new Date(1, Month.January, 2014));
         check_dates(t, expected);
         QAssert.IsTrue(t.isRegular().Last() == false);

         // Until, with truncation date falling on a schedule date
         t = s.until(new Date(30, Month.September, 2013));
         expected = new List<Date>();
         expected.Add(new Date(30, Month.September, 2009));
         expected.Add(new Date(31, Month.March, 2010));
         expected.Add(new Date(30, Month.September, 2010));
         expected.Add(new Date(31, Month.March, 2011));
         expected.Add(new Date(30, Month.September, 2011));
         expected.Add(new Date(30, Month.March, 2012));
         expected.Add(new Date(28, Month.September, 2012));
         expected.Add(new Date(29, Month.March, 2013));
         expected.Add(new Date(30, Month.September, 2013));
         check_dates(t, expected);
         QAssert.IsTrue(t.isRegular().Last() == true);

         // After
         t = s.after(new Date(1, Month.Jan, 2014));
         expected = new List<Date>();
         expected.Add(new Date(1,  Month.January, 2014));
         expected.Add(new Date(31, Month.March, 2014));
         expected.Add(new Date(30, Month.September, 2014));
         expected.Add(new Date(31, Month.March, 2015));
         expected.Add(new Date(30, Month.September, 2015));
         expected.Add(new Date(31, Month.March, 2016));
         expected.Add(new Date(30, Month.September, 2016));
         expected.Add(new Date(31, Month.March, 2017));
         expected.Add(new Date(29, Month.September, 2017));
         expected.Add(new Date(30, Month.March, 2018));
         expected.Add(new Date(28, Month.September, 2018));
         expected.Add(new Date(29, Month.March, 2019));
         expected.Add(new Date(30, Month.September, 2019));
         expected.Add(new Date(31, Month.March, 2020));
         expected.Add(new Date(15, Month.June, 2020));
         check_dates(t, expected);
         QAssert.IsTrue(t.isRegular().First() == false);

         // After, with truncation date falling on a schedule date
         t = s.after(new Date(28, Month.September, 2018));
         expected = new List<Date>();
         expected.Add(new Date(28, Month.September, 2018));
         expected.Add(new Date(29, Month.March, 2019));
         expected.Add(new Date(30, Month.September, 2019));
         expected.Add(new Date(31, Month.March, 2020));
         expected.Add(new Date(15, Month.June, 2020));
         check_dates(t, expected);
         QAssert.IsTrue(t.isRegular().First() == true);
         }

      [Fact]
      public void testPreviousDateAndNextDate()
      {
         // Testing next and previous date on schedules

         Schedule s = new MakeSchedule()
            .from(new Date(28, Month.February, 2012))
            .to(new Date(31, Month.August, 2048))
            .withCalendar(new NullCalendar())
            .withTenor(new Period(6, TimeUnit.Months))
            .withConvention(BusinessDayConvention.Unadjusted)
            .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
            .backwards()
            .endOfMonth().value();

         // Previous date
         QAssert.AreEqual(null, s.previousDate(new Date(01, 01, 2001)));
         QAssert.AreEqual(new Date(31, 08, 2015), s.previousDate(new Date(23, 02, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2015), s.previousDate(new Date(24, 02, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2015), s.previousDate(new Date(25, 02, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2015), s.previousDate(new Date(26, 02, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2015), s.previousDate(new Date(27, 02, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2015), s.previousDate(new Date(28, 02, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2015), s.previousDate(new Date(29, 02, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.previousDate(new Date(01, 03, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.previousDate(new Date(02, 03, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.previousDate(new Date(03, 03, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.previousDate(new Date(04, 03, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2048), s.previousDate(new Date(01, 01, 2060)));

         // Next date
         QAssert.AreEqual(new Date(28, 02, 2012), s.nextDate(new Date(01, 01, 2001)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.nextDate(new Date(23, 02, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.nextDate(new Date(24, 02, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.nextDate(new Date(25, 02, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.nextDate(new Date(26, 02, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.nextDate(new Date(27, 02, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.nextDate(new Date(28, 02, 2016)));
         QAssert.AreEqual(new Date(29, 02, 2016), s.nextDate(new Date(29, 02, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2016), s.nextDate(new Date(01, 03, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2016), s.nextDate(new Date(02, 03, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2016), s.nextDate(new Date(03, 03, 2016)));
         QAssert.AreEqual(new Date(31, 08, 2016), s.nextDate(new Date(04, 03, 2016)));
         QAssert.AreEqual(null, s.nextDate(new Date(01, 01, 2060)));
      }

      #endregion

   }
}
