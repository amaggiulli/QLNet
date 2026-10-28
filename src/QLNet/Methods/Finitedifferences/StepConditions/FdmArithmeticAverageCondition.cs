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
   /// <summary>
   /// Step condition to handle arithmetic average
   /// </summary>
   public class FdmArithmeticAverageCondition : IStepCondition<Vector>
   {
      private Vector x_; // grid-equity values in physical units
      private Vector a_; // average values in physical units
      private List<double> averageTimes_ = new List<double>();
      private int pastFixings_;
      private FdmMesher mesher_;
      private int equityDirection_;

      public FdmArithmeticAverageCondition(List<double> averageTimes, double u, int pastFixings, FdmMesher mesher, int equityDirection)
      {
         x_ = new Vector(mesher.layout().dim()[equityDirection]);
         a_ = new Vector(mesher.layout().dim()[equityDirection == 0 ? 1 : 0]);
         averageTimes_ = averageTimes;
         pastFixings_ = pastFixings;
         mesher_ = mesher;
         equityDirection_ = equityDirection;

         Utils.QL_REQUIRE(mesher.layout().dim().Count==2, ()=> "2D allowed only");
         Utils.QL_REQUIRE(equityDirection == 0 || equityDirection == 1, ()=> "equityDirection has to be 0 or 1");

         var xSpacing = mesher_.layout().spacing()[equityDirection];
         var tmp = mesher_.locations(equityDirection);
         for (var i = 0; i < x_.size(); ++i)
         {
            x_[i] =Math.Exp(tmp[i*xSpacing]);
         }
         var averageDirection = equityDirection == 0 ? 1 : 0;
         var aSpacing = mesher_.layout().spacing()[averageDirection];
         tmp = mesher_.locations(averageDirection);
         for (var i = 0; i < a_.size(); ++i)
         {
            a_[i] = Math.Exp(tmp[i*aSpacing]);
         }
      }

      public void applyTo(object o, double t)
      {
         var a = (Vector)o;
         Utils.QL_REQUIRE(mesher_.layout().size() == a.size(),()=> "inconsistent array dimensions");

         var iter = averageTimes_.Find(x => x.Equals(t));
         var nTimes = averageTimes_.FindAll(x => x.Equals(t)).Count;
         if (nTimes > 0)
         {
            var iT = iter - averageTimes_.First() + 1 + pastFixings_;
            var averageDirection = equityDirection_ == 0 ? 1 : 0;
            var xSpacing = mesher_.layout().spacing()[equityDirection_];
            var aSpacing = mesher_.layout().spacing()[averageDirection];
            var tmp = new Vector(a_.size());

            for (var i=0; i<x_.size(); ++i)
            {
               for (var j=0; j<a_.size(); ++j)
               {
                  var index = i*xSpacing + j*aSpacing;
                  tmp[j] = a[index];
               }
               var interp = new MonotonicCubicNaturalSpline(a_, a_.Count, tmp);
               for (var j=0; j<a_.size(); ++j)
               {
                  var index = i*xSpacing + j*aSpacing;
                  a[index] = interp.value((iT-nTimes)/(double)(iT)*a_[j] +
                                    nTimes/(double)(iT)*x_[i], true);
               }
            }
         }
      }
   }
}
