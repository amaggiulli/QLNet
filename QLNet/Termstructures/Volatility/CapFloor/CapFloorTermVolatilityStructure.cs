/*
 Copyright (C) 2008 Andrea Maggiulli
  
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
   //! Cap/floor term-volatility structure
   /*! This class is purely abstract and defines the interface of concrete
       structures which will be derived from this one.
   */
   public class CapFloorTermVolatilityStructure : VolatilityTermStructure 
   {
      #region Constructors
      /*! \warning term structures initialized by means of this
                   constructor must manage their own reference date
                   by overriding the referenceDate() method.
      */
      public CapFloorTermVolatilityStructure(BusinessDayConvention bdc, DayCounter dc = null)
         :base(bdc, dc) {}
      
      //! initialize with a fixed reference date
      public CapFloorTermVolatilityStructure(Date referenceDate,Calendar cal,BusinessDayConvention bdc,DayCounter dc = null)
         : base(referenceDate, cal, bdc, dc) {}
      
      //! calculate the reference date based on the global evaluation date
      public CapFloorTermVolatilityStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null)
         : base(settlementDays, cal, bdc, dc) {}
      
      #endregion
      
      #region Volatility

      //! returns the volatility for a given cap/floor length and strike rate
      public double volatility(Period length, double strike, bool extrapolate = false)
      {
         Date d = optionDateFromTenor(length);
         return volatility(d, strike, extrapolate);
      }

      public double volatility(Date end, double strike, bool extrapolate = false)
      {
         checkRange(end, extrapolate);
         double t = timeFromReference(end);
         return volatility(t, strike, extrapolate);
      }
      
      //! returns the volatility for a given end time and strike rate
      public double volatility(double t, double strike, bool extrapolate = false)
      {
         checkRange(t, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(t, strike);
      }

      #endregion
      
      //! implements the actual volatility calculation in derived classes
      protected virtual double volatilityImpl(double length,  double strike) { throw new NotSupportedException(); }
    }
}
