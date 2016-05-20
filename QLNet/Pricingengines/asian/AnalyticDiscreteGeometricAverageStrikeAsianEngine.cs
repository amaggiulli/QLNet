//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
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
using System.Text;

namespace QLNet
{
   //! Pricing engine for European discrete geometric average-strike Asian option
   /*! This class implements a discrete geometric average-strike Asian
       option, with European exercise.  The formula is from "Asian
       Option", E. Levy (1997) in "Exotic Options: The State of the
       Art", edited by L. Clewlow, C. Strickland, pag 65-97

       \test
       - the correctness of the returned value is tested by
         reproducing known good results.

       \ingroup asianengines
   */
   public class AnalyticDiscreteGeometricAverageStrikeAsianEngine : DiscreteAveragingAsianOption.Engine
   {
      public AnalyticDiscreteGeometricAverageStrikeAsianEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         process_.registerWith(update);
      }
      public override void calculate()
      {
         Utils.QL_REQUIRE(arguments_.averageType == Average.Type.Geometric,()=>
            "not a geometric average option");

         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=>
            "not an European option");

         Utils.QL_REQUIRE(arguments_.runningAccumulator > 0.0,()=>
            "positive running product required: " + arguments_.runningAccumulator + "not allowed");

         double runningLog = Math.Log(arguments_.runningAccumulator.GetValueOrDefault());
         int? pastFixings = arguments_.pastFixings;
         Utils.QL_REQUIRE(pastFixings == 0,()=> "past fixings currently not managed");

         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff != null,()=> "non-plain payoff given");

         DayCounter rfdc  = process_.riskFreeRate().link.dayCounter();
         DayCounter divdc = process_.dividendYield().link.dayCounter();
         DayCounter voldc = process_.blackVolatility().link.dayCounter();

         List<double> fixingTimes = new List<double>();
         for (int i=0; i<arguments_.fixingDates.Count; i++) 
         {
            if (arguments_.fixingDates[i]>=arguments_.fixingDates[0]) 
            {
               double t = voldc.yearFraction(arguments_.fixingDates[0],arguments_.fixingDates[i]);
               fixingTimes.Add(t);
            }
         }

         int remainingFixings = fixingTimes.Count;
         int numberOfFixings = pastFixings.GetValueOrDefault() + remainingFixings;
         double N = (double)(numberOfFixings);

         double pastWeight   = pastFixings.GetValueOrDefault()/N;
         double futureWeight = 1.0-pastWeight;

         double timeSum = 0;
         fixingTimes.ForEach( ( ii, vv ) => timeSum += fixingTimes[ii] );

         double residualTime = rfdc.yearFraction(arguments_.fixingDates[pastFixings.GetValueOrDefault()],
            arguments_.exercise.lastDate());


         double underlying = process_.stateVariable().link.value();
         Utils.QL_REQUIRE(underlying > 0.0,()=> "positive underlying value required");

         double volatility = process_.blackVolatility().link.blackVol(arguments_.exercise.lastDate(), underlying);

         Date exDate = arguments_.exercise.lastDate();
         double dividendRate = process_.dividendYield().link.zeroRate(exDate, divdc, 
            Compounding.Continuous, Frequency.NoFrequency).value();

         double riskFreeRate = process_.riskFreeRate().link.zeroRate(exDate, rfdc, 
            Compounding.Continuous, Frequency.NoFrequency).value();

         double nu = riskFreeRate - dividendRate - 0.5*volatility*volatility;

         double temp = 0.0;
         for (int i=pastFixings.GetValueOrDefault()+1; i<numberOfFixings; i++)
            temp += fixingTimes[i-pastFixings.GetValueOrDefault()-1]*(N-i);
         double variance = volatility*volatility /N/N * (timeSum + 2.0*temp);
         double covarianceTerm = volatility*volatility/N * timeSum;
         double sigmaSum_2 = variance + volatility*volatility*residualTime - 2.0*covarianceTerm;

         int M = (pastFixings.GetValueOrDefault() == 0 ? 1 : pastFixings.GetValueOrDefault());
         double runningLogAverage = runningLog/M;

         double muG = pastWeight * runningLogAverage +
                      futureWeight * Math.Log(underlying) +
                      nu*timeSum/N;

         CumulativeNormalDistribution f = new CumulativeNormalDistribution();

         double y1 = (Math.Log(underlying)+
                     (riskFreeRate-dividendRate)*residualTime-
                      muG - variance/2.0 + sigmaSum_2/2.0)
                        /Math.Sqrt(sigmaSum_2);
         double y2 = y1-Math.Sqrt(sigmaSum_2);

         switch (payoff.optionType()) 
         {
            case Option.Type.Call:
               results_.value = underlying*Math.Exp(-dividendRate*residualTime)
                                 *f.value(y1)- Math.Exp(muG + variance/2.0 - riskFreeRate*residualTime) *f.value(y2);
               break;
            case Option.Type.Put:
               results_.value = -underlying*Math.Exp(-dividendRate*residualTime)
                                *f.value(-y1)+ Math.Exp(muG + variance/2.0 - riskFreeRate*residualTime) * f.value(-y2);
               break;
            default:
               Utils.QL_FAIL("invalid option type");
               break;
         }
    }

      
  
      private GeneralizedBlackScholesProcess process_;
   }
}
