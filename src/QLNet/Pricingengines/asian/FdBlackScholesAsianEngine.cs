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

namespace QLNet
{
   /// <summary>
   /// Finite-differences pricing engine for discrete arithmetic average price Asian options.
   /// </summary>
   public class FdBlackScholesAsianEngine(GeneralizedBlackScholesProcess process,
      int tGrid = 100, int xGrid = 100, int aGrid = 50, FdmSchemeDesc fdmSchemeType = null)
      : GenericEngine<DiscreteAveragingAsianOption.Arguments, OneAssetOption.Results>
   {
      private readonly FdmSchemeDesc schemeDesc_ = fdmSchemeType ?? new FdmSchemeDesc().Douglas();

      public override void calculate()
      {
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=>
            "European exercise supported only");
         Utils.QL_REQUIRE(arguments_.averageType == Average.Type.Arithmetic,()=>
            "Arithmetic averaging supported only");
         Utils.QL_REQUIRE(arguments_.runningAccumulator == 0 || arguments_.pastFixings > 0, ()=>
            "Running average requires at least one past fixing");

         // 1. Mesher
         var payoff = new StrikedTypePayoff(arguments_.payoff);
         var maturity = process.time(arguments_.exercise.lastDate());
         var equityMesher = new FdmBlackScholesMesher(xGrid, process, maturity, payoff.strike());

         var spot = process.x0();
         Utils.QL_REQUIRE(spot > 0.0, ()=> "negative or null underlying given");

         var avg = arguments_.runningAccumulator == 0
            ? spot : arguments_.runningAccumulator/arguments_.pastFixings;

         var normInvEps = new InverseCumulativeNormal();
         var sigmaSqrtT = process.blackVolatility().link.blackVol(maturity, payoff.strike())
            * Math.Sqrt(maturity);
         var r = sigmaSqrtT*normInvEps.value(1-0.0001);

         var xMin = Math.Min(Math.Log(avg.GetValueOrDefault())  - 0.25*r, Math.Log(spot) - 1.5*r);
         var xMax = Math.Max(Math.Log(avg.GetValueOrDefault())  + 0.25*r, Math.Log(spot) + 1.5*r);

         var averageMesher = new FdmBlackScholesMesher(aGrid, process, maturity, payoff.strike(), xMin, xMax);
         var mesher = new FdmMesherComposite(equityMesher, averageMesher);

         // 2. Calculator
         var calculator = new FdmLogInnerValue(payoff, mesher, 1);

         // 3. Step conditions
         List<IStepCondition<Vector>> stepConditions = [];
         List<List<double>> stoppingTimes = []; 

         // 3.1 Arithmetic average step conditions
         List<double> averageTimes = [];
         foreach(var fixingDate in arguments_.fixingDates)
         {
            var t = process.time(fixingDate);
            Utils.QL_REQUIRE(t >= 0, ()=> "Fixing dates must not contain past date");
            averageTimes.Add(t);
         }

         stoppingTimes.Add(averageTimes);
         stepConditions.Add(new FdmArithmeticAverageCondition(averageTimes, arguments_.runningAccumulator.GetValueOrDefault(),
            arguments_.pastFixings.GetValueOrDefault(), mesher, 0));

         var conditions = new FdmStepConditionComposite(stoppingTimes, stepConditions);

         // 4. Boundary conditions
         var boundaries = new FdmBoundaryConditionSet();

         // 5. Solver
         var solverDesc = new FdmSolverDesc
         {
            mesher=mesher,
            bcSet=boundaries,
            condition=conditions,
            calculator=calculator,
            maturity=maturity,
            timeSteps=tGrid,
            dampingSteps=0
         };

         var solver = new FdmSimple2dBSSolver(new Handle<GeneralizedBlackScholesProcess>(process),
            payoff.strike(), solverDesc, schemeDesc_);

        results_.value = solver.valueAt(spot, avg.GetValueOrDefault());
        results_.delta = solver.deltaAt(spot, avg.GetValueOrDefault(), spot*0.01);
        results_.gamma = solver.gammaAt(spot, avg.GetValueOrDefault(), spot*0.01);
      }

   }
}
