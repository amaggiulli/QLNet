/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
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
    //! Pricing engine for European discrete geometric average price Asian
    /*! This class implements a discrete geometric average price Asian
        option, with European exercise.  The formula is from "Asian
        Option", E. Levy (1997) in "Exotic Options: The State of the
        Art", edited by L. Clewlow, C. Strickland, pag 65-97

        \todo implement correct theta, rho, and dividend-rho calculation

        \test
        - the correctness of the returned value is tested by
          reproducing results available in literature.
        - the correctness of the available greeks is tested against
          numerical calculations.

        \ingroup asianengines
    */
    public class AnalyticDiscreteGeometricAveragePriceAsianEngine
        : DiscreteAveragingAsianOption.Engine {

        private GeneralizedBlackScholesProcess process_;

        public AnalyticDiscreteGeometricAveragePriceAsianEngine(
            GeneralizedBlackScholesProcess process){
            process_ = process;
            process_.registerWith(update);
        }

        public override void calculate()
        {
            /* this engine cannot really check for the averageType==Geometric
               since it can be used as control variate for the Arithmetic version
                QL_REQUIRE(arguments_.averageType == Average::Geometric,
                           "not a geometric average option");
            */

            if(!(arguments_.exercise.type() == Exercise.Type.European))
                throw new ApplicationException("not an European Option");

            double runningLog;
            int pastFixings;
            if (arguments_.averageType == Average.Type.Geometric) {
                if(!(arguments_.runningAccumulator>0.0))
                    throw new ApplicationException("positive running product required: "
                           + arguments_.runningAccumulator + " not allowed");
                runningLog =
                    Math.Log(arguments_.runningAccumulator.GetValueOrDefault());
                pastFixings = arguments_.pastFixings.GetValueOrDefault();
            } else {  // it is being used as control variate
                runningLog = 1.0;
                pastFixings = 0;
            }

            PlainVanillaPayoff payoff = (PlainVanillaPayoff)(arguments_.payoff);
            if (payoff == null)
                throw new ApplicationException("non-plain payoff given");

            Date referenceDate = process_.riskFreeRate().link.referenceDate();
            DayCounter rfdc  = process_.riskFreeRate().link.dayCounter();
            DayCounter divdc = process_.dividendYield().link.dayCounter();
            DayCounter voldc = process_.blackVolatility().link.dayCounter();
            List<double> fixingTimes = new InitializedList<double>(arguments_.fixingDates.Count());
            int i;
            for (i=0; i<arguments_.fixingDates.Count(); i++) {
                if (arguments_.fixingDates[i]>=referenceDate) {
                    double t = voldc.yearFraction(referenceDate,
                        arguments_.fixingDates[i]);
                    fixingTimes.Add(t);
                }
            }

            int remainingFixings = fixingTimes.Count();
            int numberOfFixings = pastFixings + remainingFixings;
            double N = numberOfFixings;

            double pastWeight   = pastFixings/N;
            double futureWeight = 1.0-pastWeight;

            /*double timeSum = std::accumulate(fixingTimes.begin(),
                                           fixingTimes.end(), 0.0);*/
            double timeSum = 0;
            fixingTimes.ForEach((ii, vv) => timeSum += fixingTimes[ii]);

            double vola = process_.blackVolatility().link.blackVol(
                                                  arguments_.exercise.lastDate(),
                                                  payoff.strike());
            double temp = 0.0;
            for (i=pastFixings+1; i<numberOfFixings; i++)
                temp += fixingTimes[i-pastFixings-1]*(N-i);
            double variance = vola*vola /N/N * (timeSum+ 2.0*temp);
            double dsigG_dsig = Math.Sqrt((timeSum + 2.0*temp))/N;
            double sigG = vola * dsigG_dsig;
            double dmuG_dsig = -(vola * timeSum)/N;

            Date exDate = arguments_.exercise.lastDate();
            double dividendRate = process_.dividendYield().link.
                zeroRate(exDate, divdc,  Compounding.Continuous, Frequency.NoFrequency).rate();
            double riskFreeRate = process_.riskFreeRate().link.
                zeroRate(exDate, rfdc,  Compounding.Continuous, Frequency.NoFrequency).rate();
            double nu = riskFreeRate - dividendRate - 0.5*vola*vola;

            double s = process_.stateVariable().link.value();
            if(!(s > 0.0))
                throw new ApplicationException("positive underlying value required");

            int M = (pastFixings == 0 ? 1 : pastFixings);
            double muG = pastWeight * runningLog/M +
                futureWeight * Math.Log(s) + nu*timeSum/N;
            double forwardPrice = Math.Exp(muG + variance / 2.0);

            double riskFreeDiscount = process_.riskFreeRate().link.discount(
                                                 arguments_.exercise.lastDate());

            BlackCalculator black = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance),
                                  riskFreeDiscount);

            results_.value = black.value();
            results_.delta = futureWeight*black.delta(forwardPrice)*forwardPrice/s;
            results_.gamma = forwardPrice*futureWeight/(s*s)
                    *(  black.gamma(forwardPrice)*futureWeight*forwardPrice
                      - pastWeight*black.delta(forwardPrice) );

            double Nx_1, nx_1;
            CumulativeNormalDistribution CND = new CumulativeNormalDistribution();
            NormalDistribution ND = new NormalDistribution();
            if (sigG > Const.QL_Epsilon) {
                double x_1  = (muG-Math.Log(payoff.strike())+variance)/sigG;
                Nx_1 = CND.value(x_1);
                nx_1 = ND.value( x_1);
            } else {
                Nx_1 = (muG > Math.Log(payoff.strike()) ? 1.0 : 0.0);
                nx_1 = 0.0;
            }
            results_.vega = forwardPrice * riskFreeDiscount *
                       ( (dmuG_dsig + sigG * dsigG_dsig)*Nx_1 + nx_1*dsigG_dsig );

            if (payoff.optionType() == Option.Type.Put)
                results_.vega -= riskFreeDiscount * forwardPrice *
                                                  (dmuG_dsig + sigG * dsigG_dsig);

            double tRho = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(),
                                          arguments_.exercise.lastDate());
            results_.rho = black.rho(tRho)*timeSum/(N*tRho)
                          - (tRho-timeSum/N)*results_.value;

            double tDiv = divdc.yearFraction(
                               process_.dividendYield().link.referenceDate(),
                               arguments_.exercise.lastDate());

            results_.dividendRho = black.dividendRho(tDiv)*timeSum /(N*tDiv);

            results_.strikeSensitivity = black.strikeSensitivity();

            results_.theta =  Utils.blackScholesTheta(process_,
                                               results_.value.GetValueOrDefault(),
                                               results_.delta.GetValueOrDefault(),
                                               results_.gamma.GetValueOrDefault());
        }
    }
}
