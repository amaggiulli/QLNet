/*
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
using System;
using System.Collections.Generic;

namespace QLNet
{

   /// <summary>
   /// Base class for least square problem
   /// </summary>
   public abstract class LeastSquareProblem
   {
      /// <summary>
      /// Returns the size of the problem, namely the size of the target vector.
      /// </summary>
      public abstract int size();
      /// <summary>
      /// Computes the target vector and the values of the function being fitted.
      /// </summary>
      public abstract void targetAndValue(Vector x, ref Vector target, ref Vector fct2fit);
      //        ! compute the target vector, the values of the function to fit
      //            and the matrix of derivatives
      //
      public abstract void targetValueAndGradient(Vector x, ref Matrix grad_fct2fit, ref Vector target, ref Vector fct2fit);
   }

   /// <summary>
   /// Cost function for least-square problems.
   /// </summary>
   /// <remarks>
   /// This class adapts a <see cref="LeastSquareProblem"/> to the generic optimization
   /// interfaces used by QLNet.
   /// </remarks>
   public class LeastSquareFunction : CostFunction
   {
      // Least-square problem.
      protected LeastSquareProblem lsp_ = null;

      /// <summary>
      /// Initializes the cost function from a least-square problem.
      /// </summary>
      public LeastSquareFunction(LeastSquareProblem lsp)
      {
         lsp_ = lsp;
      }

      /// <summary>
      /// Computes the value of the least-square objective function.
      /// </summary>
      public override double value(Vector x)
      {
         // size of target and function to fit vectors
         Vector target = new Vector(lsp_.size());
         Vector fct2fit = new Vector(lsp_.size());
         // compute its values
         lsp_.targetAndValue(x, ref target, ref fct2fit);
         // do the difference
         Vector diff = target - fct2fit;
         // and compute the scalar product (square of the norm)
         return Vector.DotProduct(diff, diff);
      }
      public override Vector values(Vector x)
      {
         // size of target and function to fit vectors
         Vector target = new Vector(lsp_.size());
         Vector fct2fit = new Vector(lsp_.size());
         // compute its values
         lsp_.targetAndValue(x, ref target, ref fct2fit);
         // do the difference
         Vector diff = target - fct2fit;
         return Vector.DirectMultiply(diff, diff);
      }
      /// <summary>
      /// Computes the gradient of the least-square objective function.
      /// </summary>
      public override void gradient(ref Vector grad_f, Vector x)
      {
         // size of target and function to fit vectors
         Vector target = new Vector(lsp_.size());
         Vector fct2fit = new Vector(lsp_.size());
         // size of gradient matrix
         Matrix grad_fct2fit = new Matrix(lsp_.size(), x.size());
         // compute its values
         lsp_.targetValueAndGradient(x, ref grad_fct2fit, ref target, ref fct2fit);
         // do the difference
         Vector diff = target - fct2fit;
         // compute derivative
         grad_f = -2.0 * (Matrix.transpose(grad_fct2fit) * diff);
      }
      /// <summary>
      /// Computes both the value and gradient of the least-square objective function.
      /// </summary>
      public override double valueAndGradient(ref Vector grad_f, Vector x)
      {
         // size of target and function to fit vectors
         Vector target = new Vector(lsp_.size());
         Vector fct2fit = new Vector(lsp_.size());
         // size of gradient matrix
         Matrix grad_fct2fit = new Matrix(lsp_.size(), x.size());
         // compute its values
         lsp_.targetValueAndGradient(x, ref grad_fct2fit, ref target, ref fct2fit);
         // do the difference
         Vector diff = target - fct2fit;
         // compute derivative
         grad_f = -2.0 * (Matrix.transpose(grad_fct2fit) * diff);
         // and compute the scalar product (square of the norm)
         return Vector.DotProduct(diff, diff);
      }
   }

   /// <summary>
   /// Non-linear least-square solver.
   /// </summary>
   /// <remarks>
   /// This class minimizes the squared Euclidean norm of a vector-valued residual function
   /// by delegating to a configurable optimization method. The default optimization method
   /// is conjugate gradient.
   /// </remarks>
   public class NonLinearLeastSquare
   {
      // Solution vector.
      private Vector results_;
      private Vector initialValue_;
      // Least-square residual norm.
      private double resnorm_;
      // Exit flag of the optimization process.
      private int exitFlag_;
      // Required accuracy of the solver.
      private double accuracy_;
      private double bestAccuracy_;
      // Maximum and realized number of iterations.
      private int maxIterations_;
      // Optimization method.
      private OptimizationMethod om_;
      //constraint
      private Constraint c_;

      /// <summary>
      /// Initializes the solver with the given constraint and accuracy.
      /// </summary>
      public NonLinearLeastSquare(Constraint c, double accuracy)
         : this(c, accuracy, 100)
      {
      }
      public NonLinearLeastSquare(Constraint c)
         : this(c, 1e-4, 100)
      {
      }
      public NonLinearLeastSquare(Constraint c, double accuracy, int maxiter)
      {
         exitFlag_ = -1;
         accuracy_ = accuracy;
         maxIterations_ = maxiter;
         om_ = new ConjugateGradient();
         c_ = c;
      }
      /// <summary>
      /// Initializes the solver with the given constraint, accuracy, iteration limit, and optimization method.
      /// </summary>
      public NonLinearLeastSquare(Constraint c, double accuracy, int maxiter, OptimizationMethod om)
      {
         exitFlag_ = -1;
         accuracy_ = accuracy;
         maxIterations_ = maxiter;
         om_ = om;
         c_ = c;
      }

      /// <summary>
      /// Solves the least-square problem using the configured optimization method.
      /// </summary>
      public Vector perform(ref LeastSquareProblem lsProblem)
      {
         double eps = accuracy_;

         // wrap the least square problem in an optimization function
         LeastSquareFunction lsf = new LeastSquareFunction(lsProblem);

         // define optimization problem
         Problem P = new Problem(lsf, c_, initialValue_);

         // minimize
         EndCriteria ec = new EndCriteria(maxIterations_, Math.Min(maxIterations_ / 2, 100), eps, eps, eps);
         exitFlag_ = (int)om_.minimize(P, ec);

         results_ = P.currentValue();
         resnorm_ = P.functionValue();
         bestAccuracy_ = P.functionValue();

         return results_;
      }

      public void setInitialValue(Vector initialValue)
      {
         initialValue_ = initialValue;
      }

      /// <summary>
      /// Returns the solution vector.
      /// </summary>
      public Vector results()
      {
         return results_;
      }

      /// <summary>
      /// Returns the least-square residual norm.
      /// </summary>
      public double residualNorm()
      {
         return resnorm_;
      }

      /// <summary>
      /// Returns the last objective-function value.
      /// </summary>
      public double lastValue()
      {
         return bestAccuracy_;
      }

      /// <summary>
      /// Returns the exit flag from the optimization process.
      /// </summary>
      public int exitFlag()
      {
         return exitFlag_;
      }

   }
}
