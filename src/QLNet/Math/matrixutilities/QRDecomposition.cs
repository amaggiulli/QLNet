/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   public static partial class MatrixUtilities
   {
      /// <summary>
      /// Performs a QR decomposition of the given matrix.
      /// </summary>
      /// <remarks>
      /// This implementation is based on MINPACK and uses Householder transformations
      /// with optional column pivoting to compute matrices <c>Q</c> and <c>R</c> such
      /// that <c>A * P = Q * R</c>. The returned pivot vector defines the permutation
      /// matrix <c>P</c>.
      /// </remarks>
      public static List<int> qrDecomposition(Matrix M, ref Matrix q, ref Matrix r, bool pivot)
      {
         Matrix mT = Matrix.transpose(M);
         int m = M.rows();
         int n = M.columns();

         List<int> lipvt = new InitializedList<int>(n);
         Vector rdiag = new Vector(n);
         Vector wa = new Vector(n);

         MINPACK.qrfac(m, n, mT, 0, (pivot) ? 1 : 0, ref lipvt, n, ref rdiag, ref rdiag, wa);

         if (r.columns() != n || r.rows() != n)
            r = new Matrix(n, n);

         for (int i = 0; i < n; ++i)
         {
            r[i, i] = rdiag[i];
            if (i < m)
            {
               for (int j = i; j < mT.rows() - 1; j++)
                  r[i, j + 1] = mT[j + 1, i];
            }
         }

         if (q.rows() != m || q.columns() != n)
            q = new Matrix(m, n);

         Vector w = new Vector(m);
         for (int k = 0; k < m; ++k)
         {
            w.Erase();
            w[k] = 1.0;

            for (int j = 0; j < Math.Min(n, m); ++j)
            {
               double t3 = mT[j, j];
               if (t3.IsNotEqual(0.0))
               {
                  double t = 0;
                  for (int kk = j ; kk < mT.columns(); kk++)
                     t += (mT[j, kk] * w[kk]) / t3 ;

                  for (int i = j; i < m; ++i)
                  {
                     w[i] -= mT[j, i] * t;
                  }
               }
               q[k, j] = w[j];
            }
         }

         List<int> ipvt = new InitializedList<int>(n);
         if (pivot)
         {
            for (int i = 0; i < n; ++i)
               ipvt[i] = lipvt[i];
         }
         else
         {
            for (int i = 0; i < n; ++i)
               ipvt[i] = i;
         }

         return ipvt;
      }

      /// <summary>
      /// Solves the QR least-squares problem for the given system.
      /// </summary>
      /// <remarks>
      /// This implementation is based on MINPACK. Given a matrix <c>A</c>, a diagonal
      /// matrix represented by <c>d</c>, and a vector <c>b</c>, it determines the
      /// least-squares solution of <c>A * x = b</c> subject to <c>d * x = 0</c>.
      /// </remarks>
      public static Vector qrSolve(Matrix a, Vector b, bool pivot = true, Vector d = null)
      {
         int m = a.rows();
         int n = a.columns();
         if (d == null)
            d = new Vector();
         Utils.QL_REQUIRE(b.Count == m, () => "dimensions of A and b don't match");
         Utils.QL_REQUIRE(d.Count == n || d.empty(), () => "dimensions of A and d don't match");

         Matrix q = new Matrix(m, n), r = new Matrix(n, n);

         List<int> lipvt = MatrixUtilities.qrDecomposition(a, ref q, ref r, pivot);
         List<int> ipvt = new List<int>(n);
         ipvt = lipvt;

         Matrix aT = Matrix.transpose(a);
         Matrix rT = Matrix.transpose(r);

         Vector sdiag = new Vector(n);
         Vector wa = new Vector(n);

         Vector ld = new Vector(n, 0.0);
         if (!d.empty())
         {
            ld = d;
         }
         Vector x = new Vector(n);
         Vector qtb = Matrix.transpose(q) * b;

         MINPACK.qrsolv(n, rT, n, ipvt, ld, qtb, x, sdiag, wa);

         return x;
      }
   }
}
