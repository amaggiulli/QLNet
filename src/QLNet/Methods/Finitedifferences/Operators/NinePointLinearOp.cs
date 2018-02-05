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

namespace QLNet
{
   public class NinePointLinearOp : FdmLinearOp
   {
      public NinePointLinearOp(int d0, int d1, FdmMesher mesher)
      {
         d0_ = d0;
         d1_ = d1;
         i00_ = new InitializedList<int>(mesher.layout().size());
         i10_ = new InitializedList<int>(mesher.layout().size());
         i20_ = new InitializedList<int>(mesher.layout().size());
         i01_ = new InitializedList<int>(mesher.layout().size());
         i21_ = new InitializedList<int>(mesher.layout().size());
         i02_ = new InitializedList<int>(mesher.layout().size());
         i12_ = new InitializedList<int>(mesher.layout().size());
         i22_ = new InitializedList<int>(mesher.layout().size());
         a00_ = new InitializedList<double>(mesher.layout().size());
         a10_ = new InitializedList<double>(mesher.layout().size());
         a20_ = new InitializedList<double>(mesher.layout().size());
         a01_ = new InitializedList<double>(mesher.layout().size());
         a11_ = new InitializedList<double>(mesher.layout().size());
         a21_ = new InitializedList<double>(mesher.layout().size());
         a02_ = new InitializedList<double>(mesher.layout().size());
         a12_ = new InitializedList<double>(mesher.layout().size());
         a22_ = new InitializedList<double>(mesher.layout().size());
         mesher_ = mesher;

         Utils.QL_REQUIRE(d0_ != d1_
                          && d0_ < mesher.layout().dim().Count
                          && d1_ < mesher.layout().dim().Count,
                          () => "inconsistent derivative directions");

         FdmLinearOpLayout layout = mesher.layout();
         FdmLinearOpIterator endIter = layout.end();

         for (FdmLinearOpIterator iter = layout.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();

            i10_[i] = layout.neighbourhood(iter, d1_, -1);
            i01_[i] = layout.neighbourhood(iter, d0_, -1);
            i21_[i] = layout.neighbourhood(iter, d0_, 1);
            i12_[i] = layout.neighbourhood(iter, d1_, 1);
            i00_[i] = layout.neighbourhood(iter, d0_, -1, d1_, -1);
            i20_[i] = layout.neighbourhood(iter, d0_, 1, d1_, -1);
            i02_[i] = layout.neighbourhood(iter, d0_, -1, d1_, 1);
            i22_[i] = layout.neighbourhood(iter, d0_, 1, d1_, 1);
         }
      }
      public NinePointLinearOp(NinePointLinearOp m)
      {
         d0_ = m.d0_;
         d1_ = m.d1_;
         i00_ = new InitializedList<int>(m.mesher_.layout().size());
         i10_ = new InitializedList<int>(m.mesher_.layout().size());
         i20_ = new InitializedList<int>(m.mesher_.layout().size());
         i01_ = new InitializedList<int>(m.mesher_.layout().size());
         i21_ = new InitializedList<int>(m.mesher_.layout().size());
         i02_ = new InitializedList<int>(m.mesher_.layout().size());
         i12_ = new InitializedList<int>(m.mesher_.layout().size());
         i22_ = new InitializedList<int>(m.mesher_.layout().size());
         a00_ = new InitializedList<double>(m.mesher_.layout().size());
         a10_ = new InitializedList<double>(m.mesher_.layout().size());
         a20_ = new InitializedList<double>(m.mesher_.layout().size());
         a01_ = new InitializedList<double>(m.mesher_.layout().size());
         a11_ = new InitializedList<double>(m.mesher_.layout().size());
         a21_ = new InitializedList<double>(m.mesher_.layout().size());
         a02_ = new InitializedList<double>(m.mesher_.layout().size());
         a12_ = new InitializedList<double>(m.mesher_.layout().size());
         a22_ = new InitializedList<double>(m.mesher_.layout().size());
         mesher_ = m.mesher_;

         m.i00_.copy(0, m.i00_.Count, 0, i00_);
         m.i10_.copy(0, m.i10_.Count, 0, i10_);
         m.i20_.copy(0, m.i20_.Count, 0, i20_);
         m.i01_.copy(0, m.i01_.Count, 0, i01_);
         m.i21_.copy(0, m.i21_.Count, 0, i21_);
         m.i02_.copy(0, m.i02_.Count, 0, i02_);
         m.i12_.copy(0, m.i12_.Count, 0, i12_);
         m.i22_.copy(0, m.i22_.Count, 0, i22_);
         m.a00_.copy(0, m.a00_.Count, 0, a00_);
         m.a10_.copy(0, m.a10_.Count, 0, a10_);
         m.a20_.copy(0, m.a20_.Count, 0, a20_);
         m.a01_.copy(0, m.a01_.Count, 0, a01_);
         m.a11_.copy(0, m.a11_.Count, 0, a11_);
         m.a21_.copy(0, m.a21_.Count, 0, a21_);
         m.a02_.copy(0, m.a02_.Count, 0, a02_);
         m.a12_.copy(0, m.a12_.Count, 0, a12_);
         m.a22_.copy(0, m.a22_.Count, 0, a22_);
      }

