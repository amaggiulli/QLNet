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
	//! %Forward contract on a fixed-rate bond
	/*! 1. valueDate refers to the settlement date of the bond forward
		   contract.  maturityDate is the delivery (or repurchase)
		   date for the underlying bond (not the bond's maturity
		   date).

		2. Relevant formulas used in the calculations (\f$P\f$ refers
		   to a price):

		   a. \f$ P_{CleanFwd}(t) = P_{DirtyFwd}(t) -
			  AI(t=deliveryDate) \f$ where \f$ AI \f$ refers to the
			  accrued interest on the underlying bond.

		   b. \f$ P_{DirtyFwd}(t) = \frac{P_{DirtySpot}(t) -
			  SpotIncome(t)} {discountCurve->discount(t=deliveryDate)} \f$

		   c. \f$ SpotIncome(t) = \sum_i \left( CF_i \times
			  incomeDiscountCurve->discount(t_i) \right) \f$ where \f$
			  CF_i \f$ represents the ith bond cash flow (coupon
			  payment) associated with the underlying bond falling
			  between the settlementDate and the deliveryDate. (Note
			  the two different discount curves used in b. and c.)

		<b>Example: </b>
		\link Repo.cpp
		valuation of a repo on a fixed-rate bond
		\endlink

		\todo Add preconditions and tests

		\todo Create switch- if coupon goes to seller is toggled on,
			  don't consider income in the \f$ P_{DirtyFwd}(t) \f$
			  calculation.

		\todo Verify this works when the underlying is paper (in which
			  case ignore all AI.)

		\warning This class still needs to be rigorously tested

		\ingroup instruments
	*/
	public class FixedRateBondForward : Forward {
		protected FixedRateBond fixedCouponBond_;

        //! \name Constructors
        /*! If strike is given in the constructor, can calculate the
            NPV of the contract via NPV().

            If strike/forward price is desired, it can be obtained via
            forwardPrice(). In this case, the strike variable in the
            constructor is irrelevant and will be ignored.
        */
        //@{
		//Handle<YieldTermStructure> discountCurve = Handle<YieldTermStructure>(),
		//Handle<YieldTermStructure> incomeDiscountCurve = Handle<YieldTermStructure>());
		public FixedRateBondForward(Date valueDate, Date maturityDate, Position.Type type, double strike, int settlementDays,
									DayCounter dayCounter, Calendar calendar, BusinessDayConvention businessDayConvention,
									FixedRateBond fixedCouponBond,
									Handle<YieldTermStructure> discountCurve,
									Handle<YieldTermStructure> incomeDiscountCurve) 
			: base(dayCounter, calendar, businessDayConvention, settlementDays, new ForwardTypePayoff(type, strike),
				   valueDate, maturityDate, discountCurve) {
			fixedCouponBond_ = fixedCouponBond;
	        incomeDiscountCurve_ = incomeDiscountCurve;
			incomeDiscountCurve_.registerWith(update);
		}

		//! \name Calculations
        //@{

        //! (dirty) forward bond price
		public double forwardPrice() { return forwardValue(); }
		
		//! (dirty) forward bond price minus accrued on bond at delivery
		public double cleanForwardPrice() {	return forwardValue() - fixedCouponBond_.accruedAmount(maturityDate_); }

		//!  NPV of bond coupons discounted using incomeDiscountCurve
        /*! Here only coupons between max(evaluation date,settlement
            date) and maturity date of bond forward contract are
            considered income.
        */
		public override double spotIncome(Handle<YieldTermStructure> incomeDiscountCurve) {
			double income = 0.0;
			Date settlement = settlementDate();
			List<CashFlow> cf = fixedCouponBond_.cashflows();

			/*
			  the following assumes
			  1. cashflows are in ascending order !
			  2. considers as income: all coupons paid between settlementDate()
			  and contract delivery/maturity date
			*/
			for (int i = 0; i < cf.Count; ++i) {
				if (!cf[i].hasOccurred(settlement)) {
					if (cf[i].hasOccurred(maturityDate_)) {
						income += cf[i].amount() *
								  incomeDiscountCurve.link.discount(cf[i].date()) ;
					} else {
						break;
					}
				}
			}
			return income;
		}

		//!  NPV of underlying bond
		public override double spotValue() { return fixedCouponBond_.dirtyPrice(); }

		protected override void performCalculations() {
			underlyingSpotValue_ = spotValue();
			underlyingIncome_    = spotIncome(incomeDiscountCurve_);

			base.performCalculations();
		}
	}
}
