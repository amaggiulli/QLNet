/*
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
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
    //! South Korean calendars
   /*! Public holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Independence Day, March 1st</li>
       <li>Arbour Day, April 5th (until 2005)</li>
       <li>Labour Day, May 1st</li>
       <li>Children's Day, May 5th</li>
       <li>Memorial Day, June 6th</li>
       <li>Constitution Day, July 17th (until 2007)</li>
       <li>Liberation Day, August 15th</li>
       <li>National Fondation Day, October 3th</li>
       <li>Christmas Day, December 25th</li>
       </ul>

       Other holidays for which no rule is given
       (data available for 2004-2013 only:)
       <ul>
       <li>Lunar New Year, the last day of the previous lunar year</li> 
       <li>Election Days</li>
       <li>National Assemblies</li>
       <li>Presidency</li>
       <li>Regional Election Days</li>
       <li>Buddha's birthday</li>
       <li>Harvest Moon Day</li>
       </ul>

       Holidays for the Korea exchange
       (data from <http://www.krx.co.kr> or
       <http://www.dooriworld.com/daishin/holiday/holiday.html>):
       <ul>
       <li>Public holidays as listed above</li>
       <li>Year-end closing</li>
       </ul>

       \ingroup calendars
   */
   public class SouthKorea : Calendar {
        public enum Market { Settlement,  //!< Public holidays
                      KRX          //!< Korea exchange
        }

        public SouthKorea() : this(Market.KRX) { }
        public SouthKorea(Market m)
            : base() {
            // all calendar instances on the same market share the same
            // implementation instance
            switch (m) {
                case Market.Settlement:
                    calendar_ = Settlement.Singleton;
                    break;
                case Market.KRX:
                    calendar_ = KRX.Singleton;
                    break;
                default:
                    throw new ArgumentException("Unknown market: " + m); ;
            }
        }

        class Settlement : Calendar {
            public static readonly Settlement Singleton = new Settlement();
            public Settlement() { }
          
            public override string name() { return "South-Korean settlement"; }
            public override bool isWeekend(DayOfWeek w) {
                return w == DayOfWeek.Saturday || w == DayOfWeek.Sunday;
            }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;

                if (isWeekend(w)
                    // New Year's Day
                    || (d == 1 && m == Month.January)
                    // Independence Day
                    || (d == 1 && m == Month.March)
                    // Arbour Day
                    || (d == 5 && m == Month.April && y <= 2005)
                    // Labour Day
                    || (d == 1 && m == Month.May)
                    // Children's Day
                    || (d == 5 && m == Month.May)
                    // Memorial Day
                    || (d == 6 && m == Month.June)
                    // Constitution Day
                    || (d == 17 && m == Month.July && y <= 2007)
                    // Liberation Day
                    || (d == 15 && m == Month.August)
                    // National Foundation Day
                    || (d == 3 && m == Month.October)
                    // Christmas Day
                    || (d == 25 && m == Month.December)

                    // Lunar New Year
                    || ((d == 21 || d == 22 || d == 23) && m == Month.January && y == 2004)
                    || ((d == 8 || d == 9 || d == 10) && m == Month.February && y == 2005)
                    || ((d == 28 || d == 29 || d == 30) && m == Month.January && y == 2006)
                    || (d == 19 && m == Month.February && y == 2007)
                    || ((d == 6 || d == 7 || d == 8) && m == Month.February && y == 2008)
                    || ((d == 25 || d == 26 || d == 27) && m == Month.January && y == 2009)
                    || ((d == 13 || d == 14 || d == 15) && m == Month.February && y == 2010)
                    || ((d == 2 || d == 3 || d == 4) && m == Month.February && y == 2011)
                    || ((d == 23 || d == 24) && m == Month.January && y == 2012)
                    || (d == 11 && m == Month.February && y == 2013)
                    // Election Days
                    || (d == 15 && m == Month.April && y == 2004)    // National Assembly
                    || (d == 31 && m == Month.May && y == 2006)      // Regional election
                    || (d == 19 && m == Month.December && y == 2007) // Presidency
                    || (d == 9 && m == Month.April && y == 2008)    // National Assembly
                    || (d == 2 && m == Month.June && y == 2010)     // Local election
                    || (d == 11 && m == Month.April && y == 2012)    // National Assembly
                    || (d == 19 && m == Month.December && y == 2012) // Presidency
                    // Buddha's birthday
                    || (d == 26 && m == Month.May && y == 2004)
                    || (d == 15 && m == Month.May && y == 2005)
                    || (d == 5 && m == Month.May && y == 2006)
                    || (d == 24 && m == Month.May && y == 2007)
                    || (d == 12 && m == Month.May && y == 2008)
                    || (d == 2 && m == Month.May && y == 2009)
                    || (d == 21 && m == Month.May && y == 2010)
                    || (d == 10 && m == Month.May && y == 2011)
                    || (d == 28 && m == Month.May && y == 2012)
                    || (d == 17 && m == Month.May && y == 2013)
                    // Harvest Moon Day
                    || ((d == 27 || d == 28 || d == 29) && m == Month.September && y == 2004)
                    || ((d == 17 || d == 18 || d == 19) && m == Month.September && y == 2005)
                    || ((d == 5 || d == 6 || d == 7) && m == Month.October && y == 2006)
                    || ((d == 24 || d == 25 || d == 26) && m == Month.September && y == 2007)
                    || ((d == 13 || d == 14 || d == 15) && m == Month.September && y == 2008)
                    || ((d == 2 || d == 3 || d == 4) && m == Month.October && y == 2009)
                    || ((d == 21 || d == 22 || d == 23) && m == Month.September && y == 2010)
                    || ((d == 12 || d == 13) && m == Month.September && y == 2011)
                    || (d == 1 && m == Month.October && y == 2012)
                    || ((d == 18 || d == 19 || d == 20) && m == Month.September && y == 2013)
                    // Hangul Proclamation of Korea
                    || (d == 9 && m == Month.October && y == 2013) 
                    )
                    return false;

                return true;
            }
        }

        class KRX : Settlement {
            new public static readonly KRX Singleton = new KRX();
            public KRX() { }

            public override string name() { return "South-Korea exchange"; }
            public override bool isBusinessDay(Date date)  {
                // public holidays
                if ( !base.isBusinessDay(date) )
                    return false;

                int d = date.Day;
                Month m = (Month)date.Month;
                int y = date.Year;

                if (// Year-end closing
                       (d == 31 && m == Month.December && y == 2004)
                    || (d == 30 && m == Month.December && y == 2005)
                    || (d == 29 && m == Month.December && y == 2006)
                    || (d == 31 && m == Month.December && y == 2007)
                    || (d == 31 && m == Month.December && y == 2008)
                    || (d == 31 && m == Month.December && y == 2009)
                    || (d == 31 && m == Month.December && y == 2010)
                    || (d == 30 && m == Month.December && y == 2011)
                    || (d == 31 && m == Month.December && y == 2012)
                    || (d == 31 && m == Month.December && y == 2013)
                    )
                    return false;

                return true;
            }
        };
    }
}



  
