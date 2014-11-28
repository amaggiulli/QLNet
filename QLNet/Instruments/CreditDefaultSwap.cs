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
	public struct Protection {
        public enum Side { Buyer, Seller };
    };

	//! Credit default swap
	/*! \note This instrument currently assumes that the issuer did
				 not default until today's date.

		 \warning if <tt>Settings::includeReferenceDateCashFlows()</tt>
					 is set to <tt>true</tt>, payments occurring at the
					 settlement date of the swap might be included in the
					 NPV and therefore affect the fair-spread
					 calculation. This might not be what you want.

		  \ingroup instruments
	*/
	public class CreditDefaultSwap : Instrument
	{
		//! \name ructors
      //@{
      //! CDS quoted as running-spread only
      /*! @param side  Whether the protection is bought or sold.
          @param notional  Notional value
          @param spread  Running spread in fractional units.
          @param schedule  Coupon schedule.
          @param paymentConvention  Business-day convention for
                                    payment-date adjustment.
          @param dayCounter  Day-count convention for accrual.
          @param settlesAccrual  Whether or not the accrued coupon is
                                 due in the event of a default.
          @param paysAtDefaultTime  If set to true, any payments
                                    triggered by a default event are
                                    due at default time. If set to
                                    false, they are due at the end of
                                    the accrual period.
          @param protectionStart  The first date where a default
                                  event will trigger the contract.
      */
		public CreditDefaultSwap(Protection.Side side,
								  double notional,
								  double spread,
								  Schedule schedule,
								  BusinessDayConvention convention,
								  DayCounter dayCounter,
								  bool settlesAccrual = true,
								  bool paysAtDefaultTime = true,
								  Date protectionStart = null,
								  Claim claim = null)
		{
			side_ = side;
			notional_ = notional;
			upfront_ = null;
			runningSpread_ = spread;
			settlesAccrual_ =settlesAccrual;
			paysAtDefaultTime_ = paysAtDefaultTime;
			claim_ = claim;
			protectionStart_ = protectionStart == null ? schedule[0] :  protectionStart;

         Utils.QL_REQUIRE( protectionStart_ <= schedule[0], () => "protection can not start after accrual" );
			leg_ = new FixedRateLeg(schedule)
            .withCouponRates(spread, dayCounter)
				.withNotionals(notional)
            .withPaymentAdjustment(convention);
        
			upfrontPayment_ = new SimpleCashFlow(0.0, schedule[0]);

        if (claim_ == null )
            claim_ = new FaceValueClaim();

		  claim_.registerWith(update);
		}
      //! CDS quoted as upfront and running spread
      /*! @param side  Whether the protection is bought or sold.
          @param notional  Notional value
          @param upfront Upfront in fractional units.
          @param spread Running spread in fractional units.
          @param schedule  Coupon schedule.
          @param paymentConvention  Business-day convention for
                                    payment-date adjustment.
          @param dayCounter  Day-count convention for accrual.
          @param settlesAccrual Whether or not the accrued coupon is
                                due in the event of a default.
          @param paysAtDefaultTime If set to true, any payments
                                   triggered by a default event are
                                   due at default time. If set to
                                   false, they are due at the end of
                                   the accrual period.
          @param protectionStart The first date where a default
                                 event will trigger the contract.
          @param upfrontDate Settlement date for the upfront payment.
      */
		public CreditDefaultSwap(Protection.Side side,
										 double notional,
										 double upfront,
										 double runningSpread,
										 Schedule schedule,
										 BusinessDayConvention convention,
										 DayCounter dayCounter,
										 bool settlesAccrual = true,
										 bool paysAtDefaultTime = true,
										 Date protectionStart = null,
										 Date upfrontDate = null,
										 Claim claim = null)
		{
			
			side_ = side;
			notional_ = notional;
			upfront_ = upfront;
			runningSpread_ = runningSpread;
			settlesAccrual_ =settlesAccrual;
			paysAtDefaultTime_ = paysAtDefaultTime;
			claim_ = claim;
			protectionStart_ = protectionStart == null ? schedule[0] :  protectionStart;

         Utils.QL_REQUIRE( protectionStart_ <= schedule[0], () => "protection can not start after accrual" );
			leg_ = new FixedRateLeg(schedule)
            .withCouponRates(runningSpread, dayCounter)
            .withNotionals(notional)
            .withPaymentAdjustment(convention);
        
			Date d = upfrontDate == null ? schedule[0] : upfrontDate;
			upfrontPayment_ = new SimpleCashFlow(notional*upfront, d);
         Utils.QL_REQUIRE( upfrontPayment_.date() >= protectionStart_, () => "upfront can not be due before contract start" );

        if (claim_ == null)
            claim_ = new FaceValueClaim();
		  claim_.registerWith(update);     
		}
      //@}
      //! \name Instrument interface
      //@{
		public override bool isExpired()
		{
			for (int i = leg_.Count; i > 0; --i)
				if (!leg_[i - 1].hasOccurred())
					return false;
        return true;
		}

		public override void setupArguments(IPricingEngineArguments args)
		{
			CreditDefaultSwap.Arguments arguments = args as CreditDefaultSwap.Arguments;
         Utils.QL_REQUIRE( arguments != null, () => "wrong argument type" );

			arguments.side = side_;
			arguments.notional = notional_;
			arguments.leg = leg_;
			arguments.upfrontPayment = upfrontPayment_;
			arguments.settlesAccrual = settlesAccrual_;
			arguments.paysAtDefaultTime = paysAtDefaultTime_;
			arguments.claim = claim_;
			arguments.upfront = upfront_;
			arguments.spread = runningSpread_;
			arguments.protectionStart = protectionStart_;
		}

		public override void fetchResults(IPricingEngineResults r)
		{
			base.fetchResults(r);
         CreditDefaultSwap.Results results = r as CreditDefaultSwap.Results;
         Utils.QL_REQUIRE( results != null, () => "wrong result type" );

			fairSpread_ = results.fairSpread;
			fairUpfront_ = results.fairUpfront;
			couponLegBPS_ = results.couponLegBPS;
			couponLegNPV_ = results.couponLegNPV;
			defaultLegNPV_ = results.defaultLegNPV;
			upfrontNPV_ = results.upfrontNPV;
			upfrontBPS_ = results.upfrontBPS;
		}
      //@}
      //! \name Inspectors
      //@{
		public Protection.Side side() { return side_; }
		public double? notional() { return notional_; }
		public double runningSpread() { return runningSpread_; }
		public double? upfront() { return upfront_; }
		public bool settlesAccrual() { return settlesAccrual_; }
		public bool paysAtDefaultTime() { return paysAtDefaultTime_; }
		public List<CashFlow> coupons() { return leg_; }
      //! The first date for which defaults will trigger the contract
		public Date protectionStartDate() { return protectionStart_; }
      //! The last date for which defaults will trigger the contract
      public  Date protectionEndDate() {return ((Coupon)(leg_.Last())).accrualEndDate();}
      //@}
      //! \name Results
      //@{
      /*! Returns the upfront spread that, given the running spread
          and the quoted recovery rate, will make the instrument
          have an NPV of 0.
      */
		public double fairUpfront()
		{
			calculate();
         Utils.QL_REQUIRE( fairUpfront_ != null, () => "fair upfront not available" );
			return fairUpfront_.Value;
		}
      /*! Returns the running spread that, given the quoted recovery
          rate, will make the running-only CDS have an NPV of 0.

          \note This calculation does not take any upfront into
                account, even if one was given.
      */
		public double fairSpread()
		{
			calculate();
         Utils.QL_REQUIRE( fairSpread_ != null, () => "fair spread not available" );
			return fairSpread_.Value;
		}
      /*! Returns the variation of the fixed-leg value given a
          one-basis-point change in the running spread.
      */
		public double couponLegBPS()
		{
			calculate();
         Utils.QL_REQUIRE( couponLegBPS_ != null, () => "coupon-leg BPS not available" );
			return couponLegBPS_.Value;
		}

		public double upfrontBPS()
		{
			calculate();
         Utils.QL_REQUIRE( upfrontBPS_ != null, () => "upfront BPS not available" );
			return upfrontBPS_.Value;
		}

		public double couponLegNPV()
		{
			calculate();
         Utils.QL_REQUIRE( couponLegNPV_ != null, () => "coupon-leg NPV not available" );
			return couponLegNPV_.Value;
		}

		public double defaultLegNPV()
		{
			calculate();
         Utils.QL_REQUIRE( defaultLegNPV_ != null, () => "default-leg NPV not available" );
			return defaultLegNPV_.Value;
		}

		public double upfrontNPV()
		{
			calculate();
         Utils.QL_REQUIRE( upfrontNPV_ != null, () => "upfront NPV not available" );
			return upfrontNPV_.Value;
		}

      //! Implied hazard rate calculation
      /*! \note This method performs the calculation with the
                instrument characteristics. It will coincide with
                the ISDA calculation if your object has the standard
                characteristics. Notably:
                - The calendar should have no bank holidays, just
                  weekends.
                - The yield curve should be LIBOR piecewise ant
                  in fwd rates, with a discount factor of 1 on the
                  calculation date, which coincides with the trade
                  date.
                - Convention should be Following for yield curve and
                  contract cashflows.
                - The CDS should pay accrued and mature on standard
                  IMM dates, settle on trade date +1 and upfront
                  settle on trade date +3.
      */
		private class ObjectiveFunction : ISolver1d
		{
			public ObjectiveFunction(double target, SimpleQuote quote,IPricingEngine engine,CreditDefaultSwap.Results results)
         {
				target_ = target;
				quote_ = quote;
            engine_ = engine;
				results_ = results;
			}

			public override double value(double guess) 
			{
				quote_.setValue(guess);
				engine_.calculate();
				return results_.value.GetValueOrDefault() - target_;
         }
          
         private double target_;
         private SimpleQuote quote_;
         private IPricingEngine engine_;
         private CreditDefaultSwap.Results results_;
        
		}
		public double impliedHazardRate(double targetNPV,
												  Handle<YieldTermStructure> discountCurve,
												  DayCounter dayCounter,
												  double recoveryRate = 0.4,
												  double accuracy = 1.0e-6)
		{
			SimpleQuote flatRate = new SimpleQuote(0.0);

			Handle<DefaultProbabilityTermStructure> probability = new Handle<DefaultProbabilityTermStructure>(
            new FlatHazardRate(0, new WeekendsOnly(),new Handle<Quote>(flatRate), dayCounter));

        MidPointCdsEngine engine = new MidPointCdsEngine(probability, recoveryRate, discountCurve);
        setupArguments(engine.getArguments());
        CreditDefaultSwap.Results results = engine.getResults() as CreditDefaultSwap.Results;

        ObjectiveFunction f = new ObjectiveFunction(targetNPV, flatRate, engine, results);
        double guess = 0.001;
        double step = guess*0.1;

        return new Brent().solve(f, accuracy, guess, step);
		}

      //! Conventional/standard upfront-to-spread conversion
      /*! Under a standard ISDA model and a set of standardised
          instrument characteristics, it is the running only quoted
          spread that will make a CDS contract have an NPV of 0 when
          quoted for that running only spread.  Refer to: "ISDA
          Standard CDS converter specification." May 2009.

          The conventional recovery rate to apply in the calculation
          is as specified by ISDA, not necessarily equal to the
          market-quoted one.  It is typically 0.4 for SeniorSec and
          0.2 for subordinate.

          \note The conversion employs a flat hazard rate. As a result,
                you will not recover the market quotes.

          \note This method performs the calculation with the
                instrument characteristics. It will coincide with
                the ISDA calculation if your object has the standard
                characteristics. Notably:
                - The calendar should have no bank holidays, just
                  weekends.
                - The yield curve should be LIBOR piecewise ant
                  in fwd rates, with a discount factor of 1 on the
                  calculation date, which coincides with the trade
                  date.
                - Convention should be Following for yield curve and
                  contract cashflows.
                - The CDS should pay accrued and mature on standard
                  IMM dates, settle on trade date +1 and upfront
                  settle on trade date +3.
      */
		public double? conventionalSpread(double conventionalRecovery,
													Handle<YieldTermStructure> discountCurve,
													DayCounter dayCounter)
		{
			double flatHazardRate = impliedHazardRate(0.0,
                                                discountCurve,
                                                dayCounter,
                                                conventionalRecovery);

        Handle<DefaultProbabilityTermStructure> probability = new Handle<DefaultProbabilityTermStructure>(
            new FlatHazardRate(0, new WeekendsOnly(),flatHazardRate, dayCounter));

        MidPointCdsEngine engine = new MidPointCdsEngine(probability, conventionalRecovery,  discountCurve, true);
        setupArguments(engine.getArguments());
        engine.calculate();
		  CreditDefaultSwap.Results results = engine.getResults() as CreditDefaultSwap.Results;
        return results.fairSpread;
		}
      //@}
      //! \name Instrument interface
      //@{
		protected override void setupExpired()
		{
 			base.setupExpired();
			fairSpread_ = fairUpfront_ = 0.0;
			couponLegBPS_ = upfrontBPS_ = 0.0;
			couponLegNPV_ = defaultLegNPV_ = upfrontNPV_ = 0.0;
		}
      //@}
      // data members
      protected Protection.Side side_;
      protected double? notional_;
      protected double? upfront_;
      protected double runningSpread_;
      protected bool settlesAccrual_, paysAtDefaultTime_;
      protected Claim claim_;
      protected List<CashFlow> leg_;
      protected CashFlow upfrontPayment_;
      protected Date protectionStart_;
        // results
      protected double? fairUpfront_;
      protected double? fairSpread_;
      protected double? couponLegBPS_, couponLegNPV_;
      protected double? upfrontBPS_, upfrontNPV_;
      protected double? defaultLegNPV_;
	
		
		 
		public class Arguments : IPricingEngineArguments 
		{
			public Arguments()
			{
				side = (Protection.Side)(-1);
				notional = null;
				spread = null;
			}

			public Protection.Side side;
			public double? notional;
			public double? upfront;
			public double? spread;
			public List<CashFlow> leg;
			public CashFlow upfrontPayment;
			public bool settlesAccrual;
			public bool paysAtDefaultTime;
			public Claim claim;
			public Date protectionStart;
			public void validate()
			{
            Utils.QL_REQUIRE( side != (Protection.Side)( -1 ), () => "side not set" );
            Utils.QL_REQUIRE( notional != null, () => "notional not set" );
            Utils.QL_REQUIRE( notional != 0.0, () => "null notional set" );
            Utils.QL_REQUIRE( spread != null, () => "spread not set" );
            Utils.QL_REQUIRE( !leg.empty(), () => "coupons not set" );
            Utils.QL_REQUIRE( upfrontPayment != null, () => "upfront payment not set" );
            Utils.QL_REQUIRE( claim != null, () => "claim not set" );
            Utils.QL_REQUIRE( protectionStart != null, () => "protection start date not set" );
			}
		}


		public new class Results : Instrument.Results 
		{
			public double? fairSpread;
			public double? fairUpfront;
			public double? couponLegBPS;
			public double? couponLegNPV;
			public double? defaultLegNPV;
			public double? upfrontBPS;
			public double? upfrontNPV;
			public override void reset()
			{
				base.reset();
				fairSpread = null;
				fairUpfront = null;
				couponLegBPS = null;
				couponLegNPV = null;
				defaultLegNPV = null;
				upfrontBPS = null;
				upfrontNPV = null;
			}
		}

		public abstract class Engine : GenericEngine<CreditDefaultSwap.Arguments,CreditDefaultSwap.Results> 
		{}

	}
}
