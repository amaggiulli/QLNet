/*
 Copyright (C) 2008 Andrea Maggiulli
  
 This file is part of QLNet Project http://www.qlnet.org

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://trac2.assembla.com/QLNet/wiki/License>.
  
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
   public class CoefficientHolder 
   {
      public CoefficientHolder(int n)
      {
         n_ = n ;
         primitiveConst_ = new Array<double>(n - 1);
         a_ = new Array<double>(n - 1);
         b_ = new Array<double>(n - 1);
         c_ = new Array<double>(n - 1);
         monotonicityAdjustments_ = new Array<bool>(n); 
      }
      
      public virtual void Dispose() {}
      public int n_;
      public Array<double> primitiveConst_, a_, b_, c_;
      public Array<bool> monotonicityAdjustments_;
   };

   public class CubicSplineInterpolationImpl : Interpolation.templateImpl
   {
      private bool constrained_;
      private CubicSplineInterpolation.BoundaryCondition leftType_, rightType_;
      private double leftValue_, rightValue_;
      private CoefficientHolder cH_ ; 

      public CoefficientHolder getCoefficientHolder()
      {
         return cH_;
      }

      public CubicSplineInterpolationImpl(List<double> xBegin, int size, List<double> yBegin,
                                           CubicSplineInterpolation.BoundaryCondition leftCondition,
                                           double leftConditionValue,
                                           CubicSplineInterpolation.BoundaryCondition rightCondition,
                                           double rightConditionValue,
                                           bool monotonicityConstraint)
         : base(xBegin, size, yBegin) 
      {
         cH_ = new CoefficientHolder(size - xBegin.Count);
         constrained_ = monotonicityConstraint;
         leftType_ = leftCondition;
         rightType_ = rightCondition;
         leftValue_ =  leftConditionValue;
         rightValue_ = rightConditionValue;
      }


      public override void update()
      {
         TridiagonalOperator L = new TridiagonalOperator(cH_.n_);

         Array<double> tmp = new Array<double>(cH_.n_);
         var dx = new Array<double>(cH_.n_ - 1);
         var S = new Array<double>(cH_.n_ - 1);

         int i=0;
         dx[i] = this.xBegin_[i+1] - this.xBegin_[i];
         S[i] = (this.yBegin_[i+1] - this.yBegin_[i])/dx[i];
         for (i=1; i<cH_.n_-1; ++i) {
           dx[i] = this.xBegin_[i+1] - this.xBegin_[i];
           S[i] = (this.yBegin_[i+1] - this.yBegin_[i])/dx[i];

           L.setMidRow(i, dx[i], 2.0*(dx[i]+dx[i-1]), dx[i-1]);
           tmp[i] = 3.0*(dx[i]*S[i-1] + dx[i-1]*S[i]);
         }

         /**** BOUNDARY CONDITIONS ****/

         // left condition
         switch (leftType_) 
         {
            case CubicSplineInterpolation.BoundaryCondition.NotAKnot:
              // ignoring end condition value
              L.setFirstRow(dx[1]*(dx[1]+dx[0]),
                            (dx[0]+dx[1])*(dx[0]+dx[1]));
              tmp[0] = S[0]*dx[1]*(2.0*dx[1]+3.0*dx[0]) +
                       S[1]*dx[0]*dx[0];
              break;
            case CubicSplineInterpolation.BoundaryCondition.FirstDerivative:
              L.setFirstRow(1.0, 0.0);
              tmp[0] = leftValue_;
              break;
            case CubicSplineInterpolation.BoundaryCondition.SecondDerivative:
              L.setFirstRow(2.0, 1.0);
              tmp[0] = 3.0*S[0] - leftValue_*dx[0]/2.0;
              break;
            case CubicSplineInterpolation.BoundaryCondition.Periodic:
            case CubicSplineInterpolation.BoundaryCondition.Lagrange:
              // ignoring end condition value
              throw new ApplicationException("this end condition is not implemented yet");
            default:
              throw new ApplicationException("unknown end condition");
         }

         // right condition
         switch (rightType_) 
         {
            case CubicSplineInterpolation.BoundaryCondition.NotAKnot:
              // ignoring end condition value
              L.setLastRow(-(dx[cH_.n_-2]+dx[cH_.n_-3])*(dx[cH_.n_-2]+dx[cH_.n_-3]),
                           -dx[cH_.n_-3]*(dx[cH_.n_-3]+dx[cH_.n_-2]));
              tmp[cH_.n_-1] = -S[cH_.n_-3]*dx[cH_.n_-2]*dx[cH_.n_-2] -
                           S[cH_.n_-2]*dx[cH_.n_-3]*(3.0*dx[cH_.n_-2]+2.0*dx[cH_.n_-3]);
              break;
            case CubicSplineInterpolation.BoundaryCondition.FirstDerivative:
              L.setLastRow(0.0, 1.0);
              tmp[cH_.n_-1] = rightValue_;
              break;
            case CubicSplineInterpolation.BoundaryCondition.SecondDerivative:
              L.setLastRow(1.0, 2.0);
              tmp[cH_.n_-1] = 3.0*S[cH_.n_-2] + rightValue_*dx[cH_.n_-2]/2.0;
              break;
            case CubicSplineInterpolation.BoundaryCondition.Periodic:
            case CubicSplineInterpolation.BoundaryCondition.Lagrange:
              // ignoring end condition value
              throw new ApplicationException("this end condition is not implemented yet");
            default:
              throw new ApplicationException("unknown end condition");
         }

         // solve the system
         tmp = L.solveFor(tmp);

         for (int j = 0; i < cH_.monotonicityAdjustments_.Count; j++ )
            cH_.monotonicityAdjustments_[j] = false;

         if (constrained_) 
         {
           double correction;
           double pm, pu, pd, M;
           for (i=0; i<cH_.n_; ++i) {
               if (i==0) {
                   if (tmp[i]*S[0]>0.0) 
                   {
                       correction = tmp[i]/Math.Abs(tmp[i]) *
                           Math.Min(Math.Abs(tmp[i]),
                                    Math.Abs(3.0*S[0]));
                   } 
                   else 
                   {
                       correction = 0.0;
                   }
                   if (correction!=tmp[i]) 
                   {
                       tmp[i] = correction;
                       cH_.monotonicityAdjustments_[i] = true;
                   }
               } 
               else if (i==cH_.n_-1) 
               {
                  if (tmp[i]*S[cH_.n_-2]>0.0) 
                   {
                       correction = tmp[i]/Math.Abs(tmp[i]) *
                           Math.Min(Math.Abs(tmp[i]),
                                    Math.Abs(3.0*S[cH_.n_-2]));
                   } 
                   else 
                   {
                       correction = 0.0;
                   }
                   if (correction!=tmp[i]) 
                   {
                       tmp[i] = correction;
                       cH_.monotonicityAdjustments_[i] = true;
                   }
               } 
               else 
               {
                   pm=(S[i-1]*dx[i]+S[i]*dx[i-1])/
                       (dx[i-1]+dx[i]);
                   M = 3.0 * Math.Min(Math.Min(Math.Abs(S[i-1]),
                                               Math.Abs(S[i])),
                                      Math.Abs(pm));
                   if (i>1) 
                   {
                       if ((S[i-1]-S[i-2])*(S[i]-S[i-1])>0.0) 
                       {
                           pd=(S[i-1]*(2.0*dx[i-1]+dx[i-2])
                               -S[i-2]*dx[i-1])/
                               (dx[i-2]+dx[i-1]);
                           if (pm*pd>0.0 && pm*(S[i-1]-S[i-2])>0.0) 
                           {
                               M = Math.Max(M, 1.5*Math.Min(
                                       Math.Abs(pm),Math.Abs(pd)));
                           }
                       }
                   }
                   if (i<cH_.n_-2) 
                   {
                       if ((S[i]-S[i-1])*(S[i+1]-S[i])>0.0) 
                       {
                           pu=(S[i]*(2.0*dx[i]+dx[i+1])-S[i+1]*dx[i])/
                               (dx[i]+dx[i+1]);
                           if (pm*pu>0.0 && -pm*(S[i]-S[i-1])>0.0) 
                           {
                               M = Math.Max(M, 1.5*Math.Min(
                                       Math.Abs(pm),Math.Abs(pu)));
                           }
                       }
                   }
                   if (tmp[i]*pm>0.0) 
                   {
                       correction = tmp[i]/Math.Abs(tmp[i]) *
                           Math.Min(Math.Abs(tmp[i]), M);
                   } 
                   else 
                   {
                       correction = 0.0;
                   }
                   if (correction!=tmp[i]) 
                   {
                       tmp[i] = correction;
                       cH_.monotonicityAdjustments_[i] = true;
                   }
               }
           }
         }

         for (i=0; i<cH_.n_-1; ++i) 
         {
           cH_.a_[i] = tmp[i];
           cH_.b_[i] = (3.0*S[i] - tmp[i+1] - 2.0*tmp[i])/dx[i];
           cH_.c_[i] = (tmp[i+1] + tmp[i] - 2.0*S[i])/(dx[i]*dx[i]);
         }

         cH_.primitiveConst_[0] = 0.0;
         for (i=1; i<cH_.n_-1; ++i) 
         {
           cH_.primitiveConst_[i] = cH_.primitiveConst_[i-1]
               + dx[i-1] *
               (this.yBegin_[i-1] + dx[i-1] *
                (cH_.a_[i-1]/2.0 + dx[i-1] *
                 (cH_.b_[i-1]/3.0 + dx[i-1] * cH_.c_[i-1]/4.0)));
         }
      }

      public override double value(double x)
      {
         int  j = this.locate(x);
         double dx = x - this.xBegin_[j];
         return this.yBegin_[j] + dx * (cH_.a_[j] + dx * (cH_.b_[j] + dx * cH_.c_[j]));
      }

      public override double primitive(double x)
      {
         int j = this.locate(x);
         double dx = x - this.xBegin_[j];
         return cH_.primitiveConst_[j]
             + dx * (this.yBegin_[j] + dx * (cH_.a_[j] / 2.0
             + dx * (cH_.b_[j] / 3.0 + dx * cH_.c_[j] / 4.0)));
      }

      public override double derivative(double x)
      {
         int j = this.locate(x);
         double dx = x - this.xBegin_[j];
         return cH_.a_[j] + (2.0 * cH_.b_[j] + 3.0 * cH_.c_[j] * dx) * dx;
      }

      public override double secondDerivative(double x)
      {
         int j = this.locate(x);
         double dx = x - this.xBegin_[j];
         return 2.0 * cH_.b_[j] + 6.0 * cH_.c_[j] * dx;
      }
   }
   
   public class CubicSplineInterpolation : Interpolation
   {
      public enum BoundaryCondition 
      {
         /// <summary>
         /// Make second(-last) point an inactive knot
         /// </summary>
         NotAKnot,
         /// <summary>
         /// Match value of end-slope
         /// </summary>
         FirstDerivative,
         /// <summary>
         /// Match value of second derivative at end
         /// </summary>
         SecondDerivative,
         /// <summary>
         /// Match first and second derivative at either end
         /// </summary>
         Periodic,
         /// <summary>
         /// Match end-slope to the slope of the cubic that matches
         /// the first four data at the respective end
         /// </summary>
         Lagrange
      };

       private CoefficientHolder coeffs_;

       public CubicSplineInterpolation(List<double> xBegin, int size, List<double> yBegin,
                                       CubicSplineInterpolation.BoundaryCondition leftCondition,
                                       double leftConditionValue,
                                       CubicSplineInterpolation.BoundaryCondition rightCondition,
                                       double rightConditionValue,
                                       bool monotonicityConstraint) 
       {
            impl_ = (Interpolation.Impl) new CubicSplineInterpolationImpl(xBegin, size, yBegin,
                                               leftCondition,
                                               leftConditionValue,
                                               rightCondition,
                                               rightConditionValue,
                                               monotonicityConstraint);
            impl_.update();
            //coeffs_ = boost::dynamic_pointer_cast<detail::CoefficientHolder>(impl_);
        }

   }
}
