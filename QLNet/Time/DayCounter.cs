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

namespace QLNet
{
    // This class provides methods for determining the length of a time period according to given market convention,
    // both as a number of days and as a year fraction.
    public class DayCounter
    {
        // this is a placeholder for actual day counters for Singleton pattern use
        protected DayCounter dayCounter_;
        public DayCounter dayCounter
        {
            get { return dayCounter_; }
            set { dayCounter_ = value; }
        }

        // constructors
        /*! The default constructor returns a day counter with a null implementation, which is therefore unusable except as a
            placeholder. */
        public DayCounter() { }
        public DayCounter(DayCounter d) { dayCounter_ = d; }

        // comparison based on name
        // Returns <tt>true</tt> iff the two day counters belong to the same derived class.
        public static bool operator ==(DayCounter d1, DayCounter d2)
        {
            return ((Object)d1 == null || (Object)d2 == null) ?
                   ((Object)d1 == null && (Object)d2 == null) :
                   (d1.empty() && d2.empty()) || (!d1.empty() && !d2.empty() && d1.name() == d2.name());
        }
        public static bool operator !=(DayCounter d1, DayCounter d2) { return !(d1 == d2); }


        public bool empty() { return dayCounter_ == null; }

        public virtual string name()
        {
            if (empty()) return "No implementation provided";
            else return dayCounter_.name();
        }

        public virtual int dayCount(Date d1, Date d2)
        {
            if (empty()) throw Error.MissingImplementation();
            return dayCounter_.dayCount(d1, d2);
        }

        public double yearFraction(Date d1, Date d2) { return yearFraction(d1, d2, d1, d2); }
        public virtual double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
        {
            if (empty()) throw Error.MissingImplementation();
            return dayCounter_.yearFraction(d1, d2, refPeriodStart, refPeriodEnd);
        }

        public override bool Equals(object o) { return this == (DayCounter)o; }
        public override int GetHashCode() { return 0; }
        public override string ToString() { return this.name(); }
    }
}