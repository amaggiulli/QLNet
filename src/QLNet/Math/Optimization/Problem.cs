/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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

namespace QLNet
{
   /// <summary>
   /// Constrained optimization problem
   /// </summary>
   public class Problem
   {
      // Cost function.
      protected CostFunction costFunction_;
      public CostFunction costFunction() { return costFunction_; }

      // Constraint.
      protected Constraint constraint_;
      public Constraint constraint() { return constraint_; }

      // Current value of the local minimum.
      protected Vector currentValue_;
      public Vector currentValue() { return currentValue_; }

      // Function and gradient norm values at currentValue_.
      protected double? functionValue_, squaredNorm_;
      public double functionValue() { return functionValue_.GetValueOrDefault(); }
      public double gradientNormValue() { return squaredNorm_.GetValueOrDefault(); }

      // Number of evaluations of the cost function and its gradient.
      protected int functionEvaluation_, gradientEvaluation_;
      public int functionEvaluation() { return functionEvaluation_; }
      public int gradientEvaluation() { return gradientEvaluation_; }


      /// <summary>
      /// Initializes the optimization problem with a cost function, a constraint, and an initial value.
      /// </summary>
      public Problem(CostFunction costFunction, Constraint constraint, Vector initialValue)
      {
         costFunction_ = costFunction;
         constraint_ = constraint;
         currentValue_ = initialValue.Clone();
         Utils.QL_REQUIRE(!constraint.empty(), () => "empty constraint given");
      }

      /// <summary>
      /// Resets the cached function, gradient, and evaluation counters.
      /// </summary>
      /// <remarks>
      /// This does not restore the current minimum to an initial value.
      /// </remarks>
      public void reset()
      {
         functionEvaluation_ = gradientEvaluation_ = 0;
         functionValue_ = squaredNorm_ = null;
      }

      /// <summary>
      /// Evaluates the cost function and increments the evaluation counter.
      /// </summary>
      public double value(Vector x)
      {
         ++functionEvaluation_;
         return costFunction_.value(x);
      }

      /// <summary>
      /// Evaluates the vector of cost-function values and increments the evaluation counter.
      /// </summary>
      public Vector values(Vector x)
      {
         ++functionEvaluation_;
         return costFunction_.values(x);
      }

      /// <summary>
      /// Evaluates the gradient and increments the gradient evaluation counter.
      /// </summary>
      public void gradient(ref Vector grad_f, Vector x)
      {
         ++gradientEvaluation_;
         costFunction_.gradient(ref grad_f, x);
      }

      /// <summary>
      /// Evaluates both the cost function and its gradient and increments both counters.
      /// </summary>
      public double valueAndGradient(ref Vector grad_f, Vector x)
      {
         ++functionEvaluation_;
         ++gradientEvaluation_;
         return costFunction_.valueAndGradient(ref grad_f, x);
      }

      public void setCurrentValue(Vector currentValue)
      {
         currentValue_ = currentValue.Clone();
      }

      public void setFunctionValue(double functionValue)
      {
         functionValue_ = functionValue;
      }

      public void setGradientNormValue(double squaredNorm)
      {
         squaredNorm_ = squaredNorm;
      }
   }
}
