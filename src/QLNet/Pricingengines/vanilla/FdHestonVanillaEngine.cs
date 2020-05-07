/*
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
   public class FdHestonVanillaEngine : GenericModelEngine<HestonModel,
      DividendVanillaOption.Arguments,
      DividendVanillaOption.Results>
   {
      public FdHestonVanillaEngine(
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
         strikes_ = new List<double>();
         cachedArgs2results_ = new List<Pair<DividendVanillaOption.Arguments, OneAssetOption.Results>>();
         quantoHelper_ = null;

         model_.registerWith(update);
      }

      public FdHestonVanillaEngine(
         HestonModel model,
         FdmQuantoHelper quantoHelper,
         int tGrid, int xGrid, int vGrid, int dampingSteps,
         FdmSchemeDesc schemeDesc,
         LocalVolTermStructure leverageFct)
         : base(model)
      {
         tGrid_ = tGrid;
         xGrid_ = xGrid;
         vGrid_ = vGrid;
         dampingSteps_ = dampingSteps;
         schemeDesc_ = schemeDesc;
         leverageFct_ = leverageFct;
         strikes_ = new List<double>();
         cachedArgs2results_ = new List<Pair<DividendVanillaOption.Arguments, OneAssetOption.Results>>();
         quantoHelper_ = quantoHelper;
      }

      public FdmSolverDesc getSolverDesc(double x)
      {
         // 1. Mesher
         HestonProcess process = model_.currentLink().process();
         double maturity = process.time(arguments_.exercise.lastDate());

         // 1.1 The variance mesher
         int tGridMin = 5;
         FdmHestonVarianceMesher varianceMesher =
            new FdmHestonVarianceMesher(vGrid_, process,
                                        maturity, Math.Max(tGridMin, tGrid_ / 50));

         // 1.2 The equity mesher
         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;

         Fdm1dMesher equityMesher;
         if (strikes_.empty())
         {
            equityMesher = new FdmBlackScholesMesher(
               xGrid_,
               FdmBlackScholesMesher.processHelper(
                  process.s0(), process.dividendYield(),
                  process.riskFreeRate(), varianceMesher.volaEstimate()),
               maturity, payoff.strike(),
               null, null, 0.0001, 2.0,
               new Pair < double?, double? >(payoff.strike(), 0.1),
               arguments_.cashFlow);
         }
         else
         {
            Utils.QL_REQUIRE(arguments_.cashFlow.empty(), () => "multiple strikes engine "
                             + "does not work with discrete dividends");
            equityMesher = new FdmBlackScholesMultiStrikeMesher(
               xGrid_,
               FdmBlackScholesMesher.processHelper(
                  process.s0(), process.dividendYield(),
                  process.riskFreeRate(), varianceMesher.volaEstimate()),
               maturity, strikes_, 0.0001, 1.5,
               new Pair < double?, double? >(payoff.strike(), 0.075));
         }

         FdmMesher mesher = new FdmMesherComposite(equityMesher, varianceMesher);

         // 2. Calculator
         FdmInnerValueCalculator calculator = new FdmLogInnerValue(arguments_.payoff, mesher, 0);

         // 3. Step conditions
         FdmStepConditionComposite conditions =
            FdmStepConditionComposite.vanillaComposite(
               arguments_.cashFlow, arguments_.exercise,
               mesher, calculator,
               process.riskFreeRate().currentLink().referenceDate(),
               process.riskFreeRate().currentLink().dayCounter());

         // 4. Boundary conditions
         FdmBoundaryConditionSet boundaries = new FdmBoundaryConditionSet();

         // 5. Solver
         FdmSolverDesc solverDesc = new FdmSolverDesc();
         solverDesc.mesher = mesher;
         solverDesc.bcSet = boundaries;
         solverDesc.condition = conditions;
         solverDesc.calculator = calculator;
         solverDesc.maturity = maturity;
         solverDesc.dampingSteps = dampingSteps_;
         solverDesc.timeSteps = tGrid_;

         return solverDesc;
      }

      public override void calculate()
      {
         // cache lookup for precalculated results
         for (int i = 0; i < cachedArgs2results_.Count; ++i)
         {
            if (cachedArgs2results_[i].first.exercise.type()
                == arguments_.exercise.type()
                && cachedArgs2results_[i].first.exercise.dates()
                == arguments_.exercise.dates())
            {
               PlainVanillaPayoff p1 = arguments_.payoff as PlainVanillaPayoff;
               PlainVanillaPayoff p2 = cachedArgs2results_[i].first.payoff as PlainVanillaPayoff;

               if (p1 != null && p1.strike() == p2.strike()
                   && p1.optionType() == p2.optionType())
               {
                  Utils.QL_REQUIRE(arguments_.cashFlow.empty(),
                                   () => "multiple strikes engine does "
                                   + "not work with discrete dividends");
                  results_ = cachedArgs2results_[i].second;
                  return;
               }
            }
         }

         HestonProcess process = model_.currentLink().process();

         FdmHestonSolver solver = new FdmHestonSolver(
            new Handle<HestonProcess>(process),
            getSolverDesc(1.5), schemeDesc_,
            new Handle<FdmQuantoHelper>(), leverageFct_);

         double v0 = process.v0();
         double spot = process.s0().currentLink().value();

         results_.value = solver.valueAt(spot, v0);
         results_.delta = solver.deltaAt(spot, v0);
         results_.gamma = solver.gammaAt(spot, v0);
         results_.theta = solver.thetaAt(spot, v0);

         cachedArgs2results_ = new InitializedList<Pair<DividendVanillaOption.Arguments, DividendVanillaOption.Results>>(strikes_.Count);
         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
         for (int i = 0; i < strikes_.Count; ++i)
         {
            cachedArgs2results_[i] = new Pair<DividendVanillaOption.Arguments, OneAssetOption.Results>(new DividendVanillaOption.Arguments(), new OneAssetOption.Results());
            cachedArgs2results_[i].first.exercise = arguments_.exercise;
            cachedArgs2results_[i].first.payoff = new PlainVanillaPayoff(payoff.optionType(), strikes_[i]);
            double d = payoff.strike() / strikes_[i];

            cachedArgs2results_[i].second.value = solver.valueAt(spot * d, v0) / d;
            cachedArgs2results_[i].second.delta = solver.deltaAt(spot * d, v0);
            cachedArgs2results_[i].second.gamma = solver.gammaAt(spot * d, v0) * d;
            cachedArgs2results_[i].second.theta = solver.thetaAt(spot * d, v0) / d;
         }
      }

      public override IPricingEngineArguments getArguments()
      {
         DividendVanillaOption.Arguments arguments = arguments_;
         Utils.QL_REQUIRE(arguments != null, () => "wrong engine type");

         if (arguments.cashFlow == null)
            arguments.cashFlow = new DividendSchedule();

         return arguments_;
      }

      public override void update()
      {
         cachedArgs2results_.Clear();
         base.update();
      }

      public void enableMultipleStrikesCaching(List<double> strikes)
      {
         strikes_ = strikes;
         cachedArgs2results_.Clear();
      }

      protected int tGrid_, xGrid_, vGrid_, dampingSteps_;
      protected FdmSchemeDesc schemeDesc_;
      protected List<double> strikes_;
      protected LocalVolTermStructure leverageFct_;
      protected FdmQuantoHelper quantoHelper_;
      protected List<Pair<DividendVanillaOption.Arguments, DividendVanillaOption.Results>> cachedArgs2results_;
   }

   public class MakeFdHestonVanillaEngine
   {
      public MakeFdHestonVanillaEngine(HestonModel hestonModel)
      {
         hestonModel_ = hestonModel;
         tGrid_ = 100;
         xGrid_ = 100;
         vGrid_ = 50;
         dampingSteps_ = 0;
         schemeDesc_ = new FdmSchemeDesc().Hundsdorfer();
         leverageFct_ = null;
         quantoHelper_ = null;
      }

      public MakeFdHestonVanillaEngine withQuantoHelper(
         FdmQuantoHelper quantoHelper)
      {
         quantoHelper_ = quantoHelper;
         return this;
      }

      public MakeFdHestonVanillaEngine withTGrid(int tGrid)
      {
         tGrid_ = tGrid;
         return this;
      }
      public MakeFdHestonVanillaEngine withXGrid(int xGrid)
      {
         xGrid_ = xGrid;
         return this;
      }
      public MakeFdHestonVanillaEngine withVGrid(int vGrid)
      {
         vGrid_ = vGrid;
         return this;
      }
      public MakeFdHestonVanillaEngine withDampingSteps(
         int dampingSteps)
      {
         dampingSteps_ = dampingSteps;
         return this;
      }

      public MakeFdHestonVanillaEngine withFdmSchemeDesc(
         FdmSchemeDesc schemeDesc)
      {
         schemeDesc_ = schemeDesc;
         return this;
      }

      public MakeFdHestonVanillaEngine withLeverageFunction(
         LocalVolTermStructure leverageFct)
      {
         leverageFct_ = leverageFct;
         return this;
      }

      public IPricingEngine getAsPricingEngine()
      {
         return new FdHestonVanillaEngine(
                   hestonModel_,
                   quantoHelper_,
                   tGrid_, xGrid_, vGrid_, dampingSteps_,
                   schemeDesc_,
                   leverageFct_);
      }

      protected HestonModel hestonModel_;
      protected int tGrid_, xGrid_, vGrid_, dampingSteps_;
      protected FdmSchemeDesc schemeDesc_;
      protected LocalVolTermStructure leverageFct_;
      protected FdmQuantoHelper quantoHelper_;
   }
}
