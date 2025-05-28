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

namespace QLNet
{
   public class FdmSimple2dBSSolver : LazyObject
   {
      private Handle<GeneralizedBlackScholesProcess> process_;
      private double strike_;
      private FdmSolverDesc solverDesc_;
      private FdmSchemeDesc schemeDesc_;
      private Fdm2DimSolver solver_;

      public FdmSimple2dBSSolver(Handle<GeneralizedBlackScholesProcess> process,
         double strike, FdmSolverDesc solverDesc, FdmSchemeDesc schemeDesc)
      {
         process_ = process;
         strike_ = strike;
         solverDesc_ = solverDesc;
         schemeDesc_ = schemeDesc;

         process_.registerWith(update);
      }

      public double valueAt(double s, double a)
      {
         calculate();
         return solver_.interpolateAt(Math.Log(s), Math.Log(a));
      }

      public double deltaAt(double s, double a, double eps)
      {
         return (valueAt(s+eps, a) - valueAt(s-eps, a))/(2*eps);
      }

      public double gammaAt(double s, double a, double eps)
      {
         return (valueAt(s+eps, a)+valueAt(s-eps, a)-2*valueAt(s,a))/(eps*eps);
      }

      public double? thetaAt(double s, double a)
      {
         calculate();
         return solver_.thetaAt(Math.Log(s), Math.Log(a));
      }

      protected override void performCalculations()
      {
         var op = new FdmBlackScholesOp(solverDesc_.mesher, process_.currentLink(), strike_);
         solver_ = new Fdm2DimSolver(solverDesc_, schemeDesc_, op);
      }
   }
}
