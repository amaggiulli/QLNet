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
    //! %Mexican calendars
    /*! Holidays for the Mexican stock exchange
        (data from <http://www.bmv.com.mx/>):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st</li>
        <li>Constitution Day, February 5th</li>
        <li>Birthday of Benito Juarez, March 21st</li>
        <li>Holy Thursday</li>
        <li>Good Friday</li>
        <li>Labour Day, May 1st</li>
        <li>National Day, September 16th</li>
        <li>Our Lady of Guadalupe, December 12th</li>
        <li>Christmas, December 25th</li>
        </ul>

        \ingroup calendars
    */
    public class Mexico : Calendar {
        public Mexico() : base(Impl.Singleton) { }

        class Impl : Calendar.WesternImpl {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }
        
            public override string name() { return "Mexican stock exchange"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);
                if (isWeekend(w)
                    // New Year's Day
                    || (d == 1 && m == Month.January)
                    // Constitution Day
                    || (d == 5 && m == Month.February)
                    // Birthday of Benito Juarez
                    || (d == 21 && m == Month.March)
                    // Holy Thursday
                    || (dd == em-4)
                    // Good Friday
                    || (dd == em-3)
                    // Labour Day
                    || (d == 1 && m == Month.May)
                    // National Day
                    || (d == 16 && m == Month.September)
                    // Our Lady of Guadalupe
                    || (d == 12 && m == Month.December)
                    // Christmas
                    || (d == 25 && m == Month.December))
                    return false;
                return true;
            }
        }
    }
}