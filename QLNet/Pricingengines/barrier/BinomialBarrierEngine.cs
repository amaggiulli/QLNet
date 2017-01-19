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

namespace QLNet
{
   //! Pricing engine for barrier options using binomial trees
   /*! \ingroup barrierengines

       \note Timesteps for Cox-Ross-Rubinstein trees are adjusted using Boyle and Lau algorithm.
             See Journal of Derivatives, 1/1994,
             "Bumping up against the barrier with the binomial method"

       \test the correctness of the returned values is tested by
             checking it against analytic european results.
   */
   public class BinomialBarrierEngine : BarrierOption.Engine 
   {
      public delegate ITree GetTree( StochasticProcess1D process, double end, int steps, double strike );
      public delegate DiscretizedAsset GetAsset( BarrierOption.Arguments args, StochasticProcess process, TimeGrid grid = null );

      /*! \param maxTimeSteps is used to limit timeSteps when using Boyle-Lau
                   optimization. If zero (the default) the maximum number of 
                   steps is calculated by an heuristic: anything when < 1000,
                   otherwise no more than 5*timeSteps.
                   If maxTimeSteps is equal to timeSteps Boyle-Lau is disabled.
                   Likewise if the lattice is not CoxRossRubinstein Boyle-Lau is 
                   disabled and maxTimeSteps ignored.
      */
      public BinomialBarrierEngine( GetTree getTree, GetAsset getAsset , 
         GeneralizedBlackScholesProcess process, int timeSteps, int maxTimeSteps = 0 )
      {
         process_ = process;
         timeSteps_ = timeSteps;
         maxTimeSteps_ = maxTimeSteps;
         getTree_ = getTree;
         getAsset_ = getAsset;

         Utils.QL_REQUIRE(timeSteps>0,()=>
            "timeSteps must be positive, " + timeSteps + " not allowed");
         Utils.QL_REQUIRE(maxTimeSteps==0 || maxTimeSteps>=timeSteps,()=>
            "maxTimeSteps must be zero or greater than or equal to timeSteps, " + maxTimeSteps + " not allowed");
         if (maxTimeSteps_== 0)
               maxTimeSteps_ = Math.Max( (int)1000, timeSteps_*5);
         process_.registerWith(update);
      }

