/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 *
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
   /// Base class for line search
   /// </summary>
   public abstract class LineSearch
   {
      protected LineSearch() : this(0.0)
      {}

      protected LineSearch(double UnnamedParameter1)
      {
         qt_ = 0.0;
         qpt_ = 0.0;
         succeed_ = true;
      }

      /// <summary>
      /// Returns the last point evaluated by the line search.
      /// </summary>
      public Vector lastX()
      {
         return xtd_;
      }

      /// <summary>
      /// Returns the last cost-function value.
      /// </summary>
      public double lastFunctionValue()
      {
         return qt_;
      }

      /// <summary>
      /// Returns the last gradient computed by the line search.
      /// </summary>
      public Vector lastGradient()
      {
         return gradient_;
      }

      /// <summary>
      /// Returns the squared norm of the last gradient.
      /// </summary>
      public double lastGradientNorm2()
      {
         return qpt_;
      }

      public bool succeed()
      {
         return succeed_;
      }

      /// <summary>
      /// Performs the line search.
      /// </summary>
      public abstract double value(Problem P, ref EndCriteria.Type ecType, EndCriteria NamelessParameter3, double t_ini); // initial value of line-search step

      public double update(ref Vector data, Vector direction, double beta, Constraint constraint)
      {
         double diff = beta;
         Vector newParams = data + diff * direction;
         bool valid = constraint.test(newParams);
         int icount = 0;
         while (!valid)
         {
            Utils.QL_REQUIRE(icount <= 200, () => "can't update linesearch");
            diff *= 0.5;
            icount++;
            newParams = data + diff * direction;
            valid = constraint.test(newParams);
         }
         data += diff * direction;
         return diff;
      }

      /// <summary>
      /// Gets or sets the current search direction.
      /// </summary>
      public Vector searchDirection
      {
         get
         {
            return searchDirection_;
         }
         set
         {
            searchDirection_ = value;
         }
      }

      // Current value of the search direction.
      protected Vector searchDirection_;
      // New point and its gradient.
      protected Vector xtd_;
      protected Vector gradient_ = new Vector();
      // Cost-function value and gradient norm corresponding to xtd_.
      protected double qt_;
      protected double qpt_;
      // Flag indicating whether the line search succeeded.
      protected bool succeed_;
   }
}
