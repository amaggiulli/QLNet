/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
  
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

namespace QLNet {

	//! Continuous-averaging Asian option
//    ! \todo add running average
//
//        \ingroup instruments
//    
	public class ContinuousAveragingAsianOption : OneAssetOption
	{
		new public class Arguments : OneAssetOption.Arguments
		{
			public Arguments()
			{
                averageType = Average.Type.NULL;
			}
			public override void validate()
			{
				base.validate();

                if (averageType == Average.Type.NULL)
                    throw new ApplicationException("unspecified average type");
			}
			public Average.Type averageType;
		}

 	    new public class Engine: GenericEngine<ContinuousAveragingAsianOption.Arguments, ContinuousAveragingAsianOption.Results> 
        {
        }

		public ContinuousAveragingAsianOption(Average.Type averageType, StrikedTypePayoff payoff, Exercise exercise) : base(payoff, exercise)
		{
			averageType_ = averageType;
		}
        public override void setupArguments(IPricingEngineArguments args)
		{
	
			base.setupArguments(args);
	
			ContinuousAveragingAsianOption.Arguments moreArgs = args as ContinuousAveragingAsianOption.Arguments;
			if (!(moreArgs != null))
                throw new ApplicationException("wrong argument type");
			moreArgs.averageType = averageType_;
		}
		protected Average.Type averageType_;
	}

	//! Discrete-averaging Asian option
	//! \ingroup instruments 
	public class DiscreteAveragingAsianOption : OneAssetOption
	{
		new public class Arguments : OneAssetOption.Arguments
		{
            public Arguments()
			{
				averageType = Average.Type.NULL;
				runningAccumulator = null;
                pastFixings = null;
			}
			public override void validate()
			{
				base.validate();

                if (averageType == Average.Type.NULL)
                    throw new ApplicationException("unspecified average type");

				if (!(pastFixings != null))
                    throw new ApplicationException("null past-fixing number");

				if (!(runningAccumulator != null))
                    throw new ApplicationException("null running product");

				switch (averageType)
				{
					case Average.Type.Arithmetic:
						if (!(runningAccumulator >= 0.0))
                            throw new ApplicationException("non negative running sum required: " + runningAccumulator + " not allowed");
						break;
					case Average.Type.Geometric:
						if (!(runningAccumulator > 0.0))
                            throw new ApplicationException("positive running product required: " + runningAccumulator + " not allowed");
						break;
					default:
                        throw new ApplicationException("invalid average type");
				}
		
				// check fixingTimes_ here
			}
			public Average.Type averageType;
			public double? runningAccumulator;
            public int? pastFixings;
            public List<Date> fixingDates;
		}

 	    new public class Engine: GenericEngine<DiscreteAveragingAsianOption.Arguments, DiscreteAveragingAsianOption.Results> 
        {
        }

        public DiscreteAveragingAsianOption(Average.Type averageType, double runningAccumulator, int pastFixings, List<Date> fixingDates, StrikedTypePayoff payoff, Exercise exercise)
            : base(payoff, exercise)
		{
			averageType_ = averageType;
			runningAccumulator_ = runningAccumulator;
			pastFixings_ = pastFixings;
			fixingDates_ = fixingDates;

            // std.sort(fixingDates_.begin(), fixingDates_.end());
            fixingDates_.Sort();
		}

        public override void setupArguments(IPricingEngineArguments args)
		{
	
			base.setupArguments(args);
	
			DiscreteAveragingAsianOption.Arguments moreArgs = args as DiscreteAveragingAsianOption.Arguments;
			if (!(moreArgs != null))
                throw new ApplicationException("wrong argument type");

			moreArgs.averageType = averageType_;
			moreArgs.runningAccumulator = runningAccumulator_;
			moreArgs.pastFixings = pastFixings_;
			moreArgs.fixingDates = fixingDates_;
		}
		protected Average.Type averageType_;
		protected double? runningAccumulator_;
        protected int? pastFixings_;
        protected List<Date> fixingDates_;
	}
}
