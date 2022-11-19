/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)
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
   //! Australian calendar
   /*! Holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Australia Day, January 26th (possibly moved to Monday)</li>
       <li>Good Friday</li>
       <li>Easter Monday</li>
       <li>ANZAC Day. April 25th (possibly moved to Monday)</li>
       <li>Queen's Birthday, second Monday in June</li>
       <li>Bank Holiday, first Monday in August</li>
       <li>Labour Day, first Monday in October</li>
       <li>Christmas, December 25th (possibly moved to Monday or Tuesday)</li>
       <li>Boxing Day, December 26th (possibly moved to Monday or
           Tuesday)</li>
       </ul>

       \ingroup calendars
   */
   public class Australia : Calendar
   {
      public Australia() : base(Impl.Singleton) {}

      private class Impl : WesternImpl
      {
         private Impl() { }
         public static readonly Impl Singleton = new();
         public override string name() { return "Australia"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);

            if (isWeekend(w)
                // New Year's Day (possibly moved to Monday)
                || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == Month.January)
                // Australia Day, January 26th (possibly moved to Monday)
                || ((d == 26 || ((d == 27 || d == 28) && w == DayOfWeek.Monday)) &&
                    m == Month.January)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // ANZAC Day, April 25th
                || (d == 25 && m == Month.April)
                // Queen's Birthday, second Monday in June
                || ((d > 7 && d <= 14) && w == DayOfWeek.Monday && m == Month.June)
                // Bank Holiday, first Monday in August
                || (d <= 7 && w == DayOfWeek.Monday && m == Month.August)
                // Labour Day, first Monday in October
                || (d <= 7 && w == DayOfWeek.Monday && m == Month.October)
                // Christmas, December 25th (possibly Monday or Tuesday)
                || ((d == 25 || (d == 27 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                    && m == Month.December)
                // Boxing Day, December 26th (possibly Monday or Tuesday)
                || ((d == 26 || (d == 28 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                    && m == Month.December)
                // National Day of Mourning for Her Majesty, September 22 (only 2022)
                || (d == 22 && m == Month.September && y == 2022))
               return false;
            return true;
         }
      }
   }

}




