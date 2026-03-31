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
   /// Abstract interface ... no data, only results.
   /// Basically used to change the BlackVariance() methods to
   /// totalVariance.  Also deal with lagged observations of an index
   /// with a (usually different) availability lag.
   /// </summary>

   public abstract class YoYOptionletVolatilitySurface :  VolatilityTermStructure
   {
      // Constructor
      /// <summary>
      /// Initializes the surface using a reference date derived from the global evaluation date.
      /// </summary>
      protected YoYOptionletVolatilitySurface(int settlementDays,
                                              Calendar cal,
                                              BusinessDayConvention bdc,
                                              DayCounter dc,
                                              Period observationLag,
                                              Frequency frequency,
                                              bool indexIsInterpolated)
         : base(settlementDays, cal, bdc, dc)
      {
         baseLevel_ = null;
         observationLag_ = observationLag;
         frequency_ = frequency;
         indexIsInterpolated_ = indexIsInterpolated;

      }


      // Volatility (only)
      /// <summary>
      /// Returns the volatility for a given maturity date and strike.
      /// </summary>
      /// <remarks>
      /// By default, inflation is observed with the lag of the term structure. Because inflation is tightly linked to dates, no time-based overload is provided.
      /// </remarks>
      public double volatility(Date maturityDate, double strike)
      {
         return volatility(maturityDate, strike, new Period(-1, TimeUnit.Days), false);
      }
      public double volatility(Date maturityDate, double strike, Period obsLag)
      {
         return volatility(maturityDate, strike, obsLag, false) ;
      }

      public double volatility(Date maturityDate, double strike, Period obsLag, bool extrapolate)
      {
         Period useLag = obsLag;
         if (obsLag == new Period(-1, TimeUnit.Days))
         {
            useLag = observationLag();
         }

         if (indexIsInterpolated())
         {
            checkRange(maturityDate - useLag, strike, extrapolate);
            double t = timeFromReference(maturityDate - useLag);
            return volatilityImpl(t, strike);
         }
         else
         {
            KeyValuePair<Date, Date> dd = Utils.inflationPeriod(maturityDate - useLag, frequency());
            checkRange(dd.Key, strike, extrapolate);
            double t = timeFromReference(dd.Key);
            return volatilityImpl(t, strike);
         }
      }

      public double volatility(Period optionTenor, double strike)
      {
         Date maturityDate = optionDateFromTenor(optionTenor);
         return volatility(maturityDate, strike, new Period(-1, TimeUnit.Days), false);
      }

      public double volatility(Period optionTenor, double strike, Period obsLag)
      {
         Date maturityDate = optionDateFromTenor(optionTenor);
         return volatility(maturityDate, strike, obsLag, false);
      }

      public double volatility(Period optionTenor, double strike, Period obsLag, bool extrapolate)
      {
         Date maturityDate = optionDateFromTenor(optionTenor);
         return volatility(maturityDate, strike, obsLag, extrapolate);
      }

      /// <summary>
      /// Returns the total integrated variance for a given exercise date and strike.
      /// </summary>
      /// <remarks>
      /// Total integrated variance is useful because it scales out time in optionlet pricing formulas. It is called "total" because the surface does not know whether it represents Black, Bachelier, or displaced-diffusion variance. Because inflation is tightly linked to dates, no time-based overload is provided.
      /// </remarks>
      public virtual double totalVariance(Date maturityDate, double strike)
      {
         return totalVariance(maturityDate, strike, new Period(-1, TimeUnit.Days), false);
      }
      public virtual double totalVariance(Date maturityDate, double strike, Period obsLag)
      {
         return totalVariance(maturityDate, strike, obsLag, false);
      }
      public virtual double totalVariance(Date maturityDate, double strike, Period obsLag, bool extrapolate)
      {
         double vol = volatility(maturityDate, strike, obsLag, extrapolate);
         double t = timeFromBase(maturityDate, obsLag);
         return vol * vol * t;
      }

      public virtual double totalVariance(Period tenor, double strike)
      {
         Date maturityDate = optionDateFromTenor(tenor);
         return totalVariance(maturityDate, strike, new Period(-1, TimeUnit.Days), false);
      }
      public virtual double totalVariance(Period tenor, double strike, Period obsLag)
      {
         Date maturityDate = optionDateFromTenor(tenor);
         return totalVariance(maturityDate, strike, obsLag, false);
      }
      public virtual double totalVariance(Period tenor, double strike, Period obsLag, bool extrap)
      {
         Date maturityDate = optionDateFromTenor(tenor);
         return totalVariance(maturityDate, strike, obsLag, extrap);
      }

      /// <summary>
      /// Returns the observation lag used by the surface.
      /// </summary>
      /// <remarks>
      /// This lag is usually different from the availability lag of the index. By default, inflation is provided for the requested maturity assuming this lag.
      /// </remarks>
      public virtual Period observationLag()  { return observationLag_; }
      public virtual Frequency frequency() { return frequency_; }
      public virtual bool indexIsInterpolated() { return indexIsInterpolated_; }

      public virtual Date baseDate()
      {

         // Depends on interpolation, or not, of observed index
         // and observation lag with which it was built.
         // We want this to work even if the index does not
         // have a yoy term structure.
         if (indexIsInterpolated())
         {
            return referenceDate() - observationLag();
         }
         else
         {
            return Utils.inflationPeriod(referenceDate() - observationLag(),
                                         frequency()).Key;
         }
      }

      /// <summary>
      /// Returns the time from the base date to the given maturity.
      /// </summary>
      /// <remarks>
      /// The base date is typically in the past because of the observation lag.
      /// </remarks>
      public virtual double timeFromBase(Date maturityDate)
      {
         return timeFromBase(maturityDate, new Period(-1, TimeUnit.Days));
      }

      /// <summary>
      /// Returns the time from the base date to the given maturity using the specified observation lag.
      /// </summary>
      public virtual double timeFromBase(Date maturityDate, Period obsLag)
      {

         Period useLag = obsLag;
         if (obsLag == new Period(-1, TimeUnit.Days))
         {
            useLag = observationLag();
         }

         Date useDate;
         if (indexIsInterpolated())
         {
            useDate = maturityDate - useLag;
         }
         else
         {
            useDate = Utils.inflationPeriod(maturityDate - useLag, frequency()).Key;
         }

         // This assumes that the inflation term structure starts
         // as late as possible given the inflation index definition,
         // which is the usual case.
         return dayCounter().yearFraction(baseDate(), useDate);
      }

      // acts as zero time value for boostrapping
      public virtual double baseLevel()
      {
         Utils.QL_REQUIRE(baseLevel_ != null, () => "Base volatility, for baseDate(), not set.");
         return baseLevel_.Value;
      }


      protected virtual void checkRange(Date d, double strike, bool extrapolate)
      {

         Utils.QL_REQUIRE(d >= baseDate(), () => "date (" + d + ") is before base date");

         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || d <= maxDate(), () =>
                          "date (" + d + ") is past max curve date (" + maxDate() + ")");


         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || (strike >= minStrike() && strike <= maxStrike()), () =>
                          "strike (" + strike + ") is outside the curve domain [" + minStrike() + "," + maxStrike() + "]] at date = " + d);
      }

      protected virtual void checkRange(double t, double strike, bool extrapolate)
      {
         Utils.QL_REQUIRE(t >= timeFromReference(baseDate()), () => "time (" + t + ") is before base date");

         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || t <= maxTime(), () =>
                          "time (" + t + ") is past max curve time (" + maxTime() + ")");

         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || (strike >= minStrike() && strike <= maxStrike()), () =>
                          "strike (" + strike + ") is outside the curve domain [" + minStrike() + "," + maxStrike() + "] at time = " + t);

      }


      /// <summary>
      /// Implements the actual volatility-surface calculation in derived classes.
      /// </summary>
      protected abstract double volatilityImpl(double length, double strike);


      // acts as zero time value for boostrapping
      protected virtual void setBaseLevel(double? v) { baseLevel_ = v; }
      protected double? baseLevel_;

      // so you do not need an index
      protected Period observationLag_;
      protected Frequency frequency_;
      protected bool indexIsInterpolated_;
   }

   /// <summary>
   /// Constant surface, no K or T dependence.
   /// </summary>
   public class ConstantYoYOptionletVolatility  : YoYOptionletVolatilitySurface
   {

      // Constructor
      /// <summary>
      /// Initializes the constant surface using a reference date derived from the global evaluation date.
      /// </summary>
      public ConstantYoYOptionletVolatility(double v,
                                            int settlementDays,
                                            Calendar cal,
                                            BusinessDayConvention bdc,
                                            DayCounter dc,
                                            Period observationLag,
                                            Frequency frequency,
                                            bool indexIsInterpolated,
                                            double minStrike = -1.0,  // -100%
                                            double maxStrike = 100.0)  // +10,000%
         : base(settlementDays, cal, bdc, dc, observationLag, frequency, indexIsInterpolated)
      {
         volatility_ = v;
         minStrike_ = minStrike;
         maxStrike_ = maxStrike;
      }

      // Limits
      public override Date maxDate()  { return Date.maxDate(); }
      /// <summary>
      /// Returns the minimum strike supported by the structure.
      /// </summary>
      public override double minStrike() { return minStrike_; }
      /// <summary>
      /// Returns the maximum strike supported by the structure.
      /// </summary>
      public override double maxStrike() { return maxStrike_; }

      /// <summary>
      /// Implements the actual volatility calculation in derived classes.
      /// </summary>
      protected override double volatilityImpl(double length, double strike)
      {
         return volatility_;
      }

      protected double volatility_;
      protected double minStrike_, maxStrike_;
   }

}
