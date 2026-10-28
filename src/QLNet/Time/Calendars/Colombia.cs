/*
 Copyright (C) 2008-2024 Andrea Maggiulli (a.maggiulli@gmail.com)

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
   /// Colombian calendars
   /// </summary>
   /// <remarks>
   /// Holidays for the Colombian stock exchange (Bogota), based on ORE/QuantExt Colombia calendar.
   /// Law 2578 of 2026 adds the Feast of Our Lady of the Rosary of Chiquinquirá (July 9),
   /// observed per Law 51 of 1983 (Ley Emiliani) from 2026 onward.
   /// </remarks>
   public class Colombia : Calendar
   {
      public enum Market
      {
         CSE // Colombian stock exchange
      };

      public Colombia() : this(Market.CSE)
      {
      }

      public Colombia(Market m)
      {
         _impl = m switch
         {
            Market.CSE => CseImpl.Singleton,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }

      private class CseImpl : WesternImpl
      {
         public static readonly CseImpl Singleton = new();
         private CseImpl() { }
         public override string name() { return "Colombian stock exchange"; }

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
                || ((d == 1 || (d == 2 && w == DayOfWeek.Monday)) && m == Month.January)
                // Dia de los Reyes Magos
                || (d >= 6 && d <= 12 && w == DayOfWeek.Monday && m == Month.January)
                // St. Joseph's Day
                || (d >= 19 && d <= 25 && w == DayOfWeek.Monday && m == Month.March)
                // Maundy Thursday
                || (dd == em - 4)
                // Good Friday
                || (dd == em - 3)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Ascension Day
                || (dd == em + 42)
                // Corpus Christi
                || (dd == em + 63)
                // Sacred Heart
                || (dd == em + 70)
                // Saint Peter and Saint Paul
                || (((d >= 29 && m == Month.June) || (d <= 5 && m == Month.July)) && w == DayOfWeek.Monday)
                // Feast of Our Lady of the Rosary of Chiquinquirá (Law 2578, from 2026; Ley 51 observance)
                || isChiquinquiraObserved(date)
                // Declaration of Independence
                || (d == 20 && m == Month.July)
                // Battle of Boyaca
                || (d == 7 && m == Month.August)
                // Assumption
                || (d >= 15 && d <= 21 && w == DayOfWeek.Monday && m == Month.August)
                // Columbus Day
                || (d >= 12 && d <= 18 && w == DayOfWeek.Monday && m == Month.October)
                // All Saints' Day
                || (d >= 1 && d <= 7 && w == DayOfWeek.Monday && m == Month.November)
                // Independence of Cartagena
                || (d >= 12 && d <= 18 && w == DayOfWeek.Monday && m == Month.November)
                // Immaculate Conception
                || (d == 8 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December))
               return false;

            return true;
         }

         private static bool isChiquinquiraObserved(Date date)
         {
            if (date.Year < 2026 || (Month)date.Month != Month.July)
               return false;

            return date == chiquinquiraObservedDate(date.Year);
         }

         private static Date chiquinquiraObservedDate(int year)
         {
            var feast = new Date(9, Month.July, year);
            return Date.nextWeekday(feast, DayOfWeek.Monday);
         }
      }
   }
}
