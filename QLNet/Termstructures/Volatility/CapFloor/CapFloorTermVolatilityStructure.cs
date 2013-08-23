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
   public class CapFloorTermVolatilityStructure : VolatilityTermStructure 
   {
      /*! \name Constructors
            See the TermStructure documentation for issues regarding
            constructors.
        */
        //@{
        //! default constructor
        /*! \warning term structures initialized by means of this
                     constructor must manage their own reference date
                     by overriding the referenceDate() method.
        */
      public CapFloorTermVolatilityStructure(Calendar cal, BusinessDayConvention bdc)
         : this(cal, bdc, new DayCounter()) { }

      public CapFloorTermVolatilityStructure(Calendar cal, BusinessDayConvention bdc, DayCounter dc)
         : base (cal, bdc, dc) {}

      //! initialize with a fixed reference date
      public CapFloorTermVolatilityStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc)
         : this(referenceDate, cal, bdc, new DayCounter()) { }

      public CapFloorTermVolatilityStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc,
                                      DayCounter dc)
         : base(referenceDate, cal, bdc, dc) { }


      //! calculate the reference date based on the global evaluation date
      public CapFloorTermVolatilityStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc)
         : this(settlementDays, cal, bdc, new DayCounter()) { }

      public CapFloorTermVolatilityStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc,
                                       DayCounter dc)
         : base(settlementDays, cal, bdc, dc) {}


      #region "Volatility"

      //! returns the volatility for a given cap/floor length and strike rate
      public double volatility(Period length, double strike)
      {
         return volatility(length, strike, false);
      }

      public double volatility(Period length, double strike, bool extrapolate) 
      {
         Date d = optionDateFromTenor(length);
         return volatility(d, strike, extrapolate);
      }

      public double volatility(Date end, double strike)
      {
         return volatility(end, strike, false);
      }
      public double volatility(Date end, double strike, bool extrapolate)
      {
         checkRange(end, extrapolate);
         double t = timeFromReference(end);
         return volatility(t, strike, extrapolate);
      }
       
      //! returns the volatility for a given end time and strike rate
      public double volatility(double t, double strike)
      {
         return volatility(t, strike, false);
      }
      public double volatility(double t, double strike, bool extrapolate)
      {
         checkRange(t, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(t, strike);
      }

      #endregion

      //! implements the actual volatility calculation in derived classes
      protected virtual double volatilityImpl(double length, double strike)
      {
         throw new NotImplementedException("CapFloorVolatility");
      }


   }
}
