/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Andrea Maggiulli
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
    //! Hungarian calendar
    /*! Holidays:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>Easter Monday</li>
        <li>Whit(Pentecost) Monday </li>
        <li>New Year's Day, January 1st</li>
        <li>National Day, March 15th</li>
        <li>Labour Day, May 1st</li>
        <li>Constitution Day, August 20th</li>
        <li>Republic Day, October 23rd</li>
        <li>All Saints Day, November 1st</li>
        <li>Christmas, December 25th</li>
        <li>2nd Day of Christmas, December 26th</li>
        </ul>

        \ingroup calendars
    */
    public class Hungary : Calendar {
        public Hungary() : base(Impl.Singleton) { }

        class Impl : Calendar.WesternImpl {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }

            public override string name() { return "Hungary"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);
                if (isWeekend(w)
                    // Easter Monday
                    || (dd == em)
                    // Whit Monday
                    || (dd == em+49)
                    // New Year's Day
                    || (d == 1  && m == Month.January)
                    // National Day
                    || (d == 15  && m == Month.March)
                    // Labour Day
                    || (d == 1  && m == Month.May)
                    // Constitution Day
                    || (d == 20  && m == Month.August)
                    // Republic Day
                    || (d == 23  && m == Month.October)
                    // All Saints Day
                    || (d == 1  && m == Month.November)
                    // Christmas
                    || (d == 25 && m == Month.December)
                    // 2nd Day of Christmas
                    || (d == 26 && m == Month.December))
                    return false;
                return true;
            }
       }
    }
}