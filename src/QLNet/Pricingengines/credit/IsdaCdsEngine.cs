//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   public class IsdaCdsEngine : CreditDefaultSwap.Engine
   {

      public enum NumericalFix
      {
         None,  // as in [1] footnote 26 (i.e. 10^{-50} is added to
         // denominators $f_i+h_i$$)
         Taylor // as in [2] i.e. for $f_i+h_i < 10^{-4}$ a Taylor expansion
         // is used to avoid zero denominators
      }

      public enum AccrualBias
      {
         HalfDayBias, // as in [1] formula (50), second (error) term is
         // included
         NoBias       // as in [1], but second term in formula (50) is not included
      }

      public enum ForwardsInCouponPeriod
      {
         Flat, // as in [1], formula (52), second (error) term is included
         Piecewise // as in [1], but second term in formula (52) is not
         // included
      }

      /*! Constructor where the client code is responsible for providing a
         default curve and an interest rate curve compliant with the ISDA
         specifications.

         To be precisely consistent with the ISDA specification
         QL_USE_INDEXED_COUPON
         must not be defined. This is not checked in order not to
         kill the engine completely in this case.

         Furthermore, the ibor index in the swap rate helpers should not
         provide the evaluation date's fixing.
      */

      public IsdaCdsEngine(Handle<DefaultProbabilityTermStructure> probability,
                           double recoveryRate,
                           Handle<YieldTermStructure> discountCurve,
                           bool? includeSettlementDateFlows = null,
                           NumericalFix numericalFix = NumericalFix.Taylor,
                           AccrualBias accrualBias = AccrualBias.HalfDayBias,
                           ForwardsInCouponPeriod forwardsInCouponPeriod = ForwardsInCouponPeriod.Piecewise)
      {
         probability_ = probability;
         recoveryRate_ = recoveryRate;
         discountCurve_ = discountCurve;
         includeSettlementDateFlows_ = includeSettlementDateFlows;
         numericalFix_ = numericalFix;
         accrualBias_ = accrualBias;
         forwardsInCouponPeriod_ = forwardsInCouponPeriod;

         probability_.registerWith(update);
         discountCurve_.registerWith(update);
      }

      public Handle<YieldTermStructure> isdaRateCurve() { return discountCurve_; }
      public Handle<DefaultProbabilityTermStructure> isdaCreditCurve() { return probability_; }

      public override void calculate()
      {
         Utils.QL_REQUIRE(numericalFix_ == NumericalFix.None || numericalFix_ == NumericalFix.Taylor, () =>
                          "numerical fix must be None or Taylor");
         Utils.QL_REQUIRE(accrualBias_ == AccrualBias.HalfDayBias || accrualBias_ == AccrualBias.NoBias, () =>
                          "accrual bias must be HalfDayBias or NoBias");
         Utils.QL_REQUIRE(forwardsInCouponPeriod_ == ForwardsInCouponPeriod.Flat ||
                          forwardsInCouponPeriod_ == ForwardsInCouponPeriod.Piecewise, () =>
                          "forwards in coupon period must be Flat or Piecewise");

         // it would be possible to handle the cases which are excluded below,
         // but the ISDA engine is not explicitly specified to handle them,
         // so we just forbid them too

         Actual365Fixed dc = new Actual365Fixed();
         Actual360 dc1 = new Actual360();
         Actual360 dc2 = new Actual360(true);

         Date evalDate = Settings.evaluationDate();

         // check if given curves are ISDA compatible
         // (the interpolation is checked below)

         Utils.QL_REQUIRE(!discountCurve_.empty(), () => "no discount term structure set");
         Utils.QL_REQUIRE(!probability_.empty(), () => "no probability term structure set");
         Utils.QL_REQUIRE(discountCurve_.link.dayCounter() == dc, () =>
                          "yield term structure day counter (" + discountCurve_.link.dayCounter() + ") should be Act/365(Fixed)");
         Utils.QL_REQUIRE(probability_.link.dayCounter() == dc, () =>
                          "probability term structure day counter (" + probability_.link.dayCounter() + ") should be "
                          + "Act/365(Fixed)");
         Utils.QL_REQUIRE(discountCurve_.link.referenceDate() == evalDate, () =>
                          "yield term structure reference date (" + discountCurve_.link.referenceDate()
                          + " should be evaluation date (" + evalDate + ")");
         Utils.QL_REQUIRE(probability_.link.referenceDate() == evalDate, () =>
                          "probability term structure reference date (" + probability_.link.referenceDate()
                          + " should be evaluation date (" + evalDate + ")");
         Utils.QL_REQUIRE(arguments_.settlesAccrual, () => "ISDA engine not compatible with non accrual paying CDS");
         Utils.QL_REQUIRE(arguments_.paysAtDefaultTime, () => "ISDA engine not compatible with end period payment");
         Utils.QL_REQUIRE((arguments_.claim as FaceValueClaim) != null, () =>
                          "ISDA engine not compatible with non face value claim");

         Date maturity = arguments_.maturity;
         Date effectiveProtectionStart = Date.Max(arguments_.protectionStart, evalDate + 1);

         // collect nodes from both curves and sort them
         List<Date> yDates = new List<Date>(), cDates = new List<Date>();

         var castY1 =  discountCurve_.link as PiecewiseYieldCurve<Discount, LogLinear>;
         var castY2 = discountCurve_.link as InterpolatedForwardCurve<BackwardFlat>;
         var castY3 = discountCurve_.link as InterpolatedForwardCurve<ForwardFlat>;
         var castY4 =  discountCurve_.link as FlatForward;
         if (castY1 != null)
         {
            if (castY1.dates() != null)
               yDates = castY1.dates();
         }
         else if (castY2 != null)
         {
            yDates = castY2.dates();
         }
         else if (castY3 != null)
         {
            yDates = castY3.dates();
         }
         else if (castY4 != null)
         {
         }
         else
         {
            Utils.QL_FAIL("Yield curve must be flat forward interpolated");
         }

         var castC1 =  probability_.link as InterpolatedSurvivalProbabilityCurve<LogLinear>;
         var castC2 = probability_.link as InterpolatedHazardRateCurve<BackwardFlat>;
         var castC3 = probability_.link as FlatHazardRate;

         if (castC1 != null)
         {
            cDates = castC1.dates();
         }
         else if (castC2 != null)
         {
            cDates = castC2.dates();
         }
         else if (castC3 != null)
         {
         }
         else
         {
            Utils.QL_FAIL("Credit curve must be flat forward interpolated");
         }

         // Todo check
         List<Date> nodes = yDates.Union(cDates).ToList();

         if (nodes.empty())
         {
            nodes.Add(maturity);
         }
         double nFix = (numericalFix_ == NumericalFix.None ? 1E-50 : 0.0);

         // protection leg pricing (npv is always negative at this stage)
         double protectionNpv = 0.0;

         Date d0 = effectiveProtectionStart - 1;
         double P0 = discountCurve_.link.discount(d0);
         double Q0 = probability_.link.survivalProbability(d0);
         Date d1;
         int result = nodes.FindIndex(item => item > effectiveProtectionStart);

         for (int it = result; it < nodes.Count; ++it)
         {
            if (nodes[it] > maturity)
            {
               d1 = maturity;
               it = nodes.Count - 1; //early exit
            }
            else
            {
               d1 = nodes[it];
            }
            double P1 = discountCurve_.link.discount(d1);
            double Q1 = probability_.link.survivalProbability(d1);

            double fhat = Math.Log(P0) - Math.Log(P1);
            double hhat = Math.Log(Q0) - Math.Log(Q1);
            double fhphh = fhat + hhat;

            if (fhphh < 1E-4 && numericalFix_ == NumericalFix.Taylor)
            {
               double fhphhq = fhphh * fhphh;
               protectionNpv +=
                  P0 * Q0 * hhat * (1.0 - 0.5 * fhphh + 1.0 / 6.0 * fhphhq -
                                    1.0 / 24.0 * fhphhq * fhphh +
                                    1.0 / 120 * fhphhq * fhphhq);
            }
            else
            {
               protectionNpv += hhat / (fhphh + nFix) * (P0 * Q0 - P1 * Q1);
            }
            d0 = d1;
            P0 = P1;
            Q0 = Q1;
         }
         protectionNpv *= arguments_.claim.amount(null, arguments_.notional.Value, recoveryRate_);

         results_.defaultLegNPV = protectionNpv;

         // premium leg pricing (npv is always positive at this stage)

         double premiumNpv = 0.0, defaultAccrualNpv = 0.0;
         for (int i = 0; i < arguments_.leg.Count; ++i)
         {
            FixedRateCoupon coupon = arguments_.leg[i] as FixedRateCoupon;

            Utils.QL_REQUIRE(coupon.dayCounter() == dc ||
                             coupon.dayCounter() == dc1 ||
                             coupon.dayCounter() == dc2, () =>
                             "ISDA engine requires a coupon day counter Act/365Fixed "
                             + "or Act/360 (" + coupon.dayCounter() + ")");

            // premium coupons

            if (!arguments_.leg[i].hasOccurred(evalDate, includeSettlementDateFlows_))
            {
               double x1 = coupon.amount();
               double x2 = discountCurve_.link.discount(coupon.date());
               double x3 = probability_.link.survivalProbability(coupon.date() - 1);

               premiumNpv +=
                  coupon.amount() *
                  discountCurve_.link.discount(coupon.date()) *
                  probability_.link.survivalProbability(coupon.date() - 1);
            }

            // default accruals

            if (!new simple_event(coupon.accrualEndDate())
                .hasOccurred(effectiveProtectionStart, false))
            {
               Date start = Date.Max(coupon.accrualStartDate(), effectiveProtectionStart) - 1;
               Date end = coupon.date() - 1;
               double tstart = discountCurve_.link.timeFromReference(coupon.accrualStartDate() - 1) -
                               (accrualBias_ == AccrualBias.HalfDayBias ? 1.0 / 730.0 : 0.0);
               List<Date> localNodes = new List<Date>();
               localNodes.Add(start);
               //add intermediary nodes, if any
               if (forwardsInCouponPeriod_ == ForwardsInCouponPeriod.Piecewise)
               {
                  foreach (Date node in nodes)
                  {
                     if (node > start && node < end)
                     {
                        localNodes.Add(node);
                     }
                  }
                  //std::vector<Date>::const_iterator it0 = std::upper_bound(nodes.begin(), nodes.end(), start);
                  //std::vector<Date>::const_iterator it1 = std::lower_bound(nodes.begin(), nodes.end(), end);
                  //localNodes.insert(localNodes.end(), it0, it1);
               }
               localNodes.Add(end);

               double defaultAccrThisNode = 0.0;
               Date firstnode = localNodes.First();
               double t0 = discountCurve_.link.timeFromReference(firstnode);
               P0 = discountCurve_.link.discount(firstnode);
               Q0 = probability_.link.survivalProbability(firstnode);

               foreach (Date node in localNodes.Skip(1)) //for (++node; node != localNodes.Last(); ++node)
               {
                  double t1 = discountCurve_.link.timeFromReference(node);
                  double P1 = discountCurve_.link.discount(node);
                  double Q1 = probability_.link.survivalProbability(node);
                  double fhat = Math.Log(P0) - Math.Log(P1);
                  double hhat = Math.Log(Q0) - Math.Log(Q1);
                  double fhphh = fhat + hhat;
                  if (fhphh < 1E-4 && numericalFix_ == NumericalFix.Taylor)
                  {
                     // see above, terms up to (f+h)^3 seem more than enough,
                     // what exactly is implemented in the standard isda C
                     // code ?
                     double fhphhq = fhphh * fhphh;
                     defaultAccrThisNode +=
                        hhat * P0 * Q0 *
                        ((t0 - tstart) *
                         (1.0 - 0.5 * fhphh + 1.0 / 6.0 * fhphhq -
                          1.0 / 24.0 * fhphhq * fhphh) +
                         (t1 - t0) *
                         (0.5 - 1.0 / 3.0 * fhphh + 1.0 / 8.0 * fhphhq -
                          1.0 / 30.0 * fhphhq * fhphh));
                  }
                  else
                  {
                     defaultAccrThisNode +=
                        (hhat / (fhphh + nFix)) *
                        ((t1 - t0) * ((P0 * Q0 - P1 * Q1) / (fhphh + nFix) -
                                      P1 * Q1) +
                         (t0 - tstart) * (P0 * Q0 - P1 * Q1));
                  }

                  t0 = t1;
                  P0 = P1;
                  Q0 = Q1;
               }
               defaultAccrualNpv += defaultAccrThisNode * arguments_.notional.Value *
                                    coupon.rate() * 365.0 / 360.0;
            }
         }

         results_.couponLegNPV = premiumNpv + defaultAccrualNpv;

         // upfront flow npv

         double upfPVO1 = 0.0;
         results_.upfrontNPV = 0.0;
         if (!arguments_.upfrontPayment.hasOccurred(
                evalDate, includeSettlementDateFlows_))
         {
            upfPVO1 =
               discountCurve_.link.discount(arguments_.upfrontPayment.date());
            if (arguments_.upfrontPayment.amount().IsNotEqual(0.0))
            {
               results_.upfrontNPV = upfPVO1 * arguments_.upfrontPayment.amount();
            }
         }

         results_.accrualRebateNPV = 0.0;
         if (arguments_.accrualRebate != null &&
             arguments_.accrualRebate.amount().IsNotEqual(0.0) &&
             !arguments_.accrualRebate.hasOccurred(evalDate, includeSettlementDateFlows_))
         {
            results_.accrualRebateNPV =
               discountCurve_.link.discount(arguments_.accrualRebate.date()) *
               arguments_.accrualRebate.amount();
         }

         double upfrontSign = 1;

         if (arguments_.side == Protection.Side.Seller)
         {
            results_.defaultLegNPV *= -1.0;
            results_.accrualRebateNPV *= -1.0;
         }
         else
         {
            results_.couponLegNPV *= -1.0;
            results_.upfrontNPV *= -1.0;
         }

         results_.value = results_.defaultLegNPV + results_.couponLegNPV +
                          results_.upfrontNPV + results_.accrualRebateNPV;

         results_.errorEstimate = null;

         if (results_.couponLegNPV.IsNotEqual(0.0))
         {
            results_.fairSpread =
               -results_.defaultLegNPV * arguments_.spread /
               (results_.couponLegNPV + results_.accrualRebateNPV);
         }
         else
         {
            results_.fairSpread = null;
         }

         double upfrontSensitivity = upfPVO1 * arguments_.notional.Value;
         if (upfrontSensitivity.IsNotEqual(0.0))
         {
            results_.fairUpfront =
               -upfrontSign * (results_.defaultLegNPV + results_.couponLegNPV +
                               results_.accrualRebateNPV) /
               upfrontSensitivity;
         }
         else
         {
            results_.fairUpfront = null;
         }         

         if (arguments_.spread.IsNotEqual(0.0))
         {
            results_.couponLegBPS =
               results_.couponLegNPV * Const.BASIS_POINT / arguments_.spread;
         }
         else
         {
            results_.couponLegBPS = null;
         }

         if (arguments_.upfront != null  && arguments_.upfront.IsNotEqual(0.0))
         {
            results_.upfrontBPS =
               results_.upfrontNPV * Const.BASIS_POINT / (arguments_.upfront);
         }
         else
         {
            results_.upfrontBPS = null;
         }
      }

      private Handle<DefaultProbabilityTermStructure> probability_;
      private double recoveryRate_;
      private Handle<YieldTermStructure> discountCurve_;
      private bool? includeSettlementDateFlows_;
      private NumericalFix numericalFix_;
      private AccrualBias accrualBias_;
      private ForwardsInCouponPeriod forwardsInCouponPeriod_;
   }
}

