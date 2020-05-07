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

namespace QLNet
{
   public class FdmHestonSolver : LazyObject
   {
      public FdmHestonSolver(
         Handle<HestonProcess> process,
         FdmSolverDesc solverDesc,
         FdmSchemeDesc schemeDesc = null,
         Handle<FdmQuantoHelper> quantoHelper = null,
         LocalVolTermStructure leverageFct = null)
      {
         process_ = process;
         solverDesc_ = solverDesc;
         schemeDesc_ = schemeDesc ?? new FdmSchemeDesc().Douglas();
         quantoHelper_ = quantoHelper == null ? new Handle<FdmQuantoHelper>() : quantoHelper ;
         leverageFct_ = leverageFct;
      }

      public double valueAt(double s, double v)
      {
         calculate();
         return solver_.interpolateAt(Math.Log(s), v);
      }
      public double deltaAt(double s, double v)
      {
         calculate();
         return solver_.derivativeX(Math.Log(s), v) / s;
      }
      public double gammaAt(double s, double v)
      {
         calculate();
         return (solver_.derivativeXX(Math.Log(s), v)
                 - solver_.derivativeX(Math.Log(s), v)) / (s * s);
      }
      public double thetaAt(double s, double v)
      {
         calculate();
         return solver_.thetaAt(Math.Log(s), v);
      }
      public double meanVarianceDeltaAt(double s, double v)
      {
         calculate();

         double alpha = process_.currentLink().rho() * process_.currentLink().sigma() / s;
         return deltaAt(s, v) + alpha * solver_.derivativeY(Math.Log(s), v);
      }
      public double meanVarianceGammaAt(double s, double v)
      {
         calculate();

         double x = Math.Log(s);
         double alpha = process_.currentLink().rho() * process_.currentLink().sigma() / s;
         return gammaAt(s, v)
                + solver_.derivativeYY(x, v) * alpha * alpha
                + 2 * solver_.derivativeXY(x, v) * alpha / s;
      }
      protected override void performCalculations()
      {
         FdmLinearOpComposite op = new FdmHestonOp(
            solverDesc_.mesher, process_.currentLink(),
            (!quantoHelper_.empty()) ? quantoHelper_.currentLink() : null,
            leverageFct_);

         solver_ = new Fdm2DimSolver(solverDesc_, schemeDesc_, op);
      }

      protected Handle<HestonProcess> process_;
      protected FdmSolverDesc solverDesc_;
      protected FdmSchemeDesc schemeDesc_;
      protected Handle<FdmQuantoHelper> quantoHelper_;
      protected LocalVolTermStructure leverageFct_;
      protected Fdm2DimSolver solver_;
   }
}
