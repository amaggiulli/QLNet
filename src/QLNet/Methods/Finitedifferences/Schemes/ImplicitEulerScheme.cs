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
   public class ImplicitEulerScheme : IMixedScheme, ISchemeFactory
   {
      public enum SolverType { BiCGstab, GMRES }

      public ImplicitEulerScheme() { }
      public ImplicitEulerScheme(FdmLinearOpComposite map,
                                 List<BoundaryCondition<FdmLinearOp>> bcSet,
                                 double relTol = 1e-8,
                                 SolverType solverType = SolverType.BiCGstab)
      {
         dt_ = null;
         iterations_ = 0;
         relTol_ = relTol;
         map_ = map;
         bcSet_ = new BoundaryConditionSchemeHelper(bcSet);
         solverType_ = solverType;
      }

      #region ISchemeFactory
      public IMixedScheme factory(object L, object bcs, object[] additionalFields = null)
      {
         return new ImplicitEulerScheme(L as FdmLinearOpComposite, bcs as List<BoundaryCondition<FdmLinearOp>>);
      }
      #endregion

      #region IMixedScheme interface
      public void step(ref object a, double t, double theta = 1.0)
      {
         Utils.QL_REQUIRE(t - dt_.Value > -1e-8, () => "a step towards negative time given");
         map_.setTime(Math.Max(0.0, t - dt_.Value), t);
         bcSet_.setTime(Math.Max(0.0, t - dt_.Value));

         bcSet_.applyBeforeSolving(map_, a as Vector);

         if (map_.size() == 1)
         {
            a = map_.solve_splitting(0, a as Vector, -theta * dt_.Value);
         }
         else
         {
            if (solverType_ == SolverType.BiCGstab)
            {
               BiCGStab.MatrixMult preconditioner = x => map_.preconditioner(x, -theta * dt_.Value);
               BiCGStab.MatrixMult applyF = x => this.apply(x, theta);

               BiCGStabResult result =
                  new BiCGStab(applyF, Math.Max(10, (a as Vector).Count), relTol_, preconditioner).solve(a as Vector, a as Vector);

               iterations_ += result.Iterations;
               a = result.X;
            }
            else if (solverType_ == SolverType.GMRES)
            {
               GMRES.MatrixMult preconditioner = x => map_.preconditioner(x, -theta * dt_.Value);
               GMRES.MatrixMult applyF = x => this.apply(x, theta);

               GMRESResult result =
                  new GMRES(applyF, Math.Max(10, (a as Vector).Count) / 10, relTol_, preconditioner).solve(a as Vector, a as Vector);

               iterations_ += result.Errors.Count;
               a = result.X;
            }
            else
               Utils.QL_FAIL("unknown/illegal solver type");
         }
         bcSet_.applyAfterSolving(a as Vector);
      }

      public void setStep(double dt)
      {
         dt_ = dt;
      }
      #endregion

      public int numberOfIterations()
      {
         return iterations_;
      }

      protected Vector apply(Vector r, double theta = 1.0)
      {
         return r - (theta * dt_.Value) * map_.apply(r);
      }

      protected double? dt_;
      protected int iterations_;
      protected double relTol_;
      protected FdmLinearOpComposite map_;
      protected BoundaryConditionSchemeHelper bcSet_;
      protected SolverType solverType_;
   }
}
