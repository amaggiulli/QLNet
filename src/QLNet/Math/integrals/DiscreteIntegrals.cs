/*
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
using System.Linq;

namespace QLNet
{
   /*! References:
       Levy, D. Numerical Integration
       http://www2.math.umd.edu/~dlevy/classes/amsc466/lecture-notes/integration-chap.pdf
   */
   public class DiscreteTrapezoidIntegral
   {
      public double value(Vector x, Vector f)
      {
         int n = f.size();
         Utils.QL_REQUIRE(n == x.size(), () => "inconsistent size");

         double acc = 0;

         for (int i = 0; i < n - 1; ++i)
         {
            acc += ((x[i + 1] - x[i]) * (f[i] + f[i + 1]));
         }

         return 0.5 * acc;
      }
   }
   public class DiscreteSimpsonIntegral
   {
      public double value(Vector x, Vector f)
      {
         int n = f.size();
         Utils.QL_REQUIRE(n == x.size(), () => "inconsistent size");

         double acc = 0;

         for (int j = 0; j < n - 2; j += 2)
         {
            double dxj = x[j + 1] - x[j];
            double dxjp1 = x[j + 2] - x[j + 1];

            double alpha = -dxjp1 * (2 * x[j] - 3 * x[j + 1] + x[j + 2]);
            double dd = x[j + 2] - x[j];
            double k = dd / (6 * dxjp1 * dxj);
            double beta = dd * dd;
            double gamma = dxj * (x[j] - 3 * x[j + 1] + 2 * x[j + 2]);

            acc += (k * alpha * f[j] + k * beta * f[j + 1] + k * gamma * f[j + 2]);
         }
         if (!((n & 1) == 1))
         {
            acc += (0.5 * (x[n - 1] - x[n - 2]) * (f[n - 1] + f[n - 2]));
         }

         return acc;
      }
   }
   public class DiscreteTrapezoidIntegrator : Integrator
   {
      public DiscreteTrapezoidIntegrator(int evaluations)
         : base(null, evaluations)
      { }

      protected override double integrate(Func<double, double> f, double a, double b)
      {
         Vector x = new Vector(maxEvaluations(), a, (b - a) / (maxEvaluations() - 1));
         Vector fv = new Vector(x.size());
         x.ForEach((g, gg) => fv[g] = f(gg));

         increaseNumberOfEvaluations(maxEvaluations());
         return new DiscreteTrapezoidIntegral().value(x, fv);
      }
   }
   public class DiscreteSimpsonIntegrator : Integrator
   {
      public DiscreteSimpsonIntegrator(int evaluations)
         : base(null, evaluations)
      { }

      protected override double integrate(Func<double, double> f, double a, double b)
      {
         Vector x = new Vector(maxEvaluations(), a, (b - a) / (maxEvaluations() - 1));
         Vector fv = new Vector(x.size());
         x.ForEach((g, gg) => fv[g] = f(gg));

         increaseNumberOfEvaluations(maxEvaluations());
         return new DiscreteSimpsonIntegral().value(x, fv);
      }
   }
}
