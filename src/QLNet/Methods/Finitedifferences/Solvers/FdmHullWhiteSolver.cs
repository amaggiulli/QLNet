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

namespace QLNet
{
   public class FdmHullWhiteSolver : LazyObject
   {
      public FdmHullWhiteSolver(
         Handle<HullWhite> model,
         FdmSolverDesc solverDesc,
         FdmSchemeDesc schemeDesc = null)
      {
         solverDesc_ = solverDesc;
         schemeDesc_ = schemeDesc ?? new FdmSchemeDesc().Hundsdorfer();
         model_ = model;
         model_.registerWith(update);
      }

      public double valueAt(double s)
      {
         calculate();
         return solver_.interpolateAt(s);
      }
      public double deltaAt(double s)
      {
         return 0.0;
      }
      public double gammaAt(double s)
      {
         return 0.0;
      }
      public double thetaAt(double s)
      {
         return 0.0;
      }

      protected override void performCalculations()
      {
         FdmHullWhiteOp op = new FdmHullWhiteOp(
            solverDesc_.mesher, model_.currentLink(), 0);

         solver_ = new Fdm1DimSolver(solverDesc_, schemeDesc_, op);
      }

      protected Handle<HullWhite> model_;
      protected FdmSolverDesc solverDesc_;
      protected FdmSchemeDesc schemeDesc_;
      protected Fdm1DimSolver solver_;
   }
}
