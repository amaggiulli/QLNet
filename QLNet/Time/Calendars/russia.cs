/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
   //! Russian calendar
   /*! Public holidays (see <http://www.cbr.ru/eng/>:):
   <ul>
   <li>Saturdays</li>
   <li>Sundays</li>
   <li>New Year holidays and Christmas, January 1st to 8th</li>
   <li>Defender of the Fatherland Day, February 23rd (possibly
   moved to Monday)</li>
   <li>International Women's Day, March 8th (possibly moved to
   Monday)</li>
   <li>Labour Day, May 1st (possibly moved to Monday)</li>
   <li>Victory Day, May 9th (possibly moved to Monday)</li>
   <li>Russia Day, June 12th (possibly moved to Monday)</li>
   <li>Unity Day, November 4th (possibly moved to Monday)</li>
   </ul>
 	
   Holidays for the Moscow Exchange (MOEX) taken from
   <http://moex.com/s726> and related pages.  These holidays are
   <em>not</em> consistent year-to-year, may or may not correlate
   to public holidays, and are only available for dates since the
   introduction of the MOEX 'brand' (a merger of the stock and
   futures markets).
     
   \ingroup calendars
  */
   public class Russia : Calendar
   {
      public enum Market
      {
         Settlement,     //!< generic settlement calendar
         MOEX            //!< Moscow Exchange calendar
      }

      public Russia() : this( Market.Settlement ) { }

      public Russia( Market m )
         : base()
      {
         switch (m) 
         {
            case Market.Settlement:
               calendar_ = SettlementImpl.Singleton;
               break;
            case Market.MOEX:
               calendar_ = ExchangeImpl.Singleton;
               break;
            default:
               throw new ArgumentException("Unknown market: " + m); 
         }
      }

      class SettlementImpl : Calendar.OrthodoxImpl
      {
         public static readonly SettlementImpl Singleton = new SettlementImpl();
         private SettlementImpl() { }

         public override string name() { return "Russian settlement"; }
         public override bool isBusinessDay(Date date)
         {
            DayOfWeek w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            Month m = (Month)date.Month;
            int y = date.Year;
            int em = easterMonday(y);

            if (isWeekend(w)
 	            // New Year's holidays
 	            || (d >= 1 && d <= 8 && m == Month.January)
 	            // Defender of the Fatherland Day (possibly moved to Monday)
 	            || ((d == 23 || ((d == 24 || d == 25) && w == DayOfWeek.Monday)) &&
 	            m == Month.February)
 	            // International Women's Day (possibly moved to Monday)
 	            || ((d == 8 || ((d == 9 || d == 10) && w == DayOfWeek.Monday)) &&
 	            m == Month.March)
 	            // Labour Day (possibly moved to Monday)
 	            || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) &&
 	            m == Month.May)
 	            // Victory Day (possibly moved to Monday)
 	            || ((d == 9 || ((d == 10 || d == 11) && w == DayOfWeek.Monday)) &&
 	            m == Month.May)
 	            // Russia Day (possibly moved to Monday)
 	            || ((d == 12 || ((d == 13 || d == 14) && w == DayOfWeek.Monday)) &&
 	            m == Month.June)
 	            // Unity Day (possibly moved to Monday)
 	            || ((d == 4 || ((d == 5 || d == 6) && w == DayOfWeek.Monday)) &&
 	            m == Month.November))
 	            return false;
 	            
            return true;
         }
      }

      class ExchangeImpl : Calendar.OrthodoxImpl
      {
         public static readonly ExchangeImpl Singleton = new ExchangeImpl();
         private ExchangeImpl() { }

         private bool isWorkingWeekend( int d, Month month, int year )
         {
            switch ( year )
            {
               case 2012:
                  switch ( month )
                  {
                     case Month.March: return d == 11;
                     case Month.April: return d == 28;
                     case Month.May: return d == 5 || d == 12;
                     case Month.June: return d == 9;
                     default: return false;
                  }
               default:
                  return false;
            }
         }
         private bool isExtraHoliday(int d, Month month, int year) 
         {
            switch (year) 
            {
              case 2012:
                switch (month) 
                {
                  case Month.January: return d == 2;
                  case Month.March:   return d == 9;
                  case Month.April:   return d == 30;
                  case Month.June:    return d == 12;
                  default:      return false;
                }
              case 2013:
                switch (month) 
                {
                  case Month.January: return d == 1 || d == 2 || d == 3
                                    || d == 4 || d == 7;
                  default:      return false;
                }
              case 2014:
                switch (month) 
                {
                  case Month.January: return d == 1 || d == 2 || d == 3 || d == 7;
                  default:      return false;
                }
              case 2015:
                switch (month) 
                {
                  case Month.January: return d == 1 || d == 2 || d == 7;
                  case Month.May:     return d == 4;
                  default:      return false;
                }
              default:
                return false;
            }
        }


         public override string name() { return "Moscow exchange"; }
         public override bool isBusinessDay( Date date )
         {

            DayOfWeek w = date.DayOfWeek;
            int d = date.Day ;
            Month m = (Month)date.Month;
            int y = date.Year;

            // the exchange was formally established in 2011, so data are only
            // available from 2012 to present
            if ( y < 2012 )
               Utils.QL_FAIL( "MOEX calendar for the year " + y + " does not exist." );

            if ( isWorkingWeekend( d, m, y ) )
               return true;

            // Known holidays
            if ( isWeekend( w )
               // Defender of the Fatherland Day
                || ( d == 23 && m == Month.February )
               // International Women's Day (possibly moved to Monday)
                || ( ( d == 8 || ( ( d == 9 || d == 10 ) && w == DayOfWeek.Monday ) ) && m == Month.March )
               // Labour Day
                || ( d == 1 && m == Month.May )
               // Victory Day (possibly moved to Monday)
                || ( ( d == 9 || ( ( d == 10 || d == 11 ) && w == DayOfWeek.Monday ) ) && m == Month.May )
               // Russia Day
                || ( d == 12 && m == Month.June )
               // Unity Day (possibly moved to Monday)
                || ( ( d == 4 || ( ( d == 5 || d == 6 ) && w == DayOfWeek.Monday ) )
                    && m == Month.November )
               // New Years Eve
                || ( d == 31 && m == Month.December ) )
               return false;

            if ( isExtraHoliday( d, m, y ) )
               return false;

            return true;
         }
      }
   }
}
