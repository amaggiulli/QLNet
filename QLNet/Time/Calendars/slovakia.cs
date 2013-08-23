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
    //! Slovak calendars
    /*! Holidays for the Bratislava stock exchange
        (data from <http://www.bsse.sk/>):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st</li>
        <li>Epiphany, January 6th</li>
        <li>Good Friday</li>
        <li>Easter Monday</li>
        <li>May Day, May 1st</li>
        <li>Liberation of the Republic, May 8th</li>
        <li>SS. Cyril and Methodius, July 5th</li>
        <li>Slovak National Uprising, August 29th</li>
        <li>Constitution of the Slovak Republic, September 1st</li>
        <li>Our Lady of the Seven Sorrows, September 15th</li>
        <li>All Saints Day, November 1st</li>
        <li>Freedom and Democracy of the Slovak Republic, November 17th</li>
        <li>Christmas Eve, December 24th</li>
        <li>Christmas, December 25th</li>
        <li>St. Stephen, December 26th</li>
        </ul>

        \ingroup calendars
    */
    public class Slovakia :  Calendar {
        public Slovakia() : base(Impl.Singleton) { }

        class Impl : Calendar.WesternImpl {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }

            public override string name() { return "Bratislava stock exchange"; }
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
                    // Good Friday
                    || (dd == em-3)
                    // Easter Monday
                    || (dd == em)
                    // May Day
                    || (d == 1 && m == Month.May)
                    // Liberation of the Republic
                    || (d == 8 && m == Month.May)
                    // SS. Cyril and Methodius
                    || (d == 5 && m == Month.July)
                    // Slovak National Uprising
                    || (d == 29 && m == Month.August)
                    // Constitution of the Slovak Republic
                    || (d == 1 && m == Month.September)
                    // Our Lady of the Seven Sorrows
                    || (d == 15 && m == Month.September)
                    // All Saints Day
                    || (d == 1 && m == Month.November)
                    // Freedom and Democracy of the Slovak Republic
                    || (d == 17 && m == Month.November)
                    // Christmas Eve
                    || (d == 24 && m == Month.December)
                    // Christmas
                    || (d == 25 && m == Month.December)
                    // St. Stephen
                    || (d == 26 && m == Month.December)
                    // unidentified closing days for stock exchange
                    || (d >= 24 && d <= 31 && m == Month.December && y == 2004)
                    || (d >= 24 && d <= 31 && m == Month.December && y == 2005))
                    return false;
                return true;
            }
        }
    }
}
