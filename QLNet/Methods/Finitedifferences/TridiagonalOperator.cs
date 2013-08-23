/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Andrea Maggiulli
 
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

namespace QLNet {
    public interface IOperator : ICloneable {
        int size();
        IOperator identity(int size);
        Vector applyTo(Vector v);
        Vector solveFor(Vector rhs);

        IOperator multiply(double a, IOperator D);
        IOperator add(IOperator A, IOperator B);
        IOperator subtract(IOperator A, IOperator B);

        bool isTimeDependent();
        void setTime(double t);
    }

    //! Base implementation for tridiagonal operator
    /*! \warning to use real time-dependant algebra, you must overload
                 the corresponding operators in the inheriting
                 time-dependent class.

        \ingroup findiff
    */
    public class TridiagonalOperator : IOperator {
        protected TimeSetter timeSetter_;

        protected Vector diagonal_, lowerDiagonal_, upperDiagonal_;
        public Vector lowerDiagonal() { return lowerDiagonal_; }
        public Vector diagonal() { return diagonal_; }
        public Vector upperDiagonal() { return upperDiagonal_; }

        public int size() { return diagonal_.Count; }

        public TridiagonalOperator() : this(0) { }
        public TridiagonalOperator(int size) {
            if (size >= 2) {
                diagonal_ = new Vector(size);
                lowerDiagonal_ = new Vector(size - 1);
                upperDiagonal_ = new Vector(size - 1);
            } else if (size == 0) {
                diagonal_ = new Vector(0);
                lowerDiagonal_ = new Vector(0);
                upperDiagonal_ = new Vector(0);
            } else {
                throw new ArgumentException("invalid size (" + size + ") for tridiagonal operator " +
                        "(must be null or >= 2)");
            }
        }

        public TridiagonalOperator(Vector low, Vector mid, Vector high) {
            diagonal_ = (Vector)mid.Clone();
            lowerDiagonal_ = (Vector)low.Clone();
            upperDiagonal_ = (Vector)high.Clone();

            if (!(low.Count == mid.Count - 1))
                throw new ApplicationException("wrong size for lower diagonal vector");
            if (!(high.Count == mid.Count - 1))
                throw new ApplicationException("wrong size for upper diagonal vector");
        }

        // TridiagonalOperator(const Disposable<TridiagonalOperator>&);
        // TridiagonalOperator& operator=(const Disposable<TridiagonalOperator>&);
        public object Clone() { return this.MemberwiseClone(); }

        public IOperator multiply(double a, IOperator o) {
            TridiagonalOperator D = o as TridiagonalOperator;
            Vector low = D.lowerDiagonal_*a,
                   mid = D.diagonal_*a,
                   high = D.upperDiagonal_*a;
            TridiagonalOperator result = new TridiagonalOperator(low,mid,high);
            return result;
        }

        public IOperator add(IOperator A, IOperator B) {
            TridiagonalOperator D1 = A as TridiagonalOperator;
            TridiagonalOperator D2 = B as TridiagonalOperator;

            Vector low = D1.lowerDiagonal_+D2.lowerDiagonal_,
                   mid = D1.diagonal_+D2.diagonal_,
                   high = D1.upperDiagonal_+D2.upperDiagonal_;
            TridiagonalOperator result = new TridiagonalOperator(low,mid,high);
            return result;
        }

        public IOperator subtract(IOperator A, IOperator B) {
            TridiagonalOperator D1 = A as TridiagonalOperator;
            TridiagonalOperator D2 = B as TridiagonalOperator;

            Vector low = D1.lowerDiagonal_-D2.lowerDiagonal_,
                   mid = D1.diagonal_-D2.diagonal_,
                   high = D1.upperDiagonal_-D2.upperDiagonal_;
            TridiagonalOperator result = new TridiagonalOperator(low,mid,high);
            return result;
        }


        //! apply operator to a given array
        public Vector applyTo(Vector v) {
            if (!(v.Count == size()))
                throw new ApplicationException("vector of the wrong size (" + v.Count + "instead of " + size() + ")");

            Vector result = new Vector(size());

            // transform(InputIterator1 start1, InputIterator1 finish1, InputIterator2 start2, OutputIterator result,
            // BinaryOperation binary_op)
            result = Vector.DirectMultiply(diagonal_, v);

            // matricial product
            result[0] += upperDiagonal_[0] * v[1];
            for (int j = 1; j <= size() - 2; j++)
                result[j] += lowerDiagonal_[j - 1] * v[j - 1] + upperDiagonal_[j] * v[j + 1];
            result[size() - 1] += lowerDiagonal_[size() - 2] * v[size() - 2];

            return result;
        }

