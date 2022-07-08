/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Andrea Maggiulli
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
using System.Linq;

namespace QLNet
{
   //! New Zealand calendar
   /*! Holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st (possibly moved to Monday or
           Tuesday)</li>
       <li>Day after New Year's Day, January 2st (possibly moved to
           Monday or Tuesday)</li>
       <li>Anniversary Day, Monday nearest January 22nd</li>
       <li>Waitangi Day. February 6th</li>
       <li>Good Friday</li>
       <li>Easter Monday</li>
       <li>ANZAC Day. April 25th</li>
       <li>Queen's Birthday, first Monday in June</li>
       <li>Matariki Holiday Date, based on Maori lunar calendar, always a Friday</li>
       <li>Labour Day, fourth Monday in October</li>
       <li>Christmas, December 25th (possibly moved to Monday or Tuesday)</li>
       <li>Boxing Day, December 26th (possibly moved to Monday or
           Tuesday)</li>
       </ul>
       \note The holiday rules for New Zealand were documented by
             David Gilbert for IDB (http://www.jrefinery.com/ibd/)

       \ingroup calendars
   */
   public class NewZealand : Calendar
   {
      public NewZealand() : base(Impl.Singleton) { }

      class Impl : WesternImpl
      {
         // https://www.beehive.govt.nz/release/matariki-holiday-dates-next-thirty-years-announced
         public Date[] MatarikiHolidays = new[] {
               new Date(24, 6, 2022),
               new Date(14, 7, 2023),
               new Date(28, 6, 2024),
               new Date(20, 6, 2025),
               new Date(10, 7, 2026),
               new Date(25, 6, 2027),
               new Date(14, 7, 2028),
               new Date(6, 7, 2029),
               new Date(21, 6, 2030),
               new Date(11, 7, 2031),
               new Date(2, 7, 2032),
               new Date(24, 6, 2033),
               new Date(7, 7, 2034),
               new Date(29, 6, 2035),
               new Date(18, 7, 2036),
               new Date(10, 7, 2037),
               new Date(25, 6, 2038),
               new Date(15, 7, 2039),
               new Date(6, 7, 2040),
               new Date(19, 7, 2041),
               new Date(11, 7, 2042),
               new Date(3, 7, 2043),
               new Date(24, 6, 2044),
               new Date(7, 7, 2045),
               new Date(29, 6, 2046),
               new Date(19, 7, 2047),
               new Date(3, 7, 2048),
               new Date(25, 6, 2049),
               new Date(15, 7, 2050),
               new Date(30, 6, 2051),
               new Date(21, 6, 2052)
            };

         public static readonly Impl Singleton = new Impl();
         private Impl() { }

         public override string name() { return "New Zealand"; }
         public override bool isBusinessDay(Date date)
         {
            DayOfWeek w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            Month m = (Month)date.Month;
            int y = date.Year;
            int em = easterMonday(y);
            if (isWeekend(w)
                // New Year's Day (possibly moved to Monday or Tuesday)
                || ((d == 1 || (d == 3 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) &&
                    m == Month.January)
                // Day after New Year's Day (possibly moved to Mon or Tuesday)
                || ((d == 2 || (d == 4 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) &&
                    m == Month.January)
                // Anniversary Day, Monday nearest January 22nd
                || ((d >= 19 && d <= 25) && w == DayOfWeek.Monday && m == Month.January)
                // Waitangi Day. February 6th ("Mondayised" since 2013)
                || (d == 6 && m == Month.February)
                || ((d == 7 || d == 8) && w == DayOfWeek.Monday && m == Month.February && y > 2013)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // ANZAC Day. April 25th
                || (d == 25 && m == Month.April)
                // Queen's Birthday, first Monday in June
                || (d <= 7 && w == DayOfWeek.Monday && m == Month.June)
                // Matariki Holiday
                || MatarikiHolidays.Any(x => x == date)
                // Labour Day, fourth Monday in October
                || ((d >= 22 && d <= 28) && w == DayOfWeek.Monday && m == Month.October)
                // Christmas, December 25th (possibly Monday or Tuesday)
                || ((d == 25 || (d == 27 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                    && m == Month.December)
                // Boxing Day, December 26th (possibly Monday or Tuesday)
                || ((d == 26 || (d == 28 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                    && m == Month.December))
               return false;
            return true;
         }
      }
   }
}
