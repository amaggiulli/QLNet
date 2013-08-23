/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Andrea Maggiulli
  
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

namespace QLNet {
    //! United Kingdom calendars
    /*! Public holidays (data from http://www.dti.gov.uk/er/bankhol.htm):
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday)</li>
        <li>Good Friday</li>
        <li>Easter Monday</li>
        <li>Early May Bank Holiday, first Monday of May</li>
        <li>Spring Bank Holiday, last Monday of May</li>
        <li>Summer Bank Holiday, last Monday of August</li>
        <li>Christmas Day, December 25th (possibly moved to Monday or
            Tuesday)</li>
        <li>Boxing Day, December 26th (possibly moved to Monday or
            Tuesday)</li>
        </ul>

        Holidays for the stock exchange:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday)</li>
        <li>Good Friday</li>
        <li>Easter Monday</li>
        <li>Early May Bank Holiday, first Monday of May</li>
        <li>Spring Bank Holiday, last Monday of May</li>
        <li>Summer Bank Holiday, last Monday of August</li>
        <li>Christmas Day, December 25th (possibly moved to Monday or
            Tuesday)</li>
        <li>Boxing Day, December 26th (possibly moved to Monday or
            Tuesday)</li>
        </ul>

        Holidays for the metals exchange:
        <ul>
        <li>Saturdays</li>
        <li>Sundays</li>
        <li>New Year's Day, January 1st (possibly moved to Monday)</li>
        <li>Good Friday</li>
        <li>Easter Monday</li>
        <li>Early May Bank Holiday, first Monday of May</li>
        <li>Spring Bank Holiday, last Monday of May</li>
        <li>Summer Bank Holiday, last Monday of August</li>
        <li>Christmas Day, December 25th (possibly moved to Monday or
            Tuesday)</li>
        <li>Boxing Day, December 26th (possibly moved to Monday or
            Tuesday)</li>
        </ul>

        \todo add LIFFE
        \test the correctness of the returned results is tested  against a list of known holidays.
    */

    public class UnitedKingdom : Calendar {
        public enum Market { Settlement, Exchange, Metals }

        public UnitedKingdom() : this(Market.Settlement) { }
        public UnitedKingdom(Market m) : base() { 
            switch (m) {
                case Market.Settlement:
                    calendar_ = Settlement.Singleton;
                    break;
                case Market.Exchange:
                    calendar_ = Exchange.Singleton;
                    break;
                case Market.Metals:
                    calendar_ = Metals.Singleton;
                    break;
                default:
                    throw new ArgumentException("Unknown market: " + m); ;
            }
        }

		private class Settlement : Calendar.WesternImpl {
			public static readonly Settlement Singleton = new Settlement();
            private Settlement() { }

            public override string name() { return "UK settlement"; }
            public override bool isBusinessDay(Date date) {
		        DayOfWeek w = date.DayOfWeek;
		        int d = date.Day, dd = date.DayOfYear;
		        Month m = (Month)date.Month;
		        int y = date.Year;
		        int em = easterMonday(y);
				
		        if (isWeekend(w)
		            // New Year's Day (possibly moved to Monday)
		            || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == Month.January)
		            // Good Friday
		            || (dd == em-3)
		            // Easter Monday
		            || (dd == em)
		            // first Monday of May (Early May Bank Holiday)
		            || (d <= 7 && w == DayOfWeek.Monday && m == Month.May)
		            // last Monday of May (Spring Bank Holiday)
		            || (d >= 25 && w == DayOfWeek.Monday && m == Month.May && y != 2002)
		            // last Monday of August (Summer Bank Holiday)
		            || (d >= 25 && w == DayOfWeek.Monday && m == Month.August)
		            // Christmas (possibly moved to Monday or Tuesday)
		            || ((d == 25 || (d == 27 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) && m == Month.December)
		            // Boxing Day (possibly moved to Monday or Tuesday)
		            || ((d == 26 || (d == 28 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) && m == Month.December)
		            // June 3rd, 2002 only (Golden Jubilee Bank Holiday)
		            // June 4rd, 2002 only (special Spring Bank Holiday)
		            || ((d == 3 || d == 4) && m == Month.June && y == 2002)
		            // December 31st, 1999 only
		            || (d == 31 && m == Month.December && y == 1999))
		            return false;
		        return true;
			}
		}
        private class Exchange : Calendar.WesternImpl {
			internal static readonly Exchange Singleton = new Exchange();
            private Exchange() { }
            
