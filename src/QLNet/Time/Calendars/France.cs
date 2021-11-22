/*
 Copyright (C) 2008-2021 Andrea Maggiulli

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
   /// French calendars
   /// </summary>
   /// <remarks>
   /// 
   /// Public holidays:
   ///  
   ///  Saturdays
   ///  Sundays
   ///  New Year's Day, January 1st
   ///  Easter Monday
   ///  Labour Day, May 1st
   ///  Armistice 1945, May 8th
   ///  Ascension, May 10th
   ///  Pentecôte, May 21st
   ///  Fête nationale, July 14th
   ///  Assumption, August 15th
   ///  All Saint's Day, November 1st
   ///  Armistice 1918, November 11th
   ///  Christmas Day, December 25th
   ///  
   ///
   ///  Holidays for the stock exchange (data from https://www.stockmarketclock.com/exchanges/euronext-paris/market-holidays/):
   ///  
   ///  Saturdays
   ///  Sundays
   ///  New Year's Day, January 1st
   ///  Good Friday
   ///  Easter Monday
   ///  Labour Day, May 1st
   ///  Christmas Eve, December 24th
   ///  Christmas Day, December 25th
   ///  Boxing Day, December 26th
   ///  New Year's Eve, December 31st
   /// </remarks>
   public class France : Calendar
   {
      // French calendars
      public enum Market
      {
         Settlement, // generic settlement calendar
         Exchange // Paris stock-exchange calendar
      };

      public France() : this(Market.Settlement) { }

      public France(Market m) : base()
      {
         calendar_ = m switch
         {
            Market.Settlement => Settlement.Singleton,
            Market.Exchange => Exchange.Singleton,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }

      private class Settlement : Calendar.WesternImpl
      {
         public static readonly Settlement Singleton = new Settlement();
         private Settlement() { }
         public override string name() { return "French settlement"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);
            if (isWeekend(w)
                // Jour de l'An
                || (d == 1 && m == Month.January)
                // Lundi de Paques
                || (dd == em)
                // Fete du Travail
                || (d == 1 && m == Month.May)
                // Victoire 1945
                || (d == 8 && m == Month.May)
                // Ascension
                || (d == 10 && m == Month.May)
                // Pentecote
                || (d == 21 && m == Month.May)
                // Fete nationale
                || (d == 14 && m == Month.July)
                // Assomption
                || (d == 15 && m == Month.August)
                // Toussaint
                || (d == 1 && m == Month.November)
                // Armistice 1918
                || (d == 11 && m == Month.November)
                // Noel
                || (d == 25 && m == Month.December))
               return false; 
            return true;
         }
      }

      private class Exchange : Calendar.WesternImpl
      {
         public static readonly Exchange Singleton = new Exchange();
         private Exchange() { }
         public override string name() { return "Paris stock exchange"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);
            if (isWeekend(w)
                // Jour de l'An
                || (d == 1 && m == Month.January)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // Labor Day
                || (d == 1 && m == Month.May)
                // Christmas Eve
                || (d == 24 && m == Month.December)
                // Christmas Day
                || (d == 25 && m == Month.December)
                // Boxing Day
                || (d == 26 && m == Month.December)
                // New Year's Eve
                || (d == 31 && m == Month.December))
               return false; 
            return true;
         }
      }

   }
}
