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
   //! Year-on-year inflation-indexed swap
   /*! Quoted as a fixed rate \f$ K \f$.  At start:
       \f[
       \sum_{i=1}^{M} P_n(0,t_i) N K =
       \sum_{i=1}^{M} P_n(0,t_i) N \left[ \frac{I(t_i)}{I(t_i-1)} - 1 \right]
       \f]
       where \f$ t_M \f$ is the maturity time, \f$ P_n(0,t) \f$ is the
       nominal discount factor at time \f$ t \f$, \f$ N \f$ is the
       notional, and \f$ I(t) \f$ is the inflation index value at
       time \f$ t \f$.

       \note These instruments have now been changed to follow
             typical VanillaSwap type design conventions
             w.r.t. Schedules etc.
   */
   public class YearOnYearInflationSwap : Swap 
	{
		const double basisPoint = 1.0e-4;
		public enum Type { Receiver = -1, Payer = 1 };
		public YearOnYearInflationSwap(
						  Type type,
						  double nominal,
						  Schedule fixedSchedule,
						  double fixedRate,
						  DayCounter fixedDayCount,
						  Schedule yoySchedule,
						  YoYInflationIndex yoyIndex,
						  Period observationLag,
						  double spread,
						  DayCounter yoyDayCount,
						  Calendar paymentCalendar,    // inflation index does not have a calendar
						  BusinessDayConvention paymentConvention = BusinessDayConvention.ModifiedFollowing )
			: base( 2 )
		{
			type_ = type; 
			nominal_ = nominal;
			fixedSchedule_ = fixedSchedule; 
			fixedRate_ = fixedRate;
			fixedDayCount_ = fixedDayCount;
			yoySchedule_ = yoySchedule; 
			yoyIndex_ = yoyIndex;
			observationLag_ = observationLag;
			spread_ = spread;
			yoyDayCount_ = yoyDayCount; 
			paymentCalendar_ = paymentCalendar;
			paymentConvention_ = paymentConvention;

			// N.B. fixed leg gets its calendar from the schedule!
			List<CashFlow> fixedLeg = new FixedRateLeg( fixedSchedule_ )
			.withCouponRates( fixedRate_, fixedDayCount_ ) // Simple compounding by default
			.withNotionals( nominal_ )
			.withPaymentAdjustment( paymentConvention_ );

			List<CashFlow> yoyLeg = new yoyInflationLeg( yoySchedule_, paymentCalendar_, yoyIndex_, observationLag_ )
			.withSpreads( spread_ )
			.withPaymentDayCounter( yoyDayCount_ )
			.withNotionals( nominal_ )
			.withPaymentAdjustment( paymentConvention_ );

			yoyLeg.ForEach( x => x.registerWith( update ) );
			

			legs_[0] = fixedLeg;
			legs_[1] = yoyLeg;
			if ( type_ == Type.Payer )
			{
				payer_[0] = -1.0;
				payer_[1] = +1.0;
			}
			else
			{
				payer_[0] = +1.0;
				payer_[1] = -1.0;
			}

		}
      // results
		public virtual double fixedLegNPV()
		{
			calculate();
         Utils.QL_REQUIRE( legNPV_[0] != null, () => "result not available" );
			return legNPV_[0].Value;
		}
		public virtual double fairRate()
		{
			calculate();
         Utils.QL_REQUIRE( fairRate_ != null, () => "result not available" );
			return fairRate_.Value;
		}

		public virtual double yoyLegNPV()
		{
			calculate();
         Utils.QL_REQUIRE( legNPV_[1] != null, () => "result not available" );
			return legNPV_[1].Value;
		}
		public virtual double fairSpread()
		{
			calculate();
         Utils.QL_REQUIRE( fairSpread_ != null, () => "result not available" );
			return fairSpread_.Value;
		}
      // inspectors
      public virtual Type type() {return type_;}
      public virtual double nominal() { return nominal_;}

      public virtual Schedule fixedSchedule() {return fixedSchedule_;}
      public virtual double fixedRate() {return fixedRate_;}
		public virtual DayCounter fixedDayCount() { return fixedDayCount_; }

		public virtual Schedule yoySchedule() { return yoySchedule_; }
		public virtual YoYInflationIndex yoyInflationIndex() { return yoyIndex_; }
      public virtual Period observationLag() { return observationLag_; }
		public virtual double spread() { return spread_; }
		public virtual DayCounter yoyDayCount() { return yoyDayCount_; }

      public virtual Calendar paymentCalendar() { return paymentCalendar_; }
		public virtual BusinessDayConvention paymentConvention() { return paymentConvention_; }

		public virtual List<CashFlow> fixedLeg() { return legs_[0]; }
		public virtual List<CashFlow> yoyLeg() { return legs_[1]; }

      // other
		public override void setupArguments( IPricingEngineArguments args )
		{
			base.setupArguments(args);

			YearOnYearInflationSwap.Arguments arguments = args as YearOnYearInflationSwap.Arguments;

			if (arguments == null)  // it's a swap engine...
            return;

			arguments.type = type_;
			arguments.nominal = nominal_;

			List<CashFlow> fixedCoupons = fixedLeg();

			arguments.fixedResetDates = arguments.fixedPayDates = new List<Date>(fixedCoupons.Count);
			arguments.fixedCoupons = new List<double>(fixedCoupons.Count);

			for (int i=0; i<fixedCoupons.Count; ++i) 
			{
				FixedRateCoupon coupon = fixedCoupons[i] as FixedRateCoupon;

            arguments.fixedPayDates.Add(coupon.date());
            arguments.fixedResetDates.Add(coupon.accrualStartDate());
            arguments.fixedCoupons.Add(coupon.amount());
			}

			List<CashFlow> yoyCoupons = yoyLeg();

			arguments.yoyResetDates = arguments.yoyPayDates = arguments.yoyFixingDates =new List<Date>(yoyCoupons.Count);
			arguments.yoyAccrualTimes = new List<double>(yoyCoupons.Count);
			arguments.yoySpreads = new List<double>(yoyCoupons.Count);
			arguments.yoyCoupons = new List<double?>(yoyCoupons.Count);
			for (int i=0; i<yoyCoupons.Count; ++i) 
			{
				YoYInflationCoupon coupon = yoyCoupons[i] as YoYInflationCoupon;

            arguments.yoyResetDates.Add(coupon.accrualStartDate());
            arguments.yoyPayDates.Add(coupon.date());

            arguments.yoyFixingDates.Add(coupon.fixingDate());
            arguments.yoyAccrualTimes.Add(coupon.accrualPeriod());
            arguments.yoySpreads.Add(coupon.spread());
            try 
				{
					arguments.yoyCoupons.Add(coupon.amount());
            } 
				catch (Exception ) {
                arguments.yoyCoupons.Add(null);
            }
        }

		}
		public override void fetchResults( IPricingEngineResults r )
		{
			// copy from VanillaSwap
			// works because similarly simple instrument
			// that we always expect to be priced with a swap engine

			base.fetchResults(r);

			YearOnYearInflationSwap.Results results = r as YearOnYearInflationSwap.Results;
			if (results != null) 
			{ 
				// might be a swap engine, so no error is thrown
				fairRate_ = results.fairRate;
				fairSpread_ = results.fairSpread;
			} 
			else 
			{
				fairRate_ = null;
				fairSpread_ = null;
			}

        if (fairRate_ == null) 
		  {
            // calculate it from other results
            if (legBPS_[0] != null)
                fairRate_ = fixedRate_ - NPV_/(legBPS_[0]/basisPoint);
        }
        if (fairSpread_ == null) 
		  {
            // ditto
            if (legBPS_[1] != null)
                fairSpread_ = spread_ - NPV_/(legBPS_[1]/basisPoint);
        }

		}


		protected override void setupExpired()
		{
			base.setupExpired();
			legBPS_[0] = legBPS_[1] = 0.0;
			fairRate_ = null;
			fairSpread_ = null;
		}
      private Type type_;
      private double nominal_;
      private Schedule fixedSchedule_;
      private double fixedRate_;
      private DayCounter fixedDayCount_;
      private Schedule yoySchedule_;
      private YoYInflationIndex yoyIndex_;
      private Period observationLag_;
      private double spread_;
      private DayCounter yoyDayCount_;
      private Calendar paymentCalendar_;
      private BusinessDayConvention paymentConvention_;
      // results
      private double? fairRate_;
      private double? fairSpread_;

		//! %Arguments for YoY swap calculation
		public new class Arguments : Swap.Arguments
		{
			public Arguments()
			{
				type = Type.Receiver;
				nominal = null ;
			}

			public Type type;
			public double? nominal;

			public List<Date> fixedResetDates;
			public List<Date> fixedPayDates;
			public List<double> yoyAccrualTimes;
			public List<Date> yoyResetDates;
			public List<Date> yoyFixingDates;
			public List<Date> yoyPayDates;

			public List<double> fixedCoupons;
			public List<double> yoySpreads;
			public List<double?> yoyCoupons;
			public override void validate()
			{
				base.validate();
            Utils.QL_REQUIRE( nominal != null, () => "nominal null or not set" );
            Utils.QL_REQUIRE( fixedResetDates.Count == fixedPayDates.Count, () =>
                   "number of fixed start dates different from number of fixed payment dates");
            Utils.QL_REQUIRE( fixedPayDates.Count == fixedCoupons.Count, () =>
                   "number of fixed payment dates different from number of fixed coupon amounts");
            Utils.QL_REQUIRE( yoyResetDates.Count == yoyPayDates.Count, () =>
                   "number of yoy start dates different from number of yoy payment dates");
            Utils.QL_REQUIRE( yoyFixingDates.Count == yoyPayDates.Count, () =>
                   "number of yoy fixing dates different from number of yoy payment dates");
            Utils.QL_REQUIRE( yoyAccrualTimes.Count == yoyPayDates.Count, () =>
                   "number of yoy accrual Times different from number of yoy payment dates");
            Utils.QL_REQUIRE( yoySpreads.Count == yoyPayDates.Count, () =>
                   "number of yoy spreads different from number of yoy payment dates");
            Utils.QL_REQUIRE( yoyPayDates.Count == yoyCoupons.Count, () =>
                   "number of yoy payment dates different from number of yoy coupon amounts");
			}
		}

		//! %Results from YoY swap calculation
		public new class Results : Swap.Results
		{
			public double? fairRate;
			public double? fairSpread;
			public override void reset()
			{
				base.reset();
				fairRate = null;
				fairSpread = null;
			}
		}

		public class Engine : GenericEngine<YearOnYearInflationSwap.Arguments, YearOnYearInflationSwap.Results> {};

	}
}
