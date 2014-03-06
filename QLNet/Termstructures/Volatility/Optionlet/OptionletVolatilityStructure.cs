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

namespace QLNet 
{
   //! Optionlet (caplet/floorlet) volatility structure
   /*! This class is purely abstract and defines the interface of
      concrete structures which will be derived from this one.
   */
   public class OptionletVolatilityStructure : VolatilityTermStructure 
   {
      #region Constructors
      //! default constructor
      /*! \warning term structures initialized by means of this
                   constructor must manage their own reference date
                   by overriding the referenceDate() method.
      */
      public OptionletVolatilityStructure()
         : base(BusinessDayConvention.Following, null) { }

      public OptionletVolatilityStructure(BusinessDayConvention bdc = BusinessDayConvention.Following,
         DayCounter dc = null)
         : base(bdc, dc) {}

      //! initialize with a fixed reference date
      public OptionletVolatilityStructure(Date referenceDate,Calendar cal,BusinessDayConvention bdc,DayCounter dc = null)
         : base(referenceDate, cal, bdc, dc) {}
      
      //! calculate the reference date based on the global evaluation date
      public OptionletVolatilityStructure(int settlementDays,Calendar cal,BusinessDayConvention bdc,DayCounter dc = null)
         : base(settlementDays, cal, bdc, dc) {}
      
      #endregion

      #region Volatility and Variance

      //! returns the volatility for a given option tenor and strike rate
      public double volatility(Period optionTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return volatility(optionDate, strike, extrapolate);
      }
      
      //! returns the volatility for a given option date and strike rate
      public double volatility(Date optionDate, double strike, bool extrapolate = false)
      {
         checkRange(optionDate, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(optionDate, strike);
      }
      
      //! returns the volatility for a given option time and strike rate
      public double volatility(double optionTime, double strike, bool extrapolate = false)
      {
         checkRange(optionTime, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(optionTime, strike);
      }

      //! returns the Black variance for a given option tenor and strike rate
      public double blackVariance(Period optionTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return blackVariance(optionDate, strike, extrapolate);
      }

      //! returns the Black variance for a given option date and strike rate
      public double blackVariance(Date optionDate, double strike, bool extrapolate = false)
      {
         double v = volatility(optionDate, strike, extrapolate);
         double t = timeFromReference(optionDate);
         return v*v*t;
      }

      //! returns the Black variance for a given option time and strike rate
      public double blackVariance(double optionTime,  double strike,  bool extrapolate = false)
      {
         double v = volatility(optionTime, strike, extrapolate);
         return v*v*optionTime;
      }

      //! returns the smile for a given option tenor
      public SmileSection smileSection( Period optionTenor, bool extr = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return smileSection(optionDate, extrapolate);
      }

      //! returns the smile for a given option date
      public SmileSection smileSection(Date optionDate,bool extr = false)
      {
         checkRange(optionDate, extrapolate);
         return smileSectionImpl(optionDate);
      }

      //! returns the smile for a given option time
      public SmileSection smileSection(double optionTime,  bool extr = false)
      {
         checkRange(optionTime, extrapolate);
         return smileSectionImpl(optionTime);
      }
      
      #endregion
      
      protected virtual SmileSection smileSectionImpl(Date optionDate)
      {
          return smileSectionImpl(timeFromReference(optionDate));
      }

      //! implements the actual smile calculation in derived classes
      protected virtual SmileSection smileSectionImpl(double optionTime)  { throw new NotImplementedException(); }

      protected double volatilityImpl(Date optionDate, double strike)
      {
         return volatilityImpl(timeFromReference(optionDate), strike);
      }

      //! implements the actual volatility calculation in derived classes
      protected virtual double volatilityImpl(double optionTime, double strike) { throw new NotImplementedException(); }
   
   }
   
}
