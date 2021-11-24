//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;

namespace QLNet
{
   //! Romanian calendars
   /*! Public holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li> Day after New Year's Day, January 2nd</li>
       <li>Unification Day, January 24th</li>
       <li>Orthodox Easter (only Sunday and Monday)</li>
       <li>Labour Day, May 1st</li>
       <li>Pentecost with Monday (50th and 51st days after the
           Othodox Easter)</li>
       <li>St Marys Day, August 15th</li>
       <li>Feast of St Andrew, November 30th</li>
       <li>National Day, December 1st</li>
       <li>Christmas, December 25th</li>
       <li>2nd Day of Christmas, December 26th</li>
       </ul>

       Holidays for the Bucharest stock exchange
       (data from <http://www.bvb.ro/Marketplace/TradingCalendar/index.aspx>):
       all public holidays, plus a few one-off closing days (2014 only).      
   */
   public class Romania : Calendar
   {
      public enum Market
      {
         Public,     //!< Public holidays
         BVB         //!< Bucharest stock-exchange
      }

      public Romania() : this(Market.BVB) { }

      public Romania(Market m) : base()
      {
         calendar_ = m switch
         {
            Market.Public => PublicImpl.Singleton,
            Market.BVB => BVBImpl.Impl,
            _ => throw new ArgumentException("Unknown market: " + m)
         };
      }

      private class PublicImpl : Calendar.OrthodoxImpl
      {
         public static readonly PublicImpl Singleton = new PublicImpl();
         protected PublicImpl() { }
         public override string name() { return "Romania"; }

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
                // Day after New Year's Day
                || (d == 2 && m == Month.January)
                // Unification Day
                || (d == 24 && m == Month.January)
                // Orthodox Easter Monday
                || (dd == em)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Pentecost
                || (dd == em + 49)
                // Children's Day (since 2017)
                || (d == 1 && m == Month.June && y >= 2017)
                // St Marys Day
                || (d == 15 && m == Month.August)
                // Feast of St Andrew
                || (d == 30 && m == Month.November)
                // National Day
                || (d == 1 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // 2nd Day of Chritsmas
                || (d == 26 && m == Month.December))
               return false; 
            return true;
         }
      }

      private class BVBImpl : PublicImpl
      {
         public static readonly BVBImpl Impl = new BVBImpl();
         private BVBImpl() { }
         public override string name() { return "Romania"; }
         public override bool isBusinessDay(Date date)
         {
            if (!base.isBusinessDay(date))
               return false;
            var d = date.Day;
            var m = (Month)date.Month;
            var y = date.Year;
            if (// one-off closing days
               (d == 24 && m == Month.December && y == 2014) ||
               (d == 31 && m == Month.December && y == 2014)
            )
               return false;
            return true;
         }
      }
   }
}
