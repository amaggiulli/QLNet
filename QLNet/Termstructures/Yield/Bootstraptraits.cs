/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2014 Edem Dawui (edawui@gmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
	public interface ITraits<T>
	{
		Date initialDate( T c );		// start of curve data
		double initialValue( T c );   // value at reference date
		bool dummyInitialValue();     // true if the initialValue is just a dummy value
		double initialGuess();        // initial guess
		double guess( T c, Date d );	// further guesses
		// possible constraints based on previous values
		double minValueAfter( int s, List<double> l );
		double maxValueAfter( int i, List<double> data );
		// update with new guess
		void updateGuess( List<double> data, double discount, int i );
		int maxIterations();                          // upper bound for convergence loop

		//
		double discountImpl( Interpolation i, double t );
		double zeroYieldImpl( Interpolation i, double t );
		double forwardImpl( Interpolation i, double t );

		double guess( int i, InterpolatedCurve c, bool validData, int first );

		double minValueAfter( int i, InterpolatedCurve c, bool validData, int first );
		double maxValueAfter( int i, InterpolatedCurve c, bool validData, int first );

	}

	public class Discount : ITraits<YieldTermStructure>
	{
		const double maxRate = 1;
		const double avgRate = 0.05;
		public Date initialDate( YieldTermStructure c ) { return c.referenceDate(); }   // start of curve data
		public double initialValue( YieldTermStructure c ) { return 1; }    // value at reference date
		public bool dummyInitialValue() { return false; }   // true if the initialValue is just a dummy value
		public double initialGuess() { return 1.0 / ( 1.0 + avgRate * 0.25 ); }   // initial guess
		public double guess( YieldTermStructure c, Date d ) { return c.discount( d, true ); }  // further guesses
		// possible constraints based on previous values
		public double minValueAfter( int s, List<double> l )
		{
			// replace with Epsilon
			return Const.QL_Epsilon;
		}
		public double maxValueAfter( int i, List<double> data )
		{
			// discount are not required to be decreasing--all bets are off.
			// We choose as max a value very unlikely to be exceeded.
			return 3.0;

			// discounts cannot increase
			//return data[i - 1]; 
		}
		// update with new guess
		public void updateGuess( List<double> data, double discount, int i ) { data[i] = discount; }
		public int maxIterations() { return 50; }   // upper bound for convergence loop

		public double discountImpl( Interpolation i, double t ) { return i.value( t, true ); }
		public double zeroYieldImpl( Interpolation i, double t ) { throw new NotSupportedException(); }
		public double forwardImpl( Interpolation i, double t ) { throw new NotSupportedException(); }


		public double guess( int i, InterpolatedCurve c, bool validData, int f )
		{
			//return 1.0 / (1.0 + avgRate * 0.25);      
			if ( validData ) // previous iteration value
				return c.data()[i];

			if ( i == 1 ) // first pillar
				return 1.0 / ( 1.0 + avgRate * c.times()[1] );

			// flat rate extrapolation
			double r = -System.Math.Log( c.data()[i - 1] ) / c.times()[i - 1];
			return System.Math.Exp( -r * c.times()[i] );
		}

		public double minValueAfter( int i, InterpolatedCurve c, bool validData, int f )
		{
			//return Const.QL_Epsilon;

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

		public double maxValueAfter( int i, InterpolatedCurve c, bool validData, int f )
		{
			//return 3.0;

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
		public bool dummyInitialValue() { return true; }   // true if the initialValue is just a dummy value
		public double initialGuess() { return avgRate; }   // initial guess
		public double guess( YieldTermStructure c, Date d )
		{
			return c.zeroRate( d, c.dayCounter(), Compounding.Continuous, Frequency.Annual, true ).rate();
		}  // further guesses
		// possible constraints based on previous values
		public double minValueAfter( int s, List<double> l )
		{
#if QL_NEGATIVE_RATES
            // no constraints.
            // We choose as min a value very unlikely to be exceeded.
            return -3.0;
#else
			return Const.QL_Epsilon;
#endif
		}
		public double maxValueAfter( int i, List<double> data )
		{
			// no constraints.
			// We choose as max a value very unlikely to be exceeded.
			return 3.0;
		}
		// update with new guess
		public void updateGuess( List<double> data, double rate, int i )
		{
			data[i] = rate;
			if ( i == 1 )
				data[0] = rate; // first point is updated as well
		}
		public int maxIterations() { return 30; }   // upper bound for convergence loop

		public double discountImpl( Interpolation i, double t )
		{
			double r = zeroYieldImpl( i, t );
			return Math.Exp( -r * t );
		}
		public double zeroYieldImpl( Interpolation i, double t ) { return i.value( t, true ); }
		public double forwardImpl( Interpolation i, double t ) { throw new NotSupportedException(); }



		public double guess( int i, InterpolatedCurve c, bool validData, int f )
		{

			if ( validData ) // previous iteration value
				return c.data()[i];

			if ( i == 1 ) // first pillar
				return avgRate;

			// extrapolate
			return zeroYieldImpl( c.interpolation_, c.times()[i] );
		}

		public double minValueAfter( int i, InterpolatedCurve c, bool validData, int f )
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
			return Const.QL_Epsilon;
#endif


		}

		public double maxValueAfter( int i, InterpolatedCurve c, bool validData, int f )
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
		public bool dummyInitialValue() { return true; }    // true if the initialValue is just a dummy value
		public double initialGuess() { return avgRate; } // initial guess
		// further guesses
		public double guess( YieldTermStructure c, Date d )
		{
			return c.forwardRate( d, d, c.dayCounter(), Compounding.Continuous, Frequency.Annual, true ).rate();
		}
		// possible constraints based on previous values
		public double minValueAfter( int v, List<double> l ) { return Const.QL_Epsilon; }
		public double maxValueAfter( int v, List<double> l )
		{
			// no constraints.
			// We choose as max a value very unlikely to be exceeded.
			return 3;
		}
		// update with new guess
		public void updateGuess( List<double> data, double forward, int i )
		{
			data[i] = forward;
			if ( i == 1 )
				data[0] = forward; // first point is updated as well
		}
		// upper bound for convergence loop
		public int maxIterations() { return 30; }

		public double discountImpl( Interpolation i, double t )
		{
			double r = zeroYieldImpl( i, t );
			return Math.Exp( -r * t );
		}
		public double zeroYieldImpl( Interpolation i, double t )
		{
			if ( t == 0.0 )
				return forwardImpl( i, 0.0 );
			else
				return i.primitive( t, true ) / t;
		}
		public double forwardImpl( Interpolation i, double t )
		{
			return i.value( t, true );
		}

		public double guess( int i, InterpolatedCurve c, bool validData, int f )
		{
			//return 1.0 / (1.0 + avgRate * 0.25);
			if ( validData ) // previous iteration value
				return c.data()[i];

			if ( i == 1 ) // first pillar
				return avgRate;

			// extrapolate
			return forwardImpl( c.interpolation_, c.times()[i] );
		}

		public double minValueAfter( int i, InterpolatedCurve c, bool validData, int f )
		{
			//return Const.QL_Epsilon;

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
			return Const.QL_Epsilon;
#endif
		}

		public double maxValueAfter( int i, InterpolatedCurve c, bool validData, int f )
		{
			// return 3.0;


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
