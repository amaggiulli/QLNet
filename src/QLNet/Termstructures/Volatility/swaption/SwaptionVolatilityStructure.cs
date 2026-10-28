/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

namespace QLNet
{
   /// <summary>
   /// Swaption-volatility structure
   /// </summary>
   /// <remarks>
   /// This abstract class defines the interface of concrete swaption
   /// volatility structures which will be derived from this one.
   /// </remarks>
   public abstract class SwaptionVolatilityStructure : VolatilityTermStructure
   {
      #region Constructors
      /// <remarks>
      /// Warning: term structures initialized by means of this
      /// constructor must manage their own reference date
      /// by overriding the referenceDate() method.
      /// </remarks>

      protected SwaptionVolatilityStructure(BusinessDayConvention bdc, DayCounter dc = null)
         : base(bdc, dc) {}

      /// <summary>
      /// Initializes the structure with a fixed reference date.
      /// </summary>
      protected SwaptionVolatilityStructure(Date referenceDate, Calendar calendar, BusinessDayConvention bdc, DayCounter dc = null)
         : base(referenceDate, calendar, bdc, dc) {}

      /// <summary>
      /// Initializes the structure using a reference date derived from the global evaluation date.
      /// </summary>
      protected SwaptionVolatilityStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null)
         : base(settlementDays, cal, bdc, dc) {}

      #endregion


      #region Volatility, variance and smile

      /// <summary>
      /// Returns the volatility for the given option tenor and swap tenor.
      /// </summary>
      public double volatility(Period optionTenor, Period swapTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return volatility(optionDate, swapTenor, strike, extrapolate);
      }

