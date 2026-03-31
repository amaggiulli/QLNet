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
   /// zero inflation (i.e. CPI/RPI/HICP/etc.) volatility structures
   /// </summary>
   /// <remarks>
   /// Abstract interface. CPI volatility is always with respect to
   /// some base date.  Also deal with lagged observations of an index
   /// with a (usually different) availability lag.
   /// </remarks>
   public abstract class CPIVolatilitySurface : VolatilityTermStructure
   {
      /// <summary>
      /// Initializes the surface using a reference date derived from the global evaluation date.
      /// </summary>
      protected CPIVolatilitySurface(int settlementDays,
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

      // Volatility
      /// <summary>
      /// Returns the volatility for a given maturity date and strike.
      /// </summary>
      /// <remarks>
      /// By default, inflation is observed with the lag of the term structure. Because inflation is tightly linked to dates, time-based overloads are not provided.
      /// </remarks>
      double volatility(Date maturityDate, double strike,
                        Period obsLag = null,
                        bool extrapolate = false)
      {
         if (obsLag == null)
            obsLag = new Period(-1, TimeUnit.Days);

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

      /// <summary>
      /// Returns the volatility for a given option tenor and strike.
      /// </summary>
      public double? volatility(Period optionTenor, double strike,
                                Period obsLag = null, bool extrapolate = false)
      {
         if (obsLag == null)
            obsLag = new Period(-1, TimeUnit.Days);

         Date maturityDate = optionDateFromTenor(optionTenor);
         return volatility(maturityDate, strike, obsLag, extrapolate);
      }

      /// <summary>
      /// Returns the total integrated variance for a given exercise date and strike.
      /// </summary>
      /// <remarks>
      /// Total integrated variance is useful because it scales out time in optionlet pricing formulas. It is called "total" because the surface does not know whether it represents Black, Bachelier, or displaced-diffusion variance.
      /// </remarks>
      public virtual double totalVariance(Date exerciseDate,
                                          double strike,
                                          Period obsLag = null,
                                          bool extrapolate = false)
      {
         if (obsLag == null)
            obsLag = new Period(-1, TimeUnit.Days);

         double vol = volatility(exerciseDate, strike, obsLag, extrapolate);
         double t = timeFromBase(exerciseDate, obsLag);
         return vol * vol * t;
      }

      /// <summary>
      /// Returns the total integrated variance for a given option tenor and strike.
      /// </summary>
      public virtual double? totalVariance(Period optionTenor,
                                           double strike,
                                           Period obsLag = null,
                                           bool extrapolate = false)
      {
         if (obsLag == null)
            obsLag = new Period(-1, TimeUnit.Days);

         Date maturityDate = optionDateFromTenor(optionTenor);
         return totalVariance(maturityDate, strike, obsLag, extrapolate);
      }

      // Inspectors
      /// <summary>
      /// Returns the observation lag used by the surface.
      /// </summary>
      /// <remarks>
      /// This lag is usually different from the availability lag of the index. By default, inflation is provided for the requested maturity assuming this lag.
      /// </remarks>
      public virtual Period observationLag() { return observationLag_; }
      public virtual Frequency frequency()  { return frequency_; }
      public virtual bool indexIsInterpolated()  { return indexIsInterpolated_;}
      public virtual Date baseDate()
      {
         // Depends on interpolation, or not, of observed index
         // and observation lag with which it was built.
         // We want this to work even if the index does not
         // have a term structure.
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
      public virtual double timeFromBase(Date maturityDate, Period obsLag = null)
      {
         if (obsLag == null)
            obsLag = new Period(-1, TimeUnit.Days);

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
      public virtual double? baseLevel()
      {
         Utils.QL_REQUIRE(baseLevel_ != null, () => "Base volatility, for baseDate(), not set.");
         return baseLevel_;
      }

      protected virtual void checkRange(Date d, double strike, bool extrapolate)
      {
         Utils.QL_REQUIRE(d >= baseDate(), () =>
                          "date (" + d + ") is before base date");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || d <= maxDate(), () =>
                          "date (" + d + ") is past max curve date ("
                          + maxDate() + ")");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                          (strike >= minStrike() && strike <= maxStrike()), () =>
                          "strike (" + strike + ") is outside the curve domain ["
                          + minStrike() + "," + maxStrike() + "]] at date = " + d);
      }

      protected virtual void checkRange(double t, double strike, bool extrapolate)
      {
         Utils.QL_REQUIRE(t >= timeFromReference(baseDate()), () =>
                          "time (" + t + ") is before base date");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || t <= maxTime(), () =>
                          "time (" + t + ") is past max curve time ("
                          + maxTime() + ")");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                          (strike >= minStrike() && strike <= maxStrike()), () =>
                          "strike (" + strike + ") is outside the curve domain ["
                          + minStrike() + "," + maxStrike() + "] at time = " + t);
      }

      /// <summary>
      /// Implements the actual volatility-surface calculation in derived classes.
      /// </summary>
      protected abstract double volatilityImpl(double length, double strike);

      protected double? baseLevel_;
      // so you do not need an index
      protected Period observationLag_;
      protected Frequency frequency_;
      protected bool indexIsInterpolated_;
   }
}
