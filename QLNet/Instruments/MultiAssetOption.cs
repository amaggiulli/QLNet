/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
  
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

namespace QLNet {

	//! Base class for options on multiple assets
	public class MultiAssetOption : Option
	{
        public class Engine : GenericEngine<MultiAssetOption.Arguments, MultiAssetOption.Results> {};

        public new class Results : Instrument.Results
		{
            public double? delta, gamma, theta, vega, rho, dividendRho;

			public override void reset()
			{
				base.reset();
                // Greeks::reset();
                delta = gamma = theta = vega = rho = dividendRho = null;
            }
		}
		public MultiAssetOption(Payoff payoff, Exercise exercise) : base(payoff, exercise)
		{
		}
		//! \name Instrument interface
		//@{

		public override bool isExpired()
		{
			return new simple_event(exercise_.lastDate()).hasOccurred();
		}
		//@}
		//! \name greeks
		//@{
		public double delta()
		{
			calculate();
			if (delta_ == null)
                throw new Exception("delta not provided");
            return delta_.GetValueOrDefault();
		}
		public double gamma()
		{
			calculate();
            if (gamma_ == null)
                throw new Exception("gamma not provided");
            return gamma_.GetValueOrDefault();
		}
		public double theta()
		{
			calculate();
            if (theta_ == null)
                throw new Exception("theta not provided");
            return theta_.GetValueOrDefault();
		}
		public double vega()
		{
			calculate();
            if (vega_ == null)
                throw new Exception("vega not provided");
            return vega_.GetValueOrDefault();
		}
		public double rho()
		{
			calculate();
            if (rho_ == null)
                throw new Exception("rho not provided");
            return rho_.GetValueOrDefault();
		}
		public double dividendRho()
		{
			calculate();
            if (dividendRho_ == null)
                throw new Exception("dividend rho not provided");
            return dividendRho_.GetValueOrDefault();
		}

        public override void setupArguments(IPricingEngineArguments args)
		{
			MultiAssetOption.Arguments arguments = args as MultiAssetOption.Arguments;
			if (arguments == null)
                throw new Exception("wrong argument type");
	
			arguments.payoff = payoff_;
			arguments.exercise = exercise_;
		}

        public override void fetchResults(IPricingEngineResults r)
		{
			base.fetchResults(r);

            Results results = r as Results;
            if (results == null)
                throw new Exception("no greeks returned from pricing engine");

			delta_ = results.delta;
			gamma_ = results.gamma;
			theta_ = results.theta;
			vega_ = results.vega;
			rho_ = results.rho;
			dividendRho_ = results.dividendRho;
		}

		protected override void setupExpired()
		{
			NPV_ = delta_ = gamma_ = theta_ = vega_ = rho_ = dividendRho_ = 0.0;
		}

		// results
		protected double? delta_;
        protected double? gamma_;
        protected double? theta_;
        protected double? vega_;
        protected double? rho_;
        protected double? dividendRho_;
	}
}
