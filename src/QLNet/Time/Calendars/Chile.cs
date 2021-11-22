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
   /// Chilean calendars
   /// </summary>
   /// <remarks>
   /// Holidays for the Santiago Stock Exchange
   /// (data from <https://en.wikipedia.org/wiki/Public_holidays_in_Chile>):
   /// Saturdays
   /// Sundays
   /// New Year's Day, January 1st
   /// January 2nd, when falling on a Monday (since 2017)
   /// Good Friday
   /// Easter Saturday
   /// Labour Day, May 1st
   /// Navy Day, May 21st
   /// Day of Aboriginal People, June 21st (since 2021)
   /// Saint Peter and Saint Paul, June 29th (moved to the nearest Monday if it falls on a weekday)
   /// Our Lady of Mount Carmel, July 16th
   /// Assumption Day, August 15th
   /// Independence Day, September 18th (also the 17th if the latter falls on a Monday or Friday)
   /// Army Day, September 19th (also the 20th if the latter falls on a Friday)
   /// Discovery of Two Worlds, October 12th (moved to the nearest Monday if it falls on a weekday)
   /// Reformation Day, October 31st (since 2008; moved to the preceding Friday if it falls on a Tuesday,
   /// or to the following Friday if it falls on a Wednesday)
   /// All Saints' Day, November 1st
   /// Immaculate Conception, December 8th
   /// Christmas Day, December 25th
   /// </remarks>

   public class Chile : Calendar
   {
      public enum Market
      {
         SSE // Santiago Stock Exchange
      };

      public Chile() : this(Market.SSE)
      {
      }

      public Chile(Market m)
      {
         calendar_ = m switch
         {
            Market.SSE => Settlement.Singleton,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }

      private class Settlement : WesternImpl
      {
         public static readonly Settlement Singleton = new Settlement();
         private Settlement() {}
         public override string name() { return "Santiago Stock Exchange"; }

         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            var d = date.Day;
            var m = (Month)date.Month;
            var y = date.Year;
            var dd = date.DayOfYear;
            var em = easterMonday(y);

            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                || (d == 2 && m == Month.January && w == DayOfWeek.Monday && y > 2016)
                // Good Friday
                || (dd == em - 3)
                // Easter Saturday
                || (dd == em - 2)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Navy Day
                || (d == 21 && m == Month.May)
                // Day of Aboriginal People
                || (d == 21 && m == Month.June && y >= 2021)
                // St. Peter and St. Paul
                || (d >= 26 && d <= 29 && m == Month.June && w == DayOfWeek.Monday)
                || (d == 2 && m == Month.July && w == DayOfWeek.Monday)
                // Our Lady of Mount Carmel
                || (d == 16 && m == Month.July)
                // Assumption Day
                || (d == 15 && m == Month.August)
                // Independence Day
                || (d == 17 && m == Month.September && ((w == DayOfWeek.Monday && y >= 2007) || (w == DayOfWeek.Friday && y > 2016)))
                || (d == 18 && m == Month.September)
                // Army Day
                || (d == 19 && m == Month.September)
                || (d == 20 && m == Month.September && w == DayOfWeek.Friday && y >= 2007)
                // Discovery of Two Worlds
                || (d >= 9 && d <= 12 && m == Month.October && w == DayOfWeek.Monday)
                || (d == 15 && m == Month.October && w == DayOfWeek.Monday)
                // Reformation Day
                || (((d == 27 && m == Month.October && w == DayOfWeek.Friday)
                   || (d == 31 && m == Month.October && w != DayOfWeek.Tuesday && w != DayOfWeek.Wednesday)
                   || (d == 2 && m == Month.November && w == DayOfWeek.Friday)) && y >= 2008)
                // All Saints' Day
                || (d == 1 && m == Month.November)
                // Immaculate Conception
                || (d == 8 && m == Month.December)
                // Christmas Day
                || (d == 25 && m == Month.December)
                )
               return false;

            return true;
         }
      }
   }
}
