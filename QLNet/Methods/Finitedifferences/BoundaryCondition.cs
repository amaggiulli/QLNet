/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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
    //! Abstract boundary condition class for finite difference problems
    public class BoundaryCondition<Operator> where Operator : IOperator {
        //! \todo Generalize for n-dimensional conditions
        public enum Side { None, Upper, Lower };

        // interface
        /*! This method modifies an operator \f$ L \f$ before it is
            applied to an array \f$ u \f$ so that \f$ v = Lu \f$ will
            satisfy the given condition. */
        public virtual void applyBeforeApplying(IOperator o) { throw new NotSupportedException(); }

        /*! This method modifies an array \f$ u \f$ so that it satisfies the given condition. */
        public virtual void applyAfterApplying(Vector v) { throw new NotSupportedException(); }

        /*! This method modifies an operator \f$ L \f$ before the linear
            system \f$ Lu' = u \f$ is solved so that \f$ u' \f$ will
            satisfy the given condition. */
        public virtual void applyBeforeSolving(IOperator o, Vector v) { throw new NotSupportedException(); }

        /*! This method modifies an array \f$ u \f$ so that it satisfies the given condition. */
        public virtual void applyAfterSolving(Vector v) { throw new NotSupportedException(); }

        /*! This method sets the current time for time-dependent boundary conditions. */
        public virtual void setTime(double t) { throw new NotSupportedException(); }
    }


    // Time-independent boundary conditions for tridiagonal operators

    //! Neumann boundary condition (i.e., constant derivative)
    /*! \warning The value passed must not be the value of the derivative.
                 Instead, it must be comprehensive of the grid step
                 between the first two points--i.e., it must be the
                 difference between f[0] and f[1].
        \todo generalize to time-dependent conditions.

        \ingroup findiff
    */
    // NeumanBC works on TridiagonalOperator. IOperator here is for type compatobility with options
    public class NeumannBC : BoundaryCondition<IOperator> {
        private double value_;
        private Side side_;
        
        public NeumannBC(double value, Side side) {
            value_ = value;
            side_ = side;
        }

        // interface
        public override void applyBeforeApplying(IOperator o) {
            TridiagonalOperator L = o as TridiagonalOperator;
            switch (side_) {
                case Side.Lower:
                    L.setFirstRow(-1.0,1.0);
                    break;
                case Side.Upper:
                    L.setLastRow(-1.0,1.0);
                    break;
                default:
                    throw new ArgumentException("unknown side for Neumann boundary condition");
            }
        }

        public override void applyAfterApplying(Vector u)  {
            switch (side_) {
                case Side.Lower:
                    u[0] = u[1] - value_;
                    break;
                case Side.Upper:
                    u[u.size()-1] = u[u.size()-2] + value_;
                    break;
                default:
                    throw new ArgumentException("unknown side for Neumann boundary condition");
            }
        }

        public override void applyBeforeSolving(IOperator o, Vector rhs) {
            TridiagonalOperator L = o as TridiagonalOperator;
            switch (side_) {
                case Side.Lower:
                    L.setFirstRow(-1.0,1.0);
                    rhs[0] = value_;
                    break;
                case Side.Upper:
                    L.setLastRow(-1.0,1.0);
                    rhs[rhs.size()-1] = value_;
                    break;
                default:
                    throw new ArgumentException("unknown side for Neumann boundary condition");
            }
        }
        public override void applyAfterSolving(Vector v) {}
        
        public override void setTime(double t) {}
    }
}
