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
    //! Danish calendar
    /*! Holidays:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>Maunday Thursday</li>
        <li>Good Friday</li>
        <li>Easter Monday</li>
        <li>General Prayer Day, 25 days after Easter Monday</li>
        <li>Ascension</li>
        <li>Whit (Pentecost) Monday </li>
        <li>New Year's Day, January 1st</li>
        <li>Constitution Day, June 5th</li>
        <li>Christmas, December 25th</li>
        <li>Boxing Day, December 26th</li>
        </ul>

        \ingroup calendars
    */
    public class Denmark : Calendar {
        public Denmark() : base(Impl.Singleton) { }

        class Impl : Calendar.WesternImpl {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }
       
            public override string name() { return "Denmark"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);
                if (isWeekend(w)
                      // Maunday Thursday
                      || (dd == em - 4)
                      // Good Friday
                      || (dd == em - 3)
                      // Easter Monday
                      || (dd == em)
                      // General Prayer Day
                      || (dd == em + 25)
                      // Ascension
                      || (dd == em + 38)
                      // Whit Monday
                      || (dd == em + 49)
                      // New Year's Day
                      || (d == 1 && m == Month.January)
                      // Constitution Day, June 5th
                      || (d == 5 && m == Month.June)
                      // Christmas
                      || (d == 25 && m == Month.December)
                      // Boxing Day
                      || (d == 26 && m == Month.December))
                      return false;
                  return true;
              }
        }
    }
}

