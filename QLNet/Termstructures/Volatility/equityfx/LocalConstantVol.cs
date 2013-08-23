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
    //! Constant local volatility, no time-strike dependence
    /*! This class implements the LocalVolatilityTermStructure
        interface for a constant local volatility (no time/asset
        dependence).  Local volatility and Black volatility are the
        same when volatility is at most time dependent, so this class
        is basically a proxy for BlackVolatilityTermStructure.
    */
    public class LocalConstantVol : LocalVolTermStructure {
        Handle<Quote> volatility_;
        DayCounter dayCounter_;

        public LocalConstantVol(Date referenceDate, double volatility, DayCounter dc)
            : base(referenceDate) {
            volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
            dayCounter_ = dc;
        }

        public LocalConstantVol(Date referenceDate, Handle<Quote> volatility, DayCounter dc)
            : base(referenceDate) {
            volatility_ = volatility;
            dayCounter_ = dc;

            volatility_.registerWith(update);
        }

        public LocalConstantVol(int settlementDays, Calendar calendar, double volatility, DayCounter dayCounter)
            : base(settlementDays, calendar) {
            volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
            dayCounter_ = dayCounter;
        }

        public LocalConstantVol(int settlementDays, Calendar calendar, Handle<Quote> volatility, DayCounter dayCounter)
            : base(settlementDays,calendar) {
            volatility_ = volatility;
            dayCounter_ = dayCounter;

            volatility_.registerWith(update);
        }

        //! \name TermStructure interface
        //@{
        public override DayCounter dayCounter() { return dayCounter_; }
        public override Date maxDate() { return Date.maxDate(); }
        //@}
        //! \name VolatilityTermStructure interface
        //@{
        public override double minStrike() { return double.MinValue; }
        public override double maxStrike() { return double.MaxValue; }

        protected override double localVolImpl(double t, double s) { return volatility_.link.value(); }
    }
}
