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
   public class ExplicitEulerScheme : IMixedScheme, ISchemeFactory
   {
      public ExplicitEulerScheme() { }
      public ExplicitEulerScheme(FdmLinearOpComposite map,
                                 List<BoundaryCondition<FdmLinearOp>> bcSet = null)
      {
         dt_ = null;
         map_ = map;
         bcSet_ = new BoundaryConditionSchemeHelper(bcSet);
      }

      #region ISchemeFactory
      public IMixedScheme factory(object L, object bcs, object[] additionalInputs = null)
      {
         return new ExplicitEulerScheme(L as FdmLinearOpComposite, bcs as List<BoundaryCondition<FdmLinearOp>>);
      }
      #endregion

      #region IMixedScheme interface
      public void step(ref object a, double t, double theta = 1.0)
      {
         Utils.QL_REQUIRE(t - dt_ > -1e-8, () => "a step towards negative time given");
         map_.setTime(Math.Max(0.0, t - dt_.Value), t);
         bcSet_.setTime(Math.Max(0.0, t - dt_.Value));

         bcSet_.applyBeforeApplying(map_);
         a = (a as Vector) + (theta * dt_.Value) * map_.apply(a as Vector);
         bcSet_.applyAfterApplying(a as Vector);
      }

      public void setStep(double dt)
      {
         dt_ = dt;
      }
      #endregion

      protected double? dt_;
      protected FdmLinearOpComposite map_;
      protected BoundaryConditionSchemeHelper bcSet_;
   }
}
