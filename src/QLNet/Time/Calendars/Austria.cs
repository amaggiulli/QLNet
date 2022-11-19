/*
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
   /* Austrian calendars
    Public holidays:
       Saturdays
       Sundays
       New Year's Day, January 1st
       Epiphany, January 6th
       Easter Monday
       Ascension Thursday
       Whit Monday
       Corpus Christi
       Labour Day, May 1st
       Assumption Day, August 15th
       National Holiday, October 26th, since 1967
       All Saints Day, November 1st
       National Holiday, November 12th, 1919-1934
       Immaculate Conception Day, December 8th
       Christmas, December 25th
       St. Stephen, December 26th

   Holidays for the stock exchange (data from https://www.wienerborse.at/en/trading/trading-information/trading-calendar/):
       
       Saturdays
       Sundays
       New Year's Day, January 1st
       Good Friday
       Easter Monday
       Whit Monday
       Labour Day, May 1st
       National Holiday, October 26th, since 1967
       National Holiday, November 12th, 1919-1934
       Christmas Eve, December 24th
       Christmas, December 25th<
       St. Stephen, December 26th
       Exchange Holiday

   */
   public class Austria : Calendar
   {
      // Austrian calendars
      public enum Market
      {
         Settlement, // generic settlement calendar
         Exchange // Vienna stock-exchange calendar
      };

      public Austria() : this(Market.Settlement) {}

      public Austria(Market m) : base()
      {
         _impl = m switch
         {
            Market.Settlement => Settlement.Singleton,
            Market.Exchange => Exchange.Singleton,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }

      private class Settlement : WesternImpl
      {
         private Settlement() { }
         public static readonly Settlement Singleton = new();

         public override string name() { return "Austrian settlement"; }

         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);
            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.February)
                // Epiphany
                || (d == 6 && m == Month.January)
                // Easter Monday
                || (dd == em)
                // Ascension Thurday 
                || (dd == em + 38)
                // Whit Monday
                || (dd == em + 49)
                // Corpus Christi
                || (dd == em + 59)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Assumption
                || (d == 15 && m == Month.August)
                // National Holiday since 1967
                || (d == 26 && m == Month.October && y >= 1967)
                // National Holiday 1919-1934
                || (d == 12 && m == Month.November && y >= 1919 && y <= 1934)
                // All Saints' Day
                || (d == 1 && m == Month.November)
                // Immaculate Conception
                || (d == 8 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // St. Stephen
                || (d == 26 && m == Month.December))
               return false; 
            return true;
         }
      }

      private class Exchange : WesternImpl
      {
         private Exchange() { }
         public static readonly Exchange Singleton = new();

         public override string name() { return "Vienna stock exchange"; }
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
                // Whit Monay
                || (dd == em + 49)
                // Labour Day
                || (d == 1 && m == Month.May)
                // National Holiday since 1967
                || (d == 26 && m == Month.October && y >= 1967)
                // National Holiday 1919-1934
                || (d == 12 && m == Month.November && y >= 1919 && y <= 1934)
                // Christmas' Eve
                || (d == 24 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // St. Stephen
                || (d == 26 && m == Month.December)
                // Exchange Holiday
                || (d == 31 && m == Month.December))
               return false;
            return true;
         }
      }
   }
}
