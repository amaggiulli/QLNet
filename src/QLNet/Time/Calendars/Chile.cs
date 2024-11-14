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
   /// Day of Aboriginal People, celebrated on the Winter Solstice day, except in 2021, when it was the day after.
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
         _impl = m switch
         {
            Market.SSE => Settlement.Singleton,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }

      private class Settlement : WesternImpl
      {
         public static readonly Settlement Singleton = new();
         private Settlement() {}
         public override string name() { return "Santiago Stock Exchange"; }

         // <summary>
         // Returns true if date is Aboriginal People Day
         // Array int[] aboriginalPeopleDay represents days of June
         // from 2021 to 2199 where Winter Solstice takes place.
         // </summary>
         private bool isAboriginalPeopleDay(int d, Month m, int y) 
         {
            int[] aboriginalPeopleDay = {
                   21, 21, 21, 20, 20, 21, 21, 20, 20,   // 2021-2029
               21, 21, 20, 20, 21, 21, 20, 20, 21, 21,   // 2030-2039
               20, 20, 21, 21, 20, 20, 21, 21, 20, 20,   // 2040-2049
               20, 21, 20, 20, 20, 21, 20, 20, 20, 21,   // 2050-2059
               20, 20, 20, 21, 20, 20, 20, 21, 20, 20,   // 2060-2069
               20, 21, 20, 20, 20, 21, 20, 20, 20, 20,   // 2070-2079
               20, 20, 20, 20, 20, 20, 20, 20, 20, 20,   // 2080-2089
               20, 20, 20, 20, 20, 20, 20, 20, 20, 20,   // 2090-2099
               21, 21, 21, 21, 21, 21, 21, 21, 20, 21,   // 2100-2109
               21, 21, 20, 21, 21, 21, 20, 21, 21, 21,   // 2110-2119
               20, 21, 21, 21, 20, 21, 21, 21, 20, 21,   // 2120-2129
               21, 21, 20, 21, 21, 21, 20, 20, 21, 21,   // 2130-2139
               20, 20, 21, 21, 20, 20, 21, 21, 20, 20,   // 2140-2149
               21, 21, 20, 20, 21, 21, 20, 20, 21, 21,   // 2150-2159
               20, 20, 21, 21, 20, 20, 21, 21, 20, 20,   // 2160-2169
               20, 21, 20, 20, 20, 21, 20, 20, 20, 21,   // 2170-2179
               20, 20, 20, 21, 20, 20, 20, 21, 20, 20,   // 2180-2189
               20, 21, 20, 20, 20, 21, 20, 20, 20, 20    // 2190-2199
            };

            return m == Month.June && y >= 2021 && d == aboriginalPeopleDay[y-2021];
         }

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
                || isAboriginalPeopleDay(d, Month.June, y)
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
