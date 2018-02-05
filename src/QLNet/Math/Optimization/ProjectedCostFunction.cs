/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 *
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

namespace QLNet
{

   //! Parameterized cost function
//    ! This class creates a proxy cost function which can depend
//        on any arbitrary subset of parameters (the other being fixed)
//
   public class ProjectedCostFunction : CostFunction
   {
      private int numberOfFreeParameters_;
      private Vector fixedParameters_;
      private Vector actualParameters_;
      private List<bool> parametersFreedoms_;
      private CostFunction costFunction_;

      public ProjectedCostFunction(CostFunction costFunction, Vector parametersValues, List<bool> parametersFreedoms)
      {
         numberOfFreeParameters_ = 0;
         fixedParameters_ = parametersValues;
         actualParameters_ = parametersValues;
         parametersFreedoms_ = parametersFreedoms;
         costFunction_ = costFunction;

         Utils.QL_REQUIRE(fixedParameters_.Count == parametersFreedoms_.Count, () =>
                          "fixedParameters_.Count!=parametersFreedoms_.Count");

         for (int i = 0; i < parametersFreedoms_.Count; i++)
            if (!parametersFreedoms_[i])
               numberOfFreeParameters_++;

         Utils.QL_REQUIRE(numberOfFreeParameters_ > 0, () => "numberOfFreeParameters==0");
      }

      // CostFunction interface
      public override double value(Vector freeParameters)
      {
         mapFreeParameters(freeParameters);
         return costFunction_.value(actualParameters_);
      }
      public override Vector values(Vector freeParameters)
      {
         mapFreeParameters(freeParameters);
         return costFunction_.values(actualParameters_);
      }

      //! returns the subset of free parameters corresponding
      // to set of parameters
      public Vector project(Vector parameters)
      {
         Utils.QL_REQUIRE(parameters.Count == parametersFreedoms_.Count, () => "parameters.Count!=parametersFreedoms_.Count");

         Vector projectedParameters = new Vector(numberOfFreeParameters_);
         int i = 0;
         for (int j = 0; j < parametersFreedoms_.Count; j++)
            if (!parametersFreedoms_[j])
               projectedParameters[i++] = parameters[j];
         return projectedParameters;
      }

      //! returns whole set of parameters corresponding to the set
      // of projected parameters
      public Vector include(Vector projectedParameters)
      {
         Utils.QL_REQUIRE(projectedParameters.Count == numberOfFreeParameters_, () =>
                          "projectedParameters.Count!=numberOfFreeParameters");

         Vector y = new Vector(fixedParameters_);
         int i = 0;
         for (int j = 0; j < y.Count; j++)
            if (!parametersFreedoms_[j])
               y[j] = projectedParameters[i++];
         return y;
      }

      private void mapFreeParameters(Vector parametersValues)
      {
         Utils.QL_REQUIRE(parametersValues.Count == numberOfFreeParameters_, () =>
                          "parametersValues.Count!=numberOfFreeParameters");

         int i = 0;
         for (int j = 0; j < actualParameters_.Count; j++)
            if (!parametersFreedoms_[j])
               actualParameters_[j] = parametersValues[i++];
      }
   }
}
