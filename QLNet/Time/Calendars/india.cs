/*
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
  
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
using System.Text;

namespace QLNet {
    //! Indian calendars
   /*! Holidays for the National Stock Exchange
       (data from <http://www.nse-india.com/>):
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>Republic Day, January 26th</li>
       <li>Good Friday</li>
       <li>Ambedkar Jayanti, April 14th</li>
       <li>May Day, May 1st</li> 
       <li>Independence Day, August 15th</li>
       <li>Gandhi Jayanti, October 2nd</li>
       <li>Christmas, December 25th</li>
       </ul>

       Other holidays for which no rule is given 
       (data available for 2005-2013 only:)
       <ul>
       <li>Bakri Id</li>
       <li>Moharram</li>
       <li>Mahashivratri</li>
       <li>Holi</li>
       <li>Ram Navami</li>
       <li>Mahavir Jayanti</li>
       <li>Id-E-Milad</li>
       <li>Maharashtra Day</li>
       <li>Buddha Pournima</li>
       <li>Ganesh Chaturthi</li>
       <li>Dasara</li>
       <li>Laxmi Puja</li>
       <li>Bhaubeej</li>
       <li>Ramzan Id</li>
       <li>Guru Nanak Jayanti</li>
       </ul>

       \ingroup calendars
   */
   public class India : Calendar {
        public India() : base(Impl.Singleton) { }

        class Impl : Calendar.WesternImpl {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }
         
            public override string name() { return "National Stock Exchange of India"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);

                if (isWeekend(w)
                    // Republic Day
                    || (d == 26 && m == Month.January)
                    // Good Friday
                    || (dd == em - 3)
                    // Ambedkar Jayanti
                    || (d == 14 && m == Month.April)
                    // May Day
                    || (d == 1 && m == Month.May) 
                    // Independence Day
                    || (d == 15 && m == Month.August)
                    // Gandhi Jayanti
                    || (d == 2 && m == Month.October)
                    // Christmas
                    || (d == 25 && m == Month.December)
                    )
                    return false;

                if (y == 2005) {
                    // Moharram, Holi, Maharashtra Day, and Ramzan Id fall
                    // on Saturday or Sunday in 2005
                    if (// Bakri Id
                        (d == 21 && m == Month.January)
                        // Ganesh Chaturthi
                        || (d == 7 && m == Month.September)
                        // Dasara
                        || (d == 12 && m == Month.October)
                        // Laxmi Puja
                        || (d == 1 && m == Month.November)
                        // Bhaubeej
                        || (d == 3 && m == Month.November)
                        // Guru Nanak Jayanti
                        || (d == 15 && m == Month.November)
                        )
                        return false;
                }

                if (y == 2006) {
                    if (// Bakri Id
                        (d == 11 && m == Month.January)
                        // Moharram
                        || (d == 9 && m == Month.February)
                        // Holi
                        || (d == 15 && m == Month.March)
                        // Ram Navami
                        || (d == 6 && m == Month.April)
                        // Mahavir Jayanti
                        || (d == 11 && m == Month.April)
                        // Maharashtra Day
                        || (d == 1 && m == Month.May)
                        // Bhaubeej
                        || (d == 24 && m == Month.October)
                        // Ramzan Id
                        || (d == 25 && m == Month.October)
                        )
                        return false;
                }

                if (y == 2007) {
                    if (// Bakri Id
                        (d == 1 && m == Month.January)
                        // Moharram
                        || (d == 30 && m == Month.January)
                        // Mahashivratri
                        || (d == 16 && m == Month.February)
                        // Ram Navami
                        || (d == 27 && m == Month.March)
                        // Maharashtra Day
                        || (d == 1 && m == Month.May)
                        // Buddha Pournima
                        || (d == 2 && m == Month.May)
                        // Laxmi Puja
                        || (d == 9 && m == Month.November)
                        // Bakri Id (again)
                        || (d == 21 && m == Month.December)
                        )
                        return false;
                }

                if (y == 2008) {
                    if (// Mahashivratri
                        (d == 6 && m == Month.March)
                        // Id-E-Milad
                        || (d == 20 && m == Month.March)
                        // Mahavir Jayanti
                        || (d == 18 && m == Month.April)
                        // Maharashtra Day
                        || (d == 1 && m == Month.May)
                        // Buddha Pournima
                        || (d == 19 && m == Month.May)
                        // Ganesh Chaturthi
                        || (d == 3 && m == Month.September)
                        // Ramzan Id
                        || (d == 2 && m == Month.October)
                        // Dasara
                        || (d == 9 && m == Month.October)
                        // Laxmi Puja
                        || (d == 28 && m == Month.October)
                        // Bhau bhij
                        || (d == 30 && m == Month.October)
                        // Gurunanak Jayanti
                        || (d == 13 && m == Month.November)
                        // Bakri Id
                        || (d == 9 && m == Month.December)
                        )
                        return false;
                }

                if (y == 2009) {
                    if (// Moharram
                        (d == 8 && m == Month.January)
                        // Mahashivratri
                        || (d == 23 && m == Month.February)
                        // Id-E-Milad
                        || (d == 10 && m == Month.March)
                        // Holi
                        || (d == 11 && m == Month.March)
                        // Ram Navmi
                        || (d == 3 && m == Month.April)
                        // Mahavir Jayanti
                        || (d == 7 && m == Month.April)
                        // Maharashtra Day
                        || (d == 1 && m == Month.May)
                        // Ramzan Id
                        || (d == 21 && m == Month.September)
                        // Dasara
                        || (d == 28 && m == Month.September)
                        // Bhau Bhij
                        || (d == 19 && m == Month.October)
                        // Gurunanak Jayanti
                        || (d == 2 && m == Month.November)
                        // Moharram (again)
                        || (d == 28 && m == Month.December)
                        )
                        return false;
                }

               if (y == 2010)
                {
                   if (// New Year's Day
                       (d == 1 && m == Month.January)
                      // Mahashivratri
                       || (d == 12 && m == Month.February)
                      // Holi
                       || (d == 1 && m == Month.March)
                      // Ram Navmi
                       || (d == 24 && m == Month.March)
                      // Ramzan Id
                       || (d == 10 && m == Month.September)
                      // Laxmi Puja
                       || (d == 5 && m == Month.November)
                      // Bakri Id
                       || (d == 17 && m == Month.November)
                      // Moharram
                       || (d == 17 && m == Month.December)
                       )
                      return false;
                }

               if (y == 2011) {
 	  	             if (// Mahashivratri
                       (d == 2 && m == Month.March)
 	  	                 // Ram Navmi
                       || (d == 12 && m == Month.April)
 	  	                 // Ramzan Id
                       || (d == 31 && m == Month.August)
 	  	                 // Ganesh Chaturthi
                       || (d == 1 && m == Month.September)
 	  	                 // Dasara
                       || (d == 6 && m == Month.October)
 	  	                 // Laxmi Puja
                       || (d == 26 && m == Month.October)
 	  	                 // Diwali - Balipratipada
                       || (d == 27 && m == Month.October)
 	  	                 // Bakri Id
                       || (d == 7 && m == Month.November)
 	  	                 // Gurunanak Jayanti
                       || (d == 10 && m == Month.November)
 	  	                 // Moharram
                       || (d == 6 && m == Month.December)
 	  	                 )
 	  	                 return false;
 	  	         }

               if (y == 2012) {
                  if (// Mahashivratri
                      (d == 20 && m == Month.February)
                      // Holi
                      || (d == 8 && m == Month.March)
                      // Mahavir Jayanti
                      || (d == 5 && m == Month.April)
                      // Ramzan Id
                      || (d == 20 && m == Month.August)
                      // Ganesh Chaturthi
                      || (d == 19 && m == Month.September)
                      // Dasara
                      || (d == 24 && m == Month.October)
                      // Diwali - Balipratipada
                      || (d == 14 && m == Month.November)
                      // Gurunanak Jayanti
                      || (d == 28 && m == Month.November)
                     )
                     return false;
               }

               if (y == 2013) {
                  if (// Holi
                      (d == 27 && m == Month.March)
                      // Ram Navmi
                      || (d == 19 && m == Month.April)
                      // Mahavir Jayanti
                      || (d == 24 && m == Month.April)
                      // Ramzan Id
                      || (d == 9 && m == Month.August)
                      // Ganesh Chaturthi
                      || (d == 9 && m == Month.September)
                      // Bakri Id
                      || (d == 16 && m == Month.October)
                      // Diwali - Balipratipada
                      || (d == 4 && m == Month.November)
                      // Moharram
                      || (d == 14 && m == Month.November)
                  )
                  return false;
               } 
                return true;
            }
        }
    }
}




    