/*
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
using System.Linq;
using System.Text;

namespace QLNet {
    //! United States calendars
    /*! Public holidays (see: http://www.opm.gov/fedhol/):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday if
            actually on Sunday, or to Friday if on Saturday)</li>
        <li>Martin Luther King's birthday, third Monday in January</li>
        <li>Presidents' Day (a.k.a. Washington's birthday),
            third Monday in February</li>
        <li>Memorial Day, last Monday in May</li>
        <li>Independence Day, July 4th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Labor Day, first Monday in September</li>
        <li>Columbus Day, second Monday in October</li>
        <li>Veterans' Day, November 11th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Thanksgiving Day, fourth Thursday in November</li>
        <li>Christmas, December 25th (moved to Monday if Sunday or Friday
            if Saturday)</li>
        </ul>

        Holidays for the stock exchange (data from http://www.nyse.com):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday if
            actually on Sunday)</li>
        <li>Martin Luther King's birthday, third Monday in January (since
            1998)</li>
        <li>Presidents' Day (a.k.a. Washington's birthday),
            third Monday in February</li>
        <li>Good Friday</li>
        <li>Memorial Day, last Monday in May</li>
        <li>Independence Day, July 4th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Labor Day, first Monday in September</li>
        <li>Thanksgiving Day, fourth Thursday in November</li>
        <li>Presidential election day, first Tuesday in November of election
            years (until 1980)</li>
        <li>Christmas, December 25th (moved to Monday if Sunday or Friday
            if Saturday)</li>
        <li>Special historic closings (see
            http://www.nyse.com/pdfs/closings.pdf)</li>
        </ul>

        Holidays for the government bond market (data from
        http://www.bondmarkets.com):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday if
            actually on Sunday)</li>
        <li>Martin Luther King's birthday, third Monday in January</li>
        <li>Presidents' Day (a.k.a. Washington's birthday),
            third Monday in February</li>
        <li>Good Friday</li>
        <li>Memorial Day, last Monday in May</li>
        <li>Independence Day, July 4th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Labor Day, first Monday in September</li>
        <li>Columbus Day, second Monday in October</li>
        <li>Veterans' Day, November 11th (moved to Monday if Sunday or
            Friday if Saturday)</li>
        <li>Thanksgiving Day, fourth Thursday in November</li>
        <li>Christmas, December 25th (moved to Monday if Sunday or Friday
            if Saturday)</li>
        </ul>

        Holidays for the North American Energy Reliability Council
        (data from http://www.nerc.com/~oc/offpeaks.html):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday if
            actually on Sunday)</li>
        <li>Memorial Day, last Monday in May</li>
        <li>Independence Day, July 4th (moved to Monday if Sunday)</li>
        <li>Labor Day, first Monday in September</li>
        <li>Thanksgiving Day, fourth Thursday in November</li>
        <li>Christmas, December 25th (moved to Monday if Sunday)</li>
        </ul>

        \test the correctness of the returned results is tested
              against a list of known holidays.
    */

    public class UnitedStates : Calendar {
        //! US calendars
        public enum Market {
            Settlement,     //!< generic settlement calendar
            NYSE,           //!< New York stock exchange calendar
            GovernmentBond, //!< government-bond calendar
            NERC            //!< off-peak days for NERC
        };

        public UnitedStates() : this(Market.Settlement) { }
        public UnitedStates(Market m) : base() {
            switch (m) {
                case Market.Settlement:
                    calendar_ = Settlement.Singleton;
                    break;
                case Market.NYSE:
                    calendar_ = NYSE.Singleton;
                    break;
                case Market.GovernmentBond:
                    calendar_ = GovernmentBond.Singleton;
                    break;
                case Market.NERC:
                    calendar_ = NERC.Singleton;
                    break;
                default:
                    throw new ArgumentException("Unknown market: " + m); ;
            }
        }

        private class Settlement : Calendar.WesternImpl {
            public static readonly Settlement Singleton = new Settlement();
            private Settlement() { }

            public override string name() { return "US settlement"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day;
                Month m = (Month)date.Month;
                if (isWeekend(w)
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || ((d == 1 || (d == 2 && w == DayOfWeek.Monday)) && m == Month.January)
                    // (or to Friday if on Saturday)
                    || (d == 31 && w == DayOfWeek.Friday && m == Month.December)
                    // Martin Luther King's birthday (third Monday in January)
                    || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.January)
                    // Washington's birthday (third Monday in February)
                    || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.February)
                    // Memorial Day (last Monday in May)
                    || (d >= 25 && w == DayOfWeek.Monday && m == Month.May)
                    // Independence Day (Monday if Sunday or Friday if Saturday)
                    || ((d == 4 || (d == 5 && w == DayOfWeek.Monday) ||
                         (d == 3 && w == DayOfWeek.Friday)) && m == Month.July)
                    // Labor Day (first Monday in September)
                    || (d <= 7 && w == DayOfWeek.Monday && m == Month.September)
                    // Columbus Day (second Monday in October)
                    || ((d >= 8 && d <= 14) && w == DayOfWeek.Monday && m == Month.October)
                    // Veteran's Day (Monday if Sunday or Friday if Saturday)
                    || ((d == 11 || (d == 12 && w == DayOfWeek.Monday) ||
                         (d == 10 && w == DayOfWeek.Friday)) && m == Month.November)
                    // Thanksgiving Day (fourth Thursday in November)
                    || ((d >= 22 && d <= 28) && w == DayOfWeek.Thursday && m == Month.November)
                    // Christmas (Monday if Sunday or Friday if Saturday)
                    || ((d == 25 || (d == 26 && w == DayOfWeek.Monday) ||
                         (d == 24 && w == DayOfWeek.Friday)) && m == Month.December))
                    return false;
                return true;
            }
        }
        private class NYSE : Calendar.WesternImpl {
            public static readonly NYSE Singleton = new NYSE();
            private NYSE() { }
            
            public override string name() { return "New York stock exchange"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);
                if (isWeekend(w)
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || ((d == 1 || (d == 2 && w == DayOfWeek.Monday)) && m == Month.January)
                    // Washington's birthday (third Monday in February)
                    || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.February)
                    // Good Friday
                    || (dd == em - 3)
                    // Memorial Day (last Monday in May)
                    || (d >= 25 && w == DayOfWeek.Monday && m == Month.May)
                    // Independence Day (Monday if Sunday or Friday if Saturday)
                    || ((d == 4 || (d == 5 && w == DayOfWeek.Monday) ||
                         (d == 3 && w == DayOfWeek.Friday)) && m == Month.July)
                    // Labor Day (first Monday in September)
                    || (d <= 7 && w == DayOfWeek.Monday && m == Month.September)
                    // Thanksgiving Day (fourth Thursday in November)
                    || ((d >= 22 && d <= 28) && w == DayOfWeek.Thursday && m == Month.November)
                    // Christmas (Monday if Sunday or Friday if Saturday)
                    || ((d == 25 || (d == 26 && w == DayOfWeek.Monday) ||
                         (d == 24 && w == DayOfWeek.Friday)) && m == Month.December)
                    ) return false;

                if (y >= 1998) {
                    if (// Martin Luther King's birthday (third Monday in January)
                        ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.January)
                        // President Reagan's funeral
                        || (y == 2004 && m == Month.June && d == 11)
                        // September 11, 2001
                        || (y == 2001 && m == Month.September && (11 <= d && d <= 14))
                        // President Ford's funeral
                        || (y == 2007 && m == Month.January && d == 2)
                        ) return false;
                } else if (y <= 1980) {
                    if (// Presidential election days
                        ((y % 4 == 0) && m == Month.November && d <= 7 && w == DayOfWeek.Tuesday)
                        // 1977 Blackout
                        || (y == 1977 && m == Month.July && d == 14)
                        // Funeral of former President Lyndon B. Johnson.
                        || (y == 1973 && m == Month.January && d == 25)
                        // Funeral of former President Harry S. Truman
                        || (y == 1972 && m == Month.December && d == 28)
                        // National Day of Participation for the lunar exploration.
                        || (y == 1969 && m == Month.July && d == 21)
                        // Funeral of former President Eisenhower.
                        || (y == 1969 && m == Month.March && d == 31)
                        // Closed all day - heavy snow.
                        || (y == 1969 && m == Month.February && d == 10)
                        // Day after Independence Day.
                        || (y == 1968 && m == Month.July && d == 5)
                        // June 12-Dec. 31, 1968
                        // Four day week (closed on Wednesdays) - Paperwork Crisis
                        || (y == 1968 && dd >= 163 && w == DayOfWeek.Wednesday)
                        ) return false;
                } else {
                    if (// Nixon's funeral
                        (y == 1994 && m == Month.April && d == 27)
                        ) return false;
                }

                return true;
            }
        }
        private class GovernmentBond : Calendar.WesternImpl {
            public static readonly GovernmentBond Singleton = new GovernmentBond();
            private GovernmentBond() { }

            public override string name() { return "US government bond market"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day, dd = date.DayOfYear;
                Month m = (Month)date.Month;
                int y = date.Year;
                int em = easterMonday(y);
                if (isWeekend(w)
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || ((d == 1 || (d == 2 && w == DayOfWeek.Monday)) && m == Month.January)
                    // Martin Luther King's birthday (third Monday in January)
                    || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.January)
                    // Washington's birthday (third Monday in February)
                    || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == Month.February)
                    // Good Friday
                    || (dd == em - 3)
                    // Memorial Day (last Monday in May)
                    || (d >= 25 && w == DayOfWeek.Monday && m == Month.May)
                    // Independence Day (Monday if Sunday or Friday if Saturday)
                    || ((d == 4 || (d == 5 && w == DayOfWeek.Monday) ||
                         (d == 3 && w == DayOfWeek.Friday)) && m == Month.July)
                    // Labor Day (first Monday in September)
                    || (d <= 7 && w == DayOfWeek.Monday && m == Month.September)
                    // Columbus Day (second Monday in October)
                    || ((d >= 8 && d <= 14) && w == DayOfWeek.Monday && m == Month.October)
                    // Veteran's Day (Monday if Sunday or Friday if Saturday)
                    || ((d == 11 || (d == 12 && w == DayOfWeek.Monday) ||
                         (d == 10 && w == DayOfWeek.Friday)) && m == Month.November)
                    // Thanksgiving Day (fourth Thursday in November)
                    || ((d >= 22 && d <= 28) && w == DayOfWeek.Thursday && m == Month.November)
                    // Christmas (Monday if Sunday or Friday if Saturday)
                    || ((d == 25 || (d == 26 && w == DayOfWeek.Monday) ||
                         (d == 24 && w == DayOfWeek.Friday)) && m == Month.December))
                    return false;
                return true;
            }
        }
        private class NERC : Calendar.WesternImpl {
            public static readonly NERC Singleton = new NERC();
            private NERC() { }

            public override string name() { return "North American Energy Reliability Council"; }
            public override bool isBusinessDay(Date date) {
                DayOfWeek w = date.DayOfWeek;
                int d = date.Day;
                Month m = (Month)date.Month;
                if (isWeekend(w)
                    // New Year's Day (possibly moved to Monday if on Sunday)
                    || ((d == 1 || (d == 2 && w == DayOfWeek.Monday)) && m == Month.January)
                    // Memorial Day (last Monday in May)
                    || (d >= 25 && w == DayOfWeek.Monday && m == Month.May)
                    // Independence Day (Monday if Sunday)
                    || ((d == 4 || (d == 5 && w == DayOfWeek.Monday)) && m == Month.July)
                    // Labor Day (first Monday in September)
                    || (d <= 7 && w == DayOfWeek.Monday && m == Month.September)
                    // Thanksgiving Day (fourth Thursday in November)
                    || ((d >= 22 && d <= 28) && w == DayOfWeek.Thursday && m == Month.November)
                    // Christmas (Monday if Sunday)
                    || ((d == 25 || (d == 26 && w == DayOfWeek.Monday)) && m == Month.December))
                    return false;
                return true;
            }
        }
    }
}
