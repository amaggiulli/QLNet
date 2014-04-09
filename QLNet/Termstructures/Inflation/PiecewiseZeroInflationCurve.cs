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

	public class PiecewiseZeroInflationCurve : ZeroInflationTermStructure, InterpolatedCurve, Curve<ZeroInflationTermStructure>
	{
		#region InflationTraits

		public bool dummyInitialValue() { return traits_.dummyInitialValue(); }
		public double initialGuess() { return traits_.initialGuess(); }

		public double minValueAfter( int s, List<double> l ) { return traits_.minValueAfter( s, l ); }
		public double maxValueAfter( int s, List<double> l ) { return traits_.maxValueAfter( s, l ); }

		public double guess( ZeroInflationTermStructure c, Date d ) { return traits_.guess( c, d ); }

		// protected InflationTraits traits_;
		public Date initialDate( ZeroInflationTermStructure c ) { return traits_.initialDate( c ); }
		public double initialValue( ZeroInflationTermStructure c ) { return traits_.initialValue( c ); }
		public double guess( int i, InterpolatedCurve c, bool validData, int first ) { return traits_.guess( i, c, validData, first ); }
		public double minValueAfter( int i, InterpolatedCurve c, bool validData, int first ) { return traits_.minValueAfter( i, c, validData, first ); }
		public double maxValueAfter( int i, InterpolatedCurve c, bool validData, int first ) { return traits_.maxValueAfter( i, c, validData, first ); }
		public void updateGuess( List<double> data, double discount, int i ) { traits_.updateGuess( data, discount, i ); }
		public int maxIterations() { return traits_.maxIterations(); }

		#endregion

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

		public List<double> rates()
		{
			return this.data_;
		}

		protected override double zeroRateImpl( double t )
		{
			return this.interpolation_.value( t, true );
		}


		// these are dummy methods (for the sake of ITraits and should not be called directly
		public double discountImpl( Interpolation i, double t ) { throw new NotSupportedException(); }
		public double zeroYieldImpl( Interpolation i, double t ) { throw new NotSupportedException(); }
		public double forwardImpl( Interpolation i, double t ) { throw new NotSupportedException(); }


		# region new fields: Curve

		public double initialValue() { return _traits_.initialValue( this ); }
		public Date initialDate() { return _traits_.initialDate( this ); }

		public void registerWith( BootstrapHelper<ZeroInflationTermStructure> helper )
		{
			helper.registerWith( this.update );
		}


		//public new bool moving_
		public new bool moving_
		{
			get { return base.moving_; }
			set { base.moving_ = value; }
		}




		public void setTermStructure( BootstrapHelper<ZeroInflationTermStructure> helper )
		{
			helper.setTermStructure( this );
		}


		protected ITraits<ZeroInflationTermStructure> _traits_ = null;//todo define with the trait for yield curve
		public ITraits<ZeroInflationTermStructure> traits_
		{
			get
			{
				return _traits_;
			}
		}



		protected List<BootstrapHelper<ZeroInflationTermStructure>> _instruments_ = new List<BootstrapHelper<ZeroInflationTermStructure>>();

		public List<BootstrapHelper<ZeroInflationTermStructure>> instruments_
		{
			get
			{
				//todo edem 
				List<BootstrapHelper<ZeroInflationTermStructure>> instruments = new List<BootstrapHelper<ZeroInflationTermStructure>>();
				_instruments_.ForEach( x => instruments.Add( x ) );
				return instruments;
			}
		}

		protected IBootStrap<PiecewiseZeroInflationCurve> bootstrap_;


		protected double _accuracy_;//= 1.0e-12;
		public double accuracy_
		{
			get { return _accuracy_; }
			set { _accuracy_ = value; }
		}


		public override Date baseDate()
		{
			// if indexIsInterpolated we fixed the dates in the constructor
			return dates_.First();
		}


		# endregion




		public PiecewiseZeroInflationCurve( DayCounter dayCounter, double baseZeroRate, Period observationLag, Frequency frequency,
													  bool indexIsInterpolated, Handle<YieldTermStructure> yTS )
			: base( dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS ) { }

		public PiecewiseZeroInflationCurve( Date referenceDate, Calendar calendar, DayCounter dayCounter, double baseZeroRate,
													  Period observationLag, Frequency frequency, bool indexIsInterpolated,
													  Handle<YieldTermStructure> yTS )
			: base( referenceDate, calendar, dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS ) { }

		public PiecewiseZeroInflationCurve( int settlementDays, Calendar calendar, DayCounter dayCounter, double baseZeroRate,
													  Period observationLag, Frequency frequency, bool indexIsInterpolated,
													  Handle<YieldTermStructure> yTS )
			: base( settlementDays, calendar, dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS ) { }


		public PiecewiseZeroInflationCurve()
			: base()
		{ }
	}


	public class PiecewiseZeroInflationCurve<Interpolator, Bootstrap, Traits> : PiecewiseZeroInflationCurve
		where Traits : ITraits<ZeroInflationTermStructure>, new()
		where Interpolator : IInterpolationFactory, new()
		where Bootstrap : IBootStrap<PiecewiseZeroInflationCurve>, new()
	{

		public PiecewiseZeroInflationCurve( Date referenceDate,
					Calendar calendar,
					DayCounter dayCounter,
					Period lag,
					Frequency frequency,
					bool indexIsInterpolated,
					double baseZeroRate,
					Handle<YieldTermStructure> nominalTS,
					List<BootstrapHelper<ZeroInflationTermStructure>> instruments,
					double accuracy = 1.0e-12,
					Interpolator i = default(Interpolator),
					Bootstrap bootstrap = default(Bootstrap) )
			: base( referenceDate, calendar, dayCounter, baseZeroRate, lag, frequency, indexIsInterpolated, nominalTS )
		{
			_instruments_ = instruments;
			accuracy_ = accuracy;
			if ( bootstrap == null )
				bootstrap_ = new Bootstrap();
			else
				bootstrap_ = bootstrap;

			if ( i == null )
				interpolator_ = new Interpolator();
			else
				interpolator_ = i;

			_traits_ = new Traits();
			bootstrap_.setup( this );

		}

		//@}
		//! \name Inflation interface
		//@{
		public override Date baseDate()
		{
			this.calculate();
			return base.baseDate();
		}
		public override Date maxDate()
		{
			this.calculate();
			return base.maxDate();
		}
		//@
		//! \name Inspectors
		//@{
		public override List<double> times()
		{
			calculate();
			return base.times();
		}
		public override List<Date> dates()
		{
			calculate();
			return base.dates();
		}
		public override List<double> data()
		{
			calculate();
			return base.rates();
		}
		public override Dictionary<Date, double> nodes()
		{
			calculate();
			return base.nodes();
		}
		//@}
		//! \name Observer interface
		//@{
		public override void update() { base.update(); }
		//@}

		// methods
		protected override void performCalculations() { bootstrap_.calculate(); }
	}


	// Allows for optional 3rd generic parameter defaulted to IterativeBootstrap
	public class PiecewiseZeroInflationCurve<Interpolator> : PiecewiseZeroInflationCurve<Interpolator, IterativeBootstrapForInflation, ZeroInflationTraits>
		where Interpolator : IInterpolationFactory, new()
	{
		public PiecewiseZeroInflationCurve( Date referenceDate,
					Calendar calendar,
					DayCounter dayCounter,
					Period lag,
					Frequency frequency,
					bool indexIsInterpolated,
					double baseZeroRate,
					Handle<YieldTermStructure> nominalTS,
					List<BootstrapHelper<ZeroInflationTermStructure>> instruments,
					double accuracy = 1.0e-12,
					Interpolator i = default(Interpolator) )
			: base( referenceDate, calendar, dayCounter, lag, frequency, indexIsInterpolated, baseZeroRate, nominalTS,
					instruments, accuracy, i ) { }


	}


}
