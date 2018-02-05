/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2017 Francois Botha (igitur@gmail.com)

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
   //! Botswana calendar
   /*! Holidays:
   From the Botswana <a href="http://www.ilo.org/dyn/travail/docs/1766/Public%20Holidays%20Act.pdf">Public Holidays Act</a>
   The days named in the Schedule shall be public holidays within Botswana:
   Provided that
   <ul>
   <li>when any of the said days fall on a Sunday the following Monday shall be observed as a public holiday;</li>
   <li>if 2nd January, 1st October or Boxing Day falls on a Monday, the following Tuesday shall be observed as a public holiday;</li>
   <li>when Botswana Day referred to in the Schedule falls on a Saturday, the next following Monday shall be observed as a public holiday.</li>
   </ul>
   <ul>
   <li>Saturdays</li>
   <li>Sundays</li>
   <li>New Year's Day, January 1st</li>
   <li>Good Friday</li>
   <li>Easter Monday</li>
   <li>Labour Day, May 1st</li>
   <li>Ascension</li>
   <li>Sir Seretse Khama Day, July 1st</li>
   <li>Presidents' Day</li>
   <li>Independence Day, September 30th</li>
   <li>Botswana Day, October 1st</li>
   <li>Christmas, December 25th </li>
   <li>Boxing Day, December 26th</li>
   </ul>

   \ingroup calendars
   */
   public class Botswana : Calendar
   {
      public Botswana() : base(Impl.Singleton) { }

      class Impl : Calendar.WesternImpl
      {
         public static readonly Impl Singleton = new Impl();
         private Impl() { }

         public override string name() { return "South Africa"; }
         public override bool isBusinessDay(Date date)
         {
            DayOfWeek w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            Month m = (Month)date.Month;
            int y = date.Year;
            int em = easterMonday(y);

            if (isWeekend(w)
                // New Year's Day (possibly moved to Monday or Tuesday)
                || ((d == 1 || (d == 2 && w == DayOfWeek.Monday) || (d == 3 && w == DayOfWeek.Tuesday))
                    && m == Month.January)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // Labour Day, May 1st (possibly moved to Monday)
                || ((d == 1 || (d == 2 && w == DayOfWeek.Monday))
                    && m == Month.May)
                // Ascension
                || (dd == em + 38)
                // Sir Seretse Khama Day, July 1st (possibly moved to Monday)
                || ((d == 1 || (d == 2 && w == DayOfWeek.Monday))
                    && m == Month.July)
                // Presidents' Day (third Monday of July)
                || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.July)
                // Independence Day, September 30th (possibly moved to Monday)
                || ((d == 30 && m == Month.September) ||
                    (d == 1 && w == DayOfWeek.Monday && m == Month.October))
                // Botswana Day, October 1st (possibly moved to Monday or Tuesday)
                || ((d == 1 || (d == 2 && w == DayOfWeek.Monday) || (d == 3 && w == DayOfWeek.Tuesday))
                    && m == Month.October)
                // Christmas
                || (d == 25 && m == Month.December)
                // Boxing Day (possibly moved to Monday)
                || ((d == 26 || (d == 27 && w == DayOfWeek.Monday))
                    && m == Month.December)
               )
               return false;

            return true;
         }
      }
   }
}

