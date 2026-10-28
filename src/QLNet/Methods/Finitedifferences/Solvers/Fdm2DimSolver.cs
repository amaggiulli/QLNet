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
using System.Linq;

namespace QLNet
{
   public class Fdm2DimSolver : LazyObject
   {
      private FdmSolverDesc solverDesc_;
      private FdmSchemeDesc schemeDesc_;
      private FdmLinearOpComposite op_;
      private FdmSnapshotCondition thetaCondition_;
      private FdmStepConditionComposite conditions_;
      private List<double> x_, y_, initialValues_;
      private Matrix resultValues_;
      private BicubicSpline interpolation_;

      public Fdm2DimSolver(FdmSolverDesc solverDesc, FdmSchemeDesc schemeDesc, FdmLinearOpComposite op)
      {
         solverDesc_ = solverDesc;
         schemeDesc_ = schemeDesc;
         op_ = op;
         thetaCondition_ = new FdmSnapshotCondition(0.99 * Math.Min(1.0 / 365.0, solverDesc.condition.stoppingTimes().empty() ?
                  solverDesc.maturity : solverDesc.condition.stoppingTimes().First()));
         conditions_ = FdmStepConditionComposite.joinConditions(thetaCondition_, solverDesc.condition);
         initialValues_ =  new InitializedList<double>(solverDesc.mesher.layout().size());
         resultValues_ = new Matrix(solverDesc.mesher.layout().dim()[1], solverDesc.mesher.layout().dim()[0]);

         x_= new InitializedList<double>(solverDesc.mesher.layout().dim()[0]);
         y_ = new InitializedList<double>(solverDesc.mesher.layout().dim()[1]);

         var layout = solverDesc.mesher.layout();
         var endIter = layout.end();

         for (var iter = layout.begin(); iter != endIter; ++iter)
         {
            initialValues_[iter.index()] = solverDesc_.calculator.avgInnerValue(iter, solverDesc.maturity);
            if (iter.coordinates()[1] == 0U)
            {
               x_.Add(solverDesc.mesher.location(iter, 0));
            }
            if (iter.coordinates()[0] == 0U)
            {
               y_.Add(solverDesc.mesher.location(iter, 1));
            }
         }
      }

      public double interpolateAt(double x, double y)
      {
         calculate();
         return interpolation_.value(x, y);
      }

      public double? thetaAt(double x, double y)
      {
         if (conditions_.stoppingTimes().First() == 0.0)
            return null;

         calculate();
         Matrix thetaValues = new Matrix(resultValues_.rows(), resultValues_.columns());
         Vector rhs = thetaCondition_.getValues();
         
         // std::copy(rhs.begin(), rhs.end(), thetaValues.begin());

         return new BicubicSpline(x_, x_.Count, y_, y_.Count, thetaValues).value(x, y) - interpolateAt(x, y) /
            thetaCondition_.getTime();
      }

      public double derivativeX(double x, double y)
      {
         calculate();
         return interpolation_.derivativeX(x, y);
      }

      public double derivativeY(double x, double y)
      {
         calculate();
         return interpolation_.derivativeY(x, y);
      }

      public double derivativeXX(double x, double y)
      {
         calculate();
         return interpolation_.secondDerivativeX(x, y);
      }

      public double derivativeYY(double x, double y)
      {
         calculate();
         return interpolation_.secondDerivativeY(x, y);
      }

      public double derivativeXY(double x, double y)
      {
         calculate();
         return interpolation_.derivativeXY(x, y);
      }

      protected override void performCalculations()
      {
         object rhs = new Vector(initialValues_.Count);
         for (var i = 0; i < initialValues_.Count;  i++)
            ((Vector)rhs)[i] = initialValues_[i];

         new FdmBackwardSolver(op_, solverDesc_.bcSet, conditions_, schemeDesc_)
            .rollback(ref rhs, solverDesc_.maturity, 0.0, solverDesc_.timeSteps, solverDesc_.dampingSteps);

         for (var i = 0; i < initialValues_.Count; i++)
            resultValues_[i] = ((Vector)rhs)[i];

         interpolation_ = new BicubicSpline(x_, x_.Count, y_, y_.Count, resultValues_);
      }
   }
}
