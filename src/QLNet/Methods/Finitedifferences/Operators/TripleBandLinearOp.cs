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
   public class TripleBandLinearOp : FdmLinearOp
   {
      public TripleBandLinearOp(int direction, FdmMesher mesher)
      {
         direction_ = direction;
         i0_ = new InitializedList<int>(mesher.layout().size());
         i2_ = new InitializedList<int>(mesher.layout().size());
         reverseIndex_ = new InitializedList<int>(mesher.layout().size());
         lower_ = new InitializedList<double>(mesher.layout().size());
         diag_ = new InitializedList<double>(mesher.layout().size());
         upper_ = new InitializedList<double>(mesher.layout().size());
         mesher_ = mesher;

         FdmLinearOpLayout layout = mesher.layout();
         FdmLinearOpIterator endIter = layout.end();

         int tmp;
         List<int> newDim = new List<int>(layout.dim());
         tmp = newDim[direction_];
         newDim[direction_] = newDim[0];
         newDim[0] = tmp;

         List<int> newSpacing = new FdmLinearOpLayout(newDim).spacing();
         tmp = newSpacing[direction_];
         newSpacing[direction_] = newSpacing[0];
         newSpacing[0] = tmp;

         for (FdmLinearOpIterator iter = layout.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();

            i0_[i] = layout.neighbourhood(iter, direction, -1);
            i2_[i] = layout.neighbourhood(iter, direction,  1);

            List<int> coordinates = iter.coordinates();

            int newIndex = coordinates.inner_product(0, coordinates.Count, 0, newSpacing, 0);
            reverseIndex_[newIndex] = i;
         }
      }

      public TripleBandLinearOp(TripleBandLinearOp m)
      {
         direction_ = m.direction_;
         i0_ = new InitializedList<int>(m.mesher_.layout().size());
         i2_ = new InitializedList<int>(m.mesher_.layout().size());
         reverseIndex_ = new InitializedList<int>(m.mesher_.layout().size());
         lower_ = new InitializedList<double>(m.mesher_.layout().size());
         diag_ = new InitializedList<double>(m.mesher_.layout().size());
         upper_ = new InitializedList<double>(m.mesher_.layout().size());
         mesher_ = m.mesher_;

         int len = m.mesher_.layout().size();
         m.i0_.copy(0, len, 0, i0_);
         m.i2_.copy(0, len, 0, i2_);
         m.reverseIndex_.copy(0, len, 0, reverseIndex_);
         m.lower_.copy(0, len, 0, lower_);
         m.diag_.copy(0, len, 0, diag_);
         m.upper_.copy(0, len, 0, upper_);
      }

      protected TripleBandLinearOp()
      {

      }

      public override Vector apply(Vector r)
      {
         FdmLinearOpLayout index = mesher_.layout();

         Utils.QL_REQUIRE(r.size() == index.size(), () => "inconsistent length of r");

         Vector retVal = new Vector(r.size());
         //#pragma omp parallel for
         for (int i = 0; i < index.size(); ++i)
         {
            retVal[i] = r[i0_[i]] * lower_[i] + r[i] * diag_[i] + r[i2_[i]] * upper_[i];
         }

         return retVal;
      }

      public override SparseMatrix toMatrix()
      {
         FdmLinearOpLayout index = mesher_.layout();
         int n = index.size();

         SparseMatrix retVal = new SparseMatrix(n, n);
         for (int i = 0; i < n; ++i)
         {
            retVal[i, i0_[i]] += lower_[i];
            retVal[i, i     ] += diag_[i];
            retVal[i, i2_[i]] += upper_[i];
         }

         return retVal;
      }

      public Vector solve_splitting(Vector r, double a, double b = 1.0)
      {
         FdmLinearOpLayout layout = mesher_.layout();
         Utils.QL_REQUIRE(r.size() == layout.size(), () => "inconsistent size of rhs");

         for (FdmLinearOpIterator iter = layout.begin();
              iter != layout.end(); ++iter)
         {
            List<int> coordinates = iter.coordinates();
            Utils.QL_REQUIRE(coordinates[direction_] != 0
                             || lower_[iter.index()] == 0, () => "removing non zero entry!");
            Utils.QL_REQUIRE(coordinates[direction_] != layout.dim()[direction_] - 1
                             || upper_[iter.index()] == 0, () => "removing non zero entry!");
         }

         Vector retVal = new Vector(r.size()), tmp = new Vector(r.size());

         // Thomson algorithm to solve a tridiagonal system.
         // Example code taken from Tridiagonalopertor and
         // changed to fit for the triple band operator.
         int rim1 = reverseIndex_[0];
         double bet = 1.0 / (a * diag_[rim1] + b);
         Utils.QL_REQUIRE(bet != 0.0, () => "division by zero");
         retVal[reverseIndex_[0]] = r[rim1] * bet;

         for (int j = 1; j <= layout.size() - 1; j++)
         {
            int ri = reverseIndex_[j];
            tmp[j] = a * upper_[rim1] * bet;

            bet = b + a * (diag_[ri] - tmp[j] * lower_[ri]);
            Utils.QL_REQUIRE(bet != 0.0, () => "division by zero"); //QL_ENSURE
            bet = 1.0 / bet;

            retVal[ri] = (r[ri] - a * lower_[ri] * retVal[rim1]) * bet;
            rim1 = ri;
         }
         // cannot be j>=0 with Size j
         for (int j = layout.size() - 2; j > 0; --j)
            retVal[reverseIndex_[j]] -= tmp[j + 1] * retVal[reverseIndex_[j + 1]];
         retVal[reverseIndex_[0]] -= tmp[1] * retVal[reverseIndex_[1]];

         return retVal;
      }

      public TripleBandLinearOp mult(Vector u)
      {
         TripleBandLinearOp retVal = new TripleBandLinearOp(direction_, mesher_);

         int size = mesher_.layout().size();
         //#pragma omp parallel for
         for (int i = 0; i < size; ++i)
         {
            double s = u[i];
            retVal.lower_[i] = lower_[i] * s;
            retVal.diag_[i] = diag_[i] * s;
            retVal.upper_[i] = upper_[i] * s;
         }

         return retVal;
      }

      public TripleBandLinearOp multR(Vector u)
      {
         FdmLinearOpLayout layout = mesher_.layout();
         int size = layout.size();
         Utils.QL_REQUIRE(u.size() == size, () => "inconsistent size of rhs");
         TripleBandLinearOp retVal = new TripleBandLinearOp(direction_, mesher_);

         for (int i = 0; i < size; ++i)
         {
            double sm1 = i > 0 ? u[i - 1] : 1.0;
            double s0 = u[i];
            double sp1 = i < size - 1 ? u[i + 1] : 1.0;
            retVal.lower_[i] = lower_[i] * sm1;
            retVal.diag_[i] = diag_[i] * s0;
            retVal.upper_[i] = upper_[i] * sp1;
         }

         return retVal;
      }

      public TripleBandLinearOp add
         (TripleBandLinearOp m)
      {
         TripleBandLinearOp retVal = new TripleBandLinearOp(direction_, mesher_);
         int size = mesher_.layout().size();
         //#pragma omp parallel for
         for (int i = 0; i < size; ++i)
         {
            retVal.lower_[i] = lower_[i] + m.lower_[i];
            retVal.diag_[i] = diag_[i]  + m.diag_[i];
            retVal.upper_[i] = upper_[i] + m.upper_[i];
         }

         return retVal;
      }

      public TripleBandLinearOp add
         (Vector u)
      {

         TripleBandLinearOp retVal = new TripleBandLinearOp(direction_, mesher_);

         int size = mesher_.layout().size();
         //#pragma omp parallel for
         for (int i = 0; i < size; ++i)
         {
            retVal.lower_[i] = lower_[i];
            retVal.upper_[i] = upper_[i];
            retVal.diag_[i] = diag_[i] + u[i];
         }

         return retVal;
      }

      public void axpyb(Vector a, TripleBandLinearOp x, TripleBandLinearOp y, Vector b)
      {
         int size = mesher_.layout().size();

         if (a.empty())
         {
            if (b.empty())
            {
               //#pragma omp parallel for
               for (int i = 0; i < size; ++i)
               {
                  diag_[i]  = y.diag_[i];
                  lower_[i] = y.lower_[i];
                  upper_[i] = y.upper_[i];
               }
            }
            else
            {
               int binc = (b.size() > 1) ? 1 : 0;
               //#pragma omp parallel for
               for (int i = 0; i < size; ++i)
               {
                  diag_[i]  = y.diag_[i] + b[i * binc];
                  lower_[i] = y.lower_[i];
                  upper_[i] = y.upper_[i];
               }
            }
         }
         else if (b.empty())
         {
            int ainc = (a.size() > 1) ? 1 : 0;

            //#pragma omp parallel for
            for (int i = 0; i < size; ++i)
            {
               double s = a[i * ainc];
               diag_[i]  = y.diag_[i]  + s * x.diag_[i];
               lower_[i] = y.lower_[i] + s * x.lower_[i];
               upper_[i] = y.upper_[i] + s * x.upper_[i];
            }
         }
         else
         {
            int binc = (b.size() > 1) ? 1 : 0;
            int ainc = (a.size() > 1) ? 1 : 0;

            //#pragma omp parallel for
            for (int i = 0; i < size; ++i)
            {
               double s = a[i * ainc];
               diag_[i] = y.diag_[i] + s * x.diag_[i] + b[i * binc];
               lower_[i] = y.lower_[i] + s * x.lower_[i];
               upper_[i] = y.upper_[i] + s * x.upper_[i];
            }
         }
      }

      public void swap(TripleBandLinearOp m)
      {
         Utils.swap(ref mesher_, ref m.mesher_);
         Utils.swap(ref direction_, ref m.direction_);
         Utils.swap(ref i0_, ref m.i0_);
         Utils.swap(ref i2_, ref m.i2_);
         Utils.swap(ref reverseIndex_, ref m.reverseIndex_);
         Utils.swap(ref lower_, ref m.lower_);
         Utils.swap(ref diag_, ref m.diag_);
         Utils.swap(ref upper_, ref m.upper_);
      }

      #region IOperator interface
      public override int size() { return 0; }
      public override IOperator identity(int size) { return null; }
      public override Vector applyTo(Vector v) { return null; }
      public override Vector solveFor(Vector rhs) { return null; }

      public override IOperator multiply(double a, IOperator D) { return null; }
      public override IOperator add
         (IOperator A, IOperator B) { return null; }
      public override IOperator subtract(IOperator A, IOperator B) { return null; }

      public override bool isTimeDependent() { return false; }
      public override void setTime(double t) { }
      public override object Clone() { return this.MemberwiseClone(); }
      #endregion

      protected int direction_;
      protected List<int> i0_, i2_;
      protected List<int> reverseIndex_;
      protected List<double> lower_, diag_, upper_;
      protected FdmMesher mesher_;
   }
}
