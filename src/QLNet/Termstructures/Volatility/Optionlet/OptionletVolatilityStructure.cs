/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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
   /// Optionlet (caplet/floorlet) volatility structure
   /// </summary>
   /// <remarks>
   /// This class is purely abstract and defines the interface of
   /// concrete structures which will be derived from this one.
   /// </remarks>
   public abstract class OptionletVolatilityStructure : VolatilityTermStructure
   {
      #region Constructors
      /// <summary>
      /// default constructor
      /// </summary>
      /// <remarks>
      /// Warning: term structures initialized by means of this
      /// constructor must manage their own reference date
      /// by overriding the referenceDate() method.
      /// </remarks>

      protected OptionletVolatilityStructure(BusinessDayConvention bdc = BusinessDayConvention.Following,
                                             DayCounter dc = null)
         : base(bdc, dc) {}

      /// <summary>
      /// Initializes the structure with a fixed reference date.
      /// </summary>
      protected OptionletVolatilityStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null)
         : base(referenceDate, cal, bdc, dc) {}

      /// <summary>
      /// Initializes the structure using a reference date derived from the global evaluation date.
      /// </summary>
      protected OptionletVolatilityStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null)
         : base(settlementDays, cal, bdc, dc) {}

      #endregion

      #region Volatility and Variance

      /// <summary>
      /// Returns the volatility for the given option tenor and strike.
      /// </summary>
      public double volatility(Period optionTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return volatility(optionDate, strike, extrapolate);
      }

      /// <summary>
      /// Returns the volatility for the given option date and strike.
      /// </summary>
      public double volatility(Date optionDate, double strike, bool extrapolate = false)
      {
         checkRange(optionDate, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(optionDate, strike);
      }

      /// <summary>
      /// Returns the volatility for the given option time and strike.
      /// </summary>
      public double volatility(double optionTime, double strike, bool extrapolate = false)
      {
         checkRange(optionTime, extrapolate);
         checkStrike(strike, extrapolate);
         return volatilityImpl(optionTime, strike);
      }

      /// <summary>
      /// Returns the Black variance for the given option tenor and strike.
      /// </summary>
      public double blackVariance(Period optionTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return blackVariance(optionDate, strike, extrapolate);
      }

      /// <summary>
      /// Returns the Black variance for the given option date and strike.
      /// </summary>
      public double blackVariance(Date optionDate, double strike, bool extrapolate = false)
      {
         double v = volatility(optionDate, strike, extrapolate);
         double t = timeFromReference(optionDate);
         return v * v * t;
      }

      /// <summary>
      /// Returns the Black variance for the given option time and strike.
      /// </summary>
      public double blackVariance(double optionTime,  double strike,  bool extrapolate = false)
      {
         double v = volatility(optionTime, strike, extrapolate);
         return v * v * optionTime;
      }

      /// <summary>
      /// Returns the smile section for the given option tenor.
      /// </summary>
      public SmileSection smileSection(Period optionTenor, bool extr = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return smileSection(optionDate, extrapolate);
      }

      /// <summary>
      /// Returns the smile section for the given option date.
      /// </summary>
      public SmileSection smileSection(Date optionDate, bool extr = false)
      {
         checkRange(optionDate, extrapolate);
         return smileSectionImpl(optionDate);
      }

      /// <summary>
      /// Returns the smile section for the given option time.
      /// </summary>
      public SmileSection smileSection(double optionTime,  bool extr = false)
      {
         checkRange(optionTime, extrapolate);
         return smileSectionImpl(optionTime);
      }

      #endregion

      public virtual double displacement() {return 0.0;}
      public virtual VolatilityType volatilityType() {return VolatilityType.ShiftedLognormal;}

      protected virtual SmileSection smileSectionImpl(Date optionDate)
      {
         return smileSectionImpl(timeFromReference(optionDate));
      }

      /// <summary>
      /// Implements the actual smile-section calculation in derived classes.
      /// </summary>
      protected abstract SmileSection smileSectionImpl(double optionTime);

      protected double volatilityImpl(Date optionDate, double strike)
      {
         return volatilityImpl(timeFromReference(optionDate), strike);
      }

      /// <summary>
      /// Implements the actual volatility calculation in derived classes.
      /// </summary>
      protected abstract double volatilityImpl(double optionTime, double strike);


   }

}
