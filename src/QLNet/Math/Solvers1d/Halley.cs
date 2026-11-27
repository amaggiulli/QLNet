/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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
   /// <summary>
   /// Halley 1-D solver
   /// </summary>
   /// <remarks>
   /// Note: This solver requires that the passed function object
   /// implement the methods <c>double derivative(double)</c> and
   /// <c>double secondDerivative(double)</c>.
   /// </remarks>
   public class Halley : Solver1D
   {
      protected override double solveImpl(ISolver1d f, double xAccuracy)
      {
         while (++evaluationNumber_ <= maxEvaluations_)
         {
            double fx = f.value(root_);
            double fPrime = f.derivative(root_);
            double lf = fx * f.secondDerivative(root_) / (fPrime * fPrime);
            double step = 1.0 / (1.0 - 0.5 * lf) * fx / fPrime;
            double rootold = root_;
            root_ -= step;

            // jumped out of brackets (or the step is not a finite number),
            // switch to NewtonSafe starting from the previous estimate
            if (!((xMin_ - root_) * (root_ - xMax_) >= 0.0))
            {
               var s = new NewtonSafe();
               s.setMaxEvaluations(maxEvaluations_ - evaluationNumber_);
               return s.solve(f, xAccuracy, rootold, xMin_, xMax_);
            }

            if (Math.Abs(step) < xAccuracy)
            {
               f.value(root_);
               ++evaluationNumber_;
               return root_;
            }
         }

         Utils.QL_FAIL("maximum number of function evaluations (" + maxEvaluations_ + ") exceeded",
                       QLNetExceptionEnum.MaxNumberFuncEvalExceeded);
         return 0;
      }
   }
}
