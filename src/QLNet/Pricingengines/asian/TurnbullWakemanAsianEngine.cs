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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// Turnbull-Wakeman Asian option pricing engine
   /// </summary>
   /// <remarks>
   /// Analytical pricing based on the two-moment Turnbull-Wakeman
   /// approximation.
   /// References: "Commodity Option Pricing", Iain Clark, Wiley, section 2.7.4.
   /// "Option Pricing Formulas, Second Edition", E.G. Haug, 2006, pp. 192-202.
   /// Some parts of the implementation were modeled after calculations from the
   /// CommodityAveragePriceOptionAnalyticalEngine class in Open Source Risk Engine
   /// (https://github.com/OpenSourceRisk/Engine).
   /// </remarks>
   public class TurnbullWakemanAsianEngine : DiscreteAveragingAsianOption.Engine
    {
      private GeneralizedBlackScholesProcess process_;
      public TurnbullWakemanAsianEngine(GeneralizedBlackScholesProcess process)
      {
          process_ = process;
          process_.registerWith(update);
      }

      public override void calculate()
      {
         // Enforce a few required things
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European, ()=>"not a European Option");
         Utils.QL_REQUIRE(arguments_.averageType == Average.Type.Arithmetic,()=> "must be Arithmetic Average::Type");

         // Calculate the accrued portion
         var pastFixings = arguments_.pastFixings;
         var futureFixings = arguments_.fixingDates.Count;
         double? accruedAverage = 0.0;
         if (pastFixings != 0)
         {
           accruedAverage = arguments_.runningAccumulator / (pastFixings + futureFixings);
         }
         results_.additionalResults["accrued"] = accruedAverage;

         var discount = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate());
         results_.additionalResults["discount"] = discount;

         var payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");

         // We will read the volatility off the surface at the effective strike
         var effectiveStrike = payoff!.strike() - accruedAverage;
         results_.additionalResults["strike"] = payoff.strike();
         results_.additionalResults["effective_strike"] = effectiveStrike;

         // If the effective strike is negative, exercise resp. permanent OTM is guaranteed and the
         // valuation is made easy
         var m = futureFixings + pastFixings;
         if (effectiveStrike <= 0.0)
         {
            // For a reference, see "Option Pricing Formulas", Haug, 2nd ed, p. 193
            if (payoff.optionType() == Option.Type.Call)
            {
               var spot = process_.stateVariable().link.value();
               var S_A_hat = accruedAverage;
               foreach (var fd in arguments_.fixingDates)
               {
                  S_A_hat += (spot * process_.dividendYield().link.discount(fd) /
                     process_.riskFreeRate().link.discount(fd)) / m;
               }
               results_.value = discount * (S_A_hat - payoff.strike());
               results_.delta = discount * (S_A_hat - accruedAverage) / spot;
            }
            else if (payoff.optionType() == Option.Type.Put)
            {
               results_.value = 0;
               results_.delta = 0;
            }
            results_.gamma = 0;
            return;
         }

          // We should only get this far when the effectiveStrike > 0 but will check anyway
          Utils.QL_REQUIRE(effectiveStrike > 0.0,()=> "expected effectiveStrike to be positive");

          // Expected value of the non-accrued portion of the average prices
          // In general, m will equal n below if there is no accrued. If accrued, m > n.
          double? EA = 0.0;
          var forwards = new List<double>();
          var times = new List<double>();
          var spotVars = new List<double>();
          var spotVolsVec = new List<double>(); // additional results only
          var s = process_.stateVariable().link.value();

          foreach (var fd in arguments_.fixingDates)
          {
              var dividendDiscount = process_.dividendYield().link.discount(fd);
              var riskFreeDiscountForFwdEstimation = process_.riskFreeRate().link.discount(fd);

              forwards.Add(s * dividendDiscount / riskFreeDiscountForFwdEstimation);
              times.Add(process_.blackVolatility().link.timeFromReference(fd));

              spotVars.Add(process_.blackVolatility().link.blackVariance(times.Last(), effectiveStrike.GetValueOrDefault()));
              spotVolsVec.Add(Math.Sqrt(spotVars.Last() / times.Last()));

              EA += forwards.Last();
          }
          EA /= m;

          // Expected value of A^2.
          double? EA2 = 0.0;
          var n = forwards.Count;

          for (var i = 0; i < n; ++i)
          {
              EA2 += forwards[i] * forwards[i] * Math.Exp(spotVars[i]);
              for (var j = 0; j < i; ++j)
              {
                  EA2 += 2 * forwards[i] * forwards[j] * Math.Exp(spotVars[j]);
              }
          }

          EA2 /= m * m;

          // Calculate value
          var tn = times.Last();
          var sigma = Math.Sqrt(Math.Log(EA2.GetValueOrDefault() / (EA.GetValueOrDefault() * EA.GetValueOrDefault())) / tn);

          // Populate results
          var black = new BlackCalculator(payoff.optionType(), effectiveStrike.GetValueOrDefault(), EA.GetValueOrDefault(),
             sigma * Math.Sqrt(tn), discount);

          results_.value = black.value();
          results_.delta = black.delta(s);
          results_.gamma = black.gamma(s);

          results_.additionalResults["forward"] = EA;
          results_.additionalResults["exp_A_2"] = EA2;
          results_.additionalResults["tte"] = tn;
          results_.additionalResults["sigma"] = sigma;
          results_.additionalResults["times"] = times;
          results_.additionalResults["spotVols"] = spotVolsVec;
          results_.additionalResults["forwards"] = forwards;
       }
    }
}
