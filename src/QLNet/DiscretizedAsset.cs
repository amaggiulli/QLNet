/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
   /// Discretized asset class used by numerical
   /// </summary>
   public abstract class DiscretizedAsset
   {
      private Lattice method_;

      public Lattice method()
      {
         return method_;
      }

      protected double time_;

      public double time()
      {
         return time_;
      }

      protected double latestPreAdjustment_, latestPostAdjustment_;

      protected Vector values_;

      public Vector values()
      {
         return values_;
      }

      protected DiscretizedAsset()
      {
         latestPreAdjustment_ = double.MaxValue;
         latestPostAdjustment_ = double.MaxValue;
      }

      /* High-level interface

                  Users of discretized assets should use these methods in
                  order to initialize, evolve and take the present value of
                  the assets.  They call the corresponding methods in the
                  Lattice interface, to which we refer for
                  documentation.

              */

      public void initialize(Lattice method, double t)
      {
         method_ = method;
         method_.initialize(this, t);
      }

      public void rollback(double to)
      {
         method_.rollback(this, to);
      }

      public void partialRollback(double to)
      {
         method_.partialRollback(this, to);
      }

      public double presentValue()
      {
         return method_.presentValue(this);
      }

      /* Low-level interface

          These methods (that developers should override when
          deriving from DiscretizedAsset) are to be used by
          numerical methods and not directly by users, with the
          exception of adjustValues(), preAdjustValues() and
          postAdjustValues() that can be used together with
          partialRollback().
      */

      /// <summary>
      /// Initializes the asset values to an array of the given size.
      /// </summary>
      /// <remarks>
      /// The actual values depend on the particular asset.
      /// </remarks>
      public abstract void reset(int size);

      /// <summary>
      /// Performs pre-adjustment after rollback.
      /// </summary>
      /// <remarks>
      /// This is invoked after rollback and before any other asset has a
      /// chance to inspect the values. For instance, payments happening at
      /// times already spanned by the rollback are added here. Derived
      /// classes must override <see cref="preAdjustValuesImpl"/> instead.
      /// </remarks>

      public void preAdjustValues()
      {
         if (!Utils.close(time(), latestPreAdjustment_))
         {
            preAdjustValuesImpl();
            latestPreAdjustment_ = time();
         }
      }

      /// <summary>
      /// Performs post-adjustment after rollback.
      /// </summary>
      /// <remarks>
      /// This is invoked after rollback and after any other asset had a
      /// chance to inspect the values. For instance, payments happening at
      /// the present time are added here. Derived classes must override
      /// <see cref="postAdjustValuesImpl"/> instead.
      /// </remarks>

      public void postAdjustValues()
      {
         if (!Utils.close(time(), latestPostAdjustment_))
         {
            postAdjustValuesImpl();
            latestPostAdjustment_ = time();
         }
      }

      /// <summary>
      /// Performs both pre-adjustment and post-adjustment.
      /// </summary>

      public void adjustValues()
      {
         preAdjustValues();
         postAdjustValues();
      }

      /// <summary>
      /// This method returns the times at which the numerical
      /// method should stop while rolling back the asset. Typical
      /// examples include payment times, exercise times and such.
      /// </summary>
      /// <remarks>
      /// Note: The returned values are not guaranteed to be sorted.
      /// </remarks>
      public abstract List<double> mandatoryTimes();

      /// <summary>
      /// Returns true if the asset was rolled at the given time.
      /// </summary>

      protected bool isOnTime(double t)
      {
         TimeGrid grid = method().timeGrid();
         return Utils.close(grid[grid.index(t)], time());
      }

      /// <summary>
      /// Performs the actual pre-adjustment.
      /// </summary>

      protected virtual void preAdjustValuesImpl()
      {}

      /// <summary>
      /// Performs the actual post-adjustment.
      /// </summary>

      protected virtual void postAdjustValuesImpl()
      {}

      // safe version of QL double* time()
      public void setTime(double t)
      {
         time_ = t;
      }

      // safe version of QL Vector* values()
      public void setValues(Vector v)
      {
         values_ = v;
      }
   }

   /// <summary>
   /// Useful discretized discount bond asset
   /// </summary>
   public class DiscretizedDiscountBond : DiscretizedAsset
   {
      public override void reset(int size)
      {
         values_ = new Vector(size, 1.0);
      }

      public override List<double> mandatoryTimes()
      {
         return new Vector();
      }
   }

   /// <summary>
   /// Discretized option on a given asset
   /// </summary>
   /// <remarks>
   /// Warning: it is advised that derived classes take care of
   /// creating and initializing themselves an instance of
   /// the underlying.
   /// </remarks>

   public class DiscretizedOption : DiscretizedAsset
   {
      protected DiscretizedAsset underlying_;
      protected Exercise.Type exerciseType_;
      protected List<double> exerciseTimes_;

      public DiscretizedOption(DiscretizedAsset underlying, Exercise.Type exerciseType, List<double> exerciseTimes)
      {
         underlying_ = underlying;
         exerciseType_ = exerciseType;
         exerciseTimes_ = exerciseTimes;
      }

      public override void reset(int size)
      {
         Utils.QL_REQUIRE(method() == underlying_.method(),
                          () => "option and underlying were initialized on different methods");
         values_ = new Vector(size, 0.0);
         adjustValues();
      }

      public override List<double> mandatoryTimes()
      {
         List<double> times = underlying_.mandatoryTimes();

         // add the positive ones
         times.AddRange(exerciseTimes_.FindAll(x => x > 0));
         return times;
      }

      protected override void postAdjustValuesImpl()
      {
         /* In the real world, with time flowing forward, first
            any payment is settled and only after options can be
            exercised. Here, with time flowing backward, options
            must be exercised before performing the adjustment.
         */
         underlying_.partialRollback(time());
         underlying_.preAdjustValues();
         switch (exerciseType_)
         {
            case Exercise.Type.American:
               if (time_ >= exerciseTimes_[0] && time_ <= exerciseTimes_[1])
                  applyExerciseCondition();
               break;
            case Exercise.Type.Bermudan:
            case Exercise.Type.European:
               for (int i = 0; i < exerciseTimes_.Count; i++)
               {
                  double t = exerciseTimes_[i];
                  if (t >= 0.0 && isOnTime(t))
                     applyExerciseCondition();
               }
               break;
            default:
               Utils.QL_FAIL("invalid exercise type");
               break;
         }
         underlying_.postAdjustValues();
      }

      protected void applyExerciseCondition()
      {
         for (int i = 0; i < values_.size(); i++)
            values_[i] = Math.Max(underlying_.values()[i], values_[i]);
      }
   }
}
