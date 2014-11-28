/*
 Copyright (C) 2008, 2009 , 2010, 2011  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
    //! zero inflation (i.e. CPI/RPI/HICP/etc.) volatility structures
    /*! Abstract interface. CPI volatility is always with respect to
        some base date.  Also deal with lagged observations of an index
        with a (usually different) availability lag.
    */
    public class CPIVolatilitySurface : VolatilityTermStructure
    {
        public CPIVolatilitySurface()
          : base(BusinessDayConvention.Following, null) { }

        /*! calculates the reference date based on the global
            evaluation date.
        */
        public CPIVolatilitySurface(int settlementDays,
                                    Calendar cal,
                                    BusinessDayConvention bdc,
                                    DayCounter dc,
                                    Period observationLag,
                                    Frequency frequency,
                                    bool indexIsInterpolated)
            :base(settlementDays, cal, bdc, dc)
        {
            baseLevel_ = null;
            observationLag_ = observationLag;
            frequency_ = frequency;
            indexIsInterpolated_ = indexIsInterpolated;

        }
        
        //! \name Volatility
        /*! by default, inflation is observed with the lag
            of the term structure.

            Because inflation is highly linked to dates (for
            interpolation, periods, etc) time-based overload of the
            methods are not provided.
        */

        //@{
        //! Returns the volatility for a given maturity date and strike rate.
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

        //! returns the volatility for a given option tenor and strike rate
        public double? volatility(Period optionTenor, double strike,
                           Period obsLag = null, bool extrapolate = false)
        {
            if (obsLag == null) obsLag = new Period(-1, TimeUnit.Days);

            Date maturityDate = optionDateFromTenor(optionTenor);
            return volatility(maturityDate, strike, obsLag, extrapolate);
        }

        //! Returns the total integrated variance for a given exercise
        //! date and strike rate.
        /*! Total integrated variance is useful because it scales out
            t for the optionlet pricing formulae.  Note that it is
            called "total" because the surface does not know whether
            it represents Black, Bachelier or Displaced Diffusion
            variance.  These are virtual so alternate connections
            between const vol and total var are possible.
        */
        public virtual double totalVariance(Date exerciseDate,
                                             double strike,
                                             Period obsLag = null,
                                             bool extrapolate = false)
        {
            if (obsLag == null) obsLag = new Period(-1, TimeUnit.Days);

            double vol = volatility(exerciseDate, strike, obsLag, extrapolate);
            double t = timeFromBase(exerciseDate, obsLag);
            return vol * vol * t;
        }

        //! returns the total integrated variance for a given option
        //! tenor and strike rate.
        public virtual double? totalVariance(Period optionTenor,
                                             double strike,
                                             Period obsLag = null,
                                             bool extrapolate = false)
        {
            if (obsLag == null) obsLag = new Period(-1, TimeUnit.Days);

            Date maturityDate = optionDateFromTenor(optionTenor);
            return totalVariance(maturityDate, strike, obsLag, extrapolate);
        }
        //@}

        //! \name Inspectors
        //@{
        /*! The term structure observes with a lag that is usually
            different from the availability lag of the index.  An
            inflation rate is given, by default, for the maturity
            requested assuming this lag.
        */
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
        //! base date will be in the past because of observation lag
        public virtual double timeFromBase(Date maturityDate, Period obsLag = null)
        {
            if (obsLag == null) obsLag = new Period(-1, TimeUnit.Days);

            Period useLag = obsLag;

            if (obsLag== new Period(-1,TimeUnit.Days)) 
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
            if(baseLevel_ == null )
                throw new ApplicationException("Base volatility, for baseDate(), not set.");
            return baseLevel_;
        }
        //@}

        //! \name Limits
        //@{
        //! the minimum strike for which the term structure can return vols
        public override double minStrike() { throw new NotSupportedException(); }
        //! the maximum strike for which the term structure can return vols
        public override double maxStrike() { throw new NotSupportedException(); }
        //@}

        protected virtual void checkRange(Date d, double strike, bool extrapolate)
        {
           Utils.QL_REQUIRE( d >= baseDate(), () =>
                 "date (" + d + ") is before base date");
           Utils.QL_REQUIRE( extrapolate || allowsExtrapolation() || d <= maxDate(), () =>
                      "date (" + d + ") is past max curve date ("
                      + maxDate() + ")");
           Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                      ( strike >= minStrike() && strike <= maxStrike() ), () =>
                      "strike (" + strike + ") is outside the curve domain ["
                      + minStrike() + "," + maxStrike() + "]] at date = " + d);
        }

        protected virtual void checkRange(double t, double strike, bool extrapolate)
        {
           Utils.QL_REQUIRE( t >= timeFromReference( baseDate() ), () =>
                  "time (" + t + ") is before base date");
           Utils.QL_REQUIRE( extrapolate || allowsExtrapolation() || t <= maxTime(), () =>
                      "time (" + t + ") is past max curve time ("
                      + maxTime() + ")");
           Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() ||
                      ( strike >= minStrike() && strike <= maxStrike() ), () =>
                      "strike (" + strike + ") is outside the curve domain ["
                      + minStrike() + "," + maxStrike() + "] at time = " + t);
        }

        /*! Implements the actual volatility surface calculation in
            derived classes e.g. bilinear interpolation.  N.B. does
            not derive the surface.
        */
        protected virtual double volatilityImpl(double length, double strike) { throw new NotSupportedException(); }

        protected double? baseLevel_;
        // so you do not need an index
        protected Period observationLag_;
        protected Frequency frequency_;
        protected bool indexIsInterpolated_;
    }
}
