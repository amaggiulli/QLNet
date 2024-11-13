/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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
   //  Brazilian calendar
   /*  Banking holidays:
       
       Saturdays
       Sundays
       New Year's Day, January 1st
       Tiradentes's Day, April 21th
       Labour Day, May 1st
       Independence Day, September 7th
       Nossa Sra. Aparecida Day, October 12th
       All Souls Day, November 2nd
       Republic Day, November 15th
       Christmas, December 25th
       Passion of Christ
       Carnival
       Corpus Christi

       Holidays for the Bovespa stock exchange
       
       Saturdays
       Sundays
       New Year's Day, January 1st
       Sao Paulo City Day, January 25th
       Tiradentes's Day, April 21th
       Labour Day, May 1st
       Revolution Day, July 9th (up to 2021 included)
       Independence Day, September 7th
       Nossa Sra. Aparecida Day, October 12th
       All Souls Day, November 2nd
       Republic Day, November 15th
       Black Consciousness Day, November 20th (since 2007, except 2022 and 2023)
       Christmas Eve, December 24th
       Christmas, December 25th
       Passion of Christ
       Carnival
       Corpus Christi
       the last business day of the year

   */
   public class Brazil : Calendar
   {
      // Brazilian calendars
      public enum Market
      {
         Settlement, // generic settlement calendar
         Exchange    // BOVESPA calendar
      }

      public Brazil() : this(Market.Settlement) { }
      public Brazil(Market market)
      {
         // all calendar instances on the same market share the same implementation instance
         switch (market)
         {
            case Market.Settlement:
               _impl = SettlementImpl.Singleton;
               break;
            case Market.Exchange:
               _impl  = ExchangeImpl.Singleton;
               break;
            default:
               Utils.QL_FAIL("unknown market");
               break;
         }
      }


      private class SettlementImpl : WesternImpl
      {
         private SettlementImpl() { }
         public static readonly SettlementImpl Singleton = new();
         public override string name() { return "Brazil"; }
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
                // Tiradentes Day
                || (d == 21 && m == Month.April)
                // Labor Day
                || (d == 1 && m == Month.May)
                // Independence Day
                || (d == 7 && m == Month.September)
                // Nossa Sra. Aparecida Day
                || (d == 12 && m == Month.October)
                // All Souls Day
                || (d == 2 && m == Month.November)
                // Republic Day
                || (d == 15 && m == Month.November)
                // Christmas
                || (d == 25 && m == Month.December)
                // Passion of Christ
                || (dd == em - 3)
                // Carnival
                || (dd == em - 49 || dd == em - 48)
                // Corpus Christi
                || (dd == em + 59)
               )
               return false;
            return true;
         }
      }

      private class ExchangeImpl : WesternImpl
      {
         private ExchangeImpl() { }
         public static readonly ExchangeImpl Singleton = new();
         public override string name() { return "BOVESPA"; }
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
                // Sao Paulo City Day
                || (d == 25 && m == Month.January)
                // Tiradentes Day
                || (d == 21 && m == Month.April)
                // Labor Day
                || (d == 1 && m == Month.May)
                // Revolution Day
                || (d == 9 && m == Month.July && y < 2022)
                // Independence Day
                || (d == 7 && m == Month.September)
                // Nossa Sra. Aparecida Day
                || (d == 12 && m == Month.October)
                // All Souls Day
                || (d == 2 && m == Month.November)
                // Republic Day
                || (d == 15 && m == Month.November)
                // Black Consciousness Day
                || (d == 20 && m == Month.November && y >= 2007 && y != 2022 && y != 2023)
                // Christmas Eve
                || (d == 24 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // Passion of Christ
                || (dd == em - 3)
                // Carnival
                || (dd == em - 49 || dd == em - 48)
                // Corpus Christi
                || (dd == em + 59)
                // last business day of the year
                || (m == Month.December && (d == 31 || (d >= 29 && w == DayOfWeek.Friday)))
               )
               return false;
            return true;
         }
      }

   }
}
