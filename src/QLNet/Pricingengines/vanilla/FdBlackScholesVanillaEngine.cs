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

namespace QLNet
{
   public class FdBlackScholesVanillaEngine : DividendVanillaOption.Engine
   {
      public FdBlackScholesVanillaEngine(
              GeneralizedBlackScholesProcess process,
              int tGrid = 100, int xGrid = 100, int dampingSteps = 0,
              FdmSchemeDesc schemeDesc = null,
              bool localVol = false,
              double? illegalLocalVolOverwrite = null)
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

      public override void calculate()
      {
         // 1. Mesher
         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;

         double maturity = process_.time(arguments_.exercise.lastDate());
         Fdm1dMesher equityMesher =
             new FdmBlackScholesMesher(
                     xGrid_, process_, maturity, payoff.strike(),
                     null, null, 0.0001, 1.5,
                     new Pair<double?, double?>(payoff.strike(), 0.1));

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
   }
}
