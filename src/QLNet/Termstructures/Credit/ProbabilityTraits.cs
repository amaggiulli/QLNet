/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// Survival-Probability-curve traits
   /// </summary>
   public class SurvivalProbability : ITraits<DefaultProbabilityTermStructure>
   {
      const double avgHazardRate = 0.01;
      const double maxHazardRate = 1.0;

      public Date initialDate(DefaultProbabilityTermStructure c) { return c.referenceDate(); }   // start of curve data
      public double initialValue(DefaultProbabilityTermStructure c) { return 1; }    // value at reference date
      public void updateGuess(List<double> data, double discount, int i) { data[i] = discount; }
      public int maxIterations() { return 50; }   // upper bound for convergence loop

      public double discountImpl(Interpolation i, double t) { return i.value(t, true); }
      public double zeroYieldImpl(Interpolation i, double t) { throw new NotSupportedException(); }
      public double forwardImpl(Interpolation i, double t) { throw new NotSupportedException(); }

      public double guess(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData) // previous iteration value
            return c.data()[i];

         if (i == 1) // first pillar
            return 1.0 / (1.0 + avgHazardRate * 0.25);

         // extrapolate
         Date d = c.dates()[i];
         return ((DefaultProbabilityTermStructure)c).survivalProbability(d, true);
      }

      public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            return c.data().Last() / 2.0;
         }
         double dt = c.times()[i] - c.times()[i - 1];
         return c.data()[i - 1] * Math.Exp(-maxHazardRate * dt);
      }

      public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         // survival probability cannot increase
         return c.data()[i - 1];
      }
   }

   /// <summary>
   ///  Hazard-rate-curve traits
   /// </summary>
   public class HazardRate  : ITraits<DefaultProbabilityTermStructure>
   {
      const double avgHazardRate = 0.01;
      const double maxHazardRate = 1.0;

      public Date initialDate(DefaultProbabilityTermStructure c)
      {
         return c.referenceDate();
      }
      public double initialValue(DefaultProbabilityTermStructure c)
      {
         return avgHazardRate;
      }
      public double guess(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData) // previous iteration value
            return c.data()[i];

         if (i == 1) // first pillar
            return avgHazardRate;

         // extrapolate
         Date d = c.dates()[i];
         return ((DefaultProbabilityTermStructure)c).hazardRate(d, true);
      }
      public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            double r = c.data().Min();
            return r / 2.0;
         }
         return Const.QL_EPSILON;
      }
      public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            double r = c.data().Max();
            return r * 2.0;
         }
         // no constraints.
         // We choose as max a value very unlikely to be exceeded.
         return maxHazardRate;
      }
      public  void updateGuess(List<double> data, double rate, int i)
      {
         data[i] = rate;
         if (i == 1)
            data[0] = rate; // first point is updated as well
      }
      public int maxIterations() { return 30; }

      public double discountImpl(Interpolation i, double t) { return i.value(t, true); }
      public double zeroYieldImpl(Interpolation i, double t) { throw new NotSupportedException(); }
      public double forwardImpl(Interpolation i, double t) { throw new NotSupportedException(); }
   }

   /// <summary>
   /// Default-density-curve traits
   /// </summary>
   public class DefaultDensity : ITraits<DefaultProbabilityTermStructure>
   {
      const double avgHazardRate = 0.01;
      const double maxHazardRate = 1.0;

      public Date initialDate(DefaultProbabilityTermStructure c)
      {
         return c.referenceDate();
      }
      public double initialValue(DefaultProbabilityTermStructure c)
      {
         return avgHazardRate;
      }
      public double guess(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData) // previous iteration value
            return c.data()[i];

         if (i == 1) // first pillar
            return avgHazardRate;

         // extrapolate
         Date d = c.dates()[i];
         return ((DefaultProbabilityTermStructure)c).defaultDensity(d, true);
      }
      public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            double r = c.data().Min();
            return r / 2.0;
         }
         return Const.QL_EPSILON;
      }
      public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {
         if (validData)
         {
            double r = c.data().Max();
            return r * 2.0;
         }
         // no constraints.
         // We choose as max a value very unlikely to be exceeded.
         return maxHazardRate;
      }
      public void updateGuess(List<double> data, double density, int i)
      {
         data[i] = density;
         if (i == 1)
            data[0] = density; // first point is updated as well
      }
      public int maxIterations() { return 30; }

      public double discountImpl(Interpolation i, double t) { return i.value(t, true); }
      public double zeroYieldImpl(Interpolation i, double t) { throw new NotSupportedException(); }
      public double forwardImpl(Interpolation i, double t) { throw new NotSupportedException(); }
   }
}
