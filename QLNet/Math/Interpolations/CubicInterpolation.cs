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
    //! %Cubic interpolation between discrete points.
    /*! Cubic interpolation is fully defined when the ${f_i}$ function values
        at points ${x_i}$ are supplemented with ${f_i}$ function derivative
        values.
    
        Different type of first derivative approximations are implemented, both
        local and non-local. Local schemes (Fourth-order, Parabolic,
        Modified Parabolic, Fritsch-Butland, Akima, Kruger) use only $f$ values
        near $x_i$ to calculate $f_i$. Non-local schemes (Spline with different
        boundary conditions) use all ${f_i}$ values and obtain ${f_i}$ by
        solving a linear system of equations. Local schemes produce $C^1$
        interpolants, while the spline scheme generates $C^2$ interpolants.

        Hyman's monotonicity constraint filter is also implemented: it can be
        applied to all schemes to ensure that in the regions of local
        monotoniticity of the input (three successive increasing or decreasing
        values) the interpolating cubic remains monotonic. If the interpolating
        cubic is already monotonic, the Hyman filter leaves it unchanged
        preserving all its original features.
        
        In the case of $C^2$ interpolants the Hyman filter ensures local
        monotonicity at the expense of the second derivative of the interpolant
        which will no longer be continuous in the points where the filter has
        been applied. 

        While some non-linear schemes (Modified Parabolic, Fritsch-Butland,
        Kruger) are guaranteed to be locally monotone in their original
        approximation, all other schemes must be filtered according to the
        Hyman criteria at the expense of their linearity.
        
        See R. L. Dougherty, A. Edelman, and J. M. Hyman,
        "Nonnegativity-, Monotonicity-, or Convexity-Preserving CubicSpline and
        Quintic Hermite Interpolation"
        Mathematics Of Computation, v. 52, n. 186, April 1989, pp. 471-494.

        \todo implement missing schemes (FourthOrder and ModifiedParabolic) and
              missing boundary conditions (Periodic and Lagrange).

        \test to be adapted from old ones.
    */
    public class CubicInterpolation : Interpolation, IValue {
        #region enums
        public enum DerivativeApprox {
            /*! Spline approximation (non-local, non-monotone, linear[?]).
                Different boundary conditions can be used on the left and right
                boundaries: see BoundaryCondition.
            */
            Spline,

            //! Fourth-order approximation (local, non-monotone, linear)
            FourthOrder,

            //! Parabolic approximation (local, non-monotone, linear)
            Parabolic,

            //! Fritsch-Butland approximation (local, monotone, non-linear)
            FritschButland,

            //! Akima approximation (local, non-monotone, non-linear)
            Akima,

            //! Kruger approximation (local, monotone, non-linear)
            Kruger
        };
        public enum BoundaryCondition {
            //! Make second(-last) point an inactive knot
            NotAKnot,

            //! Match value of end-slope
            FirstDerivative,

            //! Match value of second derivative at end
            SecondDerivative,

            //! Match first and second derivative at either end
            Periodic,

            /*! Match end-slope to the slope of the cubic that matches
                the first four data at the respective end
            */
            Lagrange
        }; 
        #endregion

        // private CoefficientHolder coeffs_;

        public CubicInterpolation(List<double> xBegin, int size, List<double> yBegin,
                                  CubicInterpolation.DerivativeApprox da,
                                  bool monotonic,
                                  CubicInterpolation.BoundaryCondition leftCond,
                                  double leftConditionValue,
                                  CubicInterpolation.BoundaryCondition rightCond,
                                  double rightConditionValue) {
            impl_ = new CubicInterpolationImpl(xBegin, size, yBegin,
                                               da, monotonic,
                                               leftCond, leftConditionValue,
                                               rightCond, rightConditionValue);
            impl_.update();
            // coeffs_ = boost::dynamic_pointer_cast<detail::CoefficientHolder>(impl_);
        }

        //public List<double> primitiveConstants() { return coeffs_.primitiveConst_; }
        public List<double> aCoefficients() { return ((CubicInterpolationImpl)impl_).a_; }
        public List<double> bCoefficients() { return ((CubicInterpolationImpl)impl_).b_; }
        public List<double> cCoefficients() { return ((CubicInterpolationImpl)impl_).c_; }
        //public List<bool> monotonicityAdjustments() { return coeffs_.monotonicityAdjustments_; }
    }

    //! %Cubic interpolation factory and traits
    public class Cubic : IInterpolationFactory {
        private CubicInterpolation.DerivativeApprox da_;
        private bool monotonic_;
        private CubicInterpolation.BoundaryCondition leftType_, rightType_;
        private double leftValue_, rightValue_;

        public Cubic() : this(CubicInterpolation.DerivativeApprox.Kruger, false, 
                              CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                              CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0) { }
        public Cubic(CubicInterpolation.DerivativeApprox da, bool monotonic, 
                     CubicInterpolation.BoundaryCondition leftCondition, double leftConditionValue,
                     CubicInterpolation.BoundaryCondition rightCondition, double rightConditionValue) {
            da_ = da;
            monotonic_ = monotonic;
            leftType_ = leftCondition;
            rightType_ = rightCondition;
            leftValue_ = leftConditionValue;
            rightValue_ = rightConditionValue;
        }

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) {
            return new CubicInterpolation(xBegin, size, yBegin, da_, monotonic_, leftType_, leftValue_, rightType_, rightValue_);
        }

        public bool global { get { return true; } }
        public int requiredPoints { get { return 2; } }
    }

    public class CubicInterpolationImpl : Interpolation.templateImpl {
        private CubicInterpolation.DerivativeApprox da_;
        private bool monotonic_;
        private CubicInterpolation.BoundaryCondition leftType_, rightType_;
        private double leftValue_, rightValue_;

        // P[i](x) = y[i] +
        //           a[i]*(x-x[i]) +
        //           b[i]*(x-x[i])^2 +
        //           c[i]*(x-x[i])^3
        public InitializedList<double> primitiveConst_, a_, b_, c_;
        InitializedList<bool> monotonicityAdjustments_;


        public CubicInterpolationImpl(List<double> xBegin, int size, List<double> yBegin,
                                  CubicInterpolation.DerivativeApprox da,
                                  bool monotonic,
                                  CubicInterpolation.BoundaryCondition leftCondition,
                                  double leftConditionValue,
                                  CubicInterpolation.BoundaryCondition rightCondition,
                                  double rightConditionValue)
            : base(xBegin, size, yBegin) {
            da_ = da;
            monotonic_ = monotonic;
            leftType_ = leftCondition;
            rightType_ = rightCondition;
            leftValue_ = leftConditionValue;
            rightValue_ = rightConditionValue;

            // coefficients
            primitiveConst_ = new InitializedList<double>(size - 1);
            a_ = new InitializedList<double>(size - 1);
            b_ = new InitializedList<double>(size - 1);
            c_ = new InitializedList<double>(size - 1);
            monotonicityAdjustments_ = new InitializedList<bool>(size);
        }

        public override void update() {
            Vector tmp = new Vector(size_);
            List<double> dx = new InitializedList<double>(size_ - 1),
                         S = new InitializedList<double>(size_ - 1);

            for (int i = 0; i < size_ - 1; ++i) {
                dx[i] = xBegin_[i+1] - xBegin_[i];
                S[i] = (yBegin_[i+1] - yBegin_[i])/dx[i];
            }

            // first derivative approximation
            if (da_==CubicInterpolation.DerivativeApprox.Spline) {
                TridiagonalOperator L = new TridiagonalOperator(size_);
                for (int i = 1; i < size_ - 1; ++i) {
                    L.setMidRow(i, dx[i], 2.0*(dx[i]+dx[i-1]), dx[i-1]);
                    tmp[i] = 3.0*(dx[i]*S[i-1] + dx[i-1]*S[i]);
                }

                // left boundary condition
                switch (leftType_) {
                    case CubicInterpolation.BoundaryCondition.NotAKnot:
                        // ignoring end condition value
                        L.setFirstRow(dx[1]*(dx[1]+dx[0]), (dx[0]+dx[1])*(dx[0]+dx[1]));
                        tmp[0] = S[0]*dx[1]*(2.0*dx[1]+3.0*dx[0]) + S[1]*dx[0]*dx[0];
                        break;
                    case CubicInterpolation.BoundaryCondition.FirstDerivative:
                        L.setFirstRow(1.0, 0.0);
                        tmp[0] = leftValue_;
                        break;
                    case CubicInterpolation.BoundaryCondition.SecondDerivative:
                        L.setFirstRow(2.0, 1.0);
                        tmp[0] = 3.0*S[0] - leftValue_*dx[0]/2.0;
                        break;
                    case CubicInterpolation.BoundaryCondition.Periodic:
                    case CubicInterpolation.BoundaryCondition.Lagrange:
                        // ignoring end condition value
                        throw new NotImplementedException("this end condition is not implemented yet");
                    default:
                        throw new ArgumentException("unknown end condition");
                }

                // right boundary condition
                switch (rightType_) {
                    case CubicInterpolation.BoundaryCondition.NotAKnot:
                        // ignoring end condition value
                        L.setLastRow(-(dx[size_ - 2] + dx[size_ - 3]) * (dx[size_ - 2] + dx[size_ - 3]),
                                     -dx[size_ - 3] * (dx[size_ - 3] + dx[size_ - 2]));
                        tmp[size_ - 1] = -S[size_ - 3] * dx[size_ - 2] * dx[size_ - 2] -
                                     S[size_ - 2] * dx[size_ - 3] * (3.0 * dx[size_ - 2] + 2.0 * dx[size_ - 3]);
                        break;
                    case CubicInterpolation.BoundaryCondition.FirstDerivative:
                        L.setLastRow(0.0, 1.0);
                        tmp[size_ - 1] = rightValue_;
                        break;
                    case CubicInterpolation.BoundaryCondition.SecondDerivative:
                        L.setLastRow(1.0, 2.0);
                        tmp[size_ - 1] = 3.0 * S[size_ - 2] + rightValue_ * dx[size_ - 2] / 2.0;
                        break;
                    case CubicInterpolation.BoundaryCondition.Periodic:
                    case CubicInterpolation.BoundaryCondition.Lagrange:
                        // ignoring end condition value
                        throw new NotImplementedException("this end condition is not implemented yet");
                    default:
                        throw new ArgumentException("unknown end condition");
                }

                // solve the system
                tmp = L.solveFor(tmp);
            } else { // local schemes
                if (size_ == 2)
                    tmp[0] = tmp[1] = S[0];
                else {
                    switch (da_) {
                        case CubicInterpolation.DerivativeApprox.FourthOrder:
                            throw new NotImplementedException("FourthOrder not implemented yet");
                        case CubicInterpolation.DerivativeApprox.Parabolic:
                            // intermediate points
                            for (int i = 1; i < size_ - 1; ++i) {
                                tmp[i] = (dx[i - 1] * S[i] + dx[i] * S[i - 1]) / (dx[i] + dx[i - 1]);
                            }
                            // end points
                            tmp[0] = ((2.0 * dx[0] + dx[1]) * S[0] - dx[0] * S[1]) / (dx[0] + dx[1]);
                            tmp[size_ - 1] = ((2.0 * dx[size_ - 2] + dx[size_ - 3]) * S[size_ - 2] - dx[size_ - 2] * S[size_ - 3]) / (dx[size_ - 2] + dx[size_ - 3]);
                            break;
                        case CubicInterpolation.DerivativeApprox.FritschButland:
                            // intermediate points
                            for (int i=1; i<size_-1; ++i) {
                                double Smin = Math.Min(S[i-1], S[i]);
                                double Smax = Math.Max(S[i-1], S[i]);
                                tmp[i] = 3.0*Smin*Smax/(Smax+2.0*Smin);
                            }
                            // end points
                            tmp[0]    = ((2.0*dx[   0]+dx[   1])*S[   0] - dx[   0]*S[   1]) / (dx[   0]+dx[   1]);
                            tmp[size_-1] = ((2.0*dx[size_-2]+dx[size_-3])*S[size_-2] - dx[size_-2]*S[size_-3]) / (dx[size_-2]+dx[size_-3]);
                            break;
                        case CubicInterpolation.DerivativeApprox.Akima:
                           tmp[0] = (Math.Abs(S[1]-S[0])*2*S[0]*S[1]+Math.Abs(2*S[0]*S[1]-4*S[0]*S[0]*S[1])*S[0])/(Math.Abs(S[1]-S[0])+Math.Abs(2*S[0]*S[1]-4*S[0]*S[0]*S[1]));
                           tmp[1] = (Math.Abs(S[2]-S[1])*S[0]+Math.Abs(S[0]-2*S[0]*S[1])*S[1])/(Math.Abs(S[2]-S[1])+Math.Abs(S[0]-2*S[0]*S[1]));
                           for (int i=2; i<size_-2; ++i) {
                              if ((S[i-2]==S[i-1]) && (S[i]!=S[i+1]))
                                    tmp[i] = S[i-1];
                              else if ((S[i-2]!=S[i-1]) && (S[i]==S[i+1]))
                                    tmp[i] = S[i];
                              else if (S[i]==S[i-1])
                                    tmp[i] = S[i];
                              else if ((S[i-2]==S[i-1]) && (S[i-1]!=S[i]) && (S[i]==S[i+1]))
                                    tmp[i] = (S[i-1]+S[i])/2.0;
                              else
                                    tmp[i] = (Math.Abs(S[i+1]-S[i])*S[i-1]+Math.Abs(S[i-1]-S[i-2])*S[i])/(Math.Abs(S[i+1]-S[i])+Math.Abs(S[i-1]-S[i-2]));
                           }
                           tmp[size_-2] = (Math.Abs(2*S[size_-2]*S[size_-3]-S[size_-2])*S[size_-3]+Math.Abs(S[size_-3]-S[size_-4])*S[size_-2])/(Math.Abs(2*S[size_-2]*S[size_-3]-S[size_-2])+Math.Abs(S[size_-3]-S[size_-4]));
                           tmp[size_-1] = (Math.Abs(4*S[size_-2]*S[size_-2]*S[size_-3]-2*S[size_-2]*S[size_-3])*S[size_-2]+Math.Abs(S[size_-2]-S[size_-3])*2*S[size_-2]*S[size_-3])/(Math.Abs(4*S[size_-2]*S[size_-2]*S[size_-3]-2*S[size_-2]*S[size_-3])+Math.Abs(S[size_-2]-S[size_-3]));
                           break;
                        case CubicInterpolation.DerivativeApprox.Kruger:
                            // intermediate points
                            for (int i = 1; i < size_ - 1; ++i) {
                                if (S[i-1]*S[i]<0.0)
                                    // slope changes sign at point
                                    tmp[i] = 0.0;
                                else
                                    // slope will be between the slopes of the adjacent
                                    // straight lines and should approach zero if the
                                    // slope of either line approaches zero
                                    tmp[i] = 2.0/(1.0/S[i-1]+1.0/S[i]);
                            }
                            // end points
                            tmp[0] = (3.0*S[0]-tmp[1])/2.0;
                            tmp[size_ - 1] = (3.0 * S[size_ - 2] - tmp[size_ - 2]) / 2.0;
                            break;
                        default:
                            throw new ArgumentException("unknown scheme");
                    }
                }
            }

            monotonicityAdjustments_.Erase();

            // Hyman monotonicity constrained filter
            if (monotonic_) {
                double correction;
                double pm, pu, pd, M;
                for (int i = 0; i < size_; ++i) {
                    if (i==0) {
                        if (tmp[i]*S[0]>0.0) {
                            correction = tmp[i]/Math.Abs(tmp[i]) *
                                Math.Min(Math.Abs(tmp[i]),
                                               Math.Abs(3.0*S[0]));
                        } else {
                            correction = 0.0;
                        }
                        if (correction!=tmp[i]) {
                            tmp[i] = correction;
                            monotonicityAdjustments_[i] = true;
                        }
                    } else if (i == size_ - 1) {
                        if (tmp[i] * S[size_ - 2] > 0.0) {
                            correction = tmp[i]/Math.Abs(tmp[i]) *
                                Math.Min(Math.Abs(tmp[i]), Math.Abs(3.0 * S[size_ - 2]));
                        } else {
                            correction = 0.0;
                        }
                        if (correction!=tmp[i]) {
                            tmp[i] = correction;
                            monotonicityAdjustments_[i] = true;
                        }
                    } else {
                        pm=(S[i-1]*dx[i]+S[i]*dx[i-1])/
                            (dx[i-1]+dx[i]);
                        M = 3.0 * Math.Min(Math.Min(Math.Abs(S[i-1]), Math.Abs(S[i])),
                                           Math.Abs(pm));
                        if (i>1) {
                            if ((S[i-1]-S[i-2])*(S[i]-S[i-1])>0.0) {
                                pd=(S[i-1]*(2.0*dx[i-1]+dx[i-2])
                                    -S[i-2]*dx[i-1])/
                                    (dx[i-2]+dx[i-1]);
                                if (pm*pd>0.0 && pm*(S[i-1]-S[i-2])>0.0) {
                                    M = Math.Max(M, 1.5*Math.Min(
                                            Math.Abs(pm),Math.Abs(pd)));
                                }
                            }
                        }
                        if (i < size_ - 2) {
                            if ((S[i]-S[i-1])*(S[i+1]-S[i])>0.0) {
                                pu=(S[i]*(2.0*dx[i]+dx[i+1])-S[i+1]*dx[i])/
                                    (dx[i]+dx[i+1]);
                                if (pm*pu>0.0 && -pm*(S[i]-S[i-1])>0.0) {
                                    M = Math.Max(M, 1.5*Math.Min(
                                            Math.Abs(pm),Math.Abs(pu)));
                                }
                            }
                        }
                        if (tmp[i]*pm>0.0) {
                            correction = tmp[i]/Math.Abs(tmp[i]) *
                                Math.Min(Math.Abs(tmp[i]), M);
                        } else {
                            correction = 0.0;
                        }
                        if (correction!=tmp[i]) {
                            tmp[i] = correction;
                            monotonicityAdjustments_[i] = true;
                        }
                    }
                }
            }


            // cubic coefficients
            for (int i = 0; i < size_ - 1; ++i) {
                a_[i] = tmp[i];
                b_[i] = (3.0*S[i] - tmp[i+1] - 2.0*tmp[i])/dx[i];
                c_[i] = (tmp[i+1] + tmp[i] - 2.0*S[i])/(dx[i]*dx[i]);
            }

            primitiveConst_[0] = 0.0;
            for (int i = 1; i < size_ - 1; ++i) {
                primitiveConst_[i] = primitiveConst_[i-1]
                    + dx[i-1] *
                    (yBegin_[i-1] + dx[i-1] *
                     (a_[i-1]/2.0 + dx[i-1] *
                      (b_[i-1]/3.0 + dx[i-1] * c_[i-1]/4.0)));
            }
        }

        public override double value(double x) {
            int j = locate(x);
            double dx = x-xBegin_[j];
            return yBegin_[j] + dx*(a_[j] + dx*(b_[j] + dx*c_[j]));
        }

        public override double primitive(double x) {
            int j = locate(x);
            double dx = x-xBegin_[j];
            return primitiveConst_[j]
                + dx*(yBegin_[j] + dx*(a_[j]/2.0
                + dx*(b_[j]/3.0 + dx*c_[j]/4.0)));
        }

        public override double derivative(double x) {
            int j = locate(x);
            double dx = x-xBegin_[j];
            return a_[j] + (2.0*b_[j] + 3.0*c_[j]*dx)*dx;
        }

        public override double secondDerivative(double x) {
            int j = locate(x);
            double dx = x-xBegin_[j];
            return 2.0*b_[j] + 6.0*c_[j]*dx;
        }
    }
}
