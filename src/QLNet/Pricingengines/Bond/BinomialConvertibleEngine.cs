/*
 Copyright (C) 2018 Taha Ait Taleb (ait.taleb.mohamedtaha@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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
using System.Threading.Tasks;


namespace QLNet
{
    public class BinomialConvertibleEngine<T> : ConvertibleBond.option.Engine where T : ITree, ITreeFactory<T>, new()
    {
        #region -- Properties --
        private GeneralizedBlackScholesProcess process_ { get; set; }
        private int timeSteps_ { get; set; }
        #endregion


        public BinomialConvertibleEngine(GeneralizedBlackScholesProcess process, int timeSteps)
        {
            process_ = process;
            timeSteps_ = timeSteps;

            Utils.QL_REQUIRE(timeSteps > 0, () => " timeSteps must be positive");
            process_.registerWith(update);
        }

        public override void calculate()
        {
            DayCounter rfdc = process_.riskFreeRate().currentLink().dayCounter();
            DayCounter divdc = process_.dividendYield().currentLink().dayCounter();
            DayCounter voldc = process_.blackVolatility().currentLink().dayCounter();
            Calendar volcal = process_.blackVolatility().currentLink().calendar();

            double s0 = process_.x0();
            Utils.QL_REQUIRE(s0 > 0.0, () => "negative or null underlying");

            double v = process_.blackVolatility().currentLink().blackVol(arguments_.exercise.lastDate(), s0);

            Date maturityDate = arguments_.exercise.lastDate();

            InterestRate riskFreeRate = process_.riskFreeRate().currentLink().zeroRate(maturityDate, rfdc, Compounding.Continuous, Frequency.NoFrequency);
            InterestRate q = process_.dividendYield().currentLink().zeroRate(maturityDate, divdc, Compounding.Continuous, Frequency.NoFrequency);

            Date referenceDate = process_.riskFreeRate().currentLink().referenceDate();


            // substract dividends 

            ConvertibleBond.option.arguments args = arguments_ as ConvertibleBond.option.arguments;

            for (var i = 0; i < args.dividends.Count(); i++)
            {
                if (args.dividends[i].date() >= referenceDate)
                {
                    s0 -= args.dividends[i].amount() * process_.riskFreeRate().link.discount(args.dividends[i].date());
                }
                Utils.QL_REQUIRE(s0 > 0.0, () => "negative value after substracting dividends");
            }

            // binomial trees with constant coefficient 

            Handle<Quote> underlying = new Handle<Quote>(new SimpleQuote(s0));
            Handle<YieldTermStructure> flatRiskFree = new Handle<YieldTermStructure>(new FlatForward(referenceDate, riskFreeRate.value(), rfdc));
            Handle<YieldTermStructure> flatDividends = new Handle<YieldTermStructure>(new FlatForward(referenceDate, q.value(), divdc));
            Handle<BlackVolTermStructure> flatVol = new Handle<BlackVolTermStructure>(new BlackConstantVol(referenceDate, volcal, v, voldc));
            PlainVanillaPayoff payoff = args.payoff as PlainVanillaPayoff;

            Utils.QL_REQUIRE(payoff != null, () => " non-plain payoff given ");

            double maturity = rfdc.yearFraction(args.settlementDate, maturityDate);
            GeneralizedBlackScholesProcess bs = new GeneralizedBlackScholesProcess(underlying, flatDividends, flatRiskFree, flatVol);

            T Tree = new T().factory(bs, maturity, timeSteps_, payoff.strike());

            double creditSpread = args.creditSpread.currentLink().value();

            TsiveriotisFernandesLattice<T> lattice = new TsiveriotisFernandesLattice<T>(Tree, riskFreeRate.value(), maturity, timeSteps_, creditSpread, v, q.value());
            DiscretizedConvertible convertible = new DiscretizedConvertible(args, bs, new TimeGrid(maturity, timeSteps_));
            convertible.initialize(lattice, maturity);
            convertible.rollback(0.0);
            results_.value = convertible.presentValue();

            Utils.QL_REQUIRE(results_.value < Double.MaxValue, () => "floating-point overflow on tree grid");
        }
    }
}
