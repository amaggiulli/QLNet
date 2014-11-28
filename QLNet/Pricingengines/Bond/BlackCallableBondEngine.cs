/*
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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
   //! Black-formula callable fixed rate bond engine
   /*! Callable fixed rate bond Black engine. The embedded (European)
       option follows the Black "European bond option" treatment in
       Hull, Fourth Edition, Chapter 20.

       \todo set additionalResults (e.g. vega, fairStrike, etc.)

       \warning This class has yet to be tested

       \ingroup callablebondengines
   */
   public class BlackCallableFixedRateBondEngine : CallableFixedRateBond.Engine 
   {
      //! volatility is the quoted fwd yield volatility, not price vol
      public BlackCallableFixedRateBondEngine(Handle<Quote> fwdYieldVol, Handle<YieldTermStructure> discountCurve)
      {
         volatility_ = new Handle<CallableBondVolatilityStructure>( new CallableBondConstantVolatility(0, new NullCalendar(),
                                                                                               fwdYieldVol,
                                                                                               new Actual365Fixed()));
         discountCurve_ = discountCurve;

         volatility_.registerWith(update);
         discountCurve_.registerWith(update);
      }
      //! volatility is the quoted fwd yield volatility, not price vol
      public BlackCallableFixedRateBondEngine(Handle<CallableBondVolatilityStructure> yieldVolStructure,
                                              Handle<YieldTermStructure> discountCurve)
      {
         volatility_ = yieldVolStructure;
         discountCurve_ = discountCurve;
         volatility_.registerWith(update);
         discountCurve_.registerWith(update);
      }

      public override void calculate()
      {
         // validate args for Black engine
         Utils.QL_REQUIRE( arguments_.putCallSchedule.Count == 1, () => "Must have exactly one call/put date to use Black Engine" );

         Date settle = arguments_.settlementDate;
         Date exerciseDate = arguments_.callabilityDates[0];
         Utils.QL_REQUIRE( exerciseDate >= settle, () => "must have exercise Date >= settlement Date" );

         List<CashFlow> fixedLeg = arguments_.cashflows;

         double value = CashFlows.npv(fixedLeg, discountCurve_, false, settle);

         double npv = CashFlows.npv(fixedLeg, discountCurve_,false,  discountCurve_.link.referenceDate());

         double fwdCashPrice = (value - spotIncome())/
                              discountCurve_.link.discount(exerciseDate);

         double cashStrike = arguments_.callabilityPrices[0];

         Option.Type type = (arguments_.putCallSchedule[0].type() ==
                              Callability.Type.Call ? Option.Type.Call : Option.Type.Put);

         double priceVol = forwardPriceVolatility();

         double exerciseTime = volatility_.link.dayCounter().yearFraction(
                                                   volatility_.link.referenceDate(),
                                                   exerciseDate);
         double embeddedOptionValue = Utils.blackFormula(type,
                                                         cashStrike,
                                                         fwdCashPrice,
                                                         priceVol*Math.Sqrt(exerciseTime));

         if (type == Option.Type.Call) 
         {
            results_.value = npv - embeddedOptionValue;
            results_.settlementValue = value - embeddedOptionValue;
         } else {
            results_.value = npv + embeddedOptionValue;
            results_.settlementValue = value + embeddedOptionValue;
         }
      }
      
      private Handle<CallableBondVolatilityStructure> volatility_;
      private Handle<YieldTermStructure> discountCurve_;
      // present value of all coupons paid during the life of option
      private double spotIncome()
      {
         //! settle date of embedded option assumed same as that of bond
         Date settlement = arguments_.settlementDate;
         List<CashFlow> cf = arguments_.cashflows;
         Date optionMaturity = arguments_.putCallSchedule[0].date();

         /* the following assumes
            1. cashflows are in ascending order !
            2. income = coupons paid between settlementDate() and put/call date
         */
         double income = 0.0;
         for (int i = 0; i < cf.Count - 1; ++i)
         {
            if (!cf[i].hasOccurred(settlement, false))
            {
               if (cf[i].hasOccurred(optionMaturity, false))
               {
                  income += cf[i].amount() *
                            discountCurve_.link.discount(cf[i].date());
               }
               else
               {
                  break;
               }
            }
         }
         return income / discountCurve_.link.discount(settlement);
      }
      
      // converts the yield volatility into a forward price volatility
      private double forwardPriceVolatility()
      {
         Date bondMaturity = arguments_.redemptionDate;
         Date exerciseDate = arguments_.callabilityDates[0];
         List<CashFlow> fixedLeg = arguments_.cashflows;

         // value of bond cash flows at option maturity
         double fwdNpv = CashFlows.npv(fixedLeg, discountCurve_,false, exerciseDate);

         DayCounter dayCounter = arguments_.paymentDayCounter;
         Frequency frequency = arguments_.frequency;

         // adjust if zero coupon bond (see also bond.cpp)
         if (frequency == Frequency.NoFrequency || frequency == Frequency.Once)
            frequency = Frequency.Annual;

         double fwdYtm = CashFlows.yield(fixedLeg,
                                         fwdNpv,
                                         dayCounter,
                                         Compounding.Compounded,
                                         frequency,
                                         false,
                                         exerciseDate);

         InterestRate fwdRate = new InterestRate(fwdYtm, dayCounter, Compounding.Compounded, frequency);

         double fwdDur = CashFlows.duration(fixedLeg,
                                            fwdRate,
                                            Duration.Type.Modified,false,
                                            exerciseDate);

         double cashStrike = arguments_.callabilityPrices[0];
         dayCounter = volatility_.link.dayCounter();
         Date referenceDate = volatility_.link.referenceDate();
         double exerciseTime = dayCounter.yearFraction(referenceDate,
                                                      exerciseDate);
         double maturityTime = dayCounter.yearFraction(referenceDate,
                                                      bondMaturity);
         double yieldVol = volatility_.link.volatility(exerciseTime,
                                                       maturityTime-exerciseTime,
                                                       cashStrike);
         double fwdPriceVol = yieldVol*fwdDur*fwdYtm;
         return fwdPriceVol;
      }
   }

   //! Black-formula callable zero coupon bond engine
   /*! Callable zero coupon bond, where the embedded (European)
       option price is assumed to obey the Black formula. Follows
       "European bond option" treatment in Hull, Fourth Edition,
       Chapter 20.

       \warning This class has yet to be tested.

       \ingroup callablebondengines
   */
   public class BlackCallableZeroCouponBondEngine : BlackCallableFixedRateBondEngine
   {

      //! volatility is the quoted fwd yield volatility, not price vol
      public BlackCallableZeroCouponBondEngine(Handle<Quote> fwdYieldVol,Handle<YieldTermStructure> discountCurve)
      : base(fwdYieldVol, discountCurve) {}

      //! volatility is the quoted fwd yield volatility, not price vol
      public BlackCallableZeroCouponBondEngine(Handle<CallableBondVolatilityStructure> yieldVolStructure,
                                               Handle<YieldTermStructure> discountCurve)
      : base(yieldVolStructure, discountCurve) {}
   }
}
