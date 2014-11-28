/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014 Edem Dawui (edawui@gmail.com)
 
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
	public interface IBootStrap<T>
	{
		void setup( T ts );
		void calculate();
	}

	public class IterativeBootstrapForYield : IterativeBootstrap<PiecewiseYieldCurve, YieldTermStructure>
	{
		public IterativeBootstrapForYield()
			: base()
		{}
	}

	public class IterativeBootstrapForInflation : IterativeBootstrap<PiecewiseZeroInflationCurve, ZeroInflationTermStructure>
	{
		public IterativeBootstrapForInflation()
			: base()
		{}
	}

	public class IterativeBootstrapForYoYInflation : IterativeBootstrap<PiecewiseYoYInflationCurve, YoYInflationTermStructure>
	{
		public IterativeBootstrapForYoYInflation()
			: base()
		{ }
	}


	//! Universal piecewise-term-structure boostrapper.
	public class IterativeBootstrap<T,U>:IBootStrap<T>
		where T : Curve<U>, new()
		where U : TermStructure
	{
		private bool validCurve_ = false;
		private T ts_;
		private int n_;
		private Brent firstSolver_ = new Brent();
		private FiniteDifferenceNewtonSafe solver_ = new FiniteDifferenceNewtonSafe();
		private bool initialized_;
		private int firstAliveHelper_, alive_;
		private List<double> previousData_;
		private List<BootstrapError<T, U>> errors_;

		public IterativeBootstrap()
		{
			ts_ = new T();
			initialized_ = false;
			validCurve_ = false;

		}

		private void initialize()
		{
			// ensure helpers are sorted
			ts_.instruments_.Sort( ( x, y ) => x.latestDate().CompareTo( y.latestDate() ) );

			// skip expired helpers
			Date firstDate = ts_.initialDate();
         Utils.QL_REQUIRE( ts_.instruments_[n_ - 1].latestDate() > firstDate, () => "all instruments expired" );
			firstAliveHelper_ = 0;
			while ( ts_.instruments_[firstAliveHelper_].latestDate() <= firstDate )
				++firstAliveHelper_;
			alive_ = n_ - firstAliveHelper_;
         Utils.QL_REQUIRE( alive_ >= ts_.interpolator_.requiredPoints - 1, () =>
						  "not enough alive instruments: " + alive_ +
						  " provided, " + ( ts_.interpolator_.requiredPoints - 1 ) +
						  " required" );

			List<Date> dates = ts_.dates_ = new List<Date>();
			List<double> times = ts_.times_ = new List<double>();

			errors_ = new List<BootstrapError<T, U>>( alive_ + 1 );
			dates.Add( firstDate );
			times.Add( ts_.timeFromReference( dates[0] ) );
			for ( int i = 1, j = firstAliveHelper_; j < n_; ++i, ++j )
			{
				BootstrapHelper<U> helper = ts_.instruments_[j];
				dates.Add( helper.latestDate() );
				times.Add( ts_.timeFromReference( dates[i] ) );
				// check for duplicated maturity
            Utils.QL_REQUIRE( dates[i - 1] != dates[i], () => "more than one instrument with maturity " + dates[i] );
				errors_.Add( new BootstrapError<T, U>( ts_, helper, i ) );
			}

			// set initial guess only if the current curve cannot be used as guess
			if ( !validCurve_ || ts_.data_.Count != alive_ + 1 )
			{
				// ts_->data_[0] is the only relevant item,
				// but reasonable numbers might be needed for the whole data vector
				// because, e.g., of interpolation's early checks
				ts_.data_ = new InitializedList<double>( alive_ + 1, ts_.initialValue() );
				previousData_ = new List<double>( alive_ + 1 );
			}
			initialized_ = true;

		}

		public void setup(T ts) 
		{
         ts_ = ts;

         n_ = ts_.instruments_.Count;
         Utils.QL_REQUIRE( n_ > 0, () => "no bootstrap helpers given" );

         if (!(n_+1 >= ts_.interpolator_.requiredPoints))
               throw new ArgumentException("not enough instruments: " + n_ + " provided, " +
                     (ts_.interpolator_.requiredPoints-1) + " required");

         ts_.instruments_.ForEach(x => ts_.registerWith(x));
      }

      public void calculate() 
		{
			// we might have to call initialize even if the curve is initialized
			// and not moving, just because helpers might be date relative and change
			// with evaluation date change.
			// anyway it makes little sense to use date relative helpers with a
			// non-moving curve if the evaluation date changes
			if ( !initialized_ || ts_.moving_ )
				initialize();

			// setup helpers
			for ( int j = firstAliveHelper_; j < n_; ++j )
			{
				BootstrapHelper<U> helper = ts_.instruments_[j];
				// check for valid quote
            Utils.QL_REQUIRE( helper.quote().link.isValid(), () =>
							  ( j + 1 ) + " instrument (maturity: " +
							  helper.latestDate() + ") has an invalid quote" );
				// don't try this at home!
				// This call creates helpers, and removes "const".
				// There is a significant interaction with observability.
				ts_.setTermStructure( ts_.instruments_[j] );
			}

			List<double> times = ts_.times_;
			List<double> data = ts_.data_;
			double accuracy = ts_.accuracy_;
			int maxIterations = ts_.maxIterations() - 1;

         for (int iteration = 0; ; ++iteration)
         {
            previousData_ = ts_.data_;

            for (int i = 1; i <= alive_; ++i)
            {
               // pillar loop

               bool validData = validCurve_ || iteration > 0;

               // bracket root and calculate guess
               double min = ts_.minValueAfter(i, ts_, validData, firstAliveHelper_);
               double max = ts_.maxValueAfter(i, ts_, validData, firstAliveHelper_);
               double guess =ts_.guess(i, ts_, validData, firstAliveHelper_);
               // adjust guess if needed
               if (guess >= max)
                  guess = max - (max - min) / 5.0;
               else if (guess <= min)
                  guess = min + (max - min) / 5.0;
                    
               // extend interpolation if needed
               if (!validData)
               {
                  try
                  {
							// extend interpolation a point at a time
							// including the pillar to be boostrapped
							ts_.interpolation_ = ts_.interpolator_.interpolate(ts_.times_, i + 1, ts_.data_);
							//ts_.interpolation_ = ts_.interpolator_.interpolate(times, times.Count, data);
                  }
                  catch (Exception)
                  {
							if (!ts_.interpolator_.global)
								throw; // no chance to fix it in a later iteration

							// otherwise use Linear while the target
							// interpolation is not usable yet
							ts_.interpolation_ = new Linear().interpolate(ts_.times_, i + 1, ts_.data_);
							//ts_.interpolation_ = new Linear().interpolate(times, times.Count, data);
                  }
                  ts_.interpolation_.update();
               }

               try
               {
                  var error = new BootstrapError<T, U>(ts_, ts_.instruments_[i - 1], i);
                  if (validData)
                        ts_.data_[i] = solver_.solve(error, accuracy, guess, min, max);
                  else
                        ts_.data_[i] = firstSolver_.solve(error, accuracy, guess, min, max);

               }
               catch (Exception e)
               {
                  validCurve_ = false;
                  Utils.QL_FAIL((iteration+1) + " iteration: failed " +
                           "at " + (i) + " alive instrument, "+
                           "maturity " + ts_.instruments_[i - 1].latestDate() +
                           ", reference date " + ts_.dates_[0] +
                           ": " + e.Message);
               }

				}
				
				if ( !ts_.interpolator_.global )
					break;     // no need for convergence loop
				else if ( iteration == 0 )
					continue; // at least one more iteration to convergence check

				// exit condition
				double change = Math.Abs( data[1] - previousData_[1] );
				for ( int i = 2; i <= alive_; ++i )
					change = Math.Max( change, Math.Abs( data[i] - previousData_[i] ) );
				if ( change <= accuracy )  // convergence reached
					break;

            Utils.QL_REQUIRE( iteration < maxIterations, () =>
							  "convergence not reached after " + iteration +
							  " iterations; last improvement " + change +
							  ", required accuracy " + accuracy );
			}
			validCurve_ = true;

		}
	}

}
