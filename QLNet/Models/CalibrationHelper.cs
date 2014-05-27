/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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
	//! liquid market instrument used during calibration
	public abstract class CalibrationHelper : LazyObject
	{
		public enum CalibrationErrorType
		{
			RelativePriceError, PriceError, ImpliedVolError
		}

		public CalibrationHelper( Handle<Quote> volatility, 
			Handle<YieldTermStructure> termStructure,
			CalibrationErrorType calibrationErrorType = CalibrationErrorType.RelativePriceError )
		{
			volatility_ = volatility;
			termStructure_ = termStructure;
			calibrationErrorType_ = calibrationErrorType;

			volatility_.registerWith( update );
			termStructure_.registerWith( update );
		}

		protected override void performCalculations() 
		{
			marketValue_ = blackPrice(volatility_.link.value());
      }

		//! returns the volatility Handle
		public Handle<Quote> volatility() { return volatility_; }

		//! returns the actual price of the instrument (from volatility)
      public double marketValue() { calculate(); return marketValue_; }

		//! returns the price of the instrument according to the model
		public abstract double modelValue();

		//! returns the error resulting from the model valuation
		public virtual double calibrationError()
		{
			double error = 0 ;

			switch ( calibrationErrorType_ )
			{
				case CalibrationErrorType.RelativePriceError:
					error = Math.Abs( marketValue() - modelValue() ) / marketValue();
					break;
				case CalibrationErrorType.PriceError:
					error = marketValue() - modelValue();
					break;
				case CalibrationErrorType.ImpliedVolError:
					{
						double lowerPrice = blackPrice( 0.001 );
						double upperPrice = blackPrice( 10 );
						double modelPrice = modelValue();

						double implied;
						if ( modelPrice <= lowerPrice )
							implied = 0.001;
						else
							if ( modelPrice >= upperPrice )
								implied = 10.0;
							else
								implied = this.impliedVolatility( modelPrice, 1e-12, 5000, 0.001, 10 );
						error = implied - volatility_.link.value();
					}
					break;
				default:
					Utils.QL_FAIL( "unknown Calibration Error Type" );
					break;
			}

			return error;

		}

		public abstract void addTimesTo( List<double> times );

		//! Black volatility implied by the model
		public double impliedVolatility( double targetValue, 
			double accuracy, int maxEvaluations, double minVol, double maxVol )
		{

			ImpliedVolatilityHelper f = new ImpliedVolatilityHelper( this, targetValue );
			Brent solver = new Brent();
			solver.setMaxEvaluations( maxEvaluations );
			return solver.solve( f, accuracy, volatility_.link.value(), minVol, maxVol );
		}

		//! Black price given a volatility
		public abstract double blackPrice( double volatility );

		public void setPricingEngine( IPricingEngine engine ) {engine_ = engine;}


		protected double marketValue_;
		protected Handle<Quote> volatility_;
		protected Handle<YieldTermStructure> termStructure_;
		protected IPricingEngine engine_;


		private CalibrationErrorType calibrationErrorType_;

		private class ImpliedVolatilityHelper : ISolver1d
		{
			private CalibrationHelper helper_;
			private double value_;

			public ImpliedVolatilityHelper( CalibrationHelper helper, double value )
			{
				helper_ = helper;
				value_ = value;
			}

			public override double value( double x )
			{
				return value_ - helper_.blackPrice( x );
			}
		}
	
	}
}
