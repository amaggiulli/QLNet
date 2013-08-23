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

	//! %Swaption-volatility structure
//    ! This abstract class defines the interface of concrete swaption
//        volatility structures which will be derived from this one.
//    
	public class SwaptionVolatilityStructure : VolatilityTermStructure
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
        // parameterless ctor is required for Handles
        public SwaptionVolatilityStructure() { }

		public SwaptionVolatilityStructure(Calendar cal, BusinessDayConvention bdc) : this(cal, bdc, new DayCounter())
		{
		}
		public SwaptionVolatilityStructure(Calendar cal, BusinessDayConvention bdc, DayCounter dc) : base(cal, bdc, dc)
		{
		}
		//! initialize with a fixed reference date
		public SwaptionVolatilityStructure(Date referenceDate, Calendar calendar, BusinessDayConvention bdc) : this(referenceDate, calendar, bdc, new DayCounter())
		{
		}
		public SwaptionVolatilityStructure(Date referenceDate, Calendar calendar, BusinessDayConvention bdc, DayCounter dc) : base(referenceDate, calendar, bdc, dc)
		{
		}
		//! calculate the reference date based on the global evaluation date
		public SwaptionVolatilityStructure(int settlementDays, Calendar calendar, BusinessDayConvention bdc) : this(settlementDays, calendar, bdc, new DayCounter())
		{
		}
		public SwaptionVolatilityStructure(int settlementDays, Calendar calendar, BusinessDayConvention bdc, DayCounter dc) : base(settlementDays, calendar, bdc, dc)
		{
		}
		//! \name Volatility, variance and smile
		//@{
		//! returns the volatility for a given option tenor and swap tenor

		// inline definitions
	
		// 1. methods with Period-denominated exercise convert Period to Date and then
		//    use the equivalent Date-denominated exercise methods
		public double volatility(Period optionTenor, Period swapTenor, double strike)
		{
			return volatility(optionTenor, swapTenor, strike, false);
		}
		public double volatility(Period optionTenor, Period swapTenor, double strike, bool extrapolate)
		{
			Date optionDate = optionDateFromTenor(optionTenor);
			return volatility(optionDate, swapTenor, strike, extrapolate);
		}
		//! returns the volatility for a given option date and swap tenor

		// 3. relying on xxxImpl methods
		public double volatility(Date optionDate, Period swapTenor, double strike)
		{
            return volatility(optionDate, swapTenor, strike, false);
		}
		public double volatility(Date optionDate, Period swapTenor, double strike, bool extrapolate)
		{
			checkSwapTenor(swapTenor, extrapolate);
			checkRange(optionDate, extrapolate);
			checkStrike(strike, extrapolate);
			return volatilityImpl(optionDate, swapTenor, strike);
		}
		//! returns the volatility for a given option time and swap tenor
		public double volatility(double optionTime, Period swapTenor, double strike)
		{
            return volatility(optionTime, swapTenor, strike, false);
		}
		public double volatility(double optionTime, Period swapTenor, double strike, bool extrapolate)
		{
			checkSwapTenor(swapTenor, extrapolate);
			checkRange(optionTime, extrapolate);
			checkStrike(strike, extrapolate);
			double length = swapLength(swapTenor);
			return volatilityImpl(optionTime, length, strike);
		}
		//! returns the volatility for a given option tenor and swap length
		public double volatility(Period optionTenor, double swapLength, double strike)
		{
            return volatility(optionTenor, swapLength, strike, false);
		}
		public double volatility(Period optionTenor, double swapLength, double strike, bool extrapolate)
		{
			Date optionDate = optionDateFromTenor(optionTenor);
			return volatility(optionDate, swapLength, strike, extrapolate);
		}
		//! returns the volatility for a given option date and swap length
		public double volatility(Date optionDate, double swapLength, double strike)
		{
            return volatility(optionDate, swapLength, strike, false);
		}
		public double volatility(Date optionDate, double swapLength, double strike, bool extrapolate)
		{
			checkSwapTenor(swapLength, extrapolate);
			checkRange(optionDate, extrapolate);
			checkStrike(strike, extrapolate);
			double optionTime = timeFromReference(optionDate);
			return volatilityImpl(optionTime, swapLength, strike);
		}
		//! returns the volatility for a given option time and swap length
		public double volatility(double optionTime, double swapLength, double strike)
		{
            return volatility(optionTime, swapLength, strike, false);
		}
		public double volatility(double optionTime, double swapLength, double strike, bool extrapolate)
		{
			checkSwapTenor(swapLength, extrapolate);
			checkRange(optionTime, extrapolate);
			checkStrike(strike, extrapolate);
			return volatilityImpl(optionTime, swapLength, strike);
		}

		//! returns the Black variance for a given option tenor and swap tenor
		public double blackVariance(Period optionTenor, Period swapTenor, double strike)
		{
            return blackVariance(optionTenor, swapTenor, strike, false);
		}
		public double blackVariance(Period optionTenor, Period swapTenor, double strike, bool extrapolate)
		{
			Date optionDate = optionDateFromTenor(optionTenor);
			return blackVariance(optionDate, swapTenor, strike, extrapolate);
		}
		//! returns the Black variance for a given option date and swap tenor

		// 2. blackVariance methods rely on volatility methods
		public double blackVariance(Date optionDate, Period swapTenor, double strike)
		{
            return blackVariance(optionDate, swapTenor, strike, false);
		}
		public double blackVariance(Date optionDate, Period swapTenor, double strike, bool extrapolate)
		{
			double v = volatility(optionDate, swapTenor, strike, extrapolate);
			double optionTime = timeFromReference(optionDate);
			return v *v *optionTime;
		}
		//! returns the Black variance for a given option time and swap tenor
		public double blackVariance(double optionTime, Period swapTenor, double strike)
		{
            return blackVariance(optionTime, swapTenor, strike, false);
		}
		public double blackVariance(double optionTime, Period swapTenor, double strike, bool extrapolate)
		{
			double v = volatility(optionTime, swapTenor, strike, extrapolate);
			return v *v *optionTime;
		}
		//! returns the Black variance for a given option tenor and swap length
		public double blackVariance(Period optionTenor, double swapLength, double strike)
		{
            return blackVariance(optionTenor, swapLength, strike, false);
		}
		public double blackVariance(Period optionTenor, double swapLength, double strike, bool extrapolate)
		{
			Date optionDate = optionDateFromTenor(optionTenor);
			return blackVariance(optionDate, swapLength, strike, extrapolate);
		}
		//! returns the Black variance for a given option date and swap length
		public double blackVariance(Date optionDate, double swapLength, double strike)
		{
            return blackVariance(optionDate, swapLength, strike, false);
		}
		public double blackVariance(Date optionDate, double swapLength, double strike, bool extrapolate)
		{
			double v = volatility(optionDate, swapLength, strike, extrapolate);
			double optionTime = timeFromReference(optionDate);
			return v *v *optionTime;
		}
		//! returns the Black variance for a given option time and swap length
		public double blackVariance(double optionTime, double swapLength, double strike)
		{
            return blackVariance(optionTime, swapLength, strike, false);
		}
		public double blackVariance(double optionTime, double swapLength, double strike, bool extrapolate)
		{
			double v = volatility(optionTime, swapLength, strike, extrapolate);
			return v *v *optionTime;
		}

		//! returns the smile for a given option tenor and swap tenor
		public SmileSection smileSection(Period optionTenor, Period swapTenor)
		{
            return smileSection(optionTenor, swapTenor, false);
		}
		public SmileSection smileSection(Period optionTenor, Period swapTenor, bool extrapolate)
		{
			Date optionDate = optionDateFromTenor(optionTenor);
			return smileSection(optionDate, swapTenor, extrapolate);
		}
		//! returns the smile for a given option date and swap tenor
		public SmileSection smileSection(Date optionDate, Period swapTenor)
		{
            return smileSection(optionDate, swapTenor, false);
		}
		public SmileSection smileSection(Date optionDate, Period swapTenor, bool extrapolate)
		{
			checkSwapTenor(swapTenor, extrapolate);
			checkRange(optionDate, extrapolate);
			return smileSectionImpl(optionDate, swapTenor);
		}

		public SmileSection smileSection(double optionTime, double swapLength)
		{
            return smileSection(optionTime, swapLength, false);
		}
		public SmileSection smileSection(double optionTime, double swapLength, bool extrapolate)
		{
			checkSwapTenor(swapLength, extrapolate);
			checkRange(optionTime, extrapolate);
			return smileSectionImpl(optionTime, swapLength);
		}
		//@}
		//! \name Limits
		//@{
		//! the largest length for which the term structure can return vols
