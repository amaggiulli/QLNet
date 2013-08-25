/*
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project http://qlnet.sourceforge.net/

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
using System.Collections.Generic;

namespace QLNet {
    //! Turkish calendar
   /*! Holidays for the Istanbul Stock Exchange:
       (data from <http://borsaistanbul.com/en/products-and-markets/official-holidays>):
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>National Sovereignty and Children’s Day, April 23rd</li>
       <li>Youth and Sports Day, May 19th</li>
       <li>Victory Day, August 30th</li>
       <li>Republic Day, October 29th</li>
       <li>Local Holidays (Kurban, Ramadan; 2004 to 2013 only) </li>
       </ul>

       \ingroup calendars
   */
   public class Turkey :  Calendar {
        public Turkey() : base(Impl.Singleton) { }

        class Impl : Calendar {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }

            public override string name() { return "Turkey"; }
            public override bool isWeekend(DayOfWeek w) {
                return w == DayOfWeek.Saturday || w == DayOfWeek.Sunday;
            }

            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;

                if (isWeekend(w)
                    // New Year's Day
                    || (d == 1 && m == Month.January)
                    // 23 nisan / National Holiday
                    || (d == 23 && m == Month.April)
                    // 19 may/ National Holiday
                    || (d == 19 && m == Month.May)
                    // 30 aug/ National Holiday
                    || (d == 30 && m == Month.August)
                    ///29 ekim  National Holiday
                    || (d == 29 && m == Month.October))
                    return false;

                // Local Holidays
                if (y == 2004) {
                    // Kurban
                    if ((m == Month.February && d <= 4)
                    // Ramadan
                        || (m == Month.November && d >= 14 && d <= 16))
                        return false;
                } else if (y == 2005) {
                    // Kurban
                    if ((m == Month.January && d >= 19 && d <= 21)
                    // Ramadan
                        || (m == Month.November && d >= 2 && d <= 5))
                        return false;
                } else if (y == 2006) {
                    // Kurban
                    if ((m == Month.January && d >= 10 && d <= 13)
                    // Ramadan
                        || (m == Month.October && d >= 23 && d <= 25)
                    // Kurban
                        || (m == Month.December && d == 31))
                        return false;
                } else if (y == 2007) {
                    // Kurban
                    if ((m == Month.January && d <= 3)
                    // Ramadan
                        || (m == Month.October && d >= 12 && d <= 14)
                    // Kurban
                        || (m == Month.December && d >= 20 && d <= 23))
                        return false;
                } else if (y == 2008) {
                    // Ramadan
                    if ((m == Month.September && d == 30)
                        || (m == Month.October && d <= 2)
                        // Kurban
                        || (m == Month.December && d >= 8 && d <= 11))
                        return false;
                }
                else if (y == 2009)
                {
                   // Ramadan
                   if ((m == Month.September && d >= 20 && d <= 22)
                      // Kurban
                       || (m == Month.November && d >= 27 && d <= 30))
                      return false;
                }
                else if (y == 2010)
                {
                   // Ramadan
                   if ((m == Month.September && d >= 9 && d <= 11)
                      // Kurban
                       || (m == Month.November && d >= 16 && d <= 19))
                      return false;
                }
                else if (y == 2011) 
                {
                   // not clear from borsainstanbul.com
                   if ((m == Month.October && d == 1)
                   || (m == Month.November && d >= 9 && d <= 13))
                     return false;
                } 
                else if (y == 2012) 
                {
                   // Ramadan
                   if ((m == Month.August && d >= 18 && d <= 21)
                   // Kurban
                   || (m == Month.October && d >= 24 && d <= 28))
                     return false;
                }
                else if (y == 2013)
                {
                   // Ramadan
                   if ((m == Month.August && d >= 7 && d <= 10)
                   // Kurban
                   || (m == Month.October && d >= 14 && d <= 18)
                   // additional holiday for Republic Day
                   || (m == Month.October && d == 28))
                      return false;
                }
                return true;
            }
        };
    }
}




