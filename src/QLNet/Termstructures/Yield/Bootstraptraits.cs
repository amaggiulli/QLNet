/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2014 Edem Dawui (edawui@gmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet
{
   public interface ITraits<T>
   {
      Date initialDate(T c);        // start of curve data

      double initialValue(T c);   // value at reference date

      double guess(int i, InterpolatedCurve c, bool validData, int first);    // possible constraints based on previous values

      double minValueAfter(int i, InterpolatedCurve c, bool validData, int first);

      double maxValueAfter(int i, InterpolatedCurve c, bool validData, int first);     // update with new guess

      void updateGuess(List<double> data, double discount, int i);

      int maxIterations();                          // upper bound for convergence loop

      //
      double discountImpl(Interpolation i, double t);

      double zeroYieldImpl(Interpolation i, double t);

      double forwardImpl(Interpolation i, double t);
   }

   public class Discount : ITraits<YieldTermStructure>
   {
      private const double maxRate = 1;
      private const double avgRate = 0.05;

      public Date initialDate(YieldTermStructure c) { return c.referenceDate(); }   // start of curve data

      public double initialValue(YieldTermStructure c) { return 1; }    // value at reference date

                                                                        // update with new guess
      public void updateGuess(List<double> data, double discount, int i) { data[i] = discount; }

      public int maxIterations() { return 100; }   // upper bound for convergence loop

      public double discountImpl(Interpolation i, double t) { return i.value(t, true); }

      public double zeroYieldImpl(Interpolation i, double t) { throw new NotSupportedException(); }

      public double forwardImpl(Interpolation i, double t) { throw new NotSupportedException(); }

      public double guess(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData) // previous iteration value
            return c.data()[i];

         if (i == 1) // first pillar
            return 1.0 / (1.0 + avgRate * c.times()[1]);

         // flat rate extrapolation
         double r = -System.Math.Log(c.data()[i - 1]) / c.times()[i - 1];
         return System.Math.Exp(-r * c.times()[i]);
      }

      public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
#if QL_NEGATIVE_RATES
            return c.data().Min() / 2.0;
#else
               return c.data().Last() / 2.0;
#endif
         }
         double dt = c.times()[i] - c.times()[i - 1];
         return c.data()[i - 1] * System.Math.Exp(-maxRate * dt);
      }

      public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
#if QL_NEGATIVE_RATES
         double dt = c.times()[i] - c.times()[i - 1];
         return c.data()[i - 1] * Math.Exp(maxRate * dt);
#else
         // discounts cannot increase
         return c.data()[i - 1];
#endif
      }
   }

   //! Zero-curve traits
   public class ZeroYield : ITraits<YieldTermStructure>
   {
      private const double maxRate = 3;
      private const double avgRate = 0.05;

      public Date initialDate(YieldTermStructure c) { return c.referenceDate(); }   // start of curve data

      public double initialValue(YieldTermStructure c) { return avgRate; }    // value at reference date

                                                                              // update with new guess
      public void updateGuess(List<double> data, double rate, int i)
      {
         data[i] = rate;
         if (i == 1)
            data[0] = rate; // first point is updated as well
      }

      public int maxIterations() { return 30; }   // upper bound for convergence loop

      public double discountImpl(Interpolation i, double t)
      {
         double r = zeroYieldImpl(i, t);
         return Math.Exp(-r * t);
      }

      public double zeroYieldImpl(Interpolation i, double t) { return i.value(t, true); }

      public double forwardImpl(Interpolation i, double t) { throw new NotSupportedException(); }

      public double guess(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData) // previous iteration value
            return c.data()[i];

         if (i == 1) // first pillar
            return avgRate;

         // extrapolate
         return zeroYieldImpl(c.interpolation_, c.times()[i]);
      }

      public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            double r = c.data().Min();
#if QL_NEGATIVE_RATES

            return r < 0.0 ? r * 2.0 : r / 2.0;
#else
            return r / 2.0;
#endif
         }
#if QL_NEGATIVE_RATES

         // no constraints.
         // We choose as min a value very unlikely to be exceeded.
         return -maxRate;
#else
         return Const.QL_EPSILON;
#endif
      }

      public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            double r = c.data().Max();
#if QL_NEGATIVE_RATES
            return r < 0.0 ? r / 2.0 : r * 2.0;
#else
            return r * 2.0;
#endif
         }
         return maxRate;
      }
   }

   //! Forward-curve traits
   public class ForwardRate : ITraits<YieldTermStructure>
   {
      private const double maxRate = 3;
      private const double avgRate = 0.05;

      public Date initialDate(YieldTermStructure c) { return c.referenceDate(); }   // start of curve data

      public double initialValue(YieldTermStructure c) { return avgRate; } // dummy value at reference date

                                                                           // update with new guess
      public void updateGuess(List<double> data, double forward, int i)
      {
         data[i] = forward;
         if (i == 1)
            data[0] = forward; // first point is updated as well
      }

      // upper bound for convergence loop
      public int maxIterations() { return 30; }

      public double discountImpl(Interpolation i, double t)
      {
         double r = zeroYieldImpl(i, t);
         return Math.Exp(-r * t);
      }

      public double zeroYieldImpl(Interpolation i, double t)
      {
         if (t.IsEqual(0.0))
            return forwardImpl(i, 0.0);
         else
            return i.primitive(t, true) / t;
      }

      public double forwardImpl(Interpolation i, double t)
      {
         return i.value(t, true);
      }

      public double guess(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData) // previous iteration value
            return c.data()[i];

         if (i == 1) // first pillar
            return avgRate;

         // extrapolate
         return forwardImpl(c.interpolation_, c.times()[i]);
      }

      public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            double r = c.data().Min();
#if QL_NEGATIVE_RATES
            return r < 0.0 ? r * 2.0 : r / 2.0;
#else
            return r / 2.0;
#endif
         }
#if QL_NEGATIVE_RATES
         // no constraints.
         // We choose as min a value very unlikely to be exceeded.
         return -maxRate;
#else
         return Const.QL_EPSILON;
#endif
      }

      public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            double r = c.data().Max();
#if QL_NEGATIVE_RATES

            return r < 0.0 ? r / 2.0 : r * 2.0;
#else
            return r * 2.0;
#endif
         }
         // no constraints.
         // We choose as max a value very unlikely to be exceeded.
         return maxRate;
      }
   }
}
