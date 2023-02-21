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
   //! Cyprus calendar
   /*! Public holidays:
   <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day</li>
       <li>Epiphany</li>
       <li>Greek Independence Day</li>
       <li>Cyprus National Day</li>
       <li>Ash Monday / Clean Monday</li>
       <li>Good Friday</li>
       <li>Orthodox Easter (Sunday)</li>
       <li>Orthodox Easter (Monday)</li>
       <li>Orthodox Easter (Tuesday)</li>
       <li>Labour Day</li>
       <li>Holy Spirit Day</li>
       <li>Assumption Day</li>
       <li>Cyprus Independence Day</li>
       <li>Greek National Day</li>
       <li>Christmas Eve</li>
       <li>Christmas Day</li>
       <li>Boxing Day</li>
   </ul>
   Holidays for the Cyprus stock exchange
   All public holidays plus Catholic Good Friday, Catholic Easter Monday
   //https://www.cse.com.cy/en-GB/regulated-market/market-indices/other-information/%CE%9B%CE%B9%CF%83%CF%84%CE%B1-%CE%BC%CE%B5-%CE%B1%CF%81%CE%B3%CE%B9%CE%B5%CF%82-2014/
   */
   public class Cyprus : Calendar
   {
      public enum Market
      {
         Public,     //!< Public holidays
         CSE        //!< Cyprus stock-exchange
      }
      public Cyprus() : this(Market.CSE) { }

      public Cyprus(Market m) : base()
      {
         _impl = m switch
         {
            Market.Public => PublicImpl.Singleton,
            Market.CSE => CSEImpl.Impl,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }
      private class PublicImpl : OrthodoxImpl
      {
         public static readonly PublicImpl Singleton = new();
         protected PublicImpl() { }
         public override string name() { return "Cyprus"; }

         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.year();
            var em = easterMonday(y);
            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                // Epiphany
                || (d == 6 && m == Month.January)
                //Greek Independence Day
                || (d == 25 && m == Month.March)
                //Cyprus National Day
                || (d == 1 && m == Month.April)
                //Ash Monday / Clean Monday
                || (dd == em - 49 )
                //Good Friday
                || (dd == em-3)
                // Orthodox Easter Monday
                || (dd == em)
                // Orthodox Easter Tuesday
                || (dd == em + 1 )
                // Labour Day
                || (d == 1 && m == Month.May)
                // Holy Spirit Day
                || (dd == em + 49)
                // Assumption Day
                || (d == 15 && m == Month.August)
                // Cyprus Independence Day
                || (d == 1 && m == Month.October)
                // Greek National Day
                || (d == 28 && m == Month.October)
                // Christmas Eve
                || (d == 24 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // 2nd Day of Chritsmas
                || (d == 26 && m == Month.December))
               return false;
            return true;
         }
      }

      private class CSEImpl : WesternImpl
      {
         public static readonly CSEImpl Impl = new();
         private CSEImpl() { }
         public override string name() { return "Cyprus"; }
         public override bool isBusinessDay(Date date)
         {
            var publicImpl = new Cyprus(Market.Public);
            if (!publicImpl.isBusinessDay(date))
               return false;

            var dd = date.DayOfYear;
            var y = date.Year;
            var em = easterMonday(y);
            // Catholic //Good Friday
            if ((dd == em-3)
            // Catholic Easter Monday
            || (dd == em))
               return false;
            return true;
         }
      }
   }
}