        //! solve linear system for a given right-hand side
        public Vector solveFor(Vector rhs) {
            if (rhs.Count != size()) throw new ApplicationException("rhs has the wrong size");

            Vector result = new Vector(size()), tmp = new Vector(size());

            double bet = diagonal_[0];
            if (bet == 0.0) throw new ApplicationException("division by zero");
            result[0] = rhs[0] / bet;

            for (int j = 1; j < size(); j++) {
                tmp[j] = upperDiagonal_[j - 1] / bet;
                bet = diagonal_[j] - lowerDiagonal_[j - 1] * tmp[j];
                if (bet == 0.0) throw new ApplicationException("division by zero");
                result[j] = (rhs[j] - lowerDiagonal_[j - 1] * result[j - 1]) / bet;
            }
            // cannot be j>=0 with Size j
            for (int j = size() - 2; j > 0; --j)
                result[j] -= tmp[j + 1] * result[j + 1];
            result[0] -= tmp[1] * result[1];
            return result;
        }

        //! solve linear system with SOR approach
        public Vector SOR(Vector rhs, double tol) {
            if (rhs.Count != size()) throw new ApplicationException("rhs has the wrong size");

            // initial guess
            Vector result = (Vector)rhs.Clone();

            // solve tridiagonal system with SOR technique
            double omega = 1.5;
            double err = 2.0 * tol;
            double temp;
            int i;
            for (int sorIteration = 0; err > tol; sorIteration++) {
                if (!(sorIteration < 100000))
                    throw new ApplicationException("tolerance (" + tol + ") not reached in " + sorIteration + " iterations. "
                           + "The error still is " + err);

                temp = omega * (rhs[0] -
                                upperDiagonal_[0] * result[1] -
                                diagonal_[0] * result[0]) / diagonal_[0];
                err = temp * temp;
                result[0] += temp;

                for (i = 1; i < size() - 1; i++) {
                    temp = omega * (rhs[i] -
                                   upperDiagonal_[i] * result[i + 1] -
                                   diagonal_[i] * result[i] -
                                   lowerDiagonal_[i - 1] * result[i - 1]) / diagonal_[i];
                    err += temp * temp;
                    result[i] += temp;
                }

                temp = omega * (rhs[i] -
                                diagonal_[i] * result[i] -
                                lowerDiagonal_[i - 1] * result[i - 1]) / diagonal_[i];
                err += temp * temp;
                result[i] += temp;
            }
            return result;
        }

        //! identity instance
        public IOperator identity(int size) {
            TridiagonalOperator I = new TridiagonalOperator(new Vector(size - 1, 0.0),     // lower diagonal
                                                            new Vector(size, 1.0),     // diagonal
                                                            new Vector(size - 1, 0.0));    // upper diagonal
            return I;
        }

        public void setFirstRow(double valB, double valC) {
            diagonal_[0] = valB;
            upperDiagonal_[0] = valC;
        }
        public void setMidRow(int i, double valA, double valB, double valC) {
            if (!(i >= 1 && i <= size() - 2))
                throw new ApplicationException("out of range in TridiagonalSystem::setMidRow");
            lowerDiagonal_[i - 1] = valA;
            diagonal_[i] = valB;
            upperDiagonal_[i] = valC;
        }

        public void setMidRows(double valA, double valB, double valC) {
            for (int i = 1; i <= size() - 2; i++) {
                lowerDiagonal_[i - 1] = valA;
                diagonal_[i] = valB;
                upperDiagonal_[i] = valC;
            }
        }

        public void setLastRow(double valA, double valB) {
            lowerDiagonal_[size() - 2] = valA;
            diagonal_[size() - 1] = valB;
        }

        public bool isTimeDependent() { return timeSetter_ != null; }
        public void setTime(double t) {
            if (timeSetter_ != null)
                timeSetter_.setTime(t, this);
        }

        //! encapsulation of time-setting logic
        public abstract class TimeSetter {
            public abstract void setTime(double t, IOperator L);
        }
    }
}

