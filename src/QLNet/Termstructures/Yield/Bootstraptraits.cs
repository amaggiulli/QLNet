/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2014 Edem Dawui (edawui@gmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet
{
	public interface ITraits<T> where T : TermStructure
	{
	  Date initialDate( T c );		                                                        // start of curve data
	  double initialValue( T c );                                                             // value at reference date
      double guess<C>(int i, C c, bool validData, int first) where C : Curve<T>;              // possible constraints based on previous values
      double minValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<T>;
      double maxValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<T>;      // update with new guess
      void updateGuess( List<double> data, double discount, int i );
	  int maxIterations();                                                                    // upper bound for convergence loop
	}

    public static class ITraitsYieldTermStructure
    {
        public static YieldTermStructure factory<Interpolator>(this ITraits<YieldTermStructure> self,
                                                               DayCounter dayCounter,
                                                               List<Handle<Quote>> jumps = null,
                                                               List<Date> jumpDates = null,
                                                               Interpolator interpolator = default(Interpolator))
            where Interpolator : class, IInterpolationFactory, new()
        {
            if (self.GetType().Equals(typeof(Discount)))
                return new InterpolatedDiscountCurve<Interpolator>(dayCounter, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ZeroYield)))
                return new InterpolatedZeroCurve<Interpolator>(dayCounter, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ForwardRate)))
                return new InterpolatedForwardCurve<Interpolator>(dayCounter, jumps, jumpDates, interpolator);
            else
                return null;
        }

        public static YieldTermStructure factory<Interpolator>(this ITraits<YieldTermStructure> self,
                                                               Date referenceDate,
                                                               DayCounter dayCounter,
                                                               List<Handle<Quote>> jumps = null,
                                                               List<Date> jumpDates = null,
                                                               Interpolator interpolator = default(Interpolator))
            where Interpolator : class, IInterpolationFactory, new()
        {
            if (self.GetType().Equals(typeof(Discount)))
                return new InterpolatedDiscountCurve<Interpolator>(referenceDate, dayCounter, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ZeroYield)))
                return new InterpolatedZeroCurve<Interpolator>(referenceDate, dayCounter, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ForwardRate)))
                return new InterpolatedForwardCurve<Interpolator>(referenceDate, dayCounter, jumps, jumpDates, interpolator);
            else
                return null;
        }

        public static YieldTermStructure factory<Interpolator>(this ITraits<YieldTermStructure> self,
                                                               int settlementDays,
                                                               Calendar calendar,
                                                               DayCounter dayCounter,
                                                               List<Handle<Quote>> jumps = null,
                                                               List<Date> jumpDates = null,
                                                               Interpolator interpolator = default(Interpolator))
            where Interpolator : class, IInterpolationFactory, new()
        {
            if (self.GetType().Equals(typeof(Discount)))
                return new InterpolatedDiscountCurve<Interpolator>(settlementDays, calendar, dayCounter, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ZeroYield)))
                return new InterpolatedZeroCurve<Interpolator>(settlementDays, calendar, dayCounter, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ForwardRate)))
                return new InterpolatedForwardCurve<Interpolator>(settlementDays, calendar, dayCounter, jumps, jumpDates, interpolator);
            else
                return null;
        }

        public static YieldTermStructure factory<Interpolator>(this ITraits<YieldTermStructure> self,
                                                               List<Date> dates,
                                                               List<double> discounts,
                                                               DayCounter dayCounter,
                                                               Calendar calendar = null,
                                                               List<Handle<Quote>> jumps = null,
                                                               List<Date> jumpDates = null,
                                                               Interpolator interpolator = default(Interpolator))
            where Interpolator : class, IInterpolationFactory, new()
        {
            if (self.GetType().Equals(typeof(Discount)))
                return new InterpolatedDiscountCurve<Interpolator>(dates, discounts, dayCounter, calendar, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ZeroYield)))
                return new InterpolatedZeroCurve<Interpolator>(dates, discounts, dayCounter, calendar, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ForwardRate)))
                return new InterpolatedForwardCurve<Interpolator>(dates, discounts, dayCounter, calendar, jumps, jumpDates, interpolator);
            else
                return null;
        }

        public static YieldTermStructure factory<Interpolator>(this ITraits<YieldTermStructure> self,
                                                               List<Date> dates,
                                                               List<double> discounts,
                                                               DayCounter dayCounter,
                                                               Calendar calendar,
                                                               Interpolator interpolator)
            where Interpolator : class, IInterpolationFactory, new()
        {
            if (self.GetType().Equals(typeof(Discount)))
                return new InterpolatedDiscountCurve<Interpolator>(dates, discounts, dayCounter, calendar, interpolator);
            else if (self.GetType().Equals(typeof(ZeroYield)))
                return new InterpolatedZeroCurve<Interpolator>(dates, discounts, dayCounter, calendar, interpolator);
            else if (self.GetType().Equals(typeof(ForwardRate)))
                return new InterpolatedForwardCurve<Interpolator>(dates, discounts, dayCounter, calendar, interpolator);
            else
                return null;
        }

        public static YieldTermStructure factory<Interpolator>(this ITraits<YieldTermStructure> self,
                                                               List<Date> dates,
                                                               List<double> discounts,
                                                               DayCounter dayCounter,
                                                               Interpolator interpolator)
            where Interpolator : class, IInterpolationFactory, new()
        {
            if (self.GetType().Equals(typeof(Discount)))
                return new InterpolatedDiscountCurve<Interpolator>(dates, discounts, dayCounter, interpolator);
            else if (self.GetType().Equals(typeof(ZeroYield)))
                return new InterpolatedZeroCurve<Interpolator>(dates, discounts, dayCounter, interpolator);
            else if (self.GetType().Equals(typeof(ForwardRate)))
                return new InterpolatedForwardCurve<Interpolator>(dates, discounts, dayCounter, interpolator);
            else
                return null;
        }

        public static YieldTermStructure factory<Interpolator>(this ITraits<YieldTermStructure> self,
                                                                List<Date> dates,
                                                                List<double> discounts,
                                                                DayCounter dayCounter,
                                                                List<Handle<Quote>> jumps,
                                                                List<Date> jumpDates,
                                                                Interpolator interpolator = default(Interpolator))
            where Interpolator : class, IInterpolationFactory, new()
        {
            if (self.GetType().Equals(typeof(Discount)))
                return new InterpolatedDiscountCurve<Interpolator>(dates, discounts, dayCounter, jumps, jumpDates, interpolator);
            else if (self.GetType().Equals(typeof(ForwardRate)))
                return new InterpolatedForwardCurve<Interpolator>(dates, discounts, dayCounter, jumps, jumpDates, interpolator);
            else
                return null;
        }

    }

	public class Discount : ITraits<YieldTermStructure>
	{
		const double maxRate = 1;
		const double avgRate = 0.05;
		public Date initialDate( YieldTermStructure c ) { return c.referenceDate(); }           // start of curve data
		public double initialValue( YieldTermStructure c ) { return 1; }                        // value at reference date
		// update with new guess
		public void updateGuess( List<double> data, double discount, int i ) { data[i] = discount; }
		public int maxIterations() { return 100; }                                              // upper bound for convergence loop

        public double guess<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{
			if ( validData ) // previous iteration value
				return c.data()[i];

			if ( i == 1 ) // first pillar
				return 1.0 / ( 1.0 + avgRate * c.times()[1] );

			// flat rate extrapolation
			double r = -System.Math.Log( c.data()[i - 1] ) / c.times()[i - 1];
			return System.Math.Exp( -r * c.times()[i] );
		}
        public double minValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{
			if ( validData )
			{
				#if QL_NEGATIVE_RATES
					return c.data().Min() / 2.0;
				#else
					return c.data().Last() / 2.0;
				#endif
			}
			double dt = c.times()[i] - c.times()[i - 1];
			return c.data()[i - 1] * System.Math.Exp( -maxRate * dt );
		}
        public double maxValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{

#if QL_NEGATIVE_RATES
            double dt = c.times()[i] - c.times()[i-1];
            return c.data()[i-1] * Math.Exp(maxRate * dt);
#else
			// discounts cannot increase
			return c.data()[i - 1];
#endif

		}
 }

	//! Zero-curve traits
	public class ZeroYield : ITraits<YieldTermStructure>
	{
		const double maxRate = 3;
		const double avgRate = 0.05;

		public Date initialDate( YieldTermStructure c ) { return c.referenceDate(); }   // start of curve data
		public double initialValue( YieldTermStructure c ) { return avgRate; }    // value at reference date
		// update with new guess
		public void updateGuess( List<double> data, double rate, int i )
		{
			data[i] = rate;
			if ( i == 1 )
				data[0] = rate; // first point is updated as well
		}
		public int maxIterations() { return 100; }   // upper bound for convergence loop

        public double guess<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{

			if ( validData ) // previous iteration value
				return c.data()[i];

			if ( i == 1 ) // first pillar
				return avgRate;

			// extrapolate
            Date d = c.dates()[i];
			return (c as YieldTermStructure).zeroRate( d, (c as YieldTermStructure).dayCounter(), Compounding.Continuous, Frequency.Annual, true ).value();
		}
        public double minValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{
			if ( validData )
			{
				double r = c.data().Min();
#if QL_NEGATIVE_RATES

                return r<0.0 ? r*2.0 : r/2.0;
#else
				return r / 2.0;
#endif
			}
#if QL_NEGATIVE_RATES

            // no constraints.
            // We choose as min a value very unlikely to be exceeded.
            return -maxRate;
#else
			return Const.QL_EPSILON;
#endif
		}
        public double maxValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{


			if ( validData )
			{
				double r = c.data().Max();
#if QL_NEGATIVE_RATES
                return r<0.0 ? r/2.0 : r*2.0;
#else
				return r * 2.0;
#endif
			}
			return maxRate;
		}
   }

	//! Forward-curve traits
	public class ForwardRate : ITraits<YieldTermStructure>
	{
		const double maxRate = 3;
		const double avgRate = 0.05;

		public Date initialDate( YieldTermStructure c ) { return c.referenceDate(); }   // start of curve data
		public double initialValue( YieldTermStructure c ) { return avgRate; } // dummy value at reference date
		// update with new guess
		public void updateGuess( List<double> data, double forward, int i )
		{
			data[i] = forward;
			if ( i == 1 )
				data[0] = forward; // first point is updated as well
		}
		// upper bound for convergence loop
		public int maxIterations() { return 100; }

        public double guess<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{
			if ( validData ) // previous iteration value
				return c.data()[i];

			if ( i == 1 ) // first pillar
				return avgRate;

			// extrapolate
            Date d = c.dates()[i];
			return (c as YieldTermStructure).forwardRate( d, d, (c as YieldTermStructure).dayCounter(), Compounding.Continuous, Frequency.Annual, true ).value();
		}

        public double minValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{
			if ( validData )
			{
				double r = c.data().Min();
#if QL_NEGATIVE_RATES
                return r<0.0 ? r*2.0 : r/2.0;
#else
				return r / 2.0;
#endif
			}
#if QL_NEGATIVE_RATES
            // no constraints.
            // We choose as min a value very unlikely to be exceeded.
            return -maxRate;
#else
			return Const.QL_EPSILON;
#endif
		}

        public double maxValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<YieldTermStructure>
		{
			if ( validData )
			{
				double r = c.data().Max();
#if QL_NEGATIVE_RATES
                return r<0.0 ? r/2.0 : r*2.0;
#else
				return r * 2.0;
#endif
			}
			// no constraints.
			// We choose as max a value very unlikely to be exceeded.
			return maxRate;
		}
   }
}
