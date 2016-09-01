//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   // mixed interpolation between discrete points
   public enum Behavior
   {
      ShareRanges,  /*!< Define both interpolations over the
                               whole range defined by the passed
                               iterators. This is the default
                               behavior. */
      SplitRanges   /*!< Define the first interpolation over the
                               first part of the range, and the second
                               interpolation over the second part. */
   }
   
   public class MixedInterpolationImpl<Interpolator1,Interpolator2> : Interpolation.templateImpl
      where Interpolator1 : IInterpolationFactory,new()
      where Interpolator2 : IInterpolationFactory,new()
   {
      public MixedInterpolationImpl(List<double> xBegin, int xEnd,
                                    List<double> yBegin, int n,
                                    Behavior behavior = Behavior.ShareRanges,
                                    Interpolator1 factory1 = default(Interpolator1),
                                    Interpolator2 factory2 = default(Interpolator2))
            : base(xBegin, xEnd, yBegin,
              Math.Max(factory1 == null ? (factory1 = new Interpolator1()).requiredPoints : factory1.requiredPoints,
                       factory2 == null ? (factory2 = new Interpolator2()).requiredPoints : factory2.requiredPoints))

      {
         n_ = n;

         xBegin2_ = xBegin.GetRange( n_, xBegin.Count);
         yBegin2_ = yBegin.GetRange( n_, yBegin.Count );

         Utils.QL_REQUIRE( xBegin2_.Count < size_,()=> "too large n (" + n + ") for " + size_ + "-element x sequence");

         switch (behavior) 
         {
            case Behavior.ShareRanges:
               interpolation1_ = factory1.interpolate(xBegin_,size_,yBegin_);
               interpolation2_ = factory2.interpolate(xBegin_,size_,yBegin_);
               break;
            case Behavior.SplitRanges:
               interpolation1_ = factory1.interpolate(xBegin_,xBegin2_.Count+1,yBegin_);
               interpolation2_ = factory2.interpolate(xBegin2_,size_,yBegin2_);
               break;
            default:
               Utils.QL_FAIL("unknown mixed-interpolation behavior: " + behavior);
               break;
         }
   }


      public override void update() 
      {
         interpolation1_.update();
         interpolation2_.update();
      }
            
      public override double value(double x) 
      {
         if (x<(xBegin2_.First()))
            return interpolation1_.value(x, true);
         return interpolation2_.value(x, true);
      }

      public override double primitive(double x) 
      {
         if (x<(xBegin2_.First()))
            return interpolation1_.primitive(x, true);
         return interpolation2_.primitive(x, true) -
            interpolation2_.primitive(xBegin2_.First(), true) +
            interpolation1_.primitive( xBegin2_.First(), true );
      }

      public override double derivative(double x) 
      {
         if ( x < ( xBegin2_.First() ) )
            return interpolation1_.derivative(x, true);
         return interpolation2_.derivative(x, true);
      }  
            
      public override double secondDerivative(double x) 
      {
         if ( x < ( xBegin2_.First() ) )
            return interpolation1_.secondDerivative(x, true);
         return interpolation2_.secondDerivative(x, true);
      }

      public int switchIndex() { return n_; }
          
      private List<double> xBegin2_;
      private List<double> yBegin2_;
      private int n_;
      private Interpolation interpolation1_, interpolation2_;
        
   }
 
   //! mixed linear/cubic interpolation between discrete points
   public class MixedLinearCubicInterpolation : Interpolation 
   {
      /*! \pre the \f$ x \f$ values must be sorted. */
      public MixedLinearCubicInterpolation(List<double> xBegin, int xEnd,
                                           List<double> yBegin, int n,
                                           Behavior behavior,
                                           CubicInterpolation.DerivativeApprox da,
                                           bool monotonic,
                                           CubicInterpolation.BoundaryCondition leftC,
                                           double leftConditionValue,
                                           CubicInterpolation.BoundaryCondition rightC,
                                           double rightConditionValue) 
      {
            impl_ = new MixedInterpolationImpl<Linear, Cubic>(xBegin, xEnd, yBegin, n, behavior,
                    new Linear(),
                    new Cubic(da, monotonic,leftC, leftConditionValue,rightC, rightConditionValue));
            impl_.update();
        }
    }
       
   //! mixed linear/cubic interpolation factory and traits
   /*! \ingroup interpolations */
   public class MixedLinearCubic 
   {
      public MixedLinearCubic(int n,
                              Behavior behavior,
                              CubicInterpolation.DerivativeApprox da,
                              bool monotonic = true,
                              CubicInterpolation.BoundaryCondition leftCondition = QLNet.CubicInterpolation.BoundaryCondition.SecondDerivative,
                              double leftConditionValue = 0.0,
                              CubicInterpolation.BoundaryCondition rightCondition = CubicInterpolation.BoundaryCondition.SecondDerivative,
                              double rightConditionValue = 0.0)
      {
         n_ = n; 
         behavior_ = behavior; 
         da_ = da; 
         monotonic_ = monotonic;
         leftType_ = leftCondition; 
         rightType_ = rightCondition;
         leftValue_ = leftConditionValue;
         rightValue_ = rightConditionValue;
      }

      Interpolation interpolate(List<double> xBegin, int xEnd,List<double> yBegin) 
      {
         return new MixedLinearCubicInterpolation(xBegin, xEnd,
                                                  yBegin, n_, behavior_,
                                                  da_, monotonic_,
                                                  leftType_, leftValue_,
                                                  rightType_, rightValue_);
      }

      // fix below
      public bool global = true;
      public int requiredPoints = 3;

      private int n_;
      private Behavior behavior_;
      private CubicInterpolation.DerivativeApprox da_;
      private bool monotonic_;
      private CubicInterpolation.BoundaryCondition leftType_, rightType_;
      private double leftValue_, rightValue_;
   }

   // convenience classes

   public class MixedLinearCubicNaturalSpline : MixedLinearCubicInterpolation 
   {
      /*! \pre the \f$ x \f$ values must be sorted. */
      public MixedLinearCubicNaturalSpline(List<double> xBegin, int xEnd,List<double> yBegin, int n,
         Behavior behavior = Behavior.ShareRanges)
      : base(xBegin, xEnd, yBegin, n, behavior,
             CubicInterpolation.DerivativeApprox.Spline, false,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0) 
      {}
   }

   public class MixedLinearMonotonicCubicNaturalSpline : MixedLinearCubicInterpolation 
   {
      /*! \pre the \f$ x \f$ values must be sorted. */
      public MixedLinearMonotonicCubicNaturalSpline(List<double> xBegin, int  xEnd,List<double> yBegin, int n,
         Behavior behavior = Behavior.ShareRanges)
      : base(xBegin, xEnd, yBegin, n, behavior,
             CubicInterpolation.DerivativeApprox.Spline, true,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0) 
      {}
   }

   public class MixedLinearKrugerCubic : MixedLinearCubicInterpolation 
   {
      /*! \pre the \f$ x \f$ values must be sorted. */
      public MixedLinearKrugerCubic(List<double> xBegin, int xEnd,List<double> yBegin, int n,
         Behavior behavior = Behavior.ShareRanges)
      : base(xBegin, xEnd, yBegin, n, behavior,
             CubicInterpolation.DerivativeApprox.Kruger, false,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0) 
      {}
   }

   public class MixedLinearFritschButlandCubic : MixedLinearCubicInterpolation 
   {
      /*! \pre the \f$ x \f$ values must be sorted. */
      public MixedLinearFritschButlandCubic(List<double> xBegin, int xEnd,List<double> yBegin, int n,
         Behavior behavior = Behavior.ShareRanges )
      : base(xBegin, xEnd, yBegin, n, behavior,
             CubicInterpolation.DerivativeApprox.FritschButland, false,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0) 
      {}
   }

   public class MixedLinearParabolic : MixedLinearCubicInterpolation 
   {
      /*! \pre the \f$ x \f$ values must be sorted. */
      public MixedLinearParabolic(List<double> xBegin, int xEnd,List<double> yBegin, int n,
         Behavior behavior = Behavior.ShareRanges)
      : base(xBegin, xEnd, yBegin, n, behavior,
             CubicInterpolation.DerivativeApprox.Parabolic, false,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0) 
      {}
   }

   public class MixedLinearMonotonicParabolic : MixedLinearCubicInterpolation 
   {
      /*! \pre the \f$ x \f$ values must be sorted. */
      public MixedLinearMonotonicParabolic(List<double> xBegin, int xEnd,List<double> yBegin, int n,
         Behavior behavior =  Behavior.ShareRanges)
      : base(xBegin, xEnd, yBegin, n, behavior,
             CubicInterpolation.DerivativeApprox.Parabolic, true,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
             CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0) 
      {}
   }

}