      /// <summary>
      /// Returns the volatility for the given option date and swap tenor.
      /// </summary>
      public double volatility(Date optionDate, Period swapTenor, double strike, bool extrapolate = false)
      {
         checkSwapTenor(swapTenor, extrapolate);
         checkRange(optionDate, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(optionDate, swapTenor, strike);
      }

      /// <summary>
      /// Returns the volatility for the given option time and swap tenor.
      /// </summary>
      public double volatility(double optionTime, Period swapTenor, double strike, bool extrapolate = false)
      {
         checkSwapTenor(swapTenor, extrapolate);
         checkRange(optionTime, extrapolate);
         checkStrike(strike, extrapolate);
         double length = swapLength(swapTenor);
         return volatilityImpl(optionTime, length, strike);
      }

      /// <summary>
      /// Returns the volatility for the given option tenor and swap length.
      /// </summary>
      public double volatility(Period optionTenor, double swapLength, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return volatility(optionDate, swapLength, strike, extrapolate);
      }

      /// <summary>
      /// Returns the volatility for the given option date and swap length.
      /// </summary>
      public double volatility(Date optionDate, double swapLength, double strike, bool extrapolate = false)
      {
         checkSwapTenor(swapLength, extrapolate);
         checkRange(optionDate, extrapolate);
         checkStrike(strike, extrapolate);
         double optionTime = timeFromReference(optionDate);
         return volatilityImpl(optionTime, swapLength, strike);
      }

      /// <summary>
      /// Returns the volatility for the given option time and swap length.
      /// </summary>
      public double volatility(double optionTime, double swapLength, double strike, bool extrapolate = false)
      {
         checkSwapTenor(swapLength, extrapolate);
         checkRange(optionTime, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(optionTime, swapLength, strike);
      }

      /// <summary>
      /// Returns the Black variance for the given option tenor and swap tenor.
      /// </summary>
      public double blackVariance(Period optionTenor, Period swapTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return blackVariance(optionDate, swapTenor, strike, extrapolate);
      }

      /// <summary>
      /// Returns the Black variance for the given option date and swap tenor.
      /// </summary>
      public double blackVariance(Date optionDate, Period swapTenor, double strike, bool extrapolate = false)
      {
         double v = volatility(optionDate, swapTenor, strike, extrapolate);
         double optionTime = timeFromReference(optionDate);
         return v * v * optionTime;
      }

      /// <summary>
      /// Returns the Black variance for the given option time and swap tenor.
      /// </summary>
      public double blackVariance(double optionTime, Period swapTenor, double strike, bool extrapolate = false)
      {
         double v = volatility(optionTime, swapTenor, strike, extrapolate);
         return v * v * optionTime;
      }

      /// <summary>
      /// Returns the Black variance for the given option tenor and swap length.
      /// </summary>
      public double blackVariance(Period optionTenor, double swapLength, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return blackVariance(optionDate, swapLength, strike, extrapolate);
      }

      /// <summary>
      /// Returns the Black variance for the given option date and swap length.
      /// </summary>
      public double blackVariance(Date optionDate, double swapLength, double strike, bool extrapolate = false)
      {
         double v = volatility(optionDate, swapLength, strike, extrapolate);
         double optionTime = timeFromReference(optionDate);
         return v * v * optionTime;
      }

      /// <summary>
      /// Returns the Black variance for the given option time and swap length.
      /// </summary>
      public double blackVariance(double optionTime, double swapLength, double strike, bool extrapolate = false)
      {
         double v = volatility(optionTime, swapLength, strike, extrapolate);
         return v * v * optionTime;
      }


      /// <summary>
      /// Returns the shift for the given option tenor and swap tenor.
      /// </summary>
      public double shift(Period optionTenor, Period swapTenor, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return shift(optionDate, swapTenor, extrapolate);
      }
      /// <summary>
      /// Returns the shift for the given option date and swap tenor.
      /// </summary>
      public double shift(Date optionDate, Period swapTenor, bool extrapolate = false)
      {
         checkSwapTenor(swapTenor, extrapolate);
         checkRange(optionDate, extrapolate);
         return shiftImpl(optionDate, swapTenor);
      }
      /// <summary>
      /// Returns the shift for the given option time and swap tenor.
      /// </summary>
      public double shift(double optionTime, Period swapTenor, bool extrapolate = false)
      {
         checkSwapTenor(swapTenor, extrapolate);
         checkRange(optionTime, extrapolate);
         double length = swapLength(swapTenor);
         return shiftImpl(optionTime, length);
      }
      /// <summary>
      /// Returns the shift for the given option tenor and swap length.
      /// </summary>
      public double shift(Period optionTenor, double swapLength, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return shift(optionDate, swapLength, extrapolate);
      }
      /// <summary>
      /// Returns the shift for the given option date and swap length.
      /// </summary>
      public double shift(Date optionDate, double swapLength, bool extrapolate = false)
      {
         checkSwapTenor(swapLength, extrapolate);
         checkRange(optionDate, extrapolate);
         double optionTime = timeFromReference(optionDate);
         return shiftImpl(optionTime, swapLength);
      }
      /// <summary>
      /// Returns the shift for the given option time and swap length.
      /// </summary>
      public double shift(double optionTime, double swapLength, bool extrapolate = false)
      {
         checkSwapTenor(swapLength, extrapolate);
         checkRange(optionTime, extrapolate);
         return shiftImpl(optionTime, swapLength);
      }



      /// <summary>
      /// Returns the smile section for the given option tenor and swap tenor.
      /// </summary>
      public SmileSection smileSection(Period optionTenor, Period swapTenor, bool extr = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return smileSection(optionDate, swapTenor, extrapolate);
      }

      /// <summary>
      /// Returns the smile section for the given option date and swap tenor.
      /// </summary>
      public SmileSection smileSection(Date optionDate, Period swapTenor, bool extr = false)
      {
         checkSwapTenor(swapTenor, extrapolate);
         checkRange(optionDate, extrapolate);
         return smileSectionImpl(optionDate, swapTenor);
      }

      /// <summary>
      /// Returns the smile section for the given option time and swap length.
      /// </summary>
      public SmileSection smileSection(double optionTime, double swapLength, bool extr = false)
      {
         checkSwapTenor(swapLength, extrapolate);
         checkRange(optionTime, extrapolate);
         return smileSectionImpl(optionTime, swapLength);
      }

      #endregion

      #region Limits

      /// <summary>
      /// Returns the largest swap tenor for which the structure can provide volatilities.
      /// </summary>
      public abstract Period maxSwapTenor();

      /// <summary>
      /// Returns the largest swap length for which the structure can provide volatilities.
      /// </summary>
      public double maxSwapLength() { return swapLength(maxSwapTenor()); }

      #endregion

      /// <summary>
      /// Returns the volatility type.
      /// </summary>
      public virtual VolatilityType volatilityType() { return VolatilityType.ShiftedLognormal; }

      /// <summary>
      /// Converts a swap tenor into a swap length.
      /// </summary>
      public double swapLength(Period swapTenor)
      {
         Utils.QL_REQUIRE(swapTenor.length() > 0, () => "non-positive swap tenor (" + swapTenor + ") given");
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

      /// <summary>
      /// Converts swap start and end dates into a swap length.
      /// </summary>
      public double swapLength(Date start, Date end)
      {
         Utils.QL_REQUIRE(end > start, () => "swap end date (" + end + ") must be greater than start (" + start + ")");
         double result = (end - start) / 365.25 * 12.0; // month unit
         result = new ClosestRounding(0).Round(result);
         result /= 12.0; // year unit
         return result;
      }

      protected virtual SmileSection smileSectionImpl(Date optionDate, Period swapTenor)
      {
         return smileSectionImpl(timeFromReference(optionDate), swapLength(swapTenor));
      }

      protected abstract SmileSection smileSectionImpl(double optionTime, double swapLength);

      protected virtual double volatilityImpl(Date optionDate, Period swapTenor, double strike)
      {
         return volatilityImpl(timeFromReference(optionDate), swapLength(swapTenor),  strike);
      }

      protected abstract double volatilityImpl(double optionTime, double swapLength, double strike);

      protected virtual double shiftImpl(Date optionDate, Period swapTenor)
      {
         return shiftImpl(timeFromReference(optionDate), swapLength(swapTenor));
      }
      protected virtual double shiftImpl(double optionTime, double swapLength)
      {
         Utils.QL_REQUIRE(volatilityType() == VolatilityType.ShiftedLognormal, () =>
                          "shift parameter only makes sense for lognormal volatilities");
         return 0.0;
      }

      protected void checkSwapTenor(Period swapTenor, bool extrapolate)
      {
         Utils.QL_REQUIRE(swapTenor.length() > 0, () => "non-positive swap tenor (" + swapTenor + ") given");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || swapTenor <= maxSwapTenor(), () =>
                          "swap tenor (" + swapTenor + ") is past max tenor (" + maxSwapTenor() + ")");
      }

      protected void checkSwapTenor(double swapLength, bool extrapolate)
      {
         Utils.QL_REQUIRE(swapLength > 0.0, () => "non-positive swap length (" + swapLength + ") given");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || swapLength <= maxSwapLength(), () =>
                          "swap tenor (" + swapLength + ") is past max tenor ("  + maxSwapLength() + ")");
      }

   }
}
