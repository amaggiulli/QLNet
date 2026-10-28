/*
 Copyright (C) 2008 Alessandro Duci
 c
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
   /// <summary>
   /// Botswana calendar
   /// </summary>
   /// <remarks>
   /// Holidays:
   /// From the Botswana &lt;a href="http://www.ilo.org/dyn/travail/docs/1766/Public20Holidays20Act.pdf"&gt;Public Holidays Act&lt;/a&gt;
   /// The days named in the Schedule shall be public holidays within Botswana:
   /// Provided that
   /// when any of the said days fall on a Sunday the following Monday shall be observed as a public holiday;
   /// if 2nd January, 1st October or Boxing Day falls on a Monday, the following Tuesday shall be observed as a public holiday;
   /// when Botswana Day referred to in the Schedule falls on a Saturday, the next following Monday shall be observed as a public holiday.
   /// Saturdays
   /// Sundays
   /// New Year's Day, January 1st
   /// Good Friday
   /// Easter Monday
   /// Labour Day, May 1st
   /// Ascension
   /// Sir Seretse Khama Day, July 1st
   /// Presidents' Day
   /// Independence Day, September 30th
   /// Botswana Day, October 1st
   /// Christmas, December 25th 
   /// Boxing Day, December 26th
   /// </remarks>
   public class Botswana : Calendar
   {
      public Botswana() : base(Impl.Singleton) { }

      private class Impl : WesternImpl
      {
         private Impl() { }
         public static readonly Impl Singleton = new();
         public override string name() { return "South Africa"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);

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

