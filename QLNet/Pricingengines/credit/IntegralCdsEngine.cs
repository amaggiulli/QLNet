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
	public class IntegralCdsEngine : CreditDefaultSwap.Engine 
	{
		const double basisPoint = 1.0e-4;

		public IntegralCdsEngine(Period step, Handle<DefaultProbabilityTermStructure> probability,
				 double recoveryRate, Handle<YieldTermStructure> discountCurve, bool? includeSettlementDateFlows = null)
		{
			integrationStep_ = step ;
			probability_ = probability;
			recoveryRate_ = recoveryRate;
			discountCurve_ = discountCurve;
			includeSettlementDateFlows_ = includeSettlementDateFlows;

			probability_.registerWith(update);
			discountCurve_.registerWith(update);
		}

		public override void calculate()
		{
         Utils.QL_REQUIRE( integrationStep_ != null, () => "null period set" );
         Utils.QL_REQUIRE( !discountCurve_.empty(), () => "no discount term structure set" );
         Utils.QL_REQUIRE( !probability_.empty(), () => "no probability term structure set" );

			Date today = Settings.evaluationDate();
			Date settlementDate = discountCurve_.link.referenceDate();

        // Upfront Flow NPV. Either we are on-the-run (no flow)
        // or we are forward start
        double upfPVO1 = 0.0;
        if(!arguments_.upfrontPayment.hasOccurred( settlementDate, includeSettlementDateFlows_)) 
		  {
            // date determining the probability survival so we have to pay
            // the upfront (did not knock out)
            Date effectiveUpfrontDate = 
                arguments_.protectionStart > probability_.link.referenceDate() ?
                    arguments_.protectionStart : probability_.link.referenceDate();
            upfPVO1 =
                probability_.link.survivalProbability(effectiveUpfrontDate) *
                discountCurve_.link.discount(arguments_.upfrontPayment.date());
        }
        results_.upfrontNPV = upfPVO1 * arguments_.upfrontPayment.amount();

        results_.couponLegNPV = 0.0;
        results_.defaultLegNPV = 0.0;
        for (int i=0; i<arguments_.leg.Count; ++i) 
		  {
            if (arguments_.leg[i].hasOccurred(settlementDate,
                                               includeSettlementDateFlows_))
                continue;

            FixedRateCoupon coupon = arguments_.leg[i] as FixedRateCoupon;

            // In order to avoid a few switches, we calculate the NPV
            // of both legs as a positive quantity. We'll give them
            // the right sign at the end.

            Date paymentDate = coupon.date(),
                 startDate = (i == 0 ? arguments_.protectionStart :
                                       coupon.accrualStartDate()),
                 endDate = coupon.accrualEndDate();
            Date effectiveStartDate =
                (startDate <= today && today <= endDate) ? today : startDate;
            double couponAmount = coupon.amount();

            double S = probability_.link.survivalProbability(paymentDate);

            // On one side, we add the fixed rate payments in case of
            // survival.
            results_.couponLegNPV +=
                S * couponAmount * discountCurve_.link.discount(paymentDate);

            // On the other side, we add the payment (and possibly the
            // accrual) in case of default.

            Period step = integrationStep_;
            Date d0 = effectiveStartDate;
            Date d1 = Date.Min(d0 + step, endDate);
            double P0 = probability_.link.defaultProbability(d0);
            double endDiscount = discountCurve_.link.discount(paymentDate);
            do 
				{
                double B =
                    arguments_.paysAtDefaultTime ?
                    discountCurve_.link.discount(d1) :
                    endDiscount;

                double P1 = probability_.link.defaultProbability(d1);
                double dP = P1 - P0;

                // accrual...
                if (arguments_.settlesAccrual) {
                    if (arguments_.paysAtDefaultTime)
                        results_.couponLegNPV +=
                            coupon.accruedAmount(d1) * B * dP;
                    else
                        results_.couponLegNPV +=
                            couponAmount * B * dP;
                }

                // ...and claim.
                double claim = arguments_.claim.amount(d1,
                                                      arguments_.notional.Value,
                                                      recoveryRate_);
                results_.defaultLegNPV += claim * B * dP;

                // setup for next time around the loop
                P0 = P1;
                d0 = d1;
                d1 = Date.Min(d0 + step, endDate);
            } 
				while (d0 < endDate);
        }

        double upfrontSign = 1.0;
        switch (arguments_.side) {
          case Protection.Side.Seller:
            results_.defaultLegNPV *= -1.0;
            break;
          case Protection.Side.Buyer:
            results_.couponLegNPV *= -1.0;
            results_.upfrontNPV   *= -1.0;
            upfrontSign = -1.0;
            break;
          default:
            Utils.QL_FAIL("unknown protection side");
				  break;
        }

        results_.value =
            results_.defaultLegNPV+results_.couponLegNPV+results_.upfrontNPV;
        results_.errorEstimate = null;

        if (results_.couponLegNPV != 0.0) {
            results_.fairSpread =
                -results_.defaultLegNPV*arguments_.spread/results_.couponLegNPV;
        } else {
            results_.fairSpread = null;
        }

        double upfrontSensitivity = upfPVO1 * arguments_.notional.Value;
        if (upfrontSensitivity != 0.0) {
            results_.fairUpfront =
                -upfrontSign*(results_.defaultLegNPV + results_.couponLegNPV)
                / upfrontSensitivity;
        } else {
            results_.fairUpfront = null;
        }


        if (arguments_.spread != 0.0) {
            results_.couponLegBPS =
                results_.couponLegNPV*basisPoint/arguments_.spread;
        } else {
            results_.couponLegBPS = null;
        }

        if (arguments_.upfront.HasValue && arguments_.upfront.Value != 0.0) {
            results_.upfrontBPS =
                results_.upfrontNPV*basisPoint/(arguments_.upfront.Value);
        } else {
            results_.upfrontBPS = null;
        }
    }

		

		private Period integrationStep_;
		private Handle<DefaultProbabilityTermStructure> probability_;
		private double recoveryRate_;
		private Handle<YieldTermStructure> discountCurve_;
		private bool? includeSettlementDateFlows_;
	}
}
