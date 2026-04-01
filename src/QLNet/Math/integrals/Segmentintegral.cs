/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

namespace QLNet
{

   /// <summary>
   /// Integral of a one-dimensional function using the trapezoidal rule.
   /// </summary>
   /// <remarks>
   /// Given a number of intervals <c>N</c>, the integral between <c>a</c> and <c>b</c>
   /// is approximated using equally spaced points and the trapezoidal formula.
   /// </remarks>
   public class SegmentIntegral : Integrator
   {
      private int intervals_;

      public SegmentIntegral(int intervals)
         : base(1, 1)
      {
         intervals_ = intervals;

         Utils.QL_REQUIRE(intervals > 0, () => "at least 1 interval needed, 0 given");
      }

      // inline and template definitions
      protected override double integrate(Func<double, double> f, double a, double b)
      {
         double dx = (b - a) / intervals_;
         double sum = 0.5 * (f(a) + f(b));
         double end = b - 0.5 * dx;
         for (double x = a + dx; x < end; x += dx)
            sum += f(x);
         return sum * dx;
      }
   }

}
