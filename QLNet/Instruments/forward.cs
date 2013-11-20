/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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
	//! Abstract base forward class
	/*! Derived classes must implement the virtual functions spotValue() (NPV or spot price) and spotIncome() associated
		with the specific relevant underlying (e.g. bond, stock, commodity, loan/deposit). These functions must be used to set the
		protected member variables underlyingSpotValue_ and underlyingIncome_ within performCalculations() in the derived
		class before the base-class implementation is called.

		spotIncome() refers generically to the present value of coupons, dividends or storage costs.

		discountCurve_ is the curve used to discount forward contract cash flows back to the evaluation day, as well as to obtain
		forward values for spot values/prices.

		incomeDiscountCurve_, which for generality is not automatically set to the discountCurve_, is the curve used to
		discount future income/dividends/storage-costs etc back to the evaluation date.

		\todo Add preconditions and tests

		\warning This class still needs to be rigorously tested

		\ingroup instruments
	*/
	public abstract class Forward : Instrument {
		/*! derived classes must set this, typically via spotIncome() */
        protected double underlyingIncome_;
        /*! derived classes must set this, typically via spotValue() */
		protected double underlyingSpotValue_;

		protected DayCounter dayCounter_;
		protected Calendar calendar_;
		protected BusinessDayConvention businessDayConvention_;
		protected int settlementDays_;
		protected Payoff payoff_;
        /*! valueDate = settlement date (date the fwd contract starts accruing) */
		protected Date valueDate_;
        //! maturityDate of the forward contract or delivery date of underlying
		protected Date maturityDate_;
		protected Handle<YieldTermStructure> discountCurve_;
        /*! must set this in derived classes, based on particular underlying */
		protected Handle<YieldTermStructure> incomeDiscountCurve_;


		//protected Forward(DayCounter dayCounter, Calendar calendar, BusinessDayConvention businessDayConvention,
		//                  int settlementDays, Payoff payoff, Date valueDate, Date maturityDate,
		//                  Handle<YieldTermStructure> discountCurve = Handle<YieldTermStructure>()) {
		protected Forward(DayCounter dayCounter, Calendar calendar, BusinessDayConvention businessDayConvention,
						  int settlementDays, Payoff payoff, Date valueDate, Date maturityDate,
						  Handle<YieldTermStructure> discountCurve) {
			dayCounter_ = dayCounter;
			calendar_ = calendar;
			businessDayConvention_ = businessDayConvention;
			settlementDays_ = settlementDays;
			payoff_ = payoff;
			valueDate_ = valueDate;
			maturityDate_ = maturityDate;
			discountCurve_ = discountCurve;

			maturityDate_ = calendar_.adjust(maturityDate_, businessDayConvention_);

			Settings.registerWith(update);
			discountCurve_.registerWith(update);
		}

		public virtual Date settlementDate() {
			Date d = calendar_.advance(Settings.evaluationDate(), settlementDays_, TimeUnit.Days);
			return Date.Max(d,valueDate_);
		}

		public override bool isExpired() {
			#if QL_TODAYS_PAYMENTS
			    return maturityDate_ < settlementDate();
			#else
				return maturityDate_ <= settlementDate();
			#endif
		}


		//! returns spot value/price of an underlying financial instrument
        public abstract double spotValue();
        //! NPV of income/dividends/storage-costs etc. of underlying instrument
        public abstract double spotIncome(Handle<YieldTermStructure> incomeDiscountCurve);

		//! \name Calculations
		//@{
		//! forward value/price of underlying, discounting income/dividends
		/*! \note if this is a bond forward price, is must be a dirty
				  forward price.
		*/
		public double forwardValue() {
			calculate();
			return (underlyingSpotValue_ - underlyingIncome_ )/ discountCurve_.link.discount(maturityDate_);
		}

		/*! Simple yield calculation based on underlying spot and
		forward values, taking into account underlying income.
		When \f$ t>0 \f$, call with:
		underlyingSpotValue=spotValue(t),
		forwardValue=strikePrice, to get current yield. For a
		repo, if \f$ t=0 \f$, impliedYield should reproduce the
		spot repo rate. For FRA's, this should reproduce the
		relevant zero rate at the FRA's maturityDate_;
		*/
		public InterestRate impliedYield(double underlyingSpotValue, double forwardValue, Date settlementDate,
                                         Compounding compoundingConvention, DayCounter dayCounter) {

			double tenor = dayCounter.yearFraction(settlementDate,maturityDate_) ;
			double compoundingFactor = forwardValue/ (underlyingSpotValue-spotIncome(incomeDiscountCurve_)) ;
			return InterestRate.impliedRate(compoundingFactor,dayCounter,compoundingConvention,Frequency.Annual,tenor);
		}

		protected override void performCalculations() {
			if (discountCurve_.empty())
				throw new ApplicationException("no discounting term structure set to Forward");

			ForwardTypePayoff ftpayoff = payoff_ as ForwardTypePayoff;
			double fwdValue = forwardValue();
			NPV_ = ftpayoff.value(fwdValue) * discountCurve_.link.discount(maturityDate_);
		}
	}

	//! Class for forward type payoffs
    public class ForwardTypePayoff : Payoff {
		protected Position.Type type_;
        public Position.Type forwardType() { return type_; }

		protected double strike_;
        public double strike() { return strike_; }

		public ForwardTypePayoff(Position.Type type, double strike) {
			type_ = type;
			strike_ = strike;
			if (strike < 0.0)
				throw new ApplicationException("negative strike given");
        }

        //! \name Payoff interface
        public override string name() { return "Forward";}
		public override string description()  {
			string result = name() + ", " + strike() + " strike";
			return result;
		}
        public override double value(double price)  {
			switch (type_) {
				case Position.Type.Long:
					return (price-strike_);
				case Position.Type.Short:
					return (strike_-price);
				default:
					throw new ApplicationException("unknown/illegal position type");
			}
		}
    };
}
