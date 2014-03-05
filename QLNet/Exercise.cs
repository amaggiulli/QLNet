/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
    //! Base exercise class
    public class Exercise {
        public enum Type { American, Bermudan, European };

        protected Type type_;
        public Type type() { return type_; }

        protected List<Date> dates_;
        public List<Date> dates() { return dates_; }

        // constructor
        public Exercise(Type type) {
            type_ = type;
        }

        // inspectors
        public Date date(int index) { return dates_[index]; }
        public Date lastDate() { return dates_.Last(); }
    }

    //! Early-exercise base class
    /*! The payoff can be at exercise (the default) or at expiry */
    public class EarlyExercise : Exercise {
        private bool payoffAtExpiry_;
        public bool payoffAtExpiry() { return payoffAtExpiry_; }
        
        // public EarlyExercise(Type type, bool payoffAtExpiry = false) : base(type) {
        public EarlyExercise(Type type, bool payoffAtExpiry) : base(type) {
            payoffAtExpiry_ = payoffAtExpiry;
        }
    }

    //! American exercise
    /*! An American option can be exercised at any time between two
        predefined dates; the first date might be omitted, in which
        case the option can be exercised at any time before the expiry.

        \todo check that everywhere the American condition is applied
              from earliestDate and not earlier
    */
    public class AmericanExercise : EarlyExercise {
        public AmericanExercise(Date earliestDate, Date latestDate, bool payoffAtExpiry = false)
            : base(Type.American, payoffAtExpiry) {

            if (!(earliestDate <= latestDate))
                throw new ApplicationException("earliest > latest exercise date");
            dates_ = new InitializedList<Date>(2);
            dates_[0] = earliestDate;
            dates_[1] = latestDate;
        }

        public AmericanExercise(Date latest, bool payoffAtExpiry = false) : base(Type.American, payoffAtExpiry) {
            dates_ = new InitializedList<Date>(2);
            dates_[0] = Date.minDate();
            dates_[1] = latest;
        }
    }

    //! Bermudan exercise
    /*! A Bermudan option can only be exercised at a set of fixed dates. */
    public class BermudanExercise : EarlyExercise {
        public BermudanExercise(List<Date> dates) : this(dates, false) { }
        public BermudanExercise(List<Date> dates, bool payoffAtExpiry)
            : base(Type.Bermudan, payoffAtExpiry) {
            
            if (dates.Count == 0)
                throw new ApplicationException("no exercise date given");

            dates_ = dates;
            dates_.Sort();
        }
    }

    //! European exercise
    /*! A European option can only be exercised at one (expiry) date. */
    public class EuropeanExercise : Exercise {
        public EuropeanExercise(Date date) : base(Type.European) {
            dates_ = new InitializedList<Date>(1, date);
        }
    }
}
