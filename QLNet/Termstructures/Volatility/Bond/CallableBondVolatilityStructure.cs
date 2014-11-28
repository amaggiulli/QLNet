/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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

namespace QLNet
{
   //! Callable-bond volatility structure
   /*! This class is purely abstract and defines the interface of
       concrete callable-bond volatility structures which will be
       derived from this one.
   */
   public class CallableBondVolatilityStructure : TermStructure
   {
      /*! \name Constructors
         See the TermStructure documentation for issues regarding
         constructors.
      */
      public CallableBondVolatilityStructure()
         : base(new DayCounter())
      {
         bdc_ = BusinessDayConvention.Following;
      }
      //@{
      //! default constructor
      /*! \warning term structures initialized by means of this
                  constructor must manage their own reference date
                  by overriding the referenceDate() method.
      */
      public CallableBondVolatilityStructure(DayCounter dc = null, BusinessDayConvention bdc = BusinessDayConvention.Following)
         : base(dc == null ? new DayCounter() : dc)
      {
         bdc_ = bdc;
      }
      //! initialize with a fixed reference date
      public CallableBondVolatilityStructure( Date referenceDate, Calendar calendar = null, DayCounter dc = null,
                                              BusinessDayConvention bdc = BusinessDayConvention.Following)
         : base(referenceDate, calendar == null ? new Calendar() : calendar, dc == null ? new DayCounter() : dc)
      {
         bdc_ = bdc;
      }
      //! calculate the reference date based on the global evaluation date
      public CallableBondVolatilityStructure(int settlementDays, Calendar calendar, DayCounter dc = null,
                                              BusinessDayConvention bdc = BusinessDayConvention.Following)
         : base(settlementDays, calendar, dc == null ? new DayCounter() : dc)
      {
         bdc_ = bdc;
      }
      //@}
      //public virtual ~CallableBondVolatilityStructure() {}
      //! \name Volatility, variance and smile
      //@{
      //! returns the volatility for a given option time and bondLength
      public double volatility(double optionTenor, double bondTenor, double strike, bool extrapolate = false)
      {
         checkRange(optionTenor, bondTenor, strike, extrapolate);
         return volatilityImpl(optionTenor, bondTenor, strike);
      }
      //! returns the Black variance for a given option time and bondLength
      public double blackVariance(double optionTime, double bondLength, double strike, bool extrapolate = false)
      {
         checkRange(optionTime, bondLength, strike, extrapolate);
         double vol = volatilityImpl(optionTime, bondLength, strike);
         return vol * vol * optionTime;
      }
      //! returns the volatility for a given option date and bond tenor
      public double volatility(Date optionDate, Period bondTenor, double strike, bool extrapolate = false)
      {
         checkRange(optionDate, bondTenor, strike, extrapolate);
         return volatilityImpl(optionDate, bondTenor, strike);
      }
      //! returns the Black variance for a given option date and bond tenor
      public double blackVariance(Date optionDate, Period bondTenor, double strike, bool extrapolate = false)
      {
         double vol =  volatility(optionDate, bondTenor, strike, extrapolate);
         KeyValuePair<double, double> p = convertDates(optionDate, bondTenor);
         return vol*vol*p.Key;
      }
      public virtual SmileSection smileSection(Date optionDate, Period bondTenor)  
      {
         KeyValuePair<double, double> p = convertDates(optionDate, bondTenor);
         return smileSectionImpl(p.Key, p.Value);
      }

      //! returns the volatility for a given option tenor and bond tenor
      public double volatility(Period optionTenor, Period bondTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return volatility(optionDate, bondTenor, strike, extrapolate);
      }
      //! returns the Black variance for a given option tenor and bond tenor
      public double blackVariance(Period optionTenor, Period bondTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         double vol = volatility(optionDate, bondTenor, strike, extrapolate);
         KeyValuePair<double, double> p = convertDates(optionDate, bondTenor);
         return vol * vol * p.Key;
      }
      public SmileSection smileSection(Period optionTenor, Period bondTenor)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return smileSection(optionDate, bondTenor);
      }
      //@}
      //! \name Limits
      //@{
      //! the largest length for which the term structure can return vols
      public virtual Period maxBondTenor() {throw new ApplicationException("maxBondTenor need implementation");}
      //! the largest bondLength for which the term structure can return vols
      public virtual double maxBondLength()
      {
         return timeFromReference(referenceDate() + maxBondTenor());
      }
      //! the minimum strike for which the term structure can return vols
      public virtual double minStrike() { throw new ApplicationException("minStrike need implementation"); }
      //! the maximum strike for which the term structure can return vols
      public virtual double maxStrike() { throw new ApplicationException("maxStrike need implementation"); }
      //@}
      //! implements the conversion between dates and times
      public virtual KeyValuePair<double, double> convertDates(Date optionDate, Period bondTenor)
      {
         Date end = optionDate + bondTenor;
         Utils.QL_REQUIRE( end > optionDate, () =>
                   "negative bond tenor (" + bondTenor + ") given");
        double optionTime = timeFromReference(optionDate);
        double timeLength = dayCounter().yearFraction(optionDate, end);
        return new KeyValuePair<double,double>(optionTime, timeLength);
      }
      //! the business day convention used for option date calculation
      public virtual BusinessDayConvention businessDayConvention() { return bdc_; }
      //! implements the conversion between optionTenors and optionDates
      public Date optionDateFromTenor(Period optionTenor)
      {
         return calendar().advance(referenceDate(),
                                  optionTenor,
                                  businessDayConvention());
      }

      //! return smile section
      protected virtual SmileSection smileSectionImpl( double optionTime, double bondLength)
      { throw new ApplicationException("smileSectionImpl need implementation"); }

      //! implements the actual volatility calculation in derived classes
      protected virtual double volatilityImpl(double optionTime, double bondLength, double strike)
      { throw new ApplicationException("volatilityImpl need implementation"); }
      protected virtual double volatilityImpl(Date optionDate, Period bondTenor, double strike) 
      {
         KeyValuePair<double, double> p = convertDates(optionDate, bondTenor);
         return volatilityImpl(p.Key, p.Value, strike);
      }
      protected void checkRange(double optionTime, double bondLength, double k, bool extrapolate)
      {
         base.checkRange(optionTime, extrapolate);
         Utils.QL_REQUIRE( bondLength >= 0.0, () =>
                   "negative bondLength (" + bondLength + ") given");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                   bondLength <= maxBondLength(), () =>
                   "bondLength (" + bondLength + ") is past max curve bondLength ("
                   + maxBondLength() + ")");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                   ( k >= minStrike() && k <= maxStrike() ), () =>
                   "strike (" + k + ") is outside the curve domain ["
                   + minStrike() + "," + maxStrike()+ "]");
      }
      protected void checkRange(Date optionDate, Period bondTenor, double k, bool extrapolate)
      {
         base.checkRange(timeFromReference(optionDate),
                                  extrapolate);
         Utils.QL_REQUIRE( bondTenor.length() > 0, () =>
                   "negative bond tenor (" + bondTenor + ") given");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                   bondTenor <= maxBondTenor(), () =>
                   "bond tenor (" + bondTenor + ") is past max tenor ("
                   + maxBondTenor() + ")");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                   ( k >= minStrike() && k <= maxStrike() ), () =>
                   "strike (" + k + ") is outside the curve domain ["
                   + minStrike() + "," + maxStrike()+ "]");
      }

      private BusinessDayConvention bdc_;
   }
}