      public override Vector apply(Vector r)
      {
         FdmLinearOpLayout index = mesher_.layout();
         Utils.QL_REQUIRE(r.size() == index.size(), () => "inconsistent length of r "
                          + r.size() + " vs " + index.size());

         Vector retVal = new Vector(r.size());

         //#pragma omp parallel for
         for (int i = 0; i < retVal.size(); ++i)
         {
            retVal[i] =   a00_[i] * r[i00_[i]]
                          + a01_[i] * r[i01_[i]]
                          + a02_[i] * r[i02_[i]]
                          + a10_[i] * r[i10_[i]]
                          + a11_[i] * r[i]
                          + a12_[i] * r[i12_[i]]
                          + a20_[i] * r[i20_[i]]
                          + a21_[i] * r[i21_[i]]
                          + a22_[i] * r[i22_[i]];
         }
         return retVal;
      }
      public NinePointLinearOp mult(Vector r)
      {
         NinePointLinearOp retVal = new NinePointLinearOp(d0_, d1_, mesher_);
         int size = mesher_.layout().size();

         //#pragma omp parallel for
         for (int i = 0; i < size; ++i)
         {
            double s = r[i];
            retVal.a11_[i] = a11_[i] * s; retVal.a00_[i] = a00_[i] * s;
            retVal.a01_[i] = a01_[i] * s; retVal.a02_[i] = a02_[i] * s;
            retVal.a10_[i] = a10_[i] * s; retVal.a20_[i] = a20_[i] * s;
            retVal.a21_[i] = a21_[i] * s; retVal.a12_[i] = a12_[i] * s;
            retVal.a22_[i] = a22_[i] * s;
         }

         return retVal;
      }

      public override SparseMatrix toMatrix()
      {
         FdmLinearOpLayout index = mesher_.layout();
         int n = index.size();

         SparseMatrix retVal = new SparseMatrix(n, n);
         for (int i = 0; i < index.size(); ++i)
         {
            retVal[i, i00_[i]] += a00_[i];
            retVal[i, i01_[i]] += a01_[i];
            retVal[i, i02_[i]] += a02_[i];
            retVal[i, i10_[i]] += a10_[i];
            retVal[i, i      ] += a11_[i];
            retVal[i, i12_[i]] += a12_[i];
            retVal[i, i20_[i]] += a20_[i];
            retVal[i, i21_[i]] += a21_[i];
            retVal[i, i22_[i]] += a22_[i];
         }

         return retVal;
      }

      public void swap(NinePointLinearOp m)
      {
         Utils.swap(ref d0_, ref m.d0_);
         Utils.swap(ref d1_, ref m.d1_);

         Utils.swap(ref i00_, ref m.i00_); Utils.swap(ref i10_, ref m.i10_); Utils.swap(ref i20_, ref m.i20_);
         Utils.swap(ref i01_, ref m.i01_); Utils.swap(ref i21_, ref m.i21_); Utils.swap(ref i02_, ref m.i02_);
         Utils.swap(ref i12_, ref m.i12_); Utils.swap(ref i22_, ref m.i22_);
         Utils.swap(ref a00_, ref m.a00_); Utils.swap(ref a10_, ref m.a10_); Utils.swap(ref a20_, ref m.a20_);
         Utils.swap(ref a01_, ref m.a01_); Utils.swap(ref a21_, ref m.a21_); Utils.swap(ref a02_, ref m.a02_);
         Utils.swap(ref a12_, ref m.a12_); Utils.swap(ref a22_, ref m.a22_); Utils.swap(ref a11_, ref m.a11_);

         Utils.swap(ref mesher_, ref m.mesher_);
      }

      #region IOperator interface
      public override int size() { return 0; }
      public override IOperator identity(int size) { return null; }
      public override Vector applyTo(Vector v) { return new Vector(); }
      public override Vector solveFor(Vector rhs) { return new Vector(); }

      public override IOperator multiply(double a, IOperator D) { return null; }
      public override IOperator add
         (IOperator A, IOperator B) { return null; }
      public override IOperator subtract(IOperator A, IOperator B) { return null; }

      public override bool isTimeDependent() { return false; }
      public override void setTime(double t) { }
      public override object Clone() { return this.MemberwiseClone(); }
      #endregion

      protected int d0_, d1_;
      protected List<int> i00_, i10_, i20_;
      protected List<int> i01_, i21_;
      protected List<int> i02_, i12_, i22_;
      protected List<double> a00_, a10_, a20_;
      protected List<double> a01_, a11_, a21_;
      protected List<double> a02_, a12_, a22_;

      protected FdmMesher mesher_;
   }
}
