/*
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

using System.Collections.Generic;

namespace QLNet
{
   /// <summary>
   /// Callable-bond volatility structure
   /// </summary>
   /// <remarks>
   /// This class is purely abstract and defines the interface of
   /// concrete callable-bond volatility structures which will be
   /// derived from this one.
   /// </remarks>
   public abstract class CallableBondVolatilityStructure : TermStructure
   {
      /// <summary>
      /// default constructor
      /// </summary>
      /// <remarks>
      /// Warning: term structures initialized by means of this
      /// constructor must manage their own reference date
      /// by overriding the referenceDate() method.
      /// </remarks>

      protected CallableBondVolatilityStructure(DayCounter dc = null, BusinessDayConvention bdc = BusinessDayConvention.Following)
         : base(dc ?? new DayCounter())
      {
         bdc_ = bdc;
      }
      /// <summary>
      /// Initializes the structure with a fixed reference date.
      /// </summary>
      protected CallableBondVolatilityStructure(Date referenceDate, Calendar calendar = null, DayCounter dc = null,
                                                BusinessDayConvention bdc = BusinessDayConvention.Following)
         : base(referenceDate, calendar ?? new Calendar(), dc ?? new DayCounter())
      {
         bdc_ = bdc;
      }
      /// <summary>
      /// Initializes the structure using a reference date derived from the global evaluation date.
      /// </summary>
      protected CallableBondVolatilityStructure(int settlementDays, Calendar calendar, DayCounter dc = null,
                                                BusinessDayConvention bdc = BusinessDayConvention.Following)
         : base(settlementDays, calendar, dc ?? new DayCounter())
      {
         bdc_ = bdc;
      }
      /// <summary>
      /// Returns the volatility for the given option time and bond length.
      /// </summary>
      public double volatility(double optionTenor, double bondTenor, double strike, bool extrapolate = false)
      {
         checkRange(optionTenor, bondTenor, strike, extrapolate);
         return volatilityImpl(optionTenor, bondTenor, strike);
      }
      /// <summary>
      /// Returns the Black variance for the given option time and bond length.
      /// </summary>
      public double blackVariance(double optionTime, double bondLength, double strike, bool extrapolate = false)
      {
         checkRange(optionTime, bondLength, strike, extrapolate);
         double vol = volatilityImpl(optionTime, bondLength, strike);
         return vol * vol * optionTime;
      }
      /// <summary>
      /// Returns the volatility for the given option date and bond tenor.
      /// </summary>
      public double volatility(Date optionDate, Period bondTenor, double strike, bool extrapolate = false)
      {
         checkRange(optionDate, bondTenor, strike, extrapolate);
         return volatilityImpl(optionDate, bondTenor, strike);
      }
      /// <summary>
      /// Returns the Black variance for the given option date and bond tenor.
      /// </summary>
      public double blackVariance(Date optionDate, Period bondTenor, double strike, bool extrapolate = false)
      {
         double vol =  volatility(optionDate, bondTenor, strike, extrapolate);
         KeyValuePair<double, double> p = convertDates(optionDate, bondTenor);
         return vol * vol * p.Key;
      }
      public virtual SmileSection smileSection(Date optionDate, Period bondTenor)
      {
         KeyValuePair<double, double> p = convertDates(optionDate, bondTenor);
         return smileSectionImpl(p.Key, p.Value);
      }

      /// <summary>
      /// Returns the volatility for the given option tenor and bond tenor.
      /// </summary>
      public double volatility(Period optionTenor, Period bondTenor, double strike, bool extrapolate = false)
      {
         Date optionDate = optionDateFromTenor(optionTenor);
         return volatility(optionDate, bondTenor, strike, extrapolate);
      }
      /// <summary>
      /// Returns the Black variance for the given option tenor and bond tenor.
      /// </summary>
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
      // Limits
      /// <summary>
      /// Returns the largest bond tenor for which the structure can provide volatilities.
      /// </summary>
      public abstract Period maxBondTenor();
      /// <summary>
      /// Returns the largest bond length for which the structure can provide volatilities.
      /// </summary>
      public virtual double maxBondLength()
      {
         return timeFromReference(referenceDate() + maxBondTenor());
      }
      /// <summary>
      /// Returns the minimum strike supported by the structure.
      /// </summary>
      public abstract double minStrike();
      /// <summary>
      /// Returns the maximum strike supported by the structure.
      /// </summary>
      public abstract double maxStrike();

      /// <summary>
      /// Converts an option date and bond tenor into option time and bond length.
      /// </summary>
      public virtual KeyValuePair<double, double> convertDates(Date optionDate, Period bondTenor)
      {
         Date end = optionDate + bondTenor;
         Utils.QL_REQUIRE(end > optionDate, () =>
                          "negative bond tenor (" + bondTenor + ") given");
         double optionTime = timeFromReference(optionDate);
         double timeLength = dayCounter().yearFraction(optionDate, end);
         return new KeyValuePair<double, double>(optionTime, timeLength);
      }
      /// <summary>
      /// Returns the business-day convention used for option-date calculations.
      /// </summary>
      public virtual BusinessDayConvention businessDayConvention() { return bdc_; }
      /// <summary>
      /// Converts an option tenor into an option date.
      /// </summary>
      public Date optionDateFromTenor(Period optionTenor)
      {
         return calendar().advance(referenceDate(),
                                   optionTenor,
                                   businessDayConvention());
      }

      /// <summary>
      /// Returns the smile section for the given option time and bond length.
      /// </summary>
      protected abstract SmileSection smileSectionImpl(double optionTime, double bondLength);

      /// <summary>
      /// Implements the actual volatility calculation in derived classes.
      /// </summary>
      protected abstract double volatilityImpl(double optionTime, double bondLength, double strike);
      protected virtual double volatilityImpl(Date optionDate, Period bondTenor, double strike)
      {
         KeyValuePair<double, double> p = convertDates(optionDate, bondTenor);
         return volatilityImpl(p.Key, p.Value, strike);
      }
      protected void checkRange(double optionTime, double bondLength, double k, bool extrapolate)
      {
         base.checkRange(optionTime, extrapolate);
         Utils.QL_REQUIRE(bondLength >= 0.0, () =>
                          "negative bondLength (" + bondLength + ") given");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                          bondLength <= maxBondLength(), () =>
                          "bondLength (" + bondLength + ") is past max curve bondLength ("
                          + maxBondLength() + ")");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                          (k >= minStrike() && k <= maxStrike()), () =>
                          "strike (" + k + ") is outside the curve domain ["
                          + minStrike() + "," + maxStrike() + "]");
      }
      protected void checkRange(Date optionDate, Period bondTenor, double k, bool extrapolate)
      {
         base.checkRange(timeFromReference(optionDate),
                         extrapolate);
         Utils.QL_REQUIRE(bondTenor.length() > 0, () =>
                          "negative bond tenor (" + bondTenor + ") given");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                          bondTenor <= maxBondTenor(), () =>
                          "bond tenor (" + bondTenor + ") is past max tenor ("
                          + maxBondTenor() + ")");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                          (k >= minStrike() && k <= maxStrike()), () =>
                          "strike (" + k + ") is outside the curve domain ["
                          + minStrike() + "," + maxStrike() + "]");
      }

      private BusinessDayConvention bdc_;
   }
}
