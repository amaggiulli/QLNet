/*
 Copyright (C) 2012  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

namespace TestSuite
{
   [TestClass()]
   public class T_Schedule
   {
      void check_dates(Schedule s, List<Date> expected) 
      {
         if (s.Count != expected.Count) 
         {
            Assert.Fail("expected " + expected.Count + " dates, " + "found " + s.Count);
         }

         for (int i=0; i<expected.Count; ++i) 
         {
            if (s[i] != expected[i]) 
            {
               Assert.Fail ( "expected " + expected[i] + " at index " + i + ", " + "found " + s[i]);
        
            }
         }
      }
      
      [TestMethod()]
      public void testDailySchedule()
      {
         // Testing schedule with daily frequency

         Date startDate = new Date(17,Month.January,2012);

         Schedule s = new MakeSchedule().from(startDate).to(startDate + 7)
                      .withCalendar(new TARGET())
                      .withConvention(BusinessDayConvention.Preceding)
                      .withFrequency(Frequency.Daily).value();

         List<Date> expected = new List<Date>(6);
         // The schedule should skip Saturday 21st and Sunday 22rd.
         // Previously, it would adjust them to Friday 20th, resulting
         // in three copies of the same date.
         expected.Add(new Date(17,Month.January,2012));
         expected.Add(new Date(18,Month.January,2012));
         expected.Add(new Date(19,Month.January,2012));
         expected.Add(new Date(20,Month.January,2012));
         expected.Add(new Date(23,Month.January,2012));
         expected.Add(new Date(24,Month.January,2012));

         check_dates(s, expected);
      }

      [TestMethod()]
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
                           .withTenor(new Period(6 , TimeUnit.Months))
                           .withConvention(BusinessDayConvention.Following)
                           .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                           .forwards()
                           .endOfMonth().value();
         // ...which should leave it alone.
         expected[6] = new Date(15, Month.June, 2012);

         check_dates(s, expected);
      }

      [TestMethod()]
      public void testDatesPastEndDateWithEomAdjustment() 
      {

         Schedule s = new MakeSchedule().from(new Date(28,Month.March,2013))
                           .to(new Date(30,Month.March,2015))
                           .withCalendar(new TARGET())
                           .withTenor(new Period(1,TimeUnit.Years))
                           .withConvention(BusinessDayConvention.Unadjusted)
                           .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
                           .forwards()
                           .endOfMonth().value();

         List<Date> expected = new List<Date>();
         expected.Add(new  Date(31,Month.March,2013));
         expected.Add(new Date(31,Month.March,2014));
         // March 31st 2015, coming from the EOM adjustment of March 28th,
         // should be discarded as past the end date.
         expected.Add(new Date(30,Month.March,2015));

         check_dates(s, expected);
}

   }

}
