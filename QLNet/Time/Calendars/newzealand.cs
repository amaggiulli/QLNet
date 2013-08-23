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
    //! New Zealand calendar
    /*! Holidays:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday or
            Tuesday)</li>
        <li>Day after New Year's Day, January 2st (possibly moved to
            Monday or Tuesday)</li>
        <li>Anniversary Day, Monday nearest January 22nd</li>
        <li>Waitangi Day. February 6th</li>
        <li>Good Friday</li>
        <li>Easter Monday</li>
        <li>ANZAC Day. April 25th</li>
        <li>Queen's Birthday, first Monday in June</li>
        <li>Labour Day, fourth Monday in October</li>
        <li>Christmas, December 25th (possibly moved to Monday or Tuesday)</li>
        <li>Boxing Day, December 26th (possibly moved to Monday or
            Tuesday)</li>
        </ul>
        \note The holiday rules for New Zealand were documented by
              David Gilbert for IDB (http://www.jrefinery.com/ibd/)

        \ingroup calendars
    */
    public class NewZealand : Calendar {
        public NewZealand() : base(Impl.Singleton) { }

        class Impl : Calendar.WesternImpl {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }
          
            public override string name() { return "New Zealand"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);
                if (isWeekend(w)
                    // New Year's Day (possibly moved to Monday or Tuesday)
                    || ((d == 1 || (d == 3 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) &&
                        m == Month.January)
                    // Day after New Year's Day (possibly moved to Mon or Tuesday)
                    || ((d == 2 || (d == 4 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) &&
                        m == Month.January)
                    // Anniversary Day, Monday nearest January 22nd
                    || ((d >= 19 && d <= 25) && w == DayOfWeek.Monday && m == Month.January)
                    // Waitangi Day. February 6th
                    || (d == 6 && m == Month.February)
                    // Good Friday
                    || (dd == em-3)
                    // Easter Monday
                    || (dd == em)
                    // ANZAC Day. April 25th
                    || (d == 25 && m == Month.April)
                    // Queen's Birthday, first Monday in June
                    || (d <= 7 && w == DayOfWeek.Monday && m == Month.June)
                    // Labour Day, fourth Monday in October
                    || ((d >= 22 && d <= 28) && w == DayOfWeek.Monday && m == Month.October)
                    // Christmas, December 25th (possibly Monday or Tuesday)
                    || ((d == 25 || (d == 27 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                        && m == Month.December)
                    // Boxing Day, December 26th (possibly Monday or Tuesday)
                    || ((d == 26 || (d == 28 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday)))
                        && m == Month.December))
                    return false;
                return true;
            }
        }
    }
}