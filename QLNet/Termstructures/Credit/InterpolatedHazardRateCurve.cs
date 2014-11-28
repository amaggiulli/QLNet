/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
	public class InterpolatedHazardRateCurve<Interpolator> : HazardRateStructure,InterpolatedCurve
		where Interpolator : IInterpolationFactory, new()
	{
		public InterpolatedHazardRateCurve( List<Date> dates, List<double> hazardRates, DayCounter dayCounter, Calendar cal = null,
				List<Handle<Quote>> jumps = null, List<Date> jumpDates = null, Interpolator interpolator = default(Interpolator) )
			: base( dates[0], cal, dayCounter, jumps, jumpDates )
		{
			dates_ = dates;
			times_ = new List<double>();
			data_ = hazardRates;

			if ( interpolator == null )
				interpolator_ = new Interpolator();
			else
				interpolator_ = interpolator;

			initialize();
		}
      
		public InterpolatedHazardRateCurve( List<Date> dates,List<double> hazardRates,DayCounter dayCounter,Calendar calendar,
            Interpolator interpolator)
			:base(dates[0], calendar, dayCounter)
		{
			dates_ = dates;
			times_ = new List<double>();
			data_ = hazardRates;
			if ( interpolator == null )
				interpolator_ = new Interpolator();
			else
				interpolator_ = interpolator;
			initialize();
		}

		public InterpolatedHazardRateCurve( List<Date> dates, List<double> hazardRates, DayCounter dayCounter, Interpolator interpolator )
			: base( dates[0], null, dayCounter )
		{
			dates_ = dates;
			if ( interpolator == null )
				interpolator_ = new Interpolator();
			else
				interpolator_ = interpolator;
			initialize();
		}


		public List<double> hazardRates() { return this.data_; }

		protected InterpolatedHazardRateCurve( DayCounter dc,
				List<Handle<Quote>> jumps = null,
				List<Date> jumpDates = null,
				Interpolator interpolator = default(Interpolator) )
			: base( dc, jumps, jumpDates )
		{ }

		protected InterpolatedHazardRateCurve( Date referenceDate, DayCounter dc,
				List<Handle<Quote>> jumps = null,
				List<Date> jumpDates = null,
				Interpolator interpolator = default(Interpolator) )
			: base( referenceDate, null, dc, jumps, jumpDates )
		{ }

		protected InterpolatedHazardRateCurve( int settlementDays, Calendar cal, DayCounter dc,
				List<Handle<Quote>> jumps = null,
				List<Date> jumpDates = null,
				Interpolator interpolator = default(Interpolator) )
			: base( settlementDays, cal, dc, jumps, jumpDates )
		{}

      //! \name DefaultProbabilityTermStructure implementation
      //@{
		protected override double hazardRateImpl( double t )
		{
			if ( t <= this.times_.Last() )
				return this.interpolation_.value( t, true );

			// flat hazard rate extrapolation
			return this.data_.Last();
		}
		protected override double survivalProbabilityImpl( double t )
		{
			if (t == 0.0)
            return 1.0;

        double integral;
        if (t <= this.times_.Last()) 
		  {
            integral = this.interpolation_.primitive(t, true);
        } 
		  else 
		  {
            // flat hazard rate extrapolation
            integral = this.interpolation_.primitive(this.times_.Last(), true)
                     + this.data_.Last()*(t - this.times_.Last());
        }
        return Math.Exp(-integral);
		}
      //@}


		private void initialize()
		{
         Utils.QL_REQUIRE( dates_.Count >= interpolator_.requiredPoints, () => "not enough input dates given" );
         Utils.QL_REQUIRE( this.data_.Count == dates_.Count, () => "dates/data count mismatch" );

        //this.times_ = new List<double>(dates_.Count);
        this.times_.Add(0.0);
        for (int i=1; i<dates_.Count; ++i) 
		  {
           Utils.QL_REQUIRE( dates_[i] > dates_[i - 1], () => "invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")" );
				this.times_.Add( dayCounter().yearFraction( dates_[0], dates_[i] ) );
            Utils.QL_REQUIRE( !Utils.close( this.times_[i], this.times_[i - 1] ), () => "two dates correspond to the same time " +
                                                                             "under this curve's day count convention");
            Utils.QL_REQUIRE( this.data_[i] >= 0.0, () => "negative hazard rate" );
        }

		  setupInterpolation();
        this.interpolation_.update();
		}

		#region InterpolatedCurve

		public List<double> times_ { get; set; }
		public List<double> times() { return this.times_; }

		public List<Date> dates_ { get; set; }
		public List<Date> dates() { return dates_; }
		public override Date maxDate() { return dates_.Last(); }

		public List<double> data_ { get; set; }
		public List<double> discounts() { return this.data_; }
		public List<double> data() { return discounts(); }

		public Interpolation interpolation_ { get; set; }
		public IInterpolationFactory interpolator_ { get; set; }

		public Dictionary<Date, double> nodes()
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
	}
}
