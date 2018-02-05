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
   public class Fdm1DimSolver : LazyObject
   {
      public Fdm1DimSolver(FdmSolverDesc solverDesc,
                           FdmSchemeDesc schemeDesc,
                           FdmLinearOpComposite op)
      {
         solverDesc_ = solverDesc;
         schemeDesc_ = schemeDesc;
         op_ = op;
         thetaCondition_ = new FdmSnapshotCondition(
            0.99 * Math.Min(1.0 / 365.0,
                            solverDesc.condition.stoppingTimes().empty()
                            ? solverDesc.maturity
                            : solverDesc.condition.stoppingTimes().First()));

         conditions_ = FdmStepConditionComposite.joinConditions(thetaCondition_,
                                                                solverDesc.condition);
         x_ = new InitializedList<double>(solverDesc.mesher.layout().size());
         initialValues_ = new InitializedList<double>(solverDesc.mesher.layout().size());
         resultValues_ = new Vector(solverDesc.mesher.layout().size());

         FdmMesher mesher = solverDesc.mesher;
         FdmLinearOpLayout layout = mesher.layout();

         FdmLinearOpIterator endIter = layout.end();
         for (FdmLinearOpIterator iter = layout.begin(); iter != endIter;
              ++iter)
         {
            initialValues_[iter.index()]
               = solverDesc_.calculator.avgInnerValue(iter,
                                                      solverDesc.maturity);
            x_[iter.index()] = mesher.location(iter, 0);
         }
      }

      public double interpolateAt(double x)
      {
         calculate();
         return interpolation_.value(x);
      }
      public double thetaAt(double x)
      {
         Utils.QL_REQUIRE(conditions_.stoppingTimes().First() > 0.0,
                          () => "stopping time at zero-> can't calculate theta");

         calculate();
         Vector thetaValues = new Vector(resultValues_.size());
         thetaValues = thetaCondition_.getValues();

         double temp = new MonotonicCubicNaturalSpline(
            x_, x_.Count, thetaValues).value(x);
         return (temp - interpolateAt(x)) / thetaCondition_.getTime();
      }
      public double derivativeX(double x)
      {
         calculate();
         return interpolation_.derivative(x);
      }
      public double derivativeXX(double x)
      {
         calculate();
         return interpolation_.secondDerivative(x);
      }
      protected override void performCalculations()
      {
         object rhs = new Vector(initialValues_.Count);
         for (int i = 0; i < initialValues_.Count;  i++)
            (rhs as Vector)[i] = initialValues_[i];

         new FdmBackwardSolver(op_, solverDesc_.bcSet, conditions_, schemeDesc_)
         .rollback(ref rhs, solverDesc_.maturity, 0.0,
                   solverDesc_.timeSteps, solverDesc_.dampingSteps);

         for (int i = 0; i < initialValues_.Count; i++)
            resultValues_[i] = (rhs as Vector)[i];

         interpolation_ = new MonotonicCubicNaturalSpline(x_, x_.Count, resultValues_);
      }

      protected FdmSolverDesc solverDesc_;
      protected FdmSchemeDesc schemeDesc_;
      protected FdmLinearOpComposite op_;
      protected FdmSnapshotCondition thetaCondition_;
      protected FdmStepConditionComposite conditions_;
      protected List<double> x_, initialValues_;
      protected Vector resultValues_;
      protected CubicInterpolation interpolation_;
   }
}
