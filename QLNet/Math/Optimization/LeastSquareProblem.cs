/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 * 
 This file is part of QLNet Project http://qlnet.sourceforge.net/

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
using System.Text;

namespace QLNet
{

    //! Base class for least square problem
    public abstract class LeastSquareProblem
    {
        //! size of the problem ie size of target vector
        public abstract int size();
        //! compute the target vector and the values of the function to fit
        public abstract void targetAndValue(Vector x, ref Vector target, ref Vector fct2fit);
        //        ! compute the target vector, the values of the function to fit
        //            and the matrix of derivatives
        //        
        public abstract void targetValueAndGradient(Vector x, ref Matrix grad_fct2fit, ref Vector target, ref Vector fct2fit);
    }

    //! Cost function for least-square problems
    //    ! Implements a cost function using the interface provided by
    //        the LeastSquareProblem class.
    //    
    public class LeastSquareFunction : CostFunction
    {
        //! least square problem
        protected LeastSquareProblem lsp_ = null;

        //! Default constructor
        public LeastSquareFunction(LeastSquareProblem lsp)
        {
            lsp_ = lsp;
        }

        //! compute value of the least square function
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
        //! compute vector of derivatives of the least square function
        public void gradient(ref Vector grad_f, Vector x)
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
        //! compute value and gradient of the least square function
        public double valueAndGradient(ref Vector grad_f, Vector x)
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

    //! Non-linear least-square method.
    //    ! Using a given optimization algorithm (default is conjugate
    //        gradient),
    //
    //        \f[ min \{ r(x) : x in R^n \} \f]
    //
    //        where \f$ r(x) = |f(x)|^2 \f$ is the Euclidean norm of \f$
    //        f(x) \f$ for some vector-valued function \f$ f \f$ from
    //        \f$ R^n \f$ to \f$ R^m \f$,
    //        \f[ f = (f_1, ..., f_m) \f]
    //        with \f$ f_i(x) = b_i - \phi(x,t_i) \f$ where \f$ b \f$ is the
    //        vector of target data and \f$ phi \f$ is a scalar function.
    //
    //        Assuming the differentiability of \f$ f \f$, the gradient of
    //        \f$ r \f$ is defined by
    //        \f[ grad r(x) = f'(x)^t.f(x) \f]
    //    
    public class NonLinearLeastSquare
    {
        //! solution vector
        private Vector results_;
        private Vector initialValue_;
        //! least square residual norm
        private double resnorm_;
        //! Exit flag of the optimization process
        private int exitFlag_;
        //! required accuracy of the solver
        private double accuracy_;
        private double bestAccuracy_;
        //! maximum and real number of iterations
        private int maxIterations_;
        //private int nbIterations_;
        //! Optimization method
        private OptimizationMethod om_;
        //constraint
        private Constraint c_;

        //! Default constructor
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
        //! Default constructor
        public NonLinearLeastSquare(Constraint c, double accuracy, int maxiter, OptimizationMethod om)
        {
            exitFlag_ = -1;
            accuracy_ = accuracy;
            maxIterations_ = maxiter;
            om_ = om;
            c_ = c;
        }

        //! Solve least square problem using numerix solver
        public Vector perform(ref LeastSquareProblem lsProblem)
        {
            double eps = accuracy_;

            // wrap the least square problem in an optimization function
            LeastSquareFunction lsf = new LeastSquareFunction(lsProblem);

            // define optimization problem
            Problem P = new Problem(lsf, c_, initialValue_);

            // minimize
            EndCriteria ec = new EndCriteria(maxIterations_, Math.Min((int)(maxIterations_ / 2), (int)(100)), eps, eps, eps);
            exitFlag_ = (int)om_.minimize(P, ec);

            // summarize results of minimization
            //        nbIterations_ = om_->iterationNumber();

            results_ = P.currentValue();
            resnorm_ = P.functionValue();
            bestAccuracy_ = P.functionValue();

            return results_;
        }

        public void setInitialValue(Vector initialValue)
        {
            initialValue_ = initialValue;
        }

        //! return the results
        public Vector results()
        {
            return results_;
        }

        //! return the least square residual norm
        public double residualNorm()
        {
            return resnorm_;
        }

        //! return last function value
        public double lastValue()
        {
            return bestAccuracy_;
        }

        //! return exit flag
        public int exitFlag()
        {
            return exitFlag_;
        }

        //! return the performed number of iterations
        //public int iterationsNumber()
        //{
        //    return nbIterations_;
        //}
    }
}