//		public abstract Period maxSwapTenor();
        public virtual Period maxSwapTenor() { throw new NotSupportedException(); }

		//! the largest swapLength for which the term structure can return vols
		public double maxSwapLength()
		{
			return swapLength(maxSwapTenor());
		}
		//@}
		//! implements the conversion between swap tenor and swap (time) length
		public double swapLength(Period p)
		{
			if (!(p.length()>0))
                throw new ApplicationException("non-positive swap tenor (" + p + ") given");

	//         while using the reference date is arbitrary it is coherent between
	//           different swaption structures defined on the same reference date.
	//        
			Date start = referenceDate();
			Date end = start + p;
			return swapLength(start, end);
		}
		//! implements the conversion between swap dates and swap (time) length
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: double swapLength(const Date& start, Date& end)
		public double swapLength(Date start, Date end)
		{
			if (!(end>start))
                throw new ApplicationException("swap end date (" + end + ") must be greater than start (" + start + ")");

			double result = (end-start)/365.25 *24.0; // half a month unit
            Rounding roundingPrecision = new Rounding(0);
            result = roundingPrecision.Round(result);
			result /= 24.0; // year unit
			return result;
		}

		// 4. default implementation of Date-based xxxImpl methods
		//    relying on the equivalent double-based methods
		protected SmileSection smileSectionImpl(Date optionDate, Period swapT)
		{
			return smileSectionImpl(timeFromReference(optionDate), swapLength(swapT));
		}

        protected virtual SmileSection smileSectionImpl(double optionTime, double swapLength) { throw new NotSupportedException(); }

		protected double volatilityImpl(Date optionDate, Period swapTenor, double strike)
		{
			return volatilityImpl(timeFromReference(optionDate), swapLength(swapTenor), strike);
		}

        protected virtual double volatilityImpl(double optionTime, double swapLength, double strike) { throw new NotSupportedException(); }

		protected void checkSwapTenor(Period swapTenor, bool extrapolate)
		{
			if (!(swapTenor.length() > 0))
                throw new ApplicationException("non-positive swap tenor (" + swapTenor + ") given");

			if (!(extrapolate || allowsExtrapolation() || swapTenor <= maxSwapTenor()))
                throw new ApplicationException("swap tenor (" + swapTenor + ") is past max tenor (" + maxSwapTenor() + ")");
		}
		protected void checkSwapTenor(double swapLength, bool extrapolate)
		{
			if (!(swapLength > 0.0))
                throw new ApplicationException("non-positive swap length (" + swapLength + ") given");

			if (!(extrapolate || allowsExtrapolation() || swapLength <= maxSwapLength()))
                throw new ApplicationException("swap tenor (" + swapLength + ") is past max tenor (" + maxSwapLength() + ")");
		}
	}

}
