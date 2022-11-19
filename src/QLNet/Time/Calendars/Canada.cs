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
   //  Canadian calendar
   /*  Banking holidays:
       
       Saturdays
       Sundays
       New Year's Day, January 1st (possibly moved to Monday)
       Family Day, third Monday of February (since 2008)
       Good Friday
       Easter Monday
       Victoria Day, The Monday on or preceding 24 May
       Canada Day, July 1st (possibly moved to Monday)
       Provincial Holiday, first Monday of August
       Labour Day, first Monday of September
       Thanksgiving Day, second Monday of October
       Remembrance Day, November 11th (possibly moved to Monday)
       Christmas, December 25th (possibly moved to Monday or Tuesday)
       Boxing Day, December 26th (possibly moved to Monday or Tuesday)

       Holidays for the Toronto stock exchange (TSX):
       
       Saturdays
       Sundays
       New Year's Day, January 1st (possibly moved to Monday)
       Family Day, third Monday of February (since 2008)
       Good Friday
       Easter Monday
       Victoria Day, The Monday on or preceding 24 May
       Canada Day, July 1st (possibly moved to Monday)
       Provincial Holiday, first Monday of August
       Labour Day, first Monday of September
       Thanksgiving Day, second Monday of October
       Christmas, December 25th (possibly moved to Monday or Tuesday)
       Boxing Day, December 26th (possibly moved to Monday or Tuesday)

   */
   public class Canada : Calendar
   {
      public enum Market
      {
         Settlement,       //!< generic settlement calendar
         TSX               //!< Toronto stock exchange calendar
      }

      public Canada() : this(Market.Settlement) { }
      public Canada(Market m)
         : base()
      {
         // all calendar instances on the same market share the same
         // implementation instance
         _impl = m switch
         {
            Market.Settlement => Settlement.Singleton,
            Market.TSX => TSX.Singleton,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }

      private class Settlement : WesternImpl
      {
         private Settlement() { }
         public static readonly Settlement Singleton = new();
         public override string name() { return "Canada"; }
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
                // Family Day (third Monday in February, since 2008)
                || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.February
                    && y >= 2008)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // The Monday on or preceding 24 May (Victoria Day)
                || (d > 17 && d <= 24 && w == DayOfWeek.Monday && m == Month.May)
                // July 1st, possibly moved to Monday (Canada Day)
                || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == Month.July)
                // first Monday of August (Provincial Holiday)
                || (d <= 7 && w == DayOfWeek.Monday && m == Month.August)
                // first Monday of September (Labor Day)
                || (d <= 7 && w == DayOfWeek.Monday && m == Month.September)
                // September 30th, possibly moved to Monday
                // (National Day for Truth and Reconciliation, since 2021)
                || (((d == 30 && m == Month.September) || (d <= 2 && m == Month.October && w == DayOfWeek.Monday)) && y >= 2021)
                // second Monday of October (Thanksgiving Day)
                || (d > 7 && d <= 14 && w == DayOfWeek.Monday && m == Month.October)
                // November 11th (possibly moved to Monday)
                || ((d == 11 || ((d == 12 || d == 13) && w == DayOfWeek.Monday))
                    && m == Month.November)
                // Christmas (possibly moved to Monday or Tuesday)
                || ((d == 25 || (d == 27 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                    && m == Month.December)
                // Boxing Day (possibly moved to Monday or Tuesday)
                || ((d == 26 || (d == 28 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                    && m == Month.December)
               )
               return false;
            return true;
         }
      }

      private class TSX : WesternImpl
      {
         private TSX() { }
         public static readonly TSX Singleton = new();
         public override string name() { return "TSX"; }
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
                // Family Day (third Monday in February, since 2008)
                || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.February
                    && y >= 2008)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // The Monday on or preceding 24 May (Victoria Day)
                || (d > 17 && d <= 24 && w == DayOfWeek.Monday && m == Month.May)
                // July 1st, possibly moved to Monday (Canada Day)
                || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == Month.July)
                // first Monday of August (Provincial Holiday)
                || (d <= 7 && w == DayOfWeek.Monday && m == Month.August)
                // first Monday of September (Labor Day)
                || (d <= 7 && w == DayOfWeek.Monday && m == Month.September)
                // second Monday of October (Thanksgiving Day)
                || (d > 7 && d <= 14 && w == DayOfWeek.Monday && m == Month.October)
                // Christmas (possibly moved to Monday or Tuesday)
                || ((d == 25 || (d == 27 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                    && m == Month.December)
                // Boxing Day (possibly moved to Monday or Tuesday)
                || ((d == 26 || (d == 28 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                    && m == Month.December)
               )
               return false;
            return true;
         }
      }
   }
}
