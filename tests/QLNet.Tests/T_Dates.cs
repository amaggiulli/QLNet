/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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
   public class T_Dates
   {
#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testECBDates()
      {
         // Testing ECB dates

         List<Date> knownDates = ECB.knownDates();
         if (knownDates.empty())
            QAssert.Fail("Empty EBC date vector");

         int n = ECB.nextDates(Date.minDate()).Count;
    
         if (n != knownDates.Count)
            QAssert.Fail("NextDates(minDate) returns "  + n +
                   " instead of " + knownDates.Count + " dates");

         Date previousEcbDate = Date.minDate(),
         currentEcbDate, ecbDateMinusOne;
         
         for (int i=0; i<knownDates.Count; ++i) 
         {

            currentEcbDate = knownDates[i];
         
            if (!ECB.isECBdate(currentEcbDate))
               QAssert.Fail( currentEcbDate + " fails isECBdate check");

            ecbDateMinusOne = currentEcbDate-1;
            if (ECB.isECBdate(ecbDateMinusOne))
               QAssert.Fail(ecbDateMinusOne + " fails isECBdate check");

            if (ECB.nextDate(ecbDateMinusOne)!=currentEcbDate)
               QAssert.Fail("Next EBC date following " + ecbDateMinusOne +
                     " must be " + currentEcbDate);

            if (ECB.nextDate(previousEcbDate)!=currentEcbDate)
               QAssert.Fail("Next EBC date following " + previousEcbDate +
                     " must be " + currentEcbDate);

            previousEcbDate = currentEcbDate;
         }
         
         Date knownDate = knownDates.First();
         ECB.removeDate(knownDate);
         if (ECB.isECBdate(knownDate))
            QAssert.Fail("Unable to remove an EBC date");
    
         ECB.addDate(knownDate);
         if (!ECB.isECBdate(knownDate))
            QAssert.Fail("Unable to add an EBC date");

      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testIMMDates()
      {
         // ("Testing IMM dates...");

         string[] IMMcodes = new string[] {
                "F0", "G0", "H0", "J0", "K0", "M0", "N0", "Q0", "U0", "V0", "X0", "Z0",
                "F1", "G1", "H1", "J1", "K1", "M1", "N1", "Q1", "U1", "V1", "X1", "Z1",
                "F2", "G2", "H2", "J2", "K2", "M2", "N2", "Q2", "U2", "V2", "X2", "Z2",
                "F3", "G3", "H3", "J3", "K3", "M3", "N3", "Q3", "U3", "V3", "X3", "Z3",
                "F4", "G4", "H4", "J4", "K4", "M4", "N4", "Q4", "U4", "V4", "X4", "Z4",
                "F5", "G5", "H5", "J5", "K5", "M5", "N5", "Q5", "U5", "V5", "X5", "Z5",
                "F6", "G6", "H6", "J6", "K6", "M6", "N6", "Q6", "U6", "V6", "X6", "Z6",
                "F7", "G7", "H7", "J7", "K7", "M7", "N7", "Q7", "U7", "V7", "X7", "Z7",
                "F8", "G8", "H8", "J8", "K8", "M8", "N8", "Q8", "U8", "V8", "X8", "Z8",
                "F9", "G9", "H9", "J9", "K9", "M9", "N9", "Q9", "U9", "V9", "X9", "Z9"
            };

         Date counter = Date.minDate();
         // 10 years of futures must not exceed Date::maxDate
         Date last = Date.maxDate() - new Period(121, TimeUnit.Months);
         Date imm;

         while (counter <= last)
         {
            imm = IMM.nextDate(counter, false);

            // check that imm is greater than counter
            if (imm <= counter)
               QAssert.Fail(imm.DayOfWeek + " " + imm
                          + " is not greater than "
                          + counter.DayOfWeek + " " + counter);

            // check that imm is an IMM date
            if (!IMM.isIMMdate(imm, false))
               QAssert.Fail(imm.DayOfWeek + " " + imm
                          + " is not an IMM date (calculated from "
                          + counter.DayOfWeek + " " + counter + ")");

            // check that imm is <= to the next IMM date in the main cycle
            if (imm > IMM.nextDate(counter, true))
               QAssert.Fail(imm.DayOfWeek + " " + imm
                          + " is not less than or equal to the next future in the main cycle "
                          + IMM.nextDate(counter, true));

            //// check that if counter is an IMM date, then imm==counter
            //if (IMM::isIMMdate(counter, false) && (imm!=counter))
            //    BOOST_FAIL("\n  "
            //               << counter.weekday() << " " << counter
            //               << " is already an IMM date, while nextIMM() returns "
            //               << imm.weekday() << " " << imm);

            // check that for every date IMMdate is the inverse of IMMcode
            if (IMM.date(IMM.code(imm), counter) != imm)
               QAssert.Fail(IMM.code(imm)
                          + " at calendar day " + counter
                          + " is not the IMM code matching " + imm);

            // check that for every date the 120 IMM codes refer to future dates
            for (int i = 0; i < 40; ++i)
            {
               if (IMM.date(IMMcodes[i], counter) < counter)
                  QAssert.Fail(IMM.date(IMMcodes[i], counter)
                         + " is wrong for " + IMMcodes[i]
                         + " at reference date " + counter);
            }

            counter = counter + 1;
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testConsistency()
      {
         //("Testing dates...");

         int minDate = Date.minDate().serialNumber() + 1,
                    maxDate = Date.maxDate().serialNumber();

         int dyold = new Date(minDate - 1).DayOfYear,
             dold = new Date(minDate - 1).Day,
             mold = new Date(minDate - 1).Month,
             yold = new Date(minDate - 1).Year,
             wdold = new Date(minDate - 1).weekday();

         for (int i = minDate; i <= maxDate; i++)
         {
            Date t = new Date(i);
            int serial = t.serialNumber();

            // check serial number consistency
            if (serial != i)
               QAssert.Fail("inconsistent serial number:\n"
                          + "    original:      " + i + "\n"
                          + "    date:          " + t + "\n"
                          + "    serial number: " + serial);

            int dy = t.DayOfYear,
                d = t.Day,
                m = t.Month,
                y = t.Year,
                wd = t.weekday();

            // check if skipping any date
            if (!((dy == dyold + 1) ||
                  (dy == 1 && dyold == 365 && !Date.IsLeapYear(yold)) ||
                  (dy == 1 && dyold == 366 && Date.IsLeapYear(yold))))
               QAssert.Fail("wrong day of year increment: \n"
                          + "    date: " + t + "\n"
                          + "    day of year: " + dy + "\n"
                          + "    previous:    " + dyold);
            dyold = dy;

            if (!((d == dold + 1 && m == mold && y == yold) ||
                  (d == 1 && m == mold + 1 && y == yold) ||
                  (d == 1 && m == 1 && y == yold + 1)))
               QAssert.Fail("wrong day,month,year increment: \n"
                          + "    date: " + t + "\n"
                          + "    day,month,year: "
                          + d + "," + m + "," + y + "\n"
                          + "    previous:       "
                          + dold + "," + mold + "," + yold);
            dold = d; mold = m; yold = y;

            // check month definition
            if (m < 1 || m > 12)
               QAssert.Fail("invalid month: \n"
                          + "    date:  " + t + "\n"
                          + "    month: " + m);

            // check day definition
            if (d < 1)
               QAssert.Fail("invalid day of month: \n"
                          + "    date:  " + t + "\n"
                          + "    day: " + d);
            if (!((m == 1 && d <= 31) ||
                  (m == 2 && d <= 28) ||
                  (m == 2 && d == 29 && Date.IsLeapYear(y)) ||
                  (m == 3 && d <= 31) ||
                  (m == 4 && d <= 30) ||
                  (m == 5 && d <= 31) ||
                  (m == 6 && d <= 30) ||
                  (m == 7 && d <= 31) ||
                  (m == 8 && d <= 31) ||
                  (m == 9 && d <= 30) ||
                  (m == 10 && d <= 31) ||
                  (m == 11 && d <= 30) ||
                  (m == 12 && d <= 31)))
               QAssert.Fail("invalid day of month: \n"
                          + "    date:  " + t + "\n"
                          + "    day: " + d);

            // check weekday definition
            if (!((wd == wdold + 1) ||
                  (wd == 1 && wdold == 7)))
               QAssert.Fail("invalid weekday: \n"
                          + "    date:  " + t + "\n"
                          + "    weekday:  " + wd + "\n"
                          + "    previous: " + wdold);
            wdold = wd;

            // create the same date with a different constructor
            Date s = new Date(d, m, y);
            // check serial number consistency
            serial = s.serialNumber();
            if (serial != i)
               QAssert.Fail("inconsistent serial number:\n"
                          + "    date:          " + t + "\n"
                          + "    serial number: " + i + "\n"
                          + "    cloned date:   " + s + "\n"
                          + "    serial number: " + serial);
         }

      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testASXDates()
      {
         //Testing ASX dates...");

         String[] ASXcodes = {
         "F0", "G0", "H0", "J0", "K0", "M0", "N0", "Q0", "U0", "V0", "X0", "Z0",
         "F1", "G1", "H1", "J1", "K1", "M1", "N1", "Q1", "U1", "V1", "X1", "Z1",
         "F2", "G2", "H2", "J2", "K2", "M2", "N2", "Q2", "U2", "V2", "X2", "Z2",
         "F3", "G3", "H3", "J3", "K3", "M3", "N3", "Q3", "U3", "V3", "X3", "Z3",
         "F4", "G4", "H4", "J4", "K4", "M4", "N4", "Q4", "U4", "V4", "X4", "Z4",
         "F5", "G5", "H5", "J5", "K5", "M5", "N5", "Q5", "U5", "V5", "X5", "Z5",
         "F6", "G6", "H6", "J6", "K6", "M6", "N6", "Q6", "U6", "V6", "X6", "Z6",
         "F7", "G7", "H7", "J7", "K7", "M7", "N7", "Q7", "U7", "V7", "X7", "Z7",
         "F8", "G8", "H8", "J8", "K8", "M8", "N8", "Q8", "U8", "V8", "X8", "Z8",
         "F9", "G9", "H9", "J9", "K9", "M9", "N9", "Q9", "U9", "V9", "X9", "Z9" };

         Date counter = Date.minDate();
         // 10 years of futures must not exceed Date::maxDate
         Date last = Date.maxDate() - new Period(121 , TimeUnit.Months);
         Date asx;

         while (counter <= last) 
         {
            asx = ASX.nextDate(counter, false);

            // check that asx is greater than counter
            if (asx <= counter)
               QAssert.Fail( asx.weekday() + " " + asx
                            + " is not greater than "
                            + counter.weekday() + " " + counter);

            // check that asx is an ASX date
            if (!ASX.isASXdate(asx, false))
               QAssert.Fail( asx.weekday() + " " + asx
                            + " is not an ASX date (calculated from "
                            + counter.weekday() + " " + counter + ")");

            // check that asx is <= to the next ASX date in the main cycle
            if (asx > ASX.nextDate(counter, true))
               QAssert.Fail( asx.weekday() + " " + asx
                           + " is not less than or equal to the next future in the main cycle "
                           + ASX.nextDate(counter, true));


            // check that for every date ASXdate is the inverse of ASXcode
            if (ASX.date(ASX.code(asx), counter) != asx)
               QAssert.Fail( ASX.code(asx)
                            + " at calendar day " + counter
                            + " is not the ASX code matching " + asx);

            // check that for every date the 120 ASX codes refer to future dates
            for (int i = 0; i<120; ++i) 
            {
               if (ASX.date(ASXcodes[i], counter)<counter)
                  QAssert.Fail( ASX.date(ASXcodes[i], counter)
                               + " is wrong for " + ASXcodes[i]
                               + " at reference date " + counter);
            }

            counter = counter + 1;
         }

      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testIntraday() 
      {
         // Testing intraday information of dates

         Date d1 = new Date(12, Month.February, 2015, 10, 45, 12, 234);

         QAssert.IsTrue(d1.year() == 2015, "failed to reproduce year");
         QAssert.IsTrue(d1.month() == (int)Month.February, "failed to reproduce month");
         QAssert.IsTrue(d1.Day == 12, "failed to reproduce day");
         QAssert.IsTrue(d1.hours == 10, "failed to reproduce hour of day");
         QAssert.IsTrue(d1.minutes == 45,"failed to reproduce minute of hour");
         QAssert.IsTrue(d1.seconds == 12,"failed to reproduce second of minute");
         QAssert.IsTrue(d1.milliseconds == 234, "failed to reproduce number of milliseconds" );

         QAssert.IsTrue(d1.fractionOfSecond == 0.234,"failed to reproduce fraction of second");


         Date d2 = new Date(28, Month.February, 2015, 4, 52, 57, 999);
         QAssert.IsTrue(d2.year() == 2015, "failed to reproduce year");
         QAssert.IsTrue(d2.month() == (int)Month.February, "failed to reproduce month");
         QAssert.IsTrue(d2.Day == 28, "failed to reproduce day");
         QAssert.IsTrue(d2.hours == 4, "failed to reproduce hour of day");
         QAssert.IsTrue(d2.minutes == 52,"failed to reproduce minute of hour");
         QAssert.IsTrue(d2.seconds == 57,"failed to reproduce second of minute");
         QAssert.IsTrue( d2.milliseconds == 999, "failed to reproduce number of milliseconds" );

         // test daysBetween when d2 time part is earlier in the day than d1 time part.
         d1 = new Date( new DateTime( 2016, 1, 1, 18, 0, 0 ) );
         d2 = new Date( new DateTime( 2016, 1, 2, 0, 0, 0 ) );
         QAssert.IsTrue( Date.daysBetween(d1, d2) == 0.25, "failed daysBetween" );
      }

   }
}