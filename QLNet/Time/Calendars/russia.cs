/*
 Copyright (C) 2008 Andrea Maggiulli

 This file is part of QLNet Project http://www.qlnet.org

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://trac2.assembla.com/QLNet/wiki/License>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
using System.Text;

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
 	
   \ingroup calendars
  */
   public class Russia : Calendar
   {
      public Russia() : base(Impl.Singleton) { }

      class Impl : Calendar.WesternImpl
      {
         public static readonly Impl Singleton = new Impl();
         private Impl() { }

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
      };
   };

}
