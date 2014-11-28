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

namespace QLNet 
{
   //! %Swaption-volatility structure
   /*! This abstract class defines the interface of concrete swaption
      volatility structures which will be derived from this one.
   */
   public class SwaptionVolatilityStructure : VolatilityTermStructure 
   {
      #region Constructors
      /*! \warning term structures initialized by means of this
                   constructor must manage their own reference date
                   by overriding the referenceDate() method.
      */
      public SwaptionVolatilityStructure()
         : base(BusinessDayConvention.Following, null) { }

      public SwaptionVolatilityStructure(BusinessDayConvention bdc, DayCounter dc = null)
         : base(bdc, dc) {}
      
      //! initialize with a fixed reference date
      public SwaptionVolatilityStructure(Date referenceDate,Calendar calendar,BusinessDayConvention bdc,DayCounter dc = null)
         : base(referenceDate, calendar, bdc, dc) {}
      
      //! calculate the reference date based on the global evaluation date
      public SwaptionVolatilityStructure(int settlementDays,Calendar cal,BusinessDayConvention bdc,DayCounter dc = null)
         : base(settlementDays, cal, bdc, dc) {}
      
      #endregion


      #region Volatility, variance and smile

      //! returns the volatility for a given option tenor and swap tenor
      public double volatility(Period optionTenor, Period swapTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return volatility(optionDate, swapTenor, strike, extrapolate);
      }
      
