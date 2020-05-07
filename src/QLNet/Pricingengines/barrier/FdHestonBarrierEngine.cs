/*
 /*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

namespace QLNet
{
   /// <summary>
   /// Finite-Differences Heston barrier option engine
   /// </summary>
   public class FdHestonBarrierEngine : GenericModelEngine<HestonModel,
      DividendBarrierOption.Arguments,
      DividendBarrierOption.Results>
   {
      // Constructor
      public FdHestonBarrierEngine(
         HestonModel model,
         int tGrid = 100, int xGrid = 100,
         int vGrid = 50, int dampingSteps = 0,
         FdmSchemeDesc schemeDesc = null,
         LocalVolTermStructure leverageFct = null)
         : base(model)
      {
         tGrid_ = tGrid;
         xGrid_ = xGrid;
         vGrid_ = vGrid;
         dampingSteps_ = dampingSteps;
         schemeDesc_ = schemeDesc == null ? new FdmSchemeDesc().Hundsdorfer() : schemeDesc;
         leverageFct_ = leverageFct;

         model_.registerWith(update);
      }

      public override void calculate()
      {
         // 1. Mesher
         HestonProcess process = model_.currentLink().process();
         double maturity = process.time(arguments_.exercise.lastDate());

         // 1.1 Variance Mesher
         int tGridMin = 5;
         int tGridAvgSteps = Math.Max(tGridMin, tGrid_ / 50);
         FdmHestonLocalVolatilityVarianceMesher varianceMesher = new FdmHestonLocalVolatilityVarianceMesher(vGrid_,
               process,
               leverageFct_,
               maturity,
               tGridAvgSteps);

         // 1.2 Equity Mesher
         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;

         double? xMin = null;
         double? xMax = null;
         if (arguments_.barrierType == Barrier.Type.DownIn
             || arguments_.barrierType == Barrier.Type.DownOut)
         {
            xMin = Math.Log(arguments_.barrier.Value);
         }
         if (arguments_.barrierType == Barrier.Type.UpIn
             || arguments_.barrierType == Barrier.Type.UpOut)
         {
            xMax = Math.Log(arguments_.barrier.Value);
         }

         Fdm1dMesher equityMesher =
            new FdmBlackScholesMesher(xGrid_,
                                      FdmBlackScholesMesher.processHelper(process.s0(),
                                                                          process.dividendYield(),
                                                                          process.riskFreeRate(),
                                                                          varianceMesher.volaEstimate()),
                                      maturity,
                                      payoff.strike(),
                                      xMin,
                                      xMax,
                                      0.0001,
                                      1.5,
                                      new Pair < double?, double? >(),
                                      arguments_.cashFlow);

         FdmMesher mesher =
            new FdmMesherComposite(equityMesher, varianceMesher);

         // 2. Calculator
         FdmInnerValueCalculator calculator =
            new FdmLogInnerValue(payoff, mesher, 0);

         // 3. Step conditions
         List<IStepCondition<Vector>> stepConditions = new List<IStepCondition<Vector>>();
         List<List<double>> stoppingTimes = new List<List<double>>();

         // 3.1 Step condition if discrete dividends
         FdmDividendHandler dividendCondition =
            new FdmDividendHandler(arguments_.cashFlow, mesher,
                                   process.riskFreeRate().currentLink().referenceDate(),
                                   process.riskFreeRate().currentLink().dayCounter(), 0);

         if (!arguments_.cashFlow.empty())
         {
            stepConditions.Add(dividendCondition);
            stoppingTimes.Add(dividendCondition.dividendTimes());
         }

         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,
                          () => "only european style option are supported");

         FdmStepConditionComposite conditions =
            new FdmStepConditionComposite(stoppingTimes, stepConditions);

         // 4. Boundary conditions
         FdmBoundaryConditionSet boundaries = new FdmBoundaryConditionSet();
         if (arguments_.barrierType == Barrier.Type.DownIn
             || arguments_.barrierType == Barrier.Type.DownOut)
         {
            boundaries.Add(
               new FdmDirichletBoundary(mesher, arguments_.rebate.Value, 0,
                                        FdmDirichletBoundary.Side.Lower));
         }

         if (arguments_.barrierType == Barrier.Type.UpIn
             || arguments_.barrierType == Barrier.Type.UpOut)
         {
            boundaries.Add(
               new FdmDirichletBoundary(mesher, arguments_.rebate.Value, 0,
                                        FdmDirichletBoundary.Side.Upper));
         }

         // 5. Solver
         FdmSolverDesc solverDesc = new FdmSolverDesc();
         solverDesc.mesher = mesher;
         solverDesc.bcSet = boundaries;
         solverDesc.condition = conditions;
         solverDesc.calculator = calculator;
         solverDesc.maturity = maturity;
         solverDesc.dampingSteps = dampingSteps_;
         solverDesc.timeSteps = tGrid_;

         FdmHestonSolver solver =
            new FdmHestonSolver(
            new Handle<HestonProcess>(process),
            solverDesc, schemeDesc_,
            new Handle<FdmQuantoHelper>(), leverageFct_);

         double spot = process.s0().currentLink().value();
         results_.value = solver.valueAt(spot, process.v0());
         results_.delta = solver.deltaAt(spot, process.v0());
         results_.gamma = solver.gammaAt(spot, process.v0());
         results_.theta = solver.thetaAt(spot, process.v0());

         // 6. Calculate vanilla option and rebate for in-barriers
         if (arguments_.barrierType == Barrier.Type.DownIn
             || arguments_.barrierType == Barrier.Type.UpIn)
         {
            // Cast the payoff
            StrikedTypePayoff castedPayoff = arguments_.payoff as StrikedTypePayoff;

            // Calculate the vanilla option
            DividendVanillaOption vanillaOption =
               new DividendVanillaOption(castedPayoff, arguments_.exercise,
                                         dividendCondition.dividendDates(),
                                         dividendCondition.dividends());

            vanillaOption.setPricingEngine(
               new FdHestonVanillaEngine(
                  model_, tGrid_, xGrid_,
                  vGrid_, dampingSteps_,
                  schemeDesc_));

            // Calculate the rebate value
            DividendBarrierOption rebateOption =
               new DividendBarrierOption(arguments_.barrierType,
                                         arguments_.barrier.Value,
                                         arguments_.rebate.Value,
                                         castedPayoff, arguments_.exercise,
                                         dividendCondition.dividendDates(),
                                         dividendCondition.dividends());

            int xGridMin = 20;
            int vGridMin = 10;
            int rebateDampingSteps
               = (dampingSteps_ > 0) ? Math.Min(1, dampingSteps_ / 2) : 0;

            rebateOption.setPricingEngine(new FdHestonRebateEngine(
                                             model_, tGrid_, Math.Max(xGridMin, xGrid_ / 4),
                                             Math.Max(vGridMin, vGrid_ / 4),
                                             rebateDampingSteps, schemeDesc_));

            results_.value = vanillaOption.NPV() + rebateOption.NPV()
                             - results_.value;
            results_.delta = vanillaOption.delta() + rebateOption.delta()
                             - results_.delta;
            results_.gamma = vanillaOption.gamma() + rebateOption.gamma()
                             - results_.gamma;
            results_.theta = vanillaOption.theta() + rebateOption.theta()
                             - results_.theta;
         }
      }

      protected int tGrid_, xGrid_, vGrid_, dampingSteps_;
      protected FdmSchemeDesc schemeDesc_;
      protected LocalVolTermStructure leverageFct_;
   }
}