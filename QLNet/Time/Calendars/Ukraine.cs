/*
 Copyright (C) 2008-2015 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://qlnet.sourceforge.net/License.html>.
  
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
   //! Ukrainian calendars
   /*! Holidays for the Ukrainian stock exchange
       (data from <http://www.ukrse.kiev.ua/eng/>):
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Orthodox Christmas, January 7th</li>
       <li>International Women's Day, March 8th</li>
       <li>Easter Monday</li>
       <li>Holy Trinity Day, 50 days after Easter</li>
       <li>International Workers' Solidarity Days, May 1st and 2nd</li>
       <li>Victory Day, May 9th</li>
       <li>Constitution Day, June 28th</li>
       <li>Independence Day, August 24th</li>
       <li>Defender's Day, October 14th (since 2015)</li>
       </ul>
       Holidays falling on a Saturday or Sunday are moved to the
       following Monday.

       \ingroup calendars
   */
   public class Ukraine : Calendar
   {
      public Ukraine(Market m = Market.USE ) 
      {
         // all calendar instances on the same market share the same implementation instance
         switch ( m )
         {
            case Market.USE:
               calendar_ = Impl.Singleton;
               break;
            default:
               throw new Exception( "unknown market" );
         }
      }

      public enum Market { USE    //!< Ukrainian stock exchange
      };

      class Impl : Calendar.OrthodoxImpl
      {
         public static readonly Impl Singleton = new Impl();
         private Impl() { }

         public override string name() { return "Ukrainian stock exchange"; }
         public override bool isBusinessDay( Date date )
         {
            DayOfWeek w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            Month m = (Month)date.Month;
            int y = date.Year;
            int em = easterMonday( y );

            if ( isWeekend( w )
               // New Year's Day (possibly moved to Monday)
               || ( ( d == 1 || ( ( d == 2 || d == 3 ) && w == DayOfWeek.Monday ) )
                   && m == Month.January )
               // Orthodox Christmas
               || ( ( d == 7 || ( ( d == 8 || d == 9 ) && w == DayOfWeek.Monday ) )
                   && m == Month.January )
               // Women's Day
               || ( ( d == 8 || ( ( d == 9 || d == 10 ) && w == DayOfWeek.Monday ) )
                   && m == Month.March )
               // Orthodox Easter Monday
               || ( dd == em )
               // Holy Trinity Day
               || ( dd == em + 49 )
               // Workers' Solidarity Days
               || ( ( d == 1 || d == 2 || ( d == 3 && w == DayOfWeek.Monday ) ) && m == Month.May )
               // Victory Day
               || ( ( d == 9 || ( ( d == 10 || d == 11 ) && w == DayOfWeek.Monday ) ) && m == Month.May )
               // Constitution Day
               || ( d == 28 && m == Month.June )
               // Independence Day
               || ( d == 24 && m == Month.August )
               // Defender's Day (since 2015)
               || ( d == 14 && m == Month.October && y >= 2015 ) )
               return false;
            return true;
         }
      };
   }
}
