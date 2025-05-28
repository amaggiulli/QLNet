/*
 Copyright (C) 2008-2025 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;

namespace QLNet
{
   public class ContinuousArithmeticAsianLevyEngine : ContinuousAveragingAsianOption.Engine
   {
      private GeneralizedBlackScholesProcess process_;
      private Handle<Quote> currentAverage_ ;
      private Date startDate_;

      public ContinuousArithmeticAsianLevyEngine(GeneralizedBlackScholesProcess process, Handle<Quote> currentAverage,
          Date startDate)
      {
         process_ = process;
         currentAverage_ = currentAverage;
         startDate_ = startDate;
         process_.registerWith(update);
         currentAverage_.registerWith(update);
      }

      public override void calculate()
      {
         Utils.QL_REQUIRE(arguments_.averageType == Average.Type.Arithmetic,()=> "not an Arithmetic average option");
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=> "not an European Option");
         Utils.QL_REQUIRE(startDate_ <= process_.riskFreeRate().link.referenceDate(),()=> "startDate must be earlier than or equal to reference date");

         var rfdc  = process_.riskFreeRate().link.dayCounter();
         var divdc = process_.dividendYield().link.dayCounter();
         var voldc = process_.blackVolatility().link.dayCounter();
         var spot = process_.stateVariable().link.value();

         // payoff
         var payoff = arguments_.payoff as StrikedTypePayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");

         // original time to maturity
         var maturity = arguments_.exercise.lastDate();
         var T = rfdc.yearFraction(startDate_, arguments_.exercise.lastDate());
         // remaining time to maturity
         var T2 = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(), arguments_.exercise.lastDate());
         var strike = payoff.strike();
         var volatility = process_.blackVolatility().link.blackVol(maturity, strike);
         var N = new CumulativeNormalDistribution();

        var riskFreeRate = process_.riskFreeRate().link.zeroRate(maturity, rfdc, Compounding.Continuous, Frequency.NoFrequency);
        var dividendYield = process_.dividendYield().link.zeroRate(maturity, divdc, Compounding.Continuous, Frequency.NoFrequency);
        var b = riskFreeRate.value() - dividendYield.value();

        var Se = (Math.Abs(b) > 1000*  Const.QL_EPSILON) 
            ? ((spot/(T*b))*(Math.Exp((b-riskFreeRate.value())*T2)-Math.Exp(-riskFreeRate.value()*T2)))
            : (spot*T2/T * Math.Exp(-riskFreeRate.value()*T2));

        double X;
        if (T2 < T)
        {
            Utils.QL_REQUIRE(!currentAverage_.empty() && currentAverage_.link.isValid(),()=> "current average required");
            X = strike - ((T-T2)/T)*currentAverage_.link.value();
        }
        else
            X = strike;

        var m = (Math.Abs(b) > 1000*Const.QL_EPSILON) ? ((Math.Exp(b*T2)-1)/b) : T2;
        var M = (2*spot*spot/(b+volatility*volatility)) * (((Math.Exp((2*b+volatility*volatility)*T2)-1)
               / (2*b+volatility*volatility))-m);
        var D = M/(T*T);
        var V = Math.Log(D)-2*(riskFreeRate.value()*T2+Math.Log(Se));
        var d1 = (1/Math.Sqrt(V))*((Math.Log(D)/2)-Math.Log(X));
        var d2 = d1-Math.Sqrt(V);

        if(payoff.optionType()==Option.Type.Call)
            results_.value = Se*N.value(d1) - X*Math.Exp(-riskFreeRate.value()*T2)*N.value(d2);
        else
            results_.value = Se*N.value(d1) - X*Math.Exp(-riskFreeRate.value()*T2)*N.value(d2)
               - Se + X*Math.Exp(-riskFreeRate.value()*T2);
      }
   }
}
