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
using QLNet;

namespace QLNet
{
    //! Basic term-structure functionality
    public class TermStructure : Extrapolator, IObservable, IObserver
    {
        // fields
        private Date referenceDate_;
        private bool updated_;
        private int settlementDays_;
        private DayCounter dayCounter_;
        public virtual DayCounter dayCounter() { return dayCounter_; }

        protected bool moving_;
        protected Calendar calendar_;

        // properties
        public virtual Calendar calendar() { return calendar_; }
        public virtual int settlementDays() { return settlementDays_; }
        //! the latest time for which the curve can return values
        public virtual double maxTime() { return timeFromReference(maxDate()); }
        //! the latest date for which the curve can return values. should be overridden later
        public virtual Date maxDate() { throw new NotSupportedException(); }
        //! the date at which discount = 1.0 and/or variance = 0.0
        public virtual Date referenceDate()
        {
            if (!updated_)
            {
                Date today = Settings.evaluationDate();
                referenceDate_ = calendar_.advance(today, settlementDays_, TimeUnit.Days);
                updated_ = true;
            }
            return referenceDate_;
        }

        /* Constructors
           There are three ways in which a term structure can keep track of its reference date:
            * the first is that such date is fixed;
            * the second is that it is determined by advancing the current date of a given number of business days;
            * the third is that it is based on the reference date of some other structure.

         * In the first case, the constructor taking a date is to be used;
            * the default implementation of referenceDate() will then return such date.
         * In the second case, the constructor taking a number of days and a calendar is to be used;
            * referenceDate() will return a date calculated based on the current evaluation date,
            * and the term structure and its observers will be notified when the evaluation date changes.
         * In the last case, the referenceDate() method must be overridden in derived classes
            * so that it fetches and return the appropriate date.  */

        // default constructor
        // term structures initialized by means of this constructor must manage their own reference date 
        // by overriding the referenceDate() method.
        public TermStructure() : this(new DayCounter()) { }
        public TermStructure(DayCounter dc)
        {
            moving_ = false;
            updated_ = true;
            settlementDays_ = default(int);
            dayCounter_ = dc;
        }

        // initialize with a fixed reference date
        public TermStructure(Date referenceDate) : this(referenceDate, new Calendar(), new DayCounter()) { }
        public TermStructure(Date referenceDate, Calendar calendar, DayCounter dc)
            : this(dc)
        {
            calendar_ = calendar;
            referenceDate_ = referenceDate;
        }

        public TermStructure(int settlementDays, Calendar cal) : this(settlementDays, cal, new DayCounter()) { }
        public TermStructure(int settlementDays, Calendar cal, DayCounter dc)
        {
            moving_ = true;
            calendar_ = cal;
            updated_ = false;
            settlementDays_ = settlementDays;
            dayCounter_ = dc;

            // observe evaluationDate
            Settings.registerWith(update);

            // verify immediately if calendar and settlementDays are ok
            Date today = Settings.evaluationDate();
            referenceDate_ = calendar_.advance(today, settlementDays_, TimeUnit.Days);
        }

        //! date/time conversion
        public double timeFromReference(Date d) { return dayCounter().yearFraction(referenceDate(), d); }

        //! date-range check
        protected void checkRange(Date d, bool extrapolate)
        {
            if (!(d >= referenceDate()))
                throw new ApplicationException("date (" + d + ") before reference date (" + referenceDate() + ")");

            if (!(extrapolate || allowsExtrapolation() || d <= maxDate()))
                throw new ApplicationException("date (" + d + ") is past max curve date (" + maxDate() + ")");
        }
        //! time-range check
        protected void checkRange(double t, bool extrapolate)
        {
            if (t < 0) throw new ArgumentException("negative time (" + t + ") is given");

            if (!(extrapolate || allowsExtrapolation() || t <= maxTime() || Utils.close_enough(t, maxTime())))
                throw new ArgumentException("time (" + t + ") is past max curve time (" + maxTime() + ")");
        }

        #region observable & observer interface
        // observable interface
        // recheck, this is kind of wrong because of the lazyobject from which this one inherits though it should not
        //public delegate void Callback();
        //public event Callback notifyObservers;

        // observer interface
        public override void update()
        {
            if (moving_)
                updated_ = false;

            // recheck. this is in order to notify observers in the base method of LazyObject
            calculated_ = true;
            base.update();
            // otherwise the following code would be required
            //if (notifyObservers != null)
            //    notifyObservers();
            // the grand reason is that multiple inheritance is not allowed in c# and we need to notify observers in such way
        }
        #endregion
    }
}