/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
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
    public class BlackSwaptionEngine : SwaptionEngine{

        private Handle<YieldTermStructure> termStructure_;
        private Handle<SwaptionVolatilityStructure> volatility_;

        public BlackSwaptionEngine(Handle<YieldTermStructure> termStructure, double vol)
            : this(termStructure, vol, new Actual365Fixed()) { }
        public BlackSwaptionEngine(Handle<YieldTermStructure> termStructure,
                                 double vol, DayCounter dc )
        {
            termStructure_ = termStructure;
            volatility_ = new Handle<SwaptionVolatilityStructure>(new ConstantSwaptionVolatility(0, new NullCalendar(), BusinessDayConvention.Following, vol, dc));
            termStructure_.registerWith(update);
        }

        public BlackSwaptionEngine(Handle<YieldTermStructure> termStructure, Handle<Quote> vol)
            : this(termStructure, vol, new Actual365Fixed()) { }

        public BlackSwaptionEngine( Handle<YieldTermStructure> termStructure,
                                    Handle<Quote> vol, DayCounter dc)
        {
            termStructure_ = termStructure;
            volatility_ = new Handle<SwaptionVolatilityStructure>(new ConstantSwaptionVolatility(
                             0, new NullCalendar(), BusinessDayConvention.Following, vol, dc));
            termStructure_.registerWith(update);
            volatility_.registerWith(update);
        }

        public BlackSwaptionEngine(  Handle<YieldTermStructure> discountCurve,
                                     Handle<SwaptionVolatilityStructure> vol)
        {
            termStructure_ = discountCurve;
            volatility_=vol;
            termStructure_.registerWith(update);
            volatility_.registerWith(update);
        }

        public override void calculate() 
        {
            double basisPoint = 1.0e-4;

            Date exerciseDate = arguments_.exercise.date(0);

            // the part of the swap preceding exerciseDate should be truncated
            // to avoid taking into account unwanted cashflows
            VanillaSwap swap = arguments_.swap;

            double strike = swap.fixedRate;

            // using the forecasting curve
            swap.setPricingEngine(new DiscountingSwapEngine(swap.iborIndex().forwardingTermStructure()));
            double atmForward = swap.fairRate();

            // Volatilities are quoted for zero-spreaded swaps.
            // Therefore, any spread on the floating leg must be removed
            // with a corresponding correction on the fixed leg.
            if (swap.spread!=0.0) {
                double correction = swap.spread*Math.Abs(swap.floatingLegBPS()/swap.fixedLegBPS());
                strike -= correction;
                atmForward -= correction;
                results_.additionalResults["spreadCorrection"] = correction;
            } else {
                results_.additionalResults["spreadCorrection"] = 0.0;
            }
            results_.additionalResults["strike"] = strike;
            results_.additionalResults["atmForward"] = atmForward;

            // using the discounting curve
            swap.setPricingEngine(new DiscountingSwapEngine(termStructure_));
            double annuity;
            switch(arguments_.settlementType) {
              case Settlement.Type.Physical: {
                  annuity = Math.Abs(swap.fixedLegBPS())/basisPoint;
                  break;
              }
              case Settlement.Type.Cash: {
                  List<CashFlow> fixedLeg = swap.fixedLeg();
                  FixedRateCoupon firstCoupon =(FixedRateCoupon)fixedLeg[0];
                  DayCounter dayCount = firstCoupon.dayCounter();
                  double fixedLegCashBPS =
                      CashFlows.bps(fixedLeg,
                                     new InterestRate(atmForward, dayCount,QLNet.Compounding.Compounded,Frequency.Annual),false,
                                     termStructure_.link.referenceDate()) ;
                  annuity = Math.Abs(fixedLegCashBPS/basisPoint);
                  break;
              }
              default:
                throw new ApplicationException("unknown settlement type");
            }
            results_.additionalResults["annuity"] = annuity;

            // the swap length calculation might be improved using the value date
            // of the exercise date
            double swapLength =  volatility_.link.swapLength(exerciseDate,
                                                       arguments_.floatingPayDates.Last());
            results_.additionalResults["swapLength"] = swapLength;

            double variance = volatility_.link.blackVariance(exerciseDate,
                                                             swapLength,
                                                             strike);
            double stdDev = Math.Sqrt(variance);
            results_.additionalResults["stdDev"] = stdDev;
            Option.Type w = (arguments_.type==VanillaSwap.Type.Payer) ?
                                                    Option.Type.Call : Option.Type.Put;
            results_.value =Utils.blackFormula(w, strike, atmForward, stdDev, annuity);

            double exerciseTime = volatility_.link.timeFromReference(exerciseDate);
            results_.additionalResults["vega"] = Math.Sqrt(exerciseTime) *
                Utils.blackFormulaStdDevDerivative(strike, atmForward, stdDev, annuity);

       }

       public Handle<YieldTermStructure> termStructure() { return termStructure_; }

       public Handle<SwaptionVolatilityStructure> volatility() { return volatility_; }
      
    }
}
