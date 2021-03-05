/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
   public class FdBlackScholesVanillaEngine : DividendVanillaOption.Engine
   {
      public enum CashDividendModel { Spot, Escrowed }
      public FdBlackScholesVanillaEngine(
         GeneralizedBlackScholesProcess process,
         int tGrid = 100, int xGrid = 100, int dampingSteps = 0,
         FdmSchemeDesc schemeDesc = null,
         bool localVol = false,
         double? illegalLocalVolOverwrite = null,
         CashDividendModel cashDividendModel = CashDividendModel.Spot)
      {
         process_ = process;
         tGrid_ = tGrid;
         xGrid_ = xGrid;
         dampingSteps_ = dampingSteps;
         schemeDesc_ = schemeDesc == null ? new FdmSchemeDesc().Douglas() : schemeDesc;
         localVol_ = localVol;
         illegalLocalVolOverwrite_ = illegalLocalVolOverwrite;

         process_.registerWith(update);
      }

      public FdBlackScholesVanillaEngine(
         GeneralizedBlackScholesProcess process,
         FdmQuantoHelper quantoHelper = null,
         int tGrid = 100, int xGrid = 100, int dampingSteps = 0,
         FdmSchemeDesc schemeDesc = null,
         bool localVol = false,
         double? illegalLocalVolOverwrite = null,
         CashDividendModel cashDividendModel = CashDividendModel.Spot)
      {
         process_ = process;
         tGrid_ = tGrid;
         xGrid_ = xGrid;
         dampingSteps_ = dampingSteps;
         schemeDesc_ = schemeDesc == null ? new FdmSchemeDesc().Douglas() : schemeDesc;
         localVol_ = localVol;
         illegalLocalVolOverwrite_ = illegalLocalVolOverwrite;
         quantoHelper_ = quantoHelper;

         process_.registerWith(update);
      }

      public override void calculate()
      {
         // 0. Cash dividend model
         Date exerciseDate = arguments_.exercise.lastDate();
         double maturity = process_.time(exerciseDate);
         Date settlementDate = process_.riskFreeRate().currentLink().referenceDate();

         double spotAdjustment = 0.0;
         DividendSchedule dividendSchedule = new DividendSchedule();

         switch (cashDividendModel_)
         {
            case CashDividendModel.Spot:
               dividendSchedule = arguments_.cashFlow;
               break;
            case CashDividendModel.Escrowed:
               foreach (Dividend divIter in dividendSchedule)
               {
                  Date divDate = divIter.date();

                  if (divDate <= exerciseDate && divDate >= settlementDate)
                  {
                     double divAmount = divIter.amount();
                     double discount =
                        process_.riskFreeRate().currentLink().discount(divDate) /
                        process_.dividendYield().currentLink().discount(divDate);

                     spotAdjustment -= divAmount * discount;
                  }
               }

               Utils.QL_REQUIRE(process_.x0() + spotAdjustment > 0.0,
                                () => "spot minus dividends becomes negative");
               break;
            default:
               Utils.QL_FAIL("unknwon cash dividend model");
               break;
         }

         // 1. Mesher
         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;

         Fdm1dMesher equityMesher =
            new FdmBlackScholesMesher(
            xGrid_, process_, maturity, payoff.strike(),
            null, null, 0.0001, 1.5,
            new Pair<double?, double?>(payoff.strike(), 0.1),
            dividendSchedule, quantoHelper_,
            spotAdjustment);

         FdmMesher mesher =
            new FdmMesherComposite(equityMesher);

         // 2. Calculator
         FdmInnerValueCalculator calculator = new FdmLogInnerValue(payoff, mesher, 0);

         // 3. Step conditions
         FdmStepConditionComposite conditions = FdmStepConditionComposite.vanillaComposite(
                                                   arguments_.cashFlow, arguments_.exercise,
                                                   mesher, calculator,
                                                   process_.riskFreeRate().currentLink().referenceDate(),
                                                   process_.riskFreeRate().currentLink().dayCounter());

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

         FdmBlackScholesSolver solver =
            new FdmBlackScholesSolver(
            new Handle<GeneralizedBlackScholesProcess>(process_),
            payoff.strike(), solverDesc, schemeDesc_,
            localVol_, illegalLocalVolOverwrite_);

         double spot = process_.x0();
         results_.value = solver.valueAt(spot);
         results_.delta = solver.deltaAt(spot);
         results_.gamma = solver.gammaAt(spot);
         results_.theta = solver.thetaAt(spot);
      }

      protected GeneralizedBlackScholesProcess process_;
      protected int tGrid_, xGrid_, dampingSteps_;
      protected FdmSchemeDesc schemeDesc_;
      protected bool localVol_;
      protected double? illegalLocalVolOverwrite_;
      protected FdmQuantoHelper quantoHelper_;
      protected CashDividendModel cashDividendModel_;
   }

   public class MakeFdBlackScholesVanillaEngine
   {
      public MakeFdBlackScholesVanillaEngine(GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         tGrid_ = 100;
         xGrid_ = 100;
         dampingSteps_ = 0;
         schemeDesc_ = new FdmSchemeDesc().Douglas();
         localVol_ = false;
         illegalLocalVolOverwrite_ = null;
         quantoHelper_ = null;
         cashDividendModel_ = FdBlackScholesVanillaEngine.CashDividendModel.Spot;
      }

      public MakeFdBlackScholesVanillaEngine withQuantoHelper(FdmQuantoHelper quantoHelper)
      {
         quantoHelper_ = quantoHelper;
         return this;
      }

      public MakeFdBlackScholesVanillaEngine withTGrid(int tGrid)
      {
         tGrid_ = tGrid;
         return this;
      }

      public MakeFdBlackScholesVanillaEngine withXGrid(int xGrid)
      {
         xGrid_ = xGrid;
         return this;
      }
      public MakeFdBlackScholesVanillaEngine withDampingSteps(int dampingSteps)
      {
         dampingSteps_ = dampingSteps;
         return this;
      }

      public MakeFdBlackScholesVanillaEngine withFdmSchemeDesc(FdmSchemeDesc schemeDesc)
      {
         schemeDesc_ = schemeDesc;
         return this;
      }

      public MakeFdBlackScholesVanillaEngine withLocalVol(bool localVol)
      {
         localVol_ = localVol;
         return this;
      }
      public MakeFdBlackScholesVanillaEngine withIllegalLocalVolOverwrite(
         double illegalLocalVolOverwrite)
      {
         illegalLocalVolOverwrite_ = illegalLocalVolOverwrite;
         return this;
      }

      public MakeFdBlackScholesVanillaEngine withCashDividendModel(
         FdBlackScholesVanillaEngine.CashDividendModel cashDividendModel)
      {
         cashDividendModel_ = cashDividendModel;
         return this;
      }

      public IPricingEngine getAsPricingEngine()
      {
         return new FdBlackScholesVanillaEngine(
                   process_,
                   quantoHelper_,
                   tGrid_, xGrid_, dampingSteps_,
                   schemeDesc_,
                   localVol_,
                   illegalLocalVolOverwrite_,
                   cashDividendModel_);
      }

      protected GeneralizedBlackScholesProcess process_;
      protected int tGrid_, xGrid_, dampingSteps_;
      protected FdmSchemeDesc schemeDesc_;
      protected bool localVol_;
      protected double? illegalLocalVolOverwrite_;
      protected FdmQuantoHelper quantoHelper_;
      protected FdBlackScholesVanillaEngine.CashDividendModel cashDividendModel_;
   }
}
