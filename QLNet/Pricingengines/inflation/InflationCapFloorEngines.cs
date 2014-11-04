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
*/using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
	//! Base YoY inflation cap/floor engine
	/*! This class doesn't know yet what sort of vol it is.  The
		 inflation index must be linked to a yoy inflation term
		 structure.  This provides the curves, hence the call uses a
		 shared_ptr<> not a handle<> to the index.

		 \ingroup inflationcapfloorengines
	*/

	public class YoYInflationCapFloorEngine : YoYInflationCapFloor.Engine 
	{
		public YoYInflationCapFloorEngine( YoYInflationIndex index, Handle<YoYOptionletVolatilitySurface> vol )
		{
			index_ = index;
			volatility_ = vol;

			index_.registerWith( update );
			volatility_.registerWith(update);
		}

      public YoYInflationIndex index() { return index_;}
      public Handle<YoYOptionletVolatilitySurface> volatility()  { return volatility_; }

		public void setVolatility( Handle<YoYOptionletVolatilitySurface> vol )
		{
			if ( !volatility_.empty() )
				volatility_ .unregisterWith(update );
			volatility_ = vol;
			volatility_.registerWith(update);
			update();
		}

		public override void calculate()
		{
			// copy black version then adapt to others

			double value = 0.0;
			int optionlets = arguments_.startDates.Count;
			InitializedList<double> values = new InitializedList<double>( optionlets,0.0 );
			InitializedList<double> stdDevs = new InitializedList<double>( optionlets,0.0);
			InitializedList<double> forwards = new InitializedList<double> (optionlets,0.0);
			CapFloorType type = arguments_.type;

			Handle<YoYInflationTermStructure> yoyTS
			= index().yoyInflationTermStructure();
			Handle<YieldTermStructure> nominalTS
			= yoyTS.link.nominalTermStructure();
			Date settlement = nominalTS.link.referenceDate();


			for (int i=0; i<optionlets; ++i) 
			{
            Date paymentDate = arguments_.payDates[i];
            if (paymentDate > settlement) 
				{ 
					// discard expired caplets
               double d = arguments_.nominals[i] *
                          arguments_.gearings[i] *
                          nominalTS.link.discount(paymentDate) *
                          arguments_.accrualTimes[i];

               // We explicitly have the index and assume that
               // the fixing is natural, i.e. no convexity adjustment.
               // If that was required then we would also need
               // nominal vols in the pricing engine, i.e. a different engine.
               // This also means that we do not need the coupon to have
               // a pricing engine to return the swaplet rate and then
               // the adjusted fixing in the instrument.
               forwards[i] = yoyTS.link.yoyRate(arguments_.fixingDates[i],new Period(0,TimeUnit.Days));
               double forward = forwards[i];

               Date fixingDate = arguments_.fixingDates[i];
               double sqrtTime = 0.0;
               if (fixingDate > volatility_.link.baseDate())
					{
                  sqrtTime = Math.Sqrt(volatility_.link.timeFromBase(fixingDate));
               }

                if (type == CapFloorType.Cap || type == CapFloorType.Collar) 
					 {
                   double strike = arguments_.capRates[i].Value;
                   if (sqrtTime>0.0) 
						 {
                        stdDevs[i] = Math.Sqrt(volatility_.link.totalVariance(fixingDate, strike, new Period(0,TimeUnit.Days)));

                   }

                   // sttDev=0 for already-fixed dates so everything on forward
                   values[i] = optionletImpl(Option.Type.Call, strike, forward, stdDevs[i], d);
                }
                if (type == CapFloorType.Floor || type == CapFloorType.Collar) 
					 {
                    double strike = arguments_.floorRates[i].Value;
                    if (sqrtTime>0.0) {
                        stdDevs[i] = Math.Sqrt( volatility_.link.totalVariance(fixingDate, strike, new Period(0,TimeUnit.Days)));
                    }
                    double floorlet = optionletImpl(Option.Type.Put, strike, forward, stdDevs[i], d);
                    if (type == CapFloorType.Floor) 
						  {
                        values[i] = floorlet;
                    } 
						  else 
						  {
                        // a collar is long a cap and short a floor
                        values[i] -= floorlet;
                    }

                }
                value += values[i];
            }
        }
        results_.value = value;

        results_.additionalResults["optionletsPrice"] = values;
        results_.additionalResults["optionletsAtmForward"] = forwards;
        if (type != CapFloorType.Collar)
            results_.additionalResults["optionletsStdDev"] = stdDevs;

		}

		
      //! descendents only need to implement this
		protected virtual double optionletImpl( Option.Type type, double strike, double forward, double stdDev,
											  double d ) { throw new NotImplementedException( "not implemented" ); }

      protected  YoYInflationIndex index_;
      protected  Handle<YoYOptionletVolatilitySurface> volatility_;
	}

	//! Black-formula inflation cap/floor engine (standalone, i.e. no coupon pricer)
   public class YoYInflationBlackCapFloorEngine : YoYInflationCapFloorEngine 
	{
		public YoYInflationBlackCapFloorEngine( YoYInflationIndex index, Handle<YoYOptionletVolatilitySurface> volatility )
			: base( index, volatility )
		{ }

		protected override double optionletImpl( Option.Type type, double strike, double forward, double stdDev,
															 double d )
		{
			return Utils.blackFormula( type, strike, forward, stdDev, d );
		}

    }

	//! Unit Displaced Black-formula inflation cap/floor engine (standalone, i.e. no coupon pricer)
   public class YoYInflationUnitDisplacedBlackCapFloorEngine : YoYInflationCapFloorEngine 
	{
		public YoYInflationUnitDisplacedBlackCapFloorEngine(YoYInflationIndex index,Handle<YoYOptionletVolatilitySurface> vol)
			: base( index, vol )
		{ }

		protected override double optionletImpl( Option.Type type, double strike,
											  double forward, double stdDev,
											  double d )
		{
			// could use displacement parameter in blackFormula but this is clearer
			return Utils.blackFormula( type, strike + 1.0,  forward + 1.0, stdDev, d );
		}

	}

	//! Unit Displaced Black-formula inflation cap/floor engine (standalone, i.e. no coupon pricer)
   public class YoYInflationBachelierCapFloorEngine : YoYInflationCapFloorEngine 
	{
		public YoYInflationBachelierCapFloorEngine(YoYInflationIndex index,Handle<YoYOptionletVolatilitySurface> vol)
			: base( index, vol )
		{ }

		protected override double optionletImpl( Option.Type type, double strike,
											  double forward, double stdDev,
											  double d )
		{
			return Utils.bachelierBlackFormula( type, strike, forward, stdDev, d );
		}


    };
}
