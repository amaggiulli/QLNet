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
	//! Base class for yoy inflation cap-like instruments
	/*! \ingroup instruments

		 Note that the standard YoY inflation cap/floor defined here is
		 different from nominal, because in nominal world standard
		 cap/floors do not have the first optionlet.  This is because
		 they set in advance so there is no point.  However, yoy
		 inflation generally sets (effectively) in arrears, (actually
		 in arrears vs lag of a few months) thus the first optionlet is
		 relevant.  Hence we can do a parity test without a special
		 definition of the YoY cap/floor instrument.

		 \test
		 - the relationship between the values of caps, floors and the
			resulting collars is checked.
		 - the put-call parity between the values of caps, floors and
			swaps is checked.
		 - the correctness of the returned value is tested by checking
			it against a known good value.
	*/

	public class YoYInflationCapFloor : Instrument 
	{
      //public class arguments;
      //public class engine;

      public YoYInflationCapFloor(CapFloorType type, List<CashFlow> yoyLeg,  List<double> capRates,List<double> floorRates)
		{
			type_ = type;
			yoyLeg_ = yoyLeg;
			capRates_ = capRates;
			floorRates_ = floorRates;

			if (type_ ==  CapFloorType.Cap || type_ == CapFloorType.Collar) 
			{
            Utils.QL_REQUIRE( !capRates_.empty(), () => "no cap rates given" );
            //capRates_.reserve(yoyLeg_.size());
            while (capRates_.Count < yoyLeg_.Count)
                capRates_.Add(capRates_.Last());
        }
        if (type_ == CapFloorType.Floor || type_ == CapFloorType.Collar) 
		  {
           Utils.QL_REQUIRE( !floorRates_.empty(), () => "no floor rates given" );
            //floorRates_.reserve(yoyLeg_.size());
            while (floorRates_.Count < yoyLeg_.Count)
                floorRates_.Add(floorRates_.Last());
        }
        
			foreach (var cf in yoyLeg_)
            cf.registerWith(update);
			
			Settings.registerWith(update);

		}

      public YoYInflationCapFloor(CapFloorType type, List<CashFlow> yoyLeg,  List<double> strikes)
		{
			type_ = type;
			yoyLeg_ = yoyLeg;

         Utils.QL_REQUIRE( !strikes.empty(), () => "no strikes given" );
			if (type_ == CapFloorType.Cap) 
			{
            capRates_ = strikes;
            //capRates_.reserve(yoyLeg_.size());
            while (capRates_.Count < yoyLeg_.Count)
                capRates_.Add(capRates_.Last());
			} 
			else if (type_ == CapFloorType.Floor) 
			{
            floorRates_ = strikes;
            //floorRates_.reserve(yoyLeg_.size());
            while (floorRates_.Count < yoyLeg_.Count)
                floorRates_.Add(floorRates_.Last());
			} 
			else
            Utils.QL_FAIL("only Cap/Floor types allowed in this constructor");

			foreach (var cf in yoyLeg_)
            cf.registerWith(update);

			Settings.registerWith(update);
		}
		
		//! \name Instrument interface
		//@{
		public override bool isExpired()
		{
			for (int i=yoyLeg_.Count; i>0; --i)
            if (!yoyLeg_[i-1].hasOccurred())
                return false;
			return true;
		}
		public override void setupArguments(IPricingEngineArguments args)
		{
			YoYInflationCapFloor.Arguments arguments = args as YoYInflationCapFloor.Arguments;
         Utils.QL_REQUIRE( arguments != null, () => "wrong argument type" );

			int n = yoyLeg_.Count;

			arguments.startDates = new List<Date>(n);
			arguments.fixingDates=new List<Date>(n);
			arguments.payDates= new List<Date>(n);
			arguments.accrualTimes = new List<double>(n);
			arguments.nominals=new List<double>(n);
			arguments.gearings=new List<double>(n);
			arguments.capRates=new List<double?>(n);
			arguments.floorRates=new List<double?>(n);
			arguments.spreads= new List<double>(n);

			arguments.type = type_;

			for (int i=0; i<n; ++i) 
			{
				YoYInflationCoupon coupon = yoyLeg_[i] as YoYInflationCoupon;
            Utils.QL_REQUIRE( coupon != null, () => "non-YoYInflationCoupon given" );
            arguments.startDates.Add(coupon.accrualStartDate());
            arguments.fixingDates.Add(coupon.fixingDate());
            arguments.payDates.Add(coupon.date());

            // this is passed explicitly for precision
            arguments.accrualTimes.Add(coupon.accrualPeriod());

            arguments.nominals.Add(coupon.nominal());
            double spread = coupon.spread();
            double gearing = coupon.gearing();
            arguments.gearings.Add(gearing);
            arguments.spreads.Add(spread);

				if ( type_ == CapFloorType.Cap || type_ == CapFloorType.Collar )
                arguments.capRates.Add((capRates_[i]-spread)/gearing);
            else
                arguments.capRates.Add(null);

				if ( type_ == CapFloorType.Floor || type_ == CapFloorType.Collar )
                arguments.floorRates.Add((floorRates_[i]-spread)/gearing);
            else
                arguments.floorRates.Add(null);
        }

		}
		//@}
		//! \name Inspectors
		//@{
		public CapFloorType type() { return type_; }
		public  List<double> capRates()  { return capRates_; }
		public  List<double> floorRates()  { return floorRates_; }
		public  List<CashFlow> yoyLeg()  { return yoyLeg_; }

		public Date startDate() {return CashFlows.startDate(yoyLeg_);}
		public Date maturityDate() { return CashFlows.maturityDate(yoyLeg_);}
		public YoYInflationCoupon lastYoYInflationCoupon()
		{
			YoYInflationCoupon lastYoYInflationCoupon = yoyLeg_.Last() as YoYInflationCoupon;
			return lastYoYInflationCoupon;
		}
		//! Returns the n-th optionlet as a cap/floor with only one cash flow.
		public YoYInflationCapFloor optionlet( int i)
		{
         Utils.QL_REQUIRE( i < yoyLeg().Count, () => " optionlet does not exist, only " + yoyLeg().Count );
			List<CashFlow> cf = new List<CashFlow>();
			cf.Add(yoyLeg()[i]);

        List<double> cap = new List<double>(), floor = new List<double>();
        if (type() == CapFloorType.Cap || type() == CapFloorType.Collar)
            cap.Add(capRates()[i]);
        if (type() == CapFloorType.Floor || type() == CapFloorType.Collar)
            floor.Add(floorRates()[i]);

        return new YoYInflationCapFloor(type(), cf, cap, floor);
		}
		//@}
		public virtual double atmRate( YieldTermStructure discountCurve )
		{
			return CashFlows.atmRate(yoyLeg_, discountCurve,
                                  false, discountCurve.referenceDate());
		}

		//! implied term volatility
		public virtual double impliedVolatility(
                            double price,
                             Handle<YoYInflationTermStructure> yoyCurve,
                            double guess,
                            double accuracy = 1.0e-4,
                            int maxEvaluations = 100,
                            double minVol = 1.0e-7,
                            double maxVol = 4.0) 
		{
			Utils.QL_FAIL("not implemented yet");
			return 0;
		}
      
      private CapFloorType type_;
      private List<CashFlow> yoyLeg_;
		private List<double> capRates_;
		private List<double> floorRates_;

		//! %Arguments for YoY Inflation cap/floor calculation
		public class Arguments : IPricingEngineArguments 
		{
			public Arguments()  
			{
				//type = YoYInflationCapFloor::Type(-1))
			}
			public CapFloorType type;
			public YoYInflationIndex index;
			public Period observationLag;
			public List<Date> startDates;
			public List<Date> fixingDates;
			public List<Date> payDates;
			public List<double> accrualTimes;
			public List<double?> capRates;
			public List<double?> floorRates;
			public List<double> gearings;
			public List<double> spreads;
			public List<double> nominals;
			public void validate()
			{
            Utils.QL_REQUIRE( payDates.Count == startDates.Count, () =>
                   "number of start dates (" + startDates.Count
                   + ") different from that of pay dates ("
                   + payDates.Count + ")");
            Utils.QL_REQUIRE( accrualTimes.Count == startDates.Count, () =>
                   "number of start dates (" + startDates.Count
                   + ") different from that of accrual times ("
                   + accrualTimes.Count + ")");
				Utils.QL_REQUIRE(type == CapFloorType.Floor ||
                   capRates.Count == startDates.Count, () =>
                   "number of start dates (" + startDates.Count
                   + ") different from that of cap rates ("
                   + capRates.Count + ")");
				Utils.QL_REQUIRE(type == CapFloorType.Cap ||
                   floorRates.Count == startDates.Count, () =>
                   "number of start dates (" + startDates.Count
                   + ") different from that of floor rates ("
                   + floorRates.Count + ")");
            Utils.QL_REQUIRE( gearings.Count == startDates.Count, () =>
                   "number of start dates (" + startDates.Count
                   + ") different from that of gearings ("
                   + gearings.Count + ")");
            Utils.QL_REQUIRE( spreads.Count == startDates.Count, () =>
                   "number of start dates (" + startDates.Count
                   + ") different from that of spreads ("
                   + spreads.Count + ")");
            Utils.QL_REQUIRE( nominals.Count == startDates.Count, () =>
                   "number of start dates (" + startDates.Count
                   + ") different from that of nominals ("
                   + nominals.Count + ")");
			}
		}

		//! base class for cap/floor engines
		public class Engine : GenericEngine<YoYInflationCapFloor.Arguments, YoYInflationCapFloor.Results> 
		{}

    }

	//! Concrete YoY Inflation cap class
    /*! \ingroup instruments */
	public class YoYInflationCap : YoYInflationCapFloor 
	{
      public YoYInflationCap(List<CashFlow> yoyLeg,List<double> exerciseRates)
        : base(CapFloorType.Cap, yoyLeg, exerciseRates,new List<double>()) 
		{}
    }

	//! Concrete YoY Inflation floor class
    /*! \ingroup instruments */
   public class YoYInflationFloor : YoYInflationCapFloor 
	{
      public YoYInflationFloor(List<CashFlow> yoyLeg,List<double> exerciseRates)
        : base(CapFloorType.Floor, yoyLeg,new List<double>(), exerciseRates) 
		{}
    }

	//! Concrete YoY Inflation collar class
    /*! \ingroup instruments */
   public class YoYInflationCollar : YoYInflationCapFloor 
	{
      public YoYInflationCollar(List<CashFlow> yoyLeg, List<double> capRates,  List<double> floorRates)
        : base(CapFloorType.Collar, yoyLeg, capRates, floorRates) {}
    }

	
}
