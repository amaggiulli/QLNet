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
    //! Implied term structure at a given date in the future
    /*! The given date will be the implied reference date.

        \note This term structure will remain linked to the original structure, i.e., any changes in the latter will be
              reflected in this structure as well.

        \ingroup yieldtermstructures

        \test
        - the correctness of the returned values is tested by checking them against numerical calculations.
        - observability against changes in the underlying term structure is checked.
    */
    public class ImpliedTermStructure : YieldTermStructure {
        private Handle<YieldTermStructure> originalCurve_;

        public ImpliedTermStructure(Handle<YieldTermStructure> h, Date referenceDate)
            : base(referenceDate) {
            originalCurve_ = h;
            originalCurve_.registerWith(update);
        }

        //! \name YieldTermStructure interface
        //@{
        public override DayCounter dayCounter() { return originalCurve_.link.dayCounter(); }
        public override Calendar calendar() { return originalCurve_.link.calendar(); }
        public override int settlementDays() { return originalCurve_.link.settlementDays(); }
        public override Date maxDate() { return originalCurve_.link.maxDate(); }
        //@}

        //! returns the discount factor as seen from the evaluation date
        /* t is relative to the current reference date and needs to be converted to the time relative
           to the reference date of the original curve */
        protected override double discountImpl(double t) {
            Date refDate = referenceDate();
            double originalTime = t + dayCounter().yearFraction(originalCurve_.link.referenceDate(), refDate);
            /* discount at evaluation date cannot be cached since the original curve could change between
               invocations of this method */
            return originalCurve_.link.discount(originalTime, true) / originalCurve_.link.discount(refDate, true);
        }
    }
}
