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
    //! Pricing engine for vanilla options using binomial trees
    /*! \ingroup vanillaengines

        \test the correctness of the returned values is tested by
              checking it against analytic results.

        \todo Greeks are not overly accurate. They could be improved
              by building a tree so that it has three points at the
              current time. The value would be fetched from the middle
              one, while the two side points would be used for
              estimating partial derivatives.
    */
    public class BinomialVanillaEngine<T> : VanillaOption.Engine where T : ITreeFactory<T>, ITree, new() {
        private GeneralizedBlackScholesProcess process_;
        private int timeSteps_;

        public BinomialVanillaEngine(GeneralizedBlackScholesProcess process, int timeSteps) {
            process_ = process;
            timeSteps_ = timeSteps;

            if (!(timeSteps>0)) throw new ApplicationException("timeSteps must be positive, " + timeSteps + " not allowed");

            process_.registerWith(update);
        }

        public override void calculate() {

            DayCounter rfdc  = process_.riskFreeRate().link.dayCounter();
            DayCounter divdc = process_.dividendYield().link.dayCounter();
            DayCounter voldc = process_.blackVolatility().link.dayCounter();
            Calendar volcal = process_.blackVolatility().link.calendar();

            double s0 = process_.stateVariable().link.value();
            if (!(s0 > 0.0)) throw new ApplicationException("negative or null underlying given");
            double v = process_.blackVolatility().link.blackVol(arguments_.exercise.lastDate(), s0);
            Date maturityDate = arguments_.exercise.lastDate();
            double r = process_.riskFreeRate().link.zeroRate(maturityDate, rfdc, Compounding.Continuous, Frequency.NoFrequency).rate();
            double q = process_.dividendYield().link.zeroRate(maturityDate, divdc, Compounding.Continuous, Frequency.NoFrequency).rate();
            Date referenceDate = process_.riskFreeRate().link.referenceDate();

            // binomial trees with constant coefficient
            var flatRiskFree = new Handle<YieldTermStructure>(new FlatForward(referenceDate, r, rfdc));
            var flatDividends = new Handle<YieldTermStructure>(new FlatForward(referenceDate, q, divdc));
            var flatVol = new Handle<BlackVolTermStructure>(new BlackConstantVol(referenceDate, volcal, v, voldc));

            PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
            if (payoff== null) throw new ApplicationException("non-plain payoff given");

            double maturity = rfdc.yearFraction(referenceDate, maturityDate);

            StochasticProcess1D bs = 
                new GeneralizedBlackScholesProcess(process_.stateVariable(), flatDividends, flatRiskFree, flatVol);

            TimeGrid grid = new TimeGrid(maturity, timeSteps_);

            T tree = new T().factory(bs, maturity, timeSteps_, payoff.strike());

            BlackScholesLattice<T> lattice = new BlackScholesLattice<T>(tree, r, maturity, timeSteps_);

            DiscretizedVanillaOption option = new DiscretizedVanillaOption(arguments_, process_, grid);

            option.initialize(lattice, maturity);

            // Partial derivatives calculated from various points in the
            // binomial tree (Odegaard)

            // Rollback to third-last step, and get underlying price (s2) &
            // option values (p2) at this point
            option.rollback(grid[2]);
            Vector va2 = new Vector(option.values());
            if (!(va2.size() == 3)) throw new ApplicationException("Expect 3 nodes in grid at second step");
            double p2h = va2[2]; // high-price
            double s2 = lattice.underlying(2, 2); // high price

            // Rollback to second-last step, and get option value (p1) at
            // this point
            option.rollback(grid[1]);
            Vector va = new Vector(option.values());
            if (!(va.size() == 2)) throw new ApplicationException("Expect 2 nodes in grid at first step");
            double p1 = va[1];

            // Finally, rollback to t=0
            option.rollback(0.0);
            double p0 = option.presentValue();
            double s1 = lattice.underlying(1, 1);

            // Calculate partial derivatives
            double delta0 = (p1 - p0) / (s1 - s0);   // dp/ds
            double delta1 = (p2h - p1) / (s2 - s1);  // dp/ds

            // Store results
            results_.value = p0;
            results_.delta = delta0;
            results_.gamma = 2.0 * (delta1 - delta0) / (s2 - s0);    //d(delta)/ds
            results_.theta = Utils.blackScholesTheta(process_,
                                                     results_.value.GetValueOrDefault(),
                                                     results_.delta.GetValueOrDefault(),
                                                     results_.gamma.GetValueOrDefault());
        }
    }
}
