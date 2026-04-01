/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)

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
   /// Italian calendars
   /// </summary>
   /// <remarks>
   /// Public holidays:
   /// Saturdays
   /// Sundays
   /// New Year's Day, January 1st
   /// Epiphany, January 6th
   /// Easter Monday
   /// Liberation Day, April 25th
   /// Labour Day, May 1st
   /// Republic Day, June 2nd (since 2000)
   /// Assumption, August 15th
   /// All Saint's Day, November 1st
   /// Immaculate Conception Day, December 8th
   /// Christmas Day, December 25th
   /// St. Stephen's Day, December 26th
   ///
   /// Holidays for the stock exchange (data from http://www.borsaitalia.it):
   /// Saturdays
   /// Sundays
   /// New Year's Day, January 1st
   /// Good Friday
   /// Easter Monday
   /// Labour Day, May 1st
   /// Assumption, August 15th
   /// Christmas' Eve, December 24th
   /// Christmas, December 25th
   /// St. Stephen, December 26th
   /// New Year's Eve, December 31st
   ///
   ///
   /// Test: the correctness of the returned results is tested against a
   /// list of known holidays.
   /// </remarks>
   public class Italy : Calendar
   {
      /// <summary>
      /// Available Italian calendar markets.
      /// </summary>
      public enum Market
      {
         /// <summary>
         /// Generic settlement calendar.
         /// </summary>
         Settlement,

         /// <summary>
         /// Milan Stock Exchange calendar.
         /// </summary>
         Exchange
      }

      public Italy() : this(Market.Settlement) { }
      public Italy(Market m)
         : base()
      {
         // all calendar instances on the same market share the same
         // implementation instance
         _impl = m switch
         {
            Market.Settlement => Settlement.Singleton,
            Market.Exchange => Exchange.Singleton,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }


      private class Settlement : WesternImpl
      {
         public static readonly Settlement Singleton = new();
         private Settlement() { }

         public override string name() { return "Italian settlement"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);

            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                // Epiphany
                || (d == 6 && m == Month.January)
                // Easter Monday
                || (dd == em)
                // Liberation Day
                || (d == 25 && m == Month.April)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Republic Day
                || (d == 2 && m == Month.June && y >= 2000)
                // Assumption
                || (d == 15 && m == Month.August)
                // All Saints' Day
                || (d == 1 && m == Month.November)
                // Immaculate Conception
                || (d == 8 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // St. Stephen
                || (d == 26 && m == Month.December)
                // December 31st, 1999 only
                || (d == 31 && m == Month.December && y == 1999))
               return false;
            return true;
         }
      }

      private class Exchange : WesternImpl
      {
         public static readonly Exchange Singleton = new();
         private Exchange() { }

         public override string name() { return "Milan stock exchange"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);

            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Assumption
                || (d == 15 && m == Month.August)
                // Christmas' Eve
                || (d == 24 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // St. Stephen
                || (d == 26 && m == Month.December)
                // New Year's Eve
                || (d == 31 && m == Month.December))
               return false;
            return true;
         }
      }
   }

}
