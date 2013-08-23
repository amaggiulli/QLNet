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
    //! Volatility term structure
    /*! This abstract class defines the interface of concrete volatility structures which will be derived from this one. */
    public class VolatilityTermStructure : TermStructure {
        //! the business day convention used in tenor to date conversion
        private BusinessDayConvention bdc_;
        public virtual BusinessDayConvention businessDayConvention() { return bdc_; }


        // parameterless ctor is required for Handles
        public VolatilityTermStructure() { }

        //! default constructor
        /*! \warning term structures initialized by means of this constructor must manage their own reference date
                     by overriding the referenceDate() method. */
        // public VolatilityTermStructure(Calendar cal, BusinessDayConvention bdc, DayCounter dc = DayCounter());
        public VolatilityTermStructure(Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(dc) {
            bdc_ = bdc;
            calendar_ = cal;
        }

        //! initialize with a fixed reference date
        // public VolatilityTermStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc = DayCounter())
        public VolatilityTermStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(referenceDate, cal, dc) {
            bdc_ = bdc;
        }
            
        //! calculate the reference date based on the global evaluation date
        // public VolatilityTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc = DayCounter());
        public VolatilityTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(settlementDays, cal, dc) {
            bdc_ = bdc;
        }

        // swaption style
        public Date optionDateFromTenor(Period p) {
            return calendar().advance(referenceDate(), p, businessDayConvention());
        }

        //! the minimum strike for which the term structure can return vols
        public virtual double minStrike() { throw new NotSupportedException(); }
        //! the maximum strike for which the term structure can return vols
        public virtual double maxStrike() { throw new NotSupportedException(); }
      
        //! strike-range check
        protected void checkStrike(double strike, bool extrapolate) {
            if (!(extrapolate || allowsExtrapolation() || (strike >= minStrike() && strike <= maxStrike())))
                throw new ApplicationException("strike (" + strike + ") is outside the curve domain ["
                                               + minStrike() + "," + maxStrike() + "]");
        }
    }
}
