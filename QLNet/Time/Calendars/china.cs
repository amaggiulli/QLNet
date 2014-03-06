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
    //! Chinese calendar
    /*! Holidays:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's day, January 1st (possibly followed by one or
            two more holidays)</li>
        <li>Labour Day, first week in May</li>
        <li>National Day, one week from October 1st</li>
        </ul>

        Other holidays for which no rule is given (data available for
        2004-2013 only):
        <ul>
        <li>Chinese New Year</li>
        <li>Ching Ming Festival</li>
        <li>Tuen Ng Festival</li>
        <li>Mid-Autumn Festival</li>
        </ul>

        Data from <http://www.sse.com.cn/sseportal/en/home/home.shtml>

        \ingroup calendars
    */
    public class China : Calendar {
        public China() : base(Impl.Singleton) { }

        class Impl : Calendar {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }

            public override string name() { return "Shanghai stock exchange"; }
            public override bool isWeekend(DayOfWeek w) {
                return w == DayOfWeek.Saturday || w == DayOfWeek.Sunday;
            }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day;
                Month m = (Month)date.Month;
                int y = date.Year;

                if (isWeekend(w)
                    // New Year's Day
                    || (d == 1 && m == Month.January)
                    || (y == 2005 && d == 3 && m == Month.January)
                    || (y == 2006 && (d == 2 || d == 3) && m == Month.January)
                    || (y == 2007 && d <= 3 && m == Month.January)
                    || (y == 2007 && d == 31 && m == Month.December)
                    || (y == 2009 && d == 2 && m == Month.January)
                    || (y == 2011 && d == 3 && m == Month.January)
                    || (y == 2012 && (d == 2 || d == 3) && m == Month.January)
                    || (y == 2013 && d <= 3 && m == Month.January) 
                    // Chinese New Year
                    || (y == 2004 && d >= 19 && d <= 28 && m == Month.January)
                    || (y == 2005 && d >= 7 && d <= 15 && m == Month.February)
                    || (y == 2006 && ((d >= 26 && m == Month.January) ||
                                      (d <= 3 && m == Month.February)))
                    || (y == 2007 && d >= 17 && d <= 25 && m == Month.February)
                    || (y == 2008 && d >= 6 && d <= 12 && m == Month.February)
                    || (y == 2009 && d >= 26 && d <= 30 && m == Month.January)
                    || (y == 2010 && d >= 15 && d <= 19 && m == Month.January)
                    || (y == 2011 && d >= 2 && d <= 8 && m == Month.February)
                    || (y == 2012 && d >= 23 && d <= 28 && m == Month.January)
                    || (y == 2013 && d >= 11 && d <= 15 && m == Month.February) 
                    // Ching Ming Festival
                    || (y <= 2008 && d == 4 && m == Month.April)
                    || (y == 2009 && d == 6 && m == Month.April)
                    || (y == 2010 && d == 5 && m == Month.April)
                    || (y == 2011 && d >= 3 && d <= 5 && m == Month.April)
                    || (y == 2012 && d >= 2 && d <= 4 && m == Month.April)
                    || (y == 2013 && d >= 4 && d <= 5 && m == Month.April)                     
                    // Labor Day
                    || (y <= 2007 && d >= 1 && d <= 7 && m == Month.May)
                    || (y == 2008 && d >= 1 && d <= 2 && m == Month.May)
                    || (y == 2009 && d == 1 && m == Month.May)
                    || (y == 2010 && d == 3 && m == Month.May)
                    || (y == 2011 && d == 2 && m == Month.May)
                    || (y == 2012 && ((d == 30 && m == Month.April) || (d == 1 && m == Month.May)))
                    || (y == 2013 && ((d >= 29 && m == Month.April) || (d == 1 && m == Month.May))) 
                    // Tuen Ng Festival
                    || (y <= 2008 && d == 9 && m == Month.June)
                    || (y == 2009 && (d == 28 || d == 29) && m == Month.May)
                    || (y == 2010 && d >= 14 && d <= 16 && m == Month.June)
                    || (y == 2011 && d >= 4 && d <= 6 && m == Month.June)
                    || (y == 2012 && d >= 22 && d <= 24 && m == Month.June)
                    || (y == 2013 && d >= 10 && d <= 12 && m == Month.June) 
                    // Mid-Autumn Festival
                    || (y <= 2008 && d == 15 && m == Month.September)
                    || (y == 2010 && d >= 22 && d <= 24 && m == Month.September)
                    || (y == 2011 && d >= 10 && d <= 12 && m == Month.September)
                    || (y == 2012 && d == 30 && m == Month.September)
                    || (y == 2013 && d >= 19 && d <= 20 && m == Month.September) 
                    // National Day
                    || (y <= 2007 && d >= 1 && d <= 7 && m == Month.October)
                    || (y == 2008 && ((d >= 29 && m == Month.September) ||
                                      (d <= 3 && m == Month.October)))
                    || (y == 2009 && d >= 1 && d <= 8 && m == Month.October)
                    || (y == 2010 && d >= 1 && d <= 7 && m == Month.October)
                    || (y == 2011 && d >= 1 && d <= 7 && m == Month.October)
                    || (y == 2012 && d >= 1 && d <= 7 && m == Month.October)
                    || (y == 2013 && d >= 1 && d <= 7 && m == Month.October) 
                    )
                    return false;
                return true;
            }
        }
    }
}

