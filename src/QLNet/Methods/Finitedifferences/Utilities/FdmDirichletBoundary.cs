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

using System.Collections.Generic;

namespace QLNet
{
   /// <summary>
   /// Dirichlet boundary conditions for differential operators
   /// </summary>
   public class FdmDirichletBoundary : BoundaryCondition<FdmLinearOp>
   {
      public FdmDirichletBoundary(FdmMesher mesher,
                                  double valueOnBoundary, int direction, Side side)
      {
         side_ = side;
         valueOnBoundary_ = valueOnBoundary;
         indices_ = new FdmIndicesOnBoundary(mesher.layout(),
                                             direction, side).getIndices();
         if (side_ == Side.Lower)
         {
            xExtreme_ = mesher.locations(direction)[0];
         }
         else if (side_ == Side.Upper)
         {
            xExtreme_ = mesher
                        .locations(direction)[mesher.layout().dim()[direction] - 1];
         }
         else
         {
            Utils.QL_FAIL("internal error");
         }
      }

      public override void applyBeforeApplying(IOperator o)
      {
         return;
      }

      public override void applyBeforeSolving(IOperator o, Vector v)
      {
         return;
      }

      public override void applyAfterApplying(Vector v)
      {
         foreach (int iter in indices_)
            v[iter] = valueOnBoundary_;
      }

      public override void applyAfterSolving(Vector v)
      {
         this.applyAfterApplying(v);
      }

      public override void setTime(double t)
      {
         return;
      }

      public double applyAfterApplying(double x, double value)
      {
         return ((side_ == Side.Lower && x < xExtreme_)
                 || (side_ == Side.Upper && x > xExtreme_))
                ? valueOnBoundary_
                : value;
      }

      protected Side side_;
      protected double valueOnBoundary_;
      protected List<int> indices_;
      protected double xExtreme_;
   }
}