      public override void calculate()
      {
         DayCounter rfdc  = process_.riskFreeRate().link.dayCounter();
         DayCounter divdc = process_.dividendYield().link.dayCounter();
         DayCounter voldc = process_.blackVolatility().link.dayCounter();
         Calendar volcal = process_.blackVolatility().link.calendar();

         double s0 = process_.stateVariable().link.value();
         Utils.QL_REQUIRE(s0 > 0.0,()=> "negative or null underlying given");
         double v = process_.blackVolatility().link.blackVol(arguments_.exercise.lastDate(), s0);
         Date maturityDate = arguments_.exercise.lastDate();
         double r = process_.riskFreeRate().link.zeroRate(maturityDate,rfdc, Compounding.Continuous, Frequency.NoFrequency).value();
         double q = process_.dividendYield().link.zeroRate(maturityDate,divdc, Compounding.Continuous, Frequency.NoFrequency).value();
         Date referenceDate = process_.riskFreeRate().link.referenceDate();

         // binomial trees with constant coefficient
         Handle<YieldTermStructure> flatRiskFree = new Handle<YieldTermStructure>(new FlatForward(referenceDate, r, rfdc));
         Handle<YieldTermStructure> flatDividends = new Handle<YieldTermStructure>(new FlatForward(referenceDate, q, divdc));
         Handle<BlackVolTermStructure> flatVol = new Handle<BlackVolTermStructure>(new BlackConstantVol(referenceDate, volcal, v, voldc));

         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
         Utils.QL_REQUIRE(payoff != null,()=> "non-striked payoff given");

         double maturity = rfdc.yearFraction(referenceDate, maturityDate);

         StochasticProcess1D bs = new GeneralizedBlackScholesProcess(process_.stateVariable(),
            flatDividends, flatRiskFree, flatVol);

         // correct timesteps to ensure a (local) minimum, using Boyle and Lau
         // approach. See Journal of Derivatives, 1/1994,
         // "Bumping up against the barrier with the binomial method"
         // Note: this approach works only for CoxRossRubinstein lattices, so
         // is disabled if T is not a CoxRossRubinstein or derived from it.
         int optimum_steps = timeSteps_;
         if ( maxTimeSteps_ > timeSteps_ && s0 > 0 && arguments_.barrier > 0 )  // boost::is_base_of<CoxRossRubinstein, T>::value && 
         {
            double divisor;
            if (s0 > arguments_.barrier)
               divisor = Math.Pow(Math.Log(s0 / arguments_.barrier.Value), 2);
            else
               divisor = Math.Pow(Math.Log(arguments_.barrier.Value / s0), 2);
            if (!Utils.close(divisor,0)) 
            {
               for (int i=1; i < timeSteps_ ; ++i) 
               {
                  int optimum = (int)(( i*i * v*v * maturity) / divisor);
                  if (timeSteps_ < optimum) 
                  {
                     optimum_steps = optimum;
                     break; // found first minimum with iterations>=timesteps
                  }
               }
            }

            if (optimum_steps > maxTimeSteps_) 
               optimum_steps = maxTimeSteps_; // too high, limit
         }

         TimeGrid grid = new TimeGrid(maturity, optimum_steps);

         ITree tree = getTree_(bs, maturity, optimum_steps, payoff.strike());

         BlackScholesLattice<ITree> lattice = new BlackScholesLattice<ITree>( tree, r, maturity, optimum_steps );

         DiscretizedAsset option = getAsset_( arguments_, process_, grid );
         option.initialize(lattice, maturity);

         // Partial derivatives calculated from various points in the
         // binomial tree 
         // (see J.C.Hull, "Options, Futures and other derivatives", 6th edition, pp 397/398)

         // Rollback to third-last step, and get underlying prices (s2) &
         // option values (p2) at this point
         option.rollback(grid[2]);
         Vector va2 = new Vector(option.values());
         Utils.QL_REQUIRE(va2.size() == 3,()=> "Expect 3 nodes in grid at second step");
         double p2u = va2[2]; // up
         double p2m = va2[1]; // mid
         double p2d = va2[0]; // down (low)
         double s2u = lattice.underlying(2, 2); // up price
         double s2m = lattice.underlying(2, 1); // middle price
         double s2d = lattice.underlying(2, 0); // down (low) price

         // calculate gamma by taking the first derivate of the two deltas
         double delta2u = (p2u - p2m)/(s2u-s2m);
         double delta2d = (p2m-p2d)/(s2m-s2d);
         double gamma = (delta2u - delta2d) / ((s2u-s2d)/2);

         // Rollback to second-last step, and get option values (p1) at
         // this point
         option.rollback(grid[1]);
         Vector va = new Vector(option.values());
         Utils.QL_REQUIRE(va.size() == 2,()=> "Expect 2 nodes in grid at first step");
         double p1u = va[1];
         double p1d = va[0];
         double s1u = lattice.underlying(1, 1); // up (high) price
         double s1d = lattice.underlying(1, 0); // down (low) price

         double delta = (p1u - p1d) / (s1u - s1d);

         // Finally, rollback to t=0
         option.rollback(0.0);
         double p0 = option.presentValue();

         // Store results
         results_.value = p0;
         results_.delta = delta;
         results_.gamma = gamma;
         // theta can be approximated by calculating the numerical derivative
         // between mid value at third-last step and at t0. The underlying price
         // is the same, only time varies.
         results_.theta = (p2m - p0) / grid[2];
      }

      private GeneralizedBlackScholesProcess process_;
      private int timeSteps_;
      private int maxTimeSteps_;
      private GetTree getTree_;
      private GetAsset getAsset_;

   }
}
