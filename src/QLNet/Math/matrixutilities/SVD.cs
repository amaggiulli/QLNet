﻿/*
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
   //! Singular value decomposition
   /*! Refer to Golub and Van Loan: Matrix computation,
       The Johns Hopkins University Press

       \test the correctness of the returned values is tested by
             checking their properties.
   */
   public class SVD
   {
      private Matrix U_, V_;
      private Vector s_;
      private int m_, n_;
      private bool transpose_;

      public SVD(Matrix M)
      {
         Matrix A;

         /* The implementation requires that rows > columns.
            If this is not the case, we decompose M^T instead.
            Swapping the resulting U and V gives the desired
            result for M as

            M^T = U S V^T           (decomposition of M^T)

            M = (U S V^T)^T         (transpose)

            M = (V^T^T S^T U^T)     ((AB)^T = B^T A^T)

            M = V S U^T             (idempotence of transposition,
                                     symmetry of diagonal matrix S)
         */

         if (M.rows() >= M.columns())
         {
            A = new Matrix(M);
            transpose_ = false;
         }
         else
         {
            A = Matrix.transpose(M);
            transpose_ = true;
         }

         m_ = A.rows();
         n_ = A.columns();

         // we're sure that m_ >= n_
         s_ = new Vector(n_);
         U_ = new Matrix(m_, n_);
         V_ = new Matrix(n_, n_);
         Vector e = new Vector(n_);
         Vector work = new Vector(m_);
         int i, j, k;

         // Reduce A to bidiagonal form, storing the diagonal elements
         // in s and the super-diagonal elements in e.
         int nct = Math.Min(m_ - 1, n_);
         int nrt = Math.Max(0, n_ - 2);
         for (k = 0; k < Math.Max(nct, nrt); k++)
         {
            if (k < nct)
            {

               // Compute the transformation for the k-th column and
               // place the k-th diagonal in s[k].
               // Compute 2-norm of k-th column without under/overflow.
               s_[k] = 0;
               for (i = k; i < m_; i++)
               {
                  s_[k] = hypot(s_[k], A[i, k]);
               }
               if (s_[k].IsNotEqual(0.0))
               {
                  if (A[k, k] < 0.0)
                  {
                     s_[k] = -s_[k];
                  }
                  for (i = k; i < m_; i++)
                  {
                     A[i, k] /= s_[k];
                  }
                  A[k, k] += 1.0;
               }
               s_[k] = -s_[k];
            }
            for (j = k + 1; j < n_; j++)
            {
               if ((k < nct) && (s_[k].IsNotEqual(0.0)))
               {

                  // Apply the transformation.
                  double t = 0;
                  for (i = k; i < m_; i++)
                  {
                     t += A[i, k] * A[i, j];
                  }
                  t = -t / A[k, k];
                  for (i = k; i < m_; i++)
                  {
                     A[i, j] += t * A[i, k];
                  }
               }

               // Place the k-th row of A into e for the
               // subsequent calculation of the row transformation.
               e[j] = A[k, j];
            }
            if (k < nct)
            {
               // Place the transformation in U for subsequent back multiplication.
               for (i = k; i < m_; i++)
               {
                  U_[i, k] = A[i, k];
               }
            }
            if (k < nrt)
            {
               // Compute the k-th row transformation and place the k-th super-diagonal in e[k].
               // Compute 2-norm without under/overflow.
               e[k] = 0;
               for (i = k + 1; i < n_; i++)
               {
                  e[k] = hypot(e[k], e[i]);
               }
               if (e[k].IsNotEqual(0.0))
               {
                  if (e[k + 1] < 0.0)
                  {
                     e[k] = -e[k];
                  }
                  for (i = k + 1; i < n_; i++)
                  {
                     e[i] /= e[k];
                  }
                  e[k + 1] += 1.0;
               }
               e[k] = -e[k];
               if ((k + 1 < m_) && (e[k].IsNotEqual(0.0)))
               {
                  // Apply the transformation.
                  for (i = k + 1; i < m_; i++)
                  {
                     work[i] = 0.0;
                  }
                  for (j = k + 1; j < n_; j++)
                  {
                     for (i = k + 1; i < m_; i++)
                     {
                        work[i] += e[j] * A[i, j];
                     }
                  }
                  for (j = k + 1; j < n_; j++)
                  {
                     double t = -e[j] / e[k + 1];
                     for (i = k + 1; i < m_; i++)
                     {
                        A[i, j] += t * work[i];
                     }
                  }
               }

               // Place the transformation in V for subsequent back multiplication.
               for (i = k + 1; i < n_; i++)
               {
                  V_[i, k] = e[i];
               }
            }
         }

         // Set up the final bidiagonal matrix or order n.
         if (nct < n_)
         {
            s_[nct] = A[nct, nct];
         }
         if (nrt + 1 < n_)
         {
            e[nrt] = A[nrt, n_ - 1];
         }
         e[n_ - 1] = 0.0;

         // generate U
         for (j = nct; j < n_; j++)
         {
            for (i = 0; i < m_; i++)
            {
               U_[i, j] = 0.0;
            }
            U_[j, j] = 1.0;
         }
         for (k = nct - 1; k >= 0; --k)
         {
            if (s_[k].IsNotEqual(0.0))
            {
               for (j = k + 1; j < n_; ++j)
               {
                  double t = 0;
                  for (i = k; i < m_; i++)
                  {
                     t += U_[i, k] * U_[i, j];
                  }
                  t = -t / U_[k, k];
                  for (i = k; i < m_; i++)
                  {
                     U_[i, j] += t * U_[i, k];
                  }
               }
               for (i = k; i < m_; i++)
               {
                  U_[i, k] = -U_[i, k];
               }
               U_[k, k] = 1.0 + U_[k, k];
               for (i = 0; i < k - 1; i++)
               {
                  U_[i, k] = 0.0;
               }
            }
            else
            {
               for (i = 0; i < m_; i++)
               {
                  U_[i, k] = 0.0;
               }
               U_[k, k] = 1.0;
            }
         }

         // generate V
         for (k = n_ - 1; k >= 0; --k)
         {
            if ((k < nrt) && (e[k].IsNotEqual(0.0)))
            {
               for (j = k + 1; j < n_; ++j)
               {
                  double t = 0;
                  for (i = k + 1; i < n_; i++)
                  {
                     t += V_[i, k] * V_[i, j];
                  }
                  t = -t / V_[k + 1, k];
                  for (i = k + 1; i < n_; i++)
                  {
                     V_[i, j] += t * V_[i, k];
                  }
               }
            }
            for (i = 0; i < n_; i++)
            {
               V_[i, k] = 0.0;
            }
            V_[k, k] = 1.0;
         }

         // Main iteration loop for the singular values.
         int p = n_, pp = p - 1;
         int iter = 0;
         double eps = Math.Pow(2.0, -52.0);
         while (p > 0)
         {
            int kase;

            // Here is where a test for too many iterations would go.

            // This section of the program inspects for
            // negligible elements in the s and e arrays.  On
            // completion the variables kase and k are set as follows.

            // kase = 1     if s(p) and e[k-1] are negligible and k<p
            // kase = 2     if s(k) is negligible and k<p
            // kase = 3     if e[k-1] is negligible, k<p, and
            //              s(k), ..., s(p) are not negligible (qr step).
            // kase = 4     if e(p-1) is negligible (convergence).

            for (k = p - 2; k >= -1; --k)
            {
               if (k == -1)
               {
                  break;
               }
               if (Math.Abs(e[k]) <= eps * (Math.Abs(s_[k]) + Math.Abs(s_[k + 1])))
               {
                  e[k] = 0.0;
                  break;
               }
            }
            if (k == p - 2)
            {
               kase = 4;
            }
            else
            {
               int ks;
               for (ks = p - 1; ks >= k; --ks)
               {
                  if (ks == k)
                  {
                     break;
                  }
                  double t = (ks != p ? Math.Abs(e[ks]) : 0) +
                             (ks != k + 1 ? Math.Abs(e[ks - 1]) : 0);
                  if (Math.Abs(s_[ks]) <= eps * t)
                  {
                     s_[ks] = 0.0;
                     break;
                  }
               }
               if (ks == k)
               {
                  kase = 3;
               }
               else if (ks == p - 1)
               {
                  kase = 1;
               }
               else
               {
                  kase = 2;
                  k = ks;
               }
            }
            k++;

            // Perform the task indicated by kase.
            switch (kase)
            {

               // Deflate negligible s(p).
               case 1:
                  {
                     double f = e[p - 2];
                     e[p - 2] = 0.0;
                     for (j = p - 2; j >= k; --j)
                     {
                        double t = hypot(s_[j], f);
                        double cs = s_[j] / t;
                        double sn = f / t;
                        s_[j] = t;
                        if (j != k)
                        {
                           f = -sn * e[j - 1];
                           e[j - 1] = cs * e[j - 1];
                        }
                        for (i = 0; i < n_; i++)
                        {
                           t = cs * V_[i, j] + sn * V_[i, p - 1];
                           V_[i, p - 1] = -sn * V_[i, j] + cs * V_[i, p - 1];
                           V_[i, j] = t;
                        }
                     }
                  }
                  break;

               // Split at negligible s(k).
               case 2:
                  {
                     double f = e[k - 1];
                     e[k - 1] = 0.0;
                     for (j = k; j < p; j++)
                     {
                        double t = hypot(s_[j], f);
                        double cs = s_[j] / t;
                        double sn = f / t;
                        s_[j] = t;
                        f = -sn * e[j];
                        e[j] = cs * e[j];
                        for (i = 0; i < m_; i++)
                        {
                           t = cs * U_[i, j] + sn * U_[i, k - 1];
                           U_[i, k - 1] = -sn * U_[i, j] + cs * U_[i, k - 1];
                           U_[i, j] = t;
                        }
                     }
                  }
                  break;

               // Perform one qr step.
               case 3:
                  {
                     // Calculate the shift.
                     double scale = Math.Max(
                                       Math.Max(
                                          Math.Max(
                                             Math.Max(Math.Abs(s_[p - 1]),
                                                      Math.Abs(s_[p - 2])),
                                             Math.Abs(e[p - 2])),
                                          Math.Abs(s_[k])),
                                       Math.Abs(e[k]));
                     double sp = s_[p - 1] / scale;
                     double spm1 = s_[p - 2] / scale;
                     double epm1 = e[p - 2] / scale;
                     double sk = s_[k] / scale;
                     double ek = e[k] / scale;
                     double b = ((spm1 + sp) * (spm1 - sp) + epm1 * epm1) / 2.0;
                     double c = (sp * epm1) * (sp * epm1);
                     double shift = 0.0;
                     if ((b.IsNotEqual(0.0)) || (c.IsNotEqual(0.0)))
                     {
                        shift = Math.Sqrt(b * b + c);
                        if (b < 0.0)
                        {
                           shift = -shift;
                        }
                        shift = c / (b + shift);
                     }
                     double f = (sk + sp) * (sk - sp) + shift;
                     double g = sk * ek;

                     // Chase zeros.
                     for (j = k; j < p - 1; j++)
                     {
                        double t = hypot(f, g);
                        double cs = f / t;
                        double sn = g / t;
                        if (j != k)
                        {
                           e[j - 1] = t;
                        }
                        f = cs * s_[j] + sn * e[j];
                        e[j] = cs * e[j] - sn * s_[j];
                        g = sn * s_[j + 1];
                        s_[j + 1] = cs * s_[j + 1];
                        for (i = 0; i < n_; i++)
                        {
                           t = cs * V_[i, j] + sn * V_[i, j + 1];
                           V_[i, j + 1] = -sn * V_[i, j] + cs * V_[i, j + 1];
                           V_[i, j] = t;
                        }
                        t = hypot(f, g);
                        cs = f / t;
                        sn = g / t;
                        s_[j] = t;
                        f = cs * e[j] + sn * s_[j + 1];
                        s_[j + 1] = -sn * e[j] + cs * s_[j + 1];
                        g = sn * e[j + 1];
                        e[j + 1] = cs * e[j + 1];
                        if (j < m_ - 1)
                        {
                           for (i = 0; i < m_; i++)
                           {
                              t = cs * U_[i, j] + sn * U_[i, j + 1];
                              U_[i, j + 1] = -sn * U_[i, j] + cs * U_[i, j + 1];
                              U_[i, j] = t;
                           }
                        }
                     }
                     e[p - 2] = f;
                     iter = iter + 1;
                  }
                  break;

               // Convergence.
               case 4:
                  {
                     // Make the singular values positive.
                     if (s_[k] <= 0.0)
                     {
                        s_[k] = (s_[k] < 0.0 ? -s_[k] : 0.0);
                        for (i = 0; i <= pp; i++)
                        {
                           V_[i, k] = -V_[i, k];
                        }
                     }

                     // Order the singular values.
                     while (k < pp)
                     {
                        if (s_[k] >= s_[k + 1])
                        {
                           break;
                        }
                        s_.swap(k, k + 1);
                        if (k < n_ - 1)
                        {
                           for (i = 0; i < n_; i++)
                           {
                              V_.swap(i, k, i, k + 1);
                           }
                        }
                        if (k < m_ - 1)
                        {
                           for (i = 0; i < m_; i++)
                           {
                              U_.swap(i, k, i, k + 1);
                           }
                        }
                        k++;
                     }
                     iter = 0;
                     --p;
                  }
                  break;
            }
         }
      }


      public Matrix U() { return (transpose_ ? V_ : U_); }
      public Matrix V() { return (transpose_ ? U_ : V_); }
      public Matrix S()
      {
         Matrix S = new Matrix(n_, n_);
         for (int i = 0; i < n_; i++)
         {
            for (int j = 0; j < n_; j++)
            {
               S[i, j] = 0.0;
            }
            S[i, i] = s_[i];
         }
         return S;
      }

      public Vector singularValues() { return s_; }
      public double norm2() { return s_[0]; }
      public double cond() { return s_[0] / s_[n_ - 1]; }
      public int rank()
      {
         double eps = Math.Pow(2.0, -52.0);
         double tol = m_ * s_[0] * eps;
         int r = 0;
         for (int i = 0; i < s_.size(); i++)
         {
            if (s_[i] > tol)
            {
               r++;
            }
         }
         return r;
      }

      public Vector solveFor(Vector b)
      {
         Matrix W = new Matrix(n_, n_, 0.0);
         for (int i = 0; i < n_; i++)
            W[i, i] = 1.0 / s_[i];

         Matrix inverse = V() * W * Matrix.transpose(U());
         Vector result = inverse * b;
         return result;
      }


      /*  returns hypotenuse of real (non-complex) scalars a and b by avoiding underflow/overflow
          using (a * sqrt( 1 + (b/a) * (b/a))), rather than sqrt(a*a + b*b). */
      private double hypot(double a, double b)
      {
         if (a.IsEqual(0.0))
         {
            return Math.Abs(b);
         }
         else
         {
            double c = b / a;
            return Math.Abs(a) * Math.Sqrt(1 + c * c);
         }
      }
   }
}
