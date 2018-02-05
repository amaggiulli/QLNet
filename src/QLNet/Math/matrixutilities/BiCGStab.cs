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

namespace QLNet
{
   public struct BiCGStabResult
   {
      public BiCGStabResult(int i, double e, Vector xx)
      {
         iterations = i;
         error = e;
         x = xx;
      }

      private int iterations;
      private double error;
      private Vector x;

      public int Iterations { get { return iterations; } set { iterations = value; } }
      public double Error { get { return error; } set { error = value; } }
      public Vector X { get { return x; } set { x = value; } }
   }

   ///
   /// bi-conjugated gradient stableized algorithm
   ///
   public class BiCGStab
   {
      public delegate Vector MatrixMult(Vector x);

      public BiCGStab(MatrixMult A, int maxIter, double relTol,
                      MatrixMult preConditioner = null)
      {
         A_ = A;
         M_ = preConditioner;
         maxIter_ = maxIter;
         relTol_ = relTol;
      }

      public BiCGStabResult solve(Vector b, Vector x0 = null)
      {
         double bnorm2 = Vector.Norm2(b);
         BiCGStabResult result;
         if (bnorm2.IsEqual(0.0))
         {
            result = new BiCGStabResult(0, 0.0, b);
            return result;
         }

         Vector x = (x0 != null ? x0 : new Vector(b.size(), 0.0));
         Vector r = b - A_(x);

         Vector rTld = r;
         Vector p = null, pTld, v = null, s, sTld, t;
         double omega = 1.0;
         double rho, rhoTld = 1.0;
         double alpha = 0.0, beta;
         double error = Vector.Norm2(r) / bnorm2;

         int i;
         for (i = 0; i < maxIter_ && error >= relTol_; ++i)
         {
            rho = Vector.DotProduct(rTld, r);
            if (rho.IsEqual(0.0) || omega.IsEqual(0.0))
               break;

            if (i > 0)
            {
               beta = (rho / rhoTld) * (alpha / omega);
               p = r + beta * (p - omega * v);
            }
            else
            {
               p = r;
            }

            pTld = ((M_ != null) ? M_(p) : p);
            v = A_(pTld);

            alpha = rho / Vector.DotProduct(rTld, v);
            s = r - alpha * v;
            if (Vector.Norm2(s) < relTol_ * bnorm2)
            {
               x += alpha * pTld;
               error = Vector.Norm2(s) / bnorm2;
               break;
            }

            sTld = ((M_ != null) ? M_(s) : s);
            t = A_(sTld);
            omega = Vector.DotProduct(t, s) / Vector.DotProduct(t, t);
            x += alpha * pTld + omega * sTld;
            r = s - omega * t;
            error = Vector.Norm2(r) / bnorm2;
            rhoTld = rho;
         }

         Utils.QL_REQUIRE(i < maxIter_, () => "max number of iterations exceeded");
         Utils.QL_REQUIRE(error < relTol_, () => "could not converge");

         result = new BiCGStabResult(i, error, x);
         return result;
      }

      protected MatrixMult A_, M_;
      protected int maxIter_;
      protected double relTol_;
   }
}
