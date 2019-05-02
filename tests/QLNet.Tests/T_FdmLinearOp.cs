/*
 Copyright (C) 2008 Andrea Maggiulli

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

#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif

using QLNet;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_FdmLinearOp
   {
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmLinearOpLayout()
      {

         int[] dims = new int[] {5, 7, 8};
         List<int> dim = new List<int>(dims);

         FdmLinearOpLayout layout = new FdmLinearOpLayout(dim);

         int calculatedDim = layout.dim().Count;
         int expectedDim = dim.Count;
         if (calculatedDim != expectedDim)
         {
            QAssert.Fail("index.dimensions() should be " + expectedDim
                         + ", but is " + calculatedDim);
         }

         int calculatedSize = layout.size();
         int expectedSize = dim.accumulate(0, 3, 1, (x, y) => (x * y));

         if (calculatedSize != expectedSize)
         {
            QAssert.Fail("index.size() should be "
                         + expectedSize + ", but is " + calculatedSize);
         }

         for (int k = 0; k < dim[0]; ++k)
         {
            for (int l = 0; l < dim[1]; ++l)
            {
               for (int m = 0; m < dim[2]; ++m)
               {
                  List<int> tmp = new InitializedList<int>(3);
                  tmp[0] = k; tmp[1] = l; tmp[2] = m;

                  int calculatedIndex = layout.index(tmp);
                  int expectedIndex = k + l * dim[0] + m * dim[0] * dim[1];

                  if (expectedIndex != layout.index(tmp))
                  {
                     QAssert.Fail("index.size() should be " + expectedIndex
                                  + ", but is " + calculatedIndex);
                  }
               }
            }
         }

         FdmLinearOpIterator iter = layout.begin();

         for (int m = 0; m < dim[2]; ++m)
         {
            for (int l = 0; l < dim[1]; ++l)
            {
               for (int k = 0; k < dim[0]; ++k, ++iter)
               {
                  for (int n = 1; n < 4; ++n)
                  {
                     int nn = layout.neighbourhood(iter, 1, n);
                     int calculatedIndex = k + m * dim[0] * dim[1]
                                           + ((l < dim[1] - n)? l + n
                                              : dim[1] - 1 - (l + n - (dim[1] - 1))) * dim[0];

                     if (nn != calculatedIndex)
                     {
                        QAssert.Fail("next neighbourhood index is " + nn
                                     + " but should be " + calculatedIndex);
                     }
                  }

                  for (int n = 1; n < 7; ++n)
                  {
                     int nn = layout.neighbourhood(iter, 2, - n);
                     int calculatedIndex = k + l * dim[0]
                                           + ((m < n) ? n - m : m - n) * dim[0] * dim[1];
                     if (nn != calculatedIndex)
                     {
                        QAssert.Fail("next neighbourhood index is " + nn
                                     + " but should be " + calculatedIndex);
                     }
                  }
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testUniformGridMesher()
      {
         int[] dims = new int[] {5, 7, 8};
         List<int> dim = new List<int>(dims);

         FdmLinearOpLayout layout = new FdmLinearOpLayout(dim);
         List < Pair < double?, double? >> boundaries = new List < Pair < double?, double? >> ();;
         boundaries.Add(new Pair < double?, double? >(-5, 10));
         boundaries.Add(new Pair < double?, double? >(5, 100));
         boundaries.Add(new Pair < double?, double? >(10, 20));

         UniformGridMesher mesher = new UniformGridMesher(layout, boundaries);

         double dx1 = 15.0 / (dim[0] - 1);
         double dx2 = 95.0 / (dim[1] - 1);
         double dx3 = 10.0 / (dim[2] - 1);

         double tol = 100 * Const.QL_EPSILON;
         if (Math.Abs(dx1 - mesher.dminus(layout.begin(), 0).Value) > tol
             || Math.Abs(dx1 - mesher.dplus(layout.begin(), 0).Value) > tol
             || Math.Abs(dx2 - mesher.dminus(layout.begin(), 1).Value) > tol
             || Math.Abs(dx2 - mesher.dplus(layout.begin(), 1).Value) > tol
             || Math.Abs(dx3 - mesher.dminus(layout.begin(), 2).Value) > tol
             || Math.Abs(dx3 - mesher.dplus(layout.begin(), 2).Value) > tol)
         {
            QAssert.Fail("inconsistent uniform mesher object");
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFirstDerivativesMapApply()
      {
         int[] dims = new int[] {400, 100, 50};
         List<int> dim = new List<int>(dims);

         FdmLinearOpLayout index = new FdmLinearOpLayout(dim);

         List < Pair < double?, double? > > boundaries = new List < Pair < double?, double? >> ();
         boundaries.Add(new Pair < double?, double? >(-5, 5));
         boundaries.Add(new Pair < double?, double? >(0, 10));
         boundaries.Add(new Pair < double?, double? >(5, 15));

         FdmMesher mesher = new UniformGridMesher(index, boundaries);

         FirstDerivativeOp map = new FirstDerivativeOp(2, mesher);

         Vector r = new Vector(mesher.layout().size());
         FdmLinearOpIterator endIter = index.end();

         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            r[iter.index()] =  Math.Sin(mesher.location(iter, 0))
                               + Math.Cos(mesher.location(iter, 2));
         }

         Vector t = map.apply(r);
         double dz = (boundaries[2].second.Value - boundaries[2].first.Value) / (dims[2] - 1);
         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            int z = iter.coordinates()[2];

            int z0 = (z > 0) ? z - 1 : 1;
            int z2 = (z < dims[2] - 1) ? z + 1 : dims[2] - 2;
            double lz0 = boundaries[2].first.Value + z0 * dz;
            double lz2 = boundaries[2].first.Value + z2 * dz;

            double expected;
            if (z == 0)
            {
               expected = (Math.Cos(boundaries[2].first.Value + dz)
                           - Math.Cos(boundaries[2].first.Value)) / dz;
            }
            else if (z == dim[2] - 1)
            {
               expected = (Math.Cos(boundaries[2].second.Value)
                           - Math.Cos(boundaries[2].second.Value - dz)) / dz;
            }
            else
            {
               expected = (Math.Cos(lz2) - Math.Cos(lz0)) / (2 * dz);
            }

            double calculated = t[iter.index()];
            if (Math.Abs(calculated - expected) > 1e-10)
            {
               QAssert.Fail("first derivative calculation failed."
                            + "\n    calculated: " + calculated
                            + "\n    expected:   " + expected);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSecondDerivativesMapApply()
      {
         int[] dims = new int[] {50, 50, 50};
         List<int> dim = new List<int>(dims);

         FdmLinearOpLayout index = new FdmLinearOpLayout(dim);

         List < Pair < double?, double? > > boundaries = new List < Pair < double?, double? >> ();
         boundaries.Add(new Pair < double?, double? >(0, 0.5));
         boundaries.Add(new Pair < double?, double? >(0, 0.5));
         boundaries.Add(new Pair < double?, double? >(0, 0.5));

         FdmMesher mesher = new UniformGridMesher(index, boundaries);

         Vector r = new Vector(mesher.layout().size());
         FdmLinearOpIterator endIter = index.end();

         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            r[iter.index()] = Math.Sin(x) * Math.Cos(y) * Math.Exp(z);
         }

         Vector t = new SecondDerivativeOp(0, mesher).apply(r);

         double tol = 5e-2;
         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            double d = -Math.Sin(x) * Math.Cos(y) * Math.Exp(z);
            if (iter.coordinates()[0] == 0 || iter.coordinates()[0] == dims[0] - 1)
            {
               d = 0;
            }

            if (Math.Abs(d - t[i]) > tol)
            {
               QAssert.Fail("numerical derivative in dx^2 deviation is too big"
                            + "\n  found at " + x + " " + y + " " + z);
            }
         }

         t = new SecondDerivativeOp(1, mesher).apply(r);
         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            double d = -Math.Sin(x) * Math.Cos(y) * Math.Exp(z);
            if (iter.coordinates()[1] == 0 || iter.coordinates()[1] == dims[1] - 1)
            {
               d = 0;
            }

            if (Math.Abs(d - t[i]) > tol)
            {
               QAssert.Fail("numerical derivative in dy^2 deviation is too big"
                            + "\n  found at " + x + " " + y + " " + z);
            }
         }

         t = new SecondDerivativeOp(2, mesher).apply(r);
         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            double d = Math.Sin(x) * Math.Cos(y) * Math.Exp(z);
            if (iter.coordinates()[2] == 0 || iter.coordinates()[2] == dims[2] - 1)
            {
               d = 0;
            }

            if (Math.Abs(d - t[i]) > tol)
            {
               QAssert.Fail("numerical derivative in dz^2 deviation is too big"
                            + "\n  found at " + x + " " + y + " " + z);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testDerivativeWeightsOnNonUniformGrids()
      {
         Fdm1dMesher mesherX =
            new Concentrating1dMesher(-2.0, 3.0, 50, new Pair < double?, double? >(0.5, 0.01));
         Fdm1dMesher mesherY =
            new Concentrating1dMesher(0.5, 5.0, 25, new Pair < double?, double? >(0.5, 0.1));
         Fdm1dMesher mesherZ =
            new Concentrating1dMesher(-1.0, 2.0, 31, new Pair < double?, double? >(1.5, 0.01));

         FdmMesher meshers =
            new FdmMesherComposite(mesherX, mesherY, mesherZ);

         FdmLinearOpLayout layout = meshers.layout();
         FdmLinearOpIterator endIter = layout.end();

         double tol = 1e-13;
         for (int direction = 0; direction < 3; ++direction)
         {

            SparseMatrix dfdx
               = new FirstDerivativeOp(direction, meshers).toMatrix();
            SparseMatrix d2fdx2
               = new SecondDerivativeOp(direction, meshers).toMatrix();

            Vector gridPoints = meshers.locations(direction);

            for (FdmLinearOpIterator iter = layout.begin();
                 iter != endIter; ++iter)
            {

               int c = iter.coordinates()[direction];
               int index   = iter.index();
               int indexM1 = layout.neighbourhood(iter, direction, -1);
               int indexP1 = layout.neighbourhood(iter, direction, +1);

               // test only if not on the boundary
               if (c == 0)
               {
                  Vector twoPoints = new Vector(2);
                  twoPoints[0] = 0.0;
                  twoPoints[1] = gridPoints[indexP1] - gridPoints[index];

                  Vector ndWeights1st = new NumericalDifferentiation(x => x, 1, twoPoints).weights();

                  double beta1  = dfdx[index, index];
                  double gamma1 = dfdx[index, indexP1];
                  if (Math.Abs((beta1  - ndWeights1st[0]) / beta1) > tol
                      || Math.Abs((gamma1 - ndWeights1st[1]) / gamma1) > tol)
                  {
                     QAssert.Fail("can not reproduce the weights of the "
                                  + "first order derivative operator "
                                  + "on the lower boundary"
                                  + "\n expected beta:    " + ndWeights1st[0]
                                  + "\n calculated beta:  " + beta1
                                  + "\n difference beta:  "
                                  + (beta1 - ndWeights1st[0])
                                  + "\n expected gamma:   " + ndWeights1st[1]
                                  + "\n calculated gamma: " + gamma1
                                  + "\n difference gamma: "
                                  + (gamma1 - ndWeights1st[1]));
                  }

                  // free boundary condition by default
                  double beta2  = d2fdx2[index, index];
                  double gamma2 = d2fdx2[index, indexP1];

                  if (Math.Abs(beta2)  > Const.QL_EPSILON
                      || Math.Abs(gamma2) > Const.QL_EPSILON)
                  {
                     QAssert.Fail("can not reproduce the weights of the "
                                  + "second order derivative operator "
                                  + "on the lower boundary"
                                  + "\n expected beta:    " + 0.0
                                  + "\n calculated beta:  " + beta2
                                  + "\n expected gamma:   " + 0.0
                                  + "\n calculated gamma: " + gamma2);
                  }
               }
               else if (c == layout.dim()[direction] - 1)
               {
                  Vector twoPoints = new Vector(2);
                  twoPoints[0] = gridPoints[indexM1] - gridPoints[index];
                  twoPoints[1] = 0.0;

                  Vector ndWeights1st = new NumericalDifferentiation(x => x, 1, twoPoints).weights();

                  double alpha1 = dfdx[index, indexM1];
                  double beta1  = dfdx[index, index];
                  if (Math.Abs((alpha1 - ndWeights1st[0]) / alpha1) > tol
                      || Math.Abs((beta1  - ndWeights1st[1]) / beta1) > tol)
                  {
                     QAssert.Fail("can not reproduce the weights of the "
                                  + "first order derivative operator "
                                  + "on the upper boundary"
                                  + "\n expected alpha:   " + ndWeights1st[0]
                                  + "\n calculated alpha: " + alpha1
                                  + "\n difference alpha: "
                                  + (alpha1 - ndWeights1st[0])
                                  + "\n expected beta:    " + ndWeights1st[1]
                                  + "\n calculated beta:  " + beta1
                                  + "\n difference beta:  "
                                  + (beta1 - ndWeights1st[1]));
                  }

                  // free boundary condition by default
                  double alpha2 = d2fdx2[index, indexM1];
                  double beta2  = d2fdx2[index, index];

                  if (Math.Abs(alpha2)  > Const.QL_EPSILON
                      || Math.Abs(beta2) > Const.QL_EPSILON)
                  {
                     QAssert.Fail("can not reproduce the weights of the "
                                  + "second order derivative operator "
                                  + "on the upper boundary"
                                  + "\n expected alpha:   " + 0.0
                                  + "\n calculated alpha: " + alpha2
                                  + "\n expected beta:    " + 0.0
                                  + "\n calculated beta:  " + beta2);
                  }
               }
               else
               {
                  Vector threePoints = new Vector(3);
                  threePoints[0] = gridPoints[indexM1] - gridPoints[index];
                  threePoints[1] = 0.0;
                  threePoints[2] = gridPoints[indexP1] - gridPoints[index];

                  Vector ndWeights1st = new NumericalDifferentiation(x => x, 1, threePoints).weights();

                  double alpha1 = dfdx[index, indexM1];
                  double beta1  = dfdx[index, index];
                  double gamma1 = dfdx[index, indexP1];

                  if (Math.Abs((alpha1 - ndWeights1st[0]) / alpha1) > tol
                      || Math.Abs((beta1  - ndWeights1st[1]) / beta1) > tol
                      || Math.Abs((gamma1 - ndWeights1st[2]) / gamma1) > tol)
                  {
                     QAssert.Fail("can not reproduce the weights of the "
                                  + "first order derivative operator"
                                  + "\n expected alpha:   " + ndWeights1st[0]
                                  + "\n calculated alpha: " + alpha1
                                  + "\n difference alpha: "
                                  + (alpha1 - ndWeights1st[0])
                                  + "\n expected beta:    " + ndWeights1st[1]
                                  + "\n calculated beta:  " + beta1
                                  + "\n difference beta:  "
                                  + (beta1 - ndWeights1st[1])
                                  + "\n expected gamma:   " + ndWeights1st[2]
                                  + "\n calculated gamma: " + gamma1
                                  + "\n difference gamma: "
                                  + (gamma1 - ndWeights1st[2]));
                  }

                  Vector ndWeights2nd = new NumericalDifferentiation(x => x, 2, threePoints).weights();

                  double alpha2 = d2fdx2[index, indexM1];
                  double beta2  = d2fdx2[index, index];
                  double gamma2 = d2fdx2[index, indexP1];
                  if (Math.Abs((alpha2 - ndWeights2nd[0]) / alpha2) > tol
                      || Math.Abs((beta2  - ndWeights2nd[1]) / beta2) > tol
                      || Math.Abs((gamma2 - ndWeights2nd[2]) / gamma2) > tol)
                  {
                     QAssert.Fail("can not reproduce the weights of the "
                                  + "second order derivative operator"
                                  + "\n expected alpha:   " + ndWeights2nd[0]
                                  + "\n calculated alpha: " + alpha2
                                  + "\n difference alpha: "
                                  + (alpha2 - ndWeights2nd[0])
                                  + "\n expected beta:    " + ndWeights2nd[1]
                                  + "\n calculated beta:  " + beta2
                                  + "\n difference beta:  "
                                  + (beta2 - ndWeights2nd[1])
                                  + "\n expected gamma:   " + ndWeights2nd[2]
                                  + "\n calculated gamma: " + gamma2
                                  + "\n difference gamma: "
                                  + (gamma2 - ndWeights2nd[2]));
                  }
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSecondOrderMixedDerivativesMapApply()
      {
         int[] dims = new int[] {50, 50, 50};
         List<int> dim = new List<int>(dims);

         FdmLinearOpLayout index = new FdmLinearOpLayout(dim);

         List < Pair < double?, double? >> boundaries = new List < Pair < double?, double? >> ();
         boundaries.Add(new Pair < double?, double? >(0, 0.5));
         boundaries.Add(new Pair < double?, double? >(0, 0.5));
         boundaries.Add(new Pair < double?, double? >(0, 0.5));

         FdmMesher mesher = new UniformGridMesher(index, boundaries);

         Vector r = new Vector(mesher.layout().size());
         FdmLinearOpIterator endIter = index.end();

         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            r[iter.index()] = Math.Sin(x) * Math.Cos(y) * Math.Exp(z);
         }

         Vector t = new SecondOrderMixedDerivativeOp(0, 1, mesher).apply(r);
         Vector u = new SecondOrderMixedDerivativeOp(1, 0, mesher).apply(r);

         double tol = 5e-2;
         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            double d = -Math.Cos(x) * Math.Sin(y) * Math.Exp(z);

            if (Math.Abs(d - t[i]) > tol)
            {
               QAssert.Fail("numerical derivative in dxdy deviation is too big"
                            + "\n  found at " + x + " " + y + " " + z);
            }

            if (Math.Abs(t[i] - u[i]) > 1e5 * Const.QL_EPSILON)
            {
               QAssert.Fail("numerical derivative in dxdy not equal to dydx"
                            + "\n  found at " + x + " " + y + " " + z
                            + "\n  value    " + Math.Abs(t[i] - u[i]));
            }
         }

         t = new SecondOrderMixedDerivativeOp(0, 2, mesher).apply(r);
         u = new SecondOrderMixedDerivativeOp(2, 0, mesher).apply(r);
         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            double d = Math.Cos(x) * Math.Cos(y) * Math.Exp(z);

            if (Math.Abs(d - t[i]) > tol)
            {
               QAssert.Fail("numerical derivative in dxdy deviation is too big"
                            + "\n  found at " + x + " " + y + " " + z);
            }

            if (Math.Abs(t[i] - u[i]) > 1e5 * Const.QL_EPSILON)
            {
               QAssert.Fail("numerical derivative in dxdz not equal to dzdx"
                            + "\n  found at " + x + " " + y + " " + z
                            + "\n  value    " + Math.Abs(t[i] - u[i]));
            }
         }

         t = new SecondOrderMixedDerivativeOp(1, 2, mesher).apply(r);
         u = new SecondOrderMixedDerivativeOp(2, 1, mesher).apply(r);
         for (FdmLinearOpIterator iter = index.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            double d = -Math.Sin(x) * Math.Sin(y) * Math.Exp(z);

            if (Math.Abs(d - t[i]) > tol)
            {
               QAssert.Fail("numerical derivative in dydz deviation is too big"
                            + "\n  found at " + x + " " + y + " " + z);
            }

            if (Math.Abs(t[i] - u[i]) > 1e5 * Const.QL_EPSILON)
            {
               QAssert.Fail("numerical derivative in dydz not equal to dzdy"
                            + "\n  found at " + x + " " + y + " " + z
                            + "\n  value    " + Math.Abs(t[i] - u[i]));
            }
         }


      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testTripleBandMapSolve()
      {
         int[] dims = new int[] {100, 400};
         List<int> dim = new List<int>(dims);

         FdmLinearOpLayout layout = new FdmLinearOpLayout(dim);

         List < Pair < double?, double? >> boundaries = new List < Pair < double?, double? >> ();
         boundaries.Add(new Pair < double?, double? >(0, 1.0));
         boundaries.Add(new Pair < double?, double? >(0, 1.0));

         FdmMesher mesher = new UniformGridMesher(layout, boundaries);

         FirstDerivativeOp dy = new FirstDerivativeOp(1, mesher);
         dy.axpyb(new Vector(1, 2.0), dy, dy, new Vector(1, 1.0));

         // check copy constructor
         FirstDerivativeOp copyOfDy = new FirstDerivativeOp(dy);

         Vector u = new Vector(layout.size());
         for (int i = 0; i < layout.size(); ++i)
            u[i] = Math.Sin(0.1 * i) + Math.Cos(0.35 * i);

         Vector t = new Vector(dy.solve_splitting(copyOfDy.apply(u), 1.0, 0.0));
         for (int i = 0; i < u.size(); ++i)
         {
            if (Math.Abs(u[i] - t[i]) > 1e-6)
            {
               QAssert.Fail("solve and apply are not consistent "
                            + "\n expected      : " + u[i]
                            + "\n calculated    : " + t[i]);
            }
         }

         FirstDerivativeOp dx = new FirstDerivativeOp(0, mesher);
         dx.axpyb(new Vector(), dx, dx, new Vector(1, 1.0));

         FirstDerivativeOp copyOfDx = new FirstDerivativeOp(0, mesher);
         // check assignment
         copyOfDx = dx;

         t = dx.solve_splitting(copyOfDx.apply(u), 1.0, 0.0);
         for (int i = 0; i < u.size(); ++i)
         {
            if (Math.Abs(u[i] - t[i]) > 1e-6)
            {
               QAssert.Fail("solve and apply are not consistent "
                            + "\n expected      : " + u[i]
                            + "\n calculated    : " + t[i]);
            }
         }

         SecondDerivativeOp dxx = new SecondDerivativeOp(0, mesher);
         dxx.axpyb(new Vector(1, 0.5), dxx, dx, new Vector(1, 1.0));

         // check of copy constructor
         SecondDerivativeOp copyOfDxx = new SecondDerivativeOp(dxx);

         t = dxx.solve_splitting(copyOfDxx.apply(u), 1.0, 0.0);

         for (int i = 0; i < u.size(); ++i)
         {
            if (Math.Abs(u[i] - t[i]) > 1e-6)
            {
               QAssert.Fail("solve and apply are not consistent "
                            + "\n expected      : " + u[i]
                            + "\n calculated    : " + t[i]);
            }
         }

         //check assignment operator
         copyOfDxx.add(new SecondDerivativeOp(1, mesher));
         copyOfDxx = dxx;

         t = dxx.solve_splitting(copyOfDxx.apply(u), 1.0, 0.0);

         for (int i = 0; i < u.size(); ++i)
         {
            if (Math.Abs(u[i] - t[i]) > 1e-6)
            {
               QAssert.Fail("solve and apply are not consistent "
                            + "\n expected      : " + u[i]
                            + "\n calculated    : " + t[i]);
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCrankNicolsonWithDamping()
      {
         SavedSettings backup = new SavedSettings();

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(100.0);
         YieldTermStructure qTS = Utilities.flatRate(today, 0.06, dc);
         YieldTermStructure rTS = Utilities.flatRate(today, 0.06, dc);
         BlackVolTermStructure volTS = Utilities.flatVol(today, 0.35, dc);

         StrikedTypePayoff payoff =
            new CashOrNothingPayoff(Option.Type.Put, 100, 10.0);

         double maturity = 0.75;
         Date exDate = today + Convert.ToInt32(maturity * 360 + 0.5);
         Exercise exercise = new EuropeanExercise(exDate);

         BlackScholesMertonProcess process = new
         BlackScholesMertonProcess(new Handle<Quote>(spot),
                                   new Handle<YieldTermStructure>(qTS),
                                   new Handle<YieldTermStructure>(rTS),
                                   new Handle<BlackVolTermStructure>(volTS));
         IPricingEngine engine =
            new AnalyticEuropeanEngine(process);

         VanillaOption opt = new VanillaOption(payoff, exercise);
         opt.setPricingEngine(engine);
         double expectedPV = opt.NPV();
         double expectedGamma = opt.gamma();

         // fd pricing using implicit damping steps and Crank Nicolson
         int csSteps = 25, dampingSteps = 3, xGrid = 400;
         List<int> dim = new InitializedList<int>(1, xGrid);

         FdmLinearOpLayout layout = new FdmLinearOpLayout(dim);
         Fdm1dMesher equityMesher =
            new FdmBlackScholesMesher(
            dim[0], process, maturity, payoff.strike(),
            null, null, 0.0001, 1.5,
            new Pair < double?, double? >(payoff.strike(), 0.01));

         FdmMesher mesher =
            new FdmMesherComposite(equityMesher);

         FdmBlackScholesOp map =
            new FdmBlackScholesOp(mesher, process, payoff.strike());

         FdmInnerValueCalculator calculator =
            new FdmLogInnerValue(payoff, mesher, 0);

         object rhs = new Vector(layout.size());
         Vector x = new Vector(layout.size());
         FdmLinearOpIterator endIter = layout.end();

         for (FdmLinearOpIterator iter = layout.begin(); iter != endIter;
              ++iter)
         {
            (rhs as Vector)[iter.index()] = calculator.avgInnerValue(iter, maturity);
            x[iter.index()] = mesher.location(iter, 0);
         }

         FdmBackwardSolver solver = new FdmBackwardSolver(map, new FdmBoundaryConditionSet(),
                                                          new FdmStepConditionComposite(),
                                                          new FdmSchemeDesc().Douglas());
         solver.rollback(ref rhs, maturity, 0.0, csSteps, dampingSteps);

         MonotonicCubicNaturalSpline spline = new MonotonicCubicNaturalSpline(x, x.Count, rhs as Vector);

         double s = spot.value();
         double calculatedPV = spline.value(Math.Log(s));
         double calculatedGamma = (spline.secondDerivative(Math.Log(s))
                                   - spline.derivative(Math.Log(s))) / (s * s);

         double relTol = 2e-3;

         if (Math.Abs(calculatedPV - expectedPV) > relTol * expectedPV)
         {
            QAssert.Fail("Error calculating the PV of the digital option" +
                         "\n rel. tolerance:  " + relTol +
                         "\n expected:        " + expectedPV +
                         "\n calculated:      " + calculatedPV);
         }
         if (Math.Abs(calculatedGamma - expectedGamma) > relTol * expectedGamma)
         {
            QAssert.Fail("Error calculating the Gamma of the digital option" +
                         "\n rel. tolerance:  " + relTol +
                         "\n expected:        " + expectedGamma +
                         "\n calculated:      " + calculatedGamma);
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSpareMatrixReference()
      {
         int rows    = 10;
         int columns = 10;
         int nMatrices = 5;
         int nElements = 50;

         MersenneTwisterUniformRng rng = new MersenneTwisterUniformRng(1234);

         SparseMatrix expected = new SparseMatrix(rows, columns);
         List<SparseMatrix> refs = new List<SparseMatrix>();

         for (int i = 0; i < nMatrices; ++i)
         {
            SparseMatrix m = new SparseMatrix(rows, columns);
            for (int j = 0; j < nElements; ++j)
            {
               int row    = Convert.ToInt32(rng.next().value * rows);
               int column = Convert.ToInt32(rng.next().value * columns);

               double value = rng.next().value;
               m[row, column]        += value;
               expected[row, column] += value;
            }

            refs.Add(m);
         }

         SparseMatrix calculated = refs.accumulate(1, refs.Count, refs[0], (a, b) => a + b);

         for (int i = 0; i < rows; ++i)
         {
            for (int j = 0; j < columns; ++j)
            {
               if (Math.Abs(calculated[i, j] - expected[i, j]) > 100 * Const.QL_EPSILON)
               {
                  QAssert.Fail("Error using sparse matrix references in " +
                               "Element (" + i + ", " + j + ")" +
                               "\n expected  : " + expected[i, j] +
                               "\n calculated: " + calculated[i, j]);
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFdmMesherIntegral()
      {
         FdmMesherComposite mesher =
            new FdmMesherComposite(
            new Concentrating1dMesher(-1, 1.6, 21, new Pair < double?, double? >(0, 0.1)),
            new Concentrating1dMesher(-3, 4, 11, new Pair < double?, double? >(1, 0.01)),
            new Concentrating1dMesher(-2, 1, 5, new Pair < double?, double? >(0.5, 0.1)));

         FdmLinearOpLayout layout = mesher.layout();

         Vector f = new Vector(mesher.layout().size());
         for (FdmLinearOpIterator iter = layout.begin();
              iter != layout.end(); ++iter)
         {
            double x = mesher.location(iter, 0);
            double y = mesher.location(iter, 1);
            double z = mesher.location(iter, 2);

            f[iter.index()] = x * x + 3 * y * y - 3 * z * z
                              + 2 * x * y - x * z - 3 * y * z
                              + 4 * x - y - 3 * z + 2 ;
         }

         double tol = 1e-12;

         // Simpson's rule has to be exact here, Mathematica code gives
         // Integrate[x*x+3*y*y-3*z*z+2*x*y-x*z-3*y*z+4*x-y-3*z+2,
         //           {x, -1, 16/10}, {y, -3, 4}, {z, -2, 1}]
         double expectedSimpson = 876.512;
         double calculatedSimpson
            = new FdmMesherIntegral(mesher, new DiscreteSimpsonIntegral().value).integrate(f);

         if (Math.Abs(calculatedSimpson - expectedSimpson) > tol * expectedSimpson)
         {
            QAssert.Fail("discrete mesher integration using Simpson's rule failed: "
                         + "\n    calculated: " + calculatedSimpson
                         + "\n    expected:   " + expectedSimpson);
         }

         double expectedTrapezoid = 917.0148209153263;
         double calculatedTrapezoid
            = new FdmMesherIntegral(mesher, new DiscreteTrapezoidIntegral().value).integrate(f);

         if (Math.Abs(calculatedTrapezoid - expectedTrapezoid)
             > tol * expectedTrapezoid)
         {
            QAssert.Fail("discrete mesher integration using Trapezoid rule failed: "
                         + "\n    calculated: " + calculatedTrapezoid
                         + "\n    expected:   " + expectedTrapezoid);
         }
      }
   }
}
