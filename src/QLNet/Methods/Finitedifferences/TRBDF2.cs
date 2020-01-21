/*
 Copyright (C) 2011 Fabien Le Floc'h
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
   //! TR-BDF2 scheme for finite difference methods
   /*! See <http://ssrn.com/abstract=1648878> for details.

       In this implementation, the passed operator must be derived
       from either TimeConstantOperator or TimeDependentOperator.
       Also, it must implement at least the following interface:

       // copy constructor/assignment
       // (these will be provided by the compiler if none is defined)
       Operator(const Operator&);
       Operator& operator=(const Operator&);

       // inspectors
       Size size();

       // modifiers
       void setTime(Time t);

       // operator interface
       array_type applyTo(const array_type&);
       array_type solveFor(const array_type&);
       static Operator identity(Size size);

       // operator algebra
       Operator operator*(Real, const Operator&);
       Operator operator+(const Operator&, const Operator&);
       Operator operator+(const Operator&, const Operator&);
       \endcode

       \warning The differential operator must be linear for
                this evolver to work.

       \ingroup findiff
   */

   // NOTE: There is room for performance improvement especially in
   // the array manipulation
   public class Trbdf2<Operator> : IMixedScheme where Operator : IOperator
   {
      protected Operator L_, I_, implicitPart_, explicitBDF2PartFull_, explicitTrapezoidalPart_, explicitBDF2PartMid_;
      protected double dt_, alpha_;
      protected Vector aInit_;
      protected List<BoundaryCondition<IOperator>> bcs_;

      // constructors
      public Trbdf2() { }  // required for generics
      public Trbdf2(Operator L, List<BoundaryCondition<IOperator>> bcs)
      {
         L_ = (Operator)L.Clone();
         I_ = (Operator)L.identity(L.size());
         dt_ = 0.0;
         bcs_ = bcs;
         alpha_ = 2.0 - Math.Sqrt(2.0);
      }

      public void step(ref object a, double t, double theta = 1.0)
      {
         int i;
         Vector aInit = new Vector((a as Vector).size());
         for (i = 0; i < (a as Vector).size(); i++)
         {
            aInit[i] = (a as Vector)[i];
         }
         aInit_ = aInit;
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].setTime(t);
         //trapezoidal explicit part
         if (L_.isTimeDependent())
         {
            L_.setTime(t);
            explicitTrapezoidalPart_ = (Operator)I_.subtract(I_, L_.multiply(- 0.5 * alpha_ * dt_, L_));
         }
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].applyBeforeApplying(explicitTrapezoidalPart_);
         a = explicitTrapezoidalPart_.applyTo((a as Vector));
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].applyAfterApplying((a as Vector));

         // trapezoidal implicit part
         if (L_.isTimeDependent())
         {
            L_.setTime(t - dt_);
            implicitPart_ = (Operator)I_.add(I_, L_.multiply(0.5 * alpha_ * dt_, L_));
         }
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].applyBeforeSolving(implicitPart_, (a as Vector));
         a = implicitPart_.solveFor((a as Vector));
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].applyAfterSolving((a as Vector));


         // BDF2 explicit part
         if (L_.isTimeDependent())
         {
            L_.setTime(t);
         }
         for (i = 0; i < bcs_.Count; i++)
         {
            bcs_[i].applyBeforeApplying(explicitBDF2PartFull_);
         }
         Vector b0 = explicitBDF2PartFull_.applyTo(aInit_);
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].applyAfterApplying(b0);

         for (i = 0; i < bcs_.Count; i++)
         {
            bcs_[i].applyBeforeApplying(explicitBDF2PartMid_);
         }
         Vector b1 = explicitBDF2PartMid_.applyTo((a as Vector));
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].applyAfterApplying(b1);
         a = b0 + b1;

         // reuse implicit part - works only for alpha=2-sqrt(2)
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].applyBeforeSolving(implicitPart_, (a as Vector));
         a = implicitPart_.solveFor((a as Vector));
         for (i = 0; i < bcs_.Count; i++)
            bcs_[i].applyAfterSolving((a as Vector));
      }

      public void setStep(double dt)
      {
         dt_ = dt;

         implicitPart_ = (Operator)L_.add(I_, L_.multiply(0.5 * alpha_ * dt_, L_));
         explicitTrapezoidalPart_ = (Operator)L_.subtract(I_, L_.multiply(0.5 * alpha_ * dt_, L_));
         explicitBDF2PartFull_ = (Operator)I_.multiply(-(1.0 - alpha_) * (1.0 - alpha_) / (alpha_ * (2.0 - alpha_)), I_);
         explicitBDF2PartMid_ = (Operator)I_.multiply(1.0 / (alpha_ * (2.0 - alpha_)), I_);
      }
   }
}
