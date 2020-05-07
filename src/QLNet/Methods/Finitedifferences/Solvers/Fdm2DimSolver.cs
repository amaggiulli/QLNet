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
   public class Fdm2DimSolver : LazyObject
   {
      public Fdm2DimSolver(FdmSolverDesc solverDesc,
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

         initialValues_ = new InitializedList<double>(solverDesc.mesher.layout().size());
         resultValues_ = new Matrix(solverDesc.mesher.layout().dim()[1],
                                    solverDesc.mesher.layout().dim()[0]);

         FdmMesher mesher = solverDesc.mesher;
         FdmLinearOpLayout layout = mesher.layout();

         x_ = new List<double>();
         y_ = new List<double>();

         FdmLinearOpIterator endIter = layout.end();
         for (FdmLinearOpIterator iter = layout.begin(); iter != endIter;
              ++iter)
         {
            initialValues_[iter.index()]
               = solverDesc_.calculator.avgInnerValue(iter,
                                                      solverDesc.maturity);
            if (iter.coordinates()[1] == 0)
               x_.Add(mesher.location(iter, 0));

            if (iter.coordinates()[0] == 0)
               y_.Add(mesher.location(iter, 1));
         }
      }

      public double interpolateAt(double x, double y)
      {
         calculate();
         return interpolation_.value(x, y);
      }
      public double thetaAt(double x, double y)
      {
         Utils.QL_REQUIRE(conditions_.stoppingTimes().First() > 0.0,
                          () => "stopping time at zero-> can't calculate theta");

         calculate();
         Matrix thetaValues = new Matrix(resultValues_.rows(),
                                         resultValues_.columns());

         Vector rhs = thetaCondition_.getValues();
         int row = 0, col = 0;
         for (int i = 0; i < rhs.size(); i++)
         {
            if (col == thetaValues.columns())
            {
               row++;
               col = 0;
            }

            thetaValues[row, col] = rhs[i];
         }

         double temp = new BicubicSpline(
            x_, x_.Count, y_, y_.Count, thetaValues).value(x, y);
         return (temp - interpolateAt(x, y)) / thetaCondition_.getTime();
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
         for (int i = 0; i < initialValues_.Count;  i++)
            (rhs as Vector)[i] = initialValues_[i];

         new FdmBackwardSolver(op_, solverDesc_.bcSet, conditions_, schemeDesc_)
         .rollback(ref rhs, solverDesc_.maturity, 0.0,
                   solverDesc_.timeSteps, solverDesc_.dampingSteps);

         int row = 0, col = 0;
         for (int i = 0; i < (rhs as Vector).size(); i++)
         {
            if (col == resultValues_.columns())
            {
               row++;
               col = 0;
            }

            resultValues_[row, col] = (rhs as Vector)[i];
            col++;
         }

         interpolation_ = new BicubicSpline(x_, x_.Count, y_, y_.Count, resultValues_);
      }

      protected FdmSolverDesc solverDesc_;
      protected FdmSchemeDesc schemeDesc_;
      protected FdmLinearOpComposite op_;
      protected FdmSnapshotCondition thetaCondition_;
      protected FdmStepConditionComposite conditions_;
      protected List<double> x_, y_, initialValues_;
      protected Matrix resultValues_;
      protected BicubicSpline interpolation_;
   }
}
