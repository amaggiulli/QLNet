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
    //! %Indonesian calendars
   /*! Holidays for the Indonesia stock exchange
       (data from <http://www.idx.co.id/>):
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Good Friday</li>
       <li>Ascension of Jesus Christ</li>
       <li>Independence Day, August 17th</li>
       <li>Christmas, December 25th</li>
       </ul>

       Other holidays for which no rule is given
       (data available for 2005-2013 only:)
       <ul>
       <li>Idul Adha</li>
       <li>Ied Adha</li>
       <li>Imlek</li>
       <li>Moslem's New Year Day</li>
       <li>Chinese New Year</li>
       <li>Nyepi (Saka's New Year)</li>
       <li>Birthday of Prophet Muhammad SAW</li>
       <li>Waisak</li>
       <li>Ascension of Prophet Muhammad SAW</li>
       <li>Idul Fitri</li>
       <li>Ied Fitri</li>
       <li>Other national leaves</li>
       </ul>
       \ingroup calendars
   */
    public class Indonesia : Calendar {
        public enum Market {
           BEJ,  //!< Jakarta stock exchange (merged into IDX)
           JSX,  //!< Jakarta stock exchange (merged into IDX)
           IDX   //!< Indonesia stock exchange
        };

        public Indonesia() : this(Market.IDX) { }
        public Indonesia(Market m)
            : base() {
            // all calendar instances on the same market share the same
            // implementation instance
            switch (m) {
                case Market.BEJ:
                case Market.JSX:
                case Market.IDX:
                   calendar_ = BEJ.Singleton;
                    break;
                default:
                    throw new ArgumentException("Unknown market: " + m); ;
            }
        }

        class BEJ : Calendar.WesternImpl {
            public static readonly BEJ Singleton = new BEJ();
            private BEJ() { }
        
            public override string name() { return "Jakarta stock exchange"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);

                if (isWeekend(w)
                    // New Year's Day
                    || (d == 1 && m == Month.January)
                            // Good Friday
                    || (dd == em - 3)
                            // Ascension Thursday
                    || (dd == em + 38)
                            // Independence Day
                    || (d == 17 && m == Month.August)
                            // Christmas
                    || (d == 25 && m == Month.December)
                    )
                    return false;

                if (y == 2005) {
                    if (// Idul Adha
                        (d == 21 && m == Month.January)
                        // Imlek
                        || (d == 9 && m == Month.February)
                        // Moslem's New Year Day
                        || (d == 10 && m == Month.February)
                        // Nyepi
                        || (d == 11 && m == Month.March)
                        // Birthday of Prophet Muhammad SAW
                        || (d == 22 && m == Month.April)
                        // Waisak
                        || (d == 24 && m == Month.May)
                        // Ascension of Prophet Muhammad SAW
                        || (d == 2 && m == Month.September)
                        // Idul Fitri
                        || ((d == 3 || d == 4) && m == Month.November)
                        // National leaves
                        || ((d == 2 || d == 7 || d == 8) && m == Month.November)
                        || (d == 26 && m == Month.December)
                        )
                        return false;
                }

                if (y == 2006) {
                    if (// Idul Adha
                        (d == 10 && m == Month.January)
                        // Moslem's New Year Day
                        || (d == 31 && m == Month.January)
                        // Nyepi
                        || (d == 30 && m == Month.March)
                        // Birthday of Prophet Muhammad SAW
                        || (d == 10 && m == Month.April)
                        // Ascension of Prophet Muhammad SAW
                        || (d == 21 && m == Month.August)
                        // Idul Fitri
                        || ((d == 24 || d == 25) && m == Month.October)
                        // National leaves
                        || ((d == 23 || d == 26 || d == 27) && m == Month.October)
                        )
                        return false;
                }

                if (y == 2007) {
                    if (// Nyepi
                        (d == 19 && m == Month.March)
                        // Waisak
                        || (d == 1 && m == Month.June)
                        // Ied Adha
                        || (d == 20 && m == Month.December)
                        // National leaves
                        || (d == 18 && m == Month.May)
                        || ((d == 12 || d == 15 || d == 16) && m == Month.October)
                        || ((d == 21 || d == 24) && m == Month.October)
                        )
                        return false;
                }

                if (y == 2008) {
                    if (// Islamic New Year
                        ((d == 10 || d == 11) && m == Month.January)
                        // Chinese New Year
                        || ((d == 7 || d == 8) && m == Month.February)
                        // Saka's New Year
                        || (d == 7 && m == Month.March)
                        // Birthday of the prophet Muhammad SAW
                        || (d == 20 && m == Month.March)
                        // Vesak Day
                        || (d == 20 && m == Month.May)
                        // Isra' Mi'raj of the prophet Muhammad SAW
                        || (d == 30 && m == Month.July)
                        // National leave
                        || (d == 18 && m == Month.August)
                        // Ied Fitr
                        || (d == 30 && m == Month.September)
                        || ((d == 1 || d == 2 || d == 3) && m == Month.October)
                        // Ied Adha
                        || (d == 8 && m == Month.December)
                        // Islamic New Year
                        || (d == 29 && m == Month.December)
                        // New Year's Eve
                        || (d == 31 && m == Month.December)
                        )
                        return false;
                }

                if (y == 2009) {
                    if (// Public holiday
                        (d == 2 && m == Month.January)
                        // Chinese New Year
                        || (d == 26 && m == Month.January)
                        // Birthday of the prophet Muhammad SAW
                        || (d == 9 && m == Month.March)
                        // Saka's New Year
                        || (d == 26 && m == Month.March)
                        // National leave
                        || (d == 9 && m == Month.April)
                        // Isra' Mi'raj of the prophet Muhammad SAW
                        || (d == 20 && m == Month.July)
                        // Ied Fitr
                        || (d >= 18 && d <= 23 && m == Month.September)
                        // Ied Adha
                        || (d == 27 && m == Month.November)
                        // Islamic New Year
                        || (d == 18 && m == Month.December)
                        // Public Holiday
                        || (d == 24 && m == Month.December)
                        // Trading holiday
                        || (d == 31 && m == Month.December)
                        )
                        return false;
                }

                if (y == 2010)
                {
                   if (// Birthday of the prophet Muhammad SAW
                       (d == 26 && m == Month.February)
                       // Saka's New Year
                       || (d == 16 && m == Month.March)
                       // Birth of Buddha
                       || (d == 28 && m == Month.May)
                       // Ied Fitr
                       || (d >= 8 && d <= 14 && m == Month.September)
                       // Ied Adha
                       || (d == 17 && m == Month.November)
                       // Islamic New Year
                       || (d == 7 && m == Month.December)
                       // Public Holiday
                       || (d == 24 && m == Month.December)
                       // Trading holiday
                       || (d == 31 && m == Month.December)
                       )
                      return false;
                }

                if (y == 2011) {
 	  	             if (// Chinese New Year
                          (d == 3 && m == Month.February)
 	  	                 // Birthday of the prophet Muhammad SAW
                       || (d == 15 && m == Month.February)
 	  	                 // Birth of Buddha
                       || (d == 17 && m == Month.May)
 	  	                 // Isra' Mi'raj of the prophet Muhammad SAW
                       || (d == 29 && m == Month.June)
 	  	                 // Ied Fitr
                       || (d >= 29 && m == Month.August)
                       || (d <= 2 && m == Month.September)
 	  	                 // Public Holiday
                       || (d == 26 && m == Month.December)
 	  	                 )
                      return false;
                }

               if (y == 2012) {
                  if (// Chinese New Year
                      (d == 23 && m == Month.January)
                      // Saka New Year
                      || (d == 23 && m == Month.March)
                      // Ied ul-Fitr
                      || (d >= 20 && d <= 22 && m == Month.August)
                      // Eid ul-Adha
                      || (d == 26 && m == Month.October)
                      // Islamic New Year
                      || (d >= 15 && d <= 16 && m == Month.November)
                      // Public Holiday
                      || (d == 24 && m == Month.December)
                      // Trading Holiday
                      || (d == 31 && m == Month.December)
                     )
                     return false;
               }

               if (y == 2013) {
                  if (// Birthday of the prophet Muhammad SAW
                      (d == 24 && m == Month.January)
                      // Saka New Year
                      || (d == 12 && m == Month.March)
                      // Isra' Mi'raj of the prophet Muhammad SAW
                      || (d == 6 && m == Month.June)
                      // Ied ul-Fitr
                      || (d >= 5 && d <= 9 && m == Month.August)
                      // Eid ul-Adha
                      || (d >= 14 && d <= 15 && m == Month.October)
                      // Islamic New Year
                      || (d == 5 && m == Month.November)
                      // Public Holiday
                      || (d == 26 && m == Month.December)
                      // Trading Holiday
                      || (d == 31 && m == Month.December)
                     )
                     return false;
               } 
               return true;
            }
        }
    }
}
