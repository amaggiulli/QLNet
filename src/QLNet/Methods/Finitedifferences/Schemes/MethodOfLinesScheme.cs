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
   public class MethodOfLinesScheme : IMixedScheme, ISchemeFactory
   {
      public MethodOfLinesScheme()
      { }

      public MethodOfLinesScheme(double eps,
                                 double relInitStepSize,
                                 FdmLinearOpComposite map,
                                 List<BoundaryCondition<FdmLinearOp>> bcSet = null)
      {
         dt_ = null;
         eps_ = eps;
         relInitStepSize_ = relInitStepSize;
         map_ = map;
         bcSet_ = new BoundaryConditionSchemeHelper(bcSet);
      }

      #region ISchemeFactory

      public IMixedScheme factory(object L, object bcs, object[] additionalInputs = null)
      {
         double? eps = additionalInputs[0] as double?;
         double? relInitStepSize = additionalInputs[1] as double?;
         return new MethodOfLinesScheme(eps.Value, relInitStepSize.Value,
                                        L as FdmLinearOpComposite, bcs as List<BoundaryCondition<FdmLinearOp>>);
      }

      #endregion

      protected List<double> apply(double t, List<double> r)
      {
         map_.setTime(t, t + 0.0001);
         bcSet_.applyBeforeApplying(map_);

         Vector dxdt = -1.0 * map_.apply(new Vector(r));

         return dxdt;
      }

      public void step(ref object a, double t, double theta = 1.0)
      {
         Utils.QL_REQUIRE(t - dt_ > -1e-8, () => "a step towards negative time given");
         List<double> v = new AdaptiveRungeKutta(eps_, relInitStepSize_ * dt_.Value).value(this.apply, a as Vector, t, Math.Max(0.0, t - dt_.Value));
         Vector y = new Vector(v);
         bcSet_.applyAfterSolving(y);
         a = y;
      }

      public void setStep(double dt)
      {
         dt_ = dt;
      }

      protected double? dt_;
      protected double eps_, relInitStepSize_;
      protected FdmLinearOpComposite map_;
      protected BoundaryConditionSchemeHelper bcSet_;
   }
}
