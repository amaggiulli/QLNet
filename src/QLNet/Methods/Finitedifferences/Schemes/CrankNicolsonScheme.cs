/*
 Copyright (C) 2020 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
   /*! In one dimension the Crank-Nicolson scheme is equivalent to the
       Douglas scheme and in higher dimensions it is usually inferior to
       operator splitting methods like Craig-Sneyd or Hundsdorfer-Verwer.
   */
   public class CrankNicolsonScheme : IMixedScheme, ISchemeFactory
   {
      public CrankNicolsonScheme()
      { }

      public CrankNicolsonScheme(double theta,
                                 FdmLinearOpComposite map,
                                 List<BoundaryCondition<FdmLinearOp>> bcSet = null,
                                 double relTol = 1E-8,
                                 ImplicitEulerScheme.SolverType solverType = ImplicitEulerScheme.SolverType.BiCGstab)
      {
         dt_ = null;
         theta_ = theta;
         explicit_ = new ExplicitEulerScheme(map, bcSet);
         implicit_ = new ImplicitEulerScheme(map, bcSet, relTol, solverType);
      }

      #region ISchemeFactory

      public IMixedScheme factory(object L, object bcs, object[] additionalInputs = null)
      {
         double? theta = additionalInputs[0] as double?;
         double? relTol = additionalInputs[1] as double?;
         ImplicitEulerScheme.SolverType? solverType = additionalInputs[2] as ImplicitEulerScheme.SolverType?;
         return new CrankNicolsonScheme(theta.Value, L as FdmLinearOpComposite,
                                        bcs as List<BoundaryCondition<FdmLinearOp>>, relTol.Value, solverType.Value);
      }

      #endregion

      public void step(ref object a, double t, double theta = 1.0)
      {
         Utils.QL_REQUIRE(t - dt_ > -1e-8, () => "a step towards negative time given");
         if (theta_ != 1.0)
            explicit_.step(ref a, t, 1.0 - theta_);

         if (theta_ != 0.0)
            implicit_.step(ref a, t, theta_);
      }

      public void setStep(double dt)
      {
         dt_ = dt;
         explicit_.setStep(dt_.Value);
         implicit_.setStep(dt_.Value);
      }

      public int numberOfIterations()
      {
         return implicit_.numberOfIterations();
      }
      protected double? dt_;
      protected double theta_;
      protected ExplicitEulerScheme explicit_;
      protected ImplicitEulerScheme implicit_;
   }
}