            public override string name() { return "London stock exchange"; }
	        public override bool isBusinessDay(Date date) {
		        DayOfWeek w = date.DayOfWeek;
		        int d = date.Day, dd = date.DayOfYear;
		        Month m = (Month)date.Month;
		        int y = date.Year;
		        int em = easterMonday(y);
		        if (isWeekend(w)
		            // New Year's Day (possibly moved to Monday)
		            || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == Month.January)
		            // Good Friday
		            || (dd == em-3)
		            // Easter Monday
		            || (dd == em)
		            // first Monday of May (Early May Bank Holiday)
		            || (d <= 7 && w == DayOfWeek.Monday && m == Month.May)
		            // last Monday of May (Spring Bank Holiday)
		            || (d >= 25 && w == DayOfWeek.Monday && m == Month.May && y != 2002)
		            // last Monday of August (Summer Bank Holiday)
		            || (d >= 25 && w == DayOfWeek.Monday && m == Month.August)
		            // Christmas (possibly moved to Monday or Tuesday)
		            || ((d == 25 || (d == 27 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) && m == Month.December)
		            // Boxing Day (possibly moved to Monday or Tuesday)
		            || ((d == 26 || (d == 28 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) && m == Month.December)
		            // June 3rd, 2002 only (Golden Jubilee Bank Holiday)
		            // June 4rd, 2002 only (special Spring Bank Holiday)
		            || ((d == 3 || d == 4) && m == Month.June && y == 2002)
		            // December 31st, 1999 only
		            || (d == 31 && m == Month.December && y == 1999))
		            return false;
		        return true;
			}
		}
        private class Metals : Calendar.WesternImpl {
			internal static readonly Metals Singleton = new Metals();
            private Metals() { }
            
            public override string name() { return "London metals exchange"; }
			public override bool isBusinessDay(Date date) {
		        DayOfWeek w = date.DayOfWeek;
		        int d = date.Day, dd = date.DayOfYear;
		        Month m = (Month)date.Month;
		        int y = date.Year;
		        int em = easterMonday(y);
		        if (isWeekend(w)
		            // New Year's Day (possibly moved to Monday)
		            || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == Month.January)
		            // Good Friday
		            || (dd == em-3)
		            // Easter Monday
		            || (dd == em)
		            // first Monday of May (Early May Bank Holiday)
		            || (d <= 7 && w == DayOfWeek.Monday && m == Month.May)
		            // last Monday of May (Spring Bank Holiday)
		            || (d >= 25 && w == DayOfWeek.Monday && m == Month.May && y != 2002)
		            // last Monday of August (Summer Bank Holiday)
		            || (d >= 25 && w == DayOfWeek.Monday && m == Month.August)
		            // Christmas (possibly moved to Monday or Tuesday)
		            || ((d == 25 || (d == 27 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) && m == Month.December)
		            // Boxing Day (possibly moved to Monday or Tuesday)
		            || ((d == 26 || (d == 28 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday))) && m == Month.December)
		            // June 3rd, 2002 only (Golden Jubilee Bank Holiday)
		            // June 4rd, 2002 only (special Spring Bank Holiday)
		            || ((d == 3 || d == 4) && m == Month.June && y == 2002)
		            // December 31st, 1999 only
		            || (d == 31 && m == Month.December && y == 1999))
		            return false;
		        return true;
			}
		}
    }
}
