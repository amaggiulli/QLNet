/*
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)
 
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
	public class InterpolatedZeroInflationCurve<Interpolator> : ZeroInflationTermStructure, InterpolatedCurve
	{
		public InterpolatedZeroInflationCurve( Date referenceDate, Calendar calendar, DayCounter dayCounter, Period lag,
														  Frequency frequency, bool indexIsInterpolated, Handle<YieldTermStructure> yTS,
														  List<Date> dates, List<double> rates,
														  Interpolator interpolator = default(Interpolator) )
			: base( referenceDate, calendar, dayCounter, rates[0],
											lag, frequency, indexIsInterpolated, yTS )
		{
			dates_ = dates;

         Utils.QL_REQUIRE( dates_.Count > 1, () => "too few dates: " + dates_.Count );

			// check that the data starts from the beginning,
			// i.e. referenceDate - lag, at least must be in the relevant
			// period
			KeyValuePair<Date, Date> lim = Utils.inflationPeriod( yTS.link.referenceDate() - this.observationLag(), frequency );
         Utils.QL_REQUIRE( lim.Key <= dates_[0] && dates_[0] <= lim.Value, () =>
						"first data date is not in base period, date: " + dates_[0]
						+ " not within [" + lim.Key + "," + lim.Value + "]" );

			// by convention, if the index is not interpolated we pull all the dates
			// back to the start of their inflationPeriods
			// otherwise the time calculations will be inconsistent
			if ( !indexIsInterpolated_ )
			{
				for ( int i = 0; i < dates_.Count; i++ )
				{
					dates_[i] = Utils.inflationPeriod( dates_[i], frequency ).Key;
				}
			}



         Utils.QL_REQUIRE( this.data_.Count == dates_.Count, () =>
						"indices/dates count mismatch: "
						+ this.data_.Count + " vs " + dates_.Count );

			this.times_ = new List<double>( dates_.Count );
			this.times_.Add( timeFromReference( dates_[0] ) );
			for ( int i = 1; i < dates_.Count; i++ )
			{
            Utils.QL_REQUIRE( dates_[i] > dates_[i - 1], () => "dates not sorted" );

				// but must be greater than -1
            Utils.QL_REQUIRE( this.data_[i] > -1.0, () => "zero inflation data < -100 %" );

				// this can be negative
				this.times_[i] = timeFromReference( dates_[i] );
            Utils.QL_REQUIRE( !Utils.close( this.times_[i], this.times_[i - 1] ), () =>
							"two dates correspond to the same time under this curve's day count convention" );
			}

			this.interpolation_ = this.interpolator_.interpolate( times_, times_.Count, data_ );
			this.interpolation_.update();


		}

		#region InterpolatedCurve
		public List<double> times_ { get; set; }
		public virtual List<double> times() { return this.times_; }

		public List<Date> dates_ { get; set; }
		public virtual List<Date> dates() { return dates_; }
		public override Date maxDate() { return dates_.Last(); }

		public List<double> data_ { get; set; }
		public List<double> forwards() { return this.data_; }
		public virtual List<double> data() { return forwards(); }

		public Interpolation interpolation_ { get; set; }
		public IInterpolationFactory interpolator_ { get; set; }

		public virtual Dictionary<Date, double> nodes()
		{
			Dictionary<Date, double> results = new Dictionary<Date, double>();
			dates_.ForEach( ( i, x ) => results.Add( x, data_[i] ) );
			return results;
		}

		public void setupInterpolation()
		{
			interpolation_ = interpolator_.interpolate( times_, times_.Count, data_ );
		}

		public object Clone()
		{
			InterpolatedCurve copy = this.MemberwiseClone() as InterpolatedCurve;
			copy.times_ = new List<double>( times_ );
			copy.data_ = new List<double>( data_ );
			copy.interpolator_ = interpolator_;
			copy.setupInterpolation();
			return copy;
		}
		#endregion

		//! \name InflationTermStructure interface
		//@{
		public override Date baseDate()
		{
			// if indexIsInterpolated we fixed the dates in the constructor
			return dates_.First();
		}

		//@}

		//! \name Inspectors
		//@{



		public List<double> rates()
		{
			return this.data_;
		}

		//@}


		//! \name ZeroInflationTermStructure Interface
		//@{
		protected override double zeroRateImpl( double t )
		{
			return this.interpolation_.value( t, true );
		}
		//@}


		/*! Protected version for use when descendents don't want to
			 (or can't) provide the points for interpolation on
			 construction.
		*/
		protected InterpolatedZeroInflationCurve( Date referenceDate,
												 Calendar calendar,
												 DayCounter dayCounter,
												 Period lag,
												 Frequency frequency,
												 bool indexIsInterpolated,
												 double baseZeroRate,
												 Handle<YieldTermStructure> yTS,
												 Interpolator interpolator = default(Interpolator) )
		{
		}

	}
}
