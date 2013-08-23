/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
  
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

	//! probability
	//! \ingroup types 


	//! default probability term structure
	public class DefaultProbabilityTermStructure : TermStructure
	{
//        ! \name Constructors
//            See the TermStructure documentation for issues regarding
//            constructors.
//        
		//@{
		//! default constructor
//        ! \warning term structures initialized by means of this
//                     constructor must manage their own reference date
//                     by overriding the referenceDate() method.
//        
		public DefaultProbabilityTermStructure() : this(new DayCounter())
		{
		}
		public DefaultProbabilityTermStructure(DayCounter dc) : base(dc)
		{
		}
		//! initialize with a fixed reference date
		public DefaultProbabilityTermStructure(Date referenceDate, Calendar cal) : this(referenceDate, cal, new DayCounter())
		{
		}
		public DefaultProbabilityTermStructure(Date referenceDate) : this(referenceDate, new Calendar(), new DayCounter())
		{
		}
		public DefaultProbabilityTermStructure(Date referenceDate, Calendar cal, DayCounter dc) : base(referenceDate, cal, dc)
		{
		}
		//! calculate the reference date based on the global evaluation date
		public DefaultProbabilityTermStructure(int settlementDays, Calendar cal) : this(settlementDays, cal, new DayCounter())
		{
		}
		public DefaultProbabilityTermStructure(int settlementDays, Calendar cal, DayCounter dc) : base(settlementDays, cal, dc)
		{
		}
		//@}
		//! \name Default probability
		//@{
		//! probability of default between today and a given date
		public double defaultProbability(Date d)
		{
            return defaultProbability(d, false);
		}
		public double defaultProbability(Date d, bool extrapolate)
		{
			return 1.0 - survivalProbability(d, extrapolate);
		}
		//! probability of default between today (t = 0) and a given time
		public double defaultProbability(double t)
		{
            return defaultProbability(t, false);
		}
		public double defaultProbability(double t, bool extrapolate)
		{
			return 1.0 - survivalProbability(t, extrapolate);
		}
		//! probability of default between two given dates
		public double defaultProbability(Date d1, Date d2)
		{
            return defaultProbability(d1, d2, false);
		}
		public double defaultProbability(Date d1, Date d2, bool extrapolate)
		{
			if (!(d1 <= d2))
                throw new ApplicationException("initial date (" + d1 + ") " + "later than final date (" + d2 + ")");

			double p1 = defaultProbability(d1, extrapolate);
			double p2 = defaultProbability(d2, extrapolate);
			return p2 - p1;
		}
		//! probability of default between two given times
		public double defaultProbability(double t1, double t2)
		{
            return defaultProbability(t1, t2, false);
		}
		public double defaultProbability(double t1, double t2, bool extrapolate)
		{
			if (!(t1 <= t2))
                throw new ApplicationException("initial time (" + t1 + ") " + "later than final time (" + t2 + ")");

			double p1 = defaultProbability(t1, extrapolate);
			double p2 = defaultProbability(t2, extrapolate);
			return p2 - p1;
		}
		//@}
		//! \name Survival probability
		//@{
		//! probability of survival between today and a given date
		public double survivalProbability(Date d)
		{
			return survivalProbability(d, false);
		}
		public double survivalProbability(Date d, bool extrapolate)
		{
			checkRange(d, extrapolate);
			return survivalProbabilityImpl(timeFromReference(d));
		}
		//! probability of default between today (t = 0) and a given time
		public double survivalProbability(double t)
		{
            return survivalProbability(t, false);
		}
		public double survivalProbability(double t, bool extrapolate)
		{
			checkRange(t, extrapolate);
			return survivalProbabilityImpl(t);
		}
		//@}
		//! \name Default density
		//@{
		//! default density at a given date
		public double defaultDensity(Date d)
		{
            return defaultDensity(d, false);
		}
		public double defaultDensity(Date d, bool extrapolate)
		{
			checkRange(d, extrapolate);
			return defaultDensityImpl(timeFromReference(d));
		}
		//! default density at a given time
		public double defaultDensity(double t)
		{
            return defaultDensity(t, false);
		}
		public double defaultDensity(double t, bool extrapolate)
		{
			checkRange(t, extrapolate);
			return defaultDensityImpl(t);
		}
		//@}
		//! \name Hazard rate
		//@{
		//! hazard rate at a given date
		public double hazardRate(Date d)
		{
            return hazardRate(d, false);
		}
		public double hazardRate(Date d, bool extrapolate)
		{
			checkRange(d, extrapolate);
			return hazardRateImpl(timeFromReference(d));
		}
		//! hazard rate at a given time
		public double hazardRate(double t)
		{
            return hazardRate(t, false);
		}
		public double hazardRate(double t, bool extrapolate)
		{
			checkRange(t, extrapolate);
			return hazardRateImpl(t);
		}
		//@}
		//! probability of survival between today (t = 0) and a given time
        protected virtual double survivalProbabilityImpl(double NamelessParameter) { throw new NotSupportedException(); }
		//! instantaneous default density at a given time
        protected virtual double defaultDensityImpl(double NamelessParameter) { throw new NotSupportedException(); }
        //! instantaneous hazard rate at a given time
        protected virtual double hazardRateImpl(double NamelessParameter) { throw new NotSupportedException(); }
    }
}
