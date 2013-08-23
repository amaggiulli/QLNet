/*
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
using System.Text;

namespace QLNet {
    //! Italian calendars
    /*! Public holidays:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st</li>
        <li>Epiphany, January 6th</li>
        <li>Easter Monday</li>
        <li>Liberation Day, April 25th</li>
        <li>Labour Day, May 1st</li>
        <li>Republic Day, June 2nd (since 2000)</li>
        <li>Assumption, August 15th</li>
        <li>All Saint's Day, November 1st</li>
        <li>Immaculate Conception Day, December 8th</li>
        <li>Christmas Day, December 25th</li>
        <li>St. Stephen's Day, December 26th</li>
        </ul>

        Holidays for the stock exchange (data from http://www.borsaitalia.it):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st</li>
        <li>Good Friday</li>
        <li>Easter Monday</li>
        <li>Labour Day, May 1st</li>
        <li>Assumption, August 15th</li>
        <li>Christmas' Eve, December 24th</li>
        <li>Christmas, December 25th</li>
        <li>St. Stephen, December 26th</li>
        <li>New Year's Eve, December 31st</li>
        </ul>

        \ingroup calendars

        \test the correctness of the returned results is tested against a
              list of known holidays.
    */
   public class Italy : Calendar {
       //! Italian calendars
        public enum Market {
            Settlement,     //!< generic settlement calendar
            Exchange        //!< Milan stock-exchange calendar
        };

        public Italy() : this(Market.Settlement) { }
        public Italy(Market m)
            : base() {
            // all calendar instances on the same market share the same
            // implementation instance
            switch (m) {
                case Market.Settlement:
                    calendar_ = Settlement.Singleton;
                    break;
                case Market.Exchange:
                    calendar_ = Exchange.Singleton;
                    break;
                default:
                    throw new ArgumentException("Unknown market: " + m); ;
            }
        }


        class Settlement : Calendar.WesternImpl {
            public static readonly Settlement Singleton = new Settlement();
            private Settlement() { }
        
            public override string name() { return "Italian settlement"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);

                if (isWeekend(w)
                    // New Year's Day
                    || (d == 1 && m == Month.January)
                    // Epiphany
                    || (d == 6 && m == Month.January)
                    // Easter Monday
                    || (dd == em)
                    // Liberation Day
                    || (d == 25 && m == Month.April)
                    // Labour Day
                    || (d == 1 && m == Month.May)
                    // Republic Day
                    || (d == 2 && m == Month.June && y >= 2000)
                    // Assumption
                    || (d == 15 && m == Month.August)
                    // All Saints' Day
                    || (d == 1 && m == Month.November)
                    // Immaculate Conception
                    || (d == 8 && m == Month.December)
                    // Christmas
                    || (d == 25 && m == Month.December)
                    // St. Stephen
                    || (d == 26 && m == Month.December)
                    // December 31st, 1999 only
                    || (d == 31 && m == Month.December && y == 1999))
                    return false;
                return true;
            }
        }
        
        class Exchange : Calendar.WesternImpl {
            public static readonly Exchange Singleton = new Exchange();
            private Exchange() { }
       
            public override string name() { return "Milan stock exchange"; }
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
                    || (dd == em-3)
                    // Easter Monday
                    || (dd == em)
                    // Labour Day
                    || (d == 1 && m == Month.May)
                    // Assumption
                    || (d == 15 && m == Month.August)
                    // Christmas' Eve
                    || (d == 24 && m == Month.December)
                    // Christmas
                    || (d == 25 && m == Month.December)
                    // St. Stephen
                    || (d == 26 && m == Month.December)
                    // New Year's Eve
                    || (d == 31 && m == Month.December))
                    return false;
                return true;
            }
        };
    };

}
