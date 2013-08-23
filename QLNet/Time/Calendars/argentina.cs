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
    //! Argentinian calendars
    /*! Holidays for the Buenos Aires stock exchange
        (data from <http://www.merval.sba.com.ar/>):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st</li>
        <li>Holy Thursday</li>
        <li>Good Friday</li>
        <li>Labour Day, May 1st</li>
        <li>May Revolution, May 25th</li>
        <li>Death of General Manuel Belgrano, third Monday of June</li>
        <li>Independence Day, July 9th</li>
        <li>Death of General José de San Martín, third Monday of August</li>
        <li>Columbus Day, October 12th (moved to preceding Monday if
            on Tuesday or Wednesday and to following if on Thursday
            or Friday)</li>
        <li>Immaculate Conception, December 8th</li>
        <li>Christmas Eve, December 24th</li>
        <li>New Year's Eve, December 31th</li>
        </ul>

        \ingroup calendars
    */
    public class Argentina : Calendar {
        public Argentina() : base(Impl.Singleton) { }

        class Impl : Calendar.WesternImpl {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }
         
            public override string name() { return "Buenos Aires stock exchange"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);

                if (isWeekend(w)
                    // New Year's Day
                    || (d == 1 && m == Month.January)
                    // Holy Thursday
                    || (dd == em-4)
                    // Good Friday
                    || (dd == em-3)
                    // Labour Day
                    || (d == 1 && m == Month.May)
                    // May Revolution
                    || (d == 25 && m == Month.May)
                    // Death of General Manuel Belgrano
                    || (d >= 15 && d <= 21 && w == DayOfWeek.Monday && m == Month.June)
                    // Independence Day
                    || (d == 9 && m == Month.July)
                    // Death of General José de San Martín
                    || (d >= 15 && d <= 21 && w == DayOfWeek.Monday && m == Month.August)
                    // Columbus Day
                    || ((d == 10 || d == 11 || d == 12 || d == 15 || d == 16)
                        && w == DayOfWeek.Monday && m == Month.October)
                    // Immaculate Conception
                    || (d == 8 && m == Month.December)
                    // Christmas Eve
                    || (d == 24 && m == Month.December)
                    // New Year's Eve
                    || ((d == 31 || (d == 30 && w == DayOfWeek.Friday)) && m == Month.December))
                    return false;
                return true;
            }
        };
   };
}
