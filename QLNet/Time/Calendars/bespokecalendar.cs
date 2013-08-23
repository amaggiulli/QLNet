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
    //! Bespoke calendar
    /*! This calendar has no predefined set of business days. Holidays
        and weekdays can be defined by means of the provided
        interface. Instances constructed by copying remain linked to
        the original one; adding a new holiday or weekday will affect
        all linked instances.

        \ingroup calendars
    */
    public class BespokeCalendar : Calendar {
        private string name_;
        public override string name() { return name_; }

        /*! \warning different bespoke calendars created with the same
                     name (or different bespoke calendars created with
                     no name) will compare as equal.
        */
        public BespokeCalendar() : this("") { }
        public BespokeCalendar(string name) : base(new Impl()) { 
            name_ = name;
        }

        //! marks the passed day as part of the weekend
        public void addWeekend(DayOfWeek w) {
            (calendar_ as Impl).addWeekend(w);
        }

        // here implementation does not follow a singleton pattern
        class Impl : Calendar.WesternImpl {
            public Impl() { }

            public override bool isWeekend(DayOfWeek w) { return (weekend_.Contains(w)); }
            public override bool isBusinessDay(Date date) { return !isWeekend(date.DayOfWeek); }
            public void addWeekend(DayOfWeek w) { weekend_.Add(w); }

            private List<DayOfWeek> weekend_ = new List<DayOfWeek>();
        }
    }
}
