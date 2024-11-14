/*
 Copyright (C) 2008-2024 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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

namespace QLNet
{
   //! South Korean calendars
   /*! Public holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Independence Day, March 1st</li>
       <li>Arbour Day, April 5th (until 2005)</li>
       <li>Labour Day, May 1st</li>
       <li>Children's Day, May 5th</li>
       <li>Memorial Day, June 6th</li>
       <li>Constitution Day, July 17th (until 2007)</li>
       <li>Liberation Day, August 15th</li>
       <li>National Fondation Day, October 3th</li>
       <li>Christmas Day, December 25th</li>
       </ul>

       Other holidays for which no rule is given
       (data available for 2004-2032 only:)
       <ul>
       <li>Lunar New Year, the last day of the previous lunar year</li>
       <li>Election Days</li>
       <li>National Assemblies</li>
       <li>Presidency</li>
       <li>Regional Election Days</li>
       <li>Buddha's birthday</li>
       <li>Harvest Moon Day</li>
       </ul>

       Holidays for the Korea exchange
       (data from <http://www.krx.co.kr> or
       <http://www.dooriworld.com/daishin/holiday/holiday.html>):
       <ul>
       <li>Public holidays as listed above</li>
       <li>Year-end closing</li>
       </ul>

       \ingroup calendars
   */
   public class SouthKorea : Calendar
   {
      public enum Market
      {
         Settlement,  //!< Public holidays
         KRX          //!< Korea exchange
      }

      public SouthKorea() : this(Market.KRX) { }
      public SouthKorea(Market m)
         : base()
      {
         // all calendar instances on the same market share the same
         // implementation instance
         switch (m)
         {
            case Market.Settlement:
               _impl = Settlement.Singleton;
               break;
            case Market.KRX:
               _impl = KRX.Singleton;
               break;
            default:
               throw new ArgumentException("Unknown market: " + m);
         }
      }

      private class Settlement : CalendarImpl
      {
         public static readonly Settlement Singleton = new();
         protected Settlement(){}
         public override string name() { return "South-Korean settlement"; }
         public override bool isWeekend(DayOfWeek w)
         {
            return w == DayOfWeek.Saturday || w == DayOfWeek.Sunday;
         }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;

            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                // Independence Day
                || (d == 1 && m == Month.March)
                // Arbour Day
                || (d == 5 && m == Month.April && y <= 2005)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Children's Day
                || (d == 5 && m == Month.May)
                || (d == 7 && m == Month.May && y == 2018)
                || (d == 6 && m == Month.May && y == 2019)
                // Memorial Day
                || (d == 6 && m == Month.June)
                // Constitution Day
                || (d == 17 && m == Month.July && y <= 2007)
                // Liberation Day
                || (d == 15 && m == Month.August)
                // National Foundation Day
                || (d == 3 && m == Month.October)
                // Christmas Day
                || (d == 25 && m == Month.December)

                // Lunar New Year
                || ((d == 21 || d == 22 || d == 23) && m == Month.January && y == 2004)
                || ((d == 8 || d == 9 || d == 10) && m == Month.February && y == 2005)
                || ((d == 28 || d == 29 || d == 30) && m == Month.January && y == 2006)
                || (d == 19 && m == Month.February && y == 2007)
                || ((d == 6 || d == 7 || d == 8) && m == Month.February && y == 2008)
                || ((d == 25 || d == 26 || d == 27) && m == Month.January && y == 2009)
                || ((d == 13 || d == 14 || d == 15) && m == Month.February && y == 2010)
                || ((d == 2 || d == 3 || d == 4) && m == Month.February && y == 2011)
                || ((d == 23 || d == 24) && m == Month.January && y == 2012)
                || (d == 11 && m == Month.February && y == 2013)
                || ((d == 30 || d == 31) && m == Month.January && y == 2014)
                || ((d == 18 || d == 19 || d == 20) && m == Month.February && y == 2015)
                || ((d == 7 || d == 8 || d == 9) && m == Month.February && y == 2016)
                || ((d >= 27 && d <= 30) && m == Month.January && y == 2017)
                || ((d == 15 || d == 16 || d == 17) && m == Month.February && y == 2018)
                || ((d == 4 || d == 5 || d == 6) && m == Month.February && y == 2019)
                || ((d >= 24 && d <= 27) && m == Month.January && y == 2020)
                || ((d == 11 || d == 12 || d == 13) && m == Month.February && y == 2021)
                || (((d == 31 && m == Month.January) || ((d == 1 || d == 2)
                                                         && m == Month.February)) && y == 2022)
                || ((d == 21 || d == 22 || d == 23) && m == Month.January && y == 2023)
                || ((d == 9 || d == 10 || d == 11) && m == Month.February && y == 2024)
                || ((d == 28 || d == 29 || d == 30) && m == Month.January && y == 2025)
                || ((d == 28 || d == 29 || d == 30) && m == Month.January && y == 2025)
                || ((d == 16 || d == 17 || d == 18) && m == Month.February && y == 2026)
                || ((d == 5 || d == 6 || d == 7) && m == Month.February && y == 2027)
                || ((d == 25 || d == 26 || d == 27) && m == Month.January && y == 2028)
                || ((d == 12 || d == 13 || d == 14) && m == Month.February && y == 2029)
                || ((d == 2 || d == 3 || d == 4) && m == Month.February && y == 2030)
                || ((d == 22 || d == 23 || d == 24) && m == Month.January && y == 2031)
                || ((d == 10 || d == 11 || d == 12) && m == Month.February && y == 2032)
                || (((d == 31 && m == Month.January) || ((d ==  1|| d ==  2) && m == Month.February)) && y == 2033)
                || ((d == 20 || d == 21) && m == Month.February  && y == 2034)
                || ((d ==  7 || d ==  8 || d ==  9) && m == Month.February  && y == 2035)
                || ((d == 28 || d == 29 || d == 30) && m == Month.January   && y == 2036)
                || ((d == 16 || d == 17) && m == Month.February  && y == 2037)
                || ((d ==  3 || d ==  4 || d ==  5) && m == Month.February  && y == 2038)
                || ((d == 24 || d == 25 || d == 26) && m == Month.January   && y == 2039)
                || ((d == 13 || d == 14) && m == Month.February  && y == 2040)
                || (((d == 31 && m == Month.January) || ((d ==  1|| d ==  2) && m == Month.February)) && y == 2041)
                || ((d == 21 || d == 22 || d == 23) && m == Month.January   && y == 2042)
                || ((d ==  9 || d == 10 || d == 11) && m == Month.February  && y == 2043)
                || ((((d == 29|| d == 30|| d == 31) && m == Month.January) || (d ==  1 && m == Month.February)) && y == 2044)
                || ((d == 16 || d == 17 || d == 18) && m == Month.February  && y == 2045)
                || ((d ==  5 || d ==  6 || d ==  7) && m == Month.February  && y == 2046)
                || ((d >= 25 && d <= 28)            && m == Month.January   && y == 2047)
                || ((d == 13 || d == 14 || d == 15) && m == Month.February  && y == 2048)
                || ((d ==  1 || d ==  2 || d ==  3) && m == Month.February  && y == 2049)
                || ((d == 24 || d == 25)            && m == Month.January   && y == 2050)


                // Election Days
                || (d == 15 && m == Month.April && y == 2004)    // National Assembly
                || (d == 31 && m == Month.May && y == 2006)      // Regional election
                || (d == 19 && m == Month.December && y == 2007) // Presidency
                || (d == 9 && m == Month.April && y == 2008)    // National Assembly
                || (d == 2 && m == Month.June && y == 2010)     // Local election
                || (d == 11 && m == Month.April && y == 2012)    // National Assembly
                || (d == 19 && m == Month.December && y == 2012) // Presidency
                || (d == 4 && m == Month.June && y == 2014)        // Local election
                || (d == 13 && m == Month.April && y == 2016)   // National Assembly
                || (d == 9 && m == Month.May && y == 2017) // Presidency
                || (d == 13 && m == Month.June && y == 2018) // Local election
                || (d == 15 && m == Month.April && y == 2020) // National Assembly
                || (d ==  9 && m == Month.March && y == 2022) // Presidency
                || (d ==  1 && m == Month.June  && y == 2022) // Local election
                || (d == 10 && m == Month.April && y == 2024) // National Assembly
                // Buddha's birthday
                || (d == 26 && m == Month.May   && y == 2004)
                || (d == 15 && m == Month.May   && y == 2005)
                || (d ==  5 && m == Month.May   && y == 2006)
                || (d == 24 && m == Month.May   && y == 2007)
                || (d == 12 && m == Month.May   && y == 2008)
                || (d ==  2 && m == Month.May   && y == 2009)
                || (d == 21 && m == Month.May   && y == 2010)
                || (d == 10 && m == Month.May   && y == 2011)
                || (d == 28 && m == Month.May   && y == 2012)
                || (d == 17 && m == Month.May   && y == 2013)
                || (d ==  6 && m == Month.May   && y == 2014)
                || (d == 25 && m == Month.May   && y == 2015)
                || (d == 14 && m == Month.May   && y == 2016)
                || (d ==  3 && m == Month.May   && y == 2017)
                || (d == 22 && m == Month.May   && y == 2018)
                || (d == 12 && m == Month.May   && y == 2019)
                || (d == 30 && m == Month.April && y == 2020)
                || (d == 19 && m == Month.May   && y == 2021)
                || (d ==  8 && m == Month.May   && y == 2022)
                || (d == 29 && m == Month.May   && y == 2023) // Substitute holiday
                || (d == 15 && m == Month.May   && y == 2024)
                || (d ==  6 && m == Month.May   && y == 2025)
                || (d == 25 && m == Month.May   && y == 2026) // Substitute holiday
                || (d == 13 && m == Month.May   && y == 2027)
                || (d ==  2 && m == Month.May   && y == 2028)
                || (d == 21 && m == Month.May   && y == 2029) // Substitute holiday
                || (d ==  9 && m == Month.May   && y == 2030)
                || (d == 28 && m == Month.May   && y == 2031)
                || (d == 17 && m == Month.May   && y == 2032) // Substitute holiday
                || (d ==  6 && m == Month.May   && y == 2033)
                || (d == 25 && m == Month.May   && y == 2034)
                || (d == 15 && m == Month.May   && y == 2035)
                || (d ==  6 && m == Month.May   && y == 2036) // Substitute holiday
                || (d == 22 && m == Month.May   && y == 2037)
                || (d == 11 && m == Month.May   && y == 2038)
                || (d ==  2 && m == Month.May   && y == 2039) // Substitute holiday
                || (d == 18 && m == Month.May   && y == 2040)
                || (d ==  7 && m == Month.May   && y == 2041)
                || (d == 26 && m == Month.May   && y == 2042)
                || (d == 18 && m == Month.May   && y == 2043) // Substitute holiday
                || (d ==  6 && m == Month.May   && y == 2044)
                || (d == 24 && m == Month.May   && y == 2045)
                || (d == 14 && m == Month.May   && y == 2046) // Substitute holiday
                || (d ==  2 && m == Month.May   && y == 2047)
                || (d == 20 && m == Month.May   && y == 2048)
                || (d == 10 && m == Month.May   && y == 2049) // Substitute holiday
                || (d == 30 && m == Month.May   && y == 2050) // Substitute holiday
                // Special holiday: 70 years from Independence Day
                || (d == 14 && m == Month.August && y == 2015)
                // Special temporary holiday
                || (d == 17 && m == Month.August && y == 2020)
                || (d == 2 && m == Month.October && y == 2023)
                || (d == 1 && m == Month.October && y == 2024)
                // Harvest Moon Day
            || ((d == 27 || d == 28 || d == 29) && m == Month.September && y == 2004)
            || ((d == 17 || d == 18 || d == 19) && m == Month.September && y == 2005)
            || ((d ==  5 || d ==  6 || d ==  7) && m == Month.October   && y == 2006)
            || ((d == 24 || d == 25 || d == 26) && m == Month.September && y == 2007)
            || ((d == 13 || d == 14 || d == 15) && m == Month.September && y == 2008)
            || ((d ==  2 || d ==  3 || d ==  4) && m == Month.October   && y == 2009)
            || ((d == 21 || d == 22 || d == 23) && m == Month.September && y == 2010)
            || ((d == 12 || d == 13)            && m == Month.September && y == 2011)
            || ((d == 1)                        && m == Month.October   && y == 2012)
            || ((d == 18 || d == 19 || d == 20) && m == Month.September && y == 2013)
            || ((d ==  8 || d ==  9 || d == 10) && m == Month.September && y == 2014)
            || ((d == 28 || d == 29)            && m == Month.September && y == 2015)
            || ((d == 14 || d == 15 || d == 16) && m == Month.September && y == 2016)
            || ((d >=  3 && d <=  6)            && m == Month.October   && y == 2017)
            || ((d >= 23 && d <= 26)            && m == Month.September && y == 2018)
            || ((d == 12 || d == 13 || d == 14) && m == Month.September && y == 2019)
            || (((d == 30 && m == Month.September) || ((d == 1 || d == 2)
                                                && m == Month.October)) && y == 2020)
            || ((d == 20 || d == 21 || d == 22) && m == Month.September && y == 2021)
            || ((d ==  9 || d == 10 || d == 11) && m == Month.September && y == 2022)
            || ((d >=  9 && d <= 12)            && m == Month.September && y == 2022)
            || ((d == 28 || d == 29 || d == 30) && m == Month.September && y == 2023)
            || ((d == 16 || d == 17 || d == 18) && m == Month.September && y == 2024)
            || ((d ==  6 || d ==  7 || d ==  8) && m == Month.October   && y == 2025)
            || ((d == 24 || d == 25 || d == 26) && m == Month.September && y == 2026)
            || ((d == 14 || d == 15 || d == 16) && m == Month.September && y == 2027)
            || ((d >=  2 && d <=  5)            && m == Month.October   && y == 2028)
            || ((d >= 21 && d <= 24)            && m == Month.September && y == 2029)
            || ((d == 11 || d == 12 || d == 13) && m == Month.September && y == 2030)
            || (((d == 30 && m == Month.September) || ((d == 1 || d == 2)
                                                      && m == Month.October)) && y == 2031)
            || ((d == 20 || d == 21) && m == Month.September && y == 2032)
            || ((d ==  7 || d ==  8 || d ==  9) && m == Month.September && y == 2033)
            || ((d == 26 || d == 27 || d == 28) && m == Month.September && y == 2034)
            || ((d == 17 || d == 18) && m == Month.September && y == 2035)
            || ((d >=  3 && d <=  7) && m == Month.October   && y == 2036)
            || ((d == 23 || d == 24 || d == 25) && m == Month.September && y == 2037)
            || ((d == 13 || d == 14 || d == 15) && m == Month.September && y == 2038)
            || ((d ==  3 || d ==  4 || d == 5) && m == Month.October   && y == 2039)
            || ((d == 20 || d == 21 || d == 22) && m == Month.September && y == 2040)
            || ((d ==  9 || d == 10 || d == 11) && m == Month.September && y == 2041)
            || ((d == 29 || d == 30) && m == Month.September && y == 2042)
            || ((d == 16 || d == 17 || d == 18) && m == Month.September && y == 2043)
            || ((d ==  4 || d ==  5 || d ==  6) && m == Month.October   && y == 2044)
            || ((d == 25 || d == 26 || d == 27) && m == Month.September && y == 2045)
            || ((d >= 14 && d <= 17) && m == Month.September && y == 2046)
            || ((d ==  4 || d ==  5 || d ==  7) && m == Month.October   && y == 2047)
            || ((d == 21 || d == 22 || d == 23) && m == Month.September && y == 2048)
            || ((d >= 10 && d <= 13) && m == Month.September && y == 2049)
            || ((((d == 29|| d == 30) && m == Month.September) 
                                    || (d ==  1 && m == Month.October)) && y == 2050)

            // Hangul Proclamation of Korea
            || (d == 9 && m == Month.October && y >= 2013)
            || (w == DayOfWeek.Monday && (d == 10 || d == 11) && m == Month.October && y > 2020)
               )
               return false;

            return true;
         }
      }

      private class KRX : Settlement
      {
         public new static readonly KRX Singleton = new();
         private KRX(){}
         public override string name() { return "South-Korea exchange"; }
         public override bool isBusinessDay(Date date)
         {
            // public holidays
            if (!base.isBusinessDay(date))
               return false;

            var d = date.Day;
            var w = date.DayOfWeek;
            var m = (Month)date.Month;
            var y = date.Year;

            if ( // Year-end closing
               ((((d == 29 || d == 30) && w == DayOfWeek.Friday) || d == 31)
                && m == Month.December)
            )
               return false;

            if (// occasional closing days (KRX day)
               (d == 6 && m == Month.May && y == 2016) ||
               (d == 2 && m == Month.October && y == 2017)
            )
               return false;

            return true;
         }
      }
   }
}