      //! returns the volatility for a given option date and swap tenor
      public double volatility(Date optionDate, Period swapTenor, double strike, bool extrapolate = false)
      {
         checkSwapTenor(swapTenor, extrapolate);
         checkRange(optionDate, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(optionDate, swapTenor, strike);
      }

      //! returns the volatility for a given option time and swap tenor
      public double volatility(double optionTime, Period swapTenor, double strike, bool extrapolate = false)
      {
         checkSwapTenor(swapTenor, extrapolate);
         checkRange(optionTime, extrapolate);
         checkStrike(strike, extrapolate);
         double length = swapLength(swapTenor);
         return volatilityImpl(optionTime, length, strike);
      }

      //! returns the volatility for a given option tenor and swap length
      public double volatility(Period optionTenor, double swapLength, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return volatility(optionDate, swapLength, strike, extrapolate);
      }

      //! returns the volatility for a given option date and swap length
      public double volatility(Date optionDate, double swapLength, double strike, bool extrapolate = false)
      {
         checkSwapTenor(swapLength, extrapolate);
         checkRange(optionDate, extrapolate);
         checkStrike(strike, extrapolate);
         double optionTime = timeFromReference(optionDate);
         return volatilityImpl(optionTime, swapLength, strike);
      }

      //! returns the volatility for a given option time and swap length
      public double volatility(double optionTime, double swapLength, double strike, bool extrapolate = false)
      {
         checkSwapTenor(swapLength, extrapolate);
         checkRange(optionTime, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(optionTime, swapLength, strike);
      }

      //! returns the Black variance for a given option tenor and swap tenor
      public double blackVariance(Period optionTenor, Period swapTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return blackVariance(optionDate, swapTenor, strike, extrapolate);
      }

      //! returns the Black variance for a given option date and swap tenor
      public double blackVariance(Date optionDate, Period swapTenor, double strike, bool extrapolate = false)
      {
         double v = volatility(optionDate, swapTenor, strike, extrapolate);
         double optionTime = timeFromReference(optionDate);
         return v * v * optionTime;
      }

      //! returns the Black variance for a given option time and swap tenor
      public double blackVariance(double optionTime, Period swapTenor, double strike, bool extrapolate = false)
      {
         double v = volatility(optionTime, swapTenor, strike, extrapolate);
         return v * v * optionTime;
      }

      //! returns the Black variance for a given option tenor and swap length
      public double blackVariance(Period optionTenor, double swapLength, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return blackVariance(optionDate, swapLength, strike, extrapolate);
      }

      //! returns the Black variance for a given option date and swap length
      public double blackVariance(Date optionDate, double swapLength, double strike, bool extrapolate = false)
      {
         double v = volatility(optionDate, swapLength, strike, extrapolate);
         double optionTime = timeFromReference(optionDate);
         return v * v * optionTime;
      }

      //! returns the Black variance for a given option time and swap length
      public double blackVariance(double optionTime, double swapLength, double strike, bool extrapolate = false)
      {
         double v = volatility(optionTime, swapLength, strike, extrapolate);
         return v * v * optionTime;
      }

      //! returns the smile for a given option tenor and swap tenor
      public SmileSection smileSection(Period optionTenor, Period swapTenor, bool extr = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return smileSection(optionDate, swapTenor, extrapolate);
      }

      //! returns the smile for a given option date and swap tenor
      public SmileSection smileSection(Date optionDate, Period swapTenor, bool extr = false)
      {
         checkSwapTenor(swapTenor, extrapolate);
         checkRange(optionDate, extrapolate);
         return smileSectionImpl(optionDate, swapTenor);
      }

      //! returns the smile for a given option time and swap tenor
      //public SmileSection smileSection(double optionTime, Period swapTenor, bool extr = false);

      //! returns the smile for a given option tenor and swap length
      //public SmileSection smileSection(Period optionTenor, double swapLength, bool extr = false);


      //! returns the smile for a given option date and swap length
      //public SmileSection smileSection( Date optionDate, double swapLength, bool extr = false) ;

      //! returns the smile for a given option time and swap length
      public SmileSection smileSection(double optionTime, double swapLength, bool extr = false)
      {
         checkSwapTenor(swapLength, extrapolate);
         checkRange(optionTime, extrapolate);
         return smileSectionImpl(optionTime, swapLength);
      }
      
      #endregion

      #region Limits

      //! the largest length for which the term structure can return vols
      public virtual  Period maxSwapTenor()  { throw new NotSupportedException(); }

      //! the largest swapLength for which the term structure can return vols
      public double maxSwapLength() { return swapLength(maxSwapTenor()); }

      #endregion

      //! implements the conversion between swap tenor and swap (time) length
      public double swapLength(Period swapTenor)
      {
         Utils.QL_REQUIRE( swapTenor.length() > 0, () => "non-positive swap tenor (" + swapTenor + ") given" );
         switch (swapTenor.units())
         {
            case TimeUnit.Months:
               return swapTenor.length() / 12.0;
            case TimeUnit.Years:
               return swapTenor.length();
            default:
               Utils.QL_FAIL("invalid Time Unit (" + swapTenor.units() + ") for swap length");
               return 0;
         }
      }

      //! implements the conversion between swap dates and swap (time) length
      public double swapLength(Date start, Date end)
      {
         Utils.QL_REQUIRE( end > start, () => "swap end date (" + end + ") must be greater than start (" + start + ")" );
         double result = (end - start) / 365.25 * 12.0; // month unit
         result = new ClosestRounding(0).Round(result);
         result /= 12.0; // year unit
         return result;
      }

      protected virtual SmileSection smileSectionImpl(Date optionDate, Period swapTenor)
      {
         return smileSectionImpl(timeFromReference(optionDate), swapLength(swapTenor));
      }

      protected virtual SmileSection smileSectionImpl(double optionTime, double swapLength)   { throw new NotSupportedException(); }

      protected virtual double volatilityImpl(Date optionDate, Period swapTenor, double strike)
      {
         return volatilityImpl(timeFromReference(optionDate), swapLength(swapTenor),  strike);
      }

      protected virtual double volatilityImpl(double optionTime, double swapLength, double strike) { throw new NotSupportedException(); }

      protected void checkSwapTenor(Period swapTenor, bool extrapolate)
      {
         Utils.QL_REQUIRE( swapTenor.length() > 0, () => "non-positive swap tenor (" + swapTenor + ") given" );
         Utils.QL_REQUIRE( extrapolate || allowsExtrapolation() || swapTenor <= maxSwapTenor(), () =>
                    "swap tenor (" + swapTenor + ") is past max tenor (" + maxSwapTenor() + ")");
      }

      protected void checkSwapTenor(double swapLength, bool extrapolate)
      {
         Utils.QL_REQUIRE( swapLength > 0.0, () => "non-positive swap length (" + swapLength + ") given" );
         Utils.QL_REQUIRE( extrapolate || allowsExtrapolation() || swapLength <= maxSwapLength(), () =>
                    "swap tenor (" + swapLength + ") is past max tenor ("  + maxSwapLength() + ")");
      }

    }
}
