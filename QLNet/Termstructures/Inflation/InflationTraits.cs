/*
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014  Edem Dawui (edawui@gmail.com)

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
	public class ZeroInflationTraits : ITraits<ZeroInflationTermStructure>
	{
		const double avgInflation = 0.02;
		const double maxInflation = 0.5;

		public Date initialDate( ZeroInflationTermStructure t )
		{
			if ( t.indexIsInterpolated() )
			{
				return t.referenceDate() - t.observationLag();
			}
			else
			{
				return Utils.inflationPeriod( t.referenceDate() - t.observationLag(),
											  t.frequency() ).Key;
			}
		}

		public double initialValue( ZeroInflationTermStructure t )
		{
			return t.baseRate();
		}

		public double guess( int i, InterpolatedCurve c, bool validData, int f )
		{
			if ( validData ) // previous iteration value
				return c.data()[i];

			if ( i == 1 ) // first pillar
				return avgInflation;

			// could/should extrapolate
			return avgInflation;
		}

		public double minValueAfter( int i, InterpolatedCurve c, bool validData, int f )
		{
			if ( validData )
			{
				double r = c.data().Min();
				return r < 0.0 ? r * 2.0 : r / 2.0;
			}
			return -maxInflation;
		}

		public double maxValueAfter( int i, InterpolatedCurve c, bool validData, int f )
		{
			if ( validData )
			{
				double r = c.data().Max();
				return r < 0.0 ? r / 2.0 : r * 2.0;
			}
			// no constraints.
			// We choose as max a value very unlikely to be exceeded.
			return maxInflation;
		}

		public void updateGuess( List<double> data, double discount, int i )
		{
			data[i] = discount;
		}

		public int maxIterations()
		{
			return 5;
		}

		public double discountImpl( Interpolation i, double t ) { throw new NotSupportedException(); }

		public double zeroYieldImpl( Interpolation i, double t ) { throw new NotSupportedException(); }

		public double forwardImpl( Interpolation i, double t ) { throw new NotSupportedException(); }

		double ITraits<ZeroInflationTermStructure>.initialGuess()
		{
			throw new NotImplementedException();
		}

		double ITraits<ZeroInflationTermStructure>.guess( ZeroInflationTermStructure c, Date d )
		{
			throw new NotImplementedException();
		}

		double ITraits<ZeroInflationTermStructure>.minValueAfter( int s, List<double> l )
		{
			throw new NotImplementedException();
		}

		double ITraits<ZeroInflationTermStructure>.maxValueAfter( int i, List<double> data )
		{
			throw new NotImplementedException();
		}

		bool ITraits<ZeroInflationTermStructure>.dummyInitialValue()
		{
			throw new NotImplementedException();
		}

	}

	public class YoYInflationTraits : ITraits<YoYInflationTermStructure>
	{
		const double avgInflation = 0.02;
		const double maxInflation = 0.5;

		public Date initialDate( YoYInflationTermStructure t )
		{
			if ( t.indexIsInterpolated() )
			{
				return t.referenceDate() - t.observationLag();
			}
			else
			{
				return Utils.inflationPeriod( t.referenceDate() - t.observationLag(),
											  t.frequency() ).Key;
			}
		}

		public double initialValue( YoYInflationTermStructure t )
		{
			return t.baseRate();
		}

		public double guess( int i, InterpolatedCurve c, bool validData, int f )
		{
			if ( validData ) // previous iteration value
				return c.data()[i];

			if ( i == 1 ) // first pillar
				return avgInflation;

			// could/should extrapolate
			return avgInflation;
		}

		public double minValueAfter( int i, InterpolatedCurve c, bool validData, int f )
		{
			if ( validData )
			{
				double r = c.data().Min();
				return r < 0.0 ? r * 2.0 : r / 2.0;
			}
			return -maxInflation;
		}

		public double maxValueAfter( int i, InterpolatedCurve c, bool validData, int f )
		{
			if ( validData )
			{
				double r = c.data().Max();
				return r < 0.0 ? r / 2.0 : r * 2.0;
			}
			// no constraints.
			// We choose as max a value very unlikely to be exceeded.
			return maxInflation;
		}

		void updateGuess( List<double> data, double discount, int i )
		{
			data[i] = discount;
		}

		public int maxIterations()
		{
			return 40;
		}

		public double discountImpl( Interpolation i, double t ) { throw new NotSupportedException(); }
		public double zeroYieldImpl( Interpolation i, double t ) { throw new NotSupportedException(); }
		public double forwardImpl( Interpolation i, double t ) { throw new NotSupportedException(); }

		double ITraits<YoYInflationTermStructure>.initialGuess()
		{
			throw new NotImplementedException();
		}

		double ITraits<YoYInflationTermStructure>.guess( YoYInflationTermStructure c, Date d )
		{
			throw new NotImplementedException();
		}

		double ITraits<YoYInflationTermStructure>.minValueAfter( int s, List<double> l )
		{
			throw new NotImplementedException();
		}

		double ITraits<YoYInflationTermStructure>.maxValueAfter( int i, List<double> data )
		{
			throw new NotImplementedException();
		}

		void ITraits<YoYInflationTermStructure>.updateGuess( List<double> data, double discount, int i )
		{
			//throw new NotImplementedException();
			data[i] = discount;
		}

		bool ITraits<YoYInflationTermStructure>.dummyInitialValue()
		{
			throw new NotImplementedException();
		}

	}

}
