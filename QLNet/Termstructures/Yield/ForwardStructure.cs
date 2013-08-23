/*
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
using System.Linq;
using System.Text;

namespace QLNet {
    //! %Forward-rate term structure
    /*! This abstract class acts as an adapter to TermStructure allowing the programmer to implement only the
        <tt>forwardImpl(const Date&, bool)</tt> method in derived classes.
        Zero yields and discounts are calculated from forwards.

        Rates are assumed to be annual continuous compounding.

        \ingroup yieldtermstructures
    */
    public abstract class ForwardRateStructure : YieldTermStructure {

        #region ctors
        public ForwardRateStructure() : base(new Actual365Fixed()) { }
        public ForwardRateStructure(DayCounter dayCounter) : base(dayCounter) { }

        // public ForwardRateStructure(Date referenceDate, Calendar cal = Calendar(), DayCounter dayCounter = Actual365Fixed());
        public ForwardRateStructure(Date referenceDate, Calendar cal, DayCounter dayCounter)
            : base(referenceDate, cal, dayCounter) { }

        // public ForwardRateStructure(int settlementDays, Calendar cal, DayCounter dayCounter = Actual365Fixed());
        public ForwardRateStructure(int settlementDays, Calendar cal, DayCounter dayCounter)
            : base(settlementDays, cal, dayCounter) { } 
        #endregion

        //! \name YieldTermStructure implementation

        /*! Returns the zero yield rate for the given date calculating it from the instantaneous forward rate.
            \warning This is just a default, highly inefficient and possibly wildly inaccurate implementation. Derived
                     classes should implement their own zeroYield method. */
        protected virtual double zeroYieldImpl(double t) {
            if (t == 0.0)
                return forwardImpl(0.0);
            // implement smarter integration if plan to use the following code
            double sum = 0.5*forwardImpl(0.0);
            int N = 1000;
            double dt = t/N;
            for (double i=dt; i<t; i+=dt)
                sum += forwardImpl(i);
            sum += 0.5*forwardImpl(t);
            return sum*dt/t;
        }

        /*! Returns the discount factor for the given date calculating it from the instantaneous forward rate */
        protected override double discountImpl(double t) {
            if (t == 0.0)     // this acts as a safe guard in cases where
                return 1.0;   // zeroYieldImpl(0.0) would throw.

            double r = zeroYieldImpl(t);
            return Math.Exp(-r*t);
        }

        //! instantaneous forward-rate calculation
        protected abstract double forwardImpl(double t);
    }
}
