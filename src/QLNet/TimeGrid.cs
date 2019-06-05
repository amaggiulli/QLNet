/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2019 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// Time grid class
   /// </summary>
   public class TimeGrid
   {
      /// <summary>
      /// Regularly spaced time-grid
      /// </summary>
      /// <param name="end"></param>
      /// <param name="steps"></param>
      public TimeGrid(double end, int steps)
      {
         // We seem to assume that the grid begins at 0.
         // Let's enforce the assumption for the time being
         // (even though I'm not sure that I agree.)
         Utils.QL_REQUIRE(end > 0.0, () => "negative times not allowed");
         double dt = end / steps;
         times_ = new List<double>(steps + 1);
         for (int i = 0; i <= steps; i++)
            times_.Add(dt * i);

         mandatoryTimes_ = new InitializedList<double>(1);
         mandatoryTimes_[0] = end;

         dt_ = new InitializedList<double>(steps, dt);
      }

      /// <summary>
      /// Time grid with mandatory time points
      /// <remarks>
      /// Mandatory points are guaranteed to belong to the grid.
      /// No additional points are added.
      /// </remarks>
      /// </summary>
      /// <param name="times"></param>
      public TimeGrid(List<double> times)
      {
         Utils.QL_REQUIRE(times.Count > 0, () => "empty time sequence");

         mandatoryTimes_ = new List<double>(times);
         mandatoryTimes_.Sort();

         Utils.QL_REQUIRE(mandatoryTimes_[0] >= 0.0, () => "negative times not allowed");

         for (int i = 0; i < mandatoryTimes_.Count - 1; ++i)
         {
            if (Utils.close_enough(mandatoryTimes_[i], mandatoryTimes_[i + 1]))
            {
               mandatoryTimes_.RemoveAt(i);
               i--;
            }
         }

         times_ = new List<double>(mandatoryTimes_);

         if (mandatoryTimes_[0] > 0.0)
            times_.Insert(0, 0.0);

         var dt = times_.Zip(times_.Skip(1), (x, y) => y - x);
         dt_ = dt.ToList();
      }

      /// <summary>
      /// Time grid with mandatory time points
      /// <remarks>
      /// Mandatory points are guaranteed to belong to the grid.
      /// Additional points are then added with regular spacing
      /// between pairs of mandatory times in order to reach the
      /// desired number of steps.
      /// </remarks>
      /// </summary>
      /// <param name="times"></param>
      /// <param name="steps"></param>
      public TimeGrid(List<double> times, int steps)
      {
         Utils.QL_REQUIRE(times.Count > 0, () => "empty time sequence");

         //not really finished bu run well for actals tests
         mandatoryTimes_ = new List<double>(times);
         mandatoryTimes_.Sort();

         Utils.QL_REQUIRE(mandatoryTimes_[0] >= 0.0, () => "negative times not allowed");

         for (int i = 0; i < mandatoryTimes_.Count - 1; ++i)
         {
            if (Utils.close_enough(mandatoryTimes_[i], mandatoryTimes_[i + 1]))
            {
               mandatoryTimes_.RemoveAt(i);
               i--;
            }
         }

         double last = mandatoryTimes_.Last();
         double dtMax;
         // The resulting timegrid have points at times listed in the input
         // list. Between these points, there are inner-points which are
         // regularly spaced.
         if (steps == 0)
         {
            List<double> diff = mandatoryTimes_.Zip(mandatoryTimes_.Skip(1), (x, y) => y - x).ToList();

            if (diff.First().IsEqual(0.0))
               diff.RemoveAt(0);

            dtMax = diff.Min();
         }
         else
         {
            dtMax = last / steps;
         }

         double periodBegin = 0.0;
         times_ = new List<double>();
         times_.Add(periodBegin);
         foreach (var t in mandatoryTimes_)
         {
            double periodEnd = t;
            if (periodEnd.IsNotEqual(0.0))
            {
               // the nearest integer
               int nSteps = (int)((periodEnd - periodBegin) / dtMax + 0.5);
               // at least one time step!
               nSteps = (nSteps != 0 ? nSteps : 1);
               double dt = (periodEnd - periodBegin) / nSteps;
               //times_.reserve(nSteps);
               for (int n = 1; n <= nSteps; ++n)
                  times_.Add(periodBegin + n * dt);
            }
            periodBegin = periodEnd;
         }

         var dtf = times_.Zip(times_.Skip(1), (x, y) => y - x);
         dt_ = dtf.ToList();
      }

      public TimeGrid(List<double> times, int offset, int steps)
      {
         Utils.QL_REQUIRE(times.Count > 0, () => "empty time sequence");

         //not really finished bu run well for actals tests
         mandatoryTimes_ = times.GetRange(0, offset);
         mandatoryTimes_.Sort();

         Utils.QL_REQUIRE(mandatoryTimes_[0] >= 0.0, () => "negative times not allowed");

         for (int i = 0; i < mandatoryTimes_.Count - 1; ++i)
         {
            if (Utils.close_enough(mandatoryTimes_[i], mandatoryTimes_[i + 1]))
            {
               mandatoryTimes_.RemoveAt(i);
               i--;
            }
         }

         // The resulting timegrid have points at times listed in the input
         // list. Between these points, there are inner-points which are
         // regularly spaced.
         times_ = new List<double>(steps);
         dt_ = new List<double>(steps);
         double last = mandatoryTimes_.Last();
         double dtMax = 0;

         if (steps == 0)
         {
            List<double> diff = new List<double>();
         }
         else
         {
            dtMax = last / steps;
         }

         double periodBegin = 0.0;
         times_.Add(periodBegin);

         for (int k = 0; k < mandatoryTimes_.Count; k++)
         {
            double dt = 0;
            double periodEnd = mandatoryTimes_[k];
            if (periodEnd.IsNotEqual(0.0))
            {
               // the nearest integer
               int nSteps = (int)((periodEnd - periodBegin) / dtMax + 0.5);
               // at least one time step!
               nSteps = (nSteps != 0 ? nSteps : 1);
               dt = (periodEnd - periodBegin) / nSteps;
               for (int n = 1; n <= nSteps; ++n)
               {
                  times_.Add(periodBegin + n * dt);
                  dt_.Add(dt);
               }
            }
            periodBegin = periodEnd;
         }
      }

      public double this[int i]
      {
         get
         {
            return times_[i];
         }
      }

      public List<double> Times()
      {
         return times_;
      }

      public double dt(int i)
      {
         return dt_[i];
      }

      public List<double> mandatoryTimes()
      {
         return mandatoryTimes_;
      }

      /// <summary>
      /// Time grid interface
      /// <remarks>
      /// returns the index i such that grid[i] = t
      /// </remarks>
      /// </summary>
      /// <param name="t"></param>
      /// <returns></returns>
      public int index(double t)
      {
         int i = closestIndex(t);
         if (Utils.close(t, times_[i]))
         {
            return i;
         }
         Utils.QL_REQUIRE(t >= times_.First(), () =>
                          "using inadequate time grid: all nodes are later than the required time t = "
                          + t + " (earliest node is t1 = " + times_.First() + ")");
         Utils.QL_REQUIRE(t <= times_.Last(), () =>
                          "using inadequate time grid: all nodes are earlier than the required time t = "
                          + t + " (latest node is t1 = " + times_.Last() + ")");
         int j, k;
         if (t > times_[i])
         {
            j = i;
            k = i + 1;
         }
         else
         {
            j = i - 1;
            k = i;
         }
         Utils.QL_FAIL("using inadequate time grid: the nodes closest to the required time t = "
                       + t + " are t1 = " + times_[j] + " and t2 = " + times_[k]);
         return 0;
      }

      /// <summary>
      /// Time grid interface
      /// <remarks>
      /// returns the index i such that grid[i] is closest to t
      /// </remarks>
      /// </summary>
      /// <param name="t"></param>
      /// <returns></returns>
      public int closestIndex(double t)
      {
         int result = times_.BinarySearch(t);
         if (result < 0)
            //Lower_bound is a version of binary search: it attempts to find the element value in an ordered range [first, last)
            // [1]. Specifically, it returns the first position where value could be inserted without violating the ordering.
            // [2] The first version of lower_bound uses operator< for comparison, and the second uses the function object comp.
            // lower_bound returns the furthermost iterator i in [first, last) such that, for every iterator j in [first, i), *j < value.
            result = ~result;

         if (result == 0)
         {
            return 0;
         }
         if (result == times_.Count)
         {
            return times_.Count - 1;
         }
         double dt1 = times_[result] - t;
         double dt2 = t - times_[result - 1];
         if (dt1 < dt2)
            return result;
         return result - 1;
      }

      /// <summary>
      /// Time grid interface
      /// <remarks>
      /// returns the time on the grid closest to the given t
      /// </remarks>
      /// </summary>
      /// <param name="t"></param>
      /// <returns></returns>
      public double closestTime(double t)
      {
         return times_[closestIndex(t)];
      }

      public bool empty()
      {
         return times_.Count == 0;
      }

      public int size()
      {
         return times_.Count;
      }

      public double First()
      {
         return times_.First();
      }

      public double Last()
      {
         return times_.Last();
      }

      private List<double> times_;
      private List<double> dt_;
      private List<double> mandatoryTimes_;

   }
}
